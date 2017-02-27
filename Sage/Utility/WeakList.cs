/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Utility {

	/// <summary>
	/// A list of objects that are held behind weak references. The list is not collapsed until explicitly told to do so.
	/// </summary>
    public class WeakList : IList {

        #region Private Fields

        private ArrayList m_list;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:WeakList"/> class.
        /// </summary>
		public WeakList(){
			m_list = new ArrayList();
		}

        /// <summary>
        /// Collapses this instance, removing weak reference objects for which the targets have been garbage collected.
        /// </summary>
		public void Collapse(){
			ArrayList tmp = new ArrayList();
			foreach ( MyWeakReference wr in m_list ) if ( wr.Target != null ) tmp.Add(wr);
			m_list = tmp;
		}
		
		#region IList Members
        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList"></see> is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IList"></see> is read-only; otherwise, false.</returns>
		public bool IsReadOnly => false;

	    /// <summary>
        /// Gets or sets the <see cref="T:Object"/> at the specified index.
        /// </summary>
        /// <value></value>
		public object this[int index] {
			get {
				MyWeakReference wr = (MyWeakReference)m_list[index];
				return wr.Target;
			}
			set {
				MyWeakReference wr = new MyWeakReference(value);
				m_list[index] = wr;
			}
		}

        /// <summary>
        /// Removes the <see cref="T:System.Collections.IList"></see> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.IList"></see>. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
		public void RemoveAt(int index) {
			m_list.RemoveAt(index);
		}

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.IList"></see> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="value">The <see cref="T:System.Object"></see> to insert into the <see cref="T:System.Collections.IList"></see>.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.IList"></see>. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
        /// <exception cref="T:System.NullReferenceException">value is null reference in the <see cref="T:System.Collections.IList"></see>.</exception>
		public void Insert(int index, object value) {
			m_list.Insert(index,new WeakReference(value));
		}

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList"></see>.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object"></see> to remove from the <see cref="T:System.Collections.IList"></see>.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
		public void Remove(object value) {
			m_list.Remove(value);
		}

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IList"></see> contains a specific value.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object"></see> to locate in the <see cref="T:System.Collections.IList"></see>.</param>
        /// <returns>
        /// true if the <see cref="T:System.Object"></see> is found in the <see cref="T:System.Collections.IList"></see>; otherwise, false.
        /// </returns>
		public bool Contains(object value) {
			return m_list.Contains(value);
		}

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.IList"></see>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only. </exception>
		public void Clear() {
			m_list.Clear();
		}

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList"></see>.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object"></see> to locate in the <see cref="T:System.Collections.IList"></see>.</param>
        /// <returns>
        /// The index of value if found in the list; otherwise, -1.
        /// </returns>
		public int IndexOf(object value) {
			return m_list.IndexOf(value);
		}

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.IList"></see>.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object"></see> to add to the <see cref="T:System.Collections.IList"></see>.</param>
        /// <returns>
        /// The position into which the new element was inserted.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
		public int Add(object value) {
			return m_list.Add(new MyWeakReference(value));
		}

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList"></see> has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IList"></see> has a fixed size; otherwise, false.</returns>
		public bool IsFixedSize => false;

	    #endregion

		#region ICollection Members

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe); otherwise, false.</returns>
		public bool IsSynchronized => m_list.IsSynchronized;

	    /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.ICollection"></see>.</returns>
		public int Count => m_list.Count;

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
			for ( int i = 0 ; i < m_list.Count ; i++ ) array.SetValue(((MyWeakReference)m_list[i]).Target,new long[]{i});
		}

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.</returns>
		public object SyncRoot => m_list.SyncRoot;

	    #endregion

		#region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
		public IEnumerator GetEnumerator() {
			return new WeakListEnumerator(m_list);
		}

		#endregion
	}

	
	internal class MyWeakReference : WeakReference {
		public MyWeakReference(object obj):base(obj){}

		public override int GetHashCode() {
			return Target.GetHashCode ();
		}
		public override bool Equals(object obj) {
			return Target.Equals (obj);
		}
	}

	
	internal class WeakListEnumerator : IEnumerator {
		private readonly IList m_list;
		private int m_cursor;
		public WeakListEnumerator(IList list){
			m_list = list;
			m_cursor = -1;
		}
		#region IEnumerator Members

		public void Reset() {
			m_cursor = -1;
		}

		public object Current {
			get {
				if ( m_cursor == -1 ) throw new ApplicationException("Called Current on an enumerator without first having called MoveNext.");
				return ((MyWeakReference)m_list[m_cursor]).Target; }
		}

		public bool MoveNext() {
			if ( m_cursor == (m_list.Count-1) ) return false;
			m_cursor++;
			return true;
		}

		#endregion
	}
}