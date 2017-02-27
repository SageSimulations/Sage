/* This source code licensed under the GNU Affero General Public License */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.Graphs.PFC;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PfcAnalyst = Highpoint.Sage.Graphs.PFC.PfcAnalyst;
using Highpoint.Sage.Utility;
using pfcs = SageTestLib.TestPfcRepository;
using System.Linq;
using System.Xml;

namespace PFCDemoMaterial {

    [TestClass]
    public class PfcAnalystTester {

        [TestMethod]
        public void TestPrePostTransitionLink() {

            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            IPfcLinkElement link = pfcs.nA.Successors[0]; // Link between node A and T_000
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsPostTransitionLink(link), "Link between node A and T_000 shouldn't be a post-transition link, but was.");
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsPreTransitionLink(link), "Link between node A and T_000 should be a pre-transition link, but wasn't.");

            link = pfcs.nG.Successors[0]; // Link between node G and T_006
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsPostTransitionLink(link), "Link between node G and T_006 shouldn't be a post-transition link, but was.");
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsPreTransitionLink(link), "Link between node G and T_006 should be a pre-transition link, but wasn't.");

            link = pfcs.nB.Predecessors[0]; // Link between T_000 and node B
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsPostTransitionLink(link), "Link between T_000 and node B should be a post-transition link, but wasn't.");
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsPreTransitionLink(link), "Link between T_000 and node B shouldn't be a pre-transition link, but was.");

            link = pfcs.nG.Predecessors[0]; // Link between T_005 and node G
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsPostTransitionLink(link), "Link between T_005 and node G should be a post-transition link, but wasn't.");
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsPreTransitionLink(link), "Link between T_005 and node G shouldn't be a pre-transition link, but was.");

        }

        [TestMethod]
        public void TestSoleSuccessor() {

            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsSoleSuccessor(pfcs.nA));
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsSoleSuccessor(pfcs.nB));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsSoleSuccessor(pfcs.nH));
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsSoleSuccessor(pfcs.nJ));

            System.Diagnostics.Debug.Assert(PfcAnalyst.IsSoleSuccessor(pfcs.nG.SuccessorNodes[0]));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsSoleSuccessor(pfcs.nC.PredecessorNodes[0]));

        }

        [TestMethod]
        public void TestHasParallelPath() {

            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            System.Diagnostics.Debug.Assert(!PfcAnalyst.HasParallelPaths(pfcs.nC));
            System.Diagnostics.Debug.Assert(PfcAnalyst.HasParallelPaths(pfcs.nH));

            System.Diagnostics.Debug.Assert(!PfcAnalyst.HasParallelPaths(pfcs.nC.SuccessorNodes[0]));
            System.Diagnostics.Debug.Assert(PfcAnalyst.HasParallelPaths(pfcs.nI.SuccessorNodes[0]));

        }

        [TestMethod]
        public void TestHasAlternatePath() {

            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            System.Diagnostics.Debug.Assert(PfcAnalyst.HasAlternatePaths(pfcs.nC));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.HasAlternatePaths(pfcs.nH));

            System.Diagnostics.Debug.Assert(PfcAnalyst.HasAlternatePaths(pfcs.nC.SuccessorNodes[0]));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.HasAlternatePaths(pfcs.nI.SuccessorNodes[0]));

        }

        [TestMethod]
        public void TestIsLastElementOnTypesOfPaths() {


            #region Straight-thru test segment.
            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            IPfcTransitionNode trans = null;
            #region Find the solo transition between nodes B and N.

            foreach (IPfcTransitionNode _trans in pfc.Transitions) {
                if (_trans.PredecessorNodes.Count == 0 || _trans.SuccessorNodes.Count == 0) {
                    continue;
                }
                if (_trans.PredecessorNodes[0].Equals(pfcs.nB) && _trans.SuccessorNodes[0].Equals(pfcs.nN)) {
                    trans = _trans;
                }
            }

            #endregion Find the solo transition between nodes B and N.

            System.Diagnostics.Debug.Assert(trans != null);

            System.Diagnostics.Debug.Assert(PfcAnalyst.IsLastElementOnAlternatePath(trans));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnParallelPath(trans));
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsLastElementOnPath(trans));

            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnAlternatePath(pfcs.nP));
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsLastElementOnParallelPath(pfcs.nP));
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsLastElementOnPath(pfcs.nP));

            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnAlternatePath(pfcs.nE.PredecessorNodes[0]));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnParallelPath(pfcs.nE.PredecessorNodes[0]));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnPath(pfcs.nE.PredecessorNodes[0]));

            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnAlternatePath(pfcs.nJ));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnParallelPath(pfcs.nJ));
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnPath(pfcs.nJ));

            #endregion Loopback test segment.

            #region Straight-thru test segment.
            pfc = pfcs.CreateLoopTestPfc();

            Console.WriteLine("Structure is " + PfcDiagnostics.GetStructure(pfc));

            trans = pfc.Transitions["T_002"];

            System.Diagnostics.Debug.Assert(PfcAnalyst.IsLastElementOnAlternatePath(trans),"Loopback transition should be indicated as last element on an series-divergent path, but is not.");
            System.Diagnostics.Debug.Assert(!PfcAnalyst.IsLastElementOnParallelPath(trans), "Loopback transition should not be indicated as last element on a parallel-divergent path, but is.");
            System.Diagnostics.Debug.Assert(PfcAnalyst.IsLastElementOnPath(trans), "Loopback transition should be indicated as last element on an alternate path, but is not.");

            #endregion Loopback test segment.

        }

        [TestMethod]
        public void TestIsJoinElement_Methods() {

            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();
            IPfcElement element = PfcAnalyst.GetJoinNodeForParallelPath(pfcs.nJ);
            System.Diagnostics.Debug.Assert(element != null && element.Equals(pfc.Transitions["T_009"]));

            element = PfcAnalyst.GetJoinNodeForParallelPath(pfcs.nD);
            System.Diagnostics.Debug.Assert(element != null && element.Equals(pfcs.nN));

            element = PfcAnalyst.GetJoinNodeForAlternatePaths(pfc.Transitions["T_004"]);
            System.Diagnostics.Debug.Assert(element != null && element.Equals(pfcs.nN));

            element = PfcAnalyst.GetJoinNodeForAlternatePaths(pfcs.nD);
            System.Diagnostics.Debug.Assert(element != null && element.Equals(pfcs.nN));

            element = PfcAnalyst.GetJoinTransitionForSimultaneousPaths(pfc.Transitions["T_007"]);
            System.Diagnostics.Debug.Assert(element != null && element.Equals(pfc.Transitions["T_009"]));

            element = PfcAnalyst.GetJoinTransitionForSimultaneousPaths(pfcs.nK);
            System.Diagnostics.Debug.Assert(element != null && element.Equals(pfc.Transitions["T_009"]));

            element = PfcAnalyst.GetJoinNodeForAlternatePaths(pfcs.nP);
            System.Diagnostics.Debug.Assert(element == null);

            element = PfcAnalyst.GetJoinNodeForAlternatePaths(pfc.Transitions["T_008"]);
            System.Diagnostics.Debug.Assert(element == null);

            element = PfcAnalyst.GetJoinTransitionForSimultaneousPaths(pfcs.nM);
            System.Diagnostics.Debug.Assert(element == null);

            element = PfcAnalyst.GetJoinTransitionForSimultaneousPaths(pfc.Transitions["T_004"]);
            System.Diagnostics.Debug.Assert(element == null);


        }

        [TestMethod]
        public void TestFindLegalTargets() {

            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();
            Console.WriteLine("\r\n\r\n\r\n" + PfcDiagnostics.GetStructure(pfc));

            IPfcNode[] testNodes = new IPfcNode[] { pfcs.nG, pfcs.nI, pfcs.nB, pfcs.nN, pfcs.nD, pfc.Transitions["T_000"], pfc.Transitions["T_014"], pfc.Transitions["T_005"], pfc.Transitions["T_011"] };

            foreach (IPfcNode origin in testNodes) {
                List<IPfcNode> nodes;
                //Console.WriteLine("\r\n\r\n\r\n" + PfcDiagnostics.GetStructure(pfc));
                nodes = PfcAnalyst.GetPermissibleTargetsForLinkFrom(origin);
                Console.WriteLine("From node " + origin.Name + ", all legal targets are...");
                foreach (IPfcNode node in nodes) {
                    Console.WriteLine(node.Name);
                }
            }
        }

        [TestMethod]
        public void TestSpecificTargetLegality() {

            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();
            Console.WriteLine("\r\n\r\n\r\n" + PfcDiagnostics.GetStructure(pfc));

            IPfcNode originNode = pfcs.nG;
            IPfcNode targetNode = pfcs.nO;

            Console.WriteLine("Binding nG to nO " + (PfcAnalyst.IsTargetNodeLegal(originNode,targetNode)?"is":"is not") + " legal.");

        }

        [TestMethod]
        public void TestUnbindTransitionAndStep() {
            // Create initial pfc
            Model model = new Model();
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model);
            IPfcStepNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcStepNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcTransitionNode t1 = pfc.CreateTransition(string.Empty, string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, finishStep);

            string structureString = PfcDiagnostics.GetStructure(pfc);

            IPfcStepNode newStep = pfc.CreateStep("NEW_STEP", string.Empty, Guid.NewGuid());
            IPfcTransitionNode newTrans = pfc.CreateTransition(string.Empty, string.Empty, Guid.NewGuid());

            // Unbind t1 from finishStep
            pfc.Unbind(t1, finishStep);

            // Add new step and transition
            pfc.Bind(t1, newStep);
            pfc.Bind(newStep, newTrans);

            // Bind the finishStep
            pfc.Bind(newTrans, finishStep);

            structureString = PfcDiagnostics.GetStructure(pfc);

        }

        [TestMethod]
        public void Test_AddNewStepAndTransition() {
            // Create initial pfc

            /*
             *          START
             *            +
             *          FINISH
             */

            Model model = new Model();
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model);
            IPfcStepNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcStepNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcTransitionNode t1 = pfc.CreateTransition(string.Empty, string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, finishStep);

            // Add the new step

            /*
             *          START
             *            +
             *          STEP1
             *            +
             *          FINISH
             */

            IPfcStepNode newStep = pfc.CreateStep("NEW_STEP", string.Empty, Guid.NewGuid());
            IPfcTransitionNode newTrans = pfc.CreateTransition(string.Empty, string.Empty, Guid.NewGuid());

            // Add new step and transition
            pfc.Bind(t1, newStep);
            pfc.Bind(newStep, newTrans);

            // Bind the finishStep
            pfc.Bind(newTrans, finishStep);

            // Unbind t1 from finishStep
            pfc.Unbind(t1, finishStep);

            string result = PfcDiagnostics.GetStructure(pfc).ToString();
            Console.WriteLine(result);

            System.Diagnostics.Debug.Assert(result.Equals("{START-->[L_000(SFC 1.Root)]-->T_000}\r\n{T_000-->[L_002(SFC 1.Root)]-->NEW_STEP}\r\n{NEW_STEP-->[L_003(SFC 1.Root)]-->T_001}\r\n{T_001-->[L_004(SFC 1.Root)]-->FINISH}\r\n"));

        }

        [TestMethod]
        public void Test_GetPermissibleAlternateBranchTargets() {
            // Create a PFC that supports forward and backward branches

            /*
             *                      START
             *                        +
             *                      STEP1
             *                        +
             *                      STEP2
             *                        +
             *                      STEP3
             *                        +
             *                      FINISH
             */

            Model model = new Model();
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model);
            IPfcStepNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcStepNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcStepNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcStepNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.NewGuid());
            IPfcStepNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcTransitionNode t1 = pfc.CreateTransition("START-STEP1", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t2 = pfc.CreateTransition("STEP1-STEP2", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t3 = pfc.CreateTransition("STEP2-STEP3", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t4 = pfc.CreateTransition("STEP3-FINISH", string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, step1);
            pfc.Bind(step1, t2);
            pfc.Bind(t2, step2);
            pfc.Bind(step2, t3);
            pfc.Bind(t3, step3);
            pfc.Bind(step3, t4);
            pfc.Bind(t4, finishStep);

            //List<IPfcNode> targets = PfcAnalyst.GetPermissibleTargetsForLinkFrom(step2, true, true);
            //Console.WriteLine("Acceptable targets were " + Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedListOfNames<IPfcNode>(targets) + ".");
            //Assert.IsTrue(targets.Contains(step1), "Should be able to link back to STEP1");
            //Assert.IsTrue(targets.Contains(step2), "Should be able to link to self.");
            //Assert.IsTrue(targets.Contains(step3), "Should be able to add alternate path to STEP3.");
            //Assert.IsTrue(targets.Contains(finishStep), "Should be able to link to FINISH");

            Assert.IsTrue(PfcAnalyst.IsTargetNodeLegal(step2, step1));
        }

        [TestMethod]
        public void Test_GetExistingJoinStepForAlternateBranch() {
            // Create a PFC with an alternate branch

            /*
             *                  START
             *                    +
             *                  STEP1
             *                  +   +
             *              STEP2   STEP3
             *                  +   +
             *                  FINISH
             */

            Model model = new Model();
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model);
            IPfcStepNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcStepNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcStepNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcStepNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.NewGuid());
            IPfcStepNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcTransitionNode t1 = pfc.CreateTransition("START-STEP1", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t2 = pfc.CreateTransition("STEP1-STEP2", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t3 = pfc.CreateTransition("STEP1-STEP3", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t4 = pfc.CreateTransition("STEP2-FINISH", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t5 = pfc.CreateTransition("STEP3-FINISH", string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, step1);

            pfc.Bind(step1, t2);
            pfc.Bind(t2, step2);
            pfc.Bind(step2, t4);
            pfc.Bind(t4, finishStep);

            pfc.Bind(step1, t3);
            pfc.Bind(t3, step3);
            pfc.Bind(step3, t5);
            pfc.Bind(t5, finishStep);

            // Get the existing join step for the alternate branch from the start point - step 1
            IPfcNode target = PfcAnalyst.GetJoinNodeForAlternatePaths(step2);

            Assert.IsTrue(target.Guid == finishStep.Guid, "The join step should be the FINISH step");


        }

        [TestMethod]
        public void Test_GetConvergenceNodeFor() {
            // Create a PFC with an alternate branch

            /*
             *                  START
             *                    +
             *                  STEP1
             *                  +   +
             *              STEP2   STEP3
             *                  +   +
             *                  FINISH
             */

            Model model = new Model();
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model);
            IPfcStepNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcStepNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcStepNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcStepNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.NewGuid());
            IPfcStepNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcTransitionNode t1 = pfc.CreateTransition("START-STEP1", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t2 = pfc.CreateTransition("STEP1-STEP2", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t3 = pfc.CreateTransition("STEP1-STEP3", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t4 = pfc.CreateTransition("STEP2-FINISH", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t5 = pfc.CreateTransition("STEP3-FINISH", string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, step1);

            pfc.Bind(step1, t2);
            pfc.Bind(t2, step2);
            pfc.Bind(step2, t4);
            pfc.Bind(t4, finishStep);

            pfc.Bind(step1, t3);
            pfc.Bind(t3, step3);
            pfc.Bind(step3, t5);
            pfc.Bind(t5, finishStep);

            // Get the existing join step for the alternate branch from the start point - step 1
            IPfcNode target = PfcAnalyst.GetConvergenceNodeFor(step1);

            Assert.IsTrue(target.Guid == finishStep.Guid, "The join step should be the FINISH step");


        }

        [TestMethod]
        public void Test_GetDivergenceNodeFor() {
            // Create a PFC with an alternate branch

            /*
             *                  START
             *                    +
             *                  STEP1
             *                  +   +
             *              STEP2   STEP3
             *                  +   +
             *                  FINISH
             */

            Model model = new Model();
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model);
            IPfcStepNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcStepNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcStepNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcStepNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.NewGuid());
            IPfcStepNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            IPfcTransitionNode t1 = pfc.CreateTransition("START-STEP1", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t2 = pfc.CreateTransition("STEP1-STEP2", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t3 = pfc.CreateTransition("STEP1-STEP3", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t4 = pfc.CreateTransition("STEP2-FINISH", string.Empty, Guid.NewGuid());
            IPfcTransitionNode t5 = pfc.CreateTransition("STEP3-FINISH", string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, t1);
            pfc.Bind(t1, step1);

            pfc.Bind(step1, t2);
            pfc.Bind(t2, step2);
            pfc.Bind(step2, t4);
            pfc.Bind(t4, finishStep);

            pfc.Bind(step1, t3);
            pfc.Bind(t3, step3);
            pfc.Bind(step3, t5);
            pfc.Bind(t5, finishStep);

            // Get the existing join step for the alternate branch from the start point - step 1
            IPfcNode target = PfcAnalyst.GetDivergenceNodeFor(finishStep);

            Assert.IsTrue(target.Guid == step1.Guid, "The divergence step should be step1.");

        }

        [TestMethod]
        public void Test_SimpleDeletion() {
            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            string structureString;

            structureString = PfcDiagnostics.GetStructure(pfc);

            Assert.IsTrue(structureString.Contains("{Step_B-->[L_032(SFC 1.Root)]-->T_014}"));
            Assert.IsTrue(structureString.Contains("{T_014-->[L_033(SFC 1.Root)]-->Step_N}"));

            pfc.Delete(pfc.Nodes["T_014"]);
            structureString = PfcDiagnostics.GetStructure(pfc);

            Assert.IsFalse(structureString.Contains("{Step_B-->[L_032(SFC 1.Root)]-->T_014}"));
            Assert.IsFalse(structureString.Contains("{T_014-->[L_033(SFC 1.Root)]-->Step_N}"));

        }

        [TestMethod]
        public void Test_SimpleDeletion2() {
            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            string structureString;

            structureString = PfcDiagnostics.GetStructure(pfc);

            Assert.IsTrue(structureString.Contains("{T_003-->[L_007(SFC 1.Root)]-->Step_D}"));
            Assert.IsTrue(structureString.Contains("{Step_D-->[L_008(SFC 1.Root)]-->T_004}"));
            Assert.IsTrue(structureString.Contains("{T_004-->[L_009(SFC 1.Root)]-->Step_E}"));

            pfc.Delete(pfc.Nodes["Step_D"]);
            structureString = PfcDiagnostics.GetStructure(pfc);

            Assert.IsFalse(structureString.Contains("{T_003-->[L_007(SFC 1.Root)]-->Step_D}"));
            Assert.IsFalse(structureString.Contains("{Step_D-->[L_008(SFC 1.Root)]-->T_004}"));
            Assert.IsFalse(structureString.Contains("{T_004-->[L_009(SFC 1.Root)]-->Step_E}"));
            Assert.IsTrue(structureString.Contains("{T_003-->[L_034(SFC 1.Root)]-->Step_E}"));

        }

        [TestMethod]
        public void Test_SimpleDeletion3() {
            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();

            string structureString;

            structureString = PfcDiagnostics.GetStructure(pfc);

            Assert.IsTrue(structureString.Contains("{T_003-->[L_007(SFC 1.Root)]-->Step_D}"));
            Assert.IsTrue(structureString.Contains("{Step_D-->[L_008(SFC 1.Root)]-->T_004}"));
            Assert.IsTrue(structureString.Contains("{T_004-->[L_009(SFC 1.Root)]-->Step_E}"));

            pfc.Delete(pfc.Nodes["T_004"]);
            structureString = PfcDiagnostics.GetStructure(pfc);

            Assert.IsFalse(structureString.Contains("{T_003-->[L_007(SFC 1.Root)]-->Step_D}"));
            Assert.IsFalse(structureString.Contains("{Step_D-->[L_008(SFC 1.Root)]-->T_004}"));
            Assert.IsFalse(structureString.Contains("{T_004-->[L_009(SFC 1.Root)]-->Step_E}"));
            Assert.IsTrue(structureString.Contains("{T_003-->[L_034(SFC 1.Root)]-->Step_E}"));

        }

        // BUG: Test_GetPermissibleTargetsForLinkFrom_WithinLoopFromStepToSelf Bug is Priority 2, Severity A
#if NYRFPT
    [TestMethod]
        public void Test_GetPermissibleTargetsForLinkFrom_WithinLoopFromStepToSelf() {

            //    BEGIN
            //       |
            //       |
            //  BEGIN-STEP1 <-- A transition (as are STEP1-FINISH and STEP1-STEP1.)
            //       | ___________________________________
            //       | |                                  |
            //      STEP1                                 |
            //       | |______________________.           |
            //       |                        |           |
            //  STEP1-FINISH             STEP1-STEP1 <----|---- Want to know where else we can link to,
            //       |                        |___________|     from this transition. (Should be 'Nowhere.')
            //       |                        
            //     FINISH
            string testName = "PFC with Loop";
            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);
            ( (ProcedureFunctionChart.PfcElementFactory)pfc.ElementFactory ).SetRepeatable(Guid.Empty);

            IPfcNode beginStep = pfc.CreateStep("BEGIN", string.Empty, Guid.Empty);
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.Empty);
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.Empty);

            IPfcNode t1 = pfc.CreateTransition("BEGIN-STEP1", string.Empty, Guid.Empty);
            IPfcNode t2 = pfc.CreateTransition("STEP1-FINISH", string.Empty, Guid.Empty);

            IPfcNode loopTransition = pfc.CreateTransition("STEP1-STEP1", string.Empty, Guid.Empty);

            pfc.Bind(beginStep, t1);
            pfc.Bind(t1, step1);
            pfc.Bind(step1, t2);
            pfc.Bind(t2, finishStep);

            pfc.Bind(step1, loopTransition);
            pfc.Bind(loopTransition, step1);

            string structure = PfcDiagnostics.GetStructure(pfc);
            List<IPfcNode> results = PfcAnalyst.GetPermissibleTargetsForLinkFrom(loopTransition);
            Assert.AreEqual(structure, PfcDiagnostics.GetStructure(pfc));

            string resultString = Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedListOfNames<IPfcNode>(results);
            string shouldBe = "";
            Assert.IsTrue(resultString.Equals(shouldBe), "Should get " + shouldBe + ", but got " + resultString + " instead.");

        }
#endif
        [TestMethod]
        public void Test_DeepNonLoopingPath() {

            //          BEGIN
            //            |
            //         BEGIN-STEP1
            //            |
            //          STEP1
            //            |
            //         STEP1-STEP42   
            //     .____| | ._________________________.
            //     |      | |                         |
            //  STEP4   STEP2                         |
            //     |____. | |_____________.      STEP3-STEP2
            //          | |          STEP2-STEP3      |
            //       STEP42-FINISH        |           |
            //            |             STEP3         |
            //          FINISH            |___________|

            string testName = "PFC with Loop";
            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);
            ( (PfcElementFactory)pfc.ElementFactory ).SetRepeatable(Guid.Empty);

            IPfcNode beginStep = pfc.CreateStep("BEGIN", string.Empty, Guid.Empty);
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.Empty);
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.Empty);
            IPfcNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.Empty);
            IPfcNode step4 = pfc.CreateStep("STEP4", string.Empty, Guid.Empty);
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.Empty);

            IPfcNode t1 = pfc.CreateTransition("BEGIN-STEP1", string.Empty, Guid.Empty);
            IPfcNode t2 = pfc.CreateTransition("STEP1-STEP42", string.Empty, Guid.Empty);
            IPfcNode t3 = pfc.CreateTransition("STEP2-STEP3", string.Empty, Guid.Empty);
            IPfcNode t4 = pfc.CreateTransition("STEP3-STEP2", string.Empty, Guid.Empty);
            IPfcNode t5 = pfc.CreateTransition("STEP42-FINISH", string.Empty, Guid.Empty);

            pfc.Bind(beginStep, t1);
            pfc.Bind(t1, step1);
            pfc.Bind(step1, t2);

            pfc.Bind(t2, step2);
            pfc.Bind(t2, step4);
            pfc.Bind(step2, t3);
            pfc.Bind(step2, t5);
            pfc.Bind(step4, t5);
            pfc.Bind(t3, step3);
            pfc.Bind(step3, t4);
            pfc.Bind(t4, step2);
            pfc.Bind(t5, finishStep);

            Console.WriteLine(PfcAnalyst.AssignWeightsForBroadestNonLoopingPath(pfc));

            foreach (PfcLink link in pfc.Links) {
                Console.WriteLine("Link from {0} to {1} has priority {2}.", link.Predecessor.Name, link.Successor.Name, link.Priority);
            }

        }

        [TestMethod]
        public void Test_SelfLoopStructuralLegality() {

            //  BEGIN
            //    |
            //    +
            //    |
            //  BEGIN-STEP1
            //    |
            //    + ___________________________________
            //    | |                                  |
            //  STEP1                                  |
            //    | |______________________.           |
            //    +                 Loop STEP1-STEP1   |  <-- Want to know where we can link to, from this transition. (Nowhere.)
            //    |                        |           |
            //  STEP1-FINISH               |           |
            //    |                        |           |
            //    +                        |           |
            //    |                        |___________|
            //  FINISH
            string testName = "Self-Loop Structural Legality";
            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);
            ((PfcElementFactory)pfc.ElementFactory).SetRepeatable(Guid.Empty);

            IPfcNode beginStep = pfc.CreateStep("BEGIN", string.Empty, Guid.Empty);
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.Empty);
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.Empty);

            IPfcNode t1 = pfc.CreateTransition("BEGIN-STEP1", string.Empty, Guid.Empty);
            IPfcNode t2 = pfc.CreateTransition("STEP1-FINISH", string.Empty, Guid.Empty);

            IPfcNode loopTransition = pfc.CreateTransition("Loop STEP1-STEP1", string.Empty, Guid.Empty);

            pfc.Bind(beginStep, t1);
            pfc.Bind(t1, step1);
            pfc.Bind(step1, t2);
            pfc.Bind(t2, finishStep);

            pfc.Bind(step1, loopTransition);
            pfc.Bind(loopTransition, step1);

            PfcValidator validator = new PfcValidator(pfc);

            Assert.IsTrue(validator.PfcIsValid(),"This PFC should be valid.");

        }

        [TestMethod]
        public void Test_LoopbackWithinAParallelBranch() {

            //        START
            //          |
            //      ____+_____
            //     |          |
            //   STEP4      STEP1
            //     |          +
            //     +        STEP5 <-- What are the valid targets for a branch from STEP5? (STEP5 & STEP2.)
            //     |          +
            //   STEP6      STEP2
            //     |          |
            //     |__________|
            //          +
            //        STEP3
            //          +
            //        FINISH
            //
            string testName = "PFC with Loop";
            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);

            IPfcNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.NewGuid());
            IPfcNode step4 = pfc.CreateStep("STEP4", string.Empty, Guid.NewGuid());
            IPfcNode step5 = pfc.CreateStep("STEP5", string.Empty, Guid.NewGuid());
            IPfcNode step6 = pfc.CreateStep("STEP6", string.Empty, Guid.NewGuid());
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            pfc.BindParallelDivergent(startStep, new IPfcNode[] { step4, step1 });
            pfc.BindParallelConvergent(new IPfcNode[] { step6, step2 }, step3);
            pfc.Bind(step4, step6);
            pfc.Bind(step1, step5);
            pfc.Bind(step5, step2);
            pfc.Bind(step3, finishStep);


