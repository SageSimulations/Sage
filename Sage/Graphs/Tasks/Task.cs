/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.Graphs.Analysis;

namespace Highpoint.Sage.Graphs.Tasks {

	/// <summary>
	/// Implemented by an object that can create Task objects.
	/// </summary>
	public interface ITaskFactory {
		/// <summary>
		/// Creates a task object that is a part of the provided model.
		/// </summary>
		/// <param name="model">The model in which this task will run.</param>
		/// <param name="creationContext">The creation context (collection of serialization-related data) that this serialization stream uses.</param>
		/// <returns>The task.</returns>
		Task CreateTask(IModel model, Hashtable creationContext);
	}


    public delegate void TaskEvent(IDictionary graphContext, Task theTask);
    public delegate void StaticTaskEvent(Task theTask);
    public delegate void TaskCompletionSignaler(IDictionary graphContext);

	/// <summary>
	/// A Task is an edge that incorporates durational aspects, supports the concept of
	/// children (child tasks are tasks that "belong to" the parent, and usually provide
	/// the detailed execution sequence &amp; aspects of the conceptual parent task. Tasks
	/// also support the concept of validity, where a task is valid if (a) it's own state
	/// is valid, (b) all of its children are valid, and (c) all of its predecessor
	/// tasks are valid.
	/// </summary>
    public class Task : Edge, ISupportsPertAnalysis, IModelObject {


        #region Private Fields
        // private static readonly bool m_diagnostics = false;
	    private static readonly bool s_diagnostics = true; //Diagnostics.DiagnosticAids.Diagnostics("Task");
        private static readonly bool s_initial_Self_State = false;
        private bool m_bSelfStateValid = s_initial_Self_State;

		//private TaskHasInvalidSelfStateError m_mySubmittedError = null;
        private VolatileKey m_selfStartTimeKey = new VolatileKey("Task Start");
        private VolatileKey m_selfFinishTimeKey = new VolatileKey("Task Finish");

        private bool m_keepingTimingData = true;

        private Guid m_guid = Guid.Empty;
        private IModel m_model;

        #endregion Private Fields

        /// <summary>
        /// The ke under which this task's Edge Execution Completion Signaler is stored, in the Graph Context.
        /// </summary>
        protected VolatileKey EecsKey = new VolatileKey("Edge execution completion signaler");

        /// <summary>
        /// Fired when the task is starting, as a result of the EdgeStartingEvent, which is fired when the preVertex has been fully satisfied.
        /// </summary>
        public event TaskEvent TaskStartingEvent;
        /// <summary>
        /// Fired when the task is finishing, as a result of the EdgeCompletionEvent, which is fired when the postVertex has been fully satisfied.
        /// </summary>
        public event TaskEvent TaskFinishingEvent;
        /// <summary>
        /// Fired immediately prior to calling the ExecutionDelegate (where application code is run.)
        /// </summary>
        public event TaskEvent TaskExecutionStartingEvent;
        /// <summary>
        /// Fired immediately following completion of the ExecutionDelegate (where application code was run.)
        /// </summary>
        public event TaskEvent TaskExecutionFinishingEvent;

        /// <summary>
        /// Creates a new instance of the <see cref="T:Task"/> class. Creates an arbitrary Guid for the new task.
        /// </summary>
        /// <param name="model">The model in which the task runs.</param>
        /// <param name="name">The name of the task.</param>
		public Task(IModel model, string name):this(model,name,Guid.NewGuid()){}

