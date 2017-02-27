/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.Graphs.PFC;
using System.Collections;
using Highpoint.Sage.Utility;

namespace SageTestLib {
    public static class TestPfcRepository {

        public static ProcedureFunctionChart CreateStandardPFC(int number) {
            ProcedureFunctionChart pfc = null;

            switch (number) {
                case 2:
                    pfc = CreateTestPfc();
                    break;
                case 3:
                    pfc = CreateTestPfc2();
                    break;
                case 4:
                    pfc = CreateTestPfc3();
                    break;
                case 5:
                    pfc = CreateTestPfc4();
                    break;
                case 6: // A Flip-flop PFC.
                    pfc = CreateTestPfc5();
                    break;
                case 82:
                    pfc = CreateRandomPFC(8, 1076501454);
                    break;
                case 83:
                    pfc = CreateRandomPFC(15, 1443487589);
                    break;
                case 84:
                    pfc = CreateRandomPFC(15, 919145039);
                    break;
                case 85:
                    pfc = CreateRandomPFC(15, 1915039786);
                    break;
                case 86:
                    pfc = CreateRandomPFC(14, 1576245265);
                    break;
                case 87:
                    pfc = CreateRandomPFC(14, 1359939537);
                    break;
                case 88:
                    pfc = CreateRandomPFC(12, 3666920);
                    break;
                case 89:
                    pfc = CreateRandomPFC(12, 1688960816);
                    break;
                case 90:
                    pfc = CreateRandomPFC(12, 1444166439);
                    break;
                case 91:
                    pfc = CreateRandomPFC(12, 84258233);
                    break;
                case 92:
                    pfc = CreateRandomPFC(7, 1174395592);
                    break;
                case 93:
                    pfc = CreateRandomPFC(7, 1479576831);
                    break;
                case 94:
                    pfc = CreateRandomPFC(7, 1514213606);
                    break;
                case 95:
                    pfc = CreateRandomPFC(7, 953319173);
                    break;
                case 96:
                    pfc = CreateRandomPFC(7, 376952253);
                    break;
                case 97:
                    pfc = CreateRandomPFC(7, 1020353150);
                    break;
                case 98:
                    pfc = CreateRandomPFC(7, 585340845);
                    break;
                default:
                    break;

            }
            return pfc;
        }

        public static IPfcElement A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P;
        public static IPfcNode nA, nB, nC, nD, nE, nF, nG, nH, nI, nJ, nK, nL, nM, nN, nO, nP;


        public static ProcedureFunctionChart CreateOffsetParallelPFC() {
            ProcedureFunctionChart pfc = new ProcedureFunctionChart(null, "OffsetParallelPfc");
            IPfcStepNode start = pfc.CreateStep("Start", "", Guid.Empty);
            IPfcStepNode finish = pfc.CreateStep("Finish", "", Guid.Empty);

            char name = 'A';
            A = pfc.CreateStep("Step_" + ( name++ ), "", NextGuid());
            B = pfc.CreateStep("Step_" + ( name++ ), "", NextGuid());
            C = pfc.CreateStep("Step_" + ( name++ ), "", NextGuid());
            D = pfc.CreateStep("Step_" + ( name++ ), "", NextGuid());
            E = pfc.CreateStep("Step_" + ( name++ ), "", NextGuid());
            F = pfc.CreateStep("Step_" + ( name++ ), "", NextGuid());
            G = pfc.CreateStep("Step_" + ( name++ ), "", NextGuid());

            nA = (IPfcNode)A;
            nB = (IPfcNode)B;
            nC = (IPfcNode)C;
            nD = (IPfcNode)D;
            nE = (IPfcNode)E;
            nF = (IPfcNode)F;
            nG = (IPfcNode)G;

            pfc.Bind(start, nA);
            pfc.Bind(nA, nB);
            pfc.Bind(nB, nE);
            pfc.Bind(nE, nF);
            pfc.Bind(nF, nG);
            pfc.Bind(nG, finish);
            pfc.Bind(( (PfcTransition)( (PfcStep)nA ).SuccessorNodes[0] ), nC);
            pfc.Bind(nC, ( (PfcTransition)( (PfcStep)nE ).SuccessorNodes[0] ));
            pfc.Bind(( (PfcTransition)( (PfcStep)nB ).SuccessorNodes[0] ), nD);
            pfc.Bind(nD, ( (PfcTransition)( (PfcStep)nF ).SuccessorNodes[0] ));

            pfc.UpdateStructure();

            return pfc;
        }

