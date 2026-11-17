using NUnit.Framework;
using Zellige.App;

namespace Zellige.Tests.LocalData
{
    public sealed class SettingsTests : LocalDataTestBase
    {
        [Test]
        public void Defaults_MatchTodaysBehaviour_SoundOnAndTwoStepPlay()
        {
            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Created, store.SettingsStatus);
            Assert.IsTrue(store.Settings.SoundEnabled);
            Assert.AreEqual(CardPlayMode.TwoStep, store.Settings.CardPlayMode);
        }

        [Test]
        public void SaveAndReload_KeepsBothSettings()
        {
            LocalDataStore first = LoadStore();
            first.Settings.SoundEnabled = false;
            Assert.IsTrue(first.Settings.TrySetCardPlayMode(CardPlayMode.OneTap));
            Assert.IsTrue(first.SaveSettings());

            LocalDataStore second = LoadStore();

            Assert.AreEqual(LoadStatus.Loaded, second.SettingsStatus);
            Assert.IsFalse(second.Settings.SoundEnabled);
            Assert.AreEqual(CardPlayMode.OneTap, second.Settings.CardPlayMode);
        }

        [TestCase(CardPlayMode.TwoStep, true)]
        [TestCase(CardPlayMode.OneTap, true)]
        [TestCase((CardPlayMode)0, false)]
        [TestCase((CardPlayMode)3, false)]
        [TestCase((CardPlayMode)(-1), false)]
        public void CardPlayMode_OnlyDefinedValuesAreAccepted(CardPlayMode value, bool accepted)
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.TrySetCardPlayMode(CardPlayMode.OneTap);

            Assert.AreEqual(accepted, settings.TrySetCardPlayMode(value));
            Assert.AreEqual(accepted ? value : CardPlayMode.OneTap, settings.CardPlayMode, "a refused value changes nothing");
        }

        [Test]
        public void UndefinedCardPlayModeOnDisk_IsRepairedToTwoStep_AndSoundIsKept()
        {
            WriteFile(LocalDataStore.SettingsName, "{\"schemaVersion\":1,\"soundEnabled\":false,\"cardPlayMode\":7}");

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Repaired, store.SettingsStatus);
            Assert.AreEqual(CardPlayMode.TwoStep, store.Settings.CardPlayMode);
            Assert.IsFalse(store.Settings.SoundEnabled, "the valid sound value is kept");
        }
    }
}