        /// <summary>
        /// Creates a new instance of the <see cref="T:Task"/> class.
        /// </summary>
        /// <param name="model">The model in which the task runs.</param>
        /// <param name="name">The name of the task.</param>
        /// <param name="guid">The GUID of the task.</param>
        public Task(IModel model, string name, Guid guid):base(name) {

            InitializeIdentity(model, name, Description, guid);

            ExecutionDelegate = new EdgeExecutionDelegate(OnEdgeExecution);

            EdgeStartingEvent           += new EdgeEvent(OnEdgeStartingEvent);
            EdgeExecutionStartingEvent  += new EdgeEvent(OnEdgeExecutionStartingEvent);
            EdgeExecutionFinishingEvent += new EdgeEvent(OnEdgeExecutionFinishingEvent);
            EdgeFinishingEvent          += new EdgeEvent(OnEdgeFinishingEvent);

			StructureChangeHandler		+= new StructureChangeHandler(MyStructureChangedHandler);

			ValidityChangeEvent			+=new Graphs.Validity.ValidityChangeHandler(Task_ValidityChangeEvent);

            ResetDurationData();

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model in which the task runs.</param>
        /// <param name="name">The name of the task.</param>
        /// <param name="description">The description of the task.</param>
        /// <param name="guid">The GUID of the task.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        /// <summary>
        /// Gets or sets a value indicating whether we want to keep timing data as this task executes.
        /// </summary>
        /// <value><c>true</c> if [keep timing data]; otherwise, <c>false</c>.</value>
        public bool KeepTimingData { get { return m_keepingTimingData; } set { m_keepingTimingData = value; } }
        
        #region Implementation of ISupportsPERTAnalysis

        /// <summary>
		/// Nominal duration is the average amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
		/// </summary>
		/// <returns>The nominal duration for this task.</returns>
		public virtual TimeSpan GetNominalDuration(){
			//Trace.WriteLine(this.Name + " is being asked for nominal duration, and it is returning " + m_aggDelay + "/" + m_numDelays + ".");
			return m_numDelays==0?TimeSpan.Zero:TimeSpan.FromTicks(m_aggDelay.Ticks/m_numDelays); 
		}

		/// <summary>
		/// Optimistic duration is the minimum amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
		/// </summary>
		/// <returns>The optimistic duration for this task.</returns>
        public virtual TimeSpan GetOptimisticDuration(){ return m_numDelays==0?TimeSpan.Zero:m_minDelay; }
		/// <summary>
		/// Pessimistic duration is the maximum amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
		/// </summary>
		/// <returns>The pessimistic duration for this task.</returns>
		public virtual TimeSpan GetPessimisticDuration(){ return m_numDelays==0?TimeSpan.Zero:m_maxDelay; }

        /// <summary>
        /// Sets the duration data to be used in PERT &amp; CPM analysis.
        /// </summary>
        /// <param name="optimistic">The optimistic (shortest) duration.</param>
        /// <param name="nominal">The nominal (average) duration.</param>
        /// <param name="pessimistic">The pessimistic (longest) duration.</param>
        public void SetDurationData(TimeSpan optimistic, TimeSpan nominal, TimeSpan pessimistic ) {
			//Trace.WriteLine("Delays for " + this.Name + " are " + "[" + optimistic + "|" + nominal + "|" + pessimistic + "]");
            if ( optimistic <= nominal && nominal <= pessimistic ) {
                m_minDelay  = optimistic;
                m_maxDelay  = pessimistic;
                m_aggDelay  = nominal;
                m_numDelays = 1;
                m_delaysExplicitlySet = true;
            }
        }

        /// <summary>
        /// Resets the duration data to TimeSpan.MaxValue for the minimum, MinValue for the maximum, and zero for aggregate.
        /// </summary>
        public void ResetDurationData(){
			//Trace.WriteLine(this.Name + " is having its duration data reset.");

            m_minDelay  = TimeSpan.MaxValue;
            m_maxDelay  = TimeSpan.MinValue;
            m_aggDelay  = TimeSpan.Zero;
            m_numDelays = 0;
            m_delaysExplicitlySet = false;
        }

        //public virtual double GetCost(string measure){ return 0.0; }

        private TimeSpan m_minDelay,m_maxDelay,m_aggDelay;
        private int      m_numDelays;
        private bool     m_delaysExplicitlySet;

        private void UpdateDurationStats(TimeSpan duration){
			//Trace.WriteLine(this.Name + " is updating duration stats for " + this.Name + ": Last execution took " + duration);
            if ( duration < m_minDelay ) m_minDelay = duration;
            if ( duration > m_maxDelay ) m_maxDelay = duration;
            m_aggDelay += duration;
            m_numDelays++;
			//Trace.WriteLine("\t My aggregate delay is " + m_aggDelay + ", and the number of delays in that aggregate is " + m_numDelays);
        }

        /// <summary>
        /// Resets the duration statistics. Since a task can run many times and track its min, max and average, this resets the min, 
        /// max, aggregate and count-of executions.
        /// </summary>
        public void ResetDurationStats(){
			//Console.WriteLine(this.Name + " is resetting duration stats.");
            m_minDelay  = TimeSpan.MaxValue;
            m_maxDelay  = TimeSpan.MinValue;
            m_aggDelay  = TimeSpan.Zero;
            m_numDelays = 0;
        }

        /// <summary>
        /// Recursivelies the reset all duration statistics for this task and all tasks below this task.
        /// </summary>
        /// <param name="edge">The edge.</param>
		public static void RecursivelyResetAllDurationStats(Edge edge){
			if ( edge is Task ) ((Task)edge).ResetDurationData();
			foreach ( Edge childEdge in edge.ChildEdges ) {
				RecursivelyResetAllDurationStats(childEdge);
			}
		}
        #endregion

		/// <summary>
		/// Gets the time at which this task began in the specified GraphContext.
		/// </summary>
		/// <param name="graphContext">The context in which the given start time is desired.</param>
		/// <returns>The time at which this task began in the specified GraphContext.</returns>
		public DateTime GetStartTime(IDictionary graphContext){
			object tmp = graphContext[m_selfStartTimeKey];
            return tmp==null?DateTime.MinValue:(DateTime)tmp;
        }

		/// <summary>
		/// Records the time that this task started execution.
		/// </summary>
		/// <param name="graphContext">The graphContext in which the task's execution took place.</param>
		/// <param name="startTime">The time that the task began.</param>
		/// <param name="clearFinishTime">True if caller wants the finish time to be cleared out (as it would be at the start of a new run.)</param>
		public void RecordStartTime(IDictionary graphContext, DateTime startTime, bool clearFinishTime){
			if ( graphContext.Contains(m_selfStartTimeKey) ) {
				graphContext.Remove(m_selfStartTimeKey);
				graphContext.Remove(m_selfFinishTimeKey);
			}
			graphContext.Add(m_selfStartTimeKey, startTime);
		}

		/// <summary>
		/// Gets the time at which this task completed in the specified GraphContext.
		/// </summary>
		/// <param name="graphContext">The context in which the given finish time is desired.</param>
		/// <returns>The time at which this task completed in the specified GraphContext.</returns>
        public DateTime GetFinishTime(IDictionary graphContext){
			object tmp = graphContext[m_selfFinishTimeKey];
			return tmp==null?DateTime.MinValue:(DateTime)tmp;
        }

		/// <summary>
		/// Records the time that this task completed execution.
		/// </summary>
		/// <param name="graphContext">The graphContext in which the task's execution took place.</param>
		/// <param name="finishTime">The time that the task finished.</param>
		public void RecordFinishTime(IDictionary graphContext, DateTime finishTime){
			graphContext.Add(m_selfFinishTimeKey, finishTime);
		}

		/// <summary>
		/// Returns the duration of this task in the specified GraphContext.
		/// </summary>
		/// <param name="graphContext">The context in which the duration is desired.</param>
		/// <returns>The duration of this task in the specified GraphContext.</returns>
		public TimeSpan GetRecordedDuration(IDictionary graphContext){
			return (GetFinishTime(graphContext)-GetStartTime(graphContext));
		}

        #region Rebroadcasting dynamic Edge Events as Task Events
        private void OnEdgeExecutionStartingEvent(IDictionary graphContext, Edge edge){
            if ( TaskExecutionStartingEvent != null ) TaskExecutionStartingEvent(graphContext,this);
        }
        private void OnEdgeExecutionFinishingEvent(IDictionary graphContext, Edge edge){
            if ( TaskExecutionFinishingEvent != null ) TaskExecutionFinishingEvent(graphContext,this);
        }
        private void OnEdgeStartingEvent(IDictionary graphContext, Edge edge){
            if ( TaskStartingEvent != null ) TaskStartingEvent(graphContext,this);
            if ( m_keepingTimingData ) {
				RecordStartTime(graphContext,m_model.Executive.Now,true);
            }
        }
        private void OnEdgeFinishingEvent(IDictionary graphContext, Edge edge){
			//Trace.WriteLine("Running OnEdgeFinishingEvent for " + this.Name + " at " + m_model.Executive.Now.ToString());
			if ( m_keepingTimingData ) {
                if ( m_delaysExplicitlySet ) ResetDurationData();
                DateTime startTime = (DateTime)graphContext[m_selfStartTimeKey];
                DateTime finishTime = m_model.Executive.Now;
                RecordFinishTime(graphContext,finishTime);
                TimeSpan duration = finishTime-startTime;
                UpdateDurationStats(duration);
            }
            if ( TaskFinishingEvent != null ) TaskFinishingEvent(graphContext,this);
        }
        #endregion

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance. This method calls _PopulateClone.
        /// </returns>
        public override object Clone(){
            object clone = new Task(Model,CloneName,Guid.NewGuid());
            return _PopulateClone(clone);
        }

        /// <summary>
        /// Gets the root task of the graph to which this edge belongs.
        /// </summary>
        /// <returns>The root task.</returns>
        public Task GetRootTask(){
            Task task = this;
            while ( task.ParentEdge is Task ) task = (Task)task.ParentEdge;
            return task;
        }

        /// <summary>
        /// Called when edge execution is to begin. This is the method that subclasses of this class implement to hold
        /// application code.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        /// <param name="theEdge">The edge - this. This allows multiple edges and edge types to call library code.</param>
        /// <param name="eecs">The EdgeExecutionCompletionSignaler.</param>
        protected virtual void OnEdgeExecution(IDictionary graphContext, Edge theEdge, EdgeExecutionCompletionSignaler eecs){

			if ( graphContext.Contains(EecsKey) ) {
				// TODO: Place this into an Errors & Warnings collection on the model.
				Trace.WriteLine("ERROR : EECSKey was already in the graphContext for " + Name + "." + Environment.NewLine +
					"It will be removed, but this is a problem that should be investigated and addressed.");
				graphContext.Remove(EecsKey);
			}
            // This typically means that a task has begun that, the last time it ran, did not complete.
			graphContext.Add(EecsKey,eecs);

			DoTask(graphContext);
        }

		/// <summary>
		/// This event is fired when the task becomes valid.
		/// </summary>
        public event StaticTaskEvent TaskBecameValidEvent;
		/// <summary>
		/// This event is fired when the task becomes invalid.
		/// </summary>
        public event StaticTaskEvent TaskBecameInvalidEvent;

        /// <summary>
        /// Either removes a <see cref="T:Ligature"/> between the provided edge's postVertex and this one's PreVertex,
        /// removing the provided edge as a predecessor to this one. If the provided edge is a <see cref="T:Ligature"/>, then
        /// the ligature itself is disconnected from this edge. This API also interacts with a
        /// <see cref="T:Highpoint.Sage.Graphs.Validity.ValidationService"/> to enable it to correctly
        /// manage graph validity state.
        /// </summary>
        /// <param name="preEdge">The pre edge.</param>
		public override void RemovePredecessor(Edge preEdge) {
			base.RemovePredecessor (preEdge);
			foreach ( Edge edge in ChildEdges ) {
				if ( edge is Task ) ((Task)edge).SelfValidState = false;
			}
		}

        /// <summary>
        /// Performs the processing that is embodied in this task object. This method is called at the simulation
        /// time that the task is to begin, and the call returns once the processing has completed.<para></para>
        /// <u>It is imperative that a developer, overriding this method, ensure that they call <see cref="T:Highpoint.Sage.Graphs.Tasks.Task#SignalTaskCompletion"/></u>
        /// </summary>
        /// <param name="graphContext">The graph context in which the execution is to proceed.</param>
        protected virtual void DoTask(IDictionary graphContext){
            SignalTaskCompletion(graphContext);
        }

        /// <summary>
        /// Signals to the engine that the task has completed all of its execution, and that execution of the next task may be notified that this one has completed.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        protected void SignalTaskCompletion(IDictionary graphContext){

			if ( s_diagnostics ) Trace.WriteLine(Name + " is completing - it's validity state is (VS=" + ValidityState + "/SVS=" + SelfValidState + "/UVS=" + AllUpstreamValid + "/CVS=" + AllChildrenValid + ")");
            EdgeExecutionCompletionSignaler eecs = (EdgeExecutionCompletionSignaler)graphContext[EecsKey];
            if ( eecs != null ) {
                graphContext.Remove(EecsKey);
                eecs(graphContext);
            } else {
				throw new ApplicationException(String.Format(s_eecsExceptionMessage, Name));
            }
        }

