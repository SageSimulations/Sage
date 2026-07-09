/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;

namespace Highpoint.Sage.Transport {

    /// <summary>
    /// The shared conflict area of an unsignalized intersection. Vehicles arrive on one of
    /// several approaches (approach 0 has the highest priority - e.g. the major road; higher
    /// indices yield to lower ones), queue FIFO within their approach, and cross one at a time,
    /// occupying the zone for the crossing time. Whenever the zone clears, the vehicle at the
    /// head of the highest-priority non-empty queue is admitted, and each vehicle leaves via
    /// its own approach's output port, so downstream routing can differ per approach.
    /// </summary>
    public class ConflictZone : IPortOwner, IModelObject {

        private class Crossing {
            public object Vehicle;
            public int Approach;
        }

        private readonly TimeSpan m_crossingTime;
        private readonly List<SimpleInputPort> m_approaches = new List<SimpleInputPort>();
        private readonly List<SimpleOutputPort> m_outputs = new List<SimpleOutputPort>();
        private readonly List<Queue<object>> m_queues = new List<Queue<object>>();
        private readonly ReadOnlyCollection<SimpleInputPort> m_approachesReadOnly;
        private readonly ReadOnlyCollection<SimpleOutputPort> m_outputsReadOnly;
        private bool m_occupied = false;

        /// <summary>
        /// Creates a new instance of the <see cref="T:ConflictZone"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object.</param>
        /// <param name="guid">The GUID of this object.</param>
        /// <param name="crossingTime">How long a vehicle occupies the zone while crossing.
        /// Must not be negative.</param>
        /// <param name="approachCount">The number of approaches contending for the zone.
        /// Approach 0 has the highest priority; each higher index yields to all lower ones.</param>
        public ConflictZone(IModel model, string name, Guid guid, TimeSpan crossingTime, int approachCount) {
            if (crossingTime < TimeSpan.Zero) throw new ArgumentException("A ConflictZone's crossing time cannot be negative.", "crossingTime");
            if (approachCount < 1) throw new ArgumentException("A ConflictZone must have at least one approach.", "approachCount");

            InitializeIdentity(model, name, null, guid);

            m_crossingTime = crossingTime;
            for (int i = 0; i < approachCount; i++) {
                int approachIndex = i; // Captured per-iteration for the arrival handler.
                m_approaches.Add(new SimpleInputPort(model, string.Format("Approach_{0}", i), Guid.NewGuid(), this,
                    new DataArrivalHandler(delegate(object data, IInputPort port) { return OnVehicleArriving(data, approachIndex); })));
                m_outputs.Add(new SimpleOutputPort(model, string.Format("Output_{0}", i), Guid.NewGuid(), this, null, null));
                m_queues.Add(new Queue<object>());
            }
            m_approachesReadOnly = new ReadOnlyCollection<SimpleInputPort>(m_approaches);
            m_outputsReadOnly = new ReadOnlyCollection<SimpleOutputPort>(m_outputs);

            model.Starting += new ModelEvent(delegate(IModel theModel) {
                foreach (Queue<object> q in m_queues) q.Clear();
                m_occupied = false;
            });

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// The approach input ports. Approach 0 has the highest priority.
        /// </summary>
        public IList<SimpleInputPort> Approaches { get { return m_approachesReadOnly; } }

        /// <summary>
        /// The per-approach output ports; a vehicle that entered on approach i leaves via Outputs[i].
        /// </summary>
        public IList<SimpleOutputPort> Outputs { get { return m_outputsReadOnly; } }

        /// <summary>
        /// How long a vehicle occupies the zone while crossing.
        /// </summary>
        public TimeSpan CrossingTime { get { return m_crossingTime; } }

        /// <summary>
        /// Whether a vehicle currently occupies the zone.
        /// </summary>
        public bool IsOccupied { get { return m_occupied; } }

        /// <summary>
        /// The number of vehicles waiting (not crossing) on the specified approach.
        /// </summary>
        /// <param name="approach">The approach index.</param>
        public int QueueDepth(int approach) { return m_queues[approach].Count; }

        private bool OnVehicleArriving(object data, int approach) {
            m_queues[approach].Enqueue(data);
            TryAdmit();
            return true;
        }

        private void TryAdmit() {
            if (m_occupied) return;
            for (int i = 0; i < m_queues.Count; i++) {
                if (m_queues[i].Count > 0) {
                    m_occupied = true;
                    Crossing crossing = new Crossing { Vehicle = m_queues[i].Dequeue(), Approach = i };
                    m_model.Executive.RequestEvent(new ExecEventReceiver(OnZoneCleared), m_model.Executive.Now + m_crossingTime, 0.0, crossing);
                    return;
                }
            }
        }

        private void OnZoneCleared(IExecutive exec, object userData) {
            Crossing crossing = (Crossing)userData;
            m_occupied = false;
            m_outputs[crossing.Approach].OwnerPut(crossing.Vehicle);
            TryAdmit();
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
        /// A description of this ConflictZone.
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
