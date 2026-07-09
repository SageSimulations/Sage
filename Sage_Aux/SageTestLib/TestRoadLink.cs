/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Transport;

namespace Highpoint.Sage.Transport {

    [TestFixture]
    public class RoadLinkTester {

        private class TestVehicle : IVehicle {
            public TestVehicle(string name, double desiredSpeedMetersPerSecond) {
                Name = name;
                DesiredSpeedMetersPerSecond = desiredSpeedMetersPerSecond;
            }
            public string Name { get; private set; }
            public double DesiredSpeedMetersPerSecond { get; private set; }
        }

        private static readonly DateTime t0 = new DateTime(2026, 1, 1, 12, 0, 0);

        private IModel m_model;
        private RoadLink m_link;
        private List<TestVehicle> m_exitedVehicles;
        private Dictionary<TestVehicle, DateTime> m_exitTimes;

        [SetUp]
        public void Init() {
            m_model = new Model();
            // 1 km single-lane link; exit can discharge one vehicle every 2 seconds.
            m_link = new RoadLink(m_model, "TestLink", Guid.NewGuid(), 1000.0, TimeSpan.FromSeconds(2));
            m_exitedVehicles = new List<TestVehicle>();
            m_exitTimes = new Dictionary<TestVehicle, DateTime>();
            m_link.Output.PortDataPresented += new PortDataEvent(OnVehicleExited);
        }

        private void OnVehicleExited(object data, IPort where) {
            TestVehicle v = (TestVehicle)data;
            m_exitedVehicles.Add(v);
            m_exitTimes.Add(v, m_model.Executive.Now);
            Console.WriteLine("{0} : {1} exited.", m_model.Executive.Now, v.Name);
        }

        private void InjectAt(TestVehicle vehicle, DateTime when) {
            m_model.Executive.RequestEvent(
                delegate(IExecutive exec, object userData) { m_link.Input.Put(userData); },
                when, 0.0, vehicle);
        }

        [Test]
        public void TestUnimpededTraversal() {
            TestVehicle car = new TestVehicle("Car", 25.0); // 1000 m at 25 m/s = 40 s.
            InjectAt(car, t0);

            m_model.Start();

            Assert.AreEqual(1, m_exitedVehicles.Count, "Exactly one vehicle should have exited.");
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(40), m_exitTimes[car], "An unimpeded vehicle exits at entry + length/speed.");
            Assert.AreEqual(0, m_link.Occupancy, "The link should be empty after the run.");
        }

