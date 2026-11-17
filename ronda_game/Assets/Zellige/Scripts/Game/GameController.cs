using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zellige.AI;
using Zellige.App;
using Zellige.Core;
using Zellige.UI;

namespace Zellige.Game
{
    /// <summary>
    /// Runs a local match: one human (seat 0) against the AI (seat 1). It sends
    /// commands to the rules engine and hands the resulting events to the screen
    /// to animate. Put this component on any object in the scene.
    ///
    /// L2: each match is a <see cref="MatchSession"/> (via <see cref="MatchLifecycle"/>), recorded in
    /// the local data once the engine reports its end. This controller is also the scene's Back
    /// handler: during a match Back opens "Leave the match?" (D5); after the match it returns to the Lobby.
    /// </summary>
    public sealed class GameController : MonoBehaviour, IBackHandler
    {
        [Tooltip("0 = a different shuffle every match. Any other value repeats the same match.")]
        [SerializeField] private int fixedSeed;
        [SerializeField] private float aiMinDelay = 0.7f;
        [SerializeField] private float aiMaxDelay = 1.4f;
        [Tooltip("Demo/testing: a second AI plays the human seat.")]
        [SerializeField] private bool autoPlayHuman;
        [Tooltip("The match screen in this scene (made by Zellige > Build or Refresh Game UI).")]
        [SerializeField] private GameScreen screen;

        private RondaGame game;
        private SimpleAi ai;
        private SimpleAi demoAi;
        private bool busy;
        private MatchLifecycle lifecycle;

        /// <summary>The engine of the running match (read-only use, e.g. for tests and debugging).</summary>
        public RondaGame Game => game;
        /// <summary>The current match's session (L1 lifecycle); null before the first match.</summary>
        public MatchSession Session => lifecycle?.Current;
        /// <summary>The match screen this controller drives.</summary>
        public GameScreen Screen => screen;
        /// <summary>True while animations or the AI are running and the player cannot act.</summary>
        public bool IsBusy => busy;

        private void Start()
        {
            if (lifecycle != null) return; // never set up (or start a match) twice
            if (screen == null) screen = FindObjectOfType<GameScreen>();
            if (screen == null)
            {
                Debug.LogWarning("Zellige: no authored Game UI in this scene, building it at runtime. " +
                                 "Use Zellige > Build or Refresh Game UI to save it into the scene.", this);
                screen = GameScreen.CreateAtRuntime();
            }
            screen.CardClicked += OnCardClicked;
            screen.BackgroundClicked += () => screen.Select(null, new List<Card>());
            screen.AnnounceClicked += OnAnnounceClicked;
            screen.NewGameClicked += StartNewMatch;
            SetUpNavigation();
            lifecycle = new MatchLifecycle(AppData.Store);
            StartNewMatch();
        }

        // ---------- Navigation (L2) ----------

        private void SetUpNavigation()
        {
            screen.AddNavigationElements(); // only fills gaps in a Game UI saved before L2
            screen.LobbyClicked += GoToLobby;
            screen.LeaveDialog.Confirmed += LeaveMatch;

            BackButtonRouter[] routers = FindObjectsOfType<BackButtonRouter>();
            if (routers.Length != 1)
            {
                Debug.LogWarning("Zellige: expected one Back button router in the Game scene, found " + routers.Length + ".", this);
            }
            foreach (BackButtonRouter router in routers) router.SetHandler(this);
        }

        /// <summary>
        /// Android Back in the match (called by the scene's <see cref="BackButtonRouter"/>):
        /// an open dialog is cancelled first; during a match Back asks "Leave the match?";
        /// once the engine has ended the match (it is already recorded) Back returns to the Lobby.
        /// </summary>
        public void HandleBack()
        {
            ConfirmDialog leave = screen.LeaveDialog;
            if (leave.IsOpen)
            {
                leave.Cancel();
                return;
            }
            QuitDialog quit = screen.GetComponentInChildren<QuitDialog>(true);
            if (quit != null && quit.IsOpen)
            {
                quit.Close();
                return;
            }
            if (game != null && !game.IsMatchOver)
            {
                leave.Open();
                return;
            }
            GoToLobby();
        }

        /// <summary>"Leave the match?" confirmed: the match is abandoned (never completed) and the Lobby opens.</summary>
        private void LeaveMatch()
        {
            lifecycle.Leave();
            StopAllCoroutines();
            SceneNavigator.OpenLobby();
        }

        /// <summary>The result screen's Lobby button (D4). The result was recorded when the engine ended the match.</summary>
        private void GoToLobby()
        {
            if (game != null && !game.IsMatchOver) return; // only after the match; leaving mid-match goes through the dialog
            StopAllCoroutines();
            SceneNavigator.OpenLobby();
        }

