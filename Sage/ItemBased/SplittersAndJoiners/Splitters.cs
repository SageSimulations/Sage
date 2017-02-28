/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners {

	public interface ISplitter : IModelObject { 
		IInputPort    Input { get; }
		IOutputPort[] Outputs { get; }
	}

	/// <summary>
	/// Receives an object on its input port, and sends it out one or more output ports, as defined
	/// in a derived class. If it gets a pull from any output port, it pulls from its one input port.
	/// Notification of data available proceeds according to a derived class' logic.
	/// </summary>
	public abstract class Splitter : IPortOwner, ISplitter {

        private string m_name = null;
        private Guid m_guid = Guid.Empty;
        private IModel m_model;
		private string m_description = null;

		public IInputPort Input;
		protected SimpleInputPort m_input;
		public IOutputPort[] Outputs;
		protected SimpleOutputPort[] m_outputs;
		
		public Splitter(IModel model, string name, Guid guid, int nOuts){
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, null, ref m_guid, guid);
			m_ports = new PortSet();
			m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(),this,GetDataArrivalHandler());
            //AddPort(m_input); <-- Done in SIP's ctor.
			Input = m_input;
			Outputs = new IOutputPort[nOuts];
			m_outputs = new SimpleOutputPort[nOuts];
			for ( int i = 0 ; i < nOuts ; i++ ) {
                m_outputs[i] = new SimpleOutputPort(model, "Output" + i, Guid.NewGuid(), this, GetDataProvisionHandler(i), GetPeekHandler(i));
				Outputs[i] = m_outputs[i];
				//AddPort(m_outputs[i]); <-- Done in SOP's ctor.
			}
            IMOHelper.RegisterWithModel(this);
		}
		protected abstract DataArrivalHandler GetDataArrivalHandler();
		protected abstract DataProvisionHandler GetPeekHandler(int i);
		protected abstract DataProvisionHandler GetDataProvisionHandler(int i);

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private PortSet m_ports = new PortSet();
		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that is to be added to this IPortOwner.</param>
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
        /// <param name="port">The port that is to be removed from this IPortOwner.</param>
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

		#region ISplitter Members

		IInputPort ISplitter.Input {
			get {
				return m_input;
			}
		}

		IOutputPort[] ISplitter.Outputs {
			get {
				return m_outputs;
			}
		}

		#endregion

        #region Implementation of IModelObject

        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model { [DebuggerStepThrough] get { return m_model; } }
       
        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name { [DebuggerStepThrough]get { return m_name; } }
        
        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
		public string Description { [DebuggerStepThrough] get { return ((m_description==null)?("No description for " + m_name):m_description); } }
        
        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
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
    }

	/// <summary>
	/// This splitter places anything that appears on its input port, simultaneously
	/// onto all of its output ports. If any output port cannot accept it, that output
	/// port is ignored <b>REJECTION OF PUSHES IS NOT SUPPORTED.</b>. Pulls and Peeks are not permitted.
	/// </summary>
	public class SimultaneousPushSplitter : Splitter {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimultaneousPushSplitter"/> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:SimultaneousPushSplitter"/> will run.</param>
        /// <param name="name">The name of the new <see cref="T:SimultaneousPushSplitter"/>.</param>
        /// <param name="guid">The GUID of the new <see cref="T:SimultaneousPushSplitter"/>.</param>
        /// <param name="nOuts">The number of outputs this splitter will start with.</param>
		public SimultaneousPushSplitter(IModel model, string name, Guid guid, int nOuts):base(model,name,guid,nOuts){}
		protected override DataArrivalHandler GetDataArrivalHandler() {
			return new DataArrivalHandler(OnDataArrived);
		}

		protected override DataProvisionHandler GetDataProvisionHandler(int i) {
			return null;
		}
		protected override DataProvisionHandler GetPeekHandler(int i) {
			return null;
		}
		protected bool OnDataArrived(object data, IInputPort ip) {
			foreach ( SimpleOutputPort op in m_outputs ) op.OwnerPut(data);
			return true;
		}
	}
}