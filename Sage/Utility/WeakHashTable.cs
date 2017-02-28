/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// A hashtable whose entries are weak - that is, if the underlying object
    /// is discarded from the runtime, so is the entry in the hashtable.
    /// </summary>
    public class WeakHashtable : IDictionary {

        private readonly Hashtable m_ht;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakHashtable"/> class.
        /// </summary>
        public WeakHashtable() {
            m_ht = new Hashtable();
        }

        #region IDictionary Members

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary"></see> object is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IDictionary"></see> object is read-only; otherwise, false.</returns>
        public bool IsReadOnly => m_ht.IsReadOnly;

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IDictionaryEnumerator"></see> object for the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IDictionaryEnumerator"></see> object for the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </returns>
        public IDictionaryEnumerator GetEnumerator() {
            return new WeakHashtableEnumerator(m_ht.GetEnumerator());
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> with the specified key.
        /// </summary>
        /// <value></value>
        public object this[object key] {
            get {
                if (!m_ht.Contains(key))
                    return null;
                WeakReference wr = (WeakReference)m_ht[key];
                if (wr == null)
                    return null;
                if (!wr.IsAlive)
                    m_ht.Remove(key);
                return wr.Target;
            }
            set {
                m_ht[key] = new WeakReference(value);
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IDictionary"></see> object is read-only.-or- The <see cref="T:System.Collections.IDictionary"></see> has a fixed size. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        public void Remove(object key) {
            m_ht.Remove(key);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IDictionary"></see> object contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.IDictionary"></see> object.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.IDictionary"></see> contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        public bool Contains(object key) {
            return m_ht.Contains(key);
        }

        /// <summary>
        /// Removes all elements from the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IDictionary"></see> object is read-only. </exception>
        public void Clear() {
            m_ht.Clear();
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"></see> object containing the values in the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.ICollection"></see> object containing the values in the <see cref="T:System.Collections.IDictionary"></see> object.</returns>
        public ICollection Values {
            get {
                int i = 0;
                ArrayList al = new ArrayList(m_ht.Values.Count);
                foreach (WeakReference wr in m_ht.Values) {
                    if (wr.IsAlive) {
                        al.Add(wr.Target);
                    } else {
                        i++;
                    }
                }
                if (i > ( 0.25 * m_ht.Count ))
                    Clean();

                return al;
            }
        }

        /// <summary>
        /// Cleans this instance by removing all entries for which the Weak Link has been broken..
        /// </summary>
        public void Clean() {
            ArrayList deadKeys = new ArrayList();
            foreach (DictionaryEntry de in m_ht) {
                if (!( (WeakReference)de.Value ).IsAlive)
                    deadKeys.Add(de.Key);
            }
            foreach (object deadKey in deadKeys)
                m_ht.Remove(deadKey);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <param name="key">The <see cref="T:System.Object"></see> to use as the key of the element to add.</param>
        /// <param name="value">The <see cref="T:System.Object"></see> to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.IDictionary"></see> object. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IDictionary"></see> is read-only.-or- The <see cref="T:System.Collections.IDictionary"></see> has a fixed size. </exception>
        public void Add(object key, object value) {
            m_ht.Add(key, new WeakReference(value));
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"></see> object containing the keys of the <see cref="T:System.Collections.IDictionary"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.ICollection"></see> object containing the keys of the <see cref="T:System.Collections.IDictionary"></see> object.</returns>
        public ICollection Keys => m_ht.Keys;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary"></see> object has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IDictionary"></see> object has a fixed size; otherwise, false.</returns>
        public bool IsFixedSize => m_ht.IsFixedSize;

        #endregion

        #region ICollection Members

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe); otherwise, false.</returns>
        public bool IsSynchronized => m_ht.IsSynchronized;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.ICollection"></see>.</returns>
        public int Count => m_ht.Count;

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"></see> to an <see cref="T:System.Array"></see>, starting at a particular <see cref="T:System.Array"></see> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"></see> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"></see>. The <see cref="T:System.Array"></see> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">array is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">array is multidimensional.-or- index is equal to or greater than the length of array.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"></see> is greater than the available space from index to the end of the destination array. </exception>
        /// <exception cref="T:System.InvalidCastException">The type of the source <see cref="T:System.Collections.ICollection"></see> cannot be cast automatically to the type of the destination array. </exception>
        public void CopyTo(Array array, int index) {
            m_ht.CopyTo(array, index);
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.</returns>
        public object SyncRoot => m_ht.SyncRoot;

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return new WeakHashtableEnumerator(m_ht.GetEnumerator());
        }

        #endregion

        private class WeakHashtableEnumerator : IDictionaryEnumerator {
            private readonly IEnumerator m_enum;

            public WeakHashtableEnumerator(IEnumerator weakHashtableEnum) {
                m_enum = weakHashtableEnum;
            }

            #region IEnumerator Members

            public void Reset() {
                m_enum.Reset();
            }

            public object Current {
                get {
                    while (true) {
                        DictionaryEntry de = (DictionaryEntry)m_enum.Current;
                        if (!( (WeakReference)de.Value ).IsAlive) {
                            if (!MoveNext())
                                return null;
                        } else {
                            return new DictionaryEntry(de.Key, ( (WeakReference)de.Value ).Target);
                        }
                    }
                }
            }

            public bool MoveNext() {
                do {
                    if (!m_enum.MoveNext())
                        return false;
                } while (!( (WeakReference)( (DictionaryEntry)m_enum.Current ).Value ).IsAlive);
                return true;
            }

            #endregion

            #region IDictionaryEnumerator Members

            public object Key => ( (DictionaryEntry)Current ).Key;

            public object Value => ( (DictionaryEntry)Current ).Value;

            public DictionaryEntry Entry => (DictionaryEntry)Current;

            #endregion
        }

    }
}