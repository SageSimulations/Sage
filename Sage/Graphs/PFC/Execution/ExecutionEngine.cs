/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using Highpoint.Sage.SimCore;
using System.Collections;

namespace Highpoint.Sage.Graphs.PFC.Execution {

    public class ExecutionEngineConfiguration {

        public static readonly TimeSpan DEFAULT_SCANNING_PERIOD = TimeSpan.FromMinutes(1.0);
        public static readonly bool DEFAULT_STRUCTURE_LOCKING = true;
        public static readonly int DEFAULT_ = 1;

        public ExecutionEngineConfiguration() {}

        public ExecutionEngineConfiguration(TimeSpan scanningPeriod) {
            ScanningPeriod = scanningPeriod;
        }

        public TimeSpan ScanningPeriod { get; set; } = DEFAULT_SCANNING_PERIOD;
        public bool StructureLockedDuringRun { get; set; } = DEFAULT_STRUCTURE_LOCKING;
    }

    internal class ExecutionEngine {

        #region Private Fields
        private StepStateMachine m_startStep;
        private IModel m_model;
        private Dictionary<IPfcStepNode, StepStateMachine> m_stepStateMachines;
        private Dictionary<IPfcTransitionNode, TransitionStateMachine> m_transitionStateMachines;
        private ExecutionEngineConfiguration m_executionEngineConfiguration;
        #endregion Private Fields

        public ExecutionEngine(IProcedureFunctionChart pfc) : this(pfc, new ExecutionEngineConfiguration()){}

        public ExecutionEngine(IProcedureFunctionChart pfc, ExecutionEngineConfiguration eec) {
            m_executionEngineConfiguration = eec;
            m_model = pfc.Model;
            m_stepStateMachines = new Dictionary<IPfcStepNode, StepStateMachine>();
            m_transitionStateMachines = new Dictionary<IPfcTransitionNode, TransitionStateMachine>();

            foreach (IPfcStepNode pfcStepNode in pfc.Steps) {
                StepStateMachine ssm = new StepStateMachine(pfcStepNode);
                ssm.StructureLocked = m_executionEngineConfiguration.StructureLockedDuringRun;
                m_stepStateMachines.Add(pfcStepNode,ssm);
                ssm.MyStep = pfcStepNode;
                ((PfcStep)pfcStepNode).MyStepStateMachine = ssm;
            }

            foreach (IPfcTransitionNode pfcTransNode in pfc.Transitions) {
                TransitionStateMachine tsm = new TransitionStateMachine(pfcTransNode);
                tsm.ScanningPeriod = m_executionEngineConfiguration.ScanningPeriod;
                m_transitionStateMachines.Add(pfcTransNode, tsm);
                tsm.MyTransition = pfcTransNode;
                ((PfcTransition)pfcTransNode).MyTransitionStateMachine = tsm;
            }

            StepStateMachineEvent ssme = new StepStateMachineEvent(anSSM_StepStateChanged);
            foreach (IPfcStepNode step in pfc.Steps) {
                step.MyStepStateMachine.StepStateChanged += ssme;
                foreach (IPfcTransitionNode transNode in step.SuccessorNodes) {
                    step.MyStepStateMachine.SuccessorStateMachines.Add(transNode.MyTransitionStateMachine);
                }
                if (step.MyStepStateMachine.SuccessorStateMachines.Count == 0) {
                    string message =
                        $"Step {step.Name} in PFC {step.Parent.Name} has no successor transition. A PFC must end with a termination transition. (Did you acquire an Execution Engine while the Pfc was still under construction?)";
                    throw new ApplicationException(message);
                }
            }

            TransitionStateMachineEvent tsme = new TransitionStateMachineEvent(aTSM_TransitionStateChanged);
            foreach (IPfcTransitionNode trans in pfc.Transitions) {
                TransitionStateMachine thisTsm = m_transitionStateMachines[trans];
                thisTsm.TransitionStateChanged += tsme;
                foreach (IPfcStepNode stepNode in trans.SuccessorNodes) {
                    thisTsm.SuccessorStateMachines.Add(m_stepStateMachines[stepNode]);
                }
                foreach (IPfcStepNode stepNode in trans.PredecessorNodes) {
                    thisTsm.PredecessorStateMachines.Add(m_stepStateMachines[stepNode]);
                }
            }

            List<IPfcStepNode> startSteps = pfc.GetStartSteps();
            System.Diagnostics.Debug.Assert(startSteps.Count == 1);
            m_startStep = m_stepStateMachines[startSteps[0]];
        }

        void aTSM_TransitionStateChanged(TransitionStateMachine tsm, object userData )
        {
            TransitionStateChanged?.Invoke(tsm, userData);
        }

        void anSSM_StepStateChanged(StepStateMachine ssm, object userData)
        {
            StepStateChanged?.Invoke(ssm, userData);
        }

        /// <summary>
        /// Runs this execution engine's PFC. If this is not called by a detachable event, it calls back for a new
        /// execEvent, on a detachable event controller.
        /// </summary>
        /// <param name="exec">The exec.</param>
        /// <param name="userData">The user data.</param>
        public void Run(IExecutive exec, object userData) {

            if (exec.CurrentEventType != ExecEventType.Detachable) {
                m_model.Executive.RequestEvent(
                    delegate(IExecutive exec1, object userData1) { Run(exec, (IDictionary)userData1); }, exec.Now, exec.CurrentPriorityLevel, userData, ExecEventType.Detachable);
            } else {
                // We already got this permission as a part of the permission to start the PFC.
                //m_startStep.GetStartPermission((IDictionary)userData);
                m_startStep.Start((PfcExecutionContext)userData);
            }
        }

        public StepStateMachine StateMachineForStep(IPfcStepNode step) {
            return m_stepStateMachines[step];
        }

        public TransitionStateMachine StateMachineForTransition(IPfcTransitionNode trans) {
            return m_transitionStateMachines[trans];
        }

        public event StepStateMachineEvent StepStateChanged;
        public event TransitionStateMachineEvent TransitionStateChanged; 

    }
}
