using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.SimCore.Parallel
{
    /// <summary>
    /// Class CoExecutor is responsible for starting, and maintaining running, a set of implementers of
    /// IParallelExec until all have finished running their events.
    /// </summary>
    public class CoExecutor
    {
        private static bool m_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("CoExecutor");
        private int m_nExecsAtEndTime;
        private readonly IParallelExec[] m_execs;
        private readonly DateTime m_terminateAt;

        private CoExecutor(IParallelExec[] execs, DateTime terminateAt)
        {
            m_execs = execs;
            m_terminateAt = terminateAt;
            foreach (IParallelExec executive in m_execs)
            {
                executive.Coexecutor = this;
            }
        }

        private void StartAll()
        {
            DateTime start = DateTime.Now;
            Thread[] threads = new Thread[m_execs.Length];
            Console.WriteLine("Creating all threads.");
            foreach (IParallelExec executive in m_execs)
            {
                Monitor.Enter(executive);
            }
            for (int i = 0; i < m_execs.Length; i++)
            {
                int ndx = i;
                if (m_diagnostics) Console.WriteLine("Creating thread {0}.", ndx);
                threads[ndx] = new Thread(() =>
                {
                    // ReSharper disable once EmptyEmbeddedStatement
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
                }) {Name = m_execs[ndx].Name, Priority = ThreadPriority.Normal};

            }

            foreach (Thread t in threads) t.Start();

            if (m_diagnostics) Console.WriteLine("Starting all tasks.");
            foreach (IParallelExec executive in m_execs)
            {
                Monitor.Exit(executive);
            }

            // TODO: What if one of the execs finishes between the last line and this one?
            foreach (Thread t in threads) t.Join();

            
            DateTime finish = DateTime.Now;
            if (m_diagnostics) Console.WriteLine("All threads complete in {0} seconds.", (finish - start).TotalSeconds);
        }

        public static void CoStart(IParallelExec[] execs, DateTime terminateAt)
        {
            new CoExecutor(execs, terminateAt).StartAll();
        }

        public DateTime GetEarliestExecDateTime()
        {
            DateTime dt = DateTime.MaxValue;
            foreach (IParallelExec executive in m_execs)
            {
                dt = DateTimeOperations.Min(dt, executive.Now);
            }
            return dt;
        }

        private readonly object m_rollbackLock = new object();
        private bool m_rollbackInProgress = false;
        private DateTime m_rollbackTo = DateTime.MaxValue;
        public int nRollbacksCommanded = 0;

        /// <summary>
        /// 1.) Pause all executives.
        /// 2.) For executives that are blocked at, or running after the rollback-to-time, unblock them, repeatedly, until all are waiting at the RollbackSynchronizer.
        /// 3.) Command each to roll back to time toWhen.
        /// </summary>
        /// <param name="toWhen">The time to which a rollback is desired.</param>
        public void RollBack(DateTime toWhen)
        {
            nRollbacksCommanded++;
            lock (m_rollbackLock)
            {
                bool isMaster = !m_rollbackInProgress;
                m_rollbackInProgress = true;

                m_rollbackTo = DateTimeOperations.Min(m_rollbackTo, toWhen);

                // Tell all of the execs to stop.
                if (isMaster)
                {
                    foreach (IParallelExec executive in m_execs) executive.RollbackBlock.Reset();
                    ThreadPool.QueueUserWorkItem(state => CoordinateRollback());
                }
            }
        }

        //public bool m_executingRollback;
        private void CoordinateRollback()
        {

            // Wait until all execs have stopped.
            bool allExecsAreBlocked;
            do
            {
                allExecsAreBlocked = true;
                foreach (IParallelExec parallelExec in m_execs)
                {
                    allExecsAreBlocked &= (parallelExec.IsBlockedInEventCall || parallelExec.IsBlockedAtRollbackBlock);
                }
            } while (!allExecsAreBlocked);
            // All threads are waiting...

            // Create the list of execs that need to roll back.
            List<IParallelExec> targets = new List<IParallelExec>();
            foreach (IParallelExec parallelExec in m_execs) if ( parallelExec.Now > m_rollbackTo ) targets.Add(parallelExec);

            if ( m_diagnostics ) Console.WriteLine("Rolling back {0} to {1}.", StringOperations.ToCommasAndAndedList(targets, n=>n.Name), m_rollbackTo);

            // Unblock any blocked targets. They will advance to the Rollback Block. May have to keep doing this for a while,
            // since, for example, many future-reads are multiple-iteration reads (but good design suggests that they shouldn't be?
            targets.ForEach(n =>
            {
                if (n.IsBlockedInEventCall)
                {
                    if (m_diagnostics) Console.WriteLine("{0} is blocked in an event call.", n.Name);
                    while (!n.IsBlockedAtRollbackBlock) n.FutureReadBlock.Set();
                }
                if (n.IsBlockedAtRollbackBlock)
                {
                    if (m_diagnostics) Console.WriteLine("{0} is blocked at rollback block.", n.Name);
                }
            });

            // Wait for all targets to stop.
            while (m_execs.Any(n => !(n.IsBlockedAtRollbackBlock||n.IsBlockedInEventCall))){}

            // Now execute rollbacks on targets.
            //m_executingRollback = true;
            System.Threading.Tasks.Parallel.ForEach(targets, n => n.PerformRollback(m_rollbackTo));
            //m_executingRollback = false;

            m_rollbackTo = DateTime.MaxValue;
            nRollbacksCommanded = 0;
            m_rollbackInProgress = false;

            // All tasks have completed rollback. Resume running.
            foreach (IParallelExec executive in m_execs)
            {
                executive.RollbackBlock.Set();
            }
        }

        private void CoTerminate(IExecutive executive, object userData)
        {
            IParallelExec exec = (IParallelExec)executive;
            if (m_diagnostics) Console.WriteLine("{0} asking to terminate at {1}", exec.Name, exec.Now);
            Interlocked.Increment(ref m_nExecsAtEndTime);
            if (m_nExecsAtEndTime == m_execs.Length) foreach (IParallelExec exec2 in m_execs) exec2.Stop();
            else Thread.Sleep(500);
            Interlocked.Decrement(ref m_nExecsAtEndTime);
            exec.RequestEvent(CoTerminate, m_terminateAt);
        }
    }
}
