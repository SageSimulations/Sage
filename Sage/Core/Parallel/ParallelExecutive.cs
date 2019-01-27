/* This source code licensed under the GNU Affero General Public License */
//#define USE_TEMPORAL_DEBUGGING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Highpoint.Sage.Utility;
using NameValueCollection = System.Collections.Specialized.NameValueCollection;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedVariable
// ReSharper disable RedundantDefaultMemberInitializer
#pragma warning disable 414

// TODO: ExecEvent and ExecEventReceiver need to become generics, with the type of the exec supplied with
// the declaration, so that an IExecutiveLight's events, for example, emit references to an IExecutiveLight,
// but an IExecutive's events still emit references to an IExecutive.
// This will allow me to remove all shunted APIs in IExecutiveLight.

namespace Highpoint.Sage.SimCore.Parallel
{

    /// <summary>
    /// This is a stripped-down executive, designed for applications in which parallel operation is the
    /// overriding concern. This executive does not support rescindable or detachable events,
    /// pause and resume, or detection of causality violations. However, multiple executives can be run
    /// in the same process, with limited temporal crosstalk supported by "WaitUntil(when)" and
    /// "Rollback(toWhen)" APIs.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.SimCore.IParallelExec" />
    internal class ParallelExecutive : IParallelExec
    {
        private readonly ManualResetEvent m_execTimeBlock = new ManualResetEvent(true);
        private object m_execLock = new object();

        static ParallelExecutive()
        {
#if LICENSING_ENABLED
            if (!Licensing.LicenseManager.Check()) {
                System.Windows.Forms.MessageBox.Show("Sage® Simulation and Modeling Library license is invalid.","Licensing Error");
            }
#endif
        }

        #region Private Fields

        private readonly Guid m_execGuid;
        private DateTime m_startTime = DateTime.MinValue;
        private SortedDictionary<ParallelExecEvent, DateTime> m_futureEvents;
        private SortedDictionary<ParallelExecEvent, DateTime> m_pastEvents;
        private int m_numNonDaemonEventsPending;
        private DateTime? m_lastEventServiceTime;
        private DateTime m_now;
        private ExecState m_execState = ExecState.Stopped;
        private int m_runNumber;
        private UInt32 m_eventCount;
        private bool m_stopRequested;
        private ParallelExecEvent m_currentEvent;
        private static bool _ignoreCausalityViolations = true;
        private Thread m_execThread;
        private readonly Queue<ParallelExecEvent> m_execEventBuffer = new Queue<ParallelExecEvent>();


        #region Rollback support elements

        private readonly List<Action> m_actionsOnRollback;

        #endregion

        #endregion Private Fields

        /// <summary>
        /// Creates a new instance of the <see cref="T:Executive3"/> class.
        /// </summary>
        /// <param name="execGuid">The GUID by which this executive will be known.</param>
        internal ParallelExecutive(Guid execGuid)
        {
            NameValueCollection nvc = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("Sage");
            if (nvc != null)
            {
                if (nvc["IgnoreCausalityViolations"] != null)
                    _ignoreCausalityViolations = bool.Parse(nvc["IgnoreCausalityViolations"]);

#if USE_TEMPORAL_DEBUGGING
                nvc = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("diagnostics");
                string strEba = nvc["ExecBreakAt"];
                if (!m_hasTarget && strEba != null && strEba.Length > 0)
                {
                    _targetdatestr = strEba;
                    m_targetdate = DateTime.Parse(_targetdatestr);
                    m_hasTarget = true;
                }
#endif
            }
            else
            {
                Console.WriteLine("No Sage initialization section found in app.config.");
            }

            m_futureEvents = new SortedDictionary<ParallelExecEvent, DateTime>(new ParExecEventComparer());
            m_pastEvents = new SortedDictionary<ParallelExecEvent, DateTime>(new ParExecEventComparer());
            m_actionsOnRollback = new List<Action>();
            m_execGuid = execGuid;
            m_runNumber = 0;
            Reset();
        }

        #region IExecutive Members

