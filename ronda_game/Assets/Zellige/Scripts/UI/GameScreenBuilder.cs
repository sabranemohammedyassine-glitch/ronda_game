using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zellige.UI
{
    /// <summary>
    /// Builds the match screen's object hierarchy. The editor command
    /// Zellige > Build or Refresh Game UI calls this in Edit Mode and saves the result
    /// into Game.unity, where every object can then be edited in the Inspector.
    /// The same code is the runtime fallback when a scene has no authored UI.
    /// Layout follows UI-Design/Generated/mockup_game_screen.png on a 1920x1080 canvas;
    /// positions are relative to the screen centre.
    /// </summary>
    public sealed partial class GameScreen
    {
        public const string CanvasName = "Zellige Canvas";

        private static readonly Vector2[] PanelPositions = { new Vector2(-648f, -381f), new Vector2(-648f, 384f) };
        private static readonly Vector2 PanelSize = new Vector2(425f, 132f);
        private static readonly Vector2 DeckPosition = new Vector2(-700f, 23f);
        private static readonly Vector2 ScoreboardPosition = new Vector2(705f, 232f);
        private static readonly Vector2 TableAreaPosition = new Vector2(0f, 10f);
        private static readonly Vector2 HumanHandPosition = new Vector2(0f, -330f);
        private static readonly Vector2 OpponentHandPosition = new Vector2(0f, 373f);

        // Responsive layout: every HUD element is anchored to its nearest screen edge or corner,
        // keeping its exact position in the 1920x1080 reference, so 16:9 looks unchanged and
        // other aspect ratios keep each element at the same distance from the frame.
        private static readonly Vector2 ReferenceSize = new Vector2(1920f, 1080f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        // The table art: its 80 px border band is the zellige frame (9-slice border set in ArtImporter).
        private const string TableArtPath = "UI/bg_table_main";
        private const float TableFrameWidth = 80f;

        private static void Anchor(Component element, Vector2 anchor)
        {
            UiKit.AnchorTo((RectTransform)element.transform, anchor, ReferenceSize);
        }

        /// <summary>Creates an EventSystem unless the scene already has one.</summary>
        public static EventSystem EnsureEventSystem()
        {
            var existing = FindObjectOfType<EventSystem>();
            if (existing != null) return existing;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return go.GetComponent<EventSystem>();
        }

        /// <summary>Fallback used by GameController when the scene has no authored UI.</summary>
        public static GameScreen CreateAtRuntime()
        {
            EnsureEventSystem();
            return BuildHierarchy(SpriteLibrary.RuntimeCircle, null);
        }

        /// <summary>
        /// Creates the whole canvas and returns its GameScreen with every reference filled in.
        /// <paramref name="circle"/> is the disc used for masks and round buttons;
        /// <paramref name="outlineMaterial"/> (optional) is a shared outlined-text material.
        /// </summary>
        public static GameScreen BuildHierarchy(Sprite circle, Material outlineMaterial)
        {
            var canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            // Built inactive so that, at runtime, Awake only runs once every reference is set.
            canvasObject.SetActive(false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // Expand keeps the whole 1920x1080 layout visible on any aspect ratio;
            // the background is enlarged to cover the extra space.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var screen = canvasObject.AddComponent<GameScreen>();
            screen.sound = canvasObject.AddComponent<SoundFx>(); // also adds its AudioSource
            canvasObject.GetComponent<AudioSource>().playOnAwake = false;
            screen.BuildChildren(circle, outlineMaterial);

            canvasObject.SetActive(true);
            // Dialog canvases were created while the canvas was inactive; apply their sorting now.
            foreach (QuitDialog quit in canvasObject.GetComponentsInChildren<QuitDialog>(true)) quit.EnsureSorting();
            if (screen.leaveDialog != null) screen.leaveDialog.EnsureSorting();
            return screen;
        }

        private void BuildChildren(Sprite circle, Material outlineMaterial)
        {
            Transform root = transform;

            backgroundButton = BuildBackground(root);

            panels = new PlayerPanel[2];
            panels[HumanSeat] = BuildPlayerPanel(root, HumanSeat, circle);
            panels[OpponentSeat] = BuildPlayerPanel(root, OpponentSeat, circle);
            BuildScoreboard(root);
            BuildDeck(root);
            BuildCardAreas(root);

            hintLabel = UiKit.Text("Hint", root, new Vector2(0f, -126f), new Vector2(1000f, 40f), "", 26f,
                Palette.WithAlpha(Palette.Silver, 0.9f));

            BuildCallout(root, outlineMaterial);

            announceButton = UiKit.Button("Announce Button", root, new Vector2(708f, -377f), new Vector2(300f, 84f),
                "RONDA!", out announceLabel);
            Anchor(announceButton, BottomRight);
            announceButton.gameObject.SetActive(false);

            soundOnIcon = SpriteLibrary.Get("UI/icon_sound_on");
            soundOffIcon = SpriteLibrary.Get("UI/icon_sound_off");
            soundButton = UiKit.IconButton("Sound Button", root, new Vector2(748f, 412f), 72f, circle,
                soundOnIcon, "Sound", out soundIcon, out soundFallbackLabel);
            Anchor(soundButton, TopRight);
            newGameButton = UiKit.IconButton("New Game Button", root, new Vector2(828f, 412f), 72f, circle,
                SpriteLibrary.Get("UI/icon_restart"), "New", out _, out _);
            Anchor(newGameButton, TopRight);

            BuildResultOverlay(root);

            var templates = UiKit.Rect("Templates", root, Vector2.zero, Vector2.zero);
            cardTemplate = CardView.BuildTemplate(templates);

            QuitDialog.Build(root);
            AddNavigationElements();
        }

        /// <summary>
        /// L2 additions to the match screen: the result screen's Lobby button beside Play Again (D4),
        /// the "Leave the match?" dialog (D5) and the scene's single Back button router.
        /// Adds only what is missing, so it can upgrade a Game UI saved before L2 without rebuilding
        /// it (Zellige > Add Lobby Navigation to Game UI) and is harmless to run twice.
        /// Returns true if anything was added.
        /// </summary>
        public bool AddNavigationElements()
        {
            bool changed = false;

            if (lobbyButton == null && playAgainButton != null)
            {
                // Play Again moves left to sit beside the new Lobby button; nothing else on the result screen changes.
                var playAgain = (RectTransform)playAgainButton.transform;
                playAgain.anchoredPosition = new Vector2(-190f, playAgain.anchoredPosition.y);
                playAgain.sizeDelta = new Vector2(340f, playAgain.sizeDelta.y);
                lobbyButton = UiKit.Button("Lobby Button", playAgain.parent, new Vector2(190f, playAgain.anchoredPosition.y),
                    playAgain.sizeDelta, "", out TextMeshProUGUI lobbyLabel, primary: false);
                LocalizedText.Attach(lobbyLabel, Zellige.App.TextKeys.ResultLobby);
                if (Application.isPlaying) WireLobbyButton();
                changed = true;
            }

            if (leaveDialog == null)
            {
                leaveDialog = ConfirmDialog.Build(transform, "Leave Match Dialog", Zellige.App.TextKeys.LeaveTitle,
                    Zellige.App.TextKeys.LeaveMessage, Zellige.App.TextKeys.LeaveCancel, Zellige.App.TextKeys.LeaveConfirm);
                changed = true;
            }

            if (GetComponent<BackButtonRouter>() == null)
            {
                gameObject.AddComponent<BackButtonRouter>();
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// The table: a solid colour (also the tap-to-deselect target), the felt zoomed to fill
        /// the space inside the frame (never stretched), and the zellige frame drawn along the
        /// screen edges whatever the aspect ratio. Returns the background's button.
        /// </summary>
        private Button BuildBackground(Transform root)
        {
            Image background = UiKit.Image("Background", root, Vector2.zero, Vector2.zero, null, Palette.NavyDeep);
            UiKit.Stretch(background.rectTransform);
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            Sprite tableArt = SpriteLibrary.Get(TableArtPath);
            if (tableArt == null) return button; // no art imported: plain colour

            // The felt: the inside of the table art (a UV window on the same texture), inset by the
            // frame width and zoomed to cover the space; the mask trims what the zoom pushes outside.
            Rect full = tableArt.rect;
            RectTransform feltArea = UiKit.Rect("Felt Area", background.transform, Vector2.zero, Vector2.zero);
            UiKit.Stretch(feltArea);
            feltArea.offsetMin = Vector2.one * TableFrameWidth;
            feltArea.offsetMax = -Vector2.one * TableFrameWidth;
            feltArea.gameObject.AddComponent<RectMask2D>();

            RectTransform feltRect = UiKit.Rect("Felt", feltArea, Vector2.zero, Vector2.zero);
            var felt = feltRect.gameObject.AddComponent<RawImage>();
            felt.texture = tableArt.texture;
            felt.uvRect = new Rect(TableFrameWidth / full.width, TableFrameWidth / full.height,
                1f - 2f * TableFrameWidth / full.width, 1f - 2f * TableFrameWidth / full.height);
            felt.raycastTarget = false;
            var fitter = feltRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (full.width - 2f * TableFrameWidth) / (full.height - 2f * TableFrameWidth);

            // The frame: the art's border band only, its edges repeated along the screen edges.
            Image frame = UiKit.Image("Frame", background.transform, Vector2.zero, Vector2.zero, tableArt, Color.clear);
            UiKit.Stretch(frame.rectTransform);
            frame.type = Image.Type.Tiled;
            frame.fillCenter = false;
            return button;
        }

        private PlayerPanel BuildPlayerPanel(Transform root, int seat, Sprite circle)
        {
            var panel = new PlayerPanel();

            Image body = UiKit.Image(seat == HumanSeat ? "Human Panel" : "Opponent Panel", root, PanelPositions[seat], PanelSize,
                SpriteLibrary.Get("UI/ui_panel"), Palette.WithAlpha(Palette.NavyLight, 0.9f), 1.4f);
            Anchor(body, seat == HumanSeat ? BottomLeft : TopLeft);
            Transform t = body.transform;
            var avatarPosition = new Vector2(-142f, 0f);

            panel.TurnRing = UiKit.Image("Turn Ring", t, avatarPosition, new Vector2(150f, 150f),
                SpriteLibrary.Get("UI/ui_turn_ring"), Palette.Orange);
            panel.TurnRing.enabled = false;
            UiKit.CircularImage("Avatar", t, avatarPosition, 92f, circle,
                SpriteLibrary.Get(seat == HumanSeat ? "UI/avatar_01" : "UI/avatar_02"), Palette.Silver);
            UiKit.Image("Avatar Frame", t, avatarPosition, new Vector2(116f, 116f), SpriteLibrary.Get("UI/frame_avatar"), Color.clear);

            TextMeshProUGUI playerName = UiKit.Text("Name", t, new Vector2(4f, 18f), new Vector2(150f, 48f),
                PlayerNames[seat], 38f, Palette.Ivory, TextAlignmentOptions.Left);
            playerName.fontStyle = FontStyles.Bold;
            panel.Role = UiKit.Text("Role", t, new Vector2(4f, -26f), new Vector2(150f, 30f),
                seat == HumanSeat ? "PLAYER" : "COMPUTER", 19f, Palette.Silver, TextAlignmentOptions.Left);
            panel.Role.characterSpacing = 8f;

            Image capsule = UiKit.Image("Captured Capsule", t, new Vector2(136f, 8f), new Vector2(106f, 50f),
                SpriteLibrary.Get("UI/ui_score_capsule"), Palette.NavyDeep);
            panel.PileTarget = capsule.rectTransform;
            panel.Captured = UiKit.Text("Captured", capsule.transform, new Vector2(0f, 1f), new Vector2(106f, 50f), "0", 32f, Palette.Gold);
            panel.Captured.fontStyle = FontStyles.Bold;
            UiKit.Text("Captured Caption", t, new Vector2(136f, -36f), new Vector2(120f, 24f), "CAPTURED", 14f, Palette.Silver)
                .characterSpacing = 6f;

            Sprite chipSprite = SpriteLibrary.Get("UI/ui_dealer_chip");
            Image chip = UiKit.Image("Dealer Chip", t, avatarPosition + new Vector2(42f, 42f), new Vector2(38f, 38f),
                chipSprite != null ? chipSprite : circle, Palette.Gold);
            if (chipSprite == null)
            {
                chip.color = Palette.Gold;
                UiKit.Text("D", chip.transform, new Vector2(0f, 1f), new Vector2(38f, 38f), "D", 22f, Palette.Ink).fontStyle = FontStyles.Bold;
            }
            panel.DealerChip = chip.gameObject;
            panel.DealerChip.SetActive(false);
            return panel;
        }

        private void BuildScoreboard(Transform root)
        {
            Image board = UiKit.Image("Scoreboard", root, ScoreboardPosition, new Vector2(330f, 220f),
                SpriteLibrary.Get("UI/ui_panel"), Palette.WithAlpha(Palette.NavyLight, 0.9f), 1.4f);
            Anchor(board, TopRight);
            Transform t = board.transform;
            UiKit.Text("Title", t, new Vector2(0f, 80f), new Vector2(300f, 30f), "SCORE", 22f, Palette.Silver).characterSpacing = 12f;
            UiKit.Image("Divider", t, new Vector2(0f, 60f), new Vector2(260f, 2f), null, Palette.WithAlpha(Palette.Silver, 0.35f));

            scoreLabels = new TextMeshProUGUI[2];
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
            deck = UiKit.Image("Deck", root, DeckPosition, CardView.BaseSize * DeckScale,
                SpriteLibrary.CardBack, Palette.NavyLight).rectTransform;
            Anchor(deck, MiddleLeft);
            Image capsule = UiKit.Image("Deck Capsule", root, DeckPosition + new Vector2(0f, -164f), new Vector2(112f, 50f),
                SpriteLibrary.Get("UI/ui_score_capsule"), Palette.NavyDeep);
            Anchor(capsule, MiddleLeft);
            deckLabel = UiKit.Text("Deck Count", capsule.transform, new Vector2(0f, 1f), new Vector2(112f, 50f), "40", 32f, Palette.Ivory);
            deckLabel.fontStyle = FontStyles.Bold;
            TextMeshProUGUI caption = UiKit.Text("Deck Caption", root, DeckPosition + new Vector2(0f, -212f), new Vector2(160f, 30f),
                "DECK", 20f, Palette.Silver);
            caption.characterSpacing = 12f;
            Anchor(caption, MiddleLeft);
        }

        /// <summary>
        /// Empty, movable areas that position the dynamic cards, and the layer the cards
        /// are created in. Select an area in the Scene view to see or move its rectangle.
        /// </summary>
        private void BuildCardAreas(Transform root)
        {
            RectTransform areas = UiKit.Rect("Card Areas", root, Vector2.zero, Vector2.zero);
            UiKit.Stretch(areas);

            // Width = room for the row of table cards (the spacing shrinks to fit). It stretches
            // with the screen width, keeping the same margins as in the 1920x1080 reference.
            float tableWidth = 1060f + CardView.BaseSize.x * TableScale;
            tableArea = UiKit.Rect("Table Area", areas, TableAreaPosition, new Vector2(tableWidth - ReferenceSize.x, CardView.BaseSize.y * TableScale));
            tableArea.anchorMin = new Vector2(0f, 0.5f);
            tableArea.anchorMax = new Vector2(1f, 0.5f);

            humanHandArea = UiKit.Rect("Human Hand Area", areas, HumanHandPosition, new Vector2(4 * 182f, CardView.BaseSize.y * HandScale));
            Anchor(humanHandArea, BottomCenter);
            opponentHandArea = UiKit.Rect("Opponent Hand Area", areas, OpponentHandPosition, new Vector2(4 * 84f, CardView.BaseSize.y * OpponentScale));
            Anchor(opponentHandArea, TopCenter);

            cardLayer = UiKit.Rect("Cards", root, Vector2.zero, Vector2.zero);
        }

        private void BuildCallout(Transform root, Material outlineMaterial)
        {
            callout = UiKit.Rect("Callout", root, new Vector2(0f, 205f), new Vector2(1100f, 170f));
            Sprite banner = SpriteLibrary.Get("UI/ui_callout");
            Image background = UiKit.Image("Banner", callout, new Vector2(0f, 22f), new Vector2(820f, 128f), banner, Color.clear, 1f);
            background.enabled = banner != null;

            // The title must stay between the banner's ornamented ends.
            calloutTitle = UiKit.Text("Title", callout, new Vector2(0f, 24f), new Vector2(560f, 96f), "RONDA!", 76f, Palette.Gold);
            calloutTitle.fontStyle = FontStyles.Bold;
            calloutTitle.enableAutoSizing = true;
            calloutTitle.fontSizeMin = 36f;
            calloutTitle.fontSizeMax = 76f;
            calloutTitle.enableWordWrapping = false;
            UiKit.Outline(calloutTitle, outlineMaterial, 0.18f);
            // 1000 wide so it stays clear of the deck (left) and the scoreboard (right) at 16:9.
            calloutDetail = UiKit.Text("Detail", callout, new Vector2(0f, -48f), new Vector2(1000f, 44f), "Call-out details", 30f, Palette.Ivory);
            // One line only: long details (e.g. several announcements) shrink instead of wrapping
            // onto a second line that would hang over the table cards.
            calloutDetail.enableAutoSizing = true;
            calloutDetail.fontSizeMin = 22f;
            calloutDetail.fontSizeMax = 30f;
            calloutDetail.enableWordWrapping = false;
            UiKit.Outline(calloutDetail, outlineMaterial, 0.2f);
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
            victoryIllustration = SpriteLibrary.Get("UI/illus_victory");
            defeatIllustration = SpriteLibrary.Get("UI/illus_defeat");
            resultIllustration = UiKit.Image("Illustration", panel.transform, new Vector2(0f, 135f), new Vector2(416f, 260f),
                victoryIllustration, Color.clear);
            resultIllustration.preserveAspect = true;
            resultTitle = UiKit.Text("Title", panel.transform, new Vector2(0f, -40f), new Vector2(760f, 110f), "VICTORY", 88f, Palette.Gold);
            resultTitle.fontStyle = FontStyles.Bold;
            resultTitle.characterSpacing = 6f;
            resultScore = UiKit.Text("Score", panel.transform, new Vector2(0f, -115f), new Vector2(760f, 60f), "You 41   –   30 Nadia", 40f, Palette.Ivory);
            playAgainButton = UiKit.Button("Play Again", panel.transform, new Vector2(0f, -205f), new Vector2(360f, 96f),
                "PLAY AGAIN", out _);
            resultOverlay.SetActive(false);
        }
    }
}
