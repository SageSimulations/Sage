/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.SinksAndSources;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.Resources;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Highpoint.Sage.Persistence;

namespace Highpoint.Sage.ItemBased.Queues {

    /// <summary>
    /// Summary description for zTestQueues.
    /// </summary>
    [TestClass]
    public class QueueTester {

        #region MSTest Goo
        [TestInitialize]
        public void Init() { }
        [TestCleanup]
        public void destroy() {
            Trace.WriteLine("Done.");
        }
        #endregion

        private Model m_model;

        public QueueTester() { }

        [TestMethod]
        public void TestQueueBasics() {

            m_model = new Model();
            m_model.RandomServer = new Randoms.RandomServer(54321, 100);

            ItemSource itemFactory = CreateItemGenerator("Item_", 500, 5.0, 3.0);
            IQueue q1 = new Queue(m_model, "Queue1", Guid.NewGuid(), 10);
            IQueue q2 = new Queue(m_model, "Queue2", Guid.NewGuid(), 10);
            IQueue q3 = new Queue(m_model, "Queue3", Guid.NewGuid(), 10);

            ConnectorFactory.Connect(itemFactory.Output, q1.Input);
            ConnectorFactory.Connect(q1.Output, q2.Input);
            ConnectorFactory.Connect(q2.Output, q3.Input);

            itemFactory.Output.PortDataPresented += new PortDataEvent(Output_PortDataPresented);

            q1.LevelChangedEvent += new QueueLevelChangeEvent(Queue_LevelChangedEvent);
            q2.LevelChangedEvent += new QueueLevelChangeEvent(Queue_LevelChangedEvent);
            q3.LevelChangedEvent += new QueueLevelChangeEvent(Queue_LevelChangedEvent);

            q3.Output.PortDataPresented += new PortDataEvent(Output_PortDataPresented);

            m_model.Start();


        }

#if ORACLE_CLIENT
        [TestMethod]
        public void TestQueueSerialization() {
            Guid qg1 = Guid.NewGuid();
            Guid qg2 = Guid.NewGuid();
            Guid qg3 = Guid.NewGuid();
            Guid gc1 = Guid.NewGuid();
            Guid gc2 = Guid.NewGuid();

            DIModel model = new DIModel();
            model.Initialize(model, "Test Model", "Description of test model", Guid.NewGuid(), 0L, new Guid[] { qg1, qg2, qg3, gc1, gc2});
            model.InitializationManager.InitializationBeginning += new InitializationEvent(InitializationManager_InitializationBeginning);
            model.InitializationManager.InitializationCompleted += new InitializationEvent(InitializationManager_InitializationCompleted);

            new Queue().Initialize(model, "Queue 1", null, qg1, 100);
            new Queue().Initialize(model, "Queue 2", null, qg2, 100);
            new Queue().Initialize(model, "Queue 3", null, qg3, 100);

            IQueue q1 = (IQueue)model.ModelObjects[qg1];
            IQueue q2 = (IQueue)model.ModelObjects[qg2];
            IQueue q3 = (IQueue)model.ModelObjects[qg3];

            new Connectors.BasicNonBufferedConnector().Initialize(model, "Q1->Q2", null, gc1, qg1, "Output", qg2, "Input");
            new Connectors.BasicNonBufferedConnector().Initialize(model, "Q2->Q3", null, gc2, qg2, "Output", qg3, "Input");

            XmlConstructionRecorder recorder = new XmlConstructionRecorder();
            recorder.Attach(model);
            model.Start();
            string modelXmlString = (string)recorder.Read();
            recorder.Detach();

            string model1String = GetModelObjectDumpString(model);

            XmlConstructionLoader loader = new XmlConstructionLoader();

            DIModel model2 = (DIModel)loader.CreateModel(modelXmlString);

            string model2String = GetModelObjectDumpString(model2);

            Console.WriteLine(model1String);
            Console.WriteLine(model2String);
            System.Diagnostics.Debug.Assert(model2String.Equals(model1String));
        }
#endif
        private string GetModelObjectDumpString(IModel model) {
            List<IModelObject> modelObjects = new List<IModelObject>();
            foreach (IModelObject imo in model.ModelObjects.Values) {
                modelObjects.Add(imo);
            }

            modelObjects.Sort(new Comparison<IModelObject>(IMOCompareByGuid));

            StringBuilder sb = new StringBuilder();
            modelObjects.ForEach(delegate(IModelObject imo) { sb.Append(imo.Name + "|" + imo.Guid + "|" + imo.Description + "||"); });

            return sb.ToString();
        }

