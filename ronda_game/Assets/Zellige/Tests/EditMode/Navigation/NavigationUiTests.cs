using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zellige.App;
using Zellige.Lobby;
using Zellige.UI;

namespace Zellige.Tests.Navigation
{
    /// <summary>
    /// The L2 screens built exactly as the editor commands build them (Lobby Home, the match
    /// screen's navigation additions, the confirmation dialog), plus the scene set-up.
    /// Scene loading and the Back key itself are covered by the Play Mode tests.
    /// </summary>
    public sealed class NavigationUiTests
    {
        private const string OutlineMaterialPath = "Assets/Zellige/Art/Generated/Text Outline.mat";

        private GameObject built;
        private float savedTimeScale;

        [SetUp]
        public void SetUp()
        {
            savedTimeScale = Time.timeScale;
            MatchHandoff.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (built != null) Object.DestroyImmediate(built);
            Time.timeScale = savedTimeScale;
            MatchHandoff.Clear();
        }

        private LobbyScreen BuildLobby()
        {
            LobbyScreen lobby = LobbyScreen.BuildHierarchy(null, AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath));
            built = lobby.gameObject;
            return lobby;
        }

        private GameScreen BuildGameScreen()
        {
            GameScreen screen = GameScreen.BuildHierarchy(null, AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath));
            built = screen.gameObject;
            return screen;
        }

        private static string Label(Button button) => button.GetComponentInChildren<TextMeshProUGUI>(true).text;

        // ---------- Lobby Home ----------

        [Test]
        public void Home_ShowsTheThreeModes_DirectlyAndInOrder()
        {
            LobbyScreen lobby = BuildLobby();

            CollectionAssert.AreEqual(new[] { MatchMode.VsComputer, MatchMode.Online1v1, MatchMode.Online2v2 },
                                      lobby.ModeCards.Select(card => card.Mode).ToArray());
            foreach (LobbyScreen.ModeCard card in lobby.ModeCards)
            {
                Assert.IsTrue(card.Button.gameObject.activeInHierarchy, card.Mode + " is visible on Home");
                TextMeshProUGUI name = card.Button.transform.parent.Find("Mode Name").GetComponent<TextMeshProUGUI>();
                Assert.AreEqual(TextCatalog.Get(MatchModes.NameKey(card.Mode)), name.text);
            }
            Assert.AreEqual("Me vs Computer", lobby.ModeCards[0].Button.transform.parent.Find("Mode Name").GetComponent<TextMeshProUGUI>().text);
        }

        [Test]
        public void OnlyMeVsComputer_CanBePressed_TheOnlineCardsAreLockedComingSoon()
        {
            LobbyScreen lobby = BuildLobby();

            Assert.IsTrue(lobby.ModeCards[0].Button.interactable);
            Assert.AreEqual("PLAY", Label(lobby.ModeCards[0].Button));
            for (int i = 1; i < 3; i++)
            {
                Assert.IsFalse(lobby.ModeCards[i].Button.interactable, lobby.ModeCards[i].Mode + " must be locked");
                Assert.AreEqual("Coming Soon", Label(lobby.ModeCards[i].Button));
                Assert.AreEqual(0, lobby.ModeCards[i].Button.onClick.GetPersistentEventCount(), "no action is wired to a locked card");
            }
        }

        [Test]
        public void LockedModes_CannotStartAMatch_OrCreateAnything()
        {
            LobbyScreen lobby = BuildLobby();

            foreach (MatchMode mode in new[] { MatchMode.Online1v1, MatchMode.Online2v2 })
            {
                Assert.IsFalse(lobby.RequestMode(mode));
                Assert.IsFalse(SceneNavigator.TryPrepareMatch(mode, out MatchConfig config));
                Assert.IsNull(config);
                Assert.IsFalse(MatchHandoff.HasPending, "no hand-off for a locked mode");
                Assert.IsFalse(SceneNavigator.IsLoading, "no scene load for a locked mode");
            }
        }

        [Test]
        public void MeVsComputer_PreparesAFreshValidLocalMatch()
        {
            Assert.IsTrue(SceneNavigator.TryPrepareMatch(MatchMode.VsComputer, out MatchConfig config));

            Assert.AreEqual(MatchMode.VsComputer, config.Mode);
            Assert.AreEqual("Nadia", config.OpponentName);
            Assert.IsTrue(MatchHandoff.HasPending);
            Assert.AreSame(config, MatchHandoff.TakePendingOrDefault());
            Assert.IsFalse(MatchHandoff.HasPending, "taken once");

            SceneNavigator.TryPrepareMatch(MatchMode.VsComputer, out MatchConfig next);
            Assert.AreNotEqual(config.MatchId, next.MatchId);
        }

        [Test]
        public void LobbyBack_OpensTheQuitDialog_AndBackAgainCancelsIt()
        {
            LobbyScreen lobby = BuildLobby();
            Time.timeScale = 1f;

            lobby.HandleBack();
            Assert.IsTrue(lobby.QuitDialog.IsOpen);
            Assert.AreEqual(0f, Time.timeScale);

            lobby.HandleBack();
            Assert.IsFalse(lobby.QuitDialog.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void Lobby_HasOneQuitDialog_OneBackRouter_AndTheMatchScreensScaling()
        {
            LobbyScreen lobby = BuildLobby();

            Assert.AreEqual(1, built.GetComponentsInChildren<QuitDialog>(true).Length);
            Assert.AreEqual(1, built.GetComponentsInChildren<BackButtonRouter>(true).Length);
            Assert.AreSame(lobby.BackRouter, built.GetComponent<BackButtonRouter>());
            var scaler = built.GetComponent<CanvasScaler>();
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, scaler.screenMatchMode);
        }

        // ---------- Scenes and Build Settings ----------

        [Test]
        public void TheLobbyIsTheStartupScene_AndBothScenesAreInTheBuild()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            Assert.GreaterOrEqual(scenes.Length, 2);
            Assert.AreEqual("Assets/Zellige/Scenes/Lobby.unity", scenes[0].path);
            Assert.AreEqual("Assets/Zellige/Scenes/Game.unity", scenes[1].path);
            Assert.IsTrue(scenes[0].enabled && scenes[1].enabled);
            FileAssert.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenes[0].path));
            Assert.AreEqual(SceneNavigator.LobbyScene, Path.GetFileNameWithoutExtension(scenes[0].path));
            Assert.AreEqual(SceneNavigator.GameScene, Path.GetFileNameWithoutExtension(scenes[1].path));
        }

        // ---------- Match screen additions ----------

        [Test]
        public void ResultScreen_HasPlayAgainAndLobby_SideBySide()
        {
            GameScreen screen = BuildGameScreen();

            Button playAgain = screen.PlayAgainButton;
            Button lobby = screen.LobbyButton;
            Assert.IsNotNull(lobby);
            Assert.AreSame(playAgain.transform.parent, lobby.transform.parent, "both on the result panel");
            var a = (RectTransform)playAgain.transform;
            var b = (RectTransform)lobby.transform;
            Assert.AreEqual(a.anchoredPosition.y, b.anchoredPosition.y, 0.01f);
            Assert.Less(a.anchoredPosition.x + a.sizeDelta.x / 2f, b.anchoredPosition.x - b.sizeDelta.x / 2f, "no overlap");
            Assert.AreEqual("PLAY AGAIN", Label(playAgain));
            Assert.AreEqual("LOBBY", Label(lobby));
            Assert.IsFalse(screen.IsResultShown);
        }

        [Test]
        public void MatchScreen_HasTheLeaveDialog_AboveTheQuitDialog_AndOneBackRouter()
        {
            GameScreen screen = BuildGameScreen();
            ConfirmDialog leave = screen.LeaveDialog;
            QuitDialog quit = built.GetComponentInChildren<QuitDialog>(true);

            Assert.IsNotNull(leave);
            Assert.IsFalse(leave.IsOpen);
            Assert.AreEqual("Leave the match?", leave.Title);
            Assert.AreEqual("CANCEL", Label(leave.CancelButton));
            Assert.AreEqual("LEAVE", Label(leave.ConfirmButton));
            Assert.Greater(leave.GetComponent<Canvas>().sortingOrder, quit.GetComponent<Canvas>().sortingOrder);
            Assert.AreEqual(1, built.GetComponentsInChildren<BackButtonRouter>(true).Length);
            Assert.AreEqual(1, built.GetComponentsInChildren<QuitDialog>(true).Length, "the quit dialog is kept");
        }

        [Test]
        public void AddingTheNavigationTwice_ChangesNothing()
        {
            GameScreen screen = BuildGameScreen();

            Assert.IsFalse(screen.AddNavigationElements());

            Assert.AreEqual(1, built.GetComponentsInChildren<ConfirmDialog>(true).Length);
            Assert.AreEqual(1, built.GetComponentsInChildren<BackButtonRouter>(true).Length);
            Assert.AreEqual(1, built.GetComponentsInChildren<Button>(true).Count(b => b.name == "Lobby Button"));
        }

        [Test]
        public void Dialogs_NeverListenForTheBackKeyThemselves()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Assert.IsNull(typeof(QuitDialog).GetMethod("Update", flags), "QuitDialog must not read the key");
            Assert.IsNull(typeof(ConfirmDialog).GetMethod("Update", flags), "ConfirmDialog must not read the key");
            Assert.IsNull(typeof(LobbyScreen).GetMethod("Update", flags));
            Assert.IsNull(typeof(Zellige.Game.GameController).GetMethod("Update", flags));
            Assert.IsNotNull(typeof(BackButtonRouter).GetMethod("Update", flags), "the router is the one listener");
        }

        // ---------- Confirmation dialog ----------

        private ConfirmDialog BuildDialog()
        {
            var canvas = new GameObject("Test Canvas", typeof(RectTransform), typeof(Canvas));
            built = canvas;
            return ConfirmDialog.Build(canvas.transform, "Leave Match Dialog", TextKeys.LeaveTitle, TextKeys.LeaveMessage,
                                       TextKeys.LeaveCancel, TextKeys.LeaveConfirm);
        }

        [Test]
        public void ConfirmDialog_Cancel_ClosesIt_RestoresTime_AndDoesNotConfirm()
        {
            ConfirmDialog dialog = BuildDialog();
            int confirmed = 0, cancelled = 0;
            dialog.Confirmed += () => confirmed++;
            dialog.Cancelled += () => cancelled++;
            Time.timeScale = 0.5f;

            dialog.Open();
            Assert.IsTrue(dialog.IsOpen);
            Assert.AreEqual(0f, Time.timeScale, "the match is paused while the question is open");
            dialog.Cancel();
            dialog.Cancel();

            Assert.IsFalse(dialog.IsOpen);
            Assert.AreEqual(0.5f, Time.timeScale);
            Assert.AreEqual(1, cancelled);
            Assert.AreEqual(0, confirmed);
        }

        [Test]
        public void ConfirmDialog_Confirm_FiresOnce_EvenIfPressedTwice()
        {
            ConfirmDialog dialog = BuildDialog();
            int confirmed = 0;
            float timeScaleSeenByListener = -1f;
            dialog.Confirmed += () => { confirmed++; timeScaleSeenByListener = Time.timeScale; };
            Time.timeScale = 1f;

            dialog.Open();
            dialog.Open();
            dialog.Confirm();
            dialog.Confirm();

            Assert.AreEqual(1, confirmed);
            Assert.AreEqual(1f, timeScaleSeenByListener, "time runs again before the scene changes");
            Assert.IsFalse(dialog.IsOpen);
        }
    }
}
