using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zellige.Lobby;

namespace Zellige.EditorTools
{
    /// <summary>
    /// Zellige > Build or Refresh Lobby UI: creates Lobby.unity if needed, builds the Lobby Home
    /// screen into it in Edit Mode, and makes the Lobby the startup scene (Build Settings:
    /// Lobby first, Game second; D3). Safe to run again: the old Lobby canvas is replaced (never
    /// duplicated), extra EventSystems are removed, and the Build Settings keep any other scenes.
    /// </summary>
    public static class LobbyUiBuilder
    {
        public const string ScenePath = "Assets/Zellige/Scenes/Lobby.unity";

        [MenuItem("Zellige/Build or Refresh Lobby UI")]
        private static void BuildFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Zellige", "Leave Play Mode before building the Lobby UI.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Rebuild();
        }

        /// <summary>Opens (or creates) Lobby.unity, builds its UI, saves it and updates Build Settings. No dialogs.</summary>
        public static LobbyScreen Rebuild()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Zellige: leave Play Mode before building the Lobby UI.");
                return null;
            }

            Scene scene = OpenOrCreateLobbyScene();
            Sprite circle = GameUiBuilder.EnsureCircleSprite();

            foreach (LobbyScreen old in Object.FindObjectsOfType<LobbyScreen>(true))
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }
            LobbyScreen screen = LobbyScreen.BuildHierarchy(circle, GameUiBuilder.EnsureOutlineMaterial());
            Undo.RegisterCreatedObjectUndo(screen.gameObject, "Build Zellige Lobby UI");
            GameUiBuilder.EnsureSingleEventSystem();
            EnsureCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            Selection.activeGameObject = screen.gameObject;
            Debug.Log("Zellige: Lobby UI built and saved into " + ScenePath);
            return screen;
        }

        private static Scene OpenOrCreateLobbyScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;
            if (File.Exists(ScenePath)) return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Scene created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(created, ScenePath);
            return created;
        }

        /// <summary>A camera that clears to the Lobby's navy (the UI is a screen-space overlay).</summary>
        private static void EnsureCamera()
        {
            if (Object.FindObjectOfType<Camera>() != null) return;
            var go = new GameObject("Main Camera", typeof(Camera));
            go.tag = "MainCamera";
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Zellige.UI.Palette.NavyDeep;
            camera.orthographic = true;
            go.transform.position = new Vector3(0f, 0f, -10f);
            Undo.RegisterCreatedObjectUndo(go, "Create Lobby Camera");
        }

        /// <summary>Build Settings: Lobby at index 0 (startup), Game at index 1, other scenes kept after them.</summary>
        public static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameUiBuilder.ScenePath, true)
            };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath && existing.path != GameUiBuilder.ScenePath) scenes.Add(existing);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
