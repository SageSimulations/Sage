/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Utility {
	/// <summary>
	/// Implemeted by an object that is sortable, and has SortKeys
	/// </summary>
	public interface IHasSortKeys {
        /// <summary>
        /// Gets the sort keys for this object.
        /// </summary>
        /// <value>The sort keys.</value>
		ISortKey[] SortKeys { get; } 
	}

    /// <summary>
    /// A Sort Key is an object that can be used to sort a collection of objects, and can be presented to a user for selection.
    /// </summary>
	public interface ISortKey : IComparer {
        /// <summary>
        /// Gets the human-readable name of this Sort Key.
        /// </summary>
        /// <value>The name.</value>
		string Name { get; }
	}
}
