using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Zellige.Core;

namespace Zellige.Tests
{
    /// <summary>
    /// A whole first deck written as a script: for each of the five deals, the ranks the
    /// leader (non-dealer) and the dealer play, in play order. Suits are handed out
    /// automatically (each rank's 1st card is Oros, 2nd Copas, 3rd Espadas, 4th Bastos).
    /// The deck order is built so the engine deals exactly these hands (leader's 4 cards,
    /// then the dealer's 4, per deal). Only the first deck can be fixed through the public
    /// API, so every end-of-deck fixture is a complete scripted deck.
    ///
    /// Setup assumptions (structural, PROPOSED rules — not what these tests verify):
    /// the table is empty at the start of a deck (M-07), the non-dealer leads every deal
    /// (M-11) and the table persists between deals (M-08). <see cref="PlayRange"/>
    /// asserts the turn order before every play, so a change there fails loudly instead
    /// of silently changing a fixture.
    /// </summary>
    internal sealed class ScriptedDeck
    {
        private static readonly Suit[] SuitOrder = { Suit.Oros, Suit.Copas, Suit.Espadas, Suit.Bastos };

        private readonly Dictionary<int, int> suitsUsed = new Dictionary<int, int>();
        private readonly List<Card[]> leaderCards = new List<Card[]>();
        private readonly List<Card[]> dealerCards = new List<Card[]>();
        private readonly List<int[]> dealerAnnouncements = new List<int[]>();

        public ScriptedDeck(int dealer)
        {
            Dealer = dealer;
        }

        public int Dealer { get; }
        public int Leader => 1 - Dealer;

        /// <summary>Adds a deal. The dealer announces the given ranks before its first card.</summary>
        public ScriptedDeck Deal(int[] leaderPlays, int[] dealerPlays, params int[] dealerAnnounces)
        {
            Assert.AreEqual(4, leaderPlays.Length);
            Assert.AreEqual(4, dealerPlays.Length);
            leaderCards.Add(leaderPlays.Select(NextCard).ToArray());
            dealerCards.Add(dealerPlays.Select(NextCard).ToArray());
            dealerAnnouncements.Add(dealerAnnounces);
            return this;
        }

        /// <summary>The card played at a play number (1-8) of a deal (1-5): odd = leader, even = dealer.</summary>
        public Card CardAt(int deal, int play)
        {
            return play % 2 == 1 ? leaderCards[deal - 1][(play - 1) / 2] : dealerCards[deal - 1][play / 2 - 1];
        }

        public RondaGame Start(RulesConfig rules = null)
        {
            Assert.AreEqual(5, leaderCards.Count, "a fixture must script all five deals");
            var order = new List<Card>();
            for (int i = 0; i < leaderCards.Count; i++)
            {
                order.AddRange(leaderCards[i]);
                order.AddRange(dealerCards[i]);
            }
            var game = new RondaGame(rules ?? new RulesConfig(), 1);
            game.StartMatch(Dealer, order);
            return game;
        }

        /// <summary>Plays plays <paramref name="first"/> to <paramref name="last"/> of one deal; returns their events.</summary>
        public List<GameEvent> PlayRange(RondaGame game, int deal, int first = 1, int last = 8)
        {
            var events = new List<GameEvent>();
            for (int play = first; play <= last; play++)
            {
                int seat = play % 2 == 1 ? Leader : Dealer;
                Assert.AreEqual(seat, game.CurrentPlayer,
                    "fixture assumes the non-dealer leads every deal (deal " + deal + ", play " + play + ")");
                if (play == 2)
                {
                    foreach (int rank in dealerAnnouncements[deal - 1])
                    {
                        Assert.IsTrue(game.Announce(Dealer, rank), "dealer must be able to announce " + rank);
                    }
                }
                events.AddRange(game.PlayCard(seat, CardAt(deal, play)));
            }
            return events;
        }

        public List<GameEvent> PlayDeals(RondaGame game, int deals)
        {
            var events = new List<GameEvent>();
            for (int deal = 1; deal <= deals; deal++) events.AddRange(PlayRange(game, deal));
            return events;
        }

        private Card NextCard(int rank)
        {
            suitsUsed.TryGetValue(rank, out int used);
            Assert.Less(used, 4, "rank " + rank + " used more than four times");
            suitsUsed[rank] = used + 1;
            return new Card(SuitOrder[used], rank);
        }
    }

