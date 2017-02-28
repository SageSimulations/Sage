/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Persistence;

namespace Highpoint.Sage.Graphs.Tasks {

    /// <summary>
    /// TaskProcessor encapsulates a task, and is responsible for scheduling its
    /// execution. It must be run by an external entity, often at the model's
    /// start. This external entity must call Activate in order to cause the
    /// task to be scheduled, and subsequently run - the default Model implementation
    /// does this automatically in the Running state method.
    /// </summary>
    public class TaskProcessor : IModelObject, IXmlPersistable {

		#region Private Fields
		private Task m_masterTask;

		private bool m_startConditionsSpecified = false;
		private DateTime m_when;
		private double m_priority;
		private ExecEventType m_eet;
		private IModel m_model;
		private string m_description = null;
		private Guid m_guid = Guid.Empty;
		private string m_name = null;
		private bool m_keepGraphContexts = false;

		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("TaskProcessor");
		#endregion
		
		#region Protected Fields
		protected IDictionary GraphContext;
		protected ArrayList m_graphContexts = new ArrayList();
		#endregion

		#region Constructors
		public TaskProcessor(IModel model, string name, Task task):this(model,name,Guid.NewGuid(),task) {}

		public TaskProcessor(IModel model, string name, Guid guid, Task task){
            InitializeIdentity(model, name, null, guid);
            
            m_masterTask = task;
			m_priority = 0.0;
			m_when = DateTime.MinValue;
			m_eet = ExecEventType.Synchronous;
            Model.GetService<ITaskManagementService>().AddTaskProcessor(this);
            
            IMOHelper.RegisterWithModel(this);
		}
		#endregion

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model this component runs in.</param>
        /// <param name="name">The name of this component.</param>
        /// <param name="description">The description for this component.</param>
        /// <param name="guid">The GUID of this component.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        /// <summary>
        /// Gets the master task for this Task Processor - the task that holds the root level of the task graph to be performed.
        /// </summary>
        /// <value>The master task.</value>
        public Task MasterTask {
            get {
                return m_masterTask;
            }
        }

		public void SetConfigData(DateTime when, ExecEventType eet, double priority){
			SetStartTime(when);
			SetStartEventType(eet);
			SetStartEventPriority(priority);
		}

        public void SetStartTime(DateTime when){
            m_startConditionsSpecified = true;
            m_when = when;
        }

		public DateTime StartTime { 
			get { return m_when; }
		}

        public void SetStartEventType(ExecEventType eet){
            m_eet = eet;
        }

		public void SetStartEventPriority(double priority){
			m_priority = priority;
		}

        public virtual void Activate(){
            //_Debug.WriteLine("Activating " + m_name );
            if (!m_startConditionsSpecified){
                m_when = m_model.Executive.Now;
                m_priority = 0.0;
            }
			if ( GraphContext == null ) {
				GraphContext = new Hashtable();
			} else {
				m_model.Executive.ClearVolatiles(GraphContext);
			}
            if ( m_keepGraphContexts ) m_graphContexts.Add(GraphContext);
            m_model.Executive.RequestEvent(new ExecEventReceiver(BeginExecution),m_when,m_priority,GraphContext,m_eet);
        }

        private void BeginExecution(IExecutive exec, object userData){
			if ( s_diagnostics ) {
				_Debug.WriteLine("Task processor " + Name + " beginning execution instance of graph " + m_masterTask.Name);
			}
            m_masterTask.Start((IDictionary)userData);
        }

        public bool KeepGraphContexts { get { return m_keepGraphContexts; } set { m_keepGraphContexts = value; } }
        public ArrayList GraphContexts { get { return ArrayList.ReadOnly(m_graphContexts); } }
		public IDictionary CurrentGraphContext { get { return GraphContext; } }

        public string Name { get { return m_name; } }
		/// <summary>
		/// A description of this TaskProcessor.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}
		public Guid Guid { 
			get { return m_guid; } 
			set { 
				if ( m_model != null ) {
					m_model.ModelObjects.Remove(m_guid);
					m_model.ModelObjects.Add(value,this);
				}
				m_guid = value;
			} 
		}
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => m_model;

		#region >>> Implementation of IXmlPersistable <<<
		/// <summary>
		/// Default constructor for serialization only.
		/// </summary>
		public TaskProcessor(){}

		public virtual void SerializeTo(XmlSerializationContext xmlsc){
			xmlsc.StoreObject("ExecEventType",m_eet);
			xmlsc.StoreObject("Guid",m_guid);
			xmlsc.StoreObject("KeepGCs",m_keepGraphContexts);
			xmlsc.StoreObject("MasterTask",m_masterTask);
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("StartCondSpec",m_startConditionsSpecified);
			xmlsc.StoreObject("When",m_when);
		}

		public virtual void DeserializeFrom(XmlSerializationContext xmlsc){
			m_model = (Model)xmlsc.ContextEntities["Model"];
			m_eet = (ExecEventType)xmlsc.LoadObject("ExecEventType");
			m_guid = (Guid)xmlsc.LoadObject("Guid");
			m_keepGraphContexts = (bool)xmlsc.LoadObject("KeepGCs");
			m_masterTask = (Task)xmlsc.LoadObject("MasterTask");
			m_name = (string)xmlsc.LoadObject("Name");
			m_startConditionsSpecified = (bool)xmlsc.LoadObject("StartCondSpec");
			m_when = (DateTime)xmlsc.LoadObject("When");

		}

		#endregion
	}
}
