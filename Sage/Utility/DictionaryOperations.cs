/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using System.Text;

namespace Highpoint.Sage.Utility {
	/// <summary>
	/// A class of helper Operations focused on Dictionaries. This is an old class, kept for backward compatibility.
	/// </summary>
	public class DictionaryOperations {

        /// <summary>
        /// Comparer that can be used to sort Dictionary Entries by their keys' order using the System.Collections.Comparer.Default comparer.
        /// </summary>
        public class DictionaryEntryByKeySorter : IComparer {

            #region IComparer Members

            /// <summary>
            /// Compares two DictionaryEntry objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
            public int Compare(object x, object y) {
                return Comparer.Default.Compare(((DictionaryEntry)x).Key,((DictionaryEntry)y).Key);
            }

            #endregion
        }

		/// <summary>
		/// Returns a string containing the contents of a dictionary.
		/// </summary>
		/// <param name="name">The name to be given to this dictionary in the output.</param>
		/// <param name="dict">The dictionary to dump.</param>
		/// <returns>A string containing the contents of the specified dictionary.</returns>
		public static string DumpDictionary(string name, IDictionary dict){
			StringBuilder sb = new StringBuilder();
			sb.Append("\r\n");
			DumpDictionary(name,dict,0,ref sb);
			return sb.ToString();
		}

		private static void DumpDictionary(string name, IDictionary dict, int indent, ref StringBuilder sb){
			AddTabs(ref sb,indent);
			sb.Append(name);

			if ( dict == null ) { 
				sb.Append("\t<null>\r\n"); 
				return; 
			} 

			sb.Append("\t(Dictionary)\r\n");
			foreach ( DictionaryEntry de in dict ) {
				if ( de.Value is IDictionary ) {
					DumpDictionary((string)de.Key,(IDictionary)de.Value,indent+1,ref sb);
					//sb.Append("\r\n");
				} else if ( de.Value is Mementos.IMemento ) {
					DumpDictionary((string)de.Key,((Mementos.IMemento)de.Value).GetDictionary(),indent+1,ref sb);
					//sb.Append("\r\n");
				} else {
					AddTabs(ref sb,indent);
					sb.Append("\t" + de.Key + "\t" + (de.Value==null?"<null>":de.Value.ToString()));
					sb.Append("\r\n");
				}
			}
		}

		private static void AddTabs(ref StringBuilder sb, int tabDepth){
			for ( int i = 0 ; i < tabDepth ; i++ ) sb.Append("\t");
		}
	}
}
