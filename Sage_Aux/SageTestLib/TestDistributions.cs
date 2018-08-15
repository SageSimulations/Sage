/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;

//using Highpoint.Sage.Materials.Chemistry;

namespace Highpoint.Sage.Mathematics {

    [TestClass]
    public class Distributions101 {
        private static readonly bool m_visuallyVerify = false;
        public Distributions101() {
            m_model = new Model();
            ( (Model)m_model ).RandomServer = new Randoms.RandomServer(123456789, 0);
            Init();
        }

        [TestInitialize]
        public void Init() {
        }
        [TestCleanup]
        public void destroy() {
            Debug.WriteLine("Done.");
        }
        private IModel m_model = null;

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this distribution equals a normal distribution")]
        public void TestDistributionNormal() {
            IDoubleDistribution dist = new NormalDistribution(m_model, "NormalDistribution", Guid.NewGuid(), 5, 1);
            Assert.IsTrue(dist.GetValueWithCumulativeProbability(0.50) == 5.0);
            dist.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(dist.GetNext() == 5.0);
            dist.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionNormal.csv");

            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = dist.GetNext();

                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 0, 7.5, 100, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
            tw.Flush();
            tw.Close();


            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\DistributionNormal.csv");
            }
        }

        public void TestSingleDatapointAsTwoInLinearDoubleInterpolable() {
            LinearDoubleInterpolator ldi = new LinearDoubleInterpolator();
            ldi.SetData(new double[] { 1.0, 1.1 }, new double[] { 5.0, 5.0 });

            double five_point_zero = ldi.GetYValue(123.0);
            Assert.IsTrue(five_point_zero == 5.0);

        }

