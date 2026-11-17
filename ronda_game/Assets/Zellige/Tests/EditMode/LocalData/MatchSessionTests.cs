using System;
using NUnit.Framework;
using Zellige.App;

namespace Zellige.Tests.LocalData
{
    public sealed class MatchSessionTests : LocalDataTestBase
    {
        [Test]
        public void VsComputerConfig_IsAgainstNadia_WithANewIdEachTime()
        {
            MatchConfig a = MatchConfig.VsComputer();
            MatchConfig b = MatchConfig.VsComputer();

            Assert.AreEqual(MatchMode.VsComputer, a.Mode);
            Assert.AreEqual("Nadia", a.OpponentName);
            Assert.IsFalse(string.IsNullOrEmpty(a.MatchId));
            Assert.LessOrEqual(a.MatchId.Length, MatchRecord.MaxIdLength);
            Assert.AreNotEqual(a.MatchId, b.MatchId);
        }

        [Test]
        public void TryCreate_StartsOnlyMeVsComputer()
        {
            Assert.IsTrue(MatchConfig.TryCreate(MatchMode.VsComputer, out MatchConfig config, out string reason));
            Assert.AreEqual(MatchMode.VsComputer, config.Mode);
            Assert.IsNull(reason);
        }

        [TestCase(MatchMode.Online1v1, TextKeys.ModeNotAvailable)]
        [TestCase(MatchMode.Online2v2, TextKeys.ModeNotAvailable)]
        [TestCase((MatchMode)0, TextKeys.ModeUnknown)]
        [TestCase((MatchMode)42, TextKeys.ModeUnknown)]
        public void TryCreate_RefusesTheLockedOnlineModes_AndUnknownValues(MatchMode mode, string expectedReason)
        {
            Assert.IsFalse(MatchConfig.TryCreate(mode, out MatchConfig config, out string reason));
            Assert.IsNull(config, "no config exists for a mode that cannot be played");
            Assert.AreEqual(expectedReason, reason);
            Assert.IsTrue(TextCatalog.Has(reason));
        }

        [Test]
        public void ModeFlags_DescribeTheApprovedModes()
        {
            Assert.IsTrue(MatchModes.IsPlayable(MatchMode.VsComputer));
            Assert.IsFalse(MatchModes.IsOnline(MatchMode.VsComputer));
            foreach (MatchMode online in new[] { MatchMode.Online1v1, MatchMode.Online2v2 })
            {
                Assert.IsTrue(MatchModes.IsOnline(online));
                Assert.IsFalse(MatchModes.IsPlayable(online), online + " must stay locked in this version");
            }
        }

        [Test]
        public void Lifecycle_CreatedThenInProgressThenCompleted_AndCompletedIsFinal()
        {
            var session = new MatchSession(MatchConfig.VsComputer("m1"));
            Assert.AreEqual(MatchState.Created, session.State);
            Assert.IsFalse(session.IsFinished);

            Assert.IsTrue(session.Begin(T0));
            Assert.AreEqual(MatchState.InProgress, session.State);
            Assert.IsFalse(session.Begin(T0.AddMinutes(1)), "a match starts once");
            Assert.AreEqual(T0, session.StartedUtc);

            Assert.IsTrue(session.TryComplete(0, new[] { 41, 12 }, 2, T0.AddMinutes(5), out MatchRecord record));
            Assert.AreEqual(MatchState.Completed, session.State);
            Assert.IsTrue(session.IsFinished);
            Assert.IsNotNull(record);

            Assert.IsFalse(session.TryComplete(0, new[] { 41, 12 }, 2, T0.AddMinutes(6), out MatchRecord second));
            Assert.IsNull(second);
            Assert.IsFalse(session.TryAbandon());
            Assert.AreEqual(MatchState.Completed, session.State);
        }

