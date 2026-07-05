/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Mathematics;

namespace Highpoint.Sage.ItemBased {

	public interface IPeriodicity { TimeSpan GetNext(); }

	public class Periodicity : IPeriodicity {
		public enum Units { Seconds, [DefaultValue]Minutes, Hours, Days };
        private double m_seconds;
		private IDoubleDistribution m_distribution;
        public Periodicity(IDoubleDistribution distribution, Units units) {
            m_distribution = distribution;
            SetPeriod(distribution, ((long)(TimeSpan.TicksPerSecond * SecondsFromUnits(units))));
        }

        private double SecondsFromUnits(Units units) {
            double retval = 1.0;
            switch (units) {
                case Units.Seconds: retval = 1.0; break;
                case Units.Minutes: retval = 60.0; break;
                case Units.Hours: retval = 3600.0; break;
                case Units.Days: retval = 86400.0; break;
            }

            return retval;
        }

        public Periodicity(IDoubleDistribution distribution, long ticks) {
            SetPeriod(distribution, ticks);
        }

        public void SetPeriod(IDoubleDistribution distribution, long ticks) {
            m_seconds = TimeSpan.FromTicks(ticks).TotalSeconds;
            m_distribution = distribution;
        }

        public TimeSpan GetNext() {
			double d = m_distribution.GetNext();
            TimeSpan next = TimeSpan.FromSeconds(d * m_seconds);
            // An unbounded distribution (e.g. Normal) can yield a negative sample, but a negative
            // period is meaningless. Callers schedule events at Now + GetNext(), and the executive
            // silently discards events requested before Now when causality violations are ignored -
            // which would, for example, end a Ticker's pulse chain. Clamp to zero: "due in the
            // past" means "due now".
            return next < TimeSpan.Zero ? TimeSpan.Zero : next;
		}
	}   
}