        /// <summary>
        /// The Guid by which this executive is known.
        /// </summary>
        /// <value></value>
        public Guid Guid
        {
            get { return m_execGuid; }
        }

        /// <summary>
        /// The current DateTime being managed by this executive. This is the 'Now' point of a
        /// simulation being run by this executive.
        /// </summary>
        /// <value></value>
        public DateTime Now
        {
            get { return m_now; }
        }

        /// <summary>
        /// If this executive has been run, this holds the DateTime of the last event serviced. May be from a previous run.
        /// </summary>
        public DateTime? LastEventServed
        {
            get { return m_lastEventServiceTime; }
        }

        /// <summary>
        /// The priority of the event currently being serviced. This executive forces all priorities to zero.
        /// </summary>
        /// <value></value>
        public double CurrentPriorityLevel
        {
            get { return m_currentEvent?.Priority ?? double.MinValue; }
        }

        /// <summary>
        /// The current <see cref="Highpoint.Sage.SimCore.ExecState"/> of this executive (running, stopped, paused, finished)...
        /// Proceeds from 'Stopped' before being 'Started,' and finally transitioning to 'Finished.'
        /// </summary>
        /// <value></value>
        public ExecState State
        {
            get { return m_execState; } // TODO: Is this supported in Parallel Exec?
        }

        #region Event Request mechanisms.

        /// <summary>
        /// Requests that the executive queue up a daemon event to be serviced at a specific time and
        /// priority. If only daemon events are enqueued, the executive will not be kept alive.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. This executive forces all priorities to zero.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        public long RequestDaemonEvent(ExecEventReceiver eer, DateTime when, double priority, object userData)
        {
            if (when < m_now)
            {
                if (!_ignoreCausalityViolations)
                {
                    string who = eer.Target.GetType().FullName;
                    IHasName name = eer.Target as IHasName;
                    if (name != null) who = name.Name;
                    string method = eer.Method.Name + "(...)";
                    string msg =
                        string.Format(
                            "Executive was asked to service an event prior to current time. This is a causality violation. The call was made from {0}.{1}.",
                            who, method);
                    throw new ApplicationException(msg);
                }
                else
                {
                    when = m_now;
                }
            }

            ParallelExecEvent pee = new ParallelExecEvent(eer, when, priority, userData, false, m_now);
            lock (m_execEventBuffer) m_execEventBuffer.Enqueue(pee);
            return pee.Key;
        }

        public long RequestEventAtOrAfter(ExecEventReceiver eer, DateTime when, Action onRevocation)
        {
            lock (m_execEventBuffer) m_execEventBuffer.Enqueue(new ParallelExecEvent(eer, when, 0.0, null, false, DateTimeOperations.Max(m_now, when), onRevocation));
            return 0L;
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero, and the userData object is assumed to be null.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when)
        {
            return RequestEvent(eer, when, 0.0, null);
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when, object userData)
        {
            return RequestEvent(eer, when, 0.0, userData);
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. This executive forces all priorities to zero.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData)
        {
            if (when < m_now)
            {
                if (!_ignoreCausalityViolations)
                {
                    string who = eer.Target.GetType().FullName;
                    if (eer.Target is IHasName) who = ((IHasName)eer.Target).Name;
                    string method = eer.Method.Name + "(...)";
                    string msg =
                        string.Format(
                            "Executive was asked to service an event prior to current time. This is a causality violation. The call was made from {0}.{1}.",
                            who, method);
                    //throw new ApplicationException(msg);
                    Console.WriteLine(msg);
                }
                else
                {
                    when = m_now;
                }
            }

            ParallelExecEvent pee = new ParallelExecEvent(eer, when, priority, userData, false, m_now);
            lock (m_execEventBuffer) m_execEventBuffer.Enqueue(pee);

            return pee.Key;
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. This executive forces all priorities to zero.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <param name="execEventType">The way the event is to be served by the executive.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        long IExecutiveLight.RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData,
            ExecEventType execEventType)
        {
            if (!execEventType.Equals(ExecEventType.Synchronous)) throw new ApplicationException("This parallel exec can currently only handle synchronous events.");
            return RequestEvent(eer, when, priority, userData);
        }

