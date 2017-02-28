/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC {

    ///<summary>
    /// A collection of IPfcLinkElement objects that can be searched by name or by Guid.
    ///</summary>
    public class PfcLinkElementList : List<IPfcLinkElement> {
        /// <summary>
        /// Creates a new instance of the <see cref="T:LinkCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcLinkElementList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:LinkCollection"/> class.
        /// </summary>
        public PfcLinkElementList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:LinkCollection"/> class.
        /// </summary>
        /// <param name="srcCollection">The SRC collection.</param>
        public PfcLinkElementList(ICollection srcCollection)
            : base(srcCollection.Count) {
            foreach (object obj in srcCollection) {
                Add((IPfcLinkElement)obj);
            }
        }

        #region IPfcLinkCollection Members

        /// <summary>
        /// Gets the <see cref="T:IPfcLinkElement"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcLinkElement this[string name] {
            get { return Find(delegate(IPfcLinkElement node) { return node.Name.Equals(name); }); }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcLinkElement"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcLinkElement this[Guid guid] {
            get { return Find(delegate(IPfcLinkElement node) { return node.Guid.Equals(guid); }); }
        }

        #endregion

        /// <summary>
        /// Gets the priority comparer, used to sort this list by increasing link priority.
        /// </summary>
        /// <value>The priority comparer.</value>
        public static IComparer<IPfcLinkElement> PriorityComparer {
            get { return new _PriorityComparer(); }
        }

        private class _PriorityComparer : IComparer<IPfcLinkElement> {
            #region IComparer<IPfcLinkElement> Members

            public int Compare(IPfcLinkElement x, IPfcLinkElement y) {
                return (x.Priority > y.Priority ? 1 : x.Priority < y.Priority ? -1 : 0);
            }

            #endregion
        }

        internal bool NeedsSorting(IComparer<IPfcLinkElement> iComparer) {
            // TODO: Performance improvement to be made here, some day.
            for (int i = 0; i < Count - 1; i++) {
                if (iComparer.Compare(this[i], this[i + 1]) != 1) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// A List of IPfcNode objects that can be searched by name or by Guid.
    /// </summary>
    public class PfcNodeList : List<IPfcNode> {

        private static PfcNodeList _emptyList = new PfcNodeList();

        #region Constructors
        /// <summary>
        /// Creates a new instance of the <see cref="T:PFCNodeList"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcNodeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PFCNodeList"/> class.
        /// </summary>
        public PfcNodeList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PFCNodeList"/> class as a copy of an existing collection.
        /// </summary>
        /// <param name="srcCollection">The source collection.</param>
        public PfcNodeList(ICollection srcCollection)
            : base(srcCollection.Count) {
            foreach (object obj in srcCollection) {
                Add((IPfcNode)obj);
            }
        }

        #endregion Constructors

        #region PFCNodeList Members

        /// <summary>
        /// Returns all nodes in this collection that are of the specified type.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns>
        /// A collection of all nodes in this collection that are of the specified type.
        /// </returns>
        public PfcNodeList GetBy(PfcElementType elementType) {
            if ( elementType.Equals(PfcElementType.Link ) ) {
                return new PfcNodeList();
            }
            return (PfcNodeList)FindAll( delegate(IPfcNode node) { return node.ElementType.Equals(elementType); });

        }

        /// <summary>
        /// Gets the <see cref="T:IPfcNode"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcNode this[string name] {
            get { return Find(delegate(IPfcNode node) { return node.Name.Equals(name); }); }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcNode"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcNode this[Guid guid] {
            get { return Find(delegate(IPfcNode node) { return node.Guid.Equals(guid); }); }
        }

        #endregion

        /// <summary>
        /// Gets an empty list of PfcNodes.
        /// </summary>
        /// <value>The empty list.</value>
        public static PfcNodeList EmptyList { 
            get {
                _Debug.Assert(_emptyList.Count == 0);
                return _emptyList;
            }
        }
    }

    /// <summary>
    /// A collection of IPfcNode objects that can be searched by name or by Guid.
    /// </summary>
    public class PfcStepNodeList : List<IPfcStepNode> {
        /// <summary>
        /// Creates a new instance of the <see cref="T:StepCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcStepNodeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:StepCollection"/> class.
        /// </summary>
        public PfcStepNodeList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:StepCollection"/> class.
        /// </summary>
        /// <param name="srcCollection">The SRC collection.</param>
        public PfcStepNodeList(ICollection srcCollection)
            : base(srcCollection.Count) {
            foreach (object obj in srcCollection) {
                Add((IPfcStepNode)obj);
            }
        }

        #region IPfcStepCollection Members

        /// <summary>
        /// Gets the <see cref="T:IPfcStepNode"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcStepNode this[string name] {
            get { return Find(delegate(IPfcStepNode node) { return node.Name.Equals(name); }); }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcStepNode"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcStepNode this[Guid guid] {
            get { return Find(delegate(IPfcStepNode node) { return node.Guid.Equals(guid); }); }
        }

        #endregion
    }

    /// <summary>
    /// A collection of IPfcTransitionNode objects that can be searched by name or by Guid.
    /// </summary>
    public class PfcTransitionNodeList : List<IPfcTransitionNode> {
        /// <summary>
        /// Creates a new instance of the <see cref="T:TransitionCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcTransitionNodeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:TransitionCollection"/> class.
        /// </summary>
        public PfcTransitionNodeList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:TransitionCollection"/> class.
        /// </summary>
        /// <param name="srcCollection">The SRC collection.</param>
        public PfcTransitionNodeList(ICollection srcCollection) : base() {
            foreach (object obj in srcCollection) {
                Add((IPfcTransitionNode)obj);
            }
        }

        #region IPfcTransitionCollection Members

        /// <summary>
        /// Gets the <see cref="T:IPfcTransitionNode"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcTransitionNode this[string name] {
            get { return Find(delegate(IPfcTransitionNode node) { return node.Name.Equals(name); }); }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcTransitionNode"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcTransitionNode this[Guid guid] {
            get { return Find(delegate(IPfcTransitionNode node) { return node.Guid.Equals(guid); }); }
        }

        #endregion

    }
}
