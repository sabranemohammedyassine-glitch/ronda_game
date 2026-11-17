using System;
using System.Reflection;
using NUnit.Framework;
using Zellige.App;

namespace Zellige.Tests.LocalData
{
    public sealed class TextCatalogTests
    {
        [Test]
        public void EveryTextKey_HasNonEmptyEnglishText()
        {
            foreach (FieldInfo field in typeof(TextKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var key = (string)field.GetValue(null);
                Assert.IsTrue(TextCatalog.Has(key), field.Name + " (" + key + ") has no English text");
                Assert.IsFalse(string.IsNullOrWhiteSpace(TextCatalog.Get(key)), field.Name);
            }
        }

        [Test]
        public void TextKeys_AreUnique()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (FieldInfo field in typeof(TextKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                Assert.IsTrue(seen.Add((string)field.GetValue(null)), "duplicate key in " + field.Name);
            }
        }

        [Test]
        public void MissingKey_ShowsAVisibleMarker_InsteadOfFailing()
        {
            Assert.AreEqual("[no.such.key]", TextCatalog.Get("no.such.key"));
            Assert.AreEqual("[]", TextCatalog.Get(null));
        }

        [Test]
        public void EveryModeAndNameError_HasText()
        {
            foreach (MatchMode mode in Enum.GetValues(typeof(MatchMode)))
            {
                Assert.IsTrue(TextCatalog.Has(MatchModes.NameKey(mode)), mode.ToString());
            }
            foreach (NameError error in Enum.GetValues(typeof(NameError)))
            {
                if (error == NameError.None) continue;
                Assert.IsTrue(TextCatalog.Has(PlayerProfile.MessageKey(error)), error.ToString());
            }
            Assert.AreEqual("Me vs Computer", TextCatalog.Get(MatchModes.NameKey(MatchMode.VsComputer)));
            Assert.AreEqual("1 vs 1", TextCatalog.Get(MatchModes.NameKey(MatchMode.Online1v1)));
            Assert.AreEqual("2 vs 2", TextCatalog.Get(MatchModes.NameKey(MatchMode.Online2v2)));
        }

        [Test]
        public void DefaultProfileName_ComesFromTheTextTable_AndIsAValidName()
        {
            Assert.AreEqual("Player", PlayerProfile.DefaultName);
            Assert.AreEqual(NameError.None, PlayerProfile.ValidateName(PlayerProfile.DefaultName, out _));
        }
    }
}