        private long Enqueue(ParallelExecEvent pee)
        {
            if (pee.When < m_now)
            {
                InitiateRollback(pee.When, () => Enqueue(pee));
                return pee.Key;
            }
            else
            {
                if (!pee.IsDaemon) Interlocked.Increment(ref m_numNonDaemonEventsPending);
                lock (m_futureEvents) m_futureEvents.Add(pee, pee.When);
                return pee.Key;
            }
        }

        #endregion

        public void SetStartTime(DateTime startTime)
        {
            m_startTime = startTime;
        }

        private bool m_cursorIsAtExecLock = false;
        /// <summary>
        /// Starts the executive. 
        /// </summary>
        public void Start()
        {

            // If there are events in the future event queue that predate the start of the
            // simulation, we will remove them from earliest to latest, before starting.
            lock (m_futureEvents)
            {
                while (m_futureEvents.Any() && m_futureEvents.First().Value < m_startTime)
                {
                    ParallelExecEvent pee = m_futureEvents.First().Key;
                    if (!pee.IsDaemon) Interlocked.Decrement(ref m_numNonDaemonEventsPending);
                    m_futureEvents.Remove(pee);
                }

                m_execState = ExecState.Running;
                m_execThread = Thread.CurrentThread;
                m_now = m_startTime;
                m_executiveStarted?.Invoke(this);
                m_runNumber++;
            }

            // Fire and remove any events that are supposed to fire once, at the start of the first
            // run of this executive. (This is only interesting if the executive supports resetting
            // and re-running.)
            if (m_executiveStartedSingleShot != null)
            {
                m_executiveStartedSingleShot(this);
                m_executiveStartedSingleShot =
                    (ExecutiveEvent)Delegate.RemoveAll(m_executiveStartedSingleShot, m_executiveStartedSingleShot);
            }

            try
            {
                //Monitor.Enter(this);

                // Make the currently-filling (background) buffer be the currently-loading (foreground) buffer,
                // And then load any events into the future events queue. This only loads execEvents provisioned
                // before the simulation began.
                lock (m_execEventBuffer)
                {
                    while (m_execEventBuffer.Any()) Enqueue(m_execEventBuffer.Dequeue());
                }

                // /////////////////////////////////////////////////////////////////////////////////////////////
                // Start of the simulation's main loop.
                // /////////////////////////////////////////////////////////////////////////////////////////////
                Monitor.Enter(m_execLock);
                while (m_numNonDaemonEventsPending > 0 && !m_stopRequested)
                {
                    m_currentEvent = m_futureEvents.First().Key;

                    if (m_currentEvent.When.Ticks < m_now.Ticks)
                    {
                        // If an event sneaked into the queue at a past time (race condition between
                        // local and remote clock) then just log the current event, and do a roll back.
                        Console.WriteLine("Race condition rollback of {0} from {1} to {2}.", Name, m_now,
                            m_currentEvent.When);
                        InitiateRollback(m_currentEvent.When);
                    }
                    else
                    {
                        m_futureEvents.Remove(m_currentEvent);
                        m_eventCount++;

                        if (m_currentEvent.When.Ticks > m_now.Ticks)
                        {
                            ClockAboutToChange?.Invoke(this);
                            m_now = m_currentEvent.When;
                        }

                        m_eventAboutToFire?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now,
                            m_currentEvent.UserData, ExecEventType.Synchronous);

                        // INVOKE THE EVENT.
                        // NOTE: If an executive is awaiting a future read, the m_execLock must be released there.
                        m_currentEvent.Eer(this, m_currentEvent.UserData);
                        try
                        {
                            m_pastEvents.Add(m_currentEvent, m_currentEvent.When);
                        }
                        catch (ArgumentException /*ae*/)
                        {
                            Console.WriteLine("Past Events given an event it's already seen. {0} future events.",
                                m_futureEvents.Count);
                        }
                        if (!m_currentEvent.IsDaemon) Interlocked.Decrement(ref m_numNonDaemonEventsPending);

                        m_lastEventServiceTime = m_now;

                        m_eventHasCompleted?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now,
                            m_currentEvent.UserData, ExecEventType.Synchronous);
                    }

                    lock (m_execEventBuffer)
                    {
                        while (m_execEventBuffer.Any()) Enqueue(m_execEventBuffer.Dequeue());
                    }
           
                    Monitor.Exit(m_execLock);
                    m_cursorIsAtExecLock = true;
                    //Console.WriteLine("{0} is{1} blocked at ExecLock.", this.Name, IsBlockedAtExecLock?"": " not");
                    m_execTimeBlock.WaitOne();
                    m_cursorIsAtExecLock = false;
                    Monitor.Enter(m_execLock);
                }
                Monitor.Exit(m_execLock);
                // /////////////////////////////////////////////////////////////////////////////////////////////
                // End of the simulation's main loop.
                // /////////////////////////////////////////////////////////////////////////////////////////////
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (m_stopRequested)
            {
                m_executiveStopped?.Invoke(this);
                m_stopRequested = false;
            }

