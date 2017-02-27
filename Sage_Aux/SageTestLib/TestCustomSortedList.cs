/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.CF {
#if COMPACT_FRAMEWORK
	/// <summary>
	/// Summary description for zTestCustomSortedList.
	/// </summary>
	[TestClass]
	public class zTestCustomSortedList {
		public zTestCustomSortedList(){}

		[TestMethod]public void TestSortedList(){
			SortedList list = new SortedList();
			list.Add(3,"Horse");
			list.Add(2,"Killers");
			list.Add(4,"Abacus");

			foreach ( object key in list.Keys ) Trace.WriteLine(key);
			foreach ( object value in list.Values ) Trace.WriteLine(value);
		}
	}
#endif
}
