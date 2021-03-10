/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore; // For IExecutive and IDetachableEventController, used in Joining & Yielding.
using Highpoint.Sage.Persistence;
using Highpoint.Sage.Utility.Mementos;

namespace Highpoint.Sage.Graphs {

    /// <summary>
    /// An EdgeExecutionSignaler is called by the code in an edge's application code to signify that it has completed execution.
    /// </summary>
    /// <param name="graphContext">The graph context in which the execution is occurring.</param>
	public delegate void EdgeExecutionCompletionSignaler(IDictionary graphContext);
    /// <summary>
    /// Implemented by a method that is to contain application code. It is called by the graph when the edge's turn comes
    /// to execute application code.
    /// </summary>
    /// <param name="graphContext">The graph context in which execution is occurring.</param>
    /// <param name="theEdge">The edge on which execution is to occur.</param>
    /// <param name="ecs">The EdgeExecutionCompletionSignaler to call once this execution is complete.</param>
	public delegate void EdgeExecutionDelegate(IDictionary graphContext, Edge theEdge, EdgeExecutionCompletionSignaler ecs);
	/// <summary>
	/// An event that pertains to an edge irrespective of the execution (graph) context, therefore usually referring to
    /// a structural occurrence.
	/// </summary>
	/// <param name="theEdge">The edge to which the event pertains.</param>
    public delegate void StaticEdgeEvent(Edge theEdge);
    /// <summary>
    /// An event that pertains to an edge within an execution (graph) context, therefore usually referring to
    /// a dynamic event such as commencement or completion of an edge.
    /// </summary>
    /// <param name="graphContext">The graph context in which execution is occurring.</param>
    /// <param name="theEdge">The edge to which the event pertains.</param>
	public delegate void EdgeEvent(IDictionary graphContext, Edge theEdge);
	
    /// <summary>
	/// Implemented by any edge that modifies the state of its graphContext.
	/// </summary>
	public interface IStatefulEdge {
        /// <summary>
        /// Gets the state of the implementing object immediately prior to execution within the provided context.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        /// <returns>The state of the implementing object immediately prior to execution.</returns>
		IMemento GetPreState(IDictionary graphContext);
        /// <summary>
        /// Gets the state of the implementing object immediately following execution within the provided context.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        /// <returns>The state of the implementing object immediately following execution.</returns>
		IMemento GetPostState(IDictionary graphContext);
	}

    /// <summary>
    /// Implemented by an object that is a participant in a directed graph. Edges may be hierarchical, meaning that an edge
    /// may have child edges that are executed as a part of its own execution.
    /// </summary>
	public interface IEdge : SimCore.ICloneable, IVisitable, IXmlPersistable, IPartOfGraphStructure, IHasName {
        /// <summary>
        /// Gets the pre vertex of the object.
        /// </summary>
        /// <value>The pre vertex.</value>
		Vertex PreVertex { get; }
        /// <summary>
        /// Gets the post vertex of the object.
        /// </summary>
        /// <value>The post vertex.</value>
		Vertex PostVertex { get; }
        /// <summary>
        /// Gets the parent edge to this one. If the graph is not hierarchical, this will be null.
        /// </summary>
        /// <returns></returns>
		IEdge GetParent();
        /// <summary>
        /// Gets the child edges of this one. No sequence is implied in this collection - child edges are executed
        /// in an order according to their vertices' relationships to each other and their parents.
        /// </summary>
        /// <value>The child edges.</value>
		IList ChildEdges { get; }
        /// <summary>
        /// Gets or sets the channel with which this edge is associated. This identifies an edge as a part of a group
        /// of edges that are to be fired together by a <see cref="T:IEdgeFiringManager"/> when a preVertex is satisfied.
        /// As an example, a vertex that had two outbound edges, a forward and a loopback, would have an <see cref="T:IEdgeFiringManager"/>
        /// attached to it that knew that after a call to its Start(...) method, it was to fire the edge associated with
        /// its loopback channel a certain number of times, followed by firing the edges associated with its forward edge
        /// once the loopback count had been reached.
        /// </summary>
        /// <value>The channel.</value>
		object Channel { get; set; }
	}

	/// <summary>
	/// An edge in a graph is an executional path between two vertices. An edge will have a preVertex
	/// and a postVertex, and entail some (possibly zero) duration and procedural implementation between
	/// the satisfaction of its preVertex and that of its postVertex.
	/// </summary>
	public class Edge : IEdge, Validity.IHasValidity { //Highpoint.Sage.SimCore.ICloneable, IVisitable, IXmlPersistable {

        #region Private Fields
        
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Edge");
        private static readonly bool s_managePostMortemData = Diagnostics.DiagnosticAids.Diagnostics("Graph.KeepPostMortems");
        private ArrayList m_childEdges = null;      // My children - all of them.
        private ArrayList m_childLigatures = null;  // Ligatures that connect me, the task, to my children.
        private static ArrayList _emptyCollection = ArrayList.ReadOnly(new ArrayList());
        private EdgeExecutionCompletionSignaler m_eecs;
        private object m_channel;
        private ArrayList m_activeContexts = null;
        private StaticEdgeEvent m_onChildGainedPredecessorHandler;
        private StaticEdgeEvent m_onChildGainedSuccessorHandler;
        private StaticEdgeEvent m_onChildLostPredecessorHandler;
        private StaticEdgeEvent m_onChildLostSuccessorHandler;

        /// <summary>A description of this edge</summary>
		protected string m_description = null;

        private EdgeExecutionDelegate m_myExecutionDelegate;

		private int m_cloneNumber = 0; 

		private object m_ref = null;

		private Validity.ValidationService m_vm = null;
		private IList m_successorList = null;

        #endregion 

        /// <summary>
        /// This edge's pre-vertex.
        /// </summary>
		protected Vertex Pre = null;
        /// <summary>
        /// This edge's post-vertex.
        /// </summary>
		protected Vertex Post = null;
        /// <summary>
        /// This edge's Name.
        /// </summary>
		protected string m_name = null;
        /// <summary>
        /// This edge's parent edge.
        /// </summary>
		protected Edge ParentEdge = null;

        /// <summary>
        /// An EdgeFiringManager that is told to fire all edges that are marked with a NullChannelMarker will
        /// actually fire all edges that have no Channel marker - that is, they have a \"null\" channel marker.
        /// </summary>
		public static readonly string NULL_CHANNEL_MARKER = "NullChannelMarker";
		
        /// <summary>
        /// Fired after this edge is cloned.
        /// </summary>
        public event CloneHandler CloneEvent;
		/// <summary>
        /// Fired after an edge has been notified that it may start, and immediately prior to calling the <see cref="EdgeExecutionDelegate"/> which contains the application code.
		/// </summary>
        public event EdgeEvent EdgeExecutionStartingEvent;
        /// <summary>
        /// Called as soon as the application code in the <see cref="EdgeExecutionDelegate"/> has finished.
        /// </summary>
		public event EdgeEvent EdgeExecutionFinishingEvent;
		/// <summary>
		/// Called as an edge's pre-vertex is starting to fire.
		/// </summary>
        public event EdgeEvent EdgeStartingEvent;
        /// <summary>
        /// Called as an edge's post-vertex is starting to fire.
        /// </summary>
		public event EdgeEvent EdgeFinishingEvent;

