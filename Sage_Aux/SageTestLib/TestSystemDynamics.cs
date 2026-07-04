using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SystemDynamics;
using Highpoint.Sage.SystemDynamics.Utility;
using NUnit.Framework;

// ReSharper disable ExpressionIsAlwaysNull
namespace Highpoint.Sage.SystemDynamics
{

    [TestFixture]
    public class BoilingPointTester
    {
       public BoilingPointTester() { Init(); }

        [SetUp]
        public void Init()
            {
                XElement parameters = null;
                string outputPath = null;
            }

        [Test]
        public void TestLynxHare() {

            string[] args = new string[] {};
            XElement parameters = null;
            string outputFileName = null;

            string eulerFile = RunProgram<LynxHare3>.Run(
                args,
                Integrator.Euler,
                parameters,
                outputFileName,
                "Timeslice,Lynx,Hares",
                (TextWriter tw, LynxHare3 s) =>
                    tw.WriteLine("{2:0.000}, {0:0.0000000}, {1:0.000000}", s.Lynx, s.Hares, s.TimeSliceNdx*s.TimeStep));

            string rk4File = RunProgram<LynxHare3>.Run(
                args,
                Integrator.RK4,
                parameters,
                outputFileName,
                "Timeslice,Lynx,Hares",
                (TextWriter tw, LynxHare3 s) =>
                    tw.WriteLine("{2:0.000}, {0:0.0000000}, {1:0.000000}", s.Lynx, s.Hares, s.TimeSliceNdx*s.TimeStep));

            try {
                Trajectory euler = Trajectory.Load(eulerFile);
                Trajectory rk4 = Trajectory.Load(rk4File);

                AssertTrajectoryIsSound(euler, "Euler");
                AssertTrajectoryIsSound(rk4, "RK4");

                // The two integrators solve the same model, so trajectories must differ only
                // numerically - but they must differ, or integrator selection isn't working.
                Assert.That(euler.Hares, Is.Not.EqualTo(rk4.Hares),
                    "Euler and RK4 produced identical trajectories; integrator selection is not taking effect.");
            } finally {
                File.Delete(eulerFile);
                File.Delete(rk4File);
            }
        }

        private const int EXPECTED_TIMESLICES = 480; // (Finish=60 - Start=0) / TimeStep=0.125

        private static void AssertTrajectoryIsSound(Trajectory t, string label) {

            Assert.That(t.Count, Is.EqualTo(EXPECTED_TIMESLICES), label + ": unexpected number of timeslices.");

            for (int i = 0 ; i < t.Count ; i++) {
                Assert.That(double.IsFinite(t.Hares[i]) && double.IsFinite(t.Lynx[i]),
                    label + ": non-finite population at t=" + t.Times[i] + " - the integrator blew up.");
                Assert.That(t.Hares[i] >= 0.0 && t.Lynx[i] >= 0.0,
                    label + ": negative population at t=" + t.Times[i] + ".");
            }

            List<int> harePeaks = Peaks(t.Hares);
            List<int> lynxPeaks = Peaks(t.Lynx);

            Console.WriteLine("{0}: hares [{1:0}..{2:0}], lynx [{3:0.0}..{4:0.0}], hare peaks at t=[{5}], lynx peaks at t=[{6}]",
                label, Min(t.Hares), Max(t.Hares), Min(t.Lynx), Max(t.Lynx),
                string.Join(", ", harePeaks.ConvertAll(i => t.Times[i].ToString("0.0"))),
                string.Join(", ", lynxPeaks.ConvertAll(i => t.Times[i].ToString("0.0"))));

            Assert.That(harePeaks.Count, Is.GreaterThanOrEqualTo(2),
                label + ": hare population does not oscillate.");
            Assert.That(lynxPeaks.Count, Is.GreaterThanOrEqualTo(2),
                label + ": lynx population does not oscillate.");

            // The predator-prey signature: each lynx (predator) peak trails a hare (prey) peak.
            Assert.That(lynxPeaks[0], Is.GreaterThan(harePeaks[0]),
                label + ": first lynx peak does not lag the first hare peak.");
        }

        // A local maximum that is the largest value within a +/- half-cycle window, rising above
        // the window's low by at least 20% of the series' full excursion. The model starts at its
        // equilibrium and the harvest pulse induces a small growing oscillation of period ~11 time
        // units, so the window must span a half-cycle (~5.5 units = 44 slices) to reach from a peak
        // to its neighboring trough, and prominence must be judged against the oscillation's own
        // amplitude, not the population level. The 20% floor keeps flat segments and numerical
        // ripple from counting as oscillation.
        private static List<int> Peaks(double[] series) {
            const int window = 44;
            double amplitude = Max(series) - Min(series);
            List<int> peaks = new List<int>();
            for (int i = window ; i < series.Length - window ; i++) {
                double lo = double.MaxValue, hi = double.MinValue;
                for (int j = i - window ; j <= i + window ; j++) {
                    if (j != i) hi = Math.Max(hi, series[j]);
                    lo = Math.Min(lo, series[j]);
                }
                if (series[i] > hi && series[i] - lo >= 0.2 * amplitude) peaks.Add(i);
            }
            return peaks;
        }

        private static double Min(double[] series) { double m = double.MaxValue; foreach (double d in series) m = Math.Min(m, d); return m; }
        private static double Max(double[] series) { double m = double.MinValue; foreach (double d in series) m = Math.Max(m, d); return m; }

        private class Trajectory {
            public double[] Times;
            public double[] Lynx;
            public double[] Hares;
            public int Count => Times.Length;

