/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Graphs {
	/// <summary>
	/// Summary description for IEdgeReceiptManager.
	/// </summary>
	public interface IEdgeReceiptManager {
		void OnPreEdgeSatisfied(IDictionary graphContext, Edge edge);
	}

	public class MultiChannelEdgeReceiptManager : IEdgeReceiptManager {
		private Vertex m_vertex;
		private PreEdgesSatisfiedKey m_preEdgesSatisfiedKey = new PreEdgesSatisfiedKey();
		public MultiChannelEdgeReceiptManager(Vertex vertex){
			m_vertex = vertex;
		}

		#region IEdgeReceiptManager Members

		public void OnPreEdgeSatisfied(IDictionary graphContext, Edge edge) {
			IList preEdges = m_vertex.PredecessorEdges;
			
			if ( preEdges.Count < 2 ) { // If there's only one pre-edge, it must be okay to fire the vertex.
				m_vertex.FireVertex(graphContext);
			} else {
				object channelMarker = null;
				Hashtable channelHandlers = (Hashtable)graphContext[m_preEdgesSatisfiedKey];
				if ( channelHandlers == null ) {
					channelHandlers = new Hashtable();
					graphContext[m_preEdgesSatisfiedKey] = channelHandlers;
					foreach ( Edge _edge in preEdges ) {
						channelMarker = _edge.Channel;
						if ( !channelHandlers.Contains(channelMarker) ) {
							channelHandlers.Add(channelMarker,new ChannelMonitor(m_vertex,channelMarker));
						}
					}
				}

				channelMarker = edge.Channel;
				ChannelMonitor channelMonitor = (ChannelMonitor)channelHandlers[channelMarker];

				if ( channelMonitor.RegisterSatisfiedEdge(graphContext,edge) ) {
					graphContext.Remove(m_preEdgesSatisfiedKey);
					m_vertex.FireVertex(graphContext);
				}
			}
		}

		#endregion
	}

    class ChannelMonitor {
		private IVertex m_vertex;
		private object m_channelMarker;
		private ArrayList m_myEdges;
		private ArrayList m_preEdgesSatisfied;

		public ChannelMonitor(Vertex vertex, object channelMarker){
			m_vertex = vertex;
			m_channelMarker = channelMarker;
			m_myEdges = new ArrayList();
			m_preEdgesSatisfied = new ArrayList();
			foreach ( Edge e in vertex.PredecessorEdges ) {
				if ( channelMarker.Equals(e.Channel) ) m_myEdges.Add(e);
			}
		}

		public bool RegisterSatisfiedEdge(IDictionary graphContext, Edge edge){
				
			if ( !m_myEdges.Contains(edge) ) throw new ApplicationException("Unknown edge (" + edge + ") signaled completion to " + this);

			if ( m_preEdgesSatisfied.Contains(edge) ) {
				throw new ApplicationException("Edge (" + edge + ") signaled completion twice, to " + this);
			}
			m_preEdgesSatisfied.Add(edge);

			return ( m_preEdgesSatisfied.Count == m_myEdges.Count );
		}
	}
}
