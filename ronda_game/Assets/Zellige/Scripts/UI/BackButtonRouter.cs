using System.Collections.Generic;
using UnityEngine;
using Zellige.App;

namespace Zellige.UI
{
    /// <summary>Something that decides what the Back button does in the current scene.</summary>
    public interface IBackHandler
    {
        void HandleBack();
    }

    /// <summary>
    /// The only listener for Android Back in a scene (Unity's legacy input reports it as Escape;
    /// Escape on desktop). It forwards each press to the scene's <see cref="IBackHandler"/>
    /// (the Lobby screen, or the match controller), which knows which dialog to open or close.
    /// Dialogs never listen for the key themselves, so one press can never be handled twice.
    ///
    /// If two routers are ever enabled at once (for example for one frame during a scene change),
    /// only the most recently enabled one responds. Presses are ignored while a scene is loading.
    /// </summary>
    public sealed class BackButtonRouter : MonoBehaviour
    {
        private static readonly List<BackButtonRouter> EnabledRouters = new List<BackButtonRouter>();

        private IBackHandler handler;

        /// <summary>The router that currently responds to Back, or null.</summary>
        public static BackButtonRouter Current => EnabledRouters.Count == 0 ? null : EnabledRouters[EnabledRouters.Count - 1];

        /// <summary>How many routers are enabled right now (1 in a correctly built scene).</summary>
        public static int EnabledCount => EnabledRouters.Count;

        public IBackHandler Handler => handler;

        public void SetHandler(IBackHandler backHandler) => handler = backHandler;

        private void OnEnable()
        {
            EnabledRouters.Remove(this);
            EnabledRouters.Add(this);
            if (EnabledRouters.Count > 1)
            {
                Debug.LogWarning("Zellige: more than one Back button router is enabled; only the newest one responds.", this);
            }
        }

        private void OnDisable() => EnabledRouters.Remove(this);

        private void Update()
        {
            if (Current == this && Input.GetKeyDown(KeyCode.Escape)) Press();
        }

        /// <summary>
        /// One Back press, exactly as the key handler performs it. Returns false when this router is
        /// not the current one, has no handler, or a scene is loading.
        /// </summary>
        public bool Press()
        {
            if (Current != this || handler == null || SceneNavigator.IsLoading) return false;
            handler.HandleBack();
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => EnabledRouters.Clear();
    }
}