    /// <summary>Shared assertions for the scripted-deck tests.</summary>
    internal static class Expect
    {
        public static void Scores(RondaGame game, int seat0, int seat1, string when)
        {
            Assert.AreEqual(seat0, game.GetScore(0), "seat 0 score " + when);
            Assert.AreEqual(seat1, game.GetScore(1), "seat 1 score " + when);
        }

        public static void Piles(RondaGame game, int seat0, int seat1, string when)
        {
            Assert.AreEqual(seat0, game.GetPileCount(0), "seat 0 pile " + when);
            Assert.AreEqual(seat1, game.GetPileCount(1), "seat 1 pile " + when);
        }

        /// <summary>The points awarded by a group of events, as (seat, reason, points), in any order.</summary>
        public static void Awards(IEnumerable<GameEvent> events, params (int seat, ScoreReason reason, int points)[] expected)
        {
            var actual = events.OfType<ScoreAwardedEvent>().Select(e => (e.Seat, e.Reason, e.Points)).ToList();
            CollectionAssert.AreEquivalent(expected, actual, "score events");
        }

        public static void TableRanks(IEnumerable<Card> cards, params int[] ranks)
        {
            CollectionAssert.AreEquivalent(ranks, cards.Select(c => c.Rank).ToArray());
        }
    }

    /// <summary>
    /// Deterministic end-of-deck tests (QA checklist ED-01…ED-07). Expected values were worked
    /// out by hand from the confirmed rules and cross-checked with an independent model of the
    /// documented rules (not with the engine):
    ///   M-42 leftover cards go to the last capturer (tutorial; A039→A041 fits)
    ///   M-43 dealer not last to take → the other side +5 (live play, A021)
    ///   M-44 one point per captured card over 20 (live play, A021: 32 cards → +12)
    /// No fixture depends on an unverified rule: no Missa on the final play of the deck
    /// (M-21), no Derba across a deal boundary (M-23), no announcement, no both-reach-41 case.
    /// Event order inside the tally is not asserted (M-45 is unverified).
    ///
    /// Filler deals 1-4 use three patterns (plays: leader, dealer, leader, ...):
    ///   L(a,b,c,d) leader drops a; dealer drops b, c, d in turn, each taken at once by the leader
    ///              (Derba; a stays so no Missa); dealer takes a (Missa).
    ///              Leader pile +6 (+3 pts), dealer pile +2 (+1 pt).
    ///   D(r,s)     leader drops, dealer takes it at once (Derba + Missa), four times.
    ///              Dealer pile +8 (+8 pts).
    /// BLOCKED (see docs/mvp/03_gameplay_fidelity_audit.md §4): ED-08 (unreachable under the
    /// current capture model and unspecified), ED-03 with the exact observed numbers (see below).
    /// </summary>
    public class EndOfDeckTests
    {
        private const int H = 0; // human
        private const int A = 1; // Nadia

        private static int[] P(params int[] ranks) => ranks;

        // ---------- Fixtures ----------

        /// <summary>
        /// ED-02. Dealer H. Deals 1-4: L(1,2,3,4) x2, L(5,6,7,10) x2 → piles H 8 / A 24, scores H 4 / A 12.
        /// Deal 5 (A leads):  A 11 drop · H 12 drop · A 11 takes 11+12 (chain, Missa: A 13) · H 11 drop ·
        ///                    A 11 takes 11 (Derba+Missa: 15) · H 12 drop · A 12 takes 12 (Derba+Missa: 17) ·
        ///                    H 12 drop → table {12}. Last capture: A. Piles H 8 / A 31.
        /// </summary>
        internal static ScriptedDeck Ed02()
        {
            return new ScriptedDeck(dealer: H)
                .Deal(P(1, 2, 3, 4), P(2, 3, 4, 1))
                .Deal(P(1, 2, 3, 4), P(2, 3, 4, 1))
                .Deal(P(5, 6, 7, 10), P(6, 7, 10, 5))
                .Deal(P(5, 6, 7, 10), P(6, 7, 10, 5))
                .Deal(P(11, 11, 11, 12), P(12, 11, 12, 12));
        }

        /// <summary>
        /// ED-01 and ED-07. Dealer H. Deals 1-4: L(1,2,3,6), D(7,10), L(1,2,3,6), L(4,5,11,12)
        /// → piles H 14 / A 18, scores H 11 / A 9.
        /// Deal 5 (A leads):  A 5 drop · H 12 drop · A 11 drop · H 11 takes 11+12 (chain; Derba: H 12) ·
        ///                    A 4 drop · H 4 takes 4+5 (chain; Derba+Missa: H 14) · A 5 drop ·
        ///                    H 12 drop → table {5, 12}. Last capture: H (the dealer). Piles H 20 / A 18.
        /// </summary>
        private static ScriptedDeck Ed0107()
        {
            return new ScriptedDeck(dealer: H)
                .Deal(P(1, 2, 3, 6), P(2, 3, 6, 1))
                .Deal(P(7, 7, 10, 10), P(7, 7, 10, 10))
                .Deal(P(1, 2, 3, 6), P(2, 3, 6, 1))
                .Deal(P(4, 5, 11, 12), P(5, 11, 12, 4))
                .Deal(P(5, 11, 4, 5), P(12, 11, 4, 12));
        }

