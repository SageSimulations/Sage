/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Highpoint.Sage.Graphs.PFC.Expressions;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using Highpoint.Sage.Graphs.PFC.Execution;

namespace Highpoint.Sage.Graphs.PFC {

    #region Enumerations
   
    public enum PfcElementType { Link, Transition, Step }

    public enum StandardMacros { NewPfc };

    #endregion Enumerations

    #region Delegates
    
    public delegate void LinkableEvent(IPfcNode node);
    public delegate void LinkEvent(IPfcLinkElement node);
    public delegate void NodeEvent(IPfcElement node);

    #endregion Delegates

    /// <summary>
    /// An implementer of IPfcElementFactory is the factory from which the PfcElements are drawn when an
    /// IProcedureFunctionChart is creating an SfcElement such as a node, link or step.
    /// </summary>
    public interface IPfcElementFactory {
        /// <summary>
        /// Creates a step node with the provided characteristics.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcStepNode.</returns>
        IPfcStepNode CreateStepNode(string name, Guid guid, string description);

        /// <summary>
        /// Performs raw instantiation of a new step node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        IPfcStepNode NewStepNode(IProcedureFunctionChart parent, string name, Guid guid, string description);
       
        /// <summary>
        /// Creates a transition node with the provided characteristics.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcTransitionNode.</returns>
        IPfcTransitionNode CreateTransitionNode(string name, Guid guid, string description);
        /// <summary>
        /// Performs raw instantiation of a new transition node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        IPfcTransitionNode NewTransitionNode(IProcedureFunctionChart parent, string name, Guid guid, string description);

        /// <summary>
        /// Creates a link element with the provided characteristics.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcLinkElement.</returns>
        IPfcLinkElement CreateLinkElement(string name, Guid guid, string description);
        /// <summary>
        /// Performs raw instantiation of a new link element.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        IPfcLinkElement NewLinkElement(IProcedureFunctionChart parent, string name, Guid guid, string description);

        /// <summary>
        /// Initializes the specified step node after it has been created.
        /// </summary>
        /// <param name="stepNode">The step node.</param>
        void Initialize(IPfcStepNode stepNode);
        /// <summary>
        /// Initializes the specified transition node after it has been created.
        /// </summary>
        /// <param name="transitionNode">The transition node.</param>
        void Initialize(IPfcTransitionNode transitionNode);
        /// <summary>
        /// Initializes the specified link element after it has been created.
        /// </summary>
        /// <param name="linkElement">The link element.</param>
        void Initialize(IPfcLinkElement linkElement);

        /// <summary>
        /// Called when the loading of a new PFC has been completed.
        /// </summary>
        /// <param name="newPfc">The new PFC.</param>
        void OnPfcLoadCompleted(IProcedureFunctionChart newPfc);

        /// <summary>
        /// Gets the Procedure Function Chart for which this factory is creating elements.
        /// </summary>
        /// <value>The host PFC.</value>
        IProcedureFunctionChart HostPfc { get; set; }

        /// <summary>
        /// Gets the GUID generator in use by this element factory.
        /// </summary>
        /// <value>The GUID generator.</value>
        GuidGenerator GuidGenerator { get; }

       /// <summary>
        /// Returns true if the name of this element conforms to the naming rules that this factory imposes.
        /// </summary>
        /// <param name="element">The element whose name is to be assessed.</param>
        /// <returns><c>true</c> if the name of this element conforms to the naming rules that this factory imposes; otherwise, <c>false</c>.</returns>
        bool IsCanonicallyNamed(IPfcElement element);

        /// <summary>
        /// Causes Step, Transition and Link naming cursors to retract to the sequentially-earliest
        /// name that is not currently assigned in the PFC. That is, if the next transition name to
        /// be assigned was T_044, and the otherwise-highest assigned name was T_025, the transition
        /// naming cursor would retract to T_026. The Step and Link cursors would likewise retract
        /// as a result of this call.
        /// </summary>
        void Retract();
    }

    /// <summary>
    /// IProcedureFunctionChart is implemented by a type that provides overall management, including creation,
    /// running and modification of an SFC graph.
    /// <para></para><b>IMPORTANT NOTE: Any class implementing IProcedureFunctionChart must have a constructor
    /// that accepts a IProcedureFunctionChart, in order for serialization to work properly.</b>
    /// </summary>
    public interface IProcedureFunctionChart : IModelObject, IXmlSerializable, SimCore.ICloneable {

        /// <summary>
        /// Gets or sets the element factory in use by this ProcedureFunctionChart.
        /// </summary>
        /// <value>The element factory.</value>
        IPfcElementFactory ElementFactory { get; set; }

        /// <summary>
        /// Creates a new link. It must later be bound to a predecessor and a successor.
        /// Throws an exception if the Guid is already known to this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        IPfcLinkElement CreateLink();

