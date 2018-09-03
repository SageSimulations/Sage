using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Highpoint.Sage.SimCore.Parallel;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using OfficeOpenXml;
using InProcParallelLib;
// ReSharper disable CoVariantArrayConversion

namespace ParallelSimSandbox
{
    public static class Program
    {
        public static void Main(string[] args)
        {
#pragma warning disable 162 // Unreachable code.
            if (false)
            //if (true)
            {
                new ParallelSimTest.CoprocessingTester().TestCoprocessorInterleaving();
            }
            else
            {
                int howMany = TotallyBogusConfigMechanism.HowMany;
                if (args?.Length > 0) howMany = int.Parse(args[0]);
                RunMarketsAndFactoriesInParallel(howMany);
            }
#pragma warning restore 162 // Unreachable code.

        }

        public static void RunMarketsAndFactoriesInParallel(int howMany)
        {
            IRandomChannel randoms = TotallyBogusConfigMechanism.RANDOM_SERVER.GetRandomChannel();

            Console.WriteLine("Creating {0} executives.", howMany);
            IParallelExec[] execs = new IParallelExec[howMany];
            for (int i = 0; i < howMany; i++)
            {
                execs[i] = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
                execs[i].Name = string.Format("Executive #{0}", i);
                execs[i].SetStartTime(TotallyBogusConfigMechanism.StartTime);
            }
            ClockTracker clockTracker = new ClockTracker(execs,100000);


            #region Set up all SKU-centric data.
            Console.WriteLine("Setting up catalog of {0} SKUs.", TotallyBogusConfigMechanism.NumberOfSKUs);
            SKU[] skus = new SKU[TotallyBogusConfigMechanism.NumberOfSKUs];
            int skusPerGroup = TotallyBogusConfigMechanism.NumberOfSKUs / TotallyBogusConfigMechanism.NumberOfSKUGroups;
            for (int skuID = 0; skuID < skus.Length; skuID++)
            {
                int skuGroup = skuID / skusPerGroup;
                double skuVolume =  randoms.NextDouble(TotallyBogusConfigMechanism.SKUMinVolume,TotallyBogusConfigMechanism.SKUMaxVolume);
                skus[skuID] = new SKU(skuID, skuGroup, skuVolume);
            }
            #endregion

            #region Set up SKU groups' data.
            Console.WriteLine("Setting up catalog of {0} SKU groups.", TotallyBogusConfigMechanism.NumberOfSKUGroups);
            Range<int>[] skuGroups = new Range<int>[TotallyBogusConfigMechanism.NumberOfSKUGroups];
            for (int skuGroupId = 0; skuGroupId < skuGroups.Length; skuGroupId++)
            {
                skuGroups[skuGroupId] = new Range<int>(skuGroupId * skusPerGroup, Math.Min(((skuGroupId + 1) * skusPerGroup) - 1, TotallyBogusConfigMechanism.NumberOfSKUs - 1));
            }
            #endregion

            Console.WriteLine("Setting up {0} factories.", howMany);
            Factory[] factories = new Factory[howMany];
            for (int i = 0; i < howMany; i++)
            {
                factories[i] = new Factory(execs[i], string.Format("Factory {0}", i), Guid.NewGuid());
            }

            Console.WriteLine("Setting up {0} fulfillment centers.", howMany);
            FulfillmentCenter[] fcs = new FulfillmentCenter[howMany];
            for (int i = 0; i < howMany; i++)
            {
                fcs[i] = new FulfillmentCenter(execs[i], string.Format("Fulfillment Center {0}", i), Guid.NewGuid());
            }

            Console.WriteLine("Setting up {0} markets.", howMany);
            Market[] markets = new Market[howMany];
            for (int i = 0; i < howMany; i++)

            {
                markets[i] = new Market(execs[i], string.Format("Market {0}", (char)((char)'A'+i)), Guid.NewGuid());
            }

            Console.WriteLine("Initializing {0} factories.", howMany);
            foreach (Factory factory in factories) factory.Initialize(skus);
            Console.WriteLine("Initializing {0} fulfillment centers.", howMany);
            foreach (FulfillmentCenter fulfillmentCenter in fcs)
                fulfillmentCenter.Initialize(skus, skuGroups, factories, new FulfillmentCenterConfig());
            int targetWhichFc = 0;
            Console.WriteLine("Initializing {0} markets.", howMany);
            foreach (Market m in markets) m.Initialize(fcs, skus, howMany, targetWhichFc++);

            DateTime start = DateTime.Now;
            CoExecutor.CoStart(execs, TotallyBogusConfigMechanism.EndTime);
            DateTime finish = DateTime.Now;
            Console.WriteLine("All threads complete in {0} seconds.", ((TimeSpan) (finish - start)).TotalSeconds);

            FileInfo newFile = new FileInfo("Data.xlsx");
            if ( newFile.Exists ) newFile.Delete();

            List<int> sampleSkus = new List<int>();
            for (   int sku = 0;
                    sku < TotallyBogusConfigMechanism.NumberOfSKUs;
                    sku += (TotallyBogusConfigMechanism.NumberOfSKUs / 10))
            {
                sampleSkus.Add(sku);
            }

            List<FulfillmentCenter> sampleFCs= new List<FulfillmentCenter>();
            for (   int sfc = 0;
                    sfc < TotallyBogusConfigMechanism.HowMany;
                    sfc += (TotallyBogusConfigMechanism.HowMany / 3))
            {
                sampleFCs.Add(fcs[sfc]);
            }



            using (ExcelPackage xlPackage = new ExcelPackage(newFile))
            {
                ExcelWorksheet smplNvtry = xlPackage.Workbook.Worksheets.Add("Sample Inventories");

                int c = 1;
                int r = 1;
                foreach (var fulfillmentCenter in sampleFCs)
                {
                    smplNvtry.Cells[r++, c].Value = fulfillmentCenter.Name;

                    for (int dc = 0 ; dc < sampleSkus.Count ; dc++ )
                    {
                        int tmpC = c + (2*dc);
                        int sku = sampleSkus[dc];
                        smplNvtry.Cells[r, tmpC].Value = string.Format("SKU {0}", sku);
                        TracedValue<int> inventory = fulfillmentCenter.GetInventory(sku);
                        for (int dr = 0; dr < inventory.Length; dr++)
                        {
                            smplNvtry.Cells[r + dr +1 , tmpC].Value = inventory.GetDateTime(dr).ToString();
                            smplNvtry.Cells[r + dr + 1, tmpC +1].Value = inventory.GetValue(dr);
                        }
                    }
                    r = 1;
                    c += (2*sampleSkus.Count);
                }
                xlPackage.Save();
            }
        }
    }
}
