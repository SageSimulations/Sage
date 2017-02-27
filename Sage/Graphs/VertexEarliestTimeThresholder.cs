/* This source code licensed under the GNU Affero General Public License */


using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore; // For executive.

namespace Highpoint.Sage.Graphs {

    /// <summary>
    /// When attached to a vertex in a graph, this object ensures that the vertex does not fire before a specified simulation time.
    /// </summary>
    public class VertexEarliestTimeThresholder {

        #region Private Fields

        private TriggerDelegate m_vertexTrigger;
        private IModel m_model;
        private DateTime m_earliest;
        private Vertex m_vertex;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:VertexEarliestTimeThresholder"/> class.
        /// </summary>
        /// <param name="model">The model in which the <see cref="T:VertexEarliestTimeThresholder"/> exists.</param>
        /// <param name="vertex">The vertex that this object will control.</param>
		public VertexEarliestTimeThresholder(IModel model, Vertex vertex)
			:this(model,vertex,DateTime.MinValue){}

        /// <summary>
        /// Creates a new instance of the <see cref="T:VertexEarliestTimeThresholder"/> class.
        /// </summary>
        /// <param name="model">The model in which the <see cref="T:VertexEarliestTimeThresholder"/> exists.</param>
        /// <param name="vertex">The vertex that this object will control.</param>
        /// <param name="earliest">The earliest time that the vertex should be allowed to fire.</param>
		public VertexEarliestTimeThresholder(IModel model, Vertex vertex, DateTime earliest){
			m_earliest = earliest;
			m_model = model;
			m_vertex = vertex;
			m_vertexTrigger = vertex.FireVertex;
			vertex.FireVertex = new TriggerDelegate(FireTheVertex);
		}

        /// <summary>
        /// Gets or sets the earliest time that the vertex should be allowed to fire.
        /// </summary>
        /// <value>The earliest time that the vertex should be allowed to fire.</value>
		public DateTime Earliest { get { return m_earliest; } set { m_earliest = value; } }

		private void FireTheVertex(IDictionary graphContext){
			if ( m_model.Executive.Now < m_earliest ) {
				TimeSpan ts = (m_earliest-m_model.Executive.Now);
				// Trace.WriteLine(m_model.Executive.Now + " : " + "Will fire vertex " + m_vertex.Name + " after a delay of " + string.Format("{0:d2}:{1:d2}:{2:d2}",ts.Hours,ts.Minutes,ts.Seconds));
				ExecEventType eet = m_model.Executive.CurrentEventType;
				m_model.Executive.RequestEvent(new ExecEventReceiver(_FireTheVertex),m_earliest,0,graphContext,eet);
			} else {
				// Trace.WriteLine(m_model.Executive.Now + " : " + "Firing vertex " + m_vertex.Name);
				m_vertexTrigger(graphContext);
			}
		}

		private void _FireTheVertex(IExecutive exec, object graphContext){
			FireTheVertex((IDictionary)graphContext);
		}

	}
}