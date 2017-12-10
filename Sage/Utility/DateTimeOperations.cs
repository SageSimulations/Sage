/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility {

    /// <summary>
    /// A utility class that contains some useful operations pertaining to DateTime objects.
    /// </summary>
	public static class DateTimeOperations {

        /// <summary>
        /// Gets the later of the two DateTime objects.
        /// </summary>
        /// <param name="dt1">One DateTime.</param>
        /// <param name="dt2">The other DateTime.</param>
        /// <returns>The later of the two DateTime objects.</returns>
		public static DateTime Max(DateTime dt1, DateTime dt2){
			return new DateTime(Math.Max(dt1.Ticks,dt2.Ticks));
		}

        /// <summary>
        /// Gets the earlier of the two DateTime objects.
        /// </summary>
        /// <param name="dt1">One DateTime.</param>
        /// <param name="dt2">The other DateTime.</param>
        /// <returns>The earlier of the two DateTime objects.</returns>
        public static DateTime Min(DateTime dt1, DateTime dt2) {
			return new DateTime(Math.Min(dt1.Ticks,dt2.Ticks));
		}

        public static string DtFileString() {
            DateTime now = DateTime.Now;
            return DtFileString(now);
        }

        public static string DtFileString(DateTime when, string extension="")
        {
            if (!extension.StartsWith(".")) extension = "." + extension;
            return $"{when.Year}{when.Month:00}{when.Day:00}{when.Hour:00}{when.Minute:00}{when.Second:00}{extension}";
        }

        /// <summary>
        /// Gets the seconds since the epoch (January 1, 1970).
        /// </summary>
        /// <value>The seconds since the epoch.</value>
        public static int SecondsSinceTheEpoch => ( (int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds );
	}

    /// <summary>
    /// A utility class that contains some useful operations pertaining to TimeSpan objects.
    /// </summary>
	public static class TimeSpanOperations {
        /// <summary>
        /// Gets the longer of the two TimeSpan objects.
        /// </summary>
        /// <param name="ts1">One TimeSpan.</param>
        /// <param name="ts2">The other TimeSpan.</param>
        /// <returns>The longer of the two TimeSpan objects.</returns>
        public static TimeSpan Max(TimeSpan ts1, TimeSpan ts2) {
			//return new TimeSpan(Math.Max(ts1.Ticks,ts2.Ticks));
            return ts1 > ts2 ? ts1 : ts2;
		}

        /// <summary>
        /// Gets the shorter of the two TimeSpan objects.
        /// </summary>
        /// <param name="ts1">One TimeSpan.</param>
        /// <param name="ts2">The other TimeSpan.</param>
        /// <returns>The shorter of the two TimeSpan objects.</returns>
        public static TimeSpan Min(TimeSpan ts1, TimeSpan ts2) {
			//return new TimeSpan(Math.Min(ts1.Ticks,ts2.Ticks));
            return ts1 < ts2 ? ts1 : ts2;
        }

        /// <summary>
        /// Returns the timespan as d days, h hours, m minutes and s seconds. Starting at days, any field that's zero is omitted.
        /// </summary>
        /// <param name="ts">The timeSpan.</param>
        /// <returns>The Informal TimeSpan.</returns>
        public static string ToInformalString(TimeSpan ts) {
            string retval;
            if (ts.TotalSeconds > 0) {
                retval = $"{ts.Seconds} seconds";
                if (ts.TotalMinutes >= 1) {
                    retval = $"{ts.Minutes} {(ts.Minutes > 1 ? " minutes, " : "minute, ")}{retval}";
                    if (ts.TotalHours >= 1) {
                        retval = $"{ts.Hours} {(ts.Hours > 1 ? " hours, " : "hour, ")}{retval}";
                        if (ts.TotalDays >= 1) {
                            retval = $"{ts.Days} {(ts.Days > 1 ? " days, " : "day, ")}{retval}";
                        }
                    }
                }
            } else {
                retval = "less than a second.";
            }
            return retval;
        }
	}
}
