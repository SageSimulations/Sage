/* This source code licensed under the GNU Affero General Public License */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Highpoint.Sage.Mathematics;

namespace SageTestLib {

    [TestClass]
    public class zTestRationalizer {
        [TestInitialize]
        public void Init() {
        }
        [TestCleanup]
        public void destroy() {
            Debug.WriteLine("Done.");
        }
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test checks the basic function of a rationalizer to five digits, and fractions up to ninths.")]
        public void TestBaseFunctionality() {
            Rationalizer r = new Rationalizer(9, 5); // Up to ninths, out to 5 places.

            double[][] testVals = new double[][]{
                new double[]{5.666674, 5.666674},
                new double[]{5.6666674, 5.66666666666666666666666666},
                new double[]{-5.666674, -5.666674},
                new double[]{-5.6666674, -5.666666666666666666666666},
                new double[]{4.333376, 4.333376},
                new double[]{4.3333376, 4.333333333333333333333333333},
                new double[]{-4.333376, -4.333376},
                new double[]{-4.3333376, -4.33333333333333333333333333},
                new double[]{3.00001, 3.00001},
                new double[]{3.000001, 3},
                new double[]{-3.00001, -3.00001},
                new double[]{-3.000001, -3},
                new double[]{5.99996, 5.99996},
                new double[]{5.999996, 6},
                new double[]{-5.99996, -5.99996},
                new double[]{-5.999996, -6},
                new double[]{12.34, 12.34},
                new double[]{-12.34, -12.34}};

            bool failure = false;
            foreach (double[] testVal in testVals) {
                double ratVal = r.Rationalize(testVal[0]);
                Console.WriteLine("{0} rationalizes to 5 places as {1}, which {2} the expected value of {3}.",
                    testVal[0],
                    ratVal,
                    ratVal==testVal[1]?"matches":"does not match",
                    testVal[1]);
                if (ratVal != testVal[1]) {
                    failure = true;
                }
            }
            if (failure) {
                Debug.Assert(false, "Rationalization test failed.");
            }
        }
    }
}
