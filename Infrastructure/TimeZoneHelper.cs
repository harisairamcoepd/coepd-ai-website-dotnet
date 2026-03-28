using System;
using System.Globalization;

namespace Coepd.Web.Infrastructure
{
    public static class TimeZoneHelper
    {
        private static readonly TimeSpan IndiaOffset = TimeSpan.FromHours(5.5);

        public static DateTime ToDisplayTime(DateTime value)
        {
            if (value == default(DateTime)) return value;

            if (value.Kind == DateTimeKind.Local)
            {
                return value;
            }

            var utc = value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();

            return utc.Add(IndiaOffset);
        }

        public static string ToDisplayText(DateTime value)
        {
            var display = ToDisplayTime(value);
            return display == default(DateTime)
                ? string.Empty
                : display.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture) + " IST";
        }
    }
}
