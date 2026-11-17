using System;
using System.Globalization;

namespace Zellige.App
{
    /// <summary>
    /// Timestamps in saved files are UTC, ISO 8601 round-trip strings (for example
    /// "2026-11-17T18:30:00.0000000Z"). They are readable when a file is inspected and do not
    /// depend on the device's culture or time zone. Display code converts to local time.
    /// </summary>
    public static class Timestamps
    {
        public static string Format(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local) utc = utc.ToUniversalTime();
            else if (utc.Kind == DateTimeKind.Unspecified) utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return utc.ToString("o", CultureInfo.InvariantCulture);
        }

        public static bool TryParse(string text, out DateTime utc)
        {
            utc = default;
            // Only explicit UTC values ("...Z") are accepted, so a value can never shift with the time zone.
            if (string.IsNullOrEmpty(text) || text[text.Length - 1] != 'Z') return false;
            if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return false;
            }
            if (parsed.Kind != DateTimeKind.Utc) return false;
            utc = parsed;
            return true;
        }
    }
}
