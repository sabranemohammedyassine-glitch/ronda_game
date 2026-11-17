using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Zellige.Core;

namespace Zellige.Tests
{
    /// <summary>
    /// Phase D: the unverified assumptions H-1 … H-10 as RulesConfig settings
    /// (docs/mvp/06_test_fixture_and_rules_config.md, Part D). Each setting is tested at its
    /// default (= the behaviour before the setting existed) and at every implemented alternative.
    /// Expected values are worked out by hand in each test's comment. That the defaults preserve
    /// whole matches exactly is proved separately by <see cref="GoldenMasterTests"/>.
    /// </summary>
    public class RulesSettingsTests
    {
        private const int H = 0;
        private const int A = 1;

        private static Card C(Suit suit, int rank) => new Card(suit, rank);

        // ---------- Defaults ----------

        [Test]
        public void Defaults_AreTheBehaviourBeforeTheSettingsExisted()
        {
            var rules = new RulesConfig();
            Assert.AreEqual(FirstPlayer.NonDealer, rules.FirstPlayer, "H-1");
            Assert.AreEqual(AnnouncementTie.NobodyScores, rules.AnnouncementTie, "H-2");
            Assert.AreEqual(BothReachTarget.HigherScoreWinsTieReplays, rules.BothReachTarget, "H-3");
            Assert.IsFalse(rules.ChainWrapsAfterTwelve, "H-4");
            Assert.IsTrue(rules.DerbaAcrossDeals, "H-5");
            Assert.IsFalse(rules.PointsAfterWinningPointCount, "H-6");
            Assert.AreEqual(PendingAnnouncementsAtMatchEnd.Discard, rules.PendingAnnouncementsAtMatchEnd, "H-7");
            Assert.AreEqual(TringlaAwardCap.WholeAwardUncapped, rules.TringlaAwardCap, "H-8");
            Assert.IsFalse(rules.QuadCountsAsTwoAnnouncements, "H-9");
            Assert.IsNull(rules.FirstDealerSeat, "H-10");
        }

        /// <summary>
        /// H-2, H-7, H-8 have one defined option only; their documented alternatives are not precise
        /// enough to implement without inventing a rule. If an option is added, this test must be
        /// updated together with a test of the new option.
        /// </summary>
        [Test]
        public void SingleOptionSettings_HaveNoUndefinedAlternatives()
        {
            Assert.AreEqual(1, Enum.GetValues(typeof(AnnouncementTie)).Length, "H-2");
            Assert.AreEqual(1, Enum.GetValues(typeof(PendingAnnouncementsAtMatchEnd)).Length, "H-7");
            Assert.AreEqual(1, Enum.GetValues(typeof(TringlaAwardCap)).Length, "H-8");
        }

        // ---------- H-1 First player ----------

        /// <summary>
        /// Dealer = seat 1, standard (unshuffled) deck order. Default: the non-dealer (seat 0)
        /// receives the first 4 cards and leads. Dealer option: the dealer receives them and
        /// leads, in deal 1 and again in deal 2.
        /// </summary>
        [TestCase(FirstPlayer.NonDealer, H)]
        [TestCase(FirstPlayer.Dealer, A)]
        public void H1_FirstPlayer_LeadsEveryDeal(FirstPlayer setting, int expectedLeader)
        {
            List<Card> order = Deck.CreateStandard();
            var game = new RondaGame(new RulesConfig { FirstPlayer = setting }, 1);
            game.StartMatch(firstDealer: A, firstDeckOrder: order);

            Assert.AreEqual(expectedLeader, game.CurrentPlayer, "deal 1 leader");
            CollectionAssert.AreEqual(order.Take(4), game.GetHand(expectedLeader), "the leader gets the first 4 cards");
            CollectionAssert.AreEqual(order.Skip(4).Take(4), game.GetHand(1 - expectedLeader));

            for (int play = 0; play < 8; play++)
            {
                int seat = game.CurrentPlayer;
                Assert.AreEqual(play % 2 == 0 ? expectedLeader : 1 - expectedLeader, seat, "players alternate");
                game.PlayCard(seat, game.GetHand(seat)[0]);
            }
            Assert.AreEqual(2, game.DealNumber);
            Assert.AreEqual(expectedLeader, game.CurrentPlayer, "deal 2 leader");
        }

