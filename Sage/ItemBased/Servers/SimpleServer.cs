/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Servers {
	/// <summary>
	/// A SimpleServer is a single-channeled server that accepts one object from its input port,
	/// waits a specified timespan, and then presents that object to its output port. It does not
	/// permit its own outputs to be refused.
	/// <p></p>
	/// When a server becomes idle, it attempts to pull from its input port. If it is successful,
	/// it becomes busy for a timespan, determined by a timespan distribution after which the
	/// object is presented to its output port. Once the object at its output port is taken, the
	/// server becomes idle.
	/// If an object is presented on its input port and it is busy, it rejects the presentation
	/// by returning false. If it is not busy when the presentation is made, then it accepts 
	/// the new arrival, and commences working on it for a timespan. When the timespan expires,
	/// the object is placed on its output port. 
	/// </summary>
	public class SimpleServer : IServer {
		public IInputPort Input { get { return m_input; } }
		private SimpleInputPort m_input;
		public IOutputPort Output { get { return m_output; } }
		private SimpleOutputPort m_output;

		private bool m_supportsServerObjects;

		private DateTime m_startedService;
		private bool m_available = false;
		private bool m_inService = false;
		private IPeriodicity m_periodicity;
		public SimpleServer(IModel model, string name, Guid guid, IPeriodicity periodicity){
            InitializeIdentity(model, name, null, guid);
            
            m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(AcceptServiceObject));
            m_output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null);
            // AddPort(m_input); <-- Done in port's ctor.
            // AddPort(m_output); <-- Done in port's ctor.
            m_periodicity = periodicity;
			m_input.DataAvailable += new PortEvent(OnServiceObjectAvailable);
			string sso = m_model.ModelConfig.GetSimpleParameter("SupportsServerObjects");
			m_supportsServerObjects = (sso==null)?false:bool.Parse(sso);

            IMOHelper.RegisterWithModel(this);
		}

		#region >>> Place In Service <<<
		/// <summary>
		/// Waits until a specified time, then places the server in service. Can be done directly in code
		/// through the PlaceInService() API and an executive event with handler. 
		/// </summary>
		/// <param name="dt">The DateTime at which the server will be placed in service.</param>
		public void PlaceInServiceAt(DateTime dt){
			m_model.Executive.RequestEvent(new ExecEventReceiver(PlaceInService),dt,0.0,null);
		}

		/// <summary>
		/// Places the server in service immediately. The server will try immediately to
		/// pull and service a service object from its input port.
		/// </summary>
		/// <param name="exec">The executive controlling the timebase in which this server is
		/// to operate. Typically, model.Executive.</param>
		/// <param name="userData"></param>
		private void PlaceInService(IExecutive exec, object userData){
			PlaceInService();
		}

		/// <summary>
		/// Places the server in service immediately. The server will try immediately to
		/// pull and service a service object from its input port.
		/// </summary>
		public void PlaceInService(){
			m_inService = true;
			m_available = true;
			m_input.DataAvailable += new PortEvent(OnServiceObjectAvailable);
			TryToCommenceService();
		}
		#endregion

		#region >>> Remove From Service <<<
		/// <summary>
		/// Removes the server from service at a specified time. The server will complete
		/// servicing its current service item, and then accept no more items.
		/// </summary>
		/// <param name="dt">The DateTime at which this server is to be removed from service.</param>
		public void RemoveFromServiceAt(DateTime dt){
			m_model.Executive.RequestEvent(new ExecEventReceiver(RemoveFromService),dt,0.0,null);
		}

		/// <summary>
		/// Removes this server from service immediately. The server will complete
		/// servicing its current service item, and then accept no more items.
		/// </summary>
		public void RemoveFromService(){
			m_inService = false;
		}

		private void RemoveFromService(IExecutive exec, object userData){
			RemoveFromService();
		}
		#endregion

        /// <summary>
        /// The periodicity of the server.
        /// </summary>
        /// <value></value>
		public IPeriodicity Periodicity {
			get { return m_periodicity; }
			set { m_periodicity = value; }
		}

		/// <summary>
		/// This method is called either when an in-process service completes, or when a new
		/// service object shows up at the entry point of an idle server.
		/// </summary>
		/// <returns>true if the service event may proceed. If an implementer returns false,
		/// it is up to that implementer to ensure that in some way, it initiates re-attempt
		/// at a later time, or this server will freeze.</returns>
		protected virtual bool PrepareToServe(){
			return true;
		}

		private void OnServiceObjectAvailable(IPort inputPort){
			if ( m_inService && m_available ) TryToCommenceService();
		}

		private void TryToCommenceService(){
			if ( m_input.Connector == null ) return;
			object nextServiceObject = m_input.OwnerTake(null);
			if ( nextServiceObject != null ) Process(nextServiceObject);
		}

		private bool AcceptServiceObject(object nextServiceObject, IInputPort ip){
			if ( m_inService && m_available ) {
				Process(nextServiceObject);
				return true;
			} else {
				return false;
			}
		}

		private void Process(object serviceObject){
			IServiceObject iso = serviceObject as IServiceObject;
			
			if ( ServiceBeginning != null ) ServiceBeginning(this,serviceObject);
			if ( iso != null ) iso.OnServiceBeginning(this);
			m_available = false;
			m_startedService = m_model.Executive.Now;
			DateTime when = m_model.Executive.Now+m_periodicity.GetNext();
			m_model.Executive.RequestEvent(new ExecEventReceiver(CompleteProcessing),when,0.0,serviceObject);
		}

		private void CompleteProcessing(IExecutive exec, object serviceObject){
			IServiceObject iso = serviceObject as IServiceObject;
			if ( iso != null ) iso.OnServiceCompleting(this);
			if ( ServiceCompleted != null ) ServiceCompleted(this,serviceObject);
			m_output.OwnerPut(serviceObject);
			m_available = true;
			if ( m_inService ) TryToCommenceService();
		}

		/// <summary>
		/// Fires when the server begins servicing an object.
		/// </summary>
		public event ServiceEvent ServiceBeginning;
		/// <summary>
		/// Fires when the server completes servicing an object.
		/// </summary>
		public event ServiceEvent ServiceCompleted;

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
        /// <param name="port">The port being unregistered.</param>
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
		private string m_name = null;
		public string Name { get { return m_name; } }
		private string m_description = null;
		/// <summary>
		/// A description of this SimpleServer.
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

		#endregion

	}
}