            m_executiveFinished?.Invoke(this);

            m_execState = ExecState.Finished;

            Console.WriteLine("{0} finished at {1}", Name, Now);

        }

        //private void Dump(TextWriter @out)
        //{
        //    @out.WriteLine("Past List");
        //    foreach (ParallelExecEvent key in m_pastEvents.Keys)
        //    {
        //        @out.WriteLine(key);
        //    }
        //    @out.WriteLine("Future List ({0} non-daemon events.)", m_numNonDaemonEventsPending);
        //    foreach (ParallelExecEvent key in m_futureEvents.Keys)
        //    {
        //        @out.WriteLine(key);
        //    }
        //}

#if USE_TEMPORAL_DEBUGGING
        #region ELEMENTS IN SUPPORT OF TEMPORAL DEBUGGING
        static string _targetdatestr = new DateTime(1999, 7, 15, 3, 51, 21).ToString("r");
        DateTime m_targetdate = DateTime.Parse(_targetdatestr);
        bool m_hasTarget = false;
        bool m_hasFired = false;
        string m_hoverHere;
        #endregion ELEMENTS IN SUPPORT OF TEMPORAL DEBUGGING
#endif

        /// <summary>
        /// Stops the executive. This may be a pause or a stop, depending on if events are queued or running at the time of call.
        /// </summary>
        public void Stop()
        {
            m_stopRequested = true;
        }

        /// <summary>
        /// Resets the executive - this clears the event list and resets now to 1/1/01, 12:00 AM
        /// </summary>
        public void Reset()
        {
            m_futureEvents.Clear();
            m_pastEvents.Clear();
            m_numNonDaemonEventsPending = 0;
            m_now = DateTime.MinValue;
            m_execState = ExecState.Stopped;
            m_eventCount = 0;
            m_stopRequested = false;
            m_executiveReset?.Invoke(this);
        }

        /// <summary>
        /// The type of event currently being serviced by the executive. This executive services only Synchronous events.
        /// </summary>
        /// <value></value>
        public ExecEventType CurrentEventType
        {
            get { return ExecEventType.Synchronous; }
        }

        /// <summary>
        /// Returns a read-only list of the ExecEvents currently in queue for execution.
        /// Cast the elements in the list to IExecEvent to access the items' field values.
        /// </summary>
        /// <value></value>
        public IList EventList
        {
            get { return ArrayList.ReadOnly(new ArrayList(m_futureEvents.Keys)); }
        }

        /// <summary>
        /// The integer count of the number of times this executive has been run.
        /// </summary>
        /// <value></value>
        public int RunNumber
        {
            get { return m_runNumber; }
        }

        /// <summary>
        /// The number of events that have been serviced on this run.
        /// </summary>
        /// <value></value>
        public UInt32 EventCount
        {
            get { return m_eventCount; }
        }

        #region Events

