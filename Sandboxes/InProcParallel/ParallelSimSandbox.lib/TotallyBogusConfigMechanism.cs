using System;
using Highpoint.Sage.Randoms;

namespace ParallelSimSandbox.Lib
{
    public static class TotallyBogusConfigMechanism
    {
        static TotallyBogusConfigMechanism()
        {
            RANDOM_SERVER = new RandomServer(RandomSeed);
            FulfillmentCenterConfig = new FulfillmentCenterConfig() {FulfillmentCenterVolume = 2000000};
            MarketOrdersPerMinute = new Range<int>(200,500);
        }

        public static int HowMany = 8;
        public static DateTime StartTime = new DateTime(2018, 1, 1);
        public static DateTime EndTime = new DateTime(2018, 1, 2);
        public static readonly RandomServer RANDOM_SERVER;
        public static int FulfillmentPeriodMinHours = 1;
        public static int FulfillmentPeriodMaxHours = 4;
        public static bool DumpToConsole = false;
        public static ulong RandomSeed = 12345;
        public static FulfillmentCenterConfig FulfillmentCenterConfig;
        public static int NumberOfSKUs = 1000000;
        public static int NumberOfSKUGroups = 5000;
        public static Range<int> MarketOrdersPerMinute;

        public static double SKUMinVolume = .05;
        public static double SKUMaxVolume = .25;
    }

    /// <summary>
    /// Class FulfillmentCenterConfig holds the data by which Fulfillment Centers are to be configured.
    /// </summary>
    public class FulfillmentCenterConfig
    {
        private readonly IRandomChannel m_random = TotallyBogusConfigMechanism.RANDOM_SERVER.GetRandomChannel();

        public double FulfillmentCenterVolume { get; set; }
        public TimeSpan FulfillmentDelay
        {
            get { return TimeSpan.FromHours(m_random.NextDouble(24.0, 72.0)); }
        }

        public TimeSpan TrackingPeriodicity
        {
            get { return TimeSpan.FromHours(2.0); }
        }

        public int InitialLevelFor(SKU sku)
        {
            return (int)((1.5 * ReorderLevelFor(sku)) + (m_random.NextDouble() * FullLevelFor(sku)));
        }

        public int ReorderLevelFor(SKU sku)
        {
            return 3;
        }

        public int FullLevelFor(SKU sku)
        {
            return 12;
        }
    }


}