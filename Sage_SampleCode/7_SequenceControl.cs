/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.SimCore;
using System.Collections;

namespace Demo.SequenceControl
{
    namespace Basic
    {
        public static class TaskGraphDemo
        {
            [Microsoft.VisualStudio.TestTools.UnitTesting.Description(@"This demo shows the modeling of a hierarchical process, in this case, that of
making brownies. There are tasks that can happen in parallel, and tasks that 
must happen serially. The top level task is ""Make Brownies"" and it has four
subtasks, ""Prepare Oven"", ""Prepare Pan"", ""Assemble Brownies"" and 
""Bake Brownies"" (technically, this must be serial to ""Assemble Brownies"",
but we manage this relationship by explicitly establishing a successor/follower
role between ""Assemble Brownies"" and ""Bake Brownies,"" and we can see that 
reflected in the output.

Under ""Prepare Pan"", there are two serial tasks, ""Acquire Pan"", and 
""Grease Pan"". Likewise, ""Assemble Brownies"" has serial subtasks, as does 
""Bake Brownies"". 

The task graph runs on Synchronous events, and each task signals its completion
when finished.

There are many more capabilities of the Task Graph construct, including 
hierarchical composite validity management (if there's no grease, you can't 
prepare the pan, and therefore you can't make the brownies), post-mortem
analysis (how long did we spend greasing the pan?), and simultaneous 
multiple instance execution (let's make three batches of brownies.)
")]
            public static void Run()
            {
                Highpoint.Sage.SimCore.Model model = new Highpoint.Sage.SimCore.Model("TaskGraph 1", Guid.NewGuid());
                DateTime startTime = new DateTime(2001, 3, 5, 7, 9, 11);
                Hashtable graphContext1 = new Hashtable();
                TestTask makeBrownies = new TestTask(model, "Make Brownies", 0);

                TestTask prepareOven = new TestTask(model, "Prepare Oven", 7);

                TestTask preparePan = new TestTask(model, "Prepare Pan", 0);
                TestTask acquirePan = new TestTask(model, "Acquire Pan", 2);
                TestTask greasePan = new TestTask(model, "Grease Pan", 2);

                TestTask assembleBrownies = new TestTask(model, "Assemble Brownies", 0);
                TestTask acquireIngredients = new TestTask(model, "Acquire Ingredients", 45);
                TestTask mixIngredients = new TestTask(model, "Mix Ingredients", 45);
                TestTask pourBatter = new TestTask(model, "Pour Batter", 45);

                TestTask bakeBrownies = new TestTask(model, "Bake Brownies", 0);
                TestTask putPanInOven = new TestTask(model, "Put Pan In Oven", .5);
                TestTask waitForCookTime = new TestTask(model, "Wait for Cook Time", 45);
                TestTask removePanFromOven = new TestTask(model, "Remove Pan From Oven", 2);

                makeBrownies.AddChildEdges(new [] { prepareOven , preparePan , assembleBrownies, bakeBrownies }); // We'll allow them to proceed in parallel.
                preparePan.AddChainOfChildren(new[] { acquirePan , greasePan }); // These happen in series.
                assembleBrownies.AddChainOfChildren(new[] { acquireIngredients, mixIngredients, pourBatter }); // These happen in series.
                bakeBrownies.AddChainOfChildren(new[] { putPanInOven, waitForCookTime, removePanFromOven }); // These happen in series.

                bakeBrownies.AddPredecessor(preparePan);
                bakeBrownies.AddPredecessor(assembleBrownies);

                model.Starting += delegate(IModel theModel)
                {          
                    theModel.Executive.RequestEvent((exec, data) => makeBrownies.Start(graphContext1), startTime);
                };

                model.Start();

                Console.WriteLine("\r\nPost-run analysis:\r\n");

                foreach (TestTask testTask in new[] {prepareOven, mixIngredients, putPanInOven, waitForCookTime})
                {
                    Console.WriteLine("It was recorded that {0} started at {1} and took {2}.", testTask.Name,
                        testTask.GetStartTime(graphContext1), testTask.GetRecordedDuration(graphContext1));
                }
            }

            private class TestTask : Task
            {
                private readonly double m_minutesToDelay;

                public TestTask(IModel model, string name, double minutesToDelay)
                    : base(model, name, Guid.NewGuid())
                {
                    m_minutesToDelay = minutesToDelay;
                    this.TaskStartingEvent += (context, task) => Console.WriteLine("{2}{0} : Starting {1}.", model.Executive.Now, Name, Tabs);
                    this.TaskFinishingEvent += (context, task) => Console.WriteLine("{2}{0} : Finishing {1}.", model.Executive.Now, Name, Tabs);
                }

                private string Tabs => "  " + ((TestTask) Parent)?.Tabs;

                protected override void DoTask(IDictionary graphContext)
                {
                    DateTime completeWhen = Model.Executive.Now + TimeSpan.FromMinutes(m_minutesToDelay);
                    Model.Executive.RequestEvent((exec, data) => SignalTaskCompletion(graphContext), completeWhen);
                }
            }
        }
    }
}