        /// <summary>
        /// ED-04. Dealer H. Deals 1-4 as ED-01 → piles H 14 / A 18, scores H 11 / A 9.
        /// Deal 5 (A leads):  A 11 drop · H 12 drop · A 4 drop · H 11 takes 11+12 (chain, no Derba, no Missa) ·
        ///                    A 4 takes 4 (Missa: A 10) · H 12 drop · A 5 drop · H 5 takes 5 (Derba: H 12)
        ///                    → table {12}. Last capture: H (the dealer). Piles H 19 / A 20.
        /// </summary>
        private static ScriptedDeck Ed04()
        {
            return new ScriptedDeck(dealer: H)
                .Deal(P(1, 2, 3, 6), P(2, 3, 6, 1))
                .Deal(P(7, 7, 10, 10), P(7, 7, 10, 10))
                .Deal(P(1, 2, 3, 6), P(2, 3, 6, 1))
                .Deal(P(4, 5, 11, 12), P(5, 11, 12, 4))
                .Deal(P(11, 4, 4, 5), P(12, 11, 12, 5));
        }

        /// <summary>
        /// ED-05. Dealer H. Deals 1-4: D(5,6), L(1,2,3,4), D(7,10), L(1,2,3,4) → piles H 20 / A 12,
        /// scores H 18 / A 6. Deal 5 as ED-02 → A 11 · H 12 · A takes 11+12 (Missa: 7) · H 11 ·
        /// A takes 11 (Derba+Missa: 9) · H 12 · A takes 12 (Derba+Missa: 11) · H 12 drop → table {12}.
        /// Last capture: A (the non-dealer). Piles H 20 / A 19.
        /// </summary>
        private static ScriptedDeck Ed05()
        {
            return new ScriptedDeck(dealer: H)
                .Deal(P(5, 5, 6, 6), P(5, 5, 6, 6))
                .Deal(P(1, 2, 3, 4), P(2, 3, 4, 1))
                .Deal(P(7, 7, 10, 10), P(7, 7, 10, 10))
                .Deal(P(1, 2, 3, 4), P(2, 3, 4, 1))
                .Deal(P(11, 11, 11, 12), P(12, 11, 12, 12));
        }

        /// <summary>
        /// ED-06. Dealer H. Deals 1-4: L(4,5,11,12), D(1,2), D(3,6), D(7,10) → piles H 26 / A 6,
        /// scores H 25 / A 3. Deal 5 (A leads): A 5 drop · H 12 drop · A 4 drop · H 12 takes 12 ·
        /// A 11 drop · H 11 takes 11 (Derba: H 26) · A 4 takes 4+5 (chain, Missa: A 4) · H 5 drop
        /// → table {5}. Last capture: A (the non-dealer). Piles H 30 / A 9.
        /// </summary>
        private static ScriptedDeck Ed06()
        {
            return new ScriptedDeck(dealer: H)
                .Deal(P(4, 5, 11, 12), P(5, 11, 12, 4))
                .Deal(P(1, 1, 2, 2), P(1, 1, 2, 2))
                .Deal(P(3, 3, 6, 6), P(3, 3, 6, 6))
                .Deal(P(7, 7, 10, 10), P(7, 7, 10, 10))
                .Deal(P(5, 4, 11, 4), P(12, 12, 11, 5));
        }

        private static DeckTalliedEvent PlayLastCard(ScriptedDeck deck, RondaGame game, out List<GameEvent> events)
        {
            events = deck.PlayRange(game, 5, 8, 8);
            return events.OfType<DeckTalliedEvent>().Single();
        }

        // ---------- Tests ----------

