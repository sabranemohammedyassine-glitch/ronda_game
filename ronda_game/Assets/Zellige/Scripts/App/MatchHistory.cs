using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zellige.App
{
    /// <summary>
    /// One completed match, seen from the local player. Abandoned matches never become a record (D6).
    /// "Player" and "opponent" scores are per side, so a future online 2 vs 2 record can store team
    /// totals in the same fields; no online record can be created in this version.
    /// </summary>
    [Serializable]
    public sealed class MatchRecord
    {
        public const int MaxIdLength = 64;
        public const int MaxOpponentNameLength = 32;

        [SerializeField] private string matchId;
        [SerializeField] private MatchMode mode;
        [SerializeField] private string opponentName;
        [SerializeField] private string startedUtc;
        [SerializeField] private string completedUtc;
        [SerializeField] private MatchOutcome outcome;
        [SerializeField] private int playerScore;
        [SerializeField] private int opponentScore;
        [SerializeField] private int decksPlayed;

        public string MatchId => matchId;
        public MatchMode Mode => mode;
        /// <summary>Shown in history ("You 41 – 27 Nadia").</summary>
        public string OpponentName => opponentName;
        public string StartedUtc => startedUtc;
        public string CompletedUtc => completedUtc;
        public MatchOutcome Outcome => outcome;
        public int PlayerScore => playerScore;
        public int OpponentScore => opponentScore;
        /// <summary>Number of decks dealt in the match (history detail, D-L-11).</summary>
        public int DecksPlayed => decksPlayed;

        // Used by JsonUtility when reading a file.
        private MatchRecord() { }

        public MatchRecord(string matchId, MatchMode mode, string opponentName, DateTime startedUtc,
                           DateTime completedUtc, MatchOutcome outcome, int playerScore, int opponentScore, int decksPlayed)
        {
            this.matchId = matchId;
            this.mode = mode;
            this.opponentName = opponentName;
            this.startedUtc = Timestamps.Format(startedUtc);
            this.completedUtc = Timestamps.Format(completedUtc);
            this.outcome = outcome;
            this.playerScore = playerScore;
            this.opponentScore = opponentScore;
            this.decksPlayed = decksPlayed;
        }

        /// <summary>True when every field holds a value the game could have produced.</summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(matchId) || matchId.Length > MaxIdLength) return false;
            if (!MatchModes.IsDefined(mode)) return false;
            if (outcome != MatchOutcome.Victory && outcome != MatchOutcome.Defeat) return false;
            if (opponentName == null || opponentName.Length > MaxOpponentNameLength) return false;
            if (playerScore < 0 || opponentScore < 0 || decksPlayed < 1) return false;
            if (!Timestamps.TryParse(startedUtc, out DateTime started)) return false;
            if (!Timestamps.TryParse(completedUtc, out DateTime completed)) return false;
            return completed >= started;
        }
    }

    /// <summary>
    /// Totals derived from completed records. <see cref="Add"/> is the only place a record is
    /// counted, used both for the stored lifetime totals and for statistics computed from the
    /// retained records, so the two can never use different rules.
    /// </summary>
    [Serializable]
    public sealed class MatchStatistics
    {
        [SerializeField] private int completed;
        [SerializeField] private int victories;
        [SerializeField] private int defeats;
        [SerializeField] private int abandoned;
        [SerializeField] private int bestWinningMargin;
        [SerializeField] private int highestScore;

        /// <summary>Completed matches (victories + defeats). Abandoned matches are not included (D6).</summary>
        public int Completed => completed;
        public int Victories => victories;
        public int Defeats => defeats;
        /// <summary>Matches left before the end. Kept apart: never a completed match, never a defeat (D6, D-L-11).</summary>
        public int Abandoned => abandoned;
        /// <summary>Largest (player − opponent) score difference among victories; 0 before the first victory.</summary>
        public int BestWinningMargin => bestWinningMargin;
        /// <summary>Highest final score of the local player in a completed match.</summary>
        public int HighestScore => highestScore;

        /// <summary>Victories ÷ completed matches, or null before the first completed match. Abandoned matches are excluded.</summary>
        public float? WinRate => completed == 0 ? (float?)null : (float)victories / completed;

        public static MatchStatistics FromRecords(IEnumerable<MatchRecord> records)
        {
            var stats = new MatchStatistics();
            foreach (MatchRecord record in records) stats.Add(record);
            return stats;
        }

        internal void Add(MatchRecord record)
        {
            completed++;
            if (record.Outcome == MatchOutcome.Victory)
            {
                victories++;
                bestWinningMargin = Math.Max(bestWinningMargin, record.PlayerScore - record.OpponentScore);
            }
            else
            {
                defeats++;
            }
            highestScore = Math.Max(highestScore, record.PlayerScore);
        }

        internal void AddAbandoned() => abandoned++;

        internal MatchStatistics Copy() => (MatchStatistics)MemberwiseClone();

        internal bool IsConsistentWith(MatchStatistics recent)
        {
            return completed >= 0 && victories >= 0 && defeats >= 0 && abandoned >= 0
                && bestWinningMargin >= 0 && highestScore >= 0
                && victories + defeats == completed
                && completed >= recent.completed
                && victories >= recent.victories
                && defeats >= recent.defeats
                && bestWinningMargin >= recent.bestWinningMargin
                && highestScore >= recent.highestScore;
        }

        internal static MatchStatistics Rebuilt(MatchStatistics recent, int abandonedCount)
        {
            MatchStatistics stats = recent.Copy();
            stats.abandoned = Math.Max(0, abandonedCount);
            return stats;
        }
    }

    /// <summary>The result of trying to add a completed record.</summary>
    public enum RecordResult
    {
        Recorded,
        /// <summary>A record with the same match ID is already kept; nothing changed.</summary>
        Duplicate,
        /// <summary>The record is not valid; nothing changed.</summary>
        Invalid
    }

    /// <summary>
    /// Completed-match history and statistics (D-L-11, D6). Keeps the latest
    /// <see cref="MaxRecords"/> completed records, newest first, plus lifetime totals that count
    /// every completed record once and abandoned matches separately.
    /// </summary>
    [Serializable]
    public sealed class MatchHistory
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxRecords = 20;

        // 0 when absent from a file, so a document without a version is detected as invalid.
        [SerializeField] private int schemaVersion;
        [SerializeField] private List<MatchRecord> records = new List<MatchRecord>();
        [SerializeField] private MatchStatistics lifetime = new MatchStatistics();

        public int SchemaVersion => schemaVersion;

        /// <summary>The latest completed matches, newest first (at most <see cref="MaxRecords"/>).</summary>
        public IReadOnlyList<MatchRecord> Records => records;

        /// <summary>Totals over every completed match since the history was created (or last reset).</summary>
        public MatchStatistics Lifetime => lifetime.Copy();

        /// <summary>Totals over the retained records only (the last 20 completed matches).</summary>
        public MatchStatistics Recent => MatchStatistics.FromRecords(records);

        public static MatchHistory CreateEmpty() => new MatchHistory { schemaVersion = CurrentSchemaVersion };

        public bool Contains(string matchId)
        {
            foreach (MatchRecord record in records)
            {
                if (record.MatchId == matchId) return true;
            }
            return false;
        }

        /// <summary>Adds a completed match once. Duplicates and invalid records change nothing.</summary>
        public RecordResult TryAdd(MatchRecord record)
        {
            if (record == null || !record.IsValid()) return RecordResult.Invalid;
            if (Contains(record.MatchId)) return RecordResult.Duplicate;

            records.Insert(0, record);
            if (records.Count > MaxRecords) records.RemoveRange(MaxRecords, records.Count - MaxRecords);
            lifetime.Add(record);
            return RecordResult.Recorded;
        }

        /// <summary>Counts a match the player left before the end. It is not a completed match.</summary>
        public void AddAbandoned() => lifetime.AddAbandoned();

        /// <summary>
        /// Checks a history read from disk: drops invalid or repeated records, trims to the limit and,
        /// if the stored totals contradict the records, rebuilds them from the records. False if anything changed.
        /// </summary>
        internal bool Repair()
        {
            bool intact = true;
            if (records == null)
            {
                records = new List<MatchRecord>();
                intact = false;
            }

            var seen = new HashSet<string>();
            for (int i = 0; i < records.Count; i++)
            {
                MatchRecord record = records[i];
                if (record == null || !record.IsValid() || !seen.Add(record.MatchId))
                {
                    records.RemoveAt(i--);
                    intact = false;
                }
            }

            if (records.Count > MaxRecords)
            {
                records.RemoveRange(MaxRecords, records.Count - MaxRecords);
                intact = false;
            }

            MatchStatistics recent = Recent;
            if (lifetime == null || !lifetime.IsConsistentWith(recent))
            {
                lifetime = MatchStatistics.Rebuilt(recent, lifetime?.Abandoned ?? 0);
                intact = false;
            }

            schemaVersion = CurrentSchemaVersion;
            return intact;
        }
    }
}
