using System;
using System.IO;
using System.Linq;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using OfficeOpenXml;

namespace ModelRunner
{
    internal class MarketSegment
    {
        private readonly Range<int> m_skus;
        private readonly FulfillmentCenter[] m_fcs;
        private readonly IRandomChannel m_orderPeriodicityRNGChannel;
        private readonly IRandomChannel m_skuSelectionChannel;
        private readonly IRandomChannel m_ordersToFcStrategyChannel;
        private readonly TimeSpanRange m_interOrderPeriodRange;
        private readonly AllocationStrategy m_ordersToFcStrategy;
        private int m_nextFc;
        private int m_nOrders;

        public MarketSegment(IExecutive exec, RandomServer randoms, Range<int> skus, FulfillmentCenter[] fcs, TimeSpanRange interOrderPeriodRange, AllocationStrategy ordersToFCStrategy)
        {
            m_skus = skus;
            m_fcs = fcs;
            m_orderPeriodicityRNGChannel = randoms.GetRandomChannel();
            m_skuSelectionChannel = randoms.GetRandomChannel();
            m_interOrderPeriodRange = interOrderPeriodRange;
            m_ordersToFcStrategy = ordersToFCStrategy;
            if (m_ordersToFcStrategy == AllocationStrategy.AtRandom)
            {
                m_ordersToFcStrategyChannel = randoms.GetRandomChannel();
            }
            exec.ExecutiveStarted += executive => executive.RequestEvent(IssueOrder, exec.Now + TimeToNextOrder());
        }

        public MarketSegment(IExecutive exec, FulfillmentCenter[] fcs, RandomServer randoms, ModelConstants mc)
        {
            m_skus = new Range<int>(0, mc.NumberOfSKUs - 1);
            m_fcs = fcs;
            m_orderPeriodicityRNGChannel = randoms.GetRandomChannel();
            m_skuSelectionChannel = randoms.GetRandomChannel();
            m_interOrderPeriodRange = mc.InterOrderPeriod;
            m_ordersToFcStrategy = mc.OrdersToFCs;
            if (m_ordersToFcStrategy == AllocationStrategy.AtRandom)
            {
                m_ordersToFcStrategyChannel = randoms.GetRandomChannel();
            }
            exec.ExecutiveStarted += executive => executive.RequestEvent(IssueOrder, exec.Now + TimeToNextOrder());
        }

        private void IssueOrder(IExecutive exec, object userdata)
        {
            m_nOrders++;

            // Pick a SKU.
            int sku = m_skuSelectionChannel.Next(m_skus.Min, m_skus.Max);

            FulfillmentCenter target = null;
            bool successful;
            bool[] fcUnableToFulfill = new bool[m_fcs.Length]; // All initialize to false.
            do
            {
                // Pick a FC. TODO: Detect when none exist that can fulfill the order.
                switch (m_ordersToFcStrategy)
                {
                    case AllocationStrategy.AtRandom:
                        target = m_fcs[m_ordersToFcStrategyChannel.Next(0, m_fcs.Length)];
                        break;
                    case AllocationStrategy.RoundRobin:
                        target = m_fcs[m_nextFc++];
                        if (m_nextFc > m_fcs.Length) m_nextFc = 0;
                        break;
                }

                // Send the order.
                if (target == null)  throw new NullReferenceException();
                successful = target.Satisfy(new Order(sku));
                fcUnableToFulfill[target.IDNum] = !successful;
            } while (!successful && fcUnableToFulfill.Contains(false));

            if (!successful)
            {
                Console.WriteLine("WE TRIED ALL FULFILLMENT CENTERS AND NO ONE COULD FULFILL AN ORDER FOR {0}.", sku);
            }

            //Console.WriteLine("{0} : Sending order for {1} to {2}.", exec.Now, sku, target.IDNum);
            exec.RequestEvent(IssueOrder, exec.Now + TimeToNextOrder());
        }

        private TimeSpan TimeToNextOrder()
        {
            long nTicksToWait = (long) (m_orderPeriodicityRNGChannel.NextDouble()*
                                        (m_interOrderPeriodRange.Max.Ticks - m_interOrderPeriodRange.Min.Ticks));
            return m_interOrderPeriodRange.Min + TimeSpan.FromTicks(nTicksToWait);
        }

        public void DumpData(ExcelWorksheet worksheet, ref int row)
        {
            worksheet.Cells[row + 1, 1].Value = "# Customer Orders";
            worksheet.Cells[row + 1, 2].Value = m_nOrders;

            row += 2;
        }

    }
}