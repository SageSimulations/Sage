/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.Resources;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Demo.Resources
{
    namespace Basic
    {
        public static class ServicePoolExample
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(@"This demo shows how a bank teller simulation might treat tellers as
a resource. A customer generation method registers events to creates customers
with a certain interarrival time. The customer is created on its own thread
(see the ExecEventType.Detachable parameter to RequestEvent) and immediately 
requests a teller from the teller pool. If a teller is unavailable, the thread 
blocks, awaiting an available teller. When the teller is granted, it declares
itself busy, services the customer for the next random service time, and
declares itself idle. When the service is complete, the customer releases the
resource, and is finished.

Note that we show the use of setting the start time in the executive, and make
use of the EnumStateMachine to track states for tellers. Also, we do not make
Customers implement IModelObject. No one will need to find them, a unique Guid
is unnecessary, and they do not need names. Tellers derive from Resource, which
is an IModelObject.

This demo declares the following classes:
DemoModel     : Simple Model class without services. Creates and populates
                TellerPool, and registers events to create customers.
Customer      : On creation, acquires teller, calls teller.Service(this) and
                subsequently releases the teller.
Teller        : Manages a state machine to track its utilization fraction,
                and services the customer, with a state change before and after.
TellerState   : An enum, Idle and Busy.
TellerPool    : A plain old Highpoint.Sage.Resources.ResourceManager. That's it.
TellerRequest : A resource request that asks for one teller, any teller.
")]
            public static void Run()
            {
                int nCustomers = 100;
                IDoubleDistribution interarrivalTime = new ExponentialDistribution(3, 5);// new PoissonDistribution(6);
                int nTellers = 5;
                IDoubleDistribution serviceTime = new ExponentialDistribution(13, 4);//new PoissonDistribution(7);

                DemoModel m = new DemoModel("Demo Model", nCustomers, interarrivalTime, nTellers, serviceTime);
                m.Initialize();

                DateTime startDateTime = DateTime.Parse("Fri, 15 Jul 2016 09:00:00");
                m.Executive.SetStartTime(startDateTime);
                m.Start();

                Console.WriteLine(m.Report());
            }
        }

        public static class ServicePoolExampleWithSynchronousEvents
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(@"This demo is identical to the ServicePoolExample demo, except that rather
than using Detachable events, it accomplishes the same scenario with
Synchronous events. The difference between the two approaches is that the
former makes model code much simpler, and logic easier to follow, but at the 
expense of possibly maintaining a larger number of threads (one per 
running-or-paused entity.) Note that only one thread actually runs at a time, 
with each thread explicitly yielding or being resumed, so the typical sort of
non-deterministic issues of multithreading do not apply here.")]
            public static void Run()
            {
                int nCustomers = 100;
                IDoubleDistribution interarrivalTime = new ExponentialDistribution(3, 5);// new PoissonDistribution(6);
                int nTellers = 5;
                IDoubleDistribution serviceTime = new ExponentialDistribution(13, 4);//new PoissonDistribution(7);

                DemoModelSynchronous m = new DemoModelSynchronous("Demo Model", nCustomers, interarrivalTime, nTellers, serviceTime);
                m.Initialize();

                DateTime startDateTime = DateTime.Parse("Fri, 15 Jul 2016 09:00:00");
                m.Executive.SetStartTime(startDateTime);
                m.Start();

                Console.WriteLine(m.Report());
            }
        }

        class DemoModel : Highpoint.Sage.SimCore.Model
        {
            protected readonly int NCustomers;
            protected readonly IDoubleDistribution InterarrivalTime;
            protected readonly int NTellers;
            protected readonly IDoubleDistribution ServiceTime;
            protected TellerPool TellerPool;

            public DemoModel(string name, int nCustomers, IDoubleDistribution interarrivalTime, int nTellers, IDoubleDistribution serviceTime) : base(name)
            {
                NCustomers = nCustomers;
                InterarrivalTime = interarrivalTime;
                NTellers = nTellers;
                ServiceTime = serviceTime;
            }

            public void Initialize()
            {
                Starting += GenerateTellers;
                Executive.ExecutiveStarted += GenerateCustomers;               
            }

            protected virtual void GenerateTellers(IModel model)
            {
                TellerPool = new TellerPool(this);
                for (int i = 0; i < NTellers; i++) TellerPool.Add(new Teller(this, ServiceTime));
            }

            protected virtual void GenerateCustomers(IExecutive exec)
            {
                DateTime when = Executive.Now;
                for (int i = 0; i < NCustomers; i++)
                {
                    when += TimeSpan.FromMinutes(InterarrivalTime.GetNext());
                    Executive.RequestEvent((exec2, data) => { new Customer().Start(TellerPool); }, when, 0.0, null,
                        ExecEventType.Detachable);
                    Console.WriteLine("Customer {0} will arrive at {1}.", i, when);
                }
            }

            public string Report()
            {
                StringBuilder sb = new StringBuilder();
                foreach (Teller teller in TellerPool.Resources) sb.AppendLine(teller.Report());
                return sb.ToString();
            }
        }

        class Customer 
        {
            internal virtual void Start(TellerPool tellerPool)
            {
                TellerRequest tr = new TellerRequest();
                tellerPool.Acquire(tr, true); // Blocks until a teller is acquired.
                Teller teller = (Teller)tr.ResourceObtained;
                teller.DoService(this); // Blocks while the teller is servicing me.
                // Service is done.
                tr.Release();
            }
        }

        enum TellerState {  Busy, Idle }
        class Teller : Resource
        {
            protected static int TellerNum = 0;
            protected readonly IModel m_model;
            protected readonly IDoubleDistribution ServiceTime;
            protected readonly EnumStateMachine<TellerState> TellerState;

            public Teller(IModel model, IDoubleDistribution serviceTime) 
                : base(model, $"Teller_{TellerNum++:D3}", Guid.NewGuid(), 1, 1, true, true, true)
            {
                m_model = model;
                ServiceTime = serviceTime;
                TellerState = new EnumStateMachine<TellerState>(model.Executive,Basic.TellerState.Idle, trackTransitions:false);
            }

            internal string Report()
            {
                TimeSpan idleDuration = TellerState.TimeSpentInState(Basic.TellerState.Idle);
                TimeSpan busyDuration = TellerState.TimeSpentInState(Basic.TellerState.Busy);
                return string.Format("{0} spent {1:c} idle, and {2:c} busy, for a total of {3:c}.",
                    Name, idleDuration, busyDuration, idleDuration + busyDuration);
            }

            internal void DoService(Customer customer)
            {
                TellerState.ToState(Basic.TellerState.Busy);
                m_model.Executive.CurrentEventController.SuspendFor(TimeSpan.FromMinutes(ServiceTime.GetNext()));
                TellerState.ToState(Basic.TellerState.Idle);
            }
        }

        class TellerPool : ResourceManager
        {
            public TellerPool(IModel model) 
                : base(model, "Teller Pool", Guid.NewGuid(), priorityEnabled: false)
            {
            }
        }

        class TellerRequest : ResourceRequest
        {
            public TellerRequest() : base(1.0){} // Only ever need one teller.

            public override double GetScore(IResource resource) => double.MaxValue; // Any teller is just fine - no other one could be better.

            protected override ResourceRequestSource GetDefaultReplicator()
            {
                return () => new SimpleResourceRequest(QuantityDesired) { DefaultResourceManager = DefaultResourceManager };
            }
        }

        class DemoModelSynchronous : DemoModel
        {
            public DemoModelSynchronous(string name, int nCustomers, IDoubleDistribution interarrivalTime, int nTellers, IDoubleDistribution serviceTime) 
                : base(name, nCustomers, interarrivalTime, nTellers, serviceTime) { }

            protected override void GenerateTellers(IModel model)
            {
                TellerPool = new TellerPool(this);
                for (int i = 0; i < NTellers; i++) TellerPool.Add(new TellerSynchronous(this, ServiceTime));
            }
            protected override void GenerateCustomers(IExecutive exec)
            {
                DateTime when = Executive.Now;
                for (int i = 0; i < NCustomers; i++)
                {
                    when += TimeSpan.FromMinutes(InterarrivalTime.GetNext());
                    Executive.RequestEvent((exec2, data) => { new CustomerSynchronous().Start(TellerPool); }, when, 0.0, null,
                        ExecEventType.Detachable);
                    Console.WriteLine("Synchronous Customer {0} will arrive at {1}.", i, when);
                }
            }
        }

        class TellerSynchronous : Teller
        {
            public TellerSynchronous(IModel model, IDoubleDistribution serviceTime) : base(model, serviceTime){}

            internal void StartServiceSynchronous(CustomerSynchronous customer)
            {
                TellerState.ToState(Basic.TellerState.Busy);
                DateTime completeWhen = m_model.Executive.Now + TimeSpan.FromMinutes(ServiceTime.GetNext());
                m_model.Executive.RequestEvent(CompleteServiceSynchronous, completeWhen, customer); // Using userData to pass the customer to the event recipient.
                Console.WriteLine("{0} : {1} starting service of customer {2}.", m_model.Executive.Now, Name, customer.GetHashCode());
            }

            private void CompleteServiceSynchronous(IExecutive exec, object userdata)
            {
                CustomerSynchronous customer = (CustomerSynchronous)userdata;
                TellerState.ToState(Basic.TellerState.Idle);
                customer.FinishService();
                Console.WriteLine("{0} : {1} finishing service of customer {2}.", m_model.Executive.Now, Name, customer.GetHashCode());
            }
        }

        class CustomerSynchronous : Customer
        {
            private TellerRequest m_tellerRequest;
            private TellerPool m_tellerPool;

            internal override void Start(TellerPool tellerPool)
            {
                m_tellerPool = tellerPool;
                m_tellerRequest = new TellerRequest();
                if (!m_tellerPool.Acquire(m_tellerRequest, false))
                {
                    m_tellerPool.ResourceReleased += TellerPoolOnResourceReleased;
                    // We might use this, too, if we were adding and removing tellers dynamically.
                    //m_tellerPool.ResourceAdded += TellerPoolOnResourceAdded;
                }
                else
                {
                    StartService();
                }

            }

            private void TellerPoolOnResourceReleased(IResourceRequest irr, IResource resource)
            {
                if (m_tellerPool.Acquire(m_tellerRequest, false))
                {
                    m_tellerPool.ResourceReleased -= TellerPoolOnResourceReleased;
                }
            }

            internal void StartService()
            {
                TellerSynchronous teller = (TellerSynchronous)m_tellerRequest.ResourceObtained;
                teller.StartServiceSynchronous(this);
            }

            internal void FinishService()
            {
                // Service is done.
                m_tellerRequest.Release(); // The 'teller' resource is released back into the pool.
            }
        }
    }

    namespace Advanced
    {
        public static class OptimalResourceAcquisition
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(
                @"This demo shows how a bank teller simulation might treat tellers as
a resource. A customer generation method registers events to creates customers
with a certain interarrival time. The customer is created on its own thread
(see the ExecEventType.Detachable parameter to RequestEvent) and immediately 
requests a teller from the teller pool. If a teller is unavailable, the thread 
blocks, awaiting an available teller. When the teller is granted, it declares
itself busy, services the customer for the next random service time, and
declares itself idle. When the service is complete, the customer releases the
resource, and is finished.

Note that we show the use of setting the start time in the executive, and make
use of the EnumStateMachine to track states for tellers. Also, we do not make
Customers implement IModelObject. No one will need to find them, a unique Guid
is unnecessary, and they do not need names. Tellers derive from Resource, which
is an IModelObject.

This demo declares the following classes:
DemoModel     : Simple Model class without services. Creates and populates
                TellerPool, and registers events to create customers.
Customer      : On creation, acquires teller, calls teller.Service(this) and
                subsequently releases the teller.
Teller        : Manages a state machine to track its utilization fraction,
                and services the customer, with a state change before and after.
TellerState   : An enum, Idle and Busy.
TellerPool    : A plain old Highpoint.Sage.Resources.ResourceManager. That's it.
TellerRequest : A resource request that asks for one teller, any teller.
")]
            public static void Run()
            {
                Highpoint.Sage.SimCore.Model model = new Highpoint.Sage.SimCore.Model();
                MotorPool mp = new MotorPool(model);
                foreach (int passengerCapacity in new[] {1,3,4,7,9})
                {
                    mp.Add(new Vehicle(model,passengerCapacity));
                }
                Console.WriteLine("Stocked a motor pool with {0} seat vehicles.\r\n", 
                    StringOperations.ToCommasAndAndedList(mp.Resources.Cast<Vehicle>(),
                    vehicle => vehicle.PassengerCapacity.ToString()));

                foreach (int[] requisitions in new[] {new[] {1, 3, 4}, new[] {2, 5, 7}, new[] {1, 5, 8}, new[] { 8, 1, 6 }, new[] { 8, 7, 6 } , new[] { 1, 3, 4, 7, 8, 9 } })
                {
                    Console.WriteLine("\r\nTest:");
                    List<VehicleRequest> requests = requisitions.Select(requisition => new VehicleRequest(requisition)).ToList();

                    foreach (VehicleRequest vehicleRequest in requests)
                    {
                        Console.Write("Requested a {0} seat vehicle. ", vehicleRequest.SeatsNeeded);
                        if (vehicleRequest.Acquire(mp, false))
                        {
                            Console.WriteLine(" Got a vehicle with {0} seats.", ((Vehicle)vehicleRequest.ResourceObtained).PassengerCapacity);
                        }
                        else
                        {
                            Console.WriteLine(" Motor pool had nothing satisfactory.");
                        }
                    }

                    foreach (VehicleRequest vehicleRequest in requests.Where(n=>n.ResourceObtained != null)) vehicleRequest.Release();
                }
            }
        }
    }

    internal class MotorPool : ResourceManager
    {
        public MotorPool(IModel model)
            : base(model, "Teller Pool", Guid.NewGuid(), priorityEnabled: false) {}
    }

    internal class Vehicle : Resource
    {
        public int PassengerCapacity { get; set; }
        protected static int VehicleNum = 0;

        public Vehicle(IModel model, int passengerCapacity)
            : base(model, $"Vehicle_{VehicleNum++:D3}", Guid.NewGuid(), 1, 1, true, true, true)
        {
            PassengerCapacity = passengerCapacity;
        }
    }

    internal class VehicleRequest : ResourceRequest
    {
        public int SeatsNeeded { get; set; }

        public VehicleRequest(int seatsNeeded) : base(1)
        {
            SeatsNeeded = seatsNeeded;
        }

        public override double GetScore(IResource resource)
        {
            Vehicle vehicle = (Vehicle) resource;
            // We will choose the one with the highest score.
            if (vehicle.PassengerCapacity < SeatsNeeded) return double.MinValue; // Magic number signifying "totally unacceptable."
            return SeatsNeeded - vehicle.PassengerCapacity; // Highest number will be least number of empty seats.
        }

        protected override ResourceRequestSource GetDefaultReplicator()
        {
            return () => new VehicleRequest(SeatsNeeded) { DefaultResourceManager = DefaultResourceManager };
        }
    }
}