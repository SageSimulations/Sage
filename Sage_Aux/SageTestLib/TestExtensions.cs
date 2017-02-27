/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Linq;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Utility;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility {

    [TestClass]
    public class ExtensionTester {

        #region Private Fields
        #endregion Private Fields

        public ExtensionTester() { Init(); }

        [TestInitialize]
        public void Init() { }

        [TestCleanup]
        public void destroy() { Trace.WriteLine("Done."); }

        /// <summary>
        /// Tests the PercentileGetter extension.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Tests the Byte XOR extension.")]
        public void TestByteXOR() {
            byte[] ba1 = new byte[] { 0xF0, 0xF0 };
            byte[] ba2 = new byte[] { 0x0F, 0x0F };
            byte[] ba3 = ba1.XOR(ba2);
            Assert.AreEqual(ba3[0], 0xFF, "Comparison 1a.");
            Assert.AreEqual(ba3[1], 0xFF, "Comparison 1b.");

            ba2 = new byte[] { 0xFF, 0xFF };
            ba3 = ba1.XOR(ba2);
            Assert.AreEqual(ba3[0], 0x0F, "Comparison 2a.");
            Assert.AreEqual(ba3[1], 0x0F, "Comparison 2b.");

            ba3 = ba2.XOR(ba2);
            Assert.AreEqual(ba3[0], 0x00, "Comparison 3a.");
            Assert.AreEqual(ba3[1], 0x00, "Comparison 3b.");
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Tests the CommasAndAndedList operations.")]
        public void TestCommasAndAndedListOperations() {

            List<string> strings = new List<string>(new string[]{"Cat", "Dog", "Horse" ,"Cow"});
            string[] results = new string[]{
                "Cat",
                "Cat and Dog",
                "",
                "Cat, Dog, Horse and Cow"};

            foreach (int count in new int[] { 1, 2, 4 }) {
                List<string> tmp = new List<string>();
                for (int i = 0; i < count; i++) { tmp.Add(strings[i]); }
                string result = StringOperations.ToCommasAndAndedList(((IEnumerable<string>)tmp));
                System.Diagnostics.Debug.Assert(result.Equals(results[count - 1]));
                Console.WriteLine(result);

            }

            foreach (int count in new int[] { 1, 2, 4 }) {
                ArrayList tmp = new ArrayList();
                for (int i = 0; i < count; i++) { tmp.Add(strings[i]); }
                string result = StringOperations.ToCommasAndAndedList(tmp);
                System.Diagnostics.Debug.Assert(result.Equals(results[count - 1]));
                Console.WriteLine(result);
            }

            foreach (int count in new int[] { 1, 2, 4 }) {
                List<Thingy> tmp = new List<Thingy>();
                for (int i = 0; i < count; i++) { tmp.Add(new Thingy(strings[i])); }
                string result = StringOperations.ToCommasAndAndedListOfNames(tmp);
                System.Diagnostics.Debug.Assert(result.Equals(results[count - 1]));
                Console.WriteLine(result);
            }

            foreach (int count in new int[] { 1, 2, 4 }) {
                List<Thingy> tmp = new List<Thingy>();
                for (int i = 0; i < count; i++) { tmp.Add(new Thingy(strings[i])); }
                string result = StringOperations.ToCommasAndAndedList(tmp,n=>n.Name);
                System.Diagnostics.Debug.Assert(result.Equals(results[count - 1]));
                Console.WriteLine(result);
            }

        }

        class Thingy : Highpoint.Sage.SimCore.IHasName {
            private string m_name;
            public Thingy(string name) { m_name = name; }
            public string Name { get { return m_name; } }
        }
    }
}

namespace Highpoint.Sage.Mathematics {

    [TestClass]
    public class ExtensionTester {

        #region Private Fields
        #endregion Private Fields

        public ExtensionTester() { Init(); }

        [TestInitialize]
        public void Init() { }

        [TestCleanup]
        public void destroy() { Trace.WriteLine("Done."); }

        /// <summary>
        /// Tests the PercentileGetter extension.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Tests the PercentileGetter extension.")]
        public void TestPercentileGetter() {
            double[] testData = new double[] { -99.9, -.4, .9, 1.1, 3.6, 12.5, 42.2 };
            TestPG(testData, new double[] { 0, 50, 100 }, new double[] { -99.9, 1.1, 42.2 }, false);
            TestPG(testData, new double[] { 0, 50, 100 }, new double[] { -99.9, 1.1, 42.2 }, true);

            TestPG(testData, new double[] { 0, 50, 100 }, new double[] { -99.9, 1.1, 42.2 }, false);
            TestPG(testData, new double[] {10, 60, 95}, new double[] {-40.2, 2.6, 33.29}, true);

        }

        /// <summary>
        /// Tests the PercentileGetter extension.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Tests the  BoundBySigmas extension.")]
        public void TestSigmaBounding() {

            double[] testData = new double[] { -99.9, -.4, .9, 1.1, 3.6, 12.5, 42.2, 67.8, 123.0 };
            double[] loBound = new double[] { 3, 3, 2, 1, .5, .25 };
            double[] hiBound = new double[] { 3, 2, 1, 3, .5, .25 };
            int[] expecteds = new int[] { 9, 9, 7, 8, 6, 2 };

            TestSigmaBounding(testData, expecteds, loBound, hiBound);
        }

        private void TestPG(double[] srcData, double[] targets, double[] expecteds, bool interpolate) {
            List<Thingy> thingies = new List<Thingy>();
            Func<Thingy, double> valueGetter = n => n.DoubleValue;

            foreach (double d in srcData) {
                thingies.Add(new Thingy(d));
            }

            Console.WriteLine("Created a list of Thingies, {0}.", StringOperations.ToCommasAndAndedList(thingies.ConvertAll<string>(n => n.ToString())));

            for (int i = 0; i < targets.Length; i++) {
                double result = thingies.GetValueAtPercentile<Thingy>(targets[i], valueGetter, interpolate);

                Console.WriteLine(" > {0} value at percentile {1} was {2} - expected {3}."
                    , (interpolate ? "Interpolated" : "Uninterpolated"), targets[i], result, expecteds[i]);

                Assert.IsTrue(Math.Abs((result - expecteds[i]) / result) < 1E-8,
                    string.Format("Getting {0} percentile returned {1}, should have returned {2}.", targets[i], result, expecteds[i]));
            }
        }

        private void TestSigmaBounding(double[] srcData, int[] expecteds, double[] loBounds, double[] hiBounds) {
            
            List<Thingy> thingies = new List<Thingy>();
            foreach (double d in srcData) { thingies.Add(new Thingy(d)); }

            Console.WriteLine("Created a list of Thingies, {0}.", StringOperations.ToCommasAndAndedList(thingies.ConvertAll<string>(n => n.ToString())));

            double average = thingies.Average<Thingy>(n => n.DoubleValue);
            double stDev = thingies.StandardDeviation<Thingy>(n => n.DoubleValue);

            Console.WriteLine("Mean = {0}\r\nStDev = {1}", average, stDev);

            for (int i = 0; i < expecteds.Length; i++) {
                object state = null;
                IEnumerable<Thingy> boundedThingies = thingies.BoundBySigmas<Thingy>(n => n.DoubleValue, loBounds[i], hiBounds[i], ref state);
                IEnumerable<Thingy> enumerable = boundedThingies as Thingy[] ?? boundedThingies.ToArray();
                int numBoundedThingies = enumerable.Count();
                Assert.AreEqual(expecteds[i], numBoundedThingies, string.Format("ERROR: Thingy list bounded -{0} to +{1} should have yielded {2} elements, and yielded {3} instead.", loBounds[i], hiBounds[i], expecteds[i], numBoundedThingies));
            }
        }

        class Thingy {
            private string val;
            private double dblval;

            public Thingy(double _val) {
                dblval = _val;
                val = dblval.ToString();
            }

            public override string ToString() {
                return "\"" + val + "\"";
            }

            public double DoubleValue { get { return dblval; } }
        }
    }
}
