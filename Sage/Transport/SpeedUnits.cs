/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Transport {

    /// <summary>
    /// Conversions between the Transport namespace's internal speed unit (meters per second)
    /// and the units people actually post on signs and think in. Use these at the model-building
    /// boundary - e.g. <code>SpeedUnits.FromMilesPerHour(40)</code> for a posted limit, or
    /// <code>SpeedUnits.FromMilesPerHour(10)</code> for a driver who prefers 10 MPH over it -
    /// so that everything inside the simulation stays in SI.
    /// </summary>
    public static class SpeedUnits {

        private const double METERS_PER_SECOND_PER_MPH = 0.44704; // Exact: 1609.344 m / 3600 s.
        private const double METERS_PER_SECOND_PER_KPH = 1.0 / 3.6;

        /// <summary>
        /// Converts a speed (or a speed offset) in miles per hour to meters per second.
        /// </summary>
        /// <param name="milesPerHour">The speed in miles per hour.</param>
        public static double FromMilesPerHour(double milesPerHour) { return milesPerHour * METERS_PER_SECOND_PER_MPH; }

        /// <summary>
        /// Converts a speed (or a speed offset) in meters per second to miles per hour.
        /// </summary>
        /// <param name="metersPerSecond">The speed in meters per second.</param>
        public static double ToMilesPerHour(double metersPerSecond) { return metersPerSecond / METERS_PER_SECOND_PER_MPH; }

        /// <summary>
        /// Converts a speed (or a speed offset) in kilometers per hour to meters per second.
        /// </summary>
        /// <param name="kilometersPerHour">The speed in kilometers per hour.</param>
        public static double FromKilometersPerHour(double kilometersPerHour) { return kilometersPerHour * METERS_PER_SECOND_PER_KPH; }

        /// <summary>
        /// Converts a speed (or a speed offset) in meters per second to kilometers per hour.
        /// </summary>
        /// <param name="metersPerSecond">The speed in meters per second.</param>
        public static double ToKilometersPerHour(double metersPerSecond) { return metersPerSecond / METERS_PER_SECOND_PER_KPH; }
    }
}
