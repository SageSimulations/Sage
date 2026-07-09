/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;

namespace Highpoint.Sage.Transport {

    /// <summary>
    /// Implemented by anything that traverses a RoadLink.
    /// </summary>
    public interface IVehicle {
        /// <summary>
        /// The fastest this vehicle travels, in meters per second: its speed on unposted links,
        /// and its physical ceiling everywhere. On a posted link, a driver's preferred speed is
        /// the posted limit plus their IDriverTendencies offset (if any), and this value only
        /// matters if the vehicle cannot go that fast (e.g. a governed truck).
        /// </summary>
        double DesiredSpeedMetersPerSecond { get; }
    }

    /// <summary>
    /// Optionally implemented (alongside IVehicle) by vehicles whose drivers have behavioral
    /// tendencies. Vehicles that do not implement this interface behave neutrally: full
    /// headway, posted speed limit, and stopping on yellow.
    /// </summary>
    public interface IDriverTendencies {
        /// <summary>
        /// If true, this driver follows at half the minimum headway - at link exits and at
        /// stop-line discharge.
        /// </summary>
        bool Tailgates { get; }

        /// <summary>
        /// The speed this person prefers to travel, expressed relative to whatever limit is
        /// posted: a driver with an offset of +4.5 m/s (about +10 MPH; see
        /// SpeedUnits.FromMilesPerHour) cruises 10 over on every posted road, one with a
        /// negative offset cruises under the limit everywhere. The preference follows the
        /// driver from road to road; only the vehicle's own top speed
        /// (DesiredSpeedMetersPerSecond) can keep it from being reached. Has no effect on
        /// unposted links.
        /// </summary>
        double PreferredSpeedLimitOffsetMetersPerSecond { get; }

        /// <summary>
        /// If true, this driver crosses a stop line on yellow; otherwise yellow is treated as red.
        /// </summary>
        bool AggressiveAtStoplight { get; }
    }

    /// <summary>
    /// Applies IDriverTendencies (or neutral defaults for vehicles without them) to the
    /// quantities the Transport blocks compute from vehicles.
    /// </summary>
    internal static class TendencyOps {

        /// <summary>
        /// The speed at which the vehicle actually travels a link. On a posted link this is
        /// the driver's preferred speed - the limit plus their offset - unless the vehicle
        /// physically cannot go that fast, in which case it travels at its top speed. On an
        /// unposted link it is the vehicle's DesiredSpeedMetersPerSecond.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="speedLimitMetersPerSecond">The link's posted limit; zero or less means unposted.</param>
        public static double EffectiveSpeed(IVehicle vehicle, double speedLimitMetersPerSecond) {
            double desired = vehicle.DesiredSpeedMetersPerSecond;
            if (speedLimitMetersPerSecond <= 0.0) return desired;
            IDriverTendencies tendencies = vehicle as IDriverTendencies;
            double willing = speedLimitMetersPerSecond + (tendencies == null ? 0.0 : tendencies.PreferredSpeedLimitOffsetMetersPerSecond);
            // A pathological negative offset must not stall (or reverse) the vehicle.
            if (willing < 0.1) willing = 0.1;
            return desired < willing ? desired : willing;
        }

        /// <summary>
        /// The headway the vehicle observes behind its predecessor: half the base headway for
        /// a tailgater, the full base headway otherwise.
        /// </summary>
        /// <param name="vehicle">The vehicle (any object; non-vehicles get the full headway).</param>
        /// <param name="baseHeadway">The component's minimum headway.</param>
        public static TimeSpan Headway(object vehicle, TimeSpan baseHeadway) {
            IDriverTendencies tendencies = vehicle as IDriverTendencies;
            if (tendencies != null && tendencies.Tailgates) return TimeSpan.FromTicks(baseHeadway.Ticks / 2);
            return baseHeadway;
        }

