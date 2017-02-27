/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.Resources;
using Highpoint.Sage.Utility;

#if CREATION_CONTEXTS
using Highpoint.Sage.DynamicConstruction;
#endif


namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// This delegate is implemented by any method that wishes to be called back when a task
    /// processor event, such as 'TaskProcessorAdded' and 'TaskProcessorRemoved' is fired.
    /// </summary>
    public delegate void TaskProcessorListener(object subject, TaskProcessor taskProcessor);
    /// <summary>
    /// This delegate is implemented by any method that wishes to be called back when a model
    /// is, for example, starting or stopping.
    /// </summary>
    public delegate void ModelEvent(IModel theModel);
    /// <summary>
    /// The two default states for a model. The Model's state machine can be replaced with a
    /// custom one, but these are the states of the default state machine.
    /// </summary>
    public enum DefaultModelStates { 
        /// <summary>
        /// The state machine is idle.
        /// </summary>
        Idle,
        /// <summary>
        /// The state machine is running.
        /// </summary>
        Running
    }

    /// <summary>
    /// The base class from which models are built or derived. Models provide a state machine,
    /// error and warning management, task processor management (for Task Graphs) and access
    /// and control functions to the Executive that is running the simulation embodied in this
    /// model.
    /// </summary>
    public class Model : IModel, IModelWithResources {

        /// <summary>
        /// The executive that this model is using.
        /// </summary>
        protected IExecutive Exec;

        #region Private fields
        private static int _modelCounter = 0;
        private StateMachine m_stateMachine;
        private readonly Hashtable m_taskProcessors;
        private readonly IDictionary m_parameters;
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Model");
        private static readonly bool s_dumpWarnings = Diagnostics.DiagnosticAids.Diagnostics("ModelWarnings");
        private static readonly bool s_dumpErrors = Diagnostics.DiagnosticAids.Diagnostics("ModelErrors");
        private static readonly bool s_managePostMortemData = Diagnostics.DiagnosticAids.Diagnostics("Graph.KeepPostMortems");
        private ulong m_randomSeed = ulong.MaxValue;
        private bool m_randomSeedSpecified = false;
        private Randoms.RandomServer m_randomServer = null;
        private ModelConfig m_modelConfig;
        private ExecController m_execController;

        #endregion

                #region Constructors
                /// <summary>
                /// Creates a model with a default name ('Model1', 'Model2', etc.) and self-declared Guid.
                /// </summary>
        public Model():this("Model"+(_modelCounter++),Guid.NewGuid()){}
        
        /// <summary>
        /// Creates a model with a specified name, and self-declared Guid.
        /// </summary>
        /// <param name="name">The name for the new model.</param>
        public Model(string name):this(name,Guid.NewGuid()){}

        /// <summary>
        /// Creates a model with a specified name and Guid.
        /// </summary>
        /// <param name="name">The name for the new model.</param>
        /// <param name="guid">The guid for the new model.</param>
        public Model(string name, Guid guid){
            m_name = name;
            m_guid = guid;

            IsRunning = false;
            IsPaused = false;
            IsCompleted = false;
            IsReady = false;

            m_services = new Dictionary<Type, Dictionary<string, object>>();

            Exec = CreateModelExecutive();
            m_stateMachine = CreateStateMachine();

            m_taskProcessors = new Hashtable();
#if CREATION_CONTEXTS
            m_creationContexts = new WeakList();
#endif
            m_parameters = new Hashtable();

            if ( s_dumpErrors ) {
                ErrorHappened+=new ErrorEvent(Model_ErrorHappened);
                ErrorCleared+=new ErrorEvent(Model_ErrorCleared);
            }
            if ( s_dumpWarnings ) {
                WarningHappened+=new WarningEvent(Model_WarningHappened);
            }

            m_modelConfig = new ModelConfig();
            m_modelObjects = new ModelObjectDictionary();
        }
        #endregion

        #region >>> Manage Model Objects <<<
        private ModelObjectDictionary m_modelObjects;
        /// <summary>
        /// A dictionary of currently live IModelObjects. An IModelObject that is garbage-
        /// collected is automatically removed from this collection. Note that the object
        /// is not necessarily removed at the time of last release, but at the time of
        /// garbage collection. Code can call Remove(...) to explicitly remove the object.
        /// </summary>
        public ModelObjectDictionary ModelObjects { get { return m_modelObjects; } }

        public void AddModelObject(IModelObject modelObject) {
            m_modelObjects.Add(modelObject.Guid,modelObject);
        }

        #endregion

        #region >>> Manage Random Server <<<
        /// <summary>
        /// Gets the random server in use by this model.
        /// </summary>
        /// <value>The random server.</value>
        public Randoms.RandomServer RandomServer
        {
            get
            {
                if (m_randomServer == null) m_randomServer = new Randoms.RandomServer();
                return m_randomServer;
            }
            set { m_randomServer = value; }
        }

        /// <summary>
        /// Gets the random seed in use by this model.
        /// </summary>
        /// <value>The random seed in use by this model.</value>
        public ulong RandomSeed
        {
            get
            {
                if (!m_randomSeedSpecified)
                {
                    byte[] ba = new byte[8];
                    new Random().NextBytes(ba);
                    for (int i = 0; i < 8; i++) { m_randomSeed += ba[i]; m_randomSeed <<= 8; }
                    m_randomSeedSpecified = true;
                }
                return m_randomSeed;
            }

            set
            {
                if (m_randomServer == null)
                {
                    m_randomSeed = value;
                    m_randomSeedSpecified = true;
                }
                else
                {
                    throw new ApplicationException(_msgReseedingInitializedRandomServer);
                }
            }
        } 
        #endregion

        /// <summary>
        /// The ModelConfig is an object that holds the contents of the Sage® section of the
        /// app.config file.
        /// </summary>
        public ModelConfig ModelConfig { get { return m_modelConfig; } }


        /// <summary>
        /// Gets the executive controller that governs the rate-throttling and frame-rendering event frequency of this model.
        /// </summary>
        /// <value>The executive controller.</value>
        public ExecController ExecutiveController {
            get { return m_execController; }
            set
            {
                if (m_execController == null)
                {
                    m_execController = value;
                    m_execController.SetExecutive(this.Exec);
                }
                else
                {
                    throw new InvalidOperationException("Substitution of a model's ExecController with another is not supported.");
                }
            }
        }

        /// <summary>
        /// Provides access to the executive being used by this model.
        /// </summary>
        public IExecutive Executive { get { return Exec; } }

        /// <summary>
        /// Provides access to the state machine being used by this model. While the state machine
        /// can be set, too, this is an advanced feature, and should not be done unless the developer
        /// is sure what they are doing.
        /// </summary>
        public StateMachine StateMachine {
            get { return m_stateMachine; }
            set { 
                m_stateMachine = value;
                m_stateMachine.SetModel(this);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value><c>true</c> if this instance is running; otherwise, <c>false</c>.</value>
        public bool IsRunning { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is paused.
        /// </summary>
        /// <value><c>true</c> if this instance is paused; otherwise, <c>false</c>.</value>
        public bool IsPaused { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is completed.
        /// </summary>
        /// <value><c>true</c> if this instance is completed; otherwise, <c>false</c>.</value>
        public bool IsCompleted { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is ready to be started.
        /// </summary>
        /// <value><c>true</c> if this instance is ready to be started; otherwise, <c>false</c>.</value>
        public bool IsReady { get; set; }

        /// <summary>
        /// Called during the creation of the model, this method creates the executive. It is intended
        /// to be overridden in derived classes if the designer wishes to create a new and different
        /// executive instead of the standard executive.
        /// </summary>
        /// <returns>The executive to be used by this model.</returns>
        protected virtual IExecutive CreateModelExecutive(){
            return ExecFactory.Instance.CreateExecutive();
        }

        #region >>> Resource Management <<<
        /// <summary>
        /// Must be called by the creator when a new resource is created.
        /// </summary>
        /// <param name="resource">The resource.</param>
        public void OnNewResourceCreated(IResource resource){
            if ( s_diagnostics ) Trace.WriteLine("Created " + resource.Name + ", which is of type " + resource.GetType().Name);
            if ( ResourceCreatedEvent != null ) ResourceCreatedEvent(resource);
        }

        /// <summary>
        /// Event that is fired when a new resource has been created.
        /// </summary>
        public event ResourceEvent ResourceCreatedEvent;

        #endregion

        /// <summary>
        /// Gets the parameters dictionary, a free-form dictionary of model-wide parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public IDictionary Parameters {
            get {
                return m_parameters;
            }
        }

        public virtual void Dispose() {
            Exec.Dispose();
            if (ExecutiveController != null) {
                ExecutiveController.Dispose();
            }
        }
        
        #region >>> Error and Warning Management <<<

        private ArrayList m_warnings = new ArrayList();
        /// <summary>
        /// Fired when a warning is added to the model.
        /// </summary>
        public event WarningEvent WarningHappened;
        /// <summary>
        /// An enumeration of all of the warnings currently applicable to this model.
        /// </summary>
        public ICollection Warnings { get { return m_warnings; } }
        /// <summary>
        /// Adds a warning to this model, e.g. a 'GenericModelWarning'...
        /// </summary>
        /// <param name="theWarning">The warning to be added.</param>
        public void AddWarning(IModelWarning theWarning){
            if ( WarningHappened != null ) WarningHappened(theWarning);
            m_warnings.Add(theWarning);
        }
        /// <summary>
        /// Returns true if this model has any active warnings.
        /// </summary>
        /// <returns>Returns true if this model has any active warnings - otherwise, false.</returns>
        public bool HasWarnings() { return m_warnings.Count != 0; }
        /// <summary>
        /// Clears all of the warnings applicable to this model.
        /// </summary>
        public void ClearAllWarnings(){
            m_warnings.Clear();
        }

        protected HashtableOfLists m_errors = new HashtableOfLists();
        protected ArrayList ErrorHandlers = new ArrayList();
        
        /// <summary>
        /// Fired when an error happens in (is added to) a model.
        /// </summary>
        public event ErrorEvent ErrorHappened;
        /// <summary>
        /// Fired when an error is removed from a model.
        /// </summary>
        public event ErrorEvent ErrorCleared;

        #region ErrorHandlers
        /// <summary>
        /// Enables a user/developer to add an error handler to the model in real time,
        /// (e.g. during a simulation run) and ensures that that handler is called for
        /// any errors currently in existence in the model.
        /// </summary>
        /// <param name="theErrorHandler">The error handler delegate that is to receive notification of the error events.</param>
        public void AddErrorHandler(IErrorHandler theErrorHandler){
            foreach ( IModelError error in m_errors ) {
                if ( theErrorHandler.HandleError(error) ) {
                    m_errors.Remove(error.Target,error);
                    break;
                }
            }
            ErrorHandlers.Add(theErrorHandler);
        }
        /// <summary>
        /// Removes an error handler from the model.
        /// </summary>
        /// <param name="theErrorHandler">The error handler to be removed from the model.</param>
        public void RemoveErrorHandler(IErrorHandler theErrorHandler){
            ErrorHandlers.Remove(theErrorHandler);
        }
        #endregion

        /// <summary>
        /// An enumeration over all of the errors in the model.
        /// </summary>
        public ICollection Errors {
            get {
                ArrayList retval = new ArrayList();
                foreach ( IModelError ime in m_errors ) retval.Add(ime);
                return retval; 
            } 
        }

        /// <summary>
        /// Adds an error to the model, and iterates over all of the error handlers,
        /// allowing each in turn to respond to the error. As soon as any errorHandler
        /// indicates that it has HANDLED the error (by returning true from 'HandleError'),
        /// the error is cleared, and further handlers are not called.
        /// </summary>
        /// <param name="theError">The error that is to be added to the model's error list.</param>
        /// <returns>True if the error was successfully added to the model, false if it was cleared by a handler.</returns>
        public bool AddError(IModelError theError){
            foreach ( IErrorHandler errorHandler in ErrorHandlers ) {
                if ( errorHandler.HandleError(theError) ) {
                    if ( ErrorCleared != null ) ErrorCleared(theError);
                    return false;
                }
            }

            if ( theError.Target == null ) throw new ApplicationException("An error was added to the model with its target set to null. This is illegal.");
            m_errors.Add(theError.Target,theError);
            if ( ErrorHappened != null ) ErrorHappened(theError);

            // IF WE ARE TRANSITIONING, We are not going to abort on the
            // addition of an error, since there may be intermediate situations
            // where an error is okay. If a 'fail on the first sign of an error'
            // behavior is desired, then the developer can hook the
            // 'ErrorHappened' event. Instead, we will have the model register a
            // handler as the last thing done on attempting a transition, to
            // check that we are error-free. But, IF WE ARE NOT TRANSITIONING,
            // then anything that causes an error will abort the model, and move
            // it back to the 'Idle' state.
            if ( !StateMachine.IsTransitioning ) {
                if ( s_diagnostics ) Trace.WriteLine("Model.Abort() requested as a result of the addition of an error : " + theError);
                if ( !StateMachine.State.Equals(GetAbortEnum()) ) StateMachine.DoTransition(GetAbortEnum());
            }
            return true;
        }

        /// <summary>
        /// Adds a handler that, after transition to a specified state, will check the
        /// model for errors and if it finds any, will abort the model, putting it back to idle.
        /// </summary>
        /// <param name="onTransitionToWhichState">The model state in which the model is to be checked for errors.</param>
        public void AddErrorCheckHandlerWithModelAbortOnFailure(Enum onTransitionToWhichState){
            m_stateMachine.InboundTransitionHandler(onTransitionToWhichState).AddCommitEvent(new CommitTransitionEvent(AbortIfErrors),double.MaxValue);
        }

        private void AbortIfErrors(IModel model, object userData) {
            if ( model.HasErrors()) model.Abort();
        }

        /// <summary>
        /// Removes ModelErrors whose 'AutoClear' value is set to true. Typically applied to errors 
        /// that do not persist from run to run - i.e. that are run time, not configuration, errors.
        /// </summary>
        public void RemoveAutoclearedErrors() {
            ArrayList keysToClear = new ArrayList();
            foreach (IModelError err in m_errors) {
                if (err.Target is Task) {
                    if (s_diagnostics)
                        Trace.WriteLine("Checking error " + err.Narrative + ", targeted to " + ( (Task)err.Target ).Name);
                    if (err.AutoClear) {
                        if (s_diagnostics)
                            Trace.WriteLine("Clearing error " + err.Name);
                        keysToClear.Add(err.Target);
                    }
                }
            }

            foreach (object key in keysToClear)
                m_errors.Remove(key);

        }

        /// <summary>
        /// Removes the error from the model's collection of errors.
        /// </summary>
        /// <param name="theError">The error to be removed from the model.</param>
        public void RemoveError(IModelError theError){
            if ( s_diagnostics ) Trace.WriteLine("Removing error " + theError.Narrative);
            m_errors.Remove(theError.Target,theError);
            if ( ErrorCleared != null ) ErrorCleared(theError);
        }

        /// <summary>
        /// Removes all errors whose target is the specified object.
        /// </summary>
        /// <param name="target">The object for whom all errors are to be removed.</param>
        public void ClearAllErrorsFor(object target) {
            m_errors.Remove(target);
        }

        /// <summary>
        /// Removes all errors.
        /// </summary>
        public void ClearAllErrors() {
            m_errors.Clear();
        }

        /// <summary>
        /// Returns true if the model has errors.
        /// </summary>
        /// <returns>true if the model has errors.</returns>
        public bool HasErrors(){ return (m_errors.Count != 0);  }
        /// <summary>
        /// Provides a string that summarizes all of the errors currently active in this model.
        /// </summary>
        public string ErrorSummary { 
            get {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach ( object obj in m_errors ) {
                    sb.Append(obj);
                    sb.Append("\r\n");
                }
                return sb.ToString();
            }
        }

        private void Model_ErrorHappened(IModelError modelError) {
            Trace.WriteLine(Executive.Now + " : ERROR HAPPENED : " + modelError.Narrative);
        }

        private void Model_WarningHappened(IModelWarning modelWarning) {
            Trace.WriteLine(Executive.Now + " : WARNING HAPPENED : " + modelWarning.Narrative);
        }

        private void Model_ErrorCleared(IModelError modelError) {
            Trace.WriteLine(Executive.Now + " : ERROR CLEARED : " + modelError.Narrative);
        }
        
        #endregion

        #region >>> State Management <<<
        /// <summary>
        /// Creates the state machine to be used by this model. Called by the framework, 
        /// and intended to be overridden by a derived class.
        /// </summary>
        /// <returns></returns>
        protected virtual StateMachine CreateStateMachine(){
            bool[,] transitionMatrix = {{false,true},{true,false}};
            Enum[] followOnStates = {DefaultModelStates.Idle,DefaultModelStates.Idle};
            StateMachine retval = new StateMachine(this,transitionMatrix,followOnStates,DefaultModelStates.Idle);
            retval.SetStateMethod(RunModel,DefaultModelStates.Running);
            retval.TransitionHandler(DefaultModelStates.Running, DefaultModelStates.Idle).Commit += (model, data) => Completed?.Invoke(this);
            return retval;
        }

        /// <summary>
        /// Returns the enumeration that represents the resultant state of the START transition in this model's state machine.
        /// <p><b>NOTE:</b></p>
        /// This method, when added to a derived class, needs to ne 'new'ed, not overridden. 
        /// </summary>
        /// <returns>The enumeration that represents the resultant state of the START transition in this model's state machine.</returns>
        public virtual Enum GetStartEnum(){ 
            return DefaultModelStates.Running;
        }

        /// <summary>
        /// Returns the enumeration that represents the resultant state of the ABORT transition in this model's state machine.
        /// </summary>
        /// <p><b>NOTE:</b></p>
        /// This method, when added to a derived class, needs to ne 'new'ed, not overridden. 
        /// <returns>The enumeration that represents the resultant state of the ABORT transition in this model's state machine.</returns>
        public virtual Enum GetAbortEnum(){
            return DefaultModelStates.Idle;
        }

        /// <summary>
        /// Returns the enumeration that represents the state of this model's state machine from which START is a legal transition.
        /// </summary>
        /// <p><b>NOTE:</b></p>
        /// This method, when added to a derived class, needs to be 'new'ed, not overridden. 
        /// <returns>The enumeration that represents the state of this model's state machine from which START is a legal transition.</returns>
        public virtual Enum GetIdleEnum(){
            return DefaultModelStates.Idle;
        }

        //public Enum GetStopEnum(){ return DefaultModelStates.Idle; }
        /// <summary>
        /// Starts the model.
        /// </summary>
        public virtual void Start() {
            if ( s_diagnostics ) Trace.WriteLine("Model.Start() requested.");
            IsRunning = true;
            IsReady = false;
            IsPaused = false;
            IsCompleted = false;
            m_stateMachine.DoTransition(GetStartEnum());
        }

        /// <summary>
        /// Pauses execution of this model after completion of the running callback of the current event.
        /// </summary>
        public virtual void Pause()
        {
            if (s_diagnostics) Trace.WriteLine("Model.Pause() requested.");
            if (IsRunning)
            {
                IsRunning = true;
                IsReady = false;
                IsPaused = true;
                IsCompleted = false;
            }
            Exec.Pause();
        }

        /// <summary>
        /// Resumes execution of this model. Ignored if the model is not already paused.
        /// </summary>
        public virtual void Resume()
        {
            if (s_diagnostics) Trace.WriteLine("Model.Resume() requested.");
            if (IsPaused)
            {
                IsRunning = true;
                IsReady = false;
                IsPaused = false;
                IsCompleted = false;
            }

            Exec.Resume();
        }

        /// <summary>
        /// Aborts the model.
        /// </summary>
        public virtual void Abort(){
            if ( s_diagnostics ) Trace.WriteLine("Model.Abort() requested.");
            Exec.Stop();
            m_stateMachine.DoTransition(GetAbortEnum());
            if (!IsReady)
            {
                IsRunning = false;
                IsReady = false;
                IsPaused = false;
                IsCompleted = true;
            }
        }

        public virtual void Reset() {
            if (s_diagnostics) Trace.WriteLine("Model.Reset() requested.");
            if (IsCompleted)
            {
                IsRunning = false;
                IsReady = true;
                IsPaused = false;
                IsCompleted = false;
            }
            Exec.Reset();
            Debug.Assert(Exec.State.Equals(ExecState.Stopped));
            m_stateMachine.DoTransition(GetIdleEnum());
            Debug.Assert(m_stateMachine.State.Equals(GetIdleEnum()));
            Resetting?.Invoke(this);
        }
        
        /// <summary>
        /// Fired when the model has been commanded to start. Should only be used to queue up events in the executive.
        /// </summary>
        public event ModelEvent Starting;

        /// <summary>
        /// Fired when the model has been commanded to stop.
        /// </summary>
        public event ModelEvent Stopping;

        /// <summary>
        /// Fired when the model has been commanded to reset.
        /// </summary>
        public event ModelEvent Resetting;

        /// <summary>
        /// Fired when the model has completed.
        /// </summary>
        public event ModelEvent Completed;

        private readonly Dictionary<Type, Dictionary<string, object>> m_services;
        public void AddService<T>(T service, string name = null) where T : IModelService
        {
            Dictionary<string, object> typedServices;
            if (!m_services.TryGetValue(typeof(T), out typedServices))
            {
                typedServices = new Dictionary<string, object>();
                m_services.Add(typeof(T), typedServices);
            }
            typedServices.Add((name ?? ""),service);

            // Make sure that (a) the service hasn't already been initialized,
            // and that (b) the user isn't self-managing the initialization.
            // Note - a service added under a class type, and later requested
            // under a compatible interface type, will be re-added under that
            // new compatible interface, but will already have been initialized.
            if (!service.IsInitialized && service.InlineInitialization)
            {
                service.InitializeService(this);
                service.IsInitialized = true;
            }
        }

        public T GetService<T>(string name=null) where T : IModelService
        {
            bool exactTypeMatch = true;
            bool retvalFound; // is false.
            object retval;
            Dictionary<string, object> typedServices;
            if (!m_services.TryGetValue(typeof (T), out typedServices))
            {
                exactTypeMatch = false;
                foreach (Type type in m_services.Keys)
                {
                    if (typeof(T).IsAssignableFrom(type))
                    {
                        typedServices = m_services[type];
                        break;
                    }
                }
            }
            if (typedServices != null)
            {
                if (name == null)
                {
                    retvalFound = typedServices.Any();
                    retval = typedServices.First().Value;
                }
                else
                {
                    retvalFound = typedServices.TryGetValue(name, out retval);                   
                }
            }
            else
            {
                string message =
                    string.Format(
                        "IModel.GetService<{0}>({1}) called, but that type does not exist directly or in assignable form in the services dictionary.",
                        typeof (T), name ?? "");
                throw new ArgumentException(message);
            }

            if (retvalFound)
            {
                if (!exactTypeMatch)
                {
                    AddService((T)retval, name);
                }
                Debug.Assert(((T) retval).IsInitialized, string.Format("Service {0} stored under key \"{1}\" is not initialized. It must be explicitly initialized after being added to the model.", retval.GetType(), name));
                return (T) retval;
            }
            else return default(T);
        }

        /// <summary>
        /// Called by a derived class to cause this base class to fire the Model.Starting event.
        /// </summary>
        protected void FireModelStartingEvent(){
            Starting?.Invoke(this);
        }

        /// <summary>
        /// Called by a derived class to cause this base class to fire the Model.Stopping event.
        /// </summary>
        protected void FireModelStoppingEvent() {
            Stopping?.Invoke(this);
        }

        /// <summary>
        /// Called by a derived class to cause this base class to fire the Model.Completed event.
        /// </summary>
        protected void FireModelCompletedEvent() {
            Completed?.Invoke(this);
        }

        private void RunModel(IModel model, object userData) {
            //m_exec.Reset();

            FireModelStartingEvent();

            Exec.Start();

            IsRunning = false;
            IsReady = false;
            IsPaused = false;
            IsCompleted = true;

            FireModelStoppingEvent();
            FireModelCompletedEvent();

        }

        private void OnStopCommitted(IModel model){
            Exec.Stop();
            Stopping?.Invoke(this);
        }

        private ITransitionFailureReason OnStartRequested(IModel model){
            foreach ( IErrorHandler errorHandler in ErrorHandlers ) {
                errorHandler.HandleErrors(m_errors);
            }
            if ( m_errors.Count != 0 ) {
                return new SimpleTransitionFailureReason("Model has errors.",this);
            }
            return null;
        }
        #endregion

        #region >>> Implementation of IHasIdentity <<<
        private string m_name = null;
        /// <summary>
        /// The name of this model.
        /// </summary>
        public string Name {
            [DebuggerStepThrough]
            get { return m_name; }
            protected set {
                if (value != m_name) {
                    m_name = value;
                }
            }
        }

        private string m_description = null;
        /// <summary>
        /// A description of this Model.
        /// </summary>
        public string Description {
            [DebuggerStepThrough]
            get { return m_description == null ? m_name : m_description; }
            protected set { m_description = value; }
        }
        private Guid m_guid = Guid.Empty;
        /// <summary>
        /// The Guid by which this model will be known.
        /// </summary>
        public Guid Guid {
            [DebuggerStepThrough]get { return m_guid; }
            protected set { m_guid = value; }
        }
        #endregion

        #region IModelObject Members

        IModel IModelObject.Model => this;

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid"></param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            Debug.Assert(model == this);
            IModel m_model=null; // To fake out the call below, since Model doesn't have this member field.
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        #endregion

        private static string _msgReseedingInitializedRandomServer = "Attempting to set the RandomSeed value on a model that has already initialized the RandomServer. The RandomServer is initialized the first time it is accessed, so make sure that if you want to set the seed, it is done before that time.";

    }
}