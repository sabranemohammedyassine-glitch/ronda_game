using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Zellige.Game;
using Zellige.UI;

namespace Zellige.EditorTools
{
    /// <summary>
    /// Zellige > Build or Refresh Game UI: builds the match screen into Game.unity in
    /// Edit Mode so it can be edited in the Hierarchy and Inspector. Safe to run again:
    /// the old canvas is replaced (never duplicated), extra EventSystems are removed,
    /// and the GameController is linked to the new screen. Undo works (Ctrl+Z).
    /// </summary>
    public static class GameUiBuilder
    {
        public const string ScenePath = "Assets/Zellige/Scenes/Game.unity";
        private const string GeneratedFolder = "Assets/Zellige/Art/Generated";
        private const string CirclePath = GeneratedFolder + "/ui_circle.png";
        private const string OutlineMaterialPath = GeneratedFolder + "/Text Outline.mat";

        [MenuItem("Zellige/Build or Refresh Game UI")]
        private static void BuildFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Zellige", "Leave Play Mode before building the Game UI.", "OK");
                return;
            }
            if (!OpenGameScene()) return;

            bool exists = Object.FindObjectsOfType<GameScreen>(true).Length > 0;
            if (exists && !EditorUtility.DisplayDialog("Refresh Zellige Game UI",
                    "This replaces the existing '" + GameScreen.CanvasName + "' with a freshly built one.\n\n" +
                    "Changes you made to it by hand will be lost (Ctrl+Z restores the old one).",
                    "Rebuild", "Cancel"))
            {
                return;
            }
            Rebuild();
        }

        /// <summary>Builds the UI into the open Game scene and saves it. No dialogs (usable from scripts).</summary>
        public static GameScreen Rebuild()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
            {
                Debug.LogError("Zellige: open " + ScenePath + " in Edit Mode before building the Game UI.");
                return null;
            }

            Sprite circle = EnsureCircleSprite();
            Material outline = EnsureOutlineMaterial();

            foreach (GameScreen old in Object.FindObjectsOfType<GameScreen>(true))
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            GameScreen screen = GameScreen.BuildHierarchy(circle, outline);
            Undo.RegisterCreatedObjectUndo(screen.gameObject, "Build Zellige Game UI");
            EnsureSingleEventSystem();
            LinkController(screen);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = screen.gameObject;
            Debug.Log("Zellige: Game UI built and saved into " + ScenePath);
            return screen;
        }

        /// <summary>
        /// Adds the quit confirmation to the Game UI already saved in the scene, without
        /// rebuilding anything else (so hand edits and object IDs are kept). Does nothing if the
        /// scene already has one. New builds made with "Build or Refresh Game UI" include it.
        /// </summary>
        [MenuItem("Zellige/Add Quit Dialog to Game UI")]
        public static QuitDialog AddQuitDialog()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            {
                Debug.LogError("Zellige: open " + ScenePath + " in Edit Mode before adding the quit dialog.");
                return null;
            }
            QuitDialog[] existing = Object.FindObjectsOfType<QuitDialog>(true);
            if (existing.Length > 0)
            {
                Debug.Log("Zellige: the Game UI already has a quit dialog.");
                Selection.activeGameObject = existing[0].gameObject;
                return existing[0];
            }
            GameScreen screen = Object.FindObjectOfType<GameScreen>(true);
            if (screen == null)
            {
                Debug.LogError("Zellige: no Game UI in the scene. Run Zellige > Build or Refresh Game UI first.");
                return null;
            }

            QuitDialog dialog = QuitDialog.Build(screen.transform);
            Undo.RegisterCreatedObjectUndo(dialog.gameObject, "Add Quit Dialog");
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = dialog.gameObject;
            Debug.Log("Zellige: quit dialog added to " + ScenePath);
            return dialog;
        }

        /// <summary>
        /// L2: adds the result screen's Lobby button, the "Leave the match?" dialog and the Back button
        /// router to the Game UI already saved in the scene, without rebuilding anything else (hand
        /// edits and object IDs are kept). Running it again changes nothing. New builds include them.
        /// </summary>
        [MenuItem("Zellige/Add Lobby Navigation to Game UI")]
        public static bool AddLobbyNavigation()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            {
                Debug.LogError("Zellige: open " + ScenePath + " in Edit Mode before adding the Lobby navigation.");
                return false;
            }
            GameScreen screen = Object.FindObjectOfType<GameScreen>(true);
            if (screen == null)
            {
                Debug.LogError("Zellige: no Game UI in the scene. Run Zellige > Build or Refresh Game UI first.");
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Add Lobby Navigation");
            if (!screen.AddNavigationElements())
            {
                Debug.Log("Zellige: the Game UI already has the Lobby navigation.");
                return false;
            }
            EditorUtility.SetDirty(screen);
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Zellige: Lobby navigation added to " + ScenePath);
            return true;
        }

        private static bool OpenGameScene()
        {
            if (SceneManager.GetActiveScene().path == ScenePath) return true;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        internal static void EnsureSingleEventSystem()
        {
            EventSystem[] systems = Object.FindObjectsOfType<EventSystem>(true);
            if (systems.Length == 0)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                return;
            }
            for (int i = 1; i < systems.Length; i++)
            {
                foreach (BaseInputModule module in systems[i].GetComponents<BaseInputModule>()) Undo.DestroyObjectImmediate(module);
                Undo.DestroyObjectImmediate(systems[i]);
            }
        }

        private static void LinkController(GameScreen screen)
        {
            var controller = Object.FindObjectOfType<GameController>();
            if (controller == null)
            {
                var go = new GameObject("Game", typeof(GameController));
                Undo.RegisterCreatedObjectUndo(go, "Create GameController");
                controller = go.GetComponent<GameController>();
            }
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("screen").objectReferenceValue = screen;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>The white disc used for avatar masks and round buttons, saved as a sprite asset.</summary>
        internal static Sprite EnsureCircleSprite()
        {
            if (!File.Exists(CirclePath))
            {
                Directory.CreateDirectory(GeneratedFolder);
                Texture2D texture = SpriteLibrary.CreateDiscTexture(256);
                File.WriteAllBytes(CirclePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(CirclePath);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(CirclePath);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
        }

        /// <summary>A copy of the default TextMeshPro material with a dark outline, for call-out text.</summary>
        internal static Material EnsureOutlineMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (material != null) return material;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null) return null; // labels then fall back to per-label outlines at runtime

            Directory.CreateDirectory(GeneratedFolder);
            material = new Material(font.material) { name = "Text Outline" };
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Palette.NavyDeep);
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);
            return material;
        }
    }
}
