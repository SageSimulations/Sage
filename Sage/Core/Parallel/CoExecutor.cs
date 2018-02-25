using System;
using System.Threading;
using System.Threading.Tasks;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.SimCore.Parallel
{
    public class CoExecutor
    {
        private static bool m_diagnostics = true;
        private int m_nExecsAtEndTime;
        private readonly IExecutive[] m_execs;
        private readonly DateTime m_terminateAt;
        private CoExecutor(IExecutive[] execs, DateTime terminateAt)
        {
            m_execs = execs;
            m_terminateAt = terminateAt;
        }

        private void StartAll()
        {
            DateTime start = DateTime.Now;
            Thread[] threads = new Thread[m_execs.Length];
            Console.WriteLine("Creating all threads.");
            object lockObj = new object();
            Monitor.Enter(lockObj);
            for (int i = 0; i < m_execs.Length; i++)
            {
                int ndx = i;
                if (m_diagnostics) Console.WriteLine("Creating thread {0}.", ndx);
                threads[ndx] = new Thread(() =>
                {
                    // ReSharper disable once EmptyEmbeddedStatement
                    lock (lockObj);
                    try
                    {
                        if (m_diagnostics) Console.WriteLine("Starting executive {0}.", ndx);
                        m_execs[ndx].RequestEvent(CoTerminate, m_terminateAt);
                        m_execs[ndx].Start();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }) {Name = string.Format("Executive #{0}", ndx), Priority = ThreadPriority.Normal};
            }

            foreach (Thread t in threads) t.Start();

            if (m_diagnostics) Monitor.Exit(lockObj);// Everybody, GO!

            if (m_diagnostics) Console.WriteLine("Starting all tasks.");
            foreach (Thread t in threads) t.Join();

            
            DateTime finish = DateTime.Now;
            if (m_diagnostics) Console.WriteLine("All threads complete in {0} seconds.", (finish - start).TotalSeconds);
        }

        private void _StartAll()
        {
            DateTime start = DateTime.Now;
            Task[] tasks = new Task[m_execs.Length];
            Console.WriteLine("Creating all tasks.");
            object lockObj = new object();
            Monitor.Enter(lockObj);
            for (int i = 0; i < m_execs.Length; i++)
            {
                int ndx = i;
                if (m_diagnostics) Console.WriteLine("Creating and launching task {0}.", ndx);
                tasks[ndx] = Task.Factory.StartNew(() =>
                {
                    // ReSharper disable once EmptyEmbeddedStatement
                    lock (lockObj);
                    try
                    {
                        if (m_diagnostics) Console.WriteLine("Starting executive {0}.", ndx);
                        m_execs[ndx].RequestEvent(CoTerminate, m_terminateAt);
                        m_execs[ndx].Start();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                });
            }

            if (m_diagnostics) Monitor.Exit(lockObj);// Everybody, GO!
            if (m_diagnostics) Console.WriteLine("Starting all tasks.");

            Task.WaitAll(tasks);
            DateTime finish = DateTime.Now;
            if (m_diagnostics) Console.WriteLine("All threads complete in {0} seconds.", (finish - start).TotalSeconds);
        }

        public static void CoStart(IExecutive[] execs, DateTime terminateAt)
        {
            new CoExecutor(execs, terminateAt).StartAll();
        }

        public DateTime GetEarliestDateTime()
        {
            DateTime dt = DateTime.MaxValue;
            foreach (IExecutive executive in m_execs)
            {
                dt = DateTimeOperations.Min(dt, executive.Now);
            }
            return dt;
        }

        private void CoTerminate(IExecutive exec, object userData)
        {
            Interlocked.Increment(ref m_nExecsAtEndTime);
            if (m_nExecsAtEndTime == m_execs.Length) foreach (IExecutive exec2 in m_execs) exec2.Stop();
            else
            {
                Monitor.Exit(exec);
                Thread.Sleep(500);
                Monitor.Enter(exec);
            }
            Interlocked.Decrement(ref m_nExecsAtEndTime);
            exec.RequestEvent(CoTerminate, m_terminateAt);
        }
    }
}
