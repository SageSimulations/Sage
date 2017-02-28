/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Resources  {

	[TestClass]
	public class ResourceTesterExt {

		public ResourceTesterExt(){Init();}

		// TODO: Acquire and Reserve methods that take just the boolean arg.
		// TODO: Be more performance-sensitive when adding resource requests to
		//       a resource manager, in assuming that the collection is now dirty.

		#region MSTest Goo
		[TestInitialize] 
		public void Init() {}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}
		#endregion

		private PriRscReqTester m_prt;
		private static string m_resultString;

		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
		public void TestBasicFuctionality(){
			#region Expected Result
			string expected = @"[User_0,Req,1/1/0001 12:05:00 AM,0]
[User_1,Req,1/1/0001 12:05:00 AM,0]
[User_2,Req,1/1/0001 12:05:00 AM,0]
[User_3,Req,1/1/0001 12:05:00 AM,0]
[User_4,Req,1/1/0001 12:05:00 AM,0]
[User_0,Acq,1/1/0001 12:10:00 AM]
[User_0,Rls,1/1/0001 12:15:00 AM]
[User_1,Acq,1/1/0001 12:15:00 AM]
[User_1,Rls,1/1/0001 12:20:00 AM]
[User_2,Acq,1/1/0001 12:20:00 AM]
[User_2,Rls,1/1/0001 12:25:00 AM]
[User_3,Acq,1/1/0001 12:25:00 AM]
[User_3,Rls,1/1/0001 12:30:00 AM]
[User_4,Acq,1/1/0001 12:30:00 AM]
[User_4,Rls,1/1/0001 12:35:00 AM]";
			#endregion

			m_resultString = "";
			new PriRscReqTester(5).Start();
			//Console.WriteLine(m_resultString);

			System.Diagnostics.Debug.Assert(StripCRLF(m_resultString).Equals(StripCRLF(expected)),"TestPrioritizedResourceRequestWRemoval_2","Results didn't match!");
		}

		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
		public void TestPrioritizedResourceRequestHandling(){
			#region Expected Result
			string expected = @"[User_0,Req,1/1/0001 12:05:00 AM,0]
[User_1,Req,1/1/0001 12:05:00 AM,1]
[User_2,Req,1/1/0001 12:05:00 AM,2]
[User_3,Req,1/1/0001 12:05:00 AM,3]
[User_4,Req,1/1/0001 12:05:00 AM,4]
[User_4,Acq,1/1/0001 12:10:00 AM]
[User_4,Rls,1/1/0001 12:15:00 AM]
[User_3,Acq,1/1/0001 12:15:00 AM]
[User_3,Rls,1/1/0001 12:20:00 AM]
[User_2,Acq,1/1/0001 12:20:00 AM]
[User_2,Rls,1/1/0001 12:25:00 AM]
[User_1,Acq,1/1/0001 12:25:00 AM]
[User_1,Rls,1/1/0001 12:30:00 AM]
[User_0,Acq,1/1/0001 12:30:00 AM]
[User_0,Rls,1/1/0001 12:35:00 AM]
";
			#endregion

			m_prt = new PriRscReqTester(5);
			for ( int i = 0 ; i < 5 ; i++ ) {
				ResourceUser ru = m_prt.RscUsers[i];
				Console.WriteLine("Changing " + ru.Name + "'s priority to " + i);
				ru.ResourceRequest.Priority = i;
			}
			m_resultString = "";
			m_prt.Start();
			//Console.WriteLine(m_resultString);

			System.Diagnostics.Debug.Assert(StripCRLF(m_resultString).Equals(StripCRLF(expected)),"TestPrioritizedResourceRequestWRemoval_2","Results didn't match!");
		}
        private string StripCRLF(string structureString) => structureString.Replace("\r", "").Replace("\n", "");

        [TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
		public void TestPrioritizedResourceRequestWRemoval_1(){
			#region Expected Result
			string expected = @"[User_0,Req,1/1/0001 12:05:00 AM,0]
[User_1,Req,1/1/0001 12:05:00 AM,1]
[User_2,Req,1/1/0001 12:05:00 AM,2]
[User_3,Req,1/1/0001 12:05:00 AM,3]
[User_4,Req,1/1/0001 12:05:00 AM,4]
[User_4,Acq,1/1/0001 12:10:00 AM]
[User_4,Rls,1/1/0001 12:15:00 AM]
[User_2,Acq,1/1/0001 12:15:00 AM]
[User_2,Rls,1/1/0001 12:20:00 AM]
[User_3,Acq,1/1/0001 12:20:00 AM]
[User_3,Rls,1/1/0001 12:25:00 AM]
[User_1,Acq,1/1/0001 12:25:00 AM]
[User_1,Rls,1/1/0001 12:30:00 AM]
[User_0,Acq,1/1/0001 12:30:00 AM]
[User_0,Rls,1/1/0001 12:35:00 AM]
";
			#endregion
			m_prt = new PriRscReqTester(5);
			for ( int i = 0 ; i < 5 ; i++ ) {
				ResourceUser ru = m_prt.RscUsers[i];
				Console.WriteLine("Changing " + ru.Name + "'s priority to " + i);
				ru.ResourceRequest.Priority = i;
			}
			m_prt.Model.Starting+=new ModelEvent(Model_Starting);
			m_resultString = "";
			m_prt.Start();
			//Console.WriteLine(m_resultString);

			System.Diagnostics.Debug.Assert(StripCRLF(m_resultString).Equals(StripCRLF(expected)),"TestPrioritizedResourceRequestWRemoval_2","Results didn't match!");
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
		public void TestPrioritizedResourceRequestWRemoval_2(){
			#region Expected Result
			string expected = @"[User_0,Req,1/1/0001 12:05:00 AM,0]
[User_1,Req,1/1/0001 12:05:00 AM,0]
[User_2,Req,1/1/0001 12:05:00 AM,0]
[User_3,Req,1/1/0001 12:05:00 AM,0]
[User_4,Req,1/1/0001 12:05:00 AM,0]
[User_0,Acq,1/1/0001 12:10:00 AM]
[User_0,Rls,1/1/0001 12:15:00 AM]
[User_2,Acq,1/1/0001 12:15:00 AM]
[User_2,Rls,1/1/0001 12:20:00 AM]
[User_1,Acq,1/1/0001 12:20:00 AM]
[User_1,Rls,1/1/0001 12:25:00 AM]
[User_3,Acq,1/1/0001 12:25:00 AM]
[User_3,Rls,1/1/0001 12:30:00 AM]
[User_4,Acq,1/1/0001 12:30:00 AM]
[User_4,Rls,1/1/0001 12:35:00 AM]
";
			#endregion
			m_prt = new PriRscReqTester(5);
			m_prt.Model.Starting+=new ModelEvent(Model_Starting);
			m_resultString = "";
			m_prt.Start();
			//Console.WriteLine(m_resultString);
            System.Diagnostics.Debug.Assert(StripCRLF(m_resultString).Equals(StripCRLF(expected)), "TestPrioritizedResourceRequestWRemoval_2", "Results didn't match!");
		}

		private void Model_Starting(IModel theModel) {
			ExecEventReceiver eer = new ExecEventReceiver(AdjustPriority);
			DateTime when = theModel.Executive.Now + TimeSpan.FromMinutes(15);
			theModel.Executive.RequestEvent(eer,when,0.0,null,ExecEventType.Synchronous);
		}
		private void AdjustPriority(IExecutive exec, object userData){
			double newPri = 12.0;
			ResourceUser ru = m_prt.RscUsers[2];
			Console.WriteLine(exec.Now + " : *** Adjusting priority of " + ru.Name + " to " + newPri + ".");
			ru.ResourceRequest.Priority = newPri;
		}

		#region Support Classes 
		class PriRscReqTester {
			private ResourceUser[] m_users;
			private SelfManagingResource m_smr;
			private IModel m_model = null;
			private IResourceRequest m_rscReq;
			
			public PriRscReqTester(int nUsers){

				m_model = new Model("Resource Testing Model...");

				m_smr = new SelfManagingResource(m_model,"SMR",Guid.NewGuid(),1.0,1.0,true,true,true,true);

				m_users = new ResourceUser[nUsers];
			
				for ( int i = 0 ; i < nUsers ; i++ ) {
					m_users[i] = new ResourceUser(m_model,"User_"+i,Guid.NewGuid(),m_smr);
				}
				m_model.Starting+=new ModelEvent(AcqireResource);
			}

			public void Start(){
				m_model.Start();
			}

			#region Member Accessors
			public ResourceUser[] RscUsers { get { return m_users; } }
			public SelfManagingResource SMR { get { return m_smr; } }
			public IModel Model { get { return m_model; } }
			#endregion

			private void AcqireResource(IModel theModel) {
				Console.WriteLine("Acquiring the resource at " + theModel.Executive.Now );
				m_rscReq = new SimpleResourceRequest(1.0,m_smr);
				m_smr.Acquire(m_rscReq,false);
			
				ExecEventReceiver eer = new ExecEventReceiver(ReleaseResource);
				DateTime when = theModel.Executive.Now+TimeSpan.FromMinutes(10.0);
				double priority = 0.0;
				ExecEventType eet = ExecEventType.Synchronous;
				theModel.Executive.RequestEvent(eer,when,priority,null,eet);
			}

			private void ReleaseResource(IExecutive exec, object userData) {
				Console.WriteLine("Releasing the resource at " + exec.Now );
				m_rscReq.Release();
			}
		}

		class ResourceUser : IModelObject {
			private IResourceRequest m_irr;
			public ResourceUser(IModel model, string name, Guid guid, SelfManagingResource smr){
                InitializeIdentity(model, name, null, guid);

                m_irr = new SimpleResourceRequest(1.0,smr);
				m_model.Starting+=new ModelEvent(ScheduleMyResourceAction);

                IMOHelper.RegisterWithModel(this);
			}

			public IResourceRequest ResourceRequest { get { return m_irr; } }

			private void ScheduleMyResourceAction(IModel theModel) {
				ExecEventReceiver eer = new ExecEventReceiver(DoResourceAction);
				DateTime when = theModel.Executive.Now+TimeSpan.FromMinutes(5.0);
				double priority = 0.0;
				ExecEventType eet = ExecEventType.Detachable;
				theModel.Executive.RequestEvent(eer,when,priority,null,eet);
			}

			private void DoResourceAction(IExecutive exec, object obj){
				m_resultString += ("[" + this.Name + ",Req," + exec.Now + "," + m_irr.Priority + "]\r\n");
				Console.WriteLine("At time " + exec.Now + ", " + m_name + " trying to acquire with a priority of " + m_irr.Priority );
				m_irr.Acquire(m_irr.DefaultResourceManager,true);
				m_resultString += ("[" + this.Name + ",Acq," + exec.Now + "]\r\n");
				Console.WriteLine("At time " + exec.Now + ", " + m_name + " acquired...");
				exec.CurrentEventController.SuspendUntil(exec.Now+TimeSpan.FromMinutes(5.0));
				Console.WriteLine("At time " + exec.Now + ", " + m_name + " releasing...");
				m_resultString += ("[" + this.Name + ",Rls," + exec.Now  + "]\r\n");
				m_irr.Release();
				Console.WriteLine("...and release is done.");

			}

			#region Implementation of IModelObject
			private string m_name = null;
			public string Name { get { return m_name; } }
			private string m_description = null;
			/// <summary>
			/// A description of this Resource User.
			/// </summary>
			public string Description {
				get { return m_description==null?m_name:m_description; }
			}
			private Guid m_guid = Guid.Empty;
			public Guid Guid { get { return m_guid; } }
			private IModel m_model = null;
			public IModel Model { get { return m_model; } }
            /// <summary>
            /// Initializes the fields that feed the properties of this IModelObject identity.
            /// </summary>
            /// <param name="model">The IModelObject's new model value.</param>
            /// <param name="name">The IModelObject's new name value.</param>
            /// <param name="description">The IModelObject's new description value.</param>
            /// <param name="guid">The IModelObject's new GUID value.</param>
            public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
                IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
            }

			#endregion
		}

		#endregion

	}
}
