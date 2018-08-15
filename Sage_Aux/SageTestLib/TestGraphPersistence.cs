/* This source code licensed under the GNU Affero General Public License */
#if NYRFPT
using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Graphs;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Persistence;

namespace Highpoint.Sage.Graphs.Tasks {

	[TestClass]
	public class TaskGraphPersistenceTester {
		private Random m_random = new Random();

		public TaskGraphPersistenceTester(){Init();}
        
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
		
		// Ta(4 hr) -> Tb(1 hr)
		// Tc(1 hr) -> Td(1 hr) Check to see that Td starts at T=1.
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks to see that a plain graph can be created, stored and reloaded.  It also confirms that Td still starts at T=1")]
		public void TestPlainGraphPersistence(){

			TestGraph1 tg1 = new TestGraph1();

			storeXML(tg1);
			tg1 = null;
			loadXML(ref tg1);

			tg1.model.Start();

			Assert.AreEqual(tg1.ta.GetStartTime(tg1.GraphContext),new DateTime(1,1,1,0,0,0), "Task A did not start at 12AM 1/1/1");
			Assert.AreEqual(tg1.ta.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task A did not finish at 4AM 1/1/1");

			Assert.AreEqual(tg1.tb.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task B did not start at 4AM 1/1/1");
			Assert.AreEqual(tg1.tb.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,5,0,0), "Task B did not finish at 5AM 1/1/1");

			Assert.AreEqual(tg1.tc.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,0,0,0), "Task C did not start at 12AM 1/1/1");
			Assert.AreEqual(tg1.tc.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,1,0,0), "Task C did not finish at 1AM 1/1/1");

