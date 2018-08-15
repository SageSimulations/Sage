/* This source code licensed under the GNU Affero General Public License */
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Utility {
    [TestClass]
    public class LabelTester {

        public void BasicTest() {
            LabelManager lm1 = new LabelManager();
            LabelManager lm2 = new LabelManager();

            lm1.SetLabel("Plain", null);
            Assert.IsTrue(lm1.GetLabel(null).Equals("Plain"));

            LabelManager.SetContext("Brown");
            lm1.Label = "Chocolate";

            LabelManager.SetContext("Red");
            lm1.Label = "Cherry";

            LabelManager.SetContext(null);
            Console.WriteLine(lm1.Label);
            Assert.IsTrue(lm1.Label.Equals("Plain"));

            LabelManager.SetContext("Brown");

            Console.WriteLine(lm1.Label);
            Assert.IsTrue(lm1.Label.Equals("Chocolate"));

            LabelManager.SetContext("Red");

            Console.WriteLine(lm1.Label);
            Assert.IsTrue(lm1.Label.Equals("Cherry"));

            LabelManager.SetContext("Orange");

            Console.WriteLine(lm1.GetLabel(null));
            Assert.IsTrue(lm1.Label.Equals(""));

            lm2.Label = "Bob";

            LabelManager.SetContext("Brown");
            Assert.IsTrue(lm2.Label.Equals(""));

            LabelManager.SetContext("Orange");
            Assert.IsTrue(lm2.Label.Equals("Bob"));
            
        }
    }
}
