/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Utility;


namespace Highpoint.Sage.Scheduling {

	/// <summary>
	/// Summary description for zTestTimePeriods.
	/// </summary>
	[TestClass]
	public class TimePeriodRelationshipTester {
		
		#region Private Fields
		private DateTime Now = DateTime.Now;
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

		
		private TimePeriod.Relationship[] m_relationships = new TimePeriod.Relationship[]{
																							 TimePeriod.Relationship.StartsBeforeStartOf,
																							 TimePeriod.Relationship.StartsOnStartOf,
																							 TimePeriod.Relationship.StartsAfterStartOf,
																							 TimePeriod.Relationship.StartsBeforeEndOf,
																							 TimePeriod.Relationship.StartsOnEndOf,
																							 TimePeriod.Relationship.StartsAfterEndOf,
																							 TimePeriod.Relationship.EndsBeforeStartOf,
																							 TimePeriod.Relationship.EndsOnStartOf,
																							 TimePeriod.Relationship.EndsAfterStartOf,
																							 TimePeriod.Relationship.EndsBeforeEndOf,
																							 TimePeriod.Relationship.EndsOnEndOf,
																							 TimePeriod.Relationship.EndsAfterEndOf };
		#endregion
		
		#region Constructors
		public TimePeriodRelationshipTester() {
			Now = DateTime.Now;
			FiveMinutes    = TimeSpan.FromMinutes(5);
			TenMinutes     = TimeSpan.FromMinutes(10);
			FifteenMinutes = TimeSpan.FromMinutes(15);
			TwentyMinutes  = TimeSpan.FromMinutes(20);
			FiveMinsAgo    = Now - FiveMinutes;
			FiveMinsOn     = Now + FiveMinutes;
			TenMinsAgo     = Now - TenMinutes;
			TenMinsOn      = Now + TenMinutes;
			TwentyMinsAgo  = Now - TwentyMinutes;
			TwentyMinsOn   = Now + TwentyMinutes;
		}
		#endregion

		#region Test Setup & TearDown
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		#endregion

		#region Basics
		[TestMethod]
		public void TestBasics(){
			TimePeriod tpA=null;
			TimePeriod tpB=null;
			foreach ( TimePeriod.Relationship relationship in m_relationships ) {
				_Debug.WriteLine("************************************************************************");
				_Debug.WriteLine("          A " + relationship.ToString() + " B.");
				_Debug.WriteLine("************************************************************************");
				foreach ( TimeSpan slack in new TimeSpan[]{FiveMinutes,TenMinutes,TwentyMinutes} ){
					foreach ( TimePeriodPart moveWhichPart in new TimePeriodPart[]{TimePeriodPart.StartTime,TimePeriodPart.EndTime}){ 
						foreach ( string ofWhich in new string[]{"A","B"}) {
							foreach ( TimeSpan movement in new TimeSpan[]{FiveMinutes,TenMinutes,TwentyMinutes}) {
								SetUpTimePeriods(ref tpA, ref tpB, relationship, slack);
								RunTest(tpA, tpB, relationship, moveWhichPart, ofWhich, movement);	
							}
						}
					}
				}
			}
		}


