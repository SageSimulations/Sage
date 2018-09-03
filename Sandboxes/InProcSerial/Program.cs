using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Highpoint.Sage.Randoms;
using ModelRunner;
using OfficeOpenXml;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable InconsistentNaming

// TODO: Separate construction and initialization.

namespace ModelRunner
{
    static class Program
    {
        const string DEFAULT_CFG_FILE = "config.xml";
        private static void Main(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (args.Length > 0)
            {
                foreach (string filename in args)
                {
                    if (File.Exists(filename))
                    {
                        RunModel(ModelConstants.LoadFrom(filename));
                    }
                    else
                    {
                        Console.WriteLine("Unable to find model configuration file \"{0}\".", filename);
                    }
                }
            }
            else if (File.Exists(DEFAULT_CFG_FILE))
            {
                RunModel(ModelConstants.LoadFrom(DEFAULT_CFG_FILE));
            }
            else
            {
                RunModel(new ModelConstants(), saveConfigToFile: true);
            }
        }

        private static void RunModel(ModelConstants mc, bool saveConfigToFile = false)
        {
            RandomServer rs = new RandomServer(mc.Seed);
            DateTime initStart = DateTime.Now; 
            IExecutive exec = ExecFactory.Instance.CreateExecutive(ExecType.SingleThreaded);

            exec.SetStartTime(mc.StartTime);
            exec.RequestEvent((executive, data) => executive.Stop(), mc.StartTime + mc.RunDuration);

            Range<int>[] skuGroups = new Range<int>[mc.NumberOfSKUGroups];
            // Going to make them uniform-sized, for now.
            int skuGrpSize = mc.NumberOfSKUs / mc.NumberOfSKUGroups;

            IRandomChannel SkuVolumeChannel = rs.GetRandomChannel();
            SKU[] skus = new SKU[mc.NumberOfSKUs];
            for (int sku = 0; sku < mc.NumberOfSKUs; sku++)
            {
                int fullLevel = mc.FullLevel;
                int group = sku / skuGrpSize;
                if (sku % skuGrpSize == 0) skuGroups[group] = new Range<int>(sku, sku + skuGrpSize - 1);
                int reorderLevel = mc.ReorderLevel;
                double volume = SkuVolumeChannel.NextDouble(mc.SKU_Volume.Min, mc.SKU_Volume.Max);
                skus[sku] = new SKU()
                {
                    FullLevel = fullLevel,
                    Group = group,
                    ReorderLevel = reorderLevel,
                    Volume = volume
                };
            }

            Replenisher replenisher = new Replenisher(exec, rs, mc.ReplenishmentTime);

            FulfillmentCenter[] fcs = new FulfillmentCenter[mc.NumberOfFulfillmentCenters];
            IRandomChannel initStock = rs.GetRandomChannel();
            for (int fcNum = 0; fcNum < mc.NumberOfFulfillmentCenters; fcNum++)
            {
                fcs[fcNum] = new FulfillmentCenter(fcNum, exec, skuGroups, skus, replenisher, mc);
                Shipment initialShipment = new Shipment();
                // Stock up the fulfillment center for start of model.
                for (int i = 0; i < mc.NumberOfSKUs; i++)
                {
                    int LowInitLevel = (int) (skus[i].ReorderLevel + ((skus[i].FullLevel - skus[i].ReorderLevel)*.25));
                    int stockLevel = initStock.Next(LowInitLevel, skus[i].FullLevel);
                    initialShipment.Add(new Order(i, stockLevel));
                }
                fcs[fcNum].Receive(initialShipment, updateStats: false);
            }

            TimeSpan loggingTick = TimeSpan.FromTicks(mc.RunDuration.Ticks/mc.NumberOfTimeBins);
            Metronome_Simple metronome = 
                Metronome_Simple.CreateMetronome(exec, mc.StartTime, mc.StartTime+mc.RunDuration, loggingTick);
            List<FulfillmentCenter.DataLogger> loggers =
                fcs.Select(fc => new FulfillmentCenter.DataLogger(mc, metronome, fc)).ToList();

            MarketSegment marketSegment = new MarketSegment(exec, fcs, rs, mc);
            DateTime initDone = DateTime.Now;
            exec.Start();
            DateTime runDone = DateTime.Now;

            string dumpFileRootName = mc.DataDumpPath +
                                      Highpoint.Sage.Utility.DateTimeOperations.DtFileString(DateTime.Now);
            FileInfo excelFileName = new FileInfo(dumpFileRootName + ".xlsx");

            using (var package = new ExcelPackage(excelFileName))
            {
                package.Workbook.Properties.Title = "Serial Supply Chain Model Data";
                package.Workbook.Properties.Author = "Pete Bosch";
                package.Workbook.Properties.Comments = "Data from " + DateTime.Now.ToShortDateString() + DateTime.Now.ToShortTimeString() + " model run.";
                package.Workbook.Properties.Company = "Highpoint Software Systems";
                
                // Add a new worksheet to the empty workbook
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Model Level Data");
                int row = 1;
                mc.DumpData(worksheet, ref row);
                marketSegment.DumpData(worksheet, ref row);
                replenisher.DumpData(worksheet,ref row);

                foreach (FulfillmentCenter.DataLogger dataLogger in loggers)
                {
                    dataLogger.DumpData(mc, package);
                }

                package.Save();
            }
            DateTime dumpDone = DateTime.Now;

            Console.WriteLine("Initialization : " + (initDone - initStart));
            Console.WriteLine("Run            : " + (runDone - initDone));
            Console.WriteLine("Dump           : " + (dumpDone - runDone));
            Console.WriteLine("-------------------------------");
            Console.WriteLine("Total          : " + (dumpDone - initStart));

            if (saveConfigToFile) mc.SaveTo(dumpFileRootName + ".xml");

            
        }
    }
}
