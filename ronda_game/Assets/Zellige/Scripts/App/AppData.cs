using System;
using System.IO;
using UnityEngine;

namespace Zellige.App
{
    /// <summary>
    /// The application's single <see cref="LocalDataStore"/>, shared by the Lobby and Game scenes
    /// (D3). It is created on first use, in a "save" folder under
    /// <see cref="Application.persistentDataPath"/>, and survives scene loads because it is plain
    /// static memory (no GameObject, no DontDestroyOnLoad).
    ///
    /// Nothing calls this in L1: the existing game is unaffected. Tests never use it, so they
    /// cannot touch the player's real data; they create a <see cref="LocalDataStore"/> on a
    /// temporary folder instead.
    /// </summary>
    public static class AppData
    {
        public const string FolderName = "save";

        private static LocalDataStore store;

        /// <summary>The loaded store (loading it on first access).</summary>
        public static LocalDataStore Store
        {
            get
            {
                if (store == null)
                {
                    var created = new LocalDataStore(Path.Combine(Application.persistentDataPath, FolderName));
                    created.LoadAll(DateTime.UtcNow);
                    store = created;
                }
                return store;
            }
        }

        /// <summary>
        /// Replaces the store for the rest of this play session. Used by Play Mode tests so they write
        /// to a temporary folder, never to the player's real data. Pass null to go back to the default.
        /// </summary>
        public static void UseStore(LocalDataStore replacement) => store = replacement;

        // Cleared when play mode starts, so Editor sessions never keep stale data (risk R5).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => store = null;
    }
}
