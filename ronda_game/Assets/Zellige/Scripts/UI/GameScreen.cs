using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zellige.Core;

namespace Zellige.UI
{
    /// <summary>
    /// The match screen: builds the uGUI layout from code and animates the engine's
    /// events. It displays what it is told and contains no game rules.
    /// Layout follows UI-Design/Generated/mockup_game_screen.png on a 1920x1080
    /// canvas; positions are relative to the screen centre.
    /// </summary>
    public sealed class GameScreen : MonoBehaviour
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

        // Layout (canvas units, centre origin).
        private static readonly Vector2[] PanelPositions = { new Vector2(-648f, -381f), new Vector2(-648f, 384f) };
        private static readonly Vector2 PanelSize = new Vector2(425f, 132f);
        private static readonly Vector2 DeckPosition = new Vector2(-700f, 23f);
        private static readonly Vector2 ScoreboardPosition = new Vector2(705f, 232f);
        private const float TableY = 10f;
        private const float TableMaxSpan = 1060f;
        private const float TableSpacing = 150f;
        private const float HumanHandY = -330f;
        private const float OpponentHandY = 373f;

        private readonly Dictionary<Card, CardView> views = new Dictionary<Card, CardView>();
        private readonly List<CardView>[] hands = { new List<CardView>(), new List<CardView>() };
        private readonly List<CardView> tableCards = new List<CardView>();
        private readonly PlayerPanel[] panels = new PlayerPanel[2];
        private readonly TextMeshProUGUI[] scoreLabels = new TextMeshProUGUI[2];

        private RectTransform cardLayer;
        private GameObject deckImage;
        private TextMeshProUGUI deckLabel;
        private TextMeshProUGUI hintLabel;
        private RectTransform callout;
        private Image calloutBackground;
        private TextMeshProUGUI calloutTitle;
        private TextMeshProUGUI calloutDetail;
        private GameObject announceButton;
        private TextMeshProUGUI announceLabel;
        private Button soundButton;
        private GameObject resultOverlay;
        private Image resultIllustration;
        private TextMeshProUGUI resultTitle;
        private TextMeshProUGUI resultScore;
        private SoundFx sound;
        private CardView selected;
        private int activeSeat = -1;

        /// <summary>Raised when the player taps any card.</summary>
        public event Action<CardView> CardClicked;
        /// <summary>Raised when the player taps the table background.</summary>
        public event Action BackgroundClicked;
        public event Action AnnounceClicked;
        public event Action NewGameClicked;

        public CardView Selected => selected;

        /// <summary>The widgets of one player's panel.</summary>
        private sealed class PlayerPanel
        {
            public Image TurnRing;
            public TextMeshProUGUI Role;
            public TextMeshProUGUI Captured;
            public GameObject DealerChip;
            public int CapturedCount;
        }

        // ---------- Construction ----------

        public static GameScreen Create()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject("Zellige Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // Expand keeps the whole 1920x1080 layout visible on any aspect ratio;
            // the background is enlarged to cover the extra space.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var screen = canvasObject.AddComponent<GameScreen>();
            screen.sound = canvasObject.AddComponent<SoundFx>();
            screen.Build();
            return screen;
        }

        private void Build()
        {
            Transform root = transform;

            Image background = UiKit.Image("Background", root, Vector2.zero, new Vector2(1920f, 1080f),
                SpriteLibrary.Get("UI/bg_table_main"), Palette.Navy);
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1920f / 1080f;
            background.raycastTarget = true;
            var backgroundButton = background.gameObject.AddComponent<Button>();
            backgroundButton.transition = Selectable.Transition.None;
            backgroundButton.onClick.AddListener(() => BackgroundClicked?.Invoke());

            BuildPlayerPanel(root, HumanSeat);
            BuildPlayerPanel(root, OpponentSeat);
            BuildScoreboard(root);
            BuildDeck(root);

            cardLayer = UiKit.Rect("Cards", root, Vector2.zero, Vector2.zero);

            hintLabel = UiKit.Text("Hint", root, new Vector2(0f, -126f), new Vector2(1000f, 40f), "", 26f,
                Palette.WithAlpha(Palette.Silver, 0.9f));

            BuildCallout(root);

            Button announce = UiKit.Button("Announce Button", root, new Vector2(708f, -377f), new Vector2(300f, 84f),
                "RONDA!", () => AnnounceClicked?.Invoke());
            announceButton = announce.gameObject;
            announceLabel = announce.GetComponentInChildren<TextMeshProUGUI>();
            announceButton.SetActive(false);

            soundButton = UiKit.IconButton("Sound Button", root, new Vector2(748f, 412f), 72f,
                "UI/icon_sound_on", "Sound", ToggleSound);
            UiKit.IconButton("New Game Button", root, new Vector2(828f, 412f), 72f,
                "UI/icon_restart", "New", () => NewGameClicked?.Invoke());

            BuildResultOverlay(root);
        }

