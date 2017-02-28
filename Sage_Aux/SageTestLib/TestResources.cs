/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using Highpoint.Sage.Materials.Chemistry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using _Debug = System.Diagnostics.Debug;


namespace Highpoint.Sage.Resources  {

	[TestClass]
	public class ResourceTester {

		public ResourceTester(){Init();}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Allocates and deallocates resources and checks the behavior")]
		public void TestPersistentResourceBasics(){

			Model model = new Model("Resource Testing Model...");

			SelfManagingResource steamSystem = new SelfManagingResource(model,"SteamSystem",Guid.NewGuid(),7000.0,false,false,true);

			ResourceRequest[] requests = new ResourceRequest[7];
			for ( int i = 0; i < 7 ; i++ ) {
				requests[i] = new ResourceRequest(1000.0);
			}

			for ( int i = 0; i < 7 ; i++ ) {
				if ( steamSystem.Reserve(requests[i],false) ){
					double obtained = requests[i].QuantityObtained;
					double remaining = requests[i].ResourceObtained.Available;
					_Debug.WriteLine("Successfully reserved " + obtained + " pounds of steam - " + remaining + " remains.");
				} else {
					_Debug.WriteLine("Failed to reserve steam for request["+i+"]");
				}
			}

			_Debug.WriteLine("Unreserving steam from 2 requests");
			steamSystem.Unreserve(requests[2]);
			double available = steamSystem.Available;
			_Debug.WriteLine("Successfully unreserved steam - " + available + " available.");

			steamSystem.Unreserve(requests[3]);
			available = steamSystem.Available;
			_Debug.WriteLine("Successfully unreserved steam - " + available + " available.");

			for ( int i = 5; i < 7 ; i++ ) {
				if ( steamSystem.Reserve(requests[i],false) ){
					double obtained = requests[i].QuantityObtained;
					double remaining = requests[i].ResourceObtained.Available;
					_Debug.WriteLine("Successfully reserved " + obtained + " pounds of steam - " + remaining + " remains.");
				} else {
					_Debug.WriteLine("Failed to acquire steam for request["+i+"]");
				}
			}


			_Debug.WriteLine("Unreserving all steam requests - ");
			for ( int i = 0; i < 7 ; i++ ) steamSystem.Unreserve(requests[i]);
			available = steamSystem.Available;
			_Debug.WriteLine("Successfully unreserved steam - " + available + " available.");

			// AEL, bug "Reserve a resource over an existing one" submitted.
//			_Debug.WriteLine("Trying to acquire all steam requests - ");
//			for ( int i = 0; i < 7 ; i++ ) {
//				if ( steamSystem.Acquire(requests[i],false) ){
//					double obtained = requests[i].QuantityObtained;
//					double remaining = requests[i].ResourceObtained.Available;
//					_Debug.WriteLine("Successfully acquired " + obtained + " pounds of steam - " + remaining + " remains.");
//				} else {
//					_Debug.WriteLine("Failed to acquire steam for request["+i+"]");
//				}
//			}
//
//			_Debug.WriteLine("Releasing 2 steam requests ");
//			steamSystem.Release(requests[1]);
//			steamSystem.Release(requests[4]);
//
//			for ( int i = 5; i < 7 ; i++ ) {
//				if ( steamSystem.Acquire(requests[i],false) ){
//					double obtained = requests[i].QuantityObtained;
//					double remaining = requests[i].ResourceObtained.Available;
//					_Debug.WriteLine("Successfully acquired " + obtained + " pounds of steam - " + remaining + " remains.");
//				} else {
//					_Debug.WriteLine("Failed to acquire steam for request["+i+"]");
//				}
//			}

			_Debug.WriteLine("Releasing all steam requests - ");
			for ( int i = 0; i < 7 ; i++ ) steamSystem.Release(requests[i]);

			//model.Validate();
			model.Start();

		}

		
		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("Checks if resources can be augmented or depleted")]
		public void TestConsumableResourceBasics(){

			Model model = new Model("Resource Testing Model...");
			_Debug.WriteLine("Test results of replenishable material inventories.");

			Hashtable materialInventory = new Hashtable();

			_Debug.WriteLine("Setting up an inventory of 500 liters Water, capacity of 1000 liters.");
			MaterialType waterType = new MaterialType(model,"Water",Guid.NewGuid(),1.0,1.0,MaterialState.Liquid,18.0);
			MaterialResourceItem waterItem = new MaterialResourceItem(model,waterType,500,20,1000);

			_Debug.WriteLine("Setting up an inventory of 100 liters SodiumChloride, capacity of 250 liters.");
			MaterialType NaClType = new MaterialType(model,"SodiumChloride",Guid.NewGuid(),1.2,1.8,MaterialState.Liquid);
			MaterialResourceItem NaClItem = new MaterialResourceItem(model,NaClType,100,20,250);

			materialInventory.Add(waterType,waterItem);
			materialInventory.Add(NaClType,NaClItem);

			Enum augment = MaterialResourceRequest.Direction.Augment;
			Enum deplete = MaterialResourceRequest.Direction.Deplete;
			object[,] tests = new object[,]{    {waterType,150,augment,true}
											   ,{waterType,250,deplete,true}
											   ,{waterType,350,augment,true}
											   ,{NaClType,200,deplete,false}
											   ,{waterType,100,deplete,true}
											   ,{NaClType,300,augment,false}
											   ,{waterType,200,deplete,true}
											   ,{NaClType,300,augment,false}
											   ,{waterType,100,deplete,true}
											   ,{NaClType,250,deplete,false}
											   ,{NaClType,1000,augment,false}};
			MaterialResourceRequest mrr;
			_Debug.WriteLine("");
			for ( int i = 0 ; i < tests.GetLength(0) ; i++ ) {

				#region >>> Set up test parameters from array. <<<
				MaterialType mt = (MaterialType)tests[i,0];
				double quantity = Convert.ToDouble(tests[i,1]);
				MaterialResourceRequest.Direction direction = (MaterialResourceRequest.Direction)tests[i,2];
				string itemName = (mt.Equals(waterType)?"WaterItem":"NaClItem");
				MaterialResourceItem item = (mt.Equals(waterType)?waterItem:NaClItem);
				bool expected = (bool)tests[i,3];
				#endregion

				string testDescription = "Test " + (i+1) + ": Trying to " + (direction.Equals(augment)?"augment":"deplete") + " " + quantity + " liters of " + itemName + ".";
				_Debug.WriteLine(testDescription);
				_Debug.WriteLine("Before - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
				mrr = new MaterialResourceRequest(mt,quantity,direction);
				MaterialResourceItem mri = (MaterialResourceItem)materialInventory[mt];
				bool result = mri.Acquire(mrr,false);
                _Debug.Write((result?"Request honored.":"Request denied."));
                _Debug.Assert(result==expected,"This test is a failure");
				_Debug.WriteLine(((result==expected)?" - this was expected.":" - THIS IS A TEST FAILURE!" )) ;
                _Debug.Assert(expected==result,testDescription); 
				_Debug.WriteLine("After - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
				_Debug.WriteLine("");
			}

		}

		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("AEL, not sure what this is testing")]
		public void TestMaterialConduits(){

			Model model = new Model("Conduit Testing Model...");
			_Debug.WriteLine("Test results of replenishable material inventories.");

			Hashtable materialInventory1 = new Hashtable();
			Hashtable materialInventory2 = new Hashtable();
			Hashtable materialInventory3 = new Hashtable();
			MaterialType WaterType = new MaterialType(model,"Water",Guid.NewGuid(),1.0,1.0,MaterialState.Liquid);
			MaterialType NaClType = new MaterialType(model,"SodiumChloride",Guid.NewGuid(),1.2,1.8,MaterialState.Solid);
			MaterialResourceItem WaterItem = new MaterialResourceItem(model,WaterType,500,20,1000);
			MaterialResourceItem NaClItem  = new MaterialResourceItem(model,NaClType,100,20,250);

			_Debug.WriteLine("Setting up an inventory of 500 liters Water, capacity of 1000 liters in materialInventory1.");
			materialInventory1.Add(WaterType,WaterItem);

			_Debug.WriteLine("Setting up an inventory of 100 liters SodiumChloride, capacity of 250 liters in materialInventory1.");
			materialInventory1.Add(NaClType,NaClItem);

			_Debug.WriteLine("Setting up an inventory of 750 liters Water, capacity of 1500 liters in materialInventory2.");
			materialInventory2.Add(WaterType,new MaterialResourceItem(model,WaterType,750,20,1500));

			_Debug.WriteLine("Setting up an inventory of 400 liters SodiumChloride, capacity of 800 liters in materialInventory3.");
			materialInventory3.Add(NaClType,new MaterialResourceItem(model,NaClType,400,20,800));

			// AEL, not sure why thest lines don't matter in the test. I probably miss something.
//			MaterialConduitManager mcm = new MaterialConduitManager(materialInventory1);
//			mcm.AddConduit(materialInventory2,WaterType);
//			mcm.AddConduit(materialInventory3,NaClType);

			Enum augment = MaterialResourceRequest.Direction.Augment;
			Enum deplete = MaterialResourceRequest.Direction.Deplete;
			object[,] tests = new object[,]{    {WaterType,150,augment,true}
											   ,{WaterType,250,deplete,true}
											   ,{WaterType,350,augment,true}
											   ,{NaClType,200,deplete,false}
											   ,{WaterType,100,deplete,true}
											   ,{NaClType,300,augment,false}
											   ,{WaterType,200,deplete,true}
											   ,{NaClType,300,augment,false}
											   ,{WaterType,100,deplete,true}
											   ,{NaClType,250,deplete,false}
											   ,{NaClType,1000,augment,false}};
			MaterialResourceRequest mrr;
			_Debug.WriteLine("");
			for ( int i = 0 ; i < tests.GetLength(0) ; i++ ) {

				#region >>> Set up test parameters from array. <<<
				MaterialType mt = (MaterialType)tests[i,0];
				double quantity = Convert.ToDouble(tests[i,1]);
				MaterialResourceRequest.Direction direction = (MaterialResourceRequest.Direction)tests[i,2];
				string itemName = (mt.Equals(WaterType)?"WaterItem":"NaClItem");
				MaterialResourceItem item = (mt.Equals(WaterType)?WaterItem:NaClItem);
				bool expected = (bool)tests[i,3];
				#endregion

				string testDescription = "Test " + (i+1) + ": Trying to " + (direction.Equals(augment)?"augment":"deplete") + " " + quantity + " liters of " + itemName + ".";
				_Debug.WriteLine(testDescription);
				_Debug.WriteLine("Before - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
				mrr = new MaterialResourceRequest(mt,quantity,direction);
				MaterialResourceItem mri = (MaterialResourceItem)materialInventory1[mt];
				bool result = mri.Acquire(mrr,false);
                _Debug.Write((result?"Request honored.":"Request denied."));
                _Debug.Assert(result==expected,"This test is a failure");
				_Debug.WriteLine(((result==expected)?" - this was expected.":" - THIS IS A TEST FAILURE!" )) ;
                _Debug.Assert(result==expected,testDescription);
				_Debug.WriteLine("After - " + itemName + " has " + item.Available + " liters, and a capacity of " + item.Capacity + ".");
				_Debug.WriteLine("");
			}

		}

		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("AEL, not sure what this is testing")]
		public void TestEarmarking(){
			Model model = new Model("Test model");
			
			object genericAccessKey = new object();

			Resource rsc1 = new Resource(model,"Rsc 1",Guid.NewGuid(),2.0,2.0,true,true,true);
			Resource rsc2 = new Resource(model,"Rsc 2",Guid.NewGuid(),2.0,2.0,true,true,true);

			ResourceManager rscPool1 = new ResourceManager(model,"pool 1",Guid.NewGuid());
			rscPool1.Add(rsc1);
			rscPool1.Add(rsc2);

			// First, add an AccessManager to the resource pool. Then push a SingleKeyAccessRegulator
			// onto the AccessManager's stack - the SingleKeyAccessRegulator will permit only resource
			// requests that hold a reference to the 'genericAccessKey'.
			rscPool1.AccessRegulator = new SimpleAccessManager(true);
			((SimpleAccessManager)rscPool1.AccessRegulator).PushAccessRegulator(new SingleKeyAccessRegulator(null,genericAccessKey),null);

			// Create and try to use a resource request that has the generic access key.
			ResourceRequest rr = new ResourceRequest(1.0);
			rr.Key = genericAccessKey;

			rscPool1.Acquire(rr,false); // This should succeed.
			_Debug.WriteLine(rr.ResourceObtained==null?"Resource not obtained.":"Resource obtained.");
			rr.Release();

			// Now change the key to 'just an object' - i.e. one that the AccessManager doesn't recognize.
			rr.Key = new object();
			rscPool1.Acquire(rr,false); // This should fail.
			_Debug.WriteLine(rr.ResourceObtained==null?"Resource not obtained":"Resource obtained.");


		}
		

		[TestMethod] 
		[Highpoint.Sage.Utility.FieldDescription("AEL, not sure what this is testing")]
		public void TestAdvancedEarmarking(){
			Model model = new Model("Test model");

			SelfManagingResource rsc = new SelfManagingResource(model,"Resource",Guid.NewGuid(),2.0,2.0,true,true,true);
			rsc.AccessRegulator = new SimpleAccessManager(true);
			
			object keyA  = "Key A" ; // Always grants to any 'A' holders.
			object keyA1 = "Key A1"; // Sometimes grants to 'A1' holders.
			object keyA2 = "Key A2"; // Sometimes grants to 'A2' holders.

			IAccessRegulator reg1 = new MultiKeyAccessRegulator(rsc,new ArrayList(new object[]{keyA,keyA1}));
			IAccessRegulator reg2 = new MultiKeyAccessRegulator(rsc,new ArrayList(new object[]{keyA,keyA2}));

			ResourceRequest rrTrack1 = new ResourceRequest(1.0);
			rrTrack1.Key = keyA1;
			ResourceRequest rrTrack2 = new ResourceRequest(1.0);
			rrTrack2.Key = keyA2;
			ResourceRequest rrKahuna = new ResourceRequest(1.0);
			rrKahuna.Key = keyA;

			/////////////////////////////////////////////////////////////////////////////
			// Test A: Track 1 acquires equipment, installs key. Track 2 tries and fails.
			((SimpleAccessManager)rsc.AccessRegulator).PushAccessRegulator(reg1,null);
			TryAcquire(rsc,rrTrack1,true);
			TryAcquire(rsc,rrTrack2,false);

			/////////////////////////////////////////////////////////////////////////////
			// Test B: Push the track 2 regulator into the manager, and try to acquire from track 1 - should fail.
			((SimpleAccessManager)rsc.AccessRegulator).PushAccessRegulator(reg2,null);
			TryAcquire(rsc,rrTrack1,false);
			TryAcquire(rsc,rrTrack2,true);

			/////////////////////////////////////////////////////////////////////////////
			// Test C: Push the track 1 regulator into the manager, and try to acquire from track 1 - should succeed.
			((SimpleAccessManager)rsc.AccessRegulator).PushAccessRegulator(reg1,null);
			TryAcquire(rsc,rrTrack2,false);
			TryAcquire(rsc,rrTrack1,true);

			/////////////////////////////////////////////////////////////////////////////
			// Test D: Try to acquire from kahuna - should succeed. Then release, pop access reg, try again, should still succeed.
			TryAcquire(rsc,rrKahuna,true);
			((SimpleAccessManager)rsc.AccessRegulator).PopAccessRegulator(null);
			TryAcquire(rsc,rrKahuna,true);


			/////////////////////////////////////////////////////////////////////////////
			// Test E: Clear all access regulators, try with each key to acquire. All should succeed.
			((SimpleAccessManager)rsc.AccessRegulator).PopAccessRegulator(null);
			((SimpleAccessManager)rsc.AccessRegulator).PopAccessRegulator(null);
			TryAcquire(rsc,rrTrack1,true);
			TryAcquire(rsc,rrTrack2,true);
			TryAcquire(rsc,rrKahuna,true);

		}

		private void TryAcquire(SelfManagingResource rsc, IResourceRequest irr, bool expectSuccess){

			string result = "Acqusition of " + rsc.Name + " using key " + irr.Key.ToString() + " expected to " + (expectSuccess?"succeed":"fail") + ".";
			if ( rsc.Acquire(irr,false) == expectSuccess ) {
				if ( expectSuccess ) rsc.Release(irr);
				Console.WriteLine("Sub-test passed : " + result );
			} else {
                _Debug.Assert(false,"Access Regulation","Sub-test failed : " + result);
			}
		}

		
		private IResourceManager m_resourcePoolForStarvation;
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Allocates and deallocates resources and checks the behavior")]
		public void TestStarvation(){

			Model model = new Model("Starvation Testing Model...");

			m_resourcePoolForStarvation = new SelfManagingResource(model,"SteamSystem",Guid.NewGuid(),1000.0,false,false,true);

			model.Starting+=new ModelEvent(OnModelStarting);

			model.Start();

			foreach ( IModelWarning warning in model.Warnings ) {
				Console.WriteLine(warning.Name + " : " + warning.Narrative);
			}

		}
		private void OnModelStarting(IModel theModel) {
			theModel.Executive.RequestEvent(new ExecEventReceiver(GetScarceResource),DateTime.Now,0.0,null,ExecEventType.Detachable);
			theModel.Executive.RequestEvent(new ExecEventReceiver(GetScarceResource),DateTime.Now,1.0,null,ExecEventType.Detachable);
		}
		
		private void GetScarceResource(IExecutive exec, object userData){
			m_resourcePoolForStarvation.Acquire(new ResourceRequest(900.0),true);
		}

		
		class ResourceRequest : Highpoint.Sage.Resources.ResourceRequest {
        
			public ResourceRequest(double quantity):base(quantity){}

			public override double GetScore(IResource resource){
				if ( resource.Available >= QuantityDesired ) return double.MaxValue;
				return Double.MinValue;
			}

			protected override ResourceRequestSource GetDefaultReplicator() {
				return new ResourceRequestSource(DefaultReplicator);
			}

			private IResourceRequest DefaultReplicator(){
				ResourceRequest irr = new ResourceRequest(QuantityDesired);
				irr.DefaultResourceManager = DefaultResourceManager;
				return irr;
			}

		}

	}
}

