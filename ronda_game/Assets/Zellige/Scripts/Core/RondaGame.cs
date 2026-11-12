using System;
using System.Collections.Generic;

namespace Zellige.Core
{
    /// <summary>
    /// The complete rules engine for a two-player Ronda match. Holds the match state,
    /// validates commands (PlayCard, Announce) and returns what happened as events.
    /// No Unity types: it runs the same in the game and in tests.
    /// Seats are 0 and 1. Rule ids (RL-xx) refer to docs/phase3/03_rules_and_scoring_spec.md.
    /// </summary>
    public sealed class RondaGame
    {
        public const int PlayerCount = 2;

        public RulesConfig Rules { get; }

        private readonly Random rng;
        private readonly List<Card>[] hands = { new List<Card>(), new List<Card>() };
        private readonly List<Card>[] piles = { new List<Card>(), new List<Card>() };
        private readonly List<Card> table = new List<Card>();
        private readonly List<Card> stock = new List<Card>();
        private readonly int[] scores = new int[PlayerCount];
        private readonly List<Announcement> announcements = new List<Announcement>();
        private readonly HashSet<int>[] ranksPlayedThisDeal = { new HashSet<int>(), new HashSet<int>() };

        private List<GameEvent> events;
        private bool started;
        private bool announceWindowOpen;
        private int playsThisDeal;
        private int lastCapturer = -1;
        private bool hasLastPlay;
        private int lastPlaySeat;
        private Card lastPlayCard;
        private bool lastPlayWasDrop;
        private bool tallying;

        public RondaGame(RulesConfig rules, int seed)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            rng = new Random(seed);
        }

        // ---------- Read-only state ----------

        public int Dealer { get; private set; }
        public int CurrentPlayer { get; private set; }
        /// <summary>1-based number of the current pass through the 40 cards.</summary>
        public int DeckRound { get; private set; }
        /// <summary>1-based number of the current deal inside the deck round.</summary>
        public int DealNumber { get; private set; }
        public bool IsMatchOver { get; private set; }
        /// <summary>Winning seat, or -1 while the match is running.</summary>
        public int Winner { get; private set; } = -1;
        public bool IsAnnounceWindowOpen => announceWindowOpen;

        public IReadOnlyList<Card> Table => table;
        public int StockCount => stock.Count;
        public IReadOnlyList<Card> GetHand(int seat) => hands[seat];
        public int GetPileCount(int seat) => piles[seat].Count;
        public int GetScore(int seat) => scores[seat];

        // ---------- Commands ----------

        /// <summary>
        /// Starts a match. For tests and scripted games the first dealer and the order of
        /// the first deck can be fixed: index 0 is the top card; each deal gives the
        /// first player (the non-dealer) the next CardsPerDeal cards, then the dealer.
        /// </summary>
        public IReadOnlyList<GameEvent> StartMatch(int? firstDealer = null, IList<Card> firstDeckOrder = null)
        {
            if (started) throw new InvalidOperationException("The match has already started.");
            if (firstDeckOrder != null) ValidateDeck(firstDeckOrder);
            started = true;
            events = new List<GameEvent>();
            Dealer = firstDealer ?? rng.Next(PlayerCount);
            StartDeckRound(firstDeckOrder);
            return events;
        }

        /// <summary>Cards the seat may play right now (empty when it is not its turn).</summary>
        public List<Card> GetLegalCards(int seat)
        {
            var legal = new List<Card>();
            if (!started || IsMatchOver || seat != CurrentPlayer) return legal;

            if (Rules.MustCapture)
            {
                foreach (Card card in hands[seat])
                {
                    if (CaptureRules.FindCaptures(table, card).Count > 0) legal.Add(card);
                }
                if (legal.Count > 0) return legal;
            }
            legal.AddRange(hands[seat]);
            return legal;
        }

        public bool CanPlay(int seat, Card card)
        {
            return GetLegalCards(seat).Contains(card);
        }

