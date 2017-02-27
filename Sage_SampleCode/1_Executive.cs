/* This source code licensed under the GNU Affero General Public License */
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

using System;
using Highpoint.Sage.SimCore;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Demo.Executive
{

    namespace SynchronousEvents
    {

        /// <summary>
        /// Hello world. Single simulation event.
        /// </summary>
        internal class HelloWorld
        {
            [Description(@"This demo simply creates an executive, adds an event to it, to be fired at
 a specific simulation time, and runs the executive, firing the event at the requested time.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");

                exec.RequestEvent(SayHello, when);

                exec.Start();

            }

            private static void SayHello(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Hello, world!", exec.Now);
            }
        }

        /// <summary>
        /// Two callbacks, set up ahead of time, called under different times.
        /// </summary>
        internal class TwoCallbacksOutOfSequence
        {
            [Description(@"This demo creates an executive, adds two events to it to be fired at 
specified simulation times that are in the opposite order to their having been 
added to the executive, and then runs the executive. It demonstrates the time-
ordered nature of callback execution.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when1 = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");
                exec.RequestEvent(SayHello, when1);

                DateTime when2 = DateTime.Parse("Mon, 18 Jul 2016 05:15:08");
                exec.RequestEvent(SayWorld, when2);

                exec.Start();

            }

            private static void SayHello(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Hello,", exec.Now);
            }

            private static void SayWorld(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : World!", exec.Now);
            }
        }

        /// <summary>
        /// Two callbacks, called at same time, but different priorities. Starting to define callbacks in line.
        /// </summary>
        internal class CallbacksWithPriorities
        {
            [Description(@"This demo creates an executive, adds two events to it, to be fired at 
the same simulation time, but with differnet priorities that are in the
opposite order to their having been added to the executive, and then runs
the executive. It demonstrates the priority-ordered nature of callback 
execution.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");
                exec.RequestEvent(WriteIt, when, 0.0, "World");
                exec.RequestEvent(WriteIt, when, 1.0, "Hello");

                exec.Start();

            }

            private static void WriteIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : {1}", exec.Now, userData);
            }
        }

        /// <summary>
        /// Useful data in UserData object, callback requesting follow-on, and self-imposed delay.
        /// </summary>
        internal class UserData_FollowOn_SelfImposedDelay
        {
            [Description(@"This demo creates an executive and submits an event to it at a specified
time. User data for that event is a queue of two strings, ""Hello"" and
""World."" Upon execution of that event, the handler method looks for a word
in the user data object, and if the queue is not empty, prints the word, 
requests a future event, and provides the (now shorter) queue of words to it
as user data. The executive keeps calling the event until the queue is empty,
at which time the handler does not request another event be served.

This demo shows the use of the UserData parameter to the RequestEvent method,
and also shows an event handler requesting a further, future, event service.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                Queue<string> userData = new Queue<string>(new[] {"Hello", "World"});

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");
                exec.RequestEvent(WriteIt, when, 0.0, userData);

                exec.Start();
            }

            private static void WriteIt(IExecutive exec, object _userData)
            {
                Queue<string> userData = (Queue<string>) _userData;
                if (userData.Count > 0)
                {
                    Console.WriteLine("{0} : {1}", exec.Now, userData.Dequeue());
                    exec.RequestEvent(WriteIt, exec.Now + TimeSpan.FromMinutes(10), userData);
                }
            }
        }

        /// <summary>
        /// Executive catches an exception in a handler.
        /// </summary>
        internal class ExecCatchesRuntimeExceptionFromSynchronousEvent
        {
            [Description(@"This demo creates an executive and submits an event to it and runs the 
executive. In the service of the event, an InvalidCastException is fired.
The executive runs, and catches and stores the exception for post-execution
analysis.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                Queue<string> userData = new Queue<string>(new[] {"Hello", "World"});

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");
                exec.RequestEvent(WriteIt, when, 0.0, userData);

                try
                {
                    exec.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Message : {0}\r\n\tInner Exception's message: {1}", e.Message,
                        e.InnerException?.Message);
                }

            }

            private static void WriteIt(IExecutive exec, object _userData)
            {
                // We want to generate an exception in an event handler.
                Stack<string> userData = (Stack<string>) _userData; // <-- Miscast. It's a queue. 
                if (userData.Count > 0)
                {
                    Console.WriteLine("{0} : {1}", exec.Now, userData.Pop());
                    exec.RequestEvent(WriteIt, exec.Now + TimeSpan.FromMinutes(10), userData);
                }
            }
        }

        /// <summary>
        /// Rescinding an event. Also, different types of objects in userData
        /// </summary>
        internal class RescindingSynchEvent
        {
            [Description(@"This demo creates an executive and submits an event to it (""WriteIt()"").
However, it also submits another event, to be serviced five minutes prior
to the ""WriteIt()"" event (""RescindIt()"") and runs the executive. In the
service of the earlier event, the later event is rescinded, and therefore is
never serviced.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");

                long eventKey =
                    exec.RequestEvent(WriteIt, when + TimeSpan.FromMinutes(5.0), "Hello.");
                exec.RequestEvent(RescindIt, when, eventKey);

                exec.Start();

            }

            private static void WriteIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : {1}", exec.Now, userData);
            }

            private static void RescindIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Rescinding event {1}", exec.Now, userData);
                exec.UnRequestEvent((long) userData);
            }

        }

        /// <summary>
        /// Rescinding multiple events based on target object. Also, first "Agent Based" simulation.
        /// </summary>
        internal class MoreRescindingPlusAgentBased
        {
            [Description(@"This demo creates an executive and a number of domain agents, rastro,
(a dog) and fifteen dog agents and fifteen cat agents. Then, for each agent,
an event is created which will cause them to speak at a specified time. These
are ten minutes apart. This would make for 50 minutes of speaking agents, 
except that two more events are requested - one, 45 minutes before the end 
of the 50 minutes, whose effect is to rescind all Speak events for rastro,
and another, 35 minutes before the end of the 50 minutes, whose effect is
to rescind all events targeted to objects of type ""Cat"".

This demonstrates some more advanced capabilities of event rescinding.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");

                Domain.Sample1.Dog rastro = new Domain.Sample1.Dog("Rastro");

                for (int i = 0; i < 5; i++)
                {
                    // Schedule 15 speaking events.

                    Domain.Sample1.Cat aCat = new Domain.Sample1.Cat("Cat_" + i);
                    Domain.Sample1.Dog aDog = new Domain.Sample1.Dog("Dog_" + i);

                    exec.RequestEvent(rastro.Speak, when);
                    exec.RequestEvent(aCat.Speak, when);
                    exec.RequestEvent(aDog.Speak, when);

                    when += TimeSpan.FromMinutes(10.0);
                }

                exec.RequestEvent(RescindIndividual, when - TimeSpan.FromMinutes(45), rastro);
                exec.RequestEvent(RescindCats, when - TimeSpan.FromMinutes(35));

                exec.Start();

            }

            private static void WriteIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : {1}", exec.Now, userData);
            }

            private static void RescindIndividual(IExecutive exec, object userData)
            {
                Domain.Sample1.Dog targetDog = (Domain.Sample1.Dog) userData;
                Console.WriteLine("{0} : Rescinding events to {1}.Speak()", exec.Now, targetDog.Name);
                exec.UnRequestEvents(new ExecEventReceiver(targetDog.Speak));
            }

            private static void RescindCats(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Rescinding all cat events.", exec.Now);
                //exec.UnRequestEvents(new Revoker(typeof(Cat))); // See detail below for usage of IExecEventSelector.
                // or...
                exec.UnRequestEvents(new Domain.Sample1.Cat("prototype"));

            }

            //private class Revoker : IExecEventSelector
            //{
            //    private readonly Type m_targetType;

            //    public Revoker(Type targetType)
            //    {
            //        m_targetType = targetType;
            //    }

            //    #region IExecEventSelector Members

            //    public bool SelectThisEvent(ExecEventReceiver eer, DateTime when, double priority, object userData,
            //        ExecEventType eet)
            //    {
            //        return eer.Target != null && eer.Target.GetType() == m_targetType;
            //    }

            //    #endregion
            //}
        }

    }

    namespace DetachableEvents
    {

        /// <summary>
        /// Basic detachable events with SuspendFor(...) and SuspendUntil().
        /// </summary>
        internal class BasicWithSuspends
        {
            [Description(@"This demo creates an executive and submits a synchronous event (""WriteIt()"")
for service at a specified time. It also submits a detachable event 
(""DoSomething()"") that is to be served beforehand. Then the executive is 
started. ""DoSomething()"" is called, begins running and then suspends itself.
Before it resumes, ""WriteIt()"" is called, and runs. Then, the 
""DoSomething()"" event resumes and completes.

This demonstrates how two things may be in process at the same time.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");

                exec.RequestEvent(WriteIt, when + TimeSpan.FromMinutes(5), "Reporting.");

                double priority = 0.0;
                TimeSpan userData_duration = TimeSpan.FromMinutes(10.0);
                exec.RequestEvent(DoSomething, when, priority, userData_duration, ExecEventType.Detachable);

                exec.Start();
            }

            private static void WriteIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : {1}  (...something else going on.)", exec.Now, userData);
            }

            private static void DoSomething(IExecutive exec, object userData)
            {
                TimeSpan duration = (TimeSpan) userData;
                Console.WriteLine("{0} : Starting to do something. Will be done in {1} minutes.", exec.Now,
                    duration.TotalMinutes);
                // exec.CurrentEventController.SuspendFor(duration);
                // ...or 
                exec.CurrentEventController.SuspendUntil(exec.Now + duration);
                Console.WriteLine("{0} : Done doing something after {1} minutes.", exec.Now, duration.TotalMinutes);
            }
        }

        /// <summary>
        /// Detachable events with Suspend()/Resume(), One agent is synchronous, one asynch.
        /// </summary>
        internal class SuspendsWithMixedModeAgents
        {
            [Description(@"This demo creates an executive and defines two agent types, plumber and
electrician. A detachable event request is submitted for a specified time
for a specific plumber to come fix a sink. The executive is started, and 
at the specified time, the plumber announces that he is starting to fix 
the sink, and works on it for ten minutes. At that time, he discovers that
the disposal needs rewiring, so he calls a plumber, and suspends his work.

The electrician, when he completes his work, will tell the plumber to resume.
Note that FixTheSink is a complex and time-consuming activity, but it is all
described in one method. For the electrician, rewiring the disposal will take
45 minutes.

Besides demonstrating cooperating agents, this demonstrates one agent calling
a suspension to its activity, and relying on another agent to resume it.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");

                exec.RequestEvent(new Plumber("Paul").FixTheSink, when, 0.0, null, ExecEventType.Detachable);

                exec.Start();
            }

            private class Plumber : Domain.Sample1.Person
            {
                public Plumber(string name) : base(name)
                {
                }

                public void FixTheSink(IExecutive exec, object userData)
                {
                    IDetachableEventController idec = exec.CurrentEventController;
                    Console.WriteLine("{0} : {1} is starting to fix the sink.", exec.Now, Name);
                    idec.SuspendFor(TimeSpan.FromMinutes(10.0)); // When the suspend call returns, 10 minutes have passed.

                    Console.WriteLine("{0} : {1} discovered that the disposal needed rewiring.", exec.Now, Name);

                    DateTime when = exec.Now + TimeSpan.FromMinutes(5.0);
                    exec.RequestEvent(new Electrician("Edgar").RewireDisposal, when, idec);
                    Console.WriteLine("{0} : {1} called the electrician and is starting to eat lunch.", exec.Now, Name);
                    idec.Suspend(); // When the suspend call returns (from the electrician calling "Resume",)
                                    // the electrician is done, and presumably the sink is fixed.
                    Console.WriteLine("{0} : {1} was told by the electrician that the disposal is fixed.", exec.Now,
                        Name);
                    Console.WriteLine("{0} : {1} is resuming fixing the sink.", exec.Now, Name);
                    idec.SuspendFor(TimeSpan.FromMinutes(40.0)); // Finishing fixing the sink requires 40 minutes,
                                                                 // and on return from this call, that time has passed.
                    Console.WriteLine("{0} : {1} is done fixing the sink.", exec.Now, Name);
                }
            }

            private class Electrician : Domain.Sample1.Person
            {
                public Electrician(string name) : base(name)
                {
                }

                public void RewireDisposal(IExecutive exec, object userData)
                {
                    // userData is plumber's IDetachableEventController...
                    Console.WriteLine("{0} : {1} is starting to fix the disposal.", exec.Now, Name);
                    exec.RequestEvent(FinishRewiringDisposal, exec.Now + TimeSpan.FromMinutes(45.0), userData);
                }

                private void FinishRewiringDisposal(IExecutive exec, object userData)
                {
                    IDetachableEventController plumbersIDEC = (IDetachableEventController) userData;
                    Console.WriteLine("{0} : {1} is done fixing the disposal.", exec.Now, Name);
                    plumbersIDEC.Resume();
                    Console.WriteLine("{0} : {1} is leaving.", exec.Now, Name);
                }
            }
        }

        /// <summary>
        /// Detachable events with Joining.
        /// </summary>
        internal class UsesJoining
        {
            [Description(@"This demo takes a questionable approach to making dinner. It creates
an executive, and submits a call to ""CookDinner"" at a specified time,
and starts the executive. In servicing the ""CookDinner"" event, three
events are requested, ""MakeTurkey"", ""MakeGravy"", and ""MakeStuffing""
 - all three to start immediately, and take different amounts of time.
 The call to exec.Join(...) suspends execution on this thread until all
 three activities have completed, and then resumes, announcing dinner.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");

                exec.RequestEvent(CookDinner, when, 0.0, null, ExecEventType.Detachable);

                exec.Start();
            }

            public static void CookDinner(IExecutive exec, object userData)
            {
                Console.WriteLine(" {0} : Starting to cook dinner...", exec.Now);

                long code1 = exec.RequestEvent(MakeTurkey, exec.Now, 0.0, null, ExecEventType.Detachable);
                long code2 = exec.RequestEvent(MakeGravy, exec.Now, 0.0, null, ExecEventType.Detachable);
                long code3 = exec.RequestEvent(MakeStuffing, exec.Now, 0.0, null, ExecEventType.Detachable);

                Console.WriteLine(" {0} : We'll wait 'til everything is done before we serve dinner!", exec.Now);
                exec.Join(code1, code2, code3);

                Console.WriteLine(" {0} : Serving dinner!", exec.Now);

            }

            public static void MakeTurkey(IExecutive exec, object userData)
            {
                Console.WriteLine(" {0} : Starting to make turkey...", exec.Now);
                exec.CurrentEventController.SuspendFor(TimeSpan.FromMinutes(300));
                Console.WriteLine(" {0} : ...done making turkey.", exec.Now);
            }

            public static void MakeGravy(IExecutive exec, object userData)
            {
                Console.WriteLine(" {0} : Starting to make gravy...", exec.Now);
                exec.CurrentEventController.SuspendFor(TimeSpan.FromMinutes(250));
                Console.WriteLine(" {0} : ...done making gravy.", exec.Now);
            }

            public static void MakeStuffing(IExecutive exec, object userData)
            {
                Console.WriteLine(" {0} : Starting to make stuffing...", exec.Now);
                exec.CurrentEventController.SuspendFor(TimeSpan.FromMinutes(30));
                Console.WriteLine(" {0} : ...done making stuffing.", exec.Now);
            }
        }

        /// <summary>
        /// Rescinding multiple events, with detachable events.
        /// </summary>
        internal class RescindMultipleDetachables
        {
            [Description(@"This demo creates an executive and a number of domain agents, rastro,
(a dog) and fifteen dog agents and fifteen cat agents. Then, for each agent,
an event is created which will cause them to speak at a specified time. These
are ten minutes apart. This would make for 50 minutes of speaking agents, 
except that two more events are requested - one, 35 minutes before the end 
of the 50 minutes, whose effect is to rescind all Speak events for rastro,
and another, 25 minutes before the end of the 50 minutes, whose effect is
to rescind all events targeted to objects of type ""Cat"".

This demonstrates some more advanced capabilities of event rescinding, and
is identical to the demo shown before, except in that it is executed on
detachable events.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 03:51:21");

                Domain.Sample1.Dog rastro = new Domain.Sample1.Dog("Rastro");

                for (int i = 0; i < 5; i++)
                {
                    // Schedule 15 speaking events.

                    Domain.Sample1.Cat aCat = new Domain.Sample1.Cat("Cat_" + i);
                    Domain.Sample1.Dog aDog = new Domain.Sample1.Dog("Dog_" + i);

                    exec.RequestEvent(rastro.Speak, when, 0.0, null, ExecEventType.Detachable);
                    exec.RequestEvent(aDog.Speak, when, 0.0, null, ExecEventType.Detachable);
                    exec.RequestEvent(aCat.Speak, when, 0.0, null, ExecEventType.Detachable);

                    when += TimeSpan.FromMinutes(10.0);
                }

                exec.RequestEvent(RescindIndividual, when - TimeSpan.FromMinutes(35), rastro);
                exec.RequestEvent(RescindCats, when - TimeSpan.FromMinutes(25));

                exec.Start();

            }

            private static void WriteIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : {1}", exec.Now, userData);
            }

            private static void RescindIndividual(IExecutive exec, object userData)
            {
                Domain.Sample1.Dog dog = (Domain.Sample1.Dog) userData;
                Console.WriteLine("{0} : Rescinding events to {1}.Speak()", exec.Now, dog.Name);
                exec.UnRequestEvents(new ExecEventReceiver(dog.Speak));
            }

            private static void RescindCats(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Rescinding all cat events.", exec.Now);
                //exec.UnRequestEvents(new Revoker(typeof(Cat)));
                // or...
                exec.UnRequestEvents(new Domain.Sample1.Cat("prototype"));

            }

            private class Revoker : IExecEventSelector
            {
                private readonly Type m_targetType;

                public Revoker(Type targetType)
                {
                    m_targetType = targetType;
                }

                #region IExecEventSelector Members

                public bool SelectThisEvent(ExecEventReceiver eer, DateTime when, double priority, object userData,
                    ExecEventType eet)
                {
                    return eer.Target != null && eer.Target.GetType() == m_targetType;
                }

                #endregion
            }
        }

    }

    /*namespace AsynchronousEvents
    { // Need to support this.
        //    class SampleA {
        //        public static void Run() {
        //            IExecutive exec = ExecFactory.Instance.CreateExecutive();

        //            exec.RequestEvent(DumpState, exec.Now, 0.0, null, ExecEventType.Asynchronous);
        //            exec.Start();

        //            Thread.Sleep(250); // wait a quarter second to allow the async thread to complete.
        //        }

        //        public static void DumpState(IExecutive exec, object userData) {
        //            Console.WriteLine("{0} Captured and am dumping state asynchronously.", exec.Now);
        //        }
        //    }
    }*/

    namespace AdvancedTopics
    {

        /// <summary>
        /// Using metronomes.
        /// </summary>
        internal class Metronomes
        {
            [Description(@"A metronome is useful when one or more elements of a simulation are to be
called at a fixed periodicity. This demo creates an executive and adds a
metronome to it with a period of 9000 minutes. Every 9000 minutes, the 
""TickEvent"" event fires, until the specified end time.")]
            public static void Run()
            {

                IExecutive exec = ExecFactory.Instance.CreateExecutive();
                DateTime startAt = DateTime.Parse("Thu, 10 Jul 2003 03:51:21");
                DateTime finishAfter = DateTime.Parse("Wed 15 Dec 2004 19:22:47");
                TimeSpan period = TimeSpan.FromMinutes(9000.0);
                Metronome_Simple metronome = Metronome_Simple.CreateMetronome(exec, startAt, finishAfter, period);

                metronome.TickEvent += WriteIt;

                exec.Start();
            }

            private static void WriteIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : A tick happened on the metronome", exec.Now);
            }
        }

        /// <summary>
        /// Pause and resume the executive in user-time.
        /// </summary>
        internal class PauseAndResume
        {
            [Description(@"This demo exhibits the pause-and-resume behavior of the executive that could,
for example, be tied to a button-press in the GUI.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();
                DateTime startAt = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");
                DateTime finishAfter = DateTime.Parse("Fri, 15 Jul 2016 23:59:59");
                TimeSpan period = TimeSpan.FromMinutes(60);
                Metronome_Simple metronome = Metronome_Simple.CreateMetronome(exec, startAt, finishAfter, period);

                metronome.TickEvent += WriteIt;

                exec.Start();
            }

            private static void WriteIt(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : A tick happened on the metronome", exec.Now);
                if (exec.Now.Hour == 13)
                {
                    Console.WriteLine("\tIt is {0} - pausing the simulation (just for 5 seconds.)", exec.Now);
                    exec.Pause(); // This could be called via a GUI action.
                    Thread.Sleep(5000);
                    Console.WriteLine("\tWe're resuming the simulation.");
                    exec.Resume(); // This could be called via a GUI action.
                }
            }
        }

        /// <summary>
        /// Use the ExecController to impact simulation speed. Run 10 minutes' (600 seconds)
        /// simulation in 6 seconds with 60 render events fired.
        /// </summary>
        internal class UseExecController
        {

            private static string m_state = "Uninitiated.";

            [Description(@"This demo shows the capabilities of the ExecController, which can be used
to drive scaled-rate animation. The ExecController's two relevant properties
to this are the ""Scale"" and the ""FrameRate"" properties. They determine the
optimal rate at which the simulation clock runs, relative to the wall clock,
and how often the ""Render"" event fires. We set up an executive to run for 
ten minutes' simulation time, paced to run at ten times the wall clock rate,
that is, for one minute of wall clock time. The model updates its internal
state every five milliseconds of simulation time and firese a ""Render"" event
 ten times per second of wall-clock time.")]
            public static void Run()
            {

                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                #region Set up a metronome to fire once a second in sim time.

                DateTime startAt = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");
                DateTime finishAfter = DateTime.Parse("Fri, 15 Jul 2016 00:09:59");
                TimeSpan period = TimeSpan.FromSeconds(.005);
                Metronome_Simple metronome = Metronome_Simple.CreateMetronome(exec, startAt, finishAfter, period);
                metronome.TickEvent += UpdateState;

                #endregion

                // Attach an ExecController so that it runs at 100 x of user time,
                // and issues 10 render events every second.
                double scale = 2.0; // 10^2, or 100 x real-time.
                int frameRate = 10; // 10 render events per second.
                ExecController execController = new ExecController(exec, scale, frameRate, exec);

                execController.Render += Render;

                exec.Start();

                Console.WriteLine("{0} simulation events fired.", exec.EventCount);
            }

            private static void Render(IExecutive exec, object userData)
            {
                Console.WriteLine("User time : {0}, Sim Time {1}. State = {2}.", DateTime.Now, exec.Now, m_state);
            }

            private static void UpdateState(IExecutive exec, object userData)
            {
                m_state = exec.Now.Second.ToString();
            }
        }

        /// <summary>
        /// Use Executive's ExecutiveStarted event to set up a simulation. Reset and restart the simulation.
        /// Introduction to the executive's state machine.
        /// </summary>
        internal class ExecEventModelAndStates
        {
            [Description(@"This demo shows how, if one wants to run the same simulation multiple times,
with a reset and restart each time, the ""ExecutiveStarted_SingleShot"" event
can be used to implement initial setup, and the ""ExecutiveStarted"" event
can be used to perform subsequent runs' initializations.")]
            public static void Run()
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                exec.ExecutiveStarted += ExecStarted;
                // Allows you to register handler on every Run invocation. Else
                // second run invocation would result in two handlers, third, etc.
                exec.ExecutiveStarted_SingleShot += ExecStarted_SingleShot;

                Console.WriteLine("Starting the simulation. Executive is in state {0}.", exec.State);
                exec.Start();

                Console.WriteLine("\r\nSimulation done. Executive is in state {0}.", exec.State);
                exec.Reset();
                Console.WriteLine("\r\nReset the simulation. Executive is in state {0}.", exec.State);

                Console.WriteLine("\r\nRe-running the simulation. Executive is in state {0}.", exec.State);
                exec.Start();
            }

            private static void ReportIt(IExecutive exec, object userData)
            {
                Console.WriteLine("Simulation time is {0}. Executive is in state {1}.", exec.Now, exec.State);
            }

            private static void ExecStarted_SingleShot(IExecutive exec)
            {
                Console.WriteLine("Beginning a multi-run study. Executive is in state {0}.", exec.State);
            }

            private static void ExecStarted(IExecutive exec)
            {
                Console.WriteLine("Setting up the simulation. Executive is in state {0}.", exec.State);
                exec.RequestEvent(ReportIt, DateTime.Now);
                exec.RequestEvent(ReportIt, DateTime.Now + TimeSpan.FromMinutes(10));
                exec.RequestEvent(ReportIt, DateTime.Now + TimeSpan.FromMinutes(20));
            }

        }

        internal class DaemonEvents
        {
            [Description(@"Normally, the executive runs until all registered events have been served
or an explicitly-supplied completion time is reached. This demo describes ""Daemon 
Event registrations, through which an event can be requested for service at a future
time, but unlike a standard non-daemon event, does not serve to keep the simulation alive
by virtue of its existence.")]
            public static void Run()
            {

                DateTime when = DateTime.Parse("Fri, 15 Jul 2016 00:00:00");

                IExecutive exec = ExecFactory.Instance.CreateExecutive();

                exec.RequestDaemonEvent(CheckIfStillRunning, when, 0.0, null);
                exec.RequestEvent(Finish, when + TimeSpan.FromMinutes(100));

                exec.Start();

            }

            private static void CheckIfStillRunning(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Still running.", exec.Now);
                exec.RequestDaemonEvent(CheckIfStillRunning, exec.Now + TimeSpan.FromMinutes(7), 0.0, null);
            }

            private static void Finish(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Done processing.", exec.Now);
            }
        }
    }
}
