using System;

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

        // ---------------------------------------------------------------------------------
        // Unverified assumptions H-1 … H-10 (docs/mvp/06_test_fixture_and_rules_config.md, Part D).
        // The reference game's behaviour is UNKNOWN for each of them. Every default is the
        // game's behaviour before these settings existed — a provisional choice, not a
        // verified rule. Where an enum lists a single option, the documented alternative is not
        // defined precisely enough to implement without inventing a rule; the option list grows
        // only when evidence or the product owner defines it.
        // ---------------------------------------------------------------------------------

        /// <summary>H-1 (M-11, U-8). Who plays first in every deal.</summary>
        public FirstPlayer FirstPlayer = FirstPlayer.NonDealer;

        /// <summary>
        /// H-2 (M-36, U-4, C-3). Both players' best announcements are equal (same kind and rank).
        /// Only option: nobody scores. Not implemented: the tutorial's "nobody scores yet, later
        /// Derbas on that number pay the other side" — who receives which points is not defined.
        /// </summary>
        public AnnouncementTie AnnouncementTie = AnnouncementTie.NobodyScores;

        /// <summary>H-3 (M-50, U-7). Both players at or above the target after one end-of-deck tally.</summary>
        public BothReachTarget BothReachTarget = BothReachTarget.HigherScoreWinsTieReplays;

        /// <summary>H-4 (M-04, U-10). The upward chain continues from 12 to 1.</summary>
        public bool ChainWrapsAfterTwelve = false;

        /// <summary>
        /// H-5 (M-23). A Derba can take the card the opponent dropped on the last play of the
        /// previous deal (same deck). Never across decks.
        /// </summary>
        public bool DerbaAcrossDeals = true;

        /// <summary>
        /// H-6 (M-48). When a point reaches the target during play, further points from the same
        /// play or the same announcement award still count (e.g. a Missa after a winning Derba).
        /// The winner does not change; the final score can be higher.
        /// </summary>
        public bool PointsAfterWinningPointCount = false;

        /// <summary>
        /// H-7 (doc 03 §12). Announcements still sealed when the match ends during play.
        /// Only option: discarded. No alternative is documented.
        /// </summary>
        public PendingAnnouncementsAtMatchEnd PendingAnnouncementsAtMatchEnd = PendingAnnouncementsAtMatchEnd.Discard;

        /// <summary>
        /// H-8 (M-38, U-12). How the 40 cap treats an announcement award that contains a tringla.
        /// Only option: the whole award is uncapped. Not implemented: "only the tringla's own value
        /// is uncapped" has two readings with different final scores (capped part before or after
        /// the tringla). In 1v1 neither reading can change the winner.
        /// </summary>
        public TringlaAwardCap TringlaAwardCap = TringlaAwardCap.WholeAwardUncapped;

        /// <summary>
        /// H-9 (M-29, U-5). Only used when <see cref="QuadCountsAsTringla"/> is false: four of a kind
        /// counts as two announcements (two rondas) for the opponent's "+1 per announcement" bonus,
        /// instead of one announcement worth 2.
        /// </summary>
        public bool QuadCountsAsTwoAnnouncements = false;

        /// <summary>
        /// H-10 (M-10). The first dealer of a match: null = random (from the match seed), or seat
        /// 0 / 1. StartMatch(firstDealer) still overrides it.
        /// </summary>
        public int? FirstDealerSeat = null;

        /// <summary>
        /// Throws <see cref="ArgumentException"/> when a setting holds a value the engine does not
        /// implement (e.g. a number cast to one of the enums above), so an unsupported rule can
        /// never be switched on by accident. Called when a match starts.
        /// </summary>
        public void Validate()
        {
            RequireDefined(FirstPlayer, nameof(FirstPlayer));
            RequireDefined(AnnouncementTie, nameof(AnnouncementTie));
            RequireDefined(BothReachTarget, nameof(BothReachTarget));
            RequireDefined(PendingAnnouncementsAtMatchEnd, nameof(PendingAnnouncementsAtMatchEnd));
            RequireDefined(TringlaAwardCap, nameof(TringlaAwardCap));
            if (FirstDealerSeat.HasValue && FirstDealerSeat != 0 && FirstDealerSeat != 1)
            {
                throw new ArgumentException("RulesConfig.FirstDealerSeat must be null, 0 or 1.");
            }
        }

        private static void RequireDefined<T>(T value, string name) where T : Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentException("RulesConfig." + name + " has no implemented option " + Convert.ToInt32(value) + ".");
            }
        }
    }

    /// <summary>H-1: who plays first in every deal.</summary>
    public enum FirstPlayer
    {
        /// <summary>The player who did not deal (current behaviour).</summary>
        NonDealer,
        /// <summary>The dealer.</summary>
        Dealer
    }

    /// <summary>H-2: equal best announcements.</summary>
    public enum AnnouncementTie
    {
        /// <summary>Nobody scores for announcements in that deal (current behaviour).</summary>
        NobodyScores
    }

    /// <summary>H-3: both players at or above the target after one tally.</summary>
    public enum BothReachTarget
    {
        /// <summary>The higher score wins; an exact tie plays another deck (current behaviour).</summary>
        HigherScoreWinsTieReplays,
        /// <summary>Always play another deck (Phase 3 alternative "extraDeck").</summary>
        AlwaysPlayAnotherDeck
    }

    /// <summary>H-7: sealed announcements when the match ends during play.</summary>
    public enum PendingAnnouncementsAtMatchEnd
    {
        /// <summary>They are not scored (current behaviour).</summary>
        Discard
    }

    /// <summary>H-8: the ronda cap and awards that contain a tringla.</summary>
    public enum TringlaAwardCap
    {
        /// <summary>No part of the award is capped (current behaviour).</summary>
        WholeAwardUncapped
    }
}
