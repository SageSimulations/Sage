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
		private ExecEventReceiver m_execEvent;
		private DateTime m_startAt;
		private DateTime m_finishAfter;
		private TimeSpan m_period;
		private IExecutive m_executive;
		private bool m_autoFinish = true;
		private bool m_autoStart = true;
        private bool m_abortRequested = false;

		/// <summary>
		/// Abstract base class constructor for the Metronome_Base class. Assumes both autostart
		/// and autofinish for this metronome.
		/// </summary>
		/// <param name="exec">The executive that will be serving the events.</param>
		/// <param name="period">The periodicity of the event train.</param>
		public MetronomeBase(IExecutive exec, TimeSpan period){
			m_startAt = DateTime.MinValue;
			m_finishAfter = DateTime.MaxValue;
			m_period = period;
			m_executive = exec;
			m_autoFinish = true;
			m_autoStart = true;
			m_execEvent = new ExecEventReceiver(OnExecEvent);
			m_executive.EventAboutToFire+=new EventMonitor(m_executive_EventAboutToFire);
            TotalTicksExpected = int.MaxValue;

		}

		/// <summary>
		/// Abstract base class constructor for the Metronome_Base class.
		/// </summary>
		/// <param name="exec">The executive that will be serving the events.</param>
		/// <param name="startAt">The start time for the event train.</param>
		/// <param name="finishAfter">The end time for the event train.</param>
		/// <param name="period">The periodicity of the event train.</param>
		public MetronomeBase(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period){
			m_startAt = startAt;
			m_finishAfter = finishAfter;
			m_period = period;
			m_executive = exec;
			m_autoFinish = false;
			m_autoStart = false;
			m_execEvent = new ExecEventReceiver(OnExecEvent);
            TotalTicksExpected = (int)((m_finishAfter.Ticks - m_startAt.Ticks) / m_period.Ticks);
			m_executive.ExecutiveStarted+=new ExecutiveEvent(m_executive_ExecutiveStarted);
		}

        public int TotalTicksExpected { get; private set; }

		private void m_executive_EventAboutToFire(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType) {
			m_executive.RequestEvent(m_execEvent,when,priority+double.Epsilon,null);
			m_executive.EventAboutToFire-=new EventMonitor(m_executive_EventAboutToFire);
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

		public DateTime StartAt { get { return m_startAt; } }
		public DateTime FinishAt { get { return m_finishAfter; } }
		public TimeSpan Period { get { return m_period; } }
		public bool AutoStart { get { return m_autoStart; } }
		public bool AutoFinish { get { return m_autoFinish; } }
        public int TickIndex { get; private set; }
        public IExecutive Executive { get { return m_executive; } }
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

		private static HashtableOfLists _channels = new HashtableOfLists();

		public static Metronome_Simple CreateMetronome(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period){
			Metronome_Simple retval = null;
			foreach ( Metronome_Simple ms in _channels ) {
				if ( ms.Executive.Equals(exec) && ms.StartAt.Equals(startAt) && ms.FinishAt.Equals(finishAfter) && ms.Period.Equals(period) ) {
					retval = ms;
					break;
				}
			}
			if ( retval == null ) {
				retval = new Metronome_Simple(exec,startAt,finishAfter,period);
				_channels.Add(exec,retval);
				exec.ExecutiveFinished+=new ExecutiveEvent(exec_ExecutiveFinished);
			}
			return retval;
		}

		public static Metronome_Simple CreateMetronome(IExecutive exec, TimeSpan period){
			Metronome_Simple retval = null;

			foreach ( Metronome_Simple ms in _channels ) {
				if ( ms.Period.Equals(period) ) {
					retval = ms;
					break;
				}
			}
			if ( retval == null ) {
				retval = new Metronome_Simple(exec,period);
				_channels.Add(exec,retval);
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
		/// the Metronone_Dependencies class should be used.
		/// </summary>
		public event ExecEventReceiver TickEvent;

		protected override void FireEvents(IExecutive exec, object userData){
			if ( TickEvent != null ) TickEvent(exec,userData);
		}

		private static void exec_ExecutiveFinished(IExecutive exec) {
			_channels.Remove(exec);
		}
	}
}
