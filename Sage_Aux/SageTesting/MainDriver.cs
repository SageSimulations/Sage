/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;

namespace Highpoint.Sage.Testing
{
    using SimCore;
    using Resources;
    using _Debug = System.Diagnostics.Debug;

    public class AllTests
    {

        public static void Main(string[] args)
        {
            #region Test Cases

            //int[] protocol = new int[]{};							// No test protocol.

            //int[] protocol = new int[] { 221 };					    // Linear Double Interpolator.
            //int[] protocol = new int[] { 12 };					// Executive Pause/Resume.
            //int[] protocol = new int[]{200,201};					// BranchBlockTester.
            //int[] protocol = new int[]{/*210,211,212,*/213/**/};	// Servers.
            //int[] protocol = new int[]{240,241,200,243}			// Interpolation Testing
            //int[] protocol = new int[]{250};						// Interpolation Testing
            //int[] protocol = new int[]{260,261,262,263};			// Prioritized Resource Requests
            //int[] protocol = new int[]{300,301,302,303,304,305};	// Tuple Space testing.
            //int[] protocol = new int[]{310,311,312};				// TreeNodeHelper testing.
            //int[] protocol = new int[]{320,321};					// Graph Branching testing.
            //int[] protocol = new int[]{9};
            //int[] protocol = new int[]{402/*400,401,402,404,405,406,407,408,409*/};	// TimePeriod testing.
            //int[] protocol = new int[]{140,141,142, 143, 144, 145};				// Mersenne Tester
            //int[] protocol = new int[]{420,421,422};						// Vapor Pressure testing.

            //int[] protocol = new int[]{130,131};                  // Chemistry

            //int[] protocol = new int[]{61,62,63,64,65,66,67,68};	// Graph validities.
            //int[] protocol = new int[]{68};

            //int[] protocol = new int[]{440}; // Emissions mega-tester.
            //int[] protocol = new int[]{473}; // Emissions multi-material fill.
            //int[] protocol = new int[]{441,442,443,444,445,446,447,448,449,450,451,452}; // Early bound emissions.
            //int[] protocol = new int[]{461,462,463,464,465,466,467,468,469,470,471,472}; // Late bound emissions.
            //int[] protocol = new int[]{441,442,443,444,445,446,447,448,449,450,451,452,461,462,463,464,465,466,467,468,469,470,471,472}; // all emissions.

            //int[] protocol = new int[]{32}; // Test DAGCycleCheckerTester - Performance

            //int[] protocol = new int[]{510,511};
            //int[] protocol = new int[]{502};
            //int[] protocol = new int[]{600};
            //int[] protocol = new int[] { 321 };
            //int[] protocol = new int[] { 11 };
            //int[] protocol = new int[] { 12 };
            //int[] protocol = new int[] { 653 };
            //int[] protocol = new int[] { 126 };
            //int[] protocol = new int[] { 721 };
            //int[] protocol = new int[] { 1200 };
            //int[] protocol = new int[] { 701 };
            //int[] protocol = new int[] { 670 };
            //int[] protocol = new int[] { 658 };
            //int[] protocol = new int[] { 132 }; // Catalytic reactions.
            //int[] protocol = new int[] { 800 };
            //int[] protocol = new int[] { 121, 122, 123, 124, 125, 126, 130, 131, 132 }; // Chemistry & reactions.
            //int[] protocol = new int[] { 200, 201, 210, 211, 212, 213};
            //int[] protocol = new int[] { 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144 };
            //int[] protocol = new int[] { 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210,
            //                             210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210, 210 };
            //int[] protocol = new int[] { 1301 };
            int[] protocol = new int[] {1400};

            foreach (int test in protocol)
            {
                switch (test)
                {
                    case 0:
                        break;

                    // zTestExecutive.cs
                    case 1:
                        _Debug.WriteLine("Test ExecutiveCount");
                        new ExecTester().TestExecutiveCount();
                        break;
                    case 2:
                        _Debug.WriteLine("Test ExecutiveCountDefaultParameter");
                        new ExecTester().TestExecutiveCountDefaultParameter();
                        break;
                    case 3:
                        _Debug.WriteLine("Test ExecutiveWhen");
                        new ExecTester().TestExecutiveWhen();
                        break;
                    case 4:
                        _Debug.WriteLine("Test ExecutivePriority");
                        new ExecTester().TestExecutivePriority();
                        break;
                    case 5:
                        _Debug.WriteLine("Test ExecutiveUnRequestHash");
                        new ExecTester().TestExecutiveUnRequestHash();
                        break;
                    case 6:
                        _Debug.WriteLine("Test ExecutiveUnRequestTarget");
                        new ExecTester().TestExecutiveUnRequestTarget();
                        break;
                    case 7:
                        _Debug.WriteLine("Test ExecutiveUnRequestDelegate");
                        new ExecTester().TestExecutiveUnRequestDelegate();
                        break;
                    case 8:
                        _Debug.WriteLine("Test ExecutiveUnRequestSelector");
                        new ExecTester().TestExecutiveUnRequestSelector();
                        break;
                    case 9:
                        _Debug.WriteLine("Test ThreadSepFunctionality");
                        new ExecTester().TestThreadSepFunctionality();
                        break;
                    case 10:
                        _Debug.WriteLine("Test Executive2");
                        new ExecTester().RecreateFailure();
                        break;

                    case 11:
                        _Debug.WriteLine("Test Executive Stopping and restarting.");
                        new ExecTester().TestExecutiveStopStart();
                        break;

                    case 12:
                        _Debug.WriteLine("Test Executive Pause/Resume.");
                        new ExecTester().TestExecutivePauseResume();
                        break;

                    case 13:
                        _Debug.WriteLine("Test Executive Performance.");
                        new ExecTester().TestPerformance();
                        break;

                    // zTestStateMachine.cs
                    case 20:
                        _Debug.WriteLine("Test StateMachine");
                        new StateMachineTester().TestStateMachine();
                        break;
                    case 21:
                        _Debug.WriteLine("Test TransitionSuccessWithFollowon");
                        new StateMachineTester().TestTransitionSuccessWithFollowon();
                        break;
                    case 22:
                        _Debug.WriteLine("Test TransitionSuccessWithoutFollowon");
                        new StateMachineTester().TestTransitionSuccessWithoutFollowon();
                        break;
                    case 23:
                        _Debug.WriteLine("Test TransitionFailure");
                        new StateMachineTester().TestTransitionFailure();
                        break;
                    case 24:
                        _Debug.WriteLine("Test TransitionIllegal");
                        new StateMachineTester().TestTransitionIllegal();
                        break;
                    case 25:
                        _Debug.WriteLine("Test TransitionIllegalToo");
                        new StateMachineTester().TestTransitionIllegalToo();
                        break;
                    case 26:
                        _Debug.WriteLine("Test TransitionChainSuccess");
                        new StateMachineTester().TestTransitionChainSuccess();
                        break;
                    case 27:
                        _Debug.WriteLine("Test TransitionMultipleHandlers");
                        new StateMachineTester().TestTransitionMultipleHandlers();
                        break;
                    case 28:
                        _Debug.WriteLine("Test TransitionMultipleHandlersSorted");
                        new StateMachineTester().TestTransitionMultipleHandlersSorted();
                        break;

                    // zTestDAGCycleChecker.cs
                    case 30:
                        _Debug.WriteLine("Test DAGCycleCheckerTester - Basic");
                        new Graphs.DAGCycleCheckerTester().TestBasicValidation();
                        break;
                    case 31:
                        _Debug.WriteLine("Test DAGCycleCheckerTester - Implied");
                        new Graphs.DAGCycleCheckerTester().TestValidationWithImpliedRelationships();
                        break;
                    case 32:
                        _Debug.WriteLine("Test DAGCycleCheckerTester - Performance");
                        new Graphs.DAGCycleCheckerTester().TestValidationPerformance();
                        break;
                    case 33:
                        _Debug.WriteLine("Test DAGCycleCheckerTester - Inner Edge Performance");
                        new Graphs.DAGCycleCheckerTester().TestInnerEdgePerformance();
                        break;

                    // zTestTasks.cs
                    case 41:
                        _Debug.WriteLine("Test ChildSequencing");
                        new Graphs.Tasks.TaskTester().TestChildSequencing();
                        break;
                    case 42:
                        _Debug.WriteLine("Test PlainGraph");
                        new Graphs.Tasks.TaskTester().TestPlainGraph();
                        break;
                    case 43:
                        _Debug.WriteLine("Test CoStart");
                        new Graphs.Tasks.TaskTester().TestCoStart();
                        break;
                    case 44:
                        _Debug.WriteLine("Test CoFinish");
                        new Graphs.Tasks.TaskTester().TestCoFinish();
                        break;
                    case 45:
                        _Debug.WriteLine("Test SynchroStart");
                        new Graphs.Tasks.TaskTester().TestSynchroStart();
                        break;
                    //case 46: _Debug.WriteLine("Test SynchroFinish"); new Highpoint.Sage.Graphs.Tasks.TaskTester().TestSynchroFinish(); break;


                    case 50:
                        _Debug.WriteLine("Test SmartPropertyBag Memento leakage");
                        new SmartPropertyBagTester().TestRepeatedSnapshottingAndRestoration();
                        break;

                    // zTestGraphValidities.cs
                    case 61:
                        _Debug.WriteLine("Test BasicValidation");
                        new Tasks.GraphValidityTester().TestBasicValidation();
                        break;
                    case 62:
                        _Debug.WriteLine("Test AddTasks");
                        new Tasks.GraphValidityTester().TestAddTasks();
                        break;
                    case 63:
                        _Debug.WriteLine("Test SynchronizeTasks");
                        new Tasks.GraphValidityTester().TestSynchronizeTasks();
                        break;
                    case 64:
                        _Debug.WriteLine("Test SynchronizeTasksInTwoLevels");
                        new Tasks.GraphValidityTester().TestSynchronizeTasksInTwoLevels();
                        break;
                    case 65:
                        _Debug.WriteLine("Test SynchronizeAndAddTasks");
                        new Tasks.GraphValidityTester().TestSynchronizeAndAddTasks();
                        break;
                    case 66:
                        _Debug.WriteLine("Test RemoveTasks");
                        new Tasks.GraphValidityTester().TestRemoveTasks();
                        break;
                    case 67:
                        _Debug.WriteLine("Test SynchronizeAndRemoveTasks");
                        new Tasks.GraphValidityTester().TestSynchronizeAndRemoveTasks();
                        break;
                    case 68:
                        _Debug.WriteLine("Test ");
                        new Tasks.GraphValidityTester().TestToCauseResumeFailure();
                        break;

                    //case 69: _Debug.WriteLine("Test Task Enumerators"); new Highpoint.Sage.Tasks.GraphValidityTester().TestTaskEnumerators(); break;

#if NYRFPT
                    // zTestGraphPersistance.cs
                    case 81:
                        _Debug.WriteLine("Test PlainGraphPersistence");
                        new Graphs.Tasks.TaskGraphPersistenceTester().TestPlainGraphPersistence();
                        break;
                    case 82:
                        _Debug.WriteLine("Test CoStartPersistance");
                        new Graphs.Tasks.TaskGraphPersistenceTester().TestCoStartPersistance();
                        break;
                    case 83:
                        _Debug.WriteLine("Test CoFinishPersistence");
                        new Graphs.Tasks.TaskGraphPersistenceTester().TestCoFinishPersistence();
                        break;
                    case 84:
                        _Debug.WriteLine("Test SynchroStartPersistence");
                        new Graphs.Tasks.TaskGraphPersistenceTester().TestSynchroStartPersistence();
                        break;
                    case 85:
                        _Debug.WriteLine("Test SynchroFinishPersistence");
                        new Graphs.Tasks.TaskGraphPersistenceTester().TestSynchroFinishPersistence();
                        break;
#endif
                    // zTestResources.cs
                    case 101:
                        _Debug.WriteLine("Test PersistentResourceBasics");
                        new ResourceTester().TestPersistentResourceBasics();
                        break;
                    case 102:
                        _Debug.WriteLine("Test ConsumableResourceBasics");
                        new ResourceTester().TestConsumableResourceBasics();
                        break;
                    case 103:
                        _Debug.WriteLine("Test MaterialConduits");
                        new ResourceTester().TestMaterialConduits();
                        break;
                    case 104:
                        _Debug.WriteLine("Test Earmarking");
                        new ResourceTester().TestEarmarking();
                        break;
                    case 105:
                        _Debug.WriteLine("Test Advanced Earmarking");
                        new ResourceTester().TestAdvancedEarmarking();
                        break;

                    // zTestMaterials.cs
                    case 121:
                        _Debug.WriteLine("Test Removal");
                        new MaterialTester().TestRemoval();
                        break;
                    case 122:
                        _Debug.WriteLine("Test Combinatorics");
                        new MaterialTester().TestCombinatorics();
                        break;
                    case 123:
                        _Debug.WriteLine("Test Reactions");
                        new MaterialTester().TestReactions();
                        break;
                    case 124:
                        _Debug.WriteLine("Test SecondaryReactions");
                        new MaterialTester().TestSecondaryReactions();
                        break;
                    case 125:
                        _Debug.WriteLine("Test SecondaryReactions");
                        new MaterialTester().TestMaterialSpecifications();
                        break;

                    case 126:
                        _Debug.WriteLine("Test Material Transferrer");
                        new MaterialTester().TestMaterialTransferrer();
                        break;

                    case 130:
                        _Debug.WriteLine("Test Reaction Basics");
                        new Chemistry101().TestReactionBasics();
                        break;
                    case 131:
                        _Debug.WriteLine("Test Reaction Basics");
                        new Chemistry101().TestRP_CombineAPI();
                        break;

                    case 132:
                        _Debug.WriteLine("Test Catalytic Reaction Basics");
                        new Chemistry101().TestCatalyticReactionBasics();
                        break;

                    case 140:
                        _Debug.WriteLine("Test Mersenne Twister");
                        new Randoms.MersenneTester().TestMersenneAgainstMatumotosGoldNoBuffering();
                        break;
                    case 141:
                        _Debug.WriteLine("Test Mersenne Twister");
                        new Randoms.MersenneTester().TestMersenneAgainstMatumotosGoldWithBuffering();
                        break;
                    case 142:
                        _Debug.WriteLine("Test Mersenne Twister");
                        new Randoms.MersenneTester().TestMersenneAgainstMatumotosGoldWithBufferingMulti();
                        break;
                    case 143:
                        _Debug.WriteLine("Test Mersenne Twister Performance without threading");
                        new Randoms.MersenneTester().PerformanceTestNoThreading();
                        break;
                    case 144:
                        _Debug.WriteLine("Test Mersenne Twister Performance with threading");
                        new Randoms.MersenneTester().PerformanceTestWithThreading();
                        break;
                    case 145:
                        _Debug.WriteLine("Test Mersenne Twister interval fidelity");
                        new Randoms.MersenneTester().RunIntervalTest();
                        break;

                    case 150:
                        _Debug.WriteLine("Test Evaluator Basics");
                        new EvaluatorTester().TestEvaluatorBasics();
                        break;

                    case 199:
                        _Debug.WriteLine("Test Temperature Controller");
                        new Thermodynamics.TemperatureControllerTester101().TestTCConstDeltaTargetingUp2();
                        break;

                    case 200:
                        _Debug.WriteLine("Test Stochastic BranchBlock");
                        new ItemBased.BranchBlockTester().TestStochasticBranchBlock();
                        break;

                    case 201:
                        _Debug.WriteLine("Test Delegated BranchBlock");
                        new ItemBased.BranchBlockTester().TestDelegatedBranchBlock();
                        break;

                    case 210:
                        _Debug.WriteLine("Test Server");
                        new ItemBased.Blocks.ServerTester().TestServerBasics();
                        break;

                    case 211:
                        _Debug.WriteLine("Test Resource Server");
                        new ItemBased.Blocks.ServerTester().TestResourceServer();
                        break;

                    case 212:
                        _Debug.WriteLine("Test Resource Server");
                        new ItemBased.Blocks.ServerTester().TestResourceServerComplexDemands();
                        break;

                    case 213:
                        _Debug.WriteLine("Test Buffered Server");
                        new ItemBased.Blocks.ServerTester().TestBufferedServer();
                        break;

                    case 220:
                        _Debug.WriteLine("Empirical Distribution From Histogram");
                        new Mathematics.Distributions101().TestDistributionEmpirical();
                        break;

                    case 221:
                        _Debug.WriteLine("Single Element Distribution");
                        new Mathematics.Distributions101()
                            .TestSingleDatapointAsTwoInLinearDoubleInterpolable();
                        break;

                    case 222:
                        _Debug.WriteLine("Universal Distribution From Histogram");
                        new Mathematics.Distributions101().TestUniversalDistribution();
                        break;

                    case 230:
                        _Debug.WriteLine("Resource Starvation");
                        new ResourceTester().TestStarvation();
                        break;

                    case 240:
                        _Debug.WriteLine("Interpolations 2 Point");
                        new Mathematics.Interpolations101().TestInterpolationFrom2Points();
                        break;

                    case 241:
                        _Debug.WriteLine("Interpolations 2 Point Neg Slope");
                        new Mathematics.Interpolations101().TestInterpolationFrom2PointsNegativeSlope();
                        break;

                    case 242:
                        _Debug.WriteLine("Interpolations 4 Point");
                        new Mathematics.Interpolations101().TestInterpolationFrom4Points();
                        break;

                    case 243:
                        _Debug.WriteLine("Interpolations Point Replacement");
                        new Mathematics.Interpolations101().TestInterpolationPointReplacement();
                        break;

                    case 250:
                        _Debug.WriteLine("WeakReferenceHashtable Tester");
                        new Utility.WeakReferenceHashtableTester().TestWRHTBasics();
                        break;

                    case 260:
                        _Debug.WriteLine("Prioritized ResourceRequest Handling");
                        new ResourceTesterExt().TestBasicFuctionality();
                        break;

                    case 261:
                        _Debug.WriteLine("Prioritized ResourceRequest Handling");
                        new ResourceTesterExt().TestPrioritizedResourceRequestHandling();
                        break;

                    case 262:
                        _Debug.WriteLine("Prioritized ResourceRequest Handling");
                        new ResourceTesterExt().TestPrioritizedResourceRequestWRemoval_1();
                        break;

                    case 263:
                        _Debug.WriteLine("Prioritized ResourceRequest Handling");
                        new ResourceTesterExt().TestPrioritizedResourceRequestWRemoval_2();
                        break;

                    case 300:
                        _Debug.WriteLine("TupleSpace Testing - basic");
                        new Utility.TupleTester().TestTupleBasics();
                        break;
                    case 301:
                        _Debug.WriteLine("TupleSpace Testing - blocking read");
                        new Utility.TupleTester().TestBlockingRead();
                        break;
                    case 302:
                        _Debug.WriteLine("TupleSpace Testing - blocking take");
                        new Utility.TupleTester().TestBlockingTake();
                        break;
                    case 303:
                        _Debug.WriteLine("TupleSpace Testing - blocking post");
                        new Utility.TupleTester().TestBlockingPost();
                        break;
                    case 304:
                        _Debug.WriteLine("TupleSpace Testing - read");
                        new Utility.TupleTester().TestRead();
                        break;
                    case 305:
                        _Debug.WriteLine("TupleSpace Testing - take");
                        new Utility.TupleTester().TestTake();
                        break;

                    case 310:
                        _Debug.WriteLine("TreeNodeHelperTester - Basics");
                        new Utility.TreeNodeHelperTester().TestTreeNodeHelperBasics();
                        break;
                    case 311:
                        _Debug.WriteLine("TreeNodeHelperTester - Read-Only");
                        new Utility.TreeNodeHelperTester().TestReadOnlyTreeNodeHelperBasics();
                        break;
                    case 312:
                        _Debug.WriteLine("TreeNodeHelperTester - Child Sequencing");
                        new Utility.TreeNodeHelperTester().TestTreeNodeHelperChildSequencing();
                        break;

                    case 320:
                        _Debug.WriteLine("GraphLoopingTester - Basic Looping");
                        new Graphs.GraphLoopingTester().TestBasicLooping();
                        break;
                    case 321:
                        _Debug.WriteLine("TreeNodeHelperTester - Basic Branching");
                        new Graphs.GraphLoopingTester().TestBasicBranching();
                        break;
#if NYRFPT
                    case 400:
                        _Debug.WriteLine("TimePeriodTester - Basic Operations");
                        new Scheduling.TimePeriodTester().TestTimePeriodBasics();
                        break;
                    case 401:
                        _Debug.WriteLine("TimePeriodTester - Time Period Envelope");
                        new Scheduling.TimePeriodTester().TestTimePeriodEnvelope();
                        break;
                    case 402:
                        _Debug.WriteLine("TimePeriodTester - Nested Time Period Envelopes");
                        new Scheduling.TimePeriodTester().TestNestedTimePeriodEnvelope();
                        break;
#endif
                    case 404:
                        _Debug.WriteLine("TimePeriodRelationshipTester - Basic Operations");
                        new Scheduling.TimePeriodRelationshipTester().TestBasics();
                        break;
                    case 405:
                        _Debug.WriteLine("TimePeriodTester - Basic Operations");
                        new Scheduling.MilestoneRelationshipTester().TestMilestones();
                        break;

#if NYRFPT
                    case 406:
                        _Debug.WriteLine("ActivityTester - Test Activities");
                        new Scheduling.ActivityTester().TestActivities();
                        break;
                    case 407:
                        _Debug.WriteLine("ActivityTester - Test Activities, Deep");
                        new Scheduling.ActivityTester().TestActivitiesDeep();
                        break;
                    case 408:
                        _Debug.WriteLine("ActivityTester - Test Activities, Deep/Random");
                        new Scheduling.ActivityTester().TestActivitiesDeepRandom();
                        break;
                    case 409:
                        _Debug.WriteLine("ActivityTester - Test Task Graph Adjustments");
                        new Scheduling.ActivityTester().TestTaskGraphAdjustments();
                        break;
#endif
                    case 420:
                        _Debug.WriteLine("VaporPressureTester - Test Known Vapor Pressure Computations");
                        new Materials.Chemistry.VaporPressure.VaporPressureTester()
                            .TestKnownVaporPressureValues();
                        break;
                    case 421:
                        _Debug.WriteLine("VaporPressureTester - Test Empirical Water Vapor Pressure Computations");
                        new Materials.Chemistry.VaporPressure.VaporPressureTester()
                            .TestEmpiricalWaterVaporPressureScenarios();
                        break;
                    case 422:
                        _Debug.WriteLine("VaporPressureTester - Test Henry's Law Computations");
                        new Materials.Chemistry.VaporPressure.VaporPressureTester().TestHenrysLawScenario
                            ();
                        break;
                    case 423:
                        _Debug.WriteLine("VaporPressureTester - Test Vapor Space Mixture Handling");
                        new MaterialTester().TestVaporSpaceMixtureHandling();
                        break;

                    case 440:
                        _Debug.WriteLine("EmissionsTester - MegaTester");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().MegaTest();
                        break;

                    case 441:
                        _Debug.WriteLine("EmissionsTester - Evacuate");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestEvacuate();
                        break;
                    case 442:
                        _Debug.WriteLine("EmissionsTester - Fill");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestFill();
                        break;
                    case 443:
                        _Debug.WriteLine("EmissionsTester - GasEvolution");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestGasEvolution();
                        break;
                    case 444:
                        _Debug.WriteLine("EmissionsTester - GasSweep");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestGasSweep();
                        break;
                    case 445:
                        _Debug.WriteLine("EmissionsTester - Heat");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestHeat();
                        break;
                    case 446:
                        _Debug.WriteLine("EmissionsTester - MassBalance");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestMassBalance();
                        break;
                    case 447:
                        _Debug.WriteLine("EmissionsTester - NoEmission");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestNoEmissions();
                        break;
                    case 448:
                        _Debug.WriteLine("EmissionsTester - VacuumDistillation");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestVacuumDistillation();
                        break;
                    case 449:
                        _Debug.WriteLine("EmissionsTester - VacDistWithScrubber");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestVacuumDistillationWScrubber();
                        break;
                    case 450:
                        _Debug.WriteLine("EmissionsTester - VacDry");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestVacuumDry();
                        break;
                    case 451:
                        _Debug.WriteLine("EmissionsTester - PressureTransfer");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestPressureTransfer
                            ();
                        break;
                    case 452:
                        _Debug.WriteLine("EmissionsTester - Air Dry");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestAirDry();
                        break;

                    case 461:
                        _Debug.WriteLine("EmissionsTester - Late bound Evacuate");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundEvacuate();
                        break;
                    case 462:
                        _Debug.WriteLine("EmissionsTester - Late bound Fill");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestLateBoundFill();
                        break;
                    case 463:
                        _Debug.WriteLine("EmissionsTester - Late bound GasEvolution");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundGasEvolution();
                        break;
                    case 464:
                        _Debug.WriteLine("EmissionsTester - Late bound GasSweep");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundGasSweep();
                        break;
                    case 465:
                        _Debug.WriteLine("EmissionsTester - Late bound Heat");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestLateBoundHeat();
                        break;
                    case 466:
                        _Debug.WriteLine("EmissionsTester - Late bound MassBalance");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundMassBalance();
                        break;
                    case 467:
                        _Debug.WriteLine("EmissionsTester - Late bound NoEmission");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundNoEmissions();
                        break;
                    case 468:
                        _Debug.WriteLine("EmissionsTester - Late bound VacuumDistillation");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundVacuumDistillation();
                        break;
                    case 469:
                        _Debug.WriteLine("EmissionsTester - Late bound VacDistWithScrubber");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundVacuumDistillationWScrubber();
                        break;
                    case 470:
                        _Debug.WriteLine("EmissionsTester - Late bound VacDry");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundVacuumDry();
                        break;
                    case 471:
                        _Debug.WriteLine("EmissionsTester - Late bound PressureTransfer");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestLateBoundPressureTransfer();
                        break;
                    case 472:
                        _Debug.WriteLine("EmissionsTester - Late bound Air Dry");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester().TestLateBoundAirDry();
                        break;

                    case 473:
                        _Debug.WriteLine("EmissionsTester - Multi-material fill");
                        new Materials.Chemistry.EmissionModels.EmissionModelTester()
                            .TestMultiMaterialFill();
                        break;

                    case 500:
                        _Debug.WriteLine("DistributionTester - Poisson");
                        new Mathematics.Distributions101().TestDistributionPoisson();
                        break;
                    case 501:
                        _Debug.WriteLine("DistributionTester - Exponential");
                        new Mathematics.Distributions101().TestDistributionExponential();
                        break;
                    case 502:
                        _Debug.WriteLine("DistributionTester - Exponential Timespan");
                        new Mathematics.Distributions101().TestDistributionTimeSpanExponential();
                        break;

                    case 510:
                        _Debug.WriteLine("MultiArrayListEnumerator - Basic");
                        new Utility.MultiArrayListEnumerationTester().TestBasicsOfEnumerator();
                        break;
                    case 511:
                        _Debug.WriteLine("MultiArrayListEnumerator - Internal");
                        new Utility.MultiArrayListEnumerationTester().TestEnumeratorWithEmptyArrays();
                        break;

                    case 540:
                        _Debug.WriteLine("Heap testing");
                        new SageTestLib.HeapTester().TestHeap();
                        break;
                    case 541:
                        _Debug.WriteLine("Heap testing");
                        new SageTestLib.HeapTester().RecreateFailure();
                        break;

                    case 550:
                        _Debug.WriteLine("Local Event Queue testing");
                        new Utility.LocalEventQueueTester().TestLocalEventQueue();
                        break;
                    case 551:
                        _Debug.WriteLine("Local Event Queue testing");
                        new Utility.LocalEventQueueTester().TestLocalEventQueue2();
                        break;

                    case 560:
                        _Debug.WriteLine("Event Time Historian Tester");
                        new Utility.HistorianTester().TestEventTimeHistorian();
                        break;

                    case 600:
                        _Debug.WriteLine("HashtableOfLists testing");
                        new Utility.HashtableOfListsTester().TestHTOL();
                        break;

#if NYRFPT
                    case 650:
                        _Debug.WriteLine("Executable PFC tester without hierarchy");
                        new Graphs.PFC.Execution.ExecutablePfcTester().TestSmallLoopback();
                        break;

                    case 651:
                        _Debug.WriteLine("Executable PFC tester with hierarchy");
                        new Graphs.PFC.Execution.ExecutablePfcTester().TestSmallLoopbackHierarchical();
                        break;

                    case 652:
                        _Debug.WriteLine("Executable PFC performance tester with hierarchy");
                        new Graphs.PFC.Execution.ExecutablePfcTester().TestCreationOfABazillionEECs();
                        break;

                    case 653:
                        _Debug.WriteLine("Executable PFC performance tester with hierarchy");
                        new Graphs.PFC.Execution.ExecutablePfcExtensionTester().TestSequencers();
                        break;
#endif
                    case 654:
                        _Debug.WriteLine("Offset Parallelism tester");
                        new PFCDemoMaterial.PfcAnalystTester().Test_OffSetParallelism();
                        break;

                    case 655:
                        _Debug.WriteLine("PFC reduction tester");
                        new PFCDemoMaterial.PfcAnalystTester().Test_ValidatorFromStoredPFC();
                        break;

#if NYRFPT
                    case 656:
                        _Debug.WriteLine("PFC reduction tester");
                        new SchedulerDemoMaterial.PfcAnalystTester().Test_GetPermissibleTargetsForLinkFrom_WithinLoopFromStepToSelf();
                        break;
#endif
                    case 657:
                        _Debug.WriteLine("PFC Path Analysis tester");
                        new PFCDemoMaterial.PfcAnalystTester().Test_DeepNonLoopingPath();
                        break;

                    case 658:
                        _Debug.WriteLine("PFC Path Analysis tester");
                        new PFCDemoMaterial.PfcAnalystTester().TestBroadestNonLoopbackPath();
                        break;

                    case 670:
                        _Debug.WriteLine("String operation tester - to anded lists.");
                        new Utility.ExtensionTester().TestCommasAndAndedListOperations();
                        break;

                    case 671:
                        _Debug.WriteLine("Test XOR'ing of bytes.");
                        new Utility.ExtensionTester().TestByteXOR();
                        break;

                    case 700:
                        _Debug.WriteLine("List Extension percentile tester");
                        new Mathematics.ExtensionTester().TestPercentileGetter();
                        break;
                    case 701:
                        _Debug.WriteLine("List Extension sigma bounding tester");
                        new Mathematics.ExtensionTester().TestSigmaBounding();
                        break;

                    case 720:
                        _Debug.WriteLine("Cost tester");
                        new Scheduling.Cost.zTestCost1().TestCostBasics1();
                        break;
                    case 721:
                        _Debug.WriteLine("Cost tester");
                        new Scheduling.Cost.zTestCost1().TestCostBasics2();
                        break;

                    case 800:
                        _Debug.WriteLine("");
                        new Graphs.PFC.PFCGraphTester().Test_InsertStepIntoLoop();
                        break;

                    case 1000:
                        new TransferSpecTester101().TestMassScaling();
                        break;

                    case 1200:
                        new Materials.Chemistry.BoilingPoints.BoilingPointTester().TestBoilingPoints3();
                        break;

                    //////////////////////  PORTS AND CONNECTORS ///////////////////
                    case 1300:
                        new ItemBased.Blocks.PortsAndConnectorsTester().TestPortBasics();
                        break;

                    case 1301:
                        new ItemBased.Blocks.ManagementFacadeTester().TestManagementBasics();
                        break;


                    //////////////////////  TRANSPORT NETWORK ///////////////////
                    //case 1400:
                    //  new TestTransportNetwork().TestTransportBasics();
                    //  break;

                }

#endregion

            }

#region <<< Explicit Test Suite Calls >>>



            //new Highpoint.Sage.Graphs.Tasks.TaskTester().TestSynchroFinish();
            //new Highpoint.Sage.Tasks.GraphValidityTester().TestValidationWithReconfiguration();
            //new Highpoint.Sage.Tasks.GraphValidityTester().TestValidation();
            //new Highpoint.Sage.Materials.Chemistry.MaterialTester().TestRemoval();
            //new Highpoint.Sage.Materials.Chemistry.TransferSpecTester101().TestMassScaling();
            // --  Drop 1 Tests.
            //new Highpoint.Sage.SimCore.ExecTester().TestBaseFunctionality();
            //new Highpoint.Sage.SimCore.ExecTester().TestBatchedFunctionality();
            //new Highpoint.Sage.SimCore.ExecTester().TestThreadSepFunctionality();
            //new Highpoint.Sage.SimCore.StateMachineTester().TestStateMachine1();
            //new Highpoint.Sage.SimCore.StateMachineTester().TestTransitionFailure();
            //new Highpoint.Sage.SimCore.StateMachineTester().TestTransitionSuccess();
            //new Highpoint.Sage.SimCore.StateMachineTester().TestTransitionChainSuccess();
            //new Highpoint.Sage.SimCore.StateMachineTester().TestTransitionMultipleHandlers();
            //new Highpoint.Sage.SimCore.StateMachineTester().TestTransitionMultipleHandlersSorted();

            // --  Automatic tests.
            // [deleted] new Highpoint.Sage.Graphs.Tasks.TaskTester().TestAdvancedGraphFunctionality();
            // These need reworking. They were wrong when I looked at them for ser/deser stuff.
            //new Highpoint.Sage.Graphs.Tasks.TaskTester().TestChildSequencing();
            //new Highpoint.Sage.Graphs.Tasks.TaskTester().TestPlainGraph();
            //new Highpoint.Sage.Graphs.Tasks.TaskTester().TestCoStart();
            //new Highpoint.Sage.Graphs.Tasks.TaskTester().TestCoFinish();
            //new Highpoint.Sage.Graphs.Tasks.TaskTester().TestSynchroFinish();

            //new ResourceTester().TestPersistentResourceBasics();
            //new ResourceTester().TestConsumableResourceBasics();
            //new ResourceTester().TestMaterialConduits();
            //new ResourceTester().TestEarmarking();


            //new ModelTester().TestModelBasics();

            //new MementoTester().TestMaterialMementoRestoration();
            //new MementoTester().TestMaterialMementoEquality();

            // --  Integration Tests.
            //new IntegrationTester().TestMAIFunctionality1();
            //new ValidationSequenceTester().TestEnsureErrorIntroductionRevertsModelToIdleState();

            // new Highpoint.Sage.Tasks.GraphValidityTester().TestValidationWithReconfiguration();

            // new ValidationSequenceTester().TestRevalidationSequence();

            //new Highpoint.Sage.SimCore.SmartPropertyBagTester().TestSubsidiaries();
            //new Highpoint.Sage.SimCore.SmartPropertyBagTester().TestMementoCaching();
            //new Highpoint.Sage.SimCore.SmartPropertyBagTester().TestMementoRestorationAndEquality();
            //new Highpoint.Sage.SimCore.SmartPropertyBagTester().TestEnumerationAndIsLeaf();
            // ---- new Highpoint.Sage.SimCore.SmartPropertyBagTester().TestStringsAndBooleans();
            // This has a bug - aliases are not reflected as having changed.

            //new Highpoint.Sage.Algebra.Interpolations101().TestInterpolationFrom2Points();
            //new Highpoint.Sage.Algebra.Interpolations101().TestInterpolationFrom4Points();

            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstDeltaTargetingUp();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstDeltaTargetingDown();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstTRampRateKlendathu();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstTRampRateTargetingUp();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstTRampRateTargetingDown();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstTSrcTargetingLevel();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstTSrcTargetingUp();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCConstTSrcTargetingDown();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCDriftUp();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestTCDriftDown();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestOverShootWhileOff();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestUnderShootWhileOff();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestOverShootWhileOn();
            //new Highpoint.Sage.Thermodynamics.TemperatureControllerTester101().TestUnderShootWhileOn();

            //////////////////////  PORTS AND CONNECTORS ///////////////////
            //new Highpoint.Sage.ItemBased.Blocks.PortsAndConnectorsTester().TestBankTellerModel();
            // new Highpoint.Sage.ItemBased.Blocks.PortsAndConnectorsTester().TestPortBasics();
            // new Highpoint.Sage.ItemBased.Blocks.PortsAndConnectorsTester().TestManagementBasics();

            // new AdHocChemistry().Demonstrate();

            //new Highpoint.Sage.Materials.Chemistry.TransferSpecTester101().TestNonScaledByMassTransfer();
            // new Highpoint.Sage.Materials.Chemistry.TransferSpecTester101().TestMassScaling();

            //new Highpoint.Sage.Materials.Chemistry.MaterialTester().TestRemovalByMass();
            //new Highpoint.Sage.Materials.Chemistry.MaterialTester().TestRemovalByPercentage();

            /////////////////////     CHEMISTRY TESTS     ///////////////////
            // new Chemistry.Chemistry101().TestReactionBasics();
            // new Chemistry.Chemistry101().TestRP_CombineAPI(); // Reaction processor's 'Combine Materials' capability.

            //new zSerializationSandbox().DoSerialization();

            //new Highpoint.Sage.Mathematics.Histograms101().TestHistogramExponentialDistDouble();
            //new Highpoint.Sage.Mathematics.Histograms101().TestHistogramLinearDistDouble();
            //new Highpoint.Sage.Mathematics.Histograms101().TestHistogramLinearDistTimeSpan();

            //new Highpoint.Sage.Persistence.PersistenceTester().TestPersistenceBasics();
            //new Highpoint.Sage.Persistence.PersistenceTester().TestPersistenceWaterStorage();
            //new Highpoint.Sage.Persistence.PersistenceTester().TestPersistenceWaterRestoration();
            //new Highpoint.Sage.Persistence.PersistenceTester().TestPersistenceChemistryStorage();
            //				new TaskGraphPersistenceTester().TestPlainGraph();
            //				new TaskGraphPersistenceTester().TestChildSequencing();
            //				//new TaskGraphPersistenceTester().TestCoFinish();
            //				new TaskGraphPersistenceTester().TestCoStart();
            //				//new TaskGraphPersistenceTester().TestSynchroFinish();
            //				new TaskGraphPersistenceTester().TestSynchroStart();

            //new DiscreteTester().TestDiscreteModel();

            //new DESynchTester().TestBaseFunctionality();

            // Put the folloing lines into the test driver

            //
            // Working tests
            //
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutiveCount();
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutiveCountLessParameter();
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutivePriority();
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutiveWhen();
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutiveUnRequestHash();
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutiveUnRequestTarget();
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutiveUnRequestDelegate();
            //
            // Tests with issues
            //
            //new Highpoint.Sage.SimCore.ExecTesterAEL().TestExecutiveUnRequestSelector();

#endregion

            //_Debug.WriteLine("Tests completed. Hit <Enter> to continue.");
            //Console.ReadLine();
        }
    }
}
