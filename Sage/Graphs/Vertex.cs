/* This source code licensed under the GNU Affero General Public License */


using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore; // For executive.
using Highpoint.Sage.Persistence;

namespace Highpoint.Sage.Graphs {

    public delegate void VertexEvent(IDictionary graphContext, Vertex theVertex);
	public delegate void TriggerDelegate(IDictionary graphContext);

	public interface IVertex : IVisitable, IXmlPersistable, IHasName, Validity.IHasValidity {
		Vertex.WhichVertex Role { get; }
		Edge PrincipalEdge { get; }
		IList PredecessorEdges { get; }
		IList SuccessorEdges { get; }
		IEdgeFiringManager EdgeFiringManager { get; }
		IEdgeReceiptManager EdgeReceiptManager { get; }
		void PreEdgeSatisfied(IDictionary graphContext, Edge theEdge);
		TriggerDelegate FireVertex { get; }
	}

    
	public class Vertex : IVertex {

        public enum WhichVertex { Pre, Post };

		#region Public Events
		public event StaticEdgeEvent PreEdgeAddedEvent;
		public event StaticEdgeEvent PostEdgeAddedEvent;
		public event StaticEdgeEvent PreEdgeRemovedEvent;
		public event StaticEdgeEvent PostEdgeRemovedEvent;

		public event VertexEvent BeforeVertexFiringEvent;
		public event VertexEvent AfterVertexFiringEvent;
		#endregion

        internal int NumPreEdges = 0;
        internal int NumPostEdges = 0;

        // TODO: Tune this implementation for efficiency. Clarity is key, now.
        protected ArrayList PreEdges = new ArrayList(2);
        protected ArrayList PostEdges = new ArrayList(2);

		#region Private Fields
		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Vertex");
		private static readonly bool s_managePostMortemData = Diagnostics.DiagnosticAids.Diagnostics("Graph.KeepPostMortems");
		private static ArrayList _emptyCollection = ArrayList.ReadOnly(new ArrayList());

		private string m_name;
		private Edge m_principalEdge;
		private PreEdgesSatisfiedKey m_preEdgesSatisfiedKey = new PreEdgesSatisfiedKey();

		private static int _vertexNum = 0;

		private VertexSynchronizer m_synchronizer;
		private IEdgeFiringManager m_edgeFiringManager = null;
		private IEdgeReceiptManager m_edgeReceiptManager = null;

        private WhichVertex m_role;
        private bool m_roleIsKnown = false;
		
		private TriggerDelegate m_triggerDelegate;
		#endregion
        
		#region Constructors
		public Vertex(Edge principalEdge):this(principalEdge,"Vertex"+(_vertexNum++)){}
		public Vertex(Edge principalEdge, string name){
			m_name = name;
			m_principalEdge = principalEdge;
			m_triggerDelegate = new TriggerDelegate(DefaultVertexFiringMethod);
		}
		#endregion


        public WhichVertex Role { 
            get {
                if ( !m_roleIsKnown ) {
                    m_role = (m_principalEdge.PreVertex == this?WhichVertex.Pre:WhichVertex.Post);
                    m_roleIsKnown = true;
                }
                return m_role;
            }
        }

        public string Name { get { return m_name; } }

		
		/// <summary>
		/// The edge firing manager is responsible for determining which successor edges fire,
		/// following satisfaction of a vertex. If this is null, it is assumed that all
		/// edges are to fire. If it is non-null, then each successor edge is presented to
		/// the EdgeFiringManager on it's FireIfAppropriate(Edge e) API to determine if it
		/// should fire.
		/// </summary>
		public IEdgeFiringManager EdgeFiringManager { 
			get {
				return m_edgeFiringManager;
			}
			set {
				m_edgeFiringManager = value;
			}
		}

