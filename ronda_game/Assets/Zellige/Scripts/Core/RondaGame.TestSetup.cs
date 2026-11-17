// Editor only: tests run in the Editor, so this test-setup code is left out of player builds.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zellige.Core
{
    /// <summary>
    /// A complete game state to start a test from (plan item A-5,
    /// docs/mvp/06_test_fixture_and_rules_config.md Part C). Internal: only the EditMode test
    /// assembly can see it (AssemblyInfo.cs); the game cannot create or use it.
    /// </summary>
    internal sealed class GameSetup
    {
        public RulesConfig Rules;
        /// <summary>Seed for every later shuffle (decks after the current one).</summary>
        public int Seed = 1;
        public int Dealer;
        public int CurrentPlayer;
        public int DeckRound = 1;
        public int DealNumber = 1;
        public List<Card>[] Hands = { new List<Card>(), new List<Card>() };
        public List<Card> Table = new List<Card>();
        /// <summary>Cards captured in the current deck, per seat.</summary>
        public List<Card>[] Piles = { new List<Card>(), new List<Card>() };
        /// <summary>Undealt cards, top first.</summary>
        public List<Card> Stock = new List<Card>();
        public int[] Scores = new int[2];
        /// <summary>Seat that made the last capture of the current deck, or -1.</summary>
        public int LastCapturer = -1;
        /// <summary>The previous play, used for Derba. Null = no play yet in this deck.</summary>
        public PreviousPlay LastPlay;
        /// <summary>Announcement window state. Closed by default: no announcement pending.</summary>
        public bool AnnounceWindowOpen;
        public int PlaysThisDeal;
    }

    /// <summary>The previous play of the deck (who, which card, drop or capture, in which deal).</summary>
    internal sealed class PreviousPlay
    {
        public int Seat;
        public Card Card;
        public bool WasDrop;
        /// <summary>Deal the play was made in; 0 means "the current deal".</summary>
        public int Deal;
    }

    public sealed partial class RondaGame
    {
        /// <summary>
        /// Test-only: creates a game already in the given state, ready for the next command.
        /// Nothing is dealt or scored here; from then on the game runs through the normal public
        /// commands (PlayCard, Announce), so every rule under test is production code.
        /// Throws <see cref="ArgumentException"/> when the state is impossible.
        /// </summary>
        internal static RondaGame FromSetup(GameSetup setup)
        {
            Validate(setup);

            var game = new RondaGame(setup.Rules, setup.Seed);
            game.started = true;
            game.Dealer = setup.Dealer;
            game.CurrentPlayer = setup.CurrentPlayer;
            game.DeckRound = setup.DeckRound;
            game.DealNumber = setup.DealNumber;
            for (int seat = 0; seat < PlayerCount; seat++)
            {
                game.hands[seat].AddRange(setup.Hands[seat]);
                game.piles[seat].AddRange(setup.Piles[seat]);
                game.scores[seat] = setup.Scores[seat];
            }
            game.table.AddRange(setup.Table);
            game.stock.AddRange(setup.Stock);
            game.lastCapturer = setup.LastCapturer;
            if (setup.LastPlay != null)
            {
                game.hasLastPlay = true;
                game.lastPlaySeat = setup.LastPlay.Seat;
                game.lastPlayCard = setup.LastPlay.Card;
                game.lastPlayWasDrop = setup.LastPlay.WasDrop;
                game.lastPlayDeal = setup.LastPlay.Deal == 0 ? setup.DealNumber : setup.LastPlay.Deal;
            }
            game.announceWindowOpen = setup.AnnounceWindowOpen;
            game.playsThisDeal = setup.PlaysThisDeal;
            return game;
        }

        private static void Validate(GameSetup s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            Require(s.Rules != null, "Rules are required.");
            s.Rules.Validate();
            Require(s.Hands?.Length == 2 && s.Piles?.Length == 2 && s.Scores?.Length == 2,
                "Hands, Piles and Scores need one entry per seat.");
            Require(s.Dealer == 0 || s.Dealer == 1, "Dealer must be 0 or 1.");
            Require(s.CurrentPlayer == 0 || s.CurrentPlayer == 1, "CurrentPlayer must be 0 or 1.");
            Require(s.DeckRound >= 1 && s.DealNumber >= 1, "DeckRound and DealNumber start at 1.");

            var all = s.Hands[0].Concat(s.Hands[1]).Concat(s.Table).Concat(s.Piles[0]).Concat(s.Piles[1]).Concat(s.Stock).ToList();
            Require(all.Count == Deck.Size && new HashSet<Card>(all).Count == Deck.Size,
                "Hands, table, piles and stock must hold each of the 40 cards exactly once (found " + all.Count + ").");

            int cur = s.Hands[s.CurrentPlayer].Count;
            int other = s.Hands[1 - s.CurrentPlayer].Count;
            Require(s.Hands[0].Count <= s.Rules.CardsPerDeal && s.Hands[1].Count <= s.Rules.CardsPerDeal,
                "A hand holds at most " + s.Rules.CardsPerDeal + " cards.");
            Require(cur >= 1, "The player to move must hold a card.");
            Require(cur == other || cur == other + 1,
                "Players alternate: the player to move holds as many cards as the other, or one more.");

            Require(s.Table.Select(c => c.Rank).Distinct().Count() == s.Table.Count,
                "At most one card of each rank on the table (current capture model).");

            Require(s.Scores.All(score => score >= 0 && score < s.Rules.TargetScore),
                "Scores must be below the target: the match cannot already be over.");

            Require(s.LastCapturer >= -1 && s.LastCapturer <= 1, "LastCapturer must be -1, 0 or 1.");
            bool anyPile = s.Piles[0].Count > 0 || s.Piles[1].Count > 0;
            Require(anyPile == (s.LastCapturer >= 0),
                "Someone has captured this deck exactly when a pile is not empty, and then LastCapturer names them.");
            if (s.LastCapturer >= 0) Require(s.Piles[s.LastCapturer].Count > 0, "The last capturer's pile cannot be empty.");

            if (s.LastPlay != null)
            {
                Require(s.LastPlay.Seat == 0 || s.LastPlay.Seat == 1, "LastPlay.Seat must be 0 or 1.");
                Require(s.LastPlay.WasDrop ? s.Table.Contains(s.LastPlay.Card) : s.Piles[s.LastPlay.Seat].Contains(s.LastPlay.Card),
                    "A dropped LastPlay card must be on the table; a capturing one must be in that player's pile.");
                Require(s.LastPlay.WasDrop || s.LastCapturer == s.LastPlay.Seat,
                    "If the previous play was a capture, its player is the last capturer.");
                Require(s.LastPlay.Deal >= 0 && s.LastPlay.Deal <= s.DealNumber,
                    "LastPlay.Deal must be 0 (the current deal) or a deal of this deck up to the current one.");
            }

            Require(s.PlaysThisDeal >= 0, "PlaysThisDeal cannot be negative.");
            Require(!s.AnnounceWindowOpen || s.PlaysThisDeal < s.Rules.AnnounceWindowPlays,
                "An open announcement window must not have reached its closing play.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new ArgumentException("Invalid GameSetup: " + message);
        }
    }
}
#endif
