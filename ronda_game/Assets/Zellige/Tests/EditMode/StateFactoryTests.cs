using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Zellige.Core;

namespace Zellige.Tests
{
    /// <summary>
    /// Phase C (plan A-5): the test-only state factory. These tests check that it builds exactly
    /// the state asked for, refuses impossible states, stays invisible to the game, and agrees
    /// with the full scripted-deck tests. They also close the previously partial case ED-03.
    /// </summary>
    public class StateFactoryTests
    {
        private const int H = 0;
        private const int A = 1;

        private static Card C(Suit suit, int rank) => new Card(suit, rank);

        /// <summary>
        /// ED-03 with the exact numbers observed in the reference (A039 → A041, X-14; the
        /// decomposition is INFERRED): the dealer made the last capture; 23 captured + 3 leftover
        /// = 26 → +6; the other player has 14; no +5.
        /// Start: deck 1, deal 5, dealer H to play the last card of the deck. H holds the 12 of
        /// Copas; A's hand is empty; the table holds 2, 6, 10 and 12 of Oros; A's previous play was
        /// the drop of the 10 of Oros; A made the last capture so far; piles H 21 / A 14;
        /// scores H 10 / A 8.
        /// Play: H's 12 takes the 12 (no chain after 12; not a Derba — A dropped a 10; 2, 6, 10 stay,
        /// so no Missa). H pile 23, last capture H (the dealer).
        /// Expected tally: leftover {2, 6, 10} → H (26); no dealer point; H +6; A +0.
        /// Final: H 16, A 8. A second deck starts, dealt by A.
        /// </summary>
        [Test]
        public void ED03_DealerTookLast_TwentyThreePlusThreeLeftover_ScoresSix()
        {
            RondaGame game = new StateBuilder()
                .Dealer(H).Turn(H).Deal(1, 5)
                .Scores(10, 8)
                .Hand(H, C(Suit.Copas, 12))
                .Table(C(Suit.Oros, 2), C(Suit.Oros, 6), C(Suit.Oros, 10), C(Suit.Oros, 12))
                .LastPlay(A, C(Suit.Oros, 10), wasDrop: true)
                .LastCapturer(A)
                .PilesWithRest(seat0Pile: 21)
                .Build();
            Expect.Piles(game, 21, 14, "at the start");

            List<GameEvent> events = game.PlayCard(H, C(Suit.Copas, 12)).ToList();

            CardPlayedEvent played = events.OfType<CardPlayedEvent>().Single();
            Expect.TableRanks(played.Captured, 12);
            DeckTalliedEvent tally = events.OfType<DeckTalliedEvent>().Single();
            Assert.AreEqual(H, tally.Dealer);
            Assert.AreEqual(H, tally.LastCapturer);
            Expect.TableRanks(tally.Leftover, 2, 6, 10);
            Assert.AreEqual(H, tally.LeftoverTo);
            CollectionAssert.AreEqual(new[] { 26, 14 }, tally.PileCounts);
            Expect.Awards(events, (H, ScoreReason.CardsOverThreshold, 6));
            Expect.Scores(game, 16, 8, "after the tally");
            Assert.AreEqual(2, game.DeckRound);
            Assert.AreEqual(A, game.Dealer);
        }

