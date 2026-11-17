using NUnit.Framework;
using Zellige.App;

namespace Zellige.Tests.LocalData
{
    public sealed class MatchHistoryTests : LocalDataTestBase
    {
        [Test]
        public void CompletedVictory_IsRecorded_FromTheLocalPlayersSide()
        {
            LocalDataStore store = LoadStore();
            var session = new MatchSession(MatchConfig.VsComputer("m1"));

            Assert.AreEqual(RecordResult.Recorded, Finish(store, session, winner: 0, score0: 41, score1: 27, decks: 4, minutes: 12));

            Assert.AreEqual(1, store.History.Records.Count);
            MatchRecord record = store.History.Records[0];
            Assert.AreEqual("m1", record.MatchId);
            Assert.AreEqual(MatchMode.VsComputer, record.Mode);
            Assert.AreEqual("Nadia", record.OpponentName);
            Assert.AreEqual(MatchOutcome.Victory, record.Outcome);
            Assert.AreEqual(41, record.PlayerScore);
            Assert.AreEqual(27, record.OpponentScore);
            Assert.AreEqual(4, record.DecksPlayed);
            Assert.AreEqual(Timestamps.Format(T0), record.StartedUtc);
            Assert.AreEqual(Timestamps.Format(T0.AddMinutes(12)), record.CompletedUtc);
            Assert.AreSame(record, session.Result);
        }

        [Test]
        public void CompletedDefeat_SwapsTheScoresIntoPlayerAndOpponent()
        {
            LocalDataStore store = LoadStore();

            Finish(store, new MatchSession(MatchConfig.VsComputer("m1")), winner: 1, score0: 12, score1: 43);

            MatchRecord record = store.History.Records[0];
            Assert.AreEqual(MatchOutcome.Defeat, record.Outcome);
            Assert.AreEqual(12, record.PlayerScore);
            Assert.AreEqual(43, record.OpponentScore);
        }

        [Test]
        public void History_KeepsOnlyTheLatest20_NewestFirst_ButCountsEveryMatch()
        {
            LocalDataStore store = LoadStore();
            for (int i = 0; i < 25; i++)
            {
                Finish(store, new MatchSession(MatchConfig.VsComputer("m" + i)), winner: i % 2, score0: 41, score1: 20);
            }

            Assert.AreEqual(MatchHistory.MaxRecords, store.History.Records.Count);
            Assert.AreEqual("m24", store.History.Records[0].MatchId, "newest first");
            Assert.AreEqual("m5", store.History.Records[19].MatchId, "the 5 oldest were dropped");
            Assert.AreEqual(25, store.History.Lifetime.Completed, "lifetime totals count all completed matches");
            Assert.AreEqual(20, store.History.Recent.Completed);

            LocalDataStore reloaded = LoadStore();
            Assert.AreEqual(LoadStatus.Loaded, reloaded.HistoryStatus);
            Assert.AreEqual(20, reloaded.History.Records.Count);
            Assert.AreEqual(25, reloaded.History.Lifetime.Completed);
        }

        [Test]
        public void Statistics_AreDerivedFromTheCompletedRecords()
        {
            var history = MatchHistory.CreateEmpty();
            history.TryAdd(Record("a", MatchOutcome.Victory, 41, 30));   // margin 11
            history.TryAdd(Record("b", MatchOutcome.Defeat, 25, 44));
            history.TryAdd(Record("c", MatchOutcome.Victory, 45, 20));   // margin 25, highest score 45
            history.TryAdd(Record("d", MatchOutcome.Victory, 42, 40));   // margin 2

            MatchStatistics recent = history.Recent;
            Assert.AreEqual(4, recent.Completed);
            Assert.AreEqual(3, recent.Victories);
            Assert.AreEqual(1, recent.Defeats);
            Assert.AreEqual(0.75f, recent.WinRate.Value, 1e-6f);
            Assert.AreEqual(25, recent.BestWinningMargin);
            Assert.AreEqual(45, recent.HighestScore);

            MatchStatistics lifetime = history.Lifetime;   // below the 20-record cap they must agree
            Assert.AreEqual(recent.Completed, lifetime.Completed);
            Assert.AreEqual(recent.Victories, lifetime.Victories);
            Assert.AreEqual(recent.Defeats, lifetime.Defeats);
            Assert.AreEqual(recent.BestWinningMargin, lifetime.BestWinningMargin);
            Assert.AreEqual(recent.HighestScore, lifetime.HighestScore);
        }

        [Test]
        public void WinRate_IsUndefined_BeforeTheFirstCompletedMatch()
        {
            Assert.IsNull(MatchHistory.CreateEmpty().Lifetime.WinRate);
        }

        [Test]
        public void AbandonedMatch_IsNotACompletedMatch_NorADefeat()
        {
            LocalDataStore store = LoadStore();
            Finish(store, new MatchSession(MatchConfig.VsComputer("won")), winner: 0, score0: 41, score1: 10);
            var left = new MatchSession(MatchConfig.VsComputer("left"));
            left.Begin(T0);

            Assert.IsTrue(store.AbandonMatch(left, out bool saved));
            Assert.IsTrue(saved);

            MatchStatistics stats = store.History.Lifetime;
            Assert.AreEqual(1, store.History.Records.Count, "no record for the abandoned match");
            Assert.IsFalse(store.History.Contains("left"));
            Assert.AreEqual(1, stats.Completed);
            Assert.AreEqual(0, stats.Defeats);
            Assert.AreEqual(1, stats.Abandoned);
            Assert.AreEqual(1f, stats.WinRate.Value, "abandoned matches do not lower the win rate");
            Assert.AreEqual(1, LoadStore().History.Lifetime.Abandoned, "the abandoned count is saved");
        }

