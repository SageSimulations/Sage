/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.SimCore {
    /// <summary>
    /// This delegate is implemented by a method that is intended to perform part
    /// of the preparation for a transition. It accepts the model, and returns a
    /// reason for failing the transition, or null if the implementer of the 
    /// delegate condones the completion of the transition.
    /// </summary>
    public delegate ITransitionFailureReason PrepareTransitionEvent(IModel model, object userData);
    /// <summary>
    /// This delegate is implemented by a method that is intended to be notified
    /// of the successful attempt to perform a transition, and to take part in
    /// the commitment of that transition attempt. 
    /// </summary>
    public delegate void CommitTransitionEvent(IModel model, object userData);
    /// <summary>
    /// This delegate is implemented by a method that is intended to be notified
    /// of the unsuccessful attempt to perform a transition, and to take part in
    /// the rollback of that transition attempt. 
    /// </summary>
    public delegate void RollbackTransitionEvent(IModel model, object userData, IList reasons);

    /// <summary>
    /// Implemented by a transition handler. A transition handler embodies
    /// the actions to be performed when the state machine is asked to make
    /// a transition from one state to another. The transition is performed
    /// via a two-phase protocol, first preparing to make the transition,
    /// and then if no handler that was involved in the preparation phase
    /// registered a failure reason, a commit operation is begun. Otherwise,
    /// if objections were registered, a rollback operation is begun. 
    /// </summary>
    public interface ITransitionHandler {
        /// <summary>
        /// This event is fired when the transition is beginning, and all
        /// handlers are given an opportunity to register failure reasons.
        /// This event permits registration for the preparation using the
        /// standard += and -= syntax.
        /// </summary>
        event PrepareTransitionEvent Prepare;
        /// <summary>
        /// If preparation is successful, this event is fired to signify
        /// commitment of the transition.
        /// This event permits registration for the commitment using the
        /// standard += and -= syntax.
        /// </summary>
        event CommitTransitionEvent Commit;
        /// <summary>
        /// If preparation is not successful, this event is fired to
        /// signify the failure of an attempted transition.
        /// This event permits registration for the rollback using the
        /// standard += and -= syntax.
        /// </summary>
        event RollbackTransitionEvent Rollback;
        /// <summary>
        /// Indicates whether this transition is permissible.
        /// </summary>
        bool IsValidTransition { get; }

        /// <summary>
        /// Adds a handler to the Prepare event with an explicitly-specified
        /// sequence number. The sequence begins with those handlers that 
        /// have a low sequence number.
        /// </summary>
        /// <param name="pte">The handler for the Prepare event.</param>
        /// <param name="sequence">The sequence number for the handler.</param>
        void AddPrepareEvent(PrepareTransitionEvent pte, double sequence);
        /// <summary>
        /// Removes a handler from the set of handlers that are registered
        /// for the prepare event.
        /// </summary>
        /// <param name="pte">The PrepareTransitionEvent handler to remove.</param>
        void RemovePrepareEvent(PrepareTransitionEvent pte);
        /// <summary>
        /// Adds a handler to the Commit event with an explicitly-specified
        /// sequence number. The sequence begins with those handlers that 
        /// have a low sequence number.
        /// </summary>
        /// <param name="cte">The handler for the CommitTransitionEvent</param>
        /// <param name="sequence">The sequence number for the handler.</param>
        void AddCommitEvent(CommitTransitionEvent cte, double sequence);
        /// <summary>
        /// Removes a handler from the set of handlers that are registered
        /// for the commit event.
        /// </summary>
        /// <param name="cte">The handler for the CommitTransitionEvent</param>
        void RemoveCommitEvent(CommitTransitionEvent cte);
        /// <summary>
        /// Adds a handler to the Prepare event with an explicitly-specified
        /// sequence number. The sequence begins with those handlers that 
        /// have a low sequence number.
        /// </summary>
        /// <param name="rte">The handler for the Rollback event.</param>
        /// <param name="sequence">The sequence number for the handler.</param>
        void AddRollbackEvent(RollbackTransitionEvent rte, double sequence);
        /// <summary>
        /// Removes a handler from the set of handlers that are registered
        /// for the rollback event.
        /// </summary>
        /// <param name="rte">The handler for the Rollback event.</param>
        void RemoveRollbackEvent(RollbackTransitionEvent rte);

    }
    
    /// <summary>
    /// Implemented by a method that is to be called once the state machine
    /// completes transition to a specified state.
    /// </summary>
    public delegate void StateMethod(IModel model, object userData);

    /// <summary>
    /// A table-driven, two-phase-transaction state machine. The user configures
    /// the state machine with a number of states, and the state machine creates
    /// handlers for transition out of and into each state, as well as handlers
    /// for transitions between two specific states, and one universal transition
    /// handler. Each handler provides events that are fired when the state machine
    /// attempts and then either completes or rolls back a transition.  When a
    /// transition is requested, the state machine collects all of the outbound 
    /// transition handlers from the current state, all of the handlers into the 
    /// destination state, all handlers specified for both the current and destination
    /// states, and the universal handler. These handlers' 'Prepare' events are called
    /// in the order implied by their sequence numbers (if no sequence number was
    /// specified, it is assumed to be 0.0.) If all 'Prepare' handlers' event targets
    /// are called with none returning a TransitionFailureReason, then the State
    /// Machine calls all of the Commit events. If there was at least one
    /// TransitionFailureReason, then the 'Rollback' event handlers are called.
    /// </summary>
    public class StateMachine
    {
        private static readonly bool s_diagnostics = Highpoint.Sage.Diagnostics.DiagnosticAids.Diagnostics("StateMachine");

        /// <summary>
        /// Generic states are states that all state machines should support, and in declaring their states, set equality from
        /// appropriate states to these states. This will support interoperability of many libraries into state machines
        /// declared differently for different solutions, but with some of the same states defined in their lifecycles.
        /// </summary>
        public enum GenericStates : int {
            /// <summary>
            /// The model is idle. It has been built, perhaps not completely, but has not gone through any validation. 
            /// </summary>
            Idle = 0,
            /// <summary>
            /// The model is structurally valid.
            /// </summary>
            Validated = 1,
            /// <summary>
            /// The model has been properly initialized, and is ready to be run.
            /// </summary>
            Initialized = 2,
            /// <summary>
            /// The model is currently running.
            /// </summary>
            Running = 3,
            /// <summary>
            /// The model has completed running. The executive will read the last event time, or DateTime.MaxValue. Post-run data
            /// may be available, and a call to Reset() is probably necessary to run it again.
            /// </summary>
            Finished = 4
        }

        #region Private Fields
        private ITransitionHandler[,]  m_2DTransitions;
        private ITransitionHandler[]   m_transitionsFrom;
        private ITransitionHandler[]   m_transitionsTo;
        private ITransitionHandler     m_universalTransition;
        private int                    m_currentState;
        private int                    m_nextState;
        private bool                   m_transitionInProgress = false;
        private int                    m_numStates;
        private IModel                 m_model;
        private Array                  m_enumValues;
        private StateMethod[]          m_stateMethods;
        private Enum[]				   m_followOnStates;
        private Hashtable              m_stateTranslationTable;
        private Enum[]                 m_equivalentStates;

        private bool                   m_stateMachineStructureLocked = false;
        private MergedTransitionHandler[][] m_mergedTransitionHandlers;

        #endregion

        /// <summary>
        /// Creates a state machine that does not reference a Model. Many of the event
        /// delegates send a model reference with the notification. If the recipients
        /// all either (a) don't need this reference, (b) have it from elsewhere, or
        /// (c) the entity creating this state machine will set the Model later, then
        /// this constructor may be used.
        /// </summary>
        /// <param name="transitionMatrix">A matrix of booleans. 'From' states are the 
        /// row indices, and 'To' states are the column indices. The contents of a given
        /// cell in the matrix indicates whether that transition is permissible.</param>
        /// <param name="followOnStates">An array of enumerations of states, indicating
        /// which transition should occur automatically, if any, after transition into
        /// a given state has completed successfully.</param>
        /// <param name="initialState">Specifies the state in the state machine that is
        /// to be the initial state.</param>
        public StateMachine(bool[,] transitionMatrix, Enum[] followOnStates, Enum initialState)
            :this(null,transitionMatrix, followOnStates,initialState){}

        /// <summary>
        /// Creates a state machine that references a Model. Many of the event
        /// delegates send a model reference with the notification.
        /// </summary>
        /// <param name="model">The model to which this State Machine belongs.</param>
        /// <param name="transitionMatrix">A matrix of booleans. 'From' states are the 
        /// row indices, and 'To' states are the column indices. The contents of a given
        /// cell in the matrix indicates whether that transition is permissible.</param>
        /// <param name="followOnStates">An array of enumerations of states, indicating
        /// which transition should occur automatically, if any, after transition into
        /// a given state has completed successfully.</param>
        /// <param name="initialState">Specifies the state in the state machine that is
        /// to be the initial state.</param>
        public StateMachine(IModel model, bool[,] transitionMatrix, Enum[] followOnStates, Enum initialState){
            m_model = model;
            m_enumValues = Enum.GetValues(initialState.GetType());
            InitializeStateTranslationTable(initialState);
            if ( transitionMatrix.GetLength(0) != transitionMatrix.GetLength(1) ) {
                throw new ApplicationException("Transition matrix must be square.");
            }
            m_currentState    = GetStateNumber(initialState);
            m_2DTransitions   = new ITransitionHandler[m_numStates,m_numStates];
            m_transitionsFrom = new ITransitionHandler[m_numStates];
            m_transitionsTo   = new ITransitionHandler[m_numStates];
            m_stateMethods    = new StateMethod[m_numStates];

            m_followOnStates  = followOnStates;

            for ( int i = 0 ; i < m_numStates ; i++ ) {
                m_transitionsFrom[i] = new TransitionHandler();
                m_transitionsTo[i]   = new TransitionHandler();
                for ( int j = 0 ; j < m_numStates ; j++ ) {
                    if ( transitionMatrix[i,j] ) {
                        m_2DTransitions[i,j] = new TransitionHandler();
                    } else {
                        m_2DTransitions[i,j] = new InvalidTransitionHandler();
                    }
                }
            }

            m_universalTransition = new TransitionHandler();
        }
        
        /// <summary>
        /// Allows the caller to set the model that this State Machine
        /// references. 
        /// </summary>
        /// <param name="model">The model that this state machine references.</param>
        public void SetModel(IModel model){
            m_model = model;
        }
        
        /// <summary>
        /// Provides a reference to the transition handler that helps govern the
        /// transition between two specified states.
        /// </summary>
        /// <param name="from">The 'from' state that will select the transition handler.</param>
        /// <param name="to">The 'to' state that will select the transition handler.</param>
        /// <returns>The transition handler that will govern the transition between two
        /// specified states.</returns>
        public ITransitionHandler TransitionHandler(Enum from, Enum to){
            int iFrom = GetStateNumber(from);
            int iTo = GetStateNumber(to);
            return m_2DTransitions[iFrom, iTo];
        }

        /// <summary>
        /// Provides a reference to the transition handler that helps govern all
        /// transitions OUT OF a specified state.
        /// </summary>
        /// <param name="from">The 'from' state that will select the transition handler.</param>
        /// <returns>A reference to the transition handler that helps govern the
        /// transitions OUT OF a specified state.</returns>
        public ITransitionHandler OutboundTransitionHandler(Enum from){
            int iFrom = GetStateNumber(from);
            return m_transitionsFrom[iFrom];
        }

        /// <summary>
        /// Provides a reference to the transition handler that helps govern all
        /// transitions INTO a specified state.
        /// </summary>
        /// <param name="to">The 'to' state that will select the transition handler.</param>
        /// <returns>A reference to the transition handler that helps govern the
        /// transitions INTO a specified state.</returns>
        public ITransitionHandler InboundTransitionHandler(Enum to){
            int iTo = GetStateNumber(to);
            return m_transitionsTo[iTo];
        }

        /// <summary>
        /// Provides a reference to the transition handler that helps govern all transitions.
        /// </summary>
        /// <returns>A reference to the transition handler that helps govern all transitions.</returns>
        public ITransitionHandler UniversalTransitionHandler(){
            return m_universalTransition;
        }

        /// <summary>
        /// The current state of the state machine.
        /// </summary>
        public virtual Enum State { 
            get {
                return (Enum)m_enumValues.GetValue(m_currentState);
            }
        }

        /// <summary>
        /// Forces the state machine into the new state. No transitions are done, no handlers are called - It's just POOF, new state. Use this with extreme caution!
        /// </summary>
        /// <param name="state">The state into which the state machine is to be placed.</param>
        public void ForceOverrideState(Enum state){
            int tgtStateNum = -1;
            for ( int i = 0 ; i < m_enumValues.Length ; i++ ) {
                if ( m_enumValues.GetValue(new int[]{i}).Equals(state) ) {
                    tgtStateNum = i;
                }
            }
            if ( tgtStateNum > 0 ) {
                m_currentState = tgtStateNum;
            } else {
                throw new ApplicationException("Unable to force override state machine to state \"" + state + "\" because it is an unknown state.");
            }

        }
        
        /// <summary>
        /// True if the state machine is in the process of performing a transition.
        /// </summary>
        public bool IsTransitioning {
            get {
                return m_transitionInProgress;
            }
        }

        /// <summary>
        /// Provides the identity of the next state that the State Machine will enter.
        /// </summary>
        public virtual Enum NextState {
            get {
                // TODO: Is there a case where this would be ambiguous or wrong?
                return (Enum)m_enumValues.GetValue(m_nextState);
            }
        }

        /// <summary>
        /// Sets the method that will be called when the state machine enters a given state.
        /// </summary>
        /// <param name="newStateMethod">The method to be called.</param>
        /// <param name="forWhichState">The state in which the new method should be called.</param>
        /// <returns>The old state method, or null if there was none assigned.</returns>
        public StateMethod SetStateMethod(StateMethod newStateMethod, Enum forWhichState){
            StateMethod oldStateMethod = null;
            int iWhichState = GetStateNumber(forWhichState);
            oldStateMethod = m_stateMethods[iWhichState];
            m_stateMethods[iWhichState] = newStateMethod;
            return oldStateMethod;
        }

        /// <summary>
        /// Commands the state machine to attempt transition to the indicated state.
        /// Returns a list of ITransitionFailureReasons. If this list is empty, the
        /// transition was successful.
        /// </summary>
        /// <param name="toWhatState">The desired new state of the State Machine.</param>
        /// <returns>A  list of ITransitionFailureReasons. (Empty if successful.)</returns>
        public IList DoTransition(Enum toWhatState) {
            return DoTransition(toWhatState, null);
        }

        public bool StructureLocked {
            get { return m_stateMachineStructureLocked; }
            set { m_stateMachineStructureLocked = value; }
        }

        /// <summary>
        /// Commands the state machine to attempt transition to the indicated state.
        /// Returns a list of ITransitionFailureReasons. If this list is empty, the
        /// transition was successful.
        /// </summary>
        /// <param name="toWhatState">The desired new state of the State Machine.</param>
        /// <param name="userData">The user data to pass into this transition request - it will be sent out of each state change notification and state method.</param>
        /// <returns>
        /// A  list of ITransitionFailureReasons. (Empty if successful.)
        /// </returns>
        public IList DoTransition(Enum toWhatState, object userData) {
            Debug.Assert(m_model != null, "Did you forget to set the model on the State Machine?");
            try {
                m_transitionInProgress = true;
                m_nextState = GetStateNumber(toWhatState);

                if (s_diagnostics) {
                    Trace.Write("State machine in model \"" + m_model.Name + "\" servicing request to transition ");
                    Trace.WriteLine("from \"" + State + "\" into \"" + toWhatState + "\".");
                    StackTrace st = new StackTrace();
                    Trace.WriteLine(st.ToString());
                }

                // TODO: Determine if this is a good policy - it prohibits self-transitions.

                if (m_nextState == m_currentState) {
                    return new ArrayList();
                }

                MergedTransitionHandler mth = null;
                if (m_stateMachineStructureLocked) {
                    if (m_mergedTransitionHandlers == null) {
                        m_mergedTransitionHandlers = new MergedTransitionHandler[m_numStates][];
                        for (int i = 0 ; i < m_numStates ; i++) {
                            m_mergedTransitionHandlers[i] = new MergedTransitionHandler[m_numStates];
                        }
                    }
                    mth = m_mergedTransitionHandlers[m_currentState][m_nextState];
                }
                if (mth == null) {
                    TransitionHandler outbound = (TransitionHandler)m_transitionsFrom[m_currentState];
                    ITransitionHandler across = (ITransitionHandler)m_2DTransitions[m_currentState, m_nextState];
                    TransitionHandler inbound = (TransitionHandler)m_transitionsTo[m_nextState];
                    mth = new MergedTransitionHandler(outbound, (TransitionHandler)across, inbound, (TransitionHandler)m_universalTransition);
                    if (m_stateMachineStructureLocked) {
                        m_mergedTransitionHandlers[m_currentState][m_nextState] = mth;
                    }
                    if (across is InvalidTransitionHandler) {
                        string reason = "Illegal State Transition requested from " + State + " to " + toWhatState;
                        SimpleTransitionFailureReason stfr = new SimpleTransitionFailureReason(reason, this);
                        throw new TransitionFailureException(stfr);
                    }
                }

                if (s_diagnostics)
                    Trace.WriteLine(mth.Dump());
                IList failureReasons = mth.DoPrepare(m_model, userData);
                if (failureReasons.Count != 0) {
                    mth.DoRollback(m_model, userData, failureReasons);
                    return failureReasons;
                }

                mth.DoCommit(m_model, userData);

                if (s_diagnostics)
                    Trace.WriteLine("Exiting " + State);
                m_currentState = m_nextState;
                TransitionCompletedSuccessfully?.Invoke(m_model, userData);
                if (s_diagnostics)
                    Trace.WriteLine("Entering " + State);

                m_stateMethods[m_currentState]?.Invoke(m_model, userData);

                // After running the state method, see if there are any follow-on states to be processed.
                if (m_followOnStates == null || m_followOnStates[m_currentState] == null || m_followOnStates[m_currentState].Equals(State)) {
                    return null;
                } else {
                    return DoTransition(m_followOnStates[m_currentState], userData);
                }
                

            } finally {
                if (s_diagnostics)
                    Trace.WriteLine("Coming to a rest in state " + State);
                m_transitionInProgress = false;
                m_nextState = m_currentState;
            }
        }

        /// <summary>
        /// Attempts to run the sequence of transitions. If any fail, the call returns in the state where the failure occurred,
        /// and the reason list contains whatever reasons were given for the failure. This is to be used if the progression is
        /// simple. If checks and responses need to be done, the developer should build a more step-by-step sequencing mechanism.
        /// </summary>
        /// <param name="states">The states.</param>
        /// <returns></returns>
        public IList RunTransitionSequence(params Enum[] states) {
            IList retval = null;
            try {
                foreach (Enum t in states)
                {
                    retval = DoTransition(t);
                }
            } catch (TransitionFailureException tfe) {
                if (retval == null) {
                    retval = new ArrayList();
                }
                retval.Add(tfe);
            }
            return retval;
        }

        /// <summary>
        /// Determines whether the specified state is quiescent - i.e. has no automatic follow-on state.
        /// </summary>
        /// <param name="whichState">the specified state.</param>
        /// <returns>
        /// 	<c>true</c> if the specified state is quiescent; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStateQuiescent(Enum whichState) {
            return m_followOnStates[GetStateNumber(whichState)].Equals(whichState);
        }

        /// <summary>
        /// Sets the model-specific enums (states) that equate to each of the StateMachine.GenericState values.
        /// </summary>
        /// <param name="idle">The equivalent state for the generic idle state.</param>
        /// <param name="validated">The equivalent state for the generic validated state.</param>
        /// <param name="initialized">The equivalent state for the generic initialized state.</param>
        /// <param name="running">The equivalent state for the generic running state.</param>
        /// <param name="finished">The equivalent state for the generic finished state.</param>
        public void SetGenericStateEquivalents(Enum idle, Enum validated, Enum initialized, Enum running, Enum finished) {
            m_equivalentStates = new Enum[] { idle, validated, initialized, running, finished };
        }

        /// <summary>
        /// Gets the application defined Enum (state) that equates to the provided generic state.
        /// </summary>
        /// <param name="equivalentGenericState">The genericState whose equivalent is desired.</param>
        /// <returns>The enum that is equivalent, conceptually, the provided generic state.</returns>
        public Enum GetStateEquivalentTo(GenericStates equivalentGenericState) {
            if (m_equivalentStates == null) {
                throw new ApplicationException("A library is trying to use generic state equivalents, but none have been defined.");
            }
            return m_equivalentStates[(int)equivalentGenericState];
        }

        /// <summary>
        /// This event fires when a transition completes successfully, and reaches the intended new state.
        /// </summary>
        public event StateMethod TransitionCompletedSuccessfully;

        public void Detach(object obj) {

        }

        /// <summary>
        /// Prepares the state translation table
        /// </summary>
        /// <param name="state">Provides the enum type that contains the various states</param>
        private void InitializeStateTranslationTable(Enum state) {
            m_stateTranslationTable = new Hashtable();
            Array values = Enum.GetValues(state.GetType());
            m_numStates = values.GetLength(0);
            for (int i = 0 ; i < m_numStates ; i++) {
                m_stateTranslationTable.Add(values.GetValue(i), i);
            }

            if (s_diagnostics) {
                Trace.WriteLine("Initializing state machine table to the following states");
                foreach (object val in Enum.GetValues(state.GetType())) {
                    Trace.WriteLine(m_stateTranslationTable[val] + ", " + val + " : " + val.GetType());
                }
            }
        }

        /// <summary>
        /// Gets the state number for the provided state.
        /// </summary>
        /// <param name="stateEnum">The enum that represents the provided state.</param>
        /// <returns>The state number.</returns>
        internal int GetStateNumber(Enum stateEnum) {
            object tmp = m_stateTranslationTable[stateEnum];

            if (tmp == null) {
                IEnumerator enumerator = m_stateTranslationTable.Keys.GetEnumerator();
                enumerator.MoveNext();
                object firstEnum = enumerator.Current;

                string msg = "Cannot translate " + stateEnum + " to an index. It is of type " +
                    stateEnum.GetType() + " and this state machine is running on states of type " +
                    firstEnum.GetType();

                if (stateEnum.GetType() == typeof(DefaultModelStates)) {
                    msg += " You may have forgotten to provide a new GetStartEnum() method in your derived model.";
                }
                throw new ApplicationException(msg);
            }

            int num = (int)tmp;

            if (num < 0 || num >= m_numStates) {
                throw new ApplicationException("There is no state with the specified index.");
            }

            return num;
        }

        #region >>> Test Support Method <<<
#if DEBUG // Eventually, 'TESTING', not DEBUG
        /// <summary>
        /// Test method that exposes a state machine's state's number.
        /// </summary>
        /// <param name="stateEnum">The state.</param>
        /// <returns>The number that represents that state.</returns>
        public int _TestGetStateNumber(Enum stateEnum) {
            return GetStateNumber(stateEnum);
        }
#endif
        #endregion

    }

    internal class TransitionHandler : ITransitionHandler {
 
        #region Prepare Event
        protected SortedList m_prepareHandlers = new SortedList();
        private double m_nextPreparePriority = 0.0;
        public event PrepareTransitionEvent Prepare {
            add {
                AddPrepareEvent(value, (m_nextPreparePriority+=double.Epsilon));
            }
            remove {
                RemovePrepareEvent(value);
            }
        }
        public void AddPrepareEvent(PrepareTransitionEvent pte, double priority){
            if ( !m_prepareHandlers.ContainsValue(pte) ) {
                m_prepareHandlers.Add(priority,pte);
            }
        }
        public void RemovePrepareEvent(PrepareTransitionEvent pte){
            if ( m_prepareHandlers.ContainsValue(pte) ) {
                m_prepareHandlers.Remove(m_commitHandlers.GetKey(m_commitHandlers.IndexOfValue(pte)));
            }
        }
        internal SortedList PrepareHandlers { get { return m_prepareHandlers; } } 
        #endregion Prepare Event

        #region Commit Event
        protected SortedList m_commitHandlers = new SortedList();
        private double m_nextCommitPriority = 0.0;
        public event CommitTransitionEvent Commit {
            add {
                AddCommitEvent(value, (m_nextCommitPriority+=double.Epsilon));
            }
            remove {
                RemoveCommitEvent(value);
            }
        }
        public void AddCommitEvent(CommitTransitionEvent cte, double priority){
            if ( !m_commitHandlers.ContainsValue(cte) ) {
                m_commitHandlers.Add(priority,cte);
            }
        }
        public void RemoveCommitEvent(CommitTransitionEvent cte){
            if ( m_commitHandlers.ContainsValue(cte) ) {
                m_commitHandlers.Remove(m_commitHandlers.GetKey(m_commitHandlers.IndexOfValue(cte)));
            }
            
        }
        internal SortedList CommitHandlers { get { return m_commitHandlers; } } 
        #endregion

        #region Rollback Event
        protected SortedList m_rollbackHandlers = new SortedList();
        private double m_nextRollbackPriority = 0.0;
        public event RollbackTransitionEvent Rollback {
            add {
                AddRollbackEvent(value, (m_nextRollbackPriority+=double.Epsilon));
            }
            remove {
                RemoveRollbackEvent(value);
            }
        }
        public void AddRollbackEvent(RollbackTransitionEvent rte, double priority){
            if ( !m_rollbackHandlers.ContainsValue(rte) ) {
                m_rollbackHandlers.Add(priority,rte);
            }
        }
        public void RemoveRollbackEvent(RollbackTransitionEvent rte){
            if ( m_rollbackHandlers.ContainsValue(rte) ) {
                m_rollbackHandlers.Remove(m_commitHandlers.GetKey(m_commitHandlers.IndexOfValue(rte)));
            }
        }
        internal SortedList RollbackHandlers { get { return m_rollbackHandlers; } } 
        #endregion
        
        public bool  IsValidTransition { get { return true; } }

        public IList DoPrepare(IModel model, object userData) {
            ArrayList al = new ArrayList();
            for ( int i = 0 ; i < m_prepareHandlers.Count ; i++ ){
                PrepareTransitionEvent pte = (PrepareTransitionEvent)m_prepareHandlers.GetByIndex(i);
                object result = pte(model,userData);
                if ( result != null ) al.Add(result);
            }
            return al;
        }

        public void DoCommit(IModel model, object userData) {
            for ( int i = 0 ; i < m_commitHandlers.Count ; i++ ){
                CommitTransitionEvent cte = (CommitTransitionEvent)m_commitHandlers.GetByIndex(i); 
                cte(model, userData);
            }
        }

        public void DoRollback(IModel model, object userData, IList failureReasons) {
            for (int i = 0 ; i < m_rollbackHandlers.Count ; i++) {
                RollbackTransitionEvent rte = (RollbackTransitionEvent)m_rollbackHandlers.GetByIndex(i);
                rte(model, userData, failureReasons);
            }
        }

        public string Dump(){
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Recipients for \"Prepare\" event:");
            sb.Append("\t");sb.Append(m_prepareHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(m_prepareHandlers));

            sb.Append("Recipients for \"Rollback\" event:");
            sb.Append("\t");sb.Append(m_rollbackHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(m_rollbackHandlers));

            sb.Append("Recipients for \"Commit\" event:");
            sb.Append("\t");sb.Append(m_commitHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(m_commitHandlers));
            return sb.ToString();
        }
        
        private string DumpHandlers(SortedList handlers){
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int i = 0;
            foreach ( DictionaryEntry de in handlers){
                double   pri = Convert.ToDouble(de.Key);
                Delegate del = (Delegate)de.Value;
                sb.Append("\t" + i + ".)\t["+del.Target+"].["+del.Method+"] @ pri = " + pri + "\r\n");
                i++;
            }
            return sb.ToString();
        }
    }

    internal class MergedTransitionHandler : TransitionHandler {
        public MergedTransitionHandler(TransitionHandler inbound, 
                                       TransitionHandler across, 
                                       TransitionHandler outbound,
                                       TransitionHandler universal){

            MergeHandlers(ref m_prepareHandlers, inbound.PrepareHandlers, outbound.PrepareHandlers, across.PrepareHandlers, universal.PrepareHandlers);
            MergeHandlers(ref m_commitHandlers, inbound.CommitHandlers, outbound.CommitHandlers, across.CommitHandlers, universal.CommitHandlers);
            MergeHandlers(ref m_rollbackHandlers, inbound.RollbackHandlers, outbound.RollbackHandlers, across.RollbackHandlers, universal.RollbackHandlers);

        }

        private void MergeHandlers (ref SortedList target, SortedList src1, SortedList src2, SortedList src3, SortedList src4) {
            int nextKey = 0;
            ArrayList enumerators = new ArrayList(); 
            enumerators.Add(src1.GetEnumerator());
            enumerators.Add(src2.GetEnumerator());
            enumerators.Add(src3.GetEnumerator());
            enumerators.Add(src4.GetEnumerator());

            ArrayList removees = new ArrayList();
            foreach ( IEnumerator enumerator in enumerators ){
                if ( !enumerator.MoveNext() ) removees.Add(enumerator);
            }
            foreach (IEnumerator removee in removees ) enumerators.Remove(removee);

            while ( enumerators.Count != 0 ) {
                IEnumerator hostEnum = (IEnumerator)enumerators[0];
                double lowest = (double)((DictionaryEntry)hostEnum.Current).Key;
                foreach ( IEnumerator enumerator in enumerators ) {
                    double thisKey = (double)((DictionaryEntry)enumerator.Current).Key;
                    if ( thisKey < lowest ){
                        hostEnum = enumerator;
                        lowest = thisKey;
                    }
                }

                target.Add(nextKey++,((DictionaryEntry)hostEnum.Current).Value);
                if ( !hostEnum.MoveNext() ) enumerators.Remove(hostEnum);
            }
        }
    }
    
    internal class InvalidTransitionHandler : TransitionHandler {
        public new event PrepareTransitionEvent Prepare { add { Puke(); } remove { Puke(); } }
        public new event CommitTransitionEvent Commit { add { Puke(); } remove { Puke(); } }
        public new event RollbackTransitionEvent Rollback { add { Puke(); } remove { Puke(); } }
        public new bool IsValidTransition { get { return false; } }
        public new void AddPrepareEvent(PrepareTransitionEvent pte, double priority) { Puke(); }
        public new void RemovePrepareEvent(PrepareTransitionEvent pte) { Puke(); }
        public new void AddCommitEvent(CommitTransitionEvent cte, double priority) { Puke(); }
        public new void RemoveCommitEvent(CommitTransitionEvent cte) { Puke(); }
        public new void AddRollbackEvent(RollbackTransitionEvent rte, double priority) { Puke(); }
        public new void RemoveRollbackEvent(RollbackTransitionEvent rte) { Puke(); }

        private void Puke(){
            throw new ApplicationException("Attempt to interact with an event on an invalid transition.");
        }
    }
    
    /// <summary>
    /// An exception that is fired if and when a transition fails for a reason
    /// internal to the state machine - currently, this is only in the case of
    /// a request to perform an illegal state transition.
    /// </summary>
    public class TransitionFailureException : Exception {

        private readonly IList m_reasons;
        private readonly string m_message;

        private static string MessageFromReasons(IList reasons){
            string message = "";
            foreach ( ITransitionFailureReason itfr in reasons ) {
                message += itfr.Reason + Environment.NewLine;
            }
            return message;
        }
        private static string MessageFromReason(ITransitionFailureReason reason){
            ArrayList reasons = new ArrayList {reason};
            return MessageFromReasons(reasons);
        }

        /// <summary>
        /// Creates a TransitionFailureException around a list of failure reasons.
        /// </summary>
        /// <param name="reasons">A list of failure reasons.</param>
        public TransitionFailureException(IList reasons ) : base ( MessageFromReasons(reasons) ){
            m_message = MessageFromReasons(reasons);
        }



        /// <summary>
        /// Creates a TransitionFailureException around a single reason.
        /// </summary>
        /// <param name="reason">The TransitionFailureReason.</param>
        public TransitionFailureException(ITransitionFailureReason reason) : base ( MessageFromReason(reason) ){
            m_reasons = new ArrayList();
            m_reasons.Add(reason);
            m_message = MessageFromReason(reason);
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <value></value>
        /// <returns>The error message that explains the reason for the exception, or an empty string("").</returns>
        public override string Message { get { return m_message; } }

            /// <summary>
            /// Gives the caller access to the list (collection) of failure reasons.
            /// </summary>
            public ICollection Reasons { 
            get { return m_reasons; }
        }

        /// <summary>
        /// Provides a human-readable representation of the failure exception,
        /// in the form of a narrative describing the failure reasons.
        /// </summary>
        /// <returns>A narrative describing the failure reasons.</returns>
        public override string ToString(){
            int nr = m_reasons.Count;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Failure making model transition request. ");
            if ( nr == 1 ) {
                sb.Append("There is 1 reason why:");
            } else {
                sb.Append("There are " + nr + " reasons why:");
            }
        
            foreach ( ITransitionFailureReason itfr in m_reasons ) {
                sb.Append("\r\n\t");
                sb.Append(itfr.Reason);
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Encapsulates the reason for a transition failure, including what went wrong,
    /// and where.
    /// </summary>
    public interface ITransitionFailureReason {
        /// <summary>
        /// What went wrong.
        /// </summary>
        string Reason { get; }
        /// <summary>
        /// Where the problem arose.
        /// </summary>
        object Object { get; }
    }
   
    /// <summary>
    /// A simple class that implements ITransitionFailureReason
    /// </summary>
    public class SimpleTransitionFailureReason : ITransitionFailureReason {
        string m_reason;
        object m_object;
        /// <summary>
        /// Creates a SimpleTransitionFailureReason around a reason string and an object that
        /// indicates where the problem arose.
        /// </summary>
        /// <param name="reason">What went wrong.</param>
        /// <param name="Object">Where the problem arose.</param>
        public SimpleTransitionFailureReason(string reason, object Object){
            m_reason = reason;
            m_object = Object;
        }

        /// <summary>
        /// What went wrong.
        /// </summary>
        public string Reason { get { return m_reason; } }
        /// <summary>
        /// Where the problem arose.
        /// </summary>
        public object Object { get { return m_object; } }
    }


    /// <summary>
    /// The EnumStateMachine represents a simple state machine whose states are the values of the enum,
    /// whose transitions are all allowable, and which tracks the amount of run-time spent in each state.
    /// A developer can wrap this class in another to limit the permissible transitions, add handlers,
    /// etc.
    /// </summary>
    /// <typeparam name="TEnum">The type of the t enum.</typeparam>
    public class EnumStateMachine<TEnum> where TEnum : struct
    {
        public delegate void StateMachineEvent(TEnum from, TEnum to);
        private readonly IExecutive m_exec;
        private readonly TEnum m_initialState;
        private TEnum m_currentState;
        private DateTime m_lastStateChange;
        private Dictionary<TEnum, TimeSpan> m_stateTimes;
        private readonly bool m_trackTransitions;
        private List<TransitionRecord> m_transitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumStateMachine{TEnum}"/> class.
        /// </summary>
        /// <param name="exec">The executive whose time sequence will be followed in collecting statistics for this machine.</param>
        /// <param name="initialState">The state in which the machine initially resides.</param>
        /// <param name="trackTransitions">if set to <c>true</c>, the state machine will track the from, to, and time of each transition.</param>
        public EnumStateMachine(IExecutive exec, TEnum initialState, bool trackTransitions = true)
        {
            m_exec = exec;
            m_currentState = m_initialState = initialState;
            if (exec.State == ExecState.Running || exec.State == ExecState.Paused)
            {
                // Subject state machine was created while the model was running.
                ResetStatistics(m_exec.Now);
            }
            else
            {
                // Subject state machine was created before the model started running.
                m_exec.ExecutiveStarted += executive => ResetStatistics(m_exec.Now);                
            }
            m_exec.ExecutiveFinished += executive => UpdateStateTimes();
            m_trackTransitions = trackTransitions;
            m_stateTimes = new Dictionary<TEnum, TimeSpan>();
            Array ia = Enum.GetValues(typeof(TEnum));
            foreach (TEnum val in ia)
            {
                m_stateTimes.Add(val, TimeSpan.Zero);
            }
            if (m_trackTransitions) m_transitions = new List<TransitionRecord>();
        }

        /// <summary>
        /// The amount of time the machine spent in the specified state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>TimeSpan.</returns>
        public TimeSpan TimeSpentInState(TEnum state)
        {
            return m_stateTimes[state];
        }

        /// <summary>
        /// Resets the statistics kept on this machine's last run.
        /// </summary>
        /// <param name="when">The when.</param>
        private void ResetStatistics(DateTime when)
        {
            if (m_trackTransitions) m_transitions = new List<TransitionRecord>();
            m_stateTimes = new Dictionary<TEnum, TimeSpan>();
            Array ia = Enum.GetValues(typeof(TEnum));
            foreach (TEnum val in ia)
            {
                m_stateTimes.Add(val, TimeSpan.Zero);
            }
            m_lastStateChange = when;

        }

        /// <summary>
        /// Transitions the state of this machine to the specified state.
        /// </summary>
        /// <param name="toState">The requested destination state.</param>
        /// <returns><c>true</c> if the transition was successful, <c>false</c> otherwise.</returns>
        public virtual bool ToState(TEnum toState)
        { // TODO: Make this protected, and update all uses to employ implementation-specific transition methods.
            if (!toState.Equals(m_currentState))
            {
                if (m_trackTransitions)
                    m_transitions.Add(new TransitionRecord { From = m_currentState, To = toState, When = m_exec.Now });
                UpdateStateTimes();
                m_lastStateChange = m_exec.Now;
                m_currentState = toState;
            }
            return true; // Eventually return false if the transition was disallowed.
        }

        private void UpdateStateTimes()
        {
            TimeSpan ts = m_exec.Now - m_lastStateChange;
            m_stateTimes[m_currentState] += ts;
        }

        /// <summary>
        /// Gets the current state of the machine.
        /// </summary>
        /// <value>The state of the current.</value>
        public TEnum CurrentState
        {
            get { return m_currentState; }
        }

        /// <summary>
        /// Gets the state times in a dictionary of DateTimes, keyed on the enum value that represents the state.
        /// </summary>
        /// <value>The state times.</value>
        public IReadOnlyDictionary<TEnum, TimeSpan> StateTimes
        {
            get { return m_stateTimes; }
        }

        /// <summary>
        /// Gets the list of transitions experienced in the last run of the executive.
        /// </summary>
        /// <value>The transitions.</value>
        public IReadOnlyList<TransitionRecord> Transitions
        {
            get { return m_transitions; }
        }

        /// <summary>
        /// Records data on Transitions
        /// </summary>
        public struct TransitionRecord
        {
            /// <summary>
            /// The state from which the transition occurred.
            /// </summary>
            /// <value>From.</value>
            public TEnum From { get; set; }
            /// <summary>
            /// The state to which the transition occurred.
            /// </summary>
            /// <value>To.</value>
            public TEnum To { get; set; }
            /// <summary>
            /// When the transition occurred.
            /// </summary>
            /// <value>The when.</value>
            public DateTime When { get; set; }

        }
    }

}
