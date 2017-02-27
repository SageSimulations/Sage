/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;


namespace Highpoint.Sage.ItemBased
{
	/// <summary>
	/// Summary description for SimplePortOwner.
	/// </summary>
	public class SimplePortOwner : IPortOwner, IHasIdentity
	{

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplePortOwner"/> class.
        /// </summary>
        /// <param name="name">The name by which this SimplePortOwner will be known.</param>
        /// <param name="guid">The GUID by which this SimplePortOwner will be known.</param>
		public SimplePortOwner(string name, Guid guid)
		{
			m_name = name;
			m_guid = guid;
		}

		#region IPortOwner Members
		private PortSet m_myPortSet = new PortSet();
		/// <summary>
		/// The PortSet that contains all ports currently registered with this
		/// SimplePortOwner.
		/// </summary>
		public IPortSet Ports {
			get {
				return m_myPortSet;
			}
		}
        /// <summary>
        /// Adds a Port to this SimplePortOwner's PortSet.
        /// </summary>
        /// <param name="port">The port that is being registered.</param>
		public void AddPort(IPort port) {
			m_myPortSet.AddPort(port);
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
		/// Unregisters a port from this SimplePortOwner's PortSet.
		/// </summary>
		/// <param name="port">The port that is to be removed from this IPortOwner.</param>
		public void RemovePort(IPort port) {
			m_myPortSet.RemovePort(port);
		}
		/// <summary>
		/// Unregisters all ports from this PortSet.
		/// </summary>
		public void ClearPorts() {
			m_myPortSet.ClearPorts();
		}
		#endregion

		#region Implementation of IHasIdentity
		private string m_name = null;
		public string Name { get { return m_name; } }
		private string m_description = null;
		/// <summary>
		/// A description of this SimplePortOwner.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}
		private Guid m_guid = Guid.Empty;
		public Guid Guid => m_guid;
		#endregion

	}
}
