using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;

namespace ParallelSimSandbox.Lib
{
    public struct SKU
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SKU"/> struct. 
        /// </summary>
        /// <param name="skuId">The SKU identifier.</param>
        /// <param name="skuGroup">The SKU group.</param>
        /// <param name="volume">The volume of one element of this SKU.</param>
        public SKU(int skuId, int skuGroup, double volume)
        {
            ID = skuId;
            Volume = volume;
            MemberOfSKUGroup = skuGroup;
        }
        public int ID { get; }
        public double Volume { get; }
        public int MemberOfSKUGroup { get; }
    }

    /// <summary>
    /// Struct LineItem represents an element of an order ("I want this") or a shipment ("shipment contains this.")
    /// </summary>
    public struct LineItem
    {
        public LineItem(int sku_ID, int quantity)
        {
            SKU_ID = sku_ID;
            Quantity = quantity;
        }

        public int SKU_ID { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Interface IHasIDNumber is implemented by anything (currently order and shipment) that is known by an ID.
    /// </summary>
    public interface IHasIDNumber
    {
        int ID_Number { get; }
    }

    public interface IOrder : IList<LineItem>, IHasIDNumber
    {
    }

    public interface IShipment : IList<LineItem>, IHasIDNumber
    {
    }

    /// <summary>
    /// Class SimpleShipment is the initial default implementation of IShipment, for testing and exploration purposes.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.List{LineItem}" />
    /// <seealso cref="IShipment" />
    public class SimpleShipment : List<LineItem>, IShipment
    {
        private static int _idCursor = 0;
        public SimpleShipment(IOrder @order = null)
        {
            ID_Number = Interlocked.Increment(ref _idCursor);
            if (order != null)
            {
                foreach (LineItem lineItem in order)
                {
                    Add(lineItem);
                }
            }
        }

        public int ID_Number { get; }
    }

    /// <summary>
    /// Class SimpleOrder is the initial default implementation of IOrder, for testing and exploration purposes.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.List{LineItem}" />
    /// <seealso cref="IOrder" />
    public class SimpleOrder : List<LineItem>, IOrder
    {
        private static int _idCursor = 0;

        public SimpleOrder()
        {
            ID_Number = Interlocked.Increment(ref _idCursor);
        }

        public void Add(int sku_ID, int quantity)
        {
            Add(new LineItem(sku_ID, quantity));
        }

        public int ID_Number { get; }
    }

    /// <summary>
    /// Interface ISupplyChainLayer describes the ability to receive orders and shipments.
    /// </summary>
    /// <seealso cref="IHasIdentity" />
    public interface ISupplyChainLayer : IHasIdentity
    {
        void Receive(IOrder order, ISupplyChainLayer fromWhom, IExecutive callersExec);
        void Receive(IShipment shipment, ISupplyChainLayer fromWhom, IExecutive callersExec);
    }

    /// <summary>
    /// Class Factory is an implementation of ISupplyChainLayer that receives orders, and subsequently sends shipments.
    /// </summary>
    /// <seealso cref="ISupplyChainLayer" />
    public class Factory : ISupplyChainLayer
    {
        private readonly IExecutive m_myExec;
        private IRandomChannel m_random;
        private SKU[] m_catalog;

        public Factory(IExecutive myExec, string name, Guid guid, string description = null)
        {
            m_myExec = myExec;
            Name = name;
            Guid = guid;
            Description = description;
            m_random = TotallyBogusConfigMechanism.RANDOM_SERVER.GetRandomChannel();
        }

        public void Initialize(SKU[] catalog)
        {
            m_catalog = catalog;
        }

        public void Receive(IOrder order, ISupplyChainLayer fromWhom, IExecutive callersExec)
        {
            //if ( TotallyBogusConfigMechanism.DumpToConsole) Console.WriteLine("{0} : {1} received order {2} from {3}.", m_myExec.Now, Name, order.ID_Number, fromWhom.Name);
            TimeSpan fulfillmentPeriod = FulfillmentPeriod();

            try
            {
                m_myExec.RequestEvent((exec, userdata) => FillOrder(order, fromWhom), m_myExec.Now + fulfillmentPeriod);
            }
            catch ( Exception e)
            {
                // GOOBER
                Console.WriteLine(e.Message);
            }
        }

        public void Receive(IShipment shipment, ISupplyChainLayer fromWhom, IExecutive callersExec)
        {
            throw new NotImplementedException();
        }

        private void FillOrder(IOrder order, ISupplyChainLayer toWhom)
        {
            if (TotallyBogusConfigMechanism.DumpToConsole) Console.WriteLine("{0} : {1} is sending shipment {2} to {3}.", m_myExec.Now, Name, order.ID_Number, toWhom.Name);
            IShipment shipment = new SimpleShipment(order);
            toWhom.Receive(shipment, this, m_myExec);
        }

        private TimeSpan FulfillmentPeriod()
        {
            return
                TimeSpan.FromHours(m_random.NextDouble(TotallyBogusConfigMechanism.FulfillmentPeriodMinHours,
                    TotallyBogusConfigMechanism.FulfillmentPeriodMaxHours));
        }


        public string Name { get; }
        public string Description { get; }
        public Guid Guid { get; }
    }

    /// <summary>
    /// Class FulfillmentCenter receives orders from a market, and fulfills them from a stock that
    /// it maintains by emitting orders to, and accepting shipments from, many factories.
    /// </summary>
    /// <seealso cref="ISupplyChainLayer" />
    public class FulfillmentCenter : ISupplyChainLayer
    {
        private TracedValue<bool>[] m_onOrder;
        private TracedValue<int>[] m_inventory;
        private FulfillmentCenterConfig m_fcConfig;
        private Range<int>[] m_skuGroups;
        private SKU[] m_allSkus;
        private ISupplyChainLayer[] m_suppliers;
        private double m_fcVolumeInUse;
        private TracedValue<double> m_traceOfVolumeInUse;
        private int m_droppedOrders;
        private TracedValue<int> m_traceOfDroppedOrders;
        private IExecutive m_exec;
        //private int m_ndx;

        public FulfillmentCenter(IExecutive exec, string name, Guid guid, string description=null)
        {
            m_exec = exec;
            Name = name;
            //m_ndx = int.Parse(Name.Split(new[] {' '})[2]);
            Guid = guid;
            Description = description;
            m_exec.ExecutiveStarted_SingleShot += M_exec_ExecutiveStarted_SingleShot;
        }

        public void Initialize(SKU[] allSkus, Range<int>[] skuGroups,
            ISupplyChainLayer[] factories, FulfillmentCenterConfig fcConfig)
        {
            m_fcConfig = fcConfig;
            m_skuGroups = skuGroups;
            m_allSkus = allSkus;
            m_suppliers = factories;
            m_inventory = new TracedValue<int>[allSkus.Length];
            m_onOrder = new TracedValue<bool>[allSkus.Length];
            for (int i = 0; i < allSkus.Length; i++)
            {
                int numberOnHand = m_fcConfig.InitialLevelFor(allSkus[i]);
                m_fcVolumeInUse += (numberOnHand*allSkus[i].Volume);
                m_inventory[i] = new TracedValue<int>(m_exec, numberOnHand);
                m_onOrder[i] = new TracedValue<bool>(m_exec, false);
            }
            m_traceOfVolumeInUse = new TracedValue<double>(m_exec, m_fcVolumeInUse);
            m_traceOfDroppedOrders = new TracedValue<int>(m_exec,0);
        }

        public string Name { get; }
        public string Description { get; }
        public Guid Guid { get; }
        /// <summary>
        /// Receives an order from a market.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="fromWhom">From whom.</param>
        /// <param name="callersExecutive">The callers executive.</param>
        public void Receive(IOrder order, ISupplyChainLayer fromWhom, IExecutive callersExecutive)
        {
            //if (TotallyBogusConfigMechanism.DumpToConsole) Console.WriteLine("{0} : {1} received order {2} from {3}.", callersExecutive.Now, Name, order.ID_Number, fromWhom.Name);
            
            IOrder replenishment = null;
            IShipment shipment = null;
            foreach (LineItem lineItem in order)
            {
                TracedValue<int> onHand = m_inventory[lineItem.SKU_ID];
                int available = onHand.Get(callersExecutive);
                int requested = lineItem.Quantity;
                if (available >= requested)
                {
                    // We can fill the order in its entirety.
                    if (shipment == null) shipment = new SimpleShipment();
                    shipment.Add(new LineItem(lineItem.SKU_ID, requested));
                    available -= requested; // We've put the requested into the shipment, they're no longer available.
                    onHand.Set(available, callersExecutive);
                    m_fcVolumeInUse -= requested*m_allSkus[lineItem.SKU_ID].Volume;
                }
                else
                {
                    // shipment remains NULL.
                    // TODO: Need a mechanism for handling an order we cannot fill. (e.g. send it to another FC?)
                    m_droppedOrders++;
                    if (TotallyBogusConfigMechanism.DumpToConsole) Console.WriteLine("{0} : {1} ignored partially fillable line item for {5} of {4} in order {2} from {3}.", callersExecutive.Now, Name, order.ID_Number, fromWhom.Name, lineItem.SKU_ID, lineItem.Quantity);
                }

                int reorderLevel = m_fcConfig.ReorderLevelFor(m_allSkus[lineItem.SKU_ID]);
                bool onOrder = m_onOrder[lineItem.SKU_ID].Get(callersExecutive);

                int skuGroup = 0;
                if (available <= reorderLevel && !onOrder) // Note: There are cases where a second reorder might be warranted. (ReorderLevel > FullStock/2, for example.)
                {
                    // Perform a full SKU Group reorder.
                    if ( replenishment == null ) replenishment = new SimpleOrder();
                    skuGroup = m_allSkus[lineItem.SKU_ID].MemberOfSKUGroup;
                    Range<int> skuGroupRange = m_skuGroups[skuGroup];
                    for (int i = skuGroupRange.Min; i <= skuGroupRange.Max; i++)
                    {
                        available = m_inventory[i].Get(callersExecutive);
                        reorderLevel = m_fcConfig.ReorderLevelFor(m_allSkus[i]);
                        int fullStock = m_fcConfig.FullLevelFor(m_allSkus[i]);
                        onOrder = m_onOrder[i].Get(callersExecutive);
                        if (!onOrder)
                        {
                            replenishment.Add(new LineItem(i, fullStock - available));
                            m_onOrder[i].Set(true, callersExecutive);
                        }
                    }
                }

                if (shipment != null)
                {
                    // Fulfill the order.
                    IShipment tmp = shipment;
                    DateTime when = m_exec.Now + m_fcConfig.FulfillmentDelay;
                    m_exec.RequestEvent((exec, data) => { fromWhom.Receive(tmp, this, m_exec); }, when);
                }

                if (replenishment != null)
                {
                    // Get the factory for that SKU Group.

                    int whichFactory = skuGroup%m_suppliers.Length;
                    m_suppliers[whichFactory].Receive(replenishment, this,m_exec);
                }

            }
            // TODO: Handle partial fills of orders.
            // TODO: Can make this more efficient by syncing once at the beginning?
        }

        /*
            Exec0 is at 1:22:34, and waiting 'til Exec3 (which is at 1:20:33) reaches 1:22:34
            Exec1 is at 1:27:54, and waiting 'til Exec0 (which is at 1:22:34) reaches 1:27:54
            Exec2 is at 1:17:25, and waiting 'til Exec3 (which is at 1:20:33) reaches 1:17:25*
            Exec3 is at 1:20:33, and waiting 'til Exec1 (which is at 1:27:54) reaches 1:20:33*

            * <-- These should return immediately.

            When process running in domain A at time Ta, decides it needs something from domain B, 
            and Domain B is at Tb, where Tb < Ta, it issues a request to domain B on its thread, 
            saying "Dear Domain B : Wait until you reach time X, and then do Y, returning the 
            value from that Action to me."

            That request necessarily blocks Domain A until domain B's local time catches up to it.

            However, if between the time it determines that it needs something from domain B, and 
            the time it calls B to wait until Ta, domain B advances its clock (Tb) past Ta, then 
            the request is as shown in examples above, where it is waiting for a time that has already
            passed.
         */

        public void Receive(IShipment shipment, ISupplyChainLayer fromWhom, IExecutive callersExecutive)
        {
            if (TotallyBogusConfigMechanism.DumpToConsole) Console.WriteLine("{0} : {1} received shipment {2} from {3}.", callersExecutive.Now, Name, shipment.ID_Number, fromWhom.Name);

            // TODO: Want to let this exec rollback or wait, once. Then set all onHands at once.
            foreach (LineItem lineItem in shipment)
            {
                TracedValue<int> onHand = m_inventory[lineItem.SKU_ID];
                onHand.Set(onHand.Get(callersExecutive) + lineItem.Quantity, callersExecutive);
                m_onOrder[lineItem.SKU_ID].Set(false,callersExecutive);

            }
        }

        public TracedValue<int> GetInventory(int ofSKU)
        {
            return m_inventory[ofSKU];
        }

        private void M_exec_ExecutiveStarted_SingleShot(IExecutive exec)
        {
            RecordPeriodicData(m_exec, null);
        }

        private void RecordPeriodicData(IExecutive exec, object userData)
        {
            m_traceOfVolumeInUse.Set(m_fcVolumeInUse, exec);
            m_traceOfDroppedOrders.Set(m_droppedOrders, exec);
            m_exec.RequestEvent(RecordPeriodicData, m_exec.Now + m_fcConfig.TrackingPeriodicity);
        }
    }
    
    /// <summary>
    /// Class Market emits orders to a fulfillment center at a specified rate.
    /// </summary>
    /// <seealso cref="ISupplyChainLayer" />
    public class Market : ISupplyChainLayer
    {
        private readonly IExecutive m_myExec;
        private ISupplyChainLayer[] m_suppliers;
        private IRandomChannel m_random;
        private SKU[] m_catalog;
        private double m_maxOrderPeriod;
        private double m_minOrderPeriod;
        private int m_keySupplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="Market"/> class.
        /// </summary>
        /// <param name="myExec">The executive under whose Future Event List this market will run.</param>
        /// <param name="name">The name of the market.</param>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="description">A description of this market.</param>
        public Market(IExecutive myExec, string name, Guid guid, string description = null)
        {
            m_myExec = myExec;
            Name = name;
            Guid = guid;
            Description = description;
            m_maxOrderPeriod = double.MaxValue;
            m_minOrderPeriod = double.MaxValue;
        }

        public void Initialize(ISupplyChainLayer[] suppliers, SKU[] catalog, int numberOfMarketSegments, int keySupplier)
        {
            m_suppliers = suppliers;
            m_catalog = catalog;
            m_keySupplier = keySupplier;
            m_random = TotallyBogusConfigMechanism.RANDOM_SERVER.GetRandomChannel();
            m_minOrderPeriod = (1.0 / TotallyBogusConfigMechanism.MarketOrdersPerMinute.Min) * numberOfMarketSegments;
            m_maxOrderPeriod = (1.0 / TotallyBogusConfigMechanism.MarketOrdersPerMinute.Max) * numberOfMarketSegments;
            m_myExec.ExecutiveStarted += exec => exec.RequestEvent(PlaceOrder, TotallyBogusConfigMechanism.StartTime + OrderPeriod());
        }

        /// <summary>
        /// Generates an order and places it with the key supplier (i.e. Fulfillment Center,) then schedules the next order.
        /// </summary>
        /// <param name="exec">The execute.</param>
        /// <param name="userData">The user data.</param>
        private void PlaceOrder(IExecutive exec, object userData)
        {
            SimpleOrder so = new SimpleOrder();
            ISupplyChainLayer target = m_suppliers[m_keySupplier];
            int skuID = m_random.Next(0, m_catalog.Length);
            int quantity = 1;
            so.Add(skuID, quantity);
            if (TotallyBogusConfigMechanism.DumpToConsole) Console.WriteLine("{0} : {1} placing order {2} with {3}.", m_myExec.Now, Name, so.ID_Number, target.Name);
            target.Receive(so, this, m_myExec);
            exec.RequestEvent(PlaceOrder, exec.Now + OrderPeriod());
        }

        /// <summary>
        /// The TimeSpan between orders issued by this market.
        /// </summary>
        /// <returns>TimeSpan.</returns>
        private TimeSpan OrderPeriod()
        {
            return TimeSpan.FromMinutes(m_random.NextDouble(m_minOrderPeriod, m_maxOrderPeriod));
        }

        public void Receive(IOrder order, ISupplyChainLayer fromWhom, IExecutive callersExecutive)
        {
            // Market is the originator of orders. It never receives them.
            throw new NotImplementedException();
        }

        public void Receive(IShipment shipment, ISupplyChainLayer fromWhom, IExecutive callersExecutive)
        {
            if (TotallyBogusConfigMechanism.DumpToConsole) Console.WriteLine("{0} : {1} received shipment {2} from {3}", m_myExec.Now, Name, shipment.ID_Number, fromWhom.Name);
        }

        public string Name { get; }
        public string Description { get; }
        public Guid Guid { get; }
    }
}
