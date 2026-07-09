/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Transport;

namespace Highpoint.Sage.Transport {

    [TestFixture]
    public class TransportTester {

        private class TestVehicle : IVehicle {
            public TestVehicle(string name, double desiredSpeedMetersPerSecond) {
                Name = name;
                DesiredSpeedMetersPerSecond = desiredSpeedMetersPerSecond;
            }
            public string Name { get; private set; }
            public double DesiredSpeedMetersPerSecond { get; private set; }
            public override string ToString() { return Name; }
        }

        private class TendencyVehicle : TestVehicle, IDriverTendencies {
            public TendencyVehicle(string name, double desiredSpeedMetersPerSecond, bool tailgates, double speedLimitOffset, bool aggressiveAtStoplight)
                : base(name, desiredSpeedMetersPerSecond) {
                Tailgates = tailgates;
                PreferredSpeedLimitOffsetMetersPerSecond = speedLimitOffset;
                AggressiveAtStoplight = aggressiveAtStoplight;
            }
            public bool Tailgates { get; private set; }
            public double PreferredSpeedLimitOffsetMetersPerSecond { get; private set; }
            public bool AggressiveAtStoplight { get; private set; }
        }

        private static readonly DateTime t0 = new DateTime(2026, 1, 1, 12, 0, 0);

        private IModel m_model;

        [SetUp]
        public void Init() {
            m_model = new Model();
        }

        private void InjectAt(IInputPort into, TestVehicle vehicle, DateTime when) {
            m_model.Executive.RequestEvent(
                delegate(IExecutive exec, object userData) { into.Put(userData); },
                when, 0.0, vehicle);
        }

        private delegate void Probe();
        private void ProbeAt(DateTime when, Probe probe) {
            m_model.Executive.RequestEvent(
                delegate(IExecutive exec, object userData) { probe(); }, when, 0.0, null);
        }

        #region >>> Multi-lane segment <<<

        [Test]
        public void TestTwoLaneSegmentAllowsPassing() {
            // 500 m, two lanes, 2 s headway per lane.
            RoadSegment segment = new RoadSegment(m_model, "TwoLane", Guid.NewGuid(), 500.0, TimeSpan.FromSeconds(2), 2);
            VehicleLocationService locator = VehicleLocationService.For(m_model);

            List<TestVehicle> exited = new List<TestVehicle>();
            Dictionary<TestVehicle, DateTime> exitTimes = new Dictionary<TestVehicle, DateTime>();
            segment.Output.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                exited.Add((TestVehicle)data);
                exitTimes.Add((TestVehicle)data, m_model.Executive.Now);
            });

            TestVehicle truck = new TestVehicle("Truck", 10.0); // 50 s traversal.
            TestVehicle car = new TestVehicle("Car", 25.0);     // 20 s traversal.
            InjectAt(segment.Input, truck, t0);
            InjectAt(segment.Input, car, t0 + TimeSpan.FromSeconds(1));

            RoadLocation truckAt11 = null, carAt11 = null;
            ProbeAt(t0 + TimeSpan.FromSeconds(11), delegate {
                truckAt11 = locator.WhereIs(truck);
                carAt11 = locator.WhereIs(car);
            });

            m_model.Start();