        // If a lock is held on the Executive, then any attempt to add a handler to a public event is
        // blocked until that lock is released. This can cause client code to freeze, expecially if running
        // in a detachable event and adding a handler to an executive event. For that reason, all public
        // event members are methods with add {} and remove {} that defer to private event members. This
        // does not cause the aforementioned lockup.
        private event ExecutiveEvent m_executiveStarted;
        private event ExecutiveEvent m_executiveStartedSingleShot;
        private event ExecutiveEvent m_executiveReset;
        private event ExecutiveEvent m_executiveStopped;
        private event ExecutiveEvent m_executiveFinished;
        private event EventMonitor m_eventAboutToFire;
        private event EventMonitor m_eventHasCompleted;
        //private event ExecutiveEvent m_executiveAborted;
        //private event ExecutiveEvent m_executiveReset;
        //private event ExecutiveEvent m_clockAboutToChange;


        public event ExecutiveEvent ExecutiveStarted_SingleShot
        {
            add { m_executiveStartedSingleShot += value; }
            remove { m_executiveStartedSingleShot -= value; }
        }

        public event ExecutiveEvent ExecutiveStarted
        {
            add { m_executiveStarted += value; }
            remove { m_executiveStarted -= value; }
        }

        public event ExecutiveEvent ExecutiveStopped
        {
            add { m_executiveStopped += value; }
            remove { m_executiveStopped -= value; }
        }

        public event ExecutiveEvent ExecutiveFinished
        {
            add { m_executiveFinished += value; }
            remove { m_executiveFinished -= value; }
        }

        public event ExecutiveEvent ExecutiveReset
        {
            add { m_executiveReset += value; }
            remove { m_executiveReset -= value; }
        }

        /// <summary>
        /// Fired after an event has been selected to be fired, but before it actually fires.
        /// </summary>
        public event EventMonitor EventAboutToFire
        {
            add { m_eventAboutToFire += value; }
            remove { m_eventAboutToFire -= value; }
        }

        /// <summary>
        /// Fired after an event has been selected to be fired, and after it actually fires.
        /// </summary>
        public event EventMonitor EventHasCompleted
        {
            add { m_eventHasCompleted += value; }
            remove { m_eventHasCompleted -= value; }
        }

        #endregion

        /// <summary>
        /// This high performance exec does nothing on Dispose.
        /// </summary>
        /// <value></value>
        public void Dispose()
        {
        }

        #endregion // IExecutive members.

        public string Name { get; set; }

        //private int m_parallelWaiters;
        //public void AddWaiter() => Interlocked.Increment(ref m_parallelWaiters);
        //public void RemoveWaiter() => Interlocked.Decrement(ref m_parallelWaiters);
        private static readonly string s_dleMessage =
            "WakeCallerAt(...) called on executive's own thread. must only be called on another executive's thread.";

        public Thread ExecThread { get { return m_execThread; } set { m_execThread = value; } }

        private static string s_aveMessage2 =
            "ReleaseExecutive() was called on {0}, but it was not locked.";

        ///// <summary>
        ///// Wakes the calling thread when this executive's 'Now' reaches the specified time.
        ///// NOTE: It is a precondition that the local exec's thread must be locked, so the local
        ///// time is known and reliable.
        ///// </summary>
        ///// <param name="callingExec">The executive whose exec thread this call was made on.</param>
        ///// <param name="when">The time when this call is to return.</param>
        ///// <param name="andDoThis">What to do immediately prior to returning.</param>
        ///// <exception cref="DeadlockException"></exception>
        //public void WakeCallerAt(IParallelExec callingExec, DateTime when, Action andDoThis)
        //{
        //    if (Thread.CurrentThread == m_execThread) throw new DeadlockException(s_dleMessage);
        //    if (Now >= when)
        //    {
        //        andDoThis();
        //    }
        //    else
        //    {
        //        // This exective is at T0, and someone else wants to perform an action at T1,
        //        // where T1 > T0. We use the calling executive's PendingReadBlock
        //        RequestEventAtOrAfter((exec, data) =>
        //        {
        //            //Console.Out.WriteLine("{0} unblocked in function call.", Name);
        //            andDoThis?.Invoke();
        //            callingExec.PendingReadBlock.Set();

