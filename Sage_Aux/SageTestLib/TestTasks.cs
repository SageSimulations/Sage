/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs.Tasks {

    [TestClass]
    public class TaskTester {

        private Random m_random = new Random();

        public TaskTester(){Init();}
        
		[TestInitialize] 
		public void Init() {}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test runs a parent task, which spans over the length of the five child tasks running in a sequence.")]
		public void TestChildSequencing(){
            Model model = new Model();
            model.AddService<ITaskManagementService>(new TaskManagementService());

            TestTask parent = new TestTask(model,"Parent");

            TaskProcessor tp = new TaskProcessor(model, "TP", parent) { KeepGraphContexts = true };
		    model.GetService<ITaskManagementService>().AddTaskProcessor(tp);
            

            TestTask[] children = new TestTask[5];
            for ( int i = 0 ; i < children.Length ; i++ ) {
                children[i] = new TestTask(model,"Child"+i,TimeSpan.FromHours(i));
                if ( i > 0 ) children[i].AddPredecessor(children[i-1]);
                parent.AddChildEdge(children[i]);
            }

            model.Start();

            IDictionary gc = (IDictionary)tp.GraphContexts[0];
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), parent.GetStartTime(gc), "Parent task did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), children[0].GetStartTime(gc), "Child task 1 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), children[1].GetStartTime(gc), "Child task 2 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 1, 0, 0), children[2].GetStartTime(gc), "Child task 3 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 3, 0, 0), children[3].GetStartTime(gc), "Child task 4 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 6, 0, 0), children[4].GetStartTime(gc), "Child task 5 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), children[0].GetFinishTime(gc), "Child task 1 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 1, 0, 0), children[1].GetFinishTime(gc), "Child task 2 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 3, 0, 0), children[2].GetFinishTime(gc), "Child task 3 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 6, 0, 0), children[3].GetFinishTime(gc), "Child task 4 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 10, 0, 0), children[4].GetFinishTime(gc), "Child task 5 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 10, 0, 0), parent.GetFinishTime(gc), "Parent task did't finish at the correct time.");
        
		}

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) Check to see that Td starts at T=1.
        [TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks to see that Td starts at T=1")]
		public void TestPlainGraph(){

            TestGraph1 tg1 = new TestGraph1();

            tg1.model.Start();

            Assert.IsTrue(tg1.ta.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,0,0,0)),"Task A did not start at 12AM 1/1/1");
            Assert.IsTrue(tg1.ta.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task A did not finish at 4AM 1/1/1");

            Assert.IsTrue(tg1.tb.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task B did not start at 4AM 1/1/1");
            Assert.IsTrue(tg1.tb.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,5,0,0)),"Task B did not finish at 5AM 1/1/1");

            Assert.IsTrue(tg1.tc.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,0,0,0)),"Task C did not start at 12AM 1/1/1");
            Assert.IsTrue(tg1.tc.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,1,0,0)),"Task C did not finish at 1AM 1/1/1");

            Assert.IsTrue(tg1.td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,1,0,0)),"Task D did not start at 1AM 1/1/1");
            Assert.IsTrue(tg1.td.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,2,0,0)),"Task D did not start at 1AM 1/1/1");

            Assert.IsTrue(tg1.parent.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,0,0,0)),"Task parent did not start at 12AM 1/1/1");
            Assert.IsTrue(tg1.parent.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,5,0,0)),"Task parent did not finish at 5AM 1/1/1");

            Assert.IsTrue(tg1.follow.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,5,0,0)),"Task follow did not start at 5AM 1/1/1");
            Assert.IsTrue(tg1.follow.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,5,0,0)),"Task follow did not finish at 5AM 1/1/1");

        }

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Td costart-slaved to Tb, so it starts at t=4, not t=1.)
        [TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks Td costart-slaved to Tb, so it starts at t=4, not t=1.")]
		public void TestCoStart(){

            TestGraph1 tg1 = new TestGraph1();
            tg1.tb.AddCostart(tg1.td);

            tg1.model.Start();

            Assert.IsTrue(tg1.tb.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task B did not start at 4AM 1/1/1");

            Assert.IsTrue(tg1.td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task D did not start at 4AM 1/1/1");

        }

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Tc cofinish-slaved to Ta, so it ends at t=4, not t=1.)
        [TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks Tc cofinish-slaved to Ta, so it ends at t=4, not t=1.")]
		public void TestCoFinish(){

            TestGraph1 tg1 = new TestGraph1();
            tg1.ta.AddCofinish(tg1.tc);

            tg1.model.Start();

            Assert.IsTrue(tg1.ta.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task A did not finish at 4AM 1/1/1");

            Assert.IsTrue(tg1.tc.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task C did not finish at 4AM 1/1/1");

		}

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Tc.finish synched to Ta.finish, so tb and td start at t=4.)
        [TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("Checks if two tasks can be synchronized to start at the the same time.")]
		public void TestSynchroStart(){

			TestGraph1 tg1 = new TestGraph1();
			TestGraph1 tg2 = new TestGraph1();
			// Synchronize tb and td
			VertexSynchronizer vs1 = new VertexSynchronizer(tg1.model.Executive,new Vertex[]{tg1.tb.PreVertex,tg1.td.PreVertex},ExecEventType.Detachable);
			// Synchronize tb and tc
			VertexSynchronizer vs2 = new VertexSynchronizer(tg2.model.Executive,new Vertex[]{tg2.tb.PreVertex,tg2.tc.PreVertex},ExecEventType.Detachable);

			tg1.model.Start();
			Debug.WriteLine("Test 2");
			tg2.model.Start();

            // Test graph 1
            Assert.IsTrue(tg1.tb.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task B did not start at 4AM 1/1/1");

            Assert.IsTrue(tg1.td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task D did not start at 4AM 1/1/1");

            Assert.IsTrue(tg1.parent.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,5,0,0)),"Task parent did not finish at 5AM 1/1/1");

            // Test graph 2
            Assert.IsTrue(tg2.tb.GetStartTime(tg2.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task B did not start at 4AM 1/1/1");

            Assert.IsTrue(tg2.tc.GetStartTime(tg2.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task C did not start at 4AM 1/1/1");

            Assert.IsTrue(tg2.parent.GetFinishTime(tg2.GraphContext).Equals(new DateTime(1,1,1,6,0,0)),"Task parent did not finish at 6AM 1/1/1");

		}

		/*
        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Tc.finish synched to Ta.finish, so tb and td start at t=4.)
        [TestMethod] 
		[Highpoint.Sage.Utility.Description("Checks if two tasks can be synchronized to end at the the same time.")]
		[Ignore("This is a future feature - not yet implemented in code.")]
		public void TestSynchroFinish(){

			TestGraph1 tg1 = new TestGraph1();
			TestGraph1 tg2 = new TestGraph1();
			VertexSynchronizer vs1 = new VertexSynchronizer(tg1.model.Executive,new Vertex[]{tg1.ta.PostVertex,tg1.tc.PostVertex},ExecEventType.Detachable);
			VertexSynchronizer vs2 = new VertexSynchronizer(tg2.model.Executive,new Vertex[]{tg2.tc.PostVertex,tg2.ta.PostVertex},ExecEventType.Detachable);

            tg1.model.Start();
			Debug.WriteLine("Test 2");
			tg2.model.Start();

			// Test graph 1
			System.Diagnostics.Debug.Assert(tg1.ta.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task A did not finish at 4AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg1.tc.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task C did not finish at 4AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg1.td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task D did not start at 4AM 1/1/1");

			// Test graph 2
			System.Diagnostics.Debug.Assert(tg2.ta.GetFinishTime(tg2.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task A did not finish at 4AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg2.tc.GetFinishTime(tg2.GraphContext).Equals(new DateTime(1,1,1,1,0,0)),"Task C did not finish at 1AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg2.td.GetStartTime(tg2.GraphContext).Equals(new DateTime(1,1,1,1,0,0)),"Task D did not start at 1AM 1/1/1");

        }*/

        
		class TestGraph1{
            public TestTask ta, tb, tc, td;
			public TestTask parent, follow;
            public TaskProcessor tp;
            public Model model;
            public TestGraph1(){
                model = new Model();
                model.AddService<ITaskManagementService>(new TaskManagementService());


                parent = new TestTask(model,"Parent");
				follow = new TestTask(model,"Follow");

                tp = new TaskProcessor(model,"TP",parent);
                tp.KeepGraphContexts = true;

                ta = new TestTask(model,"TaskA",TimeSpan.FromHours(4));
                tb = new TestTask(model,"TaskB",TimeSpan.FromHours(1));
                tc = new TestTask(model,"TaskC",TimeSpan.FromHours(1));
                td = new TestTask(model,"TaskD",TimeSpan.FromHours(1));

                parent.AddChildEdge(ta);
                parent.AddChildEdge(tb);
                parent.AddChildEdge(tc);
                parent.AddChildEdge(td);
                ta.AddSuccessor(tb);
                tc.AddSuccessor(td);
				parent.AddSuccessor(follow);
            }

            public IDictionary GraphContext { get { return (IDictionary)tp.GraphContexts[0]; } }
        }

        
        /*********************************************************************************/
        /*                   S  U  P  P  O  R  T     M  E  T  H  O  D  S                 */
        /*********************************************************************************/
        IList CreateSubGraph(IModel model, int howManyTasks, string nameRoot){
            ArrayList edges = new ArrayList();
            for ( int i = 0 ; i < howManyTasks ; i++ ) {
                TestTask task = new TestTask(model,nameRoot+i);
                Debug.WriteLine("Creating task " + task.Name);
                edges.Add(task);
            }

            while ( true ) {

                // Select 2 tasks, and connect them.
                TestTask taskA = (TestTask)((Edge)edges[m_random.Next(edges.Count)]);
                TestTask taskB = (TestTask)((Edge)edges[m_random.Next(edges.Count)]);

                if ( taskA == taskB ) continue;

                Debug.WriteLine(String.Format("Considering a connection between {0} and {1}.",taskA.Name,taskB.Name));

                int forward = Graphs.Analysis.PathLength.ShortestPathLength(taskA,taskB);
                int backward = Graphs.Analysis.PathLength.ShortestPathLength(taskB,taskA);

                Debug.WriteLine(String.Format("Forward path length is {0}, and reverse path length is {1}.",forward,backward));

                if ( (forward==int.MaxValue) && (backward==int.MaxValue) ) {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine(String.Format("{0} will follow {1}.",taskB.Name,taskA.Name));
                } else if ( (forward!=int.MaxValue) && (backward==int.MaxValue) ) {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine(String.Format("{0} will follow {1}.",taskB.Name,taskA.Name));
                }else if ( (forward==int.MaxValue) && (backward!=int.MaxValue) ) {
                    taskB.AddSuccessor(taskA);
                    Debug.WriteLine(String.Format("{1} will follow {0}.",taskB.Name,taskA.Name));
                }else {
                    throw new ApplicationException("Cycle exists between " + taskA.Name + " and " + taskB.Name + ".");
                }

                // Once all tasks are connected to something, we're done constructing the test.
                bool allTasksAreConnected = true;
                foreach ( Edge edge in edges ) {
                    Task task = (Task)edge;
                    if ( (edge.PredecessorEdges.Count == 0) && (edge.SuccessorEdges.Count == 0) ) {
                        allTasksAreConnected = false;
                        break;
                    }
                }
                if ( allTasksAreConnected ) break;
            }

            return edges;
        }

        
        class TestTask : Highpoint.Sage.Graphs.Tasks.Task {
            private TimeSpan m_delay = TimeSpan.Zero;
			private bool m_svs = true;
            public TestTask(IModel model, string name):this(model,name,TimeSpan.Zero){}
            public TestTask(IModel model, string name, TimeSpan delay):base(model,name,Guid.NewGuid()){
                m_delay = delay;
                this.EdgeExecutionStartingEvent+=new EdgeEvent(OnTaskBeginning);
                this.EdgeExecutionFinishingEvent+=new EdgeEvent(OnTaskCompleting);
            }
        
            protected override void DoTask(IDictionary graphContext){
				SelfValidState = m_svs;
                if ( m_delay.Equals(TimeSpan.Zero) ) {
                    SignalTaskCompletion(graphContext);
                } else {
                    Debug.WriteLine(Model.Executive.Now + " : " +  Name + " is commencing a sleep for " + m_delay + ".");
                    Model.Executive.RequestEvent(new ExecEventReceiver(DoneDelaying),Model.Executive.Now+m_delay,0.0,graphContext);
                }
            }

            private void DoneDelaying(IExecutive exec, object graphContext){
                SignalTaskCompletion((IDictionary)graphContext);
				Debug.WriteLine(Model.Executive.Now + " : " +  Name + " is done.");
			}

            private void OnTaskBeginning(IDictionary graphContext, Edge edge){
                Debug.WriteLine(Model.Executive.Now + " : " +  Name + " is beginning.");
            }

            private void OnTaskCompleting(IDictionary graphContext, Edge edge){
                Debug.WriteLine(Model.Executive.Now + " : " +  Name + " is completing.");
            }
        }
    }
}