        /// <summary>
        /// ED-01 — cards left on the table at the end of the deck go to the last capturer.
        /// Start: dealer H, A leads, table empty, 0-0. Before the last card: piles H 20 / A 18,
        /// scores H 14 / A 9, H made the last capture (deal 5, play 6). Last card: H drops 12.
        /// Expected: leftover {5, 12} → H; tally piles H 22 / A 18 (40 in total); the table is
        /// cleared and a new deck starts.
        /// </summary>
        [Test]
        public void ED01_LeftoverCards_GoToTheLastCapturer()
        {
            ScriptedDeck deck = Ed0107();
            RondaGame game = deck.Start();
            deck.PlayDeals(game, 4);
            Expect.Piles(game, 14, 18, "after deal 4");
            Expect.Scores(game, 11, 9, "after deal 4");
            deck.PlayRange(game, 5, 1, 7);
            Expect.Piles(game, 20, 18, "before the last card");
            Expect.Scores(game, 14, 9, "before the last card");

            DeckTalliedEvent tally = PlayLastCard(deck, game, out _);

            Expect.TableRanks(tally.Leftover, 5, 12);
            Assert.AreEqual(H, tally.LastCapturer);
            Assert.AreEqual(H, tally.LeftoverTo);
            CollectionAssert.AreEqual(new[] { 22, 18 }, tally.PileCounts);
            Assert.AreEqual(40, tally.PileCounts.Sum());
            Assert.AreEqual(2, game.DeckRound, "a new deck starts");
            Assert.AreEqual(0, game.Table.Count);
        }

        /// <summary>
        /// ED-02 — the dealer did not make the last capture: the other side gets +5, and each card
        /// over 20 is worth 1. Reproduces the numbers seen live (A021): 32 cards → +12, and +5.
        /// Start: dealer H, A leads, table empty, 0-0. Before the last card: piles H 8 / A 31,
        /// scores H 4 / A 17, A made the last capture (deal 5, play 7). Last card: H drops 12.
        /// Expected: leftover {12} → A (32); A +5 (dealer rule); A +12 (32 − 20); H +0.
        /// Final: H 4, A 34. Nobody has 41: a second deck starts and the dealer changes to A.
        /// </summary>
        [Test]
        public void ED02_DealerNotLastToTake_OtherSideGetsFive_PlusCardsOverTwenty()
        {
            ScriptedDeck deck = Ed02();
            RondaGame game = deck.Start();
            deck.PlayDeals(game, 4);
            Expect.Piles(game, 8, 24, "after deal 4");
            Expect.Scores(game, 4, 12, "after deal 4");
            deck.PlayRange(game, 5, 1, 7);
            Expect.Piles(game, 8, 31, "before the last card");
            Expect.Scores(game, 4, 17, "before the last card");

            DeckTalliedEvent tally = PlayLastCard(deck, game, out List<GameEvent> events);

            Assert.AreEqual(H, tally.Dealer);
            Assert.AreEqual(A, tally.LastCapturer);
            Expect.TableRanks(tally.Leftover, 12);
            Assert.AreEqual(A, tally.LeftoverTo);
            CollectionAssert.AreEqual(new[] { 8, 32 }, tally.PileCounts);
            Expect.Awards(events, (A, ScoreReason.DealerPenalty, 5), (A, ScoreReason.CardsOverThreshold, 12));
            Expect.Scores(game, 4, 34, "after the tally");
            Assert.IsFalse(game.IsMatchOver);
            Assert.AreEqual(2, game.DeckRound);
            Expect.Piles(game, 0, 0, "at the start of deck 2");
        }

        /// <summary>
        /// ED-04 — piles 20 / 20 and the dealer made the last capture: nobody scores at the tally.
        /// Start: dealer H, A leads, table empty, 0-0. Before the last card: piles H 17 / A 20,
        /// scores H 11 / A 10. Last card: H plays 5 and takes A's 5 just dropped (Derba: H 12);
        /// {12} stays on the table. Expected: leftover {12} → H (20); piles 20 / 20; no dealer
        /// point (H, the dealer, took last); no point over 20. Final: H 12, A 10.
        /// </summary>
        [Test]
        public void ED04_TwentyEach_DealerTookLast_NoTallyPoints()
        {
            ScriptedDeck deck = Ed04();
            RondaGame game = deck.Start();
            deck.PlayDeals(game, 4);
            deck.PlayRange(game, 5, 1, 7);
            Expect.Piles(game, 17, 20, "before the last card");
            Expect.Scores(game, 11, 10, "before the last card");

            DeckTalliedEvent tally = PlayLastCard(deck, game, out List<GameEvent> events);

            Assert.AreEqual(H, tally.Dealer);
            Assert.AreEqual(H, tally.LastCapturer);
            Expect.TableRanks(tally.Leftover, 12);
            CollectionAssert.AreEqual(new[] { 20, 20 }, tally.PileCounts);
            Expect.Awards(events, (H, ScoreReason.Derba, 1)); // the last play's own Derba; nothing from the tally
            Expect.Scores(game, 12, 10, "after the tally");
        }

