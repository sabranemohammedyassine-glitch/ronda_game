using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zellige.Core;

namespace Zellige.UI
{
    /// <summary>
    /// The match screen. Its objects are authored in Game.unity (built by
    /// Zellige > Build or Refresh Game UI, see GameScreenBuilder.cs) and referenced
    /// below; this class wires the buttons and animates the engine's events.
    /// It displays what it is told and contains no game rules.
    /// Cards are positioned from the "Card Areas" objects, so moving those in the
    /// editor moves where the cards go.
    /// </summary>
    public sealed partial class GameScreen : MonoBehaviour
    {
        public const int HumanSeat = 0;
        public const int OpponentSeat = 1;
        public static readonly string[] PlayerNames = { "You", "Nadia" };

        // Card sizes, as a scale of CardView.BaseSize (168x252).
        private const float HandScale = 1.05f;
        private const float OpponentScale = 0.69f;
        private const float TableScale = 0.8f;
        private const float DeckScale = 0.85f;
        private const float PileScale = 0.3f;
        private const float MoveTime = 0.28f;
        private const float TableSpacing = 150f;
        private const float HumanHandSpacing = 182f;
        private const float OpponentHandSpacing = 84f;

        /// <summary>The widgets of one player's panel.</summary>
        [Serializable]
        private sealed class PlayerPanel
        {
            public Image TurnRing;
            public TextMeshProUGUI Role;
            public TextMeshProUGUI Captured;
            [Tooltip("Where captured cards fly to.")]
            public RectTransform PileTarget;
            public GameObject DealerChip;
            [NonSerialized] public int CapturedCount;
        }

        [Header("Card layout")]
        [SerializeField] private RectTransform cardLayer;
        [SerializeField] private RectTransform tableArea;
        [SerializeField] private RectTransform humanHandArea;
        [SerializeField] private RectTransform opponentHandArea;
        [SerializeField] private RectTransform deck;
        [SerializeField] private CardView cardTemplate;

        [Header("Players and score (index 0 = you, 1 = opponent)")]
        [SerializeField] private PlayerPanel[] panels = new PlayerPanel[2];
        [SerializeField] private TextMeshProUGUI[] scoreLabels = new TextMeshProUGUI[2];
        [SerializeField] private TextMeshProUGUI deckLabel;
        [SerializeField] private TextMeshProUGUI hintLabel;

        [Header("Call-out banner")]
        [SerializeField] private RectTransform callout;
        [SerializeField] private TextMeshProUGUI calloutTitle;
        [SerializeField] private TextMeshProUGUI calloutDetail;

        [Header("Buttons")]
        [SerializeField] private Button backgroundButton;
        [SerializeField] private Button announceButton;
        [SerializeField] private TextMeshProUGUI announceLabel;
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundIcon;
        [SerializeField] private TextMeshProUGUI soundFallbackLabel;
        [SerializeField] private Sprite soundOnIcon;
        [SerializeField] private Sprite soundOffIcon;
        [SerializeField] private Button newGameButton;

        [Header("Result screen")]
        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private Image resultIllustration;
        [SerializeField] private Sprite victoryIllustration;
        [SerializeField] private Sprite defeatIllustration;
        [SerializeField] private TextMeshProUGUI resultTitle;
        [SerializeField] private TextMeshProUGUI resultScore;
        [SerializeField] private Button playAgainButton;

        [Header("Audio")]
        [SerializeField] private SoundFx sound;

        private readonly Dictionary<Card, CardView> views = new Dictionary<Card, CardView>();
        private readonly List<CardView>[] hands = { new List<CardView>(), new List<CardView>() };
        private readonly List<CardView> tableCards = new List<CardView>();
        private CardView selected;
        private int activeSeat = -1;

        /// <summary>Raised when the player taps any card.</summary>
        public event Action<CardView> CardClicked;
        /// <summary>Raised when the player taps the table background.</summary>
        public event Action BackgroundClicked;
        public event Action AnnounceClicked;
        public event Action NewGameClicked;

        public CardView Selected => selected;

