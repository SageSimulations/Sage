/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Randoms;

namespace Highpoint.Sage.Utility {

	[TestClass]
	public class HistorianTester {
		private readonly RandomServer m_rs;
		public HistorianTester() {
			m_rs = new RandomServer();
		}
		
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}

		int NUM_SAMPLES = 9000;
		TimeSpan m_actualAverage = TimeSpan.Zero;
		TimeSpan m_accumulatedDeviation = TimeSpan.Zero;
	    readonly DateTime m_startDate = new DateTime(2006,01,27,09,26,00);
		
		[TestMethod]
		public void TestEventTimeHistorian(){
			IRandomChannel irc = m_rs.GetRandomChannel();
			IExecutive exec = ExecFactory.Instance.CreateExecutive();
			EventTimeHistorian myHistorian = new EventTimeHistorian(exec,256);
			DateTime when = m_startDate;

            // We set up NUM_SAMPLES events with random (0->50) minute intervals.
			TimeSpan totalTimeSpan = TimeSpan.Zero;
			for ( int i = 0 ; i < NUM_SAMPLES ; i++ ) {
				exec.RequestEvent(DoEvent,when,0.0,myHistorian,ExecEventType.Synchronous);
				double d = irc.NextDouble();
				TimeSpan delta = TimeSpan.FromMinutes(d*50.0);
				totalTimeSpan += delta;
				if ( i < 30 ) Console.WriteLine("Delta #" + i + ", " + delta.ToString());
				when += delta;
			}

			m_actualAverage = TimeSpan.FromTicks(totalTimeSpan.Ticks/NUM_SAMPLES);
			Console.WriteLine("Average timeSpan was " + m_actualAverage + ".");

			exec.Start();

            Console.WriteLine("After {0} events, the average interval was {1}.", myHistorian.PastEventsReceived, myHistorian.GetAverageIntraEventDuration());

		}

		private int m_numExecEventsFired;
		private void DoEvent(IExecutive exec, object userData){
            EventTimeHistorian eth = ((EventTimeHistorian)userData);
            eth.LogEvent();
			if ( exec.Now > m_startDate ) {
				m_numExecEventsFired++;
				TimeSpan aied = eth.GetAverageIntraEventDuration();
				m_accumulatedDeviation += (m_actualAverage-aied);
				Console.WriteLine(
                    "Average interval = " + aied + 
                    ", Average deviation = " + TimeSpan.FromTicks(m_accumulatedDeviation.Ticks/m_numExecEventsFired).TotalMinutes);
			}
		}
	}
}
