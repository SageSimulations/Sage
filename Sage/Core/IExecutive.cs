/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using System.Security.RightsManagement;
using System.Threading;
using System.Windows.Documents;
using Highpoint.Sage.SimCore.Parallel;

namespace Highpoint.Sage.SimCore {

	/// <summary>
	/// This delegate is implemented by any method that is to receive a time-based callback
	/// from the executive.
	/// </summary>
	public delegate void ExecEventReceiver(IExecutive exec, object userData);

	/// <summary>
	/// Implemented by any object that wishes to be notified as an event is firing.
	/// </summary>
	public delegate void EventMonitor(long key, ExecEventReceiver eer ,double priority, DateTime when, object userData, ExecEventType eventType);

    /// <summary>
    /// Implemented by any method that wants to receive notification of an executive event
    /// such as ExecutiveStarted, ExecutiveStopped, ExecutiveReset, ExecutiveFinished.
    /// </summary>
    public delegate void ExecutiveEvent(IExecutive exec);
    /// <summary>
    /// Describes the state of the executive.
    /// </summary>
    public enum ExecState { 
		/// <summary>
		/// The executive is stopped.
		/// </summary>
		Stopped,
		/// <summary>
		/// The executive is running.
		/// </summary>
		Running,
		/// <summary>
		/// The executive is paused.
		/// </summary>
		Paused,
		/// <summary>
		/// The executive is finished.
		/// </summary>
		Finished }
	
	
	/// <summary>
	/// Used to select the way the Executive dispatches an event once its time has arrived.
	/// These mechanisms are as follows:
	/// Synchronous – the callback is called on the dispatch thread, and upon completion,
	/// the next callback is selected based upon scheduled time and priority.
	/// Detachable – the callback is called on a thread from the .Net thread pool, and the
	/// dispatch thread then suspends awaiting the completion or suspension of that thread.
	/// If the event thread is suspended, an event controller is made available to other
	/// entities which can be used to resume or abort that thread. This is useful for modeling
	/// “intelligent entities” and situations where the developer wants to easily represent a
	/// delay or interruption of a process.
	/// Batched – all events at the current time and priority are called, each on separate
	/// threads, and the executive, except for servicing any new events registered for that
	/// time and priority, awaits completion of all running events. This may bring about higher
	/// performance in cases such as battlefield and transportation simulations where multiple
	/// entities may sense current conditions, plan and execute against that plan.
	/// Asynchronous - This mechanism is not yet supported.
	/// </summary>
	public enum ExecEventType { 
		/// <summary>
		/// The executive event (served to a requester) is synchronous. It will execute to its
		/// completion on the executive's thread, and no new events will be serviced until after
		/// its return.
		/// </summary>
		Synchronous,
//		/// <summary>
//		/// The execution event (served to a requester) is batched. All events of a specified
//		/// priority, and for the 'latest' time, are pulled off the stack and executed at the
//		/// same time, in different threads. Not yet implemented.
//		/// </summary>
//		Batched,
		/// <summary>
		/// The execution event (served to a requester) is detachable. It is executed in its own
		/// thread, and may be paused or put to sleep, joined with other threads, or allowed by
		/// the programmer to run in parallel to other executing executive threads. 
		/// </summary>
		Detachable,
		/// <summary>
		/// The execution event (served to a requester) is asynchronous. The thread is given the
		/// callback, the callback is fired, and the executive runs on. Useful for I/O. 
		/// </summary>
		Asynchronous,
		/// <summary>
		/// This enumeration value should not be used to request an event. It is used to indicate
		/// that no event is currently being serviced.
		/// </summary>
		None
	}

    
	/// <summary>
	/// This interface is implemented by a DetachableEventController - a DEC is associated
	/// with an event that is fired as a detachable event, and in that event's thread, can
	/// be obtained from the executive.
	/// </summary>
	public interface IDetachableEventController {
		/// <summary>
		/// Suspends this detachable event until it is explicitly resumed.
		/// </summary>
		void Suspend();

