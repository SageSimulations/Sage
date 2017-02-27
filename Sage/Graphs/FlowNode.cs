/* This source code licensed under the GNU Affero General Public License */
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs {

    public class FlowNode<T> {

        #region Protected fields
        protected T m_payload;
        protected List<FlowNode<T>> m_predecessors;
        protected List<FlowNode<T>> m_successors;
        #endregion

        public FlowNode(T payload) {
            m_payload = payload;
            m_predecessors = new List<FlowNode<T>>();
            m_successors = new List<FlowNode<T>>();
        }

        public List<FlowNode<T>> Successors { get { return m_successors; } }

        public List<FlowNode<T>> Predecessors { get { return m_predecessors; } }

        /// <summary>
        /// Gets or sets the color of the node - graph theory color, used by traversal algorithms.
        /// </summary>
        /// <value>The color.</value>
        public int Color { get; set; }

        /// <summary>
        /// Binds the specified predecessor node to the specified successor node, and vice versa.
        /// </summary>
        /// <param name="predNode">The predecessor node.</param>
        /// <param name="succNode">The successor node.</param>
        /// <param name="allowDuplicateBindings">if set to <c>true</c> [allow duplicate bindings].</param>
        public static void Bind(FlowNode<T> predNode, FlowNode<T> succNode, bool allowDuplicateBindings) {
            if (allowDuplicateBindings || !predNode.m_successors.Contains(succNode)) {
                Bind(predNode, succNode);
            }
            Debug.Assert(DebugConsistencyCheck(predNode, succNode));
        }

        /// <summary>
        /// Binds the specified pred node to the specified succ node, and vice versa.
        /// </summary>
        /// <param name="pred">The predecessor node.</param>
        /// <param name="succ">The successor node.</param>
        public static void Bind(FlowNode<T> pred, FlowNode<T> succ) {
            pred.m_successors.Add(succ);
            succ.m_predecessors.Add(pred);
            Debug.Assert(DebugConsistencyCheck(pred, succ));
        }

        private static bool DebugConsistencyCheck(FlowNode<T> pred, FlowNode<T> succ) {
            return pred.m_successors.TrueForAll(n => n.Predecessors.Contains(pred)) &&
                    succ.m_predecessors.TrueForAll(n => n.Successors.Contains(succ));
        }

        /// <summary>
        /// Binds the specified pred node to the specified succ node, and vice versa.
        /// </summary>
        /// <param name="pred">The predecessor node.</param>
        /// <param name="succ">The successor node.</param>
        internal static void UnBind(FlowNode<T> pred, FlowNode<T> succ) {
            pred.Successors.RemoveAll(n => n == succ);
            succ.Predecessors.RemoveAll(n => n == pred);
            Debug.Assert(DebugConsistencyCheck(pred, succ));
        }

        /// <summary>
        /// Unbinds all predecessors and successors from this node.
        /// </summary>
        internal void UnBindAll() {
            while (Predecessors.Count > 0) {
                FlowNode<T> pre = Predecessors.First();
                pre.Successors.Remove(this);
                Predecessors.Remove(pre);
            }

            while (Successors.Count > 0) {
                FlowNode<T> succ = Successors.First();
                succ.Predecessors.Remove(this);
                Successors.Remove(succ);
            }
            //System.Diagnostics.Debug.Assert(DebugConsistencyCheck(pred, succ));
        }

        public T Payload { [DebuggerStepThrough] get { return m_payload; } }
    }
}
