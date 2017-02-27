/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
namespace Highpoint.Sage.Utility
{
    // TODO: Consider eliminating the shift bits thing, and going to division.

	/// <summary>
	/// An EventTimeHistorian keeps track of the times at which the last 'N' events that were submitted to it
	/// occurred, and provides the average inter-event duration for those events. Historians with specific
    /// event-type-related data needs (other than simply the time of occurrence) can inherit from this class.
	/// </summary>
	public class EventTimeHistorian {

#region Private Fields
        private readonly IExecutive m_exec;
        private readonly DateTime[] m_eventTimes;
        private int m_head;
        private int m_nLogged;
        private readonly int m_nPastEventCapacity;
        private readonly int m_shiftBits;
        private bool m_filled;
#endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTimeHistorian"/> class, tracking the last m events, where
        /// m is the lowest (2^n)+1 that is greater than numPastEventsTracked. (n is any whole nonnegative number.)
        /// </summary>
        /// <param name="exec">The executive that is to be tracked.</param>
        /// <param name="numPastEventsTracked">The number of past events that will be tracked.</param>
        public EventTimeHistorian(IExecutive exec, int numPastEventsTracked){
			m_shiftBits = (int)Math.Round(Math.Log(numPastEventsTracked)/Math.Log(2.0));
			m_nPastEventCapacity = (int)Math.Pow(2.0,m_shiftBits)+1;
			m_exec = exec;
			m_eventTimes = new DateTime[m_nPastEventCapacity];
			m_head = -1;
			m_filled = false;
		}

        /// <summary>
        /// Logs the fact that an event was just fired.
        /// </summary>
        public void LogEvent(){
			m_nLogged++;
			m_head++;
			if ( m_head == m_nPastEventCapacity ) { m_head = 0; m_filled = true; }
			m_eventTimes[m_head] = m_exec.Now;
		}

        /// <summary>
        /// Gets the max number of past events that can be tracked.
        /// </summary>
        /// <value>The past event capacity.</value>
        public int PastEventCapacity => m_nPastEventCapacity;

        /// <summary>
        /// Gets the number of past events received.
        /// </summary>
        /// <value>The past events received.</value>
        public int PastEventsReceived => m_nLogged;

        /// <summary>
        /// Gets the average intra event duration for the past n events. If n is -1, .
        /// </summary>
        /// <param name="numPastEvents">The number of past events to be considered. If -1, then the entire set of tracked events (numPastEventsTracked) is considered.</param>
        /// <returns>TimeSpan.</returns>
        /// <exception cref="OverflowException"></exception>
        public TimeSpan GetAverageIntraEventDuration (int numPastEvents = -1) {
			if (numPastEvents == -1) numPastEvents = Math.Min(m_nLogged,m_nPastEventCapacity);

			if ( numPastEvents > m_eventTimes.Length ) throw new OverflowException(string.Format(s_caller_Requested_Too_Many_Data_Points,numPastEvents,m_eventTimes.Length));
			if ( !m_filled ) {
				if ( m_nLogged < 2 ) return TimeSpan.Zero;
				int tail = m_head-numPastEvents+1;
				if ( tail < 0 ) tail += m_nPastEventCapacity;
				int nEvents  = Math.Min(m_nLogged,(m_nPastEventCapacity));
				long deltaTicks = m_eventTimes[m_head].Ticks - m_eventTimes[tail].Ticks;
				long avgIntraEventDuration = deltaTicks/(nEvents-1);
				return TimeSpan.FromTicks(avgIntraEventDuration);
			} else {
				int tail = m_head+1;
				if ( tail == m_nPastEventCapacity ) tail = 0;
				return TimeSpan.FromTicks((m_eventTimes[m_head] - m_eventTimes[tail]).Ticks>>m_shiftBits);
			}
		}

		private static readonly string s_caller_Requested_Too_Many_Data_Points = "Caller tried to obtain statistics on the last {0} events in an historian that is only tracking {1} events.";
	}
}
