using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zellige.App
{
    /// <summary>
    /// Moves between the two scenes (D3): one scene at a time, with a plain
    /// <see cref="SceneManager.LoadScene(string, LoadSceneMode)"/> in Single mode. There is no bootstrap
    /// scene and no additive loading. Taps made while a scene is loading are ignored, so a double
    /// tap cannot load twice.
    /// </summary>
    public static class SceneNavigator
    {
        public const string LobbyScene = "Lobby";
        public const string GameScene = "Game";

        /// <summary>True from the moment a load is requested until the new scene has loaded.</summary>
        public static bool IsLoading { get; private set; }

        /// <summary>
        /// Prepares a match of <paramref name="mode"/>: creates a fresh config and stores it for the
        /// Game scene. Only Me vs Computer succeeds; the locked online modes return false and create
        /// nothing (no config, no session, no network activity).
        /// </summary>
        public static bool TryPrepareMatch(MatchMode mode, out MatchConfig config)
        {
            if (!MatchConfig.TryCreate(mode, out config, out _)) return false;
            return MatchHandoff.Offer(config);
        }

        /// <summary>Lobby → Game. False (and nothing happens) for a locked mode or while loading.</summary>
        public static bool StartMatch(MatchMode mode)
        {
            if (IsLoading) return false;
            if (!TryPrepareMatch(mode, out _)) return false;
            return Load(GameScene);
        }

        /// <summary>Game → Lobby. Any unused match hand-off is dropped first.</summary>
        public static bool OpenLobby()
        {
            if (IsLoading) return false;
            MatchHandoff.Clear();
            return Load(LobbyScene);
        }

        private static bool Load(string sceneName)
        {
            IsLoading = true;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => IsLoading = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsLoading = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }
}
