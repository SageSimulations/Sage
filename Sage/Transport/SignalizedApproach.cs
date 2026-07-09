/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;

namespace Highpoint.Sage.Transport {

    /// <summary>
    /// The stop line of one approach to a signalized intersection. Arriving vehicles queue;
    /// while the governing TrafficSignal shows this approach green, the queue discharges one
    /// vehicle per saturation headway through the Output port. A vehicle arriving on green
    /// with no queue ahead of it passes with no delay (beyond any headway owed to the vehicle
    /// that discharged before it). When vehicles are held on red, the block schedules its own
    /// wake-up event at the next green start, so a waiting queue keeps the executive alive but
    /// an idle approach schedules nothing.
    /// </summary>
    public class SignalizedApproach : IPortOwner, IModelObject {

        private readonly TrafficSignal m_signal;
        private readonly int m_approachIndex;
        private readonly TimeSpan m_saturationHeadway;
        private readonly SimpleInputPort m_input;
        private readonly SimpleOutputPort m_output;
        private readonly Queue<object> m_queue = new Queue<object>();
        private DateTime m_lastDischarge = DateTime.MinValue;
        private bool m_dischargeScheduled = false;

        /// <summary>
        /// Creates a new instance of the <see cref="T:SignalizedApproach"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object.</param>
        /// <param name="guid">The GUID of this object.</param>
        /// <param name="signal">The signal that governs this approach.</param>
        /// <param name="approachIndex">This approach's index in the signal's phase definitions.</param>
        /// <param name="saturationHeadway">The minimum separation between successive discharges
        /// across the stop line. Must not be negative.</param>
        public SignalizedApproach(IModel model, string name, Guid guid, TrafficSignal signal, int approachIndex, TimeSpan saturationHeadway) {
            if (signal == null) throw new ArgumentNullException("signal");
            if (!signal.EverGreen(approachIndex)) {
                throw new ArgumentException(string.Format("No phase of signal {0} ever grants approach {1} green; vehicles would wait forever.", signal.Name, approachIndex));
            }
            if (saturationHeadway < TimeSpan.Zero) throw new ArgumentException("A SignalizedApproach's saturation headway cannot be negative.", "saturationHeadway");

            InitializeIdentity(model, name, null, guid);

            m_signal = signal;
            m_approachIndex = approachIndex;
            m_saturationHeadway = saturationHeadway;

            m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(OnVehicleArriving));
            m_output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null);

            model.Starting += new ModelEvent(delegate(IModel theModel) {
                m_queue.Clear();
                m_lastDischarge = DateTime.MinValue;
                m_dischargeScheduled = false;
            });

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// The port through which vehicles arrive at the stop line.
        /// </summary>
        public IInputPort Input { get { return m_input; } }

        /// <summary>
        /// The port out of which vehicles are discharged across the stop line.
        /// </summary>
        public IOutputPort Output { get { return m_output; } }

        /// <summary>
        /// The signal that governs this approach.
        /// </summary>
        public TrafficSignal Signal { get { return m_signal; } }

        /// <summary>
        /// The number of vehicles currently waiting at (or rolling up to) the stop line.
        /// </summary>
        public int QueueDepth { get { return m_queue.Count; } }

        private bool OnVehicleArriving(object data, IInputPort port) {
            m_queue.Enqueue(data);
            EnsureDischargeScheduled();
            return true;
        }

        private void EnsureDischargeScheduled() {
            if (m_dischargeScheduled || m_queue.Count == 0) return;

            object head = m_queue.Peek();
            DateTime now = m_model.Executive.Now;
            DateTime earliest = m_lastDischarge + TendencyOps.Headway(head, m_saturationHeadway);
            if (earliest < now) earliest = now;
            // If the earliest permissible discharge falls on a non-crossable indication, wait
            // for the next one this driver will cross on: green for everyone, and yellow too
            // for a driver who is aggressive at stoplights.
            IDriverTendencies tendencies = head as IDriverTendencies;
            bool runsYellow = tendencies != null && tendencies.AggressiveAtStoplight;
            DateTime when = runsYellow ? m_signal.NextGreenOrYellow(m_approachIndex, earliest)
                                       : m_signal.NextGreen(m_approachIndex, earliest);

            m_dischargeScheduled = true;
            m_model.Executive.RequestEvent(new ExecEventReceiver(OnDischarge), when, 0.0, null);
        }

        private void OnDischarge(IExecutive exec, object userData) {
            m_dischargeScheduled = false;
            if (m_queue.Count == 0) return;

            // The discharge was scheduled on an indication its head could cross, but re-check:
            // the head may have changed since, and its tolerance for yellow may differ.
            object head = m_queue.Peek();
            if (!TendencyOps.MayCross(head, m_signal.Indication(m_approachIndex, exec.Now))) {
                EnsureDischargeScheduled();
                return;
            }

            m_queue.Dequeue();
            m_lastDischarge = exec.Now;
            m_output.OwnerPut(head);
            EnsureDischargeScheduled();
        }

        #region IPortOwner Implementation
        private readonly PortSet m_ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner.
        /// </summary>
        /// <param name="port">The port that this IPortOwner will add.</param>
        public void AddPort(IPort port) { m_ports.AddPort(port); }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { return null; }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { return null; }

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="port">The port being unregistered.</param>
        public void RemovePort(IPort port) { m_ports.RemovePort(port); }

        /// <summary>
        /// Unregisters all ports that this IPortOwner knows to be its own.
        /// </summary>
        public void ClearPorts() { m_ports.ClearPorts(); }

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
        /// A description of this SignalizedApproach.
        /// </summary>
        public string Description {
            get { return m_description == null ? m_name : m_description; }
        }
        private Guid m_guid = Guid.Empty;
        public Guid Guid { get { return m_guid; } }
        private IModel m_model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model { get { return m_model; } }

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
