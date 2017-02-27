/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs.Tasks {
    public class TaskException : Exception {
        private Task m_task = null;
        public TaskException(Task task, string message):base(message){
            m_task = task;
        }
        public Task Task { get { return m_task; } }
    }

    public class TaskError : IModelError {
        protected Task m_task;
        protected string m_Name;
        protected string m_narrative;
        protected object m_subject = null;
        protected double m_priority = 0.0;
        protected bool m_autoClear = false;

        public Task Task { get { return m_task; } }
        protected TaskError(){}
        public TaskError(Task theTask){ 
            m_task = theTask;
            m_Name = "Task Error";
            m_narrative = "Task error in task " + m_task.Name;
            m_subject   = null;
        }

        #region Implementation of IModelError
        public string Name { get { return m_Name==null?"":m_Name; } }
        public string Narrative { get { return m_narrative==null?"":m_narrative; } }
        public object Target { get { return m_task; } }
        public object Subject { get { return m_subject; } }
        public double Priority { get { return m_priority; } set { m_priority = value; } }
		/// <summary>
		/// An exception that may have been caught in the detection of this error.
		/// </summary>
		public Exception InnerException { get { return null; } }

        public bool AutoClear { get { return m_autoClear; } }
        #endregion

        public override string ToString(){
            return Name + " occurred at " + m_task.Name + " due to " + Subject + " : " + Narrative;
        }
    }

    public class TaskHasInvalidSelfStateError : TaskError {
        public TaskHasInvalidSelfStateError(Task theTask, object subject){
            m_task      = theTask;
            m_Name = "InvalidSelfStateError";
            m_narrative = "The task " + m_task.Name + " is reported to be invalid\r\n\t";
			//m_narrative += Diagnostics.DiagnosticAids.ReportOnTaskValidity(theTask);


            m_subject   = subject;
        }
    }
}
