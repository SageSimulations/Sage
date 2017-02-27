/* This source code licensed under the GNU Affero General Public License */
#if INCLUDE_WIP
using System;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Utility
{
	public delegate void TimeSelectionChangeHandler(object oldSelectable, object newSelectable);
	//public enum Recurrence { Weekday, Weekend, Daily, Weekly, Monthly, ThisDayEveryWeek, ThisDateEveryMonth }

	public interface IRecurrence : IComparable {
		DateTime Next { get; }
		event ExecEventReceiver Occurrence;
	}

	public abstract class Recurrence : IRecurrence {
		
		protected IExecutive Exec;
		protected DateTime NextOccurrence;
		protected int NumRecurrences = -1;
		protected DateTime StopAfterDate = DateTime.MaxValue;
		protected double Priority;
		protected bool UseDaemonEvents;
		protected ExecEventReceiver Eer;

		public Recurrence(IExecutive exec, DateTime baseline, int numberOfRecurrences, double priority){
			NextOccurrence = baseline;
			NumRecurrences = numberOfRecurrences;
			Priority = priority;
			Exec = exec;
			Exec.ExecutiveStarted+=new ExecutiveEvent(ScheduleNext);
			UseDaemonEvents = numberOfRecurrences < 0;
			Eer = new ExecEventReceiver(_Occurrence);
		}

		public Recurrence(IExecutive exec, DateTime baseline, DateTime stopAfter, double priority){
			NextOccurrence = baseline;
			StopAfterDate = stopAfter;
			Priority = priority;
			Exec = exec;
			Exec.ExecutiveStarted+=new ExecutiveEvent(ScheduleNext);
			UseDaemonEvents = stopAfter.Equals(DateTime.MaxValue);
			Eer = new ExecEventReceiver(_Occurrence);
		}

		public event ExecEventReceiver Occurrence;

		private void ScheduleNext(IExecutive exec){
			if ( UseDaemonEvents ) {
				Exec.RequestDaemonEvent(Eer,NextOccurrence,Priority,null);
			} else {
				Exec.RequestEvent(Eer,NextOccurrence,Priority,null);
			}
			AdvanceNextOccurrenceDate();
		}

		protected virtual void _Occurrence(IExecutive exec, object userData){
			if ( UseDaemonEvents ) {
				Exec.RequestDaemonEvent(Eer,NextOccurrence,Priority,null);
			} else {
				Exec.RequestEvent(Eer,NextOccurrence,Priority,null);
			}
			AdvanceNextOccurrenceDate();
			if ( Occurrence != null ) Occurrence(exec,userData);
		}

#region IRecurrence Members

		protected abstract void AdvanceNextOccurrenceDate();

		public DateTime Next {
			get {
				return NextOccurrence;
			}
		}

#endregion

#region IComparable Members

		public int CompareTo(object obj) {
			if ( this.NextOccurrence < ((Recurrence)obj).NextOccurrence ) return 1;
			if ( this.NextOccurrence > ((Recurrence)obj).NextOccurrence ) return -1;
			if ( this.Priority < ((Recurrence)obj).Priority ) return 1;
			return -1;
		}

#endregion

	}
 

	public class PeriodicRecurrence : Recurrence {
		private TimeSpan m_period;
		public PeriodicRecurrence(IExecutive exec, DateTime baseline, TimeSpan period, int numberOfRecurrences, double priority)
			:base(exec,baseline,numberOfRecurrences,priority){
			m_period = period;
		}

		public PeriodicRecurrence(IExecutive exec, DateTime baseline, TimeSpan period, DateTime stopAfter, double priority)
			:base(exec,baseline,stopAfter,priority){
			m_period = period;
		}

		protected override void AdvanceNextOccurrenceDate() {
			NextOccurrence += m_period;
		}

	}

	public delegate void TimeBasedSelectableEvent(object selectee);

	public class TimebasedSelector {
		private IExecutive m_exec;
		// TODO: Create a heap implementation here. 
		public TimebasedSelector(IExecutive exec) {
			m_exec = exec;
		}

		public object Get(){
			return null;
		}

#pragma warning disable 67 // Ignore it if this event is not used. It's a framework, and this event may be for clients.
        public event TimeBasedSelectableEvent SelecteeExpiring;
        public event TimeBasedSelectableEvent SelecteeArriving;
#pragma warning restore 67

		public void AddSelectable(object selectable){

		}

		public void AddSelectable(IRecurrence selectable){

		}
	}
}
#endif