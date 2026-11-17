using System;
using NUnit.Framework;
using Zellige.AI;
using Zellige.App;
using Zellige.Core;
using Zellige.Game;
using Zellige.Tests.LocalData;

namespace Zellige.Tests.Navigation
{
    /// <summary>
    /// The link between the running match and the local data (L2). Real matches are played by the
    /// unchanged engine and AI on both seats, and every event batch goes through
    /// <see cref="MatchLifecycle.Observe"/> exactly as GameController sends it.
    /// </summary>
    public sealed class MatchLifecycleTests : LocalDataTestBase
    {
        private MatchLifecycle NewLifecycle(LocalDataStore store) => new MatchLifecycle(store, () => T0.AddMinutes(30));

        /// <summary>Plays until the engine ends the match, or for <paramref name="maxPlays"/> plays.</summary>
        private static RondaGame Play(MatchLifecycle lifecycle, int seed, int maxPlays = int.MaxValue)
        {
            var game = new RondaGame(new RulesConfig(), seed);
            var players = new[] { new SimpleAi(seed + 1), new SimpleAi(seed + 2) };
            lifecycle.Observe(game.StartMatch());
            for (int plays = 0; plays < maxPlays && !game.IsMatchOver; plays++)
            {
                int seat = game.CurrentPlayer;
                foreach (int rank in players[seat].ChooseAnnouncements(game, seat)) game.Announce(seat, rank);
                lifecycle.Observe(game.PlayCard(seat, players[seat].ChooseCard(game, seat)));
            }
            return game;
        }

        [Test]
        public void WithoutAHandoff_ANewMatchIsAFreshMeVsComputerSession()
        {
            var lifecycle = NewLifecycle(LoadStore());

            MatchSession session = lifecycle.StartNew();

            Assert.AreSame(session, lifecycle.Current);
            Assert.AreEqual(MatchMode.VsComputer, session.Config.Mode);
            Assert.AreEqual("Nadia", session.Config.OpponentName);
            Assert.AreEqual(MatchState.InProgress, session.State);
            Assert.IsTrue(lifecycle.IsInProgress);
        }

        [Test]
        public void TheLobbyHandoff_IsUsedByTheFirstMatchOnly()
        {
            var lifecycle = NewLifecycle(LoadStore());
            MatchHandoff.Offer(MatchConfig.VsComputer("from-lobby"));

            Assert.AreEqual("from-lobby", lifecycle.StartNew().Config.MatchId);
            Assert.IsFalse(MatchHandoff.HasPending);
            Assert.AreNotEqual("from-lobby", lifecycle.StartNew().Config.MatchId, "Play Again gets a fresh match");
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(2026)]
        public void AFinishedMatch_IsRecordedOnce_WithTheEnginesResult(int seed)
        {
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);
            lifecycle.StartNew();

            RondaGame game = Play(lifecycle, seed);

            Assert.IsTrue(game.IsMatchOver);
            Assert.AreEqual(MatchState.Completed, lifecycle.Current.State);
            Assert.AreEqual(RecordResult.Recorded, lifecycle.LastRecordResult);
            Assert.IsTrue(lifecycle.LastSaveSucceeded);
            Assert.AreEqual(1, store.History.Records.Count);
            MatchRecord record = store.History.Records[0];
            Assert.AreEqual(lifecycle.Current.Config.MatchId, record.MatchId);
            Assert.AreEqual(game.GetScore(0), record.PlayerScore);
            Assert.AreEqual(game.GetScore(1), record.OpponentScore);
            Assert.AreEqual(game.Winner == 0 ? MatchOutcome.Victory : MatchOutcome.Defeat, record.Outcome);
            Assert.AreEqual(game.DeckRound, record.DecksPlayed);
            Assert.AreEqual(1, LoadStore().History.Lifetime.Completed, "saved to disk");
        }

        [Test]
        public void SeeingTheEndAgain_DoesNotRecordASecondTime()
        {
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);
            lifecycle.StartNew();
            RondaGame game = Play(lifecycle, 3);

