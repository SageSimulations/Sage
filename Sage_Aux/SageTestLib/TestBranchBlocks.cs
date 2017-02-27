/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.SplittersAndJoiners;

namespace Highpoint.Sage.ItemBased {
	using StochTwoChoice = SimpleStochasticTwoChoiceBranchBlock;
	using DelegTwoChoice = SimpleDelegatedTwoChoiceBranchBlock;

	/// <summary>
	/// Summary description for zTestBranchBlocks.
	/// </summary>
	[TestClass]
	public class BranchBlockTester {

		#region MSTest Goo
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}
		#endregion

		int lastResult = -1;
		public BranchBlockTester() {}

		private int [] m_expected = new int[]{1,1,1,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,0,1,1,1,0,0,1,0,1,1,1,1,
										   1,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1,0,1,0,1,
										   1,1,1,1,0,1,1,1,1,1,1,1,0,1,1,1,1,0,1,1,1,1,1,1,0,1,1,1,1,1,1,
										   1,1,1,1,1,0,1};
		private int m_itemNumber;

		[TestMethod]
		public void TestStochasticBranchBlock(){
			Model model = new Model();
			StochTwoChoice ss2cbb = new StochTwoChoice(model,"s2c",Guid.NewGuid(),.2);
			ss2cbb.Outputs[0].PortDataPresented+=new PortDataEvent(Out0_PortDataPresented);
			ss2cbb.Outputs[1].PortDataPresented+=new PortDataEvent(Out1_PortDataPresented);
			Randoms.RandomServer rs = new Randoms.RandomServer(12345,100);
			model.RandomServer = rs;
			for ( m_itemNumber = 0 ; m_itemNumber < m_expected.Length ; m_itemNumber++ ) {
				ss2cbb.Input.Put(new object());
				Trace.Write(lastResult + ",");
				System.Diagnostics.Debug.Assert(lastResult == m_expected[m_itemNumber],"Unexpected choice.");
			}
		}

		[TestMethod]
		public void TestDelegatedBranchBlock(){
			Model model = new Model();
			DelegTwoChoice d2cbb = new DelegTwoChoice(model,"s2c",Guid.NewGuid());
			d2cbb.BooleanDeciderDelegate = new BooleanDecider(ChooseYesOrNo);
			d2cbb.Outputs[0].PortDataPresented+=new PortDataEvent(Out0_PortDataPresented);
			d2cbb.Outputs[1].PortDataPresented+=new PortDataEvent(Out1_PortDataPresented);
			Randoms.RandomServer rs = new Randoms.RandomServer(12345,100);
			model.RandomServer = rs;
			for ( m_itemNumber = 0 ; m_itemNumber < m_expected.Length ; m_itemNumber++ ) {
				d2cbb.Input.Put(new object());
				Trace.Write(lastResult + ",");
				System.Diagnostics.Debug.Assert(lastResult == m_expected[m_itemNumber],"Unexpected choice.");
			}
		}

		private bool ChooseYesOrNo(object serverObject){
			return m_expected[m_itemNumber]==0;
		}

		private void Out0_PortDataPresented(object data, IPort where) {
			lastResult = 0;
		}

		private void Out1_PortDataPresented(object data, IPort where) {
			lastResult = 1;
		}
	}
}
