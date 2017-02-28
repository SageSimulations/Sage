/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Graphs;
using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.Materials.Chemistry;

namespace Highpoint.Sage.Diagnostics {
	/// <summary>
	/// DiagnosticAids is a class that holds many static methods for general-purpose diagnostics
	/// that can be performed throughout the remainder of the framework.
	/// </summary>
	public static class DiagnosticAids	{

		private static NameValueCollection _settings;
		private static bool _settingInitAttempted;
		private static bool _logMissingDiagKeys;
		private static System.IO.StreamWriter _missingKeyLog;

		/// <summary>
		/// Determines, for a specific key, whether diagnostic tracing is turned on. The on/off
		/// setting is determined by the presence of an entry in the diagnostics section of the
		/// App.config where the key appears and the value is 'true'. See below:<p></p>
		/// <!-- <diagnostics><add key="Mixture" value="false" /></diagnostics> -->
		/// </summary>
		/// <param name="whichOne">The key being queried.</param>
		/// <returns>True if diagnostic tracing has been requested.</returns>
		public static bool Diagnostics(string whichOne){
			if ( !_settingInitAttempted ) {
				try {
                    _settings = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("diagnostics") ??
                                new NameValueCollection();
				    _settingInitAttempted = true;
					string lmdkString = _settings["LogMissingDiagKeys"];
					_logMissingDiagKeys = lmdkString != null && bool.Parse(lmdkString);
					if ( _logMissingDiagKeys ) {
					    _missingKeyLog = new System.IO.StreamWriter(Utility.DirectoryOperations.GetAppDataDir() + "MissingDiagKeys.log")
					    {
					        AutoFlush = true
					    };
					}
				} catch ( System.Configuration.ConfigurationException sce ) {
					_Debug.WriteLine(sce.ToString());
				}
			}
			if ( _settings == null ) return false;
			string result = _settings[whichOne];

		    if (result != null) return Boolean.Parse(_settings[whichOne]);
		    if (!_logMissingDiagKeys) return false;

		    _missingKeyLog.WriteLine("<add key=\"" + whichOne + "\" value=\"false\" />");
		    Console.WriteLine(whichOne);
		    return false;
		}

		/// <summary>
		/// In a debug build, post-mortems of a task graph can be dumped, describing which
		/// vertices and edges fired. This method performs that activity. The post mortems
		/// are retrieved from the model through the GetPostMortems(){ ... } API. 
		/// </summary>
		/// <param name="postMortems">PostMortem data that can be acquired from the model via the GetPostMortems() API.</param>
		public static void DumpPostMortems(Hashtable postMortems){
#if DEBUG
			foreach ( DictionaryEntry de in postMortems ) {
				PmData pmData = (PmData)de.Value;
				_Debug.WriteLine("PostMortem of Context associated with " + de.Key);
				_Debug.WriteLine("Vertices that fired:");
				foreach ( Vertex v in pmData.VerticesFired ) {
					_Debug.WriteLine("\t" + v.Name);
				}
				_Debug.WriteLine("Edges that fired:");
				foreach ( Edge e in pmData.EdgesFired ) {
					_Debug.WriteLine("\t" + e.Name);
				}
			}
#endif
		}

		/// <summary>
		/// Takes an edge, and returns a string that represents structural details about its
		/// child edges.
		/// </summary>
		/// <param name="parent">The parent edge to the graph.</param>
		/// <returns>A string that represents structural details about the parent's child edges</returns>
		public static string GraphToSimpleString(Edge parent){
			StringBuilder sb = new StringBuilder();

			_ToSimpleString(parent, ref sb, 0);

			return sb.ToString();
           
		}

		/// <summary>
		/// Takes an edge, and returns a string that represents structural details about its
		/// child edges.
		/// </summary>
		/// <param name="parent">The parent edge to the graph.</param>
		/// <returns>A string that represents structural details about the parent's child edges</returns>
		public static string GraphToString(Edge parent){
			StringBuilder sb = new StringBuilder();

			_ToString(parent, ref sb, 0);

			return sb.ToString();
           
		}

