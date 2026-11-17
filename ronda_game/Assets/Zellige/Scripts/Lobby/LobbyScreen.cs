using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zellige.App;
using Zellige.UI;

namespace Zellige.Lobby
{
    /// <summary>
    /// The Lobby Home screen (L2). The three game modes are shown directly on Home (D2):
    /// Me vs Computer starts a match; 1 vs 1 and 2 vs 2 are locked "Coming Soon" cards (D1) whose
    /// buttons cannot be pressed and which never create a config, a session or a network request.
    /// Back opens the existing "Quit Zellige?" dialog, the application-exit action.
    ///
    /// Profile, Statistics, Settings, How to play and About belong to L3/L4. No button for them
    /// is shown yet, so nothing on this screen looks usable without working.
    ///
    /// The objects are authored in Lobby.unity by Zellige > Build or Refresh Lobby UI
    /// (<see cref="BuildHierarchy"/>), like the match screen.
    /// </summary>
    public sealed class LobbyScreen : MonoBehaviour, IBackHandler
    {
        public const string CanvasName = "Lobby Canvas";

        [Serializable]
        public sealed class ModeCard
        {
            public MatchMode Mode;
            public Button Button;
            public TextMeshProUGUI ButtonLabel;
        }

        [SerializeField] private ModeCard[] modeCards;
        [SerializeField] private QuitDialog quitDialog;
        [SerializeField] private BackButtonRouter backRouter;

        public IReadOnlyList<ModeCard> ModeCards => modeCards;
        public QuitDialog QuitDialog => quitDialog;
        public BackButtonRouter BackRouter => backRouter;

        private void Awake()
        {
            // A pause can never leak in from the match (dialogs restore it, but the Lobby must always run).
            Time.timeScale = 1f;

            foreach (ModeCard card in modeCards)
            {
                MatchMode mode = card.Mode;
                bool playable = MatchModes.IsPlayable(mode);
                card.Button.interactable = playable;
                if (playable) card.Button.onClick.AddListener(() => RequestMode(mode));
            }
            backRouter.SetHandler(this);
        }

        /// <summary>
        /// A tap on a mode card. Me vs Computer prepares a fresh config and loads the Game scene;
        /// a locked mode returns false and nothing happens.
        /// </summary>
        public bool RequestMode(MatchMode mode)
        {
            if (quitDialog.IsOpen) return false;
            return SceneNavigator.StartMatch(mode);
        }

        /// <summary>Android Back on Home: opens "Quit Zellige?", or cancels it when it is open.</summary>
        public void HandleBack() => quitDialog.HandleBack();

        // ---------- Building (used by the editor command; also usable at runtime) ----------

        private static readonly Vector2 CardSize = new Vector2(460f, 500f);
        private const float CardSpacing = 540f;
        private const float CardsY = -70f;
        private const float FrameWidth = 80f;

        /// <summary>Creates the whole Lobby canvas and returns its screen with every reference set.</summary>
        public static LobbyScreen BuildHierarchy(Sprite circle, Material outlineMaterial = null)
        {
            var canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.SetActive(false); // so Awake only runs once every reference is set

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            // Same strategy as the match screen: the whole 1920x1080 layout stays visible on any
            // aspect ratio (16:9, 20:9, 4:3); the background fills the extra space.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var screen = canvasObject.AddComponent<LobbyScreen>();
            Transform root = canvasObject.transform;

            BuildBackground(root);
            BuildTitle(root, outlineMaterial);

            screen.modeCards = new[]
            {
                BuildModeCard(root, MatchMode.VsComputer, new Vector2(-CardSpacing, CardsY), TextKeys.ModeVsComputerDetail, circle),
                BuildModeCard(root, MatchMode.Online1v1, new Vector2(0f, CardsY), TextKeys.ModeOnline1v1Detail, circle),
                BuildModeCard(root, MatchMode.Online2v2, new Vector2(CardSpacing, CardsY), TextKeys.ModeOnline2v2Detail, circle)
            };

            screen.quitDialog = QuitDialog.Build(root);
            screen.backRouter = canvasObject.AddComponent<BackButtonRouter>();

            canvasObject.SetActive(true);
            screen.quitDialog.EnsureSorting(); // created while the canvas was inactive
            return screen;
        }

        /// <summary>Deep navy with the table art's zellige border band along the screen edges (existing art only).</summary>
        private static void BuildBackground(Transform root)
        {
            Image background = UiKit.Image("Background", root, Vector2.zero, Vector2.zero, null, Palette.NavyDeep);
            UiKit.Stretch(background.rectTransform);

            Sprite tableArt = SpriteLibrary.Get("UI/bg_table_main");
            if (tableArt == null) return;
            Image frame = UiKit.Image("Frame", background.transform, Vector2.zero, Vector2.zero, tableArt, Color.clear);
            UiKit.Stretch(frame.rectTransform);
            frame.type = Image.Type.Tiled;
            frame.fillCenter = false;

            Image glow = UiKit.Image("Inner Panel", background.transform, Vector2.zero, Vector2.zero,
                SpriteLibrary.Get("UI/ui_panel"), Palette.Navy, 0.6f);
            UiKit.Stretch(glow.rectTransform);
            glow.rectTransform.offsetMin = Vector2.one * (FrameWidth + 24f);
            glow.rectTransform.offsetMax = -Vector2.one * (FrameWidth + 24f);
            glow.color = Palette.WithAlpha(Color.white, 0.35f);
        }