        #if DEBUG
		string[] m_breakpointEvents = new string[]{};//{"GetVat","Charge 1"};
        #endif

        /// <summary>
        /// Creates a new instance of the <see cref="T:Edge"/> class. This implementation is provided in support of serialization.
        /// </summary>
		public Edge():this((string)null){}

        /// <summary>
        /// Creates a new instance of the <see cref="T:Edge"/> class with a given name.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
		public Edge(string name){
			m_name = (name==null?ToString():name);
			CreateVertices();
			m_eecs = new EdgeExecutionCompletionSignaler(OnExecutionComplete);
			m_channel = NULL_CHANNEL_MARKER;
			m_activeContexts = new ArrayList();

			m_onChildGainedPredecessorHandler = new StaticEdgeEvent(OnChildGainedPredecessor);
			m_onChildGainedSuccessorHandler   = new StaticEdgeEvent(OnChildGainedSuccessor);
			m_onChildLostPredecessorHandler   = new StaticEdgeEvent(OnChildLostPredecessor);
			m_onChildLostSuccessorHandler     = new StaticEdgeEvent(OnChildLostSuccessor);

			InitializeStructuralChangeHandlers();
		}

        /// <summary>
        /// Initializes the structural change handlers -  GainedPredecessorEvent, GainedSuccessorEvent, LostPredecessorEvent, and LostSuccessorEvent.
        /// </summary>
		protected virtual void InitializeStructuralChangeHandlers(){
			GainedPredecessorEvent +=new StaticEdgeEvent(Edge_GainedPredecessorEvent);
			GainedSuccessorEvent   +=new StaticEdgeEvent(Edge_GainedSuccessorEvent);
			LostPredecessorEvent   +=new StaticEdgeEvent(Edge_LostPredecessorEvent);
			LostSuccessorEvent     +=new StaticEdgeEvent(Edge_LostSuccessorEvent);
		}

        /// <summary>
        /// Creates the pre and post vertices for this edge, providing them with default names
        /// and connecting them to this edge.
        /// </summary>
		protected virtual void CreateVertices(){
			// This tests for the preexistence of the vertices since, if we are deserializing,
			// a given vertex may already exist from a different reference. In that case, even
			// though the default constructor is called, we don't want to overwrite the pre-
			// or post-vertex.
			if ( Pre == null ) {
				Pre = new Vertex(this, m_name + ":Pre");
				Pre.AddPostEdge(this);
				Pre.BeforeVertexFiringEvent+=new VertexEvent(OnPreVertexStartingToFire);
			}
			if ( Post == null ) {
				Post = new Vertex(this, m_name + ":Post");
				Post.AddPreEdge(this);
				Post.BeforeVertexFiringEvent+=new VertexEvent(OnPostVertexStartingToFire);
			}
		}

        /// <summary>
        /// Fired after an edge has gained a predecessor.
        /// </summary>
		public event StaticEdgeEvent GainedPredecessorEvent {
			add { Pre.PreEdgeAddedEvent+=value; } remove { Pre.PreEdgeAddedEvent-=value; }
		}
        /// <summary>
        /// Fired after an edge has lost a predecessor.
        /// </summary>
        public event StaticEdgeEvent LostPredecessorEvent {
			add { Pre.PreEdgeRemovedEvent+=value; } remove { Pre.PreEdgeRemovedEvent-=value; }
		}
        /// <summary>
        /// Fired after an edge has gained a successor.
        /// </summary>
        public event StaticEdgeEvent GainedSuccessorEvent {
			add { Post.PostEdgeAddedEvent+=value; } remove { Post.PostEdgeAddedEvent-=value; }
		}
        /// <summary>
        /// Fired after an edge has lost a successor.
        /// </summary>
        public event StaticEdgeEvent LostSuccessorEvent {
			add { Post.PostEdgeRemovedEvent+=value; } remove { Post.PostEdgeRemovedEvent-=value; }
		}

		/// <summary>
		/// The preVertex to this edge.
		/// </summary>
		public Vertex PreVertex { get { return Pre; } }

		/// <summary>
		/// The postVertes to this edge.
		/// </summary>
		public Vertex PostVertex { get{ return Post; } }

		/// <summary>
		/// An edge's channel is used by a vertex's branch manager to determine which 
		/// successor edges are to fire when the vertex's predecessors have all fired.
		/// The channel can be null, if there is no branch manager, or if the provided
        /// branch manager allows it.
		/// </summary>
		public object Channel { 
			get { return m_channel; } 
			set { 
				if ( value != null ) {
					m_channel = value; 
				} else {
					m_channel = NULL_CHANNEL_MARKER;
				}
			} 
		}

		/// <summary>
		/// The name of this edge.
		/// </summary>
		public string Name { [DebuggerStepThrough]get { return m_name; } }

        /// <summary>
		/// A description of this Edge.
		/// </summary>
		public string Description {
			get { return m_description ?? m_name; }
		}

