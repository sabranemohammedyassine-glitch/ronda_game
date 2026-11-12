using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zellige.Core;

namespace Zellige.UI
{
    /// <summary>
    /// One card on screen. It only shows a <see cref="Card"/>; it knows no rules.
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

        private Image face;
        private Image glow;
        private TextMeshProUGUI placeholderLabel;

        public Card Card { get; private set; }
        public RectTransform Rect { get; private set; }
        public bool IsFaceUp { get; private set; }

        public static CardView Create(Transform parent, Card card, Action<CardView> onClick)
        {
            RectTransform rect = UiKit.Rect("Card " + card, parent, Vector2.zero, BaseSize);
            var view = rect.gameObject.AddComponent<CardView>();
            view.Card = card;
            view.Rect = rect;

            // The glow art is 440x620 around a 360x540 card; keep the same proportions.
            Sprite glowSprite = SpriteLibrary.Get("UI/fx_card_select");
            Vector2 glowSize = glowSprite != null
                ? new Vector2(BaseSize.x * 440f / 360f, BaseSize.y * 620f / 540f)
                : BaseSize + new Vector2(24f, 24f);
            view.glow = UiKit.Image("Glow", rect, Vector2.zero, glowSize, glowSprite, Palette.WithAlpha(Palette.Gold, 0.85f));
            view.glow.type = Image.Type.Simple;
            view.glow.enabled = false;

            view.face = UiKit.Image("Face", rect, Vector2.zero, BaseSize, null, Palette.Ivory);
            view.face.raycastTarget = true;
            var button = view.face.gameObject.AddComponent<Button>();
            button.targetGraphic = view.face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick(view));

            // Only shown when the card artwork has not been imported.
            view.placeholderLabel = UiKit.Text("Placeholder", rect, Vector2.zero, BaseSize,
                card.Rank + "\n<size=35%>" + card.Suit + "</size>", 90f, SuitColors[(int)card.Suit]);
            view.placeholderLabel.fontStyle = FontStyles.Bold;

            view.SetFaceUp(false);
            return view;
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
