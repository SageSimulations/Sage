/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using System.Collections;
using System.Text;
using System.IO;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.Analysis {

	/// <summary>
	/// Implemented by an object that can take part in a CPM (non-probabilistic)
	/// time cycle analysis. Presumption is that the implementer is also capable
	/// of acting as an edge or vertex in a network.
	/// </summary>
	public interface ISupportsCpmAnalysis {
		/// <summary>
		/// Nominal duration is the average amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
		/// </summary>
		/// <returns>The nominal duration for this task.</returns>
		TimeSpan GetNominalDuration();
		//double GetCost(string measure);
	}


	
	/// <summary>
	/// An object that is capable of examining a network of edges that implement
	/// ISupportsCPMAnalysis and determining CPM data (earliest and latest firing
	/// times for each vertex.<p></p>
	/// The algorithm here is that we start at the beginning, traversing each path, and
	/// advancing elapsed time by the duration of the edge as we cross each edge. We 
	/// record the earliest time we reach each vertex - that is the earliest possible
	/// start for the principal edge if it is a pre-vertex, and the earlies possible
	/// completion if it is a postVertex. For the second pass, we take the "earliest"
	/// time that the finish vertex was reached, which is the earliest that the whole
	/// recipe can be completed. Using that as the overall duration, we traverse back-
	/// wards, subtracting the duration of each edge and assigning the time of arrival
	/// at each vertex as the "latest possible" start or finish.
	/// This works great until you start constraining vertices, with the Vertex
	/// Synchronizer. In this case, the synchronizer delineates a set of synchronized
	/// vertices, none of which can fire until all are ready to be fired. So the
	/// earliest a set of synchronized vertices can fire is the LATEST 'earliest' time
	/// that any of the member vertices can fire. So, we have a SynchronizerData object
	/// that tracks how many vertices in its member set have been visited, and does not
	/// allow traversal beyond that set of vertices until all of them have been visited
	/// and their 'earliest' settings set to the latest time that any of them were
	/// visited. Then, 'elapsedTime' is set to that 'latest' time, and all member vertices
	/// are used, in turn, as roots from which to probe forward.
	/// </summary>
	public class CpmAnalyst {

		private static readonly bool s_permitUnknownEdges = Diagnostics.DiagnosticAids.Diagnostics("CPMAnalyst.PermitUnknownEdges");
		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("CPMAnalyst");
		private static readonly bool s_logEdgeNotFoundError = Diagnostics.DiagnosticAids.Diagnostics("CPMAnalyst.LogEdgeNotFoundError");
		private static readonly bool s_diagnosticsValidation = Diagnostics.DiagnosticAids.Diagnostics("CPMAnalyst.PerformValidation");
		private static readonly ArrayList s_emptylist = new ArrayList();

		private bool m_analyzed = false;
		private Stack m_traceStack;

		/// <summary>
		/// The start vertex for the section of the graph that is to be analyzed.
		/// </summary>
		protected Vertex Start;
		/// <summary>
		/// The finish vertex for the section of the graph that is to be analyzed.
		/// </summary>
		protected Vertex Finish;
		/// <summary>
		/// A hashtable of edge-related data compiled by this analyst. 
		/// </summary>
		protected Hashtable Edges;
		/// <summary>
		/// A hashtable of vertex-related data compiled by this analyst. 
		/// </summary>
		protected Hashtable Vertices;
		/// <summary>
		/// A hashtable of vertices that this analyst knows to be pegged at certain times. 
		/// </summary>
		protected Hashtable VertexPegs;
		/// <summary>
		/// A hashtable of synchronizer-related data compiled by this analyst. 
		/// </summary>
		protected Hashtable Synchronizers;
		/// <summary>
		/// Creates a CPMAnalyst that analyzes a given edge and all of its children.
		/// </summary>
		/// <param name="edge">The edge whose start and finish vertices are to be the subject of this analyst's 
		/// evaluation.</param>
		public CpmAnalyst(Edge edge){
			Start = edge.PreVertex;
			Finish = edge.PostVertex;
			m_traceStack = new Stack();
			Reset();
		}

		/// <summary>
		/// Resets the analyst so that it can perform its analysis again.
		/// </summary>
		public void Reset(){
			Edges = new Hashtable();
			Vertices = new Hashtable();
			Synchronizers = new Hashtable();
			m_analyzed = false;
		}

		protected void ClearBookKeeping(){
			// Must clear all "Latest" times from the graph, starting at the Units' post vertices.
			foreach ( VertexData vd in Vertices.Values ) {
				vd.ResetReverseVisitCounts();
				vd.ResetForwardVisitCounts();
			}

			foreach ( SynchronizerData sd in Synchronizers.Values ) {
				sd.ResetForwardVisitCounts();
				sd.ResetReverseVisitCounts();
			}
		}

		public void PegVertex(Vertex vertex, TimeSpan offsetFromStart){
			if ( VertexPegs == null ) VertexPegs = new Hashtable();
			if ( VertexPegs.Contains(vertex) ) VertexPegs.Remove(vertex);
			VertexPegs.Add(vertex,offsetFromStart.Ticks);

		}
		public void ClearVertexPegs(){
			VertexPegs = null;
		}

		/// <summary>
		/// Performs the CPM analysis that this object has been configured for.
		/// </summary>
		public virtual void Analyze(){
			Reset();

			m_traceStack.Clear();
			ProbeForward(Start,0);

			VertexData vd = (VertexData)Vertices[Finish];
			if ( vd == null ) {
				throw new ApplicationException("There is no path from the start to the finish vertices.");
			}

			if ( s_diagnostics ) _Debug.WriteLine("Finish vertex is " + Finish.Name);
			if ( s_diagnostics ) _Debug.WriteLine(vd.ToString());
            
			m_traceStack.Clear();
			ClearBookKeeping();
			ProbeBackward(Finish,vd.Earliest);

			if ( FixUpSynchedTasksPostVertices() ) {
				m_traceStack.Clear();
				ClearBookKeeping();
				ProbeBackward(Finish,vd.Earliest);
				ValidateResults(Start);
				m_analyzed = true;
			} else {
				throw new AnalysisFailedException("DAG Analysis Failed - check model errors for reasons.");
			}
		}

		public bool Analyzed { get { return m_analyzed; } }

		/// <summary>
		/// Recursively traverses forward from a given vertex, adding the earliest-possible
		/// arrival time to each vertex as it goes.
		/// </summary>
		/// <param name="vertex">the vertex forward from which we will probe.</param>
		/// <param name="elapsedTime">the current elapsed time.</param>
		protected void ProbeForward(Vertex vertex, long elapsedTime){
			if ( s_diagnostics ) _Debug.WriteLine(new DateTime(elapsedTime) + " : Probing forward to vertex " + vertex.Name + " at " + string.Format("{0:f2}",TimeSpan.FromTicks(elapsedTime).TotalMinutes));

			if ( VertexPegs != null && VertexPegs.Contains(vertex) ) {
				elapsedTime = (long)VertexPegs[vertex];
			}

			IList nextEdges;
			VertexData vertexData = GetVertexData(vertex);
			if ( vertex.Synchronizer == null ) {
				if ( elapsedTime > vertexData.Earliest ) vertexData.Earliest = elapsedTime;
				nextEdges = vertexData.GetNextEdgesForward();
			} else { // There is a synchronizer.
				SynchronizerData sd = GetSynchronizerData(vertex);
				sd.RegisterForwardVisit(vertex, elapsedTime);
				nextEdges = sd.NextEdgesForward(Vertices,ref elapsedTime); // PCB: Propagates earlier time?
			}

			if ( s_diagnostics ) _Debug.WriteLine(vertex.Name + " earliest " + string.Format("{0:f2}",(new TimeSpan(vertexData.Earliest).TotalMinutes)));

			m_traceStack.Push(vertex);
			foreach ( Edge edge in nextEdges ) {
				EdgeData edgeData = GetEdgeData(edge);
				if ( edge is ISupportsCpmAnalysis ) {
					edgeData.NominalDuration = ((ISupportsCpmAnalysis)edge).GetNominalDuration().Ticks;
				}
				if ( s_diagnostics ) _Debug.WriteLine("From vertex " + vertex.Name + ", we look forward " + TimeSpan.FromTicks(edgeData.NominalDuration) + " to " + edge.PostVertex.Name + ".");
				ProbeForward(edge.PostVertex,vertexData.Earliest+edgeData.NominalDuration);
			}
			Debug.Assert(m_traceStack.Pop()==vertex);
		}

		/// <summary>
		/// Recursively traverses backward from a given vertex, adding the latest-possible
		/// arrival time to each vertex as it goes.
		/// </summary>
		/// <param name="vertex">The vertex from which the backward probing is to be done.</param>
		/// <param name="elapsedTime">The time that has elapsed thus far in the backward traversal.</param>
		protected void ProbeBackward(Vertex vertex, long elapsedTime){
			string fromVertexName = m_traceStack.Count>0?((Vertex)m_traceStack.Peek()).Name:"<root>";
//			if ( vertex.Name.Equals("D : Sample4:Post") ) {
//				Console.WriteLine("Tracing back to " + vertex.Name + " from ... ");
//				foreach ( Vertex v in m_traceStack ) {
//					Console.WriteLine("\t\t" + v.Name);
//				}
//			}
			if ( s_diagnostics ) {
				Trace.Write(new DateTime(elapsedTime) + " : Probing backward from " + fromVertexName + " to vertex " + vertex.Name + " at " 
					+ string.Format("{0:f2}",TimeSpan.FromTicks(elapsedTime).TotalMinutes));
			}
            
			if ( VertexPegs != null && VertexPegs.Contains(vertex) )  elapsedTime = (long)VertexPegs[vertex];

			IList nextEdges;
			VertexData vertexData = GetVertexData(vertex);
			if ( vertex.Synchronizer == null ) {
				if ( elapsedTime < vertexData.Latest ) vertexData.Latest = elapsedTime;
				nextEdges = vertexData.GetNextEdgesReverse();
				if ( s_diagnostics ) _Debug.WriteLine(nextEdges.Count>0?" - authorized to proceed.":" - this vertex is not yet satisfied (" + vertexData.RevSatStatus + ").");
			} else {
				SynchronizerData sd = GetSynchronizerData(vertex);
				if ( vertexData.GetNextEdgesReverse().Count>0 ) sd.RegisterBackwardVisit(vertex, elapsedTime);
				nextEdges = sd.NextEdgesBackward(Vertices, ref elapsedTime);
				if ( s_diagnostics ) _Debug.WriteLine(nextEdges.Count>0?" - synchronizer authorized us to proceed.":" - this vertex has a synchronizer that is not yet satisfied.");
			}

			if ( s_diagnostics ) _Debug.WriteLine("Setting " + vertex.Name + " latest to " + string.Format("{0:f2}",TimeSpan.FromTicks(elapsedTime).TotalMinutes));

			m_traceStack.Push(vertex);
			foreach ( Edge edge in nextEdges ) {
				EdgeData edgeData = (EdgeData)Edges[edge];
				if ( edgeData == null ) {
					edgeData = new EdgeData(edge);
					Edges.Add(edge,edgeData);
				}
				if ( edge is ISupportsCpmAnalysis ) {
					edgeData.NominalDuration = ((ISupportsCpmAnalysis)edge).GetNominalDuration().Ticks;
				}
				ProbeBackward(edge.PreVertex,vertexData.Latest-edgeData.NominalDuration);
			}
			Debug.Assert(m_traceStack.Pop()==vertex);
		}

		/// <summary>
		/// This method visits the post vertices of all edges that have synchronizers
		/// attached to their prevertices, and modifies them to reflect that their latest
		/// time is always the latest of their prevertex plus the edge's nominal duration.
		/// </summary>
		protected bool FixUpSynchedTasksPostVertices(){

			// So here's the tricky part of this backward probing. Suppose a vertex is a
			// post-vertex for an edge whose pre-vertex has a synchronizer. The time at
			// which we arrive at the post-vertex is almost irrelevant - if we were to
			// cite that as the latest-possible, then as we cited the latest-possible for
			// the pre-vertex, we might end up with a situation where the latest possible
			// start and the latest possible finish were not separated by precisely the
			// edge's nominal duration. Therefore, we'd be saying (for example) that a three
			// hour task could start no later than 5/16/1997, 5:00 PM, and would finish no
			// later than 5/16/1997, 9:00 PM. 

			// This fix-up goes through and modifies all of the finish times so that they are
			// precisely the latest start time plus the nominal duration of the edge. This is
			// almost always a later "latest possible finish" than the backward probing
			// analysis would have determined.

			// Another issue we run into here, is that the parent task, if joined with the child
			// task, should have it's latest finish time adjusted, too, so that the parent shows
			// as not having finished until the child finishes, and as finishing precisely WHEN
			// the child finishes.

			// This is roughly analogous to pre vertex synchronizers, so we really ought to
			// handle this through a consistent mechanism. 

			foreach ( SynchronizerData sd in Synchronizers.Values ) {
				foreach ( Vertex pre in sd.Synchronizer.Members ) {
					VertexData preData = (VertexData)Vertices[pre];
					Vertex post = pre.PrincipalEdge.PostVertex;
					VertexData postData = (VertexData)Vertices[post];
					EdgeData ed = (EdgeData)Edges[pre.PrincipalEdge];
					if ( ed == null ) { 
						LogEdgeNotFoundError(pre.PrincipalEdge.Name + " not found in DAG Analysis.",pre.PrincipalEdge);
						return false;
					}
					postData.Latest = preData.Latest + ed.NominalDuration;
					
					if ( s_diagnostics ) {
						_Debug.WriteLine("Resetting " + post.Name + " latest time to " + pre.Name + "'s latest (" 
							+ string.Format("{0:f2}",TimeSpan.FromTicks(preData.Latest)) + ") + "
							+ pre.PrincipalEdge.Name + "'s nominal duration of " 
							+ string.Format("{0:f2}",TimeSpan.FromTicks(ed.NominalDuration))); 
					}
				}
			}

			return true;
		}

		private void DumpDiagnostics(){
			Console.WriteLine("Vertices:");
			foreach ( Vertex v in Vertices.Keys ) {
				Console.WriteLine(v.Name);
			}

			Console.WriteLine("Edges:");
			foreach ( Edge e in Edges.Keys ) {
				Console.WriteLine(e.Name);
			}
		}

		private Hashtable m_verifiedEdges;
		private StringBuilder m_sb;
		private int m_errorCount;
		private void ValidateResults(Vertex startVertex){
			
			if ( s_diagnosticsValidation ) {
				_Debug.WriteLine("Performing post-analysis validation of graph timecycle data.");
				
				m_errorCount = 0;
				m_sb = new StringBuilder();
				m_verifiedEdges = new Hashtable();
				
				_ValidateResults(startVertex);

				string msg = "Post-analysis validation of graph timecycle data completed with " + m_errorCount + " error" + (m_errorCount==1?".":"s.");

				if ( m_errorCount > 0 ) {
                    throw new TimeCycleException("Timecycle error:\r\n" + msg + "\r\n\r\n" + m_sb);
				}

				_Debug.WriteLine(msg);

			}
		}

		private void _ValidateResults(Vertex startVertex){
			//_Debug.WriteLine("Validating from vertex " + startVertex.Name);
			foreach ( Edge edge in startVertex.SuccessorEdges ) {
				//_Debug.WriteLine("\tValidating edge " + edge.Name);
				if ( !m_verifiedEdges.Contains(edge) ) {
					m_verifiedEdges.Add(edge,edge);
					VertexData vdPre  = (VertexData)Vertices[startVertex];
					EdgeData   ed     = (EdgeData)Edges[edge];
					VertexData vdPost = (VertexData)Vertices[edge.PostVertex];

					if ( vdPre != null && vdPost != null && ed != null ) {
						string svdPreEarliest = string.Format("{0:F2}",TimeSpan.FromTicks(vdPre.Earliest).TotalMinutes);
						string svdPreLatest = string.Format("{0:F2}",TimeSpan.FromTicks(vdPre.Latest).TotalMinutes);
						string svdPostEarliest = string.Format("{0:F2}",TimeSpan.FromTicks(vdPost.Earliest).TotalMinutes);
						string svdPostLatest = string.Format("{0:F2}",TimeSpan.FromTicks(vdPost.Latest).TotalMinutes);
						// Apply heuristics.
						if ( vdPre.Earliest + ed.NominalDuration - vdPost.Earliest > .001 ) {
							m_errorCount++;
							if ( s_diagnosticsValidation ) m_sb.Append(edge.Name + " earliest-time anomaly - earliest start is " + svdPreEarliest + ", duration is " + ed.NominalDuration + ", and earliest finish is " + svdPostEarliest + ".\r\n");
						}
						if ( vdPre.Latest   + ed.NominalDuration - vdPost.Latest   > .001 ) {
							m_errorCount++;
							if ( s_diagnosticsValidation ) m_sb.Append(edge.Name + " latest-time anomaly - latest start is " + svdPreLatest + ", duration is " + ed.NominalDuration + ", and latest finish is " + svdPostLatest + ".\r\n");
						}
						if ( vdPre.Earliest > vdPre.Latest ) {
							m_errorCount++;
							if ( s_diagnosticsValidation ) m_sb.Append(edge.Name + "'s earliest start (" + svdPreEarliest + ") is later than its earliest finish (" + svdPostEarliest + ").\r\n"); 
						}
						if ( vdPost.Earliest > vdPost.Latest ) {
							m_errorCount++;
							if ( s_diagnosticsValidation ) m_sb.Append(edge.Name + "'s latest start (" + svdPreLatest + ") is later than its latest finish (" + svdPostLatest + ").\r\n");
						}
					}
					_ValidateResults(edge.PostVertex);
				}
			}
		}

		#region Edge Data Accessors
		/// <summary>
		/// After an analysis has been performed, this accessor method will get the earliest
		/// start time observed for a specified edge.
		/// </summary>
		/// <param name="edge">The edge for which the start time is desired.</param>
		/// <returns>The earliest start time observed for the specified edge.</returns>
		public long GetEarliestStart(Edge edge) {
			if ( !m_analyzed ) Analyze(); 
			VertexData vd = (VertexData)Vertices[edge.PreVertex];
			if ( vd != null ) return vd.Earliest;
			if ( s_permitUnknownEdges ) return 0;
			return _OnEdgeNotFound(edge);
		}

		/// <summary>
		/// After an analysis has been performed, this accessor method will get the earliest
		/// finish time observed for a specified edge.
		/// </summary>
		/// <param name="edge">The edge for which the finish time is desired.</param>
		/// <returns>The earliest finish time observed for the specified edge.</returns>
		public long GetEarliestFinish(Edge edge){
			if ( !m_analyzed ) Analyze(); 
			VertexData vd = (VertexData)Vertices[edge.PostVertex];
			if ( vd != null ) return vd.Earliest;
			if ( s_permitUnknownEdges ) return 0;
			return _OnEdgeNotFound(edge);
		}

		/// <summary>
		/// After an analysis has been performed, this accessor method will get the latest
		/// start time observed for a specified edge.
		/// </summary>
		/// <param name="edge">The edge for which the start time is desired.</param>
		/// <returns>The latest start time observed for the specified edge.</returns>
		public long GetLatestStart(Edge edge){
			if ( !m_analyzed ) Analyze(); 
			VertexData vd = (VertexData)Vertices[edge.PreVertex];
			if ( vd != null ) return vd.Latest;
			if ( s_permitUnknownEdges ) return 0;
			return _OnEdgeNotFound(edge);
		}

		/// <summary>
		/// After an analysis has been performed, this accessor method will get the latest
		/// finish time observed for a specified edge.
		/// </summary>
		/// <param name="edge">The edge for which the finish time is desired.</param>
		/// <returns>The latest finish time observed for the specified edge.</returns>
		public long GetLatestFinish(Edge edge){
			if ( !m_analyzed ) Analyze(); 
			VertexData vd = (VertexData)Vertices[edge.PostVertex];
			if ( vd != null ) return vd.Latest;
			if ( s_permitUnknownEdges ) return 0;
			return _OnEdgeNotFound(edge);
		}

		/// <summary>
		/// This method returns the number of ticks in the acceptable slip of an edge. This is
		/// the difference between the earliest and latest permissible start times for the edge's
		/// preVertex.
		/// </summary>
		/// <param name="edge">The edge for which the slip time is desired.</param>
		/// <returns>The slip time, in ticks, that was recorded for the specified edge.</returns>
		public long GetAcceptableSlip(Edge edge){
			if ( !m_analyzed ) Analyze(); 
			VertexData vd = (VertexData)Vertices[edge.PreVertex];
			if ( vd != null ) return vd.Latest-vd.Earliest;
			if ( s_permitUnknownEdges ) return 0;
			return _OnEdgeNotFound(edge);
		}

		private long _OnEdgeNotFound(Edge edge){
			string msg = "CPM Analyst was asked for data on " + edge.Name + ",";
			bool hasPreData = ((VertexData)Vertices[edge.PreVertex]!=null);
			bool hasPostData = ((VertexData)Vertices[edge.PostVertex]!=null);
			if ( !hasPreData && !hasPostData ) {
				msg += " but the analyst has no data on either the pre or the post vertices.";
				if ( !Edges.Contains(edge) ) {
					msg += " In fact, the edge is not even in the collection of edges analyzed by this" +
						" analyst, which is the collection of edges between " + Start.Name + " and " + Finish.Name +
						" including all child edges. This may occur if the edge was removed from the graph before, or" +
						" added to the graph after, the Analyst last ran (which is almost always the time of the last simulation run).";
				} else {
					msg += " However, this edge appears to be a child of the graph that was to be analyzed, and" +
						" therefore, this may be indicative of a bug in the CPM Analyst.";
				}
			} else if ( hasPreData && hasPostData ) {
				msg += " and although the analyst has data on both vertices, it cannot provide the requested data. " +
					"This is a serious error - please report it.";
			} else {
				if ( !hasPreData ) msg += " but the analyst has no data on the pre vertex.";
				if ( !hasPostData ) msg += " but the analyst has no data on the post vertex.";
				msg += "This is a serious error - please report it.";
			}

			LogEdgeNotFoundError(msg,edge);

			throw new ArgumentException(msg);

		}

		private void LogEdgeNotFoundError(string msg, Edge edge){
			if ( s_logEdgeNotFoundError ) {
				string logFilePath = @"./SOM_Analytical.txt";
				Tasks.Task task = edge as Tasks.Task;
				StreamWriter sw = new StreamWriter(logFilePath);
				sw.WriteLine("\r\n::::::::::::::::Message::::::::::::::::\r\n" + msg + "\r\n");
				sw.WriteLine("\r\n::::::::::::::::Call Stack::::::::::::::::\r\n");
				StackTrace st = new StackTrace(true);
				sw.WriteLine(st.ToString());
				sw.WriteLine("\r\n::::::::::::::::Requested Edge::::::::::::::::\r\n");
				sw.WriteLine("Name : " + edge.Name + "\r\nGuid : " + task.Guid + "\r\nHashCode : " + task.GetHashCode());

				sw.WriteLine("\r\n::::::::::::::::Known Edges::::::::::::::::\r\n");
				foreach ( DictionaryEntry de in Edges ) {
					Edge knownEdge = (Edge)de.Key;
					EdgeData knownEdgeData = (EdgeData)de.Value;
					Tasks.Task knownTask = knownEdge as Tasks.Task;
					if ( knownTask != null ) {
						sw.WriteLine("Name : " + knownTask.Name + "\r\nGuid : " + knownTask.Guid + "\r\nHashCode : " + knownTask.GetHashCode());
					} else {
						sw.WriteLine("Name : " + knownEdge.Name + "\r\nGuid : <Edge, not task, therefore no Guid>\r\nHashCode : " + knownEdge.GetHashCode());
					}
				}
				sw.Flush();
				sw.Close();
				_Debug.WriteLine("Dumped log file to " + logFilePath);
			}
		}
		
		#endregion
		
		/// <summary>
		/// Returns true if, in the graph or subgraph that was analyzed, this edge was on the
		/// critical path.
		/// </summary>
		/// <param name="edge">The edge to be analyzed.</param>
		/// <returns>true, if this edge is on the critical path.</returns>
		public bool IsCriticalPath(Edge edge){
			if ( !m_analyzed ) Analyze(); 
			VertexData vdPre = (VertexData)Vertices[edge.PreVertex];
			VertexData vdPost = (VertexData)Vertices[edge.PostVertex];
			if ( vdPre == null || vdPost == null ) {
				// Always throws an exception. '==0' is to fake out the compiler to fit the bool return of this method.
				return _OnEdgeNotFound(edge)==0;
			}
			long duration = 0;
			if ( edge is ISupportsCpmAnalysis ) duration = ((ISupportsCpmAnalysis)edge).GetNominalDuration().Ticks;
			if ( vdPre.Earliest != vdPre.Latest ) return false;
			//if ( vdPost.Earliest != vdPost.Latest ) return false;
			//if ( vdPost.Earliest > ( vdPre.Earliest + duration  )) return false;
			return true;
		}
		
		protected VertexData GetVertexData(Vertex vertex) {
			VertexData vertexData = (VertexData)Vertices[vertex];
			if ( vertexData == null ) {
				vertexData = new VertexData(vertex);
				Vertices.Add(vertex,vertexData);
			}
			return vertexData;
		}

		protected SynchronizerData GetSynchronizerData(Vertex vertex){
			SynchronizerData sd = (SynchronizerData)Synchronizers[vertex.Synchronizer];
			if ( sd == null ) {
				sd = new SynchronizerData(vertex.Synchronizer);
				Synchronizers.Add(vertex.Synchronizer,sd);
			}
			return sd;
		}

		protected EdgeData GetEdgeData(Edge edge){
			EdgeData edgeData = (EdgeData)Edges[edge];
			if ( edgeData == null ) {
				edgeData = new EdgeData(edge);
				Edges.Add(edge,edgeData);
			}
			return edgeData;
		}

		#region Contemporaneous Vertices
		private ArrayList GetContemporaneousVertices(Vertex vertex){
			ArrayList vertices = new ArrayList();
			_GetContemporaneousVertices(vertex,ref vertices);
			return vertices;
		}
		private void _GetContemporaneousVertices(Vertex vertex, ref ArrayList vertices){
			if ( vertices.Contains(vertex) ) return;
			vertices.Add(vertex);
			foreach ( Edge pre in vertex.PredecessorEdges ) {
				if ( pre is Ligature ) _GetContemporaneousVertices(pre.PreVertex, ref vertices);
			}
			foreach ( Edge post in vertex.SuccessorEdges ) {
				if ( post is Ligature ) _GetContemporaneousVertices(post.PostVertex, ref vertices);
			}
		}
		#endregion

		protected class VertexData { 
			private Vertex m_vertex;
			private long m_earliest;
			private long m_latest;
			private int m_nFwdVisits;
			private int m_nRevVisits;
			public VertexData( Vertex vertex ){
				m_vertex = vertex;
				m_earliest = long.MinValue;
				m_latest = long.MaxValue;
				m_nFwdVisits = 0;
				m_nRevVisits = 0;
			}

			public long Earliest { 
				get { 
					return m_earliest; 
				} 
				set { 
					m_earliest = value; 
				} 
			}
			public long Latest { 
				get { 
					return m_latest; 
				} 
				set {
					m_latest = value; 
				}
			}
			public void ResetReverseVisitCounts(){
				m_nRevVisits = 0;
			}

			public void ResetForwardVisitCounts(){
				m_nFwdVisits = 0;
			}

			public override string ToString(){
				return "Earliest = " + string.Format("{0:f2}",(new TimeSpan(m_earliest)).TotalMinutes) 
					+ ", and Latest = " +  string.Format("{0:f2}",(new TimeSpan(m_latest)).TotalMinutes);
			}

			public string Name { get { return m_vertex.Name; } }

			public IList GetNextEdgesForward(){
				IList retval = s_emptylist;
				m_nFwdVisits++;
				if ( m_vertex.PredecessorEdges.Count == 0 || m_nFwdVisits == m_vertex.PredecessorEdges.Count ) retval = m_vertex.SuccessorEdges;
				return retval;
			}
			public IList GetNextEdgesReverse(){
				IList retval = s_emptylist;
				m_nRevVisits++;
				if ( m_vertex.SuccessorEdges.Count == 0 || m_nRevVisits == m_vertex.SuccessorEdges.Count ) retval =  m_vertex.PredecessorEdges;
				return retval;
			}

			internal string  RevSatStatus { get { return "" + m_nRevVisits + "/" + m_vertex.SuccessorEdges.Count; } }
		}

		protected class EdgeData {
			private Edge m_edge;
			private long m_nominalDuration;
			private long m_optimisticDuration;
			private long m_pessimisticDuration;
			private double m_mean;
			private double m_variance2;
			public EdgeData( Edge edge ){
				m_edge = edge;
				if ( edge is ISupportsPertAnalysis ) {
					m_optimisticDuration = ((ISupportsPertAnalysis)edge).GetOptimisticDuration().Ticks;
					m_pessimisticDuration = ((ISupportsPertAnalysis)edge).GetPessimisticDuration().Ticks;
					m_nominalDuration = ((ISupportsPertAnalysis)edge).GetNominalDuration().Ticks;
				} else if ( edge is ISupportsCpmAnalysis ) {
					m_nominalDuration = ((ISupportsCpmAnalysis)edge).GetNominalDuration().Ticks;
					m_optimisticDuration = m_nominalDuration;
					m_pessimisticDuration = m_nominalDuration;
				} else {
					m_nominalDuration = m_optimisticDuration = m_pessimisticDuration = 0; // TimeSpan.Zero.Ticks;
				}

				m_mean = (m_optimisticDuration + (4*m_nominalDuration) + m_pessimisticDuration)/6;
				double delta = Math.Abs(((double)m_pessimisticDuration - m_optimisticDuration));
				m_variance2 = Math.Pow(delta,2.0);
			}

			public long NominalDuration { get { return m_nominalDuration; } set { m_nominalDuration = value; } }
			public long OptimisticDuration { get { return m_optimisticDuration; } set { m_optimisticDuration = value; } }
			public long PessimisticDuration { get { return m_pessimisticDuration; } set { m_pessimisticDuration = value; } }
			public double MeanDuration { get { return m_mean; } set { m_mean = value; } }
			public double Variance2 { get { return m_variance2; } set { m_variance2 = value; } }
        
			public override string ToString(){
				return "[" + (string.Format("{0:f2}",new TimeSpan(OptimisticDuration).TotalMinutes))  
					+ "/" + (string.Format("{0:f2}",new TimeSpan(NominalDuration).TotalMinutes))
					+ "/" + (string.Format("{0:f2}",new TimeSpan(PessimisticDuration).TotalMinutes))  + "]";
			}

			public string Name { get { return m_edge.Name; } }
		}

		
		protected class SynchronizerData {
			private VertexSynchronizer m_vs;
			private ArrayList m_fwdVisits;
			private ArrayList m_revVisits;
			private ArrayList m_members;
			private long m_earliest;
			private long m_latest;

			public SynchronizerData(VertexSynchronizer vs) {
				m_vs        = vs;
				m_fwdVisits = new ArrayList();
				m_revVisits = new ArrayList();
				m_members   = new ArrayList();
				m_earliest  = long.MinValue;
				m_latest    = long.MaxValue;

				foreach ( Vertex vertex in m_vs.Members ) m_members.Add(vertex);
			}

			public void ResetReverseVisitCounts(){
				m_revVisits.Clear();
			}

			public void ResetForwardVisitCounts(){
				m_fwdVisits.Clear();
			}

			public void RegisterForwardVisit(Vertex vertex, long elapsedTime){
				if ( m_members.Contains(vertex) && !(m_fwdVisits.Contains(vertex)) ) m_fwdVisits.Add(vertex);
				if ( elapsedTime > m_earliest ) m_earliest = elapsedTime;
			}

			public void RegisterBackwardVisit(Vertex vertex, long elapsedTime){
				if ( m_members.Contains(vertex) && !(m_revVisits.Contains(vertex)) ) m_revVisits.Add(vertex);
				if ( elapsedTime < m_latest ) m_latest = elapsedTime;
			}

			public IList NextEdgesForward(Hashtable vertexDataHashtable, ref long elapsedTime){
				ArrayList retval = s_emptylist;
				if ( m_members.Count == m_fwdVisits.Count ) {
					retval = new ArrayList();
					foreach ( Vertex peer in m_members ) {
						((VertexData)vertexDataHashtable[peer]).Earliest = m_earliest;
						retval.AddRange(peer.SuccessorEdges);
					}
					elapsedTime = m_earliest;
				}
				return retval;
			}

			public IList NextEdgesBackward(Hashtable vertexDataHashtable, ref long elapsedTime){
				ArrayList retval = s_emptylist;
				if ( m_members.Count == m_revVisits.Count ) {
					retval = new ArrayList();
					foreach ( Vertex peer in m_members ) {
						((VertexData)vertexDataHashtable[peer]).Latest = m_latest;
						retval.AddRange(peer.PredecessorEdges);
					}
					elapsedTime = m_latest;
				}
				return retval;
			}

			public VertexSynchronizer Synchronizer { get { return m_vs; } } 
		}
	}

    public class TimeCycleException : Exception {
        public TimeCycleException(string msg) : base(msg) { }
    }

	/// <summary>
	/// MissingParameterException is thrown when a required parameter is missing. Typically used in a late bound, read-from-name/value pair collection scenario.
	/// </summary>
	[Serializable]
	public class AnalysisFailedException : Exception {
        // For best practice guidelines regarding the creation of new exception types, see
        //    https://msdn.microsoft.com/en-us/library/5b2yeyab(v=vs.110).aspx
        IList m_problemElements = null;

		#region protected ctors
		/// <summary>
		/// Initializes a new instance of this class with serialized data. 
		/// </summary>
		/// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
		/// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
		protected AnalysisFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
		#endregion
		#region public ctors
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class.
        /// </summary>
		public AnalysisFailedException() { }
		/// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class with a specific message.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public AnalysisFailedException(string message) : base(message) { }
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class with a specific message and a list of the DAG elements that caused the problem.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="problemElements">The problem elements.</param>
		public AnalysisFailedException(string message, IList problemElements ) : base(message) {
			m_problemElements = problemElements;
		}
		/// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class with a specific message and an inner exception.
		/// </summary>
		/// <param name="message">The exception message.</param>
		/// <param name="innerException">The exception inner exception.</param>
		public AnalysisFailedException(string message, Exception innerException) : base(message, innerException) { }
		#endregion

        /// <summary>
        /// Gets the list of problem elements.
        /// </summary>
        /// <value>The problem elements.</value>
		public IList ProblemElements { get { return m_problemElements; } }
	}

}