        /// <summary>
        /// Creates a <see cref="T:Edge.Ligature"/> between the provided edge's postVertex and this one's PreVertex,
        /// making the provided edge a predecessor to this one. This API also interacts with a
        /// <see cref="T:Highpoint.Sage.Graphs.Validity.ValidationService"/> to enable it to correctly
        /// manage graph validity state.
        /// </summary>
        /// <param name="preEdge">The pre edge.</param>
		public virtual void AddPredecessor(Edge preEdge){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			AddLigature(preEdge.PostVertex,PreVertex);
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Creates a <see cref="T:Edge.Ligature"/> between the provided edge's preVertex and this one's postVertex,
        /// making the provided edge a successor to this one. This API also interacts with a
        /// <see cref="T:Highpoint.Sage.Graphs.Validity.ValidationService"/> to enable it to correctly
        /// manage graph validity state.
        /// </summary>
        /// <param name="postEdge">The post edge.</param>
		public virtual void AddSuccessor(Edge postEdge){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			AddLigature(PostVertex,postEdge.PreVertex);
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Either removes a <see cref="T:Ligature"/> between the provided edge's postVertex and this one's PreVertex,
        /// removing the provided edge as a predecessor to this one. If the provided edge is a <see cref="T:Ligature"/>, then
        /// the ligature itself is disconnected from this edge. This API also interacts with a
        /// <see cref="T:Highpoint.Sage.Graphs.Validity.ValidationService"/> to enable it to correctly
        /// manage graph validity state.
        /// </summary>
        /// <param name="preEdge">The pre edge.</param>
		public virtual void RemovePredecessor(Edge preEdge){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( preEdge is Ligature ) {
				preEdge.Disconnect();
			} else {
				RemoveLigature(preEdge,this);
			}
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Either removes a <see cref="T:Ligature"/> between the provided edge's preVertex and this one's PostVertex,
        /// removing the provided edge as a successor to this one. If the provided edge is a <see cref="T:Ligature"/>, then
        /// the ligature itself is disconnected from this edge. This API also interacts with a
        /// <see cref="T:Highpoint.Sage.Graphs.Validity.ValidationService"/> to enable it to correctly
        /// manage graph validity state.
        /// </summary>
        /// <param name="postEdge">The post edge.</param>
		public virtual void RemoveSuccessor(Edge postEdge){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( postEdge is Ligature ) {
				postEdge.Disconnect();
			} else {
				RemoveLigature(this,postEdge);
			}
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Inserts this edge between the two provided edges. This is done by calling <see cref="T:Edge#AddSuccessor"/> for
        /// this edge on the preEdge, and <see cref="T:Edge#AddPredecessor"/> for this edge on the postEdge.
        /// </summary>
        /// <param name="preEdge">The edge that is to be this edge's predecessor.</param>
        /// <param name="postEdge">The edge that is to be this edge's successor.</param>
		public void InsertBetween(Edge preEdge, Edge postEdge){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			preEdge.AddSuccessor(this);
			postEdge.AddPredecessor(this);
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Disconnects this instance from any parent edges, predecessors and successors. Child edges are left
        /// attached.
        /// </summary>
		public virtual void Disconnect(){

			if ( ParentEdge != null ) {
				bool hasVm = (m_vm != null);
				if ( hasVm ) m_vm.Suspend();
				ParentEdge.RemoveChildEdge(this);
				if ( hasVm ) m_vm.Resume();
			}

			// If we were to iterate through the Edges, the removal of the predecessor
			// reference would result in changing the SuccessorEdges array. This would
			// cause an error, so we have to copy the collection and then iterate through
			// the copy.
			ArrayList tmp = new ArrayList(PredecessorEdges);
			foreach ( Edge e in tmp ) {
				if ( ! ( e is Ligature ) ) throw new ApplicationException("Non-ligature where a ligature was expected!!!");
				bool hasVm = (m_vm != null);
				if ( hasVm ) m_vm.Suspend();
				e.Disconnect();
				if ( hasVm ) m_vm.Resume();
			}

			tmp = new ArrayList(SuccessorEdges);
			foreach ( Edge e in tmp ) {
				if ( ! ( e is Ligature ) ) throw new ApplicationException("Non-ligature where a ligature was expected!!!");
				bool hasVm = (m_vm != null);
				if ( hasVm ) m_vm.Suspend();
				e.Disconnect();
				if ( hasVm ) m_vm.Resume();
			}
		}


		/// <summary>
        /// Adds the slave Edge as a CoStart. A CoStart is an edge that is allowed to start as soon
        /// as this edge has started. Thus, the slave edge's prevertex will not be allowed to fire until 
        /// this edge's prevertex has fired.
		/// </summary>
		/// <param name="slaveEdge">The slave edge.</param>
		/// <returns>The ligature that was added between this edge's preVertex and the slave edge's preVertex.</returns>
		public Edge AddCostart(Edge slaveEdge){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			Edge retval = AddLigature(PreVertex,slaveEdge.PreVertex);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddCostart,false);
			if ( hasVm ) m_vm.Resume();
			return retval;
		}

        /// <summary>
        /// Removes a costart relationship between this edge and the provided slave edge, if such exists.
        /// </summary>
        /// <param name="slaveEdge">The slave edge.</param>
		public void RemoveCostart(Edge slaveEdge){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			RemoveLigature(PreVertex,slaveEdge.PreVertex);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.RemoveCostart,false);
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Adds the slave Edge as a CoFinish. A CoFinish exists when a master edge's postVertex is not permitted to fire
        /// until the slave edge has completed. Thus, the master edge's postvertex will not be allowed to fire until 
        /// the slave edge's postvertex has fired.
        /// </summary>
        /// <param name="slaveEdge">The slave edge.</param>
        /// <returns>The ligature that was added between this edge's postVertex and the slave edge's postVertex.</returns>
        public Edge AddCofinish(Edge slaveEdge) {
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			Edge retval = AddLigature(PostVertex,slaveEdge.PostVertex);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddCofinish,false);
			if ( hasVm ) m_vm.Resume();
			return retval;
		}

        /// <summary>
        /// Removes a cofinish relationship between this edge and the provided slave edge, if such exists.
        /// </summary>
        /// <param name="slaveEdge">The slave edge.</param>
        public void RemoveCofinish(Edge slaveEdge) {
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			RemoveLigature(PostVertex,slaveEdge.PostVertex);
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.RemoveCofinish,false);
			if ( hasVm ) m_vm.Resume();
		}

#if NOT_IMPLEMENTED
        //public Edge AddHandoff(Edge hadIt, Edge gotIt){
        //    bool hasVM = (m_vm != null);
        //    if ( hasVM ) m_vm.Suspend();
        //    Edge retval = AddLigature(gotIt.PreVertex,hadIt.PostVertex);
        //    if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.Unknown,false);
        //    if ( hasVM ) m_vm.Resume();
        //    return retval;
        //}

        //public void RemoveHandoff(Edge hadIt, Edge gotIt){
        //    bool hasVM = (m_vm != null);
        //    if ( hasVM ) m_vm.Suspend();
        //    RemoveLigature(gotIt.PreVertex,hadIt.PostVertex);
        //    if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.Unknown,false);
        //    if ( hasVM ) m_vm.Resume();
        //}
#endif

        /// <summary>
        /// Gets a list of predecessor edges attached to this edge's preVertex.
        /// </summary>
        /// <value>The predecessor edges.</value>
		public IList PredecessorEdges {
			get {
				return Pre.PredecessorEdges;
			}
		}

        /// <summary>
        /// Gets a list of successor edges attached to this edge's postVertex.
        /// </summary>
        /// <value>The successor edges.</value>
		public IList SuccessorEdges{
			get {
				return Post.SuccessorEdges;
			}
		}

        /// <summary>
        /// Begins execution of the graph under this edge using a default GraphContext.
        /// <see cref="T:Edge.Start"/>
        /// </summary>
		public void Start(){
			Start(new Hashtable());
		}

        /// <summary>
        /// Begins execution of the graph under the specified graph context.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
		public void Start(IDictionary graphContext){
			PreVertex.FireVertex(graphContext);
		}

        /// <summary>
        /// Gets the parent edge to this one. If the graph is not hierarchical, this will be null.
        /// </summary>
        /// <returns></returns>
		public IEdge GetParent(){
			return ParentEdge;
		}

        /// <summary>
        /// Gets or sets the parent edge to this one. If the graph is not hierarchical, this will be null.
        /// </summary>
        /// <value>The parent.</value>
		protected Edge Parent { 
			get { return ParentEdge; }
			set {
				if ( value != null ) {
					ParentEdge = value;
					ParentEdge.AddChildEdge(this);
				} else {
					ParentEdge.RemoveChildEdge(this);
					ParentEdge = null;
				}
			}
		}

        /// <summary>
        /// Gets the child edges of this one. No sequence is implied in this collection - child edges are executed
        /// in an order according to their vertices' relationships to each other and their parents.
        /// </summary>
        /// <value>The child edges.</value>
		public IList ChildEdges {
			get {
				if ( m_childEdges != null ) return ArrayList.ReadOnly(m_childEdges);
				else return _emptyCollection;
			}
		}

		/// <summary>
		/// This method takes a list of edges, and first creates a chain out of them, and then
		/// adds that chain as a set of child tasks. Note that a restriction is that the edge to
		/// which these edges are being added cannot already have children assigned to it.
		/// 
		/// </summary>
		/// <param name="listOfEdges">An IList containing a group of edges that are to be added as a sequential set of children to this edge.</param>
		public virtual void AddChainOfChildren(IList listOfEdges){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			for ( int i = 1 ; i < listOfEdges.Count ; i++ ) {
				Edge e0 = (Edge)listOfEdges[i-1];
				Edge e1 = (Edge)listOfEdges[i];
				e0.AddSuccessor(e1);
			}
			AddChildEdges(listOfEdges);
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Adds the list of child edges as children to this edge. They are treated as equals, all attached at their pre-vertices
        /// to this one's pre-vertex, and at their post-vertices to this one's post-vertex. Any further sequencing between them is
        /// governed by otherwise-defined ligatures, synchronizers, etc.
        /// </summary>
        /// <param name="edges">The edges.</param>
		public virtual void AddChildEdges( IList edges ){
			if ( m_childEdges != null ) throw new ApplicationException("You are adding children to an edge that already has children. This is not yet supported.");
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			m_childEdges = new ArrayList(edges.Count);
			m_childLigatures = new ArrayList();
			foreach ( Edge edge in edges ) {
				edge.Parent = this;
				if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddChildEdge,false);
			}
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Adds the child edge as a child to this edge. It will be attached at its pre-vertex
        /// to this one's pre-vertex, and at its post-vertex to this one's post-vertex. Any further
        /// sequencing between the provided edge and other edges is governed by otherwise-defined
        /// ligatures, synchronizers, etc.
        /// </summary>
        /// <param name="child">The child.</param>
		public virtual void AddChildEdge(Edge child){
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( m_childEdges == null ) m_childEdges = new ArrayList();
			if ( m_childLigatures == null ) m_childLigatures = new ArrayList();
			m_childEdges.Add(child);
			child.ParentEdge = this;
			child.GainedPredecessorEvent += m_onChildGainedPredecessorHandler;
			child.GainedSuccessorEvent   += m_onChildGainedSuccessorHandler;
			child.LostPredecessorEvent   += m_onChildLostPredecessorHandler;
			child.LostSuccessorEvent     += m_onChildLostSuccessorHandler;
			if ( child.PredecessorEdges.Count == 0 ) m_childLigatures.Add(AddCostart(child));
			// Was, until 1/25/2004 : if ( child.SuccessorEdges.Count == 0 )   m_childLigatures.Add(AddCofinish(child));
			if ( child.SuccessorEdges.Count == 0 )   m_childLigatures.Add(child.AddCofinish(this));
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddChildEdge,false);
			if ( hasVm ) m_vm.Resume();
		}

        /// <summary>
        /// Removes the child edges, and the ligatures that establish them as children (i.e. between the parent's pre-vertex
        /// and their pre-vertices, and the parent's post-vertex and their post-vertices.)
        /// </summary>
        /// <returns>A list of the edges that were removed as children.</returns>
		public virtual IList RemoveChildEdges(){
			if ( m_childLigatures == null ) return null;

			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			ArrayList childEdgeBuffer = new ArrayList(m_childEdges);
			ArrayList childLigBuffer  = new ArrayList(m_childLigatures);

			foreach ( Edge child in childEdgeBuffer ) {
				child.ParentEdge = null;
				child.GainedPredecessorEvent -= m_onChildGainedPredecessorHandler;
				child.GainedSuccessorEvent   -= m_onChildGainedSuccessorHandler;
				child.LostPredecessorEvent   -= m_onChildLostPredecessorHandler;
				child.LostSuccessorEvent     -= m_onChildLostSuccessorHandler;
			}

			foreach ( Ligature lig in childLigBuffer ) lig.Disconnect();

			m_childEdges = null;
			m_childLigatures = null;
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddChildEdge,false);
			if ( hasVm ) m_vm.Resume();
			return childEdgeBuffer;
		}

        /// <summary>
        /// Removes the child edge, and the ligatures that establish it as a child (i.e. between the parent's pre-vertex
        /// and this one's pre-vertex, and the parent's post-vertex and this one's post-vertex.)
        /// </summary>
        /// <param name="child">The child edge that is to be removed.</param>
        /// <returns>
        /// True if the removal was successful. False if the provided edge was not a child to this edge.
        /// </returns>
        public virtual bool RemoveChildEdge(Edge child) {
			if ( !m_childEdges.Contains(child) ) return false;
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			m_childEdges.Remove(child);
			child.ParentEdge = null;
			child.GainedPredecessorEvent -= m_onChildGainedPredecessorHandler;
			child.GainedSuccessorEvent   -= m_onChildGainedSuccessorHandler;
			child.LostPredecessorEvent   -= m_onChildLostPredecessorHandler;
			child.LostSuccessorEvent     -= m_onChildLostSuccessorHandler;


			ArrayList ligaturesToDisconnect = new ArrayList();
			foreach ( Ligature childLigature in m_childLigatures ) {
				if ( childLigature.PreVertex.PrincipalEdge.Equals(child) ||
					childLigature.PostVertex.PrincipalEdge.Equals(child)){
					ligaturesToDisconnect.Add(childLigature);
				}
			}
			foreach ( Ligature childLigature in ligaturesToDisconnect ) {
				m_childLigatures.Remove(childLigature);
				childLigature.Disconnect();
			}
			if ( StructureChangeHandler != null ) StructureChangeHandler(this,StructureChangeType.AddChildEdge,false);
				
			
			if ( hasVm ) m_vm.Resume();
			return true;
		}

		private void OnChildLostPredecessor(Edge child){
			// If there are no longer any predecessors, add a CoStart.
			// Add a child Ligature.
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( child.PredecessorEdges.Count == 0 ) m_childLigatures.Add(AddCostart(child));
			if ( hasVm ) m_vm.Resume();
		}

		private void OnChildLostSuccessor(Edge child){
			// Add a child Ligature.
			// Was, until 1/25/2004 : if ( child.SuccessorEdges.Count == 0 ) m_childLigatures.Add(AddCofinish(child));
			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			if ( child.SuccessorEdges.Count == 0 ) m_childLigatures.Add(child.AddCofinish(this));
			if ( hasVm ) m_vm.Resume();
		}

		private void OnChildGainedPredecessor(Edge child){
			// If the child has only one predecessor and it's the ligature to parent, ignore this.
			if ( (child.PredecessorEdges.Count == 1 ) &&
				((Edge)child.PredecessorEdges[0]).PreVertex.PrincipalEdge.Equals(this)) return;

			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();
			// Find the ligature-to-this-child
			Ligature lttc = null;
			foreach ( Ligature childLigature in m_childLigatures ) {
				if ( childLigature.PostVertex.PrincipalEdge.Equals(child)) lttc = childLigature;
			}

			if ( lttc == null ) {
				if ( hasVm ) m_vm.Resume();
				return;
			}

			// If any predecessor of the child's preVertex is an edge whose parent is me,
			// then I will delete my childLigature that points to the child.
			foreach ( Edge childPred in PredecessorEdges ) {
				Edge edgePred = childPred;
				while ( edgePred is Ligature ) edgePred = edgePred.PreVertex.PrincipalEdge;
				if ( edgePred.Parent == this ) {
					m_childLigatures.Remove(lttc);
					lttc.Disconnect();
					break;
				}
			}
			if ( hasVm ) m_vm.Resume();
		}

		private void OnChildGainedSuccessor(Edge child){
			// If the child has only one successor and it's the ligature to parent, ignore this.
			if ( (child.SuccessorEdges.Count == 1 ) &&
				((Edge)child.SuccessorEdges[0]).PostVertex.PrincipalEdge.Equals(this)) return;

			bool hasVm = (m_vm != null);
			if ( hasVm ) m_vm.Suspend();

			// Find the ligature-to-this-child
			Ligature lttc = null;
			foreach ( Ligature childLigature in m_childLigatures ) {
				if ( childLigature.PreVertex.PrincipalEdge.Equals(child)) lttc = childLigature;
			}

			if ( lttc != null ) { // There's a ligature to this child.

				// If any successor of the child's postVertex is an edge whose parent is me,
				// then I will delete my childLigature from that child.
				foreach ( Edge childSucc in SuccessorEdges ) {
					Edge edgeSucc = childSucc;
					while ( edgeSucc is Ligature ) edgeSucc = edgeSucc.PostVertex.PrincipalEdge;
					if ( edgeSucc.Parent == this ) {
						m_childLigatures.Remove(lttc);
						lttc.Disconnect();
						break;
					}
				}
			}

			if ( hasVm ) m_vm.Resume();

		}

		private void OnPreVertexStartingToFire(IDictionary graphContext, Vertex vertex){
			if ( EdgeStartingEvent != null ) EdgeStartingEvent(graphContext,this);
		}

		private void OnPostVertexStartingToFire(IDictionary graphContext, Vertex vertex){
			if ( EdgeFinishingEvent != null ) EdgeFinishingEvent(graphContext,this);
		}

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Edge"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Edge"></see>.
        /// </returns>
		public override string ToString(){
			if ( m_name == null ) {
				return GetType() + ":" + GetHashCode();
			} else {
				return m_name;
			}
		}

        /// <summary>
        /// Gets or sets the execution delegate that this edge uses to call application code.
        /// </summary>
        /// <value>The execution delegate.</value>
        public EdgeExecutionDelegate ExecutionDelegate { get { return m_myExecutionDelegate; } set { m_myExecutionDelegate = value; } }

        /// <summary>
        /// Determines whether the specified graph context is running.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        /// <returns>
        /// 	<c>true</c> if the specified graph context is running; otherwise, <c>false</c>.
        /// </returns>
		public bool IsRunning(IDictionary graphContext){
			return m_activeContexts.Contains(graphContext);
		}

        /// <summary>
        /// Called by the pre-vertex when it has been satisfied - that is, all incoming edges and
        /// synchronizers to that vertex have fired.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
		public virtual void PreVertexSatisfied(IDictionary graphContext){
			m_activeContexts.Add(graphContext);

			if ( s_diagnostics ) _Debug.WriteLine("Firing edge " + Name);

            #region Post Mortem Support
#if DEBUG
			if ( s_managePostMortemData ) {
				PmData pmData = (PmData)graphContext["PostMortemData"];
				pmData.EdgesFired.Add(this);
			}
#endif // DEBUG
            #endregion Post Mortem Support

            #region Dynamic Breakpointing - See BreakpointEvents
#if DEBUG
            bool breakHere = false;
			foreach ( string eventName in m_breakpointEvents ) {
				if ( Name.Equals(eventName) ) breakHere = true;
			}
			if ( breakHere ) Debugger.Break();
#endif
			#endregion

			if ( EdgeExecutionStartingEvent != null ) EdgeExecutionStartingEvent(graphContext,this);
			if ( s_diagnostics ) _Debug.WriteLine("Edge " + Name + " is active.");
			if ( ExecutionDelegate != null ) {
				ExecutionDelegate(graphContext, this, m_eecs);
			} else {
				m_eecs(graphContext);
			}
		}

        /// <summary>
        /// Called when execution of this edge is complete.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
		protected void OnExecutionComplete(IDictionary graphContext){
			if ( EdgeExecutionFinishingEvent != null ) EdgeExecutionFinishingEvent(graphContext,this);
             m_activeContexts.Remove(graphContext);
			if ( PostVertex != null ) {
				if ( s_diagnostics ) _Debug.WriteLine("Edge " + Name + " is signaling completion to " + PostVertex.Name);
				PostVertex.PreEdgeSatisfied(graphContext,this);
			} else {
				if ( s_diagnostics ) _Debug.WriteLine("Edge " + Name + " is signaling completion, but " + Name + " has a null postVertex.");
			}
		}

		/// <summary>
		/// Creates a direct connection between the from vertex and the to vertex, if one does not already exist.
		/// </summary>
		/// <param name="from">The vertex that is to become the preVertex of the new edge.</param>
		/// <param name="to">The vertex that is to become the postVertex of the new edge.</param>
		/// <returns>The edge that joins the two vertices.</returns>
		public static Edge Connect(Vertex from, Vertex to){
			Edge e = AddLigature(from,to);
			if ( e == null ) {
				foreach ( Edge edge in from.SuccessorEdges ) {
					if ( edge.PostVertex.Equals(to) ) e = edge;
				}
			}
			return e;
		}

        /// <summary>
        /// Removes one or more edges, if they exist, linking the fromVertex to the toVertex. The
        /// edge will be outbound from the fromVertex, and inbound to the toVertex. 
        /// </summary>
        /// <param name="from">The fromVertex.</param>
        /// <param name="to">The toVertex.</param>
        /// <param name="deleteAllSuchEdges">if set to <c>true</c> this method will delete all such edges. 
        /// If <c>false</c>, it will delete only the first one found.</param>
        public static void Disconnect(Vertex from, Vertex to, bool deleteAllSuchEdges) {
            foreach (Edge e in from.SuccessorEdges) {
                if (e.PostVertex.Equals(to)) {
                    e.PostVertex.RemovePreEdge(e);
                    e.PreVertex.RemovePostEdge(e);
                    if (!deleteAllSuchEdges)
                        return;
                }
            }
        }


        /// <summary>
        /// Adds a ligature between the from and the to vertices.
        /// </summary>
        /// <param name="from">The 'from' vertex</param>
        /// <param name="to">The 'to' vertex</param>
        /// <returns>The new ligature.</returns>
		internal static Ligature AddLigature(Vertex from, Vertex to){
			foreach ( Edge edge in from.SuccessorEdges ) {
				if ( edge.PostVertex.Equals(to) ) {
//					if ( m_diagnostics ) {
//						_Debug.WriteLine("Skipping addition of redundant ligature, " + Ligature.CreateName(from,to));
//					}
					return null;
				}
			}
			Ligature lig = new Ligature(from,to,Ligature.CreateName(from,to)); // Gets wired in as a result.
			return lig;
		}

        /// <summary>
        /// Removes the ligature between the from and the to vertices..
        /// </summary>
        /// <param name="from">The 'from' vertex</param>
        /// <param name="to">The 'to' vertex</param>
        protected static void RemoveLigature(Vertex from, Vertex to) {
			//string name = Ligature.CreateName(from,to);
			foreach ( Edge e in from.SuccessorEdges ) {
				if ( e is Ligature && e.Post.Equals(to) ) {
					((Ligature)e).Disconnect();
					break;
				}
			}
		}

        /// <summary>
        /// Removes the ligature between the post vertex of the 'from' edge and the pre-vertex of the 'to' edge.
        /// </summary>
        /// <param name="from">The 'from' edge.</param>
        /// <param name="to">The 'to' edge.</param>
        protected static void RemoveLigature(Edge from, Edge to) {
			//string name = Ligature.CreateName(from,to);
			foreach ( Edge e in from.PostVertex.SuccessorEdges ) {
				if ( e is Ligature && e.PostVertex.Equals(to.PreVertex) ) {
					((Ligature)e).Disconnect();
					break;
				}
			}
		}

        
		#region Cloning Support

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance. This method calls _PopulateClone.
        /// </returns>
		public virtual object Clone(){
			object clone = new Edge(CloneName);
			clone = _PopulateClone(clone);
			if ( CloneEvent != null ) CloneEvent(this,clone);
			return clone;
		}
        /// <summary>
        /// Populates the clone, adding clones of its children, and the edges relating them to the clone of this edge.
        /// </summary>
        /// <param name="clone">The clone of this edge.</param>
        /// <returns></returns>
		protected virtual object _PopulateClone(object clone){

			UtilRef = clone;

			ArrayList tmpKids = (ChildEdges==_emptyCollection?null:new ArrayList());
			if ( tmpKids != null ) {
				foreach ( Edge origChild in ChildEdges ) {
					Edge childClone = (Edge)origChild.Clone();
					origChild.UtilRef = childClone;
					tmpKids.Add(childClone);
				}
				((Edge)clone).PopulateForwardEdgesFromMyVertex(this,Vertex.WhichVertex.Pre);
				foreach ( Edge origChild in ChildEdges ) {
					Edge clonedChild = (Edge)origChild.UtilRef;
					clonedChild.PopulateForwardEdgesFromMyVertex(origChild,Vertex.WhichVertex.Pre);
					clonedChild.PopulateForwardEdgesFromMyVertex(origChild,Vertex.WhichVertex.Post);
				}
				foreach ( Edge edge in tmpKids ) ((Edge)(clone)).AddChildEdge(edge);
			}

			foreach ( Edge edge in ChildEdges ) edge.UtilRef = null;
			UtilRef = null;
			return clone;
		}

        /// <summary>
        /// Gets a name for the next clone to be obtained. Note that calling this method increases the index number used to create the name.
        /// </summary>
        /// <value>The name of the clone.</value>
        protected string CloneName { get { return Name + "." + (m_cloneNumber++); } }

        
		private void PopulateForwardEdgesFromMyVertex(Edge original, Vertex.WhichVertex whichVertex){
			Vertex origLigaturePre = original.GetVertex(whichVertex);
			Vertex cloneLigaturePre = GetVertex(whichVertex);
			foreach ( Edge originalLigature in origLigaturePre.SuccessorEdges ) {
				if ( originalLigature is Ligature ) {
					Edge originalTarget = originalLigature.PostVertex.PrincipalEdge;
					Edge cloneTarget = (Edge)originalTarget.UtilRef;
					if ( cloneTarget == null ) continue;
					Vertex.WhichVertex targetRole = originalLigature.PostVertex.Role;
					Vertex cloneLigaturePost = cloneTarget.GetVertex(targetRole);
					AddLigature(cloneLigaturePre,cloneLigaturePost);
				}
			}
		}

        /// <summary>
        /// Gets the pre- or post-vertex of this edge..
        /// </summary>
        /// <param name="whichVertex">Which vertex is desired (pre or post).</param>
        /// <returns></returns>
		public Vertex GetVertex(Vertex.WhichVertex whichVertex){
			if ( whichVertex == Vertex.WhichVertex.Pre ) return Pre;
			return Post;
		}

        /// <summary>
        /// Gets or sets the utility reference. This is a reference that can be used by whomever needs to do so,
        /// for short periods. The cloning mechanism, for example, uses it during cloning.
        /// </summary>
        /// <value>The util ref.</value>
		public object UtilRef {
			get { return m_ref; }
			set {
				if ( value != null && m_ref != null ) {
					throw new UtilityReferenceInUseException(m_ref);
				}
				m_ref = value;
			}
		}
        #endregion

        /// <summary>
        /// This edge will immediately suspend execution until the otherEdge completes.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        /// <param name="exec">The executive for the model to which both edges belong.</param>
        /// <param name="otherEdge">The edge whose completion this edge will await.</param>
		public void Join(IDictionary graphContext, IExecutive exec, Edge otherEdge){
			new EdgeJoiner(graphContext,exec.CurrentEventController,otherEdge).Join(graphContext);
		}

		/// <summary>
		/// Gives up the execution thread temporarily to any awaiting edges. This edge will be
		/// called to resume execution later in this same timeslice.
		/// </summary>
		/// <param name="exec">The executive for the model to which this edge belongs.</param>
		public void Yield(IExecutive exec){
			exec.CurrentEventController.SuspendUntil(exec.Now);
		}

		/// <summary>
		/// Accepts a visitor, subsequently calling the visitor's Visit(this) method. See the Visitor design pattern for details.
		/// </summary>
		/// <param name="visitor">The visitor to be accepted.</param>
		public virtual void Accept(IVisitor visitor){
			visitor.Visit(this);
		}

		#region >>> Inactive persist-to-table methods.
		/*
		public virtual void CreateTables(ref DataSet ds){
			if (!ds.Tables.Contains("Edges") ) {
				DataTable dt = new DataTable("Edges");
				dt.Columns.Add(new DataColumn("Name",typeof(string)));
				dt.Columns.Add(new DataColumn("MyGuid",typeof(Guid)));
				dt.Columns.Add(new DataColumn("PreVertex",typeof(Guid)));
				dt.Columns.Add(new DataColumn("PostVertex",typeof(Guid)));
				DataColumn dc = new DataColumn("Parent",typeof(Guid));
				dc.AllowDBNull = true;
				dt.Columns.Add(dc);
				ds.Tables.Add(dt);
			}
			
			if (!ds.Tables.Contains("EdgeChildren") ) {
				DataTable children = new DataTable("EdgeChildren");
				children.Columns.Add(new DataColumn("Parent",typeof(Guid)));
				children.Columns.Add(new DataColumn("Child",typeof(Guid)));
				ds.Tables.Add(children);
			}

		}
		public virtual void PopulateRow(ref DataSet ds, Hashtable directory){
			DataTable dt = ds.Tables["Edges"];
			string name = this.Name;
			Guid myGuid = (Guid)directory[this];
			Guid preVertex = (Guid)directory[m_pre];
			Guid postVertex = (Guid)directory[m_post];
			object parent;
			if ( m_parentEdge == null ) {
				parent = null;
			} else {
				parent = (Guid)directory[m_parentEdge];
			}
			dt.Rows.Add(new object[]{name,preVertex,postVertex,parent});

			if ( m_childEdges != null ) {
				foreach ( Edge child in m_childEdges ) {
					ds.Tables["EdgeChildren"].Rows.Add(new object[]{myGuid,(Guid)directory[child]});
				}
			}
		}

		public virtual void PopulateObject(ref DataSet ds, Guid myGuid, Hashtable directory){
			string expr = "MyGuid == " + myGuid;
			foreach ( DataRow dr in ds.Tables["EdgeChildren"].Select(expr) ){
				Guid childGuid = (Guid)dr["Child"];
				m_childEdges.Add(directory[childGuid]);
			}
		}
		*/
		#endregion
		
		#region IXmlPersistable Members
        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public virtual void SerializeTo(XmlSerializationContext xmlsc) {
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("ChildEdges",m_childEdges);
			xmlsc.StoreObject("ChildLigatures",m_childLigatures);
			xmlsc.StoreObject("ParentEdge",ParentEdge);
			xmlsc.StoreObject("PostVertex",Post);
			xmlsc.StoreObject("PreVertex",Pre);
			xmlsc.StoreObject("ExecutionDelegate",ExecutionDelegate);
			xmlsc.StoreObject("EESE",EdgeExecutionStartingEvent);
			xmlsc.StoreObject("EEFE",EdgeExecutionFinishingEvent);
			//xmlsc.StoreObject("EECS",EdgeExecutionCompletionSignaler);

		}

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public virtual void DeserializeFrom(XmlSerializationContext xmlsc) {
			m_name = (string)xmlsc.LoadObject("Name");
			m_childEdges = (ArrayList)xmlsc.LoadObject("ChildEdges");
			//_Debug.WriteLine("Just deserialized " + m_name + ", and it has " + ChildEdges.Count + " child edges.");
			m_childLigatures = (ArrayList)xmlsc.LoadObject("ChildLigatures");
			ParentEdge = (Edge)xmlsc.LoadObject("ParentEdge");
			Post = (Vertex)xmlsc.LoadObject("PostVertex");
			//_Debug.WriteLine("Assigning " + m_post.Name + "(" + m_post.GetHashCode()+ ") into " + this.Name +"(" + this.GetHashCode()+ ").");
			Pre = (Vertex)xmlsc.LoadObject("PreVertex");
			//_Debug.WriteLine("Assigning " + m_pre.Name + "(" + m_pre.GetHashCode()+ ") into " + this.Name +"(" + this.GetHashCode()+ ").");
			ExecutionDelegate = (EdgeExecutionDelegate)xmlsc.LoadObject("ExecutionDelegate");			
			EdgeExecutionStartingEvent = (EdgeEvent)xmlsc.LoadObject("EESE");
			EdgeExecutionFinishingEvent = (EdgeEvent)xmlsc.LoadObject("EEFE");
			//EdgeExecutionCompletionSignaler = (EdgeExecutionCompletionSignaler)xmlsc.LoadObject("EECS");

			#region >>> Stuff from the constructor <<<
			m_eecs = new EdgeExecutionCompletionSignaler(OnExecutionComplete);

			m_onChildGainedPredecessorHandler = new StaticEdgeEvent(OnChildGainedPredecessor);
			m_onChildGainedSuccessorHandler   = new StaticEdgeEvent(OnChildGainedSuccessor);
			m_onChildLostPredecessorHandler   = new StaticEdgeEvent(OnChildLostPredecessor);
			m_onChildLostSuccessorHandler     = new StaticEdgeEvent(OnChildLostSuccessor);

			InitializeStructuralChangeHandlers();

			#endregion

			#region >>> Stuff from 'CreateVertices()' that isn't part of deserialization. <<<
			Pre.BeforeVertexFiringEvent+=new VertexEvent(OnPreVertexStartingToFire);
			Post.BeforeVertexFiringEvent+=new VertexEvent(OnPostVertexStartingToFire);
			#endregion

			//_Debug.WriteLine("Just deserialized " + m_name + ", and it has " + ChildEdges.Count + " child edges.");
		}

		#endregion

		#region IHasValidity Members

        /// <summary>
        /// Fired when the Validation Service determines that this edge's validity has changed.
        /// </summary>
        public event Validity.ValidityChangeHandler ValidityChangeEvent;

        /// <summary>
        /// Gets or sets the validation service that oversees the implementer.
        /// </summary>
        /// <value>The validation service.</value>
		public Validity.ValidationService ValidationService { get { return m_vm; } set { m_vm = value; } }

        /// <summary>
        /// Called by the ValidationService upon an overall validity change.
        /// </summary>
        /// <param name="newValidity">The new validity.</param>
		public void NotifyOverallValidityChange(Validity.Validity newValidity){

			if ( ValidityChangeEvent != null ) {
				ValidityChangeEvent(this,newValidity);
			} else {
				//Console.WriteLine(this.Name + " would be firing ValidityChangeEvent, but no one's listening.");
			}
		}

        /// <summary>
        /// Gets the parent (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		Validity.IHasValidity Validity.IHasValidity.GetParent() { return Parent; }

        /// <summary>
        /// Gets the children (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		public virtual IList GetChildren() { return _emptyCollection; }

        /// <summary>
        /// Gets the successors (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		public virtual IList GetSuccessors() {
			if ( m_successorList == null ) m_successorList = new ArrayList(new Validity.IHasValidity[]{Post});
			return m_successorList;
		}

