/* This source code licensed under the GNU Affero General Public License */
#if NYRFPT
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using System.Text;
using Highpoint.Sage.Utility;
using Highpoint.Sage.Graphs.PFC.Execution.Actions;

namespace Highpoint.Sage.Graphs.PFC.Execution {

    [TestClass]
    public class ExecutablePfcExtensionTester {

        [TestInitialize]
        public void Init() { }
        [TestCleanup]
        public void destroy() {
            Debug.WriteLine("Done.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PfcCreationTester"/> class.
        /// </summary>
        public ExecutablePfcExtensionTester() { }

        /// <summary>
        ///
        /// </summary>
        [TestMethod]
        public void TestSequencers() {

            Model model = new Model("MyTestModel");

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "RootPfc");
            pfc.ExecutionEngineConfiguration = new ExecutionEngineConfiguration();

            //                 Start 
            //                   |
            //                   + T_Start
            //                   |
            // =====================================
            //   |       |       |       |       |
            // Step0   Step1   Step2   Step3   Step4 
            //   |       |       |       |       |
            // =====================================
            //                   |
            //                   + Finis
            //
            // (We want to run these in order "Step3", "Step1", "Step4", "Step2", "Step0")

            #region Create PFC
            IPfcStepNode start = pfc.CreateStep("Start", null, Guid.NewGuid());
            IPfcTransitionNode startTrans = pfc.CreateTransition("T_Start", null, Guid.NewGuid());
            pfc.Bind(start, startTrans);
            IPfcTransitionNode finis = pfc.CreateTransition("Finish", null, Guid.NewGuid());

            for (int i = 0 ; i < 5 ; i++) {
                IPfcStepNode step = pfc.CreateStep("Step" + i, null, Guid.NewGuid());
                pfc.Bind(startTrans, step);
                pfc.Bind(step, finis);
            }
            #endregion Create PFC


            Guid sequencerKey = Guid.NewGuid();
            string[] stepSeq = new string[] { "Step4", "Step3", "Step2", "Step1", "Step0" };

            for (int n = 0 ; n < 10 ; n++) {

                Console.WriteLine("\r\n\r\n========================================================\r\nStarting test iteration # " + n + ":\r\n");

                stepSeq = Shuffle(stepSeq);
                Console.WriteLine("Expecting sequence " + StringOperations.ToCommasAndAndedList(new List<string>(stepSeq)));

                int j = 0;
                StringBuilder sb = new StringBuilder();
                foreach (string stepNodeName in stepSeq) {
                    IPfcStepNode step = pfc.Steps[stepNodeName];
                    step.Precondition = new Sequencer(sequencerKey, j++).Precondition;
                    step.LeafLevelAction = new PfcAction(delegate(PfcExecutionContext pfcec, StepStateMachine ssm) { sb.Append("Running " + ssm.MyStep.Name + " "); });
                }

                model.Executive.RequestEvent(
                    new ExecEventReceiver(pfc.Run), 
                    DateTime.MinValue, 
                    0.0, 
                    new PfcExecutionContext(pfc, "PFCEC", null, Guid.NewGuid(), null),ExecEventType.Detachable);

                pfc.Model.Start();

                Console.Out.Flush();

                string tgtString = string.Format(@"Running {0} Running {1} Running {2} Running {3} Running {4} ",
                    stepSeq[0], stepSeq[1], stepSeq[2], stepSeq[3], stepSeq[4]);

                System.Diagnostics.Debug.Assert(sb.ToString().Equals(tgtString));

                pfc.Model.Executive.Reset();

                Console.WriteLine("========================================================");
            }
        }

        private string[] Shuffle(string[] stepSeq) {
            Random r = new Random();
            for (int i = 0 ; i < 10 ; i++) {
                int a = r.Next(0, 4);
                int b = a;
                while (b == a) {
                    b = r.Next(0, 4);
                }
                string tmp = stepSeq[a];
                stepSeq[a] = stepSeq[b];
                stepSeq[b] = tmp;
            }
            return stepSeq;
        }
    }
}
#endif