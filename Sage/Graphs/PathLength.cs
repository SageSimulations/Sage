/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.Graphs.Analysis {

	/// <summary>
	/// This class provides the analytical capability to discern the shortest 
	/// path from one vertex to another, or from one edge to another. Its
	/// methods are static, but since all of its data members are locals, it
	/// may be considered threadsafe.
	/// </summary>
    public class PathLength	{

        //private static readonly bool m_diagnostics = false;
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PathLength");

        public static int ShortestPathLength(Edge from, Edge to){
            int spl = ShortestPathLength(from.PostVertex,to.PreVertex);
            return spl==int.MaxValue?spl:spl+1;
        }

        public static int ShortestPathLength(Vertex from, Vertex to){
            if ( from == null || to == null ) return int.MaxValue;
            if ( from == to ) return 0;
            ArrayList visitedNodes = new ArrayList();
            return _ShortestPathLength(from, to, ref visitedNodes);
        }

        private static int _ShortestPathLength(Vertex from, Vertex to, ref ArrayList visitedNodes){
            if ( from == null || to == null ) return int.MaxValue;

            if ( s_diagnostics ) _Debug.WriteLine(String.Format("\t\tProbing outward from {0}, looking for {1}.", from, to));
            if ( from == to ) {
                if ( s_diagnostics ) _Debug.WriteLine("Probe found {0}! Returning pathlength.",from.ToString());
                return 0;
            }

            if ( visitedNodes.Contains(from)) {
                if ( s_diagnostics ) _Debug.WriteLine("Located a cycle");
                return int.MaxValue;
            }
            visitedNodes.Add(from);

            int shortestPathLength = int.MaxValue;
            
            if ( s_diagnostics ) {
                /***************************************************************************************/
                _Debug.WriteLine(String.Format("\t\t{0} has {1} successor edges...",from,from.SuccessorEdges.Count));
                _Debug.Write("\t\t");
                foreach ( Edge edge in from.SuccessorEdges ) {
                    _Debug.Write("\t"+edge);
                }
                _Debug.WriteLine("");
                _Debug.WriteLine(String.Format("\t\t{0} has {1} predecessor edges...",from,from.PredecessorEdges.Count));
                _Debug.Write("\t\t");
                foreach ( Edge edge in from.PredecessorEdges ) {
                    _Debug.Write("\t"+edge);
                }
                _Debug.WriteLine("");
                /****************************************************************************************/
            }
            foreach ( Edge edge in from.SuccessorEdges ) {
                int pathLength = _ShortestPathLength(edge.PostVertex,to, ref visitedNodes);
                if ( pathLength < shortestPathLength ) shortestPathLength = pathLength;
            }
            if ( shortestPathLength==int.MaxValue) return int.MaxValue;
            return shortestPathLength+1;
        }
	}
}