        /// <summary>
        /// Creates a new link. It must later be bound to a predecessor and a successor.
        /// Throws an exception if the Guid is already known to this ProcedureFunctionChart.
        /// </summary>
        /// <param name="name">The name of the new link.</param>
        /// <param name="guid">The GUID of the new link.</param>
        /// <param name="description">The description of the new link.</param>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        IPfcLinkElement CreateLink(string name, string description, Guid guid);

        /// <summary>
        /// Creates a link with the specified name, guid, predecessor &amp; successor.
        /// </summary>
        /// <param name="name">The name of the new link.</param>
        /// <param name="description">The description of the new link.</param>
        /// <param name="guid">The GUID of the new link.</param>
        /// <param name="predecessor">The predecessor of the new link.</param>
        /// <param name="successor">The successor of the new link.</param>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        IPfcLinkElement CreateLink(string name, string description, Guid guid, IPfcNode predecessor, IPfcNode successor);

        /// <summary>
        /// Creates and adds a step with default information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <returns>The <see cref="T:IPfcStepNode"/>.</returns>
        IPfcStepNode CreateStep();
       
        /// <summary>
        /// Creates and adds a step with the specified information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="description">The description of the step.</param>
        /// <param name="guid">The GUID of the step.</param>
        /// <returns>The <see cref="T:IPfcStepNode"/>.</returns>
        IPfcStepNode CreateStep(string name, string description, Guid guid);

        /// <summary>
        /// Creates and adds a transition with default information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <returns>The <see cref="T:IPfcTransitionNode"/>.</returns>
        IPfcTransitionNode CreateTransition();

        /// <summary>
        /// Creates and adds a transition with the specified information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <param name="name">Name of the transition.</param>
        /// <param name="description">The transition description.</param>
        /// <param name="guid">The transition GUID.</param>
        /// <returns>The <see cref="T:IPfcTransitionNode"/>.</returns>
        IPfcTransitionNode CreateTransition(string name, string description, Guid guid);

        /// <summary>
        /// Binds the specified predecessor to the specified successor.
        /// </summary>
        /// <param name="predecessor">The predecessor.</param>
        /// <param name="successor">The successor.</param>
        void Bind(IPfcNode predecessor, IPfcLinkElement successor);

        /// <summary>
        /// Binds the specified predecessor to the specified successor.
        /// </summary>
        /// <param name="predecessor">The predecessor.</param>
        /// <param name="successor">The successor.</param>
        void Bind(IPfcLinkElement predecessor, IPfcNode successor);
        
        /// <summary>
        /// Binds the two nodes. If both are steps, it inserts a transition between them, and if both are 
        /// transitions, it inserts a step between them - in both cases, creating links between the 'from'
        /// node, the shim node, and the 'to' node. Piggybacking is allowed by default. Use the full-featured
        /// API to disallow piggybacking.
        /// </summary>
        /// <param name="from">The node from which a connection is being established.</param>
        /// <param name="to">The node to which a connection is being established.</param>
        void Bind(IPfcNode from, IPfcNode to);

        /// <summary>
        /// Binds the two linkables. If both are steps, it inserts a transition between them, and if both are
        /// transitions, it inserts a step between them - in both cases, creating links between the 'from'
        /// node, the shim node, and the 'to' node. If piggybacking is allowed, and a suitable path already exists,
        /// we use that path instead. A suitable path is either a link between differently-typed nodes, or a
        /// link-node-link path between same-typed nodes, where the interstitial node is simple, and opposite-typed.
        /// </summary>
        /// <param name="from">The node from which a connection is being established.</param>
        /// <param name="to">The node to which a connection is being established.</param>
        /// <param name="iPfcLink1">The first link element.</param>
        /// <param name="shimNode">The shim node, if one was created.</param>
        /// <param name="iPfcLink2">The second link element, if one was created.</param>
        /// <param name="allowPiggybacking">if set to <c>true</c>, we allow an existing link to serve the purpose of this requested link.</param>
        void Bind(IPfcNode from, IPfcNode to, out IPfcLinkElement iPfcLink1, out IPfcNode shimNode, out IPfcLinkElement iPfcLink2, bool allowPiggybacking);

        /// <summary>
        /// Unbinds the two nodes, removing the link between them. Returns false if they were
        /// not connected directly in the first place. If called directly by the user, this
        /// API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream node of the unbinding.</param>
        /// <param name="to">The downstream node of the unbinding.</param>
        ///<param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns></returns>
        bool Unbind(IPfcNode from, IPfcNode to, bool skipStructureUpdating = false);

