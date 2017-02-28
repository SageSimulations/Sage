/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners {

	public interface IJoiner : IModelObject { 
		IInputPort[] Inputs { get; }
		IOutputPort	 Output { get; }
	}

	/// <summary>
	/// Receives an object on its input port, and sends it out one or more output ports, as defined
	/// in a derived class. If it gets a pull from any output port, it pulls from its one input port.
	/// Notification of data available proceeds according to a derived class' logic.
	/// </summary>
	public abstract class Joiner : IJoiner, IPortOwner {
		protected SimpleInputPort[] m_inputs;
		public IInputPort[] Inputs { get { return m_inputs; } }
		protected SimpleOutputPort m_output;
		public IOutputPort Output { get { return m_output; } }



        public Joiner(IModel model, string name, Guid guid, int nIns) {
            InitializeIdentity(model, name, null, guid);

            m_ports = new PortSet();

            m_output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, GetTakeHandler(), GetPeekHandler());
            // AddPort(m_output); <-- Done in SOP's ctor.

            m_inputs = new SimpleInputPort[nIns];
            for (int i = 0 ; i < nIns ; i++) {
                m_inputs[i] = new SimpleInputPort(model, "Input" + i, Guid.NewGuid(), this, GetDataArrivalHandler(i));
                Inputs[i] = m_inputs[i];
                // AddPort(m_inputs[i]); <-- Done in SOP's ctor.
            }

            IMOHelper.RegisterWithModel(this);
        }

        public void AddInputPort() {

        }

		protected abstract DataArrivalHandler GetDataArrivalHandler(int i);
		protected abstract DataProvisionHandler GetPeekHandler();
		protected abstract DataProvisionHandler GetTakeHandler();

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
        /// <param name="port">The port that will be removed.</param>
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
        public string Name { [DebuggerStepThrough]get { return m_name; } }
        private Guid m_guid = Guid.Empty;
        public Guid Guid { [DebuggerStepThrough] get { return m_guid; } }
        private IModel m_model;
        public IModel Model { [DebuggerStepThrough] get { return m_model; } }
		private string m_description;
        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
		public string Description { [DebuggerStepThrough] get { return ((m_description==null)?("No description for " + m_name):m_description); } }

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

    }

	/// <summary>
	/// This joiner places anything that appears on any of its input ports, onto
	/// its output port. Pulls and Peeks are not permitted, and if the downstream
	/// entity rejects the push, the (upstream) provider's push will be refused.
	/// </summary>
	public class PushJoiner : Joiner {
		public PushJoiner(IModel model, string name, Guid guid, int nIns):base(model,name,guid,nIns){}
		protected override DataArrivalHandler GetDataArrivalHandler(int i) {
			return new DataArrivalHandler(OnDataArrived);
		}
		protected override DataProvisionHandler GetPeekHandler() {
			return null;
		}
		protected override DataProvisionHandler GetTakeHandler() {
			return null;
		}
		protected bool OnDataArrived(object data, IInputPort ip) {
			return m_output.OwnerPut(data);
		}
	}
}