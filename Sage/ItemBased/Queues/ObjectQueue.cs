/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Queues {

    /// <summary>
    ///
    /// </summary>
	public class Queue : IQueue {

        #region Member Variables
		private System.Collections.Queue m_queue;
		private SimpleInputPort m_input;
		private SimpleOutputPort m_output;
		private int m_max;
        private IModel m_model;
        private string m_name = String.Empty;
        private string m_description = String.Empty;
        private Guid m_guid = Guid.Empty;
        #endregion Member Variables

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="name">The name of this queue.</param>
        /// <param name="guid">The GUID of this queue.</param>
		public Queue(IModel model, string name, Guid guid):this(model,name,guid,int.MaxValue){}

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="name">The name of this queue.</param>
        /// <param name="guid">The GUID of this queue.</param>
        /// <param name="max">The maximum number of items that can be held in this queue.</param>
		public Queue(IModel model, string name, Guid guid, int max){
            InitializeIdentity(model, name, "", guid);
            m_max = max;
            
            m_queue = new System.Collections.Queue();
            
            Guid inGuid = Utility.GuidOps.Increment(guid);
            Guid outGuid = Utility.GuidOps.Increment(inGuid);
            
            m_output = new SimpleOutputPort(model, "Output", outGuid, this, new DataProvisionHandler(ProvideData), new DataProvisionHandler(PeekData));
            m_output.PortDataAccepted+=new PortDataEvent(OnOutputPortDataAccepted);
            m_input = new SimpleInputPort(model, "Input", inGuid, this, new DataArrivalHandler(OnDataArrived));

            LevelChangedEvent += new QueueLevelChangeEvent(OnQueueLevelChanged);

            IMOHelper.RegisterWithModel(this);
		}

        /// <summary>
        /// Gets the input port for this queue.
        /// </summary>
        /// <value>The input port.</value>
		public IInputPort Input { get { return m_input; } }

        /// <summary>
        /// Gets the output port for this queue.
        /// </summary>
        /// <value>The output.</value>
		public IOutputPort Output { get { return m_output; } }

        #region Initialization
        //TODO: 1.) Make sure that what happens in any other ctors also happens in the Initialize method.
        //TODO: 2.) Replace all DESCRIPTION? tags with the appropriate text.
        //TODO: 3.) If this class is derived from another that implements IModelObject, remove the m_model, m_name, and m_guid declarations.
        /// <summary>
        /// Use this for initialization of the form 'new Queue().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public Queue() {}

        /// <summary>
        /// The Initialize(...) method is designed to be used explicitly with the 'new ObjectQueue().Initialize(...);'
        /// idiom, and then implicitly upon loading of the model from an XML document.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="name">The name of this queue.</param>
        /// <param name="description">The description of this queue.</param>
        /// <param name="guid">The GUID of this queue.</param>
        /// <param name="max">The maximum number of items that can be held in this queue.</param>
        [Initializer(InitializerAttribute.InitializationType.PreRun, "_Initialize")]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "max", RefType.Owned, typeof(int), "The largest number of objects the queue can hold.")]
			int max) {

            InitializeIdentity(model, name, description, guid);

            IMOHelper.RegisterWithModel(this);
            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, max);
        }


        /// <summary>
        /// First-round follow-on to the <see cref="Initialize"/> call.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="p">The array of passed-in arguments.</param>
        public void _Initialize(IModel model, object[] p) {

            Guid inGuid = Utility.GuidOps.Increment(Guid);
            Guid outGuid = Utility.GuidOps.Increment(inGuid);

            m_output = new SimpleOutputPort(model, "Output", outGuid, this, new DataProvisionHandler(ProvideData), new DataProvisionHandler(PeekData));
            m_output.PortDataAccepted += new PortDataEvent(OnOutputPortDataAccepted);
            m_input = new SimpleInputPort(model, "Input", inGuid, this, new DataArrivalHandler(OnDataArrived));

            //Ports.AddPort(m_output); <-- Done in port's ctor.
            //Ports.AddPort(m_input);  <-- Done in port's ctor.

            LevelChangedEvent += new QueueLevelChangeEvent(OnQueueLevelChanged);

            m_max = (int)p[0];
            m_queue = new System.Collections.Queue(m_max);


        }

        #endregion

        /// <summary>
        /// Gets the max depth of this queue.
        /// </summary>
        /// <value>The max depth.</value>
        public int MaxDepth {
            [DebuggerStepThrough]
            get {
                return m_max;
            }
        }

		private bool OnDataArrived(object data, IInputPort ip){
			if ( data != null ) {
				m_queue.Enqueue(data);
				if ( ObjectEnqueued != null ) ObjectEnqueued(this,data);
				LevelChangedEvent(Count-1,Count,this);
				m_output.NotifyDataAvailable();
			}
			return true;
		}

        /// <summary>
        /// Called when the queue level changes.
        /// </summary>
        /// <param name="previous">The previous level.</param>
        /// <param name="current">The current level.</param>
        /// <param name="queue">The queue on which the change occurred.</param>
        public void OnQueueLevelChanged(int previous, int current, IQueue queue) {
            if (current == 0 && QueueEmptyEvent != null)
                QueueEmptyEvent(this);
            if (current == m_max && QueueFullEvent != null)
                QueueFullEvent(this);
        }

        private void OnOutputPortDataAccepted(object data, IPort where) {
            if (ObjectDequeued != null)
                ObjectDequeued(this, data);
        }

        private object ProvideData(IOutputPort op, object selector) {
			if ( m_queue.Count > 0 ) {
				object data = m_queue.Dequeue();
                LevelChangedEvent(Count+1,Count,this);
                // Commented out the following because this functionality is now provided
                // (as it was before, also) through OnPortDataAccepted. Since the Queue
                // functionality assumes that if ProvideData is called, the data will be
                // accepted, then these two avenues are equivalent.
                //if ( ObjectDequeued != null ) ObjectDequeued(this,data);
				return data;
			} else {
				return null;
			}
		}

        private object PeekData(IOutputPort op, object selector) {
			if ( m_queue.Count > 0 ) {
				object data = m_queue.Peek();
				return data;
			} else {
				return null;
			}
		}

        /// <summary>
        /// Gets the number of items currently in this queue.
        /// </summary>
        /// <value>The count.</value>
		public int Count { [DebuggerStepThrough] get { return m_queue.Count; } }

		public event QueueMilestoneEvent QueueFullEvent;
		public event QueueMilestoneEvent QueueEmptyEvent;
		public event QueueLevelChangeEvent LevelChangedEvent;
		public event QueueOccupancyEvent ObjectEnqueued;
		public event QueueOccupancyEvent ObjectDequeued;

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private PortSet m_ports = new PortSet();
		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that this IPortOwner will add.</param>
		public void AddPort(IPort port) {m_ports.AddPort(port);}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

        /// <summary>
		/// Unregisters a port from this IPortOwner.
		/// </summary>
        /// <param name="port">The port that this IPortOwner will remove.</param>
		public void RemovePort(IPort port){ m_ports.RemovePort(port); }
		/// <summary>
		/// Unregisters all ports that this IPortOwner knows to be its own.
		/// </summary>
		public void ClearPorts(){m_ports.ClearPorts();}
		/// <summary>
		/// The public property that is the PortSet this IPortOwner owns.
		/// </summary>
		public IPortSet Ports { get { return m_ports; } }
		#endregion

        #region Implementation of IModelObject

        /// <summary>
        /// The model to which this Queue belongs.
        /// </summary>
        /// <value>The Queue's description.</value>
        public IModel Model { [DebuggerStepThrough] get { return m_model; } }
        
        /// <summary>
        /// The Name for this Queue. Typically used for human-readable representations.
        /// </summary>
        /// <value>The Queue's name.</value>
        public string Name { [DebuggerStepThrough] get { return m_name; } }

        /// <summary>
        /// The Guid of this Queue.
        /// </summary>
        /// <value>The Queue's Guid.</value>
        public Guid Guid { [DebuggerStepThrough] get { return m_guid; } }

        /// <summary>
        /// The description for this Queue. Typically used for human-readable representations.
        /// </summary>
        /// <value>The Queue's description.</value>
        public string Description => (m_description ?? ("No description for " + m_name));

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The Queue's new model value.</param>
        /// <param name="name">The Queue's new name value.</param>
        /// <param name="description">The Queue's new description value.</param>
        /// <param name="guid">The Queue's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        #endregion

	}

    /// <summary>
    /// An object that has multiple inputs and one output. When a pull occurs on the output,
    /// a selection strategy is used to 
    /// </summary>
	public class MultiQueueHead  : IPortOwner {

		public IOutputPort[] Outputs;
		public IInputPort Input { get { return m_input; } }
		private SimpleInputPort m_input;
		private ISelectionStrategy m_selStrategy = null;
		private ArrayList m_queues = new ArrayList();

		public MultiQueueHead(IModel model, string name, Guid guid, ArrayList queues, ISelectionStrategy selStrategy){ // TODO: Want to add/remove queues eventually, and use an IQueueSelectionStrategy.
			m_selStrategy = selStrategy;
			selStrategy.Candidates = queues;
            
			m_input = new SimpleInputPort(model, name, guid, this, new DataArrivalHandler(OnDataPushIn));

			Outputs = new IOutputPort[queues.Count];
			for ( int i = 0 ; i < Outputs.GetLength(0) ; i++ ) {
                string portName = name+"#"+i;
                Outputs[i] = new SimpleOutputPort(model, portName, Guid.NewGuid(), this, new DataProvisionHandler(OnDataPullOut), null);
                ConnectorFactory.Connect(Outputs[i], ( (Queue)queues[i] ).Input);
			}
		}

		private object OnDataPullOut(IOutputPort op, object selector){ return m_input.OwnerTake(selector); } // Forces an upstream read.
		private bool OnDataPushIn(object data, IInputPort ip){
			if ( data != null ) {
				Queue queue = (Queue)m_selStrategy.GetNext(null);
				IOutputPort outPort = (IOutputPort)queue.Input.Peer;
				//Trace.WriteLine("Arbitrarily putting data to " + queue.ToString());
				return ((SimpleOutputPort)outPort).OwnerPut(data); // Cast is okay - it's my port.
			} else {
				return false;
			}
		}

		#region Add/Removal of Queues. Currently OOC.
		/*
		public void AddQueue( Queue queue ) {
			if ( m_queues.Count < Outputs.GetLength(0) ) {
				m_queues.Add(queue);
				for ( int i = 0 ; i < Outputs.GetLength(0) ; i++ ) {
					if ( Outputs[i].Connector == null ) {
						m_connFactory.Connect(Outputs[i],queue.Input);
						if ( QueueAddedEvent != null ) QueueAddedEvent(queue);
						break;
					}
				}
			} else {
				throw new ApplicationException("Tried to add too many ports (" + m_queues.Count+1 + ") to a MultiQueueHead.");
			}
		}

		public ArrayList Queues { get { return ArrayList.ReadOnly(m_queues); } }

		public ISelectionStrategy SelectionStrategy {
			get { return m_selStrategy; }
			set {
				if ( m_selStrategy != null ) m_selStrategy.Unregister(this);
				m_selStrategy = value;
				m_selStrategy.Register(this);
			}
		}*/
		#endregion

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private PortSet m_ports = new PortSet();
		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that this IPortOwner add.</param>
		public void AddPort(IPort port) {m_ports.AddPort(port);}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

		/// <summary>
		/// Unregisters a port from this IPortOwner.
		/// </summary>
        /// <param name="port">The port being removed.</param>
		public void RemovePort(IPort port){ m_ports.RemovePort(port); }
		/// <summary>
		/// Unregisters all ports that this IPortOwner knows to be its own.
		/// </summary>
		public void ClearPorts(){m_ports.ClearPorts();}
		/// <summary>
		/// The public property that is the PortSet this IPortOwner owns.
		/// </summary>
		public IPortSet Ports { get { return m_ports; } }
		#endregion

	}
    
	public class ShortestQueueStrategy : ISelectionStrategy {

		ICollection m_queues;

		public ShortestQueueStrategy(){}

		public ICollection Candidates { get { return m_queues; } set { m_queues = value; } }

		public object GetNext(object context){
			if ( m_queues.Count == 0 ) throw new ApplicationException("Queue selector has no queues to select from.");
			int emptiestCount = int.MaxValue;
			Queue nextQueue = null;
			foreach ( Queue queue in m_queues ) {
				if ( queue.Count < emptiestCount ) {
					emptiestCount = queue.Count;
					nextQueue = queue;
				}
			}
			return nextQueue;
		}
	}

	public class OldestShortestQueueStrategy : ISelectionStrategy {

		ICollection m_queues;
		QueueLevelChangeEvent m_qlce;
		ArrayList m_queueList = new ArrayList();

		public OldestShortestQueueStrategy(){
			m_qlce = new QueueLevelChangeEvent(OnQueueLevelChanged);
		}

		public ICollection Candidates { 
			get { 
				return m_queues; 
			} 
			set {
				if ( m_queues != null ) foreach ( Queue queue in m_queues ) queue.LevelChangedEvent -= m_qlce;
				m_queues = value;
				foreach ( Queue queue in m_queues ) {
					queue.LevelChangedEvent += m_qlce;
					m_queueList.Add(queue);
				}
			}
		}

		public object GetNext(object context){
			if ( m_queues.Count == 0 ) throw new ApplicationException("Queue selector has no queues to select from.");
			object nextQueue;
			lock ( m_queues ) {
				nextQueue = m_queueList[0];
				m_queueList.RemoveAt(0);
			}
			return nextQueue;
		}

		private void OnQueueLevelChanged(int previous, int current, IQueue queue){
			if ( m_queueList.Contains(queue) ) m_queueList.Remove(queue); // Should already be gone, from the GetNext.
			int i = 0;
			while ( i < m_queueList.Count && ((Queue)m_queueList[i]).Count <= queue.Count ) i++;
			m_queueList.Insert(i,queue);
		}
	}
}