        /// <summary>
        /// Whether the driver crosses on the given indication: green for everyone, yellow only
        /// for the aggressive.
        /// </summary>
        /// <param name="vehicle">The vehicle (any object; non-vehicles stop on yellow).</param>
        /// <param name="indication">The signal indication shown.</param>
        public static bool MayCross(object vehicle, SignalIndication indication) {
            if (indication == SignalIndication.Green) return true;
            IDriverTendencies tendencies = vehicle as IDriverTendencies;
            return indication == SignalIndication.Yellow && tendencies != null && tendencies.AggressiveAtStoplight;
        }
    }

    /// <summary>
    /// A vehicle's position on a RoadLink at a point in simulation time: the link it occupies,
    /// and how far along it is from the link's entry (end A) toward its exit (end B).
    /// </summary>
    public class RoadLocation {
        internal RoadLocation(RoadLink link, double distanceFromEntryMeters) {
            Link = link;
            DistanceFromEntryMeters = distanceFromEntryMeters;
        }

        /// <summary>
        /// The link the vehicle is on.
        /// </summary>
        public RoadLink Link { get; private set; }

        /// <summary>
        /// The distance, in meters, the vehicle has progressed from the link's entry.
        /// </summary>
        public double DistanceFromEntryMeters { get; private set; }

        /// <summary>
        /// The fraction (0.0 at entry/end A, 1.0 at exit/end B) of the link the vehicle has traversed.
        /// </summary>
        public double FractionTraversed { get { return DistanceFromEntryMeters / Link.LengthMeters; } }

        public override string ToString() {
            return string.Format("{0}, {1:0.#}% from A to B", Link.Name, 100.0 * FractionTraversed);
        }
    }

    /// <summary>
    /// A model service that answers "where is this vehicle?" across all of the model's RoadLinks.
    /// RoadLinks register vehicle entries and exits with it automatically; obtain it via
    /// <code>model.GetService&lt;VehicleLocationService&gt;()</code> once any RoadLink exists.
    /// </summary>
    public class VehicleLocationService : IModelService {

        private readonly Dictionary<IVehicle, RoadLink> m_currentLinks = new Dictionary<IVehicle, RoadLink>();

        /// <summary>
        /// Gets the model's VehicleLocationService, creating and registering it if necessary.
        /// </summary>
        /// <param name="model">The model whose location service is desired.</param>
        public static VehicleLocationService For(IModel model) {
            try {
                return model.GetService<VehicleLocationService>();
            } catch (ArgumentException) {
                // IModel offers no TryGetService; GetService throws when the service is absent.
                VehicleLocationService svc = new VehicleLocationService();
                model.AddService(svc);
                return svc;
            }
        }

        /// <summary>
        /// Reports the location of the specified vehicle, or null if it is not currently on any link.
        /// </summary>
        /// <param name="vehicle">The vehicle sought.</param>
        public RoadLocation WhereIs(IVehicle vehicle) {
            RoadLink link;
            if (!m_currentLinks.TryGetValue(vehicle, out link)) return null;
            return link.LocationOf(vehicle);
        }

        internal void VehicleEntered(IVehicle vehicle, RoadLink link) { m_currentLinks[vehicle] = link; }
        internal void VehicleExited(IVehicle vehicle, RoadLink link) {
            RoadLink current;
            if (m_currentLinks.TryGetValue(vehicle, out current) && ReferenceEquals(current, link)) {
                m_currentLinks.Remove(vehicle);
            }
        }

        #region IModelService Members
        public void InitializeService(IModel model) {
            model.Starting += new ModelEvent(delegate(IModel theModel) { m_currentLinks.Clear(); });
        }
        public bool IsInitialized { get; set; }
        public bool InlineInitialization { get { return true; } }
        #endregion
    }

    /// <summary>
    /// A single-lane road segment with first-in-first-out (no passing) semantics, modeled as a
    /// mesoscopic link. Each vehicle entering at time t is scheduled to exit at
    /// <code>max(t + length/speed, previousScheduledExit + minimumHeadway)</code>
    /// so a fast vehicle entering behind a slow one is held to the slow one's pace plus the
    /// headway, and link discharge capacity (1/headway) emerges from the recursion. Vehicles
    /// enter via the Input port and are pushed out of the Output port at their exit times.
    /// A multi-lane segment is composed of several parallel RoadLinks behind a lane-choice
    /// policy; passing is modeled by that composition, not within a single link.
    /// </summary>
    public class RoadLink : IPortOwner, IModelObject {