        private void Awake()
        {
            if (cardTemplate == null || cardLayer == null || panels == null || panels.Length != 2 || panels[0].TurnRing == null)
            {
                Debug.LogError("Zellige: the Game UI is incomplete. Run the menu Zellige > Build or Refresh Game UI.", this);
                enabled = false;
                return;
            }

            cardTemplate.gameObject.SetActive(false);
            backgroundButton.onClick.AddListener(() => BackgroundClicked?.Invoke());
            announceButton.onClick.AddListener(() => AnnounceClicked?.Invoke());
            soundButton.onClick.AddListener(ToggleSound);
            newGameButton.onClick.AddListener(() => NewGameClicked?.Invoke());
            playAgainButton.onClick.AddListener(() => NewGameClicked?.Invoke());
        }

        // ---------- Layout ----------

        /// <summary>Position of any UI object in the card layer's coordinates (works whatever its anchors).</summary>
        private Vector2 LocalPosition(RectTransform target)
        {
            return cardLayer.InverseTransformPoint(target.position);
        }

        /// <summary>Table cards sit in one row sorted by rank, so chains read left to right.</summary>
        private void LayoutTable(float duration)
        {
            tableCards.Sort((a, b) => Ranks.IndexOf(a.Card.Rank).CompareTo(Ranks.IndexOf(b.Card.Rank)));
            Vector2 center = LocalPosition(tableArea);
            float maxSpan = Mathf.Max(0f, tableArea.rect.width - CardView.BaseSize.x * TableScale);
            int count = tableCards.Count;
            float spacing = count > 1 ? Mathf.Min(TableSpacing, maxSpan / (count - 1)) : 0f;
            for (int i = 0; i < count; i++)
            {
                Vector2 target = center + new Vector2((i - (count - 1) / 2f) * spacing, 0f);
                tableCards[i].Rect.SetSiblingIndex(i);
                StartCoroutine(Move(tableCards[i].Rect, target, TableScale, 0f, duration));
            }
        }

        private void LayoutHand(int seat, float duration)
        {
            List<CardView> hand = hands[seat];
            for (int i = 0; i < hand.Count; i++)
            {
                GetHandSlot(seat, i, hand.Count, out Vector2 position, out float angle);
                if (hand[i] == selected) position += new Vector2(0f, 44f);
                StartCoroutine(Move(hand[i].Rect, position, seat == HumanSeat ? HandScale : OpponentScale, angle, duration));
            }
        }

        /// <summary>Hands are drawn as a gentle fan around their area's centre.</summary>
        private void GetHandSlot(int seat, int index, int count, out Vector2 position, out float angle)
        {
            float offset = index - (count - 1) / 2f;
            if (seat == HumanSeat)
            {
                position = LocalPosition(humanHandArea) + new Vector2(offset * HumanHandSpacing, -offset * offset * 7f);
                angle = -offset * 4f;
            }
            else
            {
                position = LocalPosition(opponentHandArea) + new Vector2(offset * OpponentHandSpacing, offset * offset * 4f);
                angle = offset * 4f;
            }
        }

        private CardView CreateCard(Card card)
        {
            CardView view = Instantiate(cardTemplate, cardLayer);
            view.gameObject.SetActive(true);
            view.Init(card, v => CardClicked?.Invoke(v));
            return view;
        }

        // ---------- State shown outside animations ----------

        public void ResetAll()
        {
            StopAllCoroutines();
            moveIds.Clear();
            foreach (CardView view in views.Values) Destroy(view.gameObject);
            views.Clear();
            hands[0].Clear();
            hands[1].Clear();
            tableCards.Clear();
            selected = null;
            for (int seat = 0; seat < 2; seat++)
            {
                panels[seat].CapturedCount = 0;
                panels[seat].DealerChip.SetActive(false);
                scoreLabels[seat].text = "0";
            }
            RefreshCaptured();
            SetActiveSeat(-1);
            SetDeckCount(Deck.Size);
            callout.gameObject.SetActive(false);
            announceButton.gameObject.SetActive(false);
            resultOverlay.SetActive(false);
            cardLayer.gameObject.SetActive(true);
            hintLabel.text = "";
        }