		/// <summary>
		/// Explicitly resumes this detachable event.
		/// </summary>
		void Resume();

		/// <summary>
		/// Explicitly resumes this detachable event with a specified (override) priority.
		/// This does not replace the initiating event's actual priority, and affects only the scheduling of the resuming event.
		/// </summary>
		void Resume(double overridePriority);

        /// <summary>
        /// Suspends this detachable event for a specified duration.
        /// </summary>
        /// <param name="howLong"></param>
        void SuspendFor(TimeSpan howLong);

        /// <summary>
        /// Suspends this detachable event until a specified time.
        /// </summary>
        /// <param name="when"></param>
        void SuspendUntil(DateTime when);

        /// <summary>
		/// When a detachable event is suspended, and if DetachableEventController diagnostics are turned on,
		/// this will return a stackTrace of the location where the DEC is suspended.
		/// </summary>
		StackTrace SuspendedStackTrace { get; }

		/// <summary>
		/// Returns true if the IDetachableEventController is at a wait. If this is true,
		/// and the IExecutive has completed its run, it usually means that some event in
		/// the simulation is blocked.
		/// </summary>
		/// <returns>true if the IDetachableEventController is at a wait.</returns>
		bool IsWaiting();

        /// <summary>
        /// Sets the abort handler.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="args">The args.</param>
        void SetAbortHandler(DetachableEventAbortHandler handler, params object[] args);
        /// <summary>
        /// Clears the abort handler.
        /// </summary>
        void ClearAbortHandler();

        /// <summary>
        /// Fires, and then clears, the abort handler.
        /// </summary>
        void FireAbortHandler();

		/// <summary>
		/// Returns the event that initially created this DetachableEventController.
		/// </summary>
		IExecEvent RootEvent { get; }
	}

	
	/// <summary>
	/// Implemented by an object that can select events, typically for removal from the
	/// event queue. It is, effectively, a filter. It is able to discern whether an event
	/// meets some criteria.
	/// </summary>
	public interface IExecEventSelector {
		/// <summary>
		/// Determines if the presented event is a candidate for the operation being considered, such as removal from the event queue.
		/// </summary>
		/// <param name="eer">The ExecEventReceiver that is to receive this event.</param>
		/// <param name="when">The DateTime that the event was to have been fired.</param>
		/// <param name="priority">The priority of the event.</param>
		/// <param name="userData">The user data that was presented with this event.</param>
		/// <param name="eet">The type of event (synchronous, batched, detachable, etc.)</param>
		/// <returns>True if this event is a candidate for the operation (e.g. removal), False if not.</returns>
		bool SelectThisEvent(ExecEventReceiver eer,DateTime when,double priority,object userData, ExecEventType eet);
	}
	

	/// <summary>
	/// A marker class that indicates that a given exception was thrown by the executive, rather than
	/// the application code.
	/// </summary>
	public class ExecutiveException : Exception {
		/// <summary>
		/// Creates an ExecutiveException.
		/// </summary>
		/// <param name="message">The message to be delivered by the exception.</param>
		public ExecutiveException(string message):base(message){}
	}
    

	/// <summary>
	/// Used to decorate the key or the value for anything that is going to be put 
	/// into the task graph that must be cleared out for each new run.
	/// </summary>
	public class TaskGraphVolatileAttribute : Attribute {}
	

	/// <summary>
	/// This class can be used as a key for an object into a Task Graph's graphContext, where the
	/// contents of the key are intended to be cleared out of the GC after each run of the model.
	/// </summary>
	[TaskGraphVolatile]
	public class VolatileKey {
		private string m_name = "VolatileKey";
		/// <summary>
		/// Creates a VolatileKey for use as a key for an object into a Task Graph's graphContext.
		/// </summary>
		public VolatileKey(){}
		/// <summary>
		/// Creates a VolatileKey for use as a key for an object into a Task Graph's graphContext.
		/// </summary>
		public VolatileKey(string name){ m_name = name;}
		/// <summary>
		/// Returns the name of this key.
		/// </summary>
		/// <returns>The name of this key.</returns>
		public override string ToString() {return m_name;}

	}

