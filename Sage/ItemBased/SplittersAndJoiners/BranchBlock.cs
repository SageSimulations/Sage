/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners {

	public delegate bool BooleanDecider(object obj);

	/// <summary>
	/// The SimpleBranchBlock takes an object off of one input port, makes a choice from among its
	/// output ports, and sends the object to that port.
	/// </summary>
	public abstract class SimpleBranchBlock : ISplitter, IPortOwner, IModelObject {
		private PortSet m_portSet;
		protected IInputPort m_input;
		public IInputPort Input{ get { return m_input; } }
		protected IOutputPort[] m_outputs;
		public IOutputPort[] Outputs { get { return m_outputs; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBranchBlock"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
		public SimpleBranchBlock(IModel model, string name, Guid guid) {
            InitializeIdentity(model, name, null, guid);
            m_portSet = new PortSet();
			SetUpInputPort();
			SetUpOutputPorts();
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

		protected void SetUpInputPort(){
			m_input = new SimpleInputPort(m_model,"In",Guid.NewGuid(),this,new DataArrivalHandler(OnDataArrived));
            // m_portSet.AddPort(m_input); <-- Done in port's ctor.
		}

		/// <summary>
		///  Implemented by a method designed to respond to the arrival of data
		///  on a port.
		/// </summary>
		private bool OnDataArrived(object data, IInputPort port){
			SimpleOutputPort outport = (SimpleOutputPort)ChoosePort(data);
			if ( outport == null ) return false;
			return outport.OwnerPut(data);
		}

		protected abstract void SetUpOutputPorts();
		protected abstract IPort ChoosePort(object dataObject);

		#region IPortOwner Members

        /// <summary>
        /// Adds a user-created port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
		public void AddPort(IPort port) {
			m_portSet.AddPort(port);
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
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective. Implementers are responsible to refuse removal of a port that
        /// is a hard property exposed (e.g. this.InputPort0), since it will remain
        /// accessible via that property.
        /// </summary>
        /// <param name="port">The port.</param>
        public void RemovePort(IPort port) {
			m_portSet.RemovePort(port);
		}

        /// <summary>
        /// Unregisters all ports.
        /// </summary>
		public void ClearPorts() {
			m_portSet.ClearPorts();
		}

        /// <summary>
        /// A PortSet containing the ports that this port owner owns.
        /// </summary>
        /// <value></value>
		public IPortSet Ports {
			get {
				return m_portSet;
			}
		}

		#endregion

		#region Sample Implementation of IModelObject
        private string m_name = null;
        public string Name { get { return m_name; } }
		private string m_description = null;
		/// <summary>
		/// A description of this SimpleBranchBlock.
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

	
	public abstract class SimpleTwoChoiceBranchBlock : SimpleBranchBlock {
		protected IOutputPort Out0, Out1;
		public SimpleTwoChoiceBranchBlock(IModel model, string name, Guid guid):base(model,name,guid){}

		protected override void SetUpOutputPorts() {
			Out0 = new SimpleOutputPort(Model,"Out0",Guid.NewGuid(),this,null,null);
            // Ports.AddPort(m_out0); <-- Done in port's ctor.
            Out1 = new SimpleOutputPort(Model, "Out1", Guid.NewGuid(), this, null, null);
            // Ports.AddPort(m_out1); <-- Done in port's ctor.
			m_outputs = new IOutputPort[]{Out0,Out1};
		}
	}

	
	public class SimpleStochasticTwoChoiceBranchBlock : SimpleTwoChoiceBranchBlock {
		private double m_percentageOut0;
		private IRandomChannel m_randomChannel;
		public SimpleStochasticTwoChoiceBranchBlock(IModel model, string name, Guid guid, double percentageOut0):base(model,name,guid){
			m_percentageOut0 = percentageOut0;
			m_randomChannel = model.RandomServer.GetRandomChannel();
		}
		protected override IPort ChoosePort(object dataObject) {
			if ( m_randomChannel.NextDouble() <= m_percentageOut0 ) return m_outputs[0];
			return m_outputs[1];
		}

	}
	public class SimpleDelegatedTwoChoiceBranchBlock : SimpleTwoChoiceBranchBlock {
		public IOutputPort YesPort;
		public IOutputPort NoPort;
		private BooleanDecider m_bd = null;
		public SimpleDelegatedTwoChoiceBranchBlock(IModel model, string name, Guid guid):base(model,name,guid){
			YesPort = Out0;
			NoPort = Out1;
		}

		public BooleanDecider BooleanDeciderDelegate {
			get { return m_bd; }
			set { m_bd = value; }
		}
		protected override IPort ChoosePort(object dataObject) {
			if ( m_bd  != null ) {
				if ( m_bd(dataObject) ){
					return YesPort;
				} else {
					return NoPort;
				}
			} else {
				return null;
			}
		}
	}
}