        // ---------- H-2 Equal announcements ----------

        /// <summary>
        /// H holds two 5s, A holds the other two 5s; both announce a ronda of 5. H drops a 1, A drops
        /// a 7: the window closes. Expected (only option, NobodyScores): the contest is a tie
        /// (winner −1) and nobody scores.
        /// </summary>
        [Test]
        public void H2_EqualAnnouncements_NobodyScores()
        {
            RondaGame game = new StateBuilder()
                .Dealer(A).Turn(H).Deal(1, 1).AnnounceWindow(0)
                .Hand(H, C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Oros, 1), C(Suit.Oros, 2))
                .Hand(A, C(Suit.Espadas, 5), C(Suit.Bastos, 5), C(Suit.Copas, 7), C(Suit.Copas, 11))
                .LastCapturer(A).PilesWithRest(16)
                .Build();
            Assert.IsTrue(game.Announce(H, 5));
            Assert.IsTrue(game.Announce(A, 5));

            game.PlayCard(H, C(Suit.Oros, 1));
            List<GameEvent> closing = game.PlayCard(A, C(Suit.Copas, 7)).ToList();

            Assert.AreEqual(-1, closing.OfType<AnnouncementsResolvedEvent>().Single().Winner);
            Assert.IsEmpty(closing.OfType<ScoreAwardedEvent>());
            Expect.Scores(game, 0, 0, "after the tie");
        }

        // ---------- H-3 Both players reach the target in one tally ----------

        /// <summary>
        /// Last card of the deck. Dealer H plays it (drops the 5 of Oros); A made the last capture.
        /// Piles H 24 / A 15. Tally: leftover {5} → A (16); A +5 (dealer rule); H +4 (24 − 20).
        /// </summary>
        private static RondaGame LastCardBothReach(RulesConfig rules, int hScore, int aScore)
        {
            return new StateBuilder(rules)
                .Dealer(H).Turn(H).Deal(1, 5)
                .Scores(hScore, aScore)
                .Hand(H, C(Suit.Oros, 5))
                .LastPlay(A, C(Suit.Copas, 7), wasDrop: false)
                .LastCapturer(A)
                .PilesWithRest(24)
                .Build();
        }

        /// <summary>Scores 37 / 37 → H 41, A 42: both reached 41, A is higher.</summary>
        [TestCase(BothReachTarget.HigherScoreWinsTieReplays, true)]
        [TestCase(BothReachTarget.AlwaysPlayAnotherDeck, false)]
        public void H3_BothReachTheTarget_DifferentScores(BothReachTarget setting, bool aWins)
        {
            RondaGame game = LastCardBothReach(new RulesConfig { BothReachTarget = setting }, 37, 37);

            List<GameEvent> events = game.PlayCard(H, C(Suit.Oros, 5)).ToList();

            Expect.Awards(events, (A, ScoreReason.DealerPenalty, 5), (H, ScoreReason.CardsOverThreshold, 4));
            Expect.Scores(game, 41, 42, "after the tally");
            Assert.AreEqual(aWins, game.IsMatchOver);
            if (aWins) Assert.AreEqual(A, game.Winner);
            else Assert.AreEqual(2, game.DeckRound, "another deck is played");
        }

        /// <summary>Scores 37 / 36 → 41 / 41: an exact tie plays another deck under both options.</summary>
        [TestCase(BothReachTarget.HigherScoreWinsTieReplays)]
        [TestCase(BothReachTarget.AlwaysPlayAnotherDeck)]
        public void H3_BothReachTheTarget_ExactTie_PlaysAnotherDeck(BothReachTarget setting)
        {
            RondaGame game = LastCardBothReach(new RulesConfig { BothReachTarget = setting }, 37, 36);

            game.PlayCard(H, C(Suit.Oros, 5));

            Expect.Scores(game, 41, 41, "after the tally");
            Assert.IsFalse(game.IsMatchOver);
            Assert.AreEqual(2, game.DeckRound);
        }