                /// <summary>
        /// Unbinds the node from the link. Returns false if they were not
        /// connected directly in the first place. If called directly by
        /// the user, this API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream node of the unbinding.</param>
        /// <param name="to">The downstream link of the unbinding.</param>
        ///<param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns>True, if successful, otherwise, false.</returns>
        bool Unbind(IPfcNode from, IPfcLinkElement to, bool skipStructureUpdating = false);

        /// <summary>
        /// Unbinds the link from the node. Returns false if they were not
        /// connected directly in the first place. If called directly by
        /// the user, this API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream link of the unbinding.</param>
        /// <param name="to">The downstream node of the unbinding.</param>
        /// <param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns>True, if successful, otherwise, false.</returns>
        bool Unbind(IPfcLinkElement from, IPfcNode to, bool skipStructureUpdating = false);


        /// <summary>
        /// Deletes the specified node and its pair (preceding Step if it is a transition,
        /// succeeding transition if it is a step).
        /// <list type="bullet">
        /// <item>If either member of the pair being deleted
        /// has more than one predecessor and one successor, the delete attempt will fail - these
        /// other paths need to be deleted themselves first.</item>
        /// <item>If neither node has multiple inputs
        /// or outputs, then they are both deleted, and a link is added from the transition
        /// preceding the deleted step to the step following the deleted transition.</item>
        /// <item>If the node to be deleted is not connected to anything on either end, then the node is
        /// simply removed from Pfc data structures.</item>
        /// </list> 
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>True if the deletion was successful, false if it was not.</returns>
        bool Delete(IPfcNode node);

        /// <summary>
        /// Binds the inbound elements to the outbound elements through a synchronizer construct. All elements in
        /// both arrays must be the same type (either all Steps or all Transitions), and null or empty arrays are
        /// illegal.
        /// </summary>
        /// <param name="predecessors">The predecessor elements.</param>
        /// <param name="successors">The successor elements.</param>
        void Synchronize(IPfcNode[] predecessors, IPfcNode[] successors);

        /// <summary>
        /// Binds the inbound elements to the outbound elements through a synchronizer construct. Empty collections are illegal.
        /// </summary>
        /// <param name="inbound">The inbound elements.</param>
        /// <param name="outbound">The outbound elements.</param>
        void Synchronize(PfcNodeList inbound, PfcNodeList outbound);

        ///// <summary>
        ///// Initializes the SFC with the specified steps, transitions and links. Any pre-existent nodes are cleared out.
        ///// </summary>
        ///// <param name="steps">The steps.</param>
        ///// <param name="transitions">The transitions.</param>
        ///// <param name="links">The links.</param>
        //void Initialize(PfcStepNodeList steps, PfcTransitionNodeList transitions, PfcLinkElementList links);

        /// <summary>
        /// A directory of participants in and below this Pfc, used in creation of expressions.
        /// </summary>
        ParticipantDirectory ParticipantDirectory { get; }

        /// <summary>
        /// By default, this orders a node's downstream links' priorities and thereby graph ordinals as GOOBER
        /// </summary>
        IComparer<IPfcLinkElement> LinkComparer { get; set; }
        /// <summary>
        /// Gets the parent step node for this SFC.
        /// </summary>
        /// <value>The parent step node.</value>
        IPfcStepNode Parent { get; set; }

        /// <summary>
        /// Gets the source PFC, if any, from which this PFC was cloned.
        /// </summary>
        /// <value>The source.</value>
        IProcedureFunctionChart Source { get; }

        /// <summary>
        /// Gets all of the edges (links) under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The edges (links).</value>
        PfcLinkElementList Edges { get; }

        /// <summary>
        /// Gets all of the edges (links) under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The edges (links).</value>
        PfcLinkElementList Links { get; }

        /// <summary>
        /// Gets the steps under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The steps.</value>
        PfcStepNodeList Steps { get; }

        /// <summary>
        /// Gets the transitions under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The transitions.</value>
        PfcTransitionNodeList Transitions { get; }

        /// <summary>
        /// Gets all of the nodes (steps and transitions)under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The nodes.</value>
        PfcNodeList Nodes { get; }

        /// <summary>
        /// Gets the elements contained directly in this Pfc.
        /// </summary>
        /// <value>The elements.</value>
        List<IPfcElement> Elements { get; }

        /// <summary>
        /// Gets all of the elements that are contained in or under this Pfc, to a depth
        /// specified by the 'depth' parameter, and that pass the 'filter' criteria.
        /// </summary>
        /// <param name="depth">The depth to which retrieval is to be done.</param>
        /// <param name="filter">The filter predicate that dictates which elements are acceptable.</param>
        /// <param name="children">The children, treated as a return value.</param>
        /// <returns></returns>
        void GetChildren(int depth, Predicate<IPfcElement> filter, ref List<IPfcElement> children);