		private static void _ToString(Edge parent, ref StringBuilder sb, int tabDepth){
			AddTabs(ref sb,tabDepth);
			sb.Append("Edge : " + parent.Name + " ("+parent.GetType()+")\r\n");
			AddTabs(ref sb,tabDepth+1);
			sb.Append("* * * * * PreVertex\r\n");
			_ToString(parent.PreVertex, ref sb, tabDepth+2);
			if ( parent.ChildEdges.Count > 0 ) {
				AddTabs(ref sb,tabDepth+1);
				sb.Append("* * * * * Child Edges (" + parent.ChildEdges.Count + ")\r\n");
				foreach ( Edge child in parent.ChildEdges ) {
					_ToString(child, ref sb, tabDepth+1);
				}
			}
			AddTabs(ref sb,tabDepth+1);
		    Task task1 = parent as Task;
		    if ( task1 != null ) {
				Task task = task1;
				sb.Append("["+task.GetOptimisticDuration()+"/"+task.GetNominalDuration()+"/"+task.GetPessimisticDuration()+"]\r\n");
			}

			AddTabs(ref sb,tabDepth-1);
			sb.Append("* * * * * PostVertex\r\n");
			_ToString(parent.PostVertex, ref sb, tabDepth+2);


		}
		private static void _ToSimpleString(Edge parent, ref StringBuilder sb, int tabDepth){
			AddTabs(ref sb,tabDepth);
			sb.Append("Edge : " + parent.Name + " ("+parent.GetType()+")\r\n");
			//AddTabs(ref sb,tabDepth+1);
			if ( parent.ChildEdges.Count > 0 ) {
				//AddTabs(ref sb,tabDepth+1);
				foreach ( Edge child in parent.ChildEdges ) {
					_ToSimpleString(child, ref sb, tabDepth+1);
				}
			}
		}

		private static void AddTabs(ref StringBuilder sb, int tabDepth){
			for ( int i = 0 ; i < tabDepth ; i++ ) sb.Append("\t");
		}

		private static void _ToString(Vertex vertex, ref StringBuilder sb, int tabDepth){
			AddTabs(ref sb,tabDepth);
			sb.Append("Name: " + vertex.Name + "\r\n");
			AddTabs(ref sb,tabDepth);
			sb.Append("Precursors: " + vertex.PredecessorEdges.Count + "\r\n");
			foreach ( Edge edge in vertex.PredecessorEdges) {
				AddTabs(ref sb,tabDepth+1);
				sb.Append(edge.Name + "\r\n");
			}
			AddTabs(ref sb,tabDepth);
			sb.Append("Successors: " + vertex.SuccessorEdges.Count + "\r\n");
			foreach ( Edge edge in vertex.SuccessorEdges) {
				AddTabs(ref sb,tabDepth+1);
				sb.Append(edge.Name + "\r\n");
			}
		}

		/// <summary>
		/// Returns a string containing the contents of a dictionary.
		/// </summary>
		/// <param name="name">The name of this dictionary. Informational only.</param>
		/// <param name="dict">The dictionary to dump.</param>
		/// <returns>A string containing the contents of the specified dictionary.</returns>
		public static string DumpDictionary(string name, IDictionary dict){
			return Utility.DictionaryOperations.DumpDictionary(name,dict);
		}
		
		/// <summary>
		/// Returns a string that indicates, in human-readable form, the validity state of the specified task.
		/// </summary>
		/// <param name="task">The task whose state is of interest.</param>
		/// <param name="deep">If set to true, checks this task and all of its descendants. 
		/// Only if all are valid, is this task considered to be valid.</param>
		/// <returns>A string that indicates, in human-readable form, the validity state of the specified task.</returns>
		public static string ReportOnTaskValidity(Task task, bool deep = false){
			String retval = "";
			retval += (task.Name + (task.ValidityState?" is valid.":" is not valid."));
			if ( !task.ValidityState ) { 
				retval += "(";
				if (task.SelfValidState==false) retval += " SelfState_Bad ";
				if (task.AllUpstreamValid==false) retval += " UpstreamValidityState_Bad ";
				if (task.AllChildrenValid==false) retval += " ChildValidityState_Bad ";
				retval += ")";
			}
			retval += "\r\n";

			if ( deep ) {
			    // ReSharper disable once LoopCanBeConvertedToQuery (for clarity because of the recursion.)
				foreach ( Task child in task.GetChildTasks(true) ) {
					retval += ReportOnTaskValidity(child,true);
				}
			}
			return retval;
		}