        private class TransitRecord {
            public IVehicle Vehicle;
            public DateTime EntryTime;
            public double SpeedMetersPerSecond;
        }

        private readonly double m_lengthMeters;
        private readonly TimeSpan m_minimumHeadway;
        private readonly double m_speedLimitMetersPerSecond;
        private readonly SimpleInputPort m_input;
        private readonly SimpleOutputPort m_output;
        private readonly VehicleLocationService m_locationService;
        private readonly List<TransitRecord> m_transits = new List<TransitRecord>(); // Front of the list = front of the platoon.
        private DateTime m_lastScheduledExit = DateTime.MinValue;
        private int m_occupancy = 0;

        /// <summary>
        /// Creates a new instance of the <see cref="T:RoadLink"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object.</param>
        /// <param name="guid">The GUID of this object.</param>
        /// <param name="lengthMeters">The length of the link, in meters. Must be positive.</param>
        /// <param name="minimumHeadway">The minimum time separation between two successive
        /// vehicles crossing the link's exit. Must not be negative.</param>
        /// <param name="speedLimitMetersPerSecond">The posted speed limit, in meters per second.
        /// Zero (the default) means unposted: vehicles travel at their desired speed.</param>
        public RoadLink(IModel model, string name, Guid guid, double lengthMeters, TimeSpan minimumHeadway, double speedLimitMetersPerSecond = 0.0) {
            if (lengthMeters <= 0.0) throw new ArgumentException("A RoadLink must have a positive length.", "lengthMeters");
            if (minimumHeadway < TimeSpan.Zero) throw new ArgumentException("A RoadLink's minimum headway cannot be negative.", "minimumHeadway");
            if (speedLimitMetersPerSecond < 0.0) throw new ArgumentException("A RoadLink's speed limit cannot be negative; use zero for an unposted link.", "speedLimitMetersPerSecond");

            InitializeIdentity(model, name, null, guid);

            m_lengthMeters = lengthMeters;
            m_minimumHeadway = minimumHeadway;
            m_speedLimitMetersPerSecond = speedLimitMetersPerSecond;

            m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(OnVehicleEntering));
            m_output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null);

            m_locationService = VehicleLocationService.For(model);

            model.Starting += new ModelEvent(delegate(IModel theModel) {
                m_lastScheduledExit = DateTime.MinValue;
                m_occupancy = 0;
                m_transits.Clear();
            });

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// The port through which vehicles enter the link.
        /// </summary>
        public IInputPort Input { get { return m_input; } }

        /// <summary>
        /// The port out of which vehicles are pushed at their exit times.
        /// </summary>
        public IOutputPort Output { get { return m_output; } }

        /// <summary>
        /// The length of this link, in meters.
        /// </summary>
        public double LengthMeters { get { return m_lengthMeters; } }

        /// <summary>
        /// The minimum time separation between two successive vehicles crossing the link's exit.
        /// </summary>
        public TimeSpan MinimumHeadway { get { return m_minimumHeadway; } }

        /// <summary>
        /// The posted speed limit, in meters per second. Zero means unposted.
        /// </summary>
        public double SpeedLimitMetersPerSecond { get { return m_speedLimitMetersPerSecond; } }

        /// <summary>
        /// The number of vehicles currently on the link. Useful as a lane-choice criterion.
        /// </summary>
        public int Occupancy { get { return m_occupancy; } }

