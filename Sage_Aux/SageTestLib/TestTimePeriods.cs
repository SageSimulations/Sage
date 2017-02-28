/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// TODO: Debug failing tests.

namespace Highpoint.Sage.Scheduling {

	/// <summary>
	/// Summary description for zTestTimePeriods.
	/// </summary>
	[TestClass]
	public class TimePeriodTester {
		public TimePeriodTester() {
			Now = new DateTime(2001,05,16,12,0,0);
			FiveMinutes    = TimeSpan.FromMinutes(5);
			TenMinutes     = TimeSpan.FromMinutes(10);
			FifteenMinutes = TimeSpan.FromMinutes(15);
			TwentyMinutes  = TimeSpan.FromMinutes(20);
			FiveMinsAgo    = Now - FiveMinutes;
			FiveMinsOn     = Now + FiveMinutes;
			TenMinsAgo     = Now - TenMinutes;
			TenMinsOn      = Now + TenMinutes;
		}

		private DateTime Now = DateTime.Now;
		private TimeSpan FiveMinutes;
		private TimeSpan TenMinutes;
		private TimeSpan FifteenMinutes;
		private TimeSpan TwentyMinutes;
		private DateTime FiveMinsAgo;
		private DateTime FiveMinsOn;
		private DateTime TenMinsAgo;
		private DateTime TenMinsOn;

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}

		[TestMethod]
		public void TestTimePeriodBasics(){

			TimePeriod tp;

			_Debug.WriteLine("Now = " + Now);

#region Fixed Start Time
			// Test creation of a fixed start time TimePeriod.
			tp = new TimePeriod(FiveMinsAgo,FiveMinsOn,TimeAdjustmentMode.FixedStart);
            Assert.IsTrue(tp.Duration.Equals(TenMinutes), "TimePeriod Failure - initial duration on fixed start.");
			
			// Test modification of duration.
			tp.Duration = FiveMinutes;
            Assert.IsTrue(tp.EndTime.Equals(Now), "TimePeriod Failure - end time on fixed start.");
			
			// Test modification of end time.
			tp.EndTime = FiveMinsOn;
		    Assert.IsTrue(tp.Duration.Equals(TenMinutes), "TimePeriod Failure - duration on fixed start.");
#endregion

#region Fixed End Time
			// Test creation of a fixed end time TimePeriod.
			tp = new TimePeriod(FiveMinsAgo,FiveMinsOn,TimeAdjustmentMode.FixedEnd);
            Assert.IsTrue(tp.Duration.Equals(TenMinutes), "TimePeriod Failure - initial duration on fixed end.");
			
			// Test modification of duration.
			tp.Duration = FiveMinutes;
            Assert.IsTrue(tp.StartTime.Equals(Now), "TimePeriod Failure - start time on fixed end.");
			
			// Test modification of start time.
			tp.StartTime = Now;
            Assert.IsTrue(tp.Duration.Equals(FiveMinutes), "TimePeriod Failure - duration on fixed end.");
#endregion

#region Fixed Duration
			// Test creation of a fixed duration TimePeriod.
			tp = new TimePeriod(FiveMinsAgo,FiveMinsOn,TimeAdjustmentMode.FixedDuration);
            Assert.IsTrue(tp.Duration.Equals(TenMinutes), "TimePeriod Failure - initial duration on fixed duration.");
            		
			// Test modification of start time.
			tp.StartTime = TenMinsAgo;
//            Assert.IsTrue(tp.EndTime.Equals(Now), "TimePeriod Failure - initial duration on fixed duration.");
			
			// Test modification of end time.
			tp.EndTime = FiveMinsOn;
//            Assert.IsTrue(tp.StartTime.Equals(FiveMinsAgo), "TimePeriod Failure - start time on fixed duration.");
#endregion

#region Infer Start Time
			// Test creation of a fixed start time TimePeriod.
			tp = new TimePeriod(TenMinutes,FiveMinsOn,TimeAdjustmentMode.InferStartTime);
		    Assert.IsTrue(tp.StartTime.Equals(FiveMinsAgo), "TimePeriod Failure - initial start time on inferred start time.");
			
			// Test modification of duration.
			tp.Duration = FiveMinutes;
            Assert.IsTrue(tp.StartTime.Equals(Now), "TimePeriod Failure - changed duration on inferred start time.");
			
			// Test modification of end time.
			tp.EndTime = TenMinsOn;
//            Assert.IsTrue(tp.StartTime.Equals(FiveMinsOn), "TimePeriod Failure - changed end time on inferred start time.");
#endregion

#region Infer End Time
			// Test creation of a fixed end time TimePeriod.
			tp = new TimePeriod(FiveMinsAgo,TenMinutes,TimeAdjustmentMode.InferEndTime);
            Assert.IsTrue(tp.EndTime.Equals(FiveMinsOn), "TimePeriod Failure - initial end time on inferred end.");
			
			// Test modification of start time.
			tp.StartTime = Now;
//            Assert.IsTrue(tp.EndTime.Equals(TenMinsOn), "TimePeriod Failure - changed start time on fixed end.");
			
			// Test modification of duration.
			tp.Duration = FiveMinutes;
            Assert.IsTrue(tp.EndTime.Equals(FiveMinsOn), "TimePeriod Failure - changed duration on fixed end." );
#endregion

#region Infer Duration
			// Test creation of a fixed duration TimePeriod.
			tp = new TimePeriod(FiveMinsAgo,FiveMinsOn,TimeAdjustmentMode.InferDuration);
		    Assert.IsTrue(tp.Duration.Equals(TenMinutes), "TimePeriod Failure - initial duration on inferred duration.");
			
			// Test modification of start time.
			tp.StartTime = Now;
		    Assert.IsTrue(tp.Duration.Equals(FiveMinutes), "TimePeriod Failure - changed end time on fixed duration.");
			
			// Test modification of end time.
			tp.EndTime = TenMinsOn;
		    Assert.IsTrue(tp.Duration.Equals(TenMinutes), "TimePeriod Failure - changed start time on fixed duration.");
#endregion
		}