            // The car took the free lane and passed the truck: it exits at its free-flow time,
            // 29 seconds ahead of the truck, despite entering one second later.
            Assert.AreEqual(new TestVehicle[] { car, truck }, exited.ToArray(), "The car should pass the truck via the second lane.");
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(21), exitTimes[car]);
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(50), exitTimes[truck]);

            // Mid-run, they report positions on different lanes of the same segment.
            StringAssert.EndsWith("Lane_0", truckAt11.Link.Name);
            StringAssert.EndsWith("Lane_1", carAt11.Link.Name);
            Assert.AreEqual(110.0, truckAt11.DistanceFromEntryMeters, 1e-9); // 11 s at 10 m/s.
            Assert.AreEqual(250.0, carAt11.DistanceFromEntryMeters, 1e-9);   // 10 s at 25 m/s, unimpeded.
            Assert.AreEqual(0, segment.Occupancy, "The segment should be empty after the run.");
        }

        [Test]
        public void TestPreferredSpeedIsRelativeToThePostedLimit() {
            // One mile of road, posted at 40 MPH.
            double oneMileMeters = 1609.344;
            RoadLink road = new RoadLink(m_model, "FortyLimit", Guid.NewGuid(), oneMileMeters, TimeSpan.FromSeconds(2), SpeedUnits.FromMilesPerHour(40.0));

            Dictionary<TestVehicle, DateTime> exitTimes = new Dictionary<TestVehicle, DateTime>();
            road.Output.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                exitTimes.Add((TestVehicle)data, m_model.Executive.Now);
            });

            // The preference travels with the driver: +10 means 50 in a 40 (and would mean 75
            // in a 65). The vehicle itself could do 80 MPH; the driver chooses not to.
            TendencyVehicle tenOver = new TendencyVehicle("TenOver", SpeedUnits.FromMilesPerHour(80.0), false, SpeedUnits.FromMilesPerHour(10.0), false);
            // -5 means 35 in a 40.
            TendencyVehicle fiveUnder = new TendencyVehicle("FiveUnder", SpeedUnits.FromMilesPerHour(80.0), false, SpeedUnits.FromMilesPerHour(-5.0), false);
            // This driver would happily do 60 in the 40, but the truck is governed at 45 MPH.
            TendencyVehicle governedTruck = new TendencyVehicle("Governed", SpeedUnits.FromMilesPerHour(45.0), false, SpeedUnits.FromMilesPerHour(20.0), false);

            // Widely spaced so no vehicle is paced by another's headway.
            InjectAt(road.Input, tenOver, t0);
            InjectAt(road.Input, fiveUnder, t0 + TimeSpan.FromSeconds(400));
            InjectAt(road.Input, governedTruck, t0 + TimeSpan.FromSeconds(800));

            m_model.Start();

            double mile = oneMileMeters;
            Assert.AreEqual(mile / SpeedUnits.FromMilesPerHour(50.0), (exitTimes[tenOver] - t0).TotalSeconds, 1e-6,
                "A +10 MPH driver covers a mile of a 40 MPH road at 50 MPH (72 s).");
            Assert.AreEqual(400.0 + mile / SpeedUnits.FromMilesPerHour(35.0), (exitTimes[fiveUnder] - t0).TotalSeconds, 1e-6,
                "A -5 MPH driver covers it at 35 MPH.");
            Assert.AreEqual(800.0 + mile / SpeedUnits.FromMilesPerHour(45.0), (exitTimes[governedTruck] - t0).TotalSeconds, 1e-6,
                "A +20 MPH preference cannot exceed the vehicle's own 45 MPH ceiling.");
        }

        [Test]
        public void TestSpeedLimitOffsetsAndTailgating() {
            // 1 km, 2 s headway, posted at 20 m/s.
            RoadLink link = new RoadLink(m_model, "PostedLink", Guid.NewGuid(), 1000.0, TimeSpan.FromSeconds(2), 20.0);

            List<TestVehicle> exited = new List<TestVehicle>();
            Dictionary<TestVehicle, DateTime> exitTimes = new Dictionary<TestVehicle, DateTime>();
            link.Output.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                exited.Add((TestVehicle)data);
                exitTimes.Add((TestVehicle)data, m_model.Executive.Now);
            });

            // Capable of 30 m/s, willing to run 5 over the 20 limit: travels at 25 -> 40 s.
            TendencyVehicle speeder = new TendencyVehicle("Speeder", 30.0, false, 5.0, false);
            // Capable of 25 but neutral: held to the 20 limit -> 50 s.
            TestVehicle compliant = new TestVehicle("Compliant", 25.0);
            // Same speed preference as the speeder, but follows at half the 2 s headway.
            TendencyVehicle tailgater = new TendencyVehicle("Tailgater", 30.0, true, 5.0, false);
            // Identical except for the tailgating: pays the full headway.
            TendencyVehicle politeFollower = new TendencyVehicle("Polite", 30.0, false, 5.0, false);

            InjectAt(link.Input, speeder, t0);
            InjectAt(link.Input, compliant, t0 + TimeSpan.FromSeconds(100));
            InjectAt(link.Input, tailgater, t0 + TimeSpan.FromSeconds(101));
            InjectAt(link.Input, politeFollower, t0 + TimeSpan.FromSeconds(102));

            m_model.Start();

            Assert.AreEqual(new TestVehicle[] { speeder, compliant, tailgater, politeFollower }, exited.ToArray());
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(40), exitTimes[speeder], "Limit 20 + offset 5 = 25 m/s over 1 km.");
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(150), exitTimes[compliant], "A neutral driver is held to the posted 20 m/s.");
            // Free-flow would be 141; held behind the compliant leader, but at only half the headway.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(151), exitTimes[tailgater], "A tailgater follows its leader at half the minimum headway.");
            // The polite follower pays the full 2 s behind the tailgater.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(153), exitTimes[politeFollower]);
        }

        #endregion

        #region >>> Signalized approach <<<

        [Test]
        public void TestSignalHoldsOnRedAndDischargesAtSaturation() {
            // Cycle: 30 s red, then 30 s green for approach 0, anchored at t0.
            TrafficSignal signal = new TrafficSignal("TestSignal", t0, new TrafficSignal.Phase[] {
                new TrafficSignal.Phase("Red", TimeSpan.FromSeconds(30)),
                new TrafficSignal.Phase("Green", TimeSpan.FromSeconds(30), 0)
            });
            Assert.IsFalse(signal.IsGreen(0, t0));
            Assert.IsTrue(signal.IsGreen(0, t0 + TimeSpan.FromSeconds(30)));
            Assert.IsFalse(signal.IsGreen(0, t0 + TimeSpan.FromSeconds(60)));
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(90), signal.NextGreen(0, t0 + TimeSpan.FromSeconds(60)));

            SignalizedApproach approach = new SignalizedApproach(m_model, "StopLine", Guid.NewGuid(), signal, 0, TimeSpan.FromSeconds(2));

            List<TestVehicle> discharged = new List<TestVehicle>();
            Dictionary<TestVehicle, DateTime> dischargeTimes = new Dictionary<TestVehicle, DateTime>();
            approach.Output.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                discharged.Add((TestVehicle)data);
                dischargeTimes.Add((TestVehicle)data, m_model.Executive.Now);
            });

            TestVehicle[] v = new TestVehicle[6];
            for (int i = 0; i < v.Length; i++) v[i] = new TestVehicle("V" + i, 25.0);

            InjectAt(approach.Input, v[0], t0);                              // Arrives on red.
            InjectAt(approach.Input, v[1], t0 + TimeSpan.FromSeconds(1));    // Queues behind v0.
            InjectAt(approach.Input, v[2], t0 + TimeSpan.FromSeconds(2));    // Queues behind v1.
            InjectAt(approach.Input, v[3], t0 + TimeSpan.FromSeconds(40));   // Arrives on green, no queue.
            InjectAt(approach.Input, v[4], t0 + TimeSpan.FromSeconds(59));   // Arrives just before red.
            InjectAt(approach.Input, v[5], t0 + TimeSpan.FromSeconds(59.5)); // Headway pushes it into red; waits a full cycle.

            m_model.Start();

            Assert.AreEqual(v, discharged.ToArray(), "Vehicles discharge in arrival order.");
            // Held on red until t0+30, then saturation discharge every 2 s.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(30), dischargeTimes[v[0]]);
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(32), dischargeTimes[v[1]]);
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(34), dischargeTimes[v[2]]);
            // Green, no queue, headway long since satisfied: passes immediately.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(40), dischargeTimes[v[3]]);
            // Still green at 59: passes.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(59), dischargeTimes[v[4]]);
            // Earliest permissible discharge (59+2=61) falls on red; waits for the next green.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(90), dischargeTimes[v[5]]);
            Assert.AreEqual(0, approach.QueueDepth);
        }

        [Test]
        public void TestYellowIndicationAndAggressiveDrivers() {
            // Cycle: 20 s green, 4 s yellow, 16 s red for approach 0, anchored at t0.
            TrafficSignal signal = new TrafficSignal("YellowSignal", t0, new TrafficSignal.Phase[] {
                new TrafficSignal.Phase("Green", TimeSpan.FromSeconds(20), 0),
                new TrafficSignal.Phase("Yellow", TimeSpan.FromSeconds(4), new int[0], new int[] { 0 }),
                new TrafficSignal.Phase("Red", TimeSpan.FromSeconds(16))
            });

            Assert.AreEqual(SignalIndication.Green, signal.Indication(0, t0 + TimeSpan.FromSeconds(10)));
            Assert.AreEqual(SignalIndication.Yellow, signal.Indication(0, t0 + TimeSpan.FromSeconds(21)));
            Assert.AreEqual(SignalIndication.Red, signal.Indication(0, t0 + TimeSpan.FromSeconds(30)));
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(40), signal.NextGreen(0, t0 + TimeSpan.FromSeconds(21)),
                "A driver who stops on yellow waits through it and the red.");
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(21), signal.NextGreenOrYellow(0, t0 + TimeSpan.FromSeconds(21)),
                "Yellow is immediately crossable for a driver willing to run it.");

            SignalizedApproach approach = new SignalizedApproach(m_model, "YellowStopLine", Guid.NewGuid(), signal, 0, TimeSpan.FromSeconds(2));

            List<TestVehicle> discharged = new List<TestVehicle>();
            Dictionary<TestVehicle, DateTime> dischargeTimes = new Dictionary<TestVehicle, DateTime>();
            approach.Output.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                discharged.Add((TestVehicle)data);
                dischargeTimes.Add((TestVehicle)data, m_model.Executive.Now);
            });

            // Arrives on yellow, stops for it, and waits through the red.
            TestVehicle cautious = new TestVehicle("Cautious", 25.0);
            InjectAt(approach.Input, cautious, t0 + TimeSpan.FromSeconds(21));
            // Arrives on the next cycle's yellow and runs it.
            TendencyVehicle aggressive = new TendencyVehicle("Aggressive", 25.0, false, 0.0, true);
            InjectAt(approach.Input, aggressive, t0 + TimeSpan.FromSeconds(61));
            // Arrives a second later; the headway lands it still on yellow, but it won't cross on yellow.
            TestVehicle follower = new TestVehicle("Follower", 25.0);
            InjectAt(approach.Input, follower, t0 + TimeSpan.FromSeconds(62));

            m_model.Start();

            Assert.AreEqual(new TestVehicle[] { cautious, aggressive, follower }, discharged.ToArray());
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(40), dischargeTimes[cautious], "A neutral driver stops on yellow and crosses on the next green.");
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(61), dischargeTimes[aggressive], "An aggressive driver crosses on yellow immediately.");
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(80), dischargeTimes[follower], "A neutral driver behind a yellow-runner still waits for green.");
        }

        #endregion

        #region >>> Conflict zone <<<

        [Test]
        public void TestConflictZonePriorityAndSerialization() {
            // A 4-second crossing; approach 0 (main) outranks approach 1 (side).
            ConflictZone zone = new ConflictZone(m_model, "Crossing", Guid.NewGuid(), TimeSpan.FromSeconds(4), 2);

            List<string> crossed = new List<string>();
            Dictionary<string, DateTime> crossedTimes = new Dictionary<string, DateTime>();
            for (int i = 0; i < 2; i++) {
                int approach = i;
                zone.Outputs[i].PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                    string tag = ((TestVehicle)data).Name + "/out" + approach;
                    crossed.Add(tag);
                    crossedTimes.Add(tag, m_model.Executive.Now);
                });
            }

            TestVehicle mainA = new TestVehicle("MainA", 25.0);
            TestVehicle mainB = new TestVehicle("MainB", 25.0);
            TestVehicle side = new TestVehicle("Side", 25.0);

            InjectAt(zone.Approaches[0], mainA, t0);
            InjectAt(zone.Approaches[1], side, t0 + TimeSpan.FromSeconds(0.5)); // Arrives before MainB...
            InjectAt(zone.Approaches[0], mainB, t0 + TimeSpan.FromSeconds(1));

            m_model.Start();

            // ...but the main road outranks it: the zone serves MainA, then MainB, then Side.
            Assert.AreEqual(new string[] { "MainA/out0", "MainB/out0", "Side/out1" }, crossed.ToArray(),
                "The zone admits by approach priority, not arrival order.");
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(4), crossedTimes["MainA/out0"]);
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(8), crossedTimes["MainB/out0"]);
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(12), crossedTimes["Side/out1"]);
            Assert.IsFalse(zone.IsOccupied);
        }

        #endregion

        #region >>> Illustrative example: the Main & 1st corridor <<<

        /// <summary>
        /// An illustrative corridor: vehicles travel a two-lane stretch of Main St (where fast
        /// cars pass slow trucks), queue at a signal, and cross the unsignalized intersection
        /// with 1st Ave, whose side-street traffic yields to Main. Prints a narrative timeline
        /// and location snapshots, and asserts conservation, signal discipline, and zone
        /// serialization.
        /// </summary>
        [Test]
        public void DemoMainStreetCorridor() {
            // ---- The network ----------------------------------------------------------
            // Two lanes, posted at 20 m/s.
            RoadSegment mainSt = new RoadSegment(m_model, "MainSt", Guid.NewGuid(), 600.0, TimeSpan.FromSeconds(2), 2, 20.0);

            TrafficSignal signal = new TrafficSignal("Main&1st Signal", t0, new TrafficSignal.Phase[] {
                new TrafficSignal.Phase("Main green", TimeSpan.FromSeconds(18), 0),
                new TrafficSignal.Phase("Main yellow", TimeSpan.FromSeconds(4), new int[0], new int[] { 0 }),
                new TrafficSignal.Phase("Main red", TimeSpan.FromSeconds(18))
            });
            SignalizedApproach stopLine = new SignalizedApproach(m_model, "MainStopLine", Guid.NewGuid(), signal, 0, TimeSpan.FromSeconds(2));

            ConflictZone crossing = new ConflictZone(m_model, "Main/1st Crossing", Guid.NewGuid(), TimeSpan.FromSeconds(3), 2);
            RoadLink firstAve = new RoadLink(m_model, "1stAve", Guid.NewGuid(), 200.0, TimeSpan.FromSeconds(2));

            ConnectorFactory.Connect(mainSt.Output, stopLine.Input);
            ConnectorFactory.Connect(stopLine.Output, crossing.Approaches[0]);
            ConnectorFactory.Connect(firstAve.Output, crossing.Approaches[1]);

            // ---- Instrumentation ------------------------------------------------------
            List<TestVehicle> clearedMain = new List<TestVehicle>();
            List<TestVehicle> clearedSide = new List<TestVehicle>();
            Dictionary<TestVehicle, DateTime> stopLineDischarges = new Dictionary<TestVehicle, DateTime>();
            List<DateTime> zoneClearances = new List<DateTime>();

            stopLine.Output.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                stopLineDischarges.Add((TestVehicle)data, m_model.Executive.Now);
                Console.WriteLine("{0:HH:mm:ss} | {1} crosses the Main St stop line (signal is {2}).",
                    m_model.Executive.Now, data, signal.Indication(0, m_model.Executive.Now));
            });
            crossing.Outputs[0].PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                clearedMain.Add((TestVehicle)data);
                zoneClearances.Add(m_model.Executive.Now);
                Console.WriteLine("{0:HH:mm:ss} | {1} clears the intersection, continuing on Main St.", m_model.Executive.Now, data);
            });
            crossing.Outputs[1].PortDataPresented += new PortDataEvent(delegate(object data, IPort where) {
                clearedSide.Add((TestVehicle)data);
                zoneClearances.Add(m_model.Executive.Now);
                Console.WriteLine("{0:HH:mm:ss} | {1} clears the intersection from 1st Ave (having yielded to Main).", m_model.Executive.Now, data);
            });

            // ---- The traffic ----------------------------------------------------------
            List<TestVehicle> everyone = new List<TestVehicle>();
            Action<TestVehicle, double, IInputPort> place = delegate(TestVehicle vehicle, double atSeconds, IInputPort into) {
                everyone.Add(vehicle);
                InjectAt(into, vehicle, t0 + TimeSpan.FromSeconds(atSeconds));
            };
            place(new TestVehicle("Car_1", 25.0), 0, mainSt.Input);
            place(new TestVehicle("Truck_1", 12.0), 2, mainSt.Input);
            place(new TestVehicle("Car_2", 25.0), 4, mainSt.Input);
            place(new TestVehicle("Truck_2", 12.0), 6, mainSt.Input);
            place(new TestVehicle("Car_3", 25.0), 8, mainSt.Input);
            // Car_4's driver prefers 10 MPH over any posted limit, tailgates, and runs yellows.
            place(new TendencyVehicle("Car_4", 30.0, true, SpeedUnits.FromMilesPerHour(10.0), true), 10, mainSt.Input);
            place(new TestVehicle("SideCar_1", 15.0), 5, firstAve.Input);
            place(new TestVehicle("SideCar_2", 15.0), 15, firstAve.Input);

            // ---- Location snapshots ---------------------------------------------------
            VehicleLocationService locator = VehicleLocationService.For(m_model);
            Probe snapshot = delegate {
                Console.WriteLine("{0:HH:mm:ss} | --- Where is everybody? ---", m_model.Executive.Now);
                foreach (TestVehicle vehicle in everyone) {
                    RoadLocation loc = locator.WhereIs(vehicle);
                    Console.WriteLine("         |   {0,-9} : {1}", vehicle, (object)loc ?? "not on a link (waiting, crossing, or done)");
                }
            };
            ProbeAt(t0 + TimeSpan.FromSeconds(15), snapshot);
            ProbeAt(t0 + TimeSpan.FromSeconds(45), snapshot);

            // ---- Run and verify -------------------------------------------------------
            m_model.Start();

            Assert.AreEqual(6, clearedMain.Count, "All six Main St vehicles should clear the intersection.");
            Assert.AreEqual(2, clearedSide.Count, "Both 1st Ave vehicles should clear the intersection.");

            foreach (KeyValuePair<TestVehicle, DateTime> discharge in stopLineDischarges) {
                SignalIndication shown = signal.Indication(0, discharge.Value);
                IDriverTendencies tendencies = discharge.Key as IDriverTendencies;
                bool runsYellow = tendencies != null && tendencies.AggressiveAtStoplight;
                Assert.IsTrue(shown == SignalIndication.Green || (shown == SignalIndication.Yellow && runsYellow),
                    string.Format("{0} crossed the stop line on {1} at {2}.", discharge.Key, shown, discharge.Value));
            }

            zoneClearances.Sort();
            for (int i = 1; i < zoneClearances.Count; i++) {
                Assert.IsTrue(zoneClearances[i] - zoneClearances[i - 1] >= crossing.CrossingTime,
                    "Zone clearances must be at least one crossing time apart (serialization).");
            }

            Assert.AreEqual(0, stopLine.QueueDepth);
            Assert.IsFalse(crossing.IsOccupied);
        }

        #endregion
    }
}
