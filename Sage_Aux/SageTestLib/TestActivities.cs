/* This source code licensed under the GNU Affero General Public License */
#if NYRFPT
using System;
using Highpoint.Sage.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Highpoint.Sage.Scheduling {

	/// <summary>
	/// Summary description for zTestTimePeriods.
	/// </summary>
	[TestClass]
	public class ActivityTester {
	
#region Private Fields
		private static readonly string KEY1 = "1";
		private static readonly string KEY2 = "2";

		private Random m_random = new Random();
		private string[] letters = new string[]{"A","B","C","D","E","F"};

		private DateTime Now = new DateTime(2001,05,16,12,0,0);
		private TimeSpan FiveMinutes;
		private TimeSpan TenMinutes;
		private TimeSpan FifteenMinutes;
		private TimeSpan TwentyMinutes;

		private DateTime FiveMinsAgo;
		private DateTime FiveMinsOn;
		private DateTime TenMinsAgo;
		private DateTime TenMinsOn;
		private DateTime TwentyMinsAgo;
		private DateTime TwentyMinsOn;
		
#endregion

#region Constructors
		public ActivityTester() {
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
#endregion

#region Test Setup & Tear-Down
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
#endregion

		[TestMethod]
		public void TestActivities(){

			Activity A = new Activity("A",Guid.NewGuid());
			A.AddTimePeriod(KEY1,new TimePeriod(A.Name+"."+KEY1,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			A.AddTimePeriod(KEY2,new TimePeriod(A.Name+"."+KEY2,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			
			Activity B = new Activity("B",Guid.NewGuid());
			B.AddTimePeriod(KEY1,new TimePeriod(B.Name+"."+KEY1,Guid.NewGuid(),TenMinsAgo,Now,TimeAdjustmentMode.InferDuration));
			B.AddTimePeriod(KEY2,new TimePeriod(B.Name+"."+KEY2,Guid.NewGuid(),TenMinsAgo,FiveMinsAgo,TimeAdjustmentMode.InferDuration));
			A.AddChild(B);

			Dump(A);
			
		}

		//[TestMethod]
		public void TestActivitiesDeep(){
			Now = new DateTime(2004,05,16,12,00,00);

			Activity A = new Activity("A",Guid.NewGuid());
			A.AddTimePeriod(KEY1,new TimePeriod(A.Name+"."+KEY1,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			A.AddTimePeriod(KEY2,new TimePeriod(A.Name+"."+KEY2,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			
			Activity B = new Activity("B",Guid.NewGuid());
			B.AddTimePeriod(KEY1,new TimePeriod(B.Name+"."+KEY1,Guid.NewGuid(),Now-TimeSpan.FromMinutes(1.0),Now+TimeSpan.FromMinutes(1.0),TimeAdjustmentMode.InferDuration));
			B.AddTimePeriod(KEY2,new TimePeriod(B.Name+"."+KEY2,Guid.NewGuid(),Now-TimeSpan.FromMinutes(1.0),Now+TimeSpan.FromMinutes(1.0),TimeAdjustmentMode.InferDuration));
            A.AddChild(B);

			Activity C = new Activity("C",Guid.NewGuid());
			C.AddTimePeriod(KEY1,new TimePeriod(C.Name+"."+KEY1,Guid.NewGuid(),Now-TimeSpan.FromMinutes(2.0),Now+TimeSpan.FromMinutes(2.0),TimeAdjustmentMode.InferDuration));
			C.AddTimePeriod(KEY2,new TimePeriod(C.Name+"."+KEY2,Guid.NewGuid(),Now-TimeSpan.FromMinutes(2.0),Now+TimeSpan.FromMinutes(2.0),TimeAdjustmentMode.InferDuration));
            B.AddChild(C);

			Activity D = new Activity("D",Guid.NewGuid());
			D.AddTimePeriod(KEY1,new TimePeriod(D.Name+"."+KEY1,Guid.NewGuid(),Now-TimeSpan.FromMinutes(3.0),Now+TimeSpan.FromMinutes(3.0),TimeAdjustmentMode.InferDuration));
			D.AddTimePeriod(KEY2,new TimePeriod(D.Name+"."+KEY2,Guid.NewGuid(),Now-TimeSpan.FromMinutes(3.0),Now+TimeSpan.FromMinutes(3.0),TimeAdjustmentMode.InferDuration));
            C.AddChild(D);

			Dump(A);

			
		}
		
        //[TestMethod]
		public void TestActivitiesDeepRandom(){

			Activity A = new Activity("A",Guid.NewGuid());
			A.AddTimePeriod(KEY1,new TimePeriod(A.Name+"."+KEY1,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			A.AddTimePeriod(KEY2,new TimePeriod(A.Name+"."+KEY2,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			
			AddChildren(A,19.0);

			Dump(A);
		}

		//[TestMethod]
		public void TestTaskGraphAdjustments(){
			Activity campaign=null, batch1=null;

			Activity[] units = new Activity[2];

			Activity[][] opSteps = new Activity[][]{new Activity[3],new Activity[3]};
			double[][] durations = new double[][]{new double[]{30.0,20.0,10.0},new double[]{20.0,30.0,40.0}};

			ConstructSchedule(ref campaign,ref batch1, ref units, ref opSteps, durations);

			ITimePeriod tpOp1A = opSteps[0][0].GetTimePeriodAspect(KEY1);
			ITimePeriod tpOp1B = opSteps[0][1].GetTimePeriodAspect(KEY1);
			ITimePeriod tpOp1C = opSteps[0][2].GetTimePeriodAspect(KEY1);

			ITimePeriod tpOp2A = opSteps[1][0].GetTimePeriodAspect(KEY1);
			ITimePeriod tpOp2B = opSteps[1][1].GetTimePeriodAspect(KEY1);
			ITimePeriod tpOp2C = opSteps[1][2].GetTimePeriodAspect(KEY1);

			if ( tpOp1B.StartTime < tpOp2B.StartTime ) tpOp1B.StartTime = tpOp2B.StartTime;
			if ( tpOp2B.StartTime < tpOp1B.StartTime ) tpOp2B.StartTime = tpOp1B.StartTime;
			
			tpOp1B.AddRelationship(TimePeriod.Relationship.StartsOnStartOf,tpOp2B);
			tpOp2B.AddRelationship(TimePeriod.Relationship.StartsOnStartOf,tpOp1B);
			
			if ( tpOp1C.StartTime < tpOp2C.StartTime ) tpOp1C.StartTime = tpOp2C.StartTime;
			if ( tpOp2C.StartTime < tpOp1C.StartTime ) tpOp2C.StartTime = tpOp1C.StartTime;
			
			tpOp1C.AddRelationship(TimePeriod.Relationship.StartsOnStartOf,tpOp2C);
			tpOp2C.AddRelationship(TimePeriod.Relationship.StartsOnStartOf,tpOp1C);


			Dump(campaign);

		}

		private void AddChildren(Activity a, double prob){
			if ( m_random.NextDouble() < prob ) {
				int nKids = m_random.Next(2,6);
				for ( int i = 0 ; i < nKids ; i++ ) {
					Activity A = new Activity(a.Name+"."+letters[i],Guid.NewGuid());
					A.AddTimePeriod(KEY1,new TimePeriod(A.Name+"."+KEY1,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
					A.AddTimePeriod(KEY2,new TimePeriod(A.Name+"."+KEY2,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
                    a.AddChild(A);
					AddChildren(A,prob/2.0);
				}
			} else {
				DateTime start1 = DateTime.Now-TimeSpan.FromMinutes(m_random.NextDouble()*45.0);
				DateTime start2 = DateTime.Now-TimeSpan.FromMinutes(m_random.NextDouble()*45.0);
				DateTime end1 = DateTime.Now+TimeSpan.FromMinutes(m_random.NextDouble()*45.0);
				DateTime end2 = DateTime.Now+TimeSpan.FromMinutes(m_random.NextDouble()*45.0);
				Activity A = new Activity(a.Name+".Leaf",Guid.NewGuid());
				A.AddTimePeriod(KEY1,new TimePeriod(A.Name+"."+KEY1,Guid.NewGuid(),start1,end1,TimeAdjustmentMode.InferDuration));
				A.AddTimePeriod(KEY2,new TimePeriod(A.Name+"."+KEY2,Guid.NewGuid(),start2,end2,TimeAdjustmentMode.InferDuration));	
				a.AddChild(A);
			}
		}
		
		private void ConstructSchedule(ref Activity campaign, ref Activity batch, ref Activity[] units, ref Activity[][] opSteps, double[][] durations){
			campaign = new Activity("Campaign",Guid.NewGuid());
			campaign.AddTimePeriod(KEY1,new TimePeriod(campaign.Name,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));

			batch = new Activity("Batch",Guid.NewGuid());
			batch.AddTimePeriod(KEY1,new TimePeriod(batch.Name,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
            campaign.AddChild(batch);

			
			for ( int unitNum = 0 ; unitNum < units.Length ; unitNum++ ) {
				units[unitNum] = new Activity("Unit_"+(unitNum+1),Guid.NewGuid());
				Activity unit = units[unitNum];
				unit.AddTimePeriod(KEY1,new TimePeriod(unit.Name,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
                batch.AddChild(unit);

				DateTime cursor = Now;
				for ( int opStepNum = 0 ; opStepNum < opSteps[unitNum].Length ; opStepNum++ ) {
					opSteps[unitNum][opStepNum] = new Activity("Op"+(unitNum+1)+letters[opStepNum],Guid.NewGuid());
					Activity op = opSteps[unitNum][opStepNum];
					TimeSpan duration = TimeSpan.FromMinutes(durations[unitNum][opStepNum]);
					op.AddTimePeriod(KEY1,new TimePeriod(op.Name,Guid.NewGuid(),cursor,duration,TimeAdjustmentMode.FixedDuration));
                    unit.AddChild(op);
					cursor+=duration;
					
				}
			}

/*			op1A.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op1B.GetTimePeriodAspect(KEY1));
			op1B.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op1C.GetTimePeriodAspect(KEY1));

			
			Activity op2A     = new Activity("Op1A",Guid.NewGuid());
			op2A.AddTimePeriod(KEY1,new TimePeriod(op2A.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(10),TimeAdjustmentMode.FixedDuration));
			unit2.AddChild(op2A);

			Activity op2B     = new Activity("Op1B",Guid.NewGuid());
			op2B.AddTimePeriod(KEY1,new TimePeriod(op2B.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(20),TimeAdjustmentMode.FixedDuration));
			unit2.AddChild(op2B);

			Activity op2C     = new Activity("Op1C",Guid.NewGuid());
			op2C.AddTimePeriod(KEY1,new TimePeriod(op2C.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(30),TimeAdjustmentMode.FixedDuration));
			unit2.AddChild(op2C);

			op2A.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op2B.GetTimePeriodAspect(KEY1));
			op2B.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op2C.GetTimePeriodAspect(KEY1));
*/
		}

		private void Dump(Activity a){
			_Dump(a,0);
		}

		private void _Dump(object a, int level){
			for ( int i = 0 ; i < level ; i++ ) Console.Write("\t");
			Activity activity;
			if ( a is Activity ) {
                activity = (Activity)a;
            } else if (a is ITreeNode<Activity>) {
                activity = ( (ITreeNode<Activity>)a ).Payload;
            } else {
                throw new ApplicationException("Unrecognized type " + a.GetType().FullName);
            }

			Console.WriteLine(activity.Name + ":" + activity.GetTimePeriodAspect(KEY1).ToString());
            activity.ForEachChild(delegate(ITreeNode<Activity> tna) { _Dump(tna, level + 1); });
		}
	}
}

/*			Activity campaign = new Activity("Campaign",Guid.NewGuid());
			campaign.AddTimePeriod(KEY1,new TimePeriod(campaign.Name,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			
			Activity batch1   = new Activity("Batch1",Guid.NewGuid());
			batch1.AddTimePeriod(KEY1,new TimePeriod(batch1.Name,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			campaign.AddChild(batch1);
			
			Activity unit1    = new Activity("Unit1",Guid.NewGuid());
			unit1.AddTimePeriod(KEY1,new TimePeriod(unit1.Name,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			batch1.AddChild(unit1);
			
			Activity unit2    = new Activity("Unit1",Guid.NewGuid());
			unit2.AddTimePeriod(KEY1,new TimePeriod(unit2.Name,Guid.NewGuid(),Now,Now,TimeAdjustmentMode.InferDuration));
			batch1.AddChild(unit2);
			

			Activity op1A     = new Activity("Op1A",Guid.NewGuid());
			op1A.AddTimePeriod(KEY1,new TimePeriod(op1A.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(40),TimeAdjustmentMode.FixedDuration));
			unit1.AddChild(op1A);

			Activity op1B     = new Activity("Op1B",Guid.NewGuid());
			op1B.AddTimePeriod(KEY1,new TimePeriod(op1B.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(30),TimeAdjustmentMode.FixedDuration));
			unit1.AddChild(op1B);

			Activity op1C     = new Activity("Op1C",Guid.NewGuid());
			op1C.AddTimePeriod(KEY1,new TimePeriod(op1C.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(20),TimeAdjustmentMode.FixedDuration));
			unit1.AddChild(op1C);

			op1A.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op1B.GetTimePeriodAspect(KEY1));
			op1B.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op1C.GetTimePeriodAspect(KEY1));

			
			Activity op2A     = new Activity("Op1A",Guid.NewGuid());
			op2A.AddTimePeriod(KEY1,new TimePeriod(op2A.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(10),TimeAdjustmentMode.FixedDuration));
			unit2.AddChild(op2A);

			Activity op2B     = new Activity("Op1B",Guid.NewGuid());
			op2B.AddTimePeriod(KEY1,new TimePeriod(op2B.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(20),TimeAdjustmentMode.FixedDuration));
			unit2.AddChild(op2B);

			Activity op2C     = new Activity("Op1C",Guid.NewGuid());
			op2C.AddTimePeriod(KEY1,new TimePeriod(op2C.Name,Guid.NewGuid(),DateTime.Now,TimeSpan.FromMinutes(30),TimeAdjustmentMode.FixedDuration));
			unit2.AddChild(op2C);

			op2A.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op2B.GetTimePeriodAspect(KEY1));
			op2B.GetTimePeriodAspect(KEY1).AddRelationship(TimePeriod.Relationship.EndsBeforeStartOf,op2C.GetTimePeriodAspect(KEY1));
*/

#endif