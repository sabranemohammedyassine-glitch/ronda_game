using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zellige.UI
{
    /// <summary>
    /// "Quit Zellige?" confirmation: the application-exit action. In the Lobby, the Android Back
    /// button (Escape on desktop; Unity's legacy input reports Android Back as Escape) opens it,
    /// and Back while it is open cancels it, like any Android dialog.
    ///
    /// Since L2 the dialog no longer listens for the key itself: the scene's single
    /// <see cref="BackButtonRouter"/> calls <see cref="HandleBack"/>, so the quit dialog and the
    /// "Leave the match?" dialog can never both react to one press (D5).
    ///
    /// The visible part (<see cref="overlay"/>) is a child that is shown and hidden. The object has
    /// its own canvas with a higher sorting order, so the dialog is always drawn above the
    /// table, call-outs and the result screen, and its dimmed backdrop blocks all clicks.
    /// While it is open the game is paused (time scale 0): card animations and Nadia's
    /// thinking delay stop, and resume exactly where they were on Cancel.
    /// </summary>
    public sealed class QuitDialog : MonoBehaviour
    {
        public const string ObjectName = "Quit Dialog";
        private const int SortingOrder = 100;

        [SerializeField] private GameObject overlay;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button quitButton;

        private float timeScaleBeforeOpen = 1f;

        public bool IsOpen => overlay != null && overlay.activeSelf;

        private void Awake()
        {
            EnsureSorting();
            overlay.SetActive(false);
            cancelButton.onClick.AddListener(Close);
            quitButton.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            // Never leave the game paused if this object goes away while the dialog is open.
            if (IsOpen) Close();
        }

        /// <summary>The Back action: opens the dialog, or cancels it when it is already open.</summary>
        public void HandleBack()
        {
            if (IsOpen) Close();
            else Open();
        }

        /// <summary>
        /// Draws the dialog above the rest of its scene. Unity ignores "override sorting" on a canvas
        /// created inside an inactive hierarchy, so this is applied again once the dialog is active.
        /// </summary>
        public void EnsureSorting()
        {
            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
        }

        public void Open()
        {
            if (IsOpen) return;
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
            overlay.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            overlay.SetActive(false);
            Time.timeScale = timeScaleBeforeOpen;
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Application.Quit is ignored in the Editor
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Builds the dialog under the game canvas (used by the Game UI builder and by the
        /// one-off "Add Quit Dialog" editor command). The object stays active; the overlay
        /// starts hidden.
        /// </summary>
        public static QuitDialog Build(Transform canvasRoot)
        {
            RectTransform root = UiKit.Rect(ObjectName, canvasRoot, Vector2.zero, Vector2.zero);
            UiKit.Stretch(root);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var dialog = root.gameObject.AddComponent<QuitDialog>();

            Image dim = UiKit.Image("Overlay", root, Vector2.zero, Vector2.zero, null, new Color(0.02f, 0.04f, 0.1f, 0.75f));
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true; // blocks clicks on everything behind
            dialog.overlay = dim.gameObject;

            Sprite panelSprite = SpriteLibrary.Get("UI/ui_dialog") ?? SpriteLibrary.Get("UI/ui_panel");
            Image panel = UiKit.Image("Panel", dim.transform, Vector2.zero, new Vector2(760f, 420f), panelSprite, Palette.NavyLight, 1f);
            TextMeshProUGUI title = UiKit.Text("Title", panel.transform, new Vector2(0f, 110f), new Vector2(680f, 80f),
                "Quit Zellige?", 56f, Palette.Gold);
            title.fontStyle = FontStyles.Bold;
            UiKit.Text("Message", panel.transform, new Vector2(0f, 20f), new Vector2(680f, 60f),
                "Do you want to quit the game?", 34f, Palette.Ivory);
            dialog.cancelButton = UiKit.Button("Cancel Button", panel.transform, new Vector2(-150f, -110f), new Vector2(260f, 88f),
                "CANCEL", out _, primary: false);
            dialog.quitButton = UiKit.Button("Quit Button", panel.transform, new Vector2(150f, -110f), new Vector2(260f, 88f),
                "QUIT", out _);

            dim.gameObject.SetActive(false);
            return dialog;
        }
    }
}
