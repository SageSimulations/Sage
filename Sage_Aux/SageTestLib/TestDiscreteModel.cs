/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.SimCore {

    [TestClass]
    public class DiscreteTester {

        private Random m_random = new Random();

        public DiscreteTester(){Init();}

		private int _dotick = 31;
		private DateTime _timelast = new DateTime();
		private TimeSpan _timedifference = TimeSpan.FromMinutes(10);
        
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test check if a defined metronome fires the defined amount of events in the correct constant time difference")]
		public void TestDiscreteModel(){
            Model model = new Model();

			Metronome_Simple sm = Metronome_Simple.CreateMetronome(model.Executive,DateTime.Now, DateTime.Now+TimeSpan.FromHours(5),_timedifference);
			sm.TickEvent += new ExecEventReceiver(sm_TickEvent);

			model.Start();

			System.Diagnostics.Debug.Assert(_dotick == 0,"Tick event did not fire 30 times");
		}

		private void sm_TickEvent(IExecutive exec, object userData) {
            Console.WriteLine(exec.Now.ToString() + ", " + _timelast.ToString() + ", " + _timedifference.ToString());
            if (_timelast > DateTime.MinValue) {
                System.Diagnostics.Debug.Assert(_timelast + _timedifference == exec.Now, "Tick does not happen at correct time difference");
            }
			Trace.WriteLine(exec.Now + " : Tick happened.");
			_dotick--;
			_timelast = exec.Now;
		}
	}
}

