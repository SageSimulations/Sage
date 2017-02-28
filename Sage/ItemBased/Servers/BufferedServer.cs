/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.Queues;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.Resources;
using Highpoint.Sage.SimCore;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Servers {

	/// <summary>
	/// A buffered server maintains a buffer before and after (both, optionally) a server, so that the server
	/// can act as a process step of arbitrarily large capacity that accepts service objects, and has them
	/// wait until the core server is ready for them. The effect of the output queue is that the server may always
	/// move on to the next service object, irrespective of whether the downstream process step is ready for
	/// the current service object. <para/>
	/// The constructors will accept an externally-provided server, for custom service behaviors, or will provide
	/// a simple, single-client server by default.
	/// </summary>
	public class BufferedServer : IPortOwner, IServer {

		//public delegate IResourceRequest[] RscReqArrayGetter(object serverObject);

		#region >>> Private Fields <<<
		private IInputPort m_entryPort;
		private IOutputPort m_exitPort;
		private IQueue m_preQueue;
		private IQueue m_postQueue;
		private IServer m_server;
		#endregion

		/// <summary>
		/// Creates a Buffered Server with the specified server, preQueue and postQueue. If either queue is null,
		/// that queue will not be used. The server cannot be null.<para/>
		/// </summary>
		/// <param name="model">The model in which this buffered server will operate.</param>
		/// <param name="name">The name given to this server.</param>
		/// <param name="guid">The guid that this server will be known by.</param>
		/// <param name="server">The inner server around which the queues will be placed.</param>
		/// <param name="preQueue">The </param>
		/// <param name="postQueue"></param>
		public BufferedServer(IModel model, string name, Guid guid, IServer server, IQueue preQueue, IQueue postQueue)
			:this(model,name,guid){
			Configure(server,preQueue,postQueue);
		}

		public BufferedServer(IModel model, string name, Guid guid, IDoubleDistribution dist, Periodicity.Units timeUnits, bool usePreQueue, bool usePostQueue)
			:this(model,name,guid){
			IServer server = new SimpleServer(model,name+".InnerServer",Guid.NewGuid(),new Periodicity(dist,timeUnits));
			IQueue preQueue = usePreQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			IQueue postQueue = usePostQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			Configure(server,preQueue,postQueue);
		}																									  

		public BufferedServer(IModel model, string name, Guid guid, IPeriodicity periodicity, IResourceRequest[] rscReq, bool usePreQueue, bool usePostQueue)
			:this(model,name,guid){
			IServer server = new ResourceServer(model,name+".InnerServer",Guid.NewGuid(),periodicity,rscReq);
			IQueue preQueue = usePreQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			IQueue postQueue = usePostQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			Configure(server,preQueue,postQueue);
		}

		public BufferedServer(IModel model, string name, Guid guid, IPeriodicity periodicity, bool usePreQueue, bool usePostQueue)
			:this(model,name,guid){
			IServer server = new SimpleServer(model,name+".InnerServer",Guid.NewGuid(),periodicity);
			IQueue preQueue = usePreQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			IQueue postQueue = usePostQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			Configure(server,preQueue,postQueue);
		}

		public BufferedServer(IModel model, string name, Guid guid, IServer server, bool usePreQueue, bool usePostQueue)
			:this(model,name,guid){
			IQueue preQueue = usePreQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			IQueue postQueue = usePostQueue?new Queue(model,name+".PreQueue",Guid.NewGuid()):null;
			Configure(server,preQueue,postQueue);
		}

		private BufferedServer(IModel model, string name, Guid guid){
            InitializeIdentity(model, name, null, guid);

			IMOHelper.RegisterWithModel(this);
        }

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

		private void Configure(IServer server, IQueue preQueue, IQueue postQueue){

			m_server = server;
			m_preQueue = preQueue;
			m_postQueue = postQueue;

			if ( m_preQueue != null ) {
				ConnectorFactory.Connect(m_preQueue.Output,m_server.Input);
				m_entryPort = m_preQueue.Input;
			} else {
				m_entryPort = m_server.Input;
			}

			if ( m_postQueue != null ) {
                ConnectorFactory.Connect(m_server.Output, m_postQueue.Input);
				m_exitPort = m_postQueue.Output;
			} else {
				m_exitPort = m_server.Output;
			}

            // AddPort(m_entryPort);  <-- Done in port's ctor.// TODO: These ports maybe ought to be known as "Input" or "Entry", and "Output" or "Exit" instead of what they are known as.
            // AddPort(m_exitPort); <-- Done in port's ctor.

			m_server.PlaceInService();

		}

		public IPeriodicity Periodicity { 
			get {
				return m_server.Periodicity;
			}
			set {
				m_server.Periodicity = value;
			}

		}

		public IQueue PreQueue {
			get { return m_preQueue; }
		}

		public IQueue PostQueue {
			get { return m_postQueue; }
		}

		public IServer Server {
			get { return m_server; }
		}

		// Always accept a service object. (Simplification - might not.)
		private bool OnDataPresented(object patient, IInputPort port){
			return true;
		}

		// Take from the entry port, and place it on the queue's input.
		private void OnDataArrived(object data, IPort port){
			m_preQueue.Input.Put(data);
		}

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private PortSet m_ports = new PortSet();
		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that is to be registered with this IPortOwner.</param>
		public void AddPort(IPort port) {
			m_ports.AddPort(port);
		}

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
        /// <param name="port">The port being unregistered.</param>
		public void RemovePort(IPort port){
            m_ports.RemovePort(port);
		}
		/// <summary>
		/// Unregisters all ports that this IPortOwner knows to be its own.
		/// </summary>
		public void ClearPorts(){
			m_ports.ClearPorts();
		}
		/// <summary>
		/// The public property that is the PortSet this IPortOwner owns.
		/// </summary>
		public IPortSet Ports { get { return m_ports; } }
		#endregion

		#region Implementation of IModelObject
 
		private string m_name = null;
		public string Name { get { return m_name; } }
		private string m_description = null;
		/// <summary>
		/// A description of this BufferedServer.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}
		private Guid m_guid = Guid.Empty;
		public Guid Guid => m_guid;
		private IModel m_model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => m_model;

		#endregion

		#region IServer Members

		public IInputPort Input {
			get {
				return m_entryPort;
			}
		}

		public IOutputPort Output {
			get {
				return m_exitPort;
			}
		}

		public void PlaceInServiceAt(DateTime dt) {
			m_server.PlaceInServiceAt(dt);
		}

		public void PlaceInService() {
			m_server.PlaceInService();
		}

		public void RemoveFromServiceAt(DateTime dt) {
			m_server.RemoveFromServiceAt(dt);
		}

		public void RemoveFromService() {
			m_server.RemoveFromService();
		}

		public event ServiceEvent ServiceBeginning {
			add { 
				m_server.ServiceBeginning += value;
			}

			remove {
				m_server.ServiceBeginning -= value;
			}
		}

		public event ServiceEvent ServiceCompleted {
			add { 
				m_server.ServiceCompleted += value;
			}

			remove {
				m_server.ServiceCompleted -= value;
			}
		}

		#endregion
	}
}