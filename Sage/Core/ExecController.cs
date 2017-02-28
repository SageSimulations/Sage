/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Threading;
using System.Collections;
using _Debug = System.Diagnostics.Debug;
// ReSharper disable UnusedParameter.Global

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// This object will govern the real-time frequency at which the Render event fires, and will also govern
    /// the simulation time that is allowed to pass between Render events. So with a frame rate of 20, there
    /// will be 20 Render events fired per second. With a scale of 2, 10^2, or 100 times that 1/20th of a
    /// second (therefore 2 seconds of simulation time) will be allowed to transpire between render events.
    /// </summary>
    public class ExecController : IDisposable {

        #region Private Fields
        private IExecutive m_executive;
        private double m_logScale;
        private double m_linearScale;
        private int m_frameRate;
        private readonly object m_userData;
        private KickoffMgr m_kickoffManager;
        private readonly ExecutiveEvent m_doThrottle;
        private TimeSpan m_maxNap;
        private DateTime m_realWorldStartTime;
        private DateTime m_simWorldStartTime;
        private Thread m_renderThread;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecController"/> class. The model is used as UserData.
        /// <br></br>Frame rate must be from zero to 25. If zero, no constraint is imposed.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="scale">The (logarithmic) run time scale.</param>
        /// <param name="frameRate">The frame rate in render events per second. If zero, execution is unconstrained.</param>
        public ExecController(IModel model, double scale, int frameRate) : this(model.Executive, scale, frameRate, model)  {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecController"/> class. The caller may specify userData.
        /// <br></br>Frame rate must be from zero to 25. If zero, no contraint is imposed.
        /// </summary>
        /// <param name="exec">The executive being controlled.</param>
        /// <param name="scale">The (logarithmic) run time scale. If set to double.MinValue, the model runs at full speed.</param>
        /// <param name="frameRate">The frame rate in render events per second. If zero, execution is unconstrained.</param>
        /// <param name="userData">The user data.</param>
        public ExecController(IExecutive exec, double scale, int frameRate, object userData)
        {
            if (!Disable)
            {
                m_userData = userData;
                m_executive = exec;
                m_executive.ExecutiveStarted += m_executive_ExecutiveStarted;
                Scale = scale;
                FrameRate = frameRate;
                m_kickoffManager = new KickoffMgr(this, m_executive);
                m_doThrottle = ThrottleExecution;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecController"/> class. The caller may specify userData.
        /// <br></br>Frame rate must be from zero to 25. If zero, no contraint is imposed. For this constructor,
        /// the executive will be set while the model sets its ExecController.
        /// </summary>
        /// <param name="scale">The (logarithmic) run time scale. If set to double.MinValue, the model runs at full speed.</param>
        /// <param name="frameRate">The frame rate in render events per second. If zero, execution is unconstrained.</param>
        /// <param name="userData">The user data.</param>
        public ExecController(double scale, int frameRate, object userData = null)
        {
            if (!Disable)
            {
                m_userData = userData;
                Scale = scale;
                FrameRate = frameRate;
                m_doThrottle = ThrottleExecution;
            }
        }

        /// <summary>
        /// Sets the executive on which this controller will operate. This API should only be called once. The
        /// ExecController cannot be targeted to control a different executive.
        /// </summary>
        /// <param name="exec">The executive on which this controller will operate.</param>
        public void SetExecutive(IExecutive exec)
        {
            if (m_executive == exec) return;
            if ( m_executive != null ) throw new InvalidOperationException("Calling SetExecutive on an ExecController that's already attached to a different executive is illegal.");
            m_executive = exec;
            m_executive.ExecutiveStarted += m_executive_ExecutiveStarted;
            m_kickoffManager = new KickoffMgr(this, m_executive);
        }

        public bool Disable { get; set; }

        /// <summary>
        /// Gets or sets the logarithmic scale of run speed to sim speed. For example, for a sim that runs 
        /// 100 x faster than a real-world clock, use a scale of 2.0.
        /// </summary>
        /// <value>The scale.</value>
        public double Scale {
            get {
                return m_logScale;
            }
            set {
                if (Math.Abs(value - double.MinValue) < double.Epsilon)
                {
                    m_linearScale = 0;
                    m_logScale = double.MinValue;
                    FrameRate = 0;
                }
                else
                {
                    m_linearScale = Math.Pow(10.0, value);
                    m_logScale = value;
                    m_maxNap = FrameRate > 0 ? TimeSpan.FromSeconds(1.0/FrameRate) : TimeSpan.MaxValue;
                }
            }
        }

        /// <summary>
        /// Gets or sets the linear scale of run speed to sim speed. For example, for a sim that runs 
        /// 100 x faster than a real-world clock, the linear scale would be 100.
        /// </summary>
        /// <value>The scale.</value>
        public double LinearScale => m_linearScale;

        /// <summary>
        /// Gets or sets the frame rate - an integer that represents the preferred number of rendering callbacks received per second.
        /// </summary>
        /// <value>The frame rate.</value>
        public int FrameRate { 
            get { return m_frameRate; } 
            set {
                if (value > 25) {
                    throw new ArgumentException("Frame rate cannot be more than 25 frames per second.");
                }
                m_frameRate = value;
                m_maxNap = m_frameRate > 0 ? TimeSpan.FromSeconds(1.0 / m_frameRate) : TimeSpan.MaxValue;
            } 
        }

        /// <summary>
        /// A user-friendly representation of the simulation speed.
        /// </summary>
        /// <value>The rate string.</value>
        public string RateString { 
            get {
                try {
                    TimeSpan ts = TimeSpan.FromSeconds(m_logScale >= 0 ? Math.Pow(10, m_logScale) : Math.Pow(10, -m_logScale));
                    double num;
                        string units;
                        if (( num = ( ts.TotalDays / 365247.7 ) ) > 1.0) {
                            units = "millennia";
                        } else if (( num = ( ts.TotalDays / 36524.77 ) ) > 1.0) {
                            units = "centuries";
                        } else if (( num = ( ts.TotalDays / 365.2477 ) ) > 1.0) {
                            units = "years";
                        } else if (( num = ts.TotalDays ) > 1.0) {
                            units = "days";
                        } else if (( num = ts.TotalHours ) >= 1.0) {
                            units = "hours";
                        } else if (( num = ts.TotalMinutes ) >= 1.0) {
                            units = "minutes";
                        } else if (( num = ts.TotalSeconds ) >= 1.0) {
                            units = "seconds";
                        } else if (( num = ts.TotalMilliseconds ) >= 1.0) {
                            units = "milliseconds";
                        } else {
                            units = "milliseconds";
                        }

                    if ( m_logScale >= 0 ) {
                        return string.Format("Up to {0:f2} {1} of simulation time per second of user time.", num, units);
                    } else {
                        return string.Format("Up to {0:f2} {1} of user time per second of simulation time.", num, units);
                    }
                } catch {
                    return string.Format("{0}.", (m_logScale < 0 ? "Controller scale is out of range low" : "Simulation speed is unconstrained"));
                }
            } 
        }

        /// <summary>
        /// This event is expected to drive rendering at the prescribed frame rate.
        /// </summary>
        public event ExecEventReceiver Render;

        public void Dispose()
        {
            m_renderThread?.Abort();
        }

        internal void Begin(IExecutive iExecutive, object userData) {
            if ( iExecutive != m_executive ) throw new InvalidOperationException("ExecController is starting within a model whose executive is not the same one to which it was initialized.");
            m_executive.ClockAboutToChange -= m_doThrottle; // In case we were listening from an earlier run.
            m_executive.ClockAboutToChange += m_doThrottle;
            if (m_renderThread != null && m_renderThread.ThreadState == ThreadState.Running) {
                m_renderThread.Abort(); 
            }
            m_renderThread = new Thread(RunRendering)
            {
                IsBackground = true,
                Name = "Rendering Thread"
            };
            m_realWorldStartTime = DateTime.Now;
            m_simWorldStartTime = iExecutive.Now;
            m_renderThread.Start();
        }

        private void ThrottleExecution(IExecutive exec) {
            if (Math.Abs(m_linearScale) > double.Epsilon){
                IList events = m_executive.EventList;
                if (events.Count > 0)
                {
                    long realWorldElapsedTicks = DateTime.Now.Ticks - m_realWorldStartTime.Ticks;
                    DateTime timeOfNextEvent = ((IExecEvent) events[0]).When;
                    long simElapsedTicks = timeOfNextEvent.Ticks - m_simWorldStartTime.Ticks;
                    long targetRealWorldElapsedTicks = simElapsedTicks/(long) m_linearScale;

                    if (realWorldElapsedTicks < targetRealWorldElapsedTicks)
                    {
                        TimeSpan realWorldNap = Utility.TimeSpanOperations.Min(m_maxNap,
                            TimeSpan.FromTicks(targetRealWorldElapsedTicks - realWorldElapsedTicks));
                        TimeSpan simNap = TimeSpan.FromTicks((long) (realWorldNap.Ticks*m_linearScale));
                        m_executive.RequestDaemonEvent(RetardExecution, m_executive.Now + simNap,
                            0.0, realWorldNap);
                    }
                }
            }
        }

        /// <summary>
        /// Retards the executive by putting it to sleep until real time has caught up with the scale.
        /// </summary>
        /// <param name="exec"></param>
        /// <param name="userData"></param>
        private void RetardExecution(IExecutive exec, object userData) {
            if ( Math.Abs(m_linearScale) > double.Epsilon ) Thread.Sleep((TimeSpan)userData);
        }


        private bool m_renderPending;
        private void RunRendering() {
            _Debug.Assert(Thread.CurrentThread.Equals(m_renderThread));

            while (true) {
                if (m_executive.State.Equals(ExecState.Running)) {
                    int nTicksToSleep = 500; // Check to see if we've changed frame rate from zero, every half-second.
                    if (m_frameRate > 0)
                    {

                        nTicksToSleep = (int) TimeSpan.FromSeconds(1.0/m_frameRate).TotalMilliseconds;
                        Thread.Sleep(nTicksToSleep);

                        if (!m_renderPending)
                        {
                            // If there's already one pending, we skip it and wait for the next one.
                            if (m_frameRate > 0 && m_executive.State.Equals(ExecState.Running))
                            {
                                // Race condition? Yes, race condition. Could complete between this and the next.
                                m_executive.RequestImmediateEvent(DoRender, null,
                                    ExecEventType.Synchronous);
                            }
                            m_renderPending = true;
                        }
                    }
                    else
                    {
                        Thread.Sleep(nTicksToSleep);
                    }
                } else if (m_executive.State.Equals(ExecState.Paused)) {
                    Thread.Sleep(500);
                    m_realWorldStartTime = DateTime.Now;
                    m_simWorldStartTime = m_executive.Now;
                } else if (m_executive.State.Equals(ExecState.Stopped)) {
                    break;
                } else if (m_executive.State.Equals(ExecState.Finished)) {
                    break;
                }
            }
        }

        private void DoRender(IExecutive exec, object userData) {
            if (m_frameRate > 0) Render?.Invoke(exec, userData);
            m_renderPending = false;
        }

        #region (Private) Kickoff support.
        private void m_executive_ExecutiveStarted(IExecutive exec) {
            exec.EventAboutToFire += m_kickoffManager.Kickoff;
        }

        #endregion

        private class KickoffMgr {
            private readonly IExecutive m_exec;
            private readonly ExecController m_parent;
            public KickoffMgr(ExecController parent, IExecutive exec) {
                m_exec = exec;
                m_parent = parent;
            }

            public void Kickoff(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType) {
                m_exec.EventAboutToFire -= Kickoff;
                m_parent.Begin(m_parent.m_executive, m_parent.m_userData);
            }
        }
    }
}