#pragma warning disable 168
            bool result;
#pragma warning restore 168
            //// MASS TESTING FOR STEP-BY-STEP CHECKING
            //string structure = PfcDiagnostics.GetStructure(pfc);
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step1);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step4);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step6);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, startStep);  // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step5);      // yes.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step2);      // yes.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step3);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, finishStep); // no.

            //// ONE-OFF TESTING FOR DEBUG SUPPORT.
            //IPfcNode from = step5;
            //IPfcNode to = step5;
            //bool expected = true;
            //result = PfcAnalyst.IsTargetNodeLegal(from, to);      // no.
            //Console.WriteLine("{1} {0} a legal target from {2}.", result?"is":"is not", to.Name, from.Name);
            //Assert.IsTrue(result == expected);

            // BULK TEST FOR AUTOMATIC TESTING.
            List<IPfcNode> results = PfcAnalyst.GetPermissibleTargetsForLinkFrom(step5);
            results.Sort((node1, node2) => Comparer<string>.Default.Compare(node1.Name,node2.Name));

            string resultString = Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedListOfNames(new List<IHasName>(results.ToArray()));
            Console.WriteLine(resultString);
            Assert.IsTrue(resultString.Equals("STEP2 and STEP5"), "Valid target steps should be STEP2 and STEP5, but they were " + resultString + ".");

        }

        [TestMethod]
        public void Test_LoopbackWithinAParallelBranchFromSavedPFC_Passes()
        {
            ProcedureFunctionChart pfc = getProcedureFunctionChartFromFile("../../TestData/RightPFC.xml");

            //IPfcNode startStep = pfc.Steps.FirstOrDefault(x => x.Name == "START");
            //IPfcNode step1 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP1");
            //IPfcNode step2 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP2");
            //IPfcNode step3 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP3");
            //IPfcNode step4 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP4");
            IPfcNode step5 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP5");
            //IPfcNode step6 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP6");
            //IPfcNode finishStep = pfc.Steps.FirstOrDefault(x => x.Name == "FINISH");

            List<IPfcNode> results = PfcAnalyst.GetPermissibleTargetsForLinkFrom(step5);
            results.Sort((node1, node2) => Comparer<string>.Default.Compare(node1.Name, node2.Name));

            string resultString = Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedListOfNames(new List<IHasName>(results.ToArray()));
            Console.WriteLine(resultString);
            Assert.IsTrue(resultString.Equals("STEP2 and STEP5"), "Valid target steps should be STEP2 and STEP5, but they were " + resultString + ".");
        }

        [TestMethod]
        public void Test_LoopbackWithinAParallelBranchFromSavedPFC_UsedToFail()
        {
            ProcedureFunctionChart pfc = getProcedureFunctionChartFromFile("../../TestData/WrongPFC.xml");

            //IPfcNode startStep = pfc.Steps.FirstOrDefault(x => x.Name == "START");
            //IPfcNode step1 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP1");
            //IPfcNode step2 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP2");
            //IPfcNode step3 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP3");
            //IPfcNode step4 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP4");
            IPfcNode step5 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP5");
            //IPfcNode step6 = pfc.Steps.FirstOrDefault(x => x.Name == "STEP6");
            //IPfcNode finishStep = pfc.Steps.FirstOrDefault(x => x.Name == "FINISH");

#pragma warning disable 168
            bool result;
#pragma warning restore 168
            //// MASS TESTING FOR STEP-BY-STEP CHECKING
            //string structure = PfcDiagnostics.GetStructure(pfc);
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step1);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step4);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step6);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, startStep);  // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step5);      // yes.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step2);      // yes.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, step3);      // no.
            //System.Diagnostics.Debug.Assert(structure == PfcDiagnostics.GetStructure(pfc));
            //result = PfcAnalyst.IsTargetNodeLegal(step5, finishStep); // no.

            //// ONE-OFF TESTING FOR DEBUG SUPPORT.
            //IPfcNode from = step5;
            //IPfcNode to = step5;
            //bool expected = true;
            //result = PfcAnalyst.IsTargetNodeLegal(from, to);      // no.
            //Console.WriteLine("{1} {0} a legal target from {2}.", result?"is":"is not", to.Name, from.Name);
            //Assert.IsTrue(result == expected);

            // BULK TEST FOR AUTOMATIC TESTING.
            List<IPfcNode> results = PfcAnalyst.GetPermissibleTargetsForLinkFrom(step5);
            results.Sort((node1, node2) => Comparer<string>.Default.Compare(node1.Name, node2.Name));

            string resultString = Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedListOfNames(new List<IHasName>(results.ToArray()));
            Console.WriteLine(resultString);
            Assert.IsTrue(resultString.Equals("STEP2 and STEP5"), "Valid target steps should be STEP2 and STEP5, but they were " + resultString + ".");
        }

        private ProcedureFunctionChart getProcedureFunctionChartFromFile(string fileName)
        {
            var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(directory, fileName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart();
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                using (XmlReader tr = System.Xml.XmlReader.Create(fs))
                    pfc.ReadXml(tr);
            }

            return pfc;
        }

        [TestMethod]
        public void Test_LoopbackUsingParallelDivergence() {

            //   START    -----
            //     |      |   |
            //    ==========  |
            //     +T1        |
            //     |          |
            //   STEP1      STEP2
            //     |          |
            //     +T2        |
            //   ===========  |
            //     |      |___|
            //   FINISH
            //
            string testName = "Illegal PFC with Loop Using Parallel Divergence";
            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);

            IPfcNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcTransitionNode T1 = pfc.CreateTransition("T1", string.Empty, Guid.NewGuid());
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcTransitionNode T2 = pfc.CreateTransition("T2", string.Empty, Guid.NewGuid());
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());

            pfc.Bind(startStep, T1);
            pfc.Bind(T1, step1);
            pfc.Bind(step1, T2);
            pfc.Bind(T2, finishStep);

            pfc.Bind(T2, step2);
            pfc.Bind(step2, T1);

            bool isValid = new PfcValidator(pfc).PfcIsValid();

            //List<IPfcNode> results = PfcAnalyst.GetPermissibleTargetsForLinkFrom(T2);

            //string resultString = Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedListOfNames(new List<IHasName>(results.ToArray()));
            //Console.WriteLine(resultString);
            Assert.IsFalse(new PfcValidator(pfc).PfcIsValid(), "Failed to flag a faux-loopback using a parallel div/convergence as invalid.");

        }


        /// <summary>
        /// Tests the PFC update structure call - specifically that it orders the node list into a
        /// breadth-first traversal order.
        /// </summary>
        [TestMethod]
        public void Test_PfcUpdateStructure() {
            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();
            pfc.UpdateStructure();

            string s = PfcDiagnostics.GetStructure(pfc);

            StringWriter sw = new StringWriter();
            foreach (IPfcNode node in pfc.Nodes) {
                sw.WriteLine(node.Name + " : " + node.GraphOrdinal);
            }
            string resultString = sw.GetStringBuilder().ToString();
            Console.WriteLine(resultString);
            Assert.AreEqual("Step_A : 0\r\nT_000 : 1\r\nStep_B : 2\r\nT_001 : 3\r\nT_002 : 4\r\nT_014 : 5\r\nStep_C : 6\r\nStep_F : 7\r\nT_003 : 8\r\nT_005 : 9\r\nStep_D : 10\r\nStep_G : 11\r\nT_004 : 12\r\nT_006 : 13\r\nStep_E : 14\r\nStep_H : 15\r\nStep_I : 16\r\nStep_P : 17\r\nT_011 : 18\r\nT_007 : 19\r\nT_008 : 20\r\nStep_J : 21\r\nStep_K : 22\r\nT_009 : 23\r\nStep_L : 24\r\nT_010 : 25\r\nStep_M : 26\r\nT_012 : 27\r\nStep_N : 28\r\nT_013 : 29\r\nStep_O : 30\r\n",resultString);
        }


        [TestMethod]
        public void Test_LoopingPfcUpdateStructure() {
            ProcedureFunctionChart pfc = pfcs.CreateLoopTestPfc();
            pfc.UpdateStructure();


            StringWriter sw = new StringWriter();
            foreach (IPfcNode node in pfc.Nodes) {
                sw.WriteLine(node.Name + " : " + node.GraphOrdinal);
            }
            string resultString = sw.GetStringBuilder().ToString();
            Console.WriteLine(resultString);
            Assert.AreEqual(resultString,"Step_A : 0\r\nT_000 : 1\r\nStep_B : 2\r\nT_001 : 3\r\nT_002 : 4\r\nStep_C : 5\r\n");
        }

        [TestMethod]
        public void Test_ComplexLoopingPfcUpdateStructure() {
            int nFailures = 0;
            for (int i = 0; i < 20; i++) {
                try {
                    _Test_ComplexLoopingPfcUpdateStructure();
                } catch {
                    nFailures++;
                }
            }
            Assert.AreEqual(0, nFailures, "There were " + nFailures + " failures, and should have been none.");
        }

        public void _Test_ComplexLoopingPfcUpdateStructure() {
            //        START
            //          |
            //      ____+________
            //     |             |
            //   STEP4         STEP1
            //     |             |
            //     +             + ___
            //     |             | |  |
            //   STEP6         STEP5  |
            //     +           +   +  |
            //     |           |   |__|
            //     |         STEP2 
            //     |           +
            //     |___________|
            //          +
            //        STEP3
            //          +
            //        FINISH
            //
            string testName = "PFC with Loop";
            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);
            ((PfcElementFactory)pfc.ElementFactory).SetRepeatable(Guid.Empty);

            IPfcNode startStep = pfc.CreateStep("START", string.Empty, Guid.Empty);
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.Empty);
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.Empty);
            IPfcNode step3 = pfc.CreateStep("STEP3", string.Empty, Guid.Empty);
            IPfcNode step4 = pfc.CreateStep("STEP4", string.Empty, Guid.Empty);
            IPfcNode step5 = pfc.CreateStep("STEP5", string.Empty, Guid.Empty);
            IPfcNode step6 = pfc.CreateStep("STEP6", string.Empty, Guid.Empty);
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.Empty);

            pfc.BindParallelDivergent(startStep, new IPfcNode[] { step4, step1 });
            pfc.BindParallelConvergent(new IPfcNode[] { step6, step2 }, step3);
            pfc.Bind(step4, step6);
            pfc.Bind(step1, step5);
            pfc.Bind(step5, step2);
            pfc.Bind(step5, step5);
            pfc.Bind(step3, finishStep);

            pfc.UpdateStructure();

            StringWriter sw = new StringWriter();
            foreach (IPfcNode node in pfc.Nodes) {
                sw.WriteLine(node.Name + " : " + node.GraphOrdinal);// + ", and Guid = " + node.Guid.ToString());
            }
            string resultString = sw.GetStringBuilder().ToString();
            Console.WriteLine(resultString);
            Assert.AreEqual("START : 0\r\nT_000 : 1\r\nSTEP1 : 2\r\nSTEP4 : 3\r\nT_003 : 4\r\nT_002 : 5\r\nSTEP5 : 6\r\nSTEP6 : 7\r\nT_004 : 8\r\nT_005 : 9\r\nSTEP2 : 10\r\nT_001 : 11\r\nSTEP3 : 12\r\nT_006 : 13\r\nFINISH : 14\r\n", resultString);
        }

        [TestMethod]
        public void Test_ComplexSeriesBranchingPfcUpdateStructure() {
            int nFailures = 0;
            for (int i = 0; i < 20; i++) {
                try {
                    _Test_ComplexSeriesBranchingPfcUpdateStructure();
                } catch {
                    nFailures++;
                }
            }
            Assert.AreEqual(0, nFailures, "There were " + nFailures + " failures, and should have been none.");
        }

        public void _Test_ComplexSeriesBranchingPfcUpdateStructure() {
            //        START
            //          |
            //      ____|______
            //     +           + 
            //     |           |
            //   STEP3      STEP1
            //     |         | |
            //     + ________+ +
            //     | |         |
            //   STEP2       STEP4
            //     +           + 
            //     |___________|
            //          +
            //        FINISH
            //
            string testName = "PFC with Loop";
            IModel model = new Model(testName);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);
            ((PfcElementFactory)pfc.ElementFactory).SetRepeatable(Guid.Empty);
            Guid g = Guid.Empty;
            g = GuidOps.Increment(g);
            IPfcNode startStep = pfc.CreateStep("START", string.Empty, g = GuidOps.Increment(g));
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, g = GuidOps.Increment(g));
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, g = GuidOps.Increment(g));
            IPfcNode step3 = pfc.CreateStep("STEP3", string.Empty, g = GuidOps.Increment(g));
            IPfcNode step4 = pfc.CreateStep("STEP4", string.Empty, g = GuidOps.Increment(g));
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, g = GuidOps.Increment(g));

            pfc.BindSeriesDivergent(startStep, new IPfcNode[] { step3, step1 });
            pfc.BindSeriesDivergent(step1, new IPfcNode[] { step2, step4 });
            pfc.BindSeriesConvergent(new IPfcNode[] { step3, step1 }, step2);
            pfc.BindSeriesConvergent(new IPfcNode[] { step2, step4 }, finishStep);

            pfc.UpdateStructure();

            StringWriter sw = new StringWriter();
            foreach (IPfcNode node in pfc.Nodes) {
                sw.Write(node.Name + " : " + node.GraphOrdinal);
                if (node.ElementType == PfcElementType.Transition) {
                    sw.WriteLine(" - from " + node.PredecessorNodes[0].Name + " to " + node.SuccessorNodes[0].Name + " )");
                } else {
                    sw.WriteLine();
                }
            }
            string resultString = sw.GetStringBuilder().ToString();
            Console.WriteLine(resultString);
            Assert.AreEqual("START : 0\r\nT_000 : 1 - from START to STEP3 )\r\nT_001 : 2 - from START to STEP1 )\r\nSTEP3 : 3\r\nSTEP1 : 4\r\nT_004 : 5 - from STEP3 to STEP2 )\r\nT_002 : 6 - from STEP1 to STEP2 )\r\nT_003 : 7 - from STEP1 to STEP4 )\r\nSTEP2 : 8\r\nSTEP4 : 9\r\nT_005 : 10 - from STEP2 to FINISH )\r\nT_006 : 11 - from STEP4 to FINISH )\r\nFINISH : 12\r\n", resultString);
            // System.Diagnostics.Debug.Assert(resultString.Equals("START : 0\r\nT_000 : 1 - from START to STEP3 )\r\nT_001 : 2 - from START to STEP1 )\r\nSTEP3 : 3\r\nSTEP1 : 4\r\nT_004 : 5 - from STEP3 to STEP2 )\r\nT_002 : 6 - from STEP1 to STEP2 )\r\nT_003 : 7 - from STEP1 to STEP4 )\r\nSTEP2 : 8\r\nSTEP4 : 9\r\nT_005 : 10 - from STEP2 to FINISH )\r\nT_006 : 11 - from STEP4 to FINISH )\r\nFINISH : 12\r\n"));
        }

        [TestMethod]
        public void Test_LinkPrioritization() {
            //        START
            //          |  
            //  L0  ____|_____ L2
            // T0  +          + T1
            // L1  |          | L3
            //   STEP1      STEP2
            //  L4 |          | L6
            //  T2 +__________+ T3
            //       L5 | L7     
            //          |
            //        FINISH
            //
            string testName = "PFC with Loop";
            IModel model = new Model(testName);

            string primaryPath = null;
            
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(model, testName);

            IPfcNode startStep = pfc.CreateStep("START", string.Empty, Guid.NewGuid());
            IPfcNode step1 = pfc.CreateStep("STEP1", string.Empty, Guid.NewGuid());
            IPfcNode step2 = pfc.CreateStep("STEP2", string.Empty, Guid.NewGuid());
            IPfcNode finishStep = pfc.CreateStep("FINISH", string.Empty, Guid.NewGuid());

            pfc.BindSeriesDivergent(startStep, new IPfcNode[] { step1, step2 });
            pfc.BindSeriesConvergent(new IPfcNode[] { step1, step2 }, finishStep);

            step1.Predecessors[0].Priority = 101; // L1 <-- Priority 101
            step1.Predecessors[0].Predecessor.Predecessors[0].Priority = 101; // L0 <-- Priority 101.
            pfc.MakeLinkPrimary(step1.Successors[0]); // L4 <-- Priority MaxValue.
            pfc.UpdateStructure();
            primaryPath = PfcAnalyst.GetPrimaryPathAsString(startStep, true);
            Assert.AreEqual(primaryPath,"START, STEP1 and FINISH");

            step2.Predecessors[0].Priority = 1;
            step2.Predecessors[0].Predecessor.Predecessors[0].Priority = 1;
            step1.Successors[0].Priority = 0;
            step1.Predecessors[0].Predecessor.Predecessors[0].Priority = 0;
            pfc.UpdateStructure();
            primaryPath = PfcAnalyst.GetPrimaryPathAsString(startStep, true);
            Assert.AreEqual(primaryPath,"START, STEP2 and FINISH");

            step1.Predecessors[0].Priority = 1;
            step1.Predecessors[0].Predecessor.Predecessors[0].Priority = 1;
            step2.Predecessors[0].Priority = 0;
            step2.Predecessors[0].Predecessor.Predecessors[0].Priority = 0;
            pfc.UpdateStructure();
            primaryPath = PfcAnalyst.GetPrimaryPathAsString(startStep, true);
            Assert.AreEqual(primaryPath,"START, STEP1 and FINISH");

            pfc = pfcs.CreateTestPfc();
            pfc.UpdateStructure();
            primaryPath = PfcAnalyst.GetPrimaryPathAsString(pfcs.nA, true);
            Assert.AreEqual(primaryPath,"Step_A, Step_B, Step_C, Step_D, Step_E, Step_N and Step_O");

            pfcs.nF.PredecessorNodes[0].Predecessors[0].Priority = 1;
            pfc.UpdateStructure();
            primaryPath = PfcAnalyst.GetPrimaryPathAsString(pfcs.nA, true);
            Assert.AreEqual(primaryPath,"Step_A, Step_B, Step_F, Step_G, Step_H, Step_J, Step_L, Step_M, Step_N and Step_O");

        }

        [TestMethod]
        public void Test_InfiniteLoopError() {
            string testName = "Test infinite loop error.";

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(null, testName);
            IPfcStepNode start = pfc.CreateStep("Start", "", Guid.Empty);
            IPfcStepNode step1 = pfc.CreateStep("Step1", "", Guid.Empty);
            IPfcStepNode step2 = pfc.CreateStep("Step2", "", Guid.Empty);
            IPfcStepNode step3 = pfc.CreateStep("Step3", "", Guid.Empty);
            IPfcStepNode step4 = pfc.CreateStep("Step4", "", Guid.Empty);
            IPfcStepNode finish = pfc.CreateStep("Finish", "", Guid.Empty);

            pfc.Bind(start, step1);
            pfc.Bind(step1, step2);
            pfc.Bind(step2, step3);
            pfc.Bind(step3, finish);
            pfc.Bind(start, step1);
            pfc.Bind(step1, step4);
            pfc.Bind(step4, step3);
            pfc.Bind(step3, step1);

            Console.WriteLine(PfcDiagnostics.GetStructure(pfc));
            List<IPfcNode> targets = PfcAnalyst.GetPermissibleTargetsForLinkFrom(pfc.Nodes["T_006"]);

            Console.WriteLine("Legal targets outbound from T_006 are : " + Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedListOfNames<IPfcNode>(targets) + ".");
            //            targets.ForEach(delegate(IPfcNode node) { Console.WriteLine(node.ToString()); });

        }

        [TestMethod]
        public void Test_OffSetParallelism() {
            string testName = "Test offset parallelism.";

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(null, testName);
            IPfcStepNode start = pfc.CreateStep("Start", "", Guid.Empty);
            IPfcStepNode finish = pfc.CreateStep("Finish", "", Guid.Empty);

            char name = 'A';
            pfcs.A = pfc.CreateStep("Step_" + ( name++ ), "", pfcs.NextGuid());
            pfcs.B = pfc.CreateStep("Step_" + ( name++ ), "", pfcs.NextGuid());
            pfcs.C = pfc.CreateStep("Step_" + ( name++ ), "", pfcs.NextGuid());
            pfcs.D = pfc.CreateStep("Step_" + ( name++ ), "", pfcs.NextGuid());
            pfcs.E = pfc.CreateStep("Step_" + ( name++ ), "", pfcs.NextGuid());
            pfcs.F = pfc.CreateStep("Step_" + ( name++ ), "", pfcs.NextGuid());
            pfcs.G = pfc.CreateStep("Step_" + ( name++ ), "", pfcs.NextGuid());

            pfcs.nA = (IPfcNode)pfcs.A;
            pfcs.nB = (IPfcNode)pfcs.B;
            pfcs.nC = (IPfcNode)pfcs.C;
            pfcs.nD = (IPfcNode)pfcs.D;
            pfcs.nE = (IPfcNode)pfcs.E;
            pfcs.nF = (IPfcNode)pfcs.F;
            pfcs.nG = (IPfcNode)pfcs.G;

            pfc.Bind(start, pfcs.nA);
            pfc.Bind(pfcs.nA, pfcs.nB);
            pfc.Bind(pfcs.nB, pfcs.nE);
            pfc.Bind(pfcs.nE, pfcs.nF);
            pfc.Bind(pfcs.nF, pfcs.nG);
            pfc.Bind(pfcs.nG, finish);
            pfc.Bind(( (PfcTransition)( (PfcStep)pfcs.nA ).SuccessorNodes[0] ), pfcs.nC);
            pfc.Bind(pfcs.nC, ( (PfcTransition)( (PfcStep)pfcs.nE ).SuccessorNodes[0] ));
            pfc.Bind(( (PfcTransition)( (PfcStep)pfcs.nB ).SuccessorNodes[0] ), pfcs.nD);
            pfc.Bind(pfcs.nD, ( (PfcTransition)( (PfcStep)pfcs.nF ).SuccessorNodes[0] ));

            pfc.UpdateStructure();

            if (m_dumpStructure) {
                PfcNodeList nodes = pfc.Nodes;
                nodes.Sort(
                        new Comparison<IPfcNode>(
                            delegate(IPfcNode n1, IPfcNode n2) { return Comparer.Default.Compare(n1.GraphOrdinal, n2.GraphOrdinal); }));
                foreach (PfcNode node in nodes) {
                    Console.WriteLine("{0} goes to {1}", node.Name, Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedList(node.SuccessorNodes, n => n.Name));
                }
            }

            PfcValidator pfcv = new PfcValidator(pfc);

            Assert.IsTrue(pfcv.PfcIsValid());

        }

        [TestMethod]
        public void Test_Validator() {
            //ProcedureFunctionChart pfc = CreateLoopTestPfc();
            ProcedureFunctionChart pfc = pfcs.CreateTestPfc();
            PfcValidator pfcv = new PfcValidator(pfc);

            Assert.IsTrue(pfcv.PfcIsValid());
        }

        private static bool m_dumpStructure = true;

        [TestMethod]
        public void Test_ValidatorFromStoredPFC() {
            int nReps = 1000000;
            int nSteps = 8;
            int randomNumber = -1;
            Random randomNumGen = new Random();


            TextWriter tw = new StreamWriter("Results.txt", true);

            PfcValidator.m_diagnostics = m_dumpStructure;

            int[] cases = new int[] { 2, 3, 4, 5, 98 };
            
            //int[] cases = new int[] { 99 }; // Mass-random testing.
            foreach (int _case in cases) {
                for (int i = 0; i < nReps; i++) {
                    //try {
                        ProcedureFunctionChart pfc;
                        switch (_case) {
                            case 2:
                                pfc = pfcs.CreateTestPfc();
                                i = nReps;
                                break;
                            case 3:
                                pfc = pfcs.CreateTestPfc2();
                                i = nReps;
                                break;
                            case 4:
                                pfc = pfcs.CreateTestPfc3();
                                i = nReps;
                                break;
                            case 5:
                                pfc = pfcs.CreateTestPfc4();
                                i = nReps;
                                break;
                            case 98:
                                pfc = pfcs.CreateStandardPFC(_case);
                                i = nReps;
                                break;
                            case 99:
                                PfcValidator.m_diagnostics = m_dumpStructure = false;
                                nSteps = i / 50;
                                if (nSteps < 8) nSteps = 8;
                                //nSteps = 24;
                                Console.WriteLine("------------------ Case {0}, {1} steps in PFC ------------------", i, nSteps);
                                randomNumber = randomNumGen.Next();
                                pfc = pfcs.CreateRandomPFC(nSteps, randomNumber);
                                break;
                            default:
                                pfc = null;
                                break;
                        }

                        pfc.UpdateStructure();
                        if (PfcValidator.m_diagnostics) {
                            DumpStructure(pfc);
                        }

                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        PfcValidator pfcv = new PfcValidator(pfc);

                        Assert.IsTrue(pfcv.PfcIsValid(), "case", randomNumber);
                }
            }

            tw.Flush();
            tw.Close();
            //Console.ReadLine();
        }

        [TestMethod]
        public void TestBroadestNonLoopbackPath() {

            foreach (int _case in Enumerable.Range(84, 15)) {
                Console.WriteLine("Testing case {0}:", _case);
                ProcedureFunctionChart pfc = pfcs.CreateStandardPFC(_case);
                DumpStructure(pfc);
                List<IPfcNode> allNodes = new List<IPfcNode>(pfc.Nodes);
                List<IPfcNode> path = PfcAnalyst.GetNodesOnBroadestNonLoopingPath(pfc, restoreOldLinkPriorities: false);
                Console.WriteLine("Executed path is : " + StringOperations.ToCommasAndAndedList(path, n => n.Name));

                foreach (PfcNode node in pfc.Nodes.OrderBy(n => n.GraphOrdinal)) {
                    Console.WriteLine("Node {0} has graph ordinal {1}.", node.Name, node.GraphOrdinal);
                    foreach (PfcLink link in node.Successors.OrderByDescending(n => n.Priority)) {
                        Console.WriteLine("\tLink to {0} has priority {1}.", link.Successor.Name, link.Priority);
                    }
                }
            }

        }

        private void DumpStructure(ProcedureFunctionChart pfc) {
            pfc.UpdateStructure();
            PfcNodeList nodes = pfc.Nodes;
            nodes.Sort(
                    new Comparison<IPfcNode>(
                        delegate(IPfcNode n1, IPfcNode n2) { return Comparer.Default.Compare(n1.GraphOrdinal, n2.GraphOrdinal); }));
            foreach (PfcNode node in nodes) {
                Console.WriteLine("{0} goes to {1}", node.Name, Highpoint.Sage.Utility.StringOperations.ToCommasAndAndedList(node.SuccessorNodes, n => n.Name));
            }
        }
    }
}
