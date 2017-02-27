/* This source code licensed under the GNU Affero General Public License */
#define PREANNOUNCE

/* This source code licensed under the GNU Affero General Public License */

#if INCLUDE_WIP
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility {

    public interface IDirectedGraphNode<T> {
        List<IDirectedGraphNode<T>> Predecessors { get; }
        List<IDirectedGraphNode<T>> Successors { get; }
    }


    public class DirectedGraphNode<T> : IDirectedGraphNode<T> {

#region Private Fields
        private T m_payload;
        private DirectedGraphNodeCollection<T> m_predecessors;
        private DirectedGraphNodeCollection<T> m_successors;
#endregion

#region Constructors
        public DirectedGraphNode(T payload) {
            m_payload = payload;
            m_predecessors = new DirectedGraphNodeCollection<T>();
            m_successors = new DirectedGraphNodeCollection<T>();
        }
#endregion

#region IDirectedGraphNode<T> Members

        public List<IDirectedGraphNode<T>> Predecessors {
            get { return m_predecessors; }
        }

        public List<IDirectedGraphNode<T>> Successors {
            get { return m_successors; }
        }

#endregion
    }

    public class DirectedGraphNodeCollection<T> : IEnumerable<IDirectedGraphNode<T>> {

        private List<IDirectedGraphNode<T>> m_members = null;

        public DirectedGraphNodeCollection() {
            m_members = new List<IDirectedGraphNode<T>>();
        }

        /// <summary>
        /// Adds the specified new member to this collection.
        /// </summary>
        /// <param name="newMember">The new member.</param>
        /// <returns>The DirectedGraphNode that resulted from this addition - either the node to be added, or its DirectedGraphNode wrapper.</returns>
        public IDirectedGraphNode<T> Add(T newMember) {

            // If necessary, create a DirectedGraphNode wrapper.
            IDirectedGraphNode<T> tn = newMember as IDirectedGraphNode<T>;
            if (tn == null) {
                tn = new DirectedGraphNode<T>(newMember);
            }

            return AddNode(tn);

        }

        public IDirectedGraphNode<T> AddNode(IDirectedGraphNode<T> dgn) {

            if (!m_members.Contains(dgn)) {
                m_members.Add(dgn);
                if (OnGainedMembership != null) {
                    OnGainedMembership(m_parent);
                }
            }
            return dgn;
        }

        /// <summary>
        /// Removes the specified existing member from this collection.
        /// </summary>
        /// <param name="existingChild">The existing member.</param>
        /// <returns>True if the removal was successful, otherwise, false.</returns>
        public bool Remove(IDirectedGraphNode<T> existingMember) {
            if (m_members.Remove(existingChild)) {
                m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.GainedNode, existingChild);
                m_parent.MyEventController.OnLostChild(existingChild);
                existingChild.MyEventController.OnLostParent(m_parent);
                return true;
            } else {
                return false;
            }
        }

        public bool Contains(T possibleChild) {
            IDirectedGraphNode<T> tn = possibleChild as IDirectedGraphNode<T>;
            if (tn == null) {
                tn = new DirectedGraphNode<T>(possibleChild);
            }
            return ContainsNode(tn);
        }

        public bool ContainsNode(IDirectedGraphNode<T> possibleChildNode) {
            return m_members.Contains(possibleChildNode);
        }

        /// <summary>
        /// Gets the count of entries in this DirectedGraphNodeCollection.
        /// </summary>
        /// <value>The count.</value>
        public int Count {
            get {
                return m_members.Count;
            }
        }

#region Sorting Handlers
        public void Sort(Comparison<IDirectedGraphNode<T>> comparison) {
            m_members.Sort(comparison);
            m_parent.MyEventController.OnChildrenResorted(m_parent);
            m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, m_parent);
        }

        public void Sort(IComparer<IDirectedGraphNode<T>> comparer) {
            m_members.Sort(comparer);
            m_parent.MyEventController.OnChildrenResorted(m_parent);
            m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, m_parent);
        }

        public void Sort(int index, int count, IComparer<IDirectedGraphNode<T>> comparer) {
            m_members.Sort(index, count, comparer);
            m_parent.MyEventController.OnChildrenResorted(m_parent);
            m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, m_parent);
        }
#endregion

#region IEnumerable<IDirectedGraphNode<T>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<IDirectedGraphNode<T>> GetEnumerator() {
            foreach (IDirectedGraphNode<T> DirectedGraphNode in m_members) {
                yield return DirectedGraphNode;
            }
        }

        /// <summary>
        /// Foreaches the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void ForEach(Action<IDirectedGraphNode<T>> action) {
            m_members.ForEach(action);
        }

        /// <summary>
        /// Finds all children for which the predicate returns true.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        public List<IDirectedGraphNode<T>> FindAll(Predicate<IDirectedGraphNode<T>> match) {
            return m_members.FindAll(match);
        }

#endregion

#region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return m_members.GetEnumerator();
        }

#endregion
    }  
}
#endif
