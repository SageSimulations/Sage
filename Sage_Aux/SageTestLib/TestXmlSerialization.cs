/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Materials.Chemistry;


namespace Highpoint.Sage.Persistence {

	[TestClass]
	public class PersistenceTester {

		public PersistenceTester(){}

		[TestMethod] public void TestPersistenceBasics(){

			XmlSerializationContext xsc = new XmlSerializationContext();

			MyTestObject mto2 = new MyTestObject("Gary",3.1,null);

			MyTestObject mto = new MyTestObject("Bill",6.2,
				new MyTestObject("Bob",12.4,
				new MyTestObject("Steve",24.8,
				new MyTestObject("Dave",48.1,
				new MyTestObject("Sally",96.2,
				new MyTestObject("Rufus",186.9,
				null))))));

			_Debug.WriteLine("Setting " + mto.Child1.Child1.Name + "'s child2 to " + mto2.Name);
			mto.Child1.Child1.Child2 = mto2;
			_Debug.WriteLine("Setting " + mto.Child1.Name + "'s child2 to " + mto2.Name);
			mto.Child1.Child2 = mto2;

			xsc.StoreObject("MTO",mto);

			xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "foo.xml");

			xsc.Reset();

            xsc.Load(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "foo.xml");

			MyTestObject mto3 = (MyTestObject)xsc.LoadObject("MTO");

