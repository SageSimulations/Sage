/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.Validity {

    /// <summary>
    /// Specifies whether, from the perspective of a Validity service, something is valid.
    /// </summary>
	public enum Validity {
        /// <summary>
        /// The item is valid.
        /// </summary>
        Valid,
        /// <summary>
        /// The item is not valid.
        /// </summary>
        Invalid }

    /// <summary>
    /// A callback that is expected to be invoked when something's state of validity changes.
    /// </summary>
    /// <param name="ihv">The object that has validity.</param>
    /// <param name="newState">The new value that the object's validity has taken on.</param>
	public delegate void ValidityChangeHandler(IHasValidity ihv, Validity newState);
	
    /// <summary>
    /// Implemented by any object that has a state of validity that is managed by a ValidationService.
    /// </summary>
	public interface IHasValidity : IPartOfGraphStructure {
        /// <summary>
        /// Gets or sets the validation service that oversees the implementer.
        /// </summary>
        /// <value>The validation service.</value>
		ValidationService ValidationService { get; set; }
        /// <summary>
        /// Gets the children (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		IList GetChildren();
        /// <summary>
        /// Gets the successors (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		IList GetSuccessors();
        /// <summary>
        /// Gets the parent (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		IHasValidity GetParent();
        /// <summary>
        /// Gets or sets the state (from a perspective of validity) of the implementer.
        /// </summary>
        /// <value>The state of the self.</value>
		Validity SelfState { get; set; }
        /// <summary>
        /// Fires when the implementer's validity state is changed.
        /// </summary>
		event ValidityChangeHandler ValidityChangeEvent;
        /// <summary>
        /// Called by the ValidationService upon an overall validity change.
        /// </summary>
        /// <param name="newValidity">The new validity.</param>
		void NotifyOverallValidityChange(Validity newValidity);
	}

	
    /// <summary>
    /// A class that abstracts and manages validity relationships in a directed acyclic graph. If
    /// an object is invalid, its parent, grandparent (etc.), and downstream objects in that graph
    /// are also seen to be invalid.
    /// </summary>
	public class ValidationService {

		#region >>> Private Fields <<<
		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("ValidationService");
		private static readonly ArrayList s_empty_List = ArrayList.ReadOnly(new ArrayList());
		private static Utility.WeakList _knownServices = new Utility.WeakList();
		private IHasValidity m_root;
		private int m_suspensions;
		private bool m_dirty;
		private StructureChangeHandler m_structureChangeListener;
		private Hashtable m_htNodes;
		private Stack m_suspendResumeStack;
		private Hashtable m_oldValidities = null; // For holding pre-refresh validities so that refresh can fire the right change events.
		#endregion

        /// <summary>
        /// Gets a list of the known ValidationServices.
        /// </summary>
        /// <value>The known services.</value>
		public static IList KnownServices { get { return _knownServices; } }

        /// <summary>
        /// Creates a new instance of the <see cref="T:ValidationService"/> class.
        /// </summary>
        /// <param name="root">The root task of the directed acyclic graph.</param>
		public ValidationService(Tasks.Task root){
			m_root = root.PreVertex;
			m_suspensions = 0;
			m_dirty = true;
			m_structureChangeListener = new StructureChangeHandler(OnStructureChange);
			if ( s_diagnostics ) m_suspendResumeStack = new Stack();
			_knownServices.Add(this);
			Refresh();

		}

        /// <summary>
        /// Suspends validation computations to control cascaded recomputations. Suspend recomputation, make a bunch 
        /// of changes, and resume. If anything needs to be recalculated, it will be. This also implements a nesting
        /// capability, so if a suspend has been done 'n' times (perhaps in a call stack), the recomputation will only
        /// be done once all 'n' suspends have been resumed.
        /// </summary>
		public void Suspend(){
			if ( m_suspensions == 0 && m_htNodes != null ) {
				m_oldValidities = new Hashtable();
				foreach ( ValidityNode vn in m_htNodes.Values ) m_oldValidities.Add(vn.Mine,vn.OverallValid);
			}

			m_suspensions++;

			if ( s_diagnostics ) {
				System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0,true);
				System.Diagnostics.StackFrame sf = st.GetFrame(1);
				string where = sf.GetMethod() + " [" + sf.GetFileName() + ", line " + sf.GetFileLineNumber() + "]";
				m_suspendResumeStack.Push(where);
				//_Debug.WriteLine("Suspend (" + m_suspensions + ") : " + where);
			}
		}

        /// <summary>
        /// Resumes this instance. See <see cref="Highpoint.Sage.Graphs.Validity.ValidationService.Suspend()"/>.
        /// </summary>
		public void Resume(){
			m_suspensions--;
			if ( m_suspensions < 0 ) {
				if ( s_diagnostics ) {
					System.Diagnostics.Debugger.Break();
				} else {
					m_suspensions = 0;
				}
			}
			if ( s_diagnostics ) {
				System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0,true);
				System.Diagnostics.StackFrame sf = st.GetFrame(1);
				string where = sf.GetMethod() + " [" + sf.GetFileName() + ", line " + sf.GetFileLineNumber() + "]";
				where = where.Split(new char[]{','},2)[0];
				string stackThinks = (string)m_suspendResumeStack.Pop();
				stackThinks = stackThinks.Split(new char[]{','},2)[0];
				// TODO: Move this into an Errors & Warnings collection on the model.
				if ( !where.Equals(stackThinks) ) {
					string msg = "ERROR - Validation Service's \"Resume\" location (" + where + ")doesn't match the opposite \"Suspend\" location!";
					if ( m_root is SimCore.IModelObject ) {
						((SimCore.IModelObject)m_root).Model.AddWarning(new SimCore.GenericModelWarning("ValidationStackMismatch",msg,where,this));
					} else {
						_Debug.WriteLine(msg);
					}
				}
				//_Debug.WriteLine("Resume  (" + m_suspensions + ") : " + where);
			}
			Refresh();
		}

        /// <summary>
        /// Performs the validity computation.
        /// </summary>
        /// <param name="force">if set to <c>true</c> the service ignores whether anything in the graph has changed since the last refresh.</param>
		public void Refresh(bool force){
			if ( force) m_dirty = true;

			m_suspensions=0; // TODO: Check if this scould capture & restore the m_suspensions value.

			Refresh();
		}


        /// <summary>
        /// Performs the validity computation if there are no suspensions in progress, and anything in the graph has changed since the last refresh. 
        /// </summary>
		public void Refresh(){
			if ( m_suspensions==0 && m_dirty ) {
				if ( s_diagnostics ) _Debug.WriteLine("Refreshing after a structure change.");
				
				if ( m_htNodes != null ) {
					foreach ( IHasValidity ihv in m_htNodes.Keys ) {
						ihv.StructureChangeHandler -= m_structureChangeListener;
					}
				}

				if ( m_htNodes != null && m_oldValidities == null ) {
					m_oldValidities = new Hashtable();
					foreach ( ValidityNode vn in m_htNodes.Values ) m_oldValidities.Add(vn.Mine,vn.OverallValid);
				}

				m_htNodes = new Hashtable();
				AddNode(m_root); // And recursively down from there.

				foreach ( ValidityNode vn in m_htNodes.Values ) vn.EstablishMappingsToValidityNodes();

				foreach ( ValidityNode vn in m_htNodes.Values ) vn.CreateValidityNetwork();

				foreach ( ValidityNode vn in m_htNodes.Values ) vn.Initialize(vn.Mine.SelfState);

				m_dirty = false;
				
				if ( m_oldValidities != null ) {
					// After recreating the graph of validityNodes, we need to update anyone who was watching.
					foreach ( ValidityNode vn in m_htNodes.Values ) {
						if ( !m_oldValidities.Contains(vn.Mine) ) continue;
						bool oldValidity = (bool)m_oldValidities[vn.Mine];
						if ( vn.OverallValid != oldValidity ) {
							vn.Mine.NotifyOverallValidityChange(vn.OverallValid?Validity.Valid:Validity.Invalid);
						}
					}
					m_oldValidities = null;
				}
			}
		}


        /// <summary>
        /// Creates a status report that describes the validity state of the graph.
        /// </summary>
        /// <returns>A status report that describes the validity state of the graph.</returns>
		public string StatusReport(){
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach ( ValidityNode vn in m_htNodes.Values ) {
				sb.Append(vn.Name + " : Self " + vn.SelfValid + ", Preds = " + vn.PredecessorsValid + "(" + vn.InvalidPredecessorCount + ") Children = " + vn.ChildrenValid + "(" + vn.InvalidChildCount + ").\r\n");
			}

			return sb.ToString();
		}

        /// <summary>
        /// Creates a status report that describes the validity state of the graph, at and below the provided node.
        /// </summary>
        /// <param name="ihv">The provided node that is to be the root of this report.</param>
        /// <returns></returns>
        public string StatusReport(IHasValidity ihv)
        {
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) {
				return "Unknown object - " + ihv;
			} else {
				return (vn.Name + " : Self " + vn.SelfValid + ", Preds = " + vn.PredecessorsValid + "(" + vn.InvalidPredecessorCount + ") Children = " + vn.ChildrenValid + "(" + vn.InvalidChildCount + ").\r\n");
			}
		}

		private void AddNode(IHasValidity ihv){
			if ( !m_htNodes.Contains(ihv) ) {
				ValidityNode vn = new ValidityNode(this,ihv);
				m_htNodes.Add(ihv,vn);
				IList list = ihv.GetSuccessors();
				if ( list.Count > 0 ) {
					foreach ( object obj in list ) {
						IHasValidity successor = (IHasValidity)obj;
						AddNode(successor);
					}
				}
			}
			ihv.StructureChangeHandler +=m_structureChangeListener;
		}
	
		
		private void OnStructureChange(object obj, StructureChangeType chType, bool isPropagated) {

			#region Rule #1. If a vertex loses or gains a predecessor, it invalidates all of its sucessors.
			if ( StructureChangeTypeSvc.IsPreEdgeChange(chType) ) {
				if ( obj is Vertex ) {
					Vertex vertex = (Vertex)obj;
					ArrayList successors = new ArrayList();
					successors.AddRange(vertex.GetSuccessors());
					while ( successors.Count > 0 ) {
						IHasValidity ihv = (IHasValidity)successors[0];
						successors.RemoveAt(0);
						if ( ihv is Tasks.Task ) {
							((Tasks.Task)ihv).SelfValidState = false;
						} else {
							successors.AddRange(ihv.GetSuccessors());
						}
					}
				}
			}
			#endregion

			m_dirty = true;
		}

        /// <summary>
        /// Notifies the specified object in the graph of its self state change.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
		public void NotifySelfStateChange(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn != null ) {
				vn.NotifySelfStateChange(ihv.SelfState);
			}
		}
	
		#region >>> Peer Getters (parent, predecessors, successors and children) <<<
        /// <summary>
        /// Gets the predecessors of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The predecessors of the specified object in the graph.</returns>
		public IList GetPredecessorsOf(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return s_empty_List;
			
			ArrayList retval = new ArrayList();
			foreach (ValidityNode subNode in vn.Predecessors ) {
				IHasValidity ihv2 = subNode.Mine;
				retval.Add(ihv2);
			}

			return retval;
		}

        /// <summary>
        /// Gets the successors of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The successors of the specified object in the graph.</returns>
		public IList GetSuccessorsOf(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return s_empty_List;
			
			ArrayList retval = new ArrayList();
			foreach (ValidityNode subNode in vn.Successors ) {
				IHasValidity ihv2 = subNode.Mine;
				retval.Add(ihv2);
			}

			return retval;
		}

        /// <summary>
        /// Gets the children of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The children of the specified object in the graph.</returns>
		public IList GetChildrenOf(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return s_empty_List;
			
			ArrayList retval = new ArrayList();
			foreach (ValidityNode subNode in vn.Children ) {
				IHasValidity ihv2 = subNode.Mine;
				retval.Add(ihv2);
			}

			return retval;
		}

        /// <summary>
        /// Gets the parent of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The parent of the specified object in the graph.</returns>
		public IHasValidity GetParentOf(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return null;
			if ( vn.Parent == null ) return null;
			return vn.Parent.Mine;
		}
		
		#endregion
		
		#region >>> ValidityState Getters (overall, self, predecessors and children) <<<
        /// <summary>
        /// Gets the overall state of the validity of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The</returns>
		public Validity GetValidityState(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return Validity.Invalid;
			return vn.OverallValid?Validity.Valid:Validity.Invalid;
		}

        /// <summary>
        /// Gets the state of the self validity of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The state of the self validity of the specified object in the graph.</returns>
		public Validity GetSelfValidityState(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return Validity.Invalid;
			return vn.SelfValid?Validity.Valid:Validity.Invalid;
		}

        /// <summary>
        /// Gets the state of validity of the predecessors of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The state of validity of the predecessors of the specified object in the graph.</returns>
		public Validity GetPredecessorValidityState(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return Validity.Invalid;
			return vn.PredecessorsValid?Validity.Valid:Validity.Invalid;
		}

        /// <summary>
        /// Gets the aggregate validity state of the children of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The aggregate validity state of the children of the specified object in the graph.</returns>
		public Validity GetChildValidityState(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return Validity.Invalid;
			return vn.ChildrenValid?Validity.Valid:Validity.Invalid;
		}

        /// <summary>
        /// Gets the invalid predecessor count of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The invalid predecessor count of the specified object in the graph.</returns>
		public int GetInvalidPredecessorCountOf(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return int.MinValue;
			return vn.InvalidPredecessorCount;
		}

        /// <summary>
        /// Gets the invalid child count of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The invalid child count of the specified object in the graph.</returns>
		public int GetInvalidChildCountOf(IHasValidity ihv){
			ValidityNode vn = (ValidityNode)m_htNodes[ihv];
			if ( vn == null ) return int.MinValue;
			return vn.InvalidChildCount;
		}
		#endregion

        /// <summary>
        /// Gets a list of the IHasValidity objects that are known to this ValidationService.
        /// </summary>
        /// <value>The known validity holders.</value>
		public IList KnownValidityHolders { 
			get {
				ArrayList retval = new ArrayList();
				foreach ( ValidityNode vn in m_htNodes.Values ) retval.Add(vn.Mine);
				return retval;
			}
		}

		private class ValidityNode : SimCore.IHasName {
			
			#region >>> Private Fields <<<
			private IHasValidity m_mine;
			private ValidationService m_validationService;
			private ArrayList m_predecessors;
			private ArrayList m_successors;
			private ArrayList m_children;
			private ValidityNode m_parent;
			private int m_nInvalidPredecessors;
			private int m_nInvalidChildren;
			#endregion

			public ValidityNode(ValidationService validationService, IHasValidity mine){
				m_nInvalidChildren = 0;
				m_nInvalidPredecessors = 0;
				m_validationService = validationService;
				m_mine = mine;
				m_mine.ValidationService = m_validationService;
				m_successors = new ArrayList(mine.GetSuccessors());
				m_children = new ArrayList(mine.GetChildren());
				m_predecessors = new ArrayList();
				if ( m_mine is SimCore.IHasName) {
					m_name = ((SimCore.IHasName)m_mine).Name;
				} else {
					m_name = m_mine.GetType().ToString();
				}
			}
			
			private string m_name;
			public string Name { get { return m_name; } }
			
			public IHasValidity Mine { get { return m_mine; } }

			#region >>> ValidityState Getters (overall, self, predecessors and children) <<<
			public bool OverallValid { get { return SelfValid && ChildrenValid && PredecessorsValid; } }
			
			public bool SelfValid { get { return (m_mine.SelfState == Validity.Valid); } }
			
			public bool PredecessorsValid {  get { return (m_nInvalidPredecessors == 0); } }
			
			public bool ChildrenValid {  get { return m_nInvalidChildren == 0; } }
			#endregion

			public void NotifySelfStateChange(Validity newValidity){
				if ( ChildrenValid && PredecessorsValid ) { // i.e. This selfState change will cause an overall state change.
					if ( newValidity == Validity.Valid ) {
						// We just became valid overall.
						if ( m_parent != null ) m_parent.InvalidChildCount--;
						foreach ( ValidityNode vn in Successors ) vn.InvalidPredecessorCount--;
					
					} else if ( newValidity == Validity.Invalid ) {
						// We just became invalid overall.
						if ( m_parent != null ) m_parent.InvalidChildCount++;
						foreach ( ValidityNode vn in Successors ) vn.InvalidPredecessorCount++;
					}
					m_mine.NotifyOverallValidityChange(newValidity);
				}
			}

			/// <summary>
			/// Gets or sets the invalid predecessor count.
			/// </summary>
			/// <value></value>
			public int InvalidPredecessorCount {
				get { return m_nInvalidPredecessors; }
				set {
					bool wasValid = OverallValid;
					m_nInvalidPredecessors = value;
					
					if ( OverallValid != wasValid ) ReactToInvalidation();
				}
			}

			/// <summary>
			/// Gets or sets the invalid child count.
			/// </summary>
			/// <value></value>
			public int InvalidChildCount {
				get { return m_nInvalidChildren; }
				set {
					bool wasValid = OverallValid;
					m_nInvalidChildren = value;

					if ( OverallValid != wasValid ) ReactToInvalidation();
				}
			}

			/// <summary>
			/// Reacts to invalidation by incrementing parent's invalid child count, successors'
			/// invalid predecessor count, and then telling my Mine element to notify listeners
			/// of its invalidation.
			/// </summary>
			private void ReactToInvalidation(){
				m_mine.NotifyOverallValidityChange(OverallValid?Validity.Valid:Validity.Invalid);
				
				int delta = OverallValid?-1:+1;
				if ( m_parent != null ) m_parent.InvalidChildCount += delta;
				foreach (ValidityNode vn in Successors) vn.InvalidPredecessorCount += delta;

				m_mine.NotifyOverallValidityChange(OverallValid?Validity.Valid:Validity.Invalid);
			}
			
			/// <summary>
			/// Establishes mappings between validity nodes and the elements they monitor.
			/// </summary>
			public void EstablishMappingsToValidityNodes(){
				if ( m_successors.Count > 0 ) {
					if ( !(m_successors[0] is ValidityNode) ) {
						for ( int i = 0 ; i < m_successors.Count ; i++ ) {
							m_successors[i] = m_validationService.m_htNodes[m_successors[i]];
						}
					}
				}

				if ( m_children.Count > 0 ) {
					if ( !(m_children[0] is ValidityNode) ) {
						for ( int i = 0 ; i < m_children.Count ; i++ ) {
							m_children[i] = m_validationService.m_htNodes[m_children[i]];
						}
					}
				}

				IHasValidity myParent = m_mine.GetParent();
				if ( m_parent == null && myParent != null ) m_parent = (ValidityNode)m_validationService.m_htNodes[myParent];

			}

			public void CreateValidityNetwork(){
				foreach ( ValidityNode succ in m_successors ) {
					succ.m_predecessors.Add(this);
				}
			}

			public void Initialize(Validity initialSelfState){
				if ( initialSelfState == Validity.Invalid ) {
					if ( m_parent != null ) m_parent.InvalidChildCount++;
					foreach ( ValidityNode vn in Successors ) vn.InvalidPredecessorCount++;
				}
				// If it's valid, it has no effect on parents & predecessors from the initial zero counts.
			}
			
			#region >>> Peer Getters (parent, predecessors, successors and children) <<<
			public ArrayList Predecessors { get { return m_predecessors; } }
			public ArrayList Successors { get { return m_successors; } }
			public ArrayList Children { get { return m_children; } }
			public ValidityNode Parent { get { return m_parent; } }
			#endregion
		}
	}
}
