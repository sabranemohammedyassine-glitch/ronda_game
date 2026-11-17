using System;
using System.Collections.Generic;
using System.Linq;
using Zellige.Core;

namespace Zellige.Tests
{
    /// <summary>
    /// Short, readable game states for tests, built on the engine's internal
    /// <see cref="RondaGame.FromSetup"/> (test-only, see Core/AssemblyInfo.cs).
    /// Place the cards that matter (hands, table, stock), then <see cref="PilesWithRest"/> puts
    /// every remaining card into the captured piles so the state holds all 40 cards.
    /// Only pile <em>sizes</em> matter for scoring, so which cards fill them is irrelevant.
    /// </summary>
    internal sealed class StateBuilder
    {
        private readonly GameSetup setup = new GameSetup();

        public StateBuilder(RulesConfig rules = null)
        {
            setup.Rules = rules ?? new RulesConfig();
        }

        public static Card C(Suit suit, int rank) => new Card(suit, rank);

        public StateBuilder Dealer(int seat) { setup.Dealer = seat; return this; }
        public StateBuilder Turn(int seat) { setup.CurrentPlayer = seat; return this; }
        public StateBuilder Deal(int deckRound, int dealNumber) { setup.DeckRound = deckRound; setup.DealNumber = dealNumber; return this; }
        public StateBuilder Scores(int seat0, int seat1) { setup.Scores = new[] { seat0, seat1 }; return this; }
        public StateBuilder Hand(int seat, params Card[] cards) { setup.Hands[seat] = cards.ToList(); return this; }
        public StateBuilder Table(params Card[] cards) { setup.Table = cards.ToList(); return this; }
        public StateBuilder Stock(params Card[] cards) { setup.Stock = cards.ToList(); return this; }
        public StateBuilder LastCapturer(int seat) { setup.LastCapturer = seat; return this; }
        public StateBuilder Seed(int seed) { setup.Seed = seed; return this; }

        public StateBuilder LastPlay(int seat, Card card, bool wasDrop, int deal = 0)
        {
            setup.LastPlay = new PreviousPlay { Seat = seat, Card = card, WasDrop = wasDrop, Deal = deal };
            return this;
        }

        public StateBuilder AnnounceWindow(int playsSoFar)
        {
            setup.AnnounceWindowOpen = true;
            setup.PlaysThisDeal = playsSoFar;
            return this;
        }

        /// <summary>
        /// Every card not placed yet goes into the piles: <paramref name="seat0Pile"/> cards to seat 0,
        /// the rest to seat 1. A capturing <see cref="LastPlay"/> card is put in its player's pile.
        /// </summary>
        public StateBuilder PilesWithRest(int seat0Pile)
        {
            var placed = new HashSet<Card>(setup.Hands[0].Concat(setup.Hands[1]).Concat(setup.Table).Concat(setup.Stock));
            var piles = new[] { new List<Card>(), new List<Card>() };
            if (setup.LastPlay != null && !setup.LastPlay.WasDrop)
            {
                piles[setup.LastPlay.Seat].Add(setup.LastPlay.Card);
                placed.Add(setup.LastPlay.Card);
            }
            foreach (Card card in Deck.CreateStandard().Where(c => !placed.Contains(c)))
            {
                (piles[0].Count < seat0Pile ? piles[0] : piles[1]).Add(card);
            }
            if (piles[0].Count != seat0Pile)
            {
                throw new InvalidOperationException("Not enough free cards for a seat-0 pile of " + seat0Pile);
            }
            setup.Piles = piles;
            return this;
        }

        public GameSetup Setup => setup;

        public RondaGame Build() => RondaGame.FromSetup(setup);
    }
}