        /// <summary>
        /// Gets the child tasks of this task.
        /// </summary>
        /// <param name="shallow">if set to <c>true</c>, only gets children. Otherwise, gets all descendants.</param>
        /// <returns></returns>
        public ArrayList GetChildTasks(bool shallow){
            ArrayList kids = new ArrayList();
            GetChildTasks(ref kids, shallow);
            return kids;
        }

        private void GetChildTasks(ref ArrayList kidTasks, bool shallow){
            foreach ( Edge edge in ChildEdges ) {
                if ( !shallow ) ((Task)edge).GetChildTasks(ref kidTasks,shallow);
                if ( edge is Task ) kidTasks.Add(edge);
            }
        }

		#region Validity Support

        /// <summary>
        /// Gets a value indicating whether this task is valid overall - meaning that it, all of its children and all of its predecessors are themselves valid.
        /// </summary>
        /// <value><c>true</c> if this task is valid overall; otherwise, <c>false</c>.</value>
        public bool ValidityState { 
			get { 
				if ( ValidationService == null ) 
					return m_bSelfStateValid;
				else 
					return ValidationService.GetValidityState(this)==Graphs.Validity.Validity.Valid; 
			} 
		}

        /// <summary>
        /// Gets or sets a value indicating whether this task's state is appropriate to call this task, irrespective of predecessors and children, valid.
        /// </summary>
        /// <value><c>true</c> if this task's state is appropriate to call this task valid; otherwise, <c>false</c>.</value>
		public bool SelfValidState { 
			get { return m_bSelfStateValid; } 
			set {
				if ( value == m_bSelfStateValid ) return;
				m_bSelfStateValid = value;
				if ( ValidationService != null ) {
					ValidationService.NotifySelfStateChange(this);
					// ValidationService will get us to fire the ValidityChangeEvent.
				} else {
					// Without a validity service, its overall validity is its self-validity.
					NotifyOverallValidityChange(m_bSelfStateValid?Graphs.Validity.Validity.Valid:Graphs.Validity.Validity.Invalid);
				}
			} 
		}

