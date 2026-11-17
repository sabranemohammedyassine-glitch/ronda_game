using NUnit.Framework;
using Zellige.App;

namespace Zellige.Tests.LocalData
{
    public sealed class ProfileTests : LocalDataTestBase
    {
        [Test]
        public void FirstLaunch_CreatesTheDefaultProfile_AndSavesIt()
        {
            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Created, store.ProfileStatus);
            Assert.AreEqual("Player", store.Profile.DisplayName);
            Assert.AreEqual(AvatarCatalog.DefaultId, store.Profile.AvatarId);
            Assert.AreEqual(Timestamps.Format(T0), store.Profile.CreatedUtc);
            Assert.AreEqual(Timestamps.Format(T0), store.Profile.ModifiedUtc);
            Assert.AreEqual(PlayerProfile.CurrentSchemaVersion, store.Profile.SchemaVersion);
            Assert.IsTrue(store.CanSave(LocalDataStore.ProfileName));
            FileAssert.Exists(FilePath(LocalDataStore.ProfileName));
        }

        [Test]
        public void SaveAndReload_KeepsNameAvatarAndCreationTime()
        {
            LocalDataStore first = LoadStore();
            Assert.AreEqual(NameError.None, first.Profile.TrySetDisplayName("Yassine", T0.AddHours(1)));
            Assert.IsTrue(first.Profile.TrySetAvatar("avatar_02", T0.AddHours(1)));
            Assert.IsTrue(first.SaveProfile());

            LocalDataStore second = LoadStore(T0.AddDays(3));   // a later app launch

            Assert.AreEqual(LoadStatus.Loaded, second.ProfileStatus);
            Assert.AreEqual("Yassine", second.Profile.DisplayName);
            Assert.AreEqual("avatar_02", second.Profile.AvatarId);
            Assert.AreEqual(Timestamps.Format(T0), second.Profile.CreatedUtc, "the creation time never changes");
            Assert.AreEqual(Timestamps.Format(T0.AddHours(1)), second.Profile.ModifiedUtc);
        }

        [Test]
        public void ExistingValidProfile_IsNeverReplacedByDefaults()
        {
            LocalDataStore first = LoadStore();
            first.Profile.TrySetDisplayName("Amine", T0);
            first.SaveProfile();
            string saved = ReadFile(LocalDataStore.ProfileName);

            LoadStore(T0.AddDays(1));
            LoadStore(T0.AddDays(2));

            Assert.AreEqual(saved, ReadFile(LocalDataStore.ProfileName), "loading must not rewrite a valid file");
        }

        [TestCase("Yassine", "Yassine")]
        [TestCase("  Yassine  ", "Yassine")]
        [TestCase("Al   Amine", "Al Amine")]
        [TestCase("Zoé-Ça_1.", "Zoé-Ça_1.")]
        [TestCase("ABCDEFGHIJKLMNOP", "ABCDEFGHIJKLMNOP")]   // 16 characters: the maximum
        public void ValidNames_AreAccepted_AndNormalised(string input, string expected)
        {
            Assert.AreEqual(NameError.None, PlayerProfile.ValidateName(input, out string normalized));
            Assert.AreEqual(expected, normalized);
        }

        [TestCase(null, NameError.Empty)]
        [TestCase("", NameError.Empty)]
        [TestCase("    ", NameError.Empty)]
        [TestCase("ABCDEFGHIJKLMNOPQ", NameError.TooLong)]   // 17 characters
        [TestCase("Bad\tName", NameError.InvalidCharacters)]
        [TestCase("Two\nLines", NameError.InvalidCharacters)]
        [TestCase("<b>Bold</b>", NameError.InvalidCharacters)]
        [TestCase("ياسين", NameError.InvalidCharacters)]   // no Arabic glyphs in the current font (R10)
        [TestCase("Smile\U0001F600", NameError.InvalidCharacters)]
        public void InvalidNames_AreRefused_WithAReason(string input, NameError expected)
        {
            Assert.AreEqual(expected, PlayerProfile.ValidateName(input, out string normalized));
            Assert.IsNull(normalized);
            Assert.IsTrue(TextCatalog.Has(PlayerProfile.MessageKey(expected)), "every refusal has a message to show");
        }

        [Test]
        public void InvalidRenameOrAvatar_ChangesNothing()
        {
            LocalDataStore store = LoadStore();
            store.Profile.TrySetDisplayName("Yassine", T0);

            Assert.AreEqual(NameError.TooLong, store.Profile.TrySetDisplayName(new string('x', 40), T0.AddHours(1)));
            Assert.AreEqual(NameError.Empty, store.Profile.TrySetDisplayName("", T0.AddHours(1)));
            Assert.IsFalse(store.Profile.TrySetAvatar("avatar_99", T0.AddHours(1)));
            Assert.IsFalse(store.Profile.TrySetAvatar(null, T0.AddHours(1)));

            Assert.AreEqual("Yassine", store.Profile.DisplayName);
            Assert.AreEqual(AvatarCatalog.DefaultId, store.Profile.AvatarId);
            Assert.AreEqual(Timestamps.Format(T0), store.Profile.ModifiedUtc);
        }

        [Test]
        public void InvalidFieldOnDisk_IsRepaired_ValidFieldsAreKept_AndTheOriginalIsBackedUp()
        {
            const string original =
                "{\"schemaVersion\":1,\"displayName\":\"<script>\",\"avatarId\":\"avatar_02\"," +
                "\"createdUtc\":\"2026-01-02T03:04:05.0000000Z\",\"modifiedUtc\":\"not a date\"}";
            WriteFile(LocalDataStore.ProfileName, original);

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Repaired, store.ProfileStatus);
            Assert.AreEqual("Player", store.Profile.DisplayName, "invalid name → default");
            Assert.AreEqual("avatar_02", store.Profile.AvatarId, "valid avatar kept");
            Assert.AreEqual("2026-01-02T03:04:05.0000000Z", store.Profile.CreatedUtc, "valid creation time kept");
            Assert.AreEqual(store.Profile.CreatedUtc, store.Profile.ModifiedUtc);
            Assert.AreEqual(original, ReadFile(LocalDataStore.ProfileName + LocalDataStore.CorruptSuffix));
            Assert.AreEqual(LoadStatus.Loaded, LoadStore().ProfileStatus, "the repaired file was saved");
        }

        [Test]
        public void UnknownAvatarOnDisk_FallsBackToTheDefaultAvatar()
        {
            WriteFile(LocalDataStore.ProfileName,
                "{\"schemaVersion\":1,\"displayName\":\"Yassine\",\"avatarId\":\"avatar_77\"," +
                "\"createdUtc\":\"2026-01-02T03:04:05.0000000Z\",\"modifiedUtc\":\"2026-01-02T03:04:05.0000000Z\"}");

            LocalDataStore store = LoadStore();

            Assert.AreEqual(LoadStatus.Repaired, store.ProfileStatus);
            Assert.AreEqual("Yassine", store.Profile.DisplayName);
            Assert.AreEqual(AvatarCatalog.DefaultId, store.Profile.AvatarId);
        }
    }
}
