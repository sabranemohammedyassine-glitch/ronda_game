using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Zellige.AI;
using Zellige.Core;

namespace Zellige.Tests
{
    /// <summary>Helpers shared by the test classes.</summary>
    internal static class TestDecks
    {
        public static Card C(Suit suit, int rank) => new Card(suit, rank);

        /// <summary>
        /// A full 40-card order whose first deal gives <paramref name="firstPlayerHand"/> to the
        /// first player (the non-dealer) and <paramref name="dealerHand"/> to the dealer.
        /// The remaining cards follow in standard order.
        /// </summary>
        public static List<Card> Stacked(Card[] firstPlayerHand, Card[] dealerHand)
        {
            var order = new List<Card>(firstPlayerHand);
            order.AddRange(dealerHand);
            order.AddRange(Deck.CreateStandard().Where(card => !order.Contains(card)));
            return order;
        }

        /// <summary>Starts a match where seat 1 deals, so seat 0 plays first.</summary>
        public static RondaGame Start(Card[] seat0Hand, Card[] seat1Hand, RulesConfig rules = null)
        {
            var game = new RondaGame(rules ?? new RulesConfig(), 1);
            game.StartMatch(1, Stacked(seat0Hand, seat1Hand));
            return game;
        }

        public static List<Card> TableOf(params int[] ranks)
        {
            return ranks.Select(rank => new Card(Suit.Oros, rank)).ToList();
        }

        public static int TotalCards(RondaGame game)
        {
            return game.GetHand(0).Count + game.GetHand(1).Count + game.Table.Count
                   + game.GetPileCount(0) + game.GetPileCount(1) + game.StockCount;
        }
    }

    public class DeckTests
    {
        [Test]
        public void StandardDeck_Has40UniqueCards_WithoutEightsOrNines()
        {
            List<Card> deck = Deck.CreateStandard();

            Assert.AreEqual(40, deck.Count);
            Assert.AreEqual(40, deck.Distinct().Count());
            Assert.AreEqual(40, deck.Select(card => card.Id).Distinct().Count());
            Assert.IsFalse(deck.Any(card => card.Rank == 8 || card.Rank == 9));
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                Assert.AreEqual(10, deck.Count(card => card.Suit == suit));
            }
        }

        [Test]
        public void Shuffle_IsAPermutation_AndRepeatableWithTheSameSeed()
        {
            List<Card> a = Deck.CreateStandard();
            List<Card> b = Deck.CreateStandard();
            Deck.Shuffle(a, new Random(42));
            Deck.Shuffle(b, new Random(42));

            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreEquivalent(Deck.CreateStandard(), a);
            CollectionAssert.AreNotEqual(Deck.CreateStandard(), a);
        }

        [Test]
        public void RankOrder_JumpsFromSevenToTen_AndStopsAtTwelve()
        {
            Assert.IsTrue(Ranks.TryGetNext(7, out int afterSeven));
            Assert.AreEqual(10, afterSeven);
            Assert.IsFalse(Ranks.TryGetNext(12, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Card(Suit.Copas, 8));
        }
    }

    public class CaptureTests
    {
        private static int[] CapturedRanks(List<Card> table, int playedRank)
        {
            return CaptureRules.FindCaptures(table, new Card(Suit.Copas, playedRank)).Select(card => card.Rank).ToArray();
        }

        [Test] // E1
        public void MatchingRank_CapturesOnlyThatCard_WhenNextRankIsAbsent()
        {
            CollectionAssert.AreEqual(new[] { 7 }, CapturedRanks(TestDecks.TableOf(4, 7, 11), 7));
        }

        [Test] // E2
        public void Chain_ContinuesUpward_UntilFirstMissingRank()
        {
            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, CapturedRanks(TestDecks.TableOf(3, 4, 5, 10), 3));
        }

        [Test] // E3
        public void Chain_JumpsFromSevenToTen()
        {
            CollectionAssert.AreEqual(new[] { 7, 10, 11 }, CapturedRanks(TestDecks.TableOf(2, 7, 10, 11), 7));
        }

