/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Graphs.PFC.Execution;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs.PFC {

    public class PfcStep : PfcNode, IPfcStepNode {

        #region Private Fields

        private Dictionary<string, IProcedureFunctionChart> m_actions;
        private Utility.LabelManager m_labelManager;
        private IPfcUnitInfo m_unit = null;
        private StepStateMachine m_myStepStateMachine = null;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcStep"/> class.
        /// </summary>
        public PfcStep() : this(null, null, null, Guid.NewGuid()) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcStep"/> class.
        /// </summary>
        /// <param name="parent">The parent Pfc of this step.</param>
        /// <param name="name">The name of this step.</param>
        /// <param name="description">The description for this step.</param>
        /// <param name="guid">The GUID of this step.</param>
        public PfcStep(IProcedureFunctionChart parent, string name, string description, Guid guid)
            : base(parent, name, description, guid) {
            m_actions = new Dictionary<string, IProcedureFunctionChart>();
            m_labelManager = new Utility.LabelManager();
        }

        #endregion Constructors

        #region IPfcStepNode Members

        /// <summary>
        /// Gets the type of this element.
        /// </summary>
        /// <value>The type of the element.</value>
        public override PfcElementType ElementType {
            get { return PfcElementType.Step; }
        }

        #region Element Enumerables

        /// <summary>
        /// Gets all of the elements that are contained in or under this Pfc, to a depth
        /// specified by the 'depth' parameter, and that pass the 'filter' criteria.
        /// </summary>
        /// <param name="depth">The depth to which retrieval is to be done.</param>
        /// <param name="filter">The filter predicate that dictates which elements are acceptable.</param>
        /// <param name="children">The children, treated as a return value.</param>
        public void GetChildren(int depth, Predicate<IPfcElement> filter, ref List<IPfcElement> children) {
            if (depth > 0 && Actions.Count > 0) {
                foreach (IProcedureFunctionChart pfc in Actions.Values) {
                    pfc.GetChildren(depth, filter, ref children);
                }
            }
        }

        #endregion Element Enumerables

        /// <summary>
        /// Finds the child node, if any, at the specified path relative to this node.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IPfcElement Find(string path) {
            throw new Exception("This method has not been implemented.");
        }

        /// <summary>
        /// Gets the actions associated with this PFC Step. They are keyed by ActionName, and are themselves, PFCs. This dictionary
        /// should not be changed - use AddAction(string ,IProcedureFunctionChart).
        /// </summary>
        /// <value>The actions.</value>
        public Dictionary<string, IProcedureFunctionChart> Actions {
            [DebuggerStepThrough]
            get {
                if (m_actions == null) {
                    lock (typeof(PfcStep)) {
                        if (m_actions == null) {
                            m_actions = new Dictionary<string, IProcedureFunctionChart>();
                        }
                    }
                }
                return m_actions;
            }
        }

        /// <summary>
        /// Adds a child Pfc into the actions list under this step.
        /// </summary>
        /// <param name="actionName">The name of this action.</param>
        /// <param name="pfc">The Pfc that contains procedural details of this action.</param>
        public void AddAction(string actionName, IProcedureFunctionChart pfc) {
            Actions.Add(actionName, pfc);
        }

        private static PfcAction _defaultPfcAction = delegate(PfcExecutionContext pfcec, StepStateMachine ssm){return;};
        private PfcAction m_pfcAction = _defaultPfcAction;
        public PfcAction LeafLevelAction{
            get { 
                return m_pfcAction==null?_defaultPfcAction:m_pfcAction; 
            } 
            set { 
                m_pfcAction = value; 
            }
        }

        /// <summary>
        /// Sets the Actor that will determine the behavior behind this step. The actor provides the leaf level
        /// action, as well as preconditiond for running.
        /// </summary>
        /// <param name="actor">The actor that will provide the behaviors.</param>
        public void SetActor(PfcActor actor) {
            LeafLevelAction = new PfcAction(actor.Run);
            Precondition = new PfcAction(actor.GetPermissionToStart);
        }


        public StepStateMachine MyStepStateMachine {
            get {
                if (m_myStepStateMachine == null) {
                    // ReSharper disable once UnusedVariable
                    object obj = ( (ProcedureFunctionChart)Parent ).ExecutionEngine; // Forces initialization so everyone has a SSM.
                }
                return m_myStepStateMachine;
            }
            internal set {
                if (m_myStepStateMachine != null && value != null) {
                    string message = string.Format(s_msg_Replace_Existing_Sm, Name);
                    throw new ApplicationException(message);
                } else {
                    m_myStepStateMachine = value;
                }
            }
        }

#pragma warning disable 67
        /// <summary>
        /// Occurs when PFC is starting.
        /// </summary>
        public event PfcAction PfcStarting;
#pragma warning restore 67

        private static readonly string s_msg_Replace_Existing_Sm = "Attempt to replace an existing step state machine on {0}. " 
            + "This could be due to initializing the Execution Engine on a PFC before that PFC's construction is complete.";

        /// <summary>
        /// Gets permission from the step to transition to run.
        /// </summary>
        /// <param name="myPfcec">My pfcec.</param>
        /// <param name="ssm">The StepStateMachine that will govern this run.</param>
        public virtual void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm) {
            
            IExecutive exec = myPfcec.Model.Executive;
            Debug.Assert(exec.CurrentEventType == ExecEventType.Detachable);
            if (EarliestStart != null && EarliestStart > exec.Now) {
                exec.CurrentEventController.SuspendUntil(EarliestStart.Value);
            }

            m_precondition?.Invoke(myPfcec,ssm);
        }

        private PfcAction m_precondition = null;
        public PfcAction Precondition { 
            set { 
                m_precondition = value;
            }
            get {
                return m_precondition;
            }
        }

        /// <summary>
        /// Returns the Guid of the element in the source recipe that is represented by this PfcStep.
        /// </summary>
        public Guid RecipeSourceGuid { get { return Guid; } }

        #endregion

        /// <summary>
        /// Returns the actions under this Step as a procedure function chart.
        /// </summary>
        /// <returns>A procedure function chart containing the actions under this Step.</returns>
        public ProcedureFunctionChart ToProcedureFunctionChart() {
            return ProcedureFunctionChart.CreateFromStep(this, false);
        }

        /// <summary>
        /// Returns the actions under this Step as a procedure function chart.
        /// </summary>
        /// <param name="autoFlatten">if set to <c>true</c>, flattens each PFC under this step and its actions and their steps' actions.</param>
        /// <returns>A procedure function chart containing the actions under this Step.</returns>
        public ProcedureFunctionChart ToProcedureFunctionChart(bool autoFlatten) {
            return ProcedureFunctionChart.CreateFromStep(this,autoFlatten);
        }

        /// <summary>
        /// Gets the unit with which this step is associated.
        /// </summary>
        /// <value>The unit.</value>
        public IPfcUnitInfo UnitInfo {
            get {
                if (m_unit == null) {
                    m_unit = new PfcUnitInfo(null, -1);
                }
                return m_unit;
            }
            set { m_unit = value; }
        }

        #region IHasLabel Members

        /// <summary>
        /// Sets the label in the context indicated by the provided context, or if null or String.Empty has been selected, then in the default context.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        public void SetLabel(string label, string context) {
            m_labelManager.SetLabel(label, context);
        }

        /// <summary>
        /// Gets the label from the context indicated by the provided context, or if null or String.Empty has been selected, then from the default context.
        /// </summary>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        /// <returns></returns>
        public string GetLabel(string context) {
            return m_labelManager.GetLabel(context);
        }

        #endregion

        public class StepComparer : IComparer<IPfcStepNode> {
            #region IComparer<IPfcStepNode> Members

            public int Compare(IPfcStepNode x, IPfcStepNode y) {
                int retval = Comparer.Default.Compare(x.GraphOrdinal, y.GraphOrdinal);
                if (retval == 0) {
                    Utility.GuidOps.Compare(x.Guid, y.Guid);
                }
                return retval;
            }

            #endregion
        }

    }
}
