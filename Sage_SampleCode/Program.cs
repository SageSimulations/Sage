/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sage_SampleCode {
    static class Program {

        static void Main(string[] args)
        {

            Console.BufferWidth = 132;
            Console.BufferHeight = 9999;
            Console.WindowWidth = 132;
            Console.WindowHeight = 80;

            Demonstrate(Demo.Executive.SynchronousEvents.HelloWorld.Run);
            Demonstrate(Demo.Executive.SynchronousEvents.TwoCallbacksOutOfSequence.Run);
            Demonstrate(Demo.Executive.SynchronousEvents.CallbacksWithPriorities.Run);
            Demonstrate(Demo.Executive.SynchronousEvents.UserData_FollowOn_SelfImposedDelay.Run);
            Demonstrate(Demo.Executive.SynchronousEvents.ExecCatchesRuntimeExceptionFromSynchronousEvent.Run);
            Demonstrate(Demo.Executive.SynchronousEvents.RescindingSynchEvent.Run);
            Demonstrate(Demo.Executive.SynchronousEvents.MoreRescindingPlusAgentBased.Run);

            Demonstrate(Demo.Executive.DetachableEvents.BasicWithSuspends.Run);
            Demonstrate(Demo.Executive.DetachableEvents.SuspendsWithMixedModeAgents.Run);
            Demonstrate(Demo.Executive.DetachableEvents.UsesJoining.Run);
            Demonstrate(Demo.Executive.DetachableEvents.RescindMultipleDetachables.Run);

            Demonstrate(Demo.Executive.AdvancedTopics.Metronomes.Run);
            Demonstrate(Demo.Executive.AdvancedTopics.PauseAndResume.Run);
            Demonstrate(Demo.Executive.AdvancedTopics.UseExecController.Run);
            Demonstrate(Demo.Executive.AdvancedTopics.ExecEventModelAndStates.Run);
            Demonstrate(Demo.Executive.AdvancedTopics.DaemonEvents.Run);

            Demonstrate(Demo.StateManagement.InAgents.Run);
            Demonstrate(Demo.StateManagement.InUserData.Run);
            Demonstrate(Demo.StateManagement.OnTheStackFrame.Run);

            Demonstrate(Demo.RandomServer.SimpleDefaultServer.Run);
            Demonstrate(Demo.RandomServer.DecorrellatedActivities.Run);

            Demonstrate(Demo.StateMachine.Basic.Default.Run);
            Demonstrate(Demo.StateMachine.Basic.SimpleCustomWithInitialization.Run);
            Demonstrate(Demo.StateMachine.Basic.SimpleEnumStateMachine.Run);

            Demonstrate(Demo.Model.Basic.DefaultModel.Run);
            Demonstrate(Demo.Model.Basic.SimpleCustomWithInitialization.Run);

            Demonstrate(Demo.Resources.Basic.ServicePoolExample.Run);
            Demonstrate(Demo.Resources.Basic.ServicePoolExampleWithSynchronousEvents.Run);
            Demonstrate(Demo.Resources.Advanced.OptimalResourceAcquisition.Run);

            Demonstrate(Demo.SequenceControl.Basic.TaskGraphDemo.Run);

            Thread thread = new Thread(() => Clipboard.SetText(s_sb.ToString()));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private static bool _prompts = false;
        private static readonly string s_markerLine = new string('-', 79);

        private static void Demonstrate(Action run)
        {
            s_collectDocs?.Invoke(run); // Collect documentation only if s_collectDocs isn't null.

            if (!_prompts) Console.WriteLine(s_markerLine);

            string demoName = run.Method.DeclaringType?.Name;
            if (_prompts)
            {
                Console.Clear();
                Console.WriteLine();
            }

            object[] oa = run.Method.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string message = "";
            if (oa.Length == 1) message = ((DescriptionAttribute)oa[0]).Description;

            Console.WriteLine("Demo : {0}\r\n\r\n{1}", demoName, message);
            if (_prompts)
            {
                Console.WriteLine("Press any key to run the demo");
                Console.ReadKey();
            }
            run();
            if (_prompts)
            {
                Console.WriteLine("Press any key to continue ('U' to run unprompted.)");
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.U) _prompts = false;
            }
        }

        private static readonly StringBuilder s_sb = new StringBuilder();
        private static string _feature = "";
        private static string _subFeature = "";
        private static readonly Action<Action> s_collectDocs = CreateDocs; // Uncomment this line to turn on doc creation.
        // private static readonly Action<Action> s_collectDocs = null; // Uncomment this line to turn off doc creation.
        private static void CreateDocs(Action run)
        {
            string @namespace = run.Method.DeclaringType?.Namespace;
            string demoNamespace = @namespace.StartsWith("Demo.")? @namespace.Substring(5) : @namespace;
            Debug.Assert(!string.IsNullOrEmpty(demoNamespace));
            if (demoNamespace.Contains("."))
            {
                string demoFeature = demoNamespace.Substring(0, demoNamespace.IndexOf('.'));
                if (!string.Equals(demoFeature, _feature))
                {
                    s_sb.AppendLine(string.Format("<h2>{0}</h2>", demoFeature));
                    _feature = demoFeature;
                }


                string subFeature = demoNamespace.Substring(demoNamespace.IndexOf('.') + 1);
                Debug.Assert(!string.IsNullOrEmpty(subFeature));
                if (!string.Equals(subFeature, _subFeature))
                {
                    s_sb.AppendLine(string.Format("<h3>{0}</h3>", subFeature));
                    _subFeature = subFeature;
                }
            }
            else
            {
                string demoFeature = demoNamespace;
                if (!string.Equals(demoFeature, _feature))
                {
                    s_sb.AppendLine(string.Format("<h2>{0}</h2>", demoFeature));
                    _feature = demoFeature;
                }
            }

            string demoName = run.Method.DeclaringType?.Name;
            s_sb.AppendLine(string.Format("<h4>{0}</h4>", demoName));

            object[] oa = run.Method.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string message = oa.Length == 1 ? ((DescriptionAttribute) oa[0]).Description.Replace("\r\n\r\n", "</p>\r\n<p>") : "ERROR";
            s_sb.AppendLine($"<p>{message}</p>");
        }
    }
}
