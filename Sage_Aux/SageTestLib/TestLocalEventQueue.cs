/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
//using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;

//using ProcessStep = Highpoint.Sage.Servers.SimpleServerWithPreQueue;

namespace Highpoint.Sage.Utility {

	/// <summary>
	/// Summary description for zTestLocalEventQueue.
	/// </summary>
	[TestClass]
	public class LocalEventQueueTester : IHasName {

		#region MSTest Goo
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		#endregion

		private int m_numEvents;
		LocalEventQueue m_leq; 

		[TestMethod]
		public void TestLocalEventQueue(){
			IExecutive exec = ExecFactory.Instance.CreateExecutive();

			m_numEvents = 10;
			m_leq = new LocalEventQueue(exec,4,new ExecEventReceiver(DoSomething));

			DateTime when = DateTime.Now;
			m_leq.Enqueue(m_numEvents--,when);
			Console.WriteLine(m_leq.EarliestCompletionTime.ToString());

			when += TimeSpan.FromMinutes(5);
			m_leq.Enqueue(m_numEvents--,when);

			when += TimeSpan.FromMinutes(5);
			m_leq.Enqueue(m_numEvents--,when);

			exec.Start();

		}

		[TestMethod]
		public void TestLocalEventQueue2(){
			IExecutive exec = ExecFactory.Instance.CreateExecutive();

			m_numEvents = 10;
			m_leq = new LocalEventQueue(exec,2,new ExecEventReceiver(DoSomething));

			DateTime when = DateTime.Now;
			m_leq.Enqueue(m_numEvents--,when);
			Console.WriteLine(m_leq.EarliestCompletionTime.ToString());

			when += TimeSpan.FromMinutes(5);
			m_leq.Enqueue(m_numEvents--,when);
			Console.WriteLine(m_leq.EarliestCompletionTime.ToString());

			when += TimeSpan.FromMinutes(5);
			m_leq.Enqueue(m_numEvents--,when);
			Console.WriteLine(m_leq.EarliestCompletionTime.ToString());

			exec.Start();

		}

		private void DoSomething(IExecutive exec, object userData){
			string msg = "";
			if ( !m_leq.IsEmpty ) msg = " - the new head of the event queue will happen at " + m_leq.EarliestCompletionTime.ToString();
			Console.WriteLine(exec.Now.ToString() + " : Receiving event " + userData.ToString() + msg + ".");
			if ( m_numEvents > 0 ) {
				DateTime when = exec.Now + TimeSpan.FromMinutes(10);
				m_leq.Enqueue(m_numEvents--,when);
			}
		}

		public string Name { get { return "Local event queue tester"; } }
	}
}