        /// <summary>
        /// This is a performance enhancer - when making internal changes (i.e. changes that are a
        /// part of a larger process such as flattening a Pfc hierarchy), there is no point to doing
        /// node sorting on the entire graph, each time. So, prior to the start of the wholesale
        /// changes, suspend node sorting, and then resume once the changes are complete. Resuming
        /// also results in a call to UpdateStructure(...).
        /// </summary>
        void ResumeNodeSorting();

        /// <summary>
        /// This is a performance enhancer - when making internal changes (i.e. changes that are a
        /// part of a larger process such as flattening a Pfc hierarchy), there is no point to doing
        /// node sorting on the entire graph, each time. So, prior to the start of the wholesale
        /// changes, suspend node sorting, and then resume once the changes are complete. Resuming
        /// also results in a call to UpdateStructure(...).
        /// </summary>
        void SuspendNodeSorting();

        /// <summary>
        /// Updates the structure of the PFC and sorts outbound links per their priority then their textual names, then
        /// their guids. Then does a breadth-first traversal, assigning nodes a sequence number. Finally sorts node lists
        /// per their sequence numbers. Loop breaking then can occur between the node with the higher sequence number and
        /// the *following* node with the lower number. This way, loop-break always occurs at the intuitively-correct place.
        /// </summary>
        /// <param name="breadthFirstOrdinalNumbers">if set to <c>false</c> assigns ordinals in a depth-first order.</param>
        void UpdateStructure(bool breadthFirstOrdinalNumbers = true);

        /// <summary>
        /// Creates an XML string representation of this Pfc.
        /// </summary>
        /// <returns>The newly-created Xml string.</returns>
        string ToXmlString();

        /// <summary>
        /// Finds the node at the specified path from this location. Currently, works only absolutely from this PFC.
        /// <para></para>
        /// </summary>
        /// <param name="path">The path (e.g. ParentName/ChildName).</param>
        IPfcNode FindNode(string path);

        /// <summary>
        /// Finds the first node for which the predicate returns true.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IPfcNode FindFirst(Predicate<IPfcNode> predicate);

        /// <summary>
        /// Retrieves a depth-first iterator over all nodes in this PFC that satisfy the predicate.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        IEnumerable<IPfcNode> FindAll(Predicate<IPfcNode> predicate);

        /// <summary>
        /// Retrieves a depth-first iterator over all nodes in this PFC.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPfcNode> DepthFirstIterator();

        /// <summary>
        /// Transmogrifies this PFC and all of its child PFCs (the actions associated with steps)
        /// into one flat PFC with no children. Steps that had children are replaced by their
        /// children, inserted inline into the parents' PFC structure, in place of the parent.
        /// </summary>
        void Flatten();

        /// <summary>
        /// Gets the start steps in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The start steps.</returns>
        List<IPfcStepNode> GetStartSteps();

        /// <summary>
        /// Gets the finish steps in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The finish steps.</returns>
        List<IPfcStepNode> GetFinishSteps();

        /// <summary>
        /// Gets the finish transition in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The finish transition.</returns>
        IPfcTransitionNode GetFinishTransition();
        /// <summary>
        /// Adds the element to the PFC.
        /// </summary>
        /// <param name="element">The element to be added to the PFC.</param>
        void AddElement(IPfcElement element);

        /// <summary>
        /// Gets a list of NewGuidHolder objects. After obtaining this list, go through it
        /// and for each NewGuidHolder, inspect the target object, determine the new Guid to
        /// be applied, and set it into the newGuidHolder.NewGuid property. After this, the
        /// entire list must be submitted to the ApplyGuidMap(myNewGuidHolderList); API, and
        /// the new guids will be applied.<para>
        /// </para>
        /// <B>Do not simply set the Guids on the objects.</B>
        /// If, after setting a new guid, you want not to change the object's guid, you can
        /// set it to NewGuidHolder.NO_CHANGE, a special guid that causes the engine to skip
        /// that object in the remapping of guids.
        /// </summary>
        /// <param name="deep">If true, steps' Action Pfc's will return their elements' guids, too.</param>
        /// <returns>A list of NewGuidHolder objects associated with the IPfcElements in this Pfc.</returns>
        List<ProcedureFunctionChart.NewGuidHolder> GetCleanGuidMap(bool deep);

        /// <summary>
        /// Applies the GUID map.
        /// </summary>
        /// <param name="newGuidHolders">The list of NewGuidHolders that serves as a new GUID map.</param>
        void ApplyGuidMap(List<ProcedureFunctionChart.NewGuidHolder> newGuidHolders);

        /// <summary>
        /// Recursively collapses childrens' participant directories into the parent, renaming the
        /// absorbed child elements and Steps as necessary. Only the rootChart's ParticipantDirectory
        /// is left in existence. All others point up to the root.
        /// </summary>
        /// <param name="rootChart">The root chart.</param>
        void CollapseParticipantDirectories(IProcedureFunctionChart rootChart);

