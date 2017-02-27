/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using Highpoint.Sage.Dependencies;

namespace Highpoint.Sage.SimCore {

    public interface IInitializationManager {
        void AddInitializationTask(Initializer initializer, params object[] parameters);
    }


    /// <summary>
    /// Class InitializerAttribute decorates any method intended to be called by an initializationManager. It declares
    /// whether the method is to be called during model setup, or during the model's run.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    public class InitializerAttribute : Attribute {
        public static readonly string DEFAULT_NAME = "_Initialize";
        /// <summary>
        /// This enumeration describes when in the lifecycle of a model, the initializer is called.
        /// </summary>
        public enum InitializationType {
            /// <summary>
            /// The initializer is called during model setup, in the transition from Dirty to initialized.
            /// </summary>
            PreRun, 
            /// <summary>
            /// The initializer is called during model run, while the model is in the running state.
            /// </summary>
            RunTime 
        }
        InitializationType m_type;
        string m_secondaryInitializerName = null;
        public InitializerAttribute(InitializationType type):this(type,DEFAULT_NAME){}

        public InitializerAttribute(InitializationType type, string secondaryInitializerName){
            m_type = type;
            m_secondaryInitializerName = secondaryInitializerName;
        }
        public InitializationType Type { get { return m_type; } }
        public string SecondaryInitializerName { 
            get { 
                if ( m_secondaryInitializerName == null ) return "_Initialize";
                return m_secondaryInitializerName;
            }
        }
    }


    /// <summary>
    /// Delegate Initializer is implemented by any method that wishes to be called for initialization by an InitializationManager.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="parameters">The parameters.</param>
    public delegate void Initializer(IModel model, object[] parameters);
    
    public delegate void InitializationEvent(int generation);
    public delegate void InitializationAction(Initializer initializer, object[] parameters);

    /// <summary>
    /// The InitializationManager provides methods and mechanisms for running the initialization of a model.
    /// </summary>
    public class InitializationManager : IModelService {

        #region Private Fields

        private static object _token = new object();
        private ArrayList m_zeroDependencyInitializers;
        private GraphSequencer m_gs;
        private Hashtable m_verts;
        private IModel m_model;
        private int m_generation = -1;
        private Action<IModel> m_initAction;

        #endregion 

        public event InitializationAction InitializationAction;
        public event InitializationEvent InitializationBeginning;
        public event InitializationEvent InitializationCompleted;

        /// <summary>
        /// Gets or sets a value indicating whether this instance has been initialized yet.
        /// </summary>
        /// <value><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</value>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Gets a value indicating whether the service is to be automatically initialized inline when
        /// the service is added to the model, or if the user (i.e. the custom model class) will do so later.
        /// </summary>
        /// <value><c>true</c> if initialization is to occur inline, otherwise, <c>false</c>.</value>
        public bool InlineInitialization => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationManager"/> class. An entity created this way will 
        /// perform initialization actions when the application's state machine transitions into a specified state.
        /// </summary>
        /// <param name="initState">The state, in the model's state machine, whose entry-to will invoke initialization.</param>
        public InitializationManager(Enum initState)
        {
            m_initAction = m =>
            {
                m.StateMachine.InboundTransitionHandler(initState).Commit +=
                    new CommitTransitionEvent(m_model_ModelInitializing);
                Clear();
            };

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationManager"/> class. An entity created this way will 
        /// perform initialization actions when the application's state machine transitions from one specified state to another.
        /// </summary>
        /// <param name="initFromState">The transition source state.</param>
        /// <param name="initToState">The transition destination state.</param>
        public InitializationManager(Enum initFromState, Enum initToState)
        {
            m_initAction = m =>
            {
                m_model = m;
                m_model.StateMachine.TransitionHandler(initFromState, initToState).Commit +=
                    new CommitTransitionEvent(m_model_ModelInitializing);
                Clear();
            };
        }


        public void InitializeService(IModel model)
        {
            m_initAction(model);
        }

        public void Clear() {
            m_gs = new GraphSequencer();
            m_verts = new Hashtable();
            m_zeroDependencyInitializers = new ArrayList();
        }

        public int Generation { get { return m_generation; } }

        public void AddInitializationTask(Initializer initializer, params object[] parameters){

            bool zeroDependencies = true;
            foreach ( object obj in parameters ) {
                if ( (obj is Guid) || (obj is Guid[]) || (obj is Guid[][]) || (obj is Guid[][][])){
                    zeroDependencies = false;
                    break;
                }
            }

            if ( zeroDependencies ) {
                m_zeroDependencyInitializers.Add(new object[]{initializer,parameters});
            } else {
                //if ( n%1000 == 0 ) Console.WriteLine(n);
                //n++;
                Guid myGuid = Guid.Empty;
                try {
                    myGuid = (Guid)initializer.Target.GetType().GetProperty("Guid").GetValue(initializer.Target,new object[]{});
                } catch ( NullReferenceException ) {
                    Console.WriteLine("Failed to find a \"Guid\" property on a " + initializer.Target.GetType().Name + ".");
                    return;
                }

                if ( myGuid.Equals(Guid.Empty) ) {
                    throw new InitializationException(REGISTERING_GUID_EMPTY);
                }

                Dv myDv = (Dv)m_verts[myGuid];
                if ( myDv == null ) {
                    myDv = new Dv(myGuid);
                    m_verts.Add(myGuid,myDv);
                }
                myDv.Initializer = initializer;
            
                foreach ( object obj in parameters ) {
                    if ( obj is Guid[] ) {
                        foreach ( Guid g in (Guid[])obj ) GetDvForGuid(g).AddPredecessor(myDv);
                    } else {
                        if ( obj is Guid ) {
                            Guid g = (Guid)obj;

                            if ( g.Equals(Guid.Empty) ){

                            } else {
                                GetDvForGuid(g).AddPredecessor(myDv);
                            }
                        }
                    }
                }

                myDv.Parameters = parameters;

                m_gs.AddVertex(myDv);
            }
        }
        
        private Dv GetDvForGuid(Guid guid){
            Dv dv = (Dv)m_verts[guid];
            if ( dv == null ) {
                dv = new Dv(guid);
                m_verts.Add(dv.MyGuid,dv);
            }
            return dv;
        }

        private void m_model_ModelInitializing(IModel model, object userData) {
            
            lock(_token){

                m_generation++;

                IList dependentList = m_gs.GetServiceSequenceList();
                IList independentList = m_zeroDependencyInitializers;

                if ( m_generation == 0 ) ValidateModel(dependentList);

                Clear();

                InitializationBeginning?.Invoke(m_generation);

                // First call into the ones that don't need it.
                foreach ( object[] oa in independentList ) {
                    Initializer initializer = (Initializer)oa[0];
                    object[] parameters = (object[])oa[1];
                    InitializationAction?.Invoke(initializer, parameters);
                    initializer(model,parameters);
                }

                // Next walk through the ones that do, in an appropriate sequence.
                try {
                    foreach ( Dv dv in dependentList ) {
                        InitializationAction?.Invoke(dv.Initializer, dv.Parameters);
                        dv.PerformInitialization(model);
                    }

                } catch ( GraphCycleException gce ) {

                    IList cycleMembers = (ArrayList)gce.Members;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    for ( int i = 0 ; i < cycleMembers.Count ; i++ ) {
                        if ( i != 0 ) sb.Append("->");
                        object target = ((Dv)cycleMembers[i]).Initializer.Target;
                        Type mbrType = target.GetType();
                        string name = null;
                        System.Reflection.PropertyInfo nameProp = mbrType.GetProperty("Name");
                        if ( nameProp == null ) {
                            name = "(unknown " + cycleMembers[i].GetType().Name + ")";
                        } else {
                            name = (string)nameProp.GetValue(target,new object[]{});
                        }
                        sb.Append(name);
                    }

                    throw new ApplicationException("Failure to initialize due to a cyclical dependency involving [...->" + sb + "->...].");

                    //string cycle = FindCycle(cyclist);
                    //Console.WriteLine("Cycle is " + cycle + ".");
                }

                // If more initializers have been added during this generation, run another generation of initialization.
                if ( m_gs.GetServiceSequenceList().Count > 0 || m_zeroDependencyInitializers.Count > 0 ) m_model_ModelInitializing(model,userData);

                if ( m_generation == 0 ) {
                    InitializationCompleted?.Invoke(m_generation);
                }

                m_generation--;

            } // End of lock.

            // When all initialization iterations have completed...
        }

        private void ValidateModel(IList dependentVertices){
            // TODO: This needs to be tested and reinstated.
//			ArrayList al = new ArrayList();
//			foreach ( DV dv in dependentVertices ) {
//				if ( dv.ParentsList.Count == 0 ) {
//					al.Add(dv);
//				}
//			}
//
//			if ( al.Count > 1 ) {
//				string roots = "";
//				for ( int i = 0; i < al.Count; i++ ) {
//					roots+=string.Format("\"{0}\"[{1}]",((DV)al[i]).SortCriteria,((DV)al[i]).MyGuid);
//					if ( i != al.Count-2 ) roots+=", "; else roots+=" and ";
//				}
//				throw new ApplicationException("Model has more than one root node - root nodes are " + roots + ". Perhaps an initializer argument is null or wrong, or maybe an initializer Arg Array is incomplete?");
//			}
//
//			if ( al.Count == 0 ) {
//				throw new ApplicationException("Model has zero root nodes. Perhaps the model itself is cited as a reference in some sub-node?");
//			}
        }

        public static object[] Merge(object[] p1, object[] p2){
            object[] p3 = new object[p1.Length+p2.Length];
            p1.CopyTo(p3,0);
            p2.CopyTo(p3,p1.Length);
            return p3;
        }

        public static readonly string REGISTERING_GUID_EMPTY = "Detected an attempt to register an object for initialization, whose guid is Guid.Empty. This is not permitted, as the object's Guid is the way that others refer to it in dependency lists.";

        private class Dv : IDependencyVertex {
            private string m_name;
            private Initializer m_initializer;
            private Guid m_myGuid;
            private ArrayList m_predecessors;
            private object[] m_parameters;
            
            public Dv(Guid myGuid){
                m_myGuid = myGuid;
                m_name = null;
                m_initializer = null;
                m_predecessors = new ArrayList();
                m_parameters = null;
            }

            public Guid MyGuid { get { return m_myGuid; } }
            public Initializer Initializer { get { return m_initializer; } set { m_initializer = value; } }
            public object[] Parameters { get { return m_parameters; } set { m_parameters = value; } }

            public void PerformInitialization(IModel model){
                if ( m_initializer != null ) {
                    m_initializer(model,m_parameters);
                } else {
                    System.Diagnostics.Debugger.Break();
                    Console.WriteLine("Failed to find an initializer on a DV with a guid of " + m_myGuid + ".");

                }
            }

            public string Name { get { return m_name; } }

            public void AddPredecessor(Dv thePredecessor) { m_predecessors.Add(thePredecessor); }

            #region IDependencyVertex Members

            public IComparable SortCriteria {
                get {
                    if ( m_name == null ) {
                        object tgt = m_initializer.Target;
                        m_name = (string)tgt.GetType().GetProperty("Name").GetValue(tgt,new object[]{});
                    }
                    return m_name;
                }
            }

            public ICollection PredecessorList
            {
                get
                {
                    return m_predecessors; // Anything that is referred to in object 'X''s initialization must first be initialized.
                }
            }
            #endregion

            public override string ToString() {
                object tgt = m_initializer.Target;
                string objType = (string)tgt.GetType().FullName;
                string name = (string)tgt.GetType().GetProperty("Name").GetValue(tgt,new object[]{});
                return objType + " named " + name;
            }


        }
    }

    
        
        /// <summary>
    /// InitializationException summary
    /// </summary>
    [Serializable]
    public class InitializationException : Exception {
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        #region protected ctors
        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected InitializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #endregion
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public InitializationException() { }
        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public InitializationException(string message) : base(message) { }
        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public InitializationException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
    }
}