        private static int IMOCompareByGuid(IModelObject imo1, IModelObject imo2) {
            return Utility.GuidOps.Compare(imo1.Guid, imo2.Guid);
        }

        private void InitializationManager_InitializationBeginning(int generation) {
            Console.WriteLine("Initialization beginning.");
        }

        private void InitializationManager_InitializationCompleted(int generation) {
            Console.WriteLine("Initialization completing.");
        }

        #region >>> Creation Helper APIs <<<
        private ItemSource CreateItemGenerator(string rootName, int numItems, double mean, double stdev) {
            IDoubleDistribution dist = new NormalDistribution(m_model, "Item Generator", Guid.NewGuid(), mean, stdev);
            IPeriodicity periodicity = new Periodicity(dist, Periodicity.Units.Minutes);
            bool autoStart = true;
            Ticker ticker = new Ticker(m_model, periodicity, autoStart, numItems);
            ObjectSource newItem = new ObjectSource(new Item.ItemFactory(m_model, "Item_", Guid.NewGuid()).NewItem);
            return new ItemSource(m_model, rootName, Guid.NewGuid(), newItem, ticker);
        }
        #endregion

        class Item : IModelObject {

            private Item(Model model, string name, Guid guid) {
                m_model = model;
                m_name = name;
                m_guid = guid;
                if (m_model != null)
                    m_model.ModelObjects.Add(guid, this);
            }

            #region Implementation of IModelObject
            private string m_name = null;
            public string Name { get { return m_name; } }
            private string m_description = null;
            /// <summary>
            /// A description of this Item.
            /// </summary>
            public string Description {
                get { return m_description == null ? m_name : m_description; }
            }
            private Guid m_guid = Guid.Empty;
            public Guid Guid { get { return m_guid; } }
            private Model m_model = null;
            public IModel Model { get { return m_model; } }

            /// <summary>
            /// Initializes the fields that feed the properties of this IModelObject identity.
            /// </summary>
            /// <param name="model">The IModelObject's new model value.</param>
            /// <param name="name">The IModelObject's new name value.</param>
            /// <param name="description">The IModelObject's new description value.</param>
            /// <param name="guid">The IModelObject's new GUID value.</param>
            public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
                if (Model == null && Guid.Equals(Guid.Empty)) {
                    m_model = (Model)model;
                    m_name = name;
                    m_description = description;
                    m_guid = guid;

                } else {
                    string identity = "Model=" + ( Model == null ? "<null>" : ( Model.Name == null ? Model.Guid.ToString() : Model.Name ) ) +
                        ", Name=" + ( Name == null ? "<null>" : Name ) + ", Description=" + ( Description == null ? "<null>" : Description ) +
                        ", Guid=" + Guid.ToString();

                    throw new ApplicationException("Cannot call InitializeIdentity(...) on an IModelObject that is already initialized. " +
                        "The IModelobject's Identity is " + identity + ".");
                }
            }

            #endregion

            public class ItemFactory : IModelObject {
                private int m_itemNumber = 0;
                public ItemFactory(Model model, string name, Guid guid) {
                    m_model = model;
                    m_name = name;
                    m_guid = guid;
                    if (m_model != null)
                        m_model.ModelObjects.Add(guid, this);
                }

                public object NewItem() {
                    return new Item(m_model, m_name + ( m_itemNumber++ ), Guid.NewGuid());
                }