		/// <summary>
		/// Provides a human-readable string with the contents of a material.
		/// </summary>
		/// <param name="material">The material whose contents are of interest.</param>
		public static void DumpMaterial(IMaterial material)
		{
		    Mixture mix = material as Mixture;
		    if (mix != null ) {
				Dump(mix);
			} else if (material is Substance ) {
				Dump((Substance)material);
			}
		}

	    private static string _dblFmt="F1";
		private static void Dump(Mixture mix){
			double totalEnergy = 0.0;
			foreach ( Substance substance in mix.Constituents ) {
				double energy = (substance.Temperature+Constants.CELSIUS_TO_KELVIN)*substance.Mass*substance.MaterialType.SpecificHeat;
				totalEnergy+=energy;
				_Debug.WriteLine("\t{0} - {1} kg, {2} C, and {3} liters. ({4} Joules of thermal energy)",
					substance.MaterialType.Name,
					substance.Mass.ToString(_dblFmt),
					substance.Temperature.ToString(_dblFmt),
					substance.Volume.ToString(_dblFmt), 
					energy.ToString(_dblFmt));
			}
			_Debug.WriteLine("{0} - {1} kg, {2} C, and {3} liters. ({4} Joules of thermal energy)",
				mix.Name,
				mix.Mass.ToString(_dblFmt),
				mix.Temperature.ToString(_dblFmt),
				mix.Volume.ToString(_dblFmt),
				totalEnergy.ToString(_dblFmt));
			_Debug.WriteLine("");
		}

		private static void Dump(Substance substance){
			double energy = (substance.Temperature+Constants.CELSIUS_TO_KELVIN)*substance.Mass*substance.MaterialType.SpecificHeat;
			_Debug.WriteLine("\t{0} - {1} kg, {2} C, and {3} liters. ({4} Joules of thermal energy)",
				substance.MaterialType.Name,
				substance.Mass.ToString(_dblFmt),
				substance.Temperature.ToString(_dblFmt),
				substance.Volume.ToString(_dblFmt), 
				energy.ToString(_dblFmt));
			_Debug.WriteLine("");
		}
	}


    /// <summary>
    /// Creates an event logger to store events from a particular executive into a file.
    /// </summary>
	public class ExecEventLogger : IDisposable {
		
        private System.IO.TextWriter m_logFile;

        /// <summary>
        /// Creates a new instance of the <see cref="T:EventLogger"/> class.
        /// </summary>
        /// <param name="exec">The executive to be logged.</param>
        /// <param name="filename">The filename into which to write the logs.</param>
        public ExecEventLogger(IExecutive exec, string filename){
			m_logFile = new System.IO.StreamWriter(filename,false);
			exec.EventAboutToFire+=Executive_EventAboutToFire;
			m_logFile.WriteLine("Time,Pri,TargetObject,MethodName,UserData,EventType");
		}

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="T:Highpoint.Sage.Diagnostics.EventLogger"/> is reclaimed by garbage collection.
        /// </summary>
        ~ExecEventLogger() {
			Dispose();
		}
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
		public void Dispose(){
			if ( m_logFile == null ) return;
			try {
				m_logFile.Flush();
				m_logFile.Close();
				m_logFile = null;
			}
			catch (Exception)
			{
			    // ignored
			}
		}

		private void Executive_EventAboutToFire(long key,ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType) {
			string method = eer.Method.ToString();
			method = method.Replace(",",":");
			m_logFile.WriteLine(when.ToString(CultureInfo.InvariantCulture) + ", " + priority + ", " + eer.Target + ", " + method + ", " +
				(userData?.ToString() ?? "<null>") + ", " + eventType);
		}
	}
}