/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;

namespace Highpoint.Sage.Transport {

    /// <summary>
    /// Chooses which lane an entering vehicle takes on a multi-lane RoadSegment.
    /// </summary>
    /// <param name="vehicle">The entering vehicle.</param>
    /// <param name="lanes">The segment's lanes, index 0 being the "first" (e.g. rightmost) lane.</param>
    /// <returns>The lane the vehicle is to take.</returns>
    public delegate RoadLink LaneSelector(IVehicle vehicle, IList<RoadLink> lanes);

    /// <summary>
    /// A road segment with one or more parallel lanes, each an independent FIFO RoadLink.
    /// An entering vehicle is assigned a lane by a LaneSelector policy (by default, the lane
    /// offering it the earliest projected exit), and vehicles from all lanes emerge from the
    /// segment's single Output port. Passing emerges from the composition: a fast vehicle
    /// behind a slow one selects a freer lane and exits earlier, while within each lane the
    /// no-passing FIFO invariant holds.
    /// </summary>
    public class RoadSegment : IPortOwner, IModelObject {

        private readonly List<RoadLink> m_lanes = new List<RoadLink>();
        private readonly ReadOnlyCollection<RoadLink> m_lanesReadOnly;
        private readonly LaneSelector m_laneSelector;
        private readonly SimpleInputPort m_input;
        private readonly SimpleOutputPort m_output;

        /// <summary>
        /// Creates a new instance of the <see cref="T:RoadSegment"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object.</param>
        /// <param name="guid">The GUID of this object.</param>
        /// <param name="lengthMeters">The length of the segment, in meters. Must be positive.</param>
        /// <param name="minimumHeadway">The minimum time separation between two successive
        /// vehicles crossing a lane's exit. Must not be negative.</param>
        /// <param name="laneCount">The number of parallel lanes. Must be positive.</param>
        /// <param name="speedLimitMetersPerSecond">The posted speed limit, in meters per second,
        /// applied to every lane. Zero (the default) means unposted.</param>
        /// <param name="laneSelector">The lane-choice policy. If null, each vehicle takes the
        /// lane offering it the earliest projected exit (ties to the lowest-indexed lane).</param>
        public RoadSegment(IModel model, string name, Guid guid, double lengthMeters, TimeSpan minimumHeadway, int laneCount, double speedLimitMetersPerSecond = 0.0, LaneSelector laneSelector = null) {
            if (laneCount < 1) throw new ArgumentException("A RoadSegment must have at least one lane.", "laneCount");

            InitializeIdentity(model, name, null, guid);

            for (int i = 0; i < laneCount; i++) {
                RoadLink lane = new RoadLink(model, string.Format("{0}.Lane_{1}", name, i), Guid.NewGuid(), lengthMeters, minimumHeadway, speedLimitMetersPerSecond);
                lane.Output.PortDataPresented += new PortDataEvent(OnVehicleLeavingLane);
                m_lanes.Add(lane);
            }
            m_lanesReadOnly = new ReadOnlyCollection<RoadLink>(m_lanes);
            m_laneSelector = laneSelector ?? new LaneSelector(EarliestProjectedExit);

            m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(OnVehicleEntering));
            m_output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null);

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// The port through which vehicles enter the segment.
        /// </summary>
        public IInputPort Input { get { return m_input; } }

        /// <summary>
        /// The port out of which vehicles are pushed, regardless of the lane they traveled.
        /// </summary>
        public IOutputPort Output { get { return m_output; } }

        /// <summary>
        /// The segment's lanes. Each is an independent FIFO RoadLink; useful for inspecting
        /// occupancies or projected exits, e.g. from a custom LaneSelector.
        /// </summary>
        public IList<RoadLink> Lanes { get { return m_lanesReadOnly; } }

        /// <summary>
        /// The number of vehicles currently on the segment, across all lanes.
        /// </summary>
        public int Occupancy {
            get {
                int n = 0;
                foreach (RoadLink lane in m_lanes) n += lane.Occupancy;
                return n;
            }
        }

        private static RoadLink EarliestProjectedExit(IVehicle vehicle, IList<RoadLink> lanes) {
            RoadLink best = lanes[0];
            DateTime bestExit = best.ProjectedExit(vehicle);
            for (int i = 1; i < lanes.Count; i++) {
                DateTime exit = lanes[i].ProjectedExit(vehicle);
                if (exit < bestExit) {
                    best = lanes[i];
                    bestExit = exit;
                }
            }
            return best;
        }

        private bool OnVehicleEntering(object data, IInputPort port) {
            IVehicle vehicle = data as IVehicle;
            if (vehicle == null) {
                throw new ArgumentException(string.Format("RoadSegment {0} was presented {1}, which does not implement IVehicle.", Name, data == null ? "<null>" : data.GetType().FullName));
            }
            RoadLink lane = m_laneSelector(vehicle, m_lanesReadOnly);
            if (lane == null || !m_lanes.Contains(lane)) {
                throw new InvalidOperationException(string.Format("The lane selector for RoadSegment {0} returned a lane that does not belong to the segment.", Name));
            }
            return lane.Input.Put(data);
        }

        private void OnVehicleLeavingLane(object data, IPort where) {
            m_output.OwnerPut(data);
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
        /// A description of this RoadSegment.
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
