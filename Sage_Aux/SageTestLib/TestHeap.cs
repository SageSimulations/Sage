/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Utility;

namespace SageTestLib {
	[TestClass]
	public class HeapTester {

		public HeapTester(){Init();}
        
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		
		string[] testTimes = new string[]{"9/1/1998 12:00:00 AM",
											 "9/1/1998 12:21:18 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:00:00 AM",
											 "9/1/1998 12:21:18 AM",
											 "9/1/1998 7:00:00 AM",
											 "9/1/1998 12:21:18 AM",
											 "9/1/1998 8:00:00 AM",
											 "9/1/1998 12:21:18 AM",
											 "9/1/1998 8:00:00 AM",
											 "9/1/1998 12:21:18 AM",
											 "9/1/1998 8:00:00 AM",
											 "9/1/1998 8:00:00 AM",
											 "9/1/1998 8:00:00 AM",
											 "9/1/1998 8:00:00 AM",
											 "9/1/1998 8:00:00 AM",
											 "9/1/1998 12:00:00 PM",
											 "9/1/1998 12:00:00 PM",
											 "9/1/1998 12:00:00 PM",
											 "9/1/1998 12:27:31 AM",
											 "9/1/1998 12:42:36 AM",
											 "9/1/1998 12:27:29 AM",
											 "9/1/1998 12:42:36 AM",
											 "9/1/1998 12:27:26 AM",
											 "9/1/1998 12:42:36 AM",
											 "9/1/1998 12:27:24 AM",
											 "9/1/1998 12:42:36 AM",
											 "9/1/1998 12:27:23 AM",
											 "9/1/1998 12:42:36 AM",
											 "9/1/1998 3:48:41 AM",
											 "9/1/1998 3:48:42 AM",
											 "9/1/1998 3:48:44 AM",
											 "9/1/1998 3:48:47 AM",
											 "9/1/1998 3:48:49 AM",
											 "9/1/1998 12:48:58 AM",
											 "9/1/1998 1:03:54 AM",
											 "9/1/1998 12:48:56 AM",
											 "9/1/1998 1:03:54 AM",
											 "9/1/1998 12:45:20 AM",
											 "9/1/1998 1:06:52 AM",
											 "9/1/1998 1:06:39 AM"};

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Test the Heap collection.")]
		public void RecreateFailure(){
			Heap heap = new Heap(Heap.HEAP_RULE.MinValue);
			foreach ( string s in testTimes ) {
				DateTime dt = DateTime.Parse(s);
				heap.Enqueue(dt);
			}

			IComparable ho = heap.Dequeue();
			do { 
				Console.WriteLine(ho.ToString());
				ho = heap.Dequeue();
			} while ( ho != null );
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Test the Heap collection.")]
		public void TestHeap(){

			int nTests = 500;

			for ( int testNum = 0 ; testNum < nTests ; testNum++ ) {
				
				int nEntries = m_random.Next(37)+13;
				Heap.HEAP_RULE direction = m_random.NextDouble()<0.5?Heap.HEAP_RULE.MinValue:Heap.HEAP_RULE.MaxValue;
				Console.WriteLine("Test # " + testNum + ": Enqueueing & dequeueing " + nEntries + " entries using " + direction.ToString() + ".");
				Heap heap = new Heap(direction);
				
				for ( int i = 0 ; i < nEntries ; i++ ) {
					string rs = RandomString(testNum%15);
					heap.Enqueue(rs);
					//heap.Dump();
				}

				//Console.ReadLine();

				IComparable lastValRead = heap.Dequeue();
				while ( heap.Count > 0 ) {
					IComparable thisValRead = heap.Dequeue();
					int comparisonVal = lastValRead.CompareTo(thisValRead);
                    _Debug.Assert(((comparisonVal == ((int)direction)) || ( comparisonVal == 0 ) ),"Heap Test","Heap test failed.");
					if ( testNum < 10 ) Console.WriteLine("Dequeueing " + lastValRead);
					lastValRead = thisValRead;
				}

				if ( testNum < 10 ) Console.WriteLine("----------------------------------------------------------------------");
			}
		}


		private static string m_letters = "abcdefghijklmnopqrstuvwxyz";
		private static Random m_random = new Random(12);

		private static string RandomString(int nChars){
			System.Text.StringBuilder sb = new System.Text.StringBuilder(nChars);
			for ( int i = 0 ; i < nChars ; i++ ) sb.Append(m_letters[m_random.Next(m_letters.Length)]);
			return sb.ToString();
		}
	}
}