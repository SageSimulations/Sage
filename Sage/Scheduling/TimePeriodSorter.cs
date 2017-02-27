/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Scheduling {

	/// <summary>
    /// An implementer of IComparer&lt;TimePeriod&gt; that can be used to sort a collection of
	/// ITimePeriodReadOnly objects. The 'sortOnWhat' parameter allows the
	/// user to choose whether to sort on start time, duration or end time.
	/// </summary>
	public class TimePeriodSorter : IComparer<ITimePeriod> {
        private static TimePeriodSorter _byIncreasingStartTime = null;
        private static TimePeriodSorter _byIncreasingDuration = null;
        private static TimePeriodSorter _byIncreasingEndTime = null;
        private static TimePeriodSorter _byDecreasingStartTime = null;
        private static TimePeriodSorter _byDecreasingDuration = null;
        private static TimePeriodSorter _byDecreasingEndTime = null;

        private TimePeriodPart m_sortOnWhat;
        private int m_ascending;

		private TimePeriodSorter(TimePeriodPart sortOnWhat, bool ascending){
			m_sortOnWhat = sortOnWhat;
            m_ascending = ascending ? 1 : -1;
		}

#region IComparer<TimePeriod> Members

        public int Compare(ITimePeriod tp1, ITimePeriod tp2) {
            switch (m_sortOnWhat) {
                case TimePeriodPart.StartTime:
                    return m_ascending * Comparer.Default.Compare(tp1.StartTime, tp2.StartTime);
                case TimePeriodPart.Duration:
                    return m_ascending * Comparer.Default.Compare(tp1.Duration, tp2.Duration);
                case TimePeriodPart.EndTime:
                    return m_ascending * Comparer.Default.Compare(tp1.EndTime, tp2.EndTime);
                default:
                    throw new ApplicationException("Unknown part of TimePeriod, " + m_sortOnWhat + ", used for sorting.");
            }
        }

#endregion

        public static TimePeriodSorter ByIncreasingStartTime {
            get {
                if (_byIncreasingStartTime == null) {
                    _byIncreasingStartTime = new TimePeriodSorter(TimePeriodPart.StartTime, true);
                }
                return _byIncreasingStartTime;
            }
        }

        public static TimePeriodSorter ByIncreasingDuration {
            get {
                if (_byIncreasingDuration == null) {
                    _byIncreasingDuration = new TimePeriodSorter(TimePeriodPart.Duration, true);
                }
                return _byIncreasingDuration;
            }
        }

        public static TimePeriodSorter ByIncreasingEndTime {
            get {
                if (_byIncreasingEndTime == null) {
                    _byIncreasingEndTime = new TimePeriodSorter(TimePeriodPart.EndTime, true);
                }
                return _byIncreasingEndTime;
            }
        }
        public static TimePeriodSorter ByDecreasingStartTime {
            get {
                if (_byDecreasingStartTime == null) {
                    _byDecreasingStartTime = new TimePeriodSorter(TimePeriodPart.StartTime, false);
                }
                return _byDecreasingStartTime;
            }
        }

        public static TimePeriodSorter ByDecreasingDuration {
            get {
                if (_byDecreasingDuration == null) {
                    _byDecreasingDuration = new TimePeriodSorter(TimePeriodPart.Duration, false);
                }
                return _byDecreasingDuration;
            }
        }

        public static TimePeriodSorter ByDecreasingEndTime {
            get {
                if (_byDecreasingEndTime == null) {
                    _byDecreasingEndTime = new TimePeriodSorter(TimePeriodPart.EndTime, false);
                }
                return _byDecreasingEndTime;
            }
        }
    }	
}