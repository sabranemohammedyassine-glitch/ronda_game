using System;
using System.IO;
using NUnit.Framework;
using Zellige.App;

namespace Zellige.Tests.LocalData
{
    /// <summary>
    /// Every local-data test gets its own empty folder under the system temp folder, deleted after
    /// the test. Nothing touches Application.persistentDataPath (the player's real data), and
    /// all times are fixed, so the tests are deterministic and independent of each other.
    /// </summary>
    public abstract class LocalDataTestBase
    {
        protected static readonly DateTime T0 = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

        protected string Folder { get; private set; }

        [SetUp]
        public void CreateFolder()
        {
            Folder = Path.Combine(Path.GetTempPath(), "zellige-l1-tests", Guid.NewGuid().ToString("N"));
            MatchHandoff.Clear();
        }

        [TearDown]
        public void DeleteFolder()
        {
            MatchHandoff.Clear();
            if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
        }

        protected LocalDataStore LoadStore(DateTime? now = null)
        {
            var store = new LocalDataStore(Folder);
            store.LoadAll(now ?? T0);
            return store;
        }

        protected string FilePath(string name) => Path.Combine(Folder, name + ".json");

        protected void WriteFile(string name, string text)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath(name), text);
        }

        protected string ReadFile(string name) => File.ReadAllText(FilePath(name));

        /// <summary>Plays the bookkeeping of one finished match through the store.</summary>
        protected static RecordResult Finish(LocalDataStore store, MatchSession session, int winner, int score0, int score1,
                                             int decks = 3, int minutes = 10)
        {
            if (session.State == MatchState.Created) session.Begin(T0);
            return store.CompleteMatch(session, winner, new[] { score0, score1 }, decks, T0.AddMinutes(minutes), out _);
        }

        protected static MatchRecord Record(string id, MatchOutcome outcome, int player, int opponent, int decks = 2)
        {
            return new MatchRecord(id, MatchMode.VsComputer, MatchConfig.ComputerOpponentName,
                                   T0, T0.AddMinutes(8), outcome, player, opponent, decks);
        }
    }
}