        /// <summary>
        /// Gets or sets the state (from a perspective of validity) of the implementer.
        /// </summary>
        /// <value>The state of the self.</value>
		public override Graphs.Validity.Validity SelfState {
			get { return (SelfValidState?Graphs.Validity.Validity.Valid:Graphs.Validity.Validity.Invalid); }
			set { SelfValidState = (value==Graphs.Validity.Validity.Valid); }
		}

        /// <summary>
        /// Gets the children (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		public override IList GetChildren() { return ChildEdges; }

        /// <summary>
        /// Gets a value indicating whether [all upstream valid].
        /// </summary>
        /// <value><c>true</c> if [all upstream valid]; otherwise, <c>false</c>.</value>
		public bool AllUpstreamValid { 
			get { 
				if ( ValidationService == null ) return false; // If no VM, then must assume something - assuming false.
				else return ValidationService.GetPredecessorValidityState(this)==Graphs.Validity.Validity.Valid;
			} 
		}

        /// <summary>
        /// Gets a value indicating whether [all children valid].
        /// </summary>
        /// <value><c>true</c> if [all children valid]; otherwise, <c>false</c>.</value>
		public bool AllChildrenValid { 
			get { 
				if ( ValidationService == null ) {
					foreach ( Edge edge in ChildEdges ) {
						if ( edge is Task ) if (!((Task)edge).ValidityState) return false;
					}
					return true;
				} else {
					return ValidationService.GetChildValidityState(this)==Graphs.Validity.Validity.Valid; 
				}
			} 
		}