        /// <summary>
        /// Plays one card (RL-07). The card captures automatically when its rank is on
        /// the table (RL-08, RL-09), otherwise it stays on the table (RL-10).
        /// Throws when the play is not legal.
        /// </summary>
        public IReadOnlyList<GameEvent> PlayCard(int seat, Card card)
        {
            if (!CanPlay(seat, card))
            {
                throw new InvalidOperationException("Illegal play: seat " + seat + ", " + card + ".");
            }

            events = new List<GameEvent>();
            bool isDerba = WouldBeDerba(seat, card);
            hands[seat].Remove(card);
            ranksPlayedThisDeal[seat].Add(card.Rank);

            List<Card> captured = CaptureRules.FindCaptures(table, card);
            events.Add(new CardPlayedEvent { Seat = seat, Card = card, Captured = captured });

            bool isFinalPlayOfDeck = stock.Count == 0 && hands[0].Count == 0 && hands[1].Count == 0;
            if (captured.Count > 0)
            {
                foreach (Card taken in captured) table.Remove(taken);
                piles[seat].Add(card);
                piles[seat].AddRange(captured);
                lastCapturer = seat;

                if (isDerba) Award(seat, Rules.DerbaPoints, ScoreReason.Derba, false);
                if (table.Count == 0 && (!isFinalPlayOfDeck || Rules.MissaOnFinalPlay))
                {
                    Award(seat, Rules.MissaPoints, ScoreReason.Missa, false);
                }
            }
            else
            {
                table.Add(card);
            }

            hasLastPlay = true;
            lastPlaySeat = seat;
            lastPlayCard = card;
            lastPlayWasDrop = captured.Count == 0;
            if (IsMatchOver) return events;

            playsThisDeal++;
            if (announceWindowOpen && playsThisDeal >= Rules.AnnounceWindowPlays)
            {
                ResolveAnnouncements();
                if (IsMatchOver) return events;
            }

            if (hands[0].Count == 0 && hands[1].Count == 0)
            {
                if (stock.Count > 0)
                {
                    Deal();
                }
                else
                {
                    TallyDeck();
                    if (!IsMatchOver) StartDeckRound(null);
                }
            }
            else
            {
                CurrentPlayer = Other(seat);
                events.Add(new TurnStartedEvent { Seat = CurrentPlayer });
            }
            return events;
        }

        /// <summary>
        /// True when playing this card would capture the card the opponent dropped on
        /// the immediately preceding play (RL-13). Shared by the engine and the AI.
        /// </summary>
        public bool WouldBeDerba(int seat, Card card)
        {
            return hasLastPlay && lastPlayWasDrop && lastPlaySeat != seat && lastPlayCard.Rank == card.Rank;
        }

        /// <summary>
        /// Ranks the seat may announce now: it holds two or more, has not played one this
        /// deal, has not announced it yet, and the window is still open (RL-14).
        /// </summary>
        public List<int> GetAnnounceableRanks(int seat)
        {
            var result = new List<int>();
            if (!started || IsMatchOver || !announceWindowOpen) return result;

            foreach (int rank in Ranks.All)
            {
                if (CountInHand(seat, rank) < 2) continue;
                if (ranksPlayedThisDeal[seat].Contains(rank)) continue;
                if (HasAnnounced(seat, rank)) continue;
                result.Add(rank);
            }
            return result;
        }

        /// <summary>
        /// Declares a ronda or tringla. Announcements stay sealed until the window closes.
        /// Returns false (and changes nothing) when the announcement is not allowed.
        /// </summary>
        public bool Announce(int seat, int rank)
        {
            if (!GetAnnounceableRanks(seat).Contains(rank)) return false;

            int count = CountInHand(seat, rank);
            bool isTringla = count == 3 || (count == 4 && Rules.QuadCountsAsTringla);
            int value = isTringla ? Rules.TringlaPoints
                : count == 4 ? 2 * Rules.RondaPoints
                : Rules.RondaPoints;
            announcements.Add(new Announcement { Seat = seat, Rank = rank, IsTringla = isTringla, Value = value });
            return true;
        }

        // ---------- Internals ----------

        private static int Other(int seat) => 1 - seat;

        private void StartDeckRound(IList<Card> fixedOrder)
        {
            DeckRound++;
            if (DeckRound > 1 && Rules.DealerAlternates) Dealer = Other(Dealer);

            table.Clear();
            piles[0].Clear();
            piles[1].Clear();
            stock.Clear();
            if (fixedOrder != null)
            {
                stock.AddRange(fixedOrder);
            }
            else
            {
                stock.AddRange(Deck.CreateStandard());
                Deck.Shuffle(stock, rng);
            }
            lastCapturer = -1;
            hasLastPlay = false;
            DealNumber = 0;

            events.Add(new DeckStartedEvent { DeckRound = DeckRound, Dealer = Dealer });
            Deal();
        }

        private void Deal()
        {
            if (announceWindowOpen) ResolveAnnouncements();
            if (IsMatchOver) return;

            DealNumber++;
            int first = Other(Dealer);
            DrawCards(first);
            DrawCards(Dealer);

            playsThisDeal = 0;
            announcements.Clear();
            ranksPlayedThisDeal[0].Clear();
            ranksPlayedThisDeal[1].Clear();
            announceWindowOpen = true;
            CurrentPlayer = first;

            events.Add(new DealStartedEvent
            {
                DealNumber = DealNumber,
                Hands = new[] { new List<Card>(hands[0]), new List<Card>(hands[1]) },
                StockRemaining = stock.Count
            });
            events.Add(new TurnStartedEvent { Seat = CurrentPlayer });
        }

        private void DrawCards(int seat)
        {
            for (int i = 0; i < Rules.CardsPerDeal && stock.Count > 0; i++)
            {
                hands[seat].Add(stock[0]);
                stock.RemoveAt(0);
            }
        }