			xsc = new XmlSerializationContext();
			xsc.StoreObject("MTO",mto3);
            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "foo2.xml");


		}
	
		[TestMethod] public void TestPersistenceWaterStorage(){

			MaterialType mt = new MaterialType(null,"Water",Guid.NewGuid(),1.234,4.05,MaterialState.Liquid,18.0,1034);

			BasicReactionSupporter brs = new BasicReactionSupporter();
			brs.MyMaterialCatalog.Add(mt);

			IMaterial water = mt.CreateMass(1500,35);

			XmlSerializationContext xsc = new XmlSerializationContext();

			xsc.StoreObject("Water",water);

            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "water.xml");

			xsc.Reset();

			xsc.StoreObject("MC",brs.MyMaterialCatalog);

            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "mc.xml");

		}

		[TestMethod] public void TestPersistenceWaterRestoration(){

			XmlSerializationContext xsc = new XmlSerializationContext();
            xsc.Load(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "water.xml");
			IMaterial water = (IMaterial)xsc.LoadObject("Water");

			_Debug.WriteLine(water);

		}

		
		[TestMethod] public void TestPersistenceChemistryStorage(){
			BasicReactionSupporter brs = new BasicReactionSupporter();
			Initialize(brs);
			XmlSerializationContext xsc = new XmlSerializationContext();
			xsc.StoreObject("Chemistry",brs);

            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "chemistry.xml");
		}
		
		private void Initialize(BasicReactionSupporter brs){

			MaterialCatalog mcat = brs.MyMaterialCatalog;
			ReactionProcessor rp = brs.MyReactionProcessor;

			mcat.Add(new MaterialType(null, "Water", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Hydrochloric Acid", Guid.NewGuid(),1.1890,2.5500,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Caustic Soda", Guid.NewGuid(),2.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Chloride", Guid.NewGuid(),2.1650,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sulfuric Acid 98%", Guid.NewGuid(),1.8420,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Potassium Hydroxide", Guid.NewGuid(),1.3000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Potassium Sulfate", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Nitrous Acid", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Nitrite", Guid.NewGuid(),2.3800,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Potassium Nitrite", Guid.NewGuid(),1.9150,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Aluminum Hydroxide", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Ammonia", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Ammonium Hydroxide", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Bromine", Guid.NewGuid(),3.1200,4.1800,MaterialState.Liquid));

			Reaction r1 = new Reaction(null,"Reaction 1",Guid.NewGuid());
			r1.AddReactant(mcat["Caustic Soda"],0.5231);
			r1.AddReactant(mcat["Hydrochloric Acid"],0.4769);
			r1.AddProduct(mcat["Water"],0.2356);
			r1.AddProduct(mcat["Sodium Chloride"],0.7644);
			rp.AddReaction(r1);

			Reaction r2 = new Reaction(null,"Reaction 2",Guid.NewGuid());
			r2.AddReactant(mcat["Sulfuric Acid 98%"],0.533622);
			r2.AddReactant(mcat["Potassium Hydroxide"],0.466378);
			r2.AddProduct(mcat["Water"],0.171333);
			r2.AddProduct(mcat["Potassium Sulfate"],0.828667);
			rp.AddReaction(r2);

			Reaction r3 = new Reaction(null,"Reaction 3",Guid.NewGuid());
			r3.AddReactant(mcat["Caustic Soda"],0.459681368);
			r3.AddReactant(mcat["Nitrous Acid"],0.540318632);
			r3.AddProduct(mcat["Water"],0.207047552);
			r3.AddProduct(mcat["Sodium Nitrite"],0.792952448);
			rp.AddReaction(r3);

			Reaction r4 = new Reaction(null,"Reaction 4",Guid.NewGuid());
			r4.AddReactant(mcat["Potassium Hydroxide"],0.544102);
			r4.AddReactant(mcat["Nitrous Acid"],0.455898);
			r4.AddProduct(mcat["Water"],0.174698);
			r4.AddProduct(mcat["Potassium Nitrite"],0.825302);
			rp.AddReaction(r4);

		}


	}

	class MyTestObject : IXmlPersistable {
		private MyTestObject m_child1;
		private MyTestObject m_child2;
		private string m_name;
		private double m_age;
		private bool m_married;
		private DateTime m_birthday;
		private TimeSpan m_ts;
		Hashtable m_ht = new Hashtable();
		ArrayList m_al = new ArrayList();
		public MyTestObject(string name, double age, MyTestObject child){
			m_name  = name;
			m_age   = age;
			m_child1 = child;
			Random random = new Random();
			m_married = random.NextDouble()<0.5;
			m_birthday = DateTime.Now - TimeSpan.FromTicks((long)(random.NextDouble()*TimeSpan.FromDays(20).Ticks));
			m_ts = TimeSpan.FromTicks((long)(random.NextDouble()*TimeSpan.FromDays(20).Ticks));
			m_ht = new Hashtable();
			m_ht.Add("Age",m_age);
			m_ht.Add("Birthday",m_birthday);
			m_al.Add("Dog");
			m_al.Add("Cat");
			m_al.Add("Cheetah");
			m_al.Add("Banana");
			m_al.Add("Which one of these is not like the others?");
		}
		public string Name { get { return m_name; } }
		public MyTestObject Child1 { get { return m_child1; } }
		public MyTestObject Child2 { get { return m_child2; } set { m_child2 = value; } }
		
		public MyTestObject(){}
		public void SerializeTo(XmlSerializationContext xmlsc){
			xmlsc.StoreObject("Child1",m_child1);
			xmlsc.StoreObject("Child2",m_child2);
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("Age",m_age);
			xmlsc.StoreObject("Married",m_married);
			xmlsc.StoreObject("Birthday",m_birthday);
			xmlsc.StoreObject("TimeSpan",m_ts);
			xmlsc.StoreObject("Hashtable",m_ht);
			xmlsc.StoreObject("ArrayList",m_al);
		}
		public void DeserializeFrom(XmlSerializationContext xmlsc){
			m_child1 = (MyTestObject)xmlsc.LoadObject("Child1");
			m_child2 = (MyTestObject)xmlsc.LoadObject("Child2");
			m_name  = (string)xmlsc.LoadObject("Name");
			m_age   = (double)xmlsc.LoadObject("Age");
			m_married = (bool)xmlsc.LoadObject("Married");
			m_birthday = (DateTime)xmlsc.LoadObject("Birthday");
			m_ts = (TimeSpan)xmlsc.LoadObject("TimeSpan");
			m_ht = (Hashtable)xmlsc.LoadObject("Hashtable");
			m_al = (ArrayList)xmlsc.LoadObject("ArrayList");
		}
	}
}