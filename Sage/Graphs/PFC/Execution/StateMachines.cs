/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using Highpoint.Sage.SimCore;
using System.Collections;
using System.Diagnostics;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Graphs.PFC.Execution {

    public enum StepState { Idle, Running, Complete, Aborting, Aborted, Stopping, Stopped, Pausing, Paused, Holding, Held, Restarting }

    public delegate void StepStateMachineEvent(StepStateMachine ssm, object userData);

    public class StepStateMachine {

        #region Static Configuration Fields
        private static readonly bool[,] s_transition_Matrix = new bool[12, 12] { {
            //         IDL    RNG    CMP    ABG    ABD    STG    STD    PSG    PSD    HDG    HLD    RSG
            /* IDL */  false, true , false, false, false, false, false, false, false, false, false, false },{
            /* RNG */  false, false, true , true , false, true , false, true , false, true , false, false },{
            /* CMP */  true , false, false, false, false, false, false, false, false, false, false, false },{
            /* ABG */  false, false, false, false, true , false, false, false, false, false, false, false },{
            /* ABD */  true , false, false, false, false, false, false, false, false, false, false, false },{
            /* STG */  false, false, false, false, false, false, true , false, false, false, false, false },{
            /* STD */  true , false, false, false, false, false, false, false, false, false, false, false },{
            /* PSG */  false, false, false, false, false, false, false, false, true , false, false, false },{
            /* PSD */  false, true , false, false, false, false, false, false, false, false, false, false },{
            /* HDG */  false, false, false, false, false, false, false, false, false, false, true , false },{
            /* HLD */  false, false, false, false, false, false, false, false, false, false, false, true  },{
            /* RSG */  false, true , false, false, false, false, false, false, false, false, false, false }
        };

        private static readonly StepState[] s_follow_On_States = new StepState[]{   
          /*Idle            -->*/ StepState.Idle,
          /*Running         -->*/ StepState.Running,
          /*Complete        -->*/ StepState.Complete,
          /*Aborting        -->*/ StepState.Aborted,
          /*Aborted         -->*/ StepState.Aborted,
          /*Stopping        -->*/ StepState.Stopped,
          /*Stopped         -->*/ StepState.Stopped,
          /*Pausing         -->*/ StepState.Paused,
          /*Paused          -->*/ StepState.Paused,
          /*Holding         -->*/ StepState.Holding,
          /*Held            -->*/ StepState.Held,
          /*Restarting      -->*/ StepState.Running
        };

        private static readonly StepState s_initial_State = StepState.Idle;
        #endregion

        #region Private Fields
        private IPfcStepNode m_myStep = null;
        private List<TransitionStateMachine> m_successorStateMachines;
        private static Guid _leafLevelActionMask = new Guid("067769d2-573b-475e-bffe-4a8a8a04cd01");
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PfcStepStateMachine");
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="StepStateMachine"/> class.
        /// </summary>
        /// <param name="myStep">My step.</param>
        public StepStateMachine(IPfcStepNode myStep) {
            m_myStep = myStep;
            m_successorStateMachines = new List<TransitionStateMachine>();
        }

        public void Start(PfcExecutionContext parentPfcec) {
            Debug.Assert(!parentPfcec.IsStepCentric); // Must be called with parent.
            Debug.Assert(parentPfcec.PFC.Equals(MyStep.Parent));

            #region Create a context under the parent PFCEC to run this iteration of this step.
            SsmData ssmData = GetSsmData(parentPfcec); // This will create a new SSMData element.

            if (ssmData.ExecutionInstanceCount == 0) {
                ssmData.InitializeExecutionInstanceUid(parentPfcec.Guid, MyStep.Guid);
            }
            Guid myExecutionInstanceGuid = ssmData.GetNextExecutionInstanceUid();

            PfcExecutionContext myPfcec = new PfcExecutionContext(m_myStep, m_myStep.Name, null, myExecutionInstanceGuid, parentPfcec);
            myPfcec.InstanceCount = ssmData.ExecutionInstanceCount-1;
            ssmData.ActiveStepInstanceEc = myPfcec;
            if (s_diagnostics) {
                Console.WriteLine("PFCEC " + myPfcec.Name + "(instance " + myPfcec.InstanceCount + ") created.");
            }
            #endregion

            if (s_diagnostics) {
                Console.WriteLine("Starting step " + m_myStep.Name + " with ec " + myPfcec.Name + ".");
            }

            GetStartPermission(myPfcec);

            // Once we have permission to start (based on state), we will create a new execContext for this execution.
            DoTransition(StepState.Running, myPfcec);
        }

        public void Stop(PfcExecutionContext parentPfcec) {
            Debug.Assert(!parentPfcec.IsStepCentric); // Must be called with parent.
            Debug.Assert(parentPfcec.PFC.Equals(MyStep.Parent));

            PfcExecutionContext pfcec = GetActiveInstanceExecutionContext(parentPfcec);
            if (s_diagnostics) {
                Console.WriteLine("Stopping step " + m_myStep.Name + " with ec " + pfcec.Name + ".");
            }

            DoTransition(StepState.Stopping, pfcec);
        }

        public void Reset(PfcExecutionContext parentPfcec) {
            Debug.Assert(!parentPfcec.IsStepCentric); // Must be called with parent.
            Debug.Assert(parentPfcec.PFC.Equals(MyStep.Parent));

            PfcExecutionContext pfcec = GetActiveInstanceExecutionContext(parentPfcec);
            if (s_diagnostics) {
                Console.WriteLine("Resetting step " + m_myStep.Name + " with ec " + pfcec.Name + ".");
            }
            DoTransition(StepState.Idle, pfcec);
        }

        /// <summary>
        /// Gets the state of this step.
        /// </summary>
        /// <value>The state.</value>
        public StepState GetState(PfcExecutionContext parentPfcec) {
            SsmData ssmData = GetSsmData(parentPfcec);
            return ssmData.State;
        }

        public PfcExecutionContext GetActiveInstanceExecutionContext(PfcExecutionContext pfcEc) {
            return GetSsmData(pfcEc).ActiveStepInstanceEc;
        }

        public List<PfcExecutionContext> GetExecutionContexts(PfcExecutionContext pfcEc) {
            return GetSsmData(pfcEc).InstanceExecutionContexts;
        }

        private class SsmData {
            private StepState m_state = s_initial_State;
            private Queue<IDetachableEventController> m_qIdec = new Queue<IDetachableEventController>();
            private Guid m_nextExecutionInstanceUid = Guid.Empty;
            private int m_numberOfIterations = 0;
            private PfcExecutionContext m_currentStepInstanceEc = null;
            private List<PfcExecutionContext> m_lstStepInstanceECs = new List<PfcExecutionContext>();
            public SsmData() {}

            public StepState State { get { return m_state; } set { m_state = value; } }
            public Queue<IDetachableEventController> QueueIdec { get { return m_qIdec; } }
            public int ExecutionInstanceCount { get { return m_numberOfIterations; } }
            public PfcExecutionContext ActiveStepInstanceEc { 
                [DebuggerStepThrough] get { return m_currentStepInstanceEc; } 
                set { 
                    Debug.Assert(value == null || m_currentStepInstanceEc == null);
                    if (value != null) {
                        InstanceExecutionContexts.Add(value);
                    }
                    m_currentStepInstanceEc = value; 
                } 
            }
            public List<PfcExecutionContext> InstanceExecutionContexts {
                get { return m_lstStepInstanceECs; }
            }
            internal Guid GetNextExecutionInstanceUid() {
                Debug.Assert(!m_nextExecutionInstanceUid.Equals(Guid.Empty));
                Guid retval = m_nextExecutionInstanceUid;
                m_nextExecutionInstanceUid = GuidOps.Increment(m_nextExecutionInstanceUid);
                m_numberOfIterations++;
                return retval;
            }

            internal void InitializeExecutionInstanceUid(Guid parentExecutionContextUid, Guid myStepUid) {
                Debug.Assert(m_nextExecutionInstanceUid.Equals(Guid.Empty));
                m_nextExecutionInstanceUid = GuidOps.XOR(parentExecutionContextUid, myStepUid);
            }
        }

        private SsmData GetSsmData(PfcExecutionContext pfcec) {
            if (MyStep.Equals(pfcec.Step)) {
                pfcec = (PfcExecutionContext)pfcec.Parent;
            }

            if (!pfcec.Contains(this)) {
                SsmData retval = new SsmData();
                pfcec.Add(this, retval);
            }

            return (SsmData)pfcec[this];
        }

        /// <summary>
        /// Gets the PFC step that this state machine represents.
        /// </summary>
        /// <value>The step.</value>
        public IPfcStepNode MyStep {
            get { return m_myStep; }
            internal set { m_myStep = value; }
        }

        public bool StructureLocked { get { return true; } set { ; } }

        public event StepStateMachineEvent StepStateChanged;

        /// <summary>
        /// Gets the successor state machines.
        /// </summary>
        /// <value>The successor state machines.</value>
        public List<TransitionStateMachine> SuccessorStateMachines {
            get { return m_successorStateMachines; }
        }

        /// <summary>
        /// Gets a value indicating whether this step state machine is in a final state - Aborted, Stopped or Complete.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in final state; otherwise, <c>false</c>.
        /// </value>
        public bool IsInFinalState(PfcExecutionContext pfcec) {
            SsmData ssmData = GetSsmData(pfcec);
            return ssmData.State == StepState.Complete || ssmData.State == StepState.Aborted || ssmData.State == StepState.Stopped;
        }

        /// <summary>
        /// Gets a value indicating whether this step state machine is in a quiescent state - Held or Paused.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in quiescent state; otherwise, <c>false</c>.
        /// </value>
        public bool IsInQuiescentState(PfcExecutionContext pfcec) {
            SsmData ssmData = GetSsmData(pfcec);
            return ssmData.State == StepState.Held || ssmData.State == StepState.Paused;
        }

        /// <summary>
        /// Gets the name of the step that this Step State Machine represents.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return m_myStep.Name; } }

        internal void GetStartPermission(PfcExecutionContext pfcec) {
            IDetachableEventController currentEventController = m_myStep.Model.Executive.CurrentEventController;
            SsmData ssmData = GetSsmData(pfcec);
            if (!ssmData.State.Equals(StepState.Idle)) {
                ssmData.QueueIdec.Enqueue(currentEventController);
                if (s_diagnostics) {
                    Console.WriteLine(m_myStep.Model.Executive.Now + " : suspending awaiting start of " + m_myStep.Name + " ...");
                }
                currentEventController.Suspend();
                if (s_diagnostics) {
                    Console.WriteLine(m_myStep.Model.Executive.Now + " : resuming the starting of     " + m_myStep.Name + " ...");
                }
            }
        }

        /// <summary>
        /// Creates pfc execution contexts, one per action under the step that is currently running. Each
        /// is given an instance count of zero, as a step can run its action only once, currently.
        /// </summary>
        /// <param name="parentContext">The parent context, that of the step that is currently running.</param>
        /// <param name="kids">The procedure function charts that live in the actions under the step that is currently running.</param>
        /// <param name="kidContexts">The pfc execution contexts that will correspond to the running of each of the child PFCs.</param>
        protected virtual void CreateChildContexts(PfcExecutionContext parentContext, out IProcedureFunctionChart[] kids, out PfcExecutionContext[] kidContexts) {
            int kidCount = MyStep.Actions.Count;
            kids = new ProcedureFunctionChart[kidCount];
            kidContexts = new PfcExecutionContext[kidCount];
            int i = 0;
            foreach (KeyValuePair<string, IProcedureFunctionChart> kvp in MyStep.Actions) {
                IProcedureFunctionChart kid = kvp.Value;
                kids[i] = kid;
                Guid kidGuid = GuidOps.XOR(parentContext.Guid, kid.Guid);
                while (parentContext.Contains(kidGuid)) {
                    kidGuid = GuidOps.Increment(kidGuid);
                }
                kidContexts[i] = new PfcExecutionContext(kid, kvp.Key, null, kidGuid, parentContext);
                kidContexts[i].InstanceCount = 0;
                i++;
            }
        }

        #region Test State Machine Methods
        private void TestStateMachine_DoTransition(StepState fromState, StepState toState, PfcExecutionContext myPfcec) {
            switch (fromState) {

                case StepState.Idle:
                    switch (toState) {
                        case StepState.Running:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Running:
                    switch (toState) {
                        case StepState.Complete:
                            break;
                        case StepState.Aborting:
                            break;
                        case StepState.Stopping:
                            break;
                        case StepState.Pausing:
                            break;
                        case StepState.Holding:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Complete:
                    switch (toState) {
                        case StepState.Idle:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Aborting:
                    switch (toState) {
                        case StepState.Aborted:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Aborted:
                    switch (toState) {
                        case StepState.Idle:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Stopping:
                    switch (toState) {
                        case StepState.Stopped:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Stopped:
                    switch (toState) {
                        case StepState.Idle:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Pausing:
                    switch (toState) {
                        case StepState.Paused:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Paused:
                    switch (toState) {
                        case StepState.Running:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Holding:
                    switch (toState) {
                        case StepState.Held:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Held:
                    switch (toState) {
                        case StepState.Restarting:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Restarting:
                    switch (toState) {
                        case StepState.Running:
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        private void StateChangeCompleted(PfcExecutionContext pfcec) {
            if (StepStateChanged != null) {
                StepStateChanged(this, pfcec);
            }
            SuccessorStateMachines.ForEach(delegate(TransitionStateMachine tsm) { tsm.PredecessorStateChange(pfcec); });

            SsmData ssmData = GetSsmData(pfcec);
            if (( ssmData.State == StepState.Idle ) && ( ssmData.QueueIdec.Count > 0 )) {
                ssmData.QueueIdec.Dequeue().Resume();
            }
        }

        private void DoRunning(PfcExecutionContext pfcec) {
            if (s_diagnostics) {
                string msg = "Starting to run {0} action{1} under step {2} with ec {3}.";
                string nKids = "1";
                string plural = "";
                string stepName = m_myStep.Name;
                string ecName = pfcec.Name;
                int nActions = (m_myStep.Actions?.Count ?? 0) + m_myStep.LeafLevelAction.GetInvocationList().Length;
                nKids = nActions.ToString();
                plural = nActions == 1 ? "" : "s";
                if (nActions == 0) msg = "There are no actions to run under step {2} with ec {3}.";
                Console.WriteLine(msg, nKids, plural, stepName, ecName);
            }

            IModel model = m_myStep.Model;
            SsmData ssmData = GetSsmData(pfcec);
            Debug.Assert(model.Executive.CurrentEventType == ExecEventType.Detachable);
            if (model != null && model.Executive != null) {
                if (m_myStep.Actions != null && m_myStep.Actions.Count > 0) {
                    IProcedureFunctionChart[] kids;
                    PfcExecutionContext[] kidContexts;
                    CreateChildContexts(ssmData.ActiveStepInstanceEc, out kids, out kidContexts);
                    foreach (IProcedureFunctionChart action in m_myStep.Actions.Values) {
                        for (int i = 0 ; i < kidContexts.Length ; i++) {
                            model.Executive.RequestEvent(new ExecEventReceiver(kids[i].Run), model.Executive.Now, 0.0, kidContexts[i], ExecEventType.Detachable);
                        }
                    }
                    new PfcStepJoiner(ssmData.ActiveStepInstanceEc, kids).RunAndWait();
                } else {
                    //PfcExecutionContext iterPfc = CreateIterationContext(pfcec);
                    m_myStep.LeafLevelAction(pfcec, this);
                }
            }

            DoTransition(StepState.Complete, pfcec);
        }

        private void DoTransition(StepState toState, PfcExecutionContext myPfcec) {
            SsmData ssmData = GetSsmData(myPfcec);
            StepState fromState = ssmData.State;
            if (s_transition_Matrix[(int)fromState, (int)toState]) {
                ssmData.State = toState;

                bool timePeriodContainer = myPfcec.TimePeriod is Scheduling.TimePeriodEnvelope;

                if (!timePeriodContainer) {
                    if (fromState == StepState.Running && toState == StepState.Complete) {
                        myPfcec.TimePeriod.EndTime = myPfcec.Model.Executive.Now;
                    }
                }

                // Get permission from Step to run.
                if (fromState == StepState.Idle && toState == StepState.Running) {
                    m_myStep.GetPermissionToStart(myPfcec, this);
                }

                //Console.WriteLine("{2} from {0} to {1}", fromState, toState, this.Name);
                if (!timePeriodContainer) {
                    if (fromState == StepState.Idle && toState == StepState.Running) {
                        myPfcec.TimePeriod.StartTime = myPfcec.Model.Executive.Now;
                    }
                }

                if (fromState == StepState.Complete && toState == StepState.Idle) {
                    ssmData.ActiveStepInstanceEc = null;
                }

                StateChangeCompleted(myPfcec);

                if (fromState == StepState.Idle && toState == StepState.Running) {
                    DoRunning(myPfcec);
                }

                StepState followOnState = s_follow_On_States[(int)toState];
                if (followOnState != toState) {
                    DoTransition(followOnState, myPfcec);
                }

            } else {
                string msg = string.Format("Illegal attempt to transition from {0} to {1} in step state machine for {2}.", fromState, toState, Name);
                throw new ApplicationException(msg);
            }
        }

        /// <summary>
        /// PFCStepJoiner, when RunAndWait is called, halts the step that owns the rootStepPfcec, and waits for completion of
        /// each child PFC (these are to have been actions of the root step) before resuming the parent step.
        /// </summary>
        private class PfcStepJoiner {

            #region Private Fields
            private IModel m_model;
            private PfcExecutionContext m_rootStepEc;
            private TransitionStateMachineEvent m_onTransitionStateChanged;
            private IDetachableEventController m_idec;
            private List<IProcedureFunctionChart> m_pendingActions;
            #endregion Private Fields

            public PfcStepJoiner(PfcExecutionContext rootStepPfcec, IProcedureFunctionChart[] childPfCs) {
                Debug.Assert(rootStepPfcec.IsStepCentric);
                m_rootStepEc = rootStepPfcec;
                m_model = m_rootStepEc.Model;
                m_idec = null;
                m_onTransitionStateChanged = new TransitionStateMachineEvent(OnTransitionStateChanged);
                m_pendingActions = new List<IProcedureFunctionChart>(childPfCs);
                m_pendingActions.ForEach(delegate(IProcedureFunctionChart kid) {
                    kid.GetFinishTransition().MyTransitionStateMachine.TransitionStateChanged += m_onTransitionStateChanged;
                });
            }

            public void RunAndWait() {
                m_idec = m_model.Executive.CurrentEventController;
                m_idec.Suspend();
            }

            private void OnTransitionStateChanged(TransitionStateMachine tsm, object userData) {
                PfcExecutionContext completedStepsParentPfcec = (PfcExecutionContext)userData;
                if (completedStepsParentPfcec.Parent.Payload.Equals(m_rootStepEc) && tsm.GetState(completedStepsParentPfcec) == TransitionState.Inactive) {
                    tsm.TransitionStateChanged -= m_onTransitionStateChanged;
                    m_pendingActions.Remove(tsm.MyTransition.Parent);
                    if (m_pendingActions.Count == 0) {
                        m_idec.Resume();
                    }
                }
            }
        }
    }

    public enum TransitionState { Active, Inactive, NotBeingEvaluated }

    public delegate void TransitionStateMachineEvent(TransitionStateMachine tsm, object userData);

    public delegate bool ExecutableCondition(object graphContext, TransitionStateMachine tsm);

    public class TransitionStateMachine {
        private class TsmData {
            private long m_nextExpressionEvaluation;
            private TransitionState m_state;
            public TsmData() {
                m_nextExpressionEvaluation = 0L;
                m_state = TransitionState.Inactive;
            }
            public long NextExpressionEvaluation { get { return m_nextExpressionEvaluation; } set { m_nextExpressionEvaluation = value; } }
            public TransitionState State { get { return m_state; } set { m_state = value; } }

        }

        #region Private Fields
        private Random m_random = new Random();

        private IPfcTransitionNode m_myTransition = null;
        private List<StepStateMachine> m_predecessors;
        private List<StepStateMachine> m_successors;
        private ExecutableCondition m_executableCondition = null;
        private TimeSpan m_scanningPeriod = ExecutionEngineConfiguration.DEFAULT_SCANNING_PERIOD;
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PfcTransitionStateMachine");
        #endregion

        public TransitionStateMachine(IPfcTransitionNode myTransition) {
            m_myTransition = myTransition;
            m_predecessors = new List<StepStateMachine>();
            m_successors = new List<StepStateMachine>();
        }

        public TimeSpan ScanningPeriod { get { return m_scanningPeriod; } set { m_scanningPeriod = value; } }

        public TransitionState GetState(PfcExecutionContext pfcec) {
            if (pfcec.IsStepCentric) {
                pfcec = (PfcExecutionContext)pfcec.Parent;
            }
            return GetTsmData(pfcec).State;
        }

        public IPfcTransitionNode MyTransition { get { return m_myTransition; } internal set { m_myTransition = value; } }

        public List<StepStateMachine> PredecessorStateMachines { get { return m_predecessors; } }

        public List<StepStateMachine> SuccessorStateMachines { get { return m_successors; } }

        internal void PredecessorStateChange(PfcExecutionContext pfcec) {

            if (pfcec.IsStepCentric) {
                pfcec = (PfcExecutionContext)pfcec.Parent;
            } else {
                Debugger.Break(); // Only step-centrics should call this.
            }

            switch (GetState(pfcec)) {

                case TransitionState.Active:
                    if (AnyPredIsQuiescent(pfcec)) {
                        SetState(TransitionState.NotBeingEvaluated, pfcec);
                        HaltConditionScanning(pfcec);
                    } else if (AllPredsAreIdle(pfcec)) {
                        SetState(TransitionState.Inactive, pfcec);
                        HaltConditionScanning(pfcec);
                    }

                    break;

                case TransitionState.Inactive:
                    if (AllPredsAreNotIdle(pfcec)) {
                        if (NoPredIsQuiescent(pfcec)) {
                            SetState(TransitionState.Active, pfcec);
                            StartConditionScanning(pfcec);
                        } else {
                            SetState(TransitionState.NotBeingEvaluated, pfcec);
                            HaltConditionScanning(pfcec);
                        }
                    }
                    break;

                case TransitionState.NotBeingEvaluated:
                    if (NoPredIsQuiescent(pfcec)) {
                        SetState(TransitionState.Active, pfcec);
                        StartConditionScanning(pfcec);
                    }
                    break;

                default:
                    break;
            }
        }

        public event TransitionStateMachineEvent TransitionStateChanged;

        private TsmData GetTsmData(PfcExecutionContext parentPfcec) {
            Debug.Assert(!parentPfcec.IsStepCentric); // State is stored in the parent of the trans, a PFC.
            if (!parentPfcec.Contains(this)) {
                parentPfcec.Add(this, new TsmData());
            }
            return (TsmData)parentPfcec[this];
        }

        private void SetState(TransitionState transitionState, PfcExecutionContext parentPfcec) {
            if (SuccessorStateMachines.Count == 0 && transitionState == TransitionState.Inactive) {
                ((ProcedureFunctionChart)m_myTransition.Parent).FirePfcCompleting(parentPfcec);
            }
            Debug.Assert(!parentPfcec.IsStepCentric); // State is stored in the parent of the trans, a PFC.
            TsmData tsmData = GetTsmData(parentPfcec);
            if (tsmData.State != transitionState) {
                tsmData.State = transitionState;
                if (TransitionStateChanged != null) {
                    TransitionStateChanged(this, parentPfcec);
                }
            }
        }

        #region Condition Scanning
        private void StartConditionScanning(PfcExecutionContext pfcec) {
            if (s_diagnostics) {
                Console.WriteLine("Starting condition-scanning on transition " + m_myTransition.Name + " in EC " + pfcec.Name + ".");
            }
            HaltConditionScanning(pfcec);
            IExecutive exec = m_myTransition.Model.Executive;
            TsmData tsmData = GetTsmData(pfcec);
            tsmData.NextExpressionEvaluation = exec.RequestEvent(new ExecEventReceiver(EvaluateCondition), exec.Now + m_scanningPeriod, 0.0, pfcec, ExecEventType.Synchronous);
        }

        private void HaltConditionScanning(PfcExecutionContext pfcec) {
            TsmData tsmData = GetTsmData(pfcec);
            if (tsmData.NextExpressionEvaluation != 0L) {
                m_myTransition.Model.Executive.UnRequestEvent(tsmData.NextExpressionEvaluation);
                tsmData.NextExpressionEvaluation = 0L;
            }
        }

        private void EvaluateCondition(IExecutive exec, object userData) {
            PfcExecutionContext pfcec = (PfcExecutionContext)userData;
            TsmData tsmData = GetTsmData(pfcec);
            tsmData.NextExpressionEvaluation = 0L;
            if (tsmData.State == TransitionState.Active && ExecutableCondition(pfcec, this)) {
                PredecessorStateMachines.ForEach(delegate(StepStateMachine ssm) {
                    if (ssm.GetState(pfcec) != StepState.Complete) {
                        ssm.Stop(pfcec);
                    }
                    ssm.Reset(pfcec);
                });
                // When the last predecessor goes to Idle, I will go to Inactive.
                Debug.Assert(AllPredsAreIdle(pfcec));
                Debug.Assert(tsmData.State == TransitionState.Inactive);
                if (s_diagnostics) {
                    Console.WriteLine("Done condition-scanning on transition " + m_myTransition.Name + " in EC " + pfcec.Name + ".");
                }
                SuccessorStateMachines.ForEach(delegate(StepStateMachine ssm) { RunSuccessor(ssm, pfcec); });
            } else {
                // Either I'm NotBeingEvaluated, or the evaluation came out false.
                // NOTE: Must halt event stream when "NotBeingEvaluated".
                tsmData.NextExpressionEvaluation = exec.RequestEvent(new ExecEventReceiver(EvaluateCondition), exec.Now + m_scanningPeriod, 0.0, pfcec, ExecEventType.Synchronous);
            }
        }

        private void RunSuccessor(StepStateMachine ssm, IDictionary graphContext) {
            m_myTransition.Model.Executive.RequestEvent(new ExecEventReceiver(_RunSuccessor), m_myTransition.Model.Executive.Now, 0.0, new object[] { ssm, graphContext }, ExecEventType.Detachable);
        }

        private void _RunSuccessor(IExecutive exec, object userData) {
            StepStateMachine ssm = ( (object[])userData )[0] as StepStateMachine;
            PfcExecutionContext parentPfcec = ( (object[])userData )[1] as PfcExecutionContext;

            Debug.Assert(!parentPfcec.IsStepCentric);
            ssm.Start(parentPfcec);// Must run ones' successor in the context of out parent, not the predecessor step.
        }

        /// <summary>
        /// Gets or sets the executable condition, the executable condition that this transition will evaluate.
        /// </summary>
        /// <value>The executable condition.</value>
        public ExecutableCondition ExecutableCondition {
            get {
                if (m_executableCondition != null) {
                    return m_executableCondition;
                } else {
                    return m_myTransition.ExpressionExecutable;
                }
            }
            set {
                m_executableCondition = value;
            }
        }
        #endregion

        #region PredecessorAssessment Methods

        private bool AnyPredIsQuiescent(PfcExecutionContext parentPfcec) {
            bool anyPredIsQuiescent = false;
            PredecessorStateMachines.ForEach(delegate(StepStateMachine ssm) { anyPredIsQuiescent |= ssm.IsInQuiescentState(parentPfcec); });
            return anyPredIsQuiescent;
        }

        private bool NoPredIsQuiescent(PfcExecutionContext parentPfcec) {
            return PredecessorStateMachines.TrueForAll(delegate(StepStateMachine ssm) { return !ssm.IsInQuiescentState(parentPfcec); });
        }

        private bool AllPredsAreIdle(PfcExecutionContext parentPfcec) {
            return PredecessorStateMachines.TrueForAll(delegate(StepStateMachine ssm) { return ssm.GetState(parentPfcec) == StepState.Idle; });
        }

        private bool AllPredsAreNotIdle(PfcExecutionContext parentPfcec) {
            return PredecessorStateMachines.TrueForAll(delegate(StepStateMachine ssm) { return ssm.GetState(parentPfcec) != StepState.Idle; });
        }

        #endregion

        /// <summary>
        /// Gets the name of the transition this state machine will execute.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return m_myTransition.Name; } }

    }
}
