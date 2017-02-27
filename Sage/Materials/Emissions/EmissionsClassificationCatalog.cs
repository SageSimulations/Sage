/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
	/// <summary>
	/// Summary description for EmissionsClassificationCatalog.
	/// </summary>
	public class EmissionsClassificationCatalog : IDictionary {
		private readonly Hashtable m_ht;

        /// <summary>
        /// A dictionary of <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/>s keyed against their names.
        /// </summary>
		public EmissionsClassificationCatalog(){
			m_ht = new Hashtable();
		}

		#region IDictionary Members

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object is read-only; otherwise, false.</returns>
		public bool IsReadOnly => m_ht.IsReadOnly;

	    /// <summary>
        /// Returns an <see cref="T:System.Collections.IDictionaryEnumerator"></see> object for the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IDictionaryEnumerator"></see> object for the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.
        /// </returns>
		public IDictionaryEnumerator GetEnumerator() { return m_ht.GetEnumerator(); }

        /// <summary>
        /// Gets or sets the <see cref="T:Object"/> with the specified key.
        /// </summary>
        /// <value></value>
		public object this[object key] {
			get { return m_ht[key]; }
			set { m_ht[key] = value; }
		}

        /// <summary>
        /// Removes the <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> with the specified key from the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.
        /// </summary>
        /// <param name="key">The key of the <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> to remove.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object is read-only.-or- The <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> has a fixed size. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
		public void Remove(object key) { m_ht.Remove(key); }

        /// <summary>
        /// Determines whether the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object contains an <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.</param>
        /// <returns>
        /// true if the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> contains an <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> with the key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
		public bool Contains(object key) { return m_ht.Contains(key); }

        /// <summary>
        /// Removes all elements from the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object is read-only. </exception>
		public void Clear() { m_ht.Clear(); }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"></see> object containing the values in the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.ICollection"></see> object containing the values in the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.</returns>
		public ICollection Values => m_ht.Values;

	    /// <summary>
        /// Adds an <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> with the provided key and value to the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.
        /// </summary>
        /// <param name="key">The <see cref="T:System.Object"></see> to use as the key of the <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> to add.</param>
        /// <param name="value">The <see cref="T:System.Object"></see> to use as the value of the <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> to add.</param>
        /// <exception cref="T:System.ArgumentException">An <see cref="Highpoint.Sage.Materials.Emissions.EmissionsClassification"/> with the same key already exists in the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> is read-only.-or- The <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> has a fixed size. </exception>
		public void Add(object key, object value) { m_ht.Add(key,value); }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"></see> object containing the keys of the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.ICollection"></see> object containing the keys of the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object.</returns>
		public ICollection Keys => m_ht.Keys;

	    /// <summary>
        /// Gets a value indicating whether the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:Highpoint.Sage.Materials.Emissions.EmissionsClassificationCatalog"></see> object has a fixed size; otherwise, false.</returns>
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
		public void CopyTo(Array array, int index) { m_ht.CopyTo(array,index); }

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
			return m_ht.GetEnumerator();
		}

		#endregion
	}
}