                #region Implementation of IModelObject
                private string m_name = null;
                public string Name { get { return m_name; } }
                private string m_description = null;
                /// <summary>
                /// A description of this Item Factory.
                /// </summary>
                public string Description {
                    get { return m_description == null ? m_name : m_description; }
                }
                private Guid m_guid = Guid.Empty;
                public Guid Guid { get { return m_guid; } }
                private Model m_model = null;
                public IModel Model { get { return m_model; } }
                /// <summary>
                /// Initializes the fields that feed the properties of this IModelObject identity.
                /// </summary>
                /// <param name="model">The IModelObject's new model value.</param>
                /// <param name="name">The IModelObject's new name value.</param>
                /// <param name="description">The IModelObject's new description value.</param>
                /// <param name="guid">The IModelObject's new GUID value.</param>
                public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
                    if (Model == null && Guid.Equals(Guid.Empty)) {
                        m_model = (Model)model;
                        m_name = name;
                        m_description = description;
                        m_guid = guid;

                    } else {
                        string identity = "Model=" + ( Model == null ? "<null>" : ( Model.Name == null ? Model.Guid.ToString() : Model.Name ) ) +
                            ", Name=" + ( Name == null ? "<null>" : Name ) + ", Description=" + ( Description == null ? "<null>" : Description ) +
                            ", Guid=" + Guid.ToString();

                        throw new ApplicationException("Cannot call InitializeIdentity(...) on an IModelObject that is already initialized. " +
                            "The IModelobject's Identity is " + identity + ".");
                    }
                }