        public void TestDistributionEmpirical() {
            double[] binBounds = new double[] { 4.0, 7.0, 8.0, 10.0, 13.0, 14.0 };
            double[] heights = new double[] { 2.0, 4.0, 3.0, 6.0, 4.0 }; // Note - one less than in intervals.

            IDoubleDistribution dist = new EmpiricalDistribution(m_model, "EmpiricalDistributionFromHistogram", Guid.NewGuid(), binBounds, heights);
            Assert.IsTrue(dist.GetValueWithCumulativeProbability(0.50) == 10.5);
            dist.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(dist.GetNext() == 10.5);
            dist.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionEmpiricalFromHistogram.csv");
            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = dist.GetNext();
                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 4, 14, 100, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
            tw.Flush();
            tw.Close();

            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\DistributionEmpiricalFromHistogram.csv");
            }
        }

        class testCdf : ICDF {
            public double GetVariate(double linear) { return linear < 0.7 ? 5 : 7; }
        }

        /// <summary>
        /// Tests the universal distribution.
        /// </summary>
        public void TestUniversalDistribution() {
            double delta = 0.002;
            UniversalDistribution ud = new UniversalDistribution(m_model, "UniversalDistribution", Guid.NewGuid(), new testCdf());
            Assert.IsTrue(ud.GetValueWithCumulativeProbability(0.50) == 5.0);
            ud.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(ud.GetNext() == 5.0);
            ud.SetCDFInterval(0.0, 1.0);

            Debug.WriteLine("Generating raw data with delta = " + delta + ".");
            int DATASETSIZE = 1500000;
            int fives = 0;
            int sevens = 0;
            int others = 0;
            for (int i = 0 ; i < DATASETSIZE ; i++) {
                double next = ud.GetNext();
                if (next == 5) {
                    fives++;
                } else if (next == 7) {
                    sevens++;
                } else {
                    others++;
                }
            }
            double ratio = (double)( (double)fives / ( (double)fives + (double)sevens ) );
            Console.WriteLine("{0} fives, {1} sevens, and {2} others. Ratio of {3}.", fives, sevens, others, ratio);
            Assert.IsTrue(others == 0 && ratio > ( 0.7 - delta ) && ratio < ( 0.7 + delta ), "Failed custom CDF.");
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this distribution equals a triangular distribution")]
        public void TestDistributionTriangular() {
            IDoubleDistribution dist = new TriangularDistribution(m_model, "TriangularDistribution", Guid.NewGuid(), 2.0, 5.0, 9.0);
            Assert.IsTrue(dist.GetValueWithCumulativeProbability(0.50) == 5.2583426132260591);
            dist.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(dist.GetNext() == 5.2583426132260591);
            dist.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionTriangular.csv");
            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = dist.GetNext();
                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 1.0, 10.0, 450, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
            tw.Flush();
            tw.Close();

            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\DistributionTriangular.csv");
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this distribution equals a uniform distribution")]
        public void TestDistributionUniform() {
            IDoubleDistribution dist = new UniformDistribution(m_model, "UniformDistribution", Guid.NewGuid(), 3.5, 7.0);
            Assert.IsTrue(dist.GetValueWithCumulativeProbability(0.50) == 5.25);
            dist.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(dist.GetNext() == 5.25);
            dist.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionUniform.csv");
            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = dist.GetNext();
                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 0, 7.5, 100, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
            tw.Flush();
            tw.Close();

            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\DistributionUniform.csv");
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this distribution equals a exponential distribution")]
        public void TestDistributionExponential() {
            IDoubleDistribution dist = new ExponentialDistribution(m_model, "ExponentialDistribution", Guid.NewGuid(), 3.0, 3.0);
            Assert.IsTrue(dist.GetValueWithCumulativeProbability(0.50) == 5.0794415416798362, "Failure in TestDistributionExponential()");
            dist.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(dist.GetNext() == 5.0794415416798362);
            dist.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionExponential.csv");
            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = dist.GetNext();
                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 0, 30.0, 100, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }

            tw.WriteLine("Sum of off-scale-high : " + ( ( (double)hist.SumEntries(HistogramBinCategory.OffScaleHigh) ) ));
            tw.WriteLine("Average value : " + ( ( (double)hist.SumEntries(HistogramBinCategory.All) ) / ( (double)hist.RawData.Length ) ));
            tw.Flush();
            tw.Close();

            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\DistributionExponential.csv");
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this timespan distribution equals a exponential distribution")]
        public void TestDistributionTimeSpanExponential() {
            IDoubleDistribution dist = new ExponentialDistribution(m_model, "ExponentialDistribution", Guid.NewGuid(), 3.0, 3.0);
            ITimeSpanDistribution tsd = new TimeSpanDistribution(m_model, "TSD:" + dist.Name, Guid.NewGuid(), dist, TimeSpanDistribution.Units.Minutes);
            tsd.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(tsd.GetNext().Equals(TimeSpan.FromMinutes(5.0794415416798362)));
            tsd.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\TimeSpanDistributionExponential.csv");
            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = tsd.GetNext().TotalMinutes;
                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 0, 120.0, 100, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }

            tw.WriteLine("Sum of off-scale-high : " + ( ( (double)hist.SumEntries(HistogramBinCategory.OffScaleHigh) ) ));
            tw.WriteLine("Average value : " + ( ( (double)hist.SumEntries(HistogramBinCategory.All) ) / ( (double)hist.RawData.Length ) ));
            tw.Flush();
            tw.Close();

            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\TimeSpanDistributionExponential.csv");
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this distribution equals a weibull distribution")]
        public void TestDistributionWeibull() {
            IDoubleDistribution dist = new WeibullDistribution(m_model, "WeibullDistribution", Guid.NewGuid(), 2, 0, 2.0);
            Assert.IsTrue(dist.GetValueWithCumulativeProbability(0.50) == 1.6651092223153954);
            dist.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(dist.GetNext() == 1.6651092223153954);
            dist.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionWeibull.csv");
            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = dist.GetNext();
                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 0, 7.5, 100, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
            tw.Flush();
            tw.Close();

            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\DistributionWeibull.csv");
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this distribution equals a cauchy distribution")]
        public void TestDistributionCauchy() {
            IDoubleDistribution dist = new CauchyDistribution(m_model, "CauchyDistribution", Guid.NewGuid(), 3.0, 3.0);
            Assert.IsTrue(dist.GetValueWithCumulativeProbability(0.50) == 3.0);
            dist.SetCDFInterval(0.5, 0.5);
            Assert.IsTrue(dist.GetNext() == 3.0);
            dist.SetCDFInterval(0.0, 1.0);

            System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionCauchy.csv");
            Debug.WriteLine("Generating raw data.");
            int DATASETSIZE = 1500000;
            double[] rawData = new double[DATASETSIZE];
            for (int x = 0 ; x < DATASETSIZE ; x++) {
                rawData[x] = dist.GetNext();
                //tw.WriteLine(rawData[x]);
            }

            Debug.WriteLine("Performing histogram analysis.");
            Histogram1D_Double hist = new Histogram1D_Double(rawData, 0, 7.5, 100, "distribution");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProvider);
            hist.Recalculate();

            Debug.WriteLine("Writing data dump file.");
            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                tw.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
            tw.Flush();
            tw.Close();

            if (m_visuallyVerify) {
                System.Diagnostics.Process.Start("excel.exe", Environment.GetEnvironmentVariable("TEMP") + "\\DistributionCauchy.csv");
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that the result of this distribution equals a poisson distribution")]
        public void TestDistributionPoisson()
        {
            const double EPSILON = 0.000001;
            IDoubleDistribution dist = new PoissonDistribution(m_model, "PoissonDistribution", Guid.NewGuid(), 5.0);
            Assert.AreEqual(5.0,dist.GetValueWithCumulativeProbability(0.50),EPSILON);
            dist.SetCDFInterval(0.5, 0.5);
            Assert.AreEqual(5.0, dist.GetNext(), EPSILON);
            dist.SetCDFInterval(0.0, 1.0);

                System.IO.StreamWriter tw = new System.IO.StreamWriter(Environment.GetEnvironmentVariable("TEMP") + "\\DistributionPoisson.csv");
                Debug.WriteLine("Generating raw data.");
                const int DATASETSIZE = 1500000;
                double[] rawData = new double[DATASETSIZE];
                for (int x = 0 ; x < DATASETSIZE ; x++) {
                    rawData[x] = dist.GetNext();
                    //tw.WriteLine(rawData[x]);
                }

                Debug.WriteLine("Performing histogram analysis.");
                Histogram1D_Double hist = new Histogram1D_Double(rawData, 0, 25, 25, "distribution");
                hist.LabelProvider = new LabelProvider(((Histogram1D_Double) hist).DefaultLabelProvider);
                hist.Recalculate();

            List<double> expected = new List<double>()// From Excel.
            {
                10107, 50535, 126337, 210561, 263201, 263201, 219334, 156667, 97917, 54398, 27199, 12363, 5151, 1981,
                708, 236, 74, 22, 6, 2, 0, 0, 0, 0, 0
            };
            IEnumerable<double> ied = new List<int>((int[]) hist.Bins).Select(n=>(double)n);
            List<double> actual = new List<double>((IEnumerable<double>)ied);
            double rmsError = Mathematics.RMSErrorCalculator.Calculate(expected, actual);
            Assert.IsTrue(rmsError < 75, "Poisson distribution at lambda = 5 does not follow the expected curve.");

            if (m_visuallyVerify)
            {
                Debug.WriteLine("Writing data dump file.");
                int[] bins = (int[]) hist.Bins;
                for (int i = 0; i < bins.Length; i++)
                {
                    //Debug.WriteLine(hist.GetLabel(new int[]{i}) + ", " + bins[i]);
                    tw.WriteLine(hist.GetLabel(new int[] {i}) + ", " + bins[i] + ", " + expected[i]);
                }
                tw.Flush();
                tw.Close();

                System.Diagnostics.Process.Start("excel.exe",
                    Environment.GetEnvironmentVariable("TEMP") + "\\DistributionPoisson.csv");
            }
        }
    }

    [TestClass]
    public class Histograms101 {
        private IModel m_model = new Model();
        public Histograms101() { }
        [TestMethod]
        public void TestHistogramUniformDistDouble() {
            IDoubleDistribution dist = new UniformDistribution(m_model, "UniformDistribution", Guid.NewGuid(), 5, 35);
            _TestDoubleHistogram(dist, 1500, 7, 33, ( 33 - 7 ));
        }

        [TestMethod]
        public void TestHistogramExponentialDistDouble() {
            IDoubleDistribution dist = new ExponentialDistribution(m_model, "ExponentialDistribution", Guid.NewGuid(), 15, 15);
            _TestDoubleHistogram(dist, 1500, 5, 35, 10);
        }

        [TestMethod]
        public void TestHistogramUniformDistTimeSpan() {
            IDoubleDistribution dist = new UniformDistribution(m_model, "UniformDistribution", Guid.NewGuid(), (double)TimeSpan.FromMinutes(10).Ticks, (double)TimeSpan.FromMinutes(25).Ticks);
            _TestTimeSpanHistogram(dist, 1500, TimeSpan.FromMinutes(12).Ticks, TimeSpan.FromMinutes(24).Ticks, 10);

        }

        [TestMethod]
        public void TestUniformDistTimeSpanPerformance() {
            IDoubleDistribution dist = new NormalDistribution(m_model, "NormalDist", Guid.NewGuid(), (double)TimeSpan.FromMinutes(25).Ticks, (double)TimeSpan.FromMinutes(10).Ticks);

            for (int i = 0 ; i < 30000 * 600 ; i++) {
                TimeSpan.FromTicks((long)dist.GetNext());
            }
        }

        private void _TestDoubleHistogram(IDoubleDistribution dist, int nDataPoints, double low, double high, int nBins) {
            double[] rawData = new double[nDataPoints];
            for (int x = 0 ; x < nDataPoints ; x++) {
                rawData[x] = dist.GetNext();
            }

            IHistogram hist = new Histogram1D_Double(rawData, low, high, nBins, "Test Histogram");
            hist.LabelProvider = new LabelProvider(( (Histogram1D_Double)hist ).DefaultLabelProviderWithError);
            hist.Recalculate();

            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                Debug.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
        }

        private void _TestTimeSpanHistogram(IDoubleDistribution dist, int nDataPoints, long low, long high, int nBins) {
            TimeSpan[] rawData = new TimeSpan[nDataPoints];
            for (int x = 0 ; x < nDataPoints ; x++) {
                rawData[x] = TimeSpan.FromTicks((long)dist.GetNext());
            }

            IHistogram hist = new Histogram1D_TimeSpan(rawData, TimeSpan.FromTicks(low), TimeSpan.FromTicks(high), nBins, "Test Histogram");
            hist.Recalculate();

            int[] bins = (int[])hist.Bins;
            for (int i = 0 ; i < bins.Length ; i++) {
                Debug.WriteLine(hist.GetLabel(new int[] { i }) + ", " + bins[i]);
            }
        }
    }
}