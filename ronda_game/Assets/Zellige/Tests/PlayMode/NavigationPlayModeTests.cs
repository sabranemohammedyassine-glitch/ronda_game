using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zellige.App;
using Zellige.Game;
using Zellige.Lobby;
using Zellige.UI;

namespace Zellige.Tests.PlayMode
{
    /// <summary>
    /// The real Lobby.unity and Game.unity, loaded the way the app loads them. These run in the Unity
    /// Editor's Play Mode (not on a device). The Back key cannot be pressed from a test, so
    /// presses go through <see cref="BackButtonRouter.Press"/>, which is exactly what the key handler calls.
    /// All saved data goes to a temporary folder (<see cref="AppData.UseStore"/>), never to the player's data.
    /// </summary>
    public sealed class NavigationPlayModeTests
    {
        private const float TimeoutSeconds = 20f;
        private const float FullMatchTimeoutSeconds = 240f;

        private string folder;
        private LocalDataStore store;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "zellige-l2-tests", Guid.NewGuid().ToString("N"));
            store = new LocalDataStore(folder);
            store.LoadAll(DateTime.UtcNow);
            AppData.UseStore(store);
            MatchHandoff.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            MatchHandoff.Clear();
            SceneManager.sceneLoaded -= MakeMatchPlayItself;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        // ---------- Helpers ----------

