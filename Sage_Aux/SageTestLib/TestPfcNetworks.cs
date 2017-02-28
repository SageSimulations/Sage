/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using System.Xml;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Graphs.PFC {

    [TestClass]
    public class PFCGraphTester {

        private enum LinkSuperType { ParallelConvergent, SeriesConvergent, ParallelDivergent, SeriesDivergent }
        private bool m_runSFCs = false;
        private bool m_testSerializationToo = true;
        private string m_pfcTestFileName;

        public PFCGraphTester() {
            m_pfcTestFileName = Path.GetTempFileName();
        }

        [TestMethod]
        public void Test_SeriesConvergentStepStep() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_000}
{T_000-->[L_001(SFC 1.Root)]-->S_Edna}
{S_Bob-->[L_002(SFC 1.Root)]-->T_001}
{T_001-->[L_003(SFC 1.Root)]-->S_Edna}
{S_Charley-->[L_004(SFC 1.Root)]-->T_002}
{T_002-->[L_005(SFC 1.Root)]-->S_Edna}
{S_David-->[L_006(SFC 1.Root)]-->T_003}
{T_003-->[L_007(SFC 1.Root)]-->S_Edna}
";
            _TestComplexLink(LinkSuperType.SeriesConvergent, PfcElementType.Step, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_SeriesConvergentStepTransition() {
            string shouldBe =
@"{S_000-->[L_000(SFC 1.Root)]-->T_Edna}
{S_Alice-->[L_001(SFC 1.Root)]-->T_000}
{T_000-->[L_002(SFC 1.Root)]-->S_000}
{S_Bob-->[L_003(SFC 1.Root)]-->T_001}
{T_001-->[L_004(SFC 1.Root)]-->S_000}
{S_Charley-->[L_005(SFC 1.Root)]-->T_002}
{T_002-->[L_006(SFC 1.Root)]-->S_000}
{S_David-->[L_007(SFC 1.Root)]-->T_003}
{T_003-->[L_008(SFC 1.Root)]-->S_000}
";
            _TestComplexLink(LinkSuperType.SeriesConvergent, PfcElementType.Step, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_SeriesConvergentTransitionStep() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_Edna}
{T_Bob-->[L_001(SFC 1.Root)]-->S_Edna}
{T_Charley-->[L_002(SFC 1.Root)]-->S_Edna}
{T_David-->[L_003(SFC 1.Root)]-->S_Edna}
";
            _TestComplexLink(LinkSuperType.SeriesConvergent, PfcElementType.Transition, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_SeriesConvergentTransitionTransition() {
            string shouldBe =
@"{S_000-->[L_000(SFC 1.Root)]-->T_Edna}
{T_Alice-->[L_001(SFC 1.Root)]-->S_000}
{T_Bob-->[L_002(SFC 1.Root)]-->S_000}
{T_Charley-->[L_003(SFC 1.Root)]-->S_000}
{T_David-->[L_004(SFC 1.Root)]-->S_000}
";
            _TestComplexLink(LinkSuperType.SeriesConvergent, PfcElementType.Transition, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelConvergentStepStep() {
            string shouldBe =
@"{T_000-->[L_000(SFC 1.Root)]-->S_Edna}
{S_Alice-->[L_001(SFC 1.Root)]-->T_000}
{S_Bob-->[L_002(SFC 1.Root)]-->T_000}
{S_Charley-->[L_003(SFC 1.Root)]-->T_000}
{S_David-->[L_004(SFC 1.Root)]-->T_000}
";
            _TestComplexLink(LinkSuperType.ParallelConvergent, PfcElementType.Step, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelConvergentStepTransition() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_Edna}
{S_Bob-->[L_001(SFC 1.Root)]-->T_Edna}
{S_Charley-->[L_002(SFC 1.Root)]-->T_Edna}
{S_David-->[L_003(SFC 1.Root)]-->T_Edna}
";
            _TestComplexLink(LinkSuperType.ParallelConvergent, PfcElementType.Step, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelConvergentTransitionStep() {
            string shouldBe =
@"{T_000-->[L_000(SFC 1.Root)]-->S_Edna}
{T_Alice-->[L_001(SFC 1.Root)]-->S_000}
{S_000-->[L_002(SFC 1.Root)]-->T_000}
{T_Bob-->[L_003(SFC 1.Root)]-->S_001}
{S_001-->[L_004(SFC 1.Root)]-->T_000}
{T_Charley-->[L_005(SFC 1.Root)]-->S_002}
{S_002-->[L_006(SFC 1.Root)]-->T_000}
{T_David-->[L_007(SFC 1.Root)]-->S_003}
{S_003-->[L_008(SFC 1.Root)]-->T_000}
";
            _TestComplexLink(LinkSuperType.ParallelConvergent, PfcElementType.Transition, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelConvergentTransitionTransition() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_000}
{S_000-->[L_001(SFC 1.Root)]-->T_Edna}
{T_Bob-->[L_002(SFC 1.Root)]-->S_001}
{S_001-->[L_003(SFC 1.Root)]-->T_Edna}
{T_Charley-->[L_004(SFC 1.Root)]-->S_002}
{S_002-->[L_005(SFC 1.Root)]-->T_Edna}
{T_David-->[L_006(SFC 1.Root)]-->S_003}
{S_003-->[L_007(SFC 1.Root)]-->T_Edna}
";
            _TestComplexLink(LinkSuperType.ParallelConvergent, PfcElementType.Transition, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelDivergentStepStep() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_000}
{T_000-->[L_001(SFC 1.Root)]-->S_Bob}
{T_000-->[L_002(SFC 1.Root)]-->S_Charley}
{T_000-->[L_003(SFC 1.Root)]-->S_David}
{T_000-->[L_004(SFC 1.Root)]-->S_Edna}
";
            _TestComplexLink(LinkSuperType.ParallelDivergent, PfcElementType.Step, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelDivergentStepTransition() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_000}
{T_000-->[L_001(SFC 1.Root)]-->S_000}
{S_000-->[L_002(SFC 1.Root)]-->T_Bob}
{T_000-->[L_003(SFC 1.Root)]-->S_001}
{S_001-->[L_004(SFC 1.Root)]-->T_Charley}
{T_000-->[L_005(SFC 1.Root)]-->S_002}
{S_002-->[L_006(SFC 1.Root)]-->T_David}
{T_000-->[L_007(SFC 1.Root)]-->S_003}
{S_003-->[L_008(SFC 1.Root)]-->T_Edna}
";
            _TestComplexLink(LinkSuperType.ParallelDivergent, PfcElementType.Step, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelDivergentTransitionStep() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_Bob}
{T_Alice-->[L_001(SFC 1.Root)]-->S_Charley}
{T_Alice-->[L_002(SFC 1.Root)]-->S_David}
{T_Alice-->[L_003(SFC 1.Root)]-->S_Edna}
";
            _TestComplexLink(LinkSuperType.ParallelDivergent, PfcElementType.Transition, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_ParallelDivergentTransitionTransition() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_000}
{S_000-->[L_001(SFC 1.Root)]-->T_Bob}
{T_Alice-->[L_002(SFC 1.Root)]-->S_001}
{S_001-->[L_003(SFC 1.Root)]-->T_Charley}
{T_Alice-->[L_004(SFC 1.Root)]-->S_002}
{S_002-->[L_005(SFC 1.Root)]-->T_David}
{T_Alice-->[L_006(SFC 1.Root)]-->S_003}
{S_003-->[L_007(SFC 1.Root)]-->T_Edna}
";
            _TestComplexLink(LinkSuperType.ParallelDivergent, PfcElementType.Transition, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_SeriesDivergentStepStep() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_000}
{T_000-->[L_001(SFC 1.Root)]-->S_Bob}
{S_Alice-->[L_002(SFC 1.Root)]-->T_001}
{T_001-->[L_003(SFC 1.Root)]-->S_Charley}
{S_Alice-->[L_004(SFC 1.Root)]-->T_002}
{T_002-->[L_005(SFC 1.Root)]-->S_David}
{S_Alice-->[L_006(SFC 1.Root)]-->T_003}
{T_003-->[L_007(SFC 1.Root)]-->S_Edna}
";
            _TestComplexLink(LinkSuperType.SeriesDivergent, PfcElementType.Step, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_SeriesDivergentStepTransition() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_Bob}
{S_Alice-->[L_001(SFC 1.Root)]-->T_Charley}
{S_Alice-->[L_002(SFC 1.Root)]-->T_David}
{S_Alice-->[L_003(SFC 1.Root)]-->T_Edna}
";
            _TestComplexLink(LinkSuperType.SeriesDivergent, PfcElementType.Step, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_SeriesDivergentTransitionStep() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_000}
{S_000-->[L_001(SFC 1.Root)]-->T_000}
{T_000-->[L_002(SFC 1.Root)]-->S_Bob}
{S_000-->[L_003(SFC 1.Root)]-->T_001}
{T_001-->[L_004(SFC 1.Root)]-->S_Charley}
{S_000-->[L_005(SFC 1.Root)]-->T_002}
{T_002-->[L_006(SFC 1.Root)]-->S_David}
{S_000-->[L_007(SFC 1.Root)]-->T_003}
{T_003-->[L_008(SFC 1.Root)]-->S_Edna}
";
            _TestComplexLink(LinkSuperType.SeriesDivergent, PfcElementType.Transition, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_SeriesDivergentTransitionTransition() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_000}
{S_000-->[L_001(SFC 1.Root)]-->T_Bob}
{S_000-->[L_002(SFC 1.Root)]-->T_Charley}
{S_000-->[L_003(SFC 1.Root)]-->T_David}
{S_000-->[L_004(SFC 1.Root)]-->T_Edna}
";
            _TestComplexLink(LinkSuperType.SeriesDivergent, PfcElementType.Transition, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_SimpleBindingStepStep() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_000}
{T_000-->[L_001(SFC 1.Root)]-->S_Bob}
";
            _TestSimpleLink(PfcElementType.Step, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_SimpleBindingStepTransition() {
            string shouldBe =
@"{S_Alice-->[L_000(SFC 1.Root)]-->T_Bob}
";
            _TestSimpleLink(PfcElementType.Step, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_SimpleBindingTransitionStep() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_Bob}
";
            _TestSimpleLink(PfcElementType.Transition, PfcElementType.Step, shouldBe);

        }

        [TestMethod]
        public void Test_SimpleBindingTransitionTransition() {
            string shouldBe =
@"{T_Alice-->[L_000(SFC 1.Root)]-->S_000}
{S_000-->[L_001(SFC 1.Root)]-->T_Bob}
";
            _TestSimpleLink(PfcElementType.Transition, PfcElementType.Transition, shouldBe);

        }

        [TestMethod]
        public void Test_SynchronizerConstruct_Transitions() {
            Model model = new Model("SFC Test 1");
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "SFC 1", "", Guid.NewGuid());

            IPfcTransitionNode t0 = pfc.CreateTransition("T_Alice", "", Guid.Empty);
            IPfcTransitionNode t1 = pfc.CreateTransition("T_Bob", "", Guid.Empty);
            IPfcTransitionNode t2 = pfc.CreateTransition("T_Charley", "", Guid.Empty);
            IPfcTransitionNode t3 = pfc.CreateTransition("T_David", "", Guid.Empty);
            IPfcTransitionNode t4 = pfc.CreateTransition("T_Edna", "", Guid.Empty);
            IPfcTransitionNode t5 = pfc.CreateTransition("T_Frank", "", Guid.Empty);
            IPfcTransitionNode t6 = pfc.CreateTransition("T_Gary", "", Guid.Empty);
            IPfcTransitionNode t7 = pfc.CreateTransition("T_Hailey", "", Guid.Empty);

            pfc.Synchronize(new IPfcTransitionNode[] { t0, t1, t2, t3 }, new IPfcTransitionNode[] { t4, t5, t6, t7 });

            string structureString = PfcDiagnostics.GetStructure(pfc);
            string shouldBe = "{T_Alice-->[L_000(SFC 1.Root)]-->S_000}\r\n{S_000-->[L_001(SFC 1.Root)]-->T_000}\r\n{T_Bob-->[L_002(SFC 1.Root)]-->S_001}\r\n{S_001-->[L_003(SFC 1.Root)]-->T_000}\r\n{T_Charley-->[L_004(SFC 1.Root)]-->S_002}\r\n{S_002-->[L_005(SFC 1.Root)]-->T_000}\r\n{T_David-->[L_006(SFC 1.Root)]-->S_003}\r\n{S_003-->[L_007(SFC 1.Root)]-->T_000}\r\n{T_000-->[L_008(SFC 1.Root)]-->S_004}\r\n{S_004-->[L_009(SFC 1.Root)]-->T_Edna}\r\n{T_000-->[L_010(SFC 1.Root)]-->S_005}\r\n{S_005-->[L_011(SFC 1.Root)]-->T_Frank}\r\n{T_000-->[L_012(SFC 1.Root)]-->S_006}\r\n{S_006-->[L_013(SFC 1.Root)]-->T_Gary}\r\n{T_000-->[L_014(SFC 1.Root)]-->S_007}\r\n{S_007-->[L_015(SFC 1.Root)]-->T_Hailey}\r\n";

            Console.WriteLine("After a synchronization of transitions, structure is \r\n" + structureString);
            Assert.AreEqual(structureString, shouldBe, "Structure should have been\r\n" + shouldBe + "\r\nbut it was\r\n" + structureString + "\r\ninstead.");

            if (m_runSFCs) {

                TestEvaluator testEvaluator = new TestEvaluator(new IPfcTransitionNode[] { t0, t1, t2, t3, t4, t5, t6, t7 });
                testEvaluator.NextExpectedActivations = new IPfcTransitionNode[] { t0, t1, t2, t3, t4, t5, t6, t7 };

                foreach (IPfcNode ilinkable in new IPfcTransitionNode[] { t0, t1, t2, t3 }) {
                    Console.WriteLine("Incrementing " + ilinkable.Name + ".");
                    //ilinkable.Increment();
                    throw new ApplicationException("PFCs are not currently executable.");
                }

                testEvaluator.NextExpectedActivations = new IPfcTransitionNode[] { }; // Ensure it's empty and all have fired.

            }
        }

        [TestMethod]
        public void Test_SynchronizerConstruct_Steps() {
            Model model = new Model("SFC Test 1");
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "SFC 1", "", Guid.NewGuid());

            IPfcStepNode t0 = pfc.CreateStep("S_Alice", "", Guid.Empty);
            IPfcStepNode t1 = pfc.CreateStep("S_Bob", "", Guid.Empty);
            IPfcStepNode t2 = pfc.CreateStep("S_Charley", "", Guid.Empty);
            IPfcStepNode t3 = pfc.CreateStep("S_David", "", Guid.Empty);
            IPfcStepNode t4 = pfc.CreateStep("S_Edna", "", Guid.Empty);
            IPfcStepNode t5 = pfc.CreateStep("S_Frank", "", Guid.Empty);
            IPfcStepNode t6 = pfc.CreateStep("S_Gary", "", Guid.Empty);
            IPfcStepNode t7 = pfc.CreateStep("S_Hailey", "", Guid.Empty);

            pfc.Synchronize(new IPfcStepNode[] { t0, t1, t2, t3 }, new IPfcStepNode[] { t4, t5, t6, t7 });

            string structureString = PfcDiagnostics.GetStructure(pfc);
            structureString = structureString.Replace("SFC 1.Root", "SFC 1.Root");
            string shouldBe = "{S_Alice-->[L_000(SFC 1.Root)]-->T_000}\r\n{S_Bob-->[L_001(SFC 1.Root)]-->T_000}\r\n{S_Charley-->[L_002(SFC 1.Root)]-->T_000}\r\n{S_David-->[L_003(SFC 1.Root)]-->T_000}\r\n{T_000-->[L_004(SFC 1.Root)]-->S_Edna}\r\n{T_000-->[L_005(SFC 1.Root)]-->S_Frank}\r\n{T_000-->[L_006(SFC 1.Root)]-->S_Gary}\r\n{T_000-->[L_007(SFC 1.Root)]-->S_Hailey}\r\n";

            Console.WriteLine("After a synchronization of steps, structure is \r\n" + structureString);
            Assert.AreEqual(structureString, shouldBe, "Structure should have been\r\n" + shouldBe + "\r\nbut it was\r\n" + structureString + "\r\ninstead.");

            if (m_runSFCs) {

                TestEvaluator testEvaluator = new TestEvaluator(new IPfcStepNode[] { t0, t1, t2, t3, t4, t5, t6, t7 });
                pfc.Synchronize(new IPfcStepNode[] { t0, t1, t2, t3 }, new IPfcStepNode[] { t4, t5, t6, t7 });

                testEvaluator.NextExpectedActivations = new IPfcStepNode[] { t0, t1, t2, t3, t4, t5, t6, t7 };

                foreach (IPfcNode ilinkable in new IPfcStepNode[] { t0, t1, t2, t3 }) {
                    Console.WriteLine("Incrementing " + ilinkable.Name + ".");
                    //ilinkable.Increment();
                    throw new ApplicationException("PFCs are not currently executable.");
                }

                testEvaluator.NextExpectedActivations = new IPfcTransitionNode[] { }; // Ensure it's empty and all have fired.

            }
        }

        [TestMethod]
        public void Test_InsertStepAndTransition() {
            Model model = new Model("SFC Test 1");
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "SFC 1", "", Guid.NewGuid());

            IPfcStepNode t0 = pfc.CreateStep("START", "", Guid.Empty);
            IPfcStepNode t1 = pfc.CreateStep("FINISH", "", Guid.Empty);
            pfc.Bind(t0, t1);

            string structureString = PfcDiagnostics.GetStructure(pfc);

            Console.WriteLine("Structure is \r\n" + structureString);

            // Get reference to old successor
            IPfcNode pfcNode = pfc.Nodes["T_000"];
            IPfcNode oldSuccessorNode = pfcNode.SuccessorNodes[0];

            // Add the step
            IPfcStepNode newStep = pfc.CreateStep("STEP_1", "", Guid.Empty);
            IPfcTransitionNode newTrans = pfc.CreateTransition();

            // We are adding a step following a transition - binding is from selectedTrans-newStep-newTrans-oldSuccessorStep
            pfc.Bind(pfcNode, newStep);
            pfc.Bind(newStep, newTrans);
            pfc.Bind(newTrans, oldSuccessorNode);

            // Disconnect old successor
            pfc.Unbind(pfcNode, oldSuccessorNode);

            structureString = PfcDiagnostics.GetStructure(pfc);
            Console.WriteLine("Structure is \r\n" + structureString);
            System.Diagnostics.Debug.Assert(structureString.Equals("{START-->[L_000(SFC 1.Root)]-->T_000}\r\n{T_000-->[L_002(SFC 1.Root)]-->STEP_1}\r\n{STEP_1-->[L_003(SFC 1.Root)]-->T_001}\r\n{T_001-->[L_004(SFC 1.Root)]-->FINISH}\r\n"));

        }

        [TestMethod]
        public void Test_RemoveStep() {
            string testName = "PFC With Simultaneous Branch";

            IModel model = new Model(testName);

            /*
             *                      START
             *                        + {T1}
             *                      STEP1
             *                        + (T2)
             *                  STEP2   STEP3
             *                    |       + (T3)
             *                    |     STEP4
             *                        + (T4)
             *                      STEP5
             *                        + (T5)
             *                      FINISH
             */

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);

            IPfcNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.NewGuid());
            IPfcNode step4 = pfc.CreateStep("STEP4", string.Empty, Guid.NewGuid());
            IPfcNode step5 = pfc.CreateStep("STEP5", string.Empty, Guid.NewGuid());
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcNode t1 = pfc.CreateTransition("T1", string.Empty, Guid.NewGuid());
            IPfcNode t2 = pfc.CreateTransition("T2", string.Empty, Guid.NewGuid());
            IPfcNode t3 = pfc.CreateTransition("T3", string.Empty, Guid.NewGuid());
            IPfcNode t4 = pfc.CreateTransition("T4", string.Empty, Guid.NewGuid());
            IPfcNode t5 = pfc.CreateTransition("T4", string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, step1);
            pfc.Bind(step1, t2);
            pfc.Bind(t2, step2);
            pfc.Bind(t2, step3);
            pfc.Bind(step2, t4);
            pfc.Bind(step3, t3);
            pfc.Bind(t3, step4);
            pfc.Bind(step4, t4);
            pfc.Bind(t4, step5);
            pfc.Bind(step5, t5);
            pfc.Bind(t5, finishStep);

            /* Delete Step4 and T3*/

            // Need to delete the predecessor transition

            IPfcTransitionNode predecessorTrans = (IPfcTransitionNode)step4.PredecessorNodes[0];
            Assert.IsTrue(predecessorTrans.Name == t3.Name, "The predecessor trans should be T3");

            IPfcStepNode predecessorStep = (IPfcStepNode)predecessorTrans.PredecessorNodes[0];
            Assert.IsTrue(predecessorStep.Name == step3.Name, "The predecessor step should be STEP3");

            IPfcTransitionNode successorTrans = (IPfcTransitionNode)step4.SuccessorNodes[0];
            Assert.IsTrue(successorTrans.Name == t4.Name, "The successor trans should be T4");


            // Connect the predecessor step to the successor transition
            pfc.Bind(predecessorStep, successorTrans);

            // Unbind the existing path from the predecessor step to the successor transition
            pfc.Unbind(predecessorStep, predecessorTrans);
            pfc.Unbind(predecessorTrans, step4);
            pfc.Unbind(step4, successorTrans);

            // Delete the predecessor transition
            pfc.Delete(predecessorTrans);

            // Delete step
            pfc.Delete(step4);

            Assert.IsTrue(pfc.Transitions[t3.Name] == null, "T3 Should be Deleted");
            Assert.IsTrue(pfc.Steps[step4.Name] == null, "Step4 Should be Deleted");
        }

        [TestMethod]
        public void Test_InsertStepIntoLoop() {
            string testName = "PFC with loop gets the loop extended";

            /*
             *           START
             *           |
             *   |----   +T1
             *   |   |   |
             *   |   STEP1
             *   |   |   |
             *   | T2+   +T3
             *   ----|   |
             *           FINISH
             *       
             *      TO
             *      
             *          START
             *           |
             *   |----   +T1
             *   |   |   |
             *   |   STEP1
             *   |   |   |
             *   | T4+   +T3
             *   |   |   |
             *   | STEP2 FINISH
             *   |   |
             *   | T2+
             *   -----  
             * 
             */


            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);

            IPfcNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcNode t1 = pfc.CreateTransition("T1", string.Empty, Guid.NewGuid());
            IPfcNode t2 = pfc.CreateTransition("T2", string.Empty, Guid.NewGuid());
            IPfcNode t3 = pfc.CreateTransition("T3", string.Empty, Guid.NewGuid());
            IPfcNode t4 = pfc.CreateTransition("T4", string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, step1);
            pfc.Bind(step1, t2);
            pfc.Bind(step1, t3);
            pfc.Bind(t2, step1);
            pfc.Bind(t3, finishStep);

            Console.WriteLine(PfcDiagnostics.GetStructure(pfc));
            Console.WriteLine();

            /* Connect new step (step2) to existing transition (t2) */
            pfc.Bind(step2, t2);

            /* Connect existing step (step1) to new transition (t3)*/
            pfc.Bind(step1, t4);

            /* Connect new transition (t3) to new step (step2)*/
            pfc.Bind(t4, step2);

            /* Unbind existing step (step1) and existing transition (t2) */
            pfc.Unbind(step1, t2);

            string result = PfcDiagnostics.GetStructure(pfc);
            Console.WriteLine(result);
            System.Diagnostics.Debug.Assert(result.Equals("{START-->[L_000(SFC 1.Root)]-->T1}\r\n{T1-->[L_001(SFC 1.Root)]-->STEP1}\r\n{STEP1-->[L_003(SFC 1.Root)]-->T3}\r\n{T2-->[L_004(SFC 1.Root)]-->STEP1}\r\n{T3-->[L_005(SFC 1.Root)]-->FINISH}\r\n{STEP2-->[L_006(SFC 1.Root)]-->T2}\r\n{STEP1-->[L_007(SFC 1.Root)]-->T4}\r\n{T4-->[L_008(SFC 1.Root)]-->STEP2}\r\n"));


        }

        #region Delegated test methods

        private void _TestComplexLink(LinkSuperType superType, PfcElementType inType, PfcElementType outType, string shouldBe) {
            string testName = superType.ToString() + " from " + inType.ToString() + " to " + outType.ToString();
            Model model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "SFC 1", "", Guid.NewGuid());

            IPfcNode n0, n1, n2, n3, n4;
            switch (superType) {
                case LinkSuperType.ParallelConvergent:
                    n0 = CreateNode(pfc, "Alice", inType);
                    n1 = CreateNode(pfc, "Bob", inType);
                    n2 = CreateNode(pfc, "Charley", inType);
                    n3 = CreateNode(pfc, "David", inType);
                    n4 = CreateNode(pfc, "Edna", outType);
                    pfc.BindParallelConvergent(new IPfcNode[] { n0, n1, n2, n3 }, n4);
                    break;
                case LinkSuperType.SeriesConvergent:
                    n0 = CreateNode(pfc, "Alice", inType);
                    n1 = CreateNode(pfc, "Bob", inType);
                    n2 = CreateNode(pfc, "Charley", inType);
                    n3 = CreateNode(pfc, "David", inType);
                    n4 = CreateNode(pfc, "Edna", outType);
                    pfc.BindSeriesConvergent(new IPfcNode[] { n0, n1, n2, n3 }, n4);
                    break;
                case LinkSuperType.ParallelDivergent:
                    n0 = CreateNode(pfc, "Alice", inType);
                    n1 = CreateNode(pfc, "Bob", outType);
                    n2 = CreateNode(pfc, "Charley", outType);
                    n3 = CreateNode(pfc, "David", outType);
                    n4 = CreateNode(pfc, "Edna", outType);
                    pfc.BindParallelDivergent(n0, new IPfcNode[] { n1, n2, n3, n4 });
                    break;
                case LinkSuperType.SeriesDivergent:
                    n0 = CreateNode(pfc, "Alice", inType);
                    n1 = CreateNode(pfc, "Bob", outType);
                    n2 = CreateNode(pfc, "Charley", outType);
                    n3 = CreateNode(pfc, "David", outType);
                    n4 = CreateNode(pfc, "Edna", outType);
                    pfc.BindSeriesDivergent(n0, new IPfcNode[] { n1, n2, n3, n4 });
                    break;
                default:
                    break;
            }


            string structureString = PfcDiagnostics.GetStructure(pfc);
            Console.WriteLine("After a " + testName + ", structure is \r\n" + structureString);
            Assert.AreEqual(StripCRLF(structureString), StripCRLF(shouldBe), "Structure should have been\r\n" + shouldBe + "\r\nbut it was\r\n" + structureString + "\r\ninstead.");

            if (m_testSerializationToo) {
                _TestSerialization(pfc, shouldBe, testName);
            }

        }

        private string StripCRLF(string structureString)
        {
            return structureString.Replace("\r", "").Replace("\n", "");
        }

        private void _TestSimpleLink(PfcElementType inType, PfcElementType outType, string shouldBe) {
            string testName = "Simple link from " + inType.ToString() + " to " + outType.ToString();
            Model model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "SFC 1", "", Guid.NewGuid());

            IPfcNode n0, n1;

            n0 = CreateNode(pfc, "Alice", inType);
            n1 = CreateNode(pfc, "Bob", outType);
            pfc.Bind(n0, n1);

            string structureString = PfcDiagnostics.GetStructure(pfc);
            Console.WriteLine("After a " + testName + ", structure is \r\n" + structureString);
            Assert.AreEqual(StripCRLF(structureString), StripCRLF(shouldBe), "Structure should have been\r\n" + shouldBe + "\r\nbut it was\r\n" + structureString + "\r\ninstead.");

            if (m_testSerializationToo) {
                _TestSerialization(pfc, shouldBe, testName);
            }

        }

        private void _TestSerialization(ProcedureFunctionChart pfc, string shouldBe, string testName) {

            #region Store the Pfc to a file.

            #region Create an XmlWriter
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.IO.StringWriter sw = new System.IO.StringWriter(sb);
            XmlTextWriter writer = new XmlTextWriter(sw);

            writer.Formatting = Formatting.Indented;

            #endregion Create an XmlWriter

            #region Write the PFC to the writer
            writer.WriteStartDocument();
            pfc.WriteXml(writer);
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();

            #endregion Write the PFC to the writer

            XmlDocument doc = new XmlDocument();
            doc.InnerXml = sb.ToString();

            doc.Save(m_pfcTestFileName);

            #endregion Store the Pfc to a file.

            #region Load the Pfc from a file.

            ProcedureFunctionChart pfc2 = new ProcedureFunctionChart();
            FileStream fs = new FileStream(m_pfcTestFileName, FileMode.Open);
            XmlReader tr = System.Xml.XmlReader.Create(fs);
            pfc2.ReadXml(tr);
            tr.Close();
            fs.Close();
            File.Delete(m_pfcTestFileName);

            Console.WriteLine(PfcDiagnostics.GetStructure(pfc2));

            #endregion Load the Pfc from a file.

            string structureString = PfcDiagnostics.GetStructure(pfc2);
            Console.WriteLine("After storing and reloading a " + testName + ", the reloaded structure is \r\n" + structureString);
            Assert.AreEqual(StripCRLF(structureString), StripCRLF(shouldBe), "Structure should have been\r\n" + shouldBe + "\r\nbut it was\r\n" + structureString + "\r\ninstead.");

        }

        private void _TestStepToStepBinding() {
            string testName = "Step-to-Step binding, maintaining SFC Compliance";

            Model model = new Model("SFC Test 1");
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "SFC 1", "", Guid.NewGuid());

            PfcStep s1 = (PfcStep)pfc.CreateStep("Alice", "", Guid.Empty);
            PfcStep s2 = (PfcStep)pfc.CreateStep("Bob", "", Guid.Empty);
            //SfcStep s3 = (SfcStep)pfc.CreateStep("Charlie", "", Guid.Empty);

            pfc.Bind(s1, s2);

            string structureString = PfcDiagnostics.GetStructure(pfc);
            Console.WriteLine("After a " + testName + ", structure is \r\n" + structureString);

        }

        private void _TestTransitionToTransitionBinding() {
            string testName = "Transition-to-Transition binding, maintaining SFC Compliance";

            Model model = new Model("SFC Test 1");
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, "SFC 1", "", Guid.NewGuid());

            IPfcTransitionNode s1 = pfc.CreateTransition("Alice", "", Guid.Empty);
            IPfcTransitionNode s2 = pfc.CreateTransition("Bob", "", Guid.Empty);
            //IPfcTransitionNode s3 = pfc.CreateTransition("Charlie", "", Guid.Empty);

            pfc.Bind(s1, s2);

            string structureString = PfcDiagnostics.GetStructure(pfc);
            Console.WriteLine("After a " + testName + ", structure is \r\n" + structureString);

        }

        private IPfcNode CreateNode(ProcedureFunctionChart pfc, string name, PfcElementType inType) {
            switch (inType) {
                case PfcElementType.Link:
                    break;
                case PfcElementType.Transition:
                    return pfc.CreateTransition("T_" + name, "", Guid.NewGuid());
                //break;
                case PfcElementType.Step:
                    return pfc.CreateStep("S_" + name, "", Guid.NewGuid());
                //break;
                default:
                    break;
            }
            return null;
        }

        #endregion Delegated test methods

        class Selector {
            private IPfcTransitionNode[] m_outbounds = null;
            private int i;
            public Selector(IPfcTransitionNode[] outbounds) {
                m_outbounds = outbounds;
                i = 0;
            }

            public IPfcTransitionNode OutboundSelector() {
                if (i >= m_outbounds.Length) { i = 0; }
                return m_outbounds[i++];
            }
        }

        class TestEvaluator {
            private Queue m_nextExpected;
            private ArrayList m_linkablesToMonitor;

            public TestEvaluator(IPfcNode[] linkablesToMonitor) {
                m_nextExpected = new Queue();
                m_linkablesToMonitor = new ArrayList(linkablesToMonitor);
                foreach (IPfcNode t in linkablesToMonitor) {
                    //t.NodeActivated += new ILinkableEvent(OnActivationHappened);
                }
            }

            public IPfcNode NextExpectedActivation { set { m_nextExpected.Enqueue(value); } }
            public IPfcNode[] NextExpectedActivations {
                set {
                    Assert.IsTrue(m_nextExpected.Count == 0, "We are adding new expected activations, but have not yet observed all of the previously expected ones. This is an error.");
                    foreach (IPfcNode t in value) {
                        m_nextExpected.Enqueue(t);
                    }
                }
            }

            private void OnActivationHappened(IPfcNode whoActivated) {
                Assert.IsTrue(m_nextExpected.Count > 0, "Unexpected activation occurred on " + whoActivated.Name + ".");
                IPfcNode t = (IPfcNode)m_nextExpected.Dequeue();
                Assert.AreEqual(t, whoActivated, "" + whoActivated.Name + " activated, but we were expecting " + t.Name + " to do so. This is an error.");
                Console.WriteLine("Activation happened with " + t.Name + ".");
            }
        }
    }
}
