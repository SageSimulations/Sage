/* This source code licensed under the GNU Affero General Public License */

using System.Diagnostics;

namespace Highpoint.Sage.SimCore {
	using System;
		using Microsoft.VisualStudio.TestTools.UnitTesting;

	/// <summary>
    /// Summary description for EvaluatorTester.
	/// </summary>
	[TestClass]
	public class EvaluatorTester {
		public EvaluatorTester() {}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}

		[TestMethod]
		public void TestEvaluatorBasics(){

			Evaluator eval = EvaluatorFactory.CreateEvaluator("double x = y + z;","x",new string[]{"y","z"});


            Assert.IsTrue(((double)eval(3.0,4.0)) == 7.0,"Evaluator did not return the proper value.");
			Console.WriteLine(eval(3.0,4.0));
		}
	}
}
