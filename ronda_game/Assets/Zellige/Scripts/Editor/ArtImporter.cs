using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zellige.EditorTools
{
    /// <summary>
    /// Brings the approved artwork from UI-Design/Exported (the source of truth, kept
    /// outside the Unity project) into Assets/Zellige/Resources/Zellige, where the
    /// game loads it by name. Run it again after the art is regenerated.
    /// </summary>
    public static class ArtImporter
    {
        private const string ResourcesRoot = "Assets/Zellige/Resources/Zellige";

        [MenuItem("Zellige/Import Art from UI-Design")]
        public static void ImportArt()
        {
            string exported = Path.GetFullPath(Path.Combine(Application.dataPath, "../../UI-Design/Exported"));
            if (!Directory.Exists(exported))
            {
                Debug.LogError("Zellige: art folder not found: " + exported);
                return;
            }

            int copied = 0;
            var files = new List<string>(Directory.GetFiles(exported, "*.png"));
            string cardsFolder = Path.Combine(exported, "cards");
            // Files in Exported/cards are copied last so they win over older root copies.
            if (Directory.Exists(cardsFolder)) files.AddRange(Directory.GetFiles(cardsFolder, "*.png"));

            foreach (string source in files)
            {
                string name = Path.GetFileName(source);
                if (name == "card_face_blank.png" || name.StartsWith("suit_")) continue; // composition parts, not used at runtime

                string folder = name.StartsWith("card_") ? "Cards" : "UI";
                string target = Path.Combine(ResourcesRoot, folder, name);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target, true);
                copied++;
            }

            AssetDatabase.Refresh();
            Debug.Log("Zellige: imported " + copied + " art files from " + exported);
        }
    }

    /// <summary>Applies the UI sprite import settings to everything under Resources/Zellige.</summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        // 9-slice borders in pixels: left, bottom, right, top (from UI-Design/Specifications/02_asset_inventory.md).
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "ui_btn_primary", new Vector4(44, 48, 44, 40) },
            { "ui_btn_secondary", new Vector4(44, 48, 44, 40) },
            { "ui_panel", new Vector4(48, 48, 48, 48) },
            { "ui_score_capsule", new Vector4(44, 0, 44, 0) },
            { "ui_dialog", new Vector4(72, 72, 72, 72) },
            { "ui_callout", new Vector4(110, 0, 110, 0) },
        };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Zellige/Resources/Zellige/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Cards are drawn well below their 360x540 size; mipmaps keep them crisp while they scale.
            bool isCard = assetPath.Contains("/Cards/");
            importer.mipmapEnabled = isCard;
            importer.mipMapBias = isCard ? -0.5f : 0f;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            string name = Path.GetFileNameWithoutExtension(assetPath);
            importer.spriteBorder = Borders.TryGetValue(name, out Vector4 border) ? border : Vector4.zero;
        }
    }
}
