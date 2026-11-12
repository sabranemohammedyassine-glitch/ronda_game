using System.Collections.Generic;

namespace Zellige.Core
{
    /// <summary>Why points were awarded.</summary>
    public enum ScoreReason
    {
        Missa,
        Derba,
        Announcement,
        AnnouncementBonus,
        DealerPenalty,
        CardsOverThreshold
    }

    /// <summary>A declared pair (ronda) or three/four of a kind (tringla).</summary>
    public sealed class Announcement
    {
        public int Seat;
        public int Rank;
        public bool IsTringla;
        public int Value;
    }

    /// <summary>
    /// The engine reports everything that happened as an ordered list of events.
    /// The UI plays them back one by one; it never has to work out rules itself.
    /// </summary>
    public abstract class GameEvent
    {
    }

    public sealed class DeckStartedEvent : GameEvent
    {
        public int DeckRound;
        public int Dealer;
    }

    public sealed class DealStartedEvent : GameEvent
    {
        public int DealNumber;
        /// <summary>Cards dealt to each seat, index = seat.</summary>
        public List<Card>[] Hands;
        public int StockRemaining;
    }

    public sealed class TurnStartedEvent : GameEvent
    {
        public int Seat;
    }

    public sealed class CardPlayedEvent : GameEvent
    {
        public int Seat;
        public Card Card;
        /// <summary>Table cards taken, in chain order. Empty when the card was dropped.</summary>
        public List<Card> Captured;
        public bool IsCapture => Captured.Count > 0;
    }

    public sealed class AnnouncementsResolvedEvent : GameEvent
    {
        public List<Announcement> Announcements;
        /// <summary>Seat that won the contest, or -1 for a tie.</summary>
        public int Winner;
    }

    public sealed class ScoreAwardedEvent : GameEvent
    {
        public int Seat;
        /// <summary>Points actually added (may be less than Requested because of the ronda cap).</summary>
        public int Points;
        public int Requested;
        public ScoreReason Reason;
        public int NewScore;
    }

    public sealed class DeckTalliedEvent : GameEvent
    {
        /// <summary>Cards still on the table when the deck ran out.</summary>
        public List<Card> Leftover;
        /// <summary>Seat that received the leftover cards (the last capturer), or -1.</summary>
        public int LeftoverTo;
        /// <summary>Seat that made the last capture of the deck, or -1 if nobody captured.</summary>
        public int LastCapturer;
        /// <summary>Final captured-card counts, index = seat.</summary>
        public int[] PileCounts;
        public int Dealer;
    }

    public sealed class MatchEndedEvent : GameEvent
    {
        public int Winner;
        public int[] Scores;
    }
}
