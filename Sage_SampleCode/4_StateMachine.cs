/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Text;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Demo.StateMachine
{

    namespace Basic
    {

        public static class Default
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(@"This demonstration shows the default configuration of
            a model state machine. It's pretty simple - the model is either idle, or running. If it's idle, calling ""Start()""
            transitions it to running, in which state the executive processes all of its events. After running, it returns to idle")]
            public static void Run()
            {
                Highpoint.Sage.SimCore.Model m = new Highpoint.Sage.SimCore.Model("Demo Model");

                Highpoint.Sage.SimCore.StateMachine sm = m.StateMachine;
                sm.TransitionCompletedSuccessfully +=
                    (model, data) =>
                        Console.WriteLine("Model transitioned successfully to the {0} state.", m.StateMachine.State);

                string[] stateNames = Enum.GetNames(sm.State.GetType());
                Console.WriteLine("State machine's default states are {0}", StringOperations.ToCommasAndAndedList(stateNames));
                m.Executive.RequestEvent(
                    (exec, data) => Console.WriteLine("Model state is {0}.", m.StateMachine.State),
                    DateTime.Parse("Fri, 15 Jul 2016 00:00:00"));
                Console.WriteLine("Before starting, model state is {0}.", m.StateMachine.State);
                m.Start();
                Console.WriteLine("After completion, model state is {0}.", m.StateMachine.State);
            }
        }

        public static class SimpleCustomWithInitialization
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(@"This demonstration shows a simple custom
            configuration of a model state machine. We will add a state, ""Initialize"" that, upon invocation of
            the model's ""Start()"" method, performs setup.")]
            public static void Run()
            {
                Highpoint.Sage.SimCore.Model m = new DemoModel("Demo Model");

                Highpoint.Sage.SimCore.StateMachine sm = m.StateMachine;
                sm.TransitionCompletedSuccessfully +=
                    (model, data) =>
                        Console.WriteLine("Model transitioned successfully to the {0} state.", m.StateMachine.State);

                string[] stateNames = Enum.GetNames(sm.State.GetType());
                Console.WriteLine("State machine's default states are {0}", StringOperations.ToCommasAndAndedList(stateNames));
                m.Executive.RequestEvent(
                    (exec, data) => Console.WriteLine("Model state is {0}.", m.StateMachine.State),
                    DateTime.Parse("Fri, 15 Jul 2016 00:00:00"));
                Console.WriteLine("Before starting, model state is {0}.", m.StateMachine.State);
                m.Start();
                Console.WriteLine("After completion, model state is {0}.", m.StateMachine.State);
            }
        }

        public static class SimpleEnumStateMachine
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(
                @"This demo shows the utility of a different kind of state machine. It
is not usable to control model state, but is demonstrated here to
hopefully avoid confusion as to its use. It manages the state of Model
Objects (such as agents) and tracks the time each agent spends in each
state. Again, this conceptually-different state machine is demonstrated
here to try to avoid confusion.")]
            public static void Run()
            {
                // Create the model.
                Highpoint.Sage.SimCore.Model m = new Highpoint.Sage.SimCore.Model("Model");

                // Add the model objects to it.
                List<StateAwareDemoObject> tools = new List<StateAwareDemoObject>();
                foreach (string name in new[] { "Drill", "Welder", "Rinse" })
                {
                    StateAwareDemoObject tool = new StateAwareDemoObject(m, name, 10 /* run ten jobs. */);
                    tools.Add(tool);
                    m.AddModelObject(tool);
                }

                m.Start();

                tools.ForEach(o => Console.WriteLine(o.StateReport));
            }
        }

        enum SADO_States {  Idle, Running }

        internal class StateAwareDemoObject : BaseModelObject
        {
            private readonly int m_howManyCycles;
            private int m_jobNum;
            private readonly EnumStateMachine<SADO_States> m_state;
            private readonly IRandomChannel m_rc;

            public StateAwareDemoObject(IModel model, string name, int howManyCycles)
                : base(model, name, System.Guid.NewGuid())
            {
                m_howManyCycles = howManyCycles;
                m_state = new EnumStateMachine<SADO_States>(model.Executive, SADO_States.Idle, true);
                m_rc = model.RandomServer.GetRandomChannel();
                model.Starting += WaitAndRun;
            }

            private void WaitAndRun(IModel theModel)
            {
                IExecutive exec = theModel.Executive;
                DateTime whenToStart = exec.Now + TimeSpan.FromHours(m_rc.NextDouble()*2);
                TimeSpan howLongToRun = TimeSpan.FromHours(m_rc.NextDouble()*4);
                exec.RequestEvent((executive, data) => StartRunning(howLongToRun), whenToStart);
            }

            private void StartRunning(TimeSpan forHowLong)
            {
                Console.WriteLine("{0} : {1} starting job {2}.", Model.Executive.Now, Name, m_jobNum);
                if (m_state.CurrentState == SADO_States.Idle)
                {
                    m_state.ToState(SADO_States.Running);
                    DateTime runUntil = Model.Executive.Now + forHowLong;
                    Model.Executive.RequestEvent(FinishRunning, runUntil, null);
                }
            }

            private void FinishRunning(IExecutive exec, object userdata)
            {
                Console.WriteLine("{0} : {1} finishing job {2}.", Model.Executive.Now, Name, m_jobNum);
                m_state.ToState(SADO_States.Idle);
                if (++m_jobNum < m_howManyCycles) WaitAndRun(Model);
            }

            public SADO_States State => m_state.CurrentState;

            public string StateReport
            {
                get
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0} spent {1} idle and {2} running.\r\n",
                        Name,
                        m_state.StateTimes[SADO_States.Idle],
                        m_state.StateTimes[SADO_States.Running]);

                    foreach (var transitionRecord in m_state.Transitions)
                    {
                        sb.AppendFormat("{0} : {1} transitioned from {2} to {3}.\r\n",
                            transitionRecord.When,
                            Name,
                            transitionRecord.From,
                            transitionRecord.To);
                    }

                    return sb.ToString();
                }
            }
        }

        class DemoModel : Highpoint.Sage.SimCore.Model
        {
            public DemoModel(string name) : base(name)
            {
                AddService(new InitializationManager(States.Initialized));
                GetService<InitializationManager>().AddInitializationTask((model, parameters) => Console.WriteLine("Performing some initialization task..."));
            }

            #region State Machine Configuration

            /// <summary>
            /// Enum 'States' is the new (custom) structure of the state machine that will drive the model.
            /// </summary>
            enum States
            {
                /// <summary>
                /// The model is in the 'idle' state when it is ready to be initialized and then run.
                /// </summary>
                Idle,
                /// <summary>
                /// The model is in the 'initialized' state when it has been initialized and is about to run.
                /// </summary>
                Initialized,
                /// <summary>
                /// The model is in the 'running' state while it is running. When it completes, it transitions
                /// back to the 'idle' state automatically.
                /// </summary>
                Running
            }
            protected override Highpoint.Sage.SimCore.StateMachine CreateStateMachine()
            {
                bool[,] transitionMatrix = //        To
                    new bool[,] { { //        IDL    INI    RUN    
                                   /* IDL */  false, true,  false },{ // Idle can only transition to initializing.
                       /* From */  /* INI */  false, false, true },{ // Initializing can only transition to Running.
                                   /* RUN */  true,  false, false } };// Running can only transition to Idle.

                // In the follow-on-states array, if a state has itself as a follow-on-state, it is a terminal
                // state, and requires an overt action to leave that state (Such as calling "Run" to leave "Idle.")
                Enum[] followOnStates = new Enum[] { States.Idle, States.Running, States.Idle };

                Enum initialState = States.Idle;

                return new Highpoint.Sage.SimCore.StateMachine(this, transitionMatrix, followOnStates, initialState);
            }

            // This is the state we wish to transition to, if the model is to be started.
            public override Enum GetStartEnum() => States.Initialized;

            // This is the state we wish to transition to, if the model is to be aborted.
            public override Enum GetAbortEnum() => States.Idle;

            // This is the state we wish to transition to, if the model is to be idle.
            public override Enum GetIdleEnum() => States.Idle;
            #endregion

        }

    }
}