        /// <summary>
        /// Reduces this procedure function chart, applying reduction rules until the PFC is no longer reduceable.
        /// </summary>
        void Reduce();

        /// <summary>
        /// Looks forward from the node for nodes on path ending at the finish node.
        /// </summary>
        /// <param name="finish">The finish.</param>
        /// <param name="node">The node.</param>
        /// <param name="deletees">The deletees.</param>
        /// <returns></returns>
        bool LookForwardForNodesOnPathEndingAt(IPfcNode finish, IPfcNode node, ref List<IPfcNode> deletees);


        /// <summary>
        /// Applies the naming cosmetics appropriate for the type of recipe being generated. This is currently
        /// hard-coded, and performs naming of transitions to T_001, T_002, ... T_00n, and null steps to 
        /// NULL_UP:0, NULL_UP:1, ... NULL_UP:n.
        /// </summary>
        void ApplyNamingCosmetics();


        void Prune(Func<IPfcStepNode, bool> keepThisStep);

        /// <summary>
        /// Runs the PFC under control of the specified executive.
        /// </summary>
        /// <param name="exec">The exec.</param>
        /// <param name="userData">The user data.</param>
        void Run(IExecutive exec, object userData);

        DateTime? EarliestStart { get; set; }

        void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm);

        PfcAction Precondition { get; set; }

        /// <summary>
        /// Occurs when PFC start requested, but before permission has been obtained to do so.
        /// </summary>
        event PfcAction PfcStartRequested;

        /// <summary>
        /// Occurs when PFC is starting.
        /// </summary>
        event PfcAction PfcStarting;

        /// <summary>
        /// Occurs when PFC is completing.
        /// </summary>
        event PfcAction PfcCompleting;

        event StepStateMachineEvent StepStateChanged;

