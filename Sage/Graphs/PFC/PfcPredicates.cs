/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Graphs.PFC {

    public static class PfcPredicates {
        public static Predicate<IPfcElement> StepsOnly = new Predicate<IPfcElement>(delegate(IPfcElement element) { return element.ElementType.Equals(PfcElementType.Step); });
        public static Predicate<IPfcElement> LinksOnly = new Predicate<IPfcElement>(delegate(IPfcElement element) { return element.ElementType.Equals(PfcElementType.Link); });
        public static Predicate<IPfcElement> TransitionsOnly = new Predicate<IPfcElement>(delegate(IPfcElement element) { return element.ElementType.Equals(PfcElementType.Transition); });
        public static Predicate<IPfcElement> NodesOnly = new Predicate<IPfcElement>(delegate(IPfcElement element) { return element.ElementType.Equals(PfcElementType.Transition) || element.ElementType.Equals(PfcElementType.Step); });
        public static Predicate<T> ByName<T>(string targetName) where T : SimCore.IHasIdentity {
            return new Predicate<T>(delegate(T element) { return element.Name.Equals(targetName); });
        }
        public static Predicate<T> ByGuid<T>(Guid targetGuid) where T : SimCore.IHasIdentity {
            return new Predicate<T>(delegate(T element) { return element.Guid.Equals(targetGuid); });
        }
    }
}
