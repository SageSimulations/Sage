using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ModelRunner
{
    /// <summary>
    /// Class ModelConstants contains the data that configures the model.
    /// </summary>
    public class ModelConstants
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConstants"/> class.
        /// </summary>
        public ModelConstants()
        {
            if (RunDuration == TimeSpan.Zero)
            {
                Seed = 1024;
                FullLevel = 15;
                ReorderLevel = 2;
                NumberOfFulfillmentCenters = 4;
                NumberOfSKUs = 1000000;
                SKUsOfInterestInterleave = 10240;
                SKU_Volume = new Range<double>(0.05, 0.5);
                NumberOfSKUGroups = 5000;
                OrdersToFCs = AllocationStrategy.AtRandom;
                ReplenishmentTime = new TimeSpanRange(TimeSpan.FromDays(2), TimeSpan.FromDays(4));
                InterOrderPeriod = new TimeSpanRange(TimeSpan.FromMilliseconds(125), TimeSpan.FromMilliseconds(200));
                FcVolume = 3500000;
                StartTime = DateTime.Parse("1/1/2018 00:00:00");
                RunDuration = TimeSpan.FromDays(28);
                NumberOfTimeBins = 512;
                DataDumpPath = "./";
                FileInfo fi = new FileInfo(DataDumpPath + "config.xml");
                if ( !fi.Exists ) SaveTo(fi.Name);
            }

            if (SKUsOfInterest == null ) SKUsOfInterest = new int[0];
            if (SKUsOfInterest.Length == 0 && SKUsOfInterestInterleave > 0)
            {
                SKUsOfInterest = new int[NumberOfSKUs / SKUsOfInterestInterleave + 1];
                int n = 0;
                while (n < SKUsOfInterest.Length) SKUsOfInterest[n] = (n++)*SKUsOfInterestInterleave;
            }
        }
        /// <summary>
        /// The number of Fulfillment Centers
        /// </summary>
        public int NumberOfFulfillmentCenters { get; set; }
        /// <summary>
        /// The number of SKUs represented in the model.
        /// </summary>
        public int NumberOfSKUs { get; set; }
        /// <summary>
        /// The interleave between SKUs of interest. (i.e. "Capture every n-th SKU's data.")
        /// Zero means no SKUs are of interest. If SKUsOfInterest is defined, then this is
        /// ignored.
        /// </summary>
        public int SKUsOfInterestInterleave { get; set; }
        /// <summary>
        /// The specific SKUs whose data are to be captured.
        /// </summary>
        public int[] SKUsOfInterest { get; set; }

        /// <summary>
        /// The volume assigned to an individual SKU item.
        /// </summary>
        public Range<double> SKU_Volume { get; set; }
        /// <summary>
        /// The number of sku groups
        /// </summary>
        public int NumberOfSKUGroups { get; set; }
        /// <summary>
        /// The strategy that the market uses in choosing the Fulfillment Center to send the next order to.
        /// </summary>
        public AllocationStrategy OrdersToFCs { get; set; }

        /// <summary>
        /// The replenishment time is how long it will take to ship a restocking order to a fulfillment center.
        /// </summary>
        public TimeSpanRange ReplenishmentTime { get; set; }

        // 300-500 orders per minute is 5-8 orders per second.
        public TimeSpanRange InterOrderPeriod { get; set; }
        /// <summary>
        /// Gets the volume of a fulfillment center.
        /// </summary>
        /// <value>The fc volume.</value>
        public double FcVolume { get; set; }
        /// <summary>
        /// Gets the start time for the simulation.
        /// </summary>
        /// <value>The start time.</value>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// Gets the duration of the simulation run.
        /// </summary>
        /// <value>The duration of the run.</value>
        [XmlElement(Type = typeof(Highpoint.Sage.Utility.XmlTimeSpan))]
        public TimeSpan RunDuration { get; set; }
        /// <summary>
        /// Gets the number of times that stock and other periodically-captured levels are recorded.
        /// </summary>
        /// <value>The number of time bins.</value>
        public int NumberOfTimeBins { get; set; }

        /// <summary>
        /// Gets the path to which result data will be written.
        /// </summary>
        /// <value>The data dump path.</value>
        public string DataDumpPath { get; set; }

        /// <summary>
        /// Gets or sets the random seed that will drive this simulation.
        /// </summary>
        /// <value>The seed.</value>
        public ulong Seed { get; set; }
        /// <summary>
        /// Gets or sets the full stock level to which reorders are done.
        /// </summary>
        /// <value>The full level.</value>
        public int FullLevel { get; set; }
        /// <summary>
        /// Gets or sets the stock level that triggers a reorder.
        /// </summary>
        /// <value>The reorder level.</value>
        public int ReorderLevel { get; set; }
        public void DumpData(ExcelWorksheet worksheet, ref int row)
        {
            worksheet.Column(1).Width = 24;
            worksheet.Column(2).Width = 11;
            worksheet.Column(3).Width = 11;

            worksheet.Cells[row, 1].Value = "Random Seed:";
            worksheet.Cells[row++, 2].Value = Seed;

            worksheet.Cells[row, 1].Value = "Run Date (World)";
            worksheet.Cells[row++,  2].Value = DateTime.Now.ToShortDateString();

            worksheet.Cells[row, 1].Value = "Run Time (World)";
            worksheet.Cells[row++,  2].Value = DateTime.Now.ToShortTimeString();
            row++;

            worksheet.Cells[row, 1].Value = "Model Start:";
            worksheet.Cells[row++, 2].Value = StartTime.ToString(CultureInfo.InvariantCulture);

            worksheet.Cells[row, 1].Value = "Model Duration:";
            worksheet.Cells[row,  2].Value = RunDuration.TotalDays;
            worksheet.Cells[row++, 3].Value = "days";

            worksheet.Cells[row, 1].Value = "Number of Data Captures:";
            worksheet.Cells[row++,  2].Value = NumberOfTimeBins;
            row++;

            worksheet.Cells[row, 1].Value = "Fulfillment Ctrs:";
            worksheet.Cells[row++,  2].Value = NumberOfFulfillmentCenters;

            worksheet.Cells[row, 1].Value = "FC Capacity:";
            worksheet.Cells[row, 2].Value = FcVolume;
            worksheet.Cells[row++, 3].Value = "cubic units";

            worksheet.Cells[row, 1].Value = "# of SKUs:";
            worksheet.Cells[row++, 2].Value = NumberOfSKUs;

            worksheet.Cells[row, 1].Value = "SKU Groups:";
            worksheet.Cells[row++, 2].Value = NumberOfSKUGroups;
            row++;

            worksheet.Cells[row, 1].Value = "SKU Reorder @:";
            worksheet.Cells[row++, 2].Value = ReorderLevel;
            worksheet.Cells[row, 1].Value = "SKU Reorder to:";
            worksheet.Cells[row++, 2].Value = FullLevel;
            row++;

            worksheet.Cells[row, 2].Value = "Min";
            worksheet.Cells[row++, 3].Value = "Max";
            worksheet.Cells[row, 1].Value = "Replenishment (days)";
            worksheet.Cells[row, 2].Value = ReplenishmentTime.Min;
            worksheet.Cells[row++, 3].Value = ReplenishmentTime.Max;
            worksheet.Cells[row, 1].Value = "Orders (per minute)";
            worksheet.Cells[row, 2].Value = 1.0 / InterOrderPeriod.Max.TotalMinutes;
            worksheet.Cells[row++, 3].Value = 1.0 / InterOrderPeriod.Min.TotalMinutes;
            worksheet.Cells[row, 1].Value = "SKU Volume";
            worksheet.Cells[row, 2].Value = SKU_Volume.Min;
            worksheet.Cells[row++, 3].Value = SKU_Volume.Max;

            using (ExcelRange r = worksheet.Cells[row-3,2,row-1,3])
            {
                r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

        }
        public static ModelConstants LoadFrom(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                return (ModelConstants)new XmlSerializer(typeof(ModelConstants)).Deserialize(XmlReader.Create(fs));
            }
        }

        public void SaveTo(string saveToFileName)
        {
            using (FileStream fs = new FileStream(saveToFileName, FileMode.OpenOrCreate))
            {
                new XmlSerializer(typeof(ModelConstants)).Serialize(XmlWriter.Create(fs), this);
            }
        }
    }
}