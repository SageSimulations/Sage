/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs.PFC.Execution.Actions {
    public class SimpleDelay : PfcActor {

        private ITimeSpanDistribution m_tsd;

        public SimpleDelay(IModel model, string name, Guid guid, IPfcStepNode myStepNode, ITimeSpanDistribution tsd)
        :base(model,name,guid,myStepNode){
            m_tsd = tsd;
        }

        public override void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm) { }

        public override void Run(PfcExecutionContext pfcec, StepStateMachine ssm) {
            IExecutive exec = Model.Executive;
            exec.CurrentEventController.SuspendUntil(exec.Now + m_tsd.GetNext());
        }


        public override void SetStochasticMode(StochasticMode mode) {
            switch (mode) {
                case StochasticMode.Full:
                    m_tsd.SetCDFInterval(0.0,1.0);
                    break;
                case StochasticMode.Schedule:
                    m_tsd.SetCDFInterval(0.5,0.5);
                    break;
                default:
                    throw new ApplicationException("Unknown stochastic mode " + mode);
#pragma warning disable 0162 // Unreachable Code Detected
                    // Pragma is because if everything is okay, this code *will* be unreachable.
                    break;
#pragma warning disable 0162
            }
        }
    }
}
