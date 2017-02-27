/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.SinksAndSources {

	/// <summary>
	/// Implemented by a method that is intended to generate objects.
	/// </summary>
	public delegate object ObjectSource();

	public class ItemSource : IPortOwner, IModelObject {
		
        private ObjectSource m_objectSource;
        private IPulseSource m_pulseSource;
        private object m_latestEmission = null;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSource"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="objectSource">The object source.</param>
        /// <param name="pulseSource">The pulse source.</param>
        /// <param name="persistentOutput">If true, then the most recent output value will be returned on any peek or pull.</param>
		public ItemSource(IModel model, string name, Guid guid, ObjectSource objectSource, IPulseSource pulseSource, bool persistentOutput = false){
            InitializeIdentity(model, name, null, guid);

            if (persistentOutput) {
                m_output = new SimpleOutputPort(model, "Source", Guid.NewGuid(), this, new DataProvisionHandler(PersistentOutput), new DataProvisionHandler(PersistentOutput));
            } else {
                m_output = new SimpleOutputPort(model, "Source", Guid.NewGuid(), this, new DataProvisionHandler(VolatileOutput), new DataProvisionHandler(VolatileOutput));
            }
            // m_ports.AddPort(m_output); <-- Done in port's ctor.
			m_objectSource = objectSource;
            m_pulseSource = pulseSource;
			pulseSource.PulseEvent+=new PulseEvent(OnPulse);
            
            IMOHelper.RegisterWithModel(this);

            model.Starting += new ModelEvent(delegate(IModel theModel) { m_latestEmission = null; });
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

        /// <summary>
        /// Gets the output port for this source.
        /// </summary>
        /// <value>The output.</value>
		public IOutputPort Output { get { return m_output; } }
		private SimpleOutputPort m_output;
		private void OnPulse(){
            m_latestEmission = m_objectSource();
            m_output.OwnerPut(m_latestEmission);
		}

        /// <summary>
        /// Gets or sets the object source, the factory method for creating items from this source.
        /// </summary>
        /// <value>The object source.</value>
        public ObjectSource ObjectSource {
            get { return m_objectSource; }
            set { m_objectSource = value; }
        }

        /// <summary>
        /// Gets or sets the pulse source, the ModelObject tjat provides the cadence for creating items from this source.
        /// </summary>
        /// <value>The pulse source.</value>
        public IPulseSource PulseSource {
            get { return m_pulseSource; }
            set {
                if (m_pulseSource != null) {
                    m_pulseSource.PulseEvent -= new PulseEvent(OnPulse);
                }

                m_pulseSource = value;
                m_pulseSource.PulseEvent += new PulseEvent(OnPulse);
            }
        }


        private static object VolatileOutput(IOutputPort port, object selector) { return null; }
        private object PersistentOutput(IOutputPort port, object selector) { return m_latestEmission; }

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private PortSet m_ports = new PortSet();
		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that this IPortOwner will know by this key.</param>
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
        public List<IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdOutputOnly; } }

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="port">The port.</param>
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
		/// A description of this ItemSource.
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