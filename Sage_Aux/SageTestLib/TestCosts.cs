/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Scheduling.Cost {

    /// <summary>
    /// Summary description for zTestCost.
    /// </summary>
    [TestClass]
    public class zTestCost1 {
        public zTestCost1() { Init(); }

        [TestInitialize]
        public void Init() {
        }
        [TestCleanup]
        public void destroy() {
            Debug.WriteLine("Done.");
        }

        private Thing alice, bob, charlie, dave, edna, frank, george, harry;



        [TestMethod]
        public void TestCostBasics1() {

            SetUpTree();

            ((IHasCost<Thing>)alice).Cost["Personnel"].DirectCost = 100.0;
            ((IHasCost<Thing>)dave).Cost["Equipment"].DirectCost = 8.0;

            ((IHasCost<Thing>)dave).Cost.Reconcile();

            DumpCostData(alice);
        }

        [TestMethod]
        public void TestCostBasics2() {

            SetUpTree();

            ((IHasCost<Thing>)alice).Cost["Personnel"].DirectCost = 100.0;
            ((IHasCost<Thing>)bob).Cost["Personnel"].DirectCost = 90.0;
            ((IHasCost<Thing>)charlie).Cost["Personnel"].DirectCost = 80.0;
            ((IHasCost<Thing>)dave).Cost["Personnel"].DirectCost = 70.0;
            ((IHasCost<Thing>)edna).Cost["Personnel"].DirectCost = 60.0;
            ((IHasCost<Thing>)frank).Cost["Personnel"].DirectCost = 50.0;
            ((IHasCost<Thing>)george).Cost["Personnel"].DirectCost = 40.0;

            ((IHasCost<Thing>)alice).Cost["Equipment"].DirectCost = 2.0;
            ((IHasCost<Thing>)bob).Cost["Equipment"].DirectCost = 4.0;
            ((IHasCost<Thing>)charlie).Cost["Equipment"].DirectCost = 6.0;
            ((IHasCost<Thing>)dave).Cost["Equipment"].DirectCost = 8.0;
            ((IHasCost<Thing>)edna).Cost["Equipment"].DirectCost = 10.0;
            ((IHasCost<Thing>)frank).Cost["Equipment"].DirectCost = 12.0;
            ((IHasCost<Thing>)george).Cost["Equipment"].DirectCost = 14.0;

            ((IHasCost<Thing>)edna).Cost["Training"].DirectCost = 77.0;

            ((IHasCost<Thing>)dave).Cost.Reconcile();

            DumpCostData(alice);
        }

        private void SetUpTree() {
            alice = new Thing("Alice");
            bob = new Thing("Bob");
            charlie = new Thing("Charlie");
            dave = new Thing("Dave");
            edna = new Thing("Edna");
            frank = new Thing("Frank");
            george = new Thing("George");
            harry = new Thing("Harry");

            alice.AddChild(bob);
            alice.AddChild(charlie);
            alice.AddChild(dave);
            dave.AddChild(edna);
            dave.AddChild(frank);
            edna.AddChild(george);
        }

        private void DumpCostData(Thing alice) {
            _DumpCostData(alice, 0);
        }

        private void _DumpCostData(Thing thing, int indentLevel) {
            Console.WriteLine("{0}{1} - total cost {2}",StringOperations.Spaces(indentLevel*3),thing.Name, thing.Cost.Total);
            foreach ( string categoryName in Thing.COST_CATEGORIES.Select(n=>n.Name) ) {
                CostCategory<Thing> category = ((IHasCost<Thing>)thing).Cost[categoryName];
                Console.WriteLine("{0}{1} : {2:F2}\t{3:F2}\t{4:F2}", StringOperations.Spaces(15), category.Name, category.InheritedCost, category.DirectCost, category.ApportionedCost);
            }
            foreach (Thing child in thing.Children) {
                _DumpCostData(child, indentLevel + 1);
            }
        }

        class Thing : TreeNode<Thing>, IHasCost<Thing>, IHasName {
            public static List<CostCategory<Thing>> COST_CATEGORIES = new List<CostCategory<Thing>>()
            {   new CostCategory<Thing>("Personnel",true, true, n=>1.0/n.Children.Count()),
                new CostCategory<Thing>("Equipment",true, true, n=>1.0/n.Children.Count()),
                new CostCategory<Thing>("Training",true, true, n=>1.0/n.Children.Count()),
                new CostCategory<Thing>("Material ",true, true, n=>1.0/n.Children.Count())};

            private Cost<Thing> m_cost;
            private string m_name;

            public Thing(string name) {
                m_name = name;
                m_cost = new Cost<Thing>(this, COST_CATEGORIES);
                IsSelfReferential = true;
            }

            public string Name { get { return m_name; } }


            public Cost<Thing> Cost { get { return m_cost; } }
            public IHasCost<Thing> CostParent { get { return (IHasCost<Thing>)Parent; } }
            public IEnumerable<IHasCost<Thing>> CostChildren {
                get {
                    foreach ( IHasCost<Thing> thing in Children ) yield return thing;
                } 
            }
            public IHasCost<Thing> CostRoot { get { return Parent.Root.Payload; } }

        }

    }
}
