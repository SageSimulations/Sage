/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs {

	[TestClass]
	public class DAGCycleCheckerTester {

		private Random m_random;
		private ArrayList m_edges;

		public DAGCycleCheckerTester(){Init();}
        
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test initializes a model and runs a validation")]
		public void TestBasicValidation() {
			m_random = new Random(98765);

			Edge root = CreateGraph(10,"Foo");
			Console.WriteLine(Highpoint.Sage.Diagnostics.DiagnosticAids.GraphToString(root));

			DAGCycleChecker dcc = new DAGCycleChecker(root);
			dcc.Check(true);
			Console.WriteLine("There are " + dcc.Errors.Count + " errors.");
			foreach ( IModelError err in dcc.Errors ) Console.WriteLine(err.ToString());

			AddLoop(root);
			
			Console.WriteLine(Highpoint.Sage.Diagnostics.DiagnosticAids.GraphToString(root));
			dcc.Check(true);
			Console.WriteLine("There are " + dcc.Errors.Count + " errors.");
			foreach ( IModelError err in dcc.Errors ) Console.WriteLine(err.ToString());

			//if ( ag != null ) ag.AssertEquals("Freshly initialized model, (config = ) is not valid.", true, t.ValidityState);
		
		}

		class MyDAGCycleChecker : Highpoint.Sage.Graphs.DAGCycleChecker
        { 
			private Edge m_root;
			private Edge m_loopback;
			private bool m_enableLoopback = false;

			public MyDAGCycleChecker(Edge root):base(root){
				m_loopback = new Edge("Loopback");
				m_root = root;
			}
			public bool EnableLoopback { get { return m_enableLoopback; } set { m_enableLoopback = value; } }
			protected override ArrayList GetSuccessors(object element) {
				ArrayList retval = base.GetSuccessors (element);
				if ( m_enableLoopback && element.Equals(m_root.PostVertex) ) {
					retval.Add(m_loopback);
				}
				if ( m_enableLoopback && element.Equals(m_loopback) ) {
					retval.Add(m_root.PreVertex);
				}
				return retval;
			}
		}
		
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test initializes a model and runs a validation")]
		public void TestValidationWithImpliedRelationships() {
			DateTime start;
			m_random = new Random(40419);

			start = DateTime.Now;
			Edge root = CreateGraph(500,"Foo");
			Console.WriteLine("Creating the graph took " + (DateTime.Now - start));
			MyDAGCycleChecker mdcc = new MyDAGCycleChecker(root);
			//Console.WriteLine(Highpoint.Sage.Diagnostics.DiagnosticAids.GraphToString(root));
			
			start = DateTime.Now;
			mdcc.Check(false);
			Console.WriteLine("Checking took " + (DateTime.Now - start));
			Console.WriteLine("There are " + mdcc.Errors.Count + " errors.");
			foreach ( IModelError err in mdcc.Errors ) Console.WriteLine(err.ToString());
			
			mdcc.EnableLoopback = true;

			mdcc.Check(false);
			Console.WriteLine("There are " + mdcc.Errors.Count + " errors.");
			foreach ( IModelError err in mdcc.Errors ) Console.WriteLine(err.ToString());

			//if ( ag != null ) ag.AssertEquals("Freshly initialized model, (config = ) is not valid.", true, t.ValidityState);
	
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test initializes a model and runs a validation")]
		public void TestValidationPerformance() {

			foreach (int i in new int[]{5,10,20,50,100,200/*,500,1000*/} ) {

				long totalTicks = 0;
				int nSeeds = 50;
				Random seedGenerator = new Random(40419);
				for ( int seed = 0 ; seed < nSeeds ; seed++ ){
					m_random = new Random(seedGenerator.Next());
					Edge root = CreateGraph(i,"Foo");
					MyDAGCycleChecker mdcc = new MyDAGCycleChecker(root);
					//Console.WriteLine(Highpoint.Sage.Diagnostics.DiagnosticAids.GraphToString(root));
			
					DateTime dt = DateTime.Now;
					mdcc.Check(false);
					totalTicks += ((TimeSpan)(DateTime .Now - dt)).Ticks;
					//Console.WriteLine(i.ToString() + "," + ((TimeSpan)(DateTime .Now - dt)).TotalSeconds);
					//Console.WriteLine("There are " + mdcc.Errors.Count + " errors.");
					//foreach ( IModelError err in mdcc.Errors ) Console.WriteLine(err.ToString());
			
					//mdcc.EnableLoopback = true;

					//mdcc.Check(false);
					//Console.WriteLine("There are " + mdcc.Errors.Count + " errors.");
					//foreach ( IModelError err in mdcc.Errors ) Console.WriteLine(err.ToString());

					//if ( ag != null ) ag.AssertEquals("Freshly initialized model, (config = ) is not valid.", true, t.ValidityState);
				}
				Console.WriteLine(i.ToString() + "," + (TimeSpan.FromTicks(totalTicks/nSeeds)).TotalSeconds);
			}
		}
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test initializes a model and runs a validation")]
		public void TestInnerEdgePerformance() {

			foreach (int i in new int[]{5,10,15,20,25,30,35,40,45,50,55,60,65,70,75,80,85,90,95,100} ) {

				long totalTicks = 0;
				long innerTicks = 0;
				int nSeeds = 25;
				DateTime dt;
				Random seedGenerator = new Random(40419);
				for ( int seed = 0 ; seed < nSeeds ; seed++ ){
					m_random = new Random(seedGenerator.Next());
					Edge randomInnerEdge;
					Edge root = CreateGraph(i,"Foo", out randomInnerEdge);
					MyDAGCycleChecker mdcc = new MyDAGCycleChecker(root);
			
					dt = DateTime.Now;
					mdcc.Check(false);
					totalTicks += ((TimeSpan)(DateTime.Now - dt)).Ticks;

					dt = DateTime.Now;
					mdcc.Check(false,randomInnerEdge.PreVertex);
					innerTicks += ((TimeSpan)(DateTime.Now - dt)).Ticks;
				}
				TimeSpan avgWholeTime = TimeSpan.FromTicks(totalTicks/nSeeds);
				TimeSpan avgInnerTime = TimeSpan.FromTicks(innerTicks/nSeeds);
				double improvement    = avgInnerTime.TotalSeconds/avgWholeTime.TotalSeconds;
				Console.WriteLine(i.ToString() + "," + avgWholeTime.TotalSeconds + "," + avgInnerTime.TotalSeconds + "," + improvement);
			}
		}

		private Edge CreateGraph(int howManyChildren, string nameRoot){
			Edge rie;
			return CreateGraph(howManyChildren,nameRoot,out rie);
		}
		
		/*********************************************************************************/
		/*                   S  U  P  P  O  R  T     M  E  T  H  O  D  S                 */
		/*********************************************************************************/
		private Edge CreateGraph(int howManyChildren, string nameRoot, out Edge randomInnerEdge){
			randomInnerEdge = null;
			m_edges = new ArrayList();
			for ( int i = 0 ; i < howManyChildren ; i++ ) {
				Edge edge = new Edge(nameRoot+i);
				if ( i == ((int)howManyChildren/2) ) randomInnerEdge = edge;
				//_Debug.WriteLine("Creating edge " + edge.Name);
				m_edges.Add(edge);
			}

			while ( true ) {

				// Select 2 edges, and connect them.
				Edge edgeA = (Edge)((Edge)m_edges[m_random.Next(m_edges.Count)]);
				Edge edgeB = (Edge)((Edge)m_edges[m_random.Next(m_edges.Count)]);

				if ( edgeA == edgeB ) continue;

				//_Debug.WriteLine(String.Format("Considering a connection between {0} and {1}.",edgeA.Name,edgeB.Name));

				int forward = Graphs.Analysis.PathLength.ShortestPathLength(edgeA,edgeB);
				int backward = Graphs.Analysis.PathLength.ShortestPathLength(edgeB,edgeA);

				//_Debug.WriteLine(String.Format("Forward path length is {0}, and reverse path length is {1}.",forward,backward));

				if ( (forward==int.MaxValue) && (backward==int.MaxValue) ) {
					edgeA.AddSuccessor(edgeB);
					//_Debug.WriteLine(String.Format("{0} will follow {1}.",edgeB.Name,edgeA.Name));
				} else if ( (forward!=int.MaxValue) && (backward==int.MaxValue) ) {
					edgeA.AddSuccessor(edgeB);
					//_Debug.WriteLine(String.Format("{0} will follow {1}.",edgeB.Name,edgeA.Name));
				}else if ( (forward==int.MaxValue) && (backward!=int.MaxValue) ) {
					edgeB.AddSuccessor(edgeA);
					//_Debug.WriteLine(String.Format("{1} will follow {0}.",edgeB.Name,edgeA.Name));
				}else {
					throw new ApplicationException("Cycle exists between " + edgeA.Name + " and " + edgeB.Name + ".");
				}

				// Once all edges are connected to something, we're done constructing the test.
				bool allEdgesAreConnected = true;
				foreach ( Edge edge in m_edges ) {
					if ( (edge.PredecessorEdges.Count == 0) && (edge.SuccessorEdges.Count == 0) ) {
						allEdgesAreConnected = false;
						break;
					}
				}
				if ( allEdgesAreConnected ) break;
			}

			Edge root = new Edge("Root");

			foreach ( Edge edge in m_edges ) {
				if ( edge.PreVertex.PredecessorEdges.Count == 0 ) {
					root.AddChildEdge(edge);
				}
				if ( edge.PostVertex.SuccessorEdges.Count == 0 ) {
					root.AddChildEdge(edge);
				}
			}

			return root;
		}
	

		void AddLoop(Edge edgeRoot){
			Edge tmp = (Edge)m_edges[m_random.Next(m_edges.Count)];
			
			while ( tmp.PostVertex.SuccessorEdges.Count == 0 ) tmp = (Edge)m_edges[m_random.Next(m_edges.Count)];
            Vertex to = tmp.PreVertex;

			foreach ( Edge e in m_edges ) {
				Vertex from = e.PostVertex;
				if ( IsV1PredecessorOfV2(to,from) ) {
					Ligature loopback = new Ligature(from,to,"Loopback");
				}
			}
		}

		bool IsV1PredecessorOfV2(Vertex v1, Vertex v2){
			foreach ( Edge e in v2.PredecessorEdges ) {
				if ( e.PreVertex.Equals(v1) ) return true;
				return IsV1PredecessorOfV2(v1,e.PreVertex); 
			}
			return false;
		}
	}
}

