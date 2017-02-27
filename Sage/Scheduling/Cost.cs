/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.Utility;
using System.Collections.Generic;
using System.Linq;
using Highpoint.Sage.SimCore;
#pragma warning disable 1587

// TODO: Code coverage to test, and then remove these disables.
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global


/// <summary>
/// The Cost namespace contains elements that are still works-in-progress. It should be used with caution.
/// </summary>
namespace Highpoint.Sage.Scheduling.Cost {

    public interface IHasCost<T> where T : IHasCost<T>, IHasName {
        Cost<T> Cost { get; }
        IHasCost<T> CostParent { get; }
        IEnumerable<IHasCost<T>> CostChildren { get; }
        IHasCost<T> CostRoot { get; }
    }

    /// <summary>
    /// Cost Categories are, e.g. Personnel, Equipment, and Materials. The same instances can be shared among many Cost elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CostCategory<T> where T : IHasCost<T>, IHasName {
        private readonly Func<T, double> m_getApportionmentFraction;
        public CostCategory(string name, bool apportionable, bool inheritable, Func<T, double> getApportionmentFraction) {
            Name = name;
            Apportionable = apportionable;
            Inheritable = inheritable;
            m_getApportionmentFraction = getApportionmentFraction;
            Clear();
        }

        public string Name { get; set; }
        public bool Apportionable { get; set; }
        public bool Inheritable { get; set; }

        public double DirectCost { get; set; }

        public double ApportionedCost { get; internal set; }

        public double InheritedCost { get; internal set; }

        public double ApportionableCost => Apportionable? DirectCost + ApportionedCost : 0.0;
        public double InheritableCost => Inheritable? DirectCost + InheritedCost : 0.0;

        public double Total => DirectCost + ApportionedCost + InheritedCost;

        /// <summary>
        /// Zeros all costs.
        /// </summary>
        public void Clear() {
            Reset();
            ApportionedCost = 0.0;
        }

        /// <summary>
        /// Zeros all derived (i.e. non-direct) costs.
        /// </summary>
        public void Reset() {
            InheritedCost = 0.0;
            ApportionedCost = 0.0;
        }

        public Func<T, double> ApportionmentFraction => m_getApportionmentFraction;

        public CostCategory<T> Clone() {
            return new CostCategory<T>(Name, Apportionable, Inheritable, m_getApportionmentFraction);
        }

        public override string ToString() {
            return $"{Name}, {(Apportionable ? "" : "not ")}apportionable, {(Inheritable ? "" : "not ")}inheritable";
        }
    }

    public class Cost<T> : TreeNode<Cost<T>> where T : IHasCost<T>, IHasName {

        private static readonly bool s_diagnostics = false;
        private bool m_initialized;
        private readonly IHasCost<T> m_master;
        private readonly List<CostCategory<T>> m_categories;

        public Cost(IHasCost<T> master, IEnumerable<CostCategory<T>> categories) {
            m_master = master;
            m_categories = new List<CostCategory<T>>();
            foreach (CostCategory<T> category in categories) {
                m_categories.Add(category.Clone());
            }
        }

        CostCategory<T> GetMatchingCategory(CostCategory<T> exemplar) {
            return m_categories.Single(n => n.Name.Equals(exemplar.Name));
        }

        public IEnumerable<CostCategory<T>> Categories => m_categories;

        public CostCategory<T> this[string categoryName] {
            get { return m_categories.Single(n => n.Name.Equals(categoryName)); }
        }

        public double Total { get { return m_categories.Sum(n=>n.Total); } }

        public bool Initialized { get { return m_initialized; } set { m_initialized = value; } }

        public void Reset() {
            foreach (CostCategory<T> category in Categories) {
                category.Reset();
                m_initialized = false;
            }
        }

        public void Subsume() {
            foreach (CostCategory<T> category in Categories) {
                if (category.Inheritable) {
                    // Pull inheritable costs up.
                    foreach (Cost<T> child in m_master.CostChildren.Select(n => n.Cost)) {
                        CostCategory<T> childsCategory = child.GetMatchingCategory(category);
                        if (s_diagnostics) {
                            Console.WriteLine("{0} is inheriting {1} cost {2} from {3}.",
                                ((IHasName)m_master).Name,
                                category.Name,
                                childsCategory.InheritableCost,
                                ((IHasName)child.m_master).Name);
                        }
                        category.InheritedCost += (childsCategory.InheritableCost);
                    }
                }
            }
        }
        public void Apportion() {
            foreach (CostCategory<T> category in m_categories) {
                if (category.Apportionable) {
                    // Push my apportionable costs down
                    foreach (Cost<T> child in m_master.CostChildren.Select(n => n.Cost)) {
                        CostCategory<T> childsCategory = child.GetMatchingCategory(category);
                        double portion = childsCategory.ApportionmentFraction((T)child.m_master);
                        if (s_diagnostics) {
                            Console.WriteLine("{0} is assigning {1:0%} of its {2} apportionable cost {3} to {4}.",
                                ((IHasName)m_master).Name,
                                portion,
                                category.Name,
                                category.ApportionableCost,
                                ((IHasName)child.m_master).Name);
                        }
                        childsCategory.ApportionedCost = portion * category.ApportionableCost;
                    }
                }
            }
        }


        public void Reconcile(bool startAtRoot = true) {
            if (startAtRoot && m_master.CostParent != null) {
                m_master.CostRoot.Cost.Reconcile();
            } else {
                // I'm the root. Reconcile all below me.
                _Reconcile();
            }
        }
        private void _Reconcile() {

            foreach (Cost<T> child in m_master.CostChildren.Select(n => n.Cost)) {
                foreach (CostCategory<T> childsCategory in child.m_categories) {
                    childsCategory.Clear();
                }
            }
    #region Apportion each category downward
            Apportion();
    #endregion

    #region Reconcile Children
            foreach (Cost<T> child in m_master.CostChildren.Select(n => n.Cost)) {
                child._Reconcile();
            }
    #endregion

    #region Inherit each category upward
            Subsume();
    #endregion

            //m_valid = true;
        }
    }
}
