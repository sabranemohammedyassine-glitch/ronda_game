using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Zellige.AI;
using Zellige.Core;

namespace Zellige.Tests
{
    /// <summary>
    /// A stable text form of the engine's events, for comparing whole matches. Numbers are written
    /// with the invariant culture, so the text (and the fingerprint) is the same on every machine,
    /// whatever its language settings (e.g. cultures whose minus sign is not "-").
    /// </summary>
    internal static class EventLog
    {
        public static string Canonical(GameEvent e)
        {
            switch (e)
            {
                case DeckStartedEvent d: return "DS" + N(d.DeckRound) + "," + N(d.Dealer);
                case DealStartedEvent d: return "DL" + N(d.DealNumber) + "," + Cards(d.Hands[0]) + "|" + Cards(d.Hands[1]) + "," + N(d.StockRemaining);
                case TurnStartedEvent t: return "T" + N(t.Seat);
                case CardPlayedEvent p: return "P" + N(p.Seat) + "," + N(p.Card.Id) + ">" + Cards(p.Captured);
                case AnnouncementsResolvedEvent a:
                    return "A" + string.Join(";", a.Announcements.Select(x => N(x.Seat) + ":" + N(x.Rank) + ":" + (x.IsTringla ? "True" : "False") + ":" + N(x.Value)))
                               + "w" + N(a.Winner);
                case ScoreAwardedEvent s: return "S" + N(s.Seat) + "," + s.Reason + "," + N(s.Points) + "/" + N(s.Requested) + "=" + N(s.NewScore);
                case DeckTalliedEvent t:
                    return "TL" + Cards(t.Leftover) + ">" + N(t.LeftoverTo) + ",lc" + N(t.LastCapturer) + ",p" + N(t.PileCounts[0]) + "/" + N(t.PileCounts[1]) + ",d" + N(t.Dealer);
                case MatchEndedEvent m: return "M" + N(m.Winner) + "," + N(m.Scores[0]) + "/" + N(m.Scores[1]);
                default: return "?" + e.GetType().Name;
            }
        }

        private static string N(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static string Cards(IEnumerable<Card> cards) => string.Join(".", cards.Select(c => N(c.Id)));

        /// <summary>64-bit FNV-1a, written out so the value never depends on the runtime's string hashing.</summary>
        public static string Fingerprint(IEnumerable<string> lines)
        {
            ulong hash = 14695981039346656037UL;
            foreach (string line in lines)
            {
                foreach (byte b in Encoding.UTF8.GetBytes(line + "\n"))
                {
                    hash ^= b;
                    hash *= 1099511628211UL;
                }
            }
            return hash.ToString("x16");
        }
    }

    /// <summary>
    /// Golden master: proves that the default rules still produce exactly the same matches.
    /// The fingerprints below were recorded on 2026-11-17 from the engine as it was BEFORE the
    /// Phase D configuration work (RondaGame.cs MD5 8b75aed4…). Any change in default behaviour —
    /// a card, a capture, a score, a tally, a winner, the order of events — changes them.
    /// If a deliberate, documented rule change alters default behaviour, record new values and
    /// say why in the commit.
    /// </summary>
    public class GoldenMasterTests
    {
        private const int Matches = 20;

        // Recorded baseline (see class comment).
        private const string AiMatchesFingerprint = "7ff012fe113351e9";
        private const int AiMatchesEventCount = 8563;
        private const string RandomMatchesFingerprint = "390f3d5d08a2cfb2";
        private const int RandomMatchesEventCount = 12722;

        private static List<string> PlayMatches(Func<int, Func<RondaGame, int, Card>> playerFactory, Func<int, bool> announce)
        {
            var lines = new List<string>();
            for (int seed = 1; seed <= Matches; seed++)
            {
                var game = new RondaGame(new RulesConfig(), seed);
                Func<RondaGame, int, Card> choose = playerFactory(seed);
                lines.Add("match " + seed);
                lines.AddRange(game.StartMatch().Select(EventLog.Canonical));
                int plays = 0;
                while (!game.IsMatchOver && plays++ < 20000)
                {
                    int seat = game.CurrentPlayer;
                    if (announce(seed))
                    {
                        foreach (int rank in game.GetAnnounceableRanks(seat)) game.Announce(seat, rank);
                    }
                    lines.AddRange(game.PlayCard(seat, choose(game, seat)).Select(EventLog.Canonical));
                }
                Assert.IsTrue(game.IsMatchOver, "match " + seed + " must finish");
            }
            return lines;
        }

        [Test]
        public void DefaultRules_AiVersusAi_ReproduceTheRecordedMatches()
        {
            List<string> lines = PlayMatches(seed =>
            {
                var ais = new[] { new SimpleAi(seed), new SimpleAi(seed + 1000) };
                return (game, seat) => ais[seat].ChooseCard(game, seat);
            }, seed => true);

            string actual = EventLog.Fingerprint(lines);
            Assert.AreEqual(AiMatchesEventCount, lines.Count, "event count; fingerprint " + actual);
            Assert.AreEqual(AiMatchesFingerprint, actual);
        }

        [Test]
        public void DefaultRules_RandomPlayers_ReproduceTheRecordedMatches()
        {
            List<string> lines = PlayMatches(seed =>
            {
                var rng = new Random(seed * 31);
                return (game, seat) =>
                {
                    List<Card> legal = game.GetLegalCards(seat);
                    return legal[rng.Next(legal.Count)];
                };
            }, seed => seed % 2 == 0);

            string actual = EventLog.Fingerprint(lines);
            Assert.AreEqual(RandomMatchesEventCount, lines.Count, "event count; fingerprint " + actual);
            Assert.AreEqual(RandomMatchesFingerprint, actual);
        }
    }
}
