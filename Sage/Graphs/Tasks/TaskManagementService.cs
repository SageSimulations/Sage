/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Diagnostics;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs.Tasks
{

    public interface ITaskManagementService : IModelService
    {
        /// <summary>
        /// Fired when a TaskProcessor is added to this model.
        /// </summary>
        event TaskProcessorListener TaskProcessorAddedEvent;

        /// <summary>
        /// Fired when a TaskProcessor is removed from this model.
        /// </summary>
        event TaskProcessorListener TaskProcessorRemovedEvent;

        /// <summary>
        /// Adds a task processor to this model. A Task Processor is an entity that knows when to
        /// start executing a given task graph. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being added to this model.</param>
        void AddTaskProcessor(TaskProcessor taskProcessor);

        /// <summary>
        /// Removes a task processor from this model. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being removed from this model.</param>
        void RemoveTaskProcessor(TaskProcessor taskProcessor);

        /// <summary>
        /// The collection of task processors being managed by this model.
        /// </summary>
        ArrayList TaskProcessors { get; }

        /// <summary>
        /// Locates a task processor by its Guid.
        /// </summary>
        /// <param name="guid">The Guid of the task processor to be located.</param>
        /// <returns>The task processor, if found, otherwise null.</returns>
        TaskProcessor GetTaskProcessor(Guid guid);

        /// <summary>
        /// Returns the tasks known to this model.
        /// </summary>
        /// <param name="masterTasksOnly">If this is true, then only the root (master) tasks of all of the
        /// known task graphs are returned. Otherwise, all tasks under those root tasks are included as well.</param>
        /// <returns>A collection of the requested tasks.</returns>
        ICollection GetTasks(bool masterTasksOnly);
    }

    public class TaskManagementService : ITaskManagementService
    {
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("TaskManagementService");
        private static readonly bool s_managePostMortemData = Diagnostics.DiagnosticAids.Diagnostics("Graph.KeepPostMortems");

        private readonly Hashtable m_taskProcessors;
        private IModel m_model;
        public TaskManagementService()
        {
            m_taskProcessors = new Hashtable();
        }

        
        #region >>> TaskProcessor Management <<<

        public void OnModelStarting(IModel model){
            // A part of the protocol for this generic model is that 
            // all TaskProcessors run when the model runs.
            foreach ( TaskProcessor tp in TaskProcessors ) tp.Activate();
        }

        /// <summary>
        /// Fired when a TaskProcessor is added to this model.
        /// </summary>
        public event TaskProcessorListener TaskProcessorAddedEvent;
        /// <summary>
        /// Fired when a TaskProcessor is removed from this model.
        /// </summary>
        public event TaskProcessorListener TaskProcessorRemovedEvent;

        /// <summary>
        /// Adds a task processor to this model. A Task Processor is an entity that knows when to
        /// start executing a given task graph. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being added to this model.</param>
        public void AddTaskProcessor(TaskProcessor taskProcessor){
            // TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
            if ( m_taskProcessors.Contains(taskProcessor.Guid) ) {
                Trace.WriteLine("Model already contains task processor being added at:");
                Trace.WriteLine((new StackTrace()).ToString());
                Trace.WriteLine("...the request to add it will be ignored.");
                return;
            }
            m_taskProcessors.Add(taskProcessor.Guid,taskProcessor);
            TaskProcessorAddedEvent?.Invoke(this, taskProcessor);
        }

        /// <summary>
        /// Removes a task processor from this model. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being removed from this model.</param>
        public void RemoveTaskProcessor(TaskProcessor taskProcessor) {
            m_taskProcessors.Remove(taskProcessor.Guid);
            TaskProcessorRemovedEvent?.Invoke(this, taskProcessor);
        }

        /// <summary>
        /// The collection of task processors being managed by this model.
        /// </summary>
        public ArrayList TaskProcessors {
            get {
                ArrayList tps = new ArrayList(m_taskProcessors.Values);
                return ArrayList.ReadOnly(tps);
            }
        }

        /// <summary>
        /// Locates a task processor by its Guid.
        /// </summary>
        /// <param name="guid">The Guid of the task processor to be located.</param>
        /// <returns>The task processor, if found, otherwise null.</returns>
        public TaskProcessor GetTaskProcessor(Guid guid){
            return (TaskProcessor)m_taskProcessors[guid];
        }

        /// <summary>
        /// Returns the tasks known to this model.
        /// </summary>
        /// <param name="masterTasksOnly">If this is true, then only the root (master) tasks of all of the
        /// known task graphs are returned. Otherwise, all tasks under those root tasks are included as well.</param>
        /// <returns>A collection of the requested tasks.</returns>
        public virtual ICollection GetTasks(bool masterTasksOnly){
            ArrayList kids = new ArrayList();
            foreach ( TaskProcessor tp in TaskProcessors ) {
                kids.Add(tp.MasterTask);
                if ( !masterTasksOnly ) kids.AddRange(tp.MasterTask.GetChildTasks(false));
            }
            return kids;
        }

        /// <summary>
        /// Clears any errors whose target (the place where the error occurred) is a task that has been 
        /// removed from the model.
        /// </summary>
        public void ClearOrphanedErrors()
        {

            ArrayList allTasks = new ArrayList();

            foreach (TaskProcessor tp in m_taskProcessors.Values)
            {
                if (allTasks.Contains(tp.MasterTask)) continue;
                allTasks.AddRange(tp.MasterTask.GetChildTasks(false));
            }

            if (s_diagnostics)
            {
                Trace.WriteLine("Clearing orphaned errors:\r\nKnown Tasks:");
                foreach (Task task in allTasks)
                {
                    Trace.WriteLine("\t" + task.Name);
                }
            }

            ArrayList keysToClear = new ArrayList();
            foreach (IModelError err in m_model.Errors)
            {
                if (err.Target is Task)
                {
                    if (s_diagnostics)
                        Trace.WriteLine("Checking error " + err.Narrative + ", targeted to " + ((Task)err.Target).Name);
                    if (!allTasks.Contains(err.Target))
                    {
                        if (s_diagnostics)
                            Trace.WriteLine("Clearing error " + err.Name);
                        keysToClear.Add(err.Target);
                    }
                }
            }

            foreach (object key in keysToClear) m_model.ClearAllErrorsFor(key);
        }

                /// <summary>
        /// Returns the post mortem data on all known task graph executions. This data indicates the vertices
        /// and edges that fired and those that did not fire. It is typically fed into the Diagnostics class'
        /// DumpPostMortemData(...) API.
        /// </summary>
        /// <returns>A Hashtable of postmortem data.</returns>
        public Hashtable GetPostMortems(){
            Hashtable postmortems = new Hashtable();
#if DEBUG
            if ( s_managePostMortemData ) {
                foreach ( TaskProcessor tp in TaskProcessors ) {
                    foreach ( IDictionary graphContext in tp.GraphContexts ) {
                        PmData pmData = (PmData)graphContext["PostMortemData"];
                        postmortems.Add(tp.Name,pmData);
                    }
                }
            }
#endif //DEBUG
            return postmortems;
        }

        #endregion


        public void InitializeService(IModel model)
        {
            m_model = model;
            m_model.Starting += OnModelStarting;
        }

        public bool IsInitialized {
            get { return m_model != null; }
            set { }
        }
        public bool InlineInitialization => true;
    }
}
