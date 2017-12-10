/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.SimCore {

	/// <summary>
	/// Abstract base class from which concrete Metronomes derive. Provides basic services
	/// and event handlers for a Metronome, which is an object that uses a model's executive
	/// to create a series of 'tick' events with a consistent period - Simulation Objects
	/// that are written to expect a uniform discrete time notification can use a metronome
	/// to achieve that. Multiple metronomes may be defined within a model, with different
	/// periods, start times and/or finish times.
	/// </summary>
	public abstract class MetronomeBase {
		private readonly ExecEventReceiver m_execEvent;
		private readonly DateTime m_startAt;
		private readonly DateTime m_finishAfter;
		private readonly TimeSpan m_period;
		private readonly IExecutive m_executive;
		private readonly bool m_autoFinish = true;
		private readonly bool m_autoStart = true;
        private bool m_abortRequested = false;

		/// <summary>
		/// Abstract base class constructor for the Metronome_Base class. Assumes both autostart
		/// and autofinish for this metronome.
		/// </summary>
		/// <param name="exec">The executive that will be serving the events.</param>
		/// <param name="period">The periodicity of the event train.</param>
		protected MetronomeBase(IExecutive exec, TimeSpan period){
			m_startAt = DateTime.MinValue;
			m_finishAfter = DateTime.MaxValue;
			m_period = period;
			m_executive = exec;
			m_autoFinish = true;
			m_autoStart = true;
			m_execEvent = OnExecEvent;
			m_executive.EventAboutToFire+=m_executive_EventAboutToFire;
            TotalTicksExpected = int.MaxValue;

		}

		/// <summary>
		/// Abstract base class constructor for the Metronome_Base class.
		/// </summary>
		/// <param name="exec">The executive that will be serving the events.</param>
		/// <param name="startAt">The start time for the event train.</param>
		/// <param name="finishAfter">The end time for the event train.</param>
		/// <param name="period">The periodicity of the event train.</param>
		protected MetronomeBase(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period){
			m_startAt = startAt;
			m_finishAfter = finishAfter;
			m_period = period;
			m_executive = exec;
			m_autoFinish = false;
			m_autoStart = false;
			m_execEvent = OnExecEvent;
            TotalTicksExpected = (int)((m_finishAfter.Ticks - m_startAt.Ticks) / m_period.Ticks);
			m_executive.ExecutiveStarted+=m_executive_ExecutiveStarted;
		}

        public int TotalTicksExpected { get; private set; }

		private void m_executive_EventAboutToFire(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType) {
			m_executive.RequestEvent(m_execEvent,when,priority+double.Epsilon,null);
			m_executive.EventAboutToFire-=m_executive_EventAboutToFire;
		}

		private void m_executive_ExecutiveStarted(IExecutive exec) {
            TickIndex = 0;
			Debug.Assert(m_executive.Now <= m_startAt,"Start Time Error"
				,"A metronome was told to start at " + m_startAt + ", but the executive started at time " + exec.Now+ ".");
			m_executive.RequestEvent(m_execEvent,m_startAt,0.0,null);

		}

		private void OnExecEvent(IExecutive exec, object userData){
            if (!m_abortRequested) {
                Debug.Assert(exec == m_executive, "Executive Mismatch"
                    , "A metronome was called by an executive that is not the one it was initially created for.");
                FireEvents(exec, userData);
                if (!m_abortRequested) {
                    TickIndex++;
                    DateTime nextEventTime = exec.Now + m_period;
                    if (nextEventTime <= m_finishAfter) {
                        if (m_autoFinish) {
                            exec.RequestDaemonEvent(m_execEvent, nextEventTime, 0.0, null);
                        } else {
                            exec.RequestEvent(m_execEvent, nextEventTime, 0.0, null);
                        }
                    }
                }
            }
		}

		public DateTime StartAt => m_startAt;
	    public DateTime FinishAt => m_finishAfter;
	    public TimeSpan Period => m_period;
	    public bool AutoStart => m_autoStart;
	    public bool AutoFinish => m_autoFinish;
	    public int TickIndex { get; private set; }
        public IExecutive Executive => m_executive;
	    protected abstract void FireEvents(IExecutive exec, object userData);
        public void Abort() { m_abortRequested = true; }
	}

	/// <summary>
	/// Simple Metronome class is an object that uses a model's executive
	/// to create a series of 'tick' events with a consistent period - Simulation Objects
	/// that are written to expect a uniform discrete time notification can use a metronome
	/// to achieve that. Multiple metronomes may be defined within a model, with different
	/// periods, start times and/or finish times.
	/// </summary>
	public class Metronome_Simple : MetronomeBase {

		private static HashtableOfLists m_channels = new HashtableOfLists();

        /// <summary>
        /// Creates a metronome with the specified parameters. It uses a static factory method 
        /// because if there is already an existing metronome with the same parameters, that 
        /// metronome is returned, rather than creating another one - after all, the whole 
        /// point is to avoid a large number of tick events on the executive's event list.
        /// </summary>
        /// <param name="exec">The executive that will issue the ticks.</param>
        /// <param name="startAt">The time at which the metronome's ticks are to start.</param>
        /// <param name="finishAfter">The time at which the metronome's ticks are to stop.</param>
        /// <param name="period">The period of the ticking.</param>
        /// <returns>A metronome that meets the criteria.</returns>
        public static Metronome_Simple CreateMetronome(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period){
			Metronome_Simple retval = null;
			foreach ( Metronome_Simple ms in m_channels ) {
				if ( ms.Executive.Equals(exec) && ms.StartAt.Equals(startAt) && ms.FinishAt.Equals(finishAfter) && ms.Period.Equals(period) ) {
					retval = ms;
					break;
				}
			}
			if ( retval == null ) {
				retval = new Metronome_Simple(exec,startAt,finishAfter,period);
				m_channels.Add(exec,retval);
				exec.ExecutiveFinished+=exec_ExecutiveFinished;
			}
			return retval;
		}

        /// <summary>
        /// Creates a metronome with the specified parameters. It uses a static factory method 
        /// because if there is already an existing metronome with the same parameters, that 
        /// metronome is returned, rather than creating another one - after all, the whole 
        /// point is to avoid a large number of tick events on the executive's event list.
        /// </summary>
        /// <param name="exec">The executive that will issue the ticks.</param>
        /// <param name="period">The period of the ticking.</param>
        /// <returns>Metronome_Simple.</returns>
        public static Metronome_Simple CreateMetronome(IExecutive exec, TimeSpan period){
			Metronome_Simple retval = null;

			foreach ( Metronome_Simple ms in m_channels ) {
				if ( ms.Period.Equals(period) && ms.StartAt == DateTime.MinValue && ms.FinishAt == DateTime.MaxValue ) {
					retval = ms;
					break;
				}
			}
			if ( retval == null ) {
				retval = new Metronome_Simple(exec,period);
				m_channels.Add(exec,retval);
			}
			return retval;
		}

		/// <summary>
		/// Constructor for the Metronome_Simple class.
		/// </summary>
		/// <param name="exec">The executive that will be serving the events.</param>
		/// <param name="startAt">The start time for the event train.</param>
		/// <param name="finishAfter">The end time for the event train.</param>
		/// <param name="period">The periodicity of the event train.</param>
		private Metronome_Simple(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period)
			:base(exec, startAt, finishAfter, period){}
		/// <summary>
		/// Constructor for the Metronome_Simple class. Assumes auto-start, and auto-finish.
		/// </summary>
		/// <param name="exec">The executive that will be serving the events.</param>
		/// <param name="period">The periodicity of the event train.</param>
		private Metronome_Simple(IExecutive exec, TimeSpan period)
			:base(exec, period){}
		/// <summary>
		/// The tick event that is fired by this metronome. Simulation objects expecting to
		/// receive periodic notifications will receive them from this event. Note that there is
		/// no inferred sequence to these notifications. If a dependency order is required, then
		/// the Metronome_Dependencies class should be used.
		/// </summary>
		public event ExecEventReceiver TickEvent;

		protected override void FireEvents(IExecutive exec, object userData){
            TickEvent?.Invoke(exec, userData);
        }

		private static void exec_ExecutiveFinished(IExecutive exec) {
			m_channels.Remove(exec);
		}
	}
}