        public void SetStatus(string text) => hintLabel.text = text;

        public void SetActiveSeat(int seat)
        {
            activeSeat = seat;
            for (int s = 0; s < 2; s++)
            {
                bool active = s == seat;
                panels[s].TurnRing.enabled = active;
                panels[s].TurnRing.transform.localScale = Vector3.one;
                string role = s == HumanSeat ? "PLAYER" : "COMPUTER";
                string activeRole = s == HumanSeat ? "YOUR TURN" : "THINKING...";
                panels[s].Role.text = active ? activeRole : role;
                panels[s].Role.color = active ? Palette.Orange : Palette.Silver;
            }
        }

        /// <summary>Shows the announce button with the given label, or hides it when the label is null.</summary>
        public void SetAnnounceButton(string text)
        {
            announceButton.gameObject.SetActive(text != null);
            if (text != null) announceLabel.text = text;
        }

        /// <summary>Lifts the chosen hand card and highlights the table cards it would take.</summary>
        public void Select(CardView view, IReadOnlyList<Card> wouldCapture)
        {
            ClearHighlights();
            selected = view;
            if (view != null)
            {
                view.SetHighlight(true);
                foreach (Card card in wouldCapture) views[card].SetHighlight(true);
            }
            LayoutHand(HumanSeat, 0.12f);
        }

        private void ClearHighlights()
        {
            foreach (CardView view in views.Values) view.SetHighlight(false);
        }

        private void SetDeckCount(int count)
        {
            deckLabel.text = count.ToString();
            deck.gameObject.SetActive(count > 0);
        }

        private void RefreshCaptured()
        {
            for (int seat = 0; seat < 2; seat++) panels[seat].Captured.text = panels[seat].CapturedCount.ToString();
        }

        private void ToggleSound()
        {
            sound.Muted = !sound.Muted;
            Sprite icon = sound.Muted ? soundOffIcon : soundOnIcon;
            soundIcon.sprite = icon;
            soundIcon.color = icon != null ? Color.white : Color.clear;
            if (soundFallbackLabel != null) soundFallbackLabel.text = sound.Muted ? "Off" : "Sound";
        }

        private void Update()
        {
            // Gentle pulse on the active player's turn ring.
            if (activeSeat >= 0)
            {
                float pulse = 1f + 0.04f * Mathf.Sin(Time.time * Mathf.PI * 2f);
                panels[activeSeat].TurnRing.transform.localScale = Vector3.one * pulse;
            }
        }

        // ---------- Event playback ----------

        public IEnumerator PlayDeckStarted(DeckStartedEvent e)
        {
            panels[0].CapturedCount = 0;
            panels[1].CapturedCount = 0;
            RefreshCaptured();
            SetDeckCount(Deck.Size);
            panels[0].DealerChip.SetActive(e.Dealer == 0);
            panels[1].DealerChip.SetActive(e.Dealer == 1);
            string dealer = e.Dealer == HumanSeat ? "You deal" : PlayerNames[e.Dealer] + " deals";
            yield return ShowCallout("DECK " + e.DeckRound, dealer + "  ·  first to 41 wins", 1.2f);
        }

        public IEnumerator PlayDeal(DealStartedEvent e)
        {
            int remaining = e.StockRemaining + e.Hands[0].Count + e.Hands[1].Count;
            int cardsEach = Mathf.Max(e.Hands[0].Count, e.Hands[1].Count);
            for (int i = 0; i < cardsEach; i++)
            {
                for (int seat = 0; seat < 2; seat++)
                {
                    if (i >= e.Hands[seat].Count) continue;
                    CardView view = CreateCard(e.Hands[seat][i]);
                    view.Rect.anchoredPosition = LocalPosition(deck);
                    view.Rect.localScale = Vector3.one * DeckScale;
                    views[view.Card] = view;
                    hands[seat].Add(view);
                    SetDeckCount(--remaining);
                    sound.PlayDeal();

                    GetHandSlot(seat, i, e.Hands[seat].Count, out Vector2 position, out float angle);
                    StartCoroutine(Move(view.Rect, position, seat == HumanSeat ? HandScale : OpponentScale, angle, MoveTime));
                    if (seat == HumanSeat) view.SetFaceUp(true);
                    yield return new WaitForSeconds(0.09f);
                }
            }
            yield return new WaitForSeconds(MoveTime);
        }

