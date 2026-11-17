using System;
using System.Collections.Generic;
using Zellige.Core;

namespace Zellige.AI
{
    /// <summary>
    /// A small rule-based opponent. It only reads the game through the public engine
    /// API and reuses the engine's capture and derba checks, so it cannot disagree
    /// with the rules. It never changes the game state itself.
    /// </summary>
    public sealed class SimpleAi
    {
        private readonly Random rng;

        public SimpleAi(int seed)
        {
            rng = new Random(seed);
        }

        /// <summary>The AI always announces every pair or triple it is allowed to.</summary>
        public List<int> ChooseAnnouncements(RondaGame game, int seat)
        {
            return game.GetAnnounceableRanks(seat);
        }

        /// <summary>Picks the legal card with the best simple score; ties are broken at random.</summary>
        public Card ChooseCard(RondaGame game, int seat)
        {
            List<Card> legal = game.GetLegalCards(seat);
            if (legal.Count == 0) throw new InvalidOperationException("Seat " + seat + " has no legal card to play.");

            var best = new List<Card>();
            int bestScore = int.MinValue;
            foreach (Card card in legal)
            {
                int score = Evaluate(game, seat, card);
                if (score > bestScore)
                {
                    bestScore = score;
                    best.Clear();
                }
                if (score == bestScore) best.Add(card);
            }
            return best[rng.Next(best.Count)];
        }

        private static int Evaluate(RondaGame game, int seat, Card card)
        {
            IReadOnlyList<Card> table = game.Table;
            List<Card> captured = game.PreviewCaptures(card); // same capture rule as the engine (incl. H-4)

            if (captured.Count > 0)
            {
                int score = 100 + 10 * captured.Count;                  // more cards = closer to the over-20 bonus
                if (captured.Count == table.Count) score += 60;         // Missa
                if (game.WouldBeDerba(seat, card)) score += 60;         // Derba
                return score;
            }

            // Dropping: avoid feeding the opponent a long chain, and prefer to drop a
            // rank we hold twice (we can take it back next turn).
            var tableAfterDrop = new List<Card>(table) { card };
            int chainGiven = CaptureRules.FindCaptures(tableAfterDrop, card, game.Rules.ChainWrapsAfterTwelve).Count;
            int dropScore = -10 * chainGiven;
            if (CountRank(game.GetHand(seat), card.Rank) >= 2) dropScore += 15;
            return dropScore;
        }

        private static int CountRank(IReadOnlyList<Card> cards, int rank)
        {
            int count = 0;
            foreach (Card card in cards)
            {
                if (card.Rank == rank) count++;
            }
            return count;
        }
    }
}