        [TestMethod]
        public void TestTimePeriodEnvelope() {

            TimePeriod tp1 = new TimePeriod(FiveMinsAgo, Now, TimeAdjustmentMode.FixedDuration);
            foreach (IMilestone ms in new IMilestone[] { tp1.StartMilestone, tp1.EndMilestone }) {
                Console.WriteLine("Relationships involving " + ms.Name + " are:");
                foreach (MilestoneRelationship mr in ms.Relationships) {
                    Console.WriteLine("\t" + mr.ToString());
                }
            }

            TimePeriod tp2 = new TimePeriod(Now, FiveMinsOn, TimeAdjustmentMode.FixedDuration);
            TimePeriod tp3 = new TimePeriod(FiveMinsOn, TenMinsOn, TimeAdjustmentMode.FixedDuration);


            Console.WriteLine("Creating a time period envelope and adding " + tp1.ToString() + " and " + tp2.ToString() + " to it.");
            TimePeriodEnvelope tpe = new TimePeriodEnvelope();
            tpe.AddTimePeriod(tp1);
            tpe.AddTimePeriod(tp2);

            Assert.IsTrue(tpe.Duration.Equals(TenMinutes),"TimePeriodEnvelope Failure a");

            tpe.AddTimePeriod(tp3);
            Assert.IsTrue(tpe.Duration.Equals(FifteenMinutes), "TimePeriodEnvelope Failure b");

            Console.WriteLine("Removing " + tp1.ToString() + " from it.");
            tpe.RemoveTimePeriod(tp1);
            Assert.IsTrue(tpe.Duration.Equals(TenMinutes), "TimePeriodEnvelope Failure c");


        }
        [TestMethod]
        public void TestNestedTimePeriodEnvelope() {

            TimePeriod tp1 = new TimePeriod("FivePast", Guid.NewGuid(), FiveMinsAgo, Now, TimeAdjustmentMode.FixedDuration);
            foreach (IMilestone ms in new IMilestone[] { tp1.StartMilestone, tp1.EndMilestone }) {
                Console.WriteLine("Relationships involving " + ms.Name + " are:");
                foreach (MilestoneRelationship mr in ms.Relationships) {
                    Console.WriteLine("\t" + mr.ToString());
                }
            }

            TimePeriod tp2 = new TimePeriod("FiveNext", Guid.NewGuid(), Now, FiveMinsOn, TimeAdjustmentMode.FixedDuration);
            TimePeriod tp3 = new TimePeriod("FiveFuture", Guid.NewGuid(), FiveMinsOn, TenMinsOn, TimeAdjustmentMode.FixedDuration);

            TimePeriodEnvelope tpe = new TimePeriodEnvelope("Root",Guid.NewGuid());
            TimePeriodEnvelope tpe2 = new TimePeriodEnvelope("RootsChild", Guid.NewGuid());
            tpe.AddTimePeriod(tpe2);
            tpe2.AddTimePeriod(tp1);
            tpe2.AddTimePeriod(tp2);
            Assert.IsTrue(tpe.Duration.Equals(TenMinutes), "TimePeriodEnvelope Failure a");

            tpe2.AddTimePeriod(tp3);

            Assert.IsTrue(tpe.Duration.Equals(FifteenMinutes), "TimePeriodEnvelope Failure b");

            Console.WriteLine("Removing " + tp1.ToString() + " from it.");
            tpe2.RemoveTimePeriod(tp1);

            Assert.IsTrue(tpe.Duration.Equals(TenMinutes), "TimePeriodEnvelope Failure c");

        }
    }
}
