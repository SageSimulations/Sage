/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// Summary description for zTestInterpolations.
    /// </summary>
    [TestClass]
    public class MultiArrayListEnumerationTester {
		private ArrayList m_al1, m_al2, m_al3, m_ale;
		private static string m_expected123 = "AlphaBravoCharleyDeltaEchoFoxtrotGolfHotelIndia";
        public MultiArrayListEnumerationTester() {
			Init();
			m_al1 = new ArrayList();
			m_al1.Add("Alpha");m_al1.Add("Bravo");m_al1.Add("Charley");
			m_al2 = new ArrayList();
			m_al2.Add("Delta");m_al2.Add("Echo");m_al2.Add("Foxtrot");
			m_al3 = new ArrayList();
			m_al3.Add("Golf");m_al3.Add("Hotel");m_al3.Add("India");
			m_ale = new ArrayList();
		}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		
		/// <summary>
		/// Basic test.
		/// </summary>
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Simple test to aggregate three non-empty arraylists under one enumerator.")]
		public void TestBasicsOfEnumerator(){
			MultiArrayListEnumerable male = new MultiArrayListEnumerable(new ArrayList[]{m_al1,m_al2,m_al3});
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach ( string s in male ) sb.Append(s);

			string result = sb.ToString();
			Console.WriteLine("Simple three list aggregation - " + result + ".");
            _Debug.Assert(result.Equals(m_expected123),"MultiArrayListEnumerable basics","Failed test");
		}
		/// <summary>
		/// Basic test.
		/// </summary>
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Simple test to aggregate three non-empty arraylists and one empty one in various locations under one enumerator.")]
		public void TestEnumeratorWithEmptyArrays(){
			Validate(new ArrayList[]{m_ale,m_al1,m_al2,m_al3},m_expected123,"Leading","Empty Arraylist at leading element of arraylists.");
			Validate(new ArrayList[]{m_al1,m_ale,m_al2,m_al3},m_expected123,"Internal","Empty Arraylist at internal element of arraylists.");
			Validate(new ArrayList[]{m_al1,m_al2,m_al3,m_ale},m_expected123,"Trailing","Empty Arraylist at trailing element of arraylists.");
		}

		private void Validate(ArrayList[] arraylists, string expected, string name, string description){
			MultiArrayListEnumerable male = new MultiArrayListEnumerable(new ArrayList[]{m_al1,m_al2,m_al3});
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach ( string s in male ) sb.Append(s);
			string result = sb.ToString();
			Console.WriteLine(name + "\r\n\texpected = \"" + expected + "\",\r\n\tresult   = \"" + result + "\".\r\n\t\t" + (result.Equals(expected)?"Passed.\r\n":"Failed.\r\n"));
            _Debug.Assert(result.Equals(expected),"MultiArrayListEnumerable basics","Failed test");
		}
	}
}
