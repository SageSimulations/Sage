using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Highpoint.Sage.Mathematics;

namespace Highpoint.Sage.SystemDynamics
{
    public abstract class StateBase<T> : StateBase
    {
        public static bool m_debug = false;
        public static bool DEFAULT_NEGATIVE_STOCK_MANAGEMENT_SETTING = true;

        public double Start = 0;
        public double Finish = 60;
        public double TimeStep = 0.125;
        public int TimeSliceNdx;
        public bool ManageStocksToStayPositive { get; set; } = DEFAULT_NEGATIVE_STOCK_MANAGEMENT_SETTING;

        public abstract string[] StockNames();
        public abstract string[] FlowNames();

        public List<Func<StateBase<T>, double>> Flows;

        public Func<StateBase<T>, double>[] StockGetters;

        public Action<StateBase<T>, double>[] StockSetters;

        public List<int[]> StockInflows;
        public List<int[]> StockOutflows;

        public abstract StateBase<T> Copy();

        public abstract void Configure(XElement parameters = null);

        // Library functions go here.

        #region Mathematical Functions (3.5.1)

        protected double INF => double.PositiveInfinity;
        protected double PI => Math.PI;

        #endregion

        #region Statistical Fuctions (3.5.2)

        private Dictionary<string, IDoubleDistribution> m_distros = new Dictionary<string, IDoubleDistribution>();

        private IDoubleDistribution getDistro(string key, Func<IDoubleDistribution> creator)
        {
            IDoubleDistribution retval;
            if (!m_distros.TryGetValue(key, out retval))
            {
                retval = creator();
                m_distros.Add(key, retval);
            }
            return retval;
        }

        protected double ExponentialDist(double mean, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new ExponentialDistribution(mean, 1)).GetNext();
        }