        private bool OnVehicleEntering(object data, IInputPort port) {
            IVehicle vehicle = data as IVehicle;
            if (vehicle == null) {
                throw new ArgumentException(string.Format("RoadLink {0} was presented {1}, which does not implement IVehicle.", Name, data == null ? "<null>" : data.GetType().FullName));
            }
            if (vehicle.DesiredSpeedMetersPerSecond <= 0.0 || double.IsNaN(vehicle.DesiredSpeedMetersPerSecond) || double.IsInfinity(vehicle.DesiredSpeedMetersPerSecond)) {
                throw new ArgumentException(string.Format("RoadLink {0} was presented a vehicle with non-positive or non-finite desired speed {1}.", Name, vehicle.DesiredSpeedMetersPerSecond));
            }
            double speed = TendencyOps.EffectiveSpeed(vehicle, m_speedLimitMetersPerSecond);

            DateTime now = m_model.Executive.Now;
            DateTime exitTime = ProjectedExit(vehicle);

            m_lastScheduledExit = exitTime;
            m_occupancy++;
            m_transits.Add(new TransitRecord { Vehicle = vehicle, EntryTime = now, SpeedMetersPerSecond = speed });
            m_locationService.VehicleEntered(vehicle, this);
            m_model.Executive.RequestEvent(new ExecEventReceiver(OnVehicleExiting), exitTime, 0.0, data);
            return true;
        }

        private void OnVehicleExiting(IExecutive exec, object userData) {
            m_occupancy--;
            IVehicle vehicle = (IVehicle)userData;
            // Exits occur in FIFO order, so this is normally the front of the platoon.
            for (int i = 0; i < m_transits.Count; i++) {
                if (ReferenceEquals(m_transits[i].Vehicle, vehicle)) { m_transits.RemoveAt(i); break; }
            }
            m_locationService.VehicleExited(vehicle, this);
            m_output.OwnerPut(userData);
        }

        /// <summary>
        /// The exit time this link would assign a vehicle of the given speed if it entered now.
        /// No-passing physics: the exit line cannot be crossed sooner than one minimum headway
        /// after the previous vehicle's scheduled crossing, so a fast follower inherits its
        /// leader's pace. This holds even across an empty link - two crossings can never be
        /// closer than the headway. Useful as a lane-choice criterion.
        /// </summary>
        /// <param name="speedMetersPerSecond">The entering vehicle's travel speed.</param>
        public DateTime ProjectedExit(double speedMetersPerSecond) {
            DateTime freeFlowExit = m_model.Executive.Now + TimeSpan.FromSeconds(m_lengthMeters / speedMetersPerSecond);
            DateTime pacedExit = m_lastScheduledExit + m_minimumHeadway;
            return freeFlowExit > pacedExit ? freeFlowExit : pacedExit;
        }

        /// <summary>
        /// The exit time this link would assign the specified vehicle if it entered now,
        /// accounting for the driver's tendencies: the vehicle travels at its effective speed
        /// under this link's posted limit, and a tailgater accepts half the minimum headway.
        /// </summary>
        /// <param name="vehicle">The prospective entrant.</param>
        public DateTime ProjectedExit(IVehicle vehicle) {
            double speed = TendencyOps.EffectiveSpeed(vehicle, m_speedLimitMetersPerSecond);
            DateTime freeFlowExit = m_model.Executive.Now + TimeSpan.FromSeconds(m_lengthMeters / speed);
            DateTime pacedExit = m_lastScheduledExit + TendencyOps.Headway(vehicle, m_minimumHeadway);
            return freeFlowExit > pacedExit ? freeFlowExit : pacedExit;
        }

        /// <summary>
        /// Reports where the specified vehicle currently is on this link, or null if it is not
        /// on this link. Position is computed on demand from the mesoscopic schedule: a vehicle
        /// progresses at its desired speed but can advance no further than the vehicle ahead of
        /// it, so held vehicles stack up behind their leader rather than reaching the exit early.
        /// </summary>
        /// <param name="vehicle">The vehicle sought.</param>
        public RoadLocation LocationOf(IVehicle vehicle) {
            DateTime now = m_model.Executive.Now;
            double leaderPosition = double.MaxValue;
            foreach (TransitRecord tr in m_transits) {
                double position = tr.SpeedMetersPerSecond * (now - tr.EntryTime).TotalSeconds;
                if (position > m_lengthMeters) position = m_lengthMeters;
                if (position > leaderPosition) position = leaderPosition;
                if (position < 0.0) position = 0.0;
                if (ReferenceEquals(tr.Vehicle, vehicle)) return new RoadLocation(this, position);
                leaderPosition = position;
            }
            return null;
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
        /// A description of this RoadLink.
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