        //        }, when, () =>
        //        {
        //            //Console.Out.WriteLine("{0} unblocked in function call.", Name);
        //            callingExec.PendingReadBlock.Set();
        //        });

        //        // Block this thread until someone unblocks it. 
        //        // (This will probably be the completed read, though it could also be a read-abort on rollback.)
        //        callingExec.PendingReadBlock.WaitOne();
        //    }
        //}

        /// <summary>
        /// Gets or sets the coexecutor that is assigned to managing all of the parallel executives
        /// running in the same simulation.
        /// </summary>
        /// <value>The coexecutor.</value>
        public CoExecutor Coexecutor { get; set; }

        public bool IsBlockedPending { get { return !PendingBlock.WaitOne(0); } }
        public bool IsBlockedAtExecLock { get { return m_cursorIsAtExecLock && !m_execTimeBlock.WaitOne(0); } }

        public bool IsRollbackRequester { get; set; }
        public bool IsSynching { get; set; }

        public ManualResetEvent RollbackBlock { get; } = new ManualResetEvent(true);
        public ManualResetEvent PendingBlock { get; } = new ManualResetEvent(true);

        public void InitiateRollback(DateTime toWhen, Action doWhenRollbackCompletes = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by the CoExecutor to initiate the actual rollback.
        /// </summary>
        /// <param name="toWhen">To when.</param>
        public void PerformRollback(DateTime toWhen)
        {
            //Console.WriteLine("{0} BEGINNING a rollback to {1} with {2} PRAs.", this.Name, toWhen, m_actionsOnRollback.Count); // (Post-Rollback Actions)

            if (m_now > toWhen)
            {
                lock (m_actionsOnRollback)
                {
                    if (IsBlockedPending) System.Diagnostics.Debugger.Break();

                    // If there are multiple executives coexecuting, then we will delete any rollback targets prior
                    // to the earliest-running executive.
                    DateTime earliestCurrentExecTime = Coexecutor?.GetEarliestExecDateTime() ?? DateTime.MinValue;
                    //Console.WriteLine("{0} at {1} rollback to {2}.", Name, Now, toWhen, earliestCurrentExecTime);

                    m_numNonDaemonEventsPending = 0;
                    SortedDictionary<ParallelExecEvent, DateTime> futureEvents =
                        new SortedDictionary<ParallelExecEvent, DateTime>(new ParExecEventComparer());
                    SortedDictionary<ParallelExecEvent, DateTime> pastEvents =
                        new SortedDictionary<ParallelExecEvent, DateTime>(new ParExecEventComparer());

                    lock (m_futureEvents)
                    {
                        foreach (KeyValuePair<ParallelExecEvent, DateTime> keyValuePair in m_futureEvents)
                        {
                            if (keyValuePair.Key.AddedWhen < toWhen)
                            {
                                futureEvents.Add(keyValuePair.Key, keyValuePair.Value);
                                if (!keyValuePair.Key.IsDaemon) Interlocked.Increment(ref m_numNonDaemonEventsPending);
                            }
                            else
                            {
                                keyValuePair.Key.RevocationAction?.Invoke();
                            }
                        }
                    }

                    foreach (KeyValuePair<ParallelExecEvent, DateTime> keyValuePair in m_pastEvents)
                    {
                        if (keyValuePair.Key.When < earliestCurrentExecTime) continue;
                        if (keyValuePair.Key.When >= toWhen)
                        {
                            if (keyValuePair.Key.AddedWhen < toWhen)
                            {
                                futureEvents.Add(keyValuePair.Key, keyValuePair.Value);
                                if (!keyValuePair.Key.IsDaemon) Interlocked.Increment(ref m_numNonDaemonEventsPending);
                            }
                        }

                        else if (keyValuePair.Key.When < toWhen)
                            pastEvents.Add(keyValuePair.Key, keyValuePair.Value);

                    }

                    //Console.WriteLine("Rescheduled {0} of {1} events as a result of a rollback.", futureEvents.Count,
                    //    m_futureEvents.Count);
                    m_pastEvents = pastEvents;
                    m_futureEvents = futureEvents;

                    m_now = toWhen;

                    //Console.Out.WriteLine("{0} performing {1} post-rollback actions.", Name, m_actionsOnRollback.Count);
                    //Console.Out.Flush();
                    foreach (Action action in m_actionsOnRollback)
                    {
                        action();
                    }
                    m_actionsOnRollback.Clear();
                }
            }


            RolledBack?.Invoke(m_now);  // TODO: 

            //Console.WriteLine("{0} COMPLETING a rollback to {1}, with {2} PRAs remaining.", this.Name, toWhen, m_actionsOnRollback.Count); // (Post-Rollback Actions)

            if (m_actionsOnRollback.Count > 0)
            {
                //System.Diagnostics.Debugger.Break();
                m_actionsOnRollback.First()();
            }    
        }

        //public void PurgePastEvents(DateTime priorTo)
        //{
        //    SortedDictionary<ParallelExecEvent, DateTime> pastEvents = new SortedDictionary<ParallelExecEvent, DateTime>(new ParExecEventComparer());
        //    foreach (KeyValuePair<ParallelExecEvent, DateTime> keyValuePair in m_pastEvents)
        //    {
        //        if (keyValuePair.Key.When >= priorTo) pastEvents.Add(keyValuePair.Key, keyValuePair.Value);
        //    }
        //    int nPurges = pastEvents.Count - m_pastEvents.Count;
        //    m_pastEvents = pastEvents;

        //    Console.WriteLine("Purged {0} historical events.", nPurges);
        //}

        public event TimeEvent RolledBack;

        #region Ugliness. Because a large body of code relies on the IExecutive interface, and the events specified in it use that interface, this class must also implement that interface

        public long RequestImmediateEvent(ExecEventReceiver eer, object userData, ExecEventType execEventType)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvent(long eventHashCode)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvents(IExecEventSelector ees)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvents(object execEventReceiverTarget)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvents(Delegate execEventReceiverMethod)
        {
            throw new NotImplementedException();
        }

        public void Join(params long[] eventCodes)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Detach(object target)
        {
            throw new NotImplementedException();
        }

        public void ClearVolatiles(IDictionary dictionary)
        {
            throw new NotImplementedException();
        }

        public IDetachableEventController CurrentEventController
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public ArrayList LiveDetachableEvents
        {
            get
            {
                throw new NotImplementedException();
            }
        }

#pragma warning disable 67
        public event ExecutiveEvent ExecutivePaused;
        public event ExecutiveEvent ExecutiveResumed;
        public event ExecutiveEvent ExecutiveAborted;
#pragma warning restore
        public event ExecutiveEvent ClockAboutToChange;

        #endregion

        public override string ToString()
        {
            return string.Format("{0} at {1} with {2} events waiting.", Name, Now, m_futureEvents.Count);
        }

        private int m_execTimeLocks;

        private readonly object m_execLockLock = new object();

        /// <summary>
        /// Called by another executive to lock this one. When it returns, the executive is at a
        /// consistent place in the event execution loop.
        /// </summary>
        public void LockExecutive()
        {
            lock (m_execLockLock)
            {
                m_execTimeLocks++;
                Console.WriteLine("Locked {0}, #locks = {1}", Name, m_execTimeLocks);
                if (m_execTimeLocks == 1)
                {
                    m_execTimeBlock.Reset();
                    Console.WriteLine(!m_execTimeBlock.WaitOne(0));
                }
            }
        }

        public void ReleaseExecutive(bool @override = false)
        {
            lock (m_execLockLock)
            {
                if (m_execTimeLocks == 0) throw new AccessViolationException(string.Format(s_aveMessage2, this));
                m_execTimeLocks--;
                Console.WriteLine("Unlocked {0}, #locks = {1}", Name, m_execTimeLocks - 1);
                if (@override)
                {
                    m_execTimeLocks = 0;
                    m_execTimeBlock.Set();
                }
                else
                {
                    if (m_execTimeLocks == 0) m_execTimeBlock.Set();
                }
            }
        }

        public void SuspendExecLock() { Monitor.Exit(m_execLock); }
        public void ResumeExecLock() { Monitor.Enter(m_execLock); }

        public void SynchronizeTo(IExecutive callersExecutive, SyncMode mode, Action actionToSynchronize)
        {

            if (callersExecutive == this)
            {
                actionToSynchronize();
            }
            else
            {
                IParallelExec callersExecAsParallel = callersExecutive as IParallelExec;
                System.Diagnostics.Debug.Assert(callersExecAsParallel != null);
                CoExecutor.SyncAction action = Coexecutor.Synchronize(callersExecAsParallel, this, mode);
                switch (action)
                {
                    case CoExecutor.SyncAction.Execute:
                        actionToSynchronize();
                        break;
                    case CoExecutor.SyncAction.Abort:
                        break;
                    case CoExecutor.SyncAction.Defer:
                        // Caller's exec past the time of the called executive. Caller must wait until the
                        // called executive catches up, then do the thing.
                        RequestEvent((exec, data) => { actionToSynchronize();
                                                       callersExecAsParallel.PendingBlock.Set(); // Unblock when the called exec catches up.
                        }, callersExecutive.Now);
                        callersExecAsParallel.PendingBlock.Reset(); // Reset will cause the thread to block at WaitOne().
                        callersExecAsParallel.PendingBlock.WaitOne(); // ...and block.
                        break;
                    default:
                        break;
                }
                // Successful synchronization.
                Coexecutor.ReleaseSync(callersExecAsParallel, this);
            }
        }
    }