            lifecycle.Observe(new GameEvent[] { new MatchEndedEvent { Winner = game.Winner, Scores = new[] { game.GetScore(0), game.GetScore(1) } } });

            Assert.AreEqual(1, store.History.Records.Count);
            Assert.AreEqual(1, store.History.Lifetime.Completed);
        }

        [Test]
        public void AFinishedMatch_CannotBeAbandonedAfterwards()
        {
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);
            lifecycle.StartNew();
            Play(lifecycle, 4);

            Assert.IsFalse(lifecycle.Leave(), "Lobby or Back after the result must not count an abandon");
            Assert.AreEqual(MatchState.Completed, lifecycle.Current.State);
            Assert.AreEqual(0, store.History.Lifetime.Abandoned);
            Assert.AreEqual(1, store.History.Lifetime.Completed);
        }

        [Test]
        public void LeavingDuringTheMatch_AbandonsIt_WithoutCompletingIt()
        {
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);
            lifecycle.StartNew();
            RondaGame game = Play(lifecycle, 5, maxPlays: 10);
            Assume.That(game.IsMatchOver, Is.False);

            Assert.IsTrue(lifecycle.Leave());
            Assert.IsFalse(lifecycle.Leave(), "abandoned once only");

            Assert.AreEqual(MatchState.Abandoned, lifecycle.Current.State);
            Assert.AreEqual(0, store.History.Records.Count);
            Assert.AreEqual(0, store.History.Lifetime.Completed);
            Assert.AreEqual(0, store.History.Lifetime.Defeats);
            Assert.AreEqual(1, store.History.Lifetime.Abandoned);
        }

        [Test]
        public void AnAbandonedMatch_CannotBeCompletedLater()
        {
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);
            lifecycle.StartNew();
            Play(lifecycle, 6, maxPlays: 10);
            lifecycle.Leave();

            lifecycle.Observe(new GameEvent[] { new MatchEndedEvent { Winner = 0, Scores = new[] { 41, 10 } } });

            Assert.AreEqual(MatchState.Abandoned, lifecycle.Current.State);
            Assert.AreEqual(0, store.History.Records.Count);
        }

        [Test]
        public void PlayAgain_AfterAResult_StartsAFreshMatch_AndKeepsTheFirstRecord()
        {
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);
            MatchSession first = lifecycle.StartNew();
            Play(lifecycle, 8);

            MatchSession second = lifecycle.StartNew();

            Assert.AreNotSame(first, second);
            Assert.AreNotEqual(first.Config.MatchId, second.Config.MatchId);
            Assert.AreEqual(MatchState.InProgress, second.State);
            Assert.AreEqual(MatchState.Completed, first.State);
            Assert.AreEqual(1, store.History.Records.Count);

            Play(lifecycle, 9);
            Assert.AreEqual(2, store.History.Records.Count);
        }

        [Test]
        public void NewGameDuringAMatch_RecordsNothingForTheUnfinishedMatch()
        {
            // Open owner decision (L2 report, question 2): until it is answered, an unfinished match
            // replaced by "New game" is neither completed nor abandoned. This test pins that neutral behaviour.
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);
            MatchSession first = lifecycle.StartNew();
            Play(lifecycle, 10, maxPlays: 6);

            lifecycle.StartNew();

            Assert.AreEqual(MatchState.InProgress, first.State, "the old session is simply dropped");
            Assert.AreEqual(0, store.History.Records.Count);
            Assert.AreEqual(0, store.History.Lifetime.Abandoned);
        }

        [Test]
        public void BeforeTheFirstMatch_NothingCanBeRecordedOrLeft()
        {
            LocalDataStore store = LoadStore();
            var lifecycle = NewLifecycle(store);

            Assert.IsFalse(lifecycle.Leave());
            lifecycle.Observe(new GameEvent[] { new MatchEndedEvent { Winner = 0, Scores = new[] { 41, 0 } } });
            lifecycle.Observe(null);

            Assert.IsNull(lifecycle.Current);
            Assert.AreEqual(0, store.History.Records.Count);
        }

        [Test]
        public void ANullStore_IsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchLifecycle(null));
        }
    }
}
