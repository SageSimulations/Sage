/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Highpoint.Sage.Utility {

	/// <summary>
	/// Manages a hashtable of lists. This is useful for maintaining collections
	/// of keyed entries where the keys are duplicated across multiple entries.
	/// </summary>
    public class HashtableOfLists : IEnumerable {
        private readonly Hashtable m_ht;
		private static readonly ArrayList s_empty_List = ArrayList.ReadOnly(new ArrayList());

		/// <summary>
		/// Creates a hashtable of lists.
		/// </summary>
        public HashtableOfLists(){
            m_ht = new Hashtable();
        }

		/// <summary>
		/// Adds an element with the specified key and value into the Hashtable of Lists.
		/// </summary>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="item">The value of the element to add.</param>
        public void Add(object key, object item){
            object obj = m_ht[key];
            if ( obj == null ) {
                m_ht.Add(key,item);
            } else if ( obj is ListWrapper ) {
				ListWrapper lw = (ListWrapper)obj;
				if ( !lw.List.Contains(item) ) lw.List.Add(item);
            } else {
				if ( !obj.Equals(item) ) { // If we're re-adding the same thing, skip it.
					m_ht.Remove(key);
					ListWrapper lw = new ListWrapper();
					lw.List.Add(obj);
					lw.List.Add(item);
					m_ht.Add(key,lw);
				}
            }
        }

		/// <summary>
		/// Removes the element with the specified key from the Hashtableof Lists.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		/// <param name="item">The value of the element to remove.</param>
		public void Remove(object key, object item){
			object obj = m_ht[key];
			if ( obj != null )
			{
			    ListWrapper wrapper = obj as ListWrapper;
			    if ( wrapper != null ) {
					wrapper.List.Remove(item);
				} else if ( obj.Equals(item) ) {
					m_ht.Remove(key);
				}
			}
		}

		/// <summary>
		/// Removes all elements from the Hashtable of Lists.
		/// </summary>
        public void Clear(){
            m_ht.Clear();
        }

		/// <summary>
		/// Removes all elements with the specified key from the Hashtable of Lists.
		/// </summary>
        public void Remove(object key){
            m_ht.Remove(key);
        }

		/// <summary>
		/// Retrieves a list of items associated with the provided key.
		/// </summary>
		public IList this[object key]{
			get {
				object obj = m_ht[key];
			    ListWrapper wrapper = obj as ListWrapper;
			    if ( wrapper != null ) return wrapper.List;
			    ArrayList al = obj != null ? new ArrayList {obj} : s_empty_List;
				return al;
			}
		}

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection Keys => m_ht.Keys;

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection Values { 
			get {
				ArrayList retval = new ArrayList();
				foreach ( object key in m_ht.Keys ) {
					retval.AddRange(this[key]);
				}
				return retval;
			}
		}

        /// <summary>
        /// Determines whether this hashtable of lists contains the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if this hashtable of lists contains the specified key; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(object key){
			return m_ht.ContainsKey(key);
		}

		/// <summary>
		/// Returns an Enumerator that can iterate through all of the entries in the 
		/// Hashtable of Lists. The enumerator is an enumerator of values, not
		/// DictionaryEntries. This method first prunes empty lists from the hashtable.
		/// </summary>
		/// <returns>An Enumerator that can iterate through all of the entries in the 
		/// Hashtable of Lists.</returns>
        public IEnumerator GetEnumerator(){
			PruneEmptyLists();
            return new HtolEnumerator(this);
        }

		/// <summary>
		/// Removes any entries in the HTOL that comprise a key and an empty list (which can
		/// result from removals of entries.)
		/// </summary>
		public void PruneEmptyLists(){
			ArrayList removees = new ArrayList();
			foreach ( DictionaryEntry de in m_ht )
			{
			    ListWrapper value = de.Value as ListWrapper;
			    if ( value != null && value.List.Count == 0 ) removees.Add(de.Key);
			}
		    foreach ( object key in removees ) m_ht.Remove(key);
		}

		/// <summary>
		/// Returns the number of entries in the Hashtable of Lists.
		/// </summary>
        public long Count {
            get {
                int count = 0;
                foreach ( DictionaryEntry de in m_ht )
                {
                    ListWrapper value = de.Value as ListWrapper;
                    if ( value != null ) count += value.List.Count;
                    else count++;
                }
                return count;
            }
        }

        private class ListWrapper {
            public ListWrapper(){
                List = new ArrayList();
            }
            public ArrayList List { get; }
        }

        private class HtolEnumerator : IEnumerator {
            private readonly HashtableOfLists m_htol;
            private IEnumerator m_htEnum, m_lstEnum;
            public HtolEnumerator(HashtableOfLists htol){
                m_htol = htol;
                Reset();
            }
            #region Implementation of IEnumerator
            public void Reset() {
                m_htEnum = m_htol.m_ht.GetEnumerator();
                m_lstEnum = null;
            }
            public bool MoveNext() {
                if ( m_htEnum == null ) return false;
                if ( m_lstEnum != null ) {
                    if ( m_lstEnum.MoveNext() ) return true;
                    m_lstEnum = null;
                }

				while ( m_htEnum.MoveNext() ) {
					object obj = ((DictionaryEntry)m_htEnum.Current).Value;

					// Find the first non-listWrapper object, or non-empty listWrapper.
				    ListWrapper wrapper = obj as ListWrapper;
				    if (wrapper == null) continue;
				    if ( wrapper.List.Count == 0 ) continue;
					
					// Now that we've found it,  handle it.
				    m_lstEnum = wrapper.List.GetEnumerator();
				    m_lstEnum.MoveNext(); // We know it's non-empty, so this must succeed.
				    return true;
				}

				m_htEnum = null;
				return false;
            }
            public object Current => m_lstEnum != null ? m_lstEnum.Current : ((DictionaryEntry?) m_htEnum?.Current)?.Value;

            #endregion
        }
    }


    /// <summary>
    /// Manages a hashtable of lists of values. The keys are of type TKey, and the lists contain elements of type TValue.
    /// This is useful for maintaining collections of keyed entries where the keys are duplicated across multiple entries.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso>
    ///     <cref>System.Collections.Generic.IDictionary{TKey, System.Collections.Generic.List{TValue}}</cref>
    /// </seealso>
    /// <seealso cref="System.Collections.Generic.IEnumerable{TValue}" />
    /// <seealso>
    ///   <cref>System.Collections.Generic.IDictionary{TKey, List{TValue}}</cref>
    /// </seealso>
    public class HashtableOfLists<TKey, TValue> : IEnumerable<TValue>, IDictionary<TKey, List<TValue>> {

        #region Private Fields
        private readonly Dictionary<TKey, List<TValue>> m_dictOfLists;
        private readonly IComparer<TValue> m_comparer;
        #endregion

        #region Constructors
        public HashtableOfLists()
        {
            m_dictOfLists = new Dictionary<TKey, List<TValue>>();
        }

        public HashtableOfLists(IComparer<TValue> comparer)
            : this()
        {
            m_comparer = comparer;
        }  
        #endregion

        /// <summary>
        /// Adds an element with the specified key and value into the Hashtable of Lists.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="item">The value of the element to add.</param>
        public void Add(TKey key, TValue item) {
            if (!m_dictOfLists.ContainsKey(key)) {
                m_dictOfLists.Add(key, new List<TValue>());
            }
            m_dictOfLists[key].Add(item);
            if (m_comparer != null) {
                m_dictOfLists[key].Sort(m_comparer);
            }
        }

        /// <summary>
        /// Removes all of the elements with the specified key from the Hashtableof Lists.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        public void Remove(TKey key) {
            m_dictOfLists.Remove(key);
        }

        /// <summary>
        /// Removes all elements with the specified key from the Hashtable of Lists.
        /// </summary>
        public bool Remove(TKey key, TValue item) {
            return m_dictOfLists[key].Remove(item);
        }

        /// <summary>
        /// Removes all elements from the Hashtable of Lists.
        /// </summary>
        public void Clear() {
            m_dictOfLists.Clear();
        }

        /// <summary>
        /// Retrieves a list of items associated with the provided key.
        /// </summary>
        public List<TValue> this[TKey key] {
            get {
                return m_dictOfLists[key];
            }
            set
            {
                m_dictOfLists[key] = value;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The keys.</value>
        public Dictionary<TKey,List<TValue>>.KeyCollection Keys => m_dictOfLists.Keys;

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The values.</value>
        public Dictionary<TKey, List<TValue>>.ValueCollection Values => m_dictOfLists.Values;

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains a list element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains a list element with the key; otherwise, false.</returns>
        public bool ContainsKey(TKey key) { return m_dictOfLists.ContainsKey(key); }

        /// <summary>
        /// Returns an Enumerator that can iterate through all of the entries in the 
        /// Hashtable of Lists. The enumerator is an enumerator of values, not
        /// DictionaryEntries. This method first prunes empty lists from the hashtable.
        /// </summary>
        /// <returns>An Enumerator that can iterate through all of the entries in the 
        /// Hashtable of Lists.</returns>
        public IEnumerator GetEnumerator() {
            PruneEmptyLists();
            return new HtolEnumerator<TKey,TValue>(this);
        }

        /// <summary>
        /// Removes any entries in the HTOL that comprise a key and an empty list (which can
        /// result from removals of entries.)
        /// </summary>
        public void PruneEmptyLists() {

            List<TKey> keys = new List<TKey>(m_dictOfLists.Keys);

            foreach (TKey keyVal in keys) {
                if (m_dictOfLists[keyVal].Count == 0) {
                    m_dictOfLists.Remove(keyVal);
                }
            }
        }

        /// <summary>
        /// Returns the number of entries in the Hashtable of Lists.
        /// </summary>
        public long Count {
            get
            {
                return m_dictOfLists.Values.Sum(valList => valList.Count);
            }
        }

        private class HtolEnumerator<TTKey,TTValue> : IEnumerator<TTValue> {
            private readonly HashtableOfLists<TTKey, TTValue> m_htol;
            private IEnumerator<List<TTValue>> m_allListEnumerator;
            private IEnumerator<TTValue> m_currListEnumerator;
            public HtolEnumerator(HashtableOfLists<TTKey, TTValue> htol) {
                m_htol = htol;
                Reset();
            }
            #region Implementation of IEnumerator
            public void Reset() {
                m_allListEnumerator = m_htol.Values.GetEnumerator();
                m_currListEnumerator = null;
            }

            public bool MoveNext() {
                if (m_currListEnumerator == null) {
                    if (m_allListEnumerator.MoveNext()) {
                        m_currListEnumerator = m_allListEnumerator.Current.GetEnumerator();
                    } else {
                        return false;
                    }
                    return MoveNext();
                } else {
                    if (m_currListEnumerator.MoveNext()) {
                        return true;
                    } else {
                        m_currListEnumerator.Dispose();
                        m_currListEnumerator = null;
                        return MoveNext();
                    }
                }
            }

            public object Current => m_currListEnumerator.Current;

            #endregion

            #region IEnumerator<_TValue> Members

            TTValue IEnumerator<TTValue>.Current => m_currListEnumerator.Current;

            #endregion

            #region IDisposable Members

            public void Dispose() {
                if (m_currListEnumerator != null) {
                    m_currListEnumerator.Dispose();
                    m_allListEnumerator.Dispose();
                }
            }

            #endregion
        }

        #region IEnumerable<TValue> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
            return new HtolEnumerator<TKey, TValue>(this);
        }

        #endregion

        #region IDictionary<TKey,List<TValue>> Members

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public void Add(TKey key, List<TValue> value)
        {
            value.ForEach(delegate(TValue tv) {
                Add(key, tv);
            });
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The keys.</value>
        ICollection<TKey> IDictionary<TKey, List<TValue>>.Keys => m_dictOfLists.Keys;

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        bool IDictionary<TKey, List<TValue>>.Remove(TKey key)
        {
            return m_dictOfLists.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out List<TValue> value)
        {
            return m_dictOfLists.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The values.</value>
        ICollection<List<TValue>> IDictionary<TKey, List<TValue>>.Values => m_dictOfLists.Values;

        #endregion

        #region ICollection<KeyValuePair<TKey,List<TValue>>> Members

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public void Add(KeyValuePair<TKey, List<TValue>> item)
        {
            item.Value.ForEach(delegate(TValue tv) { Add(item.Key, tv); });
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
        public bool Contains(KeyValuePair<TKey, List<TValue>> item)
        {
            return m_dictOfLists.ContainsKey(item.Key) && m_dictOfLists[item.Key].Equals(item.Value);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        int ICollection<KeyValuePair<TKey, List<TValue>>>.Count => m_dictOfLists.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly => false;

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Remove(KeyValuePair<TKey, List<TValue>> item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,List<TValue>>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<TKey, List<TValue>>> IEnumerable<KeyValuePair<TKey, List<TValue>>>.GetEnumerator()
        {
            return m_dictOfLists.GetEnumerator();
        }

        #endregion
    }
}