        public IEnumerator PlayCardPlayed(CardPlayedEvent e)
        {
            ClearHighlights();
            selected = null;
            hintLabel.text = "";

            CardView view = views[e.Card];
            hands[e.Seat].Remove(view);
            LayoutHand(e.Seat, 0.2f);
            view.SetFaceUp(true);
            view.Rect.SetAsLastSibling();

            if (!e.IsCapture)
            {
                tableCards.Add(view);
                LayoutTable(MoveTime);
                yield return new WaitForSeconds(MoveTime);
                sound.PlayPlace();
                yield break;
            }

            // Land on top of the matching card, slightly offset so both stay visible,
            // light up the chain in order, then sweep everything to the capturer.
            CardView first = views[e.Captured[0]];
            yield return Move(view.Rect, first.Rect.anchoredPosition + new Vector2(28f, -28f), TableScale, 0f, MoveTime);
            sound.PlayPlace();
            var taken = new List<CardView> { view };
            foreach (Card card in e.Captured) taken.Add(views[card]);
            foreach (CardView card in taken)
            {
                card.SetHighlight(true);
                yield return new WaitForSeconds(0.09f);
            }
            sound.PlayCapture();
            yield return new WaitForSeconds(0.35f);
            yield return SweepToPile(taken, e.Seat);
            LayoutTable(0.2f);
        }

        private IEnumerator SweepToPile(List<CardView> cards, int seat)
        {
            Vector2 pile = LocalPosition(panels[seat].PileTarget);
            foreach (CardView card in cards)
            {
                tableCards.Remove(card);
                StartCoroutine(Move(card.Rect, pile, PileScale, 0f, 0.35f));
            }
            yield return new WaitForSeconds(0.37f);
            foreach (CardView card in cards)
            {
                views.Remove(card.Card);
                Destroy(card.gameObject);
            }
            panels[seat].CapturedCount += cards.Count;
            RefreshCaptured();
        }

        public IEnumerator PlayScore(ScoreAwardedEvent e)
        {
            scoreLabels[e.Seat].text = e.NewScore.ToString();
            sound.PlayScore();
            string who = e.Seat == HumanSeat ? "You" : PlayerNames[e.Seat];
            string points = who + " +" + e.Points;
            switch (e.Reason)
            {
                case ScoreReason.Missa:
                    yield return ShowCallout("MISSA!", "Table cleared  ·  " + points, 1.4f);
                    break;
                case ScoreReason.Derba:
                    yield return ShowCallout("DERBA!", "Took the card just dropped  ·  " + points, 1.4f);
                    break;
                case ScoreReason.Announcement:
                    string capped = e.Points < e.Requested ? "  (ronda points stop at 40)" : "";
                    yield return ShowCallout("ANNOUNCEMENT", points + capped, 1.3f);
                    break;
                case ScoreReason.AnnouncementBonus:
                    yield return ShowCallout("BEST ANNOUNCEMENT", "+1 for each opposing announcement  ·  " + points, 1.3f);
                    break;
                case ScoreReason.DealerPenalty:
                    yield return ShowCallout("DEALER RULE", "The dealer did not make the last capture  ·  " + points, 1.8f);
                    break;
                default:
                    yield return ShowCallout("CARDS OVER 20", "One point per captured card beyond 20  ·  " + points, 1.8f);
                    break;
            }
        }

        public IEnumerator PlayAnnouncements(AnnouncementsResolvedEvent e)
        {
            bool anyTringla = false;
            var parts = new List<string>();
            foreach (Announcement a in e.Announcements)
            {
                anyTringla |= a.IsTringla;
                string who = a.Seat == HumanSeat ? "You" : PlayerNames[a.Seat];
                parts.Add(who + ": " + (a.IsTringla ? "Tringla" : "Ronda") + " of " + a.Rank + "s");
            }
            string detail = string.Join("   ·   ", parts);
            if (e.Winner < 0) detail += "   ·   tie, nobody scores";
            yield return ShowCallout(anyTringla ? "TRINGLA!" : "RONDA!", detail, 1.8f);
        }

