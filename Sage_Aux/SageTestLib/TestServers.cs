/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
//using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.ItemBased.Servers;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.Queues;
using Highpoint.Sage.ItemBased.SinksAndSources;
using Highpoint.Sage.ItemBased.SplittersAndJoiners;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.Resources;

//using ProcessStep = Highpoint.Sage.Servers.SimpleServerWithPreQueue;

namespace Highpoint.Sage.ItemBased.Blocks {

	/// <summary>
	/// Summary description for zTestBranchBlocks.
	/// </summary>
	[TestClass]
	public class ServerTester {

		#region MSTest Goo
		[TestInitialize] 
		public void Init() {}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
		#endregion

		private IModel m_model = null;
		private int m_noAdmitCount = 0;
		private int m_admitCount = 0;
		public ServerTester() {}

		[TestMethod]
		public void TestServerBasics(){

			m_noAdmitCount = 0;
			m_admitCount = 0;

			m_model = new Model();
            ( (Model)m_model ).RandomServer = new Randoms.RandomServer(54321, 100);

			ItemSource	patientFactory		= CreatePatientGenerator("Patient_",500,5.0,3.0);
			IServer		receiving			= CreateProcessStep("Receiving",3.0,5.0,8.0);
			ISplitter	evaluation			= CreateBranch("Evaluation",0.20);
			IServer		admit				= CreateProcessStep("Admit",3.0,5.0,9.0);
			IServer		inPatientTreat		= CreateProcessStep("InPatientTreat",300.0,2160.0,7200.0);
			IServer		discharge			= CreateProcessStep("Discharge",3.0,5.0,8.0);
			IServer		outPatientTreat		= CreateProcessStep("OutPatientTreat",300.0,2160.0,7200.0);
			IJoiner		toStreet			= new PushJoiner(m_model,"Push Joiner",Guid.NewGuid(), 2);
			ItemSink	street				= new ItemSink(m_model,"Street",Guid.NewGuid());


            ConnectorFactory.Connect(patientFactory.Output, receiving.Input);
            ConnectorFactory.Connect(receiving.Output, evaluation.Input);
            ConnectorFactory.Connect(evaluation.Outputs[0], admit.Input);
            ConnectorFactory.Connect(admit.Output, inPatientTreat.Input);
            ConnectorFactory.Connect(inPatientTreat.Output, discharge.Input);
            ConnectorFactory.Connect(discharge.Output, toStreet.Inputs[0]);
            ConnectorFactory.Connect(evaluation.Outputs[1], outPatientTreat.Input);
            ConnectorFactory.Connect(outPatientTreat.Output, toStreet.Inputs[1]);
            ConnectorFactory.Connect(toStreet.Output, street.Input);


			evaluation.Outputs[0].PortDataPresented+=new PortDataEvent(Admit_Patient); // Count admitted.
			evaluation.Outputs[1].PortDataPresented+=new PortDataEvent(NoAdmit_Patient); // Count not-admitted.

			inPatientTreat.ServiceBeginning+=new ServiceEvent(Server_ServiceBeginning);
			inPatientTreat.ServiceCompleted+=new ServiceEvent(Server_ServiceCompleted);

			outPatientTreat.ServiceBeginning+=new ServiceEvent(Server_ServiceBeginning);
			outPatientTreat.ServiceCompleted+=new ServiceEvent(Server_ServiceCompleted);

			m_model.Start();

			Console.WriteLine("NoAdmit = " + m_noAdmitCount + ", and admitted = " + m_admitCount);

		}

		[TestMethod]
		public void TestBufferedServer(){
			m_noAdmitCount = 0;
			m_admitCount = 0;

			m_model = new Model();
            ( (Model)m_model ).RandomServer = new Randoms.RandomServer(54321, 100);

			ItemSource factory = CreatePatientGenerator("Patient_",25,5.0,3.0);

			NormalDistribution dist = new NormalDistribution(m_model,"SvcDistribution",Guid.NewGuid(),15.0,3.0);
			IPeriodicity per = new Periodicity(dist,Periodicity.Units.Minutes);
			IResourceManager rscPool = new SelfManagingResource(m_model,"RscMgr",Guid.NewGuid(),5.0,5.0,false,true,true);

			BufferedServer bs = new BufferedServer(m_model,"RscSvr",Guid.NewGuid(),per,new IResourceRequest[]{new SimpleResourceRequest(1.0,rscPool)},true,false);
			bs.PlaceInService();
            ConnectorFactory.Connect(factory.Output, bs.Input);

			factory.Output.PortDataPresented+=new PortDataEvent(FactoryOutput_PortDataPresented);
			bs.PreQueue.LevelChangedEvent+=new QueueLevelChangeEvent(Queue_LevelChangedEvent);
			bs.ServiceBeginning+=new ServiceEvent(Server_ServiceBeginning);
			bs.ServiceCompleted+=new ServiceEvent(Server_ServiceCompleted);

			m_model.Start();


		}