		/// <summary>
		/// The edge receipt manager is notified of the satisfaction (firing) of pre-edges, and
		/// is responsible for determining when the vertex is to fire. If it is null, then it is
		/// assumed that only if all incoming edges have fired, is the vertex to fire.
		/// </summary>
		public IEdgeReceiptManager EdgeReceiptManager { 
			get {
				return m_edgeReceiptManager;
			}
			set {
				m_edgeReceiptManager = value;
			}
		}


		#region Add, Remove and Access Pre and Post edges including Principal edge.
        public Edge PrincipalEdge { get { return m_principalEdge; } }

        public IList PredecessorEdges {
            get {
                return ArrayList.ReadOnly(PreEdges);
            }
        }

        public IList SuccessorEdges {
            get {
                return ArrayList.ReadOnly(PostEdges);
            }
        }

		public void AddPreEdge(Edge preEdge){
			if ( PreEdges.Contains(preEdge) ) return;
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( s_diagnostics ) Trace.WriteLine(String.Format("{0} adding preEdge {1}.",Name,preEdge.Name));
			PreEdges.Add(preEdge);
			System.Threading.Interlocked.Increment(ref NumPreEdges);
			if ( PreEdgeAddedEvent != null ) PreEdgeAddedEvent(PrincipalEdge);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddPreEdge,false);
			if ( hasVm ) m_vm.Resume();
		}

