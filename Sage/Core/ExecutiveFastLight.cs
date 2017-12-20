/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

using NameValueCollection=System.Collections.Specialized.NameValueCollection;

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// This is a very fast executive, designed for applications in which raw speed is the greatest and most
    /// overriding concern. This executive accomplishes this goal without rescindable or detachable events,
    /// support for pause and resume, event priority sorting within the same timeslice, or detection of
    /// causality violations. Use the Executive class if these features are important and you are willing to
    /// sacrifice a little bit of speed to get them. Note that if you are doing anything non-trivial in your
    /// event handlers, this 'sacrifice' quickly becomes unnoticeable.
    /// </summary>
    internal sealed class ExecutiveFastLight : IExecutive {

        static ExecutiveFastLight() {
#if LICENSING_ENABLED
            if (!Licensing.LicenseManager.Check()) {
                System.Windows.Forms.MessageBox.Show("Sage� Simulation and Modeling Library license is invalid.","Licensing Error");
            }
#endif
        }

        ///// <summary>
        ///// Beeps for the specified duration at the specified frequency.
        ///// </summary>
        ///// <param name="freq">The frequency in hertz.</param>
        ///// <param name="duration">The duration in milliseconds.</param>
        ///// <returns></returns>
        //[ System.Runtime.InteropServices.DllImport("kernel32.dll")]
        //public static extern bool Beep(int freq,int duration);

		private class ExecEventCache {
			private int m_head = 0;
			private int m_tail = 0;
			private int m_numExecEvents;
			private _ExecEvent[] m_execEventCache;

			public ExecEventCache():this(InitialSize){}
			public ExecEventCache(int initialEventCacheSize){
				m_numExecEvents = initialEventCacheSize;
				 m_execEventCache = new _ExecEvent[m_numExecEvents];
				for ( int i = 0 ; i < initialEventCacheSize ; i++ ) {
					m_execEventCache[i] = new _ExecEvent();
				}
			}

			public _ExecEvent Take(ExecEventReceiver eer, DateTime when, object userData, long key, bool isDaemon ){
				if ( m_head == m_tail ) {
					// Queue is empty!
					m_tail = m_numExecEvents;
					m_head = 0;
					m_numExecEvents*=2;
					m_execEventCache = new _ExecEvent[m_numExecEvents];
					for ( int i = m_head ; i < m_tail ; i++ ) {
						m_execEventCache[i] = new _ExecEvent();
					}
				}
				_ExecEvent retval = m_execEventCache[m_head++];
				if ( m_head == m_numExecEvents ) m_head = 0;

				retval.Eer = eer;
				retval.Key = key;
				retval.UserData = userData;
				retval.When = when.Ticks;
				retval.IsDaemon = isDaemon;

				return retval;
			}

			public void Return(_ExecEvent execEvent){
				m_execEventCache[m_tail++] = execEvent;

				// Comment out for better performance.
				execEvent.Key = -1;
				execEvent.UserData = null;
				execEvent.Eer = null;
				execEvent.When = 0L;

				if ( m_tail == m_numExecEvents ) m_tail = 0;
			}
		}
		
		private class _ExecEvent {
			public ExecEventReceiver Eer;
			public long When;
			public object UserData;
			public long Key;
			public bool IsDaemon;
			public _ExecEvent(){
				Eer = null;
				When = 0L;
				UserData = null;
				Key = 0L;
				IsDaemon = false;
			}

			public _ExecEvent(ExecEventReceiver eer, DateTime when, object userData, long key, bool isDaemon){
				Eer = eer;
				When = when.Ticks;
				UserData = userData;
				Key = key;
				IsDaemon = isDaemon;
			}

            public _ExecEvent(_ExecEvent anEvent)
            {
                Eer = anEvent.Eer;
                WhenToServe = anEvent.WhenToServe;
                WhenSubmitted = anEvent.WhenSubmitted;
                UserData = anEvent.UserData;
                Key = anEvent.Key;
                IsDaemon = anEvent.IsDaemon;
                Ignore = anEvent.Ignore;
            }

            public override bool Equals(object obj)
		    {
		        return GetHashCode().Equals(obj?.GetHashCode());
		    }
		}

        /// <summary>
        /// The initial size of the event heap.
        /// </summary>
		public static int InitialSize = 16;

#region Private Fields
        private ExecEventCache m_execEventCache;
        private Guid m_execGuid;

        private _ExecEvent[] m_eventArray;
        private int m_eventArraySize;
        private int m_numEventsPending;
        private int m_numNonDaemonEventsPending;

        private DateTime? m_lastEventServiceTime = null;
        private DateTime m_now;
        private ExecState m_execState;
        private int m_runNumber;
        private UInt32 m_eventCount;
        private bool m_stopRequested;
        private long m_key;
        private _ExecEvent m_currentEvent;
        private static ArrayList _emptyList = ArrayList.ReadOnly(new ArrayList());
        private static bool _ignoreCausalityViolations = true;

		private _ExecEvent m_parentEvent;

#endregion Private Fields

        /// <summary>
        /// Creates a new instance of the <see cref="T:Executive3"/> class.
        /// </summary>
        /// <param name="execGuid">The GUID by which this executive will be known.</param>
        public ExecutiveFastLight(Guid execGuid) {
            NameValueCollection nvc = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("Sage");
            if (nvc != null)
            {
                if (nvc["IgnoreCausalityViolations"] != null)
                    _ignoreCausalityViolations = bool.Parse(nvc["IgnoreCausalityViolations"]);


                nvc = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("diagnostics");
                string strEba = nvc["ExecBreakAt"];
                if (!m_hasTarget && strEba != null && strEba.Length > 0)
                {
                    _targetdatestr = strEba;
                    m_targetdate = DateTime.Parse(_targetdatestr);
                    m_hasTarget = true;
                }
            }
            else
            {
                Console.WriteLine("No Sage initialization section found in app.config.");
            }

            m_execGuid = execGuid;
            m_runNumber = 0;
            Reset();
        }
		
#region IExecutive Members

        /// <summary>
        /// The Guid by which this executive is known.
        /// </summary>
        /// <value></value>
		public Guid Guid {
			get {
				return m_execGuid;
			}
		}

        /// <summary>
        /// The current DateTime being managed by this executive. This is the 'Now' point of a
        /// simulation being run by this executive.
        /// </summary>
        /// <value></value>
		public DateTime Now {
			get {
				return m_now;
			}
		}

        /// <summary>
        /// If this executive has been run, this holds the DateTime of the last event serviced. May be from a previous run.
        /// </summary>
        public DateTime? LastEventServed { get { return m_lastEventServiceTime; } }

        /// <summary>
        /// The priority of the event currently being serviced. This executive forces all priorities to zero.
        /// </summary>
        /// <value></value>
		public double CurrentPriorityLevel {
			get {
				return 0.0;
			}
		}

        /// <summary>
        /// The current <see cref="Highpoint.Sage.SimCore.ExecState"/> of this executive (running, stopped, paused, finished)...
        /// </summary>
        /// <value></value>
		public ExecState State {
			get {
				return m_execState;
			}
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
		public long RequestDaemonEvent(ExecEventReceiver eer, DateTime when, double priority, object userData){
			if ( when < m_now ) {
				if ( !_ignoreCausalityViolations ) {
					string who = eer.Target.GetType().FullName;
					if ( eer.Target is IHasName ) who = ((IHasName)eer.Target).Name;
					string method = eer.Method.Name + "(...)";
					string msg = string.Format("Executive was asked to service an event prior to current time. This is a causality violation. The call was made from {0}.{1}.",who,method);
					//throw new ApplicationException(msg);
					Console.WriteLine(msg);
				} else {
					when = m_now;
				}
			}
			m_key++;

			Enqueue(new _ExecEvent(eer,when,userData,m_key,true));
			//Enqueue(m_execEventCache.Take(eer,when,userData,m_key));
			return m_key;
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
        public long RequestEvent(ExecEventReceiver eer, DateTime when) {
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
        public long RequestEvent(ExecEventReceiver eer, DateTime when, object userData) {
            return RequestEvent(eer, when, 0.0, userData);
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at the current executive time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <param name="eet">The EventType that declares how the event is to be served by the executive.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        public long RequestImmediateEvent(ExecEventReceiver eer, object userData, ExecEventType eet) {
            throw new NotSupportedException("The selected executive type does not support contemporaneous enqueueing.");
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
		public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData) {
			if ( when < m_now ) {
				if ( !_ignoreCausalityViolations ) {
					string who = eer.Target.GetType().FullName;
					if ( eer.Target is IHasName ) who = ((IHasName)eer.Target).Name;
					string method = eer.Method.Name + "(...)";
					string msg = string.Format("Executive was asked to service an event prior to current time. This is a causality violation. The call was made from {0}.{1}.",who,method);
					//throw new ApplicationException(msg);
					Console.WriteLine(msg);
				} else {
					when = m_now;
				}
			}
			m_key++;
			Enqueue(new _ExecEvent(eer,when,userData,m_key,false));
			//Enqueue(m_execEventCache.Take(eer,when,userData,m_key));
			return m_key;
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
		long IExecutive.RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType) {
			//if ( !execEventType.Equals(ExecEventType.Synchronous) ) throw new ApplicationException("This high performance exec can currently only handler synchronous events.");
			return RequestEvent(eer,when,priority,userData);
		}

#region Unrequest Events - not supported
        /// <summary>
        /// This executive does not support unrequesting already-submitted event requests.
        /// </summary>
        /// <param name="eventHashCode">The code that identifies the event request to be removed.</param>
		public void UnRequestEvent(long eventHashCode) {
			throw new ApplicationException("This high performance exec does not support unrequesting events.");
		}

        /// <summary>
        /// This executive does not support unrequesting already-submitted event requests.
        /// </summary>
        /// <param name="ees">An object that will be used to select the events to remove.</param>
		public void UnRequestEvents(IExecEventSelector ees) {
			throw new ApplicationException("This high performance exec does not support unrequesting events.");
		}

        /// <summary>
        /// This executive does not support unrequesting already-submitted event requests.
        /// </summary>
        /// <param name="execEventReceiverTarget">The callback target for which all queued events are to be removed.</param>
		void IExecutive.UnRequestEvents(object execEventReceiverTarget) {
			throw new ApplicationException("This high performance exec does not support unrequesting events.");
		}

        /// <summary>
        /// This executive does not support unrequesting already-submitted event requests.
        /// </summary>
        /// <param name="execEventReceiverMethod">The callback method for which all queued events are to be removed.</param>
		void IExecutive.UnRequestEvents(Delegate execEventReceiverMethod) {
			throw new ApplicationException("This high performance exec does not support unrequesting events.");
		}

        /// <summary>
        /// This high performance exec does not support Joining on events.
        /// </summary>
        /// <param name="eventCodes">The event codes.</param>
        public void Join(params long[] eventCodes) {
            throw new ApplicationException("This high performance exec does not support Joining on events.");
        }

#endregion

        private DateTime m_startTime = DateTime.MinValue;
        public void SetStartTime(DateTime startTime) {
            m_startTime = startTime;
        }

        /// <summary>
        /// Starts the executive. 
        /// </summary>
        public void Start() {
            lock (this) {
                m_now = m_startTime;
                if (m_executiveStarted != null)
                    m_executiveStarted(this);

                if (ExecutiveStartedSingleShot != null) {
                    ExecutiveStartedSingleShot(this);
                    ExecutiveStartedSingleShot = (ExecutiveEvent)Delegate.RemoveAll(ExecutiveStartedSingleShot, ExecutiveStartedSingleShot);
                }

                if (_ignoreCausalityViolations)
                    StartWocv();
                else
                    StartWcv();
                if (m_stopRequested) {
                    if (m_executiveStopped != null)
                        m_executiveStopped(this);
                    m_stopRequested = false;
                }

                if (m_executiveFinished != null)
                    m_executiveFinished(this);
            }
        }

        private void StartWcv()
        {
            throw new NotImplementedException();
            m_runNumber++;
            while (m_numNonDaemonEventsPending > 0 && !m_stopRequested)
            {
                m_currentEvent = Dequeue();
                m_eventCount++;
                if (m_now.Ticks > m_currentEvent.WhenToServe)
                {
                    string who = m_currentEvent.Eer.Target.GetType().FullName;
                    if (m_currentEvent.Eer.Target is IHasName)
                    {
                        who = ((IHasName)m_currentEvent.Eer.Target).Name;
                    }
                    string method = m_currentEvent.Eer.Method.Name + "(...)";
                    if (true)
                    {
                        m_currentEvent.WhenToServe = m_now.Ticks;// System.Diagnostics.Debugger.Break();
                    }
                    else
                    {
                        //						throw new ApplicationException(msg);
                    }
                }
                m_lastEventServiceTime = m_now;
                m_now = new DateTime(m_currentEvent.WhenToServe);
                m_eventAboutToFire?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now, m_currentEvent.UserData, ExecEventType.Synchronous);
                m_currentEvent.Eer(this, m_currentEvent.UserData);
                m_eventHasCompleted?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now, m_currentEvent.UserData, ExecEventType.Synchronous);
                m_execEventCache.Return(m_currentEvent);
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

        private void StartWocv() {
			m_runNumber++;
            while (m_numNonDaemonEventsPending > 0 && !m_stopRequested)
            {
                lock (m_rollbackLock)
                {
                    m_currentEvent = Dequeue();
                    m_eventCount++;
                    m_now = new DateTime(m_currentEvent.WhenToServe);

#region TEMPORAL DEBUGGING

                if (m_hasTarget && (m_now.ToString().Equals(_targetdatestr) || (!m_hasFired && m_now > m_targetdate))) {
                    m_hasFired = true;
                    m_hoverHere = m_now.ToString();
                    System.Diagnostics.Debugger.Break();
                }

#endregion TEMPORAL DEBUGGING

                    m_eventAboutToFire?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now,
                        m_currentEvent.UserData, ExecEventType.Synchronous);
                    if (m_supportRollback)
                    {
                        if (!m_currentEvent.Ignore)
                        {
                            m_currentEvent.Eer(this, m_currentEvent.UserData);
                            if (m_rollbackList.Contains(m_currentEvent)) System.Diagnostics.Debugger.Break(); // GOOBER
                            m_rollbackList.Add(new _ExecEvent(m_currentEvent));
                            if (m_rollbackList.Contains(m_currentEvent)) System.Diagnostics.Debugger.Break(); // GOOBER
                        }
                    }
                    else
                    {
                        m_currentEvent.Eer(this, m_currentEvent.UserData);
                    }
                    m_eventHasCompleted?.Invoke(m_currentEvent.Key, m_currentEvent.Eer, 0.0, m_now,
                        m_currentEvent.UserData, ExecEventType.Synchronous);
                    if (m_supportRollback && m_rollbackList.Contains(m_currentEvent)) System.Diagnostics.Debugger.Break(); // GOOBER
                    m_execEventCache.Return(m_currentEvent);
                }
                Monitor.Pulse(m_rollbackLock); // This is the ONLY place we want the local thread to be when a rollback is being processed.
            }
        }

        /// <summary>
        /// Stops the executive. This may be a pause or a stop, depending on if events are queued or running at the time of call.
        /// </summary>
		public void Stop() {
			m_stopRequested = true;
		}

        /// <summary>
        /// If running, pauses the executive and transitions its state to 'Paused'.
        /// </summary>
        public void Pause() { throw new NotSupportedException("The selected executive type does not support pause, resume or abort."); }
        /// <summary>
        /// If running, pauses the executive and transitions its state to 'Paused'.
        /// </summary>
        public void Abort() { throw new NotSupportedException("The selected executive type does not support pause, resume or abort."); }
        /// <summary>
        /// If paused, unpauses the executive and transitions its state to 'Running'.
        /// </summary>
        public void Resume() { throw new NotSupportedException("The selected executive type does not support pause, resume or abort."); }


        /// <summary>
        /// Resets the executive - this clears the event list and resets now to 1/1/01, 12:00 AM
        /// </summary>
		public void Reset() {
			m_eventArray = new _ExecEvent[InitialSize+1];
			m_execEventCache = new ExecEventCache(InitialSize+1);

			m_eventArraySize = InitialSize;
			m_numEventsPending = 0;
			m_numNonDaemonEventsPending = 0;
			m_now = DateTime.MinValue;
			m_execState = ExecState.Stopped;
			m_eventCount = 0;
			m_stopRequested = false;
			m_key = 0;

		}

        public void Detach(object target) {
            throw new NotSupportedException("The selected executive type does not support automated object detachment.");
        }

        /// <summary>
        /// This high performance exec does not support volatiles.
        /// </summary>
        /// <param name="dictionary">The task graph context to be 'reset'.</param>
		public void ClearVolatiles(IDictionary dictionary) {
			throw new ApplicationException("This high performance exec does not support volatiles.");
		}

        /// <summary>
        /// This high performance exec does not support detached events.
        /// </summary>
        /// <value></value>
		public IDetachableEventController CurrentEventController {
			get {
				throw new ApplicationException("This high performance exec does not support detached events.");
			}
		}

        /// <summary>
        /// The type of event currently being serviced by the executive. This executive services only Synchronous events.
        /// </summary>
        /// <value></value>
		public ExecEventType CurrentEventType {
			get {
				return ExecEventType.Synchronous;
			}
		}

        /// <summary>
        /// Returns a list of the detachable events that are currently running. As this high performance exec does not support detached events, this list will always be empty.
        /// </summary>
        /// <value></value>
		public ArrayList LiveDetachableEvents {
			get {
				return _emptyList;
			}
		}


        /// <summary>
        /// Returns a read-only list of the ExecEvents currently in queue for execution.
        /// Cast the elements in the list to IExecEvent to access the items' field values.
        /// </summary>
        /// <value></value>
		public IList EventList {
			get {
				return ArrayList.ReadOnly(new ArrayList(m_eventArray));
			}
		}

        /// <summary>
        /// The integer count of the number of times this executive has been run.
        /// </summary>
        /// <value></value>
		public int RunNumber {
			get {
				return m_runNumber;
			}
		}

        /// <summary>
        /// The number of events that have been serviced on this run.
        /// </summary>
        /// <value></value>
		public UInt32 EventCount {
			get {
				return m_eventCount;
			}
        }

#region Events

        // If a lock is held on the Executive, then any attempt to add a handler to a public event is
        // blocked until that lock is released. This can cause client code to freeze, expecially if running
        // in a detachable event and adding a handler to an executive event. For that reason, all public
        // event members are methods with add {} and remove {} that defer to private event members. This
        // does not cause the aforementioned lockup.
        private event ExecutiveEvent m_executiveStarted;
        private event ExecutiveEvent ExecutiveStartedSingleShot;
        private event ExecutiveEvent m_executiveStopped;
        private event ExecutiveEvent m_executiveFinished;
        private event EventMonitor m_eventAboutToFire;
        private event EventMonitor m_eventHasCompleted;
        //private event ExecutiveEvent m_executiveAborted;
        //private event ExecutiveEvent m_executiveReset;
        //private event ExecutiveEvent m_clockAboutToChange;


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
            add { throw new NotImplementedException("This executive does not support Pausing and Resuming."); }
            remove { throw new NotImplementedException("This executive does not support Pausing and Resuming."); }
        }
        /// <summary>
        /// Fired when this executive resumes.
        /// </summary>
        public event ExecutiveEvent ExecutiveResumed {
            add { throw new NotImplementedException("This executive does not support Pausing and Resuming."); }
            remove { throw new NotImplementedException("This executive does not support Pausing and Resuming."); }
        }

        public event ExecutiveEvent ExecutiveStopped {
            add { m_executiveStopped += value; }
            remove { m_executiveStopped -= value; }
        }
        public event ExecutiveEvent ExecutiveFinished {
            add { m_executiveFinished += value; }
            remove { m_executiveFinished -= value; }
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
        /// Resetting is not supported by this high performance executive.
        /// </summary>
        public event ExecutiveEvent ExecutiveReset { add { throw new NotSupportedException(); } remove { throw new NotSupportedException(); } }

        /// <summary>
        /// This fires when the executive has been aborted. This high performance executive does not support being aborted. Call Stop instead.
        /// </summary>
        public event ExecutiveEvent ExecutiveAborted { add { throw new NotSupportedException(); } remove { throw new NotSupportedException(); } }
        /// <summary>
        /// Fired after service of the last event scheduled in the executive to fire at a specific time,
        /// assuming that there are more non-daemon events to fire.
        /// </summary>
        public event ExecutiveEvent ClockAboutToChange {
            add { throw new NotSupportedException(); }
            remove { throw new NotSupportedException(); }
        }

        //TODO: Reconcile Abort versus Stop. What's the difference?

#endregion

        /// <summary>
        /// This high performance exec does nothing on Dispose.
        /// </summary>
        /// <value></value>
        public void Dispose (){ }

#endregion // IExecutive members.

        private void Enqueue(_ExecEvent ee) {
			//Console.WriteLine("Enqueueing #" + ee.m_key + ", its time is " + (new DateTime(ee.m_when)).ToString());
			int ndx = 1;
			if ( m_numEventsPending == m_eventArraySize ) GrowArray();
			m_numEventsPending++;
			if ( !ee.IsDaemon ) m_numNonDaemonEventsPending++;

			ndx = m_numEventsPending;
			int parentNdx = ndx/2;
			m_parentEvent = m_eventArray[parentNdx];
			while ( parentNdx > 0 && ee.When < m_parentEvent.When ) { // HEAP PROPERTY - root is lowest.
				m_eventArray[ndx] = m_parentEvent;
				ndx = parentNdx;
				parentNdx /= 2;
				m_parentEvent = m_eventArray[parentNdx];
			}
			
			m_eventArray[ndx] = ee;
		}

		private _ExecEvent Dequeue(){
			if ( m_numEventsPending == 0 ) return null;
			_ExecEvent leastEvent = m_eventArray[1];
			_ExecEvent relocatee  = m_eventArray[m_numEventsPending];
			m_numEventsPending--;
			int ndx = 1;
			int child = 2;
			while ( child <= m_numEventsPending ) {
				if ( child < m_numEventsPending && m_eventArray[child].When > m_eventArray[child+1].When ) child++;
				// m_entryArray[child] is the (e.g. in a minTree) lesser of the two children.
				// Therefore, if m_entryArray[child] is greater than relocatee, put Relocatee
				// in at ndx, and we're done. Otherwise, swap and drill down some more.
				if ( m_eventArray[child].When > relocatee.When ) break;
				m_eventArray[ndx] = m_eventArray[child];
				ndx = child;
				child *= 2;
			}

			m_eventArray[ndx] = relocatee;

			//Console.WriteLine("Dequeueing #" + leastEvent.m_key + ", its time is " + (new DateTime(leastEvent.m_when)).ToString());
			if ( !leastEvent.IsDaemon ) m_numNonDaemonEventsPending--;

			return leastEvent;
		}

		private void GrowArray(){
			_ExecEvent[] tmp = m_eventArray;
			m_eventArraySize*=4;
			m_eventArray = new _ExecEvent[m_eventArraySize+1];
			Array.Copy(tmp,m_eventArray,m_numEventsPending+1);
			//for ( int i = m_numEventsPending+1 ; i < m_eventArraySize+1 ; i++ ) m_eventArray[i] = new _ExecEvent();
		}

		private void Dump(){
			Console.WriteLine(m_numEventsPending);
			for ( int i = 0 ; i <= m_numEventsPending ; i++ ) {
				string when = (m_eventArray[i] == null)?"<null>":(new DateTime(m_eventArray[i].When)).ToString();
				Console.WriteLine("(" + i + ") " + when);
			}
		}

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

        /// True if this executive is to support runtime rollbacks.
        public bool SupportsRollback {
            get { return m_supportRollback; }
            internal set {
                m_supportRollback = value;
                m_rollbackList = new List<_ExecEvent>();
            }
        }

        public event ExecutiveEvent ExecutivePaused;
        public event ExecutiveEvent ExecutiveResumed;
        public event ExecutiveEvent ExecutiveAborted;
        public event ExecutiveEvent ClockAboutToChange; 
#endregion

        private object m_rollbackLock = new object();
        public void Rollback(DateTime toWhen)
        {
            // We want to hold up any thread that is not the one owned by this executive, and not release it until
            // this executive is done processing the current event, so that the rollback that occurs, occurs from
            // a consistent state, and does not have any remaining event processing to do from the future timeslice
            // when the past timeslice is finally reached. So...
            lock (m_rollbackLock)
            {

                long toWhenTicks = toWhen.Ticks;
                foreach (_ExecEvent execEvent in m_eventArray)
                {
                    // Any event that is in the event list, but was submitted AFTER the rollback-to time, will be ignored.
                    if (execEvent != null && execEvent.WhenSubmitted > toWhenTicks)
                    {
                        Console.WriteLine("Setting event that was scheduled for {0} to \"Ignore.\"",
                            new DateTime(execEvent.WhenToServe));
                        execEvent.Ignore = true;
                    }
                }

                List<_ExecEvent> tmp = new List<_ExecEvent>(m_rollbackList.Count);
                int n = 0;
                // Higher the index, the later in the simulation.
                foreach (_ExecEvent execEvent in m_rollbackList)
                {
                    // Any event that is in the history list, but was submitted AFTER the rollback-to time, will be ignored.
                    if (execEvent.WhenSubmitted < toWhenTicks)
                    {
                        if (execEvent.WhenToServe > toWhenTicks)
                        {
                            Console.WriteLine("Rescheduling event that was scheduled at {0} for {1}",
                                new DateTime(execEvent.WhenSubmitted), new DateTime(execEvent.WhenToServe));

                            _ExecEvent ee = m_execEventCache.Take(execEvent.Eer, new DateTime(execEvent.WhenToServe),
                                execEvent.UserData, execEvent.Key, execEvent.IsDaemon);
                            ee.WhenSubmitted = execEvent.WhenSubmitted;
                            Enqueue(ee);
                        }
                        else
                        {
                            tmp.Add(execEvent);
                        }
                    }
                    m_rollbackList = tmp;
                }

                m_now = toWhen;


                OnRollback?.Invoke(m_now);
            }
        }

        public event TimeEvent OnRollback;
    }
}
