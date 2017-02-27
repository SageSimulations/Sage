/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using System.Text;

namespace Highpoint.Sage.Graphs.PFC {
    public class PfcDiagnostics {
        private PfcDiagnostics() { } // Statics only.

        /// <summary>
        /// Gets the SFC's diagnostic report.
        /// </summary>
        /// <param name="pfc">The ProcedureFunctionChart to be reported on.</param>
        /// <returns></returns>
        public string PfcDiagnosticReport(IProcedureFunctionChart pfc) {
            StringBuilder sb = new StringBuilder();

            sb.Append(PfcSummaryInfo(pfc));


            return sb.ToString();
        }

        /// <summary>
        /// Gets the PFC's summary info in string format.
        /// </summary>
        /// <param name="pfc">The pfc.</param>
        /// <returns></returns>
        public static string PfcSummaryInfo(IProcedureFunctionChart pfc) {
            StringBuilder sb = new StringBuilder();
            sb.Append(pfc.GetType().FullName + " : " + pfc.Name + "\r\n");

            sb.Append("\tSteps       : " + pfc.Steps.Count + "\r\n");
            sb.Append("\tLinks       : " + pfc.Edges.Count + "\r\n");
            sb.Append("\tTransitions : " + pfc.Transitions.Count + "\r\n");
            sb.Append("\tNodes       : " + pfc.Nodes.Count + "\r\n");

            sb.Append("\tNode Hierarchy\r\n");

            DumpElementContents(pfc, sb, 1);

            sb.Append("\r\n");

            return sb.ToString();
        }

        public static string GetStructure(IProcedureFunctionChart pfc) {
            StringBuilder sb = new StringBuilder();
            PfcLinkElementList llist = pfc.Links;
            llist.Sort(delegate(IPfcLinkElement le1, IPfcLinkElement le2) { return Comparer.Default.Compare(le1.Name,le2.Name); } );

            foreach ( IPfcLinkElement linkElement in llist ) {
                string predName = linkElement.Predecessor == null ? "<null>" : linkElement.Predecessor.Name;
                string linkName = linkElement.Name;
                string parentName = linkElement.Parent == null ? "<null>" : linkElement.Parent.Name;
                string succName = linkElement.Successor == null ? "<null>" : linkElement.Successor.Name;

                sb.Append("{" + predName + "-->[" + linkName + "(" + /*parentName*/"SFC 1.Root" + ")]-->" + succName + "}\r\n");
            }

            return sb.ToString();
        }

        public static string GetDeepStructure(IProcedureFunctionChart ipfc) {
            StringBuilder sb = new StringBuilder();
            GetDeepStructure(ipfc, 0, ref sb);
            return sb.ToString();
        }

        private static void GetDeepStructure(IProcedureFunctionChart ipfc, int indent, ref StringBuilder sb) {
            string indents = Utility.StringOperations.Spaces(indent*3);
            foreach (IPfcStepNode step in ipfc.Steps) {
                sb.Append(indents + step.Name + "\r\n");
                if (step.Actions != null) {
                    foreach (string key in step.Actions.Keys) {
                        sb.Append(indents + "[" + key + "]\r\n");
                        GetDeepStructure(step.Actions[key], indent + 1, ref sb);
                    }
                }
            }
        }

        static void DumpElementContents(IProcedureFunctionChart pfc, StringBuilder sb, int indent) {
            foreach (IPfcElement element in pfc.Elements) {
                DumpElementContents(element, sb, indent);
            }
        }

        static void DumpElementContents(IPfcElement element, StringBuilder sb, int indent) {
            sb.Append("\r\n");

            for ( int i = 0 ; i < indent ; i++ ) {
                sb.Append("\t");
            }
            if (element == null) {
                sb.Append("<null>");
            } else {
                sb.Append(element.Name);
                if (element is IPfcStepNode) {
                    IPfcStepNode node = (IPfcStepNode)element;
                    sb.Append(" [ " + node.Predecessors.Count + ", " + node.Successors.Count + " ]");
                    foreach (IProcedureFunctionChart childPfc in node.Actions.Values) {
                        foreach (IPfcElement child in childPfc.Elements) {
                            DumpElementContents(child, sb, indent + 1);
                        }
                    }
                }
            }
        }

    }
}