        [Test]
        public void Chain_DoesNotWrapFromTwelveToOne()
        {
            CollectionAssert.AreEqual(new[] { 12 }, CapturedRanks(TestDecks.TableOf(1, 2, 12), 12));
        }

        [Test] // E5
        public void NoMatchingRank_CapturesNothing()
        {
            CollectionAssert.IsEmpty(CapturedRanks(TestDecks.TableOf(4, 7), 2));
        }
    }

    public class EngineTests
    {
        private static Card C(Suit suit, int rank) => TestDecks.C(suit, rank);

        // Hands with no pairs, used when announcements are not the subject of the test.
        private static readonly Card[] PlainHand0 = { C(Suit.Oros, 10), C(Suit.Oros, 5), C(Suit.Oros, 1), C(Suit.Oros, 2) };
        private static readonly Card[] PlainHand1 = { C(Suit.Copas, 10), C(Suit.Copas, 3), C(Suit.Copas, 6), C(Suit.Copas, 7) };

        [Test]
        public void StartMatch_DealsFourCardsEach_AndNonDealerPlaysFirst()
        {
            var game = new RondaGame(new RulesConfig(), 7);
            IReadOnlyList<GameEvent> events = game.StartMatch(firstDealer: 1);

            Assert.AreEqual(4, game.GetHand(0).Count);
            Assert.AreEqual(4, game.GetHand(1).Count);
            Assert.AreEqual(32, game.StockCount);
            Assert.AreEqual(0, game.Table.Count);
            Assert.AreEqual(0, game.CurrentPlayer);
            Assert.AreEqual(40, TestDecks.TotalCards(game));
            Assert.IsInstanceOf<DeckStartedEvent>(events[0]);
            Assert.IsInstanceOf<DealStartedEvent>(events[1]);
            Assert.IsInstanceOf<TurnStartedEvent>(events[2]);
        }

        [Test]
        public void SameSeed_GivesTheSameDeal()
        {
            var a = new RondaGame(new RulesConfig(), 123);
            var b = new RondaGame(new RulesConfig(), 123);
            a.StartMatch();
            b.StartMatch();

            Assert.AreEqual(a.Dealer, b.Dealer);
            CollectionAssert.AreEqual(a.GetHand(0), b.GetHand(0));
            CollectionAssert.AreEqual(a.GetHand(1), b.GetHand(1));
        }

        [Test]
        public void IllegalPlays_AreRejected_AndChangeNothing()
        {
            RondaGame game = TestDecks.Start(PlainHand0, PlainHand1);

            Assert.IsFalse(game.CanPlay(1, PlainHand1[0]), "not seat 1's turn");
            Assert.Throws<InvalidOperationException>(() => game.PlayCard(1, PlainHand1[0]));
            Assert.IsFalse(game.CanPlay(0, PlainHand1[0]), "card is not in seat 0's hand");
            Assert.Throws<InvalidOperationException>(() => game.PlayCard(0, PlainHand1[0]));

            Assert.AreEqual(4, game.GetHand(0).Count);
            Assert.AreEqual(4, game.GetHand(1).Count);
            Assert.AreEqual(0, game.CurrentPlayer);
        }

        [Test]
        public void Drop_PutsCardOnTable_AndPassesTheTurn()
        {
            RondaGame game = TestDecks.Start(PlainHand0, PlainHand1);
            game.PlayCard(0, C(Suit.Oros, 5));

            CollectionAssert.AreEqual(new[] { C(Suit.Oros, 5) }, game.Table);
            Assert.AreEqual(1, game.CurrentPlayer);
            Assert.AreEqual(3, game.GetHand(0).Count);
        }

        [Test] // E8
        public void CapturingTheCardJustDropped_ScoresDerba_AndMissaWhenTableIsCleared()
        {
            RondaGame game = TestDecks.Start(PlainHand0, PlainHand1);
            game.PlayCard(0, C(Suit.Oros, 10));
            IReadOnlyList<GameEvent> events = game.PlayCard(1, C(Suit.Copas, 10));

            Assert.AreEqual(2, game.GetScore(1));
            Assert.AreEqual(0, game.GetScore(0));
            Assert.AreEqual(2, game.GetPileCount(1));
            Assert.AreEqual(0, game.Table.Count);
            ScoreReason[] reasons = events.OfType<ScoreAwardedEvent>().Select(e => e.Reason).ToArray();
            CollectionAssert.AreEqual(new[] { ScoreReason.Derba, ScoreReason.Missa }, reasons);
        }

        [Test] // E7
        public void CapturingAnOlderDrop_IsNotDerba()
        {
            RondaGame game = TestDecks.Start(PlainHand0, PlainHand1);
            game.PlayCard(0, C(Suit.Oros, 10)); // seat 0 drops 10
            game.PlayCard(1, C(Suit.Copas, 3)); // seat 1 drops 3
            game.PlayCard(0, C(Suit.Oros, 5));  // seat 0 drops 5
            game.PlayCard(1, C(Suit.Copas, 10)); // takes the old 10; table keeps 3 and 5

            Assert.AreEqual(0, game.GetScore(1));
            Assert.AreEqual(2, game.GetPileCount(1));
            Assert.AreEqual(2, game.Table.Count);
        }

        [Test] // E6
        public void Derba_WithoutClearingTable_ScoresOne()
        {
            RondaGame game = TestDecks.Start(PlainHand0, PlainHand1);
            game.PlayCard(0, C(Suit.Oros, 1));   // drop 1
            game.PlayCard(1, C(Suit.Copas, 3));  // drop 3
            game.PlayCard(0, C(Suit.Oros, 10));  // drop 10
            game.PlayCard(1, C(Suit.Copas, 10)); // derba, table keeps 1 and 3

            Assert.AreEqual(1, game.GetScore(1));
        }

        [Test]
        public void MustCapture_LimitsLegalCardsToCapturingOnes()
        {
            var rules = new RulesConfig { MustCapture = true };
            RondaGame game = TestDecks.Start(PlainHand0, PlainHand1, rules);
            game.PlayCard(0, C(Suit.Oros, 10));

            CollectionAssert.AreEqual(new[] { C(Suit.Copas, 10) }, game.GetLegalCards(1));
        }

        [Test]
        public void NewDeal_StartsWhenBothHandsAreEmpty_AndTablePersists()
        {
            RondaGame game = TestDecks.Start(PlainHand0, PlainHand1);
            for (int i = 0; i < 4; i++)
            {
                game.PlayCard(0, game.GetHand(0)[0]);
                game.PlayCard(1, game.GetHand(1)[0]);
            }

            Assert.AreEqual(2, game.DealNumber);
            Assert.AreEqual(4, game.GetHand(0).Count);
            Assert.AreEqual(4, game.GetHand(1).Count);
            Assert.AreEqual(24, game.StockCount);
            Assert.AreEqual(0, game.CurrentPlayer);
            Assert.AreEqual(40, TestDecks.TotalCards(game));
        }
    }

    public class AnnouncementTests
    {
        private static Card C(Suit suit, int rank) => TestDecks.C(suit, rank);

        /// <summary>Both players drop one harmless card, which closes the announcement window.</summary>
        private static void CloseWindow(RondaGame game, Card seat0Drop, Card seat1Drop)
        {
            game.PlayCard(0, seat0Drop);
            game.PlayCard(1, seat1Drop);
        }

        [Test] // E12
        public void LoneRonda_ScoresOne_WhenWindowCloses()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Oros, 2), C(Suit.Oros, 11) },
                new[] { C(Suit.Copas, 3), C(Suit.Copas, 6), C(Suit.Copas, 7), C(Suit.Copas, 12) });

            Assert.IsTrue(game.Announce(0, 5));
            Assert.AreEqual(0, game.GetScore(0), "sealed until the window closes");
            CloseWindow(game, C(Suit.Oros, 2), C(Suit.Copas, 12));

            Assert.AreEqual(1, game.GetScore(0));
            Assert.IsFalse(game.IsAnnounceWindowOpen);
        }

        [Test] // E9
        public void TringlaBeatsRonda_AndCollectsOneForTheOpposingAnnouncement()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Oros, 2), C(Suit.Oros, 11) },
                new[] { C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Espadas, 3), C(Suit.Copas, 12) });

            Assert.IsTrue(game.Announce(0, 5));
            Assert.IsTrue(game.Announce(1, 3));
            CloseWindow(game, C(Suit.Oros, 2), C(Suit.Copas, 12));

            Assert.AreEqual(0, game.GetScore(0));
            Assert.AreEqual(6, game.GetScore(1));
        }

        [Test] // E10
        public void HigherRondaWins()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 7), C(Suit.Copas, 7), C(Suit.Oros, 1), C(Suit.Oros, 2) },
                new[] { C(Suit.Oros, 4), C(Suit.Copas, 4), C(Suit.Copas, 5), C(Suit.Copas, 6) });

            game.Announce(0, 7);
            game.Announce(1, 4);
            CloseWindow(game, C(Suit.Oros, 1), C(Suit.Copas, 5));

            Assert.AreEqual(2, game.GetScore(0));
            Assert.AreEqual(0, game.GetScore(1));
        }

        [Test] // E11
        public void LowerTringlaWins()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Espadas, 3), C(Suit.Oros, 1) },
                new[] { C(Suit.Oros, 6), C(Suit.Copas, 6), C(Suit.Espadas, 6), C(Suit.Copas, 12) });

            game.Announce(0, 3);
            game.Announce(1, 6);
            CloseWindow(game, C(Suit.Oros, 1), C(Suit.Copas, 12));

            Assert.AreEqual(6, game.GetScore(0));
            Assert.AreEqual(0, game.GetScore(1));
        }

        [Test] // E13
        public void EqualRondas_ScoreNothing()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Oros, 1), C(Suit.Oros, 2) },
                new[] { C(Suit.Espadas, 5), C(Suit.Bastos, 5), C(Suit.Copas, 3), C(Suit.Copas, 4) });

            game.Announce(0, 5);
            game.Announce(1, 5);
            CloseWindow(game, C(Suit.Oros, 1), C(Suit.Copas, 3));

            Assert.AreEqual(0, game.GetScore(0));
            Assert.AreEqual(0, game.GetScore(1));
        }

        [Test] // E14
        public void TwoRondas_ScoreTwoPlusBonus()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Oros, 10), C(Suit.Copas, 10) },
                new[] { C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Copas, 1), C(Suit.Copas, 2) });

            Assert.IsTrue(game.Announce(0, 3));
            Assert.IsTrue(game.Announce(0, 10));
            Assert.IsTrue(game.Announce(1, 5));
            game.PlayCard(0, C(Suit.Oros, 3));
            game.PlayCard(1, C(Suit.Copas, 1));

            Assert.AreEqual(3, game.GetScore(0));
        }

        [Test]
        public void Announce_IsRefused_WithoutAPair_Twice_OrAfterPlayingTheRank()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Oros, 2), C(Suit.Oros, 11) },
                new[] { C(Suit.Oros, 4), C(Suit.Copas, 4), C(Suit.Copas, 7), C(Suit.Copas, 12) });

            Assert.IsFalse(game.Announce(0, 2), "no pair of 2s");
            Assert.IsTrue(game.Announce(0, 5));
            Assert.IsFalse(game.Announce(0, 5), "already announced");

            game.PlayCard(0, C(Suit.Oros, 2));
            game.PlayCard(1, C(Suit.Oros, 4)); // plays one of the pair without announcing
            Assert.IsFalse(game.Announce(1, 4), "window closed and the rank was played");
            Assert.AreEqual(0, game.GetScore(1));
        }

        [Test] // E16 (scaled down: target 3 instead of 41)
        public void RondaPoints_CannotReachTheTarget()
        {
            var rules = new RulesConfig { TargetScore = 3 };
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Oros, 10), C(Suit.Copas, 10) },
                new[] { C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Copas, 1), C(Suit.Copas, 2) },
                rules);

            game.Announce(0, 3);
            game.Announce(0, 10);
            game.Announce(1, 5);
            game.PlayCard(0, C(Suit.Oros, 3));
            game.PlayCard(1, C(Suit.Copas, 1));

            Assert.AreEqual(2, game.GetScore(0), "worth 3 but capped at target - 1");
            Assert.IsFalse(game.IsMatchOver);
        }

        [Test] // E17 (scaled down: target 5)
        public void TringlaPoints_CanWinTheMatch()
        {
            var rules = new RulesConfig { TargetScore = 5 };
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 3), C(Suit.Copas, 3), C(Suit.Espadas, 3), C(Suit.Oros, 1) },
                new[] { C(Suit.Copas, 5), C(Suit.Copas, 6), C(Suit.Copas, 7), C(Suit.Copas, 12) },
                rules);

            game.Announce(0, 3);
            game.PlayCard(0, C(Suit.Oros, 1));
            IReadOnlyList<GameEvent> events = game.PlayCard(1, C(Suit.Copas, 12));

            Assert.IsTrue(game.IsMatchOver);
            Assert.AreEqual(0, game.Winner);
            Assert.IsInstanceOf<MatchEndedEvent>(events[events.Count - 1]);
            Assert.IsFalse(game.CanPlay(0, C(Suit.Oros, 3)), "no plays after the match ends");
        }
    }

    /// <summary>Whole matches played by machines, checking invariants on every step.</summary>
    public class FullMatchTests
    {
        private static void PlayMatch(int seed, Func<RondaGame, int, Card> chooseCard, bool announce)
        {
            var rules = new RulesConfig();
            var game = new RondaGame(rules, seed);
            var allEvents = new List<GameEvent>(game.StartMatch());
            int[] trackedScores = new int[2];
            int plays = 0;

            while (!game.IsMatchOver)
            {
                int seat = game.CurrentPlayer;
                if (announce)
                {
                    foreach (int rank in game.GetAnnounceableRanks(seat)) Assert.IsTrue(game.Announce(seat, rank));
                }
                Card card = chooseCard(game, seat);
                Assert.IsTrue(game.CanPlay(seat, card));
                allEvents.AddRange(game.PlayCard(seat, card));

                Assert.AreEqual(40, TestDecks.TotalCards(game), "cards must be conserved");
                Assert.LessOrEqual(game.Table.Select(c => c.Rank).Distinct().Count(), 10);
                Assert.AreEqual(game.Table.Count, game.Table.Select(c => c.Rank).Distinct().Count(),
                    "at most one card of each rank on the table");
                Assert.Less(++plays, 20000, "match must terminate");
            }

            // The score shown by the engine equals the sum of the awards it reported.
            foreach (ScoreAwardedEvent award in allEvents.OfType<ScoreAwardedEvent>())
            {
                trackedScores[award.Seat] += award.Points;
                Assert.AreEqual(trackedScores[award.Seat], award.NewScore);
            }
            Assert.AreEqual(trackedScores[0], game.GetScore(0));
            Assert.AreEqual(trackedScores[1], game.GetScore(1));

            Assert.GreaterOrEqual(game.GetScore(game.Winner), rules.TargetScore);
            Assert.Greater(game.GetScore(game.Winner), game.GetScore(1 - game.Winner));
            Assert.IsInstanceOf<MatchEndedEvent>(allEvents[allEvents.Count - 1]);

            CheckTallies(allEvents, rules);
        }

        /// <summary>Every end-of-deck tally must follow the dealer rule and the over-20 rule.</summary>
        private static void CheckTallies(List<GameEvent> events, RulesConfig rules)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (!(events[i] is DeckTalliedEvent tally)) continue;

                Assert.AreEqual(40, tally.PileCounts[0] + tally.PileCounts[1]);
                var awards = new List<ScoreAwardedEvent>();
                for (int j = i + 1; j < events.Count && events[j] is ScoreAwardedEvent award; j++) awards.Add(award);

                bool expectPenalty = tally.LastCapturer != tally.Dealer;
                ScoreAwardedEvent penalty = awards.SingleOrDefault(a => a.Reason == ScoreReason.DealerPenalty);
                Assert.AreEqual(expectPenalty, penalty != null);
                if (penalty != null)
                {
                    Assert.AreEqual(1 - tally.Dealer, penalty.Seat);
                    Assert.AreEqual(rules.DealerPenaltyPoints, penalty.Points);
                }

                for (int seat = 0; seat < 2; seat++)
                {
                    int expected = Math.Max(0, tally.PileCounts[seat] - rules.PileThreshold);
                    int actual = awards.Where(a => a.Reason == ScoreReason.CardsOverThreshold && a.Seat == seat).Sum(a => a.Points);
                    Assert.AreEqual(expected, actual);
                }
            }
        }

        [Test]
        public void RandomPlayers_AlwaysFinishAValidMatch([Range(1, 40)] int seed)
        {
            var rng = new Random(seed * 31);
            PlayMatch(seed, (game, seat) =>
            {
                List<Card> legal = game.GetLegalCards(seat);
                return legal[rng.Next(legal.Count)];
            }, announce: seed % 2 == 0);
        }

        [Test]
        public void AiPlayers_AlwaysFinishAValidMatch([Range(1, 40)] int seed)
        {
            var ais = new[] { new SimpleAi(seed), new SimpleAi(seed + 1000) };
            PlayMatch(seed, (game, seat) => ais[seat].ChooseCard(game, seat), announce: true);
        }
    }

    public class AiTests
    {
        private static Card C(Suit suit, int rank) => TestDecks.C(suit, rank);

        [Test]
        public void Ai_PrefersACapture_OverADrop()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 10), C(Suit.Oros, 5), C(Suit.Oros, 1), C(Suit.Oros, 2) },
                new[] { C(Suit.Copas, 5), C(Suit.Copas, 3), C(Suit.Copas, 6), C(Suit.Copas, 12) });
            game.PlayCard(0, C(Suit.Oros, 5));

            for (int seed = 0; seed < 20; seed++)
            {
                Assert.AreEqual(C(Suit.Copas, 5), new SimpleAi(seed).ChooseCard(game, 1));
            }
        }

        [Test]
        public void Ai_ChoosesALegalCard_AndDoesNotChangeTheGame()
        {
            var game = new RondaGame(new RulesConfig(), 5);
            game.StartMatch();
            int seat = game.CurrentPlayer;
            var handBefore = new List<Card>(game.GetHand(seat));

            Card choice = new SimpleAi(9).ChooseCard(game, seat);

            Assert.IsTrue(game.CanPlay(seat, choice));
            CollectionAssert.AreEqual(handBefore, game.GetHand(seat));
            Assert.AreEqual(seat, game.CurrentPlayer);
            Assert.AreEqual(0, game.Table.Count);
        }

        [Test]
        public void Ai_AnnouncesItsPairs()
        {
            RondaGame game = TestDecks.Start(
                new[] { C(Suit.Oros, 5), C(Suit.Copas, 5), C(Suit.Oros, 2), C(Suit.Oros, 11) },
                new[] { C(Suit.Copas, 3), C(Suit.Copas, 6), C(Suit.Copas, 7), C(Suit.Copas, 12) });

            CollectionAssert.AreEqual(new[] { 5 }, new SimpleAi(1).ChooseAnnouncements(game, 0));
            CollectionAssert.IsEmpty(new SimpleAi(1).ChooseAnnouncements(game, 1));
        }
    }
}
