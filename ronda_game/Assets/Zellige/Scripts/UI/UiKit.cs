using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zellige.Core;

namespace Zellige.UI
{
    /// <summary>Colours of the visual identity (see UI-Design/Specifications/01_visual_identity.md).</summary>
    public static class Palette
    {
        public static readonly Color Navy = Hex("#0B1A3F");
        public static readonly Color NavyDeep = Hex("#08122C");
        public static readonly Color NavyLight = Hex("#17306B");
        public static readonly Color Silver = Hex("#C9D1DC");
        public static readonly Color Gold = Hex("#E8BE5A");
        public static readonly Color Orange = Hex("#E8842C");
        public static readonly Color Ivory = Hex("#F6EFDF");
        public static readonly Color Ink = Hex("#1D2433");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    /// <summary>
    /// Loads artwork by name from Resources/Zellige (filled by the menu
    /// Zellige > Import Art from UI-Design). Returns null when a sprite is missing;
    /// callers then draw a flat placeholder, so art can be replaced without code changes.
    /// Works in Edit Mode too, which is how the UI builder assigns sprites to the scene.
    /// </summary>
    public static class SpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Sprite runtimeCircle;

        public static Sprite Get(string path)
        {
            if (!Cache.TryGetValue(path, out Sprite sprite) || sprite == null)
            {
                sprite = Resources.Load<Sprite>("Zellige/" + path);
                Cache[path] = sprite;
            }
            return sprite;
        }

        public static Sprite CardFace(Card card)
        {
            return Get("Cards/card_" + card.Suit.ToString().ToLowerInvariant() + "_" + card.Rank.ToString("00"));
        }

        public static Sprite CardBack => Get("Cards/card_back");

        /// <summary>
        /// A white disc made in memory. Only used when the UI is built at runtime; the
        /// editor builder saves the same disc as an asset so the scene can reference it.
        /// </summary>
        public static Sprite RuntimeCircle
        {
            get
            {
                if (runtimeCircle == null)
                {
                    Texture2D texture = CreateDiscTexture(256);
                    runtimeCircle = Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f);
                }
                return runtimeCircle;
            }
        }

        /// <summary>A white anti-aliased disc on a transparent background.</summary>
        public static Texture2D CreateDiscTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float radius = size / 2f - 1f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    byte alpha = (byte)(Mathf.Clamp01(radius - distance + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }

    /// <summary>Small helpers for building uGUI objects from code (used by the UI builder).</summary>
    public static class UiKit
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Anchors an element to a point of its parent, e.g. (0, 1) = top-left corner, without
        /// moving it in the reference layout (parent of <paramref name="referenceSize"/>).
        /// On other aspect ratios it then keeps its distance to that edge or corner.
        /// </summary>
        public static void AnchorTo(RectTransform rect, Vector2 anchor, Vector2 referenceSize)
        {
            Vector2 anchorShift = Vector2.Scale(anchor - new Vector2(0.5f, 0.5f), referenceSize);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition -= anchorShift;
        }

        /// <summary>
        /// An image using the sprite when available, otherwise a flat colour. Sprites
        /// with 9-slice borders are drawn sliced; <paramref name="borderScale"/> above 1
        /// makes the borders thinner on small elements.
        /// </summary>
        public static Image Image(string name, Transform parent, Vector2 position, Vector2 size,
            Sprite sprite, Color fallbackColor, float borderScale = 1f)
        {
            RectTransform rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : fallbackColor;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = borderScale;
            }
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size,
            string text, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = Rect(name, parent, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Gives a label a dark outline. With a shared outline material (an asset, used
        /// when building in the editor) the outline is saved with the scene; without
        /// one, TextMeshPro makes a per-label material at runtime.
        /// </summary>
        public static void Outline(TextMeshProUGUI label, Material outlineMaterial, float width)
        {
            if (outlineMaterial != null)
            {
                label.fontSharedMaterial = outlineMaterial;
            }
            else
            {
                label.outlineWidth = width;
                label.outlineColor = Palette.NavyDeep;
            }
        }

        /// <summary>The gold primary button (or the secondary navy one) with a text label. Listeners are added by GameScreen.</summary>
        public static Button Button(string name, Transform parent, Vector2 position, Vector2 size, string text,
            out TextMeshProUGUI label, bool primary = true)
        {
            Sprite sprite = SpriteLibrary.Get(primary ? "UI/ui_btn_primary" : "UI/ui_btn_secondary");
            Image background = Image(name, parent, position, size, sprite, primary ? Palette.Gold : Palette.NavyLight, 1.3f);
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            label = Text("Label", background.transform, new Vector2(0f, 3f), size,
                text, size.y * 0.36f, primary ? Palette.Ink : Palette.Ivory);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 6f;
            return button;
        }

        /// <summary>
        /// A round icon button: navy disc, silver rim, icon sprite. When the icon art is
        /// missing, <paramref name="fallbackLabel"/> shows a short word instead.
        /// </summary>
        public static Button IconButton(string name, Transform parent, Vector2 position, float size, Sprite circle,
            Sprite iconSprite, string fallbackText, out Image icon, out TextMeshProUGUI fallbackLabel)
        {
            Image rim = Image(name, parent, position, Vector2.one * size, circle, Palette.Silver);
            rim.color = Palette.Silver;
            rim.raycastTarget = true;
            Image disc = Image("Disc", rim.transform, Vector2.zero, Vector2.one * (size - 6f), circle, Palette.NavyDeep);
            disc.color = Palette.NavyDeep;
            icon = Image("Icon", rim.transform, Vector2.zero, Vector2.one * size * 0.55f, iconSprite, Color.clear);
            fallbackLabel = null;
            if (iconSprite == null)
            {
                fallbackLabel = Text("Fallback", rim.transform, Vector2.zero, Vector2.one * size, fallbackText, size * 0.3f, Palette.Ivory);
            }
            var button = rim.gameObject.AddComponent<Button>();
            button.targetGraphic = rim;
            return button;
        }

        /// <summary>A square picture shown inside a circle (used for avatars).</summary>
        public static Image CircularImage(string name, Transform parent, Vector2 position, float size, Sprite circle,
            Sprite sprite, Color fallbackColor)
        {
            Image mask = Image(name, parent, position, Vector2.one * size, circle, Color.white);
            mask.color = Color.white;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            return Image("Picture", mask.transform, Vector2.zero, Vector2.one * size, sprite, fallbackColor);
        }
    }
}