        public static ProcedureFunctionChart CreateTestPfc() {

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(new Highpoint.Sage.SimCore.Model("Test model", Guid.NewGuid()), "SFC 1");
            ((PfcElementFactory)pfc.ElementFactory).SetRepeatable(Guid.Empty); // Ensures Guids are repeatable.

            #region Create Nodes

            char name = 'A';
            A = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            B = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            C = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            D = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            E = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            F = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            G = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            H = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            I = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            J = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            K = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            L = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            M = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            N = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            O = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            P = pfc.CreateStep("Step_" + (name), "", NextGuid());

            nA = (IPfcNode)A;
            nB = (IPfcNode)B;
            nC = (IPfcNode)C;
            nD = (IPfcNode)D;
            nE = (IPfcNode)E;
            nF = (IPfcNode)F;
            nG = (IPfcNode)G;
            nH = (IPfcNode)H;
            nI = (IPfcNode)I;
            nJ = (IPfcNode)J;
            nK = (IPfcNode)K;
            nL = (IPfcNode)L;
            nM = (IPfcNode)M;
            nN = (IPfcNode)N;
            nO = (IPfcNode)O;
            nP = (IPfcNode)P;

            #endregion Create Nodes

            #region Create Structure

            pfc.Bind(nA, nB);
            pfc.BindSeriesDivergent(nB, new IPfcNode[] { nC, nF });
            pfc.Bind(nC, nD);
            pfc.Bind(nD, nE);
            pfc.Bind(nF, nG);
            pfc.BindParallelDivergent(nG, new IPfcNode[] { nH, nI, nP });
            pfc.Bind(nH, nJ);
            pfc.Bind(nI, nK);
            pfc.BindParallelConvergent(new IPfcNode[] { nJ, nK, nP }, nL);
            pfc.Bind(nL, nM);
            pfc.BindSeriesConvergent(new IPfcNode[] { nE, nM }, nN);
            pfc.Bind(nN, nO);
            pfc.Bind(nB, nN);

            #endregion Create Structure

            pfc.UpdateStructure();

            return pfc;
        }

        public static ProcedureFunctionChart CreateTestPfc2() {

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(new Highpoint.Sage.SimCore.Model("Test model", Guid.NewGuid()), "SFC 1");

            #region Create Nodes

            char name = 'A';
            A = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            B = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            C = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            D = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            E = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            F = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            G = pfc.CreateStep("Step_" + (name++), "", NextGuid());

            nA = (IPfcNode)A;
            nB = (IPfcNode)B;
            nC = (IPfcNode)C;
            nD = (IPfcNode)D;
            nE = (IPfcNode)E;
            nF = (IPfcNode)F;
            nG = (IPfcNode)G;

            #endregion Create Nodes

            #region Create Structure

            pfc.BindParallelDivergent(nA, new IPfcNode[] { nB, nC });
            pfc.BindParallelDivergent(nB, new IPfcNode[] { nD, nE });
            pfc.BindParallelConvergent(new IPfcNode[] { nD, nE }, nF);
            pfc.BindParallelConvergent(new IPfcNode[] { nF, nC }, nG);

            PfcLinkElementList links = new PfcLinkElementList(pfc.Links);
            links.Sort(new Comparison<IPfcLinkElement>(delegate(IPfcLinkElement a, IPfcLinkElement b) {
                return Comparer.Default.Compare(a.Name, b.Name);
            }));


            System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            foreach (IPfcLinkElement link in links) {
                typeof(PfcElement).GetFields(bf);
                typeof(PfcElement).GetField("m_guid", bf).SetValue((PfcElement)link, NextGuid()); // Totally cheating.
            }

            #endregion Create Structure

            return pfc;
        }