        /// <summary>
        /// ED-05 — piles 20 / 20 and the non-dealer made the last capture: the non-dealer gets +5
        /// only (20 is not over 20). Start: dealer H, A leads, table empty, 0-0. Before the last
        /// card: piles H 20 / A 19, scores H 18 / A 11. Last card: H drops 12.
        /// Expected: leftover {12} → A (20); A +5; no point over 20. Final: H 18, A 16.
        /// </summary>
        [Test]
        public void ED05_TwentyEach_NonDealerTookLast_GetsFiveOnly()
        {
            ScriptedDeck deck = Ed05();
            RondaGame game = deck.Start();
            deck.PlayDeals(game, 4);
            Expect.Piles(game, 20, 12, "after deal 4");
            Expect.Scores(game, 18, 6, "after deal 4");
            deck.PlayRange(game, 5, 1, 7);
            Expect.Piles(game, 20, 19, "before the last card");
            Expect.Scores(game, 18, 11, "before the last card");

            DeckTalliedEvent tally = PlayLastCard(deck, game, out List<GameEvent> events);

            Assert.AreEqual(A, tally.LastCapturer);
            CollectionAssert.AreEqual(new[] { 20, 20 }, tally.PileCounts);
            Expect.Awards(events, (A, ScoreReason.DealerPenalty, 5));
            Expect.Scores(game, 18, 16, "after the tally");
        }

        /// <summary>
        /// ED-06 — both tally rules at once, for different players: the dealer has 30 cards (+10)
        /// while the non-dealer took last (+5). Start: dealer H, A leads, table empty, 0-0.
        /// Before the last card: piles H 30 / A 9, scores H 26 / A 4. Last card: H drops 5.
        /// Expected: leftover {5} → A (10); A +5 (dealer rule); H +10 (30 − 20). Final: H 36, A 9.
        /// </summary>
        [Test]
        public void ED06_DealerOverTwenty_AndNonDealerTookLast_BothScore()
        {
            ScriptedDeck deck = Ed06();
            RondaGame game = deck.Start();
            deck.PlayDeals(game, 4);
            Expect.Piles(game, 26, 6, "after deal 4");
            Expect.Scores(game, 25, 3, "after deal 4");
            deck.PlayRange(game, 5, 1, 7);
            Expect.Piles(game, 30, 9, "before the last card");
            Expect.Scores(game, 26, 4, "before the last card");

            DeckTalliedEvent tally = PlayLastCard(deck, game, out List<GameEvent> events);

            Assert.AreEqual(A, tally.LastCapturer);
            Expect.TableRanks(tally.Leftover, 5);
            CollectionAssert.AreEqual(new[] { 30, 10 }, tally.PileCounts);
            Expect.Awards(events, (A, ScoreReason.DealerPenalty, 5), (H, ScoreReason.CardsOverThreshold, 10));
            Expect.Scores(game, 36, 9, "after the tally");
        }

        /// <summary>
        /// ED-07 — cards over 20 are counted after the leftover has been added: the leftover lifts
        /// H from 20 to 22 → +2. Same deck as ED-01. Before the last card: piles H 20 / A 18, scores
        /// H 14 / A 9. Expected tally points: H +2 only (no dealer point: H, the dealer, took last;
        /// A has 18). Final: H 16, A 9.
        /// Evidence: M-42 + M-44 (the observed 23 + 3 = 26 → +6 example fits; its decomposition is INFERRED).
        /// </summary>
        [Test]
        public void ED07_LeftoverCountsTowardsCardsOverTwenty()
        {
            ScriptedDeck deck = Ed0107();
            RondaGame game = deck.Start();
            deck.PlayDeals(game, 4);
            deck.PlayRange(game, 5, 1, 7);
            Expect.Piles(game, 20, 18, "before the last card");

            PlayLastCard(deck, game, out List<GameEvent> events);

            Expect.Awards(events, (H, ScoreReason.CardsOverThreshold, 2));
            Expect.Scores(game, 16, 9, "after the tally");
        }
    }