		private void MyStructureChangedHandler(object obj, StructureChangeType sct, bool isPropagated) {
			switch (sct ) {
				case StructureChangeType.AddPreEdge:    SelfValidState = false; break;
				case StructureChangeType.RemovePreEdge: SelfValidState = false; break;
				case StructureChangeType.AddCostart:    SelfValidState = false; break;
				case StructureChangeType.RemoveCostart: SelfValidState = false; break;
				case StructureChangeType.AddPostEdge:							break;
				case StructureChangeType.RemovePostEdge:						break;
				case StructureChangeType.AddCofinish:							break;
				case StructureChangeType.RemoveCofinish:						break;
				case StructureChangeType.AddChildEdge:							break;
				case StructureChangeType.RemoveChildEdge:						break;
				case StructureChangeType.NewSynchronizer: SelfValidState = false; break;
				case StructureChangeType.Unknown: SelfValidState = false; if ( s_diagnostics ) Debugger.Break(); break;
				default: throw new ApplicationException("Unknown StructureChangeType, " + sct + ", referenced.");
					
			}
		}

		private void Task_ValidityChangeEvent(Graphs.Validity.IHasValidity ihv, Graphs.Validity.Validity newState) {
			if ( newState == Graphs.Validity.Validity.Valid ) {
				if ( TaskBecameValidEvent != null ) TaskBecameValidEvent(this);
			} else if ( newState == Graphs.Validity.Validity.Invalid ) {
				if ( TaskBecameInvalidEvent != null ) TaskBecameInvalidEvent(this);
			}
		}

