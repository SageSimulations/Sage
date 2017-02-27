/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Demo.Model
{

    namespace Basic
    {

        public static class DefaultModel
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(
                @"This demonstration shows the default configuration of a model. It's pretty
simple - no services are provided, and the model is either idle, or running.
If it's idle, calling ""Start()"" transitions it to running, in which state
the executive processes all of its events. After running, it returns to idle.")]
            public static void Run()
            {
                IModel m = new Highpoint.Sage.SimCore.Model("Demo Model");

                Highpoint.Sage.SimCore.StateMachine sm = m.StateMachine;
                sm.TransitionCompletedSuccessfully +=
                    (model, data) =>
                        Console.WriteLine("Model transitioned successfully to the {0} state.", m.StateMachine.State);

                string[] stateNames = Enum.GetNames(sm.State.GetType());
                Console.WriteLine("State machine's default states are {0}",
                    StringOperations.ToCommasAndAndedList(stateNames));

                DateTime startDateTime = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");
                Console.WriteLine("Registering an event to run in the model at {0}",
                    StringOperations.ToCommasAndAndedList(stateNames));
                m.Executive.RequestEvent(
                    (exec, data) =>
                        Console.WriteLine("{0} : While running, Model state is {1}.", exec.Now, m.StateMachine.State),
                    startDateTime);

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
                Highpoint.Sage.SimCore.Model m = new DemoModel1("Demo Model");

                Highpoint.Sage.SimCore.StateMachine sm = m.StateMachine;
                sm.TransitionCompletedSuccessfully += ReportStateTransition;

                string[] stateNames = Enum.GetNames(sm.State.GetType());
                Console.WriteLine("State machine's default states are {0}",
                    StringOperations.ToCommasAndAndedList(stateNames));

                DateTime startDateTime = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");
                m.Executive.RequestEvent(
                    (exec, data) => Console.WriteLine("Model state is {0}.", m.StateMachine.State), startDateTime);

                Console.WriteLine("Before starting, model state is {0}.", m.StateMachine.State);
                Console.WriteLine("Calling \"Start()\" on the model.");
                m.Start();
                Console.WriteLine("After completion, model state is {0}.", m.StateMachine.State);
            }

            private static void ReportStateTransition(IModel m, object userdata)
            {
                Console.WriteLine("Model transitioned successfully to the {0} state.", m.StateMachine.State);
            }
        }

        public static class DefaultModelWithSelfManagingModelObjects
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(@"This demo creates three tools as IModelObjects, each of which manages and
tracks its own state through nine jobs, requesting events from the executive 
to transition it from idle to running and back. At the end, each tool reports
its utilization and the times at which it underwent each state transition.

It is the same as the ""StateMachine.Basic.SimpleEnumStateMachine"" demo.")]
            public static void Run()
            {
                StateMachine.Basic.SimpleEnumStateMachine.Run();
            }
        }

        internal class DemoModel1 : Highpoint.Sage.SimCore.Model
        {
            public DemoModel1(string name) : base(name)
            {
                AddService(new InitializationManager(States.Initialized));
                GetService<InitializationManager>().AddInitializationTask(DoInitialization);
            }

            #region State Machine Configuration

            /// <summary>
            /// Enum 'States' is the new (custom) structure of the state machine that will drive the model.
            /// </summary>
            private enum States
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
                    new bool[,]
                    {
                        {
                            //        IDL    INI    RUN    
                            /* IDL */  false, true, false
                        },
                        {
                            // Idle can only transition to initializing.
                            /* From */ /* INI */  false, false, true
                        },
                        {
                            // Initializing can only transition to Running.
                            /* RUN */  true, false, false
                        }
                    }; // Running can only transition to Idle.

                // In the follow-on-states array, if a state has itself as a follow-on-state, it is a terminal
                // state, and requires an overt action to leave that state (Such as calling "Run" to leave "Idle.")
                Enum[] followOnStates = new Enum[] {States.Idle, States.Running, States.Idle};

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

            private void DoInitialization(IModel model, object[] parameters)
            {
                Console.WriteLine("Performing some initialization task...");
            }
        }
    }
}