        /// <summary>Closes the announcement window and scores the contest (RL-16, RL-17).</summary>
        private void ResolveAnnouncements()
        {
            announceWindowOpen = false;
            if (announcements.Count == 0) return;

            int winner = FindContestWinner();
            events.Add(new AnnouncementsResolvedEvent
            {
                Announcements = new List<Announcement>(announcements),
                Winner = winner
            });
            if (winner < 0) return;

            int own = 0;
            int opposing = 0;
            bool winnerHasTringla = false;
            foreach (Announcement a in announcements)
            {
                if (a.Seat == winner)
                {
                    own += a.Value;
                    winnerHasTringla |= a.IsTringla;
                }
                else
                {
                    opposing++;
                }
            }

            bool capped = Rules.RondaCannotWin && !winnerHasTringla;
            Award(winner, own, ScoreReason.Announcement, capped);
            if (opposing > 0) Award(winner, opposing, ScoreReason.AnnouncementBonus, capped);
        }

        private int FindContestWinner()
        {
            Announcement best0 = BestAnnouncement(0);
            Announcement best1 = BestAnnouncement(1);
            if (best0 == null) return 1;
            if (best1 == null) return 0;

            int comparison = CompareStrength(best0, best1);
            if (comparison == 0) return -1;
            return comparison > 0 ? 0 : 1;
        }

        private Announcement BestAnnouncement(int seat)
        {
            Announcement best = null;
            foreach (Announcement a in announcements)
            {
                if (a.Seat != seat) continue;
                if (best == null || CompareStrength(a, best) > 0) best = a;
            }
            return best;
        }

        /// <summary>
        /// Positive when a is stronger: a tringla beats a ronda; between rondas the
        /// higher rank wins; between tringlas the lower rank wins (RL-16).
        /// </summary>
        private static int CompareStrength(Announcement a, Announcement b)
        {
            if (a.IsTringla != b.IsTringla) return a.IsTringla ? 1 : -1;
            int difference = Ranks.IndexOf(a.Rank) - Ranks.IndexOf(b.Rank);
            return a.IsTringla ? -difference : difference;
        }

        /// <summary>End of a deck: leftover cards, dealer rule, cards over 20 (RL-19 to RL-23).</summary>
        private void TallyDeck()
        {
            tallying = true;

            var leftover = new List<Card>(table);
            table.Clear();
            if (lastCapturer >= 0) piles[lastCapturer].AddRange(leftover);

            events.Add(new DeckTalliedEvent
            {
                Leftover = leftover,
                LeftoverTo = leftover.Count > 0 ? lastCapturer : -1,
                LastCapturer = lastCapturer,
                PileCounts = new[] { piles[0].Count, piles[1].Count },
                Dealer = Dealer
            });

            if (lastCapturer >= 0 && lastCapturer != Dealer)
            {
                Award(Other(Dealer), Rules.DealerPenaltyPoints, ScoreReason.DealerPenalty, false);
            }
            for (int seat = 0; seat < PlayerCount; seat++)
            {
                int extra = piles[seat].Count - Rules.PileThreshold;
                if (extra > 0) Award(seat, extra, ScoreReason.CardsOverThreshold, false);
            }

            tallying = false;

            // RL-23(b): after a tally the higher score wins; equal scores play another deck.
            bool targetReached = scores[0] >= Rules.TargetScore || scores[1] >= Rules.TargetScore;
            if (targetReached && scores[0] != scores[1])
            {
                EndMatch(scores[0] > scores[1] ? 0 : 1);
            }
        }

        private void Award(int seat, int points, ScoreReason reason, bool cappedBelowTarget)
        {
            if (IsMatchOver) return;

            int granted = points;
            if (cappedBelowTarget)
            {
                int room = Math.Max(0, Rules.TargetScore - 1 - scores[seat]);
                granted = Math.Min(points, room);
            }
            scores[seat] += granted;
            events.Add(new ScoreAwardedEvent
            {
                Seat = seat,
                Points = granted,
                Requested = points,
                Reason = reason,
                NewScore = scores[seat]
            });

            // RL-23(a): during play the match ends the moment the target is reached.
            if (!tallying && granted > 0 && scores[seat] >= Rules.TargetScore) EndMatch(seat);
        }

        private void EndMatch(int winner)
        {
            IsMatchOver = true;
            Winner = winner;
            announceWindowOpen = false;
            events.Add(new MatchEndedEvent { Winner = winner, Scores = new[] { scores[0], scores[1] } });
        }

        private int CountInHand(int seat, int rank)
        {
            int count = 0;
            foreach (Card card in hands[seat])
            {
                if (card.Rank == rank) count++;
            }
            return count;
        }

        private bool HasAnnounced(int seat, int rank)
        {
            foreach (Announcement a in announcements)
            {
                if (a.Seat == seat && a.Rank == rank) return true;
            }
            return false;
        }

        private static void ValidateDeck(IList<Card> order)
        {
            if (order.Count != Deck.Size || new HashSet<Card>(order).Count != Deck.Size)
            {
                throw new ArgumentException("A fixed deck order must contain each of the 40 cards exactly once.");
            }
        }
    }
}
