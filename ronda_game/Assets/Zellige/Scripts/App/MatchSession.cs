using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zellige.App
{
    /// <summary>
    /// What the Lobby asks the Game scene to play (D-L-05). Immutable. It carries no rule options:
    /// Me vs Computer always uses the default <c>RulesConfig</c> (D-L-20). Every config gets a new
    /// match ID, so a new match can never reuse an old match's identity or result.
    /// </summary>
    public sealed class MatchConfig
    {
        /// <summary>The computer opponent's name, as shown on the match screen today.</summary>
        public const string ComputerOpponentName = "Nadia";

        /// <summary>The local human's seat in the two-seat engine (the match screen's HumanSeat).</summary>
        public const int LocalSeat = 0;

        public MatchMode Mode { get; }
        public string MatchId { get; }
        public string OpponentName { get; }

        private MatchConfig(MatchMode mode, string matchId, string opponentName)
        {
            Mode = mode;
            MatchId = matchId;
            OpponentName = opponentName;
        }

        /// <summary>A Me vs Computer match against Nadia. <paramref name="matchId"/> is for tests; normally a new GUID.</summary>
        public static MatchConfig VsComputer(string matchId = null)
        {
            return new MatchConfig(MatchMode.VsComputer, matchId ?? NewMatchId(), ComputerOpponentName);
        }

        /// <summary>
        /// Builds a config for a mode. Only playable modes succeed; the online modes return false with
        /// a text key to show (D1: they are visible but locked). Nothing is contacted over a network.
        /// </summary>
        public static bool TryCreate(MatchMode mode, out MatchConfig config, out string reasonKey)
        {
            config = null;
            reasonKey = null;
            if (!MatchModes.IsDefined(mode))
            {
                reasonKey = TextKeys.ModeUnknown;
                return false;
            }
            if (!MatchModes.IsPlayable(mode))
            {
                reasonKey = TextKeys.ModeNotAvailable;
                return false;
            }
            config = VsComputer();
            return true;
        }

        private static string NewMatchId() => Guid.NewGuid().ToString("N");
    }

    /// <summary>The lifecycle of one match (D6).</summary>
    public enum MatchState
    {
        /// <summary>Configured but not started yet.</summary>
        Created,
        /// <summary>Being played.</summary>
        InProgress,
        /// <summary>Finished normally; a completed record exists. Final.</summary>
        Completed,
        /// <summary>Left before the end. No completed record; never resumed (D6). Final.</summary>
        Abandoned
    }

    /// <summary>
    /// Tracks one match from start to end: Created → InProgress → Completed or Abandoned.
    /// Both end states are final, so a match produces at most one completed record and can never be
    /// both completed and abandoned. A new match always needs a new session (and a new config).
    /// </summary>
    public sealed class MatchSession
    {
        public MatchConfig Config { get; }
        public MatchState State { get; private set; } = MatchState.Created;
        public DateTime StartedUtc { get; private set; }

        /// <summary>The completed record, once <see cref="State"/> is Completed; otherwise null.</summary>
        public MatchRecord Result { get; private set; }

        public MatchSession(MatchConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (!MatchModes.IsPlayable(config.Mode))
            {
                throw new InvalidOperationException("Mode " + config.Mode + " is not playable in this version.");
            }
        }

        public bool IsFinished => State == MatchState.Completed || State == MatchState.Abandoned;

        /// <summary>Created → InProgress. False (and no change) in any other state.</summary>
        public bool Begin(DateTime nowUtc)
        {
            if (State != MatchState.Created) return false;
            StartedUtc = nowUtc;
            State = MatchState.InProgress;
            return true;
        }

        /// <summary>
        /// InProgress → Completed, building the record from the engine's final result
        /// (<c>MatchEndedEvent.Winner</c> and <c>Scores</c>, and the last deck round).
        /// Returns false (and no change) if the match is not in progress, e.g. a second call.
        /// </summary>
        public bool TryComplete(int winnerSeat, IReadOnlyList<int> scores, int decksPlayed, DateTime nowUtc, out MatchRecord record)
        {
            record = null;
            if (State != MatchState.InProgress) return false;

            if (scores == null || scores.Count != 2) throw new ArgumentException("Expected the scores of the two seats.", nameof(scores));
            if (winnerSeat != 0 && winnerSeat != 1) throw new ArgumentOutOfRangeException(nameof(winnerSeat));
            if (scores[0] < 0 || scores[1] < 0) throw new ArgumentException("Scores cannot be negative.", nameof(scores));
            if (decksPlayed < 1) throw new ArgumentOutOfRangeException(nameof(decksPlayed));
            if (nowUtc < StartedUtc) nowUtc = StartedUtc;   // device clock moved back during the match

            int local = MatchConfig.LocalSeat;
            int other = 1 - local;
            record = new MatchRecord(Config.MatchId, Config.Mode, Config.OpponentName, StartedUtc, nowUtc,
                                     winnerSeat == local ? MatchOutcome.Victory : MatchOutcome.Defeat,
                                     scores[local], scores[other], decksPlayed);
            Result = record;
            State = MatchState.Completed;
            return true;
        }

        /// <summary>
        /// InProgress → Abandoned (the player left; D5/D6). A match that never started, or one that
        /// already ended, cannot be abandoned: returns false and nothing changes.
        /// </summary>
        public bool TryAbandon()
        {
            if (State != MatchState.InProgress) return false;
            State = MatchState.Abandoned;
            return true;
        }
    }

    /// <summary>
    /// Hands a <see cref="MatchConfig"/> from the Lobby scene to the Game scene (D3). It lives in
    /// static memory, which survives a single scene load; no GameObject or DontDestroyOnLoad is
    /// involved. A pending config is consumed once, so it can never start a second match.
    /// </summary>
    public static class MatchHandoff
    {
        private static MatchConfig pending;

        public static bool HasPending => pending != null;

        /// <summary>Stores the config the next Game scene will take. Only playable modes are accepted.</summary>
        public static bool Offer(MatchConfig config)
        {
            if (config == null || !MatchModes.IsPlayable(config.Mode)) return false;
            pending = config;
            return true;
        }

        /// <summary>
        /// Returns the pending config and clears it. Without one (for example <c>Game.unity</c> opened
        /// directly in the Editor) it returns a fresh Me vs Computer config: today's behaviour.
        /// </summary>
        public static MatchConfig TakePendingOrDefault()
        {
            MatchConfig config = pending ?? MatchConfig.VsComputer();
            pending = null;
            return config;
        }

        public static void Clear() => pending = null;

        // Static state is cleared when play mode starts, so nothing leaks between Editor
        // play sessions even when "Enter Play Mode Options" skips the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => pending = null;
    }
}