			Assert.AreEqual(tg1.td.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,1,0,0), "Task D did not start at 1AM 1/1/1");
			Assert.AreEqual(tg1.td.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,2,0,0), "Task D did not start at 1AM 1/1/1");

			Assert.AreEqual(tg1.parent.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,0,0,0), "Task parent did not start at 12AM 1/1/1");
			// AEL, bug \"Set finish time on a task\" submitted
			//Assert.AreEqual(tg1.parent.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,5,0,0), "Task parent did not finish at 5AM 1/1/1");

		}

		// Ta(4 hr) -> Tb(1 hr)
		// Tc(1 hr) -> Td(1 hr) (Td costart-slaved to Tb, so it starts at t=4, not t=1.)
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks to see that store and reload also works with a costart relationship defined.  It also confirms that Td still costart-slaved to Tb, so it starts at t=4, not t=1.")]
		public void TestCoStartPersistance(){

			TestGraph1 tg1 = new TestGraph1();
			tg1.tb.AddCostart(tg1.td);

			storeXML(tg1);
			tg1 = null;
			loadXML(ref tg1);

			tg1.model.Start();

			Assert.AreEqual(tg1.tb.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task B did not start at 4AM 1/1/1");

			Assert.AreEqual(tg1.td.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task D did not start at 4AM 1/1/1");

		}

		// Ta(4 hr) -> Tb(1 hr)
		// Tc(1 hr) -> Td(1 hr) (Tc cofinish-slaved to Ta, so it ends at t=4, not t=1.)
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks to see that store and reload also works with a cofinish relationship defined.  It also confirms that Tc still cofinish-slaved to Ta, so it ends at t=4, not t=1.")]
		public void TestCoFinishPersistence(){

			TestGraph1 tg1 = new TestGraph1();
			tg1.ta.AddCofinish(tg1.tc);

			storeXML(tg1);
			tg1 = null;
			loadXML(ref tg1);

			tg1.model.Start();

			Assert.AreEqual(tg1.ta.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task A did not finish at 4AM 1/1/1");

			Assert.AreEqual(tg1.tc.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task C did not finish at 4AM 1/1/1");

		}

		// Ta(4 hr) -> Tb(1 hr)
		// Tc(1 hr) -> Td(1 hr) (Tc.finish synched to Ta.finish, so tb and td start at t=4.)
		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("Checks to see that store and reload also works with a synchro start relationship defined.  It also confirms that Td still SynchroStart-slaved to Tb as well as Tc still SynchroStart-slaved to Tb.")]
		[Ignore(/*"Vertex deserialization not yet implemented in VertexSynchronizers."*/)]
		public void TestSynchroStartPersistence(){

			TestGraph1 tg1 = new TestGraph1();
			TestGraph1 tg2 = new TestGraph1();
			// Synchronize tb and td
			VertexSynchronizer vs1 = new VertexSynchronizer(tg1.model.Executive,new Vertex[]{tg1.tb.PreVertex,tg1.td.PreVertex},ExecEventType.Detachable);
			// Synchronize tb and tc
			VertexSynchronizer vs2 = new VertexSynchronizer(tg2.model.Executive,new Vertex[]{tg2.tb.PreVertex,tg2.tc.PreVertex},ExecEventType.Detachable);

			storeXML(tg1);
			tg1 = null;
			loadXML(ref tg1);

			storeXML(tg2);
			tg2 = null;
			loadXML(ref tg2);

			tg1.model.Start();
			Debug.WriteLine("Test 2");
			tg2.model.Start();

			// Test graph 1
			Assert.AreEqual(tg1.tb.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task B did not start at 4AM 1/1/1");

			Assert.AreEqual(tg1.td.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task D did not start at 4AM 1/1/1");

			// AEL, bug \"Set finish time on a task\" submitted
			//Assert.AreEqual(tg1.parent.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,5,0,0), "Task parent did not finish at 5AM 1/1/1");

			// Test graph 2
			Assert.AreEqual(tg2.tb.GetStartTime(tg2.GraphContext), new DateTime(1,1,1,4,0,0), "Task B did not start at 4AM 1/1/1");

			Assert.AreEqual(tg2.tc.GetStartTime(tg2.GraphContext), new DateTime(1,1,1,4,0,0), "Task C did not start at 4AM 1/1/1");

			// AEL, bug \"Set finish time on a task\" submitted
			//Assert.AreEqual(tg2.parent.GetFinishTime(tg2.GraphContext), new DateTime(1,1,1,6,0,0), "Task parent did not finish at 6AM 1/1/1");

		}

		
		// Ta(4 hr) -> Tb(1 hr)
		// Tc(1 hr) -> Td(1 hr) (Tc.finish synched to Ta.finish, so tb and td start at t=4.)
		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("Checks to see that store and reload also works with a synchro finish relationship defined."
			 +"It also confirms that Tc still SynchroFinish-slaved to Ta as well as Ta still SynchroFinish-slaved to Tc.")]
		[Ignore(/*"Synchro-finish is a future feature - not yet implemented in code."*/)]
		public void TestSynchroFinishPersistence(){

			TestGraph1 tg1 = new TestGraph1();
			TestGraph1 tg2 = new TestGraph1();
			// Synchronize ta and tc
			VertexSynchronizer vs1 = new VertexSynchronizer(tg1.model.Executive,new Vertex[]{tg1.ta.PostVertex,tg1.tc.PostVertex},ExecEventType.Detachable);
			// Synchronize tc and ta
			VertexSynchronizer vs2 = new VertexSynchronizer(tg2.model.Executive,new Vertex[]{tg2.tc.PostVertex,tg2.ta.PostVertex},ExecEventType.Detachable);

			storeXML(tg1);
			tg1 = null;
			loadXML(ref tg1);

			storeXML(tg2);
			tg2 = null;
			loadXML(ref tg2);

			// Test graph 1
			Assert.AreEqual(tg1.ta.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task A did not finish at 4AM 1/1/1");

			Assert.AreEqual(tg1.tc.GetFinishTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task C did not finish at 4AM 1/1/1");

			Assert.AreEqual(tg1.td.GetStartTime(tg1.GraphContext), new DateTime(1,1,1,4,0,0), "Task D did not start at 4AM 1/1/1");

			tg1.model.Start();
			Debug.WriteLine("Test 2");
			tg2.model.Start();

			// Test graph 2
			Assert.AreEqual(tg2.ta.GetFinishTime(tg2.GraphContext), new DateTime(1,1,1,4,0,0), "Task A did not finish at 4AM 1/1/1");

			Assert.AreEqual(tg2.tc.GetFinishTime(tg2.GraphContext), new DateTime(1,1,1,1,0,0), "Task C did not finish at 1AM 1/1/1");

			Assert.AreEqual(tg2.td.GetStartTime(tg2.GraphContext), new DateTime(1,1,1,1,0,0), "Task D did not start at 1AM 1/1/1");

		}

		private void storeXML(TestGraph1 tg1) {
			XmlSerializationContext xmlsc = new XmlSerializationContext();
			xmlsc.StoreObject("TG",tg1);
			xmlsc.Save(System.Environment.GetEnvironmentVariable("TEMP") + "\\TestGraphPersistence.xml");
		}

		private void loadXML(ref TestGraph1 tg1) {
			XmlSerializationContext xmlsc = new XmlSerializationContext();
			xmlsc.Load(System.Environment.GetEnvironmentVariable("TEMP") + "\\TestGraphPersistence.xml");
			xmlsc.ContextEntities.Add("Model",new Model("Reconstituted Model"));
			tg1 = (TestGraph1)xmlsc.LoadObject("TG");
		}
        
		class TestGraph1 : IXmlPersistable {
			public TestTask ta, tb, tc, td;
			public TestTask parent;
			public TaskProcessor tp;
			public Model model;
			public TestGraph1(){
				model = new Model();
                model.AddService<ITaskManagementService>(new TaskManagementService());
                parent = new TestTask(model,"Parent");

			    tp = new TaskProcessor(model, "TP", parent) {KeepGraphContexts = true};
			    model.GetService<ITaskManagementService>().AddTaskProcessor(tp);

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
			}

			public IDictionary GraphContext { get { return (IDictionary)tp.GraphContexts[0]; } }

			#region IXmlPersistable Members
			/// <summary>
			/// A default constructor, to be used for creating an empty object prior to reconstitution from a serializer.
			/// </summary>
//			public TestGraph1(){} 
			/// <summary>
			/// Serializes this object to the specified XmlSerializatonContext.
			/// </summary>
			/// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
			public void SerializeTo(XmlSerializationContext xmlsc) {
				xmlsc.StoreObject("Parent",tp.MasterTask);
			}

			/// <summary>
			/// Deserializes this object from the specified XmlSerializatonContext.
			/// </summary>
			/// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
			public void DeserializeFrom(XmlSerializationContext xmlsc) {
				model = (Model)xmlsc.ContextEntities["Model"];
				Task rootTask = (Task)xmlsc.LoadObject("Parent");
				tp = new TaskProcessor(model,"TP_Reloaded",Guid.NewGuid(),rootTask);
				tp.KeepGraphContexts = true;

				foreach ( TestTask task in tp.MasterTask.GetChildTasks(false) ) {
					if ( task.Name.Equals("TaskA") ) ta = task;
					if ( task.Name.Equals("TaskB") ) tb = task;
					if ( task.Name.Equals("TaskC") ) tc = task;
					if ( task.Name.Equals("TaskD") ) td = task;
				}
			}

			#endregion
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

        
		class TestTask : Highpoint.Sage.Graphs.Tasks.Task, IXmlPersistable	 {
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
			}

			private void OnTaskBeginning(IDictionary graphContext, Edge edge){
				Debug.WriteLine(Model.Executive.Now + " : " +  Name + " is beginning.");
			}

			private void OnTaskCompleting(IDictionary graphContext, Edge edge){
				Debug.WriteLine(Model.Executive.Now + " : " +  Name + " is completing.");
			}
			#region IXmlPersistable Members
			public TestTask(){}
			public override void SerializeTo(XmlSerializationContext xmlsc) {
				base.SerializeTo(xmlsc);
				xmlsc.StoreObject("Delay",m_delay);
				xmlsc.StoreObject("SVS",m_svs);
			}

			public override void DeserializeFrom(XmlSerializationContext xmlsc) {
				base.DeserializeFrom(xmlsc);
				m_delay = (TimeSpan)xmlsc.LoadObject("Delay");
				m_svs = (bool)xmlsc.LoadObject("SVS");
			}

			#endregion
		}
	}
}

#endif