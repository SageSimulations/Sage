/* This source code licensed under the GNU Affero General Public License */


using System.Diagnostics;
using NUnit.Framework;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.ItemBased.Blocks {

    /// <summary>BlockModelPersistence.
    /// </summary>
    [TestFixture]
    public class BlockModelTester {

        #region MSTest Goo
        [SetUp]
        public void Init() {
        }
        [TearDown]
        public void destroy() {
            Debug.WriteLine("Done.");
        }
        #endregion

        public BlockModelTester() { }

        [Test]
        public void TestBlockModelPersistence() {

            Model model = new Model();

            model.RandomServer = new Randoms.RandomServer(12345, 100);



        }
    }
}
