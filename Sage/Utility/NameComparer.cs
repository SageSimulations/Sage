/* This source code licensed under the GNU Affero General Public License */
using System.Collections.Generic;

namespace Highpoint.Sage.Utility {
    public class NameComparer<T> : IComparer<T> where T : SimCore.IHasName {
        #region IComparer<T> Members
        /// <summary>
        /// Compares two objects and returns a value indicating whether the name of one is less than, equal to, or greater than the name of the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />.Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.</returns>
        public int Compare(T x, T y) {
            return System.Collections.Comparer.Default.Compare(x.Name, y.Name);
        }
        #endregion
    }
}
