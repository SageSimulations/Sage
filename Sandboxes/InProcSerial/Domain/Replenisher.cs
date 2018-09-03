using System;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using OfficeOpenXml;

namespace ModelRunner
{
    internal class Replenisher
    {
        #region Private fields.
        private readonly IExecutive m_exec;
        private readonly TimeSpanRange m_replenishmentTimeRange;
        private readonly IRandomChannel m_replenishmentTimeRNGChannel;
        private int m_nReplenishments; 
        #endregion

        public Replenisher(IExecutive exec, RandomServer randoms, TimeSpanRange replenishmentTime)
        {
            m_exec = exec;
            m_replenishmentTimeRange = replenishmentTime;
            m_replenishmentTimeRNGChannel = randoms.GetRandomChannel();
        }

        public bool Satisfy(Shipment request, FulfillmentCenter target)
        {
            m_exec.RequestEvent((exec, data) => { target.Receive(request);
                                                    m_nReplenishments++;
            }, m_exec.Now + TimeToReplenish());
            // Note: Shipment data is sent so that the replenisher can eventually 
            // vary its response time based on the size of the shipment.
            return true;
        }

        private TimeSpan TimeToReplenish()
        {
            long nTicksToWait = (long)(m_replenishmentTimeRNGChannel.NextDouble() *
                                       (m_replenishmentTimeRange.Max.Ticks - m_replenishmentTimeRange.Min.Ticks));
            return m_replenishmentTimeRange.Min + TimeSpan.FromTicks(nTicksToWait);
        }

        public void DumpData(ExcelWorksheet worksheet, ref int row)
        {
            worksheet.Cells[row, 1].Value = "# FC Replenishments:";
            worksheet.Cells[row, 2].Value = m_nReplenishments;
            row++;
        }

    }
}