        /// <summary>
        /// CHARACTERISATION of current behaviour, not a verified rule (finding F-1, audit §3):
        /// after a tie at or above the target, both players are already past 41, so the during-play
        /// "reaching the target ends the match" check makes the first non-capped point of the next
        /// deck win. Phase 3 (RL-23b) describes the extra deck as decided "after a tally" instead;
        /// the reference is UNKNOWN. This test pins today's behaviour so any change is deliberate.
        /// </summary>
        [Test]
        public void H3_AfterATieAtTheTarget_TheFirstPointOfTheNextDeckWins_CurrentBehaviour()
        {
            RondaGame game = LastCardBothReach(new RulesConfig(), 37, 36);
            game.PlayCard(H, C(Suit.Oros, 5));
            Assert.IsTrue(game.GetScore(H) == 41 && game.GetScore(A) == 41 && !game.IsMatchOver, "setup: a 41-41 tie after the tally");

            var rng = new Random(7);
            ScoreAwardedEvent firstPoint = null;
            List<GameEvent> playEvents = null;
            while (!game.IsMatchOver)
            {
                int seat = game.CurrentPlayer;
                List<Card> legal = game.GetLegalCards(seat);
                playEvents = game.PlayCard(seat, legal[rng.Next(legal.Count)]).ToList();
                firstPoint = playEvents.OfType<ScoreAwardedEvent>().FirstOrDefault(e => e.Points > 0);
                if (firstPoint != null) break;
            }

            Assert.IsNotNull(firstPoint, "someone scores in the next deck");
            Assert.IsTrue(game.IsMatchOver, "the first point ends the match");
            Assert.AreEqual(firstPoint.Seat, game.Winner);
            Assert.AreEqual(2, game.DeckRound, "during the next deck, before its tally");
            Assert.IsEmpty(playEvents.OfType<DeckTalliedEvent>());
        }

        // ---------- H-4 Chain wraps after 12 ----------

        [Test]
        public void H4_CaptureRules_WrapOnlyWhenAsked()
        {
            List<Card> table = TestDecks.TableOf(12, 1, 2, 5);
            Card played = C(Suit.Copas, 12);

            CollectionAssert.AreEqual(new[] { 12 }, CaptureRules.FindCaptures(table, played).Select(c => c.Rank));
            CollectionAssert.AreEqual(new[] { 12, 1, 2 }, CaptureRules.FindCaptures(table, played, true).Select(c => c.Rank),
                "12, then wraps to 1, 2; stops at the missing 3");
        }

        [Test]
        public void H4_Wrap_TakesEachRankOnceAndStops()
        {
            List<Card> table = TestDecks.TableOf(1, 2, 3, 4, 5, 6, 7, 10, 11, 12);
            List<Card> taken = CaptureRules.FindCaptures(table, C(Suit.Copas, 5), true);

            CollectionAssert.AreEqual(new[] { 5, 6, 7, 10, 11, 12, 1, 2, 3, 4 }, taken.Select(c => c.Rank));
        }

        /// <summary>
        /// Through the engine: H plays the 12 of Copas onto {12, 1, 2, 6 of Oros}. Default: takes the
        /// 12 only (table {1, 2, 6}). Wrap: takes 12, 1, 2 (table {6}).
        /// </summary>
        [TestCase(false, new[] { 12 }, new[] { 1, 2, 6 })]
        [TestCase(true, new[] { 12, 1, 2 }, new[] { 6 })]
        public void H4_Engine_UsesTheSetting(bool wrap, int[] expectedCaptured, int[] expectedTable)
        {
            RondaGame game = new StateBuilder(new RulesConfig { ChainWrapsAfterTwelve = wrap })
                .Dealer(A).Turn(H).Deal(1, 4)
                .Hand(H, C(Suit.Copas, 12), C(Suit.Copas, 3))
                .Hand(A, C(Suit.Copas, 4), C(Suit.Copas, 5))
                .Table(C(Suit.Oros, 12), C(Suit.Oros, 1), C(Suit.Oros, 2), C(Suit.Oros, 6))
                .LastCapturer(A).PilesWithRest(15)
                .Build();

            CardPlayedEvent played = game.PlayCard(H, C(Suit.Copas, 12)).OfType<CardPlayedEvent>().Single();

            CollectionAssert.AreEqual(expectedCaptured, played.Captured.Select(c => c.Rank));
            Expect.TableRanks(game.Table, expectedTable);
        }

