using System.Collections.Generic;

namespace Zellige.App
{
    /// <summary>
    /// Stable keys for user-facing text (D8). Data and logic code return keys, never English
    /// sentences; the UI turns a key into text through <see cref="TextCatalog"/>. Adding a
    /// language later means adding one table, not editing every screen.
    ///
    /// Naming: lower case, dot-separated, "area.item". Keys are never renamed once shipped
    /// (a future translation file would be keyed by them).
    ///
    /// L1 only adds the keys used by the data layer. Screen texts are added with their screens
    /// (L2 onwards). The existing match screen texts are not migrated in L1.
    /// </summary>
    public static class TextKeys
    {
        // Game modes (D1)
        public const string ModeVsComputer = "mode.vs_computer";
        public const string ModeOnline1v1 = "mode.online_1v1";
        public const string ModeOnline2v2 = "mode.online_2v2";
        public const string ModeUnknown = "mode.unknown";
        public const string ModeComingSoon = "mode.coming_soon";
        public const string ModeNotAvailable = "mode.not_available";

        // Completed-match outcomes (D6: abandoned is not an outcome)
        public const string OutcomeVictory = "outcome.victory";
        public const string OutcomeDefeat = "outcome.defeat";

        // Profile name validation
        public const string NameEmpty = "profile.name.error.empty";
        public const string NameTooLong = "profile.name.error.too_long";
        public const string NameInvalidCharacters = "profile.name.error.invalid_characters";
        public const string ProfileDefaultName = "profile.name.default";
        public const string ProfileSavedOnDevice = "profile.saved_on_device";

        // Settings
        public const string SettingSound = "settings.sound";
        public const string SettingCardPlay = "settings.card_play";
        public const string CardPlayTwoStep = "settings.card_play.two_step";
        public const string CardPlayOneTap = "settings.card_play.one_tap";

        // Saved-data recovery notices
        public const string DataRecovered = "data.recovered";
        public const string DataNotSaved = "data.not_saved";

        // Lobby Home (L2)
        public const string LobbyTitle = "lobby.title";
        public const string LobbySubtitle = "lobby.subtitle";
        public const string ModeVsComputerDetail = "mode.vs_computer.detail";
        public const string ModeOnline1v1Detail = "mode.online_1v1.detail";
        public const string ModeOnline2v2Detail = "mode.online_2v2.detail";
        public const string ModePlay = "mode.play";
        public const string ModeVersus = "mode.versus";

        // Leaving a match (D5) and the result screen (D4)
        public const string LeaveTitle = "leave.title";
        public const string LeaveMessage = "leave.message";
        public const string LeaveCancel = "leave.cancel";
        public const string LeaveConfirm = "leave.confirm";
        public const string ResultLobby = "result.lobby";
    }

    /// <summary>English, the initial release language (D8).</summary>
    internal static class EnglishText
    {
        public static readonly Dictionary<string, string> Table = new Dictionary<string, string>
        {
            { TextKeys.ModeVsComputer, "Me vs Computer" },
            { TextKeys.ModeOnline1v1, "1 vs 1" },
            { TextKeys.ModeOnline2v2, "2 vs 2" },
            { TextKeys.ModeUnknown, "Unknown mode" },
            { TextKeys.ModeComingSoon, "Coming Soon" },
            { TextKeys.ModeNotAvailable, "This mode is not available yet." },

            { TextKeys.OutcomeVictory, "Victory" },
            { TextKeys.OutcomeDefeat, "Defeat" },

            { TextKeys.NameEmpty, "Please enter a name." },
            { TextKeys.NameTooLong, "Names can have at most 16 characters." },
            { TextKeys.NameInvalidCharacters, "Use letters, digits, spaces and - _ . only." },
            { TextKeys.ProfileDefaultName, "Player" },
            { TextKeys.ProfileSavedOnDevice, "Saved on this device" },

            { TextKeys.SettingSound, "Sound effects" },
            { TextKeys.SettingCardPlay, "Card play" },
            { TextKeys.CardPlayTwoStep, "Two-step (select, then play)" },
            { TextKeys.CardPlayOneTap, "One tap" },

            { TextKeys.DataRecovered, "Some saved data could not be read and was reset." },
            { TextKeys.DataNotSaved, "Your changes could not be saved on this device." },

            { TextKeys.LobbyTitle, "ZELLIGE" },
            { TextKeys.LobbySubtitle, "Moroccan Ronda" },
            { TextKeys.ModeVsComputerDetail, "Play against Nadia · Offline" },
            { TextKeys.ModeOnline1v1Detail, "Online · Two players" },
            { TextKeys.ModeOnline2v2Detail, "Online · Two teams of two" },
            { TextKeys.ModePlay, "PLAY" },
            { TextKeys.ModeVersus, "vs" },

            { TextKeys.LeaveTitle, "Leave the match?" },
            { TextKeys.LeaveMessage, "The match will end here and cannot be resumed." },
            { TextKeys.LeaveCancel, "CANCEL" },
            { TextKeys.LeaveConfirm, "LEAVE" },
            { TextKeys.ResultLobby, "LOBBY" },
        };
    }

    /// <summary>
    /// Looks up user-facing text by key. English only for now; the lookup is the single point
    /// where another language table would be selected later.
    /// </summary>
    public static class TextCatalog
    {
        /// <summary>The text for <paramref name="key"/>; a visible "[key]" marker when it is missing.</summary>
        public static string Get(string key)
        {
            if (key != null && EnglishText.Table.TryGetValue(key, out string text)) return text;
            return "[" + key + "]";
        }

        /// <summary>True when the key has text in the current language.</summary>
        public static bool Has(string key) => key != null && EnglishText.Table.ContainsKey(key);
    }
}
