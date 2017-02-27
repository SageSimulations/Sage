/* This source code licensed under the GNU Affero General Public License */
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Utility
{
    [TestClass]
    public class UniqueNameGeneratorTester
    {
        [TestMethod]
        public static void TestUniqueNameGenerator()
        {

            UniqueNameGenerator ung = new UniqueNameGenerator();

            string dog00 = ung.GetNextName("Dog", 2, true);
            string dog01 = ung.GetNextName("Dog", 2, true);
            string dog02 = ung.GetNextName("Dog", 2, false);
            string dog03 = ung.GetNextName("Dog", 2, false);

            string cat00 = ung.GetNextName("Cat", 2, true);
            string cat01 = ung.GetNextName("Cat", 2, true);
            string cat02 = ung.GetNextName("Cat", 2, false);
            string cat03 = ung.GetNextName("Cat", 2, false);

            string dog04 = ung.GetNextName("Dog", 2, false);

            // TODO: Check results.
        }
    }
}