        // ---------- H-5 Derba across a deal boundary ----------

        /// <summary>
        /// Deal 2 has just been dealt; H leads. A's last play (in deal <paramref name="lastPlayDeal"/>)
        /// was the drop of the 7 of Oros; the table is {7, 3}. H plays the 7 of Copas: takes the 7
        /// (the 3 stays, no Missa). Derba +1 unless the drop was in the previous deal and
        /// DerbaAcrossDeals is off.
        /// </summary>
        [TestCase(true, 1, 1)]
        [TestCase(false, 1, 0)]
        [TestCase(false, 2, 1)]
        public void H5_DerbaAcrossDeals(bool setting, int lastPlayDeal, int expectedDerba)
        {
            RondaGame game = new StateBuilder(new RulesConfig { DerbaAcrossDeals = setting })
                .Dealer(A).Turn(H).Deal(1, 2)
                .Hand(H, C(Suit.Copas, 7), C(Suit.Copas, 1), C(Suit.Copas, 2), C(Suit.Copas, 4))
                .Hand(A, C(Suit.Espadas, 5), C(Suit.Bastos, 5), C(Suit.Espadas, 11), C(Suit.Espadas, 12))
                .Table(C(Suit.Oros, 7), C(Suit.Oros, 3))
                .LastPlay(A, C(Suit.Oros, 7), wasDrop: true, deal: lastPlayDeal)
                .LastCapturer(A).PilesWithRest(15)
                .Build();

            List<GameEvent> events = game.PlayCard(H, C(Suit.Copas, 7)).ToList();

            if (expectedDerba == 1) Expect.Awards(events, (H, ScoreReason.Derba, 1));
            else Expect.Awards(events);
            Expect.TableRanks(game.Table, 3);
        }

        // ---------- H-6 Points after the winning point ----------

        /// <summary>
        /// A (40 points) plays the 10 of Copas onto {10 of Oros}, which H has just dropped: Derba
        /// (→ 41) and Missa on the same play. Default: the match ends at 41; the Missa is not
        /// awarded. Alternative: the Missa still counts → 42. A wins either way.
        /// </summary>
        [TestCase(false, 41, new[] { ScoreReason.Derba })]
        [TestCase(true, 42, new[] { ScoreReason.Derba, ScoreReason.Missa })]
        public void H6_DerbaAndMissaOnTheWinningPlay(bool setting, int finalScore, ScoreReason[] awarded)
        {
            RondaGame game = new StateBuilder(new RulesConfig { PointsAfterWinningPointCount = setting })
                .Dealer(A).Turn(A).Deal(1, 3)
                .Scores(10, 40)
                .Hand(A, C(Suit.Copas, 10))
                .Table(C(Suit.Oros, 10))
                .Stock(C(Suit.Bastos, 1), C(Suit.Bastos, 2), C(Suit.Bastos, 3), C(Suit.Bastos, 4),
                       C(Suit.Bastos, 5), C(Suit.Bastos, 6), C(Suit.Bastos, 7), C(Suit.Bastos, 11))
                .LastPlay(H, C(Suit.Oros, 10), wasDrop: true)
                .LastCapturer(H).PilesWithRest(15)
                .Build();

            List<GameEvent> events = game.PlayCard(A, C(Suit.Copas, 10)).ToList();

            CollectionAssert.AreEqual(awarded, events.OfType<ScoreAwardedEvent>().Select(e => e.Reason));
            MatchEndedEvent end = events.OfType<MatchEndedEvent>().Single();
            Assert.AreEqual(A, end.Winner);
            CollectionAssert.AreEqual(new[] { 10, finalScore }, end.Scores);
        }