	    private void RunTest(TimePeriod tpA, TimePeriod tpB, TimePeriod.Relationship relationship,
	        TimePeriodPart moveWhichPart, string ofWhich, TimeSpan byHowMuch)
	    {
	        bool CONSOLE_OUTPUT = false;

            tpA.AddRelationship(relationship, tpB);

	        DateTime earliest = DateTimeOperations.Min(tpA.StartTime, tpB.StartTime);
	        DateTime left = earliest - new TimeSpan(0, 0, (earliest.Minute%10), 0, 0);

	        int spaces, width;
	        string A, B;

	        if (CONSOLE_OUTPUT)
	        {
                _Debug.WriteLine("           ....*....|....*....|....*....|....*....|....*....|");
                A = "Initial A : ";
                spaces = (int)((TimeSpan)(tpA.StartTime - left)).TotalMinutes;
                width = (int)tpA.Duration.TotalMinutes;
                for (int i = 0; i < spaces; i++) A += " ";
                for (int i = 0; i < width; i++) A += "a";
                for (int i = (spaces + width); i < 50; i++) A += " ";
                _Debug.WriteLine(A + tpA.ToString());


                B = "Initial B : ";
                spaces = (int)((TimeSpan)(tpB.StartTime - left)).TotalMinutes;
                width = (int)tpB.Duration.TotalMinutes;
                for (int i = 0; i < spaces; i++) B += " ";
                for (int i = 0; i < width; i++) B += "b";
                for (int i = (spaces + width); i < 50; i++) B += " ";
                _Debug.WriteLine(B + tpB.ToString());

                _Debug.WriteLine("We'll move " + ofWhich + "." + moveWhichPart.ToString() + " by " + byHowMuch.ToString() + "...");
            }

	        int selector = 0;
	        if (ofWhich.Equals("A")) selector += 0;
	        if (ofWhich.Equals("B")) selector += 1;
	        selector <<= 1;
	        if (moveWhichPart.Equals(TimePeriodPart.StartTime)) selector += 0;
	        if (moveWhichPart.Equals(TimePeriodPart.EndTime)) selector += 1;


	        tpA.AdjustmentMode = TimeAdjustmentMode.FixedDuration;
	        tpB.AdjustmentMode = TimeAdjustmentMode.FixedDuration;
	        if (CONSOLE_OUTPUT)
	        {
	            Console.WriteLine("3.");
	            foreach (IMilestone ms in new IMilestone[] {tpB.StartMilestone, tpB.EndMilestone})
	            {
	                Console.WriteLine(ms.Name);
	                foreach (MilestoneRelationship mr in ms.Relationships)
	                {
	                    Console.WriteLine(mr.ToString());
	                }
	            }
	            Console.WriteLine("4.");
	            foreach (IMilestone ms in new IMilestone[] {tpB.StartMilestone, tpB.EndMilestone})
	            {
	                Console.WriteLine(ms.Name);
	                foreach (MilestoneRelationship mr in ms.Relationships)
	                {
	                    Console.WriteLine(mr.ToString());
	                }
	            }
	        }

	        switch ( selector ) {
				case 0: tpA.AdjustmentMode = TimeAdjustmentMode.InferEndTime;   tpA.StartTime += byHowMuch; break;
				case 1: tpA.AdjustmentMode = TimeAdjustmentMode.InferStartTime; tpA.EndTime   += byHowMuch; break;
				case 2: tpB.AdjustmentMode = TimeAdjustmentMode.InferEndTime;   tpB.StartTime += byHowMuch; break;
				case 3: tpB.AdjustmentMode = TimeAdjustmentMode.InferStartTime; tpB.EndTime   += byHowMuch; break;
			}

	        if (CONSOLE_OUTPUT)
	        {
	            A = "Final A   : ";
	            spaces = (int) ((TimeSpan) (tpA.StartTime - left)).TotalMinutes;
	            width = (int) tpA.Duration.TotalMinutes;
	            for (int i = 0; i < spaces; i++) A += " ";
	            for (int i = 0; i < width; i++) A += "A";
	            for (int i = (spaces + width); i < 50; i++) A += " ";
	            _Debug.WriteLine(A + tpA.ToString());

	            B = "Final B   : ";
	            spaces = (int) ((TimeSpan) (tpB.StartTime - left)).TotalMinutes;
	            width = (int) tpB.Duration.TotalMinutes;
	            for (int i = 0; i < spaces; i++) B += " ";
	            for (int i = 0; i < width; i++) B += "B";
	            for (int i = (spaces + width); i < 50; i++) B += " ";
	            _Debug.WriteLine(B + tpB.ToString());
	        }

	        foreach ( TimePeriod tp in new TimePeriod[]{tpA,tpB} ){
				foreach ( IMilestone ms in new IMilestone[]{tp.StartMilestone,tp.EndMilestone}){
					foreach ( MilestoneRelationship mr in ms.Relationships )
					{
					    bool b = mr.IsSatisfied();
                        Console.WriteLine(mr + (b?" is ":" is not ") + "satisfied.");
                        //Assert.IsTrue(b, mr + " is not satisfied!");
					}
				}
			}
		}

		
		private void SetUpTimePeriods(ref TimePeriod tpA, ref TimePeriod tpB, TimePeriod.Relationship relationship, TimeSpan offset){
			tpB = new TimePeriod("TimePeriod B",Guid.NewGuid(),FiveMinsAgo,FiveMinsOn,TimeAdjustmentMode.InferDuration);
			switch (relationship ) {
				case TimePeriod.Relationship.StartsBeforeStartOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),tpB.StartTime-offset,TenMinutes,TimeAdjustmentMode.InferEndTime);
					//tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.StartsOnStartOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),tpB.StartTime,TenMinutes,TimeAdjustmentMode.InferEndTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.StartsAfterStartOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),tpB.StartTime+offset,TenMinutes,TimeAdjustmentMode.InferEndTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.StartsBeforeEndOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),tpB.EndTime-offset,TenMinutes,TimeAdjustmentMode.InferEndTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.StartsOnEndOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),tpB.EndTime,TenMinutes,TimeAdjustmentMode.InferEndTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.StartsAfterEndOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),tpB.EndTime+offset,TenMinutes,TimeAdjustmentMode.InferEndTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.EndsBeforeStartOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),TenMinutes,tpB.StartTime-offset,TimeAdjustmentMode.InferStartTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.EndsOnStartOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),TenMinutes,tpB.StartTime,TimeAdjustmentMode.InferStartTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.EndsAfterStartOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),TenMinutes,tpB.StartTime+offset,TimeAdjustmentMode.InferStartTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.EndsBeforeEndOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),TenMinutes,tpB.EndTime-offset,TimeAdjustmentMode.InferStartTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.EndsOnEndOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),TenMinutes,tpB.EndTime,TimeAdjustmentMode.InferStartTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
				case TimePeriod.Relationship.EndsAfterEndOf:{
					tpA = new TimePeriod("TimePeriod A",Guid.NewGuid(),TenMinutes,tpB.EndTime+offset,TimeAdjustmentMode.InferStartTime);
					tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
					break;
				}
			}
		}
		#endregion

	}
}
