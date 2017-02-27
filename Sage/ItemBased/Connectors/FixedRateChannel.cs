/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Channels
{
	/// <summary>
	/// Summary description for FixedRateChannel.
	/// </summary>
	public class FixedRateChannel : IPortOwner {
		
		#region >>> Private Variables <<<
		private SimpleInputPort m_entry;
		private SimpleOutputPort m_exit;
		private IExecutive m_exec;
		private double m_capacity;
		private DateTime m_lastExitArrivalTime;
		private DateTime m_lastEntryAcceptanceTime;
		private TimeSpan m_entryPeriod;
		private TimeSpan m_transitPeriod;
		private Queue m_queue;
		private ExecEventReceiver m_dequeueEventHandler;
		#endregion

        /// <summary>
        /// Creates a channel for which the transit rate is fixed, and which can hold a specified
        /// capacity of payload.
        /// </summary>
        /// <param name="model">The model in which this FixedRateChannel exists.</param>
        /// <param name="name">The name of this FixedRateChannel.</param>
        /// <param name="guid">The GUID of this FixedRateChannel.</param>
        /// <param name="exec">The executive that controls this channel.</param>
        /// <param name="transitPeriod">How long it takes an object to transit the channel.</param>
        /// <param name="capacity">How many objects the channel can hold.</param>
		public FixedRateChannel(IModel model, string name, Guid guid, IExecutive exec, TimeSpan transitPeriod, double capacity) {
			m_exec = exec;
			m_transitPeriod = transitPeriod;
			m_capacity = capacity;
			m_entryPeriod = TimeSpan.FromTicks((long)((double)m_transitPeriod.Ticks/m_capacity));
			m_queue = new Queue();
			m_entry = new SimpleInputPort(model, "Entry", Guid.NewGuid(), this, new DataArrivalHandler(OnEntryAttempted));
            m_exit = new SimpleOutputPort(model, "Exit", Guid.NewGuid(), this, new DataProvisionHandler(CantTakeFromChannel), new DataProvisionHandler(CantPeekFromChannel));
            //m_ports.AddPort(m_entry); <-- Done in port's ctor.
            //m_ports.AddPort(m_exit); <-- Done in port's ctor.
			m_dequeueEventHandler = new ExecEventReceiver(DequeueEventHandler);
		}

		private bool OnEntryAttempted(object obj, IInputPort ip){
			if ( m_queue.Count == 0 ) return AcceptEntry(obj);
			if ( m_exec.Now-m_lastEntryAcceptanceTime >= m_entryPeriod ) return AcceptEntry(obj);
			return false;			
		}
		private bool AcceptEntry(object obj){
			TimeSpan forwardBuffer;
			Bin bin;
			if ( m_queue.Count == 0 ) {
				forwardBuffer  = m_transitPeriod;
				bin = new Bin(obj,1.0,forwardBuffer);
				ScheduleDequeueEvent(bin);
			} else {
				forwardBuffer = TimeSpan.FromTicks(Math.Max(m_transitPeriod.Ticks,(m_exec.Now.Ticks - m_lastEntryAcceptanceTime.Ticks)));
				bin = new Bin(obj,1.0,forwardBuffer);
			}
			m_lastEntryAcceptanceTime = m_exec.Now;
			return true;
		}
		private void ScheduleDequeueEvent(Bin bin){
			m_exec.RequestEvent(m_dequeueEventHandler,m_exec.Now+bin.ForwardBuffer,0.0,bin);
		}
		private void DequeueEventHandler(IExecutive exec, object bin){
			m_lastExitArrivalTime = exec.Now;
			m_exit.OwnerPut(((Bin)bin).Payload);
		}
		private object CantTakeFromChannel(IOutputPort op, object selector){return null;}
        private object CantPeekFromChannel(IOutputPort op, object selector){ return null;}
		
		/// <summary>
		/// The input port (i.e. the on-ramp).
		/// </summary>
		public IInputPort Entry { get { return m_entry; } }
		/// <summary>
		/// The output port (i.e. the off-ramp).
		/// </summary>
		public IOutputPort Exit { get { return m_exit;  } }

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
        /// <param name="port">The port to be removed.</param>
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

		/// <summary>
		/// A channel contains a series of bins. 
		/// </summary>
		private struct Bin {
			private object m_payload;
			private double m_capacity;
			private TimeSpan m_forwardBuffer;
			public Bin(object payload, double capacity, TimeSpan forwardBuffer){
				m_payload = payload;
				m_capacity = capacity;
				m_forwardBuffer = forwardBuffer;
			}
			public object Payload { get { return m_payload; } }
			public double Capacity { get { return m_capacity; } }
			public TimeSpan ForwardBuffer { get { return m_forwardBuffer; } }
		}
	}
}