    /// <summary>
    /// Deterministic match-end tests at the real 41-point target (QA ME-01, ME-02, ME-05, ME-06,
    /// DB-07, RN-13). Expected values worked out by hand and cross-checked with an independent
    /// model of the documented rules.
    ///
    /// Fixture "march": the dealer reaches exactly 40 after four deals. In each deal the leader
    /// holds r r s s and the dealer r r s s; the dealer announces two rondas (r and s) before its
    /// first card; the leader drops, the dealer takes it at once (Derba + Missa), four times.
    /// Per deal the dealer scores 8 (4 Derba + 4 Missa) + 2 (two rondas, sole announcer) = 10:
    /// 10, 20, 30, 40. Rules used: M-14, M-20, M-22, M-26, M-27, M-34, M-37 (ronda cap 40),
    /// M-47/M-48 (first to 41; the match ends at once during play).
    /// The ronda totals are the same whether announcements are credited at the start of the deal
    /// or when the window closes (U-3 does not change these fixtures).
    ///
    /// BLOCKED (see docs/mvp/03_gameplay_fidelity_audit.md §4): ME-03 and ME-04 (both players
    /// at 41 or more after one tally, M-50 unresolved).
    /// </summary>
    public class MatchEndTests
    {
        private const int H = 0;
        private const int A = 1;

        private static int[] P(params int[] ranks) => ranks;

        private static ScriptedDeck March(ScriptedDeck deck, int r, int s)
        {
            return deck.Deal(P(r, r, s, s), P(r, r, s, s), r, s);
        }

        /// <summary>
        /// Deals 1-4 march (1,2), (3,4), (5,6), (7,11) → dealer 40. Deal 5, leader 10 10 12 12,
        /// dealer 12 12 10 10 (announces 10 and 12): leader 10 drop · dealer 12 drop (window closes:
        /// two rondas requested 2, granted 0 — capped at 40) · leader 10 takes 10 (no Derba, 12 stays,
        /// no Missa) · dealer 12 takes 12 (no Derba: the previous play was a capture; table empty →
        /// Missa) → 41.
        /// </summary>
        private static ScriptedDeck MissaToFortyOne(int dealer)
        {
            var deck = new ScriptedDeck(dealer);
            March(deck, 1, 2);
            March(deck, 3, 4);
            March(deck, 5, 6);
            March(deck, 7, 11);
            return deck.Deal(P(10, 10, 12, 12), P(12, 12, 10, 10), 10, 12);
        }

        private static void PlayToForty(ScriptedDeck deck, RondaGame game)
        {
            int leader = deck.Leader;
            int dealer = deck.Dealer;
            int[] expected = { 10, 20, 30, 40 };
            for (int deal = 1; deal <= 4; deal++)
            {
                deck.PlayRange(game, deal);
                Assert.AreEqual(expected[deal - 1], game.GetScore(dealer), "dealer score after deal " + deal);
                Assert.AreEqual(0, game.GetScore(leader), "leader score after deal " + deal);
                Assert.IsFalse(game.IsMatchOver);
            }
        }

        /// <summary>
        /// ME-01 (+ RN-13) — reaching exactly 41 with a Missa during play ends the match at once.
        /// Run twice: the human as the scorer (victory) and Nadia as the scorer (the human's defeat).
        /// Start: scorer deals; 0-0. After deal 4: scorer 40, other 0.
        /// Deal 5: the scorer's two rondas are worth 0 at 40 (requested 2); still 40 after plays 2
        /// and 3. Play 4: the scorer's Missa → 41. Expected: exactly one point awarded on that play
        /// (Missa +1); MatchEnded with the scorer as winner and scores 41 / 0; no tally; no further
        /// play is accepted.
        /// </summary>
        [TestCase(H, TestName = "ME01_MissaToExactly41_EndsTheMatch_HumanWins")]
        [TestCase(A, TestName = "ME01_MissaToExactly41_EndsTheMatch_HumanLoses")]
        public void ME01_MissaToExactly41(int scorer)
        {
            ScriptedDeck deck = MissaToFortyOne(dealer: scorer);
            RondaGame game = deck.Start();
            PlayToForty(deck, game);

            List<GameEvent> windowCloses = deck.PlayRange(game, 5, 1, 2);
            ScoreAwardedEvent rondas = windowCloses.OfType<ScoreAwardedEvent>().Single();
            Assert.AreEqual(ScoreReason.Announcement, rondas.Reason);
            Assert.AreEqual(2, rondas.Requested, "two rondas requested");
            Assert.AreEqual(0, rondas.Points, "ronda points cannot go past 40 (RN-13)");
            deck.PlayRange(game, 5, 3, 3);
            Assert.AreEqual(40, game.GetScore(scorer), "one point short, immediately before the threshold");
            Assert.IsFalse(game.IsMatchOver);

            List<GameEvent> deciding = deck.PlayRange(game, 5, 4, 4);

            Expect.Awards(deciding, (scorer, ScoreReason.Missa, 1));
            MatchEndedEvent end = deciding.OfType<MatchEndedEvent>().Single();
            Assert.AreEqual(scorer, end.Winner);
            Assert.AreEqual(41, end.Scores[scorer]);
            Assert.AreEqual(0, end.Scores[1 - scorer]);
            Assert.IsEmpty(deciding.OfType<DeckTalliedEvent>(), "no tally when the match ends during play");
            Assert.IsTrue(game.IsMatchOver);
            Assert.AreEqual(scorer, game.Winner);
            Assert.IsEmpty(game.GetLegalCards(game.CurrentPlayer), "no card can be played after the match ends");
            Assert.Throws<InvalidOperationException>(() => game.PlayCard(deck.Leader, deck.CardAt(5, 5)));
        }

