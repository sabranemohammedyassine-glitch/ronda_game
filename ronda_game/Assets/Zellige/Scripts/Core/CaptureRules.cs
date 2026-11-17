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
        /// With <paramref name="wrapAfterTwelve"/> (RulesConfig.ChainWrapsAfterTwelve, H-4) the chain
        /// continues from 12 to 1; it never takes more than one card per rank.
        /// </summary>
        public static List<Card> FindCaptures(IReadOnlyList<Card> table, Card played, bool wrapAfterTwelve = false)
        {
            var captured = new List<Card>();
            int rank = played.Rank;
            while (captured.Count < Ranks.All.Count && TryFindRank(table, rank, out Card match))
            {
                captured.Add(match);
                if (!Ranks.TryGetNext(rank, out rank))
                {
                    if (!wrapAfterTwelve) break;
                    rank = Ranks.All[0];
                }
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