                #endregion
            }
        }

        private void Output_PortDataPresented(object data, IPort where) {
            Item p = (Item)data;
            Console.WriteLine(p.Model.Executive.Now + " : " + ( ( (IModelObject)where.Owner ).Name ) + "." + p.Name + " created.");
        }

        private void Queue_LevelChangedEvent(int previous, int current, IQueue queue) {
            Console.WriteLine("Queue level in " + queue.Name + " is now " + current);
        }

        public interface IGenerator : IModelObject { }
        public interface IActivity : IModelObject { }
        public interface IDecision : IModelObject { }
        public interface ICompletion : IModelObject { }

        public interface IConstructionFacade { // Will be implemented by DIModel.
            
            // Note: IModel has ModelObjectCollection that can return any IModelObject by Guid.

            event ModelObjectEvent DIModelObjectAdded;
            event ModelObjectEvent DIModelObjectRemoved;

            IQueue AddQueue(string name, int maxDepth);
            bool RemoveQueue(Guid guid);

            IConnector Connect(IPort from, IPort to);
            IConnector Connect(IPortOwner from, string fromPortName, IPortOwner to, string toPortName );
            bool Disconnect(IConnector connector);
            
            IGenerator AddGenerator(string name, string distributionType, params object[] distroParameters);
            bool RemoveGenerator(Guid guid);

            IGenerator AddActivity(string name, params object[] otherStuff); // will be defined later...
            bool RemoveActivity(Guid guid);

            IDecision AddDecision(string name, params object[] otherStuff); // will be defined later...
            bool RemoveDecision(Guid guid);

            ICompletion AddCompletion(string name, params object[] otherStuff); // will be defined later...
            bool RemoveCompletion(Guid guid);

            IResource AddResource(string name, params object[] otherStuff); // will be defined later...
            bool RemoveResource(Guid guid);
        }

        public class DIModel : IModel, IConstructionFacade {

            #region Private Fields
            private string m_name = null;
            private Guid m_guid = Guid.Empty;
            private IModel m_model = null;
            private string m_description = null;
            private System.Collections.Hashtable m_parameters;
            private ulong m_randomSeed;
            private Randoms.RandomServer m_randomServer;
            private ModelObjectDictionary m_modelObjectDictionary;
            private ModelConfig m_modelConfig;
            private IExecutive m_executive;
            private ArrayList m_modelWarnings;
            private ArrayList m_modelErrors;
            private ArrayList m_errorHandlers;
            private StateMachine m_stateMachine;
            #endregion

            /// <summary>
            /// The states of this model.
            /// </summary>
            public enum State {
                /// <summary>
                /// The model is raw if it contains objects that have not been initialized.
                /// </summary>
                Raw,
                /// <summary>
                /// The model is initialized if it is ready to run.
                /// </summary>
                Initialized,
                /// <summary>
                /// The model is complete if it has been run and has data ready to be read.
                /// </summary>
                Complete
            };

            /// <summary>
            /// Initializes a new instance of the <see cref="DESModel"/> class.
            /// </summary>
            public DIModel() {
                PopulatePrivateFields();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DESModel"/> class.
            /// </summary>
            /// <param name="name">The name by which this model is known. Typically not required to be unique.</param>
            /// <param name="guid">The Guid for this object. Required to be unique in a given Application Context.</param>
            /// <param name="description">The description for this model. Typically used for human-readable representations.</param>
            /// <param name="randomSeed">The random seed with which the Random Server will be initialized.</param>
            public DIModel(string name, Guid guid, string description, ulong randomSeed) {
                PopulatePrivateFields();
            }

            [Initializer(InitializerAttribute.InitializationType.PreRun, "_Initialize")]
            public void Initialize(IModel model, string name, string description, Guid guid,
                [InitializerArg(0, "Random Seed", RefType.Owned, typeof(ulong), "The random seed to be used by this model.")]
			ulong randomSeed,
                [InitializerArg(1, "Other Model Objects", RefType.Owned, typeof(IModelObject), "We'll get more specific...")]
			Guid[] otherModelObjects
               ) {

                InitializeIdentity(model, name, description, guid);

                IMOHelper.RegisterWithModel(this);

                model.GetService<InitializationManager>().AddInitializationTask(new Initializer(_Initialize), randomSeed, otherModelObjects);
            }

            /// <summary>
            /// Services needs in the first dependency-sequenced round of initialization.
            /// </summary>
            /// <param name="model">The model in which the initialization is taking place.</param>
            /// <param name="p">The array of objects that take part in this round of initialization.</param>
            public void _Initialize(IModel model, object[] p) {
                m_randomSeed = (ulong)p[0];
                m_randomServer = new Highpoint.Sage.Randoms.RandomServer(m_randomSeed, 0);
            }

            /// <summary>
            /// Populates the private fields of this model, inasmuch as they may be independent of initialization parameters.
            /// </summary>
            /// <param name="randomSeed">The random seed.</param>
            private void PopulatePrivateFields() {
                m_parameters = new Hashtable();
                m_modelObjectDictionary = new ModelObjectDictionary();
                m_modelConfig = new ModelConfig("Sage");
                m_executive = ExecFactory.Instance.CreateExecutive();
                m_modelWarnings = new ArrayList();
                m_modelErrors = new ArrayList();
                m_errorHandlers = new ArrayList();
                m_stateMachine = CreateStateMachine();
                m_stateMachine.InboundTransitionHandler(State.Complete).AddCommitEvent(new CommitTransitionEvent(OnModelCompleted), Double.MaxValue);
                AddService(new InitializationManager(DIModel.State.Raw, DIModel.State.Initialized));
            }

            /// <summary>
            /// Called when the model has completed.
            /// </summary>
            /// <param name="model">The model.</param>
            private void OnModelCompleted(IModel model, object userData) {
                if (Completed != null) {
                    Completed(this);
                }
            }

            /// <summary>
            /// Creates the state machine for this model.
            /// </summary>
            /// <returns>The state machine.</returns>
            private StateMachine CreateStateMachine() {
                bool[,] transitionMatrix = new bool[3, 3] { {
                    //         RAW    INI    CMP   
                    /* RAW */  false, true , false },{
                    /* INI */  true , false, true  },{
                    /* CMP */  false, true , false }
                };


                Enum[] followOnStates = new Enum[]{
                    /*Raw          -->*/ DIModel.State.Initialized,
                    /*Initialized  -->*/ DIModel.State.Complete,
                    /*Complete     -->*/ null
                };

                DIModel.State initialState = DIModel.State.Raw;

                StateMachine sm = new StateMachine(transitionMatrix, followOnStates, initialState);

                sm.SetModel(this);

                return sm;
            }

            #region IModel Members

            /// <summary>
            /// Gets the random seed in use by this model.
            /// </summary>
            /// <value>The random seed in use by this model.</value>
            public ulong RandomSeed {
                get { return m_randomSeed; }
            }

            /// <summary>
            /// Gets the random server.
            /// </summary>
            /// <value>The random server.</value>
            public Highpoint.Sage.Randoms.RandomServer RandomServer {
                get { return m_randomServer; }
            }

            private ExecController m_execController = null;
            public ExecController ExecutiveController {
                get {
                    if (m_execController == null) {
                        m_execController = new ExecController(this.Executive, 2, 7, this);
                    }
                    return m_execController;
                }
            }

            /// <summary>
            /// A dictionary of currently live IModelObjects. An IModelObject that is garbage-
            /// collected is automatically removed from this collection. Note that the object
            /// is not necessarily removed at the time of last release, but at the time of
            /// garbage collection. Code can call Remove(...) to explicitly remove the object.
            /// </summary>
            /// <value></value>
            public ModelObjectDictionary ModelObjects {
                get { return m_modelObjectDictionary; }
            }

            /// <summary>
            /// Adds a model object to this model's ModelObjects collection.
            /// </summary>
            /// <param name="modelObject">The model object.</param>
            public void AddModelObject(IModelObject modelObject) {
                m_modelObjectDictionary.Add(modelObject.Guid, modelObject);
            }

            /// <summary>
            /// The ModelConfig is an object that holds the contents of the Sage® section of the
            /// app.config file.
            /// </summary>
            /// <value></value>
            public ModelConfig ModelConfig {
                [DebuggerStepThrough]
                get { return m_modelConfig; }
            }

            /// <summary>
            /// Provides access to the executive being used by this model.
            /// </summary>
            /// <value></value>
            public IExecutive Executive {
                [DebuggerStepThrough]
                get { return m_executive; }
            }

            /// <summary>
            /// An collection of all of the warnings currently applicable to this model.
            /// </summary>
            /// <value></value>
            public System.Collections.ICollection Warnings {
                [DebuggerStepThrough]
                get { return m_modelWarnings; }
            }

            /// <summary>
            /// Adds a warning to this model, e.g. a 'GenericModelWarning'...
            /// </summary>
            /// <param name="theWarning">The warning to be added.</param>
            public void AddWarning(IModelWarning theWarning) {
                m_modelWarnings.Add(theWarning);
            }

            /// <summary>
            /// Returns true if this model has any active warnings.
            /// </summary>
            /// <returns>
            /// Returns true if this model has any active warnings - otherwise, false.
            /// </returns>
            public bool HasWarnings() {
                return m_modelWarnings.Count > 0;
            }

            /// <summary>
            /// Clears all of the warnings applicable to this model.
            /// </summary>
            public void ClearAllWarnings() {
                m_modelWarnings.Clear();
            }

            /// <summary>
            /// Fired when an error is added to the model. This fires only after all handlers have failed to
            /// self-remove the error.
            /// </summary>
            public event ErrorEvent ErrorHappened;

            /// <summary>
            /// Fired when an error is removed from the model.
            /// </summary>
            public event ErrorEvent ErrorCleared;

            /// <summary>
            /// Enables a user/developer to add an error handler to the model in real time,
            /// (e.g. during a simulation run) and ensures that that handler is called for
            /// any errors currently in existence in the model.
            /// </summary>
            /// <param name="theErrorHandler">The error handler delegate that is to receive notification of the error events.</param>
            public void AddErrorHandler(IErrorHandler theErrorHandler) {
                m_errorHandlers.Add(theErrorHandler);
            }

            /// <summary>
            /// Removes an error handler from the model.
            /// </summary>
            /// <param name="theErrorHandler">The error handler to be removed from the model.</param>
            public void RemoveErrorHandler(IErrorHandler theErrorHandler) {
                m_errorHandlers.Remove(theErrorHandler);
            }

            /// <summary>
            /// A collection of the errors in the model.
            /// </summary>
            /// <value></value>
            public System.Collections.ICollection Errors {
                get { return ArrayList.ReadOnly(m_modelErrors); }
            }

            /// <summary>
            /// Adds an error to the model, and iterates over all of the error handlers,
            /// allowing each in turn to respond to the error. As soon as any errorHandler
            /// indicates that it has HANDLED the error (by returning true from 'HandleError'),
            /// the error is cleared, and further handlers are not called.
            /// </summary>
            /// <param name="theError">The error that is to be added to the model's error list.</param>
            /// <returns>
            /// True if the error was successfully added to the model, false if it was cleared by a handler.
            /// </returns>
            public bool AddError(IModelError theError) {

                foreach (IErrorHandler ieh in m_errorHandlers) {
                    if (ieh.HandleError(theError)) {
                        return false;
                    }
                }

                m_modelErrors.Add(theError);

                if (ErrorHappened != null) {
                    ErrorHappened(theError);
                }
                return true;
            }

            /// <summary>
            /// Removes the error from the model's collection of errors.
            /// </summary>
            /// <param name="theError">The error to be removed from the model.</param>
            public void RemoveError(IModelError theError) {
                m_modelErrors.Remove(theError);
                if (ErrorCleared != null) {
                    ErrorCleared(theError);
                }
            }

            /// <summary>
            /// Removes all errors whose target is the specified object.
            /// </summary>
            /// <param name="target">The object for whom all errors are to be removed.</param>
            public void ClearAllErrorsFor(object target) {
                ArrayList toBeCleared = new ArrayList();
                foreach (IModelError ime in m_modelErrors) {
                    if (target == null || target.Equals(ime.Target)) {
                        toBeCleared.Add(ime);
                        break; // No need to keep trying to clear once someone has indicated that they have cleared it.
                    }
                }

                foreach (IModelError ime in toBeCleared) {
                    RemoveError(ime);
                    if (ErrorCleared != null) {
                        ErrorCleared(ime);
                    }
                }
            }

            /// <summary>
            /// Returns true if the model has errors.
            /// </summary>
            /// <returns>true if the model has errors.</returns>
            public bool HasErrors() {
                return m_modelErrors.Count > 0;
            }

            /// <summary>
            /// Provides a string that summarizes all of the errors currently active in this model.
            /// </summary>
            /// <value></value>
            public string ErrorSummary {
                get {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (IModelError ime in m_modelErrors) {
                        sb.Append(ime.ToString());
                        sb.Append("\r\n");
                    }
                    return sb.ToString();
                }
            }

            /// <summary>
            /// Provides access to the state machine being used by this model. While the state machine
            /// can be set, too, this is an advanced feature, and should not be done unless the developer
            /// is sure what they are doing.
            /// </summary>
            /// <value></value>
            public StateMachine StateMachine {
                [DebuggerStepThrough]
                get {
                    return m_stateMachine;
                }
                set {
                    if (StateMachine == null) {
                        m_stateMachine = value;
                    } else {
                        throw new InvalidOperationException("Attempting to replace an existing (rather than simply install a new) state machine. This is not supported.)");
                    }
                }
            }

            public bool IsRunning { get; set; }
            public bool IsPaused { get; set; }
            public bool IsCompleted { get; set; }
            public bool IsReady { get; set; }

            /// <summary>
            /// Starts the model.
            /// </summary>
            public void Start() {

                if (!m_stateMachine.State.Equals(DIModel.State.Initialized)) {
                    m_stateMachine.DoTransition(DIModel.State.Initialized);
                }

                if (Starting != null) {
                    Starting(this);
                }

                m_stateMachine.DoTransition(DIModel.State.Complete);

                if (Stopping != null) {
                    Stopping(this);
                }
            }

            /// <summary>
            /// Pauses execution of this model after completion of the running callback of the current event.
            /// </summary>
            public virtual void Pause() {
                m_model.Pause();
            }

            /// <summary>
            /// Resumes execution of this model. Ignored if the model is not already paused.
            /// </summary>
            public virtual void Resume() {
                m_model.Resume();
            }

            /// <summary>
            /// Aborts the model.
            /// </summary>
            public void Abort() {
                m_executive.EventList.Clear();
            }

            /// <summary>
            /// Fired when the model is starting, whether it is starting fresh, or being restarted after a pause.
            /// </summary>
            public event ModelEvent Starting;

            /// <summary>
            /// Fired when the model is stopping, whether it is complete, or has been paused.
            /// </summary>
            public event ModelEvent Stopping;

            /// <summary>
            /// Fired when the model has run to completion.
            /// </summary>
            public event ModelEvent Completed;

            public void AddService<T>(T service, string name = null) where T : IModelService
            {
                throw new NotImplementedException();
            }

            public T GetService<T>(string identifier = null) where T : IModelService
            {
                throw new NotImplementedException();
            }

            #endregion

            #region IHasParameters Members

            /// <summary>
            /// Gets the parameters dictionary.
            /// </summary>
            /// <value>The parameters.</value>
            public System.Collections.IDictionary Parameters {
                get { return m_parameters; }
            }

            #endregion

            #region Implementation of IModelObject

            /// <summary>
            /// The IModel to which this object belongs.
            /// </summary>
            /// <value>The model.</value>
            public IModel Model { [DebuggerStepThrough] get { return m_model; } }

            /// <summary>
            /// The name by which this model is known. Typically not required to be unique.
            /// </summary>
            /// <value>The model's name.</value>
            public string Name { [DebuggerStepThrough]get { return m_name; } }

            /// <summary>
            /// The description for this model. Typically used for human-readable representations.
            /// </summary>
            /// <value>The model's description.</value>
            public string Description { [DebuggerStepThrough] get { return ( ( m_description == null ) ? ( "No description for " + m_name ) : m_description ); } }

            /// <summary>
            /// The Guid for this model. Required to be unique in a given Application Context.
            /// </summary>
            /// <value>The model's Guid.</value>
            public Guid Guid { [DebuggerStepThrough] get { return m_guid; } }

            /// <summary>
            /// Initializes the fields that feed the properties of this IModelObject identity.
            /// </summary>
            /// <param name="model">The IModelObject's new model value.</param>
            /// <param name="name">The IModelObject's new name value.</param>
            /// <param name="description">The IModelObject's new description value.</param>
            /// <param name="guid">The IModelObject's new GUID value.</param>
            public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
                IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
            }
            #endregion

            public DeserializationContext DeserializationContext { get; set; }

            #region ConstructionFacade Members

#pragma warning disable 67 // Ignore it if this event is not used. It's a framework, and this event may be for clients.
            public event ModelObjectEvent DIModelObjectAdded;

            public event ModelObjectEvent DIModelObjectRemoved;
#pragma warning restore 67

            public IQueue AddQueue(string name, int maxDepth) {
                throw new Exception("The method or operation is not implemented.");
            }

            public bool RemoveQueue(Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            public IConnector Connect(IPort from, IPort to) {
                throw new Exception("The method or operation is not implemented.");
            }

            public IConnector Connect(IPortOwner from, string fromPortName, IPortOwner to, string toPortName) {
                throw new Exception("The method or operation is not implemented.");
            }

            public bool Disconnect(IConnector connector) {
                throw new Exception("The method or operation is not implemented.");
            }

            public IGenerator AddGenerator(string name, string distributionType, params object[] distroParameters) {
                throw new Exception("The method or operation is not implemented.");
            }

            public bool RemoveGenerator(Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            public IGenerator AddActivity(string name, params object[] otherStuff) {
                throw new Exception("The method or operation is not implemented.");
            }

            public bool RemoveActivity(Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            public IDecision AddDecision(string name, params object[] otherStuff) {
                throw new Exception("The method or operation is not implemented.");
            }

            public bool RemoveDecision(Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            public ICompletion AddCompletion(string name, params object[] otherStuff) {
                throw new Exception("The method or operation is not implemented.");
            }

            public bool RemoveCompletion(Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            public IResource AddResource(string name, params object[] otherStuff) {
                throw new Exception("The method or operation is not implemented.");
            }

            public bool RemoveResource(Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            #endregion



            #region IModel Members


            public void Reset() {
                throw new Exception("The method or operation is not implemented.");
            }

#pragma warning disable 67
            public event ModelEvent Resetting;
#pragma warning restore 67


            #endregion


            ExecController IModel.ExecutiveController {
                get {
                    throw new NotImplementedException();
                }
                set {
                    throw new NotImplementedException();
                }
            }

            public void ClearAllErrors() {
                throw new NotImplementedException();
            }

            public virtual void Dispose() {
                Executive.Dispose();
                if (this.ExecutiveController != null) {
                    this.ExecutiveController.Dispose();
                }
            }

        }
    }
}
