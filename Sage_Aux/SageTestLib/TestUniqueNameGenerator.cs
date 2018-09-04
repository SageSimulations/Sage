/* This source code licensed under the GNU Affero General Public License */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Utility
{
    [TestClass]
    public class UniqueNameGeneratorTester 
    {
        [TestMethod]
        public void TestUniqueNameGenerator()
        {

            UniqueNameGenerator ung = new UniqueNameGenerator();

            string dog00 = ung.GetNextName("Dog", 2, true);
            string dog01 = ung.GetNextName("Dog", 2);
            string dog02 = ung.GetNextName("Dog", 3, false);
            string dog03 = ung.GetNextName("Dog", 3);

            string cat00 = ung.GetNextName("Cat", 2, false);
            string cat01 = ung.GetNextName("Cat", 2);
            string cat02 = ung.GetNextName("Cat", 4, true);
            string cat03 = ung.GetNextName("Cat", 4);

            string dog04 = ung.GetNextName("Dog", 2, false);

            string result = string.Concat(cat00, cat01, cat02, cat03, dog00, dog01, dog02, dog03, dog04);
            //Console.WriteLine(result);
            Assert.AreEqual(result, "Cat01Cat02Cat0000Cat0001Dog00Dog01Dog001Dog002Dog02");
        }
    }
}