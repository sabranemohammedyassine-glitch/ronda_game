using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Zellige.App;

namespace Zellige.Tests.LocalData
{
    public sealed class PersistenceTests : LocalDataTestBase
    {
        [Test]
        public void MissingFolderAndFiles_CreateDefaults_WithoutErrors()
        {
            Assert.IsFalse(Directory.Exists(Folder));

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Created, store.ProfileStatus);
            Assert.AreEqual(LoadStatus.Created, store.SettingsStatus);
            Assert.AreEqual(LoadStatus.Created, store.HistoryStatus);
            Assert.AreEqual(0, store.History.Records.Count);
            FileAssert.Exists(FilePath(LocalDataStore.ProfileName));
            FileAssert.Exists(FilePath(LocalDataStore.SettingsName));
            FileAssert.Exists(FilePath(LocalDataStore.HistoryName));
        }

        [TestCase("{not json")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("[1,2,3]")]
        [TestCase("{}")]                              // valid JSON but no schema version: not our document
        [TestCase("{\"schemaVersion\":0}")]
        [TestCase("\u0000\u0001garbage")]
        public void CorruptFile_IsKeptAside_AndDefaultsAreUsed(string content)
        {
            WriteFile(LocalDataStore.SettingsName, content);

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Reset, store.SettingsStatus);
            Assert.IsTrue(store.Settings.SoundEnabled);
            Assert.AreEqual(CardPlayMode.TwoStep, store.Settings.CardPlayMode);
            Assert.AreEqual(content, ReadFile(LocalDataStore.SettingsName + LocalDataStore.CorruptSuffix), "the original is kept for inspection");
            Assert.AreEqual(LoadStatus.Loaded, LoadStore().SettingsStatus, "a valid default file replaced the corrupt one");
        }

        [Test]
        public void CorruptHistory_DoesNotAffectTheOtherFiles()
        {
            LocalDataStore first = LoadStore();
            first.Profile.TrySetDisplayName("Yassine", T0);
            first.SaveProfile();
            WriteFile(LocalDataStore.HistoryName, "{\"records\": [ oops");

            LocalDataStore second = LoadStore();

            Assert.AreEqual(LoadStatus.Reset, second.HistoryStatus);
            Assert.AreEqual(LoadStatus.Loaded, second.ProfileStatus);
            Assert.AreEqual("Yassine", second.Profile.DisplayName);
        }

        [Test]
        public void FileFromANewerGameVersion_IsNeverOverwritten()
        {
            const string newer = "{\"schemaVersion\":99,\"soundEnabled\":false,\"cardPlayMode\":2,\"futureField\":true}";
            WriteFile(LocalDataStore.SettingsName, newer);

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Protected, store.SettingsStatus);
            Assert.IsFalse(store.CanSave(LocalDataStore.SettingsName));
            Assert.IsTrue(store.Settings.SoundEnabled, "defaults are used in memory");
            store.Settings.SoundEnabled = false;
            Assert.IsFalse(store.SaveSettings(), "saving is refused");
            Assert.AreEqual(newer, ReadFile(LocalDataStore.SettingsName), "the newer file is untouched");
        }

        [Test]
        public void FailedSave_ReturnsFalse_AndKeepsThePreviousFile()
        {
            LocalDataStore store = LoadStore();
            store.Settings.SoundEnabled = false;
            Assert.IsTrue(store.SaveSettings());
            string before = ReadFile(LocalDataStore.SettingsName);

            // Make the write fail: a folder sits where the temporary file must be created.
            string blocker = FilePath(LocalDataStore.SettingsName) + ".tmp";
            Directory.CreateDirectory(blocker);
            store.Settings.SoundEnabled = true;

            Assert.IsFalse(store.SaveSettings());
            Assert.AreEqual(before, ReadFile(LocalDataStore.SettingsName));
            Assert.IsFalse(LoadStore().Settings.SoundEnabled, "the last good value is still what loads");

            Directory.Delete(blocker);
            Assert.IsTrue(store.SaveSettings(), "saving works again once the problem is gone");
            Assert.IsTrue(LoadStore().Settings.SoundEnabled);
        }

        [Test]
        public void FailedHistorySave_IsReported_ButTheMatchIsStillCountedInMemory()
        {
            LocalDataStore store = LoadStore();
            Directory.CreateDirectory(FilePath(LocalDataStore.HistoryName) + ".tmp");
            var session = new MatchSession(MatchConfig.VsComputer("m1"));
            session.Begin(T0);

            RecordResult result = store.CompleteMatch(session, 0, new[] { 41, 20 }, 3, T0.AddMinutes(9), out bool saved);

            Assert.AreEqual(RecordResult.Recorded, result);
            Assert.IsFalse(saved);
            Assert.AreEqual(1, store.History.Records.Count);
        }

        [Test]
        public void WriteInterruptedBeforeTheNewFileWasInPlace_RestoresTheLastGoodFile()
        {
            LocalDataStore first = LoadStore();
            first.Settings.TrySetCardPlayMode(CardPlayMode.OneTap);
            first.SaveSettings();
            string path = FilePath(LocalDataStore.SettingsName);
            // Simulate a crash between "move current to .old" and "move .tmp into place".
            File.Move(path, path + ".old");
            File.WriteAllText(path + ".tmp", "{\"schemaVersion\":1,\"sound");

            LocalDataStore second = LoadStore();

            Assert.AreEqual(LoadStatus.Loaded, second.SettingsStatus);
            Assert.AreEqual(CardPlayMode.OneTap, second.Settings.CardPlayMode);
            FileAssert.DoesNotExist(path + ".old");
            FileAssert.DoesNotExist(path + ".tmp");
        }

        [Test]
        public void WriteInterruptedAfterTheNewFileWasInPlace_KeepsTheNewFile()
        {
            LocalDataStore first = LoadStore();
            first.Settings.TrySetCardPlayMode(CardPlayMode.OneTap);
            first.SaveSettings();
            string path = FilePath(LocalDataStore.SettingsName);
            // Simulate a crash after the new file was moved into place but before the previous copy was deleted.
            File.WriteAllText(path + ".old", "{\"schemaVersion\":1,\"soundEnabled\":true,\"cardPlayMode\":1}");

            LocalDataStore second = LoadStore();

            Assert.AreEqual(CardPlayMode.OneTap, second.Settings.CardPlayMode, "the complete new file wins");
            FileAssert.DoesNotExist(path + ".old");
        }

        [Test]
        public void UnreadableFile_IsProtected_AndNotOverwritten()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("Exclusive file locks are only enforced on Windows.");
            }

            LocalDataStore first = LoadStore();
            first.Profile.TrySetDisplayName("Yassine", T0);
            first.SaveProfile();
            string before = ReadFile(LocalDataStore.ProfileName);

            LocalDataStore second;
            using (new FileStream(FilePath(LocalDataStore.ProfileName), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                second = LoadStore();
                Assert.AreEqual(LoadStatus.Protected, second.ProfileStatus);
                Assert.IsFalse(second.SaveProfile());
            }

            Assert.AreEqual(before, ReadFile(LocalDataStore.ProfileName));
            Assert.AreEqual("Yassine", LoadStore().Profile.DisplayName);
        }

        [Test]
        public void SavingOrRecordingBeforeLoading_IsAProgrammingError()
        {
            var store = new LocalDataStore(Folder);

            Assert.Throws<InvalidOperationException>(() => store.SaveSettings());
            Assert.Throws<InvalidOperationException>(() => store.AbandonMatch(new MatchSession(MatchConfig.VsComputer()), out _));
        }

        // ---------- Serialisation compatibility: version-1 files written by this release must always load ----------

        [Test]
        public void Version1Files_LoadWithTheExpectedFieldNamesAndValues()
        {
            WriteFile(LocalDataStore.ProfileName,
                "{\"schemaVersion\":1,\"displayName\":\"Zoé\",\"avatarId\":\"avatar_02\"," +
                "\"createdUtc\":\"2026-11-01T08:00:00.0000000Z\",\"modifiedUtc\":\"2026-11-02T09:30:00.0000000Z\"}");
            WriteFile(LocalDataStore.SettingsName, "{\"schemaVersion\":1,\"soundEnabled\":false,\"cardPlayMode\":2}");
            WriteFile(LocalDataStore.HistoryName,
                "{\"schemaVersion\":1,\"records\":[{\"matchId\":\"abc\",\"mode\":1,\"opponentName\":\"Nadia\"," +
                "\"startedUtc\":\"2026-11-17T12:00:00.0000000Z\",\"completedUtc\":\"2026-11-17T12:10:00.0000000Z\"," +
                "\"outcome\":2,\"playerScore\":30,\"opponentScore\":41,\"decksPlayed\":5}]," +
                "\"lifetime\":{\"completed\":7,\"victories\":4,\"defeats\":3,\"abandoned\":1,\"bestWinningMargin\":19,\"highestScore\":44}}");

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Loaded, store.ProfileStatus);
            Assert.AreEqual(LoadStatus.Loaded, store.SettingsStatus);
            Assert.AreEqual(LoadStatus.Loaded, store.HistoryStatus);
            Assert.AreEqual("Zoé", store.Profile.DisplayName);
            Assert.AreEqual("avatar_02", store.Profile.AvatarId);
            Assert.AreEqual("2026-11-01T08:00:00.0000000Z", store.Profile.CreatedUtc);
            Assert.IsFalse(store.Settings.SoundEnabled);
            Assert.AreEqual(CardPlayMode.OneTap, store.Settings.CardPlayMode);
            MatchRecord record = store.History.Records[0];
            Assert.AreEqual(MatchOutcome.Defeat, record.Outcome);
            Assert.AreEqual(30, record.PlayerScore);
            Assert.AreEqual(5, record.DecksPlayed);
            Assert.AreEqual(7, store.History.Lifetime.Completed);
            Assert.AreEqual(1, store.History.Lifetime.Abandoned);
            Assert.AreEqual(19, store.History.Lifetime.BestWinningMargin);
        }

        [Test]
        public void SavedFiles_UseTheVersion1FieldNames()
        {
            LocalDataStore store = LoadStore();
            Finish(store, new MatchSession(MatchConfig.VsComputer("m1")), 0, 41, 9);

            string profile = ReadFile(LocalDataStore.ProfileName);
            string settings = ReadFile(LocalDataStore.SettingsName);
            string history = ReadFile(LocalDataStore.HistoryName);

            foreach (string field in new[] { "\"schemaVersion\": 1", "\"displayName\"", "\"avatarId\"", "\"createdUtc\"", "\"modifiedUtc\"" })
                StringAssert.Contains(field, profile);
            foreach (string field in new[] { "\"schemaVersion\": 1", "\"soundEnabled\": true", "\"cardPlayMode\": 1" })
                StringAssert.Contains(field, settings);
            foreach (string field in new[] { "\"schemaVersion\": 1", "\"records\"", "\"matchId\": \"m1\"", "\"mode\": 1",
                                             "\"outcome\": 1", "\"playerScore\": 41", "\"opponentScore\": 9", "\"lifetime\"" })
                StringAssert.Contains(field, history);
        }

        [Test]
        public void Timestamps_RoundTripInUtc_AndRejectLocalOrMalformedValues()
        {
            string text = Timestamps.Format(T0);
            Assert.AreEqual("2026-11-17T12:00:00.0000000Z", text);
            Assert.IsTrue(Timestamps.TryParse(text, out DateTime parsed));
            Assert.AreEqual(T0, parsed);
            Assert.AreEqual(DateTimeKind.Utc, parsed.Kind);

            Assert.IsFalse(Timestamps.TryParse("2026-11-17T12:00:00", out _), "no zone: ambiguous");
            Assert.IsFalse(Timestamps.TryParse("2026-11-17T12:00:00+01:00", out _));
            Assert.IsFalse(Timestamps.TryParse("yesterday", out _));
            Assert.IsFalse(Timestamps.TryParse(null, out _));
        }
    }
}
