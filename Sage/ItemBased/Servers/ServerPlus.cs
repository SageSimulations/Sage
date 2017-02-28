/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Servers {
	/// <summary>
	/// A 'server plus' is a server that can decide whether it can provide service based on some
	/// outside criteria, then do something (i.e. setup) before starting service, and something
	/// else (i.e. teardown) before completing service.
	/// </summary>
	public class ServerPlus : IServer {
		public IInputPort Input { get { return m_input; } }
		public IOutputPort Output { get { return m_output; } }
		
		#region >>> Private Fields <<<
		private SimpleInputPort m_input;
		private SimpleOutputPort m_output;
		private bool m_supportsServerObjects;
		private bool m_inService = false;
		private bool m_pending = false;
		private IPeriodicity m_periodicity;
		#endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:ServerPlus"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="periodicity">The periodicity.</param>
		public ServerPlus(IModel model, string name, Guid guid, IPeriodicity periodicity){
            InitializeIdentity(model, name, null, guid);

            m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(AcceptServiceObject));
			m_output = new SimpleOutputPort(model,"Output",Guid.NewGuid(),this,null,null);
			
			m_periodicity = periodicity;
			
			string sso = m_model.ModelConfig.GetSimpleParameter("SupportsServerObjects");
			m_supportsServerObjects = (sso==null)?false:bool.Parse(sso);

			OnCanWeProcessServiceObject = new ServiceRequestEvent(CanWeProcessServiceObjectHandler);
			OnPreCommencementSetup = new ServiceEvent(PreCommencementSetupHandler);
			OnPreCompletionTeardown = new ServiceEvent(PreCompletionTeardownHandler);

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


		public IPeriodicity Periodicity {
			get { return m_periodicity; }
			set { m_periodicity = value; }
		}


		public bool SupportsServerObjects { get { return m_supportsServerObjects; } }

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
			m_input.DataAvailable += new PortEvent(OnServiceObjectAvailable);
			TryToPullServiceObject();
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
			m_input.DataAvailable -= new PortEvent(OnServiceObjectAvailable);
			m_inService = false;
		}

		private void RemoveFromService(IExecutive exec, object userData){
			RemoveFromService();
		}
		#endregion

		private void OnServiceObjectAvailable(IPort inputPort){
			if ( m_inService ) TryToPullServiceObject();
		}

		#region >>> Override this stuff for extended functionality <<<
		protected virtual bool RequiresAsyncEvents { get { return false; } }
		public ServiceRequestEvent OnCanWeProcessServiceObject;
		protected virtual bool CanWeProcessServiceObjectHandler(IServer server, object obj){
			return true;
		}

		public ServiceEvent OnPreCommencementSetup;
		protected virtual void PreCommencementSetupHandler(IServer server, object obj){	}

		public ServiceEvent OnPreCompletionTeardown;
		protected virtual void PreCompletionTeardownHandler(IServer server, object obj){ }
		#endregion

		protected void TryToPullServiceObject(){
			IExecutive exec = Model.Executive;
			if ( RequiresAsyncEvents && exec.CurrentEventController == null ) {
				Model.Executive.RequestEvent(new ExecEventReceiver(_TryToPullServiceObject),exec.Now,0.0,null,ExecEventType.Detachable);
			} else {
				_TryToPullServiceObject(exec,null);
			}
		}

		private void _TryToPullServiceObject(IExecutive exec, object obj){
			if ( m_input.Connector == null ) return;
			object serviceObject = m_input.OwnerPeek(null);
			if ( serviceObject == null ) return;
			lock (this){
				if ( m_pending ) return;
				if ( RequiresAsyncEvents ) m_pending = true;
			}
			if ( OnCanWeProcessServiceObject(this,serviceObject) ) {
				serviceObject = m_input.OwnerTake(null);
				OnPreCommencementSetup(this,serviceObject);
				m_pending = false;
				Process(serviceObject);
			}
		}

		private bool AcceptServiceObject(object nextServiceObject, IInputPort ip){
			if ( m_inService ) {
				Process(nextServiceObject);
				return true;
			} else {
				return false;
			}
		}
		
		private void Process(object serviceObject){
			
			if ( ServiceBeginning != null ) ServiceBeginning(this,serviceObject);
			
			if ( m_supportsServerObjects ) {
				IServiceObject iso = serviceObject as IServiceObject;
				if ( iso != null ) iso.OnServiceBeginning(this);
			}
			
			DateTime when = m_model.Executive.Now+m_periodicity.GetNext();
			m_model.Executive.RequestEvent(new ExecEventReceiver(CompleteProcessing),when,0.0,serviceObject);
		}

		
		private void CompleteProcessing(IExecutive exec, object serviceObject){

			OnPreCompletionTeardown(this,serviceObject);

			if ( m_supportsServerObjects ) {
				IServiceObject iso = serviceObject as IServiceObject;
				if ( iso != null ) iso.OnServiceCompleting(this);
			}
			if ( ServiceCompleted != null ) ServiceCompleted(this,serviceObject);
			m_output.OwnerPut(serviceObject);
			if ( m_inService ) TryToPullServiceObject();
		}

		#region >>> Service Events <<<
		/// <summary>
		/// Fires when the server begins servicing an object.
		/// </summary>
		public event ServiceEvent ServiceBeginning;
		/// <summary>
		/// Fires when the server completes servicing an object.
		/// </summary>
		public event ServiceEvent ServiceCompleted;
		#endregion

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private PortSet m_ports = new PortSet();
		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that this IPortOwner will add. It is known by the Guid and name of the port.</param>
        public void AddPort(IPort port) { m_ports.AddPort(port); }

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
        /// <param name="port">The port that is to be removed.</param>
        public void RemovePort(IPort port) { m_ports.RemovePort(port); }
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
		/// A description of this ServerPlus.
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

	}
}