        /// <summary>
        /// The factory agrees with the full scripted deck: ED-02's last card, set up directly,
        /// gives the same tally, the same points and the same next deck as playing all 40 cards.
        /// </summary>
        [Test]
        public void Factory_ReproducesTheFullDeckResult_OfED02()
        {
            ScriptedDeck deck = EndOfDeckTests.Ed02();
            RondaGame full = deck.Start();
            deck.PlayDeals(full, 4);
            deck.PlayRange(full, 5, 1, 7);
            List<GameEvent> fullEvents = deck.PlayRange(full, 5, 8, 8);

            Card hLast = C(Suit.Bastos, 12);
            RondaGame direct = new StateBuilder()
                .Dealer(H).Turn(H).Deal(1, 5)
                .Scores(4, 17)
                .Hand(H, hLast)
                .LastPlay(A, C(Suit.Espadas, 12), wasDrop: false)
                .LastCapturer(A)
                .PilesWithRest(seat0Pile: 8)
                .Build();
            Expect.Piles(direct, 8, 31, "before the last card");
            List<GameEvent> directEvents = direct.PlayCard(H, hLast).ToList();

            DeckTalliedEvent a = fullEvents.OfType<DeckTalliedEvent>().Single();
            DeckTalliedEvent b = directEvents.OfType<DeckTalliedEvent>().Single();
            CollectionAssert.AreEqual(a.PileCounts, b.PileCounts);
            Assert.AreEqual(a.LastCapturer, b.LastCapturer);
            Assert.AreEqual(a.LeftoverTo, b.LeftoverTo);
            Assert.AreEqual(a.Dealer, b.Dealer);
            CollectionAssert.AreEquivalent(a.Leftover.Select(c => c.Rank), b.Leftover.Select(c => c.Rank));
            CollectionAssert.AreEquivalent(
                fullEvents.OfType<ScoreAwardedEvent>().Select(e => (e.Seat, e.Reason, e.Points, e.NewScore)),
                directEvents.OfType<ScoreAwardedEvent>().Select(e => (e.Seat, e.Reason, e.Points, e.NewScore)));
            Assert.AreEqual(full.GetScore(H), direct.GetScore(H));
            Assert.AreEqual(full.GetScore(A), direct.GetScore(A));
            Assert.AreEqual(full.DeckRound, direct.DeckRound);
            Assert.AreEqual(full.Dealer, direct.Dealer);
        }

        [Test]
        public void Factory_BuildsExactlyTheStateAskedFor()
        {
            RondaGame game = new StateBuilder()
                .Dealer(A).Turn(H).Deal(2, 3)
                .Scores(12, 30)
                .Hand(H, C(Suit.Oros, 1), C(Suit.Oros, 2))
                .Hand(A, C(Suit.Copas, 1), C(Suit.Copas, 2))
                .Table(C(Suit.Espadas, 7))
                .Stock(C(Suit.Bastos, 1), C(Suit.Bastos, 2), C(Suit.Bastos, 3), C(Suit.Bastos, 4),
                       C(Suit.Bastos, 5), C(Suit.Bastos, 6), C(Suit.Bastos, 7), C(Suit.Bastos, 10))
                .LastCapturer(A)
                .PilesWithRest(seat0Pile: 10)
                .Build();

            Assert.AreEqual(A, game.Dealer);
            Assert.AreEqual(H, game.CurrentPlayer);
            Assert.AreEqual(2, game.DeckRound);
            Assert.AreEqual(3, game.DealNumber);
            Expect.Scores(game, 12, 30, "as set");
            Expect.Piles(game, 10, 17, "as set"); // 40 − 4 in hands − 1 on the table − 8 in stock − 10
            CollectionAssert.AreEqual(new[] { C(Suit.Oros, 1), C(Suit.Oros, 2) }, game.GetHand(H));
            CollectionAssert.AreEqual(new[] { C(Suit.Espadas, 7) }, game.Table);
            Assert.AreEqual(8, game.StockCount);
            Assert.IsFalse(game.IsMatchOver);
            Assert.IsFalse(game.IsAnnounceWindowOpen);
            Assert.IsTrue(game.CanPlay(H, C(Suit.Oros, 1)));
            Assert.IsFalse(game.CanPlay(A, C(Suit.Copas, 1)), "not A's turn");
        }

        // ---------- Impossible states are refused ----------

        private static StateBuilder Valid()
        {
            return new StateBuilder()
                .Dealer(H).Turn(H).Deal(1, 5)
                .Hand(H, C(Suit.Copas, 12))
                .Table(C(Suit.Oros, 2))
                .LastCapturer(A)
                .PilesWithRest(seat0Pile: 18);
        }

