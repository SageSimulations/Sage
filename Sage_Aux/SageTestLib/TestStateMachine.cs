/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.SimCore {

    [TestClass]
    public class StateMachineTester {

        private Random m_random = new Random();
        public static int m_testCounter = 0;
        public static Hashtable m_batch;
        public static bool m_outputEnabled = true;

        public StateMachineTester() { Init(); }

        public enum States : int { Idle = 0, Validated = 1, Running = 2, Paused = 3, Finished = 4 }

        [TestInitialize]
        public void Init() {
            m_batch = new Hashtable();
            m_testCounter = 0;
            m_batch.Add("Batch", m_testCounter);
        }
        [TestCleanup]
        public void destroy() {
            Trace.WriteLine("Done.");
        }

        private static void CheckBatch(object userData) {
            IDictionary graphContext = userData as IDictionary;
            System.Diagnostics.Debug.Assert(graphContext != null);
            System.Diagnostics.Debug.Assert(graphContext["Batch"] != null);
            System.Diagnostics.Debug.Assert(graphContext["Batch"].Equals((object)m_testCounter));
            graphContext["Batch"] = ++m_testCounter;
            System.Diagnostics.Debug.Assert(graphContext["Batch"].Equals((object)m_testCounter));
        }

        /// <summary>
        /// This test confirms some base information about the transition matrix.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test confirms some base information about the transition matrix.")]
        public void TestStateMachine() {

            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);
#if DEBUG
            Trace.WriteLine("Idle state is " + sm._TestGetStateNumber(States.Idle));
            Trace.WriteLine("Validated state is " + sm._TestGetStateNumber(States.Validated));
            Trace.WriteLine("Paused state is " + sm._TestGetStateNumber(States.Paused));
            Trace.WriteLine("Running state is " + sm._TestGetStateNumber(States.Running));
            Trace.WriteLine("Finished state is " + sm._TestGetStateNumber(States.Finished));

            Trace.WriteLine("Idle to Validated is valid? " + sm.TransitionHandler(States.Idle, States.Validated).IsValidTransition);
            Trace.WriteLine("Idle to Paused is valid? " + sm.TransitionHandler(States.Idle, States.Paused).IsValidTransition);
#endif
            sm.DoTransition(States.Validated, m_batch);

        }

        /// <summary>
        /// This test confirms some base information about the transition matrix.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test confirms some base information about the transition matrix.")]
        public void TestStateMachinePerformance() {

            StateMachine sm = Initialize();
            sm.StructureLocked = false;
            m_outputEnabled = false;
            //sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            //sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            //sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);

            //Trace.WriteLine("Idle state is " + sm._TestGetStateNumber(States.Idle));
            //Trace.WriteLine("Validated state is " + sm._TestGetStateNumber(States.Validated));
            //Trace.WriteLine("Paused state is " + sm._TestGetStateNumber(States.Paused));
            //Trace.WriteLine("Running state is " + sm._TestGetStateNumber(States.Running));
            //Trace.WriteLine("Finished state is " + sm._TestGetStateNumber(States.Finished));

            //Trace.WriteLine("Idle to Validated is valid? " + sm.TransitionHandler(States.Idle, States.Validated).IsValidTransition);
            //Trace.WriteLine("Idle to Paused is valid? " + sm.TransitionHandler(States.Idle, States.Paused).IsValidTransition);

            for (int i = 0 ; i < 1000 ; i++) {
                sm.DoTransition(States.Validated, m_batch);
                sm.DoTransition(States.Idle, m_batch);
            }

            m_outputEnabled = true;

        }


        /// <summary>
        /// This test has been set up so that it should succeed and endup in a 'Finished' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that it should succeed and end up in a 'Finished' state.")]
        public void TestTransitionSuccessWithFollowon() {
            StateMachine sm = Initialize(true);
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);

            sm.DoTransition(States.Validated, m_batch);

            System.Diagnostics.Debug.Assert(States.Finished.Equals(sm.State), "State machine did not transition to 'Finished' state");

        }

        /// <summary>
        /// This test has been set up so that it should succeed and endup in a 'Valid' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that it should succeed and end up in a 'Validated' state.")]
        public void TestTransitionSuccessWithoutFollowon() {
            StateMachine sm = Initialize(false);
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);

            sm.DoTransition(States.Validated, m_batch);

            System.Diagnostics.Debug.Assert(States.Validated.Equals(sm.State), "State machine did not transition to 'Validated' state");

        }

        /// <summary>
        /// This test has been set up so that the preparation fails, 
        /// which means the state machine has to stay in the 'Idle' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that the preparation fails, which means the state machine has to stay in the 'Idle' state.")]
        public void TestTransitionFailure() {
            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithFailure);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);

            try {
                sm.DoTransition(States.Validated, m_batch);
            } catch (TransitionFailureException tfe) {
                Trace.WriteLine(tfe);
            }
            System.Diagnostics.Debug.Assert(States.Idle.Equals(sm.State), "State machine did not stay in 'Idle' state");

        }

        /// <summary>
        /// This test has been set up so that we attempt an illegle transition from 'Idle' to 'Paused', 
        /// which means the state machine has to stay in the 'Idle' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that we attempt an illegal transition from 'Idle' to 'Paused', "
                    + "which means the state machine has to stay in the 'Idle' state.")]
        public void TestTransitionIllegal() {
            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);

            try {
                sm.DoTransition(States.Paused, m_batch);
            } catch (TransitionFailureException tfe) {
                Trace.WriteLine(tfe);
            }
            System.Diagnostics.Debug.Assert(States.Idle.Equals(sm.State), "State machine did not stay in 'Idle' state");

        }

        /// <summary>
        /// This test has been set up so that we attempt to set up an illegle TransitionHandler from 'Idle' to 'Paused', 
        /// which means the state machine has to throw an ApplicationException.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that we attempt to set up an illegal TransitionHandler from 'Idle' to 'Paused', "
                    + "which means the state machine has to throw an ApplicationException.")]
        [ExpectedException(typeof(TransitionFailureException))]
        public void TestTransitionIllegalToo() {
            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Paused).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            sm.DoTransition(States.Paused, m_batch);
        }

        /// <summary>
        /// This test has been set up so that a complete cicle through all states successfully completes.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that a complete cycle through all states successfully completes.")]
        public void TestTransitionChainSuccess() {

            StateMachine sm = Initialize(false);
            sm.TransitionHandler(States.Finished, States.Idle).Prepare += new PrepareTransitionEvent(PrepareToTransitionToIdleWithSuccess);
            sm.TransitionHandler(States.Finished, States.Idle).Commit += new CommitTransitionEvent(CommitTransitionToIdle);
            sm.TransitionHandler(States.Finished, States.Idle).Rollback += new RollbackTransitionEvent(RollbackTransitionToIdle);

            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);

            sm.TransitionHandler(States.Validated, States.Running).Prepare += new PrepareTransitionEvent(PrepareToTransitionToRunningWithSuccess);
            sm.TransitionHandler(States.Validated, States.Running).Commit += new CommitTransitionEvent(CommitTransitionToRunning);
            sm.TransitionHandler(States.Validated, States.Running).Rollback += new RollbackTransitionEvent(RollbackTransitionToRunning);

            sm.TransitionHandler(States.Paused, States.Running).Prepare += new PrepareTransitionEvent(PrepareToTransitionToRunningWithSuccess);
            sm.TransitionHandler(States.Paused, States.Running).Commit += new CommitTransitionEvent(CommitTransitionToRunning);
            sm.TransitionHandler(States.Paused, States.Running).Rollback += new RollbackTransitionEvent(RollbackTransitionToRunning);

            sm.TransitionHandler(States.Running, States.Paused).Prepare += new PrepareTransitionEvent(PrepareToTransitionToPausedWithSuccess);
            sm.TransitionHandler(States.Running, States.Paused).Commit += new CommitTransitionEvent(CommitTransitionToPaused);
            sm.TransitionHandler(States.Running, States.Paused).Rollback += new RollbackTransitionEvent(RollbackTransitionToPaused);

            sm.TransitionHandler(States.Running, States.Finished).Prepare += new PrepareTransitionEvent(PrepareToTransitionToFinishedWithSuccess);
            sm.TransitionHandler(States.Running, States.Finished).Commit += new CommitTransitionEvent(CommitTransitionToFinished);
            sm.TransitionHandler(States.Running, States.Finished).Rollback += new RollbackTransitionEvent(RollbackTransitionToFinished);

            sm.DoTransition(States.Validated, m_batch);
            System.Diagnostics.Debug.Assert(States.Validated.Equals(sm.State), "Transition chain did not move to the 'Validated' state.");
            sm.DoTransition(States.Running, m_batch);
            System.Diagnostics.Debug.Assert(States.Running.Equals(sm.State), "Transition chain did not move to the 'Running' state.");
            sm.DoTransition(States.Paused, m_batch);
            System.Diagnostics.Debug.Assert(States.Paused.Equals(sm.State), "Transition chain did not move to the 'Paused' state.");
            sm.DoTransition(States.Running, m_batch);
            System.Diagnostics.Debug.Assert(States.Running.Equals(sm.State), "Transition chain did not move to the 'Running' state.");
            sm.DoTransition(States.Finished, m_batch);
            System.Diagnostics.Debug.Assert(States.Finished.Equals(sm.State), "Transition chain did not move to the 'Finished' state.");

        }

        /// <summary>
        /// This test has been set up to see if multiple TransitionHandler can be defined successfully.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up to see if multiple TransitionHandler can be defined successfully.")]
        public void TestTransitionMultipleHandlers() {

            StateMachine sm = Initialize(false);

            // Set up Prepare handlers.
            sm.UniversalTransitionHandler().Prepare += new PrepareTransitionEvent(UniversalPrepareToTransition);

            sm.OutboundTransitionHandler(States.Idle).Prepare += new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_1);
            sm.OutboundTransitionHandler(States.Idle).Prepare += new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_2);
            sm.OutboundTransitionHandler(States.Idle).Prepare += new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_3);
            sm.OutboundTransitionHandler(States.Idle).Prepare += new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_4);

            sm.InboundTransitionHandler(States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_1);
            sm.InboundTransitionHandler(States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_2);
            sm.InboundTransitionHandler(States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_3);
            sm.InboundTransitionHandler(States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_4);

            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_1);
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_2);
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_3);
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_4);

            // Set up Commit handlers.
            sm.UniversalTransitionHandler().Commit += new CommitTransitionEvent(UniversalCommitTransition);

            sm.OutboundTransitionHandler(States.Idle).Commit += new CommitTransitionEvent(CommitTransitionOutOfIdle_1);
            sm.OutboundTransitionHandler(States.Idle).Commit += new CommitTransitionEvent(CommitTransitionOutOfIdle_2);
            sm.OutboundTransitionHandler(States.Idle).Commit += new CommitTransitionEvent(CommitTransitionOutOfIdle_3);
            sm.OutboundTransitionHandler(States.Idle).Commit += new CommitTransitionEvent(CommitTransitionOutOfIdle_4);

            sm.InboundTransitionHandler(States.Validated).Commit += new CommitTransitionEvent(CommitTransitionToValid_1);
            sm.InboundTransitionHandler(States.Validated).Commit += new CommitTransitionEvent(CommitTransitionToValid_2);
            sm.InboundTransitionHandler(States.Validated).Commit += new CommitTransitionEvent(CommitTransitionToValid_3);
            sm.InboundTransitionHandler(States.Validated).Commit += new CommitTransitionEvent(CommitTransitionToValid_4);

            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitionIdleToValid_1);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitionIdleToValid_2);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitionIdleToValid_3);
            sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitionIdleToValid_4);

            sm.DoTransition(States.Validated, m_batch);

            System.Diagnostics.Debug.Assert(States.Validated.Equals(sm.State), "");

        }

        /// <summary>
        /// This test has been set up to see if multiple TransitionHandler can successfully be defined in a sorted order.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up to see if multiple TransitionHandler can successfully be defined in a sorted order.")]
        public void TestTransitionMultipleHandlersSorted() {

            StateMachine sm = Initialize(false);

            // Set up Prepare handlers.
            sm.UniversalTransitionHandler().AddPrepareEvent(new PrepareTransitionEvent(UniversalPrepareToTransition), -2);

            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_1), 4);
            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_2), 3);
            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_3), 2);
            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionOutOfIdleWithSuccess_4), 1);

            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_1), 4);
            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_2), 3);
            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_3), 2);
            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess_4), 1);

            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_1), 4);
            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_2), 3);
            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_3), 2);
            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(new PrepareTransitionEvent(PrepareToTransitionIdletoValidWithSuccess_4), 1);

            // Set up Commit handlers.
            sm.UniversalTransitionHandler().AddCommitEvent(new CommitTransitionEvent(UniversalCommitTransition), -2);

            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(new CommitTransitionEvent(CommitTransitionOutOfIdle_1), 4);
            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(new CommitTransitionEvent(CommitTransitionOutOfIdle_2), 3);
            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(new CommitTransitionEvent(CommitTransitionOutOfIdle_3), 2);
            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(new CommitTransitionEvent(CommitTransitionOutOfIdle_4), 1);

            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionToValid_1), 4);
            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionToValid_2), 3);
            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionToValid_3), 2);
            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionToValid_4), 1);

            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionIdleToValid_1), 4);
            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionIdleToValid_2), 3);
            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionIdleToValid_3), 2);
            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(new CommitTransitionEvent(CommitTransitionIdleToValid_4), 1);

            sm.DoTransition(States.Validated, m_batch);

            System.Diagnostics.Debug.Assert(States.Validated.Equals(sm.State), "");

        }

        #region Internal Methods

        private StateMachine Initialize() {
            return Initialize(true);
        }

        private StateMachine Initialize(bool enableAutoFollowOnStates) {

            StateMachineTestModel.EnableAutoFollowOnStates = enableAutoFollowOnStates;
            Model model = new StateMachineTestModel("SMTestModel");

            model.StateMachine.SetStateMethod(new StateMethod(StateHandler), States.Idle);
            model.StateMachine.SetStateMethod(new StateMethod(StateHandler), States.Validated);
            model.StateMachine.SetStateMethod(new StateMethod(StateHandler), States.Running);
            model.StateMachine.SetStateMethod(new StateMethod(StateHandler), States.Paused);
            model.StateMachine.SetStateMethod(new StateMethod(StateHandler), States.Finished);

            return model.StateMachine;
        }

        class StateMachineTestModel : Model {

            public static bool EnableAutoFollowOnStates = true;

            public StateMachineTestModel(string name)
                : base(name, Guid.NewGuid()) {

            }
            protected override StateMachine CreateStateMachine() {
                bool[,] transitionMatrix =
                    new bool[5, 5] { {
                                        ///        IDL    VAL    RUN    PAU    FIN
                                        /* IDL */  false, true,  false, false, true},{
                                        /* VAL */  true,  false, true,  false, true},{
                                        /* RUN */  true,  false, false, true,  true},{
                                        /* PAU */  true,  false, true,  false, true},{
                                        /* FIN */  true,  false, false, false, false }};

                Enum[] followOnStates = null;
                if (EnableAutoFollowOnStates) {
                    followOnStates = new Enum[] { States.Idle, States.Running, States.Finished, States.Paused, States.Finished };
                }

                return new StateMachine(this, transitionMatrix, followOnStates, States.Idle);
            }

        }

        private void StateHandler(IModel model, object userData) {
            CheckBatch(userData);
            if (m_outputEnabled) {
                Trace.WriteLine("The model " + model.Name + " is now in the " + model.StateMachine.State + " state.");
            }
        }

        #endregion

        #region A Gazillion Handlers.

        public ITransitionFailureReason PrepareToTransitionToIdleWithSuccess(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toIdle");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToIdleWithFailure(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toIdle (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Idle.", null);
        }

        public void CommitTransitionToIdle(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to Idle transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitionToIdle(IModel model, object userData, IList reasonsForFailure) {
            if (m_outputEnabled)
                Trace.WriteLine("Rolling back Idle transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure) {
                if (m_outputEnabled)
                    Trace.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition to Valid");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitiontoValidWithFailure(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition to Valid (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Valid.", null);
        }

        public void CommitTransitiontoValid(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to Valid transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitiontoValid(IModel model, object userData, IList reasonsForFailure) {
            if (m_outputEnabled)
                Trace.WriteLine("Rolling back Valid transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure) {
                if (m_outputEnabled)
                    Trace.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionToRunningWithSuccess(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toRunning");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToRunningWithFailure(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toRunning (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Running.", null);
        }

        public void CommitTransitionToRunning(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to Running transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitionToRunning(IModel model, object userData, IList reasonsForFailure) {
            if (m_outputEnabled)
                Trace.WriteLine("Rolling back Running transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure) {
                Trace.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionToPausedWithSuccess(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toPaused");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToPausedWithFailure(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toPaused (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Paused.", null);
        }

        public void CommitTransitionToPaused(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to Paused transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitionToPaused(IModel model, object userData, IList reasonsForFailure) {
            if (m_outputEnabled)
                Trace.WriteLine("Rolling back Paused transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure) {
                Trace.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionToFinishedWithSuccess(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toFinished");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToFinishedWithFailure(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition toFinished (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Finished.", null);
        }

        public void CommitTransitionToFinished(IModel model, object userData) {
            CheckBatch(userData);
            if (m_outputEnabled)
                Trace.WriteLine("Committing to Finished transition.");
        }

        public void RollbackTransitionToFinished(IModel model, object userData, IList reasonsForFailure) {
            if (m_outputEnabled)
                Trace.WriteLine("Rolling back Finished transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure) {
                Trace.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_1(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition out of Idle (1)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_2(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition out of Idle (2)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_3(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition out of Idle (3)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_4(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition out of Idle (4)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_1(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition into Valid (1)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_2(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition into Valid (2)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_3(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition into Valid (3)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_4(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition into Valid (4)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_1(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition from Idle into Valid (1)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_2(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition from Idle into Valid (2)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_3(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition from Idle into Valid (3)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_4(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Preparing to transition from Idle into Valid (4)");
            return null;
            //CheckBatch(userData);
        }

        public void CommitTransitionOutOfIdle_1(IModel model, object userData) {
            CheckBatch(userData);
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle (1).");
        }

        public void CommitTransitionOutOfIdle_2(IModel model, object userData) {
            CheckBatch(userData);
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle (2).");
        }

        public void CommitTransitionOutOfIdle_3(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle (3).");
            CheckBatch(userData);
        }

        public void CommitTransitionOutOfIdle_4(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle (4).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_1(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition to Valid (1).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_2(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition to Valid (2).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_3(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition to Valid (3).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_4(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition to Valid (4).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_1(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle to Valid (1).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_2(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle to Valid (2).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_3(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle to Valid (3).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_4(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Committing to transition from Idle to Valid (4).");
            CheckBatch(userData);
        }

        public ITransitionFailureReason UniversalPrepareToTransition(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Universal handler reports, Preparing to transition to Valid (success)");
            CheckBatch(userData);
            return null;
        }

        public void UniversalCommitTransition(IModel model, object userData) {
            if (m_outputEnabled)
                Trace.WriteLine("Universal handler reports, Committing to Valid transition.");
            CheckBatch(userData);
        }

        public void UniversalRollbackTransition(IModel model, object userData, IList reasonsForFailure) {
            if (m_outputEnabled)
                Trace.WriteLine("Universal handler reports, Rolling back transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            if (m_outputEnabled) {
                foreach (ITransitionFailureReason tfr in reasonsForFailure) {
                    Trace.WriteLine(tfr.Reason);
                }
            }
        }



        #endregion
    }

    internal class DescriptionAttribute : Attribute
    {
        private string v;

        public DescriptionAttribute(string v)
        {
            this.v = v;
        }
    }
}