        /// <summary>
        /// DB-07 — reaching 41 with a Derba during play ends the match at once.
        /// The deciding capture leaves a card on the table, so no Missa happens on the same play
        /// (whether a simultaneous Missa would still count after 41 is not established).
        /// Start: Nadia deals; 0-0. Deals 1-4 march (3,4), (5,6), (7,10), (11,12) → Nadia 40.
        /// Deal 5, leader 1 1 2 1, dealer 2 1 2 2 (no announcement): leader 1 drop · dealer 2 drop ·
        /// leader 1 takes 1+2 (chain, Missa: leader 1) · dealer 1 drop · leader 2 drop ·
        /// dealer 2 takes 2 (Derba; 1 stays on the table) → 41.
        /// Expected: that play awards exactly Derba +1; MatchEnded, Nadia wins 41 to 1.
        /// </summary>
        [Test]
        public void DB07_DerbaToExactly41_EndsTheMatch()
        {
            var deck = new ScriptedDeck(dealer: A);
            March(deck, 3, 4);
            March(deck, 5, 6);
            March(deck, 7, 10);
            March(deck, 11, 12);
            deck.Deal(P(1, 1, 2, 1), P(2, 1, 2, 2));
            RondaGame game = deck.Start();
            PlayToForty(deck, game);

            deck.PlayRange(game, 5, 1, 5);
            Expect.Scores(game, 1, 40, "immediately before the deciding play");

            List<GameEvent> deciding = deck.PlayRange(game, 5, 6, 6);

            Expect.Awards(deciding, (A, ScoreReason.Derba, 1));
            Expect.TableRanks(game.Table, 1);
            MatchEndedEvent end = deciding.OfType<MatchEndedEvent>().Single();
            Assert.AreEqual(A, end.Winner);
            CollectionAssert.AreEqual(new[] { 1, 41 }, end.Scores);
            Assert.IsTrue(game.IsMatchOver);
        }

        /// <summary>
        /// ME-02 — 40 is not a win; the tally lifts a player past 41 and the match ends after the
        /// whole tally. Start: Nadia deals; 0-0. Deals 1-4 march (1,2), (3,4), (5,6), (7,10) → Nadia 40.
        /// Deal 5 (no announcement), human 11 11 11 11, Nadia 12 12 12 12: H 11 drop · A 12 drop ·
        /// H 11 takes 11+12 (chain, Missa: H 1) · A 12 drop · H 11 drop · A 12 takes 12 (no Derba,
        /// 11 stays: no Missa, still 40) · H 11 takes 11 (Missa: H 2) · A 12 drop → table {12}.
        /// Before the tally: H 2, A 40, piles H 5 / A 34, the human (non-dealer) took last.
        /// Expected tally: leftover {12} → H (6); H +5 (dealer rule); A +14 (34 − 20).
        /// Final: H 7, A 54; Nadia wins after the full tally (only one player reached 41).
        /// </summary>
        [Test]
        public void ME02_TallyLiftsPast41_WinnerDecidedAfterTheWholeTally()
        {
            var deck = new ScriptedDeck(dealer: A);
            March(deck, 1, 2);
            March(deck, 3, 4);
            March(deck, 5, 6);
            March(deck, 7, 10);
            deck.Deal(P(11, 11, 11, 11), P(12, 12, 12, 12));
            RondaGame game = deck.Start();
            PlayToForty(deck, game);

            deck.PlayRange(game, 5, 1, 7);
            Expect.Scores(game, 2, 40, "before the tally");
            Expect.Piles(game, 5, 34, "before the tally");
            Assert.IsFalse(game.IsMatchOver, "40 is not a win");

            List<GameEvent> last = deck.PlayRange(game, 5, 8, 8);

            DeckTalliedEvent tally = last.OfType<DeckTalliedEvent>().Single();
            Assert.AreEqual(H, tally.LastCapturer);
            Expect.TableRanks(tally.Leftover, 12);
            CollectionAssert.AreEqual(new[] { 6, 34 }, tally.PileCounts);
            Expect.Awards(last, (H, ScoreReason.DealerPenalty, 5), (A, ScoreReason.CardsOverThreshold, 14));
            MatchEndedEvent end = last.OfType<MatchEndedEvent>().Single();
            Assert.AreEqual(A, end.Winner);
            CollectionAssert.AreEqual(new[] { 7, 54 }, end.Scores);
            Assert.IsEmpty(last.OfType<DeckStartedEvent>(), "no new deck after the match ends");
        }

