/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.SimCore {

	[TestClass]
	public class DESynchTester {

		private int NUM_EVENTS = 12;
		private Random m_random = new Random();
		private DetachableEventSynchronizer m_des = null;
		public DESynchTester(){Init();}

		private int _submitted = 0;
		private int _synchronized = 0;
		private int _secondary = 0;
		private DateTime _synchtime = new DateTime();

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks the submission of detached events and synchronization of those events with an ISynchChannel")]
		public void TestBaseFunctionality() {

			Model model = new Model();

			IExecutive exec = model.Executive;
			DateTime now = DateTime.Now;
			DateTime when;
			double priority;

			m_des = new DetachableEventSynchronizer(model);

			for ( int i = 0 ; i < NUM_EVENTS ; i++ ) {
				when = new DateTime(now.Ticks + m_random.Next());
				priority = m_random.NextDouble();
				Trace.WriteLine("Primary requesting event service for " + when + ", at priority " + priority);
				object userData = null;
				if ( m_random.Next(5) < 2 ) {
					ISynchChannel isc = m_des.GetSynchChannel(priority);
					userData = isc;
					Trace.WriteLine("Creating synchronized event for time " + when);
					_synchronized ++;
				}
				exec.RequestEvent(new ExecEventReceiver(MyExecEventReceiver),when,priority,userData,ExecEventType.Detachable);
				_submitted ++;
			}

			System.Diagnostics.Debug.Assert(_submitted > 0,"There are no events submitted");
			System.Diagnostics.Debug.Assert(_synchronized > 0,"There are no events synchronized");
			System.Diagnostics.Debug.Assert(_secondary == 0,"There cannot be secondary events submitted yet");

			exec.Start();

			System.Diagnostics.Debug.Assert(_submitted == 0,"Not all submitted events had been fired");
			System.Diagnostics.Debug.Assert(_synchronized == 0,"Not all synchronized events had been fired");
			System.Diagnostics.Debug.Assert(_secondary > 0,"There has not been a secondary events submitted");
		}

		private void MyExecEventReceiver(IExecutive exec, object userData){
			if ( userData == null ) {
				DoUnsynchronized(exec,userData);
			} else {
				DoSynchronized(exec,(ISynchChannel)userData);
			}
		}
		private void DoUnsynchronized(IExecutive exec, object userData){
			if ( m_random.NextDouble() > .15 ) {
				DateTime when = new DateTime(exec.Now.Ticks + m_random.Next());
				Trace.WriteLine("Secondary requesting event service for " + when + ".");
				exec.RequestEvent(new ExecEventReceiver(MyExecEventReceiver),when,m_random.NextDouble(),null,ExecEventType.Detachable);
				_submitted ++;
				_secondary++;
			}
			Trace.WriteLine("Running event at time " + exec.Now + ", and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
			_submitted --;
		}
		private void DoSynchronized(IExecutive exec, ISynchChannel isc){
			Trace.WriteLine("Pausing synchronized event at time " + exec.Now + ", and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
			isc.Synchronize();
			if ( _synchtime == new DateTime() ) {
				_synchtime = exec.Now;
			} else {
				System.Diagnostics.Debug.Assert(_synchtime.Equals(exec.Now),"Synchronized event did not fire at the synchronization time");
			}
			_synchronized --;
			_submitted --;
			Trace.WriteLine("Running synchronized event at time " + exec.Now + ", sequence number " + isc.Sequencer + " and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
		}
	}
}