        /// <summary>
        /// Gets or sets the state (from a perspective of validity) of the implementer.
        /// </summary>
        /// <value>The state of the self.</value>
		public virtual Validity.Validity SelfState { get { return Validity.Validity.Valid; } set { } }

		#endregion

		#region IPartOfGraphStructure Members

        /// <summary>
        /// This event is fired any time the graph's structure changes.
        /// </summary>
		public event StructureChangeHandler StructureChangeHandler;

		#endregion

        #region Structural Change Events
        private void Edge_GainedPredecessorEvent(Edge theEdge) {
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.AddPreEdge, false);
        }

        private void Edge_GainedSuccessorEvent(Edge theEdge) {
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.AddPostEdge, false);
        }

        private void Edge_LostPredecessorEvent(Edge theEdge) {
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.RemovePreEdge, false);
        }

        private void Edge_LostSuccessorEvent(Edge theEdge) {
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.RemovePostEdge, false);
        }

        #endregion Structural Change Events


    }

	/// <summary>
	/// Used internally, dynamically, and only during a Join operation. Create only on the stack.
	/// </summary>
	class EdgeJoiner{
		EdgeEvent m_edgeFinishingEvent;
		Edge m_otherEdge;
		public EdgeJoiner(IDictionary graphContext, IDetachableEventController idec, Edge otherEdge){
			m_edgeFinishingEvent = new EdgeEvent(OtherEdgeCompleted);

			m_otherEdge = otherEdge;
			if ( graphContext.Contains(this) ) graphContext.Remove(this);
			graphContext.Add(this,idec);
		}

		public void Join(IDictionary graphContext){
			m_otherEdge.EdgeFinishingEvent += m_edgeFinishingEvent;
			IDetachableEventController idec = (IDetachableEventController)graphContext[this];
			idec.Suspend();
		}

		private void OtherEdgeCompleted(IDictionary graphContext, Edge otherEdge){
			IDetachableEventController idec = ((IDetachableEventController)graphContext[this]);
			if ( idec.IsWaiting() ) {
				idec.Resume();
			} else {
				//System.Diagnostics.Debugger.Break();
			}
			m_otherEdge.EdgeFinishingEvent -= m_edgeFinishingEvent;	
		}
	}

	
    /// <summary>
    /// Thrown when a UtilityReference is being set, but is already in use. A UtilityReference is a reference that can be used by 
    /// whomever needs to do so, for short periods. The cloning mechanism, for example, uses it during cloning. 
    /// </summary>
	public class UtilityReferenceInUseException : Exception {
		object m_ref;
		public UtilityReferenceInUseException(object reference):base("Utility reference already in use by " + reference){
			m_ref = reference;
		}
		public object Reference { get { return m_ref ;} }
	}


    /// <summary>
    /// A Ligature is an edge that connects nodes, but unlike a task, has no duration. It is used only to model dependencies
    /// such as when two predecessor tasks must complete before a successor task is allowed to commence - the finish vertices
    /// of the two predecessor tasks would be connected to the start vertex of the successor task.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Graphs.Edge" />
    public class Ligature : Edge {

		/// <summary>
		/// Default constructor for persistence only.
		/// </summary>
		public Ligature(){}

        /// <summary>
        /// Initializes a new instance of the <see cref="Ligature"/> class.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        public Ligature(string name){
			m_name = (name==null?ToString():name);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="Ligature"/> class.
        /// </summary>
        /// <param name="from">The vertex from which this ligature starts.</param>
        /// <param name="to">The vertex at which this ligature ends.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        public Ligature(Vertex from, Vertex to, string name):this(name){
			Pre = from;
			Post = to;
			from.AddPostEdge(this);
			to.AddPreEdge(this);
//			from.StructureChangeHandler +=new StructureChangeHandler(from_StructureChangeHandler);
//			to.StructureChangeHandler += new StructureChangeHandler(to_StructureChangeHandler);
		}

        /// <summary>
        /// Called by the pre-vertex when it has been satisfied - that is, all incoming edges and
        /// synchronizers to that vertex have fired.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        public override void PreVertexSatisfied(IDictionary graphContext){
			PostVertex.PreEdgeSatisfied(graphContext, this);
		}

        /// <summary>
        /// Initializes the structural change handlers -  GainedPredecessorEvent, GainedSuccessorEvent, LostPredecessorEvent, and LostSuccessorEvent.
        /// </summary>
        protected override void InitializeStructuralChangeHandlers() {
		
		}


        /// <summary>
        /// Disconnects this instance from any parent edges, predecessors and successors. Child edges are left
        /// attached.
        /// </summary>
        public override void Disconnect(){
			Post.RemovePreEdge(this);
			Pre.RemovePostEdge(this);
//			m_pre.StructureChangeHandler -=new StructureChangeHandler(from_StructureChangeHandler);
//			m_post.StructureChangeHandler -= new StructureChangeHandler(to_StructureChangeHandler);
			Post = null;  // Set post to null first, so that invalidity will not
			// propagate downstream to a task that is immediately 
			// thereafter, disconnected.
			Pre = null;

		}

        /// <summary>
        /// Creates a new object that is a copy of the current instance. This is not supported for ligatures.
        /// </summary>
        /// <returns>A new object that is a copy of this instance. This method calls _PopulateClone.</returns>
        /// <exception cref="ApplicationException">Application attempted to clone a ligature.</exception>
        public override object Clone(){
			throw new InvalidOperationException("Application attempted to clone a ligature.");
		}

        /// <summary>
        /// Creates the pre and post vertices for this edge, providing them with default names
        /// and connecting them to this edge.
        /// </summary>
        protected override void CreateVertices(){}

        /// <summary>
        /// Creates a name for this ligature based on the names of the from and to edges.
        /// </summary>
        /// <param name="from">The edge from which this ligature starts.</param>
        /// <param name="to">The edge at which this ligature ends.</param>
        /// <returns>System.String.</returns>
        public static string CreateName(Edge from, Edge to){
			return CreateName(from.PostVertex,to.PreVertex);
		}

        /// <summary>
        /// Creates a name for this ligature based on the names of the from and to vertices.
        /// </summary>
        /// <param name="from">The vertex from which this ligature starts.</param>
        /// <param name="to">The vertex at which this ligature ends.</param>
        /// <returns>System.String.</returns>
        public static string CreateName(Vertex from, Vertex to){
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("(");
			sb.Append(from.Name);
			sb.Append("->");
			sb.Append(to.Name);
			sb.Append(")");
			return sb.ToString();
		}
	}
}