        public IEnumerator PlayTally(DeckTalliedEvent e)
        {
            SetActiveSeat(-1);
            if (e.Leftover.Count > 0 && e.LeftoverTo >= 0)
            {
                string who = e.LeftoverTo == HumanSeat ? "You take" : PlayerNames[e.LeftoverTo] + " takes";
                StartCoroutine(ShowCallout("END OF DECK", who + " the cards left on the table (last capture)", 1.6f));
                var leftover = new List<CardView>();
                foreach (Card card in e.Leftover) leftover.Add(views[card]);
                yield return SweepToPile(leftover, e.LeftoverTo);
                yield return new WaitForSeconds(1.3f);
            }
            panels[0].CapturedCount = e.PileCounts[0];
            panels[1].CapturedCount = e.PileCounts[1];
            RefreshCaptured();
            yield return ShowCallout("DECK FINISHED", "You captured " + e.PileCounts[0] + " cards  ·  "
                                     + PlayerNames[OpponentSeat] + " captured " + e.PileCounts[1], 1.9f);
        }

        public IEnumerator PlayMatchEnded(MatchEndedEvent e)
        {
            SetActiveSeat(-1);
            hintLabel.text = "";
            yield return new WaitForSeconds(0.5f);

            bool won = e.Winner == HumanSeat;
            sound.PlayResult(won);
            Sprite illustration = won ? victoryIllustration : defeatIllustration;
            resultIllustration.sprite = illustration;
            resultIllustration.enabled = illustration != null;
            if (illustration == null)
            {
                // No art: move the text up into the empty space.
                resultTitle.rectTransform.anchoredPosition = new Vector2(0f, 100f);
                resultScore.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            }

            resultTitle.text = won ? "VICTORY" : "DEFEAT";
            resultTitle.color = won ? Palette.Gold : Palette.Silver;
            resultScore.text = "You " + e.Scores[0] + "   –   " + e.Scores[1] + " " + PlayerNames[OpponentSeat];
            cardLayer.gameObject.SetActive(false); // the dialog is slightly translucent
            resultOverlay.transform.SetAsLastSibling();
            resultOverlay.SetActive(true);
        }

        // ---------- Animation helpers ----------

        private IEnumerator ShowCallout(string title, string detail, float seconds)
        {
            calloutTitle.text = title;
            calloutDetail.text = detail;
            callout.gameObject.SetActive(true);
            callout.SetAsLastSibling();
            for (float t = 0f; t < 0.16f; t += Time.deltaTime)
            {
                callout.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, t / 0.16f);
                yield return null;
            }
            callout.localScale = Vector3.one;
            yield return new WaitForSeconds(seconds);
            callout.gameObject.SetActive(false);
        }

        // The latest move started for each card; an older move stops when a newer one begins.
        private readonly Dictionary<RectTransform, int> moveIds = new Dictionary<RectTransform, int>();
        private int nextMoveId;

        private IEnumerator Move(RectTransform rect, Vector2 targetPosition, float targetScale, float targetAngle, float duration)
        {
            if (rect == null) yield break;
            int id = ++nextMoveId;
            moveIds[rect] = id;
            Vector2 startPosition = rect.anchoredPosition;
            float startScale = rect.localScale.x;
            float startAngle = Mathf.DeltaAngle(0f, rect.localEulerAngles.z);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (rect == null || moveIds[rect] != id) yield break;
                float k = 1f - Mathf.Pow(1f - t / duration, 3f); // ease-out
                rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, k);
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, targetScale, k);
                rect.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(startAngle, targetAngle, k));
                yield return null;
            }
            if (rect == null || moveIds[rect] != id) yield break;
            moveIds.Remove(rect);
            rect.anchoredPosition = targetPosition;
            rect.localScale = Vector3.one * targetScale;
            rect.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        }
    }
}
