/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Highpoint.Sage.Scheduling {

	/// <summary>
	/// Summary description for zTestTimePeriods.
	/// </summary>
	[TestClass]
	public class MilestoneRelationshipTester {
		public MilestoneRelationshipTester() {
			Now = DateTime.Now;
			FiveMinutes    = TimeSpan.FromMinutes(5);
			TenMinutes     = TimeSpan.FromMinutes(10);
			FifteenMinutes = TimeSpan.FromMinutes(15);
			TwentyMinutes  = TimeSpan.FromMinutes(20);
			FiveMinsAgo    = Now - FiveMinutes;
			FiveMinsOn     = Now + FiveMinutes;
			TenMinsAgo     = Now - TenMinutes;
			TenMinsOn      = Now + TenMinutes;
			TwentyMinsOn   = Now + TwentyMinutes;
			TwentyMinsAgo  = Now - TwentyMinutes;
		}

		private DateTime Now = new DateTime(2001,05,16,12,0,0);
		private TimeSpan FiveMinutes;
		private TimeSpan TenMinutes;
		private TimeSpan FifteenMinutes;
		private TimeSpan TwentyMinutes;
		private DateTime FiveMinsAgo;
		private DateTime FiveMinsOn;
		private DateTime TenMinsAgo;
		private DateTime TenMinsOn;
		private DateTime TwentyMinsOn;
		private DateTime TwentyMinsAgo;

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}

		[TestMethod]
		public void TestMilestones(){
			Milestone ms1 = new Milestone(DateTime.Now);
			ms1.ChangeEvent +=new ObservableChangeHandler(ChangeEvent);
			Trace.WriteLine(ms1.ToString());
			ms1.MoveBy(-TwentyMinutes);
			Trace.WriteLine(ms1.ToString());

			Milestone ms2 = new Milestone(DateTime.Now+FiveMinutes);
			ms2.ChangeEvent +=new ObservableChangeHandler(ChangeEvent);
			Trace.WriteLine(ms2.ToString());
			ms2.MoveBy(-TwentyMinutes);
			Trace.WriteLine(ms2.ToString());

            Trace.WriteLine("Milestone 1 is at " + ms1 +", and Milestone 2 is at " + ms2 +". Strutting them together.");
			MilestoneRelationship mr = new MilestoneRelationship_Strut(ms1,ms2);
			ms1.AddRelationship(mr);
			ms2.AddRelationship(mr);

            Trace.WriteLine("Moving Milestone1 by ten minutes.");
			ms1.MoveBy(TenMinutes);
            Trace.WriteLine("Milestone 1 is at " + ms1 + ", and Milestone 2 is at " + ms2 + ".");

		}

		private void ChangeEvent(object whoChanged, object whatChanged, object howChanged) {
			Trace.WriteLine(((Milestone)whoChanged).ToString() + " changed by " + howChanged.ToString());
		}
	}
}