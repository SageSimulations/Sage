/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility {
	[TestClass]
	public class HashtableOfListsTester {

		public HashtableOfListsTester(){Init();}
        
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the HashtableOfLists.")]
        public void TestHTOL() {

            HashtableOfLists htol = new HashtableOfLists();

            htol.Add("Dog", "Collie");
            htol.Add("Pig", "Pot-bellied");
            htol.Add("Horse", "Arabian");
            htol.Add("Horse", "Clydesdale");
            htol.Add("Dog", "Chihuahua");

            Console.WriteLine("Test before removal...");
            foreach (string str in htol)
                Console.WriteLine(str);

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            foreach (string str in htol)
                Console.WriteLine(str);

        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the HashtableOfLists.")]
        public void TestHTOLTemplate() {

            HashtableOfLists<string, string> htol = new HashtableOfLists<string, string>();

            htol.Add("Dog", "Collie");
            htol.Add("Pig", "Pot-bellied");
            htol.Add("Horse", "Arabian");
            htol.Add("Horse", "Clydesdale");
            htol.Add("Dog", "Chihuahua");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol) {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys) {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol) {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys) {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }
        }
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the HashtableOfLists.")]
        public void TestSortedHTOLTemplate() {

            IComparer<String> strComp = Comparer<string>.Default;
            HashtableOfLists<string, string> htol = new HashtableOfLists<string, string>(strComp);

            htol.Add("Dog", "Collie");
            htol.Add("Pig", "Pot-bellied");
            htol.Add("Horse", "Clydesdale");
            htol.Add("Horse", "Arabian");
            htol.Add("Dog", "Chihuahua");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol) {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys) {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol) {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys) {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }
        }
    }
}