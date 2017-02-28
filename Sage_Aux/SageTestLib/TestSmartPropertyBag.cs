/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
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
			_Debug.WriteLine( "Done." );
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

			_Debug.WriteLine("Animals.Horses = " + animals["Horses"]);
			_Debug.WriteLine("Animals.Kingdom = " + animals["Kingdom"]);
			_Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

			animals["Dogs.Collies"] = 18;
			_Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

			SmartPropertyBag labs = new SmartPropertyBag();
			labs.AddValue("Black",4);
			labs.AddValue("Brown",2);
			labs.AddValue("Yellow",1);
			labs.AddString("Temperament","Mellow");
			labs.AddBoolean("Faithful",false);
			dogs.AddChildSPB("Labs",labs);
			_Debug.WriteLine(DiagnosticAids.DumpDictionary("",(IDictionary)animals.Memento.GetDictionary()));

            _Debug.Assert((double)animals["Dogs.Labs.Black"] == 4,"Black Lab is not 4");
            _Debug.Assert((double)animals["Dogs.Labs.Brown"] == 2,"Brown Lab is not 2");
            _Debug.Assert((double)animals["Dogs.Labs.Yellow"] == 1,"Yellow Lab is not 1");
            _Debug.Assert("Mellow".Equals((string)labs["Temperament"]),"The Labs temperament is not Mellow");
            _Debug.Assert((double)animals["Dogs.Collies"] == 18,"Collies are not 18");
            _Debug.Assert((double)animals["Dogs.Chows"] == 16,"Chows are not 16");
            _Debug.Assert((double)animals["Horses"] == 12,"Horses are not 12");
            _Debug.Assert("Animal".Equals((string)animals["Kingdom"]),"The Kingdom is not the Animal Kingdom");
            _Debug.Assert(!(bool)labs["Faithful"],"Labs are not not faithful");


			_Debug.WriteLine("Setting Animals.Dogs.Labs.Black to 19, Animals.Dogs.Labs.Temperament to \"Lovable\".");
			_Debug.WriteLine("...and Animals.Dogs.Labs.Faithful to \"true\".");
			animals["Dogs.Labs.Black"] = 19;
			animals["Dogs.Labs.Temperament"] = "Lovable";
			animals["Dogs.Labs.Faithful"] = true;
			_Debug.WriteLine(DiagnosticAids.DumpDictionary("",(IDictionary)animals.Memento.GetDictionary()));

            _Debug.Assert((double)animals["Dogs.Labs.Black"] == 19,"Black Lab is not 19");
            _Debug.Assert((double)animals["Dogs.Labs.Brown"] == 2,"Brown Lab is not 2");
            _Debug.Assert((double)animals["Dogs.Labs.Yellow"] == 1,"Yellow Lab is not 1");
            _Debug.Assert("Lovable".Equals((string)labs["Temperament"]),"The Labs temperament is not Lovable");
            _Debug.Assert((double)animals["Dogs.Collies"] == 18,"Collies are not 18");
            _Debug.Assert((double)animals["Dogs.Chows"] == 16,"Chows are not 16");
            _Debug.Assert((double)animals["Horses"] == 12,"Horses are not 12");
            _Debug.Assert("Animal".Equals((string)animals["Kingdom"]),"The Kingdom is the Animal Kingdom");
            _Debug.Assert((bool)labs["Faithful"],"Labs are faithful");
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

			_Debug.WriteLine("Animals.Horses = " + animals["Horses"]);
			_Debug.WriteLine("Animals.Kingdom = " + animals["Kingdom"]);
			_Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

			animals["Dogs.Collies"] = 18;
			_Debug.WriteLine("Animals.Dogs.Collies = " + animals["Dogs.Collies"]);

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

                _Debug.Assert((double)animals["Dogs.Labs.Black"] == 4,"Black Lab is not 4");
                _Debug.Assert((double)animals["Dogs.Labs.Brown"] == 2,"Brown Lab is not 2");
                _Debug.Assert((double)animals["Dogs.Labs.Yellow"] == 1,"Yellow Lab is not 1");
                _Debug.Assert("Mellow".Equals((string)labs["Temperament"]),"The Lab's temperament is not Mellow");
                _Debug.Assert((double)animals["Dogs.Collies"] == 18,"Collies are not 18");
                _Debug.Assert((double)animals["Dogs.Chows"] == 16,"Chows are not 16");
                _Debug.Assert((double)animals["Horses"] == 12,"Horses are not 12");
                _Debug.Assert("Animal".Equals((string)animals["Kingdom"]),"The Kingdom is not the Animal Kingdom");
                _Debug.Assert((bool)labs["Faithful"],"Labs are not faithful");

				//_Debug.WriteLine(DiagnosticAids.DumpDictionary("",(IDictionary)animals.Memento.GetDictionary()));
				mem = dogs.Memento;
				labs["Faithful"] = false;
				labs["Faithful"] = true;
				if ( (i%100)==0 ) {
					_Debug.WriteLine(((TimeSpan)(DateTime.Now-start)).TotalSeconds);
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
			_Debug.WriteLine(DiagnosticAids.DumpDictionary("mem1",mem1.GetDictionary()));
			Highpoint.Sage.Utility.Mementos.IMemento mem2 = spb1.Memento;
			_Debug.WriteLine(DiagnosticAids.DumpDictionary("mem2",mem2.GetDictionary()));
			_Debug.WriteLine("The new snapshot is " + (mem1==mem2?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem2 == mem1,"Memento 1 is not equal Memento 2");

            _Debug.Assert((double)spb1["Bill"] == 28,"Bill is not 28");
            _Debug.Assert((double)spb1["Steve"] == 12,"Steve is not 12");
            _Debug.Assert("SPB1".Equals(spb1["Name"]),"Name is not SPB1");

			_Debug.WriteLine("Changing \"Fred\" to 14 from 12.");
			spb1["Fred"]=14;
			Highpoint.Sage.Utility.Mementos.IMemento mem3 = spb1.Memento;
			_Debug.WriteLine(DiagnosticAids.DumpDictionary("mem3",mem3.GetDictionary()));
			_Debug.WriteLine("The new snapshot is " + (mem2==mem3?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem2!=mem3,"Memento 2 is not not equal to Memento 3");
            _Debug.Assert((double)spb1["Fred"] == 14,"Fred is not 14");

			_Debug.WriteLine("Changing \"Name\" to \"spb1\" from \"SPB1\".");
			spb1["Name"]="spb1";
			Highpoint.Sage.Utility.Mementos.IMemento mem3a = spb1.Memento;
			_Debug.WriteLine(DiagnosticAids.DumpDictionary("mem3a",mem3a.GetDictionary()));
			_Debug.WriteLine("The new snapshot is " + (mem2==mem3a?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem2!=mem3a,"Memento 2 is equal to memento 3a");

            _Debug.Assert((double)spb1["Bill"] == 29,"Bill is not 29");	// changing Fred to 14 on line 120 changes Bill too

			spb1.AddValue("_Connie",99);

			SmartPropertyBag spb2 = new SmartPropertyBag();
			spb2.AddValue("_Pete",14);
			spb1.AddAlias("Pete",spb2,"_Pete");
			spb1.AddExpression("Marvin","Pete+Bill",new string[]{"Pete","Bill"});
            _Debug.Assert((double)spb1["Marvin"] == 43,"Marvin is not 43");


			Highpoint.Sage.Utility.Mementos.IMemento mem4 = spb1.Memento;
			Highpoint.Sage.Utility.Mementos.IMemento mem5 = spb1.Memento;
			_Debug.WriteLine("The new snapshot is " + (mem4==mem5?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem5 == mem4,"Memento 5 is not equal to memento 4");

			_Debug.WriteLine("\r\nChanging a value in a foreign alias.");
			spb2["_Pete"]=16;
            _Debug.Assert((double)spb1["Marvin"] == 45,"Marvin is not 45");

			Highpoint.Sage.Utility.Mementos.IMemento mem6 = spb1.Memento;
			_Debug.WriteLine("The new snapshot is " + (mem5==mem6?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem5!=mem6,"Memento is equal to memento 6");

			_Debug.WriteLine("Capturing another snapshot without change.");
			Highpoint.Sage.Utility.Mementos.IMemento mem7 = spb1.Memento;
			_Debug.WriteLine("The new snapshot is " + (mem6==mem7?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem6 == mem7,"Memento 6 is not equal to memento 7");
            
			_Debug.WriteLine("Changing the data driving a delegate-computed value.");
			m_steve = 14;

			Highpoint.Sage.Utility.Mementos.IMemento mem8 = spb1.Memento;
			_Debug.WriteLine("The new snapshot is " + (mem8==mem7?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem7!=mem8,"Memento 7 is equal to memento 8");

			_Debug.WriteLine("\r\nWe will now test snapshotting's functionality as it pertains to subsidiaries.");

			SmartPropertyBag spb1_1 = new SmartPropertyBag();
			spb1.AddChildSPB("Sub",spb1_1);
			spb1_1.AddValue("marine",12);

			Highpoint.Sage.Utility.Mementos.IMemento mem9 = spb1.Memento;
			_Debug.WriteLine(DiagnosticAids.DumpDictionary("Main",mem9.GetDictionary()));
			_Debug.WriteLine("Taking another immediately-following snapshot.");
			Highpoint.Sage.Utility.Mementos.IMemento mem10 = spb1.Memento;
			_Debug.WriteLine("The new snapshot is " + (mem9==mem10?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem10 == mem9,"Memento 10 is not equal to memento 9");


			_Debug.WriteLine("Changing a data member in a subsidiary.");
			spb1_1["marine"] = 21;
			Highpoint.Sage.Utility.Mementos.IMemento mem11 = spb1.Memento;
			_Debug.WriteLine("The new snapshot is " + (mem10==mem11?"":"not ") + "equal to the one preceding it.\r\n");
            _Debug.Assert(mem11!=mem10,"Memento 11 is equal to memento 10");

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

            _Debug.Assert(spb1!=spb2,"Object spb1 and spb2 cannot be the same instance");
            _Debug.Assert(spb1.Equals(spb2),"SPB1 and SPB2 should be equal");
            _Debug.Assert(spb1.Memento==spb2.Memento,"SPB1.Memento and SPB2.Memento should be equal");

            _Debug.WriteLine(DiagnosticAids.DumpDictionary("Before",spb1.Memento.GetDictionary()));
            _Debug.WriteLine(DiagnosticAids.DumpDictionary("After",spb2.Memento.GetDictionary()));

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

            _Debug.WriteLine(spb2["OtherGuysHot"]);
            spb1["Hot"] = false;
            _Debug.WriteLine(spb2["OtherGuysHot"]);
            _Debug.Assert(!(bool)spb2["OtherGuysHot"],"OtherGuysHot is not false");


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

            _Debug.Assert(!(bool)spb0["Habanero.Hot"],"Habanero.Hot is hot");
            _Debug.Assert("Habañero".Equals((string)spb0["Habanero.Name"]),"Habanero.Name is not Habañero");
            _Debug.Assert((double)spb0["Habanero.Scovilles"] == 32000,"Habanero.Scovilles is not 32000");

            _Debug.Assert((bool)spb0["Jalapeno.Hot"],"Jalapeno.Hot is not hot");
            _Debug.Assert("Jalapeño".Equals((string)spb0["Jalapeno.Name"]),"Jalapeno.Name is not Jalapeño");
            _Debug.Assert((double)spb0["Jalapeno.Scovilles"] == 16000,"Jalapeno.Scovilles is not 16000");

			Highpoint.Sage.Utility.Mementos.IMemento mem2 = spb0.Memento;

            _Debug.WriteLine(DiagnosticAids.DumpDictionary("Before",mem1.GetDictionary()));
            _Debug.WriteLine(DiagnosticAids.DumpDictionary("After",mem2.GetDictionary()));

            spb0["Jalapeno.Hot"] = false;
            spb0["Habanero.Hot"] = true;
            spb0["Jalapeno.Scovilles"] = 12000;
            spb0["Habanero.Scovilles"] = 29000;
            
            Highpoint.Sage.Utility.Mementos.IMemento mem3 = spb0.Memento;

            _Debug.Assert((bool)spb0["Habanero.Hot"],"Habanero.Hot is not hot");
            _Debug.Assert((double)spb0["Habanero.Scovilles"] == 29000,"Habanero.Scovilles is not 29000");

            _Debug.Assert(!(bool)spb0["Jalapeno.Hot"],"Jalapeno.Hot is not hot");
            _Debug.Assert((double)spb0["Jalapeno.Scovilles"] == 12000,"Jalapeno.Scovilles is not 12000");

			_Debug.WriteLine(DiagnosticAids.DumpDictionary("After more...",mem3.GetDictionary()));

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

            _Debug.Assert(true, "Visual test not successfull");

        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to make sure that a SPB, when asked for an object that is not present in the bag, returns null, not smoke.")]
        public void TestRetrieveNonexistentItem() {
            SmartPropertyBag sciences = new SmartPropertyBag();

            try {
                object x = sciences["Phlebotomy"];
                _Debug.Assert(true);
            } catch {
                _Debug.Assert(false);
            }

        }

        private void DumpEnumerable(IEnumerable enumerable, int depth) {
			foreach ( HierarchicalDictionaryEntry hde in enumerable ) {
				for ( int i = 0 ; i < depth ; i++ ) _Debug.Write("\t");
                _Debug.Write(hde.Key.ToString() + ", ");
                _Debug.Write(hde.Value.GetType() + ", ");
				if ( hde.IsLeaf ) {
                    _Debug.Write(hde.Value.ToString());
					if ( hde.Value is double ) {
						_Debug.WriteLine(" <NOTE: this is a double.>"); 
					} else {
						_Debug.WriteLine("");
					}
				} else {
					_Debug.WriteLine("");
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
