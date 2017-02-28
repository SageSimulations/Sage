/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using System.Collections.Generic;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// This is a full-featured executive, with rescindable and detachable events, support for pause and resume and
    /// event priority sorting within the same timeslice, and detection of causality violations. Use FastExecutive
    /// if these features are unimportant and you want blistering speed.
    /// </summary>
    internal sealed class Executive : MarshalByRefObject, IExecutive {
        private DateTime? m_lastEventServiceTime = null;
        private Exception m_terminationException = null;
        private readonly ExecEventType m_defaultEventType = ExecEventType.Synchronous;
        private ExecState m_state = ExecState.Stopped;
        private DateTime m_now = DateTime.MinValue;
        private SortedList m_events = new SortedList(new ExecEventComparer());
        private Stack m_removals = new Stack();
        private double m_currentPriorityLevel = double.MinValue;
        private long m_nextReqHashCode = 0;
        private bool m_stopRequested = false;
        private bool m_abortRequested = false;
        private Guid m_guid;
        private int m_runNumber = -1;
        private UInt32 m_eventCount = 0;
        private int m_numDaemonEventsInQueue = 0;
        private int m_numEventsInQueue = 0;
        private ExecEventType m_currentEventType;

        private DetachableEvent m_currentDetachableEvent = null;
        
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Executive");
        private static bool _ignoreCausalityViolations = true;

        private static bool _clrConfigDone = false;
        
        private object m_eventLock = new Object();
        public int EventLockCount = 0;

        internal Executive(Guid execGuid) {
            m_guid = execGuid;
            m_currentEventType = ExecEventType.None;

            #region >>> Set up from-config-file parameters <<<
            int desiredMinWorkerThreads = 100;
            int desiredMaxWorkerThreads = 900;
            int desiredMinIocThreads = 50;
            int desiredMaxIocThreads = 100;
            NameValueCollection nvc = null;
            try {
                nvc = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("Sage");
            } catch (Exception e ){
                Console.WriteLine(e);
            }
            if (nvc == null) {
                Trace.WriteLine(s_cannot_Find_Sage_Section);
                // Leave at default values.
            } else {
                string workerThreads = nvc["WorkerThreads"];
                if (workerThreads == null || ( !int.TryParse(workerThreads, out desiredMaxWorkerThreads) )) {
                    Trace.WriteLine(s_cannot_Find_Workerthread_Directive);
                } // else wt has been set to the desired value.

                string ignoreCausalityViolations = nvc["IgnoreCausalityViolations"];
                if (ignoreCausalityViolations == null || !bool.TryParse(ignoreCausalityViolations, out _ignoreCausalityViolations)) {
                    Trace.WriteLine(s_cannot_Find_Causality_Directive);
                } // else micv has been set to the desired value.
            }

            if (!_clrConfigDone) {
                if (desiredMinWorkerThreads > desiredMaxWorkerThreads)
                    Swap(ref desiredMinWorkerThreads, ref desiredMaxWorkerThreads);
                if ( desiredMinIocThreads > desiredMaxIocThreads )
                    Swap(ref desiredMinIocThreads, ref desiredMaxIocThreads);

                // We want to know the number of worker and IO Threads the executive wants available.
                // It must be 
                int anwt, axwt, aniot, axiot;
                ThreadPool.GetMinThreads(out anwt, out aniot);
                ThreadPool.GetMaxThreads(out axwt, out axiot);

                desiredMinWorkerThreads = Math.Max(anwt,desiredMinWorkerThreads);
                desiredMaxWorkerThreads = Math.Max(axwt, desiredMaxIocThreads);
                desiredMinIocThreads = Math.Max(aniot, desiredMinIocThreads);
                desiredMaxIocThreads = Math.Max(axiot, desiredMaxIocThreads);
                try {

                    ThreadPool.SetMinThreads(desiredMinWorkerThreads, desiredMinIocThreads);
                    ThreadPool.SetMinThreads(desiredMinIocThreads, desiredMaxIocThreads);

                    _clrConfigDone = true;
                } catch (System.Runtime.InteropServices.COMException ce) {
                    string msg = string.Format("Failed attempt to set CLR Threadpool Working Threads [{0},{1}] and IO Completion Threads [{2},{3}].\r\n{4}",
                        desiredMinWorkerThreads, desiredMaxWorkerThreads, desiredMinIocThreads, desiredMaxIocThreads, ce);
                    Trace.WriteLine(msg);
                }
            }

            #endregion
        }

        private static void Swap(ref int a, ref int b) { int tmp = a; a = b; b = tmp; }

    #region Error Messages

    private static readonly string s_cannot_Find_Sage_Section =
@"Missing Sage section of config file. Defaulting to maintaining between 100 and 900 execution threads.
Add the following two sections to your app.config to fix this issue:
<configSections>
    <section name=""Sage"" type=""System.Configuration.NameValueSectionHandler, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" />
</configSections>
   ...and
<Sage>
    <add key=""WorkerThreads"" value=""100""/>
    <add key=""IgnoreCausalityViolations"" value=""true""/>
</Sage>
NOTE - everything will still work fine, we're just defaulting to maintaining between 100 and 900 worker threads for now, and ignoring causality exceptions.";

    private static readonly string s_cannot_Find_Workerthread_Directive =
@"Unable to find (or parse) WorkerThread directive in Sage section of App Config file. Add the following to the Sage section:
<Sage>\r\n<add key=""WorkerThreads"" value=""100""/>\r\n</Sage>
NOTE - everything will still work fine, we're just defaulting to maintaining between 100 and 900 worker threads for now.";

    private static readonly string s_cannot_Find_Causality_Directive =
@"Unable to find Causality Exception directive in Sage section of App Config file. Add the following to the Sage section:
    <add key=""IgnoreCausalityViolations"" value=""true""/>
NOTE - the engine will still run, we'll just ignore it if an event is requested earlier than tNow during a simulation.";

    #endregion

        internal ArrayList RunningDetachables = new ArrayList();

        /// <summary>
        /// Returns a read-only list of the detachable events that are currently running.
        /// </summary>
        public ArrayList LiveDetachableEvents { get { return ArrayList.ReadOnly(RunningDetachables); } }

        /// <summary>
        /// Returns a read-only list of the ExecEvents currently in queue for execution.
        /// Cast the elements in the list to IExecEvent to access the items' field values.
        /// </summary>
        public IList EventList { get { return ArrayList.ReadOnly(m_events.GetKeyList()); } }

        public Guid Guid => m_guid;

        /// <summary>
        /// Returns the simulation time that the executive is currently processing. Any event submitted with a requested
        /// service time prior to this time, will initiate a causality violation. If the App.Config file is not set to
        /// ignore these (see below), this will result in a CausalityException being thrown.
        /// </summary>
        public DateTime Now { get { return m_now; } }

        /// <summary>
        /// If this executive has been run, this holds the DateTime of the last event serviced. May be from a previous run.
        /// </summary>
        public DateTime? LastEventServed { get { return m_lastEventServiceTime; } }

        /// <summary>
        /// Returns the simulation time that the executive is currently processing. For a given time, any priority event
        /// may be submitted. For example, if the executive is processing an event with priority 1.5, and another event
        /// is requested with priority 2.0, (higher priorities are serviced first), that event will be the next to be
        /// serviced.
        /// </summary>
        public double CurrentPriorityLevel { get { return m_currentPriorityLevel; } }

        /// <summary>
        /// The state of the executive - Stopped, Running, Paused, Finished.
        /// </summary>
        public ExecState State { get { return m_state; } }

        /// <summary>
        /// Requests that the executive queue up a daemon event to be serviced at a specific time and
        /// priority. If only daemon events are enqueued, the executive will not be kept alive.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        public long RequestDaemonEvent(ExecEventReceiver eer, DateTime when, double priority, object userData){
            return RequestEvent(eer, when, priority, userData, m_defaultEventType,true);
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero, and the userData object is assumeds to be null.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when) {
            return RequestEvent(eer, when, 0.0, null, m_defaultEventType, false);
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
        public long RequestEvent(ExecEventReceiver eer, DateTime when, object userData) {
            return RequestEvent(eer, when, 0.0, userData, m_defaultEventType, false);
        }

        /// <summary>
        /// Requests scheduling of a synchronous event. Event service takes the form of a call, by the executive, into a specified method
        /// on a specified object, passing it the executive and a specified user data object. The method, the object and the
        /// user data are specified at the time of scheduling the event (i.e. when making this call). <p></p><p></p>
        /// <B>Note:</B> The event will be scheduled as a synchronous event. If you want another type of event, use the other
        /// form of this API.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver (a delegate) that will accept the call from the executive.</param>
        /// <param name="when">The DateTime at which the event is to be served.</param>
        /// <param name="priority">The priority at which the event is to be serviced. Higher values are serviced first,
        /// if both are scheduled for the same precise time.</param>
        /// <param name="userData">An object of any type that the code scheduling the event (i.e. making this call) wants to
        /// have passed to the code executing the event (i.e. the body of the ExecEventReceiver.)</param>
        /// <returns>A long, which is a number that serves as a key. This key is used, for example, to unrequest the event.</returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData){
            return RequestEvent(eer, when, priority, userData, m_defaultEventType, false );
        }

        /// <summary>
        /// Requests scheduling of an event, allowing the caller to specify the type of the event. Event service takes the
        /// form of a call, by the executive, into a specified method on a specified object, passing it the executive and a
        /// specified user data object. The method, the object and the user data are specified at the time of scheduling the
        /// event (i.e. when making this call). 
        /// </summary>
        /// <param name="eer">The ExecEventReceiver (a delegate) that will accept the call from the executive.</param>
        /// <param name="when">The DateTime at which the event is to be served.</param>
        /// <param name="priority">The priority at which the event is to be serviced. Higher values are serviced first,
        /// if both are scheduled for the same precise time.</param>
        /// <param name="userData">An object of any type that the code scheduling the event (i.e. making this call) wants to
        /// have passed to the code executing the event (i.e. the body of the ExecEventReceiver.)</param>
        /// <param name="execEventType">Specifies the type of event dispatching to be employed for this event.</param>
        /// <returns>A long, which is a number that serves as a key. This key is used, for example, to unrequest the event.</returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType){
            return RequestEvent(eer,when,priority,userData,execEventType,false);
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at the current executive time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <param name="execEventType">The way the event is to be served by the executive.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        public long RequestImmediateEvent(ExecEventReceiver eer, object userData, ExecEventType execEventType) {
            lock (m_events) {
                return RequestEvent(eer, m_now, m_currentPriorityLevel, userData, execEventType, false);
            }
        }

        private long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType, bool isDaemon){
            if (!m_stopRequested && !m_abortRequested) {
                Debug.Assert(eer != null, "An event was requested to call into a null callback.");
                lock (m_events) {
                    if (m_state == ExecState.Running) {
                        if (when < m_now) {
                            if (!_ignoreCausalityViolations) {
                                throw new CausalityException("Event requested for time " + when + ", but executive is at time " + m_now + ". " +
                                    "\r\nAdd \"<add key=\"IgnoreCausalityViolations\" value=\"true\"/>\r\n" +
                                    "to the Sage section of your app.config file to prevent these exceptions. (Submitted event requests will be ignored.)");
                            } else {
                                return long.MinValue;
                            }
                        }
                    }
                    if (m_state != ExecState.Finished) {
                        m_nextReqHashCode++;
                        if (isDaemon) m_numDaemonEventsInQueue++;
                        m_numEventsInQueue++;
                        m_events.Add(ExecEvent.Get(eer, when, priority, userData, execEventType, m_nextReqHashCode, isDaemon), m_nextReqHashCode);
                        if (s_diagnostics) {
                            Trace.WriteLine("Event requested for time " + when + ", to call back at " + eer.Target + "(" + eer.Target.GetHashCode() + ")." + eer.Method.Name);
                        }
                        return m_nextReqHashCode;
                    } else {
                        throw new ApplicationException("Event service cannot be requested from an Executive that is in the \"Finished\" state.");
                    }
                }
            } else {
                return -1;
            }
        }

        public void UnRequestEvent(long requestedEventHashCode){
            if ( requestedEventHashCode == long.MinValue ) return; // illegitimate key.
            m_removals.Push(new ExecEventRemover(requestedEventHashCode));
        }

        public void UnRequestEvents(object execEventReceiverTarget){
            m_removals.Push(new ExecEventRemover(execEventReceiverTarget));
        }

        public void UnRequestEvents(IExecEventSelector eventSelector){
            m_removals.Push(new ExecEventRemover(eventSelector));
        }

        public void UnRequestEvents(Delegate execEventReceiverMethod){
            m_removals.Push(new ExecEventRemover((Delegate)execEventReceiverMethod));
        }

        #region Join Handling
        private class JoinSet {

            private Executive m_exec;
            private List<long> m_eventCodes;
            private IDetachableEventController m_idec;

            public JoinSet(Executive exec, long[] eventCodes) {
                m_exec = exec;
                m_eventCodes = new List<long>(eventCodes);
                foreach (long eventCode in eventCodes) {
                    ((ExecEvent)m_exec.m_events.GetKey(m_exec.m_events.IndexOfValue(eventCode))).ServiceCompleted += new EventMonitor(ee_ServiceCompleted);
                }
            }

            private void ee_ServiceCompleted(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType) {
                m_eventCodes.Remove(key);
                if (m_eventCodes.Count == 0) {
                    m_idec.Resume();
                }
            }

            public void Join() {
                Debug.Assert(m_exec.CurrentEventType == ExecEventType.Detachable, "Cannot call Join on a non-Detachable event.");
                m_idec = m_exec.CurrentEventController;
                List<string> eventCodes = new List<string>();
                m_eventCodes.ForEach(delegate(long ec){eventCodes.Add(ec.ToString());});
                Console.WriteLine("I am waiting to join on " + Utility.StringOperations.ToCommasAndAndedList(eventCodes) + ".");
                m_idec.Suspend();
                Console.WriteLine("I am done waiting to join on " + Utility.StringOperations.ToCommasAndAndedList(eventCodes) + ".");
            }
        }

        /// <summary>
        /// This method blocks until the events that correlate to the provided event codes (which are returned from the RequestEvent
        /// APIs) have been completely serviced. The event on whose thread this method is called must be a detachable event, all of
        /// the provided events must have been requested already, and none can have already been serviced.
        /// </summary>
        /// <param name="eventCodes">The event codes.</param>
        public void Join(params long[] eventCodes) {
            JoinSet joinSet = new JoinSet(this, eventCodes);
            joinSet.Join();
        }

        #endregion

        public int RunNumber { get { return m_runNumber; } }

        /// <summary>
        /// The number of events that have been serviced on this run.
        /// </summary>
        public UInt32 EventCount { get { return m_eventCount; } }

        private DateTime m_startTime = DateTime.MinValue;
        public void SetStartTime(DateTime startTime) {
            m_startTime = startTime;
        }

        public void Start() {

            m_pauseMgr = new Thread(new ThreadStart(_DoPause));
            m_pauseMgr.Name = "Pause Management";
            m_pauseMgr.Start();

            lock (this) {

                m_now = m_startTime;

                #region Initial bookkeeping setup
                m_terminationException = null;
                m_runNumber++;
                m_eventCount = 0;
                #endregion Initial bookkeeping setup

                #region Diagnostics
                if (s_diagnostics) {
                    Trace.WriteLine("Executive starting with the following events queued up...");
                }
                #endregion Diagnostics

                #region Kickoff Events
                m_executiveStarted?.Invoke(this);
                if (ExecutiveStartedSingleShot != null) {
                    ExecutiveStartedSingleShot(this);
                    ExecutiveStartedSingleShot = (ExecutiveEvent)Delegate.RemoveAll(ExecutiveStartedSingleShot, ExecutiveStartedSingleShot);
                }
                #endregion Kickoff Events

                m_state = ExecState.Running;

                uint initialEventCount = m_eventCount;

                while (!m_stopRequested && !m_abortRequested && ( m_numEventsInQueue > m_numDaemonEventsInQueue )) {

                    Monitor.Enter(m_runLock);
                    Monitor.Exit(m_runLock);

                    #region Diagnostics
                    if (s_diagnostics)
                        DumpEventQueue();
                    #endregion Diagnostics

                    m_eventCount++;

                    #region Process queued-up event removal requests
                    while (m_removals.Count > 0) {
                        ExecEventRemover er = (ExecEventRemover)m_removals.Pop();
                        er.Filter(ref m_events);

                        // Now determine the correct number of regular and daemon events in the executive.
                        // TODO: Can we do this outside the while loop?
                        m_numDaemonEventsInQueue = 0;
                        m_numEventsInQueue = 0;
                        foreach (ExecEvent ee in m_events.Keys) {
                            m_numEventsInQueue++;
                            if (ee.IsDaemon)
                                m_numDaemonEventsInQueue++;
                        }
                    }
                    #endregion Process queued-up event removal requests

                    ExecEvent currentEvent;
                    #region Identify and select the current event
                    lock (m_events) {
                        // TODO: While awaiting this lock, the last even may have been resc
                        if (m_numEventsInQueue > 0) {
                            try {  // MTHACK
                                currentEvent = (ExecEvent)m_events.GetKey(0);
                                m_events.RemoveAt(0);
                                m_currentPriorityLevel = currentEvent.m_priority;
                                m_lastEventServiceTime = m_now;
                                m_now = currentEvent.m_when;
                            } catch { // MTHACK 
                                break;  // MTHACK 
                            } // MTHACK 
                        } else {
                            break;
                        }
                    }
                    #endregion Identify and select the current event

                    m_eventAboutToFire?.Invoke(currentEvent.Key, currentEvent.Eer, currentEvent.m_priority, currentEvent.m_when, currentEvent.m_userData, currentEvent.m_eventType);

                    try {
                        m_currentEventType = currentEvent.m_eventType;
                        if (currentEvent.IsDaemon) m_numDaemonEventsInQueue--;
                        m_numEventsInQueue--;
                        if (s_diagnostics)
                            Trace.WriteLine(string.Format(_eventSvcMsg, currentEvent, currentEvent.Eer.Target, currentEvent.Eer.Target.GetHashCode(), currentEvent.Eer.Method.Name));
                        switch (currentEvent.m_eventType) {
                            case ExecEventType.Synchronous:
                                currentEvent.Eer(this, currentEvent.m_userData);
                                currentEvent.OnServiceCompleted();
                                break;
                            case ExecEventType.Detachable:
                                m_currentDetachableEvent = new DetachableEvent(this, currentEvent);
                                m_currentDetachableEvent.Begin();
                                m_currentDetachableEvent = null;
                                break;
                            case ExecEventType.Asynchronous:
                                ThreadPool.QueueUserWorkItem(AsyncExecutor, new object[] { this, currentEvent });
                                break;
                            default:
                                throw new ExecutiveException("EventType " + currentEvent.m_eventType + " is not yet supported.");
                        }
                        m_eventHasCompleted?.Invoke(currentEvent.Key, currentEvent.Eer, currentEvent.m_priority, currentEvent.m_when, currentEvent.m_userData, currentEvent.m_eventType);

                    } catch (Exception e) {
                        m_terminationException = e;
                        // TODO: Re-throw this exception on the simulation executor's thread.
                        //Trace.WriteLine("Exception thrown back into the executive : " + e);
                        //Trace.Flush();
                        m_stopRequested = true;
                    } finally {
                        m_currentEventType = ExecEventType.None;
                    }

                    while (EventLockCount > 0) {
                        Monitor.Pulse(m_eventLock);
                        lock (m_eventLock) { }
                    }

                    if (m_clockAboutToChange != null) {
                        if (m_numEventsInQueue > m_numDaemonEventsInQueue ) {
                            DateTime nextEventTime = ( (ExecEvent)m_events.GetKey(0) ).m_when;
                            //DateTime nextEventTime = ((ExecEvent)m_events[0]).m_when;
                            if (nextEventTime > m_now) {
                            m_clockAboutToChange(this);
                            }
                        }
                    }
                }

                if (initialEventCount == m_eventCount) {
                    Trace.WriteLine("Simulation completed without having executed a single event.");
                }

                if (m_stopRequested) {
                    if (m_events.Count > 0) {
                        m_state = ExecState.Paused;
                        if (m_executiveStopped != null)
                            m_executiveStopped(this);
                    } else {
                        m_state = ExecState.Stopped;
                    }
                    m_stopRequested = false; // We've taken care of it.
                } else {
                    m_state = ExecState.Finished;
                }

                if (RunningDetachables.Count > 0) {
                    // TODO: Move this error reporting into a StringBuilder, and report it upward, rather than just to Console.
                    ArrayList tmp = new ArrayList(RunningDetachables);
                    foreach (DetachableEvent de in tmp) {
                        bool issuedError = false;
                        if (de.IsWaiting()) {
                            if (!de.HasAbortHandler) {
                                if (!issuedError)
                                    Trace.WriteLine("ERROR : MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE!");
                                issuedError = true;
                                Trace.WriteLine("\tWaiting Event : " + de.RootEvent.ToString());
                                Trace.WriteLine("\tEvent initially called into " + ( (ExecEvent)de.RootEvent ).Eer.Target + ", on method " + ( (ExecEvent)de.RootEvent ).Eer.Method.Name);
                                Trace.WriteLine("\tThe event was suspended at time " + de.TimeOfLastWait + ", and was never resumed.");
                                if (de.SuspendedStackTrace != null)
                                    Trace.WriteLine("CALL STACK:\r\n" + de.SuspendedStackTrace);
                            }
                            de.Abort();
                        }
                    }
                    m_currentDetachableEvent = null;

                    while (RunningDetachables.Count > 0)
                        Thread.SpinWait(1);

                    if (m_executiveAborted != null)
                        m_executiveAborted(this);
                }

                //if (m_terminationException != null) {
                //    m_pauseMgr.Abort();
                //    if (m_executiveFinished != null)
                //        m_executiveFinished(this);
                //    throw new RuntimeException(String.Format("Executive with hashcode {0} experienced an exception in user code.", GetHashCode()), m_terminationException);
                //}
            }

            m_pauseMgr.Abort();

            m_executiveFinished?.Invoke(this);

            if (m_terminationException != null && !m_abortRequested) {
                throw new RuntimeException(String.Format("Executive with hashcode {0} experienced an exception in user code.", GetHashCode()), m_terminationException);
            }
            m_abortRequested = false;
        }
        private static string _eventSvcMsg = "Serving {0} event to {1}({2}).{3}";


        private void DumpEventQueue() {
            lock (m_events) {
                Trace.WriteLine("Event Queue: (highest number served next)");
                foreach (DictionaryEntry de in m_events) {
                    ExecEvent ee = (ExecEvent)de.Key;
                    if (ee.Eer.Target is DetachableEvent) {
                        ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.Eer.Target).RootEvent).Eer;
                        Trace.WriteLine(de.Value + ").\t" + ee.m_eventType + " Event is waiting to be fired at time " + ee.m_when + " into " + eer.Target + "(" + eer.Target.GetHashCode() + "), " + eer.Method.Name);
                    } else {
                        Trace.WriteLine(de.Value + ").\t" + ee.m_eventType + " Event is waiting to be fired at time " + ee.m_when + " into " + ee.Eer.Target + "(" + ee.Eer.Target.GetHashCode() + "), " + ee.Eer.Method.Name);
                    }
                }
                Trace.WriteLine("***********************************");
            }
        }


        public IDetachableEventController CurrentEventController {
            get {
                return m_currentDetachableEvent;
            }
        }

        /// <summary>
        /// The type of event currently being serviced by the executive.
        /// </summary>
        public ExecEventType CurrentEventType { get { return m_currentEventType; } }

        internal void SetCurrentEventType(ExecEventType eet){
            m_currentEventType = eet;
        }


        internal void SetCurrentEventController(DetachableEvent de){
            //if ( m_currentDetachableEvent != null ) {
            //    throw new ExecutiveException("Attempt to overwrite the current detachable event!");
            //}
            m_currentDetachableEvent = de;
        }

        private object m_pauseLock = new object();
        private object m_runLock = new object();
        private Thread m_pauseMgr = null;
        private void _DoPause() {
            try {
                while (true) {
                    lock (m_runLock) {
                        //Console.WriteLine("Pause thread is waiting for a pulse on RunLock.");
                        
                        Monitor.Wait(m_runLock);
                        //Console.WriteLine("Pause thread received a pulse on RunLock.");
                    }
                    m_state = ExecState.Paused;
                    lock (m_pauseLock) {
                        //Console.WriteLine("Pause thread is acquiring an exclusive handle on RunLock.");
                        Monitor.Enter(m_runLock);
                        //Console.WriteLine("Pause thread is waiting for a pulse on PauseLock.");
                        Monitor.Wait(m_pauseLock);
                        //Console.WriteLine("Pause thread received a pulse on PauseLock.");
                        //Console.WriteLine("Pause thread is releasing its exclusive handle on RunLock.");
                        Monitor.Exit(m_runLock);
                    }
                    m_state = ExecState.Running;
                }
            } catch (ThreadAbortException) {
                Thread.ResetAbort();
            }
        }
        /// <summary>
        /// If running, pauses the executive and transitions its state to 'Paused'.
        /// </summary>
        public void Pause() {
            if (m_state.Equals(ExecState.Running)) {
                lock (m_runLock) {
                    //Console.WriteLine("User thread is pulsing RunLock.");
                    Monitor.Pulse(m_runLock);
                }
                if (m_executivePaused != null) m_executivePaused(this);
            }
        }

        /// <summary>
        /// If paused, unpauses the executive and transitions its state to 'Running'.
        /// </summary>
        public void Resume() {
            if (m_state.Equals(ExecState.Paused)) {
                lock (m_pauseLock) {
                    //Console.WriteLine("User thread is pulsing PauseLock.");
                    Monitor.Pulse(m_pauseLock);
                }
                if (m_executiveResumed != null) m_executiveResumed(this);
            }
        }

        public void Stop(){
            // State change happens in the processing loop when 'm_stopRequested' is discovered true.
            m_stopRequested = true;
            if (m_state.Equals(ExecState.Paused)) {
                lock (m_pauseLock) {
                    //Console.WriteLine("User thread is pulsing PauseLock.");
                    Monitor.Pulse(m_pauseLock);
                }
            }
        }


        public void Abort(){
            // We need to do two things. First, we have to abort any Detachable Events that are currently
            // running. Second, we need to scrub all possible graphcontexts that could have been in process
            // in those detachable events.
            m_abortRequested = true;
            foreach ( DetachableEvent de in RunningDetachables ) de.Abort();

            Reset();
        }

        /// <summary>
        /// Resets the executive - this clears the event list and resets now to 1/1/01, 12:00 AM
        /// </summary>
        public void Reset(){
            m_state = ExecState.Stopped;
            m_now = DateTime.MinValue;
            m_events = new SortedList(new ExecEventComparer());
            m_currentPriorityLevel = double.MinValue;
            m_stopRequested = false;
            m_numEventsInQueue = 0;
            m_numDaemonEventsInQueue = 0;

            // We were hanging the executive if we reset while it was paused.
            Resume();

            if (m_executiveReset != null) m_executiveReset(this);
        }

        /// <summary>
        /// Removes all instances of .NET event and simulation discrete event callbacks from this executive.
        /// </summary>
        /// <param name="target">The object to be detached from this executive.</param>
        public void Detach(object target) {

            foreach (ExecutiveEvent md in new ExecutiveEvent[] { m_clockAboutToChange, m_executiveAborted, m_executiveFinished, m_executiveReset, m_executiveStarted, ExecutiveStartedSingleShot, m_executiveStopped}) {
                if (md == null) continue;
                ExecutiveEvent tmp = md;
                List<ExecutiveEvent> lstDels = new List<ExecutiveEvent>();
                foreach (ExecutiveEvent ee in md.GetInvocationList()) {
                    if (ee.Target == target) {
                        lstDels.Add(ee);
                    }
                }
                lstDels.ForEach(n => tmp -= n);
            }

            foreach (EventMonitor em in new EventMonitor[] { m_eventAboutToFire, m_eventHasCompleted }) {
                if (em == null) continue;
                EventMonitor tmp = em;
                List<EventMonitor> lstDels = new List<EventMonitor>();
                foreach (EventMonitor ee in em.GetInvocationList()) {
                    if (ee.Target == target) {
                        lstDels.Add(ee);
                    }
                }
                lstDels.ForEach(n => tmp -= n);
            }

            UnRequestEvents(target);

        }

        // If a lock is held on the Executive, then any attempt to add a handler to a public event is
        // blocked until that lock is released. This can cause client code to freeze, expecially if running
        // in a detachable event and adding a handler to an executive event. For that reason, all public
        // event members are methods with add {} and remove {} that defer to private event members. This
        // does not cause the aforementioned lockup.
        private event ExecutiveEvent m_executiveStarted;
        private event ExecutiveEvent ExecutiveStartedSingleShot;
        private event ExecutiveEvent m_executiveStopped;
        private event ExecutiveEvent m_executivePaused;
        private event ExecutiveEvent m_executiveResumed;
        private event ExecutiveEvent m_executiveFinished;
        private event ExecutiveEvent m_executiveAborted;
        private event ExecutiveEvent m_executiveReset;
        private event ExecutiveEvent m_clockAboutToChange;
        private event EventMonitor m_eventAboutToFire;
        private event EventMonitor m_eventHasCompleted;


        public event ExecutiveEvent ExecutiveStarted_SingleShot {
            add { ExecutiveStartedSingleShot += value; }
            remove { ExecutiveStartedSingleShot -= value; }
        }
        public event ExecutiveEvent ExecutiveStarted {
            add { m_executiveStarted += value; }
            remove { m_executiveStarted -= value; }
        }

        /// <summary>
        /// Fired when this executive pauses.
        /// </summary>
        public event ExecutiveEvent ExecutivePaused {
            add { m_executivePaused += value; }
            remove { m_executivePaused -= value; }
        }
        /// <summary>
        /// Fired when this executive resumes.
        /// </summary>
        public event ExecutiveEvent ExecutiveResumed {
            add { m_executiveResumed += value; }
            remove { m_executiveResumed -= value; }
        }

        public event ExecutiveEvent ExecutiveStopped {
            add { m_executiveStopped += value; }
            remove { m_executiveStopped -= value; }
        }
        public event ExecutiveEvent ExecutiveFinished {
            add { m_executiveFinished += value; }
            remove { m_executiveFinished -= value; }
        }
        public event ExecutiveEvent ExecutiveAborted {
            add { m_executiveAborted += value; }
            remove { m_executiveAborted -= value; }
        }
        public event ExecutiveEvent ExecutiveReset {
            add { m_executiveReset += value; }
            remove { m_executiveReset -= value; }
        }

        /// <summary>
        /// Fired after service of the last event scheduled in the executive to fire at a specific time,
        /// assuming that there are more non-daemon events to fire.
        /// </summary>
        public event ExecutiveEvent ClockAboutToChange {
            add { m_clockAboutToChange += value; }
            remove { m_clockAboutToChange -= value; }
        }

        /// <summary>
        /// Fired after an event has been selected to be fired, but before it actually fires.
        /// </summary>
        public event EventMonitor EventAboutToFire {
            add { m_eventAboutToFire += value; }
            remove { m_eventAboutToFire -= value; }
        }

        /// <summary>
        /// Fired after an event has been selected to be fired, and after it actually fires.
        /// </summary>
        public event EventMonitor EventHasCompleted {
            add { m_eventHasCompleted += value; }
            remove { m_eventHasCompleted -= value; }
        }

        /// <summary>
        /// Must call this before disposing of a model.
        /// </summary>
        /// <value></value>
        public void Dispose() {
            try { if (m_pauseMgr != null && m_pauseMgr.IsAlive) m_pauseMgr.Abort(); } catch { }
        }

        /// <summary>
        /// Acquires the event lock.
        /// </summary>
        internal void AcquireEventLock(){
            lock ( m_eventLock ) {
                Interlocked.Increment(ref EventLockCount);
            }
        }

        /// <summary>
        /// Releases the event lock.
        /// </summary>
        internal void ReleaseEventLock() {
            lock ( m_eventLock ) {
                Interlocked.Decrement(ref EventLockCount);
            }
        }

        private static readonly bool s_dumpVolatileClearing = false;
        public void ClearVolatiles(IDictionary dictionary){
            if (s_dumpVolatileClearing) Trace.WriteLine("---------------------- C L E A R I N G   V O L A T I L E S --------------------------------");
            ArrayList entriesToRemove = new ArrayList();
            foreach ( DictionaryEntry de in dictionary ) {
                
                if (s_dumpVolatileClearing) Trace.WriteLine("Checking key " + de.Key + " and value " + de.Value);

                if ( de.Key.GetType().GetCustomAttributes(typeof(TaskGraphVolatileAttribute),true).Length > 0 ) {
                    entriesToRemove.Add(de.Key);
                } else if ( de.Value != null ) {
                    if ( de.Value.GetType().GetCustomAttributes(typeof(TaskGraphVolatileAttribute),true).Length > 0 ) {
                        entriesToRemove.Add(de.Key);
                    }
                }
            }
            foreach ( object key in entriesToRemove ) {
                if (s_dumpVolatileClearing) Trace.WriteLine("Removing volatile listed under key " + key);
                dictionary.Remove(key);
            }
            if (s_dumpVolatileClearing) Trace.WriteLine("---------------------- C L E A R E D  " + entriesToRemove.Count + "   V O L A T I L E S --------------------------------");

            if (s_dumpVolatileClearing) {
                Trace.WriteLine("Here's what's left:");
                foreach ( DictionaryEntry de in dictionary ) {
                    Trace.WriteLine(de.Key + "\t" + de.Value);
                }
            }
        }

        private static void AsyncExecutor(object payload) { 
            object[] p = (object[])payload;
            IExecutive executive = (IExecutive)p[0];
            ExecEvent execEvent = (ExecEvent)p[1];
            execEvent.ExecEventReceiver(executive, execEvent.UserData);
        }
    }

    /// <summary>
    /// MissingParameterException is thrown when a required parameter is missing. Typically used in a late bound, read-from-name/value pair collection scenario.
    /// </summary>
    [Serializable]
    public class RuntimeException : Exception {

        #region protected ctors
        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected RuntimeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #endregion

        #region public ctors
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class.
        /// </summary>
        public RuntimeException() { }
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public RuntimeException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
   }



    /// <summary>
    /// A CausalityException is raised if the executive encounters a request to fire an event at a time earlier than the
    /// current time whose events are being served.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class CausalityException : Exception {
        public CausalityException(string message):base(message){}
    }

    /// <summary>
    /// Delegate DetachableEventAbortHandler is the signature implemented by a method intended to respond to the aborting of a detachable event.
    /// </summary>
    /// <param name="exec">The executive whose detachable event is being aborted.</param>
    /// <param name="idec">The detachable event controller.</param>
    /// <param name="args">The arguments that were to have been provided to the ExecEventReceiver.</param>
    public delegate void DetachableEventAbortHandler(IExecutive exec, IDetachableEventController idec, params object[] args);
    
    internal class DetachableEvent : IDetachableEventController {

        #region >>> Private Fields <<<
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("DetachableEventController");
        private StackTrace m_suspendedStackTrace = null;
        private static int _lockNum = 0;
        private string m_lock = "Lock #" + _lockNum++;
        private Executive m_exec;
        private ExecEvent  m_currEvent;
        private bool m_abortRequested = false;
        private long m_isWaitingCount;
        private DateTime m_timeOfLastWait;
        #endregion

        private DetachableEventAbortHandler m_abortHandler;
        private object[] m_args = null;
        public void SetAbortHandler(DetachableEventAbortHandler handler, params object[] args) {
            m_abortHandler = handler;
            m_args = args;
        }

        public void ClearAbortHandler() {
            m_abortHandler = null;
            m_args = null;
        }

        public void FireAbortHandler() {
            if (m_abortHandler != null) {
                m_abortHandler(m_exec, this, m_args);
                ClearAbortHandler();
            }
        }
        
        public DetachableEvent(Executive exec, ExecEvent currentEvent){
            m_exec = exec;
            m_currEvent = currentEvent;
            m_exec.RunningDetachables.Add(this);
        }

        public void Begin(){
            Debug.Assert(!m_abortRequested,"(Re)beginning an aborted DetachableEvent");

            // This method runs in the executive's event service thread.
            lock ( m_lock ) {
                object userData = m_currEvent.m_userData;
                IAsyncResult iar = m_currEvent.Eer.BeginInvoke(m_exec,userData,new AsyncCallback(End),null);
                Interlocked.Increment(ref m_isWaitingCount);
                m_timeOfLastWait = m_exec.Now;
                Monitor.Wait(m_lock); // Keeps exec from running off and launching another event.
                Interlocked.Decrement(ref m_isWaitingCount);
                if ( m_abortRequested ) Abort();
            }
        }

        /// <summary>
        /// Suspends this detachable event until it is explicitly resumed.
        /// </summary>
        public void Suspend(){
            Debug.Assert(!m_abortRequested,"Suspending an aborted DetachableEvent");
            Debug.Assert(m_exec.CurrentEventType.Equals(ExecEventType.Detachable),m_errMsg1 + m_exec.CurrentEventType,m_errMsg1Explanation);

            // This method runs on the DE's execution thread. The only way to get the DE is to use Executive's
            // CurrentEventController property, and this DE should be used immediately, not held for later.
            Debug.Assert(Equals(m_exec.CurrentEventController),"Suspend called from someone else's thread!");
            lock ( m_lock ) {
                //Trace.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is suspending." + GetHashCode());
                if ( s_diagnostics ) m_suspendedStackTrace = new StackTrace();

                m_exec.SetCurrentEventController(null);
                Monitor.Pulse(m_lock);
                Interlocked.Increment(ref m_isWaitingCount);
                m_timeOfLastWait = m_exec.Now;
                Monitor.Wait(m_lock);
                m_exec.SetCurrentEventType(ExecEventType.Detachable); // Whatever it was, it is now a detachable.
                Interlocked.Decrement(ref m_isWaitingCount);
                if ( m_abortRequested ) DoAbort();
            }
        }

        public void SuspendUntil(DateTime when){
            // This method runs on the DE's execution thread.
            Debug.Assert(m_exec.CurrentEventType.Equals(ExecEventType.Detachable),m_errMsg1 + m_exec.CurrentEventType,m_errMsg1Explanation);
            m_exec.RequestEvent(new ExecEventReceiver(_Resume),when,0,null);
            Suspend();
        }

        public void SuspendFor(TimeSpan howLong) { SuspendUntil(m_exec.Now + howLong); }


        public void Resume(double overridePriorityLevel){
            Debug.Assert(!m_abortRequested,"Resumed an aborted DetachableEvent");
            // This method is called by someone else's thread. The DE should be suspended at this point.
            Debug.Assert(!Equals(m_exec.CurrentEventController),"Resume called from DE's own thread!");
            m_exec.AcquireEventLock();
            m_exec.RequestEvent(new ExecEventReceiver(_Resume),m_exec.Now,overridePriorityLevel,null);
            m_exec.ReleaseEventLock();
        }

        public void Resume(){
            Resume(m_exec.CurrentPriorityLevel);
        }

        public bool HasAbortHandler { get { return m_abortHandler != null; } }

        internal void Abort(){
            Debug.Assert(!m_abortRequested,"(Re)aborting an aborted DetachableEvent");
            FireAbortHandler();
            lock ( m_lock ) {
                m_abortRequested = true;
                Monitor.Pulse(m_lock);
            }
        }

        private void DoAbort(){
            Debug.Assert(!Equals(m_exec.CurrentEventController),"DoAbort called from someone else's thread!");

            lock ( m_lock ) {
                Monitor.Pulse(m_lock);
            }

            if ( Diagnostics.DiagnosticAids.Diagnostics("Executive.BreakOnThreadAbort") ) {
                Debugger.Break();
            }

            Thread.CurrentThread.Abort();

        }
        
        private void _Resume(IExecutive exec, object userData){
            // This method is always called on the Executive's event service thread.
            lock ( m_lock ) {

                //Trace.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is resuming." + GetHashCode());

                if ( s_diagnostics ) m_suspendedStackTrace = null;

                //Trace.WriteLine(DateTime.Now.Ticks + "Task Resume is Pulsing " + m_lock);Trace.Out.Flush();
                m_exec.SetCurrentEventController(this);
                Monitor.Pulse(m_lock);
                Interlocked.Increment(ref m_isWaitingCount);
                if ( m_isWaitingCount > 1 ) Monitor.Wait(m_lock);
                Interlocked.Decrement(ref m_isWaitingCount);

            }
        }

        private void End(IAsyncResult iar){

            try {
                m_exec.RunningDetachables.Remove(this);
                //Trace.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is finishing." + GetHashCode());
                lock (m_lock) {
                    m_currEvent.OnServiceCompleted();
                    Monitor.Pulse(m_lock);
                }
                //Trace.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is really finishing." + GetHashCode());
                m_currEvent.Eer.EndInvoke(iar);
            } catch (ThreadAbortException) {
                //Trace.WriteLine(tae.Message); // Must explicitly catch the ThreadAbortException or it bubbles up.
            } catch (Exception e) {
                // TODO: Report this to an Executive Errors & Warnings collection.
                Trace.WriteLine("Caught model error : " + e);
                m_exec.Stop();
            }
        }


        public StackTrace SuspendedStackTrace { 
            get {
                return m_suspendedStackTrace; 
            }
        }

        public IExecEvent RootEvent { get { return m_currEvent; } }

        public bool IsWaiting(){ return m_isWaitingCount > 0; }

        public DateTime TimeOfLastWait { get { return m_timeOfLastWait; } }

        private string m_errMsg1 = "Detachable event control being inappropriately exercised from an event of type ";
        private string m_errMsg1Explanation = "The caller is trying to suspend an event thread from a thread that was not launched as result of a detachable event.";
    }

    internal class ExecEvent : IExecEvent {
        private static bool _usePool = false;
        private static Queue<ExecEvent> _pool = new Queue<ExecEvent>();
        public static ExecEvent Get(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType eet, long key, bool isDaemon) {
            ExecEvent retval;
            if (_pool.Count == 0) {
                retval = new ExecEvent(eer, when, priority, userData, eet, key, isDaemon);
            } else {
                retval = _pool.Dequeue();
                retval.Initialize(eer, when, priority, userData, eet, key, isDaemon);
            }
            return retval;
        }

        private ExecEvent(ExecEventReceiver eer,DateTime when,double priority,object userData, ExecEventType eet, long key, bool isDaemon){
            Initialize(eer, when, priority, userData, eet, key, isDaemon);
        }

        private void Initialize(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType eet, long key, bool isDaemon) {
            Eer = eer;
            m_when = when;
            m_priority = priority;
            m_userData = userData;
            m_eventType = eet;
            m_key = key;
            IsDaemon = isDaemon;
            Ticks = when.Ticks;
            ServiceCompleted = null;
        }

        public bool IsDaemon;
        public ExecEventReceiver Eer;
        public DateTime m_when;
        public double m_priority;
        public object m_userData;
        public ExecEventType m_eventType;
        public long m_key;
        internal long Ticks;
        public override string ToString(){return "Event: Time= " + m_when + ", pri= " + m_priority + ", type= " + m_eventType + ", userData= "+m_userData;}

        #region IExecEvent Members

        public ExecEventReceiver ExecEventReceiver {
            get { return Eer; }
        }

        public DateTime When {
            get { return m_when; }
        }

        public double Priority {
            get { return m_priority; }
        }

        public object UserData {
            get { return m_userData; }
        }

        public ExecEventType EventType {
            get { return m_eventType; }
        }

        public long Key {
            get { return m_key; }
        }
        #endregion

        public void OnServiceCompleted() {
            if (ServiceCompleted != null) {
                ServiceCompleted(Key, ExecEventReceiver, Priority, When, UserData, EventType);
            }
            if (_usePool) {
                _pool.Enqueue(this);
            }
        }

        public event EventMonitor ServiceCompleted;

    }

    /// <summary>
    /// Interface IExecEvent is implemented by an internal class that keeps track of all of the key data
    /// about an event that is to be served by the Executive.
    /// </summary>
    public interface IExecEvent {
        /// <summary>
        /// Gets the ExecEventReceiver (the delegate into which the event will be served.)
        /// </summary>
        /// <value>The execute event receiver.</value>
        ExecEventReceiver ExecEventReceiver { get; }
        /// <summary>
        /// Gets the date &amp; time that the event is to be served.
        /// </summary>
        /// <value>The date &amp; time that the event is to be served.</value>
        DateTime When { get; }
        /// <summary>
        /// Gets the priority of the event.
        /// </summary>
        /// <value>The priority.</value>
        double Priority { get; }
        /// <summary>
        /// Gets the user data to be provided to the method into which the event will be served.).
        /// </summary>
        /// <value>The user data.</value>
        object UserData { get; }
        /// <summary>
        /// Gets the <see cref="ExecEventType"/> of the event.
        /// </summary>
        /// <value>The type of the event.</value>
        ExecEventType EventType { get; }
        /// <summary>
        /// Gets the key by which the event is known. This is useful when the event is being rescinded or logged.
        /// </summary>
        /// <value>The key.</value>
        long Key { get; }
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        string ToString();
    }

    internal class ExecEventComparer:IComparer {
        public int Compare(object x, object y) {
            ExecEvent ee1 = (ExecEvent)x;
            ExecEvent ee2 = (ExecEvent)y;
            /*if ( EE1.m_ticks < EE2.m_ticks) return -1;
            if ( EE1.m_ticks > EE2.m_ticks ) return  1;
            if ( EE1.m_priority < EE2.m_priority ) return  1;
            if ( EE1.m_key == EE2.m_key ) return 0;
            return -1;*/
            if ( ee1.m_when < ee2.m_when) return -1;
            if ( ee1.m_when > ee2.m_when ) return  1;
            if ( ee1.m_priority < ee2.m_priority ) return  1;
            if ( ee1.m_key == ee2.m_key ) return 0;
            return -1;
            /*
            if ( ((ExecEvent)x).m_ticks < ((ExecEvent)y).m_ticks ) return -1;
            if ( ((ExecEvent)x).m_ticks > ((ExecEvent)y).m_ticks ) return  1;
            if ( ((ExecEvent)x).m_priority < ((ExecEvent)y).m_priority ) return  1;
            if ( ((ExecEvent)x).m_key == ((ExecEvent)y).m_key ) return 0;
            return -1;
            */
        }
    }

    internal delegate void FilterMethod( ref SortedList events );
    
    internal class ExecEventRemover {
        private IExecEventSelector m_ees = null;
        private long m_eventId;
        private object m_target = null;
        private FilterMethod m_filterMethod;
        public ExecEventRemover(IExecEventSelector ees){
            m_ees = ees;
            m_filterMethod = new FilterMethod(FilterOnFullData);
        }
        public ExecEventRemover(long eventId){
            m_eventId = eventId;
            m_filterMethod = new FilterMethod(FilterOnEventId);
        }

        public ExecEventRemover(Delegate target){
            m_target = target;
            m_filterMethod = new FilterMethod(FilterOnDelegateAll);
        }

        public ExecEventRemover(object target){
            m_target = target;
            m_filterMethod = new FilterMethod(FilterOnTargetAll);
        }

        public void Filter( ref SortedList events ) { m_filterMethod(ref events); }

        private void FilterOnFullData( ref SortedList events ) {

            IList keyList = events.GetKeyList();
                ExecEvent ee;
            for( int i = keyList.Count-1 ; i >= 0 ;  i-- ) {
                ee = (ExecEvent)keyList[i];
                if ( m_ees.SelectThisEvent(ee.Eer,ee.m_when,ee.m_priority,ee.m_userData,ee.m_eventType ) ) {
                    events.RemoveAt(i);
                }
            }
        }

        private void FilterOnEventId(ref SortedList events ) {
            if ( events.ContainsValue(m_eventId) ) {
                // Need to remove the entry that has the value of m_eventID.
                events.RemoveAt(events.IndexOfValue(m_eventId));
            } else {
                throw new ApplicationException("Attempted to remove an event from the executive by its event ID (" + m_eventId + "), where that event ID was not in the event list.");
            } 
        }

        private void FilterOnTarget(ref SortedList events ) {

            object eventTarget = null;
            foreach ( ExecEvent ee in events.Keys ) {
                
                if ( ee.Eer.Target is DetachableEvent ) {
                    ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.Eer.Target).RootEvent).Eer;
                    eventTarget = eer.Target;
                } else {
                    eventTarget = ee.Eer.Target;
                }
                    
                // We're comparing at the object level - we can't compare any higher, since we
                // have no control over what kinds of objects we may be comparing. To avoid an
                // invalid cast exception, we treat them both as objects.
                if ( Equals(eventTarget,m_target) ) {
                    //Trace.WriteLine("Sure would like to remove " + ee.ToString());
                    int indexOfKey = events.IndexOfKey(ee);
                    events.RemoveAt(indexOfKey);
                    break;
                }
            }
        }

        private void FilterOnDelegate(ref SortedList events ) {

            object eventTarget = null;
            foreach ( ExecEvent ee in events.Keys ) {

                if ( ee.Eer.Target is DetachableEvent ) {
                    ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.Eer.Target).RootEvent).Eer;
                    eventTarget = eer;
                } else {
                    eventTarget = ee.Eer;
                }
                    
                if ( ((Delegate)eventTarget).Equals((Delegate)m_target) ) {
                    //Trace.WriteLine("Sure would like to remove " + ee.ToString());
                    int indexOfKey = events.IndexOfKey(ee);
                    events.RemoveAt(indexOfKey);
                    break;
                }
            }
        }

        private void FilterOnTargetAll(ref SortedList events ) {

            ArrayList eventsToDelete = new ArrayList();																// AEL
            Type soughtTargetType = m_target.GetType();
            Type eventTargetType = null;

            IList keyList = events.GetKeyList();
            for( int i = keyList.Count-1 ; i >= 0 ;  i-- ) {
                ExecEvent ee = (ExecEvent)keyList[i];
                
                if ( ee.Eer.Target is DetachableEvent ) {
                    ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.Eer.Target).RootEvent).Eer;
                    eventTargetType = eer.Target.GetType();
                } else {
                    // The callback could be static, so if it is, then we need the targetType a different way.
                    eventTargetType = ee.Eer.Target == null ? ee.Eer.Method.ReflectedType : ee.Eer.Target.GetType();
                }

                // We're comparing at the object level - we can't compare any higher, since we
                // have no control over what kinds of objects we may be comparing. To avoid an
                // invalid cast exception, we treat them both as objects.
                //if ( object.Equals(eventTarget,m_target) ) {
                if (eventTargetType.Equals(soughtTargetType)) {
                    events.RemoveAt(i);
                }
            }
        }

        private void FilterOnDelegateAll(ref SortedList events ) {

            object eventTarget = null;
            ExecEvent ee;
            DetachableEvent de;
            IList keyList = events.GetKeyList();
            for( int i = keyList.Count-1 ; i >= 0 ;  i-- ) {
                ee = (ExecEvent)keyList[i];
                de = ee.Eer.Target as DetachableEvent;
                if (de != null) {
                    eventTarget = de.RootEvent.ExecEventReceiver.Target;
                } else {
                    eventTarget = ee.Eer;
                }
                    
                if ( ((Delegate)eventTarget).Equals((Delegate)m_target) ) {
                    events.RemoveAt(i);
                }
            }
        }
    }
}