        [Test]
        public void Lifecycle_InProgressThenAbandoned_AndAbandonedIsFinal()
        {
            var session = new MatchSession(MatchConfig.VsComputer("m1"));
            session.Begin(T0);

            Assert.IsTrue(session.TryAbandon());
            Assert.AreEqual(MatchState.Abandoned, session.State);
            Assert.IsNull(session.Result);
            Assert.IsFalse(session.TryComplete(0, new[] { 41, 12 }, 2, T0, out _));
            Assert.IsFalse(session.Begin(T0), "an abandoned match is never resumed");
            Assert.AreEqual(MatchState.Abandoned, session.State);
        }

        [Test]
        public void MatchThatNeverStarted_CannotCompleteOrBeAbandoned()
        {
            var session = new MatchSession(MatchConfig.VsComputer("m1"));

            Assert.IsFalse(session.TryAbandon());
            Assert.IsFalse(session.TryComplete(0, new[] { 41, 12 }, 2, T0, out _));
            Assert.AreEqual(MatchState.Created, session.State);
        }

        [Test]
        public void InvalidFinalResult_Throws_AndLeavesTheMatchInProgress()
        {
            var session = new MatchSession(MatchConfig.VsComputer("m1"));
            session.Begin(T0);

            Assert.Throws<ArgumentException>(() => session.TryComplete(0, new[] { 41 }, 2, T0, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.TryComplete(2, new[] { 41, 12 }, 2, T0, out _));
            Assert.Throws<ArgumentException>(() => session.TryComplete(0, new[] { 41, -1 }, 2, T0, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.TryComplete(0, new[] { 41, 12 }, 0, T0, out _));
            Assert.AreEqual(MatchState.InProgress, session.State);
        }

        [Test]
        public void DeviceClockMovedBack_StillGivesAValidRecord()
        {
            var session = new MatchSession(MatchConfig.VsComputer("m1"));
            session.Begin(T0);

            session.TryComplete(0, new[] { 41, 12 }, 2, T0.AddHours(-2), out MatchRecord record);

            Assert.IsTrue(record.IsValid());
            Assert.AreEqual(record.StartedUtc, record.CompletedUtc);
        }

        [Test]
        public void WithoutAPendingConfig_TheGameGetsAFreshMeVsComputerMatch()
        {
            Assert.IsFalse(MatchHandoff.HasPending);

            MatchConfig first = MatchHandoff.TakePendingOrDefault();
            MatchConfig second = MatchHandoff.TakePendingOrDefault();

            Assert.AreEqual(MatchMode.VsComputer, first.Mode);
            Assert.AreEqual("Nadia", first.OpponentName);
            Assert.AreNotEqual(first.MatchId, second.MatchId);
        }

        [Test]
        public void PendingConfig_IsTakenOnce_AndNeverReused()
        {
            MatchConfig offered = MatchConfig.VsComputer("from-lobby");
            Assert.IsTrue(MatchHandoff.Offer(offered));
            Assert.IsTrue(MatchHandoff.HasPending);

            Assert.AreSame(offered, MatchHandoff.TakePendingOrDefault());
            Assert.IsFalse(MatchHandoff.HasPending);
            Assert.AreNotEqual("from-lobby", MatchHandoff.TakePendingOrDefault().MatchId, "the next match gets a new config");
        }

        [Test]
        public void Handoff_RefusesANullConfig()
        {
            Assert.IsFalse(MatchHandoff.Offer(null));
            Assert.IsFalse(MatchHandoff.HasPending);
        }

        [Test]
        public void ConsecutiveMatches_UseSeparateSessions_AndDoNotShareResults()
        {
            LocalDataStore store = LoadStore();
            var first = new MatchSession(MatchHandoff.TakePendingOrDefault());
            Finish(store, first, 0, 41, 12);

            var second = new MatchSession(MatchHandoff.TakePendingOrDefault());   // "Play again"

            Assert.AreEqual(MatchState.Created, second.State);
            Assert.IsNull(second.Result);
            Assert.AreNotEqual(first.Config.MatchId, second.Config.MatchId);
            Finish(store, second, 1, 30, 41);
            Assert.AreEqual(2, store.History.Records.Count);
        }
    }
}
