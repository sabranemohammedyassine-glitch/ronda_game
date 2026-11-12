using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zellige.Core;

namespace Zellige.UI
{
    /// <summary>
    /// One card on screen. It only shows a <see cref="Card"/>; it knows no rules.
    /// Cards are clones of the "Card Template" object saved in the scene, so the card's
    /// look (glow, face, placeholder text) can be edited in the Inspector.
    /// All card views have the same base size (2:3, like the art) and are scaled for
    /// hand, table and deck.
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        public static readonly Vector2 BaseSize = new Vector2(168f, 252f);

        private static readonly Color[] SuitColors =
        {
            Palette.Hex("#B7860F"), // Oros
            Palette.Hex("#B8432F"), // Copas
            Palette.Hex("#2F5FA8"), // Espadas
            Palette.Hex("#2F7A4B")  // Bastos
        };

        [SerializeField] private Image face;
        [SerializeField] private Image glow;
        [SerializeField] private Button button;
        [Tooltip("Shown only when the card artwork has not been imported.")]
        [SerializeField] private TextMeshProUGUI placeholderLabel;

        public Card Card { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public bool IsFaceUp { get; private set; }

        /// <summary>Builds the (inactive) template card that the game clones.</summary>
        public static CardView BuildTemplate(Transform parent)
        {
            RectTransform rect = UiKit.Rect("Card Template", parent, Vector2.zero, BaseSize);
            var view = rect.gameObject.AddComponent<CardView>();

            // The glow art is 440x620 around a 360x540 card; keep the same proportions.
            Sprite glowSprite = SpriteLibrary.Get("UI/fx_card_select");
            Vector2 glowSize = glowSprite != null
                ? new Vector2(BaseSize.x * 440f / 360f, BaseSize.y * 620f / 540f)
                : BaseSize + new Vector2(24f, 24f);
            view.glow = UiKit.Image("Glow", rect, Vector2.zero, glowSize, glowSprite, Palette.WithAlpha(Palette.Gold, 0.85f));
            view.glow.type = Image.Type.Simple;
            view.glow.enabled = false;

            view.face = UiKit.Image("Face", rect, Vector2.zero, BaseSize, SpriteLibrary.CardBack, Palette.NavyLight);
            view.face.raycastTarget = true;
            view.button = view.face.gameObject.AddComponent<Button>();
            view.button.targetGraphic = view.face;
            view.button.transition = Selectable.Transition.None;

            view.placeholderLabel = UiKit.Text("Placeholder", rect, Vector2.zero, BaseSize, "", 90f, Palette.Ink);
            view.placeholderLabel.fontStyle = FontStyles.Bold;
            view.placeholderLabel.enabled = false;

            rect.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Turns a fresh clone of the template into the given card, face down.</summary>
        public void Init(Card card, Action<CardView> onClick)
        {
            Card = card;
            name = "Card " + card;
            placeholderLabel.text = card.Rank + "\n<size=35%>" + card.Suit + "</size>";
            placeholderLabel.color = SuitColors[(int)card.Suit];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick(this));
            SetHighlight(false);
            SetFaceUp(false);
        }

        public void SetFaceUp(bool faceUp)
        {
            IsFaceUp = faceUp;
            Sprite sprite = faceUp ? SpriteLibrary.CardFace(Card) : SpriteLibrary.CardBack;
            face.sprite = sprite;
            face.color = sprite != null ? Color.white : (faceUp ? Palette.Ivory : Palette.NavyLight);
            placeholderLabel.enabled = faceUp && sprite == null;
        }

        public void SetHighlight(bool on)
        {
            glow.enabled = on;
        }
    }
}