        [Test]
        public void TestFastFollowerIsHeldBehindSlowLeader() {
            TestVehicle slow = new TestVehicle("Slow", 10.0); // Traverses in 100 s.
            TestVehicle fast = new TestVehicle("Fast", 25.0); // Would traverse in 40 s.
            TestVehicle late = new TestVehicle("Late", 25.0); // Enters after the link clears.

            InjectAt(slow, t0);
            InjectAt(fast, t0 + TimeSpan.FromSeconds(1));
            InjectAt(late, t0 + TimeSpan.FromSeconds(200));

            m_model.Start();

            Assert.AreEqual(3, m_exitedVehicles.Count);
            Assert.AreEqual(new TestVehicle[] { slow, fast, late }, m_exitedVehicles.ToArray(),
                "Vehicles must exit a single-lane link in the order they entered (no passing).");

            Assert.AreEqual(t0 + TimeSpan.FromSeconds(100), m_exitTimes[slow], "The slow leader exits at its free-flow time.");
            // Free-flow would be t0+41; the follower is held to the leader's exit plus one headway.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(102), m_exitTimes[fast], "The fast follower exits one headway behind the slow leader.");
            // The late entrant is unconstrained: its free-flow exit (t0+240) is far beyond the last exit + headway.
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(240), m_exitTimes[late], "A vehicle entering an open road travels at free-flow speed.");
        }

        [Test]
        public void TestVehiclesReportTheirLocations() {
            VehicleLocationService locator = VehicleLocationService.For(m_model);

            TestVehicle slow = new TestVehicle("Slow", 10.0); // 300 m into the link at t0+30.
            TestVehicle fast = new TestVehicle("Fast", 25.0); // Catches the slow leader and is capped by it.
            TestVehicle late = new TestVehicle("Late", 25.0); // Travels an open road.

            InjectAt(slow, t0);
            InjectAt(fast, t0 + TimeSpan.FromSeconds(1));
            InjectAt(late, t0 + TimeSpan.FromSeconds(200));

            Dictionary<string, RoadLocation> probes = new Dictionary<string, RoadLocation>();
            ProbeAt(t0 + TimeSpan.FromSeconds(0.5), delegate { probes["fast@0.5"] = locator.WhereIs(fast); });
            ProbeAt(t0 + TimeSpan.FromSeconds(10), delegate {
                probes["slow@10"] = locator.WhereIs(slow);
                probes["fast@10"] = locator.WhereIs(fast);
            });
            ProbeAt(t0 + TimeSpan.FromSeconds(50), delegate {
                probes["slow@50"] = locator.WhereIs(slow);
                probes["fast@50"] = locator.WhereIs(fast);
            });
            ProbeAt(t0 + TimeSpan.FromSeconds(150), delegate { probes["slow@150"] = locator.WhereIs(slow); });
            ProbeAt(t0 + TimeSpan.FromSeconds(220), delegate { probes["late@220"] = locator.WhereIs(late); });

            m_model.Start();

            Assert.IsNull(probes["fast@0.5"], "A vehicle that has not yet entered any link has no location.");
            Assert.IsNull(probes["slow@150"], "A vehicle that has exited has no location.");

            // At t0+10 both are in free flow: slow has gone 100 m, fast (9 s at 25 m/s) 225 m... but the
            // fast vehicle cannot be ahead of its leader, so it is capped at the slow vehicle's 100 m.
            Assert.AreEqual(100.0, probes["slow@10"].DistanceFromEntryMeters, 1e-9);
            Assert.AreEqual(100.0, probes["fast@10"].DistanceFromEntryMeters, 1e-9,
                "A follower's position is capped by its leader's position (no passing).");

            // At t0+50 the slow leader is at 500 m (50%); the fast follower remains pinned behind it.
            Assert.AreEqual(0.5, probes["slow@50"].FractionTraversed, 1e-9);
            Assert.AreEqual(0.5, probes["fast@50"].FractionTraversed, 1e-9);

            // The late vehicle has an open road: 20 s at 25 m/s = 500 m.
            Assert.AreEqual(500.0, probes["late@220"].DistanceFromEntryMeters, 1e-9);

            StringAssert.Contains("TestLink", probes["slow@50"].ToString());
            StringAssert.Contains("% from A to B", probes["slow@50"].ToString());
            Console.WriteLine("Location report sample: " + probes["slow@50"]);
        }

        private delegate void Probe();
        private void ProbeAt(DateTime when, Probe probe) {
            m_model.Executive.RequestEvent(
                delegate(IExecutive exec, object userData) { probe(); }, when, 0.0, null);
        }

        [Test]
        public void TestPlatoonDischargesAtSaturationHeadway() {
            TestVehicle leader = new TestVehicle("Leader", 10.0); // Exits at t0+100.
            InjectAt(leader, t0);

            TestVehicle[] platoon = new TestVehicle[3];
            for (int i = 0; i < platoon.Length; i++) {
                platoon[i] = new TestVehicle("Follower_" + i, 25.0);
                InjectAt(platoon[i], t0 + TimeSpan.FromSeconds(i + 1));
            }

            m_model.Start();

            Assert.AreEqual(4, m_exitedVehicles.Count);
            Assert.AreEqual(t0 + TimeSpan.FromSeconds(100), m_exitTimes[leader]);
            for (int i = 0; i < platoon.Length; i++) {
                // Queued followers discharge exactly one headway apart: 102, 104, 106.
                Assert.AreEqual(t0 + TimeSpan.FromSeconds(102 + (2 * i)), m_exitTimes[platoon[i]],
                    platoon[i].Name + " should discharge at the saturation headway behind its predecessor.");
            }
        }
    }
}