    public interface IExecutiveLight : IDisposable
    {
        /// <summary>
        /// The Guid by which this executive is known.
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// The current DateTime being managed by this executive. This is the 'Now' point of a
        /// simulation being run by this executive.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// If this executive has been run, this holds the DateTime of the last event serviced. May be from a previous run.
        /// </summary>
        DateTime? LastEventServed { get; }

        /// <summary>
        /// The priority of the event currently being serviced.
        /// </summary>
        double CurrentPriorityLevel { get; }

        /// <summary>
        /// The current state of this executive (running, stopped, paused, finished)
        /// </summary>
        ExecState State { get; }

        /// <summary>
        /// The type of event currently being serviced by the executive.
        /// </summary>
        ExecEventType CurrentEventType { get; }

        /// <summary>
        /// Returns a read-only list of the ExecEvents currently in queue for execution.
        /// Cast the elements in the list to IExecEvent to access the items' field values.
        /// </summary>
        IList EventList { get; }

        /// <summary>
        /// The integer count of the number of times this executive has been run.
        /// </summary>
        int RunNumber { get; }

        /// <summary>
        /// The number of events that have been serviced on this run.
        /// </summary>
        UInt32 EventCount { get; }

        /// <summary>
        /// Requests that the executive queue up a daemon event to be serviced at a specific time and
        /// priority. If only daemon events are enqueued, the executive will not be kept alive.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestDaemonEvent(ExecEventReceiver eer, DateTime when, double priority, object userData);

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero, and the userData object is assumed to be null.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestEvent(ExecEventReceiver eer, DateTime when);

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestEvent(ExecEventReceiver eer, DateTime when, object userData);

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData);

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <param name="execEventType">The way the event is to be served by the executive.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType);

        /// <summary>
        /// Starts the executive. The calling thread will be the primary execution thread, and will not return until
        /// execution is completed (via completion of all non-daemon events or the Abort method.)
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the executive. This may be a pause or a stop, depending on if events are queued or running at the time of call.
        /// </summary>
        void Stop();

        /// <summary>
        /// Resets the executive - this clears the event list and resets now to 1/1/01, 12:00 AM
        /// </summary>
        void Reset();

        /// <summary>
        /// Fired when this executive starts. All events are fired once, and then cleared.
        /// This enables the designer to register this event on starting the model, to
        /// set up the simulation model when the executive starts. If it was not then cleared,
        /// it would be re-registered and then called twice on the second start, three times
        /// on the third call, etc.
        /// </summary>
        event ExecutiveEvent ExecutiveStarted_SingleShot;

        /// <summary>
        /// Fired when this executive starts.
        /// </summary>
        event ExecutiveEvent ExecutiveStarted;

        /// <summary>
        /// Fired when this executive stops.
        /// </summary>
        event ExecutiveEvent ExecutiveStopped;

        /// <summary>
        /// Fired when this executive finishes (including after an abort).
        /// </summary>
        event ExecutiveEvent ExecutiveFinished;

        /// <summary>
        /// Fired when this executive is reset.
        /// </summary>
        event ExecutiveEvent ExecutiveReset;

        /// <summary>
        /// Fired after an event has been selected to be fired, but before it actually fires.
        /// </summary>
        event EventMonitor EventAboutToFire;

        /// <summary>
        /// Fired after an event has been selected to be fired, and after it actually fires.
        /// </summary>
        event EventMonitor EventHasCompleted;

        void SetStartTime(DateTime startTime);
    }

    public delegate void TimeEvent(DateTime dt);

    public interface IParallelExec : IExecutive
    {
        string Name { get; set; }

        void InitiateRollback(DateTime toWhen, Action doWhenRollbackCompletes = null);
        void PerformRollback(DateTime toWhen);
        event TimeEvent Rolledback;
        void WakeCallerAt(IParallelExec callerExec, DateTime @when, Action thenDoThis);
        CoExecutor Coexecutor { get; set; }
        ManualResetEvent RollbackBlock { get; }
        AutoResetEvent FutureReadBlock { get; }
        Thread ExecThread { get; set; }
        bool IsBlockedInEventCall { get; set; }
        bool IsBlockedAtRollbackBlock { get; set; }
    }

    /// <summary>
	/// Interface that is implemented by an executive.
	/// </summary>
	public interface IExecutive : IExecutiveLight
    {
        /// <summary>
        /// Requests that the executive queue up an event to be serviced at the current executive time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <param name="execEventType">The way the event is to be served by the executive.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestImmediateEvent(ExecEventReceiver eer, object userData, ExecEventType execEventType);

        /// <summary>
		/// Removes an already-submitted request for a time-based notification.
		/// </summary>
		/// <param name="eventHashCode">The code that identifies the event request to be removed.</param>
		void UnRequestEvent(long eventHashCode);
		/// <summary>
		/// Removes an already-submitted request for a time-based notification based on a user-provided selector object.
		/// </summary>
		/// <param name="ees">An object that will be used to select the events to remove.</param>
		void UnRequestEvents(IExecEventSelector ees);
		/// <summary>
		/// Removes all already-submitted requests for a time-based notification into a specific callback target object.
		/// </summary>
		/// <param name="execEventReceiverTarget">The callback target for which all queued events are to be removed.</param>
		void UnRequestEvents(object execEventReceiverTarget);
		/// <summary>
		/// Removes all already-submitted requests for a time-based notification into a specific callback target object.
		/// </summary>
		/// <param name="execEventReceiverMethod">The callback method for which all queued events are to be removed.</param>
		void UnRequestEvents(Delegate execEventReceiverMethod);
        /// <summary>
        /// This method blocks until the events that correlate to the provided event codes (which are returned from the RequestEvent
        /// APIs) are completely serviced. The event on whose thread this method is called must be a detachable event, all of the
        /// provided events must have been requested already, and none can have already been serviced.
        /// </summary>
        /// <param name="eventCodes">The event codes.</param>
        void Join(params long[] eventCodes);

        /// <summary>
        /// If running, pauses the executive and transitions its state to 'Paused'.
        /// </summary>
        void Pause();
        /// <summary>
        /// If paused, unpauses the executive and transitions its state to 'Running'.
        /// </summary>
        void Resume();

        /// <summary>
		/// Aborts the executive. This always flushes the event queue and terminates all running events.
		/// </summary>
		void Abort();

        /// <summary>
        /// Removes all instances of .NET event and simulation discrete event callbacks from this executive.
        /// </summary>
        /// <param name="target">The object to be detached from this executive.</param>
        void Detach(object target);

        /// <summary>
		/// Removes any entries in the task graph whose keys or values have the TaskGraphVolatile attribute.
		/// This is used, typically, to 'reset' the task graph for a new simulation run.
		/// </summary>
		/// <param name="dictionary">The task graph context to be 'reset'.</param>
		void ClearVolatiles(IDictionary dictionary);
		/// <summary>
		/// The DetachableEventController associated with the currently-executing event, if it was
		/// launched as a detachable event. Otherwise, it returns null.
		/// </summary>
		IDetachableEventController CurrentEventController { get; }

        /// <summary>
		/// Returns a list of the detachable events that are currently running.
		/// </summary>
		ArrayList LiveDetachableEvents { get; }

        /// <summary>
        /// Fired when this executive pauses.
        /// </summary>
        event ExecutiveEvent ExecutivePaused;
        /// <summary>
        /// Fired when this executive resumes.
        /// </summary>
        event ExecutiveEvent ExecutiveResumed;

        /// <summary>
		/// Fired when this executive has been aborted.
		/// </summary>
		event ExecutiveEvent ExecutiveAborted;
        /// <summary>
        /// Fired after service of the last event scheduled in the executive to fire at a specific time,
        /// assuming that there are more non-daemon events to fire.
        /// </summary>
        event ExecutiveEvent ClockAboutToChange;
    }
}
