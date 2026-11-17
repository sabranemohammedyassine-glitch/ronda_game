using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zellige.App
{
    /// <summary>What happened when a saved document was loaded.</summary>
    public enum LoadStatus
    {
        /// <summary>Not loaded yet.</summary>
        NotLoaded,
        /// <summary>A valid file was read unchanged.</summary>
        Loaded,
        /// <summary>No file existed (first launch); defaults were created and saved.</summary>
        Created,
        /// <summary>The file was readable but some values were invalid; they were replaced with defaults,
        /// valid values were kept, and the original file was copied to "name.corrupt.json".</summary>
        Repaired,
        /// <summary>The file could not be parsed; it was copied to "name.corrupt.json" and defaults were used and saved.</summary>
        Reset,
        /// <summary>The file could not be read, or was written by a newer version of the game. Defaults are used
        /// in memory and saving is disabled for this session, so the file on disk is never overwritten.</summary>
        Protected
    }

    /// <summary>
    /// The device's saved data: profile, settings and match history, one JSON file each, in one folder.
    /// Loading never throws and never replaces a valid file with defaults. Every save is atomic
    /// (<see cref="AtomicFileStore"/>). This class has no static state; tests give it a temporary folder.
    /// </summary>
    public sealed class LocalDataStore
    {
        public const string ProfileName = "profile";
        public const string SettingsName = "settings";
        public const string HistoryName = "match_history";
        public const string CorruptSuffix = ".corrupt";

        private readonly AtomicFileStore files;
        private readonly HashSet<string> protectedNames = new HashSet<string>();

        public PlayerProfile Profile { get; private set; }
        public GameSettings Settings { get; private set; }
        public MatchHistory History { get; private set; }

        public LoadStatus ProfileStatus { get; private set; }
        public LoadStatus SettingsStatus { get; private set; }
        public LoadStatus HistoryStatus { get; private set; }

        public string Directory => files.Directory;

        public LocalDataStore(string directory)
        {
            files = new AtomicFileStore(directory);
        }

        /// <summary>Loads (or creates) the three documents. Safe to call on every launch.</summary>
        public void LoadAll(DateTime nowUtc)
        {
            Profile = Load(ProfileName, () => PlayerProfile.CreateDefault(nowUtc), p => p.SchemaVersion,
                           PlayerProfile.CurrentSchemaVersion, p => p.Repair(nowUtc), out LoadStatus profileStatus);
            ProfileStatus = profileStatus;

            Settings = Load(SettingsName, GameSettings.CreateDefault, s => s.SchemaVersion,
                            GameSettings.CurrentSchemaVersion, s => s.Repair(), out LoadStatus settingsStatus);
            SettingsStatus = settingsStatus;

            History = Load(HistoryName, MatchHistory.CreateEmpty, h => h.SchemaVersion,
                           MatchHistory.CurrentSchemaVersion, h => h.Repair(), out LoadStatus historyStatus);
            HistoryStatus = historyStatus;
        }

        public bool SaveProfile() => Save(ProfileName, Profile);
        public bool SaveSettings() => Save(SettingsName, Settings);
        public bool SaveHistory() => Save(HistoryName, History);

        /// <summary>True while a document may be written (false after <see cref="LoadStatus.Protected"/>).</summary>
        public bool CanSave(string name) => !protectedNames.Contains(name);

        // ---------- Match results (the only way a match reaches the history) ----------

        /// <summary>
        /// Ends a match normally: moves the session to Completed and adds its record to the history
        /// once, then saves. A second call for the same session, or a call after it was abandoned,
        /// returns <see cref="RecordResult.Duplicate"/> and changes nothing.
        /// </summary>
        public RecordResult CompleteMatch(MatchSession session, int winnerSeat, IReadOnlyList<int> scores,
                                          int decksPlayed, DateTime nowUtc, out bool saved)
        {
            saved = false;
            RequireLoaded();
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (!session.TryComplete(winnerSeat, scores, decksPlayed, nowUtc, out MatchRecord record))
            {
                return RecordResult.Duplicate;
            }
            RecordResult result = History.TryAdd(record);
            if (result == RecordResult.Recorded) saved = SaveHistory();
            return result;
        }

        /// <summary>
        /// The player left a running match (D5/D6): moves the session to Abandoned, counts one
        /// abandoned match (not a completed one, not a defeat) and saves. False, with no change, if
        /// the session was not in progress, so a match can never be abandoned twice or after it ended.
        /// </summary>
        public bool AbandonMatch(MatchSession session, out bool saved)
        {
            saved = false;
            RequireLoaded();
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (!session.TryAbandon()) return false;
            History.AddAbandoned();
            saved = SaveHistory();
            return true;
        }

        // ---------- Internals ----------

        private T Load<T>(string name, Func<T> createDefault, Func<T, int> versionOf, int currentVersion,
                          Func<T, bool> repair, out LoadStatus status) where T : class
        {
            protectedNames.Remove(name);
            ReadStatus read = files.TryRead(name, out string text);

            if (read == ReadStatus.Missing)
            {
                T created = createDefault();
                files.TryWrite(name, ToJson(created));   // a failed write is retried on the next save
                status = LoadStatus.Created;
                return created;
            }

            if (read == ReadStatus.Unreadable)
            {
                protectedNames.Add(name);
                status = LoadStatus.Protected;
                return createDefault();
            }

            T value = TryParse<T>(text);
            int version = value == null ? 0 : versionOf(value);

            if (version > currentVersion)
            {
                // Written by a newer game version (e.g. after a downgrade). Keep the file untouched.
                protectedNames.Add(name);
                status = LoadStatus.Protected;
                return createDefault();
            }

            if (value == null || version < 1)
            {
                // Unparseable, or not one of our documents. Version 1 is the first schema, so there is
                // nothing older to migrate yet; a future version 2 would migrate version 1 here.
                files.TryWrite(name + CorruptSuffix, text);
                T fresh = createDefault();
                files.TryWrite(name, ToJson(fresh));
                status = LoadStatus.Reset;
                return fresh;
            }

            if (!repair(value))
            {
                files.TryWrite(name + CorruptSuffix, text);
                files.TryWrite(name, ToJson(value));
                status = LoadStatus.Repaired;
                return value;
            }

            status = LoadStatus.Loaded;
            return value;
        }

        private bool Save<T>(string name, T value) where T : class
        {
            RequireLoaded();
            if (!CanSave(name)) return false;
            return files.TryWrite(name, ToJson(value));
        }

        private static T TryParse<T>(string text) where T : class
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try
            {
                return JsonUtility.FromJson<T>(text);
            }
            catch (Exception)
            {
                // JsonUtility reports malformed text with ArgumentException; any parse failure means "corrupt".
                return null;
            }
        }

        private static string ToJson(object value) => JsonUtility.ToJson(value, true);

        private void RequireLoaded()
        {
            if (Profile == null || Settings == null || History == null)
            {
                throw new InvalidOperationException("Call LoadAll before saving or recording matches.");
            }
        }
    }
}
