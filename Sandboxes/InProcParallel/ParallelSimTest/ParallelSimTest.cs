using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.SimCore.Parallel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelSimSandbox;
using ParallelSimSandbox.Lib;

namespace ParallelSimTest
{
    /// <summary>
    /// Class RollbackTester.
    /// </summary>
    [TestClass]
    public class RollbackTester
    {
        private readonly DateTime m_testStart = new DateTime(2018, 1, 1);

        [TestMethod]
        public void TestRollbackInExec()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            IRandomChannel irc = GlobalRandomServer.Instance.GetRandomChannel(246810, 0);
            foreach (int i in Enumerable.Range(0, 8))
            {
                exec.RequestEvent(
                    (executive, data) => Console.Out.WriteLine("{0} : Callback from {1}.", executive.Now, data),
                    m_testStart + TimeSpan.FromHours(irc.NextDouble(0.0, 8.0)),
                    irc.NextDouble(0.0, 1.0),
                    "Bob " + (char) ('A' + i));
            }

            foreach (int i in Enumerable.Range(0, 3))
            {
                exec.RequestEvent(
                    (executive, data) =>
                        Console.Out.WriteLine("{0} : Daemon Callback from {1}.", executive.Now, data),
                    m_testStart + TimeSpan.FromHours(irc.NextDouble(0.0, 8.0)),
                    irc.NextDouble(0.0, 1.0),
                    "Dan " + (char) ('A' + i));
            }

            foreach (int i in Enumerable.Range(8, 16))
            {
                exec.RequestEvent((executive, data) =>
                {
                    executive.RequestEvent(
                        (executive2, data2) =>
                            Console.Out.WriteLine("{0} : Future Callback from {1}.", executive2.Now, data2),
                        executive.Now + TimeSpan.FromHours(irc.NextDouble(0.0, 8.0)),
                        irc.NextDouble(0.0, 1.0),
                        "Fez " + (char) ('A' + i));
                }, m_testStart + TimeSpan.FromHours(irc.NextDouble(0.0, 8.0)));
            }

            foreach (int i in Enumerable.Range(8, 16))
            {
                exec.RequestEvent((executive, data) =>
                {
                    executive.RequestEvent((executive2, data2) =>
                    {
                        executive.RequestEvent(
                            (executive3, data3) =>
                                Console.Out.WriteLine("{0} : Far Future Callback from {1}.", executive3.Now, data3),
                            executive.Now + TimeSpan.FromHours(irc.NextDouble(0.0, 8.0)),
                            irc.NextDouble(0.0, 1.0),
                            "Ffg " + (char) ('A' + i));
                    }, executive.Now + TimeSpan.FromHours(irc.NextDouble(0.0, 8.0)));
                }, m_testStart + TimeSpan.FromHours(irc.NextDouble(0.0, 8.0)));
            }

            // Events will run from 1/1/2018 12:03:18 AM to 1/1/2018 6:22:38 PM.
            // Want to initiate a rollback at 1/1/2018 10:30 AM to 1/1/2018 5:30 AM.
            int nRollbacks = (int) 5;
            exec.RequestEvent(
                (executive, data) =>
                {
                    if (--nRollbacks > 0)
                    {
                        Console.WriteLine("{0} : Initiating rollback to {1}.", executive.Now, data);
                        ((IParallelExec) executive).InitiateRollback((DateTime) data);
                    }
                },
                new DateTime(2018, 1, 1, 10, 30, 00), 0.0, new DateTime(2018, 1, 1, 05, 30, 00));

            exec.Start();

            Console.Out.WriteLine("Callbacks are all done.");

        }

        [TestMethod]
        public void TestRollback()
        {
            DateTime startAt = new DateTime(2018, 1, 1);
            DateTime finishAt = new DateTime(2018, 6, 1);
            // Create the new executive.
            IExecutive exec = ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);

            // Set start and finish times.
            exec.SetStartTime(startAt);
            exec.RequestEvent((executive, data) => executive.Stop(), finishAt);

