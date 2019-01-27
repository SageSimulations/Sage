using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.SimCore.Parallel
{
    /// <summary>
    /// Class CoExecutor is responsible for starting, and maintaining running, a set of implementers of
    /// IParallelExec until all have finished running their events.
    /// </summary>
    public class CoExecutor
    {
        // ReSharper disable once InconsistentNaming
        private static readonly bool m_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("CoExecutor");
        private int m_nExecsAtEndTime;
        private readonly IParallelExec[] m_execs;
        private readonly DateTime m_terminateAt;

        private CoExecutor(IParallelExec[] execs, DateTime terminateAt)
        {
            for (int i = 0; i < execs.Length; i++)
            {
                if (execs[i] == null) execs[i] = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            }
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
            // TODO: INVESTIGATE, can this line go after 'foreach (Thread t in threads) t.Start();'?
            foreach (Thread t in threads) t.Join();

            
            DateTime finish = DateTime.Now;
            if (m_diagnostics) Console.WriteLine("All threads complete in {0} seconds.", (finish - start).TotalSeconds);
        }

        public static void CoStart(IParallelExec[] execs, DateTime terminateAt)
        {
            new CoExecutor(execs, terminateAt).StartAll();
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

        private readonly object m_lock1 = new object();


        internal SyncAction Synchronize(IParallelExec callingExecutive, IParallelExec calledExecutive, SyncMode mode)
        {
            SyncAction retval;

            callingExecutive.IsSynching = true;
            lock (m_lock1)
            {
                calledExecutive.LockExecutive();
                while (!calledExecutive.IsBlockedPending
                       && !calledExecutive.IsBlockedAtExecLock
                       && !calledExecutive.IsRollbackRequester
                       && !calledExecutive.IsSynching)
                {
                }
                // At this point, both executives are blocked somewhere. (Calling exec is on this thread.)

                DateTime callersTime = callingExecutive.Now;
                DateTime calleesTime = calledExecutive.Now;

                switch (mode)
                {
                    case SyncMode.Read:
                        if (callersTime > calleesTime) retval = SyncAction.Defer; // Until caller's time in callee.
                        else retval = SyncAction.Execute;
                        break;
                    case SyncMode.Write: // Write and ReadWrite are both the same.
                    case SyncMode.ReadWrite:
                        if (callersTime <= calleesTime)
                        {
                            // Pause the caller until the callee catches up, and then write into present time.
                            retval = SyncAction.Defer; // Until caller's time in callee.
                        }
                        else
                        {
                            // We have to roll back the callee to the callers time. (And everyone else, too, for now.)
                            callingExecutive.IsRollbackRequester = true;
                            InitiateRollBack();
                            callingExecutive.RollbackBlock.WaitOne();
                            retval = SyncAction.Execute;
                        }
                        break;
                    case SyncMode.Execute:
                    // caller is trying to add an event to called executive. This requires that
                    // the caller's requested event posting time must be at or after the 'Now' time
                    // of the called executive. Since the caller wouldn't try to schedule an event
                    // in its own past, that means that the caller's 'Now' must be at or after that
                    // of the called executive, in order to be sure that it's scheduling for the called
                    // executive's future.
                        if (callersTime >= calleesTime)
                        {
                            retval = SyncAction.Execute;
                        }
                        else
                        {
                            // caller's time is earlier than the callee's. Roll back the callee.
                            calledExecutive.IsRollbackRequester = true;
                            InitiateRollBack();
                            // Still need calling executive (whose thread we're on) to await completion of the rollback.
                            callingExecutive.RollbackBlock.WaitOne();
                            retval = SyncAction.Execute;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            callingExecutive.IsSynching = false;
            return retval;
        }

        internal void ReleaseSync(IParallelExec callersExecutive, IParallelExec calledExecutive)
        {
            if (calledExecutive.IsBlockedAtExecLock) calledExecutive.ReleaseExecutive();
        }

        internal DateTime GetEarliestExecDateTime()
        {
            return m_execs.Aggregate(DateTime.MaxValue, (current, executive) => DateTimeOperations.Min(current, executive.Now));
        }

        private bool m_rollbackRequested = false;

        /// <summary>
        /// Stops all execs. Any whose "now" is past the earliest time of any rollback requesters is rolled back.
        /// </summary>
        private void InitiateRollBack()
        {
            if (m_rollbackRequested) return;
            lock (this)
            {
                m_rollbackRequested = true;
                // By executing this in another thread, we allow this one, an executive thread, to proceed to its rollback lock.
                ThreadPool.QueueUserWorkItem(state =>
                {
                    // Wait until all execs have stopped somewhere. Could be pending read block, or could be rollback block.
                    while (m_execs.Any(n => !(n.IsRollbackRequester || n.IsBlockedPending || n.IsBlockedAtExecLock)))
                    {/* NOOP */}

                    DateTime toWhen = m_execs.Where(exec => exec.IsRollbackRequester).Aggregate(DateTime.MaxValue, (current, exec) => Utility.DateTimeOperations.Min(current, exec.Now));

                    // Create the list of execs that need to roll back.
                    List<IParallelExec> targets = m_execs.Where(parallelExec => parallelExec.Now > toWhen).ToList();

                    if (m_diagnostics)
                        Console.WriteLine("Rolling back {0} to {1}.",
                            StringOperations.ToCommasAndAndedList(targets, n => n.Name), toWhen);

                    foreach (IParallelExec target in targets)
                    {
                        // Any target executive that's not at the Rollback Block is stuck at the Pending Read Block. 
                        // The pending read block must be aborted so that it can advance to the Exec Lock.
                        if (target.IsBlockedPending) {
                            target.PendingBlock.Set();
                            while (!target.IsBlockedAtExecLock) {}
                        }
                    }

                    // Now execute rollbacks on targets.
                    System.Threading.Tasks.Parallel.ForEach(targets, n => n.PerformRollback(toWhen));

                    m_rollbackRequested = false;
                });
            }
        }

        /// <summary>
        /// All of the execs must stop during a rollback, for now. // TODO: Maybe figure out how not to halt everyone.
        /// 
        /// 1.) Pause all executives somewhere (if it's a pending read block, and the exec needs to rollback, the
        ///     Pending Read Block will be aborted.)
        /// 2.) 
        /// </summary>
        /// <param name="toWhen">The time to which a rollback is desired.</param>
        private void RollBack(DateTime toWhen)
        {



        }


        public enum SyncAction { Execute, Abort, Defer }
    }
}
