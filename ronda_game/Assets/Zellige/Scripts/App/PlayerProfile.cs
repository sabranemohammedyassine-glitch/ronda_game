using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Zellige.App
{
    /// <summary>Why a display name was refused. Each value maps to a <see cref="TextKeys"/> message.</summary>
    public enum NameError
    {
        None = 0,
        Empty,
        TooLong,
        InvalidCharacters
    }

    /// <summary>
    /// The avatars a profile may point to. Profiles store only the identifier; the identifier is
    /// the sprite name under Resources/Zellige/UI (the two avatars that exist today). Adding
    /// avatars later only extends this list.
    /// </summary>
    public static class AvatarCatalog
    {
        public const string DefaultId = "avatar_01";

        public static readonly IReadOnlyList<string> Ids = new[] { "avatar_01", "avatar_02" };

        public static bool IsKnown(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (string known in Ids)
            {
                if (known == id) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The local player profile (D-L-10): a display name and an avatar, kept on this device only.
    /// There is no account, no XP, level, currency, rank or achievement (D7).
    /// Fields are private and serialised by Unity's JsonUtility; changes go through methods that
    /// validate them, so a profile in memory is always valid.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxNameLength = 16;

        // 0 when absent from a file, so a document without a version is detected as invalid.
        [SerializeField] private int schemaVersion;
        [SerializeField] private string displayName;
        [SerializeField] private string avatarId;
        [SerializeField] private string createdUtc;
        [SerializeField] private string modifiedUtc;

        public int SchemaVersion => schemaVersion;
        public string DisplayName => displayName;
        public string AvatarId => avatarId;
        public string CreatedUtc => createdUtc;
        public string ModifiedUtc => modifiedUtc;

        /// <summary>The first-launch profile: the default name ("Player" in English) and the first avatar.</summary>
        public static PlayerProfile CreateDefault(DateTime nowUtc)
        {
            string now = Timestamps.Format(nowUtc);
            return new PlayerProfile
            {
                schemaVersion = CurrentSchemaVersion,
                displayName = DefaultName,
                avatarId = AvatarCatalog.DefaultId,
                createdUtc = now,
                modifiedUtc = now
            };
        }

        /// <summary>The default display name, from the text table so it follows the release language.</summary>
        public static string DefaultName
        {
            get
            {
                string text = TextCatalog.Get(TextKeys.ProfileDefaultName);
                return ValidateName(text, out string normalized) == NameError.None ? normalized : "Player";
            }
        }

        /// <summary>Renames the profile. An invalid name changes nothing and returns the reason.</summary>
        public NameError TrySetDisplayName(string name, DateTime nowUtc)
        {
            NameError error = ValidateName(name, out string normalized);
            if (error != NameError.None) return error;
            if (normalized != displayName)
            {
                displayName = normalized;
                modifiedUtc = Timestamps.Format(nowUtc);
            }
            return NameError.None;
        }

        /// <summary>Changes the avatar. An unknown identifier changes nothing and returns false.</summary>
        public bool TrySetAvatar(string id, DateTime nowUtc)
        {
            if (!AvatarCatalog.IsKnown(id)) return false;
            if (id != avatarId)
            {
                avatarId = id;
                modifiedUtc = Timestamps.Format(nowUtc);
            }
            return true;
        }

        /// <summary>
        /// Name rules (spec §4.3): surrounding spaces are trimmed and inner runs of spaces become one;
        /// 1–16 characters; letters, digits, spaces and "- _ ." only. Letters are limited to the
        /// Latin script because the current font (LiberationSans) cannot show others yet; widening
        /// this is an owner decision tied to the font (D-L-13 / R10).
        /// </summary>
        public static NameError ValidateName(string name, out string normalized)
        {
            normalized = null;
            if (name == null) return NameError.Empty;

            var builder = new StringBuilder(name.Length);
            bool pendingSpace = false;
            foreach (char c in name.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (c != ' ') return NameError.InvalidCharacters;   // tabs, line breaks, other spaces
                    pendingSpace = true;
                    continue;
                }
                if (!IsAllowedNameCharacter(c)) return NameError.InvalidCharacters;
                if (pendingSpace) builder.Append(' ');
                pendingSpace = false;
                builder.Append(c);
            }

            if (builder.Length == 0) return NameError.Empty;
            if (builder.Length > MaxNameLength) return NameError.TooLong;
            normalized = builder.ToString();
            return NameError.None;
        }

        public static string MessageKey(NameError error)
        {
            switch (error)
            {
                case NameError.Empty: return TextKeys.NameEmpty;
                case NameError.TooLong: return TextKeys.NameTooLong;
                case NameError.InvalidCharacters: return TextKeys.NameInvalidCharacters;
                default: return null;
            }
        }

        private static bool IsAllowedNameCharacter(char c)
        {
            if (c == '-' || c == '_' || c == '.') return true;
            if (c >= '0' && c <= '9') return true;
            // Basic Latin, Latin-1 Supplement and Latin Extended-A/B letters (é, ç, ñ, ...).
            return c <= 'ɏ' && char.IsLetter(c);
        }

        /// <summary>
        /// Checks a profile read from disk. Valid fields are kept; an invalid field is replaced
        /// (the caller keeps a backup of the original file). Returns false if anything was replaced.
        /// </summary>
        internal bool Repair(DateTime nowUtc)
        {
            bool intact = true;
            string now = Timestamps.Format(nowUtc);

            if (ValidateName(displayName, out string normalized) != NameError.None)
            {
                displayName = DefaultName;
                intact = false;
            }
            else if (normalized != displayName)
            {
                displayName = normalized;
                intact = false;
            }

            if (!AvatarCatalog.IsKnown(avatarId))
            {
                avatarId = AvatarCatalog.DefaultId;
                intact = false;
            }

            if (!Timestamps.TryParse(createdUtc, out DateTime created))
            {
                createdUtc = now;
                intact = false;
            }

            if (!Timestamps.TryParse(modifiedUtc, out DateTime modified) || modified < created)
            {
                modifiedUtc = createdUtc;
                intact = false;
            }

            schemaVersion = CurrentSchemaVersion;
            return intact;
        }
    }
}
