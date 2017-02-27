/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.Graphs.Analysis {

	/// <summary>
	/// This interface is implemented by any edge in a graph where the edge has
	/// duration, and therefore can be used as a part of the computations necessary
	/// to performing a PERT analysis.
	/// NOTE: WORKS-IN-PROGRESS
	/// </summary>
    public interface ISupportsPertAnalysis : ISupportsCpmAnalysis {
		/// <summary>
		/// Optimistic duration is the minimum amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
		/// </summary>
		/// <returns>The optimistic duration for this task.</returns>
		TimeSpan GetOptimisticDuration();
		/// <summary>
		/// Pessimistic duration is the maximum amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
		/// </summary>
		/// <returns>The pessimistic duration for this task.</returns>
		TimeSpan GetPessimisticDuration();
    }

	/// <summary>
	/// This class is able, in its instances, to perform a PERT analysis, including
	/// determination of critical paths, and their tasks' mean and variances.
	/// NOTE: WORKS-IN-PROGRESS
	/// </summary>
    public class PertAnalyst : CpmAnalyst {

        double m_criticalPathMean;
        double m_criticalPathVariance;
        ArrayList m_criticalPath;

        //public PERTAnalyst(Vertex start, Vertex finish):base(start,finish){}
        public PertAnalyst(Edge edge):base(edge){}

        public override void Analyze(){
            base.Analyze();
            m_criticalPath = new ArrayList();
            DetermineMeanAndVarianceOfCriticalPath();
        }

        public void DetermineMeanAndVarianceOfCriticalPath(){
            Vertex here = Start;
            double mean = 0.0;
            double vari = 0.0;

            while ( here != null ) {
                here = NextVertexInCriticalPath(here,ref mean,ref vari);
                if ( here.Equals(Finish) ) {
                    m_criticalPathMean = mean;
                    m_criticalPathVariance = vari;
                    return;
                }
            }
            throw new ApplicationException("There is no path from the start to the finish vertices." +
                " This should NOT happen, as CPM Analysis has already taken place, and FOUND such a path.");
            
        }

        protected Vertex NextVertexInCriticalPath(Vertex vertex, ref double mean, ref double variance){
            Edge targetEdge = null;
            foreach ( Edge edge in vertex.SuccessorEdges ) {
                targetEdge = edge;
                if ( targetEdge is Ligature ) targetEdge = edge.PostVertex.PrincipalEdge;
                EdgeData ed = (EdgeData)Edges[targetEdge];
                if ( ed == null ) continue;
                if ( IsCriticalPath(targetEdge) ) {
                    m_criticalPath.Add(targetEdge);
                    mean += ed.MeanDuration;
                    variance += ed.Variance2;
                    return targetEdge.PostVertex;
                }
            }

            if ( targetEdge.Equals(Finish.PrincipalEdge) ) return Finish;

            throw new ApplicationException("The vertex " + vertex.Name + " is not on the critical path.");
        }

        public ArrayList CriticalPath { get { return ArrayList.ReadOnly(m_criticalPath); } }
        public TimeSpan  CriticalPathMean { get { return TimeSpan.FromTicks((long)m_criticalPathMean); } }
        public TimeSpan  CriticalPathVariance { 
            get { 
                long ticks = (long)Math.Sqrt(m_criticalPathVariance);
                return TimeSpan.FromTicks(ticks); 
            }
        }
    }
}