        /// <summary>
        /// ME-05 — the target comes from the ruleset (RL-02 "configurable per ruleset"); the ronda
        /// cap follows it (RL-17 "target − 1"). Target 21: deals 1-2 march (1,2), (3,4) → Nadia 20.
        /// Deal 3 as ME-01's last deal (ranks 10, 12): two rondas requested 2, granted 0 (cap 20);
        /// the Missa on play 4 → 21 ends the match.
        /// </summary>
        [Test]
        public void ME05_ConfiguredTarget_MovesTheThresholdAndTheRondaCap()
        {
            var rules = new RulesConfig { TargetScore = 21 };
            var deck = new ScriptedDeck(dealer: A);
            March(deck, 1, 2);
            March(deck, 3, 4);
            deck.Deal(P(10, 10, 12, 12), P(12, 12, 10, 10), 10, 12);
            March(deck, 5, 6);
            March(deck, 7, 11);
            RondaGame game = deck.Start(rules);

            deck.PlayRange(game, 1);
            deck.PlayRange(game, 2);
            Expect.Scores(game, 0, 20, "after deal 2");
            ScoreAwardedEvent rondas = deck.PlayRange(game, 3, 1, 2).OfType<ScoreAwardedEvent>().Single();
            Assert.AreEqual(0, rondas.Points, "ronda points stop at target − 1 = 20");
            deck.PlayRange(game, 3, 3, 3);
            Assert.IsFalse(game.IsMatchOver);

            List<GameEvent> deciding = deck.PlayRange(game, 3, 4, 4);

            Expect.Awards(deciding, (A, ScoreReason.Missa, 1));
            Assert.IsTrue(game.IsMatchOver);
            Assert.AreEqual(A, game.Winner);
            Assert.AreEqual(21, game.GetScore(A));
        }

        /// <summary>
        /// ME-06 — a new match after a finished one starts from a clean state and replays
        /// identically. Plays ME-01 (human loses) to the end, then starts a new game with the same
        /// rules object and the same deck: scores 0-0, piles 0-0, empty table, 4 + 4 cards dealt,
        /// 32 in the stock, deck 1 / deal 1, no winner, no points in the start events, target
        /// still 41; replaying the script ends exactly as before.
        /// </summary>
        [Test]
        public void ME06_NewMatchAfterAFinishedOne_StartsClean_AndReplaysIdentically()
        {
            var rules = new RulesConfig();
            ScriptedDeck deck = MissaToFortyOne(dealer: A);
            RondaGame first = deck.Start(rules);
            deck.PlayDeals(first, 4);
            deck.PlayRange(first, 5, 1, 4);
            Assert.IsTrue(first.IsMatchOver);

            ScriptedDeck replay = MissaToFortyOne(dealer: A);
            RondaGame second = replay.Start(rules);

            Expect.Scores(second, 0, 0, "at the start of the new match");
            Expect.Piles(second, 0, 0, "at the start of the new match");
            Assert.AreEqual(0, second.Table.Count);
            Assert.AreEqual(4, second.GetHand(0).Count);
            Assert.AreEqual(4, second.GetHand(1).Count);
            Assert.AreEqual(32, second.StockCount);
            Assert.AreEqual(1, second.DeckRound);
            Assert.AreEqual(1, second.DealNumber);
            Assert.IsFalse(second.IsMatchOver);
            Assert.AreEqual(-1, second.Winner);
            Assert.AreEqual(41, rules.TargetScore, "the rules object is not changed by a match");

            replay.PlayDeals(second, 4);
            replay.PlayRange(second, 5, 1, 4);
            Assert.IsTrue(second.IsMatchOver);
            Assert.AreEqual(first.Winner, second.Winner);
            Assert.AreEqual(first.GetScore(0), second.GetScore(0));
            Assert.AreEqual(first.GetScore(1), second.GetScore(1));
        }
    }
}
