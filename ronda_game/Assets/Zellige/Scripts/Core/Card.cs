using System;
using System.Collections.Generic;

namespace Zellige.Core
{
    /// <summary>The four suits of the Spanish deck.</summary>
    public enum Suit
    {
        Oros = 0,
        Copas = 1,
        Espadas = 2,
        Bastos = 3
    }

    /// <summary>
    /// Rank helpers. The deck has no 8 or 9, so the order is
    /// 1 2 3 4 5 6 7 10 11 12 and the rank after 7 is 10.
    /// </summary>
    public static class Ranks
    {
        public static readonly IReadOnlyList<int> All = new[] { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

        public static bool IsValid(int rank)
        {
            return (rank >= 1 && rank <= 7) || (rank >= 10 && rank <= 12);
        }

        /// <summary>Position of the rank in the rank order (0..9).</summary>
        public static int IndexOf(int rank)
        {
            if (!IsValid(rank)) throw new ArgumentOutOfRangeException(nameof(rank), rank, "Not a Spanish-deck rank.");
            return rank <= 7 ? rank - 1 : rank - 3;
        }

        /// <summary>The next rank up, or false for 12 (the order never wraps).</summary>
        public static bool TryGetNext(int rank, out int next)
        {
            if (rank == 12)
            {
                next = 0;
                return false;
            }
            next = rank == 7 ? 10 : rank + 1;
            return true;
        }
    }

    /// <summary>One playing card. Immutable; equal when suit and rank are equal.</summary>
    public readonly struct Card : IEquatable<Card>
    {
        public readonly Suit Suit;
        public readonly int Rank;

        public Card(Suit suit, int rank)
        {
            if (!Ranks.IsValid(rank)) throw new ArgumentOutOfRangeException(nameof(rank), rank, "Not a Spanish-deck rank.");
            Suit = suit;
            Rank = rank;
        }

        /// <summary>Unique id 0..39 (suit * 10 + rank index).</summary>
        public int Id => (int)Suit * 10 + Ranks.IndexOf(Rank);

        public bool Equals(Card other) => Suit == other.Suit && Rank == other.Rank;
        public override bool Equals(object obj) => obj is Card other && Equals(other);
        public override int GetHashCode() => Id;
        public static bool operator ==(Card a, Card b) => a.Equals(b);
        public static bool operator !=(Card a, Card b) => !a.Equals(b);
        public override string ToString() => Rank + " of " + Suit;
    }

    /// <summary>Deck creation and shuffling.</summary>
    public static class Deck
    {
        public const int Size = 40;

        /// <summary>The 40 cards in suit then rank order.</summary>
        public static List<Card> CreateStandard()
        {
            var cards = new List<Card>(Size);
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (int rank in Ranks.All)
                {
                    cards.Add(new Card(suit, rank));
                }
            }
            return cards;
        }

        /// <summary>Fisher–Yates shuffle; the same Random seed gives the same order.</summary>
        public static void Shuffle(IList<Card> cards, Random rng)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                Card tmp = cards[i];
                cards[i] = cards[j];
                cards[j] = tmp;
            }
        }
    }
}
