/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Linq;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Graphs {
    /// <summary>
    /// An engine for determining critical paths through a directed acyclic graph (DAG).
    /// </summary>
    /// <typeparam name="T">The type of task being represented in this DAG.</typeparam>
    public class CriticalPathAnalyst<T> {

        private T m_startNode;
        private T m_finishNode;
        private Func<T, DateTime> m_startTime;
        private Func<T, TimeSpan> m_duration;
        private Func<T, bool> m_isFixed;
        private Func<T, IEnumerable<T>> m_successors;
        private Func<T, IEnumerable<T>> m_predecessors;
        private List<T> m_criticalPath;
        private Dictionary<T, TimingData> m_timingData;

        /// <summary>
        /// Initializes a new instance of the <see cref="CriticalPathAnalyst{T}"/> class.
        /// </summary>
        /// <param name="startNode">The start node of the directed acyclic graph (DAG).</param>
        /// <param name="finishNode">The finish node of the DAG.</param>
        /// <param name="startTime">A function that, given a task element, returns its start time.</param>
        /// <param name="duration">A function that, given a task element, returns its duration.</param>
        /// <param name="isFixed">A function that, given a task element, returns whether its start time and duration are fixed.</param>
        /// <param name="successors">A function that, given a task element, returns its successors.</param>
        /// <param name="predecessors">A function that, given a task element, returns its predecessors.</param>
        public CriticalPathAnalyst(T startNode, T finishNode, Func<T, DateTime> startTime, Func<T, TimeSpan> duration, Func<T,bool> isFixed, Func<T, IEnumerable<T>> successors, Func<T, IEnumerable<T>> predecessors) {
            m_startNode = startNode;
            m_finishNode = finishNode;
            m_startTime = startTime;
            m_duration = duration;
            m_isFixed = isFixed;
            m_successors = successors;
            m_predecessors = predecessors;

            m_criticalPath = null;
            m_timingData = null;
        }

        /// <summary>
        /// Returns the critical path.
        /// </summary>
        /// <value>The critical path.</value>
        public IEnumerable<T> CriticalPath {
            get {
                if (m_criticalPath == null) {
                    ComputeCriticalPath();
                }
                return m_criticalPath;
            }
        }

        /// <summary>
        /// Computes (or recomputes) the critical path. This is called automatically if necessary when the Critical Path is requested.
        /// </summary>
        public void ComputeCriticalPath() {
            m_criticalPath = new List<T>();
            m_timingData = new Dictionary<T, TimingData>();
            PropagateForward(TimingDataNodeFor(m_startNode));

            TimingData tdFinish = TimingDataNodeFor(m_finishNode);
            tdFinish.Fix(tdFinish.EarlyStart, tdFinish.NominalDuration, true);
            PropagateBackward(TimingDataNodeFor(m_finishNode));

            AnalyzeCriticality();
        }

        private void AnalyzeCriticality() {
            // Rough. Starting.
            foreach (TimingData tdNode in m_timingData.Values.Where(n=>n.IsCritical).OrderBy(n=>n.EarlyStart)) {
                m_criticalPath.Add(tdNode.Subject);
            }
        }

        // TODO: Performance improvement if TDNode had its TDNode successors & predecessors retrievable directly.

        /// <summary>
        /// Performs a depth-first propagation along a path for which all predecessors' computations are complete,
        /// adjusting early start &amp; finish according to a PERT methodology.
        /// </summary>
        /// <param name="tdNode">The TimingData node.</param>
        private void PropagateForward(TimingData tdNode) {
            
            tdNode.EarlyFinish = tdNode.EarlyStart + tdNode.NominalDuration;

            foreach (TimingData successor in m_successors(tdNode.Subject).Select(n=>TimingDataNodeFor(n))) {
                if (!successor.IsFixed) {
                    successor.EarlyStart = DateTimeOperations.Max(successor.EarlyStart, tdNode.EarlyFinish);
                }
                successor.RegisterPredecessor();
                if (successor.AllPredecessorsHaveWeighedIn) {
                    PropagateForward(successor);
                }
            }
        }

        /// <summary>
        /// Performs a depth-first propagation backwards along a path for which all successors' computations
        /// are complete, adjusting late start &amp; finish according to a PERT methodology.
        /// </summary>
        /// <param name="tdNode">The TimingData node.</param>
        private void PropagateBackward(TimingData tdNode) {
                
            tdNode.LateStart = tdNode.LateFinish - tdNode.NominalDuration;

            foreach (TimingData predecessor in m_predecessors(tdNode.Subject).Select(n => TimingDataNodeFor(n))) {
                if (!predecessor.IsFixed) {
                    predecessor.LateFinish = DateTimeOperations.Min(predecessor.LateFinish, tdNode.LateStart);
                }
                predecessor.RegisterSuccessor();
                if (predecessor.AllSuccessorsHaveWeighedIn) {
                    PropagateBackward(predecessor);
                }
            }
        }

        /// <summary>
        /// Gets (or creates) the timing data node for the provided client-domain node.
        /// </summary>
        /// <param name="node">The client-domain node.</param>
        /// <returns></returns>
        private TimingData TimingDataNodeFor(T node) {
            TimingData tdNode;
            if (!m_timingData.TryGetValue(node, out tdNode)) {
                tdNode = new TimingData(
                    node, 
                    m_isFixed(node), 
                    m_startTime(node), 
                    m_duration(node), 
                    (short)m_predecessors(node).Count(),
                    (short)m_successors(node).Count());
                m_timingData.Add(node, tdNode);
            }
            return tdNode;
        }

        private class TimingData : ICriticalPathTimingData {

            public TimingData(T subject, bool isFixed, DateTime nominalStart, TimeSpan nominalDuration, short nPreds, short nSuccs) {
                Subject = subject;
                if (isFixed) {
                    Fix(nominalStart, nominalDuration, true);
                } else {
                    NominalStart = nominalStart;
                    NominalDuration = nominalDuration;
                    EarlyStart = EarlyFinish = DateTime.MinValue; // Explicit for clarity.
                    LateStart = LateFinish = DateTime.MaxValue;
                }
                m_totalNumOfPredecessors = nPreds;
                m_totalNumOfSuccessors = nSuccs;
                m_totalNumOfPredecessorsWeighedIn = 0;
                m_totalNumOfSuccessorsWeighedIn = 0;
            }
            #region ITimingData Members

            public DateTime EarlyStart { get; set; }

            public DateTime LateStart { get; set; }

            public DateTime EarlyFinish { get; set; }

            public DateTime LateFinish { get; set; }

            public double Criticality { get; set; }

            public bool IsCritical {
                get { return EarlyStart.Equals(LateStart) && EarlyFinish.Equals(LateFinish); }
            }
            #endregion

            private bool m_fixed;
            public bool IsFixed { get { return m_fixed; } }
            public void Fix(DateTime startTime, TimeSpan duration, bool setAsNominal) {
                EarlyStart = LateStart = startTime;
                EarlyFinish = LateFinish = startTime + duration;
                m_fixed = true;
                if (setAsNominal) {
                    NominalStart = startTime;
                    NominalDuration = duration;
                }
            }

            public DateTime NominalStart { get; set; }

            public TimeSpan NominalDuration { get; set; }

            public T Subject { get; set; }

            private short m_totalNumOfPredecessors;
            private short m_totalNumOfPredecessorsWeighedIn;
            internal void RegisterPredecessor() {
                m_totalNumOfPredecessorsWeighedIn++;
            }

            internal bool AllPredecessorsHaveWeighedIn {
                get {
                    return m_totalNumOfPredecessorsWeighedIn == m_totalNumOfPredecessors;
                }
            }
            private short m_totalNumOfSuccessors;
            private short m_totalNumOfSuccessorsWeighedIn;
            internal void RegisterSuccessor() {
                m_totalNumOfSuccessorsWeighedIn++;
            }

            internal bool AllSuccessorsHaveWeighedIn {
                get {
                    return m_totalNumOfSuccessorsWeighedIn == m_totalNumOfSuccessors;
                }
            }
        }
    }

    public interface ICriticalPathTimingData {
        DateTime EarlyStart { get; }
        DateTime LateStart { get; }
        DateTime EarlyFinish { get; }
        DateTime LateFinish { get; }
        bool IsCritical { get; }
        double Criticality { get; }
    }


}
