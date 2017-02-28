/* This source code licensed under the GNU Affero General Public License */
//#define PREANNOUNCE
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// The types of subtree changes reported by the Tree Support classes.
    /// </summary>
    public enum SubtreeChangeType
    {
        /// <summary>
        /// A node was added somewhere in the tree.
        /// </summary>
        GainedNode,
        /// <summary>
        /// A node was removed somewhere in the tree.
        /// </summary>
        LostNode,
        /// <summary>
        /// A node's children were resorted somewhere in the tree.
        /// </summary>
        ChildrenResorted
    }

    public enum TreeStructureCheckType { None, Shallow, Deep }

    /// <summary>
    /// An event that pertains to some change in relationship between two nodes.
    /// </summary>
    /// <typeparam name="T">The payload type of the nodes.</typeparam>
    /// <param name="self">The node firing the event.</param>
    /// <param name="subject">The node to which the event refers.</param>
    public delegate void TreeNodeEvent<T>(ITreeNode<T> self, ITreeNode<T> subject);

    /// <summary>
    /// An event that pertains to some change in the tree underneath a given node.
    /// </summary>
    /// <typeparam name="T">The payload type of the nodes.</typeparam>
    /// <param name="changeType">The SubtreeChangeType.</param>
    /// <param name="where">The node to which the event refers.</param>
    public delegate void TreeChangeEvent<T>(SubtreeChangeType changeType, ITreeNode<T> where);

    /// <summary>
    /// The ITreeNode interface is implemented by any object that participates in a tree data structure.
    /// An object may derive from TreeNode&lt;T&gt; or implement ITreeNode&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITreeNode<T>
    {

        #region Events
#if PREANNOUNCE
        event TreeNodeEvent<T> AboutToLoseParent;
        event TreeNodeEvent<T> AboutToGainParent;
        event TreeNodeEvent<T> AboutToLoseChild;
        event TreeNodeEvent<T> AboutToGainChild;
#endif
        /// <summary>
        /// Fired when this node is detached from a parent.
        /// </summary>
        event TreeNodeEvent<T> LostParent;

        /// <summary>
        /// Fired when this node is attached to a parent.
        /// </summary>
        event TreeNodeEvent<T> GainedParent;

        /// <summary>
        /// Fired when this node has lost a child.
        /// </summary>
        event TreeNodeEvent<T> LostChild;

        /// <summary>
        /// Fired when this node has gained a child.
        /// </summary>
        event TreeNodeEvent<T> GainedChild;

        /// <summary>
        /// <summary>
        /// Fired when this node's child list has been resorted.
        /// </summary>
        /// </summary>
        event TreeNodeEvent<T> ChildrenResorted;

        /// <summary>
        /// Fired when a change (Gain, Loss or Child-Resorting) in this node's subtree has occurred.
        /// </summary>
        event TreeChangeEvent<T> SubtreeChanged;
        #endregion Events

        /// <summary>
        /// Gets the root node above this one.
        /// </summary>
        /// <value>The root.</value>
        ITreeNode<T> Root { get; }

        /// <summary>
        /// Gets the payload of this node. The payload is the node itself, if the subject nodes inherit from TreeNode&lt;T&gt;.
        /// If the Payload is null, and you inherit from TreeNode&lt;T&gt;, you need to set SelfReferential to true in the ctor.
        /// </summary>
        /// <value>The payload.</value>
        T Payload { get; }

        /// <summary>
        /// Gets or sets the parent of this tree node.
        /// </summary>
        /// <value>The parent.</value>
        ITreeNode<T> Parent { get; set; }

        /// <summary>
        /// Sets the parent of this node, but does not then set this node as a child to that parent if childAlreadyAdded is set to <c>true</c>.
        /// </summary>
        /// <param name="newParent">The new parent.</param>
        /// <param name="skipStructureChecking">if set to <c>true</c> [skip structure checking].</param>
        /// <param name="childAlreadyAdded">if set to <c>true</c> [child already added].</param>
        void SetParent(ITreeNode<T> newParent, bool skipStructureChecking, bool childAlreadyAdded = false);

        #region Enumerables
        /// <summary>
        /// Gets an enumerable over this node's siblings in the hierarchy.
        /// </summary>
        IEnumerable<T> Siblings(bool includeSelf);

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes breadth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        IEnumerable<ITreeNode<T>> DescendantNodesBreadthFirst(bool includeSelf);

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes depth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        IEnumerable<ITreeNode<T>> DescendantNodesDepthFirst(bool includeSelf);

        /// <summary>
        /// Returns an IEnumerable that traverses the descendant payloads breadth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        IEnumerable<T> DescendantsBreadthFirst(bool includeSelf);

        /// <summary>
        /// Returns an IEnumerable that traverses the descendant payloads depth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        IEnumerable<T> DescendantsDepthFirst(bool includeSelf);

        #endregion Enumerables

        /// <summary>
        /// Determines whether this node is a child of the specified 'possible parent' node.
        /// </summary>
        /// <param name="possibleParentNode">The possible parent node.</param>
        /// <returns>
        /// 	<c>true</c> if this node is a child of the specified 'possible parent' node; otherwise, <c>false</c>.
        /// </returns>
        bool IsChildOf(ITreeNode<T> possibleParentNode);

        /// <summary>
        /// Gets the children, if any, of this node. Return value will be an empty collection if there are no children.
        /// </summary>
        /// <value>The children.</value>
        IEnumerable<ITreeNode<T>> Children { get; }

        ITreeNode<T> AddChild(T newChild, bool skipStructuralChecking = false);
        ITreeNode<T> AddChild(ITreeNode<T> tn, bool skipStructuralChecking = false);
        bool RemoveChild(T existingChild);
        bool RemoveChild(ITreeNode<T> existingChild);
        void SortChildren(Comparison<ITreeNode<T>> comparison);
        void SortChildren(IComparer<ITreeNode<T>> comparer);
        bool HasChild(ITreeNode<T> existingChild);
        bool HasChild(T possibleChild);
        void ForEachChild(Action<T> action);
        void ForEachChild(Action<ITreeNode<T>> action);

        /// <summary>
        /// Provides an IEnumerable over the child nodes (i.e. the payloads of the children.)
        /// </summary>
        /// <value>The child nodes.</value>
        IEnumerable<T> ChildNodes { get; }

        /// <summary>
        /// Gets the tree node event controller. This should only be obtained by a descendant
        /// or parent TreeNode or TreeNodeCollection to report changes that are taking place
        /// with respect to the subject TreeNode so that it may report its own changes.
        /// </summary>
        /// <value>The tree node event controller.</value>
        ITreeNodeEventController<T> MyEventController { get; }

    }

    public interface ITreeNodeEventController<T>
    {

#if PREANNOUNCE
        void OnAboutToLoseParent(ITreeNode<T> parent);
        void OnAboutToGainParent(ITreeNode<T> parent);
        void OnAboutToLoseChild(ITreeNode<T> child);
        void OnAboutToGainChild(ITreeNode<T> child);
#endif
        void OnLostParent(ITreeNode<T> parent);
        void OnGainedParent(ITreeNode<T> parent);
        void OnLostChild(ITreeNode<T> child);
        void OnGainedChild(ITreeNode<T> child);
        void OnChildrenResorted(ITreeNode<T> self);
        void OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where);
    }

    /// <summary>
    /// Want to be able to use TreeNode as either a base class, a container or a wrapper.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TreeNode<T> : ITreeNode<T>
    {

        #region Private Fields
        private T m_payload;
        private TreeNodeCollection<T> m_children;
        private ITreeNode<T> m_parent;
        private ITreeNodeEventController<T> m_treeNodeEventController;
        private bool m_isSelfReferential;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode&lt;T&gt;"/> class.
        /// </summary>
        // ReSharper disable once MemberCanBeProtected.Global Treenode can be delegated to, or contain, its payload.
        public TreeNode() : this(default(T)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public TreeNode(T payload)
        {
            m_payload = payload;
        }

        #endregion

        /// <summary>
        /// Gets the children, if any, of this node. Return value will be an empty collection if there are no children.
        /// </summary>
        /// <value>The children.</value>
        public IEnumerable<ITreeNode<T>> Children => m_children ?? (m_children = new TreeNodeCollection<T>(this));

        public ITreeNode<T> AddChild(T newChild, bool skipStructuralChecking = false)
        {
            if ( m_children == null ) m_children = new TreeNodeCollection<T>(this);
            return m_children.Add(newChild, skipStructuralChecking);
        }

        public ITreeNode<T> AddChild(ITreeNode<T> newNode, bool skipStructuralChecking = false)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            return m_children.AddNode(newNode, skipStructuralChecking);
        }


        public bool RemoveChild(T existingChild)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            return m_children.Remove(existingChild);
        }

        public bool RemoveChild(ITreeNode<T> existingChild)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            return m_children.Remove(existingChild);
        }

        public void SortChildren(Comparison<ITreeNode<T>> comparison)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            m_children.Sort(comparison);
        }

        public void SortChildren(IComparer<ITreeNode<T>> comparer)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            m_children.Sort(comparer);
        }

        public bool HasChild(ITreeNode<T> possibleChild)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            return m_children.ContainsNode(possibleChild);
        }

        public bool HasChild(T possibleChild)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            return m_children.Contains(possibleChild);
        }

        public void ForEachChild(Action<T> action)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            m_children.ForEach(n=>action(n.Payload));
        }

        public void ForEachChild(Action<ITreeNode<T>> action)
        {
            if (m_children == null) m_children = new TreeNodeCollection<T>(this);
            m_children.ForEach(action);
        }

        /// <summary>
        /// Provides an IEnumerable over the child nodes (i.e. the payloads of the children.)
        /// </summary>
        /// <value>The child nodes.</value>
        public IEnumerable<T> ChildNodes => Children.Cast<T>();

        /// <summary>
        /// Gets or sets the parent of this tree node.
        /// </summary>
        /// <value>The parent.</value>
        public ITreeNode<T> Parent
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return m_parent;
            }
            set
            {
                SetParent(value, false); // By default, we do not skip structure checking.
            }
        }

        public void SetParent(ITreeNode<T> newParent, bool skipStructureChecking, bool childAlreadyAdded = false)
        {
            if (!Equals(m_parent,newParent))
            {
                if (m_parent != null)
                {
                    ITreeNode<T> tmpParent = m_parent;
                    ITreeNode<T> tmpRoot = Root;
                    m_parent = null;
                    tmpParent.RemoveChild(this);
                    tmpRoot.MyEventController.OnSubtreeChanged(SubtreeChangeType.LostNode, this);
                    MyEventController.OnLostParent(tmpParent);
                    tmpParent.MyEventController.OnLostChild(this);
                }
                m_parent = newParent;
                if (m_parent != null)
                {
                    m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.GainedNode, this);
                    MyEventController.OnGainedParent(m_parent);
                }
            }

            // Cannot skip structure checking with the 'ContainsNode' call because when the parent is set,
            // it sets the child, which sets the parent, which sets the ... . Something has to break this.
            // This is an opportunity for performance improvement.
            if (m_parent != null && (skipStructureChecking || !m_parent.HasChild(this)))
            {
                if (!childAlreadyAdded)
                {
                    m_parent.AddChild((T)this, skipStructureChecking);
                }
            }

        }

        /// <summary>
        /// Gets the root node above this one.
        /// </summary>
        /// <value>The root.</value>
        public ITreeNode<T> Root => m_parent == null ? this : m_parent.Root;

        #region Enumerators and Enumerables
        /// <summary>
        /// Gets an enumerator over this node's siblings in the hierarchy.
        /// </summary>
        /// <param name="includeSelf">if set to <c>true</c> [include self].</param>
        /// <returns></returns>
        /// <value></value>
        public IEnumerable<T> Siblings(bool includeSelf)
        {
            if (Parent == null && includeSelf)
            {
                yield return Payload;
            }
            else
            {
                if (Parent == null) yield break;
                foreach (ITreeNode<T> tn in Parent.Children)
                {
                    if (!Equals(tn, this) || includeSelf)
                    {
                        yield return tn.Payload;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes breadth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        public IEnumerable<ITreeNode<T>> DescendantNodesBreadthFirst(bool includeSelf)
        {

            Queue<ITreeNode<T>> q = new Queue<ITreeNode<T>>();

            #region Prime the queue
            if (includeSelf)
            {
                q.Enqueue(this);
            }
            else
            {
                foreach (ITreeNode<T> child in Children)
                {
                    q.Enqueue(child);
                }
            }
            #endregion

            // For every node in the queue, dequeue it, enqueue all of its children, and yield-return it.
            while (q.Count > 0)
            {
                ITreeNode<T> node = q.Dequeue();
                node.ForEachChild(delegate (ITreeNode<T> tn) { q.Enqueue(tn); });
                yield return node;
            }
        }

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes depth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        public IEnumerable<ITreeNode<T>> DescendantNodesDepthFirst(bool includeSelf)
        {
            if (includeSelf)
            {
                yield return this;
            }

            foreach (ITreeNode<T> child in Children)
            {
                foreach (ITreeNode<T> itnt in child.DescendantNodesDepthFirst(true))
                {
                    yield return itnt;
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerable that traverses the descendant payloads breadth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<T> DescendantsBreadthFirst(bool includeSelf) => DescendantNodesBreadthFirst(includeSelf).Select(itnt => itnt.Payload);

        /// <summary>
        /// Returns an iterator that traverses the descendant payloads depth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<T> DescendantsDepthFirst(bool includeSelf) => DescendantNodesDepthFirst(includeSelf).Select(itnt => itnt.Payload);

        #endregion

        #region ITreeNode<T> Members

#if PREANNOUNCE
        internal void _OnAboutToLoseParent(ITreeNode<T> parent) { if (AboutToLoseParent != null) AboutToLoseParent(this, parent); }

        internal void _OnAboutToGainParent(ITreeNode<T> parent) { if (AboutToGainParent != null) AboutToGainParent(this, parent); }

        internal void _OnAboutToLoseChild(ITreeNode<T> child) { if (AboutToLoseChild != null) AboutToLoseChild(this, child); }

        internal void _OnAboutToGainChild(ITreeNode<T> child) { if (AboutToGainChild != null) AboutToGainChild(this, child); }

        public event TreeNodeEvent<T> AboutToLoseParent;

        public event TreeNodeEvent<T> AboutToGainParent;

        public event TreeNodeEvent<T> AboutToLoseChild;

        public event TreeNodeEvent<T> AboutToGainChild;
#endif
        internal void _OnLostParent(ITreeNode<T> parent) { LostParent?.Invoke(this, parent); }

        internal void _OnGainedParent(ITreeNode<T> parent) { GainedParent?.Invoke(this, parent); }

        internal void _OnLostChild(ITreeNode<T> child) { LostChild?.Invoke(this, child); }

        internal void _OnGainedChild(ITreeNode<T> child) { GainedChild?.Invoke(this, child); }

        internal void _OnGainedDescendant(ITreeNode<T> descendant)
        {
            GainedDescendant?.Invoke(this, descendant);
            ((TreeNode<T>)Parent)?._OnGainedDescendant(descendant);
        }

        internal void _OnLostDescendant(ITreeNode<T> descendant)
        {
            LostDescendant?.Invoke(this, descendant);
            ((TreeNode<T>)Parent)?._OnLostDescendant(descendant);
        }

        // ReSharper disable once UnusedParameter.Global // Has to fit the signature.
        internal void _OnChildrenResorted(ITreeNode<T> where)
        {
            ChildrenResorted?.Invoke(this, this);
        }

        internal void _OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where) { SubtreeChanged?.Invoke(changeType, where); }

        public event TreeNodeEvent<T> LostParent;

        public event TreeNodeEvent<T> GainedParent;

        public event TreeNodeEvent<T> LostChild;

        public event TreeNodeEvent<T> GainedChild;

        public event TreeNodeEvent<T> LostDescendant;

        public event TreeNodeEvent<T> GainedDescendant;

        public event TreeNodeEvent<T> ChildrenResorted;

        public event TreeChangeEvent<T> SubtreeChanged;

        public T Payload
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return m_payload;
            }
        }

        protected bool IsSelfReferential
        {
            get { return m_isSelfReferential; }
            set
            {
                m_isSelfReferential = value;
                if (m_isSelfReferential)
                {
                    // This is necessary because if we try to directly set m_payload to this,
                    // the static type converter just returns m_payload, and we are setting 
                    // m_payload to m_payload (which is already null). Thus, payload is null
                    // in a self-referential TreeNode if we don't do this.
                    object obj = this;
                    m_payload = (T)obj;
                }
            }
        }

        protected void SetPayload(T payload)
        {

            if (m_isSelfReferential && !Equals(payload as TreeNode<T>, this))
            {
                string msg = string.Format("Instances of {0} are self-referential, and therefore their payloads cannot be set to other than themselves.", GetType().Name);
                throw new ApplicationException(msg);
            }
            m_payload = payload;
        }

        #endregion

        public bool IsChildOf(ITreeNode<T> possibleParentNode)
        {
            ITreeNode<T> cursor = Parent;
            while (cursor != null)
            {
                if (cursor.Equals(possibleParentNode))
                {
                    return true;
                }
                else
                {
                    cursor = cursor.Parent;
                }
            }
            return false;
        }

        public static explicit operator T(TreeNode<T> treeNode)
        {
            return treeNode.Payload;
        }

        public ITreeNodeEventController<T> MyEventController
        {
            get { return m_treeNodeEventController ?? (m_treeNodeEventController = new TreeNodeEventController(this)); }

            protected set
            {
                m_treeNodeEventController = value;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false; // Since I'm not null.

            TreeNode<T> that = obj as TreeNode<T>;
            if (that == null)
            { // It's not a treenode, so compare it to my payload.
                return obj.Equals(m_payload);
            }

            // The other object is a TreeNode<T>.
            return GetHashCode() == that.GetHashCode() && Children.Equals(that.Children);

        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode()
        {

            if (m_payload != null && !Equals(this, m_payload))
            {
                return m_payload.GetHashCode();
            }

            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        private class TreeNodeEventController : ITreeNodeEventController<T>
        {

            private readonly TreeNode<T> m_me;
            public TreeNodeEventController(TreeNode<T> me)
            {
                m_me = me;
            }

            #region ITreeNodeEventController<T> Members
#if PREANNOUNCE
            public void OnAboutToLoseParent(ITreeNode<T> parent) {
                m_me._OnAboutToLoseParent(parent);
            }

            public void OnAboutToGainParent(ITreeNode<T> parent) {
                m_me._OnAboutToGainParent(parent);
            }

            public void OnAboutToLoseChild(ITreeNode<T> child) {
                m_me._OnAboutToLoseChild(child);
            }

            public void OnAboutToGainChild(ITreeNode<T> child) {
                m_me._OnAboutToGainChild(child);
            }
#endif
            public void OnLostParent(ITreeNode<T> parent)
            {
                m_me._OnLostParent(parent);
            }

            public void OnGainedParent(ITreeNode<T> parent)
            {
                m_me._OnGainedParent(parent);
            }

            public void OnLostChild(ITreeNode<T> child)
            {
                m_me._OnLostChild(child);
                m_me._OnLostDescendant(child);
            }

            public void OnGainedChild(ITreeNode<T> child)
            {
                m_me._OnGainedChild(child);
                m_me._OnGainedDescendant(child);
            }

            public void OnChildrenResorted(ITreeNode<T> self)
            {
                m_me._OnChildrenResorted(self);
            }

            public void OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where)
            {
                m_me._OnSubtreeChanged(changeType, where);
            }
            #endregion
        }

        // ReSharper disable once UnusedMember.Local
        private class MuteTreeNodeEventController : ITreeNodeEventController<T>
        {

            // ReSharper disable once UnusedParameter.Local
            public MuteTreeNodeEventController(TreeNode<T> me) { }

            #region ITreeNodeEventController<T> Members
#if PREANNOUNCE
            public void OnAboutToLoseParent(ITreeNode<T> parent) {}

            public void OnAboutToGainParent(ITreeNode<T> parent) {}

            public void OnAboutToLoseChild(ITreeNode<T> child) {}

            public void OnAboutToGainChild(ITreeNode<T> child) {}
#endif
            public void OnLostParent(ITreeNode<T> parent) { }

            public void OnGainedParent(ITreeNode<T> parent) { }

            public void OnLostChild(ITreeNode<T> child) { }

            public void OnGainedChild(ITreeNode<T> child) { }

            public void OnChildrenResorted(ITreeNode<T> self) { }

            public void OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where) { }
            #endregion
        }
    }

    public class TreeNodeCollection<T> : IEnumerable<ITreeNode<T>>
    {

        private readonly ITreeNode<T> m_parent;
        private readonly List<ITreeNode<T>> m_children;

        public TreeNodeCollection(ITreeNode<T> parent)
        {
            m_parent = parent;
            m_children = new List<ITreeNode<T>>();
        }

        #region Mutating members.
        /// <summary>
        /// Adds the specified new child to this collection.
        /// </summary>
        /// <param name="newChild">The new child.</param>
        /// <param name="skipStructuralChecking">if set to <c>true</c> addition of this child will be perforemd without structural checking.</param>
        /// <returns>
        /// The TreeNode that resulted from this addition - either the node to be added, or its TreeNode wrapper.
        /// </returns>
        public ITreeNode<T> Add(T newChild, bool skipStructuralChecking = false)
        {

            // If necessary, create a TreeNode wrapper.
            ITreeNode<T> tn = newChild as ITreeNode<T> ?? new TreeNode<T>(newChild);

            return AddNode(tn, skipStructuralChecking);

        }


        public void Insert(int where, T treeNode)
        {
            // If necessary, create a TreeNode wrapper.
            ITreeNode<T> tn = treeNode as ITreeNode<T> ?? new TreeNode<T>(treeNode);
            m_children.Insert(where, tn);
        }
        
        public ITreeNode<T> AddNode(ITreeNode<T> tn, bool skipStructuralChecking = false)
        {
            if (m_parent != null && (!skipStructuralChecking && m_parent.IsChildOf(tn)))
            {
                throw new ArgumentException("Adding node " + tn.Payload + " as a child of " + m_parent.Payload + " would create a circular tree structure.");
            }
            if (skipStructuralChecking || !m_children.Contains(tn))
            {
#if PREANNOUNCE
                m_parent.MyEventController.OnAboutToGainChild(tn);
                tn.MyEventController.OnAboutToGainParent(m_parent);
#endif
                m_children.Add(tn);
                if (m_parent != null)
                {
                    m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.GainedNode, tn);
                    m_parent.MyEventController.OnGainedChild(tn);
                    tn.MyEventController.OnGainedParent(m_parent);

                    if (!Equals(tn.Parent, m_parent))
                    {
                        tn.SetParent(m_parent, skipStructuralChecking, /*childAlreadyAdded =*/ true);
                    }
                }
            }
            return tn;
        }

        /// <summary>
        /// Removes the specified existing child from this collection.
        /// </summary>
        /// <param name="existingChild">The existing child node to be removed.</param>
        /// <returns>True if the removal was successful, otherwise, false.</returns>
        public bool Remove(T existingChild)
        {
            return (from node in m_children where node.Payload.Equals(existingChild) select Remove(node)).FirstOrDefault();
        }

        /// <summary>
        /// Removes the specified existing child from this collection.
        /// </summary>
        /// <param name="existingChild">The existing child.</param>
        /// <returns>True if the removal was successful, otherwise, false.</returns>
        public bool Remove(ITreeNode<T> existingChild)
        {
#if PREANNOUNCE
            existingChild.MyEventController.OnAboutToLoseParent(m_parent);
            m_parent.MyEventController.OnAboutToLoseChild(existingChild);
#endif
            if (m_children.Remove(existingChild))
            {
                existingChild.Parent = null;
                m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.GainedNode, existingChild);
                m_parent.MyEventController.OnLostChild(existingChild);
                existingChild.MyEventController.OnLostParent(m_parent);
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        public ITreeNode<T> this[int index] => m_children[index];

        public int IndexOf(T treeNode)
        {
            // If necessary, create a TreeNode wrapper.
            ITreeNode<T> tn = treeNode as ITreeNode<T> ?? new TreeNode<T>(treeNode);

            return m_children.IndexOf(tn);
        }

        public bool Contains(T possibleChild)
        {
            ITreeNode<T> tn = possibleChild as ITreeNode<T> ?? new TreeNode<T>(possibleChild);
            return ContainsNode(tn);
        }

        public bool ContainsNode(ITreeNode<T> possibleChildNode)
        {
            return m_children.Contains(possibleChildNode);
        }

        /// <summary>
        /// Gets the count of entries in this TreeNodeCollection.
        /// </summary>
        /// <value>The count.</value>
        public int Count => m_children.Count;

        #region Sorting Handlers
        /// <summary>
        /// Sorts the specified list according to the provided comparison object.
        /// </summary>
        /// <param name="comparison">The comparison.</param>
        public void Sort(Comparison<ITreeNode<T>> comparison)
        {
            m_children.Sort(comparison);
            m_parent.MyEventController.OnChildrenResorted(m_parent);
            m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, m_parent);
        }

        /// <summary>
        /// Sorts the specified list according to the provided comparer implementation.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        public void Sort(IComparer<ITreeNode<T>> comparer)
        {
            m_children.Sort(comparer);
            m_parent.MyEventController.OnChildrenResorted(m_parent);
            m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, m_parent);
        }

        /// <summary>
        /// Sorts the specified list according to the provided comparer implementation.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <param name="comparer">The comparer.</param>
        public void Sort(int index, int count, IComparer<ITreeNode<T>> comparer)
        {
            m_children.Sort(index, count, comparer);
            m_parent.MyEventController.OnChildrenResorted(m_parent);
            m_parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, m_parent);
        }
        #endregion

        #region IEnumerable<ITreeNode<T>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<ITreeNode<T>> GetEnumerator()
        {
            return ((IEnumerable<ITreeNode<T>>)m_children).GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Foreaches the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void ForEach(Action<ITreeNode<T>> action)
        {
            m_children.ForEach(action);
        }

        /// <summary>
        /// Finds all children for which the predicate returns true.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        public List<ITreeNode<T>> FindAll(Predicate<ITreeNode<T>> match)
        {
            return m_children.FindAll(match);
        }

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_children.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Clears all children out of this collection.
        /// </summary>
        public void Clear()
        {
            List<ITreeNode<T>> children = new List<ITreeNode<T>>(m_children);
            children.ForEach(delegate (ITreeNode<T> child) { Remove(child); });

        }
    }
}