/* This source code licensed under the GNU Affero General Public License */
#if NYRFPT 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using System.Text;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Graphs.PFC.Execution {

    [TestClass]
    public class ExecutablePfcTester {

        [TestInitialize]
        public void Init() { }
        [TestCleanup]
        public void destroy() {
            Debug.WriteLine("Done.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PfcCreationTester"/> class.
        /// </summary>
        public ExecutablePfcTester() { }

        /// <summary>
        ///
        /// </summary>
        [TestMethod]
        public void TestSmallLoopback() {
            Assert.Fail();
            Model model = new Model("MyTestModel");

            IProcedureFunctionChart pfc = CreatePfc(model, "RootPfc", 1.0, new ExecutionEngineConfiguration());

            Hashtable ht = new Hashtable();
            ht.Add("StringBuilder", new StringBuilder());
            pfc.Model.Executive.RequestEvent(new ExecEventReceiver(pfc.Run), DateTime.MinValue, 0.0, ht, ExecEventType.Detachable);

            pfc.Model.Start();

            int resultHashCode = ht["StringBuilder"].ToString().GetHashCode();
            System.Diagnostics.Debug.Assert(resultHashCode == 1263719327, "Result value did not match expected value.");


            Console.WriteLine(ht["StringBuilder"]);

        }

        /// <summary>
        ///
        /// </summary>
        [TestMethod]
        public void TestSmallLoopbackHierarchical() {
            DateTime start = DateTime.Now;
            Model model = new Model("MyTestModel");

            ExecutionEngineConfiguration eec = new ExecutionEngineConfiguration();
            eec.ScanningPeriod = TimeSpan.FromSeconds(60.0);

            IProcedureFunctionChart pfc = CreatePfc(model, "RootPfc", 15.0, eec);
            IProcedureFunctionChart pfcChild1 = CreatePfc(model, "Step1Child", 15.0, eec);
            ( (PfcStep)pfc.Nodes["RootPfc" + "Step1"] ).AddAction("Alice", pfcChild1);
            IProcedureFunctionChart pfcGrandChild1 = CreatePfc(model, "Step1GrandChild", 15.0, eec);
            ( (PfcStep)pfcChild1.Nodes["Step1Child" + "Step1"] ).AddAction("Bob", pfcGrandChild1);

            PfcExecutionContext dictionary = new PfcExecutionContext(pfc, "MyPfcExecutionContext", "", Guid.NewGuid(), null);

            dictionary.Add("StringBuilder", new StringBuilder());
            pfc.Model.Executive.RequestEvent(new ExecEventReceiver(pfc.Run), DateTime.MinValue, 0.0, dictionary, ExecEventType.Detachable);

            pfc.Model.Start();

            int resultHashCode = dictionary["StringBuilder"].ToString().GetHashCode();

            foreach (object obj in dictionary.Values) {
                if (obj is PfcExecutionContext) {
                    Console.WriteLine(obj.ToString() + " contains " +((PfcExecutionContext)obj).Values.Count +" members.");
                }
            }

            Console.WriteLine(dictionary["StringBuilder"]);
            DateTime finish = DateTime.Now;
            Console.WriteLine("The test took " + ( (TimeSpan)( finish - start ) ));
        }

        private static int nECs = 0;
        private static int nSteps = 100000;
        private static int nAvgKids = 50;
        [TestMethod]
        public void TestCreationOfABazillionEECs() {
            Assert.Fail();

            DateTime start = DateTime.Now;
            ExecutionEngineConfiguration eec = new ExecutionEngineConfiguration();
            IProcedureFunctionChart pfc = CreatePfc(new Model(), "RootPfc", 15.0, eec);
            PfcExecutionContext pfcec = new PfcExecutionContext(pfc, "MyPfcExecutionContext_0", "", Guid.NewGuid(), null);

            while (nECs < nSteps) {
                Propagate(pfc, pfcec);
            }

            DateTime finish = DateTime.Now;
            Console.WriteLine("The test took " + ( (TimeSpan)( finish - start ) ) + " to create " + nECs + " execution contexts.");
        }

        private void Propagate(IProcedureFunctionChart pfc, PfcExecutionContext pfcec) {
            List<PfcExecutionContext> readyToProcreate = new List<PfcExecutionContext>();
            foreach (PfcExecutionContext node in pfcec.DescendantNodesDepthFirst(true)) {
                if (node.Children.Count() == 0) {
                    readyToProcreate.Add(node);
                }
            }
            foreach (PfcExecutionContext kid in readyToProcreate) {
                for (int i = 1 ; i < nAvgKids && nECs < nSteps ; i++) {
                    PfcExecutionContext kidEC = new PfcExecutionContext(pfc, "MyPfcExecutionContext_" + ( nECs++ ), "", Guid.NewGuid(), pfcec);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        [TestMethod]
        public void TestScheduleHierarchical() {/*
            DateTime start = DateTime.Now;
            Model model = new Model("MyTestModel");

            ExecutionEngineConfiguration eec = new ExecutionEngineConfiguration();
            eec.ScanningPeriod = TimeSpan.FromSeconds(10.0);

            IProcedureFunctionChart sked = CreateSchedulePfc(model, "Schedule", 15.0, eec);
            IProcedureFunctionChart campaign = CreateCampaignPfc(model, "MakeMuffins", 15.0, eec);
            IProcedureFunctionChart recipe1 = CreateRecipePfc(model, "MegaMuffins_V2", 15.0, eec);
            IProcedureFunctionChart recipe2 = CreateRecipePfc(model, "UltraMuffins", 15.0, eec);
            for (int i = 0 ; i < nCampaigns ; i++) {
                string campaignName = string.Format("C_{0:D3}", i);
                ( (PfcStep)sked.Nodes["Campaigns"] ).AddAction(campaignName, campaign);
            }
            ( (PfcStep)pfcChild1.Nodes["Step1"] ).AddAction("Bob", pfcGrandChild1);

            Hashtable ht = new Hashtable();
            ht.Add("StringBuilder", new StringBuilder());
            pfc.Model.Executive.RequestEvent(new ExecEventReceiver(pfc.Run), DateTime.MinValue, 0.0, ht, ExecEventType.Detachable);

            pfc.Model.Start();

            int resultHashCode = ht["StringBuilder"].ToString().GetHashCode();
            System.Diagnostics.Debug.Assert(resultHashCode == -430794099, "Result value did not match expected value.");

            Console.WriteLine(ht["StringBuilder"]);
            DateTime finish = DateTime.Now;
            Console.WriteLine("The test took " + ( (TimeSpan)( finish - start ) ));
        */
        }

        private IProcedureFunctionChart CreatePfc(IModel model, string pfcName, double minutesPerTask, ExecutionEngineConfiguration eec) {
            //    Start
            //      |
            //      +T1   ----  
            //      |     |  |
            //      -------  |
            //         |     |
            //       Step1   |
            //         |     |
            //         +T2   |
            //         |     |
            //       Step2   |
            //         |     |
            //      -------  |
            //     T3+   +T4 |
            //       |   |---
            //    Finish
            //       |
            //     T5+
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, pfcName);
            pfc.ExecutionEngineConfiguration = eec;
            pfc.CreateStep(pfcName+"Start", "", Guid.NewGuid());
            pfc.CreateStep(pfcName + "Step1", "", Guid.NewGuid());
            pfc.CreateStep(pfcName + "Step2", "", Guid.NewGuid());
            pfc.CreateStep(pfcName + "Finish", "", Guid.NewGuid());
            pfc.CreateTransition(pfcName + "T1", "", Guid.NewGuid());
            pfc.CreateTransition(pfcName + "T2", "", Guid.NewGuid());
            pfc.CreateTransition(pfcName + "T3", "", Guid.NewGuid());
            pfc.CreateTransition(pfcName + "T4", "", Guid.NewGuid());
            pfc.CreateTransition(pfcName + "T5", "", Guid.NewGuid());
            pfc.Bind(pfc.Nodes[pfcName + "Start"], pfc.Nodes[pfcName + "T1"]);
            pfc.Bind(pfc.Nodes[pfcName + "T1"], pfc.Nodes[pfcName + "Step1"]);
            pfc.Bind(pfc.Nodes[pfcName + "Step1"], pfc.Nodes[pfcName + "T2"]);
            pfc.Bind(pfc.Nodes[pfcName + "T2"], pfc.Nodes[pfcName + "Step2"]);
            pfc.Bind(pfc.Nodes[pfcName + "Step2"], pfc.Nodes[pfcName + "T3"]);
            pfc.Bind(pfc.Nodes[pfcName + "Step2"], pfc.Nodes[pfcName + "T4"]);
            pfc.Bind(pfc.Nodes[pfcName + "T4"], pfc.Nodes[pfcName + "Step1"]);
            pfc.Bind(pfc.Nodes[pfcName + "T3"], pfc.Nodes[pfcName + "Finish"]);
            pfc.Bind(pfc.Nodes[pfcName + "Finish"], pfc.Nodes[pfcName + "T5"]);

            pfc.Steps.ForEach(delegate(IPfcStepNode psn) {
                psn.LeafLevelAction = new PfcAction(delegate(PfcExecutionContext pfcec, StepStateMachine ssm) {
                    StringBuilder sb = (StringBuilder)pfcec.Root.Payload["StringBuilder"];
                    string stepName = pfc.Name + "." + psn.Name;
                    IExecutive exec = psn.Model.Executive;
                    sb.AppendLine(string.Format("{0} : {1} is running its intrinsic action.", exec.Now, stepName));
                    exec.CurrentEventController.SuspendUntil(exec.Now + TimeSpan.FromMinutes(minutesPerTask));
                });
            });

            pfc.Transitions[pfcName + "T1"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    PfcExecutionContext execContext = (PfcExecutionContext)userData;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    if (!execContext.Contains(countKey)) {
                        execContext.Add(countKey, 1);
                    } else {
                        execContext[countKey] = 1;
                    }
                    return DEFAULT_EXECUTABLE_EXPRESSION(execContext, tsm);
                });

            pfc.Transitions[pfcName + "T3"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    PfcExecutionContext execContext = (PfcExecutionContext)userData;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    return ( DEFAULT_EXECUTABLE_EXPRESSION(execContext, tsm) && ( ( (int)execContext[countKey] ) >= 5 ) );
                });

            pfc.Transitions[pfcName + "T4"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    PfcExecutionContext execContext = (PfcExecutionContext)userData;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    if (( DEFAULT_EXECUTABLE_EXPRESSION(execContext, tsm) && ( ( (int)execContext[countKey] ) < 5 ) )) {
                        execContext[countKey] = ( (int)execContext[countKey] ) + 1;
                        return true;
                    } else {
                        return false;
                    }
                });

            pfc.UpdateStructure();

            return pfc;
        }

        private IProcedureFunctionChart CreateSchedulePfc(IModel model, string pfcName, double minutesPerTask, ExecutionEngineConfiguration eec) {
            //       Start
            //         |
            //         + T1 
            //         |
            //     Campaigns
            //         |
            //         + T2
            //
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, pfcName);
            pfc.ExecutionEngineConfiguration = eec;
            pfc.CreateStep("Start", "", Guid.NewGuid());
            pfc.CreateStep("Campaigns", "", Guid.NewGuid());
            pfc.CreateTransition("T1", "", Guid.NewGuid());
            pfc.CreateTransition("T2", "", Guid.NewGuid());
            pfc.Bind(pfc.Nodes["Start"], pfc.Nodes["T1"]);
            pfc.Bind(pfc.Nodes["T1"], pfc.Nodes["Campaigns"]);
            pfc.Bind(pfc.Nodes["Campaigns"], pfc.Nodes["T2"]);

            pfc.UpdateStructure();

            return pfc;
        }

        private IProcedureFunctionChart CreateCampaignPfc(IModel model, string pfcName, double minutesPerTask, ExecutionEngineConfiguration eec) {
            //    Start
            //      |
            //      +T1   ----  
            //      |     |  |
            //      -------  |
            //         |     |
            //       Step1   |
            //         |     |
            //         +T2   |
            //         |     |
            //       Step2   |
            //         |     |
            //      -------  |
            //     T3+   +T4 |
            //       |   |---
            //    Finish
            //       |
            //     T5+
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, pfcName);
            pfc.ExecutionEngineConfiguration = eec;
            pfc.CreateStep("Start", "", Guid.NewGuid());
            pfc.CreateStep("Step1", "", Guid.NewGuid());
            pfc.CreateStep("Step2", "", Guid.NewGuid());
            pfc.CreateStep("Finish", "", Guid.NewGuid());
            pfc.CreateTransition("T1", "", Guid.NewGuid());
            pfc.CreateTransition("T2", "", Guid.NewGuid());
            pfc.CreateTransition("T3", "", Guid.NewGuid());
            pfc.CreateTransition("T4", "", Guid.NewGuid());
            pfc.CreateTransition("T5", "", Guid.NewGuid());
            pfc.Bind(pfc.Nodes["Start"], pfc.Nodes["T1"]);
            pfc.Bind(pfc.Nodes["T1"], pfc.Nodes["Step1"]);
            pfc.Bind(pfc.Nodes["Step1"], pfc.Nodes["T2"]);
            pfc.Bind(pfc.Nodes["T2"], pfc.Nodes["Step2"]);
            pfc.Bind(pfc.Nodes["Step2"], pfc.Nodes["T3"]);
            pfc.Bind(pfc.Nodes["Step2"], pfc.Nodes["T4"]);
            pfc.Bind(pfc.Nodes["T4"], pfc.Nodes["Step1"]);
            pfc.Bind(pfc.Nodes["T3"], pfc.Nodes["Finish"]);
            pfc.Bind(pfc.Nodes["Finish"], pfc.Nodes["T5"]);

            pfc.Steps.ForEach(delegate(IPfcStepNode psn) {
                psn.LeafLevelAction = new PfcAction(delegate(PfcExecutionContext pfcec, StepStateMachine ssm) {
                    StringBuilder sb = (StringBuilder)pfcec["StringBuilder"];
                    string stepName = pfc.Name + "." + psn.Name;
                    IExecutive exec = psn.Model.Executive;
                    sb.AppendLine(string.Format("{0} : {1} is running its intrinsic action.", exec.Now, stepName));
                    exec.CurrentEventController.SuspendUntil(exec.Now + TimeSpan.FromMinutes(minutesPerTask));
                });
            });

            pfc.Transitions["T1"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    IDictionary graphContext = userData as IDictionary;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    if (!( (IDictionary)graphContext ).Contains(countKey)) {
                        ( (IDictionary)graphContext ).Add(countKey, 1);
                    } else {
                        graphContext[countKey] = 1;
                    }
                    return DEFAULT_EXECUTABLE_EXPRESSION(graphContext, tsm);
                });

            pfc.Transitions["T3"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    IDictionary graphContext = userData as IDictionary;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    return ( DEFAULT_EXECUTABLE_EXPRESSION(graphContext, tsm) && ( ( (int)graphContext[countKey] ) > 5 ) );
                });

            pfc.Transitions["T4"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    IDictionary graphContext = userData as IDictionary;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    if (( DEFAULT_EXECUTABLE_EXPRESSION(graphContext, tsm) && ( ( (int)graphContext[countKey] ) <= 5 ) )) {
                        graphContext[countKey] = ( (int)graphContext[countKey] ) + 1;
                        return true;
                    } else {
                        return false;
                    }
                });

            pfc.UpdateStructure();

            return pfc;
        }

        private IProcedureFunctionChart CreateRecipePfc(IModel model, string pfcName, double minutesPerTask, ExecutionEngineConfiguration eec) {
            //    Start
            //      |
            //      +T1   ----  
            //      |     |  |
            //      -------  |
            //         |     |
            //       Step1   |
            //         |     |
            //         +T2   |
            //         |     |
            //       Step2   |
            //         |     |
            //      -------  |
            //     T3+   +T4 |
            //       |   |---
            //    Finish
            //       |
            //     T5+
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, pfcName);
            pfc.ExecutionEngineConfiguration = eec;
            pfc.CreateStep("Start", "", Guid.NewGuid());
            pfc.CreateStep("Step1", "", Guid.NewGuid());
            pfc.CreateStep("Step2", "", Guid.NewGuid());
            pfc.CreateStep("Finish", "", Guid.NewGuid());
            pfc.CreateTransition("T1", "", Guid.NewGuid());
            pfc.CreateTransition("T2", "", Guid.NewGuid());
            pfc.CreateTransition("T3", "", Guid.NewGuid());
            pfc.CreateTransition("T4", "", Guid.NewGuid());
            pfc.CreateTransition("T5", "", Guid.NewGuid());
            pfc.Bind(pfc.Nodes["Start"], pfc.Nodes["T1"]);
            pfc.Bind(pfc.Nodes["T1"], pfc.Nodes["Step1"]);
            pfc.Bind(pfc.Nodes["Step1"], pfc.Nodes["T2"]);
            pfc.Bind(pfc.Nodes["T2"], pfc.Nodes["Step2"]);
            pfc.Bind(pfc.Nodes["Step2"], pfc.Nodes["T3"]);
            pfc.Bind(pfc.Nodes["Step2"], pfc.Nodes["T4"]);
            pfc.Bind(pfc.Nodes["T4"], pfc.Nodes["Step1"]);
            pfc.Bind(pfc.Nodes["T3"], pfc.Nodes["Finish"]);
            pfc.Bind(pfc.Nodes["Finish"], pfc.Nodes["T5"]);

            pfc.Steps.ForEach(delegate(IPfcStepNode psn) {
                psn.LeafLevelAction = new PfcAction(delegate(PfcExecutionContext pfcec, StepStateMachine ssm) {
                    StringBuilder sb = (StringBuilder)pfcec["StringBuilder"];
                    string stepName = pfc.Name + "." + psn.Name;
                    IExecutive exec = psn.Model.Executive;
                    sb.AppendLine(string.Format("{0} : {1} is running its intrinsic action.", exec.Now, stepName));
                    exec.CurrentEventController.SuspendUntil(exec.Now + TimeSpan.FromMinutes(minutesPerTask));
                });
            });

            pfc.Transitions["T1"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    IDictionary graphContext = userData as IDictionary;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    if (!( (IDictionary)graphContext ).Contains(countKey)) {
                        ( (IDictionary)graphContext ).Add(countKey, 1);
                    } else {
                        graphContext[countKey] = 1;
                    }
                    return DEFAULT_EXECUTABLE_EXPRESSION(graphContext, tsm);
                });

            pfc.Transitions["T3"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    IDictionary graphContext = userData as IDictionary;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    return ( DEFAULT_EXECUTABLE_EXPRESSION(graphContext, tsm) && ( ( (int)graphContext[countKey] ) > 5 ) );
                });

            pfc.Transitions["T4"].ExpressionExecutable
                = new Highpoint.Sage.Graphs.PFC.Execution.ExecutableCondition(
                delegate(object userData, Highpoint.Sage.Graphs.PFC.Execution.TransitionStateMachine tsm) {
                    IDictionary graphContext = userData as IDictionary;
                    string countKey = pfc.Guid.ToString() + ".Count";
                    if (( DEFAULT_EXECUTABLE_EXPRESSION(graphContext, tsm) && ( ( (int)graphContext[countKey] ) <= 5 ) )) {
                        graphContext[countKey] = ( (int)graphContext[countKey] ) + 1;
                        return true;
                    } else {
                        return false;
                    }
                });

            pfc.UpdateStructure();

            return pfc;
        }

        private static bool DEFAULT_EXECUTABLE_EXPRESSION(IDictionary execContext, TransitionStateMachine tsm)
        {
            return true;
        }
    }
}
#endif