        protected double LogNormalDist(double mean, double stdev, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new LognormalDistribution(mean, stdev)).GetNext();
        }

        protected double NormalDist(double mean, double stdev, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new NormalDistribution(mean, stdev)).GetNext();
        }

        protected double PoissonDist(double mean, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new PoissonDistribution(mean)).GetNext();
        }

        protected double UniformDist(double min, double max, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new UniformDistribution(min, max)).GetNext();
        }

        #endregion

        // Delay Functions (3.5.3) in base class.

        #region Test Input Functions (3.5.4)

        protected double Pulse(double magnitude, double firstTime, double interval)
        {
            double tolerance = TimeStep/1000;
            double timeTilPulse = firstTime - ((TimeSliceNdx * TimeStep) % interval);
            if (Math.Abs(timeTilPulse) < tolerance)
            {
                return magnitude/TimeStep;
            }
            return 0.0;
        }

        protected double Ramp(double slope, double startTime)
        {
            double nIntervals = (TimeSliceNdx*TimeStep) - startTime;
            if (nIntervals > 0) return nIntervals*slope/TimeStep;
            return 0.0;
        }

        protected double Step(double height, double startTime)
        {
            double nIntervals = (TimeSliceNdx*TimeStep) - startTime;
            if (nIntervals > 0) return height/ TimeStep; // It's going to get multiplied in the aggregation stage.
            return 0.0;
        }

        #endregion

        #region Miscellaneous Functions (3.5.6) [NOT COMPLETE]

        protected double InitialValue(int variableIndex)
        {
            throw new NotImplementedException();
        }

        protected double PreviousValue(int variableIndex)
        {
            throw new NotImplementedException();
        }

        // TODO: Figure out what to do about "SELF"... 

        #endregion

        #region Time Functions (3.5.5)

        protected double DeltaT => TimeStep;
        protected double StartTime => Start;
        protected double StopTime => Finish;
        protected double CurrentTime => TimeSliceNdx*TimeStep; // TODO: Compute this once each timestep.

        #endregion

        protected static double Constrain(double min, double max, double val)
        {
            return Math.Max(min, Math.Min(max, val));
        }

        protected virtual void ProcessChildModelsAsEuler(StateBase<T> state) {}

        public StateBase<T> RunOneTimesliceAsEuler(StateBase<T> state)
        {
            StateBase<T> newState = Copy();
            newState.TimeSliceNdx++;

            double[] deltas = GetDeltaForEachStock(state);

            int nStocks = state.StockNames().Length;
            for (int i = 0; i < nStocks; i++)
            {
                double @is = state.StockGetters[i](state);
                double delta = deltas[i];

                if (double.IsNaN(@is))
                {
                    Console.WriteLine($"Stock {state.StockNames()[i]} is {@is}.");
                }
                if (@is + delta < 0)
                {
                    Console.WriteLine($"Stock {state.StockNames()[i]} is {@is}, and is about to change by {delta}.");
                }
            }

            ProcessChildModelsAsEuler(newState);

            ApplyDeltasToState(newState, deltas);

            return newState;
        }

        public StateBase<T> RunOneTimeSliceAsRK4(StateBase<T> state)
        {
            // Get slopes at ti
            StateBase<T> newState = state.Copy();
            newState.TimeStep /= 2;
            newState.TimeSliceNdx *= 2;
            double[] k1 = GetDeltaForEachStock(newState); // delta from t0
            //Console.Write("Computing {2:0.0} : dY/dt[{0:0.0}] = {1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k1[1] / newState.TimeStep, state.TimeSliceNdx * state.TimeStep);
            Console.Write("{2:0.0},{0:0.0},{1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k1[1] / newState.TimeStep, state.TimeSliceNdx * state.TimeStep);

            // Get slopes at Ti+.5

            newState.TimeSliceNdx++;
            ApplyDeltasToState(newState, k1);
            double[] k2 = GetDeltaForEachStock(newState); // first delta from t0+1/2
            //Console.Write("dY/dt[{0:0.0}] = {1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k2[1] / newState.TimeStep);
            Console.Write("{0:0.0},{1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k2[1] / newState.TimeStep);

            // Apply slopes at Ti+.5 to state at Ti, 
            StateBase<T> newState2 = state.Copy();
            newState2.TimeStep /= 2;
            newState2.TimeSliceNdx *= 2;
            newState2.TimeSliceNdx++;
            ApplyDeltasToState(newState2, k2);
            double[] k3 = GetDeltaForEachStock(newState2); // second delta from t0+1/2
            //Console.Write("dY/dt[{0:0.0}] = {1:0.00000}, ", newState2.TimeSliceNdx * newState2.TimeStep, k3[1] / newState2.TimeStep);
            Console.Write("{0:0.0},{1:0.00000}, ", newState2.TimeSliceNdx * newState2.TimeStep, k3[1] / newState2.TimeStep);

            StateBase<T> newState3 = state.Copy();
            newState3.TimeSliceNdx++;
            newState3.TimeStep /= 2;
            newState3.TimeSliceNdx *= 2;
            ApplyDeltasToState(newState3, k3); // takes initial to halfway using second slope.
            ApplyDeltasToState(newState3, k3); // takes halfway to all the way using second slope.

            double[] k4 = GetDeltaForEachStock(newState3); // delta from t1
            //Console.WriteLine("dY/dt[{0:0.0}] = {1:0.00000}", newState3.TimeSliceNdx * newState3.TimeStep, k4[1] / newState3.TimeStep);
            Console.WriteLine("{0:0.0},{1:0.00000}", newState3.TimeSliceNdx * newState3.TimeStep, k4[1] / newState3.TimeStep);

            double[] finalK = new double[k4.Length];
            for (int i = 0; i < finalK.Length; i++)
            {
                finalK[i] = 2.0 * (k1[i] + k2[i] + k2[i] + k3[i] + k3[i] + k4[i]) / 6.0;
            }

            StateBase<T> finalState = state.Copy();
            finalState.TimeSliceNdx++;
            ApplyDeltasToState(finalState, finalK);

            return finalState;
        }

        private static void ApplyDeltasToState(StateBase<T> state, double[] deltas)
        {
            for (int i = 0; i < state.StockGetters.Length; i++)
            {
                double stockLevel = state.StockGetters[i](state);
                state.StockSetters[i](state, stockLevel + deltas[i]);
            }
        }

        private static double[] GetDeltaForEachStock(StateBase<T> state)
        {
            double[] retval = new double[state.StockGetters.Length];

            // For each stock.
            if (m_debug) Console.WriteLine($"TIMESLICE {state.TimeSliceNdx} = {state.CurrentTime}");
            for (int i = 0; i < state.StockGetters.Length; i++)
            {
                if (m_debug) Console.WriteLine("\t{0}\r\n\t\t  {1:F2}", state.StockNames()[i], state.StockGetters[i](state));
                double increase = 0;
                double decrease = 0;
                // accumulate inflows
                for (int j = 0; j < state.StockInflows[i].Length; j++)
                {
                    int whichFlow = state.StockInflows[i][j];
                    if (m_debug) Console.WriteLine("\t\t+ {0:F2} ({1}).", state.Flows[whichFlow](state), state.FlowNames()[whichFlow]);
                    increase += state.Flows[whichFlow](state);
                }
                increase *= state.TimeStep;


                // accumulate outflows
                for (int j = 0; j < state.StockOutflows[i].Length; j++)
                {
                    int whichFlow = state.StockOutflows[i][j];
                    if (m_debug) Console.WriteLine("\t\t- {0:F2} ({1}).", state.Flows[whichFlow](state), state.FlowNames()[whichFlow]);
                    decrease += state.Flows[whichFlow](state);
                }
                decrease *= state.TimeStep;

                double current = state.StockGetters[i](state);
                if (state.ManageStocksToStayPositive)
                {
                    if (current + increase - decrease < 0.0)
                    {
                        decrease = current + increase;
                    }
                }
                retval[i] = increase - decrease;
                if (current + retval[i] < 0)
                {
                    Console.WriteLine($"Stock {state.StockNames()[i]} goes negative to {current + retval[i]} in step {i}.");
                }
            }

            return retval;
        }

    }

    public abstract class StateBase
    {
        public abstract TimeSpan NominalPeriod { get; }
        public abstract TimeSpan ActivePeriod { get; }
        protected abstract void Initialize();

        protected double PeriodAdjust(double val)
        {
            return val * NominalPeriod.TotalSeconds / ActivePeriod.TotalSeconds;
        }

        protected double FractionAdjust(double val)
        {
            return Math.Max(0.0, Math.Min(1.0, val * NominalPeriod.TotalSeconds / ActivePeriod.TotalSeconds));
        }

        #region Delay Functions (3.5.3)

        public interface IFunction
        {
            double Process(double stimulus);
        }

        public class Delay : IFunction
        {
            private readonly Queue<double> queue = new Queue<double>();
            private string m_myString;
            private bool m_hasInitialValue;
            private int m_nBins;

            public Delay(double dt, double delay, double initVal = Double.NegativeInfinity)
            {
                m_myString = String.Format("Delay({0},{1},{2});", delay, dt, initVal);
                m_hasInitialValue = !Double.IsNegativeInfinity(initVal);
                m_nBins = (int)(delay / dt);
                if (!m_hasInitialValue)
                {
                    for (int i = 0; i < m_nBins; i++) queue.Enqueue(initVal);
                }
            }

            public double Process(double stimulus)
            {
                if (!m_hasInitialValue)
                {
                    for (int i = 0; i < m_nBins - 1; i++) queue.Enqueue(stimulus);
                }
                queue.Enqueue(stimulus);
                return queue.Dequeue();
            }

            public override string ToString()
            {
                return m_myString;
            }
        }

        public class Delay1 : IFunction
        {
            private double m_hold;
            private readonly double m_delay;
            private readonly double m_dt;
            private bool m_hasInitialValue;
            private string m_myString;

            public Delay1(double dt, double delay, double initVal = Double.NegativeInfinity)
            {
                m_myString = String.Format("Delay1({0},{1},{2});", delay, dt, initVal);
                m_delay = delay;
                m_dt = dt;
                m_hasInitialValue = !Double.IsNegativeInfinity(initVal);
                m_hold = m_hasInitialValue ? initVal * delay : 0;
            }

            public double Process(double stimulus)
            {
                if (!m_hasInitialValue)
                {
                    m_hold = stimulus * m_delay;
                    m_hasInitialValue = true;
                }

                double retval = m_hold / m_delay;
                m_hold -= (retval * m_dt);
                m_hold += (stimulus * m_dt);
                return retval;
            }

            public override string ToString()
            {
                return m_myString;
            }
        }

        public class Delay3 : IFunction
        {
            private readonly double m_delay;
            private double m_hold1;
            private double m_hold2;
            private double m_hold3;
            private double m_dt;
            private bool m_hasInitialValue;
            private string m_myString;

            public Delay3(double dt, double delay, double initVal = Double.NegativeInfinity)
            {
                m_myString = String.Format("Delay3({0},{1},{2});", delay, dt, initVal);
                m_dt = dt;
                m_delay = delay;
                m_hasInitialValue = !Double.IsNegativeInfinity(initVal);
                m_hold1 = m_hold2 = m_hold3 = m_hasInitialValue ? (initVal * (m_delay / 3)) : 0;
            }

            public double Process(double stimulus)
            {
                if (!m_hasInitialValue)
                {
                    m_hold1 = m_hold2 = m_hold3 = stimulus * (m_delay / 3);
                    m_hasInitialValue = true;
                }
                double from3 = (m_hold3 / (m_delay / 3)) * m_dt;
                m_hold3 -= from3;

                double from2 = (m_hold2 / (m_delay / 3)) * m_dt;
                m_hold2 -= from2;
                m_hold3 += from2;

                double from1 = (m_hold1 / (m_delay / 3)) * m_dt;
                m_hold1 += stimulus * m_dt;
                m_hold1 -= from1;
                m_hold2 += from1;

                return from3 / m_dt;
            }

            public override string ToString()
            {
                return m_myString;
            }
        }

        public class DelayN : IFunction
        {
            private readonly double m_delay;
            private readonly double m_dt;
            private readonly int m_nStages;
            private double[] m_hold;
            private bool m_hasInitialValue;
            private string m_myString;

            public DelayN(double dt, double delay, int nStages, double initVal = Double.NegativeInfinity)
            {
                m_myString = String.Format("DelayN({0},{1},{2},{3});", delay, dt, nStages, initVal);
                m_dt = dt;
                m_delay = delay;
                m_nStages = nStages;
                m_hasInitialValue = !Double.IsNegativeInfinity(initVal);
                if (m_hasInitialValue)
                {
                    m_hold = Enumerable.Repeat(initVal * (m_delay / m_nStages), m_nStages).ToArray();
                }
            }

            public double Process(double stimulus)
            {
                if (!m_hasInitialValue)
                {
                    m_hold = Enumerable.Repeat(stimulus * (m_delay / m_nStages), m_nStages).ToArray();
                    m_hasInitialValue = true;
                }

                double[] xfer = new double[m_nStages + 1];
                xfer[0] = stimulus * m_dt;
                for (int i = 1; i <= m_nStages; i++)
                {
                    xfer[i] = (m_hold[i - 1] / (m_delay / m_nStages)) * m_dt;
                }

                for (int i = 0; i <= m_nStages; i++)
                {
                    if (i > 0) m_hold[i - 1] -= xfer[i];
                    if (i < m_nStages) m_hold[i] += xfer[i];
                }

                //Console.WriteLine("{0:F2}->[{1:F2}]-{2:F2}->[{3:F2}]-{4:F2}->[{5:F2}]-{6:F2}->",
                //    xfer[0], m_hold[0], xfer[1], m_hold[1], xfer[2], m_hold[2], xfer[3]);

                return xfer[m_nStages] / m_dt;

            }

            public override string ToString()
            {
                return m_myString;
            }

        }

        public class Smooth1 : IFunction
        {
            private double m_hold;
            private readonly double m_averagingTime;
            private bool m_hasInitialValue;
            private string m_myString;

            public Smooth1(double averagingTime, double initVal = Double.NegativeInfinity)
            {
                m_myString = String.Format("Smooth1({0},{1});", averagingTime, initVal);
                m_averagingTime = averagingTime;
                m_hasInitialValue = !Double.IsNegativeInfinity(initVal);
                m_hold = m_hasInitialValue ? initVal * averagingTime : 0;
            }

            public double Process(double stimulus)
            {
                if (!m_hasInitialValue)
                {
                    m_hold = stimulus * m_averagingTime;
                    m_hasInitialValue = true;
                }

                double retval = m_hold / m_averagingTime;
                m_hold -= retval;
                m_hold += stimulus;
                return retval;
            }

            public override string ToString()
            {
                return m_myString;
            }
        }

        public class Smooth3 : IFunction
        {
            private readonly double m_averagingTime;
            private double m_hold1;
            private double m_hold2;
            private double m_hold3;
            private bool m_hasInitialValue;
            private string m_myString;

            public Smooth3(double averagingTime, double initVal = Double.NegativeInfinity)
            {
                m_myString = String.Format("Smooth3({0},{1});", averagingTime, initVal);
                m_averagingTime = averagingTime;
                m_hasInitialValue = !Double.IsNegativeInfinity(initVal);
                m_hold1 = m_hold2 = m_hold3 = m_hasInitialValue ? (initVal * (m_averagingTime / 3)) : 0;
            }

            public double Process(double stimulus)
            {
                if (!m_hasInitialValue)
                {
                    m_hold1 = m_hold2 = m_hold3 = stimulus * (m_averagingTime / 3);
                    m_hasInitialValue = true;
                }
                double from3 = (m_hold3 / (m_averagingTime / 3));
                m_hold3 -= from3;

                double from2 = (m_hold2 / (m_averagingTime / 3));
                m_hold2 -= from2;
                m_hold3 += from2;

                double from1 = (m_hold1 / (m_averagingTime / 3));
                m_hold1 += stimulus;
                m_hold1 -= from1;
                m_hold2 += from1;

                return from3;
            }

            public override string ToString()
            {
                return m_myString;
            }
        }

        public class SmoothN : IFunction
        {
            private readonly double m_averagingTime;
            private readonly int m_nStages;
            private double[] m_hold;
            private bool m_hasInitialValue;
            private string m_myString;

            public SmoothN(double averagingTime, int nStages, double initVal = Double.NegativeInfinity)
            {
                m_myString = String.Format("SmoothN({0},{1},{2});", averagingTime, nStages, initVal);
                m_averagingTime = averagingTime;
                m_nStages = nStages;
                m_hasInitialValue = !Double.IsNegativeInfinity(initVal);
                if (m_hasInitialValue)
                {
                    m_hold = Enumerable.Repeat(initVal * (m_averagingTime / m_nStages), m_nStages).ToArray();
                }
            }

            public double Process(double stimulus)
            {
                if (!m_hasInitialValue)
                {
                    m_hold = Enumerable.Repeat(stimulus * (m_averagingTime / m_nStages), m_nStages).ToArray();
                    m_hasInitialValue = true;
                }

                double[] xfer = new double[m_nStages + 1];
                xfer[0] = stimulus;
                for (int i = 1; i <= m_nStages; i++)
                {
                    xfer[i] = (m_hold[i - 1] / (m_averagingTime / m_nStages));
                }

                for (int i = 0; i <= m_nStages; i++)
                {
                    if (i > 0) m_hold[i - 1] -= xfer[i];
                    if (i < m_nStages) m_hold[i] += xfer[i];
                }

                //Console.WriteLine("{0:F2}->[{1:F2}]-{2:F2}->[{3:F2}]-{4:F2}->[{5:F2}]-{6:F2}->",
                //    xfer[0], m_hold[0], xfer[1], m_hold[1], xfer[2], m_hold[2], xfer[3]);

                return xfer[m_nStages];

            }

            public override string ToString()
            {
                return m_myString;
            }

        }

        public class Trend : IFunction
        {
            private double m_averageInput;
            private readonly double m_averagingTime;
            private readonly double m_dt;
            private string m_myString;

            public Trend(double dt, double averagingTime, double initVal = 0)
            {
                m_myString = String.Format("Trend({0}, {1}, {2});", averagingTime, dt, initVal);
                m_averagingTime = averagingTime;
                m_dt = dt;
                m_averageInput = initVal;
            }

            public double Process(double input)
            {
                double changeInAverage = (input - m_averageInput) / m_averagingTime;
                double trend = (input - m_averageInput) / (m_averageInput * m_averagingTime * m_dt);
                //Console.WriteLine("{0:F2}->[{1:F2}] = {2:F2}, Trend = {3}", input, m_averageInput, changeInAverage, trend);
                m_averageInput += m_dt * changeInAverage;

                return trend;
            }

            public double AverageInput => m_averageInput;

            public override string ToString()
            {
                return m_myString;
            }
        }

        public class Forecast : IFunction
        {
            private Trend m_trend;
            private readonly double m_horizon;
            private string m_myString;

            public Forecast(double dt, double averagingTime, double horizon, double initVal = 0)
            {
                m_myString = String.Format("Forecast({0}, {1}, {2}, {3});", averagingTime, dt, horizon, initVal);
                m_trend = new Trend(averagingTime, dt, initVal);
                m_horizon = horizon;
            }

            public double Process(double input)
            {
                double trend = m_trend.Process(input);
                return m_trend.AverageInput + (trend * m_horizon);
            }

            public override string ToString()
            {
                return m_myString;
            }
        }

        #endregion
    }

    public struct ModelToModelFlow<TModelTypeFrom, TModelTypeTo> 
        where TModelTypeFrom : StateBase<TModelTypeFrom>
        where TModelTypeTo : StateBase<TModelTypeTo>
    {
        public ModelToModelFlow(TModelTypeFrom flowFrom, TModelTypeTo flowTo, Func<double> flow)
        {
            
        }
    }
}
