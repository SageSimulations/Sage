/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
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
            double retval = 0.0;
            switch (units) {
                case Units.Minutes: retval = 60.0; break;
                case Units.Hours: retval = 3600.0; break;
                case Units.Days: retval = 8640.0; break;
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
            return TimeSpan.FromSeconds(d * m_seconds);
			
		}
	}   
}