        public static ProcedureFunctionChart CreateTestPfc3() {

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(new Highpoint.Sage.SimCore.Model("Test model", Guid.NewGuid()), "SFC 1");

            #region Create Nodes

            char name = 'A';
            A = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            B = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            C = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            D = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            E = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            F = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            G = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            H = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            I = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            J = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            K = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            L = pfc.CreateStep("Step_" + (name++), "", NextGuid());

            nA = (IPfcNode)A;
            nB = (IPfcNode)B;
            nC = (IPfcNode)C;
            nD = (IPfcNode)D;
            nE = (IPfcNode)E;
            nF = (IPfcNode)F;
            nG = (IPfcNode)G;
            nH = (IPfcNode)H;
            nI = (IPfcNode)I;
            nJ = (IPfcNode)J;
            nK = (IPfcNode)K;
            nL = (IPfcNode)L;

            #endregion Create Nodes

            #region Create Structure

            pfc.BindParallelDivergent(nA, new IPfcNode[] { nB, nC, nD, nE });
            pfc.BindParallelDivergent(nB, new IPfcNode[] { nF, nG });
            pfc.BindParallelDivergent(nE, new IPfcNode[] { nJ, nK });

            pfc.BindParallelConvergent(new IPfcNode[] { nF, nG }, nH);
            pfc.BindParallelConvergent(new IPfcNode[] { nC, nD }, nI);
            pfc.BindParallelConvergent(new IPfcNode[] { nH, nI, nJ, nK }, nL);

            PfcLinkElementList links = new PfcLinkElementList(pfc.Links);
            links.Sort(new Comparison<IPfcLinkElement>(delegate(IPfcLinkElement a, IPfcLinkElement b) {
                return Comparer.Default.Compare(a.Name, b.Name);
            }));


            System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            foreach (IPfcLinkElement link in links) {
                typeof(PfcElement).GetFields(bf);
                typeof(PfcElement).GetField("m_guid", bf).SetValue((PfcElement)link, NextGuid()); // Totally cheating.
            }

            #endregion Create Structure

            return pfc;
        }

        public static ProcedureFunctionChart CreateTestPfc4() {

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(new Highpoint.Sage.SimCore.Model("Test model", Guid.NewGuid()), "SFC 1");

            #region Create Nodes

            char name = 'A';
            A = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            B = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            C = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            D = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            E = pfc.CreateStep("Step_" + (name++), "", NextGuid());

            nA = (IPfcNode)A;
            nB = (IPfcNode)B;
            nC = (IPfcNode)C;
            nD = (IPfcNode)D;
            nE = (IPfcNode)E;

            #endregion Create Nodes

            #region Create Structure

            pfc.Bind(nA, nB);
            pfc.Bind(nB, nE);
            pfc.Bind(nA.SuccessorNodes[0], nC);
            pfc.Bind(nC, nD);
            pfc.Bind(nD, nE.PredecessorNodes[0]);

            PfcLinkElementList links = new PfcLinkElementList(pfc.Links);
            links.Sort(new Comparison<IPfcLinkElement>(delegate(IPfcLinkElement a, IPfcLinkElement b) {
                return Comparer.Default.Compare(a.Name, b.Name);
            }));


            System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            foreach (IPfcLinkElement link in links) {
                typeof(PfcElement).GetFields(bf);
                typeof(PfcElement).GetField("m_guid", bf).SetValue((PfcElement)link, NextGuid()); // Totally cheating.
            }

            #endregion Create Structure

            return pfc;
        }

        public static ProcedureFunctionChart CreateTestPfc5() {

            // Flip-flop pattern.

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(new Highpoint.Sage.SimCore.Model("Test model", Guid.NewGuid()), "SFC 1");

            #region Create Nodes

            char name = 'A';
            A = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            B = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            C = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            D = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            E = pfc.CreateStep("Step_" + (name++), "", NextGuid());
            F = pfc.CreateStep("Step_" + (name++), "", NextGuid());

            nA = (IPfcNode)A;
            nB = (IPfcNode)B;
            nC = (IPfcNode)C;
            nD = (IPfcNode)D;
            nE = (IPfcNode)E;
            nF = (IPfcNode)F;

            #endregion Create Nodes

            #region Create Structure

            pfc.BindParallelDivergent(nA, new IPfcNode[] { nB, nC });
            pfc.BindSeriesDivergent(nB, new IPfcNode[] { nD, nE });
            pfc.BindSeriesDivergent(nC, new IPfcNode[] { nD, nE });
            pfc.BindParallelConvergent(new IPfcNode[] { nD, nE }, nF);

            PfcLinkElementList links = new PfcLinkElementList(pfc.Links);
            links.Sort(new Comparison<IPfcLinkElement>(delegate(IPfcLinkElement a, IPfcLinkElement b) {
                return Comparer.Default.Compare(a.Name, b.Name);
            }));


            System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            foreach (IPfcLinkElement link in links) {
                typeof(PfcElement).GetFields(bf);
                typeof(PfcElement).GetField("m_guid", bf).SetValue((PfcElement)link, NextGuid()); // Totally cheating.
            }

            //pfc.Bind(nD, pfc.Nodes["T_005"]);

            #endregion Create Structure

            return pfc;
        }