		[TestMethod]
		public void TestResourceServer(){
			m_noAdmitCount = 0;
			m_admitCount = 0;

			m_model = new Model();
            ( (Model)m_model ).RandomServer = new Randoms.RandomServer(54321, 100);

			ItemSource factory = CreatePatientGenerator("Patient_",25,5.0,3.0);
			Queue queue = new Queue(m_model,"TestQueue",Guid.NewGuid());

            ConnectorFactory.Connect(factory.Output, queue.Input);

			NormalDistribution dist = new NormalDistribution(m_model,"SvcDistribution",Guid.NewGuid(),15.0,3.0);
			IPeriodicity per = new Periodicity(dist,Periodicity.Units.Minutes);
			ResourceManager rscPool = new ResourceManager(m_model,"RscMgr",Guid.NewGuid());
			for ( int i = 0 ; i < 3 ; i++ ) {
				rscPool.Add(new Resource(m_model,"rsc_"+i,Guid.NewGuid(),1.0,1.0,true,true,true));
			}
			ResourceServer rs = new ResourceServer(m_model,"RscSvr",Guid.NewGuid(),per,new IResourceRequest[]{new SimpleResourceRequest(1.0,rscPool)});
			rs.PlaceInService();
            ConnectorFactory.Connect(queue.Output, rs.Input);

			factory.Output.PortDataPresented+=new PortDataEvent(FactoryOutput_PortDataPresented);
			queue.LevelChangedEvent+=new QueueLevelChangeEvent(Queue_LevelChangedEvent);
			rs.ServiceBeginning+=new ServiceEvent(Server_ServiceBeginning);
			rs.ServiceCompleted+=new ServiceEvent(Server_ServiceCompleted);

			m_model.Start();


		}
		[TestMethod]
		public void TestResourceServerComplexDemands(){
			m_noAdmitCount = 0;
			m_admitCount = 0;

			m_model = new Model();
            ( (Model)m_model ).RandomServer = new Randoms.RandomServer(54321, 100);

			ItemSource factory = CreatePatientGenerator("Patient_",50,5.0,3.0);
			Queue queue = new Queue(m_model,"TestQueue",Guid.NewGuid());

            ConnectorFactory.Connect(factory.Output, queue.Input);

			NormalDistribution dist = new NormalDistribution(m_model,"SvcDistribution",Guid.NewGuid(),240.0,45.0);
			IPeriodicity per = new Periodicity(dist,Periodicity.Units.Minutes);
			SelfManagingResource nursing = new SelfManagingResource(m_model,"Nursing",Guid.NewGuid(),7.0,7.0,false,false,true);
			SelfManagingResource clerks  = new SelfManagingResource(m_model,"Clerks",Guid.NewGuid(),6.0,6.0,false,false,true);
			SelfManagingResource doctors = new SelfManagingResource(m_model,"Doctors",Guid.NewGuid(),2.0,2.0,false,false,true);

			MultiResourceTracker mrt = new MultiResourceTracker(m_model);
			mrt.Filter = ResourceEventRecordFilters.AcquireAndReleaseOnly;
			//mrt.Filter = ResourceTracker.Filters.AllEvents;
			mrt.AddTargets(nursing,clerks,doctors);

			IResourceRequest[] demands = new IResourceRequest[]{
																   new SimpleResourceRequest(.20,nursing),
																   new SimpleResourceRequest(.15,clerks),
																   new SimpleResourceRequest(.05,doctors)
			};

			ResourceServer rs = new ResourceServer(m_model,"RscSvr",Guid.NewGuid(),per,demands);
			rs.PlaceInService();
            ConnectorFactory.Connect(queue.Output, rs.Input);

			factory.Output.PortDataPresented+=new PortDataEvent(FactoryOutput_PortDataPresented);
			queue.LevelChangedEvent+=new QueueLevelChangeEvent(Queue_LevelChangedEvent);
			rs.ServiceBeginning+=new ServiceEvent(Server_ServiceBeginning);
			rs.ServiceCompleted+=new ServiceEvent(Server_ServiceCompleted);

			m_model.Start();

//          string dataFileName = Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + @"Data.csv";
//			System.IO.TextWriter tw = new System.IO.StreamWriter(dataFileName);
//			foreach ( ResourceEventRecord rer in mrt ) {
//				tw.WriteLine(rer.When.ToLongTimeString()+","
//					+rer.Resource.Name+","
//					+rer.Action.ToString()+","
//					+rer.Available	);
//			}
//			tw.Close();
//			System.Diagnostics.Process.Start("excel.exe","\""+dataFileName+"\"");

			Console.WriteLine(new CSVDumper(mrt, new CompoundComparer(ResourceEventRecord.ByAction(false),ResourceEventRecord.ByTime(false))).ToString());

		}