    internal class ParallelExecEvent : ExecEvent
    {
        private static readonly ExecEventType PARALLEL_ALWAYS_SYNCHRONOUS = ExecEventType.Synchronous;
        private static int s_hcSeed = 0;
        private readonly int m_hcode;

        public static ParallelExecEvent Get(ExecEventReceiver eer, DateTime serveWhen, double priority, object userData,
            bool isDaemon, DateTime addedWhen)
        {
            ParallelExecEvent retval = new ParallelExecEvent(eer, serveWhen, priority, userData, isDaemon,
                addedWhen);
            return retval;
        }

        public ParallelExecEvent(ExecEventReceiver eer, DateTime serveWhen, double priority, object userData,
            bool isDaemon, DateTime addedWhen, Action revocationAction = null) :
                base(eer, serveWhen, priority, userData, PARALLEL_ALWAYS_SYNCHRONOUS, 0L, isDaemon)
        {
            RevocationAction = revocationAction;
            AddedWhen = addedWhen;
            m_hcode = Interlocked.Increment(ref s_hcSeed);
        }

        public ParallelExecEvent(ParallelExecEvent currentEvent) :
                this(currentEvent.Eer, currentEvent.When, currentEvent.Priority, currentEvent.UserData, currentEvent.IsDaemon, currentEvent.AddedWhen)
        {
        }

        public override int GetHashCode()
        {
            return m_hcode;
        }

        public Action RevocationAction { get; }
        public DateTime AddedWhen { get; }

        public override string ToString()
        {
            return string.Format("{0}, pri {1:N2}, call {2} with {3}. Added at {4}, {5} daemon.",
                When, Priority, ExecEventReceiver.Method.Name, UserData, AddedWhen,
                IsDaemon ? "is" : "is not");
        }
    }

    internal class ParExecEventComparer : IComparer<ParallelExecEvent>
    {
        public int Compare(ParallelExecEvent ee1, ParallelExecEvent ee2)
        {
            if (ee1 == null || ee2 == null)
                throw new ExecutiveException("ExecEventComparer called with one or more ExecEvents being null.");
            if (ee1 == ee2) return 0;
            if (ee1.m_when < ee2.m_when) return -1;
            if (ee1.m_when > ee2.m_when) return 1;
            if (ee1.m_priority < ee2.m_priority) return 1;
            return (ee1.GetHashCode() < ee2.GetHashCode()) ? -1 : 1;
        }
    }
}
