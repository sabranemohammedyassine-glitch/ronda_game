using System;
using UnityEngine;

namespace Zellige.App
{
    /// <summary>
    /// How the player plays a card from the hand. Values are written to JSON; never renumber.
    /// </summary>
    public enum CardPlayMode
    {
        /// <summary>Tap a card to lift it and preview its captures, tap it again to play. The current MVP behaviour.</summary>
        TwoStep = 1,
        /// <summary>A single tap plays a legal card. Stored only in L1; the match does not read it yet.</summary>
        OneTap = 2
    }

    /// <summary>
    /// Device settings that survive restarts (D-L-13). Defaults equal today's behaviour:
    /// sound on (the match's AudioSource starts unmuted) and two-step card play.
    /// L1 stores these values only; nothing in the match reads them until L2/L3 wires them.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        public const int CurrentSchemaVersion = 1;

        // 0 when absent from a file, so a document without a version is detected as invalid.
        [SerializeField] private int schemaVersion;
        [SerializeField] private bool soundEnabled = true;
        [SerializeField] private CardPlayMode cardPlayMode = CardPlayMode.TwoStep;

        public int SchemaVersion => schemaVersion;

        public bool SoundEnabled
        {
            get => soundEnabled;
            set => soundEnabled = value;
        }

        public CardPlayMode CardPlayMode => cardPlayMode;

        public static GameSettings CreateDefault() => new GameSettings { schemaVersion = CurrentSchemaVersion };

        public static bool IsDefined(CardPlayMode mode) => mode == CardPlayMode.TwoStep || mode == CardPlayMode.OneTap;

        /// <summary>Sets the card play mode. An undefined value changes nothing and returns false.</summary>
        public bool TrySetCardPlayMode(CardPlayMode mode)
        {
            if (!IsDefined(mode)) return false;
            cardPlayMode = mode;
            return true;
        }

        /// <summary>Checks settings read from disk; an invalid value goes back to its default. False if anything changed.</summary>
        internal bool Repair()
        {
            bool intact = true;
            if (!IsDefined(cardPlayMode))
            {
                cardPlayMode = CardPlayMode.TwoStep;
                intact = false;
            }
            schemaVersion = CurrentSchemaVersion;
            return intact;
        }
    }
}
