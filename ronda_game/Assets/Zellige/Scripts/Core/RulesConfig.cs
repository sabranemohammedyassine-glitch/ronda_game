namespace Zellige.Core
{
    /// <summary>
    /// Every tunable or not-yet-verified rule lives here so it can be changed
    /// without touching the engine. "Verified" = observed in Phase 2; "provisional"
    /// = our default from docs/phase3/03_rules_and_scoring_spec.md (rule ids RL-xx).
    /// </summary>
    public sealed class RulesConfig
    {
        /// <summary>Verified (RL-02): first to 41.</summary>
        public int TargetScore = 41;

        /// <summary>Verified (RL-05): 1v1 deals 4 cards to each player, five times per deck.</summary>
        public int CardsPerDeal = 4;

        /// <summary>
        /// Provisional (RL-11): when true a player holding a capturing card must play one.
        /// The reference behaviour is unknown; default is free choice.
        /// </summary>
        public bool MustCapture = false;

        /// <summary>Verified (RL-12): clearing the table scores 1.</summary>
        public int MissaPoints = 1;

        /// <summary>Provisional (RL-12): Missa also counts on the very last play of a deck.</summary>
        public bool MissaOnFinalPlay = true;

        /// <summary>
        /// Verified (RL-13): capturing the card the opponent has just dropped scores 1.
        /// The tutorial's 1/5/10 escalation is not understood and is not implemented.
        /// </summary>
        public int DerbaPoints = 1;

        /// <summary>Verified from tutorial text (RL-14): a pair is worth 1.</summary>
        public int RondaPoints = 1;

        /// <summary>Verified from tutorial text (RL-14): three of a kind is worth 5.</summary>
        public int TringlaPoints = 5;

        /// <summary>Provisional (RL-14): four of a kind is announced as one tringla.</summary>
        public bool QuadCountsAsTringla = true;

        /// <summary>
        /// Provisional MVP simplification of RL-15. The reference uses a ~15 s window;
        /// offline we close the window after this many plays in the deal (2 = each
        /// player must announce before their first card). Announcements stay hidden
        /// until the window closes, then are resolved once.
        /// </summary>
        public int AnnounceWindowPlays = 2;

        /// <summary>Verified from tutorial text (RL-17): ronda points cannot lift a score above target - 1.</summary>
        public bool RondaCannotWin = true;

        /// <summary>Verified (RL-20): +5 to the non-dealer when the dealer did not make the last capture.</summary>
        public int DealerPenaltyPoints = 5;

        /// <summary>Verified (RL-21): one point per captured card above this count.</summary>
        public int PileThreshold = 20;

        /// <summary>Provisional (RL-04): the dealer alternates after every deck.</summary>
        public bool DealerAlternates = true;
    }
}