            // Parses the CSV written by TestLynxHare: a header line, then "time, lynx, hares" rows.
            public static Trajectory Load(string fileName) {
                string[] lines = File.ReadAllLines(fileName);
                int n = lines.Length - 1; // Skip the header row.
                Trajectory t = new Trajectory {
                    Times = new double[n],
                    Lynx = new double[n],
                    Hares = new double[n]
                };
                for (int i = 0 ; i < n ; i++) {
                    string[] fields = lines[i + 1].Split(',');
                    t.Times[i] = double.Parse(fields[0]);
                    t.Lynx[i] = double.Parse(fields[1]);
                    t.Hares[i] = double.Parse(fields[2]);
                }
                return t;
            }
        }

        public partial class LynxHare3
        {
            public override TimeSpan NominalPeriod { get; } = TimeSpan.FromDays(1.0);
            public override TimeSpan ActivePeriod { get; } = TimeSpan.FromDays(1.0);
            protected override void Initialize() { }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public partial class LynxHare3 : StateBase<LynxHare3>
        {

            // Constant Auxiliaries
            private readonly double hare_birth_fraction = 1.25;
            private readonly double area = 1E3;
            private readonly double lynx_birth_fraction = 0.25;
            private readonly double size_of_1_time_lynx_harvest = 1;
            private readonly static IDoubleInterpolator m_lynx_death_fraction_idi = new LinearDoubleInterpolator();
            private readonly static IDoubleInterpolator m_hares_killed_per_lynx_idi = new LinearDoubleInterpolator();

            // ID Numbers for flows.
            private readonly static int hare_births = 0;
            private readonly static int hare_deaths = 1;
            private readonly static int lynx_births = 2;
            private readonly static int lynx_deaths = 3;
            private readonly static int one_time_lynx_harvest = 4;

            static LynxHare3()
            {   // Class-global set up.
                m_lynx_death_fraction_idi.SetData(new double[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, new double[] { 0.94, 0.66, 0.4, 0.35, 0.3, 0.25, 0.2, 0.15, 0.1, 0.07, 0.05 });
                m_hares_killed_per_lynx_idi.SetData(new double[] { 0, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 }, new double[] { 0, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 });
            }

            // Stock Names
            public override string[] StockNames()
                =>
                    new[]
                    {
                    "Hares",
                    "Lynx"
                    };

            // Flow Names
            public override string[] FlowNames()
                =>
                    new[]
                    {
                    "hare_births",
                    "hare_deaths",
                    "lynx_births",
                    "lynx_deaths",
                    "one_time_lynx_harvest"
                    };

            public LynxHare3() : this(false) { }

            public LynxHare3(bool fromCopy = false)
            {
                Start = 0;
                Finish = 60;
                TimeStep = 0.125;

                m_hares = 5E4;
                m_lynx = 1250;

                StockSetters = new Action<StateBase<LynxHare3>, double>[]
                {
                (state, d) => ((LynxHare3)state).Hares = d,
                (state, d) => ((LynxHare3)state).Lynx = d,
                };

                StockGetters = new Func<StateBase<LynxHare3>, double>[]
                {
                state => ((LynxHare3)state).Hares,
                state => ((LynxHare3)state).Lynx,
                };

                Flows = new List<Func<StateBase<LynxHare3>, double>>
            {
                /* hare_births */ state => ((LynxHare3)state).Hares * ((LynxHare3)state).hare_birth_fraction,
                /* hare_deaths */ state => ((LynxHare3)state).Lynx * ((LynxHare3)state).hares_killed_per_lynx,
                /* lynx_births */ state => ((LynxHare3)state).Lynx * ((LynxHare3)state).lynx_birth_fraction,
                /* lynx_deaths */ state => ((LynxHare3)state).Lynx * ((LynxHare3)state).lynx_death_fraction,
                /* one_time_lynx_harvest */ state => Pulse ( ((LynxHare3)state).size_of_1_time_lynx_harvest , 4 , 1e3 )
            };

                StockInflows = new List<int[]> { new int[] { hare_births }, new int[] { lynx_births } };
                StockOutflows = new List<int[]> { new int[] { hare_deaths }, new int[] { lynx_deaths, one_time_lynx_harvest } };
            }

            public override StateBase<LynxHare3> Copy()
            {
                LynxHare3 retval = new LynxHare3(true);
                retval.m_hares = m_hares;
                retval.m_lynx = m_lynx;
                retval.TimeSliceNdx = TimeSliceNdx;
                return retval;
            }

            public override void Configure(XElement parameters = null)
            {

            }

            // Non-constant Auxiliaries
            public double hare_density => Hares / area;
            public double lynx_death_fraction => m_lynx_death_fraction_idi.GetYValue(hare_density);
            public double hares_killed_per_lynx => m_hares_killed_per_lynx_idi.GetYValue(hare_density);

            // These predicates are applied to all values set directly into stocks.
            private List<Predicate<double>> Tests = null; // new List<Predicate<double>>() {double.IsNaN, d => d < 0.0};

            public double Hares
            {
                get { return m_hares; }
                set
                {
                    Tests?.ForEach(n => { if (n(value)) Debugger.Break(); });
                    m_hares = value;
                }
            }
            public double Lynx
            {
                get { return m_lynx; }
                set
                {
                    Tests?.ForEach(n => { if (n(value)) Debugger.Break(); });
                    m_lynx = value;
                }
            }


            // Stocks
            private double m_hares;
            private double m_lynx;

            //////////////////////////////////////////////////////////////
            // MACRO IMPLEMENTATIONS
            //////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////

        }
    }
}