        private void BuildPlayerPanel(Transform root, int seat)
        {
            var panel = new PlayerPanel();
            panels[seat] = panel;

            Image body = UiKit.Image("Player Panel " + seat, root, PanelPositions[seat], PanelSize,
                SpriteLibrary.Get("UI/ui_panel"), Palette.WithAlpha(Palette.NavyLight, 0.9f), 1.4f);
            Transform t = body.transform;
            var avatarPosition = new Vector2(-142f, 0f);

            panel.TurnRing = UiKit.Image("Turn Ring", t, avatarPosition, new Vector2(150f, 150f),
                SpriteLibrary.Get("UI/ui_turn_ring"), Palette.Orange);
            panel.TurnRing.enabled = false;
            UiKit.CircularImage("Avatar", t, avatarPosition, 92f,
                SpriteLibrary.Get(seat == HumanSeat ? "UI/avatar_01" : "UI/avatar_02"), Palette.Silver);
            UiKit.Image("Avatar Frame", t, avatarPosition, new Vector2(116f, 116f), SpriteLibrary.Get("UI/frame_avatar"), Color.clear);

            TextMeshProUGUI name = UiKit.Text("Name", t, new Vector2(4f, 18f), new Vector2(150f, 48f),
                PlayerNames[seat], 38f, Palette.Ivory, TextAlignmentOptions.Left);
            name.fontStyle = FontStyles.Bold;
            panel.Role = UiKit.Text("Role", t, new Vector2(4f, -26f), new Vector2(150f, 30f),
                "", 19f, Palette.Silver, TextAlignmentOptions.Left);
            panel.Role.characterSpacing = 8f;

            Image capsule = UiKit.Image("Captured Capsule", t, new Vector2(136f, 8f), new Vector2(106f, 50f),
                SpriteLibrary.Get("UI/ui_score_capsule"), Palette.NavyDeep);
            panel.Captured = UiKit.Text("Captured", capsule.transform, new Vector2(0f, 1f), new Vector2(106f, 50f), "0", 32f, Palette.Gold);
            panel.Captured.fontStyle = FontStyles.Bold;
            UiKit.Text("Captured Caption", t, new Vector2(136f, -36f), new Vector2(120f, 24f), "CAPTURED", 14f, Palette.Silver)
                .characterSpacing = 6f;

            Image chip = UiKit.Image("Dealer Chip", t, avatarPosition + new Vector2(42f, 42f), new Vector2(38f, 38f),
                SpriteLibrary.Get("UI/ui_dealer_chip"), Palette.Gold);
            if (chip.sprite == null)
            {
                chip.sprite = SpriteLibrary.Circle;
                UiKit.Text("D", chip.transform, new Vector2(0f, 1f), new Vector2(38f, 38f), "D", 22f, Palette.Ink).fontStyle = FontStyles.Bold;
            }
            panel.DealerChip = chip.gameObject;
            panel.DealerChip.SetActive(false);
        }