		public void RemovePreEdge(Edge preEdge){
			if ( !PreEdges.Contains(preEdge) ) return;
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( s_diagnostics ) Trace.WriteLine(String.Format("{0} removing preEdge {1}.",Name,preEdge.Name));
			PreEdges.Remove(preEdge);
			System.Threading.Interlocked.Decrement(ref NumPreEdges);
			if ( PreEdgeRemovedEvent != null ) PreEdgeRemovedEvent(PrincipalEdge);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.RemovePreEdge,false);
			if ( hasVm ) m_vm.Resume();
		}

		public void AddPostEdge(Edge postEdge){
			if ( PostEdges.Contains(postEdge) ) return;
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( s_diagnostics ) Trace.WriteLine(String.Format("{0} adding postEdge {1}.",Name,postEdge.Name));
			PostEdges.Add(postEdge);
			System.Threading.Interlocked.Increment(ref NumPostEdges);
			if ( PostEdgeAddedEvent != null ) PostEdgeAddedEvent(PrincipalEdge);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddPostEdge,false);
			if ( hasVm ) m_vm.Resume();
		}

		public void RemovePostEdge(Edge postEdge){
			if ( !PostEdges.Contains(postEdge) ) return;
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( s_diagnostics ) Trace.WriteLine(String.Format("{0} removing postEdge {1}.",Name,postEdge.Name));
			PostEdges.Remove(postEdge);
			System.Threading.Interlocked.Decrement(ref NumPostEdges);
			if ( PostEdgeRemovedEvent != null ) PostEdgeRemovedEvent(PrincipalEdge);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.RemovePostEdge,false);
			if ( hasVm ) m_vm.Resume();
		}
		#endregion

		
		#region Handlers for actually firing the vertex
		/// <summary>
		/// This property represents the firing method will be called when it is time to fire the vertex. The developer may
		/// substitute a delegate that performs some activity prior to actually firing the vertex. This
		/// substituted delegate must, after doing whatever it does, call the DefaultVertexFiringMethod(graphContext)...
		/// </summary>
		public TriggerDelegate FireVertex {
			get { return m_triggerDelegate; }
			set { m_triggerDelegate = value; }
		}

		/// <summary>
		/// This is the default method used to fire this vertex.
		/// </summary>
		/// <param name="graphContext">The graph context for execution.</param>
		public void DefaultVertexFiringMethod(IDictionary graphContext){
			if ( m_synchronizer != null ){
				m_synchronizer.NotifySatisfied(this,graphContext);
				return;
			} else {
				_FireVertex(graphContext);
			}
		}

		/// This is here as a target for an event handler in case the vertex firing is desired to be
		/// done asynchronously.
		/// <param name="exec">The executive by which this event is being serviced.</param>
		/// <param name="graphContext">The graph context for execution.</param>
		internal void _AsyncFireVertexHandler(IExecutive exec, object graphContext){
			_FireVertex((IDictionary)graphContext);
		}

		/// <summary>
		/// This method is called when it's time to fire the vertex, and even the vertex's
		/// synchronizer (if it has one) has been satisfied.
		/// </summary>
		/// <param name="graphContext">The graphContext of the current event thread.</param>
		internal void _FireVertex(IDictionary graphContext){

			// Start by notifying anyone who cares that we're about to fire the vertex.
			if ( BeforeVertexFiringEvent != null ) BeforeVertexFiringEvent(graphContext,this);
            
			#region Manage Post-Mortem Data
#if DEBUG
			if ( s_managePostMortemData ){
				PmData pmData = (PmData)graphContext["PostMortemData"];
				if ( pmData == null ) {
					pmData = new PmData();
					graphContext.Add("PostMortemData",pmData);
				}
				pmData.VerticesFired.Add(this);
			}
#endif //DEBUG
			#endregion

			if ( s_diagnostics ) Trace.WriteLine("Firing vertex " + Name);
	
			if ( m_edgeFiringManager != null ) m_edgeFiringManager.Start(graphContext);

			// If this is a preVertex, we want to make sure the principal edge is fired first,
			// otherwise it doesn't matter. We fire all successor (post) edges.
			if ( Role.Equals(WhichVertex.Pre) ) {
				if ( m_edgeFiringManager == null ) {
					m_principalEdge.PreVertexSatisfied(graphContext);
				} else {
					m_edgeFiringManager.FireIfAppropriate(graphContext,m_principalEdge);
				}
			}
			foreach ( Edge e in PostEdges ) {
				if ( e.Equals(m_principalEdge) ) continue; // We've already fired it.
				if ( m_edgeFiringManager == null ) {
					e.PreVertexSatisfied(graphContext);
				} else {
					m_edgeFiringManager.FireIfAppropriate(graphContext,e);
				}
			}

			// Finish with a notification of completion.
			if ( AfterVertexFiringEvent != null ) AfterVertexFiringEvent(graphContext,this);
		}

		#endregion
		
		/// <summary>
		/// This method is called when an incoming pre-edge has been fired, and it could therefore
		/// be time to fire this vertex.
		/// </summary>
		/// <param name="graphContext">The graphContext in whose context this traversal of the graph
		/// is to take place.</param>
		/// <param name="theEdge">The edge that was just fired.</param>
		public void PreEdgeSatisfied(IDictionary graphContext, Edge theEdge){
			if ( m_edgeReceiptManager == null ) {
				#region Default Edge Firing Handling
				if ( PreEdges.Count < 2 ) { // If there's only one pre-edge, it must be okay to fire the vertex.
					FireVertex(graphContext);
				} else {
					ArrayList preEdgesSatisfied = (ArrayList)graphContext[m_preEdgesSatisfiedKey];
					if ( preEdgesSatisfied == null ) {
						preEdgesSatisfied = new ArrayList();
						graphContext[m_preEdgesSatisfiedKey] = preEdgesSatisfied;
					}
                    
					if ( PreEdges == null ) throw new ApplicationException("Edge (" + theEdge + ") signaled completion to " + this + ", a node with no predecessor edges.");
                    
					if ( !PreEdges.Contains(theEdge) ) throw new ApplicationException("Unknown edge (" + theEdge + ") signaled completion to " + this);
                    
					if ( preEdgesSatisfied.Contains(theEdge) ) {
						throw new ApplicationException("Edge (" + theEdge + ") signaled completion twice, to " + this);
					}
					preEdgesSatisfied.Add(theEdge);

					if ( preEdgesSatisfied.Count == PreEdges.Count ) {
						graphContext.Remove(this); // Remove the preEdgesSatisfied arraylist. Implicit recycle.
						FireVertex(graphContext);
					}
				}
				#endregion
			} else {
				m_edgeReceiptManager.OnPreEdgeSatisfied(graphContext,theEdge);
			}
        }

		#region Synchronizer Management
		/// <summary>
		/// A synchronizer, ip present, defines a relationship among vertices wherein all vertices
		/// wait until they are all ready to fire, and then they fire in the specified order.
		/// </summary>
		public VertexSynchronizer Synchronizer { get { return m_synchronizer; } } 
		/// <summary>
		/// This is used as an internal accessor to set synchronizer to null, or other values. The
		/// property is public, but read-only.
		/// </summary>
		/// <param name="synch"></param>
		internal void SetSynchronizer(VertexSynchronizer synch){
			if ( synch != null && m_synchronizer != null ) throw new ApplicationException(Name + " already has a synchronizer assigned!");
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			m_synchronizer = synch;
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.NewSynchronizer,false);
			if ( hasVm ) m_vm.Resume();
		}

		#endregion
	
		#region IVisitable Members
		public virtual void Accept(IVisitor visitor){
			visitor.Visit(this);
		}
		#endregion

		#region IXmlPersistable Members
		public Vertex(){}
		public virtual void SerializeTo(XmlSerializationContext xmlsc) {
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("PostEdges",PostEdges);
			xmlsc.StoreObject("PreEdges",PreEdges);
			xmlsc.StoreObject("PrincipalEdge",m_principalEdge);
			xmlsc.StoreObject("Role",m_role);
			xmlsc.StoreObject("RoleIsKnown",m_roleIsKnown);
			xmlsc.StoreObject("Synchronizer",m_synchronizer);
			xmlsc.StoreObject("TriggerDelegate",m_triggerDelegate);
			// What about Trigger Delegate?
		}

		public virtual void DeserializeFrom(XmlSerializationContext xmlsc) {

			m_name = (string)xmlsc.LoadObject("Name");
			ArrayList tmpPostEdges = (ArrayList)xmlsc.LoadObject("PostEdges");
			foreach ( Edge edge in tmpPostEdges ) {
				if ( !PostEdges.Contains(edge) ) PostEdges.Add(edge);
				NumPostEdges++;
			}
			ArrayList tmpPreEdges = (ArrayList)xmlsc.LoadObject("PreEdges");
			foreach ( Edge edge in tmpPreEdges)  {
				if ( !PreEdges.Contains(edge) ) PreEdges.Add(edge);
				NumPreEdges++;
			}

			m_principalEdge = (Edge)xmlsc.LoadObject("PrincipalEdge");
			m_role = (WhichVertex)xmlsc.LoadObject("Role");
			m_roleIsKnown = (bool)xmlsc.LoadObject("RoleIsKnown");
			m_synchronizer = (VertexSynchronizer)xmlsc.LoadObject("Synchronizer");
			m_triggerDelegate = (TriggerDelegate)xmlsc.LoadObject("TriggerDelegate");
//			Trace.WriteLine("Deserializing " + m_name + " : it has " + m_postEdges.Count + " post edges in object w/ hashcode " 
//				+ m_postEdges.GetHashCode() + ". (BTW, this has hashcode " + this.GetHashCode() + ").");
		}

		#endregion

		/// <summary>
		/// Returns the name of this vertex.
		/// </summary>
		/// <returns>The name of this vertex.</returns>
		public override string ToString(){
			return m_name;
		}
		
		#region IHasValidity Members

		private Validity.ValidationService m_vm = null;
		public Validity.ValidationService ValidationService { get { return m_vm; } set { m_vm = value; } }

		public Validity.Validity SelfState { get { return Validity.Validity.Valid; } set { } }

		public void NotifyOverallValidityChange(Validity.Validity newValidity){
			//Console.WriteLine(Name + " is becoming " + newValidity);

			//if ( ValidityChangeEvent != null ) ValidityChangeEvent(this,newValidity);
		}

		public event Validity.ValidityChangeHandler ValidityChangeEvent{ add {} remove {} }

		public IList GetChildren() { return _emptyCollection; }

		public IList GetSuccessors() {
			ArrayList retval = new ArrayList(PostEdges);
			if ( m_synchronizer != null ) {
				bool vrtxComesAfterMe = false;
				foreach ( Vertex v in m_synchronizer.Members ) {
					if ( vrtxComesAfterMe ) retval.Add(v);
					if ( Equals(v) ) vrtxComesAfterMe = true;
				}
			}
			return retval;
		}

		public Validity.IHasValidity GetParent(){ return null; }

		#endregion

		#region IPartOfGraphStructure Members

		public event StructureChangeHandler StructureChangeHandler;

