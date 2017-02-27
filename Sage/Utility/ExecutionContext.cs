/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Utility {

    public delegate void DictionaryChange(object key, object value);


    /// <summary>
    /// An ExecutionContext holds all of the information necessary to track one execution through a process structure. The 
    /// process structure governs structure, and the ExecutionContext governs process-instance-specific data.
    /// </summary>
    /// <seealso cref="ExecutionContext" />
    /// <seealso cref="IModelObject" />
    /// <seealso cref="IDictionary" />
    public class ExecutionContext : TreeNode<ExecutionContext>, IModelObject, IDictionary {

        #region Private Fields
        private readonly Hashtable m_dictionary;
        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="parent">The parent ExecutionContext.</param>
        public ExecutionContext(IModel model, string name, string description, Guid guid, ExecutionContext parent)
        {
            IsSelfReferential = true;
            m_dictionary = new Hashtable();
            InitializeIdentity(model, name, description, guid);
            // We skip structural checking, since we just created this node, so it cannot be a child or other
            // descendant of the parent.
            parent?.AddChild(this);
        }

        private void AddChild(ExecutionContext ec, TreeStructureCheckType checkType = TreeStructureCheckType.Deep) {
            base.AddChild(ec, checkType==TreeStructureCheckType.None);
            m_dictionary.Add(ec.Guid, ec);
        }

        #region Implementation of IModelObject

        private string m_name;
        private Guid m_guid;
        private IModel m_model;
        private string m_description;

        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model { [System.Diagnostics.DebuggerStepThrough] get { return m_model; } }

        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name { [System.Diagnostics.DebuggerStepThrough]get { return m_name; } }

        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
        public string Description => (m_description ?? ("No description for " + m_name));

        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid { [System.Diagnostics.DebuggerStepThrough] get { return m_guid; } }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The IModelObject's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        #endregion

        #region IDictionary Delegation
        #region IDictionary Members

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <param name="key">The <see cref="T:System.Object"></see> to use as the key of the element to add.</param>
        /// <param name="value">The <see cref="T:System.Object"></see> to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.IDictionary"></see> object. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IDictionary"></see> is read-only.-or- The <see cref="T:System.Collections.IDictionary"></see> has a fixed size. </exception>
        public void Add(object key, object value) {
            m_dictionary.Add(key, value);
            if (EntryAdded != null) {
                EntryAdded(key, value);
            }
        }

        /// <summary>
        /// Removes all elements from the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IDictionary"></see> object is read-only. </exception>
        [System.Diagnostics.DebuggerStepThrough]
        public void Clear() {
            // TODO: Clear other elements that are not in the dictionary.
            if (EntryRemoved != null) {
                foreach (object key in m_dictionary.Keys) {
                    object val = m_dictionary[key];
                    m_dictionary.Remove(key);
                    EntryRemoved(key, val);
                }
            } else {
                m_dictionary.Clear();
            }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IDictionary"></see> object contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.IDictionary"></see> object.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.IDictionary"></see> contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        [System.Diagnostics.DebuggerStepThrough]
        public bool Contains(object key) {
            return m_dictionary.Contains(key);
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IDictionaryEnumerator"></see> object for the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IDictionaryEnumerator"></see> object for the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </returns>
        [System.Diagnostics.DebuggerStepThrough]
        public IDictionaryEnumerator GetEnumerator() {
            return m_dictionary.GetEnumerator();
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary"></see> object has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IDictionary"></see> object has a fixed size; otherwise, false.</returns>
        public bool IsFixedSize {
            [System.Diagnostics.DebuggerStepThrough] get {
                return m_dictionary.IsFixedSize;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary"></see> object is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IDictionary"></see> object is read-only; otherwise, false.</returns>
        public bool IsReadOnly {
            [System.Diagnostics.DebuggerStepThrough] get {
                return m_dictionary.IsReadOnly;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"></see> object containing the keys of the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.ICollection"></see> object containing the keys of the <see cref="T:System.Collections.IDictionary"></see> object.</returns>
        public ICollection Keys {
            [System.Diagnostics.DebuggerStepThrough]
            get {
                return m_dictionary.Keys;
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IDictionary"></see> object is read-only.-or- The <see cref="T:System.Collections.IDictionary"></see> has a fixed size. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        [System.Diagnostics.DebuggerStepThrough]
        public void Remove(object key) {
            // TODO: Clear other elements that are not in the dictionary.
            if (EntryRemoved != null) {
                object val = m_dictionary[key];
                m_dictionary.Remove(key);
                EntryRemoved(key, val);
            } else {
                m_dictionary.Remove(key);
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"></see> object containing the values in the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.ICollection"></see> object containing the values in the <see cref="T:System.Collections.IDictionary"></see> object.</returns>
        public ICollection Values {
            [System.Diagnostics.DebuggerStepThrough]
            get {
                return m_dictionary.Values;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> with the specified key.
        /// </summary>
        /// <value></value>
        public object this[object key] {
            [System.Diagnostics.DebuggerStepThrough]
            get {
                return m_dictionary[key];
            }
            [System.Diagnostics.DebuggerStepThrough]
            set {
                if (EntryChanging != null) {
                    EntryChanging(key, m_dictionary[key]); // Only retrieve if it's needed.
                }
                m_dictionary[key] = value;
                if (EntryChanged == null) {
                    EntryChanged(key, value);
                }
            }
        }

        #endregion

        #region ICollection Members

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"></see> to an <see cref="T:System.Array"></see>, starting at a particular <see cref="T:System.Array"></see> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"></see> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"></see>. The <see cref="T:System.Array"></see> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">array is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">array is multidimensional.-or- index is equal to or greater than the length of array.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"></see> is greater than the available space from index to the end of the destination array. </exception>
        /// <exception cref="T:System.InvalidCastException">The type of the source <see cref="T:System.Collections.ICollection"></see> cannot be cast automatically to the type of the destination array. </exception>
        [System.Diagnostics.DebuggerStepThrough]
        public void CopyTo(Array array, int index) {
            m_dictionary.CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.ICollection"></see>.</returns>
        public int Count {
            [System.Diagnostics.DebuggerStepThrough]
            get { return m_dictionary.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe); otherwise, false.</returns>
        public bool IsSynchronized {
            [System.Diagnostics.DebuggerStepThrough]
            get { return m_dictionary.IsSynchronized; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.</returns>
        public object SyncRoot {
            [System.Diagnostics.DebuggerStepThrough]
            get { return m_dictionary.SyncRoot; }
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        [System.Diagnostics.DebuggerStepThrough]
        IEnumerator IEnumerable.GetEnumerator() {
            return m_dictionary.GetEnumerator();
        }

        #endregion
        #endregion

        public event DictionaryChange EntryAdded;
        public event DictionaryChange EntryRemoved;
        public event DictionaryChange EntryChanging;
        public event DictionaryChange EntryChanged;


        public object FindUp(string key) {
            if (Contains(key)) {
                return this[key];
            } else
            {
                return Parent?.Payload.FindUp(key);
            }
        }
    }
}
