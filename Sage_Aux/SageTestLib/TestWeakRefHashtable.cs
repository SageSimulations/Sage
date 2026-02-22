/* This source code licensed under the GNU Affero General Public License */

using System.Diagnostics;

namespace Highpoint.Sage.Utility {
	using System;
		using System.Collections;
	using NUnit.Framework;

	/// <summary>
	/// Summary description for zTestChemistry.
	/// </summary>
	[TestFixture]
	public class WeakReferenceHashtableTester {
		public WeakReferenceHashtableTester() {}

		[SetUp] 
		public void Init() {
		}
		[TearDown]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}

		[Test]
		public void TestWRHTBasics(){
			ArrayList keepers = new ArrayList();
			WeakHashtable wht = new WeakHashtable();
			for ( int i = 0 ; i < 15 ; i++ ) {
				string s = "Object " + i; 
				wht.Add(i,s);
				if ( i%3==0 ) keepers.Add(s);
			}
			Console.WriteLine("The following are in the persistent array...");
			foreach ( string s in keepers ) Console.WriteLine(s);
			Console.WriteLine("The following are in the WR Hashtable...");
//			foreach ( string s in wht.Values ) Console.WriteLine(s);
			foreach ( DictionaryEntry de in wht ) {
				Console.WriteLine(de.Key + ", " + de.Value ) ;
			}
			System.GC.Collect(4);
			Console.WriteLine("Doing GC - now let's see what remains...");
			foreach ( DictionaryEntry de in wht ) {
				Console.WriteLine(de.Key + ", " + de.Value ) ;
			}
//			foreach ( string s in wht.Values ) Console.WriteLine(s);
		}
	}
}
