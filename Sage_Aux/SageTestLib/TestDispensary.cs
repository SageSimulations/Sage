/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Highpoint.Sage.Materials.Chemistry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Randoms;


namespace Highpoint.Sage.Materials {

    /// <summary>
    /// Summary description for zTestDispensary.
    /// </summary>
    [TestClass]
    public class DispensaryTester {

        #region Private Fields
        private static readonly double AMBIENT_TEMPERATURE = 27.0;
        private IModel m_model;
        Dispensary m_dispensary;
        private MaterialType m_mt1;
        private MaterialType m_mt2;
        #endregion Private Fields

        [TestInitialize] 
		public void Init() {
            m_model = new Model();
            m_dispensary = new Dispensary(m_model.Executive);
            m_mt1 = new MaterialType(m_model, "Ethanol", Guid.NewGuid(), 1.5000, 3.2500, MaterialState.Liquid);
            m_mt2 = new MaterialType(m_model, "Cyclohexane", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid);

		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Simple set of puts and takes.")]
        public void TestBaseFunctionality() {
            m_model.Executive.ExecutiveStarted_SingleShot += new ExecutiveEvent(Executive_ExecutiveStarted_SingleShot1);
            m_model.Start();
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Simple set of puts and takes.")]
        public void TestRandomFunctionality() {
            m_model.Executive.ExecutiveStarted_SingleShot += new ExecutiveEvent(Executive_ExecutiveStarted_SingleShot2);
            m_model.Start();
        }

        void Executive_ExecutiveStarted_SingleShot1(IExecutive exec) {
            AddSomeMaterial(new DateTime(2008, 08, 01, 12, 00, 00), m_mt1.CreateMass(10, AMBIENT_TEMPERATURE));
            RequestMaterial(new DateTime(2008, 08, 01, 13, 00, 00), 10);
            ConfirmTotlMass(new DateTime(2008, 08, 01, 14, 00, 00), 0);
            AddSomeMaterial(new DateTime(2008, 08, 01, 15, 00, 00), m_mt1.CreateMass(10, AMBIENT_TEMPERATURE));
            RequestMaterial(new DateTime(2008, 08, 01, 16, 00, 00), 20);
            ConfirmTotlMass(new DateTime(2008, 08, 01, 17, 00, 00), 10);
            AddSomeMaterial(new DateTime(2008, 08, 01, 18, 00, 00), m_mt1.CreateMass(8, AMBIENT_TEMPERATURE));
            AddSomeMaterial(new DateTime(2008, 08, 01, 19, 00, 00), m_mt1.CreateMass(10, AMBIENT_TEMPERATURE));
            ConfirmTotlMass(new DateTime(2008, 08, 01, 20, 00, 00), 8);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            AddSomeMaterial(new DateTime(2008, 08, 01, 21, 00, 00), m_mt1.CreateMass(33, AMBIENT_TEMPERATURE));
            ConfirmTotlMass(new DateTime(2008, 08, 01, 22, 00, 00), 1);
        }

        private double m_howMuchPut;
        private double m_howMuchRetrieved;
        void Executive_ExecutiveStarted_SingleShot2(IExecutive exec) {
            RandomServer r = new RandomServer(12345, 1000);
            Randoms.IRandomChannel rc = r.GetRandomChannel(98765, 1000);

            double howMuch = 0.0;
            DateTime when = new DateTime(2008, 08, 01, 12, 00, 00);
            for (int i = 0 ; i < 1000 ; i++) {
                int key = rc.Next(0, 2);
                int deltaT = rc.Next(0,2); 
                howMuch = rc.NextDouble() * 100.0;
                switch (key) {
                    case 0:
                        m_howMuchPut += howMuch;
                        Console.WriteLine("{0} : Add {1} kg.", when, howMuch);
                        AddSomeMaterial(when, m_mt1.CreateMass(howMuch, AMBIENT_TEMPERATURE));
                        break;
                    case 1:
                        m_howMuchRetrieved += howMuch;
                        Console.WriteLine("{0} : Try to remove {1} kg.", when, howMuch);
                        RequestMaterial(when, howMuch);
                        break;
                    case 2:
                        Console.WriteLine("{0} : Confirm bookkeeping.", when);
                        ConfirmTotlMass(when);
                        break;
                    default:
                        break;
                }
                when += TimeSpan.FromMinutes(deltaT);
            }

            // Now, if there are outstanding requests, satisfy them.
            howMuch = m_howMuchRetrieved - m_howMuchPut;
            if (howMuch > 0) {
                Console.WriteLine("{0} : Add {1} kg.", when, howMuch);
                AddSomeMaterial(when, m_mt1.CreateMass(howMuch, AMBIENT_TEMPERATURE));
            }

            m_howMuchPut = 0.0;
            m_howMuchRetrieved = 0.0;

            Console.WriteLine("Starting Test...");
        }

        private void ConfirmTotlMass(DateTime dateTime) {
            m_model.Executive.RequestEvent(new ExecEventReceiver(delegate(IExecutive exec, object userData) {
                double expectedMass = m_howMuchPut - m_howMuchRetrieved;
                Console.WriteLine("{0} : Expect mass = {1} kg. in dispensary - now contains {2} kg.", exec.Now, expectedMass, m_dispensary.PeekMixture.Mass);
                Assert.AreEqual(expectedMass, m_dispensary.PeekMixture.Mass);
            }), dateTime, 0.0, null, ExecEventType.Detachable);
        }

        private void AddSomeMaterial(DateTime dateTime, IMaterial iMaterial) {
            m_model.Executive.RequestEvent(new ExecEventReceiver(delegate(IExecutive exec, object userData) {
                m_howMuchPut += iMaterial.Mass;
                m_dispensary.Put(iMaterial);
                Console.WriteLine("{0} : Added {1} kg. to dispensary - now contains {2}.", exec.Now, iMaterial.ToString(), m_dispensary.PeekMixture.ToString());
            }),dateTime,0.0,null,ExecEventType.Detachable);
        }

        private void RequestMaterial(DateTime dateTime, double mass) {
            m_model.Executive.RequestEvent(new ExecEventReceiver(delegate(IExecutive exec, object userData) {
                Console.WriteLine("{0} : Requested {1} kg. from dispensary - now contains {2}.", exec.Now, mass, m_dispensary.PeekMixture.ToString());
                Mixture m = m_dispensary.Get(mass);
                m_howMuchRetrieved += mass;
                Console.WriteLine("{0} : Received {1} kg. from dispensary - now contains {2}.", exec.Now, mass, m_dispensary.PeekMixture.ToString());
            }), dateTime, 0.0, null, ExecEventType.Detachable);
        }

        private void ConfirmTotlMass(DateTime dateTime, double expectedMass) {
            m_model.Executive.RequestEvent(new ExecEventReceiver(delegate(IExecutive exec, object userData) {
                Console.WriteLine("{0} : Expect mass = {1} kg. in dispensary - now contains {2} kg.", exec.Now, expectedMass, m_dispensary.PeekMixture.Mass);
                Assert.AreEqual(expectedMass, m_dispensary.PeekMixture.Mass);
            }), dateTime, 0.0, null, ExecEventType.Detachable);
        }
    }
}