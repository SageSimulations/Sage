/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Graphs;
using Highpoint.Sage.Graphs.Analysis;
using Highpoint.Sage.Graphs.Tasks;

namespace SchedulerDemoMaterial  {

    [TestClass]
    public class TaskTester {

        private Random m_random = new Random();

        public TaskTester(){}

        [TestMethod] public void TestBaseFunctionality(){

            long oneDay = TimeSpan.FromDays(1.0).Ticks;
            long oneHour = TimeSpan.FromHours(1.0).Ticks;
    
            Model model = new Model();
            model.AddService<ITaskManagementService>(new TaskManagementService());

            Task[] tasks = new Task[]{new MyTask(model, 0),new DelayTask(model,1,oneDay),new MyTask(model, 2),new DelayTask(model,3,oneHour)};
            
            int[] from = new int[]{0,0,1,2};//,4,5,6,7,8};
            int[] to   = new int[]{1,3,2,3};//,4,6,5,9,1};

            ArrayList childTasks = new ArrayList();
            foreach ( Task task in tasks ) childTasks.Add(task);

            for ( int ndx = 0 ; ndx < to.Length ; ndx++ ) {

                Task taskA = (Task)((Edge)childTasks[from[ndx]]);
                Task taskB = (Task)((Edge)childTasks[to[ndx]]);

                Debug.WriteLine(String.Format("Considering a connection between {0} and {1}.",taskA.Name,taskB.Name));

                int forward =  PathLength.ShortestPathLength(taskA,taskB);
                int backward = PathLength.ShortestPathLength(taskB,taskA);

                Debug.WriteLine(String.Format("Forward path length is {0}, and reverse path length is {1}.",forward,backward));

                if ( (forward==int.MaxValue) && (backward==int.MaxValue) ) {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine(String.Format("{0} will follow {1}.",taskB.Name,taskA.Name));
                } else if ( (forward!=int.MaxValue) && (backward==int.MaxValue) ) {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine(String.Format("{0} will follow {1}.",taskB.Name,taskA.Name));
                }else if ( (forward==int.MaxValue) && (backward!=int.MaxValue) ) {
                    taskB.AddSuccessor(taskA);
                    Debug.WriteLine(String.Format("{1} will follow {0}.",taskB.Name,taskA.Name));
                }else {
                    throw new ApplicationException("Cycle exists between " + taskA.Name + " and " + taskB.Name + ".");
                }
            }

            Task topTask = new Task(model,"Parent",Guid.NewGuid());
            topTask.AddChildEdges(childTasks);
            TaskProcessor tp = new TaskProcessor(model,"Task Processor",topTask);
            tp.SetStartTime(DateTime.Now);
            model.GetService<ITaskManagementService>().AddTaskProcessor(tp);

            model.StateMachine.InboundTransitionHandler(model.GetStartEnum()).Commit+=new CommitTransitionEvent(OnModelStarting);
            
            model.Start();
            if ( model.StateMachine.State.Equals(StateMachine.GenericStates.Running) ) {
                Debug.WriteLine("Error attempting to transition to Started state.");
            }
        }

        void OnModelStarting(IModel model, object userData) {
            Debug.WriteLine("Model " + model.Name + " starting.");
        }
    

        class MyTask : Highpoint.Sage.Graphs.Tasks.Task {

            public MyTask(IModel model, int i):base(model, "Task #"+i,Guid.NewGuid()){}
        
            protected override void DoTask(IDictionary graphContext){
                Debug.WriteLine(Name + " is of type \"MyTask\" and therefore reports that it is executing.");
            }
        
        }

        class DelayTask : Highpoint.Sage.Graphs.Tasks.Task {
            long m_delay;

            public DelayTask(IModel model, int i, long delay):base(model,"Task #"+i,Guid.NewGuid()){
                m_delay = delay;
            }
        }
    }
}