        /// <summary>
        /// Announcement award: H announces a ronda of 6; A (score <paramref name="aScore"/>)
        /// announces a tringla of 3 and drops the 11, which closes the window. A wins the contest:
        /// own 5 (tringla, uncapped) + bonus 1 (H's announcement).
        /// </summary>
        private static List<GameEvent> TringlaAgainstRonda(RulesConfig rules, int aScore, out RondaGame game)
        {
            game = new StateBuilder(rules)
                .Dealer(A).Turn(A).Deal(1, 1).AnnounceWindow(1)
                .Scores(0, aScore)
                .Hand(H, C(Suit.Oros, 6), C(Suit.Copas, 6), C(Suit.Oros, 1))
                .Hand(A, C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Espadas, 3), C(Suit.Oros, 11))
                .Table(C(Suit.Oros, 2))
                .LastPlay(H, C(Suit.Oros, 2), wasDrop: true)
                .LastCapturer(H).PilesWithRest(16)
                .Build();
            Assert.IsTrue(game.Announce(H, 6));
            Assert.IsTrue(game.Announce(A, 3));
            return game.PlayCard(A, C(Suit.Oros, 11)).ToList();
        }

        /// <summary>A at 38: own 5 → 43 reaches 41. Default: the bonus is not awarded (43); alternative: it is (44).</summary>
        [TestCase(false, 43)]
        [TestCase(true, 44)]
        public void H6_AnnouncementBonusAfterTheWinningPoints(bool setting, int finalScore)
        {
            List<GameEvent> events = TringlaAgainstRonda(new RulesConfig { PointsAfterWinningPointCount = setting }, 38, out RondaGame game);

            Assert.AreEqual(A, events.OfType<AnnouncementsResolvedEvent>().Single().Winner);
            Assert.IsTrue(game.IsMatchOver);
            Assert.AreEqual(A, game.Winner);
            Assert.AreEqual(finalScore, game.GetScore(A));
        }

        // ---------- H-7 Pending announcements when the match ends ----------

        /// <summary>
        /// Deal just dealt, window open. A announces a ronda of 5. H (40 points) leads with the 6 of
        /// Copas onto {6 of Oros}: takes it, table empty → Missa → 41, before the window closes.
        /// Expected (only option, Discard): A's ronda is never resolved or scored.
        /// </summary>
        [Test]
        public void H7_PendingAnnouncementsAreDiscardedWhenTheMatchEnds()
        {
            RondaGame game = new StateBuilder()
                .Dealer(A).Turn(H).Deal(2, 2).AnnounceWindow(0)
                .Scores(40, 20)
                .Hand(H, C(Suit.Copas, 6), C(Suit.Copas, 1), C(Suit.Copas, 2), C(Suit.Copas, 4))
                .Hand(A, C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Oros, 7), C(Suit.Copas, 11))
                .Table(C(Suit.Oros, 6))
                .LastPlay(A, C(Suit.Oros, 12), wasDrop: false)
                .LastCapturer(A).PilesWithRest(15)
                .Build();
            Assert.IsTrue(game.Announce(A, 5));

            List<GameEvent> events = game.PlayCard(H, C(Suit.Copas, 6)).ToList();

            Expect.Awards(events, (H, ScoreReason.Missa, 1));
            Assert.IsEmpty(events.OfType<AnnouncementsResolvedEvent>());
            CollectionAssert.AreEqual(new[] { 41, 20 }, events.OfType<MatchEndedEvent>().Single().Scores);
        }

        // ---------- H-8 Ronda cap and awards containing a tringla ----------

        /// <summary>
        /// Only option, WholeAwardUncapped: with H-6 on (so the bonus is awarded after 41), A at 39
        /// gets own 5 → 44 and the +1 bonus → 45. The bonus is not capped because the award
        /// contains a tringla; a capped bonus would leave 44.
        /// </summary>
        [Test]
        public void H8_TheWholeTringlaAwardIsUncapped()
        {
            var rules = new RulesConfig { PointsAfterWinningPointCount = true };
            List<GameEvent> events = TringlaAgainstRonda(rules, 39, out RondaGame game);

            Expect.Awards(events, (A, ScoreReason.Announcement, 5), (A, ScoreReason.AnnouncementBonus, 1));
            Assert.AreEqual(45, game.GetScore(A));
        }

        // ---------- H-9 Four of a kind as two announcements ----------

        /// <summary>
        /// QuadCountsAsTringla off. H announces a tringla of 3; A holds all four 5s and announces
        /// them (a ronda-class announcement worth 2). H drops the 1, A drops a 5: the window closes.
        /// H wins (tringla beats ronda): own 5 + 1 per opposing announcement. Default: A's quad is
        /// one announcement → 6. Alternative: two announcements → 7.
        /// </summary>
        [TestCase(false, 6)]
        [TestCase(true, 7)]
        public void H9_QuadAsTwoAnnouncements(bool setting, int expectedH)
        {
            var rules = new RulesConfig { QuadCountsAsTringla = false, QuadCountsAsTwoAnnouncements = setting };
            RondaGame game = new StateBuilder(rules)
                .Dealer(A).Turn(H).Deal(1, 1).AnnounceWindow(0)
                .Hand(H, C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Espadas, 3), C(Suit.Oros, 1))
                .Hand(A, C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Espadas, 5), C(Suit.Bastos, 5))
                .LastCapturer(H).PilesWithRest(16)
                .Build();
            Assert.IsTrue(game.Announce(H, 3));
            Assert.IsTrue(game.Announce(A, 5));

            game.PlayCard(H, C(Suit.Oros, 1));
            List<GameEvent> closing = game.PlayCard(A, C(Suit.Oros, 5)).ToList();

            Assert.AreEqual(H, closing.OfType<AnnouncementsResolvedEvent>().Single().Winner);
            Assert.AreEqual(expectedH, game.GetScore(H));
        }

        /// <summary>With QuadCountsAsTringla on (default), H-9 changes nothing: the quad is a tringla.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void H9_HasNoEffectWhileQuadsAreTringlas(bool setting)
        {
            RondaGame game = new StateBuilder(new RulesConfig { QuadCountsAsTwoAnnouncements = setting })
                .Dealer(A).Turn(H).Deal(1, 1).AnnounceWindow(0)
                .Hand(H, C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Espadas, 3), C(Suit.Oros, 1))
                .Hand(A, C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Espadas, 5), C(Suit.Bastos, 5))
                .LastCapturer(H).PilesWithRest(16)
                .Build();
            game.Announce(H, 3);
            game.Announce(A, 5);
            game.PlayCard(H, C(Suit.Oros, 1));
            game.PlayCard(A, C(Suit.Oros, 5));

            // Tringla of 3 vs tringla of 5: the lower tringla wins → H 5 + 1 = 6.
            Assert.AreEqual(6, game.GetScore(H));
        }

        // ---------- H-10 First dealer ----------

        [Test]
        public void H10_DefaultFirstDealer_IsRandomFromTheSeed()
        {
            var dealers = new HashSet<int>();
            for (int seed = 1; seed <= 20; seed++)
            {
                var a = new RondaGame(new RulesConfig(), seed);
                var b = new RondaGame(new RulesConfig(), seed);
                a.StartMatch();
                b.StartMatch();
                Assert.AreEqual(a.Dealer, b.Dealer, "same seed, same dealer");
                dealers.Add(a.Dealer);
            }
            Assert.AreEqual(2, dealers.Count, "both seats deal first across seeds");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void H10_FixedFirstDealer(int seat)
        {
            for (int seed = 1; seed <= 10; seed++)
            {
                var game = new RondaGame(new RulesConfig { FirstDealerSeat = seat }, seed);
                game.StartMatch();
                Assert.AreEqual(seat, game.Dealer);
            }
        }

        [Test]
        public void H10_StartMatchArgumentStillOverridesTheSetting()
        {
            var game = new RondaGame(new RulesConfig { FirstDealerSeat = 0 }, 3);
            game.StartMatch(firstDealer: 1);
            Assert.AreEqual(1, game.Dealer);
        }

        [Test]
        public void H10_InvalidSeatIsRefused()
        {
            var game = new RondaGame(new RulesConfig { FirstDealerSeat = 2 }, 3);
            Assert.Throws<ArgumentException>(() => game.StartMatch());
        }

        // ---------- Unsupported values can never be switched on ----------

        /// <summary>
        /// A value outside the implemented options (only reachable by casting a number) is refused
        /// when the match starts, for every enum setting — including the single-option ones
        /// (H-2, H-7, H-8) that the engine does not otherwise read.
        /// </summary>
        [Test]
        public void UndefinedSettingValues_AreRefusedAtMatchStart()
        {
            var bad = new Action<RulesConfig>[]
            {
                r => r.FirstPlayer = (FirstPlayer)7,
                r => r.AnnouncementTie = (AnnouncementTie)1,
                r => r.BothReachTarget = (BothReachTarget)9,
                r => r.PendingAnnouncementsAtMatchEnd = (PendingAnnouncementsAtMatchEnd)1,
                r => r.TringlaAwardCap = (TringlaAwardCap)1,
            };
            foreach (Action<RulesConfig> apply in bad)
            {
                var rules = new RulesConfig();
                apply(rules);
                Assert.Throws<ArgumentException>(() => new RondaGame(rules, 1).StartMatch());
            }
        }

        [Test]
        public void UndefinedSettingValues_AreRefusedByTheTestFixtureToo()
        {
            var rules = new RulesConfig { AnnouncementTie = (AnnouncementTie)1 };
            var builder = new StateBuilder(rules).Dealer(H).Turn(H).Deal(1, 5)
                .Hand(H, C(Suit.Copas, 12)).LastCapturer(A).PilesWithRest(18);
            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        // ---------- H-4 is applied consistently: engine, AI evaluation, capture preview ----------

        /// <summary>
        /// The table holds 12, 1, 2 (Oros) and 5, 6 (Copas). Nadia (A) holds the 12 of Copas and the
        /// 5 of Espadas. Without wrap, the 12 takes 1 card and the 5 takes 2 (5, 6), so she plays the 5.
        /// With wrap, the 12 takes 3 (12, 1, 2), so she plays the 12. The preview the human sees
        /// (PreviewCaptures) uses the same rule.
        /// </summary>
        [TestCase(false, 5, new[] { 12 })]
        [TestCase(true, 12, new[] { 12, 1, 2 })]
        public void H4_AiAndCapturePreview_FollowTheEngineRule(bool wrap, int expectedAiRank, int[] expectedPreview)
        {
            RondaGame game = new StateBuilder(new RulesConfig { ChainWrapsAfterTwelve = wrap })
                .Dealer(H).Turn(A).Deal(1, 4)
                .Hand(A, C(Suit.Copas, 12), C(Suit.Espadas, 5))
                .Hand(H, C(Suit.Bastos, 3))
                .Table(C(Suit.Oros, 12), C(Suit.Oros, 1), C(Suit.Oros, 2), C(Suit.Copas, 5), C(Suit.Copas, 6))
                .LastCapturer(H).PilesWithRest(16)
                .Build();

            CollectionAssert.AreEqual(expectedPreview, game.PreviewCaptures(C(Suit.Copas, 12)).Select(c => c.Rank));
            for (int seed = 0; seed < 10; seed++)
            {
                Assert.AreEqual(expectedAiRank, new Zellige.AI.SimpleAi(seed).ChooseCard(game, A).Rank);
            }
            // The engine captures exactly what the preview showed.
            CardPlayedEvent played = game.PlayCard(A, C(Suit.Copas, 12)).OfType<CardPlayedEvent>().Single();
            CollectionAssert.AreEqual(expectedPreview, played.Captured.Select(c => c.Rank));
        }
    }
}