        public void StartNewMatch()
        {
            StopAllCoroutines();
            screen.ResetAll();
            lifecycle.StartNew();

            int seed = fixedSeed != 0 ? fixedSeed : Environment.TickCount;
            game = new RondaGame(new RulesConfig(), seed);
            ai = new SimpleAi(seed + 1);
            demoAi = new SimpleAi(seed + 2);
            StartCoroutine(PlayEvents(game.StartMatch()));
        }

        // ---------- Player input ----------

        private bool IsHumanTurn => !busy && !game.IsMatchOver && game.CurrentPlayer == GameScreen.HumanSeat;

        private void OnCardClicked(CardView view)
        {
            if (!IsHumanTurn || !game.CanPlay(GameScreen.HumanSeat, view.Card)) return;

            if (screen.Selected == view)
            {
                // Second tap on the lifted card plays it.
                StartCoroutine(PlayEvents(game.PlayCard(GameScreen.HumanSeat, view.Card)));
            }
            else
            {
                screen.Select(view, game.PreviewCaptures(view.Card));
            }
        }

        private void OnAnnounceClicked()
        {
            if (busy) return;
            foreach (int rank in game.GetAnnounceableRanks(GameScreen.HumanSeat))
            {
                game.Announce(GameScreen.HumanSeat, rank);
            }
            RefreshPrompt();
            screen.SetStatus("Announcement sealed: it is revealed once both players have played a card");
        }

        // ---------- Turn flow ----------

        /// <summary>
        /// Hands a batch of engine events to the match record keeping at once (so a finished match is
        /// recorded before its animations play), then animates it.
        /// </summary>
        private IEnumerator PlayEvents(IReadOnlyList<GameEvent> events)
        {
            lifecycle.Observe(events);
            return AnimateEvents(events);
        }

        private IEnumerator AnimateEvents(IReadOnlyList<GameEvent> events)
        {
            busy = true;
            screen.SetAnnounceButton(null);
            foreach (GameEvent gameEvent in events)
            {
                yield return PlayEvent(gameEvent);
            }
            busy = false;

            if (game.IsMatchOver) yield break;
            if (game.CurrentPlayer == GameScreen.OpponentSeat)
            {
                StartCoroutine(PlayAiTurn(ai, GameScreen.OpponentSeat));
            }
            else if (autoPlayHuman)
            {
                StartCoroutine(PlayAiTurn(demoAi, GameScreen.HumanSeat));
            }
            else
            {
                RefreshPrompt();
            }
        }

        private IEnumerator PlayEvent(GameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case DeckStartedEvent e: return screen.PlayDeckStarted(e);
                case DealStartedEvent e: return screen.PlayDeal(e);
                case CardPlayedEvent e: return screen.PlayCardPlayed(e);
                case AnnouncementsResolvedEvent e: return screen.PlayAnnouncements(e);
                case ScoreAwardedEvent e: return screen.PlayScore(e);
                case DeckTalliedEvent e: return screen.PlayTally(e);
                case MatchEndedEvent e: return screen.PlayMatchEnded(e);
                case TurnStartedEvent e: return ShowTurn(e.Seat);
                default: return null;
            }
        }

        private IEnumerator ShowTurn(int seat)
        {
            screen.SetActiveSeat(seat);
            screen.SetStatus("");
            yield break;
        }

        private IEnumerator PlayAiTurn(SimpleAi player, int seat)
        {
            busy = true;
            yield return new WaitForSeconds(UnityEngine.Random.Range(aiMinDelay, aiMaxDelay));

            foreach (int rank in player.ChooseAnnouncements(game, seat)) game.Announce(seat, rank);
            Card card = player.ChooseCard(game, seat);
            yield return PlayEvents(game.PlayCard(seat, card));
        }

        /// <summary>Tells the player what they can do now, including announcing a pair.</summary>
        private void RefreshPrompt()
        {
            screen.SetStatus("Tap a card to see what it takes, tap it again to play it");

            List<int> ranks = game.GetAnnounceableRanks(GameScreen.HumanSeat);
            if (ranks.Count == 0)
            {
                screen.SetAnnounceButton(null);
                return;
            }
            bool hasTringla = false;
            foreach (int rank in ranks)
            {
                int count = 0;
                foreach (Card card in game.GetHand(GameScreen.HumanSeat))
                {
                    if (card.Rank == rank) count++;
                }
                hasTringla |= count >= 3;
            }
            screen.SetAnnounceButton(hasTringla ? "TRINGLA!" : "RONDA!");
        }
    }
}
