using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Highpoint.Sage.SimCore;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace ModelRunner
{
    internal class FulfillmentCenter
    {

        #region Private fields.
        private readonly Stock[] m_stock;
        private readonly SKU[] m_skus;
        private readonly Range<int>[] m_skuGroups;
        // ReSharper disable once NotAccessedField.Local
        private readonly IExecutive m_exec;
        private readonly Replenisher m_replenisher;
        private readonly double m_fcVolume;
        private double m_fcUtilizedVolume;
        private int m_nOrdersReceived;
        private int m_nReplenishmentsReceived;
        private int m_nReplenishmentsRequested;
        private int m_nRescalings; 
        #endregion

        public FulfillmentCenter(int idNum, IExecutive exec, Range<int>[] skuGroups, SKU[] skus, Replenisher replenisher, ModelConstants mc)
        {
            IDNum = idNum;
            m_exec = exec;
            m_skuGroups = skuGroups;
            m_skus = skus;
            m_replenisher = replenisher;
            m_fcVolume = mc.FcVolume;
            m_stock = new Stock[mc.NumberOfSKUs];
        }

        public bool Satisfy(Order order)
        {
            if (m_stock[order.SKU].Quantity < order.Quantity) return false;

            m_stock[order.SKU].Quantity -= order.Quantity;
            m_fcUtilizedVolume -= order.Quantity*m_skus[order.SKU].Volume;
            if (m_stock[order.SKU].Quantity <= m_skus[order.SKU].ReorderLevel)
            {
                if (m_stock[order.SKU].OnOrder == 0 ) Reorder(order.SKU);
            }
            m_nOrdersReceived++;
            return true;
        }

        private void Reorder(int sku)
        {
            Range<int> thisSkuGroup = m_skuGroups[m_skus[sku].Group];
            Shipment replenishment = new Shipment();
            for ( int i = thisSkuGroup.Min; i <= thisSkuGroup.Max ; i++)
            {
                if (m_stock[i].OnOrder == 0)
                {
                    int deficit = m_skus[i].FullLevel - m_stock[i].Quantity;
                    if (deficit > 0) replenishment.Add(new Order(i, deficit));
                    m_stock[i].OnOrder = deficit;

                }
            }
            m_nReplenishmentsRequested++;
            m_replenisher.Satisfy(replenishment, this);
        }

        public void Receive(Shipment shipment, bool updateStats = true)
        {
            foreach (Order order in shipment)
            {
                m_stock[order.SKU].Quantity += order.Quantity;
                m_stock[order.SKU].OnOrder = 0;
                m_fcUtilizedVolume += order.Quantity*m_skus[order.SKU].Volume;
                if (m_fcUtilizedVolume > m_fcVolume)
                {
                    Rescale();
                    if ( updateStats ) m_nRescalings++;
                }
            }
            if ( updateStats ) m_nReplenishmentsReceived++;
        }

        private void Rescale()
        {
            Console.WriteLine("Performing rescale.");
            for(int i = 0 ; i < m_stock.Length ; i++ )
            {
                m_stock[i].Quantity -= 2;
                m_fcUtilizedVolume -= (2*m_skus[i].Volume);
            }
        }

        public int IDNum { get; }

        internal class DataLogger
        {
            private readonly int[] m_skusOfInterest;
            private readonly FulfillmentCenter m_myFulfillmentCenter;
            private readonly int[] m_nOrders;
            private readonly int[] m_nReplenishmentsRequested;
            private readonly int[] m_nReplenishmentsReceived;
            private readonly int[] m_nRescalings;
            private readonly int[] m_usedCapacity;
            private readonly DateTime[] m_TimeStamps;
            private readonly int[,] m_stock;
            private int m_cursor;
            private TimeSpan m_deltaT; 

            public string FCName => m_myFulfillmentCenter.IDNum.ToString();

            public DataLogger(ModelConstants mc, Metronome_Simple metronome, FulfillmentCenter fc)
            {
                m_deltaT = metronome.Period;
                m_skusOfInterest = mc.SKUsOfInterest;
                m_myFulfillmentCenter = fc;

                m_nOrders = new int[mc.NumberOfTimeBins+1];
                m_nReplenishmentsRequested = new int[mc.NumberOfTimeBins + 1];
                m_nReplenishmentsReceived = new int[mc.NumberOfTimeBins + 1];
                m_nRescalings = new int[mc.NumberOfTimeBins + 1];
                m_usedCapacity = new int[mc.NumberOfTimeBins + 1];
                m_stock = new int[mc.NumberOfTimeBins + 1, m_skusOfInterest.Length];
                m_TimeStamps = new DateTime[mc.NumberOfTimeBins + 1];
                m_cursor = 0;

                metronome.TickEvent += Record;

            }

            private void Record(IExecutive exec, object userdata)
            {
                m_nOrders[m_cursor] = m_myFulfillmentCenter.m_nOrdersReceived;
                m_nReplenishmentsReceived[m_cursor] = m_myFulfillmentCenter.m_nReplenishmentsReceived;
                m_nReplenishmentsRequested[m_cursor] = m_myFulfillmentCenter.m_nReplenishmentsRequested;
                m_nRescalings[m_cursor] = m_myFulfillmentCenter.m_nRescalings;
                m_usedCapacity[m_cursor] = (int)m_myFulfillmentCenter.m_fcUtilizedVolume;

                m_usedCapacity[m_cursor] = (int)m_myFulfillmentCenter.m_fcUtilizedVolume;

                for ( int i = 0 ; i < m_skusOfInterest.Length; i++)
                    m_stock[m_cursor, i] = m_myFulfillmentCenter.m_stock[m_skusOfInterest[i]].Quantity;

                m_TimeStamps[m_cursor] = exec.Now;
                m_cursor++;
            }

            internal void DumpData(ModelConstants mc, ExcelPackage package)
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data: FC #" + m_myFulfillmentCenter.IDNum);

                int rowCursor = 1;
                worksheet.Column(1).Width = 20;
                worksheet.Cells[rowCursor, 1].Value = "Total Orders";
                worksheet.Cells[rowCursor++, 2].Value = m_myFulfillmentCenter.m_nOrdersReceived;

                worksheet.Cells[rowCursor, 1].Value = "Replenishments Requested";
                worksheet.Cells[rowCursor++, 2].Value = m_myFulfillmentCenter.m_nReplenishmentsRequested;

                worksheet.Cells[rowCursor, 1].Value = "Replenishments Received";
                worksheet.Cells[rowCursor++, 2].Value = m_myFulfillmentCenter.m_nReplenishmentsReceived;

                worksheet.Cells[rowCursor++, 2].Value = "Time";
                for (int i = 0; i < m_cursor; i++)
                {
                    worksheet.Row(rowCursor).Style.TextRotation = 90;
                    worksheet.Cells[rowCursor, i + 2].Value = m_TimeStamps[i].ToString(CultureInfo.InvariantCulture);
                }
                ExcelRange timeSeries = worksheet.Cells[rowCursor, 2, rowCursor, m_cursor + 1];
                rowCursor++;

                worksheet.Cells[rowCursor, 1].Value = "Total Orders";
                for (int j = 0; j < m_cursor; j++)
                {
                    worksheet.Cells[rowCursor, j + 2].Value = m_nOrders[j];
                }
                rowCursor++;

                worksheet.Cells[rowCursor, 1].Value = "New Orders";
                worksheet.Cells[rowCursor+1, 1].Value = "Orders per Hour";
                for (int j = 0; j < m_cursor; j++)
                {
                    int nNewOrders = m_nOrders[j] - (j == 0 ? 0 : m_nOrders[j - 1]);
                    worksheet.Cells[rowCursor, j + 2].Value = nNewOrders;
                    worksheet.Cells[rowCursor+1, j + 2].Value = nNewOrders / m_deltaT.TotalHours;
                }
                //ExcelRange ordersProcessedSeries = worksheet.Cells[rowCursor, 2, rowCursor, m_cursor + 1];
                ExcelRange ordersProcessedRateSeries = worksheet.Cells[rowCursor+1, 2, rowCursor+1, m_cursor + 1];
                rowCursor+=2;

                worksheet.Cells[rowCursor, 1].Value = "Replenishments Requested";
                worksheet.Cells[rowCursor + 1, 1].Value = "Replenishments Waiting";
                worksheet.Cells[rowCursor + 2, 1].Value = "Replenishments Received";
                for (int j = 0; j < m_cursor; j++)
                {
                    worksheet.Cells[rowCursor, j + 2].Value = m_nReplenishmentsRequested[j];
                    worksheet.Cells[rowCursor + 1, j + 2].Value = m_nReplenishmentsReceived[j] - m_nReplenishmentsRequested[j];
                    worksheet.Cells[rowCursor + 2, j + 2].Value = m_nReplenishmentsReceived[j];
                }
                rowCursor +=3;

                worksheet.Cells[rowCursor, 1].Value = "New Replenishments";
                worksheet.Cells[rowCursor+1, 1].Value = "Per Day (avg)";
                for (int j = 0; j < m_cursor; j++)
                {
                    int nNewReplenishments = m_nReplenishmentsReceived[j] - (j == 0 ? 0 : m_nReplenishmentsReceived[j - 1]);
                    worksheet.Cells[rowCursor, j + 2].Value = nNewReplenishments;
                    double nNewReplenishmentsAvg = m_nReplenishmentsReceived[j] - (j < 10 ? 0 : m_nReplenishmentsReceived[j - 10]);
                    worksheet.Cells[rowCursor+1, j + 2].Value = nNewReplenishmentsAvg / (10*m_deltaT.TotalDays);
                }
                ExcelRange replenishmentRateSeries = worksheet.Cells[rowCursor+1, 2, rowCursor+1, m_cursor + 1];
                rowCursor += 2;

                worksheet.Cells[rowCursor, 1].Value = "Rescalings";
                for (int j = 0; j < m_cursor; j++)
                {
                    worksheet.Cells[rowCursor, j + 2].Value = m_nRescalings[j];
                }
                rowCursor++;

                worksheet.Cells[rowCursor, 1].Value = "Capacity";
                for (int j = 0; j < m_cursor; j++)
                {
                    worksheet.Cells[rowCursor, j + 2].Value = m_usedCapacity[j];
                }
                ExcelRange capacitySeries = worksheet.Cells[rowCursor, 2, rowCursor, m_cursor + 1];
                rowCursor++;

                worksheet.Cells[rowCursor++, 1].Value = "Sample SKUs";

                List< ExcelRange> stockSerieses = new List<ExcelRange>();
                for (int i = 0; i < m_skusOfInterest.Length; i++)
                {
                    worksheet.Cells[rowCursor, 1].Value = m_skusOfInterest[i];
                    for (int j = 0; j < m_cursor; j++)
                    {
                        worksheet.Cells[rowCursor, j + 2].Value = m_stock[j, i];
                    }
                    stockSerieses.Add(worksheet.Cells[rowCursor, 2, rowCursor, m_cursor + 1]);
                    rowCursor++;
                }

                worksheet.View.FreezePanes(1,2);

                worksheet = package.Workbook.Worksheets.Add("Charts: FC #" + m_myFulfillmentCenter.IDNum);
                ExcelLineChart chart = (ExcelLineChart) worksheet.Drawings.AddChart("Orders per Day", eChartType.Line);
                chart.Legend.Remove();
                chart.SetSize(1024, 384);
                chart.SetPosition(0, 0);
                chart.Title.Text = "Orders Processed";
                chart.Series.Add(ordersProcessedRateSeries, timeSeries);

                chart = (ExcelLineChart) worksheet.Drawings.AddChart("Replenishments", eChartType.Line);
                chart.Legend.Remove();
                chart.SetSize(1024, 384);
                chart.SetPosition(0,1025);
                chart.Title.Text = "Replenishments per Day";
                chart.Series.Add(replenishmentRateSeries, timeSeries);

                chart = (ExcelLineChart) worksheet.Drawings.AddChart("Capacity", eChartType.Line);
                chart.Legend.Remove();
                chart.SetSize(1024, 384);
                chart.SetPosition(385, 0);
                chart.Title.Text = "Capacity";
                chart.Series.Add(capacitySeries, timeSeries);

                chart = (ExcelLineChart) worksheet.Drawings.AddChart("Stock", eChartType.Line);
                chart.Legend.Remove();
                chart.SetSize(1024, 384);
                chart.SetPosition(385, 1025);
                chart.Title.Text = "Stock Levels (First 30 Sample SKUs)";
                for (int j = 0; j < 30; j++) chart.Series.Add(stockSerieses[j], timeSeries);

            }
        }
    }
}