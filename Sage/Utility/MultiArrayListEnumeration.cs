/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Utility
{
	public class MultiArrayListEnumerable : IEnumerable {
		private readonly ArrayList[] m_arraylists;

		public MultiArrayListEnumerable(ArrayList[] arrayLists){
			m_arraylists = arrayLists;
		}
#region IEnumerable Members

		public IEnumerator GetEnumerator() {
			return new MultiArrayListEnumerator(m_arraylists);
		}

#endregion

	}

	/// <summary>
	/// Summary description for MultiArrayListEnumerator.
	/// </summary>
	public class MultiArrayListEnumerator : IEnumerator
	{
		private readonly ArrayList[] m_arrayLists;
		private int m_alCursor;
		private IEnumerator m_enumerator;

		public MultiArrayListEnumerator(ArrayList[] arrayLists)
		{
			m_arrayLists = arrayLists;
			Reset();
		}

#region Implementation of IEnumerator

		public void Reset() {
			m_alCursor = -1;
			GetNextEnumerator();
		}

		public bool MoveNext() {
			if ( m_enumerator != null ) {
				if ( !m_enumerator.MoveNext() ) {
					m_enumerator = GetNextEnumerator();
					if ( m_enumerator == null ) return false;
					return m_enumerator.MoveNext();
				} else {
					return true;
				}
			}
			return false;
		}
		public object Current => m_enumerator?.Current;

#endregion

		private IEnumerator GetNextEnumerator(){
			if ( m_arrayLists.Length > (m_alCursor+1) ) {
				m_alCursor++;
				m_enumerator = m_arrayLists[m_alCursor].GetEnumerator();
			} else {
				m_enumerator = null;
				m_alCursor = -1;
			}
			return m_enumerator;
		}
	}
}