        private static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == scene && !SceneNavigator.IsLoading, TimeoutSeconds);
            yield return null; // let Start run
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out after " + seconds + " s");
                yield return null;
            }
        }

        private static IEnumerator WaitForGame()
        {
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNavigator.GameScene, TimeoutSeconds);
            yield return WaitFor(() => Controller() != null && Controller().Session != null, TimeoutSeconds);
        }

        private static GameController Controller() => UnityEngine.Object.FindObjectOfType<GameController>();
        private static LobbyScreen Lobby() => UnityEngine.Object.FindObjectOfType<LobbyScreen>();

        /// <summary>Before the match starts: Nadia also plays the human seat, without thinking pauses (demo mode).</summary>
        private static void MakeMatchPlayItself(Scene scene, LoadSceneMode mode)
        {
            GameController controller = UnityEngine.Object.FindObjectOfType<GameController>();
            if (controller == null) return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GameController).GetField("autoPlayHuman", flags).SetValue(controller, true);
            typeof(GameController).GetField("aiMinDelay", flags).SetValue(controller, 0f);
            typeof(GameController).GetField("aiMaxDelay", flags).SetValue(controller, 0f);
        }

        private IEnumerator PlayAWholeMatch()
        {
            SceneManager.sceneLoaded += MakeMatchPlayItself;
            yield return Load(SceneNavigator.GameScene);
            SceneManager.sceneLoaded -= MakeMatchPlayItself;
            Time.timeScale = 40f; // animations and pauses run 40x faster; the rules do not depend on time
            yield return WaitFor(() => Controller().Screen.IsResultShown, FullMatchTimeoutSeconds);
            Time.timeScale = 1f;
        }

        // ---------- Lobby ----------

        [UnityTest]
        public IEnumerator Lobby_ShowsTheThreeModes_WithOneBackHandler()
        {
            yield return Load(SceneNavigator.LobbyScene);

            LobbyScreen lobby = Lobby();
            Assert.IsNotNull(lobby);
            Assert.AreEqual(3, lobby.ModeCards.Count);
            Assert.IsTrue(lobby.ModeCards[0].Button.interactable);
            Assert.IsFalse(lobby.ModeCards[1].Button.interactable);
            Assert.IsFalse(lobby.ModeCards[2].Button.interactable);
            Assert.AreEqual(1, BackButtonRouter.EnabledCount);
            Assert.AreSame(lobby, BackButtonRouter.Current.Handler);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsOfType<EventSystem>().Length);
        }

        [UnityTest]
        public IEnumerator LobbyBack_OpensTheQuitDialog_AndBackAgainCancels()
        {
            yield return Load(SceneNavigator.LobbyScene);
            LobbyScreen lobby = Lobby();

            Assert.IsTrue(BackButtonRouter.Current.Press());
            Assert.IsTrue(lobby.QuitDialog.IsOpen, "Back on Home asks to quit the app");
            Assert.AreEqual(0f, Time.timeScale);

            Assert.IsTrue(BackButtonRouter.Current.Press());
            Assert.IsFalse(lobby.QuitDialog.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            yield return null;
            Assert.AreEqual(SceneNavigator.LobbyScene, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator LockedOnlineCards_DoNothing()
        {
            yield return Load(SceneNavigator.LobbyScene);
            LobbyScreen lobby = Lobby();

            lobby.ModeCards[1].Button.onClick.Invoke();
            lobby.ModeCards[2].Button.onClick.Invoke();
            Assert.IsFalse(lobby.RequestMode(MatchMode.Online1v1));
            Assert.IsFalse(lobby.RequestMode(MatchMode.Online2v2));
            for (int i = 0; i < 5; i++) yield return null;

            Assert.AreEqual(SceneNavigator.LobbyScene, SceneManager.GetActiveScene().name);
            Assert.IsFalse(MatchHandoff.HasPending);
            Assert.IsFalse(SceneNavigator.IsLoading);
        }

        [UnityTest]
        public IEnumerator MeVsComputer_OpensTheGame_WithAFreshLocalMatch()
        {
            yield return Load(SceneNavigator.LobbyScene);

            Lobby().ModeCards[0].Button.onClick.Invoke();
            Lobby().ModeCards[0].Button.onClick.Invoke(); // a double tap must not load twice
            yield return WaitForGame();

            GameController controller = Controller();
            Assert.AreEqual(MatchMode.VsComputer, controller.Session.Config.Mode);
            Assert.AreEqual(MatchState.InProgress, controller.Session.State);
            Assert.IsNotNull(controller.Game);
            Assert.IsFalse(MatchHandoff.HasPending, "the hand-off was used once");
            Assert.AreEqual(1, BackButtonRouter.EnabledCount);
            Assert.AreSame(controller, BackButtonRouter.Current.Handler);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsOfType<EventSystem>().Length);
        }

        // ---------- Game opened directly ----------

        [UnityTest]
        public IEnumerator GameOpenedDirectly_StartsTheDefaultMatchAsBefore()
        {
            yield return Load(SceneNavigator.GameScene);
            yield return WaitForGame();

            GameController controller = Controller();
            Assert.AreEqual(MatchMode.VsComputer, controller.Session.Config.Mode);
            Assert.AreEqual("Nadia", controller.Session.Config.OpponentName);
            Assert.AreEqual(MatchState.InProgress, controller.Session.State);
            Assert.IsFalse(controller.Game.IsMatchOver);
            Assert.IsFalse(controller.Screen.IsResultShown);
        }

        // ---------- Back during a match ----------

        [UnityTest]
        public IEnumerator BackDuringTheMatch_AsksToLeave_AndCancelKeepsTheMatch()
        {
            yield return Load(SceneNavigator.GameScene);
            yield return WaitForGame();
            GameController controller = Controller();
            string matchId = controller.Session.Config.MatchId;
            QuitDialog quit = controller.Screen.GetComponentInChildren<QuitDialog>(true);

            Assert.IsTrue(BackButtonRouter.Current.Press());
            Assert.IsTrue(controller.Screen.LeaveDialog.IsOpen, "Back opens Leave the match?");
            Assert.IsFalse(quit.IsOpen, "the app quit dialog is not opened behind it");
            Assert.AreEqual(0f, Time.timeScale, "the match waits while the question is open");

            Assert.IsTrue(BackButtonRouter.Current.Press()); // Back again = cancel
            Assert.IsFalse(controller.Screen.LeaveDialog.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);

            BackButtonRouter.Current.Press();
            controller.Screen.LeaveDialog.CancelButton.onClick.Invoke(); // the Cancel button
            Assert.IsFalse(controller.Screen.LeaveDialog.IsOpen);

            yield return null;
            Assert.AreEqual(SceneNavigator.GameScene, SceneManager.GetActiveScene().name);
            Assert.AreEqual(matchId, controller.Session.Config.MatchId);
            Assert.AreEqual(MatchState.InProgress, controller.Session.State);
            Assert.AreEqual(0, store.History.Lifetime.Abandoned);
            Assert.AreEqual(0, store.History.Lifetime.Completed);
        }

        [UnityTest]
        public IEnumerator ConfirmedLeave_AbandonsTheMatch_AndReturnsToTheLobby()
        {
            yield return Load(SceneNavigator.GameScene);
            yield return WaitForGame();
            GameController controller = Controller();
            MatchSession session = controller.Session;

            BackButtonRouter.Current.Press();
            controller.Screen.LeaveDialog.ConfirmButton.onClick.Invoke();
            controller.Screen.LeaveDialog.ConfirmButton.onClick.Invoke(); // a second tap changes nothing
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNavigator.LobbyScene && Lobby() != null, TimeoutSeconds);

            Assert.AreEqual(MatchState.Abandoned, session.State);
            Assert.AreEqual(1, store.History.Lifetime.Abandoned);
            Assert.AreEqual(0, store.History.Lifetime.Completed, "an abandoned match is not a completed match");
            Assert.AreEqual(0, store.History.Records.Count);
            Assert.AreEqual(1f, Time.timeScale, "the Lobby is not paused");
            Assert.AreEqual(1, LoadAgain().History.Lifetime.Abandoned, "saved on disk");
        }

        private LocalDataStore LoadAgain()
        {
            var reloaded = new LocalDataStore(folder);
            reloaded.LoadAll(DateTime.UtcNow);
            return reloaded;
        }

        // ---------- After a finished match ----------

        [UnityTest]
        public IEnumerator FinishedMatch_IsRecordedOnce_AndLobbyReturnsHome()
        {
            yield return PlayAWholeMatch();
            GameController controller = Controller();
            MatchSession session = controller.Session;

            Assert.IsTrue(controller.Game.IsMatchOver);
            Assert.AreEqual(MatchState.Completed, session.State);
            Assert.AreEqual(1, store.History.Records.Count);
            MatchRecord record = store.History.Records[0];
            Assert.AreEqual(session.Config.MatchId, record.MatchId);
            Assert.AreEqual(controller.Game.GetScore(0), record.PlayerScore);
            Assert.AreEqual(controller.Game.GetScore(1), record.OpponentScore);
            Assert.AreEqual(controller.Game.Winner == 0 ? MatchOutcome.Victory : MatchOutcome.Defeat, record.Outcome);
            Assert.AreEqual(controller.Game.DeckRound, record.DecksPlayed);

            controller.Screen.LobbyButton.onClick.Invoke();
            controller.Screen.LobbyButton.onClick.Invoke(); // repeated taps
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNavigator.LobbyScene && Lobby() != null, TimeoutSeconds);

            Assert.AreEqual(MatchState.Completed, session.State);
            Assert.AreEqual(1, store.History.Lifetime.Completed, "recorded once");
            Assert.AreEqual(0, store.History.Lifetime.Abandoned, "returning to the Lobby is not an abandon");
            Assert.AreEqual(1, LoadAgain().History.Records.Count);
        }

        [UnityTest]
        public IEnumerator FinishedMatch_PlayAgain_StartsAFreshMatch_AndBackAfterTheResultGoesToTheLobby()
        {
            yield return PlayAWholeMatch();
            GameController controller = Controller();
            MatchSession first = controller.Session;

            controller.Screen.PlayAgainButton.onClick.Invoke();
            yield return null;

            Assert.AreNotSame(first, controller.Session);
            Assert.AreNotEqual(first.Config.MatchId, controller.Session.Config.MatchId);
            Assert.AreEqual(MatchState.InProgress, controller.Session.State);
            Assert.IsFalse(controller.Screen.IsResultShown);
            Assert.AreEqual(1, store.History.Records.Count, "the first result is not counted again");

            // The new match is running: Back asks to leave it.
            BackButtonRouter.Current.Press();
            Assert.IsTrue(controller.Screen.LeaveDialog.IsOpen);
            BackButtonRouter.Current.Press();
            Assert.IsFalse(controller.Screen.LeaveDialog.IsOpen);
        }

        [UnityTest]
        public IEnumerator BackOnTheResultScreen_ReturnsToTheLobby_WithoutAnotherRecord()
        {
            yield return PlayAWholeMatch();
            GameController controller = Controller();

            Assert.IsTrue(BackButtonRouter.Current.Press());
            Assert.IsFalse(controller.Screen.LeaveDialog.IsOpen, "no leave question once the match is over");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNavigator.LobbyScene && Lobby() != null, TimeoutSeconds);

            Assert.AreEqual(1, store.History.Lifetime.Completed);
            Assert.AreEqual(0, store.History.Lifetime.Abandoned);
        }
    }
}
