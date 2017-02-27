/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using Highpoint.Sage.Graphs.PFC.Execution;

namespace Highpoint.Sage.Graphs.PFC {

    /// <summary>
    /// A PfcTransition acts as a Transition in a Pfc (Procedure Function Chart).
    /// </summary>
    public class PfcTransition : PfcNode, IPfcTransitionNode {

        #region Private Fields

        private ExecutableCondition m_executableExpression;
        private Expressions.Expression m_expression = null;
        private TransitionStateMachine m_myTransitionStateMachine = null;

        #endregion Private Fields

        #region Static Fields
        public static ExecutableCondition DefaultExecutableExpression
            = new ExecutableCondition(delegate(object userData, TransitionStateMachine tsm) {
            return tsm.PredecessorStateMachines.TrueForAll(delegate(StepStateMachine ssm) { return ssm.GetState((PfcExecutionContext)userData) == StepState.Complete; });
        });



        /// <summary>
        /// This is the default expression that new Transitions take on. The initial setting is
        /// performed automatically, subsequent resettings can be done via this field.
        /// </summary>
        public static string DefaultExpression = Expressions.PredecessorsComplete.NAME;

        #endregion 
        
        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcTransition"/> class.
        /// </summary>
        public PfcTransition() : this(null, "", "", Guid.NewGuid()) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcTransition"/> class.
        /// </summary>
        /// <param name="parent">The ProcedureFunctionChart this transition runs in.</param>
        /// <param name="name">The name of this transition.</param>
        /// <param name="description">The description for this transition.</param>
        /// <param name="guid">The GUID of this transition.</param>
        public PfcTransition(IProcedureFunctionChart parent, string name, string description, Guid guid)
            : base(parent, name, description, guid) {
            m_expression = Expressions.Expression.FromUf(DefaultExpression,parent.ParticipantDirectory, this);
            m_executableExpression = DefaultExecutableExpression;
        }

        #endregion Constructors

        #region IPfcTransitionNode Members

        /// <summary>
        /// Gets the expression that is attached to this transition.
        /// </summary>
        /// <value>The expression.</value>
        public Expressions.Expression Expression { get { return m_expression; } }

        /// <summary>
        /// Gets or sets the 'friendly' value of this expression. Uses step names and macro names.
        /// </summary>
        /// <value>The expression value.</value>
        public string ExpressionUFValue {
            get {
                return Expression.ToString(Expressions.ExpressionType.Friendly, this);
            }
            set {
                m_expression = Expressions.Expression.FromUf(value, Parent.ParticipantDirectory, this);
            }
        }

        /// <summary>
        /// Gets or sets the 'hostile' value of this expression.
        /// </summary>
        /// <value>The expression value.</value>
        public string ExpressionUHValue {
            get {
                return Expression.ToString(Expressions.ExpressionType.Hostile, this);
            }
            set {
                m_expression = Expressions.Expression.FromUh(value, Parent.ParticipantDirectory, this);
            }
        }

        /// <summary>
        /// Gets the expanded value of this expression. Uses step names and expands macro names into their resultant names.
        /// </summary>
        /// <value>The expanded value of this expression.</value>
        public string ExpressionExpandedValue {
            get {
                return Expression.ToString(Expressions.ExpressionType.Expanded, this);
            }
        }

        /// <summary>
        /// Gets or sets the default executable condition, that is the executable condition that this transition will
        /// evaluate unless overridden in the execution manager.
        /// </summary>
        /// <value>The default executable condition.</value>
        public ExecutableCondition ExpressionExecutable { get { return m_executableExpression; } set { m_executableExpression = value; } }

        public TransitionStateMachine MyTransitionStateMachine {
            get {
                if (m_myTransitionStateMachine == null) {
                    object obj = ( (ProcedureFunctionChart)Parent ).ExecutionEngine; // Forces initialization so everyone has a TSM.
                }
                return m_myTransitionStateMachine;
            }
            internal set {
                if (m_myTransitionStateMachine != null && value != null) {
                    string message = string.Format(s_msg_Replace_Existing_Sm, Name);
                    throw new ApplicationException(message);
                } else {
                    m_myTransitionStateMachine = value;
                }
            }
        }

        private static readonly string s_msg_Replace_Existing_Sm = "Attempt to replace an existing step state machine on {0}. " 
            + "This could be due to initializing the Execution Engine on a PFC before that PFC's construction is complete.";

        #endregion

        #region IPfcElement Members

        /// <summary>
        /// Gets the type of this element.
        /// </summary>
        /// <value>The type of the element.</value>
        public override PfcElementType ElementType {
            get { return PfcElementType.Transition; }
        }

        /// <summary>
        /// Gets a value indicating whether this transition instance is null. A transition that is null can be
        /// eliminated when PFCs are combined.
        /// </summary>
        /// <value><c>true</c> if this instance is null; otherwise, <c>false</c>.</value>
        public override bool IsNullNode {
            get {
                return (/*PredecessorNodes.Count < 2 && SuccessorNodes.Count < 2 &&*/ ExpressionUFValue.Equals(DefaultExpression));
            }
            set {
                //System.Diagnostics.Debug.Assert(value == true, "Transitions are non-null only by virtue of their expressions.");
            }
        }

        #endregion 

        public static IPfcTransitionNode Between(IPfcStepNode before, IPfcStepNode after) {
            return (IPfcTransitionNode)before.SuccessorNodes.Find(delegate(IPfcNode trans) { return trans.SuccessorNodes.Contains(after); });
        }

        /// <summary>
        /// The TransitionComparer is used to sort transitions by their graph ordinals.
        /// </summary>
        public class TransitionComparer : IComparer<IPfcTransitionNode> {
            /// <summary>
            /// Compares two IPfcTransitionNodes and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first IPfcTransitionNode to compare.</param>
            /// <param name="y">The second IPfcTransitionNode to compare.</param>
            /// <returns>
            /// Value Condition Less than zero, x is less than y. Zero, x equals y.Greater than zero, x is greater than y.
            /// </returns>
            public int Compare(IPfcTransitionNode x, IPfcTransitionNode y) {
                int retval = Comparer<int>.Default.Compare(x.GraphOrdinal, y.GraphOrdinal);
                if (retval == 0) {
                    retval = Utility.GuidOps.Compare(x.Guid, y.Guid);
                }
                return retval;
            }
        }
    }
}
