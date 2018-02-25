/* This source code licensed under the GNU Affero General Public License */
//#define USE_TEMPORAL_DEBUGGING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using NameValueCollection = System.Collections.Specialized.NameValueCollection;

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
    internal class ParallelExecutive : IExecutive, IParallelExec
    {

        static ParallelExecutive()
        {
#if LICENSING_ENABLED
            if (!Licensing.LicenseManager.Check()) {
                System.Windows.Forms.MessageBox.Show("Sage® Simulation and Modeling Library license is invalid.","Licensing Error");
            }
#endif
        }

        #region Private Fields

        private Guid m_execGuid;
        private DateTime m_startTime = DateTime.MinValue;
        private SortedList<ParallelExecEvent, DateTime> m_futureEvents;
        private SortedList<ParallelExecEvent, DateTime> m_pastEvents;
        private int m_numNonDaemonEventsPending;
        private DateTime? m_lastEventServiceTime;
        private DateTime m_now;
        private ExecState m_execState = ExecState.Stopped;
        private int m_runNumber;
        private UInt32 m_eventCount;
        private bool m_stopRequested;
        private long m_key;
        private ParallelExecEvent m_currentEvent;
        private static bool _ignoreCausalityViolations = true;
        private Thread m_execThread;

        #endregion Private Fields

        /// <summary>
        /// Creates a new instance of the <see cref="T:Executive3"/> class.
        /// </summary>
        /// <param name="execGuid">The GUID by which this executive will be known.</param>
        internal ParallelExecutive(Guid execGuid)
        {
            NameValueCollection nvc = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("Sage");
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

            m_futureEvents = new SortedList<ParallelExecEvent, DateTime>(new ParExecEventComparer());
            m_pastEvents = new SortedList<ParallelExecEvent, DateTime>(new ParExecEventComparer());
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

            return Enqueue(new ParallelExecEvent(eer, when, priority, userData, m_key, true, m_now));
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
                    if (eer.Target is IHasName) who = ((IHasName) eer.Target).Name;
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

            return Enqueue(new ParallelExecEvent(eer, when, priority, userData, m_key, false, m_now));
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
            //if ( !execEventType.Equals(ExecEventType.Synchronous) ) throw new ApplicationException("This high performance exec can currently only handler synchronous events.");
            return RequestEvent(eer, when, priority, userData);
        }

        private long Enqueue(ParallelExecEvent pee)
        {
            if ( !pee.IsDaemon ) m_numNonDaemonEventsPending++;
            if (pee.When < m_now)
            {
                IHasName ihn = pee.ExecEventReceiver.Target as IHasName;
                string msg =
                    string.Format(
                        "At {0}, an event was requested added to queue for (past) time {1} requesting call-in to {3}.{2}",
                        m_now, pee.When, pee.ExecEventReceiver.Method.Name,
                        ihn?.Name ?? pee.ExecEventReceiver.Target);
                if (_ignoreCausalityViolations)
                {
                    Console.WriteLine(msg);
                    return -1;
                }
                else
                {
                    throw new CausalityException(msg);
                }
            }
            else
            {
                m_futureEvents.Add(pee, pee.When);
                return m_key++;
            }
        }

        public void SetStartTime(DateTime startTime)
        {
            m_startTime = startTime;
        }

        /// <summary>
        /// Starts the executive. 
        /// </summary>
        public void Start()
        {
            m_execState = ExecState.Running;
            m_execThread = Thread.CurrentThread;
            while (m_futureEvents.Keys[0].When < m_startTime) { 
                if (!m_futureEvents.Keys[0].IsDaemon) m_numNonDaemonEventsPending--;
                m_futureEvents.RemoveAt(0);
            }
            m_now = m_startTime;
            m_executiveStarted?.Invoke(this);

            if (m_executiveStartedSingleShot != null)
            {
                m_executiveStartedSingleShot(this);
                m_executiveStartedSingleShot =
                    (ExecutiveEvent) Delegate.RemoveAll(m_executiveStartedSingleShot, m_executiveStartedSingleShot);
            }

            m_runNumber++;

            try
            {
                Monitor.Enter(this);

                while (m_numNonDaemonEventsPending > 0 && !m_stopRequested)
                {

                    //int tmp = 0;
                    //foreach (ParallelExecEvent parallelExecEvent in m_futureEvents.Keys)
                    //{
                    //    if (!parallelExecEvent.IsDaemon) tmp++;
                    //}
                    //if ( m_numNonDaemonEventsPending != tmp ) System.Diagnostics.Debugger.Break();

                    m_currentEvent = m_futureEvents.Keys[0];
                    m_futureEvents.RemoveAt(0);
                    m_eventCount++;

                    if (m_currentEvent.When.Ticks > m_now.Ticks)
                    {
                        ClockAboutToChange?.Invoke(this);

                        Monitor.Exit(this);
                        lock (this)
                        {
                            Monitor.PulseAll(this);
                            Monitor.Wait(this, 0);
                        }
                        while (m_parallelWaiters > 0) Thread.Yield();

                        Monitor.Enter(this);

                        //if (m_now < new DateTime(2018, 1, 1, 5, 30, 00) &&
                        //    m_currentEvent.When >= new DateTime(2018, 1, 1, 5, 30, 00))
                        //{
                        //    Dump(Console.Out);
                        //}
                        //if (m_now == new DateTime(2018, 1, 1, 5, 30, 00))
                        //{
                        //    Dump(Console.Out);
                        //}

                        m_now = m_currentEvent.When;
                    }

                    m_eventAboutToFire?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now,
                        m_currentEvent.UserData, ExecEventType.Synchronous);

                    m_currentEvent.Eer(this, m_currentEvent.UserData);
                    m_pastEvents.Add(m_currentEvent, m_currentEvent.When);

                    m_lastEventServiceTime = m_now;
                    if (!m_currentEvent.IsDaemon) m_numNonDaemonEventsPending--;

                    m_eventHasCompleted?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now,
                        m_currentEvent.UserData, ExecEventType.Synchronous);

                    if (m_rollBack)
                    {
                        DoRollback(m_rollbackToWhen);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Monitor.Exit(this);
            }

            if (m_stopRequested)
            {
                m_executiveStopped?.Invoke(this);
                m_stopRequested = false;
            }

            m_executiveFinished?.Invoke(this);

            m_execState = ExecState.Finished;

        }

        private void Dump(TextWriter @out)
        {
            @out.WriteLine("Past List");
            foreach (ParallelExecEvent key in m_pastEvents.Keys)
            {
                @out.WriteLine(key);
            }
            @out.WriteLine("Future List ({0} non-daemon events.)", m_numNonDaemonEventsPending);
            foreach (ParallelExecEvent key in m_futureEvents.Keys)
            {
                @out.WriteLine(key);
            }
        }

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
            m_key = 0;
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
            get { return ArrayList.ReadOnly(new ArrayList(((ICollection) m_futureEvents.Keys))); }
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


        private int m_parallelWaiters;
        public void AddWaiter() => Interlocked.Increment(ref m_parallelWaiters);
        public void RemoveWaiter() => Interlocked.Decrement(ref m_parallelWaiters);

        public void WakeMeAt(DateTime when)
        {
            bool doLog = (m_eventCount % 99 == 0);
            if (Thread.CurrentThread == m_execThread)
                throw new DeadlockException(
                    "WakeMeAt(...) called on executive's own thread. must only be called on another executive's thread.");

            if (doLog)
                Console.WriteLine(
                    "Other exec wants value at {0}, but locally, it is only {1}, so other exec will wait.", when, Now);
            AutoResetEvent are = new AutoResetEvent(false);
            RequestEvent((exec, data) =>
            {
                are.Set();
                AddWaiter(); /*Console.WriteLine("Starting to wait."); Console.Out.Flush();*/
            }, when);
            Monitor.Exit(this);
            // This is necessary so that the local exec can continue running while the remote one waits.
            are.WaitOne(); // This triggers when the local executive calls the event above.
            Monitor.Enter(this);
            // This holds the remote exec until the local exec is about to change the clock - or later.
            //Console.WriteLine("Wait complete at {0}.", Now); Console.Out.Flush();
            RemoveWaiter();
            if (doLog) Console.WriteLine("Other exec, after calling into the local, is resuming at {0}.", Now);
            System.Diagnostics.Debug.Assert(Now == when);

        }

        private CoExecutor m_coExecutor;
        public void SetCoexecutor(CoExecutor coExecutor)
        {
            m_coExecutor = coExecutor;
        }

        private bool m_rollBack;
        private DateTime m_rollbackToWhen;

        public void Rollback(DateTime toWhen)
        {
            m_rollBack = true;
            m_rollbackToWhen = toWhen;
        }

        private void DoRollback(DateTime toWhen)
        {
            // If there are multiple executives coexecuting, then we will delete any rollback targets prior
            // to the earliest-running executive.
            DateTime dawnOfHistory = m_coExecutor?.GetEarliestDateTime()??DateTime.MinValue;
            Console.WriteLine("Rolling back {0} from {1} to {2}, and erasing anything before {3}.", this.Name, this.Now, toWhen, dawnOfHistory);

            m_numNonDaemonEventsPending = 0;
            SortedList<ParallelExecEvent, DateTime> futureEvents = new SortedList<ParallelExecEvent, DateTime>(new ParExecEventComparer());
            SortedList<ParallelExecEvent, DateTime> pastEvents = new SortedList<ParallelExecEvent, DateTime>(new ParExecEventComparer());

            foreach (KeyValuePair<ParallelExecEvent, DateTime> keyValuePair in m_futureEvents)
            {
                if (keyValuePair.Key.AddedWhen < toWhen)
                {
                    futureEvents.Add(keyValuePair.Key, keyValuePair.Value);
                    if (!keyValuePair.Key.IsDaemon) m_numNonDaemonEventsPending++;
                }
            }

            foreach(KeyValuePair<ParallelExecEvent, DateTime> keyValuePair in m_pastEvents)
            {
                if (keyValuePair.Key.When < dawnOfHistory) continue;
                if (keyValuePair.Key.When >= toWhen)
                {
                    if ( keyValuePair.Key.AddedWhen < toWhen)
                    {
                        futureEvents.Add(keyValuePair.Key, keyValuePair.Value);
                        if (!keyValuePair.Key.IsDaemon) m_numNonDaemonEventsPending++;
                    }
                }

                else if (keyValuePair.Key.When < toWhen)
                    pastEvents.Add(keyValuePair.Key, keyValuePair.Value);                    

            }

            m_pastEvents = pastEvents;
            int nReschedules = futureEvents.Count - m_futureEvents.Count;
            m_futureEvents = futureEvents;

            Console.WriteLine("Rescheduled {0} events as a result of a rollback.", nReschedules);

            m_now = toWhen;

            m_rollBack = false;

            OnRollback?.Invoke(m_now);
        }

        public void PurgePastEvents(DateTime priorTo)
        {
            SortedList<ParallelExecEvent, DateTime> pastEvents = new SortedList<ParallelExecEvent, DateTime>(new ParExecEventComparer());
            foreach (KeyValuePair<ParallelExecEvent, DateTime> keyValuePair in m_pastEvents)
            {
                if (keyValuePair.Key.When >= priorTo) pastEvents.Add(keyValuePair.Key, keyValuePair.Value);
            }
            int nPurges = pastEvents.Count - m_pastEvents.Count;
            m_pastEvents = pastEvents;

            Console.WriteLine("Purged {0} historical events.", nPurges);
        }

        public event TimeEvent OnRollback;

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

        public IDetachableEventController CurrentEventController { get; }
        public ArrayList LiveDetachableEvents { get; }

        public event ExecutiveEvent ExecutivePaused;
        public event ExecutiveEvent ExecutiveResumed;
        public event ExecutiveEvent ExecutiveAborted;
        public event ExecutiveEvent ClockAboutToChange;

        #endregion

        public override string ToString()
        {
            return string.Format("{0} at {1} with {2} events waiting.", Name, Now, m_futureEvents.Count);
        }
    }

    internal class ParallelExecEvent : ExecEvent
    {
        private static ExecEventType PARALLEL_ALWAYS_SYNCHRONOUS = ExecEventType.Synchronous;

        public static ParallelExecEvent Get(ExecEventReceiver eer, DateTime serveWhen, double priority, object userData,
            long key, bool isDaemon, DateTime addedWhen)
        {
            ParallelExecEvent retval = new ParallelExecEvent(eer, serveWhen, priority, userData, key, isDaemon,
                addedWhen);
            return retval;
        }

        public ParallelExecEvent(ExecEventReceiver eer, DateTime serveWhen, double priority, object userData,
            long key, bool isDaemon, DateTime addedWhen) :
                base(eer, serveWhen, priority, userData, PARALLEL_ALWAYS_SYNCHRONOUS, key, isDaemon)
        {
            AddedWhen = addedWhen;
        }

        public ParallelExecEvent(ParallelExecEvent currentEvent) :
                this(currentEvent.Eer, currentEvent.When, currentEvent.Priority, currentEvent.UserData, currentEvent.Key, currentEvent.IsDaemon, currentEvent.AddedWhen)
        {
        }

        public DateTime AddedWhen { get; set; }

        public override string ToString()
        {
            return string.Format("{0}, pri {1:N2}, call {2} with {3}. Added at {4}, {5} daemon.",
                this.When, this.Priority, this.ExecEventReceiver.Method.Name, this.UserData, this.AddedWhen,
                this.IsDaemon ? "is" : "is not");
        }
    }

    internal class ParExecEventComparer : IComparer<ParallelExecEvent>
    {
        public int Compare(ParallelExecEvent ee1, ParallelExecEvent ee2)
        {
            if (ee1 == null || ee2 == null)
                throw new ExecutiveException("ExecEventComparer called with one or more ExecEvents being null.");
            if (ee1.m_when < ee2.m_when) return -1;
            if (ee1.m_when > ee2.m_when) return 1;
            if (ee1.m_priority < ee2.m_priority) return 1;
            if (ee1.m_key == ee2.m_key) return 0;
            return -1;
        }
    }
}
