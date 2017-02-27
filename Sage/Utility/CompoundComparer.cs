/* This source code licensed under the GNU Affero General Public License */

using Trace = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.Utility {

	/// <summary>
	/// Provided with an array of Comparers, this comparer produces a comarison value
	/// that allows a list to be sorted on first one key (defined by the first element
	/// in the array), then by another key (defined by the second element in the array),
	/// and so on. There is no limit to the number of comparers that may be provided.
	/// </summary>
	public class CompoundComparer : IComparer {
	    readonly IComparer[] m_comparers;
		/// <summary>
		/// Creates a compound comparer that sorts on the keys whose comparers are
		/// provided in the array that is passed to the constructor.
		/// </summary>
		/// <param name="comparers">A param list of IComparers that will be used, in
		/// the provided order, to sort the elements passed to this IComparer.</param>
		public CompoundComparer(params IComparer[] comparers){
			m_comparers = comparers;
		}

		#region IComparer Members
		/// <summary>
		/// Compares two objects according to the rules embodied in the comparers around
		/// which this CompoundComparer was created.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns></returns>
		public int Compare(object x, object y) {
		    // ReSharper disable once LoopCanBeConvertedToQuery (for clarity.)
		    foreach (IComparer comparer in  m_comparers)
		    {
				int rslt = comparer.Compare(x,y);
				if ( rslt != 0 ) return rslt;		        
			}
			return 0;
		}
		#endregion
	}
}
