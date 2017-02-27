/* This source code licensed under the GNU Affero General Public License */
// ReSharper disable InconsistentNaming

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Demo.StateManagement {
    using System;
    using Highpoint.Sage.SimCore;
    using Highpoint.Sage.Randoms;

    /// <summary>
    /// Use agents (a tank, in this case) to maintain state. Recalculate
    /// only as necessary. Also note the first use of anonymous delegate.
    /// </summary>
    class InAgents {
        private static Tank _tank;

        [Description(@"This demonstration shows the maintenance of simulation state in agents, in
this case, the agent is a tank, and the state it maintains is the position of
a fill valve, expressed in fill rate, the level and capacity of the tank, and
management of an event that will notify it when it is full. This demo also
shows lazy evaluation of the Level parameter, as well as rescinding the 
TankFull event if some part of its state (such as valve position) changes.

We start with the tank 10% full and the inlet closed. After a minute, the inlet
is opened, and the tank starts to fill, registering an event for its expected 
time of being full. After a while, we close the fill valve, then a little later
we reopen it to a lesser fill rate. The tank manages its level and its events
to keep its state correct from the perspective of external actors.")]
        public static void Run() {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            _tank = new Tank(exec, 1000.0, 100.0);

            DateTime when = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");

            exec.SetStartTime(when); // Must do this, or the initial level is computed at time zero, rather than Fri, 15 Jul 2016 00:00:00.
            exec.RequestEvent(delegate { _tank.OpenFillValve(); }, when, 1.0);
            exec.RequestEvent(delegate { _tank.CloseFillValve(); }, when + TimeSpan.FromMinutes(15.0));
            exec.RequestEvent(delegate { _tank.OpenFillValve(40); }, when + TimeSpan.FromMinutes(30.0));
            for (int i = 0; i < 8; i++)
            {
                exec.RequestEvent(ReportTankLevel, when, 0.0);
                when += TimeSpan.FromMinutes(6.0);
            }

            exec.Start();
        }

        static void ReportTankLevel(IExecutive exec, object userData) {
            Console.WriteLine("{0} : Tank level is {1}.", exec.Now, _tank.Level);
        }

        class Tank {

            #region Private Fields
            private const double FULL_RATE = 45.0;
            private readonly IExecutive m_exec;
            private readonly double m_capacity;
            private double m_level;
            private double m_fillRate;
            private DateTime m_levelAt;
            private long m_fullEventID = -1;
            #endregion

            public Tank(IExecutive exec, double capacity, double initialLevel) {
                m_exec = exec;
                m_level = initialLevel;
                m_capacity = capacity;
                m_fillRate = 0.0;
                // When the executive starts, force computation of level, and last computed time.
                m_exec.ExecutiveStarted += executive =>
                {
                    CloseFillValve();
                    Console.WriteLine("{0} : Level is {1} at start of simulation.", executive.Now, Level);
                };
            }

            public void OpenFillValve(double fillRate = FULL_RATE)
            {
                SetFillRate(fillRate);
            }

            public void CloseFillValve() {
                SetFillRate(0.0);
            }

            public double Level {
                get {
                    if (Math.Abs(m_fillRate) > 1E-6) {
                        double elapsedMinutes = ((TimeSpan)(m_exec.Now - m_levelAt)).TotalMinutes;
                        m_level = m_level + (m_fillRate * elapsedMinutes);
                    }
                    m_levelAt = m_exec.Now;
                    return m_level;
                }
            }

            private void SetFillRate(double fillRate) {
                double level = Level; // Forces recalc of level.
                Console.Write("{0} : Setting tank fill rate to {1}, Level is currently {2}. ", m_exec.Now, fillRate, level);
                m_fillRate = fillRate;
                if (fillRate > 0.0)
                {
                    double minutesTilFull = ((m_capacity - m_level)/m_fillRate);
                    m_fullEventID = m_exec.RequestEvent(TankIsFull, m_exec.Now + TimeSpan.FromMinutes(minutesTilFull));
                    Console.WriteLine("Tank will be full in {0} minutes.", minutesTilFull);
                }
                else
                {
                    if (m_fullEventID != -1)
                    {
                        m_exec.UnRequestEvent(m_fullEventID); // Rescind full event.
                    }
                    Console.WriteLine("Tank level is constant.");
                }
            }

            private void TankIsFull(IExecutive exec, object userData) {
                Console.WriteLine("{0} : Tank full notification received.", m_exec.Now);
                m_fullEventID = -1;
                CloseFillValve();
            }

        }
    }

    /// <summary>
    /// Token &amp; server model.
    /// </summary>
    class InUserData {

        [Description(@"This demonstration shows the maintenance of simulation state in the UserData
object that is maintained with each event request. 100 tokens are created with
state consisting of a name and a random start time in the first 100 minutes of
the simulation, and a processing duration between 0 and 1000 minutes. The
event, when served, retrieves the name of the token and the duration of service
of the token. It then requests a future event to signify completion of the
token's processing, and passes the event registration userData that is the name
of the token. When that even is serviced, the token's processing is indicated
to have been completed.")]
        public static void Run() {

            DateTime now = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");

            // More about this one later.
            IRandomChannel rc = GlobalRandomServer.Instance.GetRandomChannel();

            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            for (int i = 0; i < 100; i++) {
                State state = new State{ Who = string.Format("Token_{0}", i) , Duration = TimeSpan.FromMinutes(1000.0 * rc.NextDouble())};
                DateTime startWhen = now + TimeSpan.FromMinutes(100.0*rc.NextDouble());
                exec.RequestEvent(StartProcessToken, startWhen, state);
            }

            exec.Start();

        }

        private static void StartProcessToken(IExecutive exec, object userData) {
            // UserData is a State object.
            State state = (State)userData;
            Console.WriteLine("{0} : Commence processing {1}.", exec.Now, state.Who);
            DateTime completeWhen = exec.Now + state.Duration;
            exec.RequestEvent(FinishProcessToken, completeWhen, state.Who);
        }

        private static void FinishProcessToken(IExecutive exec, object userData) {
            // UserData is a string, now.
            Console.WriteLine("{0} : Done processing {1}.", exec.Now, userData);
        }

        class State
        {
            public string Who;
            public TimeSpan Duration;
        }

    }

    class OnTheStackFrame {
        [Description(@"This demonstration shows how, by declaring event handlers as local anonymous
methods, the stack frame (i.e. locally declared variables) of the method that
requests the events can be used to hold values that will be of interest to all
of the handlers.")]
        public static void Run() {

            DateTime when = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");

            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            exec.RequestEvent(StartProcessToken, when, 0.0, null, ExecEventType.Detachable);

            exec.Start();

        }

        private static void StartProcessToken(IExecutive exec, object userData) {

            int n = 7; // Start off with 7 widgets.

            Console.WriteLine("{0} : Entering the host event method, n = {1}.", exec.Now, n);

            exec.RequestEvent(
                delegate { Console.WriteLine("{0} : n = {1}.", exec.Now, n++); /* Add a widget. */}, 
                exec.Now + TimeSpan.FromMinutes(5.0)
            );

            exec.RequestEvent(
                delegate { Console.WriteLine("{0} : n = {1}.", exec.Now, n++); /* Add a widget. */}, 
                exec.Now + TimeSpan.FromMinutes(10.0)
            );

            exec.RequestEvent(
                delegate { Console.WriteLine("{0} : n = {1}.", exec.Now, n++); /* Add a widget. */}, 
                exec.Now + TimeSpan.FromMinutes(15.0)
            );

            Console.WriteLine("{0} : Exiting the host event method.", exec.Now, n);
        }

    }
}