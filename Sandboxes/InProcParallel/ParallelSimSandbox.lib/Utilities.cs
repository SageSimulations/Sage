using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace ParallelSimSandbox.Lib
{

    /// <summary>
    /// Class ClockTracker follows a set of executives, and after a specified number of clock changes across all of them,
    /// prints data to the console on the values of all executives' clocks.
    /// </summary>
    public class ClockTracker
    {
        private readonly object m_lockObj = new object();
        private readonly int m_reportInterval;
        private readonly long[] m_latestDate;
        private int m_reportCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockTracker"/> class.
        /// </summary>
        /// <param name="execs">The array of executives to be tracked.</param>
        /// <param name="reportInterval">The number of clock changes across all tracked executives after which the next
        /// report is to be issued to the console.</param>
        /// <param name="txtOut">The TextWriter to which the report is to be issued. Defaults to Console.Out</param>
        public ClockTracker(IExecutive[] execs, int reportInterval, TextWriter txtOut = null)
        {
            if (txtOut == null) txtOut = Console.Out;
            m_reportCounter = 0;
            m_reportInterval = reportInterval;
            m_latestDate = new long[execs.Length];
            for (int i = 0; i < execs.Length; i++)
            {
                int index = i;
                execs[index].ClockAboutToChange += executive =>
                {
                    long[] tmp = { };
                    lock (m_lockObj)
                    {
                        m_latestDate[index] = executive.Now.Ticks;
                        if (++m_reportCounter % m_reportInterval == 0)
                        {
                            tmp = new long[execs.Length];
                            for (int i2 = 0; i2 < execs.Length; i2++) tmp[i2] = m_latestDate[i2];
                        }

                    }

                    if (tmp.Length > 0)
                    {
                        //long avg = 0;
                        //foreach (long longVar in tmp)
                        //{
                        //    avg += (longVar/execs.Length);
                        //}
                        //DateTime avgDateTime = new DateTime(avg);
                        //Console.WriteLine("Average dateTime is {0}.", avgDateTime);
                        StringBuilder sb = new StringBuilder();
                        lock (txtOut)
                        {
                            foreach (long longVal in tmp) sb.AppendFormat("{0}, ", new DateTime(longVal));
                            txtOut.WriteLine(sb.ToString());
                            txtOut.FlushAsync();
                        }
                    }
                };
            }
        }
    }

    public class TracedValue<T0> : IHasIdentity
    {
        // TODO: If an initial value is set with time t=0, and the simulation is begun, also at time t=0,
        // There could(?) be two entries at t=0 - the initial value, and the first set. The initial value
        // needs to be set into a separate variable, and the exec's reset event used to return currentValue
        // to that initial value.

        private struct Delta<T1>
        {
            public DateTime When;
            public T1 NewValue;

            public override string ToString()
            {
                return string.Format("Changed to {0} at {1}", NewValue, When);
            }
        }

        private static int LINEAR_VS_BINARY_SEARCH_BREAK_EVEN = 15;
        private readonly IExecutive m_localExec = null;
        private readonly IParallelExec m_localParallelExec = null;
        private readonly List<Delta<T0>> m_trace;
        private Delta<T0> m_currentValue;
        private static readonly UniqueNameGenerator m_ung = new UniqueNameGenerator();
        public static int NameGeneratorNumPlaces = 4;
        public static string m_nameSeed = string.Format("TV<{0}>", typeof (T0).Name);

        public TracedValue(IExecutive exec, T0 initialValue, string name = "", string description = "", Guid guid = default(Guid))
        {
            Name = string.IsNullOrEmpty(name)?m_ung.GetNextName(m_nameSeed, NameGeneratorNumPlaces):name;
            Description = description;
            Guid = guid;
            m_localExec = exec;
            m_localParallelExec = exec as IParallelExec;
            m_currentValue = new Delta<T0> { NewValue = initialValue, When = exec.Now };
            m_trace = new List<Delta<T0>> {m_currentValue};

            if (m_localParallelExec != null)
            {
                m_localParallelExec.Rolledback += ResetTo;
                Get = executive => m_getPT(executive as IParallelExec);
            }
            else
            {
                Get = otherExec => m_getSt(otherExec, m_localExec, this);
            } 
            
        }

        public int Length => m_trace.Count;

        private void ResetTo(DateTime toWhen)
        {
            if (m_currentValue.When < toWhen) return;

            int endIndex = m_trace.Count - 1;
            int length = 0;
            for ( ; length < endIndex; length++ )
            {
                if (m_trace[endIndex - length].When < toWhen) break;
            }
            m_trace.RemoveRange(endIndex - length + 1, length);
            m_currentValue = m_trace[endIndex - length];
        }

        public Func<IExecutive, T0> Get { get; }

        public string Description { get; }

        public Guid Guid { get; }

        public string Name { get; }

        private Func<IExecutive, IExecutive, TracedValue<T0>, T0> m_getSt = (fromExec, localExec, tracedValue) =>
        {
            System.Diagnostics.Debug.Assert(fromExec.Equals(localExec));
            return tracedValue.m_currentValue.NewValue;
        };

        private T0 m_getPT(IExecutive otherExec)
        {
            lock (this)
            {
                if (otherExec == m_localExec) return m_currentValue.NewValue;

                DateTime callersNow = otherExec.Now;
                DateTime myNow = m_localParallelExec.Now;
                T0 retval = default(T0);

                if (callersNow <= myNow)
                {
                    // Return the value, read from history.
                    retval = ReadHistoricalValue(callersNow);

                }
                else if (callersNow > myNow)
                {
                    IParallelExec ipoe = (IParallelExec)otherExec;
                    Monitor.Exit(this);
                    //Console.Out.WriteLine("{0} blocked in function call.", ((IParallelExec)otherExec).Name);

                    m_localParallelExec.WakeCallerAt(ipoe, callersNow, () =>
                    {
                        retval = ReadHistoricalValue(callersNow);
                    });
                    Monitor.Enter(this);
                }
                return retval;
            }
        }

        /// <summary>
        /// Sets the value from a thread on the local executive.
        /// </summary>
        /// <param name="value">The value.</param>
        private void LocalSet(T0 value)
        {
            // Writing to local value.
            // If the value to set is the same as the one already there, ignore the set.
            if (Equals(value, m_currentValue.NewValue)) return;
            // If the time is the same as the most current record, overwrite the value.
            if (m_localExec.Now == m_currentValue.When)
            {
                m_currentValue.NewValue = value;
            }
            else
            {
                // Otherwise, create a new tracedValue Delta and add it to the list.
                m_currentValue = new Delta<T0> {When = m_localExec.Now, NewValue = value};
                m_trace.Add(m_currentValue);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Set(T0 value, IExecutive otherExec)
        {
            if (otherExec == m_localExec)
            {
                LocalSet(value);
            }
            else
            {
                if (m_localParallelExec == null)
                {
                    throw new ArgumentException("TraceValue.Set that was declared with a non-parallel executive is being called from another executive. This is not supported.");
                }

                // Running on the thread of the REMOTE exec, so it won't advance.
                // TODO: IS THIS OKAY? GOOBER ((IParallelExec)m_localExec).HoldToCurrentTimeslice();
                DateTime remoteNow = otherExec.Now;
                DateTime localNow = m_localParallelExec.Now;

                // Someone in a remote domain is writing a value to a traced value in this domain.
                if (remoteNow < localNow)
                {
                    // Roll the local exec back to the time of the remote exec in whose time frame the
                    // write is to be done. After the rollback, m_localExec will be at the right time.
                    // We include ==, since as this method executes on remote exec's thread, local exec
                    // could be moving on.
                    ((IParallelExec)m_localParallelExec).InitiateRollback(remoteNow, () => LocalSet(value));
                }
                else if (remoteNow == localNow)
                {
                    LocalSet(value); // GOOBER : Problem is that we don't know whether a local set might
                                     // have been overridden by a remote set, or vice-versa. 
                                     // Ergo, results are non-deterministic.
                }
                else if (remoteNow > localNow)
                {
                    // The other exec is writing into the future of the local exec. Schedule an event at
                    // that future time to perform the write.
                    m_localParallelExec.RequestEvent((exec, data) => LocalSet(value), remoteNow);
                }
                // TODO: IS THIS OKAY? GOOBER ((IParallelExec)m_localExec).ReleaseFromCurrentTimeslice();
            }
        }

        private T0 ReadHistoricalValue(DateTime fromWhatTime)
        {
            T0 retval = default(T0);
            int cursor = m_trace.Count-1;

            // If the cursor is already in the right place, return that value:
            // Either at the end of the list, or at the last location prior to the time at which value is requested.
            if (m_trace[cursor].When < fromWhatTime)
            {
                //Console.Out.WriteLine("0a.) Looking for value at {0}, it {1}.", fromWhatTime, m_trace[cursor]);
                //Console.Out.Flush();
                return m_trace[cursor].NewValue; // The next one is at or after 'when.'
            }
            if (m_trace[cursor].When == fromWhatTime)
            {
                if (cursor == 0) cursor = 1;
                //Console.Out.WriteLine("0b.) Looking for value at {0}, it {1}.", fromWhatTime, m_trace[cursor-1]);
                //Console.Out.Flush();
                return m_trace[cursor-1].NewValue; // The next one is at or after 'when.'
            }

            // If it's a short list, use a simple linear search for the right place.
            int traceCount = m_trace.Count;
            if (traceCount < LINEAR_VS_BINARY_SEARCH_BREAK_EVEN)
            {
                for (cursor = traceCount-1; cursor > 0; cursor--)
                {
                    if (m_trace[cursor].When == fromWhatTime)
                    {
                        cursor--; break;
                    }
                    if (m_trace[cursor].When < fromWhatTime) break;
                }
                //Console.Out.WriteLine("1.) Looking for value at {0}, it {1}.", fromWhatTime, m_trace[cursor]);
                //Console.Out.Flush();
                return m_trace[cursor].NewValue;
            }

            //// Otherwise, use a binary search.
            int low = 0;
            int high = m_trace.Count - 1;
            if ((m_trace[low].When <= fromWhatTime) && (m_trace[low + 1].When > fromWhatTime))
            {
                //Console.Out.WriteLine("3.) Looking for value at {0}, it {1}.", fromWhatTime, m_trace[low]); Console.Out.Flush();
                return m_trace[low].NewValue;
            }
            if (m_trace[high].When < fromWhatTime)
            {
                //Console.Out.WriteLine("4.) Looking for value at {0}, it {1}.", fromWhatTime, m_trace[high]); Console.Out.Flush();
                return m_trace[high].NewValue;
            }
            bool found = false;
            while (high - low > 1)
            {
                int middle = (high + low)/2;
                if (m_trace[middle].When < fromWhatTime)
                {
                    if (m_trace[middle + 1].When >= fromWhatTime)
                    {
                        retval = m_trace[middle].NewValue;
                        //Console.Out.WriteLine("5.) Looking for value at {0}, it {1}.", fromWhatTime, m_trace[middle]);Console.Out.Flush();
                        found = true;
                        return retval;
                    }
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            if (!found)
                throw new ApplicationException(string.Format("Binary search of history for value at time {0} failed.",
                    fromWhatTime));

            return retval;
        }

        public T0 GetValue(int dr)
        {
            return m_trace[dr].NewValue;
        }

        public object GetDateTime(int dr)
        {
            return m_trace[dr].When;
        }
    }

    /// <summary>
    /// Struct Range contains a high and a low value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Range<T>
    {
        public Range(T min, T max)
        {
            Min = min;
            Max = max;
        }
        public T Min { get; set; }
        public T Max { get; set; }
    }

    public struct TimeSpanRange
    {
        public TimeSpanRange(TimeSpan min, TimeSpan max)
        {
            Min = min;
            Max = max;
        }
        [XmlElement(Type = typeof(Highpoint.Sage.Utility.XmlTimeSpan))]
        public TimeSpan Min { get; set; }
        [XmlElement(Type = typeof(Highpoint.Sage.Utility.XmlTimeSpan))]
        public TimeSpan Max { get; set; }

    }

}