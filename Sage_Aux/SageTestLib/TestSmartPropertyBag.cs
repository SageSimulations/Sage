/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Diagnostics;


namespace Highpoint.Sage.SimCore  {

    [TestClass]
    public class SmartPropertyBagTester {

        private Random m_random = new Random();

        public SmartPropertyBagTester(){Init();}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
		
		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("Checks the base functionality of Name/Value, Name/String, Name/SPB, and set a value in a referenced SPB")]
		public void TestSubsidiaries(){
			SmartPropertyBag animals = new SmartPropertyBag();
			animals.AddValue("Horses",12);
			animals.AddString("Kingdom","Animal");
			SmartPropertyBag dogs = new SmartPropertyBag();
			animals.AddChildSPB("Dogs",dogs);

			dogs.AddValue("Collies",14);
			dogs.AddValue("Chows",16);

			Debug.WriteLine("Animals.Horses = " + animals["Horses"]);
			Debug.WriteLine("Animals.Kingdom = " + animals["Kingdom"]);
			Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

			animals["Dogs.Collies"] = 18;
			Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

			SmartPropertyBag labs = new SmartPropertyBag();
			labs.AddValue("Black",4);
			labs.AddValue("Brown",2);
			labs.AddValue("Yellow",1);
			labs.AddString("Temperament","Mellow");
			labs.AddBoolean("Faithful",false);
			dogs.AddChildSPB("Labs",labs);
			Debug.WriteLine(DiagnosticAids.DumpDictionary("",(IDictionary)animals.Memento.GetDictionary()));

            Assert.IsTrue((double)animals["Dogs.Labs.Black"] == 4,"Black Lab is not 4");
            Assert.IsTrue((double)animals["Dogs.Labs.Brown"] == 2,"Brown Lab is not 2");
            Assert.IsTrue((double)animals["Dogs.Labs.Yellow"] == 1,"Yellow Lab is not 1");
            Assert.IsTrue("Mellow".Equals((string)labs["Temperament"]),"The Labs temperament is not Mellow");
            Assert.IsTrue((double)animals["Dogs.Collies"] == 18,"Collies are not 18");
            Assert.IsTrue((double)animals["Dogs.Chows"] == 16,"Chows are not 16");
            Assert.IsTrue((double)animals["Horses"] == 12,"Horses are not 12");
            Assert.IsTrue("Animal".Equals((string)animals["Kingdom"]),"The Kingdom is not the Animal Kingdom");
            Assert.IsTrue(!(bool)labs["Faithful"],"Labs are not not faithful");


			Debug.WriteLine("Setting Animals.Dogs.Labs.Black to 19, Animals.Dogs.Labs.Temperament to \"Lovable\".");
			Debug.WriteLine("...and Animals.Dogs.Labs.Faithful to \"true\".");
			animals["Dogs.Labs.Black"] = 19;
			animals["Dogs.Labs.Temperament"] = "Lovable";
			animals["Dogs.Labs.Faithful"] = true;
			Debug.WriteLine(DiagnosticAids.DumpDictionary("",(IDictionary)animals.Memento.GetDictionary()));

            Assert.IsTrue((double)animals["Dogs.Labs.Black"] == 19,"Black Lab is not 19");
            Assert.IsTrue((double)animals["Dogs.Labs.Brown"] == 2,"Brown Lab is not 2");
            Assert.IsTrue((double)animals["Dogs.Labs.Yellow"] == 1,"Yellow Lab is not 1");
            Assert.IsTrue("Lovable".Equals((string)labs["Temperament"]),"The Labs temperament is not Lovable");
            Assert.IsTrue((double)animals["Dogs.Collies"] == 18,"Collies are not 18");
            Assert.IsTrue((double)animals["Dogs.Chows"] == 16,"Chows are not 16");
            Assert.IsTrue((double)animals["Horses"] == 12,"Horses are not 12");
            Assert.IsTrue("Animal".Equals((string)animals["Kingdom"]),"The Kingdom is the Animal Kingdom");
            Assert.IsTrue((bool)labs["Faithful"],"Labs are faithful");
		}

		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("Checks the base functionality of Name/Value, Name/String, Name/SPB, and set a value in a referenced SPB")]
		public void TestRepeatedSnapshottingAndRestoration(){
			SmartPropertyBag animals = new SmartPropertyBag();
			animals.AddValue("Horses",12);
			animals.AddString("Kingdom","Animal");
			SmartPropertyBag dogs = new SmartPropertyBag();
			animals.AddChildSPB("Dogs",dogs);

			dogs.AddValue("Collies",14);
			dogs.AddValue("Chows",16);

			Debug.WriteLine("Animals.Horses = " + animals["Horses"]);
			Debug.WriteLine("Animals.Kingdom = " + animals["Kingdom"]);
			Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

			animals["Dogs.Collies"] = 18;
			Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

			SmartPropertyBag labs = new SmartPropertyBag();
			labs.AddValue("Black",4);
			labs.AddValue("Brown",2);
			labs.AddValue("Yellow",1);
			labs.AddString("Temperament","Mellow");
			labs.AddBoolean("Faithful",true);
			dogs.AddChildSPB("Labs",labs);

			Highpoint.Sage.Utility.Mementos.IMemento mem = dogs.Memento;
			DateTime start = DateTime.Now;
			for ( int i = 0 ; i < 10000 ; i++ ) {

				dogs.Memento = mem;

				labs["Faithful"] = false;
				labs["Faithful"] = true;

                Assert.IsTrue((double)animals["Dogs.Labs.Black"] == 4,"Black Lab is not 4");
                Assert.IsTrue((double)animals["Dogs.Labs.Brown"] == 2,"Brown Lab is not 2");
                Assert.IsTrue((double)animals["Dogs.Labs.Yellow"] == 1,"Yellow Lab is not 1");
                Assert.IsTrue("Mellow".Equals((string)labs["Temperament"]),"The Lab's temperament is not Mellow");
                Assert.IsTrue((double)animals["Dogs.Collies"] == 18,"Collies are not 18");
                Assert.IsTrue((double)animals["Dogs.Chows"] == 16,"Chows are not 16");
                Assert.IsTrue((double)animals["Horses"] == 12,"Horses are not 12");
                Assert.IsTrue("Animal".Equals((string)animals["Kingdom"]),"The Kingdom is not the Animal Kingdom");
                Assert.IsTrue((bool)labs["Faithful"],"Labs are not faithful");

				//Debug.WriteLine(DiagnosticAids.DumpDictionary("",(IDictionary)animals.Memento.GetDictionary()));
				mem = dogs.Memento;
				labs["Faithful"] = false;
				labs["Faithful"] = true;
				if ( (i%100)==0 ) {
					Debug.WriteLine(((TimeSpan)(DateTime.Now-start)).TotalSeconds);
					start = DateTime.Now;
					//if ( i > 7500 ) System.Diagnostics.Debugger.Break();
				}
			}
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks the functionality of the memento concept")]
		public void TestMementoCaching(){
			m_steve = 12;
			SmartPropertyBag spb1 = new SmartPropertyBag();
			spb1.AddValue("Fred",12);
			spb1.AddExpression("Bill","Math.Max(Fred,13)+15",new string[]{"Fred"});
			spb1.AddDelegate("Steve",new SmartPropertyBag.SPBDoubleDelegate(ComputeSteve));
			spb1.AddString("Name","SPB1");
            
			Highpoint.Sage.Utility.Mementos.IMemento mem1 = spb1.Memento;
			Debug.WriteLine(DiagnosticAids.DumpDictionary("mem1",mem1.GetDictionary()));
			Highpoint.Sage.Utility.Mementos.IMemento mem2 = spb1.Memento;
			Debug.WriteLine(DiagnosticAids.DumpDictionary("mem2",mem2.GetDictionary()));
			Debug.WriteLine("The new snapshot is " + (mem1==mem2?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem2 == mem1,"Memento 1 is not equal Memento 2");

            Assert.IsTrue((double)spb1["Bill"] == 28,"Bill is not 28");
            Assert.IsTrue((double)spb1["Steve"] == 12,"Steve is not 12");
            Assert.IsTrue("SPB1".Equals(spb1["Name"]),"Name is not SPB1");

			Debug.WriteLine("Changing \"Fred\" to 14 from 12.");
			spb1["Fred"]=14;
			Highpoint.Sage.Utility.Mementos.IMemento mem3 = spb1.Memento;
			Debug.WriteLine(DiagnosticAids.DumpDictionary("mem3",mem3.GetDictionary()));
			Debug.WriteLine("The new snapshot is " + (mem2==mem3?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem2!=mem3,"Memento 2 is not not equal to Memento 3");
            Assert.IsTrue((double)spb1["Fred"] == 14,"Fred is not 14");

			Debug.WriteLine("Changing \"Name\" to \"spb1\" from \"SPB1\".");
			spb1["Name"]="spb1";
			Highpoint.Sage.Utility.Mementos.IMemento mem3a = spb1.Memento;
			Debug.WriteLine(DiagnosticAids.DumpDictionary("mem3a",mem3a.GetDictionary()));
			Debug.WriteLine("The new snapshot is " + (mem2==mem3a?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem2!=mem3a,"Memento 2 is equal to memento 3a");

            Assert.IsTrue((double)spb1["Bill"] == 29,"Bill is not 29");	// changing Fred to 14 on line 120 changes Bill too

			spb1.AddValue("_Connie",99);

			SmartPropertyBag spb2 = new SmartPropertyBag();
			spb2.AddValue("_Pete",14);
			spb1.AddAlias("Pete",spb2,"_Pete");
			spb1.AddExpression("Marvin","Pete+Bill",new string[]{"Pete","Bill"});
            Assert.IsTrue((double)spb1["Marvin"] == 43,"Marvin is not 43");


			Highpoint.Sage.Utility.Mementos.IMemento mem4 = spb1.Memento;
			Highpoint.Sage.Utility.Mementos.IMemento mem5 = spb1.Memento;
			Debug.WriteLine("The new snapshot is " + (mem4==mem5?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem5 == mem4,"Memento 5 is not equal to memento 4");

			Debug.WriteLine("\r\nChanging a value in a foreign alias.");
			spb2["_Pete"]=16;
            Assert.IsTrue((double)spb1["Marvin"] == 45,"Marvin is not 45");

			Highpoint.Sage.Utility.Mementos.IMemento mem6 = spb1.Memento;
			Debug.WriteLine("The new snapshot is " + (mem5==mem6?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem5!=mem6,"Memento is equal to memento 6");

			Debug.WriteLine("Capturing another snapshot without change.");
			Highpoint.Sage.Utility.Mementos.IMemento mem7 = spb1.Memento;
			Debug.WriteLine("The new snapshot is " + (mem6==mem7?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem6 == mem7,"Memento 6 is not equal to memento 7");
            
			Debug.WriteLine("Changing the data driving a delegate-computed value.");
			m_steve = 14;

			Highpoint.Sage.Utility.Mementos.IMemento mem8 = spb1.Memento;
			Debug.WriteLine("The new snapshot is " + (mem8==mem7?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem7!=mem8,"Memento 7 is equal to memento 8");

			Debug.WriteLine("\r\nWe will now test snapshotting's functionality as it pertains to subsidiaries.");

			SmartPropertyBag spb1_1 = new SmartPropertyBag();
			spb1.AddChildSPB("Sub",spb1_1);
			spb1_1.AddValue("marine",12);

			Highpoint.Sage.Utility.Mementos.IMemento mem9 = spb1.Memento;
			Debug.WriteLine(DiagnosticAids.DumpDictionary("Main",mem9.GetDictionary()));
			Debug.WriteLine("Taking another immediately-following snapshot.");
			Highpoint.Sage.Utility.Mementos.IMemento mem10 = spb1.Memento;
			Debug.WriteLine("The new snapshot is " + (mem9==mem10?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem10 == mem9,"Memento 10 is not equal to memento 9");


			Debug.WriteLine("Changing a data member in a subsidiary.");
			spb1_1["marine"] = 21;
			Highpoint.Sage.Utility.Mementos.IMemento mem11 = spb1.Memento;
			Debug.WriteLine("The new snapshot is " + (mem10==mem11?"":"not ") + "equal to the one preceding it.\r\n");
            Assert.IsTrue(mem11!=mem10,"Memento 11 is equal to memento 10");

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks that a smart property bag can copy information through simply copying the memento from one instance to the other")]
		public void TestMementoRestorationAndEquality(){
            SmartPropertyBag spb1 = new SmartPropertyBag();
            spb1.AddValue("Fred",12);
            spb1.AddExpression("Bill","Math.Max(Fred,17)",new string[]{"Fred"});
            spb1.AddDelegate("Steve",new SmartPropertyBag.SPBDoubleDelegate(ComputeSteve));
            spb1.AddString("Donkey","Kong");
            spb1.AddBoolean("HabaneroHot",true);
            
            Highpoint.Sage.Utility.Mementos.IMemento mem1 = spb1.Memento;

            SmartPropertyBag spb2 = new SmartPropertyBag();
            spb2.Memento = mem1;

            Assert.IsTrue(spb1!=spb2,"Object spb1 and spb2 cannot be the same instance");
            Assert.IsTrue(spb1.Equals(spb2),"SPB1 and SPB2 should be equal");
            Assert.IsTrue(spb1.Memento==spb2.Memento,"SPB1.Memento and SPB2.Memento should be equal");

            Debug.WriteLine(DiagnosticAids.DumpDictionary("Before",spb1.Memento.GetDictionary()));
            Debug.WriteLine(DiagnosticAids.DumpDictionary("After",spb2.Memento.GetDictionary()));

        }

        [TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("Checks managing values, strings, booleans, and aliases over more levels.")]
		public void TestStringsAndBooleans(){
            SmartPropertyBag spb1 = new SmartPropertyBag();
            spb1.AddString("Name","Habanero");
            spb1.AddBoolean("Hot",true);
            spb1.AddValue("Scovilles",35000);

            SmartPropertyBag spb2 = new SmartPropertyBag();
            spb2.AddString("Name","Jalapeno");
            spb2.AddBoolean("Hot",false);
            spb2.AddValue("Scovilles",22000);
            spb2.AddAlias("OtherGuysName",spb1,"Name");
            spb2.AddAlias("OtherGuysHot",spb1,"Hot");
            spb2.AddAlias("OtherGuysScovilles",spb1,"Scovilles");

            Debug.WriteLine(spb2["OtherGuysHot"]);
            spb1["Hot"] = false;
            Debug.WriteLine(spb2["OtherGuysHot"]);
            Assert.IsTrue(!(bool)spb2["OtherGuysHot"],"OtherGuysHot is not false");


			spb1.AddAlias("OtherGuysName",spb2,"Name");
            spb1.AddAlias("OtherGuysHot",spb2,"Hot");
            spb1.AddAlias("OtherGuysScovilles",spb2,"Scovilles");


            SmartPropertyBag spb0 = new SmartPropertyBag();
            spb0.AddChildSPB("Jalapeno",spb2);
            spb0.AddChildSPB("Habanero",spb1);
            
            Highpoint.Sage.Utility.Mementos.IMemento mem1 = spb0.Memento;

            spb1["Hot"] = false;
            spb1["Name"] = "Habañero";
            spb1["Scovilles"] = 32000;

            spb2["Hot"] = true;
            spb2["Name"] = "Jalapeño";
            spb2["Scovilles"] = 16000;

            Assert.IsTrue(!(bool)spb0["Habanero.Hot"],"Habanero.Hot is hot");
            Assert.IsTrue("Habañero".Equals((string)spb0["Habanero.Name"]),"Habanero.Name is not Habañero");
            Assert.IsTrue((double)spb0["Habanero.Scovilles"] == 32000,"Habanero.Scovilles is not 32000");

            Assert.IsTrue((bool)spb0["Jalapeno.Hot"],"Jalapeno.Hot is not hot");
            Assert.IsTrue("Jalapeño".Equals((string)spb0["Jalapeno.Name"]),"Jalapeno.Name is not Jalapeño");
            Assert.IsTrue((double)spb0["Jalapeno.Scovilles"] == 16000,"Jalapeno.Scovilles is not 16000");

			Highpoint.Sage.Utility.Mementos.IMemento mem2 = spb0.Memento;

            Debug.WriteLine(DiagnosticAids.DumpDictionary("Before",mem1.GetDictionary()));
            Debug.WriteLine(DiagnosticAids.DumpDictionary("After",mem2.GetDictionary()));

            spb0["Jalapeno.Hot"] = false;
            spb0["Habanero.Hot"] = true;
            spb0["Jalapeno.Scovilles"] = 12000;
            spb0["Habanero.Scovilles"] = 29000;
            
            Highpoint.Sage.Utility.Mementos.IMemento mem3 = spb0.Memento;

            Assert.IsTrue((bool)spb0["Habanero.Hot"],"Habanero.Hot is not hot");
            Assert.IsTrue((double)spb0["Habanero.Scovilles"] == 29000,"Habanero.Scovilles is not 29000");

            Assert.IsTrue(!(bool)spb0["Jalapeno.Hot"],"Jalapeno.Hot is not hot");
            Assert.IsTrue((double)spb0["Jalapeno.Scovilles"] == 12000,"Jalapeno.Scovilles is not 12000");

			Debug.WriteLine(DiagnosticAids.DumpDictionary("After more...",mem3.GetDictionary()));

		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks that a smart property bag implements IEnumerable interface correctly")]
        public void TestEnumerationAndIsLeaf() {
            SmartPropertyBag animals = new SmartPropertyBag();
            animals.AddValue("Horses", 12);
            animals.AddString("Kingdom", "Animal");
            SmartPropertyBag dogs = new SmartPropertyBag();
            animals.AddChildSPB("Dogs", dogs);

            dogs.AddValue("Collies", 14);
            dogs.AddValue("Chows", 16);

            animals["Dogs.Collies"] = 18;

            SmartPropertyBag labs = new SmartPropertyBag();
            labs.AddValue("Black", 4);
            labs.AddValue("Brown", 2);
            labs.AddValue("Yellow", 1);
            labs.AddString("Temperament", "Mellow");
            labs.AddBoolean("Faithful", false);
            dogs.AddChildSPB("Labs", labs);

            DumpEnumerable(animals, 0);

            Assert.IsTrue(true, "Visual test not successfull");

        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to make sure that a SPB, when asked for an object that is not present in the bag, returns null, not smoke.")]
        public void TestRetrieveNonexistentItem() {
            SmartPropertyBag sciences = new SmartPropertyBag();

            try {
                object x = sciences["Phlebotomy"];
                Assert.IsTrue(true);
            } catch {
                Assert.IsTrue(false);
            }

        }

        private void DumpEnumerable(IEnumerable enumerable, int depth) {
			foreach ( HierarchicalDictionaryEntry hde in enumerable ) {
				for ( int i = 0 ; i < depth ; i++ ) Debug.Write("\t");
                Debug.Write(hde.Key.ToString() + ", ");
                Debug.Write(hde.Value.GetType() + ", ");
				if ( hde.IsLeaf ) {
                    Debug.Write(hde.Value.ToString());
					if ( hde.Value is double ) {
						Debug.WriteLine(" <NOTE: this is a double.>"); 
					} else {
						Debug.WriteLine("");
					}
				} else {
					Debug.WriteLine("");
					DumpEnumerable((IEnumerable)hde.Value,depth+1);
				}
			}
		}

        public double m_steve = 12;
        public double ComputeSteve(){
            return m_steve;
        }
    }

}
