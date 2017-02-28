/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.Graphs {

	/// <summary>
	/// A DAGCycleChecker walks a Directed Acyclic Graph, depth-first, looking for cycles, which it detects
	/// through the repeated encountering of a given vertex along a given path. After evaluating the DAG,
	/// it presents a collection of errors (the Errors field) in the DAG. The errors are instances of
	/// DAGStructureError, which implements IModelError, and describes either the first, or all cycles in
	/// the network of edges underneath the root edge.
	/// </summary>
	public class DAGCycleChecker {

		#region Private Fields
		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("DAGCycleChecker");
		private IEdge m_rootEdge;
		private ArrayList m_errors;
		private bool m_haltOnError;
		private Stack m_currentPath;
		private int m_level = 0;
		private Hashtable m_nodes;
		private bool m_collapse;
		#endregion

		public DAGCycleChecker(IEdge rootEdge):this(rootEdge,false){}
		
		/// <summary>
		/// Creates a DAGCycleChecker that can evaluate the DAG under the specified edge.
		/// </summary>
        /// <param name="rootEdge">The edge that defines the DAG to be analyzed - the DAG runs from thepreVertex of this edge to the postVertex, and includes all children.</param>
		/// <param name="collapse">If true, the graph is collapsed to make it smaller.</param>
        public DAGCycleChecker(IEdge rootEdge, bool collapse) {
			m_rootEdge = rootEdge;
			m_haltOnError = true;
			m_currentPath = new Stack();
			m_errors = new ArrayList();
			m_nodes = new Hashtable();
			m_collapse = collapse;
		}

        /// <summary>
        /// Forces this DAGCycleChecker to validate the entire DAG by checking for cycles.
        /// </summary>
        /// <param name="haltOnError">If true, the checker will stop as soon as it finds the first error.</param>
        /// <param name="startElement">The vertex at which the Cycle Checker begins its search.</param>
        /// <returns>
        /// True if the DAGCycleChecker found no errors.
        /// </returns>
        public virtual bool Check(bool haltOnError, object startElement) {
			m_haltOnError = haltOnError;
			m_currentPath.Clear();
			m_errors.Clear();
			m_nodes.Clear();

			Build(m_rootEdge.PreVertex);
			Node start = (Node)m_nodes[startElement?? m_rootEdge];

			Advance(start);

			return (m_errors.Count == 0);
		}

		/// <summary>
		/// Forces this DAGCycleChecker to validate the entire DAG by checking for cycles.
		/// </summary>
		/// <param name="haltOnError">If true, the checker will stop as soon as it finds the first error.</param>
		/// <returns>True if the DAGCycleChecker found an error.</returns>
		public virtual bool Check(bool haltOnError){
			return Check(haltOnError,m_rootEdge.PreVertex);
		}

		private Node Build(object element){
			Node node = m_nodes[element] as Node;
			if ( node == null ) {
				node = new Node(element);
				m_nodes.Add(element,node);
				object[] successors = (object[])GetSuccessors(element).ToArray(typeof(object));
				Node[] naSuccessors = new Node[successors.Length];
				for ( int i = 0 ; i < successors.Length ; i++ ) {
					naSuccessors[i]=Build(successors[i]);
				}
				node.Successors = naSuccessors;
			}

			if ( m_collapse ) {
				for ( int i = 0 ; i < node.Successors.Length ; i++ ) {
					Collapse(node);
				}
			}
			return node;
		}

		private void Collapse(Node node){
			if ( node.Element.Equals(m_rootEdge.PostVertex) ) return;
			for ( int i = 0 ; i < node.Successors.Length ; i++ ) {
				Node successor = node.Successors[i];
				Collapse(successor);
				if ( successor.Successors.Length == 0 ) {
					node.Successors[i] = null;
				} else if ( successor.Successors.Length == 1 ) {
					node.Successors[i] = successor.Successors[i];
				}
			}
		}

		#region Element Handlers
		/// <summary>
		/// Moves the checking cursor forward to the specified vertex. Calls EvaluateVertex(...) to ensure 
		/// that the new vertex has not been encountered along this path yet, and calls GetEdgesFromVertex(...)
		/// to determine the next group of edges to be traversed, following which, it calls Advance(Edge edge)
		/// on each of those edges. After a path has been explored, the Advance method calls Retreat(...) on
		/// the specified vertex, and the cursor retreats to where it was before this path was explored.
		/// </summary>
		/// <param name="node">The Node that is to be added to the current depth path.</param>
		private void Advance(Node node){
			if ( s_diagnostics ) {
				for ( int i = 0 ; i < m_level ; i++ ) Console.Write("  ");
				Console.WriteLine("Advancing to " + node.Element + ".");
			}

			if ( m_haltOnError && m_errors.Count>0 ) return;

			if ( node.OnPath ) {
				LogError(node.Element);
			} else {
				if ( !node.Visited ) {
					node.Visited = true;
					node.OnPath = true;
					m_currentPath.Push(node.Element);
					m_level++;
					foreach ( Node successor in node.Successors ) Advance(successor);
					m_level--;
					node.OnPath = false;
					m_currentPath.Pop();
				}
			}
		}

		/// <summary>
		/// Returns an ArrayList of the edges that are downstream from the given vertex, in a depth-first traversal.
		/// </summary>
		/// <param name="element">The element, forward from which we wish to proceed.</param>
		/// <returns>An ArrayList of the elements that are downstream from the given element, in a depth-first traversal.</returns>
		protected virtual ArrayList GetSuccessors(object element){
			ArrayList successors = new ArrayList();
			if ( element is Vertex ) {
				Vertex vertex = (Vertex)element;
				if ( vertex.SuccessorEdges != null && !vertex.Equals(m_rootEdge.PostVertex) ) successors.AddRange(vertex.SuccessorEdges);
			} else if (element is Edge){
				Edge edge = (Edge)element;
				successors.Add(edge.PostVertex);
			} else {
				//throw new ApplicationException("Don't know what to do with a " + element.ToString() + ".");
			}
			return successors;
		}

		/// <summary>
		/// Adds an error indicating that this element represents the start of a cycle. Cycles will be detected
		/// by the first recurring element in a path.
		/// </summary>
		/// <param name="element">The element that has just been added to the current depth path.</param>
		private void LogError(object element){
			#region Create & add an error.
			if ( s_diagnostics ) {
				for ( int i = 0 ; i < m_level ; i++ ) Console.Write("  ");
				Console.WriteLine(">>>>>>>>>>>> Element " + element + " represents the start of a cycle.");
			}

			ArrayList elements = new ArrayList();
			ArrayList pathObjArray = new ArrayList(m_currentPath.ToArray());
			string narrative = "Cycle detected - elements are : ";
			int startOfLoop = pathObjArray.IndexOf(element);
			for ( int i = startOfLoop ; i >= 0; i-- ) {
				object elementInPath = pathObjArray[i];
				elements.Add(elementInPath);
				narrative += elementInPath.ToString();
				if ( i > 1 ) {
					narrative += ", ";
				} else if ( i == 1 ) {
					narrative += " and ";
				}
			}
			DagStructureError se = new DagStructureError(m_rootEdge,elements,narrative);
			m_errors.Add(se);
			#endregion
		}
		
		
		#endregion

		#region Error Management (Present & Clear errors...)
		/// <summary>
		/// A collection of the errors that the DAGCycleChecker found in the DAG, during its last check.
		/// </summary>
		public ICollection Errors {
			get { return ArrayList.ReadOnly(m_errors); }
		}

		/// <summary>
		/// Clears out the collection of errors.
		/// </summary>
		public void ClearErrors(){
			m_errors.Clear();
		}
		#endregion
		
		class Node {
			#region Private Fields
			private static Node[] _emptyArray = new Node[]{};
			private bool m_onPath;
			private bool m_visited;
			private object m_element;
			private Node[] m_successors;
			#endregion
			
			public Node(object element){
				m_element = element;
				m_successors = _emptyArray;
				m_onPath = false;
			}

			public object Element { get { return m_element; } }
			public bool OnPath { get { return m_onPath; } set { m_onPath = value; } }
			public bool Visited { get { return m_visited; } set { m_visited = value; } }
			public Node[] Successors { get { return m_successors; } set { m_successors = (value==null?_emptyArray:value); } }

			public override bool Equals(object obj) {
				return m_element.Equals(((Node)obj).m_element);
			}
			public override int GetHashCode() {
				return m_element.GetHashCode();
			}
		}
	}
}
