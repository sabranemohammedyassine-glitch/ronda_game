using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zellige.App;

namespace Zellige.UI
{
    /// <summary>
    /// A two-button confirmation drawn above everything else in its scene, with the same look and
    /// pause behaviour as <see cref="QuitDialog"/>: while it is open the game is paused (time scale 0)
    /// and its dimmed backdrop blocks every tap behind it. Used for "Leave the match?" (D5).
    /// It does not listen for the Back key; the scene's <see cref="BackButtonRouter"/> decides.
    /// Confirm and Cancel close the dialog first (restoring the time scale), then raise their
    /// event once; a second tap on a closed dialog does nothing.
    /// </summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        private const int SortingOrder = 110;   // above the quit dialog (100)

        [SerializeField] private GameObject overlay;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI title;

        private float timeScaleBeforeOpen = 1f;

        public event Action Confirmed;
        public event Action Cancelled;

        public bool IsOpen => overlay != null && overlay.activeSelf;
        public Button CancelButton => cancelButton;
        public Button ConfirmButton => confirmButton;
        public string Title => title != null ? title.text : null;

        private void Awake()
        {
            EnsureSorting();
            overlay.SetActive(false);
            cancelButton.onClick.AddListener(Cancel);
            confirmButton.onClick.AddListener(Confirm);
        }

        private void OnDisable()
        {
            // Never leave the game paused if this object goes away while the dialog is open.
            if (IsOpen) Hide();
        }

        public void Open()
        {
            if (IsOpen) return;
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
            overlay.SetActive(true);
        }

        /// <summary>Closes the dialog and raises <see cref="Cancelled"/>. Does nothing if it is closed.</summary>
        public void Cancel()
        {
            if (!IsOpen) return;
            Hide();
            Cancelled?.Invoke();
        }

        /// <summary>Closes the dialog and raises <see cref="Confirmed"/>. Does nothing if it is closed.</summary>
        public void Confirm()
        {
            if (!IsOpen) return;
            Hide();
            Confirmed?.Invoke();
        }

        /// <summary>
        /// Draws this dialog above the rest of its scene. Unity ignores "override sorting" on a canvas
        /// created inside an inactive hierarchy, so this is applied again once the dialog is active.
        /// </summary>
        public void EnsureSorting()
        {
            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
        }

        private void Hide()
        {
            overlay.SetActive(false);
            Time.timeScale = timeScaleBeforeOpen;
        }

        /// <summary>
        /// Builds the dialog under <paramref name="canvasRoot"/>. All texts are <see cref="TextKeys"/>.
        /// The object stays active; the overlay starts hidden.
        /// </summary>
        public static ConfirmDialog Build(Transform canvasRoot, string objectName, string titleKey, string messageKey,
                                          string cancelKey, string confirmKey)
        {
            RectTransform root = UiKit.Rect(objectName, canvasRoot, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);   // so Awake runs only once every reference is set
            UiKit.Stretch(root);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var dialog = root.gameObject.AddComponent<ConfirmDialog>();

            Image dim = UiKit.Image("Overlay", root, Vector2.zero, Vector2.zero, null, new Color(0.02f, 0.04f, 0.1f, 0.75f));
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true; // blocks taps on everything behind
            dialog.overlay = dim.gameObject;

            Sprite panelSprite = SpriteLibrary.Get("UI/ui_dialog") ?? SpriteLibrary.Get("UI/ui_panel");
            Image panel = UiKit.Image("Panel", dim.transform, Vector2.zero, new Vector2(760f, 420f), panelSprite, Palette.NavyLight, 1f);
            dialog.title = UiKit.Text("Title", panel.transform, new Vector2(0f, 110f), new Vector2(680f, 80f), "", 56f, Palette.Gold);
            dialog.title.fontStyle = FontStyles.Bold;
            LocalizedText.Attach(dialog.title, titleKey);
            TextMeshProUGUI message = UiKit.Text("Message", panel.transform, new Vector2(0f, 20f), new Vector2(680f, 80f), "", 32f, Palette.Ivory);
            LocalizedText.Attach(message, messageKey);
            dialog.cancelButton = UiKit.Button("Cancel Button", panel.transform, new Vector2(-150f, -110f), new Vector2(260f, 88f),
                "", out TextMeshProUGUI cancelLabel, primary: false);
            LocalizedText.Attach(cancelLabel, cancelKey);
            dialog.confirmButton = UiKit.Button("Confirm Button", panel.transform, new Vector2(150f, -110f), new Vector2(260f, 88f),
                "", out TextMeshProUGUI confirmLabel);
            LocalizedText.Attach(confirmLabel, confirmKey);

            dim.gameObject.SetActive(false);
            root.gameObject.SetActive(true);
            dialog.EnsureSorting();
            return dialog;
        }
    }
}
