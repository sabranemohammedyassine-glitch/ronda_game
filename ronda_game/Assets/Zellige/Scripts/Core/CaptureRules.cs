using System.Collections.Generic;

namespace Zellige.Core
{
    /// <summary>
    /// The capture rule, as a pure function so the engine, the AI and the UI
    /// preview all use the same code.
    /// </summary>
    public static class CaptureRules
    {
        /// <summary>
        /// Table cards taken by playing <paramref name="played"/>: the card of the same
        /// rank, then each next rank up that is on the table (7 is followed by 10),
        /// stopping at the first missing rank. Empty list = the card is dropped.
        /// </summary>
        public static List<Card> FindCaptures(IReadOnlyList<Card> table, Card played)
        {
            var captured = new List<Card>();
            int rank = played.Rank;
            while (TryFindRank(table, rank, out Card match))
            {
                captured.Add(match);
                if (!Ranks.TryGetNext(rank, out rank)) break;
            }
            return captured;
        }

        private static bool TryFindRank(IReadOnlyList<Card> table, int rank, out Card match)
        {
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].Rank == rank)
                {
                    match = table[i];
                    return true;
                }
            }
            match = default;
            return false;
        }
    }
}
