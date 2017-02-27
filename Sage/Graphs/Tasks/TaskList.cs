/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.Diagnostics;

namespace Highpoint.Sage.Graphs.Tasks {


    public class TaskList : IXmlPersistable, IModelObject {

        private Task m_masterTask;
        private ArrayList m_list;
        private Hashtable m_hashtable;

        public TaskList(IModel model, string name,Guid guid):this(model,name,Guid.NewGuid(),new Task(model,name,Guid.NewGuid())){}

        public TaskList(IModel model, string name,Guid guid, Task task) {
			Debug.Assert(model.Equals(task.Model),"TaskList being created for a model, but with a root task that is assigned to a different model.");
            InitializeIdentity(model, name, null, guid);
            
            m_masterTask = task;
            m_list = new ArrayList();
            m_hashtable = new Hashtable();
            
            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model this component runs in.</param>
        /// <param name="name">The name of this component.</param>
        /// <param name="description">The description for this component.</param>
        /// <param name="guid">The GUID of this component.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        public Task MasterTask {
            get {
                return m_masterTask;
            }
        }

        
		public void AddTaskAfter(Task predecessor, Task subject){
            int predIndex = m_list.IndexOf(predecessor);
            if ( predIndex == -1 ) {
                if ( m_list.Contains(subject) && !m_list.Contains(predecessor)) {
                    throw new ApplicationException("In \"AddTaskAfter\" operation, TaskList contains subject, but not predecessor - argument order swap?");
                } else {
                    throw new ApplicationException("In \"AddTaskAfter\" operation, TaskList does not contain the predecessor.");
                }
            }

            if ( predIndex == m_list.Count - 1 ) {
				//Trace.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
                AppendTask(subject);
            } else { // actual insertion...
                Task pred = (Task)m_list[predIndex];
				Task succ = (Task)m_list[predIndex+1];
                m_list.Insert(predIndex+1,subject);
				//Trace.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
				m_hashtable.Add(subject.Guid,subject);
				pred.RemoveSuccessor(succ);
				pred.AddSuccessor(subject);
				subject.AddSuccessor(succ);
				//succ.AddPredecessor(subject);
				m_masterTask.AddChildEdge(subject);
			}
		}

        public void AddTaskBefore(Task successor, Task subject){
            int succIndex = m_list.IndexOf(successor);
            if ( succIndex == -1 ) {
                if ( m_list.Contains(subject) && !m_list.Contains(successor)) {
                    throw new ApplicationException("In \"AddTaskBefore\" operation, the ChildTaskList for " + m_masterTask.Name + " contains subject, but not successor - argument order swap?");
                } else {  
                    throw new ApplicationException("In \"AddTaskBefore\" operation, the ChildTaskList for " + m_masterTask.Name + " does not contain the successor, " + successor.Name + ", so the new task, " + subject.Name + " cannot be added after it.");
                }
            }
            Task succ = null;
            if ( succIndex > 0 ) {
                Task pred = (Task)m_list[succIndex-1];
                pred.RemoveSuccessor(successor);
                pred.AddSuccessor(subject);
            }
            succ = (Task)m_list[succIndex];
            succ.AddPredecessor(subject);
            m_list.Insert(succIndex,subject);
			//Trace.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
            m_hashtable.Add(subject.Guid,subject);

            m_masterTask.AddChildEdge(subject);    
        }

        public void AppendTask(Task subject){
            if ( m_list.Count > 0 ) {
                Task predecessor = (Task)m_list[m_list.Count-1];
                m_list.Add(subject);
				//Trace.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
                m_hashtable.Add(subject.Guid,subject);

                subject.AddPredecessor(predecessor);
            } else {
				//Trace.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
                m_list.Add(subject);
                m_hashtable.Add(subject.Guid,subject);

            }

            m_masterTask.AddChildEdge(subject);

        }

        public void RemoveTask(Task subject){

            Task pred = null;
            Task succ = null;
            int subjNdx = m_list.IndexOf(subject);

            if ( subjNdx < m_list.Count - 1 ) succ = (Task)m_list[subjNdx+1];
            if ( subjNdx > 0 ) pred = (Task)m_list[subjNdx-1];

//			Trace.WriteLine("\r\n\r\n*************************************************************\r\nBefore RemoveTask\r\n");
//			Trace.WriteLine(DiagnosticAids.GraphToString(m_masterTask));
//			Trace.WriteLine("\r\n\r\n*************************************************************\r\nBefore RemoveChildEdge\r\n");
			m_masterTask.RemoveChildEdge(subject);
//			Trace.WriteLine("\r\n\r\n*************************************************************\r\nBefore The rest of the stuff...\r\n");
//			Trace.WriteLine(DiagnosticAids.GraphToString(m_masterTask));
            m_list.Remove(subject);
            m_hashtable.Remove(subject.Guid);

            if ( pred != null ) pred.RemoveSuccessor(subject);
            if ( succ != null ) succ.RemovePredecessor(subject);

            // Must heal the list, now.
            if ( pred != null && succ != null ) pred.AddSuccessor(succ);
            if ( pred == null && succ != null ) MasterTask.AddCostart(succ);
			// Was, until 1/25/2004 : if ( pred != null && succ == null ) MasterTask.AddCofinish(pred);
			if ( pred != null && succ == null ) pred.AddCofinish(MasterTask);
			//			Trace.WriteLine("\r\n\r\n*************************************************************\r\nAfter everything...\r\n");
//			Trace.WriteLine(DiagnosticAids.GraphToString(m_masterTask));
            subject.SelfValidState = false;
//			foreach ( Highpoint.Sage.Graphs.Edge childEdge in subject.ChildEdges ) {
//				if ( childEdge is Task ) ((Task)childEdge).SelfValidState = false;
//			}
        }

        public Task this[int i]{
            get { return (Task)m_list[i]; }
        }

        public Task this[Guid guid]{
            get { return (Task)m_hashtable[guid]; }
        }

        public IList List { get { return m_list; } }
        public IDictionary Hashtable { get { return m_hashtable; } }

        public string ToStringDeep(){
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int n = 0;
            foreach ( Task task in m_list ) {
                sb.Append("Entry # ");
                sb.Append(n++);
                sb.Append(" : \r\n");
                sb.Append(DiagnosticAids.GraphToString(task));
            }
            return sb.ToString();


        }

		#region IXmlPersistable Members
		/// <summary>
		/// Default constructor for serialization only.
		/// </summary>
		public TaskList(){
			m_hashtable = new Hashtable();
			m_list = new ArrayList();
		}
		public virtual void SerializeTo(XmlSerializationContext xmlsc) {
			xmlsc.StoreObject("MasterTask",m_masterTask);
			xmlsc.StoreObject("ChildTasks",m_list);
			// Don't need to store the hashtable.
		}

		public virtual void DeserializeFrom(XmlSerializationContext xmlsc) {
			m_masterTask = (Task)xmlsc.LoadObject("MasterTask");
			
			ArrayList tmpList = (ArrayList)xmlsc.LoadObject("ChildTasks");

			foreach ( Task task in tmpList ) AppendTask(task);

		}

		#endregion

		#region IModelObject Members

		private IModel m_model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model
        { 
			get { return m_model; }
		}

		private string m_name = null;
		public string Name {
			get {
				return m_name;
			}
		}

		private string m_description = null;
		/// <summary>
		/// A description of this TaskList.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}


		private Guid m_guid = Guid.Empty;
		public Guid Guid {
			get {
				return m_guid;
			}
		}

		#endregion
	}
}