        [Test]
        public void CompletingTheSameMatchTwice_RecordsItOnce()
        {
            LocalDataStore store = LoadStore();
            var session = new MatchSession(MatchConfig.VsComputer("m1"));

            Assert.AreEqual(RecordResult.Recorded, Finish(store, session, 0, 41, 30));
            Assert.AreEqual(RecordResult.Duplicate, Finish(store, session, 0, 41, 30));
            Assert.AreEqual(RecordResult.Duplicate, store.History.TryAdd(session.Result), "the history refuses a known ID too");

            Assert.AreEqual(1, store.History.Records.Count);
            Assert.AreEqual(1, store.History.Lifetime.Completed);
        }

        [Test]
        public void CompletedMatch_CannotBeAbandonedAfterwards_AndViceVersa()
        {
            LocalDataStore store = LoadStore();
            var finished = new MatchSession(MatchConfig.VsComputer("finished"));
            Finish(store, finished, 0, 41, 30);
            var left = new MatchSession(MatchConfig.VsComputer("left"));
            left.Begin(T0);
            store.AbandonMatch(left, out _);

            Assert.IsFalse(store.AbandonMatch(finished, out _), "Lobby after the result must not count an abandon");
            Assert.AreEqual(RecordResult.Duplicate, Finish(store, left, 0, 41, 30), "a left match cannot complete later");
            Assert.IsFalse(store.AbandonMatch(left, out _), "abandoned once only");

            MatchStatistics stats = store.History.Lifetime;
            Assert.AreEqual(1, stats.Completed);
            Assert.AreEqual(1, stats.Abandoned);
            Assert.AreEqual(MatchState.Completed, finished.State);
            Assert.AreEqual(MatchState.Abandoned, left.State);
        }

        [Test]
        public void InvalidRecords_AreRefused()
        {
            var history = MatchHistory.CreateEmpty();

            Assert.AreEqual(RecordResult.Invalid, history.TryAdd(null));
            Assert.AreEqual(RecordResult.Invalid, history.TryAdd(Record("", MatchOutcome.Victory, 41, 3)));
            Assert.AreEqual(RecordResult.Invalid, history.TryAdd(Record("neg", MatchOutcome.Victory, -1, 3)));
            Assert.AreEqual(RecordResult.Invalid, history.TryAdd(Record("decks", MatchOutcome.Victory, 41, 3, decks: 0)));
            Assert.AreEqual(RecordResult.Invalid, history.TryAdd(Record("outcome", (MatchOutcome)0, 41, 3)));
            Assert.AreEqual(RecordResult.Invalid, history.TryAdd(new MatchRecord("mode", (MatchMode)9, "Nadia", T0, T0, MatchOutcome.Victory, 41, 3, 1)));
            Assert.AreEqual(RecordResult.Invalid, history.TryAdd(new MatchRecord("time", MatchMode.VsComputer, "Nadia", T0, T0.AddMinutes(-1), MatchOutcome.Victory, 41, 3, 1)));
            Assert.AreEqual(0, history.Records.Count);
            Assert.AreEqual(0, history.Lifetime.Completed);
        }

        [Test]
        public void HistoryOnDisk_WithBadOrRepeatedRecords_IsRepaired_AndTotalsRebuilt()
        {
            const string good = "{\"matchId\":\"good\",\"mode\":1,\"opponentName\":\"Nadia\",\"startedUtc\":\"2026-11-17T12:00:00.0000000Z\"," +
                                "\"completedUtc\":\"2026-11-17T12:10:00.0000000Z\",\"outcome\":1,\"playerScore\":41,\"opponentScore\":20,\"decksPlayed\":3}";
            const string bad = "{\"matchId\":\"bad\",\"mode\":1,\"opponentName\":\"Nadia\",\"startedUtc\":\"yesterday\"," +
                               "\"completedUtc\":\"2026-11-17T12:10:00.0000000Z\",\"outcome\":1,\"playerScore\":41,\"opponentScore\":20,\"decksPlayed\":3}";
            // Totals claim 0 completed while one valid record exists: inconsistent, so they are rebuilt.
            WriteFile(LocalDataStore.HistoryName,
                "{\"schemaVersion\":1,\"records\":[" + good + "," + bad + "," + good + "]," +
                "\"lifetime\":{\"completed\":0,\"victories\":0,\"defeats\":0,\"abandoned\":2,\"bestWinningMargin\":0,\"highestScore\":0}}");

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Repaired, store.HistoryStatus);
            Assert.AreEqual(1, store.History.Records.Count);
            Assert.AreEqual("good", store.History.Records[0].MatchId);
            Assert.AreEqual(1, store.History.Lifetime.Completed);
            Assert.AreEqual(1, store.History.Lifetime.Victories);
            Assert.AreEqual(21, store.History.Lifetime.BestWinningMargin);
            Assert.AreEqual(2, store.History.Lifetime.Abandoned, "a valid abandoned count is kept");
            FileAssert.Exists(FilePath(LocalDataStore.HistoryName + LocalDataStore.CorruptSuffix));
        }
    }
}