        event TransitionStateMachineEvent TransitionStateChanged;

    }

    public delegate bool ReductionRule(IProcedureFunctionChart pfc); // Modifies in-place.

    /// <summary>
    /// Implemented by any entity (Links, Steps and Transitions) that participates
    /// in the structure of a ProcedureFunctionChart.
    /// </summary>
    public interface IPfcElement : IModelObject, IResettable {

        /// <summary>
        /// Sets (re-sets) the name of this element.
        /// </summary>
        /// <param name="newName">The new name.</param>
        void SetName(string newName);

        /// <summary>
        /// Gets the type of the element.
        /// </summary>
        /// <value>The type of the element.</value>
        PfcElementType ElementType { get; }

        /// <summary>
        /// The parent ProcedureFunctionChart of this node.
        /// </summary>
        IProcedureFunctionChart Parent { get; set; }

        /// <summary>
        /// Gets or sets some piece of arbitrary user data. This data is (currently) not serialized.
        /// </summary>
        /// <value>The user data.</value>
        object UserData { get; set; }

        /// <summary>
        /// Updates the portion of the structure of the SFC that relates to this element.
        /// This is called after any structural changes in the Sfc, but before the resultant data
        /// are requested externally.
        /// </summary>
        void UpdateStructure();

        /// <summary>
        /// Determines whether this instance is connected to anything upstream or downstream.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </returns>
        bool IsConnected();

        /// <summary>
        /// Gets the SEID, or Source Element ID of this element. If the PFC of which 
        /// this element is a member is cloned, then this SEID will be the Guid of the element
        /// in the source PFC that is semantically/structurally equivalent to this one.
        /// </summary>
        /// <value>The SEID.</value>
        Guid SEID { get; }

    }

    /// <summary>
    /// Implemented by an object that is an SFC SfcLink.
    /// </summary>
    public interface IPfcLinkElement : IPfcElement {
       
        /// <summary>
        /// Gets the predecessor IPfcNode to this Link node.
        /// </summary>
        /// <value>The predecessor.</value>
        IPfcNode Predecessor { get; }
       
        /// <summary>
        /// Gets the successor IPfcNode to this Link node.
        /// </summary>
        /// <value>The successor.</value>
        IPfcNode Successor { get; }

        /// <summary>
        /// Gets or sets the priority of this link. The higher the number representing a 
        /// link among its peers, the higher priority it has. The highest-priority link is said
        /// to define the 'primary' path through the graph. Default priority is 0.
        /// </summary>
        /// <value>The priority of the link.</value>
        int? Priority { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this link creates a loopback along one or more paths.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a loopback; otherwise, <c>false</c>.
        /// </value>
        bool IsLoopback { get; set; }

        /// <summary>
        /// Detaches this link from its predecessor and successor.
        /// </summary>
        void Detach();

        /// <summary>
        /// A PfcLink is a part of one of these types of aggregate links, depending on the type of its predecessor
        /// or successor, and the number of (a) successors its predecessor has, and (b) predecessors its successor has.
        /// </summary>
        AggregateLinkType AggregateLinkType { get; }

    }

    /// <summary>
    /// Implemented by anything that can be a node in an SFC graph. This includes steps and transitions.
    /// Nodes are connected to Links as their predecessor &amp; successors.
    /// Links such as ParallelConvergentLinks and SeriesDivergentLinks have multiple predecessors or
    /// successors, and their logic to fire or not is dependent upon input steps' or transitions' states.
    /// </summary>
    public interface IPfcNode : IPfcElement {

        /// <summary>
        /// Gets or sets a value indicating whether the structure of this SFC is dirty (in effect, whether it has changed since
        /// consolidation was last done.
        /// </summary>
        /// <value><c>true</c> if [structure dirty]; otherwise, <c>false</c>.</value>
        bool StructureDirty { get; set; }

        /// <summary>
        /// Gets the predecessor list for this node.
        /// </summary>
        /// <value>A list of the predecessor links.</value>
        PfcLinkElementList Predecessors { get; }

        /// <summary>
        /// Gets the predecessor nodes list for this node. The list contains all nodes at the other end of links that are
        /// predecessors of this node.
        /// </summary>
        /// <value>A list of the predecessor nodes.</value>
        PfcNodeList PredecessorNodes { get; }
        
        /// <summary>
        /// Adds the new predecessor link to this node's list of predecessors.
        /// </summary>
        /// <param name="newPredecessor">The new predecessor link.</param>
        void AddPredecessor(IPfcLinkElement newPredecessor);

        /// <summary>
        /// Removes the predecessor link from this node's list of predecessors.
        /// </summary>
        /// <param name="currentPredecessor">The current predecessor.</param>
        /// <returns></returns>
        bool RemovePredecessor(IPfcLinkElement currentPredecessor);

        /// <summary>
        /// Gets the successor list for this node.
        /// </summary>
        /// <value>A list of the successor links.</value>
        PfcLinkElementList Successors { get; }

        /// <summary>
        /// Gets the successor nodes list for this node. The list contains all nodes at the other end of links that are
        /// successors of this node.
        /// </summary>
        /// <value>A list of the successor nodes.</value>
        PfcNodeList SuccessorNodes { get; }

        /// <summary>
        /// Adds the new successor link to this node's list of successors.
        /// </summary>
        /// <param name="newSuccessor">The new successor link.</param>
        void AddSuccessor(IPfcLinkElement newSuccessor);

        /// <summary>
        /// Removes the successor link from this node's list of successors.
        /// </summary>
        /// <param name="currentSuccessor">The current successor.</param>
        /// <returns></returns>
        bool RemoveSuccessor(IPfcLinkElement currentSuccessor);


        /// <summary>
        /// Gets the link that connects this node to a successor node. Returns null if there is no such link.
        /// </summary>
        /// <param name="successorNode">The successor node.</param>
        /// <returns></returns>
        IPfcLinkElement GetLinkForSuccessorNode(IPfcNode successorNode);

        /// <summary>
        /// Gets the link that connects this node to a predecessor node. Returns null if there is no such link.
        /// </summary>
        /// <param name="predecessorNode">The predecessor node.</param>
        /// <returns></returns>
        IPfcLinkElement GetLinkForPredecessorNode(IPfcNode predecessorNode);

        /// <summary>
        /// Gives the specified link (which must be one of the outbound links from this node) the highest 
        /// priority of all links outbound from this node. Retuens false if the specified link is not a 
        /// successor link to this node. NOTE: This API will renumber the outbound links' priorities.
        /// </summary>
        /// <param name="outbound">The link, already in existence and an outbound link from this node, that 
        /// is to be set to the highest priority of all links already outbound from this node.</param>
        /// <returns></returns>
        bool SetLinkHighestPriority(IPfcLinkElement outbound);

        /// <summary>
        /// Gives the specified link (which must be one of the outbound links from this node) the lowest 
        /// priority of all links outbound from this node. Retuens false if the specified link is not a 
        /// successor link to this node. NOTE: This API will renumber the outbound links' priorities.
        /// </summary>
        /// <param name="outbound">The link, already in existence and an outbound link from this node, that 
        /// is to be set to the lowest priority of all links already outbound from this node.</param>
        /// <returns></returns>
        bool SetLinkLowestPriority(IPfcLinkElement outbound);

        /// <summary>
        /// Gets or sets the graph ordinal of this node - a number that roughly (but consistently)
        /// represents its place in the execution order for this graph. Loopbacks' ordinals indicate
        /// their place in the execution order as of their first execution.
        /// </summary>
        /// <value>The graph ordinal.</value>
        int GraphOrdinal { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is simple. A node is simple if it 
        /// has one input and one output and performs no tasks beyond a pass-through.
        /// </summary>
        /// <value><c>true</c> if this instance is simple; otherwise, <c>false</c>.</value>
        bool IsSimple { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is null. A node that is null can be
        /// eliminated when PFCs are combined.
        /// </summary>
        /// <value><c>true</c> if this instance is null; otherwise, <c>false</c>.</value>
        bool IsNullNode { get; set; }

        /// <summary>
        /// Used by a variety of graph analysis algorithms.
        /// </summary>
        NodeColor NodeColor { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a start node.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a start node; otherwise, <c>false</c>.
        /// </value>
        bool IsStartNode { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a finish node.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a finish node; otherwise, <c>false</c>.
        /// </value>
        bool IsFinishNode { get; }

        /// <summary>
        /// A string dictionary containing name/value pairs that represent graphics &amp; layout-related values.
        /// </summary>
        Dictionary<string, string> GraphicsData { get; }

    }

    /// <summary>
    /// The signature of a method that a PFC Step can call as an action.
    /// </summary>
    /// <param name="pfcec">A PfcExecutionContext containing the parameters to be used by this call.</param>
    /// <param name="ssm">The state machine controlling state of the step to which this action belongs.</param>
    public delegate void PfcAction(PfcExecutionContext pfcec, StepStateMachine ssm);

    /// <summary>
    /// Implemented by an object that is an SFC SfcStep.
    /// </summary>
    public interface IPfcStepNode : IPfcNode {

        /// <summary>
        /// Finds the child node, if any, at the specified path relative to this node.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        IPfcElement Find(string path);

        /// <summary>
        /// Gets all of the elements that are contained in or under this Pfc, to a depth
        /// specified by the 'depth' parameter, and that pass the 'filter' criteria.
        /// </summary>
        /// <param name="depth">The depth to which retrieval is to be done.</param>
        /// <param name="filter">The filter predicate that dictates which elements are acceptable.</param>
        /// <param name="children">The children, treated as a return value.</param>
        /// <returns></returns>
        void GetChildren(int depth, Predicate<IPfcElement> filter, ref List<IPfcElement> children);

        /// <summary>
        /// Gets the actions associated with this PFC Step. They are keyed by ActionName, and are themselves, PFCs.
        /// </summary>
        /// <value>The actions.</value>
        Dictionary<string, IProcedureFunctionChart> Actions { get; }

        /// <summary>
        /// Adds a child Pfc into the actions list under this step.
        /// </summary>
        /// <param name="actionName">The name of this action.</param>
        /// <param name="pfc">The Pfc that contains procedural details of this action.</param>
        void AddAction(string actionName, IProcedureFunctionChart pfc);

        /// <summary>
        /// The executable action that will be performed if there are no PFCs under this step. By default, it will
        /// run the child Action PFCs in parallel, if there are any, and will return immediately if there are not.
        /// </summary>
        PfcAction LeafLevelAction{ get; set ; }

        /// <summary>
        /// Sets the Actor that will determine the behavior behind this step. The actor provides the leaf level
        /// action, as well as preconditiond for running.
        /// </summary>
        /// <param name="actor">The actor that will provide the behaviors.</param>
        void SetActor(PfcActor actor);

        /// <summary>
        /// Gets the step state machine associated with this PFC step.
        /// </summary>
        /// <value>My step state machine.</value>
        StepStateMachine MyStepStateMachine { get;}

        /// <summary>
        /// Returns the actions under this Step as a procedure function chart.
        /// </summary>
        /// <returns></returns>
        ProcedureFunctionChart ToProcedureFunctionChart();

        /// <summary>
        /// Gets key data on the unit with which this step is associated. Note that a step, such as a
        /// recipe start step, or one added without such data, may not hold any unit data at all. In this case,
        /// the UnitInfo property will be null.
        /// </summary>
        /// <value>The unit info.</value>
        IPfcUnitInfo UnitInfo { get; set; }

        /// <summary>
        /// Returns the Guid of the element in the source recipe that is represented by this PfcStep.
        /// </summary>
        Guid RecipeSourceGuid { get; }

        /// <summary>
        /// Gets or sets the earliest time that this element can start.
        /// </summary>
        /// <value>The earliest start.</value>
        DateTime? EarliestStart { get; set; }

        /// <summary>
        /// Gets permission from the step to start.
        /// </summary>
        /// <param name="myPfcec">My pfcec.</param>
        /// <param name="ssm">The StepStateMachine that will govern this run.</param>
        void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm);

        /// <summary>
        /// Gets or sets the precondition under which this step is permitted to start. If null, permission is assumed.
        /// </summary>
        /// <value>The precondition.</value>
        PfcAction Precondition { get; set; }

    }

    /// <summary>
    /// Holds key data on the unit with which a specific step is associated.
    /// </summary>
    public interface IPfcUnitInfo {
        /// <summary>
        /// The name of the unit with which a step is associated.
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// The sequence number of the unit with which a step is associated.
        /// </summary>
        int SequenceNumber { get; set; }
    }

    /// <summary>
    /// Implemented by an object that is an SfcTransition.
    /// </summary>
    public interface IPfcTransitionNode : IPfcNode {

        /// <summary>
        /// Gets the expression that is attached to this transition node.
        /// </summary>
        /// <value>The expression.</value>
        Expression Expression { get; }

        /// <summary>
        /// Gets or sets the 'user-friendly' value of this expression. Uses step names and macro names.
        /// </summary>
        /// <value>The expression value.</value>
        string ExpressionUFValue { get; set;}

        /// <summary>
        /// Gets or sets the 'user-hostile' value of this expression. Uses guids in place of names.
        /// </summary>
        /// <value>The expression value.</value>
        string ExpressionUHValue { get; set;}

        /// <summary>
        /// Gets the expanded value of this expression. Uses step names and expands macro names into their resultant names.
        /// </summary>
        /// <value>The expression expanded.</value>
        string ExpressionExpandedValue { get; }

        /// <summary>
        /// Gets or sets the default executable condition, that is the executable condition that this transition will
        /// evaluate unless overridden in the execution manager.
        /// </summary>
        /// <value>The default executable condition.</value>
        ExecutableCondition ExpressionExecutable { get; set; }

        /// <summary>
        /// Gets the transition state machine associated with this PFC transition.
        /// </summary>
        /// <value>My step state machine.</value>
        TransitionStateMachine MyTransitionStateMachine { get; }

    }

    /// <summary>
    /// A PfcLink is a part of one of these types of aggregate links, depending on the type of its predecessor
    /// or successor, and the number of (a) successors its predecessor has, and (b) predecessors its successor has.
    /// </summary>
    public enum AggregateLinkType { 
        Unknown, 
        Simple, 
        ParallelConvergent, 
        SeriesConvergent, 
        ParallelDivergent, 
        SeriesDivergent 
    }

    /// <summary>
    /// Declares whether a port is an input port or an output port.
    /// </summary>
    public enum PfcPortDirection {
        Input, 
        Output
    }

    /// <summary>
    /// Implemented by an object that filters SFC Nodes.
    /// </summary>
    public interface IPfcNodeFilter {
        /// <summary>
        /// Determines whether the specified element is acceptable to be used by whomever is employing the filter.
        /// </summary>
        /// <param name="element">The element under consideration.</param>
        /// <returns>
        /// 	<c>true</c> if the specified element is acceptable; otherwise, <c>false</c>.
        /// </returns>
        bool IsAcceptable(IPfcElement element);
    }

    /// <summary>
    /// An interface implemented by anything in an SFC that is to be evaluated as an expression.
    /// </summary>
    /// <typeparam name="T">The return type of the expression.</typeparam>
    public interface IPfcExpression<T> {
        T Evaluate();
        T Evaluate(IDictionary parameters);
    }

    /// <summary>
    /// An interface implemented by anything in an SFC that is to be evaluated as an expression that returns a boolean.
    /// </summary>
    public interface IPfcBooleanExpression : IPfcExpression<bool> {
        /// <summary>
        /// Gets the left hand side of the boolean expression.
        /// </summary>
        /// <value>The left hand side of the boolean expression..</value>
        string Lhs { get; }
        /// <summary>
        /// Gets the right hand side of the boolean expression.
        /// </summary>
        /// <value>The right hand side of the boolean expression..</value>
        string Rhs { get; }

    }

    /// <summary>
    /// The types of operations permitted by this operation type.
    /// </summary>
    public enum OperationType {
        /// <summary>
        /// True if the LHS equals the RHS.
        /// </summary>
        Equal,
        /// <summary>
        /// True if the LHS does not equal the RHS.
        /// </summary>
        NotEqual,
        /// <summary>
        /// True if the LHS is an element of the RHS.
        /// </summary>
        In,
        /// <summary>
        /// True if the LHS is not an element of the RHS.
        /// </summary>
        NotIn,
        /// <summary>
        /// True if Mike tells me what it means :-)
        /// </summary>
        Exists,
        /// <summary>
        /// True if Mike tells me what it means :-)
        /// </summary>
        NotExists,
        /// <summary>
        /// True if the LHS is greater than the RHS.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// True if the LHS is greater than or equal to the RHS.
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// True if the LHS is less than the RHS.
        /// </summary>
        LessThan,
        /// <summary>
        /// True if the LHS is less than or equal to the RHS.
        /// </summary>
        LessThanOrEqual
    }
}
