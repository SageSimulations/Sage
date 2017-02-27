/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Servers {

	public class MultiChannelDelayServer : IPortOwner, IServer {

		#region >>> Private Fields <<<
		private SimpleInputPort m_entryPort;
		private SimpleOutputPort m_exitPort;
		TimeSpanDistribution m_timeSpanDistribution;
		private ArrayList m_inService;
		private int m_capacity;
		private int m_pending;
		private ExecEventReceiver m_releaseObject;
		#endregion

		/// <summary>
		/// Creates a Server that accepts service objects on its input port, and holds them for a duration
		/// specified by a TimeSpanDistribution before emitting them from its output port. It currently is
		/// designed always to be "in service."<para/>
		/// </summary>
		/// <param name="model">The model in which this buffered server will operate.</param>
		/// <param name="name">The name given to this server.</param>
		/// <param name="guid">The guid that this server will be known by.</param>
		/// <param name="timeSpanDistribution">The TimeSpanDistribution that specifies how long each object is held.</param>
		/// <param name="capacity">The capacity of this server to hold service objects (i.e. how many it can hold)</param>
		public MultiChannelDelayServer(IModel model, string name, Guid guid, TimeSpanDistribution timeSpanDistribution, int capacity)
			:this(model,name,guid){
		
			m_timeSpanDistribution = timeSpanDistribution;
			m_capacity = capacity;
		}

		private MultiChannelDelayServer(IModel model, string name, Guid guid){

            InitializeIdentity(model, name, null, guid);

            m_entryPort = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(OnDataArrived));
            m_exitPort = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null); // No take, no peek.

            // AddPort(m_entryPort); <-- Done in port's ctor.
            // AddPort(m_exitPort); <-- Done in port's ctor.

			m_releaseObject = new ExecEventReceiver(ReleaseObject);

			m_inService = new ArrayList();
			m_pending = 0;

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

		public TimeSpanDistribution DelayDistribution { 
			get {
				return m_timeSpanDistribution;
			}
			set {
				m_timeSpanDistribution = value;
			}
		}

		// Always accept a service object. (Simplification - might not.)
		private bool OnDataPresented(object patient, IInputPort port){
			bool retval = false;
			lock(this){
				if ( m_inService.Count < (m_capacity-m_pending) ) m_pending++;
				retval = true;
			}
			return retval;
		}

		// Take from the entry port, and place it on the queue's input.
		private bool OnDataArrived(object data, IInputPort port){
			m_inService.Add(data);
			m_pending--;
			if ( ServiceBeginning != null ) ServiceBeginning(this,data);
			DateTime releaseTime = m_model.Executive.Now + m_timeSpanDistribution.GetNext();
			m_model.Executive.RequestEvent(m_releaseObject,releaseTime,0.0,data);
			return true;
		}

		private void ReleaseObject(IExecutive exec, object userData){
			m_inService.Remove(userData);
			if ( ServiceCompleted != null ) ServiceCompleted(this,userData);
			m_exitPort.OwnerPut(userData);
		}

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private PortSet m_ports = new PortSet();
		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that this IPortOwner will add.</param>
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
        /// <param name="port">The port to be removed from this MultiChannelDelayServer.</param>
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

		/// <summary>
		/// From class docs - It currently is designed always to be "in service."
		/// </summary>
		/// <param name="dt"></param>
		public void PlaceInServiceAt(DateTime dt) {
			// From class docs - It currently is designed always to be "in service."
		}

		/// <summary>
		/// From class docs - It currently is designed always to be "in service."
		/// </summary>
		public void PlaceInService() {
			// From class docs - It currently is designed always to be "in service."
		}

		/// <summary>
		/// From class docs - It currently is designed always to be "in service."
		/// </summary>
		/// <param name="dt"></param>
		public void RemoveFromServiceAt(DateTime dt) {
			// From class docs - It currently is designed always to be "in service."
		}

		/// <summary>
		/// From class docs - It currently is designed always to be "in service."
		/// </summary>
		public void RemoveFromService() {
			// From class docs - It currently is designed always to be "in service."
		}

		/// <summary>
		/// This server has no periodicity, but rather a TimeSpanDistribution (since it
		/// services multiple objects at the same time.)
		/// </summary>
		public IPeriodicity Periodicity { get{return null;} set{ } }

		public event ServiceEvent ServiceBeginning;

		public event ServiceEvent ServiceCompleted;

		#endregion
	}
}