		#region >>> Creation Helper APIs <<<
		private ItemSource CreatePatientGenerator(string rootName, int numPatients, double mean, double stdev ) {
			IDoubleDistribution dist = new NormalDistribution(m_model,"Patient Generator",Guid.NewGuid(),mean,stdev);
            IPeriodicity periodicity = new Periodicity(dist, Periodicity.Units.Minutes);
			bool autoStart = true;
			Ticker ticker = new Ticker(m_model,periodicity,autoStart,numPatients);
			ObjectSource newPatient = new ObjectSource(new Patient.PatientFactory(m_model,"Patient_",Guid.NewGuid()).NewPatient);
            return new ItemSource(m_model, rootName, Guid.NewGuid(), newPatient, ticker);
		}

		private IServer CreateProcessStep(string name, double min, double mean, double max){
			IDoubleDistribution triangle = new TriangularDistribution(m_model,name+"_distribution",Guid.NewGuid(),min,mean,max);
			IPeriodicity svcPeriod = new Periodicity(triangle,Periodicity.Units.Minutes);
            return new BufferedServer(m_model, name, Guid.NewGuid(), svcPeriod, null, true, false);
		}

		private SimpleTwoChoiceBranchBlock CreateBranch(string name, double pctToOut0){
            return new SimpleStochasticTwoChoiceBranchBlock(m_model, name, Guid.NewGuid(), pctToOut0);
		}
		#endregion

		class CSVDumper {
			private System.Collections.IComparer m_comparer;
			private System.Collections.ArrayList m_arer;
			public CSVDumper(IResourceTracker irt, System.Collections.IComparer comparer){
				m_comparer = comparer;
				m_arer = new System.Collections.ArrayList();
				foreach ( ResourceEventRecord rer in irt ) m_arer.Add(rer);
			}
			
			public override string ToString(){
				m_arer.Sort(m_comparer);

				System.IO.StringWriter sw = new System.IO.StringWriter();
				foreach ( ResourceEventRecord rer in m_arer ) sw.WriteLine(rer.Detail());

				return sw.ToString();
			}

		}

		
		class Patient : IModelObject {

			private Patient(IModel model, string name, Guid guid){
                
                InitializeIdentity(model, name, null, guid);
                IMOHelper.RegisterWithModel(this);
            }

			#region Implementation of IModelObject
			private string m_name = null;
			public string Name { get { return m_name; } }
			private string m_description = null;
			/// <summary>
			/// A description of this Patient.
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

			public class PatientFactory : IModelObject {
				private int m_patientNumber = 0;
				public PatientFactory(IModel model, string name, Guid guid){
                    
                    InitializeIdentity(model, name, null, guid);
                    IMOHelper.RegisterWithModel(this);

                }

				public object NewPatient(){
					return new Patient(m_model,m_name + (m_patientNumber++),Guid.NewGuid());
				}

				#region Implementation of IModelObject
				private string m_name = null;
				public string Name { get { return m_name; } }
				private string m_description = null;
				/// <summary>
				/// A description of this Patient Factory.
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
		}
	
		
		private void Admit_Patient(object data, IPort where) {
			m_admitCount++;
		}
		private void NoAdmit_Patient(object data, IPort where) {
			m_noAdmitCount++;
		}

		private void Server_ServiceBeginning(IServer server, object serviceObject) {
			Console.WriteLine(server.Model.Executive.Now + " : " + server.Name + " commencing service for " + ((IModelObject)serviceObject).Name + ".");
		}

		private void Server_ServiceCompleted(IServer server, object serviceObject) {
			Console.WriteLine(server.Model.Executive.Now + " : " + server.Name + " completing service for " + ((IModelObject)serviceObject).Name + ".");
		}

		private void FactoryOutput_PortDataPresented(object data, IPort where) {
			Patient p = (Patient)data;
			Console.WriteLine(p.Model.Executive.Now + " : " + p.Name + " created.");
		}

		private void Queue_LevelChangedEvent(int previous, int current, IQueue queue) {
			Console.WriteLine("Queue level is now " + current);
		}

        public void TestIOOB() {
            throw new NotImplementedException();
        }
    }
}
