namespace Zellige.App
{
    /// <summary>
    /// The three game modes approved by the owner (D1). The numeric values are written to the
    /// saved JSON, so they must never be renumbered. 0 is deliberately unused: a missing or
    /// zeroed field in a file is therefore detected as invalid instead of read as a real mode.
    /// </summary>
    public enum MatchMode
    {
        /// <summary>Me vs Computer: the local, offline single-player mode against Nadia.</summary>
        VsComputer = 1,
        /// <summary>1 vs 1 online (two humans). Shown locked; not playable in this version.</summary>
        Online1v1 = 2,
        /// <summary>2 vs 2 online (four humans in two teams). Shown locked; not playable in this version.</summary>
        Online2v2 = 3
    }

    /// <summary>
    /// How a completed match ended for the local player. The rules engine always produces a
    /// winner (an exact tie at the target plays another deck, H-3), so there is no Draw value.
    /// Abandoned matches are not outcomes: they never become a completed record (D6).
    /// </summary>
    public enum MatchOutcome
    {
        Victory = 1,
        Defeat = 2
    }

    public static class MatchModes
    {
        /// <summary>True only for modes that can actually be started in this version.</summary>
        public static bool IsPlayable(MatchMode mode) => mode == MatchMode.VsComputer;

        /// <summary>True for the online modes (playable or not).</summary>
        public static bool IsOnline(MatchMode mode) => mode == MatchMode.Online1v1 || mode == MatchMode.Online2v2;

        /// <summary>True for a value that is one of the declared modes (guards against bad JSON).</summary>
        public static bool IsDefined(MatchMode mode) =>
            mode == MatchMode.VsComputer || mode == MatchMode.Online1v1 || mode == MatchMode.Online2v2;

        /// <summary>The text key of the mode's display name (see <see cref="TextKeys"/>).</summary>
        public static string NameKey(MatchMode mode)
        {
            switch (mode)
            {
                case MatchMode.VsComputer: return TextKeys.ModeVsComputer;
                case MatchMode.Online1v1: return TextKeys.ModeOnline1v1;
                case MatchMode.Online2v2: return TextKeys.ModeOnline2v2;
                default: return TextKeys.ModeUnknown;
            }
        }
    }
}