        private static void BuildTitle(Transform root, Material outlineMaterial)
        {
            TextMeshProUGUI title = UiKit.Text("Title", root, new Vector2(0f, 345f), new Vector2(1200f, 150f), "", 128f, Palette.Gold);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 24f;
            if (outlineMaterial != null || Application.isPlaying) UiKit.Outline(title, outlineMaterial, 0.15f);
            LocalizedText.Attach(title, TextKeys.LobbyTitle);

            TextMeshProUGUI subtitle = UiKit.Text("Subtitle", root, new Vector2(0f, 255f), new Vector2(1200f, 60f), "", 42f, Palette.Silver);
            subtitle.characterSpacing = 10f;
            LocalizedText.Attach(subtitle, TextKeys.LobbySubtitle);
        }

        private static ModeCard BuildModeCard(Transform root, MatchMode mode, Vector2 position, string detailKey, Sprite circle)
        {
            bool playable = MatchModes.IsPlayable(mode);
            Image panel = UiKit.Image(mode + " Card", root, position, CardSize, SpriteLibrary.Get("UI/ui_panel"),
                Palette.NavyLight, 1.2f);
            panel.color = playable ? Color.white : new Color(0.72f, 0.75f, 0.82f, 0.85f);
            Transform t = panel.transform;

            TextMeshProUGUI name = UiKit.Text("Mode Name", t, new Vector2(0f, 180f), new Vector2(420f, 70f), "", 52f,
                playable ? Palette.Gold : Palette.Silver);
            name.fontStyle = FontStyles.Bold;
            name.enableWordWrapping = false;   // one line; long names (or translations) shrink to fit
            name.enableAutoSizing = true;
            name.fontSizeMin = 32f;
            name.fontSizeMax = 52f;
            LocalizedText.Attach(name, MatchModes.NameKey(mode));
            TextMeshProUGUI detail = UiKit.Text("Detail", t, new Vector2(0f, 122f), new Vector2(420f, 44f), "", 27f, Palette.Ivory);
            LocalizedText.Attach(detail, detailKey);

            BuildSeats(t, mode, circle, playable);

            Button button = UiKit.Button(playable ? "Play Button" : "Coming Soon Button", t, new Vector2(0f, -178f),
                new Vector2(360f, 96f), "", out TextMeshProUGUI label, primary: playable);
            label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            if (!playable)
            {
                label.fontSize = 30f;
                label.color = Palette.Silver;
                button.interactable = false;
                ColorBlock colors = button.colors;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.7f);
                button.colors = colors;
            }
            LocalizedText.Attach(label, playable ? TextKeys.ModePlay : TextKeys.ModeComingSoon);

            return new ModeCard { Mode = mode, Button = button, ButtonLabel = label };
        }

        /// <summary>The players of the mode, drawn with the two existing avatars; dimmed on locked cards.</summary>
        private static void BuildSeats(Transform card, MatchMode mode, Sprite circle, bool playable)
        {
            Color tint = playable ? Color.white : new Color(0.55f, 0.6f, 0.7f, 0.6f);
            Sprite you = SpriteLibrary.Get("UI/avatar_01");
            Sprite other = SpriteLibrary.Get("UI/avatar_02");
            Sprite frame = SpriteLibrary.Get("UI/frame_avatar");

            if (mode == MatchMode.Online2v2)
            {
                Seat(card, new Vector2(-135f, 32f), 88f, you, frame, circle, tint);
                Seat(card, new Vector2(-65f, -38f), 88f, you, frame, circle, tint);
                Seat(card, new Vector2(65f, 32f), 88f, other, frame, circle, tint);
                Seat(card, new Vector2(135f, -38f), 88f, other, frame, circle, tint);
            }
            else
            {
                Seat(card, new Vector2(-110f, 0f), 130f, you, frame, circle, tint);
                Seat(card, new Vector2(110f, 0f), 130f, other, frame, circle, tint);
            }
            TextMeshProUGUI versus = UiKit.Text("Versus", card, new Vector2(0f, -5f), new Vector2(80f, 60f), "", 36f,
                playable ? Palette.Orange : Palette.Silver);
            versus.fontStyle = FontStyles.Bold | FontStyles.Italic;
            LocalizedText.Attach(versus, TextKeys.ModeVersus);
        }

        private static void Seat(Transform card, Vector2 position, float size, Sprite avatar, Sprite frame, Sprite circle, Color tint)
        {
            Image picture = UiKit.CircularImage("Seat", card, position, size * 0.8f, circle, avatar, Palette.Silver);
            if (picture.sprite != null) picture.color = tint;
            Image ring = UiKit.Image("Seat Frame", card, position, Vector2.one * size, frame, Color.clear);
            if (ring.sprite != null) ring.color = tint;
        }
    }
}