//		public void PropagateStructureChange(object obj, StructureChangeType sct, bool isPropagated){
//			if ( StructureChangeHandler != null ) StructureChangeHandler(obj,sct,isPropagated);
//		}
		#endregion
	}

	[TaskGraphVolatile]
	internal class PreEdgesSatisfiedKey {}

	/// a synchronization primitive between two or more vertices, where the vertices
	/// are, once all are able to fire, fired in the order specified in the 'vertices'
	/// array.
    public class VertexSynchronizer : IXmlPersistable {

		public static void Synchronize(IExecutive exec, params Vertex[] vertices){
			ArrayList verticesToSynchronize = new ArrayList();
			foreach ( Vertex vtx in vertices){
				if ( vtx.Synchronizer != null ) {
					foreach ( Vertex vtx2 in vtx.Synchronizer.Members) {
						if ( !verticesToSynchronize.Contains(vtx2) ) verticesToSynchronize.Add(vtx2);
					}
					vtx.Synchronizer.Destroy();
				} else {
					if ( !verticesToSynchronize.Contains(vtx) ) verticesToSynchronize.Add(vtx);
				}
			}

			Vertex[] allVertices = (Vertex[])verticesToSynchronize.ToArray(typeof(Vertex));
			VertexSynchronizer vs = new VertexSynchronizer(exec, allVertices, ExecEventType.Detachable);

		}

		private Vertex[] m_vertices; // This one is contained in the synchronizer
		private IExecutive m_exec;
		private ExecEventType m_eventType;

		/// <summary>
		/// Creates a synchronization between two or more vertices, where the vertices
		/// are, once all are able to fire, fired in the order specified in the 'vertices'
		/// array.
		/// </summary>
		/// <param name="exec">The executive in whose simulation this VS is currently running.</param>
		/// <param name="vertices">An array of vertices to be synchronized.</param>
		/// <param name="vertexFiringType">The type of ExecEvent that successor edges to this vertex
		/// should be called with.</param>
        public VertexSynchronizer(IExecutive exec, Vertex[] vertices, ExecEventType vertexFiringType){

            m_vertices = vertices;
			m_exec = exec;
			m_eventType = vertexFiringType;
			//Trace.Write("CREATING SYNCHRONIZER WITH TASKS ( " );

			foreach ( Vertex vertex in m_vertices ) {
				if ( vertex.PrincipalEdge is Tasks.Task ) ((Tasks.Task)vertex.PrincipalEdge).SelfValidState = false;
			}

            foreach ( Vertex vertex in m_vertices ) {
				//Trace.Write(vertex.Name + ", ");
                if ( vertex.Role.Equals(Vertex.WhichVertex.Post) ){
                    throw new ApplicationException("Cannot synchronize postVertices at this time.");
                } else {
                    vertex.SetSynchronizer(this);
                }
            }
			//Trace.WriteLine(" )");
        }

		/// <summary>
		/// Removes this synchronizer from all vertices that it was synchronizing.
		/// </summary>
		public void Destroy(){
			foreach ( Vertex vertex in m_vertices ) {
				vertex.SetSynchronizer(null);
			}
			m_vertices = new Vertex[]{};
		}

        internal void NotifySatisfied(Vertex vertex, IDictionary graphContext){

			#region // HACK: This leaves orphaned Synchronizers hanging off the graph.
			ArrayList newVertices = new ArrayList();
			foreach ( Vertex _vertex in m_vertices ) {
				if ( _vertex.PrincipalEdge.GetParent() != null ) {
					newVertices.Add(_vertex);
				}
			}
			m_vertices = (Vertex[])newVertices.ToArray(typeof(Vertex));
			#endregion

			ArrayList satisfiedVertices = (ArrayList)graphContext[m_vsKey];
            if ( satisfiedVertices == null ) {
                satisfiedVertices = new ArrayList(m_vertices.Length);
                graphContext.Add(m_vsKey,satisfiedVertices);
            }

            foreach ( Vertex _vertex in m_vertices ) {
                if ( vertex.Equals(_vertex) && !satisfiedVertices.Contains(vertex) ) {
                    satisfiedVertices.Add(vertex);
                }
            }

            if ( satisfiedVertices.Count == m_vertices.Length ) {
				foreach ( Vertex _vertex in m_vertices ) {
					// We need to fire these both at the same time and priorities, but to
					// queue them up as separate simultaneous events. This is because a vertex
					// that relies on activity on a second, simultaneous vertex's handler, will
					// need to yield to that vertex, which means that vertex will need to be on
					// a separate thread - hence we fire them as separate events.
					m_exec.RequestEvent(new ExecEventReceiver(_vertex._AsyncFireVertexHandler),m_exec.Now,m_exec.CurrentPriorityLevel,graphContext,m_eventType); 
					//_vertex._FireVertex(graphContext);
				}
                graphContext.Remove(m_vsKey);
            }
        }

		/// <summary>
		/// A sequenced array of the member vertices of this synchronizer.
		/// </summary>
		public Vertex[] Members { get { return m_vertices; } }

        private object m_vsKey = new VolatileKey();

		#region IXmlPersistable Members
		public VertexSynchronizer(){}
		public virtual void SerializeTo(XmlSerializationContext xmlsc) {
			xmlsc.StoreObject("EventType",m_eventType);
			xmlsc.StoreObject("VertexCount",m_vertices.Length);
			for ( int i = 0 ; i < m_vertices.Length ; i++ ) {
				xmlsc.StoreObject("Vertex_"+i,m_vertices[i]);
			}
			// Skipping m_vsKey & m_executive.
		}

		public virtual void DeserializeFrom(XmlSerializationContext xmlsc) {
			// TODO:  Add Vertex.DeserializeFrom implementation
			m_exec = ((Model)xmlsc.ContextEntities["Model"]).Executive;
			m_eventType = (ExecEventType)xmlsc.LoadObject("EventType");
			int vertexCount = (int)xmlsc.LoadObject("VertexCount");
			m_vertices  = new Vertex[vertexCount];
			//for ( int i = 0 ; i < vertexCount ; i++ ) {
				throw new NotImplementedException("Vertex deserialization not yet implemented in VertexSynchronizers.");
			//}
		}

		#endregion
	}
    

	public class SynchronizerSorter : IComparer {
		#region IComparer Members

		public int Compare(object x, object y) {
			VertexSynchronizer vsx = (VertexSynchronizer)x;
			VertexSynchronizer vsy = (VertexSynchronizer)y;

			System.Text.StringBuilder sbx = new System.Text.StringBuilder();
			System.Text.StringBuilder sby = new System.Text.StringBuilder();

			foreach ( Vertex vx in vsx.Members ) sbx.Append(vx.Name);
			foreach ( Vertex vy in vsy.Members ) sby.Append(vy.Name);

			return Comparer.Default.Compare(sbx.ToString(),sby.ToString());
		}

		#endregion

	}

}