            // Create the initializing self-propagating event.
            exec.ExecutiveStarted +=
                (executive => exec.RequestEvent(ReportAndReschedule, exec.Now + TimeSpan.FromDays(7)));

            exec.Start();

        }

        private static readonly DateTime[] s_rollbackTriggers = new DateTime[]
        {new DateTime(2018, 3, 1), new DateTime(2018, 4, 1)};

        private static int _rollBackNdx;

        private void ReportAndReschedule(IExecutive exec, object obj)
        {
            Console.WriteLine("Event served at " + exec.Now);
            exec.RequestEvent(ReportAndReschedule, exec.Now + TimeSpan.FromDays(7));
            if ((_rollBackNdx < s_rollbackTriggers.Length) && (exec.Now > s_rollbackTriggers[_rollBackNdx]))
            {
                _rollBackNdx++;
                DateTime rollbackToWhen = exec.Now - TimeSpan.FromDays(40);
                Console.WriteLine("It's {0}, and I'm rolling back to {1}.", exec.Now, rollbackToWhen);
                ((IParallelExec) exec).InitiateRollback(rollbackToWhen);
            }
        }
    }

    [TestClass]
    public class CoprocessingTester
    {
        private readonly DateTime m_testStart = new DateTime(2018, 1, 1);

        private int PERIODICITY = 100;
        //[TestMethod]
        public void TestCoprocessorInterleaving()
        {
            foreach (int i in Enumerable.Range(1, 1000)) TestCoprocessorInterleaving(i);
        }

        private void TestCoprocessorInterleaving(int iteration)
        {
            DateTime m_testStart = new DateTime(2018, 1, 1);
            IParallelExec exec0, exec1;
            
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine(
                "{0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} {0} ",
                iteration);
            Console.WriteLine("--------------------------------------------------------------------------");

            // First, we perform activities from two separate executives. One is writing to
            // a TracedValue owned by that executive, at a specified world-speed and sim-speed. 
            // The other is reading from that value at the same world-speed, but a faster sim-speed,
            // such that it is often waiting for the value at its current time to be written.
            // Both executives finish at roughly the same time.
            exec0 = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            _tracedValue = new TracedValue<int>(exec0, 1000);
            exec0.RequestEvent(RepeatedWrite1, m_testStart);

            exec1 = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            exec1.RequestEvent(RepeatedRead1, m_testStart);

            CoExecutor.CoStart(new[] {exec0, exec1}, terminateAt: new DateTime(2019, 1, 1));

            // Second, we perform activities from two separate executives. One is writing to
            // a TracedValue owned by that executive, at a specified world-speed and sim-speed. 
            // The other is reading from that value at the same world-speed, but a slower sim-speed,
            // such that it is always reading from history.
            // Exec1 finishes its write events long before Exec2 finishes its read events. This
            // also tests the CoExecution mechanism whereby an executive is held running when others
            // have not yet completed.
            exec0 = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            exec0.Name = "Exec0";
            _tracedValue = new TracedValue<int>(exec0, 1000);
            exec0.RequestEvent(RepeatedWrite2, m_testStart);

            exec1 = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            exec1.Name = "Exec1";
            exec1.RequestEvent(RepeatedRead2, m_testStart);

            CoExecutor.CoStart(new[] {exec0, exec1}, terminateAt: new DateTime(2019, 1, 1));
        }

        private static int _nReads = 0;
        private static int _nWrites = 0;

        private static TracedValue<int> _tracedValue;
        private Random random = new Random(12345);

        private void RepeatedWrite1(IExecutive exec, object userData)
        {
            Thread.Sleep(random.Next(0, 5));
            int n = _tracedValue.Get(exec);
            _tracedValue.Set(n - 1, exec);
            if (0 == (++_nWrites%PERIODICITY)) Console.WriteLine("{0} : traced value set was : {1}.", exec.Now, n);
            if (n > 0) exec.RequestEvent(RepeatedWrite1, exec.Now + TimeSpan.FromMinutes(1),2.0);
        }

        private void RepeatedRead1(IExecutive exec, object userData)
        {
            Thread.Sleep(random.Next(0,5));
            int n = _tracedValue.Get(exec);
            if (0 == (++_nReads% PERIODICITY)) Console.WriteLine("{0} : traced value read was : {1}.", exec.Now, n);
            if (Math.Abs(n - (999 - ((exec.Now - m_testStart).TotalMinutes-1))) > .0001)
            {
                Console.WriteLine("Erroneous read of {0} for time {1}.", n, exec.Now);
            }
            if (n > 0) exec.RequestEvent(RepeatedRead1, exec.Now + TimeSpan.FromMinutes(4), 1.0);
        }

        private void RepeatedWrite2(IExecutive exec, object userData)
        {
            Thread.Sleep(random.Next(0, 5));
            int n = _tracedValue.Get(exec);
            _tracedValue.Set(n - 1, exec);
            if (0 == (++_nWrites % PERIODICITY)) Console.WriteLine("{0} : traced value set was : {1}.", exec.Now, n);
            if (n > 0) exec.RequestEvent(RepeatedWrite2, exec.Now + TimeSpan.FromMinutes(4));
        }

        private void RepeatedRead2(IExecutive exec, object userData)
        {
            Thread.Sleep(random.Next(0, 5));
            int n = _tracedValue.Get(exec);
            if (0 == (++_nReads % PERIODICITY)) Console.WriteLine("{0} : traced value read was : {1}.", exec.Now, n);
            if (!(exec.Now == m_testStart && n == 1000)
                && (n != 999 - (int)(((exec.Now - m_testStart).TotalMinutes - 1) / 4)))
            {
                Console.WriteLine("Erroneous read of {0} for time {1}.", n, exec.Now);
            }
            if (n > 0) exec.RequestEvent(RepeatedRead2, exec.Now + TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public void TestCoTermination()
        {
            IParallelExec exec1 = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            IParallelExec exec2 = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            IParallelExec exec3 = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);

            StringBuilder sb = new StringBuilder();
            exec1.RequestEvent((exec, data) =>
            {
                Thread.Sleep(1000);
                lock (sb) sb.Append("A");
            }, new DateTime(2017, 12, 21));
            exec2.RequestEvent((exec, data) =>
            {
                Thread.Sleep(500);
                lock (sb) sb.Append("B");
            }, new DateTime(2017, 12, 19));
            exec3.RequestEvent((exec, data) =>
            {
                Thread.Sleep(100);
                lock (sb) sb.Append("C");
            }, new DateTime(2017, 12, 18));
            CoExecutor.CoStart(new[] {exec1, exec2, exec3}, terminateAt: m_testStart);

            Console.WriteLine(sb.ToString());
        }
    }

    [TestClass]
    public class TraceVarTester
    {
        [TestMethod]
        public void TestTraceVarRecordingWithNonParallelExec()
        {
            TestExec exec1 = new TestExec();

            TracedValue<int> tv1 = new TracedValue<int>(exec1, 0);
            exec1.Now = new DateTime(2018, 1, 1, 12, 0, 0);
            tv1.Set(111, exec1);
            for (int i = 1; i < 16; i++)
            {
                exec1.Now += TimeSpan.FromHours(i);
                tv1.Set(111 + i, exec1);
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tv1.Length; i++)
            {
                sb.AppendFormat("At {0}, tv1 changed to {1}.\r\n", tv1.GetDateTime(i), tv1.GetValue(i));
            }
            string s = sb.ToString();
            Console.WriteLine(s);
            System.Diagnostics.Debug.Assert(sb.ToString().Equals(
@"At 1/1/0001 12:00:00 AM, tv1 changed to 0.
At 1/1/2018 12:00:00 PM, tv1 changed to 111.
At 1/1/2018 1:00:00 PM, tv1 changed to 112.
At 1/1/2018 3:00:00 PM, tv1 changed to 113.
At 1/1/2018 6:00:00 PM, tv1 changed to 114.
At 1/1/2018 10:00:00 PM, tv1 changed to 115.
At 1/2/2018 3:00:00 AM, tv1 changed to 116.
At 1/2/2018 9:00:00 AM, tv1 changed to 117.
At 1/2/2018 4:00:00 PM, tv1 changed to 118.
At 1/3/2018 12:00:00 AM, tv1 changed to 119.
At 1/3/2018 9:00:00 AM, tv1 changed to 120.
At 1/3/2018 7:00:00 PM, tv1 changed to 121.
At 1/4/2018 6:00:00 AM, tv1 changed to 122.
At 1/4/2018 6:00:00 PM, tv1 changed to 123.
At 1/5/2018 7:00:00 AM, tv1 changed to 124.
At 1/5/2018 9:00:00 PM, tv1 changed to 125.
At 1/6/2018 12:00:00 PM, tv1 changed to 126.
"));
        }

        private IExecutive m_exec1;
        private readonly DateTime m_testStart = new DateTime(2018,1,1,1,0,0);

        private SortedSet<string> eventPreports = new SortedSet<string>();
        private int indents = 0;
        private int tabSize = 4;

        [TestMethod]
        public void TestTraceVarRecordingWithParallelExec()
        {
            m_exec1 = ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            ((IParallelExec) m_exec1).Rolledback +=
                dt =>
                    Console.WriteLine("{0}Exec[{1}] rolled back to {2}.",
                        new string(' ', indents * tabSize),
                        m_exec1.GetHashCode(), 
                        dt.ToShortTimeString());

            IExecutive exec2 = ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);

            TracedValue<int>.NameGeneratorNumPlaces = 1;
            TracedValue<int> tv1 = new TracedValue<int>(m_exec1, 0);

            SetValueAtTime(tv1, m_exec1, 1, 99);
            SetValueAtTime(tv1, m_exec1, 2, 88);
            SetValueAtTime(tv1, m_exec1, 3, 77);
            SetValueAtTime(tv1, m_exec1, 4, 66);
            SetValueAtTime(tv1, m_exec1, 5, 55);

            GetValueAtTime(tv1, m_exec1, 3.5);
            GetValueAtTime(tv1, m_exec1, 6.5);
            GetValueAtTime(tv1, m_exec1, 7.5);

            GetValueAtTime(tv1, exec2, 0.1);
            GetValueAtTime(tv1, exec2, 1.1);
            GetValueAtTime(tv1, exec2, 2.1);
            GetValueAtTime(tv1, exec2, 3);
            GetValueAtTime(tv1, exec2, 4.1);

            SetValueAtTime(tv1, exec2, 5.1, 44);
            SetValueAtTime(tv1, exec2, 6.1, 33);

            GetValueAtTime(tv1, exec2, 5.5);

            foreach ( string report in eventPreports ) Console.WriteLine(report);

            CoExecutor.CoStart(new[]{(IParallelExec)m_exec1, (IParallelExec)exec2}, m_testStart+TimeSpan.FromHours(10));

            Console.WriteLine("Exec1 ran {0} events, and Exec2 ran {1} events.", m_exec1.EventCount, exec2.EventCount);

        }

        private void GetValueAtTime(TracedValue<int> tv, IExecutive fromExec, double hours)
        {
            DateTime when = m_testStart + TimeSpan.FromHours(hours);
            eventPreports.Add(string.Format("At {0} local time, a thread from {1} will read the value of {2}.", 
                when.ToShortTimeString(), 
                string.Format("Exec[{0}]", 
                fromExec.GetHashCode()), 
                tv.Name));

            fromExec.RequestEvent((exec, data) =>
            {
                //lock (Console.Out)
                {
                    Console.Out.WriteLine("{0}Get commanded into world at {1} from world at {2}",
                        new string(' ', indents * tabSize), 
                        m_exec1.Now.ToShortTimeString(),
                        fromExec.Now.ToShortTimeString());

                    indents++;
                    int val = tv.Get(fromExec);
                    indents--;

                    Console.Out.WriteLine("{0}Get returned {1} at Target: {2}, Origin: {3}", 
                        new string(' ', indents * tabSize),
                        val,
                        m_exec1.Now.ToShortTimeString(), 
                        fromExec.Now.ToShortTimeString());
                }
            }, when);
        }

        private void SetValueAtTime(TracedValue<int> tv, IExecutive fromExec, double hours, int value)
        {
            DateTime when = m_testStart + TimeSpan.FromHours(hours);
            eventPreports.Add(string.Format("{4}At {0} local time, a thread from {1} will set the value of {2} to {3}.", 
                when.ToShortTimeString(), 
                string.Format("Exec[{0}]", 
                fromExec.GetHashCode()), 
                tv.Name, 
                value, 
                new string(' ', indents*tabSize)));

            m_exec1.RequestEvent((exec, data) =>
            {
                Console.Out.WriteLine("{0}Set({1}) commanded into world at {2} from world at {3}.",
                    new string(' ', indents * tabSize),
                    value,
                    m_exec1.Now.ToShortTimeString(), 
                    fromExec.Now.ToShortTimeString());

                indents++;
                tv.Set(value, fromExec);
                indents--;

                Console.Out.WriteLine("{0}Set({1}) completed at Target: {2}, Origin: {3}.",
                    new string(' ', indents * tabSize),
                    value,
                    m_exec1.Now.ToShortTimeString(), 
                    fromExec.Now.ToShortTimeString());
            }, when);
        }
    }

#pragma warning disable CS0067
    public class TestExec : IExecutive
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Guid Guid { get; }
        public DateTime Now { get; set; }
        public DateTime? LastEventServed { get; }
        public double CurrentPriorityLevel { get; }
        public ExecState State { get; }
        public ExecEventType CurrentEventType { get; }
        public IList EventList { get; }
        public int RunNumber { get; }
        public uint EventCount { get; }
        public long RequestDaemonEvent(ExecEventReceiver eer, DateTime when, double priority, object userData)
        {
            throw new NotImplementedException();
        }

        public long RequestEvent(ExecEventReceiver eer, DateTime when)
        {
            throw new NotImplementedException();
        }

        public long RequestEvent(ExecEventReceiver eer, DateTime when, object userData)
        {
            throw new NotImplementedException();
        }

        public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData)
        {
            throw new NotImplementedException();
        }

        public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public event ExecutiveEvent ExecutiveStarted_SingleShot;
        public event ExecutiveEvent ExecutiveStarted;
        public event ExecutiveEvent ExecutiveStopped;
        public event ExecutiveEvent ExecutiveFinished;
        public event ExecutiveEvent ExecutiveReset;
        public event EventMonitor EventAboutToFire;
        public event EventMonitor EventHasCompleted;
        public void SetStartTime(DateTime startTime)
        {
            throw new NotImplementedException();
        }

        public long RequestImmediateEvent(ExecEventReceiver eer, object userData, ExecEventType execEventType)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvent(long eventHashCode)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvents(IExecEventSelector ees)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvents(object execEventReceiverTarget)
        {
            throw new NotImplementedException();
        }

        public void UnRequestEvents(Delegate execEventReceiverMethod)
        {
            throw new NotImplementedException();
        }

        public void Join(params long[] eventCodes)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Detach(object target)
        {
            throw new NotImplementedException();
        }

        public void ClearVolatiles(IDictionary dictionary)
        {
            throw new NotImplementedException();
        }

        public IDetachableEventController CurrentEventController { get; }
        public ArrayList LiveDetachableEvents { get; }
        public event ExecutiveEvent ExecutivePaused;
        public event ExecutiveEvent ExecutiveResumed;
        public event ExecutiveEvent ExecutiveAborted;
        public event ExecutiveEvent ClockAboutToChange;
    }
#pragma warning restore CS0067
}