        public static ProcedureFunctionChart CreateRandomPFC(int nSteps, int seed) {

            Guid mask = GuidOps.FromString(string.Format("{0}, {1}", nSteps, seed));
            Guid seedGuid = GuidOps.FromString(string.Format("{0}, {1}", seed, nSteps));
            int rotate = 3;

            GuidGenerator guidGen = new GuidGenerator(seedGuid, mask, rotate);
            PfcElementFactory pfcef = new PfcElementFactory(guidGen);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(new Highpoint.Sage.SimCore.Model("Test model", Guid.NewGuid()), "Name", "", guidGen.Next(), pfcef);

            IPfcStepNode start = pfc.CreateStep("Start", "", Guid.Empty);
            IPfcStepNode step1 = pfc.CreateStep("Step1", "", Guid.Empty);
            IPfcStepNode finish = pfc.CreateStep("Finish", "", Guid.Empty);

            pfc.Bind(start, step1);
            pfc.Bind(step1, finish);

            Console.WriteLine("Seed = {0}.", seed);

            Random r = new Random(seed);
            while (pfc.Steps.Count < nSteps) {

                double steeringValue = r.NextDouble();

                if (steeringValue < .5) {
                    // Insert a step in series.
                    IPfcLinkElement link = pfc.Links[r.Next(0, pfc.Links.Count - 1)];
                    IPfcStepNode stepNode = pfc.CreateStep();
                    pfc.Bind(link.Predecessor, stepNode);
                    pfc.Bind(stepNode, link.Successor);
                    //Console.WriteLine("Inserted {0} between {1} and {2}.", stepNode.Name, link.Predecessor.Name, link.Successor.Name);
                    link.Detach();

                } else if (steeringValue < .666) {
                    // Insert a step in parallel.
                    for (int i = 0; i < 50; i++) { // Try, but give up if don't find suitable step.
                        IPfcStepNode target = pfc.Steps[r.Next(0, pfc.Steps.Count - 1)];
                        if (target.PredecessorNodes.Count == 1 && target.SuccessorNodes.Count == 1) {
                            IPfcStepNode stepNode = pfc.CreateStep();
                            pfc.Bind(target.PredecessorNodes[0], stepNode);
                            pfc.Bind(stepNode, target.SuccessorNodes[0]);
                            //Console.WriteLine("Inserted {0} parallel to {1}.", stepNode.Name, target.Name);
                            break;
                        }
                    }

                } else if (steeringValue < .833) {
                    // Insert a branch
                    for (int i = 0; i < 50; i++) { // Try, but give up if don't find suitable step.
                        IPfcStepNode step = pfc.Steps[r.Next(0, pfc.Steps.Count - 1)];
                        if (step.PredecessorNodes.Count == 1 && step.SuccessorNodes.Count == 1) {
                            IPfcStepNode entryStep = pfc.CreateStep(step.Name+"_IN",null,Guid.Empty);
                            IPfcStepNode exitStep = pfc.CreateStep(step.Name + "_OUT", null, Guid.Empty);
                            IPfcStepNode leftStep = pfc.CreateStep(step.Name + "_LFT", null, Guid.Empty);
                            IPfcStepNode rightStep = pfc.CreateStep(step.Name + "_RGT", null, Guid.Empty);
                            pfc.Bind(step.PredecessorNodes[0], entryStep);
                            pfc.Bind(entryStep, leftStep);
                            pfc.Bind(entryStep, rightStep);
                            pfc.Bind(leftStep, exitStep);
                            pfc.Bind(rightStep, exitStep);
                            pfc.Bind(exitStep, step.SuccessorNodes[0]);
                            pfc.Unbind(step.PredecessorNodes[0], step);
                            pfc.Unbind(step,step.SuccessorNodes[0]);
                            //Console.WriteLine("Inserted a branch in place of {0}.", step.Name);
                            break;
                        }
                    }

                } else {
                    for (int i = 0; i < 50; i++) { // Try, but give up if don't find suitable step.
                        IPfcTransitionNode trans = pfc.Transitions[r.Next(0, pfc.Transitions.Count - 1)];
                        if (trans.PredecessorNodes.Count == 1 && trans.SuccessorNodes.Count == 1) {
                            IPfcStepNode successor = (IPfcStepNode)trans.SuccessorNodes[0];
                            IPfcStepNode subject = pfc.CreateStep();
                            pfc.Bind(trans, subject);
                            pfc.Bind(subject, successor);
                            pfc.Unbind(trans, successor);
                            IPfcStepNode loopback = pfc.CreateStep();
                            pfc.Bind(subject, loopback);
                            pfc.Bind(loopback, subject);
                            //Console.WriteLine("Inserted {0} between {1} and {2}, and created a branchback around it using {3}.",
                            //    subject.Name, trans.PredecessorNodes[0].Name, successor.Name, loopback.Name);
                            break;
                        }
                    }
                    //// insert a loopback
                    //IPfcStepNode step;
                    //do { step = pfc.Steps[r.Next(0, pfc.Steps.Count - 1)]; } while (step == start || step == finish);
                    //IPfcStepNode newNode = pfc.CreateStep();
                    //pfc.Bind(step, newNode);
                    //pfc.Bind(newNode, step);
                    //Console.WriteLine("Inserted a loopback around {0} using new step, {1}.", step.Name, newNode.Name);

                }

                //IPfcStepNode origin = pfc.Steps[r.Next(0, pfc.Steps.Count - 1)];
                //if (origin.Equals(finish)) continue;

                //if (r.NextDouble() < .2) {
                //    IPfcStepNode stepNode = pfc.CreateStep();
                //    IPfcNode target = origin.SuccessorNodes[r.Next(0, origin.SuccessorNodes.Count - 1)];
                //    // Insert a step in series.
                //    pfc.Bind(origin, stepNode);
                //    pfc.Bind(stepNode, target);
                //    pfc.Unbind(origin, target);
                //    Console.WriteLine("Inserting {0} between {1} and {2}.", 
                //        stepNode.Name, origin.Name, target.Name);


                //} else if (r.NextDouble() < .55) {
                //    // Insert a step in parallel
                //    if (origin.PredecessorNodes.Count == 1 && origin.SuccessorNodes.Count == 1) {
                //        origin = origin.PredecessorNodes[0];
                //        target = origin.SuccessorNodes[0];
                //        pfc.Bind(origin, stepNode);
                //        pfc.Bind(stepNode, target);
                //        Console.WriteLine("Inserting {0} parallel to {1} - between {2} and {3}.", 
                //            stepNode.Name, parallelTo.Name, origin.Name, target.Name);

                //    }

                //} else {
                //    // Insert a loopback or branchforward.
                //    IPfcNode target = null;
                //    string parallelType = null;
                //    if (!origin.PredecessorNodes.Contains(start) && r.NextDouble() < .5) {
                //        target = origin;
                //        parallelType = "loopback";
                //    } else if (origin.SuccessorNodes.Count==1 && origin.PredecessorNodes==1) {
                //        target = origin.SuccessorNodes[r.Next(0, origin.SuccessorNodes.Count - 1)];
                //        parallelType = "branch forward";
                //    }

                //    if (target != null) {
                //        IPfcStepNode stepNode = pfc.CreateStep();
                //        pfc.Bind(origin, stepNode);
                //        pfc.Bind(stepNode, target);
                //        Console.WriteLine("Inserting {0} around {1} to {2}, with {3} on the new alternate path.",
                //            parallelType, origin.Name, target.Name, stepNode.Name);
                //    }
                //}
            }

            return pfc;

        }
       
        private static Guid s_guid = new Guid("{00000000-0000-0000-0000-000000000001}");

        public static Guid NextGuid() {
            s_guid = Highpoint.Sage.Utility.GuidOps.Increment(s_guid);
            return s_guid;
        }

        public static ProcedureFunctionChart CreateLoopTestPfc() {

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(new Highpoint.Sage.SimCore.Model("Test model", Guid.NewGuid()), "SFC 1");

            #region Create Nodes

            A = pfc.CreateStep("Step_A", "", Guid.NewGuid());
            B = pfc.CreateStep("Step_B", "", Guid.NewGuid());
            C = pfc.CreateStep("Step_C", "", Guid.NewGuid());

            nA = (IPfcNode)A;
            nB = (IPfcNode)B;
            nC = (IPfcNode)C;

            #endregion Create Nodes

            #region Create Structure

            pfc.Bind(nA, nB);
            pfc.Bind(nB, nC);
            pfc.Bind(nB, nB);

            #endregion Create Structure

            return pfc;
        }
    }
}