        private void BuildScoreboard(Transform root)
        {
            Image board = UiKit.Image("Scoreboard", root, ScoreboardPosition, new Vector2(330f, 220f),
                SpriteLibrary.Get("UI/ui_panel"), Palette.WithAlpha(Palette.NavyLight, 0.9f), 1.4f);
            Transform t = board.transform;
            UiKit.Text("Title", t, new Vector2(0f, 80f), new Vector2(300f, 30f), "SCORE", 22f, Palette.Silver).characterSpacing = 12f;
            UiKit.Image("Divider", t, new Vector2(0f, 60f), new Vector2(260f, 2f), null, Palette.WithAlpha(Palette.Silver, 0.35f));

            for (int seat = 0; seat < 2; seat++)
            {
                float y = seat == HumanSeat ? 20f : -40f;
                UiKit.Text("Name " + seat, t, new Vector2(-55f, y), new Vector2(180f, 50f),
                    PlayerNames[seat], 34f, Palette.Ivory, TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;
                Image capsule = UiKit.Image("Capsule " + seat, t, new Vector2(88f, y), new Vector2(100f, 50f),
                    SpriteLibrary.Get("UI/ui_score_capsule"), Palette.NavyDeep);
                scoreLabels[seat] = UiKit.Text("Score", capsule.transform, new Vector2(0f, 1f), new Vector2(100f, 50f), "0", 34f, Palette.Gold);
                scoreLabels[seat].fontStyle = FontStyles.Bold;
            }
            UiKit.Text("Target", t, new Vector2(0f, -86f), new Vector2(300f, 28f), "First to 41", 20f, Palette.Silver);
        }

        private void BuildDeck(Transform root)
        {
            deckImage = UiKit.Image("Deck", root, DeckPosition, CardView.BaseSize * DeckScale,
                SpriteLibrary.CardBack, Palette.NavyLight).gameObject;
            Image capsule = UiKit.Image("Deck Capsule", root, DeckPosition + new Vector2(0f, -164f), new Vector2(112f, 50f),
                SpriteLibrary.Get("UI/ui_score_capsule"), Palette.NavyDeep);
            deckLabel = UiKit.Text("Deck Count", capsule.transform, new Vector2(0f, 1f), new Vector2(112f, 50f), "", 32f, Palette.Ivory);
            deckLabel.fontStyle = FontStyles.Bold;
            UiKit.Text("Deck Caption", root, DeckPosition + new Vector2(0f, -212f), new Vector2(160f, 30f), "DECK", 20f, Palette.Silver)
                .characterSpacing = 12f;
        }

        private void BuildCallout(Transform root)
        {
            callout = UiKit.Rect("Callout", root, new Vector2(0f, 205f), new Vector2(1100f, 170f));
            Sprite banner = SpriteLibrary.Get("UI/ui_callout");
            calloutBackground = UiKit.Image("Banner", callout, new Vector2(0f, 22f), new Vector2(820f, 128f), banner, Color.clear, 1f);
            calloutBackground.enabled = banner != null;

            // The title must stay between the banner's ornamented ends.
            calloutTitle = UiKit.Text("Title", callout, new Vector2(0f, 24f), new Vector2(560f, 96f), "", 76f, Palette.Gold);
            calloutTitle.fontStyle = FontStyles.Bold;
            calloutTitle.enableAutoSizing = true;
            calloutTitle.fontSizeMin = 36f;
            calloutTitle.fontSizeMax = 76f;
            calloutTitle.enableWordWrapping = false;
            calloutTitle.outlineWidth = 0.18f;
            calloutTitle.outlineColor = Palette.NavyDeep;
            calloutDetail = UiKit.Text("Detail", callout, new Vector2(0f, -48f), new Vector2(1100f, 44f), "", 30f, Palette.Ivory);
            calloutDetail.outlineWidth = 0.2f;
            calloutDetail.outlineColor = Palette.NavyDeep;
            callout.gameObject.SetActive(false);
        }

        private void BuildResultOverlay(Transform root)
        {
            Image dim = UiKit.Image("Result Overlay", root, Vector2.zero, Vector2.zero, null, new Color(0.02f, 0.04f, 0.1f, 0.75f));
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true; // blocks clicks on the table behind
            resultOverlay = dim.gameObject;

            Sprite dialog = SpriteLibrary.Get("UI/ui_dialog") ?? SpriteLibrary.Get("UI/ui_panel");
            Image panel = UiKit.Image("Panel", dim.transform, Vector2.zero, new Vector2(820f, 600f), dialog, Palette.NavyLight, 1f);
            resultIllustration = UiKit.Image("Illustration", panel.transform, new Vector2(0f, 135f), new Vector2(416f, 260f), null, Color.clear);
            resultTitle = UiKit.Text("Title", panel.transform, Vector2.zero, new Vector2(760f, 110f), "", 88f, Palette.Gold);
            resultTitle.fontStyle = FontStyles.Bold;
            resultTitle.characterSpacing = 6f;
            resultScore = UiKit.Text("Score", panel.transform, Vector2.zero, new Vector2(760f, 60f), "", 40f, Palette.Ivory);
            UiKit.Button("Play Again", panel.transform, new Vector2(0f, -205f), new Vector2(360f, 96f),
                "PLAY AGAIN", () => NewGameClicked?.Invoke());
            resultOverlay.SetActive(false);
        }

        // ---------- Layout ----------

        /// <summary>Table cards sit in one row sorted by rank, so chains read left to right.</summary>
        private void LayoutTable(float duration)
        {
            tableCards.Sort((a, b) => Ranks.IndexOf(a.Card.Rank).CompareTo(Ranks.IndexOf(b.Card.Rank)));
            int count = tableCards.Count;
            float spacing = count > 1 ? Mathf.Min(TableSpacing, TableMaxSpan / (count - 1)) : 0f;
            for (int i = 0; i < count; i++)
            {
                var target = new Vector2((i - (count - 1) / 2f) * spacing, TableY);
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

        /// <summary>Hands are drawn as a gentle fan.</summary>
        private static void GetHandSlot(int seat, int index, int count, out Vector2 position, out float angle)
        {
            float offset = index - (count - 1) / 2f;
            if (seat == HumanSeat)
            {
                position = new Vector2(offset * 182f, HumanHandY - offset * offset * 7f);
                angle = -offset * 4f;
            }
            else
            {
                position = new Vector2(offset * 84f, OpponentHandY + offset * offset * 4f);
                angle = offset * 4f;
            }
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
            announceButton.SetActive(false);
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
            announceButton.SetActive(text != null);
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
            deckImage.SetActive(count > 0);
        }

        private void RefreshCaptured()
        {
            for (int seat = 0; seat < 2; seat++) panels[seat].Captured.text = panels[seat].CapturedCount.ToString();
        }

        private void ToggleSound()
        {
            sound.Muted = !sound.Muted;
            UiKit.SetIcon(soundButton, sound.Muted ? "UI/icon_sound_off" : "UI/icon_sound_on", sound.Muted ? "Off" : "Sound");
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
                    CardView view = CardView.Create(cardLayer, e.Hands[seat][i], v => CardClicked?.Invoke(v));
                    view.Rect.anchoredPosition = DeckPosition;
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
            foreach (CardView card in cards)
            {
                tableCards.Remove(card);
                StartCoroutine(Move(card.Rect, PanelPositions[seat] + new Vector2(136f, 8f), PileScale, 0f, 0.35f));
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
            resultIllustration.sprite = SpriteLibrary.Get(won ? "UI/illus_victory" : "UI/illus_defeat");
            bool hasIllustration = resultIllustration.sprite != null;
            resultIllustration.enabled = hasIllustration;
            resultIllustration.color = Color.white;
            resultIllustration.preserveAspect = true;

            resultTitle.text = won ? "VICTORY" : "DEFEAT";
            resultTitle.color = won ? Palette.Gold : Palette.Silver;
            resultTitle.rectTransform.anchoredPosition = new Vector2(0f, hasIllustration ? -40f : 100f);
            resultScore.text = "You " + e.Scores[0] + "   –   " + e.Scores[1] + " " + PlayerNames[OpponentSeat];
            resultScore.rectTransform.anchoredPosition = new Vector2(0f, hasIllustration ? -115f : 10f);
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
