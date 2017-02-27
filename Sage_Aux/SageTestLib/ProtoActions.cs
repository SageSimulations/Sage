/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Console;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;


namespace SchedulerDemoMaterial  {

    [TestClass]
    public class TestActions {

		#region Predefined Timespans
		private static TimeSpan FIVE_MINS    = TimeSpan.FromMinutes(05.0);
		private static TimeSpan TEN_MINS     = TimeSpan.FromMinutes(10.0);
		private static TimeSpan FIFTEEN_MINS = TimeSpan.FromMinutes(15.0);
		private static TimeSpan TWENTY_MINS  = TimeSpan.FromMinutes(20.0);
		#endregion

		[TestMethod]
		public void DoActionTest1(){

			#region Create Actions
			Action task1 = new Action("Task 1",FIVE_MINS,0.0);
			Action task2 = new Action("Task 2",TEN_MINS,0.0);
			Action task3 = new Action("Task 3",FIFTEEN_MINS,0.0);
			Action task4 = new Action("Task 4",TEN_MINS,0.0);
			Action task5 = new Action("Task 5",TWENTY_MINS,0.0);
			Action task6 = new Action("Task 6",FIVE_MINS,0.0);
			#endregion

			IExecutive exec = ExecFactory.Instance.CreateExecutive();

			IAction scheme = new ActionList(task1,new ConcurrentActionSet(task2,task3),task4,new ParallelActionSet(task5,task6));
			exec.RequestEvent(new ExecEventReceiver(scheme.Run),DateTime.Now,0.0,null,ExecEventType.Detachable);
			
			exec.Start();

		}

		public void DoActionTest2(){
			IExecutive exec = ExecFactory.Instance.CreateExecutive();

			IAction part1 = new ParallelActionSet(new Action("PreDelay",FIVE_MINS,1.0),new Action("RscAcquire",TEN_MINS,1.0));
			IAction part2 = new Action("PreDelay",TimeSpan.FromMinutes(5.0),1.0);
			IAction part3 = new ConcurrentActionSet(new Action("XferIn",FIVE_MINS,1.0),new Action("XferOut",FIFTEEN_MINS,1.0));
			IAction part4 = new Action("PostDelay",TimeSpan.FromMinutes(5.0),1.0);
			IAction part5 = new ParallelActionSet(new Action("RscRelease",FIVE_MINS,1.0),new Action("PostDelay",TWENTY_MINS,1.0));
			IAction scheme = new ActionList(part1, part2, part3, part4, part5);

			exec.RequestEvent(new ExecEventReceiver(scheme.Run),DateTime.Now,0.0,null,ExecEventType.Detachable);
			exec.Start();
		}
	}

	
	public interface IAction {

		event ExecEventReceiver Starting;
		void Run(IExecutive exec, object userData);
		event ExecEventReceiver Finishing;
		string Name { get; }

	}
		
	public class Action : IAction {

        #region Private Fields
        private string m_name;
		private TimeSpan m_duration;
		private double m_priority;
        #endregion

        public Action(string name, TimeSpan duration, double priority) {
			m_name  = name;
			m_duration = duration;
			m_priority = priority;
		}
		
		#region IAction Members
		public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;
		public void Run(IExecutive exec, object userData) {
			Console.WriteLine(exec.Now + " : " + m_name + " is starting.");
			if ( Starting != null ) Starting(exec,userData);

			Console.WriteLine(exec.Now + " : " + m_name + " is pausing.");
			exec.CurrentEventController.SuspendUntil(exec.Now + m_duration);

			if ( Finishing != null ) Finishing(exec,userData);
			Console.WriteLine(exec.Now + " : " + m_name + " is completing.");
		}
		public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;
		public string Name { get { return m_name; } }
		#endregion
	}

	public class ActionList : IAction {

        #region Private Fields
        private IAction[] m_actions;
		private string m_name;
        #endregion

