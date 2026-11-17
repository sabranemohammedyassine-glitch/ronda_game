using System;
using System.Collections.Generic;
using Zellige.App;
using Zellige.Core;

namespace Zellige.Game
{
    /// <summary>
    /// Connects the running match to the L1 local data (no Unity objects, so it is tested directly).
    ///
    /// <list type="bullet">
    /// <item><see cref="StartNew"/> takes the Lobby's hand-off once (or a fresh Me vs Computer config
    /// when there is none: <c>Game.unity</c> opened directly, or Play Again) and begins a new session.</item>
    /// <item><see cref="Observe"/> sees every batch of engine events as soon as the engine returns it,
    /// before any animation. The engine's <see cref="MatchEndedEvent"/> (winner and final scores) and the
    /// last <see cref="DeckStartedEvent"/> round become the completed record, exactly once.</item>
    /// <item><see cref="Leave"/> is the confirmed "Leave the match?" (D5): the session becomes Abandoned,
    /// which is never a completed match (D6).</item>
    /// </list>
    ///
    /// Exactly-once is guaranteed by <see cref="MatchSession"/> (final states) and by
    /// <see cref="LocalDataStore.CompleteMatch"/> / <see cref="LocalDataStore.AbandonMatch"/>, not by UI state.
    /// </summary>
    public sealed class MatchLifecycle
    {
        private readonly LocalDataStore store;
        private readonly Func<DateTime> utcNow;
        private int decksStarted;

        public MatchLifecycle(LocalDataStore store, Func<DateTime> utcNow = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>The session of the current match; null before the first match.</summary>
        public MatchSession Current { get; private set; }

        public bool IsInProgress => Current != null && Current.State == MatchState.InProgress;

        /// <summary>The result of the last completion attempt (for diagnostics and tests).</summary>
        public RecordResult? LastRecordResult { get; private set; }

        /// <summary>False if the last completed or abandoned match could not be written to disk.</summary>
        public bool LastSaveSucceeded { get; private set; } = true;

        /// <summary>
        /// Begins a new match with a fresh session. A previous session that is still in progress is
        /// left as it is and nothing is recorded for it: what an unfinished match replaced by "New game"
        /// should count as is an open owner decision (L2 report, open question 2), so no result is invented.
        /// </summary>
        public MatchSession StartNew()
        {
            MatchConfig config = MatchHandoff.TakePendingOrDefault();
            Current = new MatchSession(config);
            Current.Begin(utcNow());
            decksStarted = 0;
            LastRecordResult = null;
            return Current;
        }

        /// <summary>Reads one batch of engine events; records the match when the engine reports its end.</summary>
        public void Observe(IReadOnlyList<GameEvent> events)
        {
            if (events == null) return;
            foreach (GameEvent gameEvent in events)
            {
                switch (gameEvent)
                {
                    case DeckStartedEvent deck:
                        decksStarted = Math.Max(decksStarted, deck.DeckRound);
                        break;
                    case MatchEndedEvent ended:
                        Complete(ended);
                        break;
                }
            }
        }

        /// <summary>
        /// The player confirmed "Leave the match?". True if the match was in progress and is now
        /// abandoned; false (nothing changes) if it had already ended or was already left.
        /// </summary>
        public bool Leave()
        {
            if (!IsInProgress) return false;
            bool left = store.AbandonMatch(Current, out bool saved);
            LastSaveSucceeded = saved;
            return left;
        }

        private void Complete(MatchEndedEvent ended)
        {
            if (!IsInProgress) return;   // already recorded, or left: never twice, never both
            LastRecordResult = store.CompleteMatch(Current, ended.Winner, ended.Scores, Math.Max(1, decksStarted),
                                                   utcNow(), out bool saved);
            LastSaveSucceeded = saved;
        }
    }
}
