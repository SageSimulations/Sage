/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility
{
	// A binary heap can be efficiently implemented as an array, where a node at index i 
	// has children at indexes 2i and 2i+1 and a parent at index i/2, with one-based indexing.
	public class Heap {
		public enum HEAP_RULE { MinValue=-1, MaxValue=1 }
		private static int _defaultInitialCapacity = 5;
		private static int _defaultGrowthFactor = 4;
		private int m_nEntries;
		private readonly int m_direction;
		private readonly int m_growthFactor;
		private IComparable m_parentEntry;
		private int m_entryArraySize;
		private IComparable[] m_entryArray;

		public Heap(HEAP_RULE direction, int initialCapacity, int growthFactor){
			m_direction = (int)direction;
			m_nEntries = 0;
			m_growthFactor = growthFactor;
			m_parentEntry = null;
			m_entryArraySize = initialCapacity;
			m_entryArray = new IComparable[m_entryArraySize+1];
		}

		public Heap(HEAP_RULE direction, int initialCapacity):this(direction,initialCapacity,_defaultGrowthFactor){}

		public Heap(HEAP_RULE direction):this(direction,_defaultInitialCapacity,_defaultGrowthFactor){}

		public void Enqueue(IComparable newEntry){
			int ndx = 1;
			if ( m_nEntries == m_entryArraySize ) GrowArray();
			if ( m_nEntries++ > 0 ) {
				ndx = m_nEntries;
				int parentNdx = ndx/2;
				m_parentEntry = m_entryArray[parentNdx];
				while ( parentNdx > 0 && newEntry.CompareTo(m_parentEntry) == m_direction ) {
					m_entryArray[ndx] = m_parentEntry;
					ndx = parentNdx;
					parentNdx /= 2;
					m_parentEntry = m_entryArray[parentNdx];
				}
			}
			m_entryArray[ndx] = newEntry;
		}

		public int Count => m_nEntries;

	    public IComparable Peek() { return m_entryArray[1]; }

		public IComparable Dequeue(){
			if ( m_nEntries == 0 ) return null;
			IComparable leastEntry = m_entryArray[1];
			IComparable relocatee  = m_entryArray[m_nEntries];
			m_nEntries--;
			int ndx = 1;
			int child = 2;
			while ( child <= m_nEntries ) {
				if ( (child < m_nEntries) && m_entryArray[child].CompareTo(m_entryArray[child+1]) == (-m_direction)) child++;
				// m_entryArray[child] is the (e.g. in a minTree) lesser of the two children.
				// Therefore, if m_entryArray[child] is greater than relocatee, put Relocatee
				// in at ndx, and we're done. Otherwise, swap and drill down some more.
				if ( m_entryArray[child].CompareTo(relocatee) == (-m_direction) ) break;
				m_entryArray[ndx] = m_entryArray[child];
				ndx = child;
				child *= 2;
			}

			m_entryArray[ndx] = relocatee;

			return leastEntry;
		}

		public override string ToString() {
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for ( int i = 1 ; i <= m_nEntries ; i++ ) {
				string pt = (m_entryArray[i] == null)?"<null>":m_entryArray[i].ToString();
				string lc = "<empty>"; if ( (i*2)<=m_nEntries ) lc = m_entryArray[i*2].ToString();
				string rc = "<empty>"; if ( ((i*2)+1)<=m_nEntries ) rc = m_entryArray[(i*2)+1].ToString();
				bool ok = (lc=="<empty>" || String.Compare(pt, lc, StringComparison.Ordinal) == m_direction) && (rc=="<empty>" || String.Compare(pt, rc, StringComparison.Ordinal) == m_direction);
				//if ( !ok ) System.Diagnostics.Debugger.Break();
				sb.Append("(" + i + ") " + pt + " : left Child is " + lc + ", right child is " + rc + "." + (ok?"OK\r\n":"NOT_OK\r\n"));
			}
			return sb.ToString();
		}


		private void GrowArray(){
			IComparable[] tmp = m_entryArray;
			m_entryArraySize*=m_growthFactor;
			m_entryArray = new IComparable[m_entryArraySize+1];
			Array.Copy(tmp,m_entryArray,m_nEntries+1);
		}

		internal void Dump(){
			for ( int i = 1 ; i <= m_nEntries ; i++ ) {
				string pt = (m_entryArray[i] == null)?"<null>":m_entryArray[i].ToString();
				string lc = "<empty>"; if ( (i*2)<=m_nEntries ) lc = m_entryArray[i*2].ToString();
				string rc = "<empty>"; if ( ((i*2)+1)<=m_nEntries ) rc = m_entryArray[(i*2)+1].ToString();
				bool ok = (lc=="<empty>" || String.Compare(pt, lc, StringComparison.Ordinal) == m_direction) && (rc=="<empty>" || String.Compare(pt, rc, StringComparison.Ordinal) == m_direction);
				//if ( !ok ) System.Diagnostics.Debugger.Break();
				Console.WriteLine("(" + i + ") " + pt + " : left Child is " + lc + ", right child is " + rc + ". " + (ok?"OK":"NOT_OK"));
			}
		}
	}

    /// <summary>
    /// A binary heap can be efficiently implemented as an array, where a node at index i 
    /// has children at indexes 2i and 2i+1 and a parent at index i/2, with one-based indexing.
    /// </summary>
    /// <typeparam name="T">The type of things held in the heap.</typeparam>
    public class Heap<T> {

        /// <summary>
        /// Enum HEAP_RULE - MinValue builds a heap with the 
        /// </summary>
        public enum HEAP_RULE { MinValue = -1, MaxValue = 1 }

        #region Private variables.
        private static readonly int _defaultInitialCapacity = 5;
        private static readonly int _defaultGrowthFactor = 4;
        private readonly int m_direction;
        private readonly int m_growthFactor;
        private T m_parentEntry;
        private int m_entryArraySize;
        private T[] m_entryArray;
        private readonly IComparer<T> m_comparer;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap{T}"/> class.
        /// </summary>
        /// <param name="direction">The heap rule to be used for heap maintenance.</param>
        /// <param name="initialCapacity">The initial capacity.</param>
        /// <param name="growthFactor">The growth factor.</param>
        /// <param name="comparer">The comparer.</param>
        public Heap(HEAP_RULE direction, int initialCapacity, int growthFactor, IComparer<T> comparer) {
            m_direction = (int)direction;
            Count = 0;
            m_growthFactor = growthFactor;
            m_parentEntry = default(T);
            m_entryArraySize = initialCapacity;
            m_entryArray = new T[m_entryArraySize + 1];
            m_comparer = comparer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap{T}"/> class.
        /// </summary>
        /// <param name="direction">The heap rule to be used for heap maintenance.</param>
        /// <param name="initialCapacity">The initial capacity.</param>
        /// <param name="comparer">The comparer.</param>
        public Heap(HEAP_RULE direction, int initialCapacity, IComparer<T> comparer) : this(direction, initialCapacity, _defaultGrowthFactor, comparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap{T}"/> class.
        /// </summary>
        /// <param name="direction">The heap rule to be used for heap maintenance.</param>
        /// <param name="comparer">The comparer.</param>
        public Heap(HEAP_RULE direction, IComparer<T> comparer) : this(direction, _defaultInitialCapacity, _defaultGrowthFactor, comparer) { }

        /// <summary>
        /// Enqueues the specified new entry.
        /// </summary>
        /// <param name="newEntry">The new entry.</param>
        public void Enqueue(T newEntry) {
            int ndx = 1;
            if (Count == m_entryArraySize) GrowArray();
            if (Count++ > 0) {
                ndx = Count;
                int parentNdx = ndx / 2;
                m_parentEntry = m_entryArray[parentNdx];
                while (parentNdx > 0 && m_comparer.Compare(newEntry,m_parentEntry) == m_direction) {
                    m_entryArray[ndx] = m_parentEntry;
                    ndx = parentNdx;
                    parentNdx /= 2;
                    m_parentEntry = m_entryArray[parentNdx];
                }
            }
            m_entryArray[ndx] = newEntry;
        }

        /// <summary>
        /// Gets the count of elements in the heap.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; private set; }

        /// <summary>
        /// Peeks at the instance at the top of the heap.
        /// </summary>
        /// <returns>T.</returns>
        public T Peek() { return m_entryArray[1]; }

        /// <summary>
        /// Dequeues the instance at the top of the heap.
        /// </summary>
        /// <returns>T.</returns>
        public T Dequeue() {
            if (Count == 0) return default(T);
            T leastEntry = m_entryArray[1];
            T relocatee = m_entryArray[Count];
            Count--;
            int ndx = 1;
            int child = 2;
            while (child <= Count) {
                if ((child < Count) && m_comparer.Compare(m_entryArray[child],m_entryArray[child + 1]) == (-m_direction)) child++;
                // m_entryArray[child] is the (e.g. in a minTree) lesser of the two children.
                // Therefore, if m_entryArray[child] is greater than relocatee, put Relocatee
                // in at ndx, and we're done. Otherwise, swap and drill down some more.
                if (m_comparer.Compare(m_entryArray[child], relocatee) == (-m_direction)) break;
                m_entryArray[ndx] = m_entryArray[child];
                ndx = child;
                child *= 2;
            }

            m_entryArray[ndx] = relocatee;

            return leastEntry;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 1; i <= Count; i++) {
                string pt = (m_entryArray[i] == null) ? "<null>" : m_entryArray[i].ToString();
                string lc = "<empty>"; if ((i * 2) <= Count) lc = m_entryArray[i * 2].ToString();
                string rc = "<empty>"; if (((i * 2) + 1) <= Count) rc = m_entryArray[(i * 2) + 1].ToString();
                bool ok = (lc == "<empty>" || string.Compare(pt, lc, StringComparison.Ordinal) == m_direction) && (rc == "<empty>" || string.Compare(pt, rc, StringComparison.Ordinal) == m_direction);
                //if ( !ok ) System.Diagnostics.Debugger.Break();
                sb.Append("(" + i + ") " + pt + " : left Child is " + lc + ", right child is " + rc + "." + (ok ? "OK\r\n" : "NOT_OK\r\n"));
            }
            return sb.ToString();
        }

        internal void Dump() {
            for (int i = 1; i <= Count; i++) {
                string String = (m_entryArray[i] == null) ? "<null>" : m_entryArray[i].ToString();
                string lc = "<empty>"; if ((i * 2) <= Count) lc = m_entryArray[i * 2].ToString();
                string rc = "<empty>"; if (((i * 2) + 1) <= Count) rc = m_entryArray[(i * 2) + 1].ToString();
                bool ok = (lc == "<empty>" || string.Compare(String, lc, StringComparison.Ordinal) == m_direction) && (rc == "<empty>" || string.Compare(String, rc, StringComparison.Ordinal) == m_direction);
                //if ( !ok ) System.Diagnostics.Debugger.Break();
                Console.WriteLine("(" + i + ") " + String + " : left Child is " + lc + ", right child is " + rc + ". " + (ok ? "OK" : "NOT_OK"));
            }
        }

        private void GrowArray() {
            T[] tmp = m_entryArray;
            m_entryArraySize *= m_growthFactor;
            m_entryArray = new T[m_entryArraySize + 1];
            Array.Copy(tmp, m_entryArray, Count + 1);
        }

    }


}