        public ActionList(params IAction[] actions){
			m_actions = actions;
			m_name = "[List] ";
			foreach ( IAction action in m_actions ) m_name += (" : " + action.Name);
		}

		#region IAction Members

		public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;

		public void Run(IExecutive exec, object userData) {
			if ( Starting != null ) Starting(exec,userData);
			foreach ( IAction action in m_actions )  action.Run(exec, userData);
			if ( Finishing != null ) Finishing(exec,userData);
		}

		public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;

		public string Name {
			get {
				return m_name;
			}
		}

		#endregion

	}
	public class ParallelActionSet : IAction {

		#region Private Fields
		private IAction[] m_actions;
		private IDetachableEventController m_myIDEC;
		private object m_lock;
		private int m_remaining;
		private string m_name;
		#endregion

		#region Constructors
		public ParallelActionSet(params IAction[] actions) {
			m_actions = actions;
			m_lock = new object();
			m_remaining = 0;
			m_name = "[Parallel]";
			foreach ( IAction action in actions ) m_name += ( " : " + action.Name );
		}
		#endregion

		#region IAction Members

		public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;

		public void Run(IExecutive exec, object userData) {
			Console.WriteLine(exec.Now + " : " + m_name + " is starting.");
			if ( Starting != null ) Starting(exec,userData);

			foreach ( IAction action in m_actions ) {
				action.Finishing+=new ExecEventReceiver(action_Finishing);
				exec.RequestEvent(new ExecEventReceiver(action.Run),exec.Now,0.0,userData,ExecEventType.Detachable);
				m_remaining++;
			}
			WaitForAllActionsToComplete(exec);

			if ( Finishing != null ) Finishing(exec,userData);
			Console.WriteLine(exec.Now + " : " + m_name + " is completing.");
		}

		public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;

		public string Name { get { return m_name; } }
		#endregion

		private void action_Finishing(IExecutive exec, object userData) {
			if ( --m_remaining == 0 ) m_myIDEC.Resume();
		}

		private void WaitForAllActionsToComplete(IExecutive exec) {
			m_myIDEC = exec.CurrentEventController;
			m_myIDEC.Suspend();
		}
	}
	
	public class ConcurrentActionSet : IAction {

		#region Private Fields
		private IAction[] m_actions;
		private ArrayList m_suspendedIdecs;
		private object m_lock;
		private int m_remaining;
		private string m_name;
		#endregion

		#region Constructors
		public ConcurrentActionSet(params IAction[] actions) {
			m_actions = actions;
			m_suspendedIdecs = new ArrayList();
			m_lock = new object();
			m_remaining = 0;
			m_name = "[Parallel]";
			foreach ( IAction action in actions ) m_name += ( " : " + action.Name );
		}
		#endregion

		#region IAction Members

		public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;

		public void Run(IExecutive exec, object userData) {
			Console.WriteLine(exec.Now + " : " + m_name + " is starting.");
			if ( Starting != null ) Starting(exec,userData);

			foreach ( IAction action in m_actions ) {
				action.Finishing+=new ExecEventReceiver(action_Finishing);
				exec.RequestEvent(new ExecEventReceiver(action.Run),exec.Now,0.0,userData,ExecEventType.Detachable);
				m_remaining++;
			}
			m_suspendedIdecs.Add(exec.CurrentEventController);
			exec.CurrentEventController.Suspend();

			if ( Finishing != null ) Finishing(exec,userData);
			Console.WriteLine(exec.Now + " : " + m_name + " is completing.");
		}

		public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;

		public string Name { get { return m_name; } }
		#endregion

		private void action_Finishing(IExecutive exec, object userData) {
			if ( m_suspendedIdecs.Count == m_actions.Length ) {
				foreach ( IDetachableEventController idec in m_suspendedIdecs ) idec.Resume();
			} else {
				m_suspendedIdecs.Add(exec.CurrentEventController);
				exec.CurrentEventController.Suspend();
			}
		}
	}
}