        [Test]
        public void Factory_AcceptsTheValidBaseState()
        {
            Assert.DoesNotThrow(() => Valid().Build());
        }

        [Test]
        public void Factory_Refuses_MissingOrDuplicatedCards()
        {
            StateBuilder b = Valid();
            b.Setup.Piles[1].RemoveAt(0);
            Assert.Throws<ArgumentException>(() => b.Build(), "39 cards");

            b = Valid();
            b.Setup.Piles[1].Add(C(Suit.Copas, 12)); // also in H's hand
            Assert.Throws<ArgumentException>(() => b.Build(), "a card twice");
        }

        [Test]
        public void Factory_Refuses_TwoCardsOfOneRankOnTheTable()
        {
            StateBuilder b = new StateBuilder()
                .Dealer(H).Turn(H).Deal(1, 5)
                .Hand(H, C(Suit.Copas, 12))
                .Table(C(Suit.Oros, 2), C(Suit.Copas, 2))
                .LastCapturer(A)
                .PilesWithRest(seat0Pile: 18);
            Assert.Throws<ArgumentException>(() => b.Build());
        }

        [Test]
        public void Factory_Refuses_HandsThatCannotAlternate()
        {
            StateBuilder b = new StateBuilder()
                .Dealer(H).Turn(A).Deal(1, 5) // A to move with an empty hand
                .Hand(H, C(Suit.Copas, 12))
                .LastCapturer(A)
                .PilesWithRest(seat0Pile: 19);
            Assert.Throws<ArgumentException>(() => b.Build());
        }

        [Test]
        public void Factory_Refuses_AFinishedMatch()
        {
            StateBuilder b = Valid().Scores(41, 3);
            Assert.Throws<ArgumentException>(() => b.Build());
        }

        [Test]
        public void Factory_Refuses_InconsistentCaptureHistory()
        {
            StateBuilder b = Valid().LastCapturer(-1); // piles are not empty
            Assert.Throws<ArgumentException>(() => b.Build());

            b = Valid().LastPlay(A, C(Suit.Copas, 5), wasDrop: true); // that card is not on the table
            Assert.Throws<ArgumentException>(() => b.Build());

            // The previous play was a capture by H, but A is named as the last capturer.
            b = new StateBuilder()
                .Dealer(H).Turn(H).Deal(1, 5)
                .Hand(H, C(Suit.Copas, 12))
                .LastPlay(H, C(Suit.Espadas, 4), wasDrop: false)
                .LastCapturer(A)
                .PilesWithRest(seat0Pile: 18);
            Assert.Throws<ArgumentException>(() => b.Build());

            b = Valid().Deal(1, 2).LastPlay(A, C(Suit.Oros, 2), wasDrop: true, deal: 3); // a later deal
            Assert.Throws<ArgumentException>(() => b.Build());
        }

        [Test]
        public void Factory_Refuses_AnAnnouncementWindowPastItsClosingPlay()
        {
            StateBuilder b = Valid().AnnounceWindow(playsSoFar: 2);
            Assert.Throws<ArgumentException>(() => b.Build());
        }

        // ---------- Isolation from the game ----------

        [Test]
        public void Factory_IsNotPartOfThePublicApi()
        {
            MethodInfo factory = typeof(RondaGame).GetMethod("FromSetup", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(factory, "the factory exists");
            Assert.IsTrue(factory.IsAssembly, "and is internal");
            Assert.IsNull(typeof(RondaGame).GetMethod("FromSetup", BindingFlags.Static | BindingFlags.Public));
            Assert.IsFalse(typeof(GameSetup).IsVisible, "GameSetup is internal");
            Assert.IsFalse(typeof(PreviousPlay).IsVisible, "PreviousPlay is internal");
        }
    }
}