        #endregion

        #region Implementation of IModelObject
        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value>The Guid</value>
        public Guid Guid => m_guid;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => m_model;
        #endregion

        #region IXmlPersistable Members
        /// <summary>
		/// Default constructor for serialization only.
		/// </summary>
		public Task(){}

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public override void SerializeTo(XmlSerializationContext xmlsc) {
			base.SerializeTo(xmlsc);
			xmlsc.StoreObject("Guid",m_guid);
			xmlsc.StoreObject("KeepingTimingData",m_keepingTimingData);
			/*xmlsc.StoreObject("MinDelay",m_minDelay);
			xmlsc.StoreObject("MaxDelay",m_maxDelay);
			xmlsc.StoreObject("AggDelay",m_aggDelay);
			xmlsc.StoreObject("DelaysExplicitlySet",m_delaysExplicitlySet);
			xmlsc.StoreObject("NumDelays",m_numDelays);
			xmlsc.StoreObject("ExplicitlySet",m_delaysExplicitlySet);*/

		}

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public override void DeserializeFrom(XmlSerializationContext xmlsc) {
			m_model = (Model)xmlsc.ContextEntities["Model"];
			m_guid = (Guid)xmlsc.LoadObject("Guid");
			
			base.DeserializeFrom(xmlsc);
			
			m_keepingTimingData = (bool)xmlsc.LoadObject("KeepingTimingData");

			#region >>> Do what else is in the constructor. <<<
			ExecutionDelegate = new EdgeExecutionDelegate(OnEdgeExecution);

			EdgeStartingEvent           += new EdgeEvent(OnEdgeStartingEvent);
			EdgeExecutionStartingEvent  += new EdgeEvent(OnEdgeExecutionStartingEvent);
			EdgeExecutionFinishingEvent += new EdgeEvent(OnEdgeExecutionFinishingEvent);
			EdgeFinishingEvent          += new EdgeEvent(OnEdgeFinishingEvent);

			StructureChangeHandler		+=new StructureChangeHandler(MyStructureChangedHandler);

			ValidityChangeEvent			+=new Graphs.Validity.ValidityChangeHandler(Task_ValidityChangeEvent);

			ResetDurationData();

			#endregion

		}

		#endregion

		private static readonly string s_eecsExceptionMessage = 
            @"Edge Execution Completion Signaler was null in task {0}. This is usually a " +
			"result of the execution immediately preceding this one, for this graphContext, " +
			"having been interrupted in a non-standard way. There is a good possibility " +
			"that the graphContext was left in an incongruent state, by that prior termination, " +
			"and therefore this execution cannot be allowed to proceed.";

	}
}
