/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Highpoint.Sage.Utility;
using System.Collections;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs.PFC
{

    public class PfcValidator
    {

        #region Private fields

        //private static readonly bool s_use_Legacy_Validation = false;
        public static bool m_diagnostics = Highpoint.Sage.Diagnostics.DiagnosticAids.Diagnostics("PfcValidator");
        private IProcedureFunctionChart m_pfc;
        private bool? m_pfcIsValid = null;
        private Queue<QueueData> m_activePath;
        private ValidationToken m_root;
        private bool[,] m_dependencies;
        private List<PfcValidationError> m_errorList = null;
        private int m_maxGraphOrdinal = 0;

        #endregion

        public PfcValidator(IProcedureFunctionChart pfc)
        {
            m_errorList = new List<PfcValidationError>();
            m_pfc = (IProcedureFunctionChart) pfc.Clone();
            Reduce();
            try
            {
                m_pfc.UpdateStructure();
                Validate();
            }
            catch (PFCValidityException)
            {
                m_pfcIsValid = false;
            }
        }

        private void Reduce()
        {
            int count = 0;
            bool success = false;
            do
            {
                success = false;
                foreach (IPfcNode node in m_pfc.Nodes)
                {
                    if (
                        node.PredecessorNodes.Count == 1 &&
                        node.SuccessorNodes.Count == 1 &&
                        node.SuccessorNodes[0].PredecessorNodes.Count == 1 &&
                        node.SuccessorNodes[0].SuccessorNodes.Count == 1 && (
                            node.PredecessorNodes[0].SuccessorNodes.Count == 1 ||
                            node.SuccessorNodes[0].SuccessorNodes[0].PredecessorNodes.Count == 1)
                        )
                    {
                        IPfcNode target1 = node;
                        IPfcNode target2 = node.SuccessorNodes[0];
                        IPfcNode from = node.PredecessorNodes[0];
                        IPfcNode to = node.SuccessorNodes[0].SuccessorNodes[0];
                        m_pfc.Bind(from, to);
                        target1.Predecessors[0].Detach();
                        target1.Successors[0].Detach();
                        target2.Successors[0].Detach();
                        success = true;
                        count += 2;
                    }
                }
            } while (success);
        }

        /// <summary>
        /// Validates the PFC in this instance, and sets (MUST SET) the m_pfcIsValid to true or false.
        /// </summary>
        private void Validate()
        {

            try
            {
                m_activePath = new Queue<QueueData>();

                foreach (IPfcNode node in m_pfc.Nodes)
                    NodeValidationData.Attach(node);
                foreach (IPfcLinkElement link in m_pfc.Links)
                    LinkValidationData.Attach(link);
                BuildDependencies();
                Propagate();
                Check();

                DateTime finish = DateTime.Now;
                foreach (IPfcNode node in m_pfc.Nodes)
                    NodeValidationData.Detach(node);
                foreach (IPfcLinkElement link in m_pfc.Links)
                    LinkValidationData.Detach(link);

            }
            catch (Exception e)
            {
                m_errorList.Add(new PfcValidationError("Exception while validating.", e.Message, null));
                m_pfcIsValid = false;
            }
        }

        public bool PfcIsValid()
        {
            if (!m_pfcIsValid.HasValue)
                Validate();
            return m_pfcIsValid.Value;
        }

        #region Dependency mechanism

        private void BuildDependencies()
        {
            m_maxGraphOrdinal = m_pfc.Nodes.Max(n => n.GraphOrdinal);
            BuildDependencies(m_pfc.GetStartSteps()[0], new Stack<IPfcLinkElement>());

            m_dependencies = new bool[m_maxGraphOrdinal + 1, m_maxGraphOrdinal + 1];
            foreach (IPfcLinkElement link in m_pfc.Links)
            {
                int independent = link.Predecessor.GraphOrdinal;
                bool[] dependents = GetValidationData(link).NodesBelow;
                for (int dependentNum = 0; dependentNum < m_maxGraphOrdinal + 1; dependentNum++)
                {
                    m_dependencies[independent, dependentNum] |= dependents[dependentNum];
                }
            }
        }

        private void BuildDependencies(IPfcNode node, Stack<IPfcLinkElement> stack)
        {

            foreach (IPfcLinkElement outbound in node.Successors)
            {
                if (!stack.Contains(outbound))
                {
                    LinkValidationData lvd = GetValidationData(outbound);
                    if (lvd.NodesBelow == null)
                    {
                        stack.Push(outbound);
                        BuildDependencies(outbound.Successor, stack);
                        stack.Pop();

                        lvd.NodesBelow = new bool[m_maxGraphOrdinal + 1];
                        lvd.NodesBelow[outbound.Successor.GraphOrdinal] = true;
                        foreach (IPfcLinkElement succOutboundLink in outbound.Successor.Successors)
                        {
                            LinkValidationData lvSuccLink = GetValidationData(succOutboundLink);
                            if (lvSuccLink.NodesBelow != null)
                            {
                                for (int i = 0; i < m_maxGraphOrdinal + 1; i++)
                                {
                                    lvd.NodesBelow[i] |= lvSuccLink.NodesBelow[i];
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        private bool Node1DependsOnNode2(IPfcNode node1, IPfcNode node2)
        {
            return m_dependencies[node2.GraphOrdinal, node1.GraphOrdinal];
        }

        private void Propagate()
        {

            if (m_pfc.GetStartSteps().Count() != 1)
            {
                m_pfcIsValid = false;
                return;
            }

            #region Initialize the processing queue.

            IPfcNode start = m_pfc.GetStartSteps()[0];
            m_root = new ValidationToken(start);
            GetValidationData(start).ValidationToken = m_root;
            if (m_diagnostics)
                Console.WriteLine("Enqueueing {0} with token {1}.", start.Name, GetValidationData(start).ValidationToken);
            m_activePath.Enqueue(new QueueData(null, start));

            #endregion

            do
            {
                // Now process the queue.
                while (m_activePath.Count() > 0)
                {
                    IPfcNode node = Dequeue();
                    GetValidationData(node).DequeueCount++;

                    if (m_diagnostics)
                        Console.WriteLine("Dequeueing {0} with token {1}.", node.Name,
                            GetValidationData(node).ValidationToken);

                    bool continueProcessing = true;
                    switch (GetValidationData(node).InputRole)
                    {
                        case InputRole.ParallelConvergence:
                            continueProcessing = ProcessParallelConvergence(node);
                            break;
                        case InputRole.SerialConvergence:
                            continueProcessing = ProcessSerialConvergence(node);
                            break;
                        case InputRole.PassIn:
                            break;
                        case InputRole.StartNode:
                            break;
                        default:
                            break;
                    }

                    if (continueProcessing)
                    {
                        switch (GetValidationData(node).OutputRole)
                        {
                            case OutputRole.ParallelDivergence:
                                ProcessParallelDivergence(node);
                                break;
                            case OutputRole.SerialDivergence:
                                ProcessSerialDivergence(node);
                                break;
                            case OutputRole.PassOut:
                                ProcessPassthrough(node);
                                break;
                            case OutputRole.TerminalNode:
                                ProcessTerminalNode(node);
                                break;
                            default:
                                break;
                        }
                    }
                    GetValidationData(node).NodeHasRun = true;
                }

            } while (m_activePath.Any());
        }

        private bool ProcessSerialConvergence(IPfcNode node)
        {
            if (GetValidationData(node).NodeHasRun)
            {
                GetValidationData(node).ValidationToken.DecrementAlternatePathsOpen();

                // TODO: Check that converging tokens have the same parent.

                if (m_diagnostics)
                    Console.WriteLine("\tNot processing {0} further - we've already traversed.", node.Name);
                return false;
            }

            // if the node hasn't run, don't decrement alt-open-paths, since we'll propagate this leg.
            return true;
        }

        private bool ProcessParallelConvergence(IPfcNode node)
        {
            // Only run it if it's the last encounter of a parallel convergence.
            if (GetValidationData(node).DequeueCount == node.PredecessorNodes.Count())
            {
                if (m_diagnostics)
                    Console.WriteLine("\tProcessing closure of {0}.", node.Name);

                // To test parallel convergence into a target node, find the divergence node, and then
                // from that point, all parallel, and at least one of every set of serially divergent
                // paths, must contain the target node. If not all of the serially-divergent paths does,
                // then we will catch that in the serial convergence handler.
                UpdateClosureToken(node as IPfcTransitionNode);
                return true;
            }
            else
            {
                if (m_diagnostics)
                    Console.WriteLine("\tNot processing {0} further - we'll encounter it again.", node.Name);
                return false;
            }
        }

        private void ProcessParallelDivergence(IPfcNode node)
        {
            ValidationToken nodeVt = GetValidationData(node).ValidationToken;
            foreach (IPfcNode successor in node.SuccessorNodes)
            {
                ValidationToken successorVtVt = new ValidationToken(successor);
                nodeVt.AddChild(successorVtVt);

                if (m_diagnostics)
                    Console.WriteLine("\tCreated {0} as child for {1}.", successorVtVt.Name, nodeVt.Name);

                Enqueue(node, successor, successorVtVt);
            }
            nodeVt.DecrementAlternatePathsOpen(); // I've been replaced by my childrens' collective path.
        }

        private void ProcessSerialDivergence(IPfcNode node)
        {
            ValidationToken nodeVt = GetValidationData(node).ValidationToken;
            foreach (IPfcNode successor in node.SuccessorNodes)
            {
                nodeVt.IncrementAlternatePathsOpen();
                Enqueue(node, successor, nodeVt);
            }
            nodeVt.DecrementAlternatePathsOpen();
            //added 1 per child, subtract 1 overall. Thus 1 split to 4 yields 4 alt.
        }

        private void ProcessPassthrough(IPfcNode node)
        {
            Enqueue(node, node.SuccessorNodes[0], GetValidationData(node).ValidationToken);
        }

        private void ProcessTerminalNode(IPfcNode node)
        {
            GetValidationData(node).ValidationToken.DecrementAlternatePathsOpen();
        }

        private void Enqueue(IPfcNode from, IPfcNode node, ValidationToken vt)
        {
            NodeValidationData vd = GetValidationData(node);
            if (vd.InputRole == InputRole.ParallelConvergence)
            {
                GetValidationData(from).ValidationToken.DecrementAlternatePathsOpen();
            }
            vd.ValidationToken = vt;
            m_activePath.Enqueue(new QueueData(from, node));
            if (m_diagnostics)
                Console.WriteLine("\tEnqueueing {0} with {1} ({2}).", node.Name, vt, vt.AlternatePathsOpen);
        }

        /// <summary>
        /// Dequeues and sets up for evaluation, the active path.
        /// </summary>
        /// <returns>IPfcNode.</returns>
        private IPfcNode Dequeue()
        {
            List<QueueData> tmp = new List<QueueData>(m_activePath);
            tmp.Sort(OnProcessingSequence);

            m_activePath.Clear();
            tmp.ForEach(n => m_activePath.Enqueue(n));
            QueueData qd = m_activePath.Dequeue();
            IPfcNode retval = qd.To;

            return retval;
        }

        private void UpdateClosureToken(IPfcTransitionNode closureTransition)
        {

            // Find the youngest common ancestor to all gazinta tokens.
            IPfcNode yca = DivergenceNodeFor(closureTransition);

            bool completeParallelConverge = AllParallelAndAtLeastOneOfEachSetOfSerialPathsContain(yca, closureTransition);

            ValidationToken replacementToken =
                completeParallelConverge ?                                                // Are all of the root node's outbound
                                                                                          //    paths closed by the closure node?
                GetValidationData(yca).ValidationToken :                                  // If so, its token is the closure token.
                GetValidationData(closureTransition.PredecessorNodes[0]).ValidationToken; // If not, pick one of the gazinta tokens.

            replacementToken.IncrementAlternatePathsOpen();
            GetValidationData(closureTransition).ValidationToken = replacementToken;

            if (m_diagnostics)
                Console.WriteLine("\t\tAssigning {0} ({1}) to {2} on its closure.", replacementToken.Name,
                    replacementToken.AlternatePathsOpen, closureTransition.Name);
        }

        private bool AllParallelAndAtLeastOneOfEachSetOfSerialPathsContain(IPfcNode from, IPfcNode target)
        {

            NodeValidationData nvd = GetValidationData(from);
            if (nvd.IsInPath == null)
            {
                if (!from.SuccessorNodes.Any())
                {
                    nvd.IsInPath = false;
                }
                else if (from == target)
                {
                    nvd.IsInPath = true;
                }
                else if (from.SuccessorNodes.Count == 1)
                {
                    return AllParallelAndAtLeastOneOfEachSetOfSerialPathsContain(from.SuccessorNodes[0], target);
                }
                else // It's a divergence.
                {
                    if (from is IPfcStepNode)
                    { // serial divergence.
                        bool retval = false;
                        foreach (IPfcNode successorNode in from.SuccessorNodes)
                        {
                            retval |= AllParallelAndAtLeastOneOfEachSetOfSerialPathsContain(successorNode, target);
                        }
                        return retval;
                    }
                    else
                    { // parallel divergence.
                        bool retval = true;
                        foreach (IPfcNode successorNode in from.SuccessorNodes)
                        {
                            retval &= AllParallelAndAtLeastOneOfEachSetOfSerialPathsContain(successorNode, target);
                        }
                        return retval;
                    }
                }
            }
            return nvd.IsInPath.Value;
        }

        #region Closure Token Mechanism

        private ValidationToken ClosureToken(IPfcNode targetNodeToBeClosed)
        {

            // Find the youngest common ancestor to all gazinta tokens.
            IPfcNode yca = DivergenceNodeFor(targetNodeToBeClosed);

            // Are all of the root node's outbound paths closed by the closure node?
            IPfcNode target = targetNodeToBeClosed;

            bool allDivergencesConverge = AllForwardPathsContain(yca, target);

            // If so, its token is the closure token.
            // If not, pick one of the gazinta tokens.
            if (allDivergencesConverge)
            {
                return GetValidationData(yca).ValidationToken;
            }
            else
            {
                return GetValidationData(targetNodeToBeClosed.PredecessorNodes[0]).ValidationToken;
            }
        }

        private bool AllForwardPathsContain(IPfcNode from, IPfcNode target)
        {
            m_pfc.Nodes.ForEach(n => GetValidationData(n).IsInPath = null);
            Stack<IPfcNode> path = new Stack<IPfcNode>();
            path.Push(from);
            return AllForwardPathsContain(from, target, path);
        }

        private bool AllForwardPathsContain(IPfcNode from, IPfcNode target, Stack<IPfcNode> path)
        {
            NodeValidationData nvd = GetValidationData(from);
            if (nvd.IsInPath == null)
            {
                if (from.SuccessorNodes.Count() == 0)
                {
                    nvd.IsInPath = false;
                }
                else if (from == target)
                {
                    nvd.IsInPath = true;
                }
                else
                {
                    nvd.IsInPath = true;
                    foreach (IPfcNode succ in from.SuccessorNodes)
                    {
                        if (!AllForwardPathsContain(succ, target, path))
                        {
                            nvd.IsInPath = false;
                            break;
                        }
                    }
                }
            }
            return nvd.IsInPath.Value;
        }

        /// <summary>
        /// Determines whether all backward paths from 'from' contain the node 'target.' If they do,
        /// and it is the first such encounter for a specific 'from' then it may be said that target
        /// is the divergence node for 'from.'
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        private bool AllBackwardPathsContain(IPfcNode from, IPfcNode target)
        {
            NodeValidationData nvd = GetValidationData(from);
            if (nvd.IsInPath == null)
            {
                if (!from.PredecessorNodes.Any())
                {
                    nvd.IsInPath = false;
                }
                else if (from == target)
                {
                    nvd.IsInPath = true;
                }
                else
                {
                    nvd.IsInPath = true;
                    foreach (IPfcNode predecessorNode in from.PredecessorNodes)
                    {
                        if (!AllBackwardPathsContain(predecessorNode, target))
                        {
                            nvd.IsInPath = false;
                            break;
                        }
                    }
                }
            }
            return nvd.IsInPath.Value;
        }

        //private bool AllBackwardPathsContain(IPfcNode from, IPfcNode target, Stack<IPfcNode> path) {
        //    if (from.PredecessorNodes.Count() == 0) {
        //        return false;
        //    } else if (from == target) {
        //        return true;
        //    } else {
        //        foreach (IPfcNode pred in from.PredecessorNodes) {
        //            if (!path.Contains(pred)) {
        //                path.Push(pred);
        //                if (!AllBackwardPathsContain(pred, target, path))
        //                    return false;
        //                path.Pop();
        //            }
        //        }
        //        return true;
        //    }
        //}

        private IPfcNode DivergenceNodeFor(IPfcNode closure)
        {

            NodeValidationData nvd = GetValidationData(closure);
            //if (nvd.DivergenceNode != null)
            //    return nvd.DivergenceNode;

            List<IPfcNode> possibles = new List<IPfcNode>();
            foreach (IPfcTransitionNode trans in m_pfc.Transitions)
            {
                if (Node1DependsOnNode2(closure, trans))
                {
                    possibles.Add(trans);
                }
            }
            possibles.Sort(new PfcNode.NodeComparer());
            possibles.Reverse();

            foreach (IPfcNode possible in possibles)
            {
                m_pfc.Nodes.ForEach(n => GetValidationData(n).IsInPath = null);
                if (AllBackwardPathsContain(closure, possible))
                {
                    return possible;
                }
            }
            return null;
        }

        #endregion Closure Token Mechanism

        /// <summary>
        /// </summary>
        /// <param name="q1">The q1.</param>
        /// <param name="q2">The q2.</param>
        /// <returns></returns>
        private int OnProcessingSequence(QueueData q1, QueueData q2)
        {
            return OnProcessingSequence(q1.To, q2.To);
        }

        private int OnProcessingSequence(IPfcNode node1, IPfcNode node2)
        {
            // Must process parallel convergences last.
            InputRole ir1 = GetValidationData(node1).InputRole;
            InputRole ir2 = GetValidationData(node2).InputRole;
            if (ir1 == InputRole.ParallelConvergence && ir2 != InputRole.ParallelConvergence)
            {
                return 1;
            }
            if (ir1 != InputRole.ParallelConvergence && ir2 == InputRole.ParallelConvergence)
            {
                return -1;
            }
            if (Node1DependsOnNode2(node1, node2))
            {
                return 1;
            }
            else if (Node1DependsOnNode2(node2, node1))
            {
                return -1;
            }
            else
            {
                return -Comparer.Default.Compare(node2.GraphOrdinal, node1.GraphOrdinal);
            }
        }

        private void Check()
        {
            m_errorList = new List<PfcValidationError>();
            List<ValidationToken> tokensWithLiveChildren = new List<ValidationToken>();
            List<ValidationToken> tokensWithOpenAlternates = new List<ValidationToken>();
            List<IPfcNode> unreachableNodes = new List<IPfcNode>();
            List<IPfcNode> unexecutedNodes = new List<IPfcNode>();
            List<IPfcStepNode> inconsistentSerialConvergences = new List<IPfcStepNode>();

            #region Collect errors

            foreach (IPfcNode node in m_pfc.Nodes)
            {
                NodeValidationData nodeVd = GetValidationData(node);
                ValidationToken nodeVt = nodeVd.ValidationToken;
                if (nodeVt == null)
                {
                    unreachableNodes.Add(node);
                    unexecutedNodes.Add(node);
                }
                else
                {
                    if (nodeVt.AlternatePathsOpen > 0 && !tokensWithOpenAlternates.Contains(nodeVt))
                    {
                        tokensWithOpenAlternates.Add(GetValidationData(node).ValidationToken);
                    }
                    if (!nodeVd.NodeHasRun) unexecutedNodes.Add(node);

                    if (node.ElementType == PfcElementType.Step && node.PredecessorNodes.Count() > 1)
                    {
                        ValidationToken vt = GetValidationData(node.PredecessorNodes[0]).ValidationToken;
                        if (!node.PredecessorNodes.TrueForAll(n => GetValidationData(n).ValidationToken.Equals(vt)))
                        {
                            inconsistentSerialConvergences.Add((IPfcStepNode)node);
                        }
                    }

                    if (nodeVt.AnyChildLive) tokensWithLiveChildren.Add(nodeVt);
                }
            }

            #endregion

            m_pfcIsValid = tokensWithLiveChildren.Count() == 0 &&
                           tokensWithOpenAlternates.Count() == 0 &&
                           unreachableNodes.Count() == 0 &&
                           unexecutedNodes.Count() == 0 &&
                           inconsistentSerialConvergences.Count() == 0;

            StringBuilder sb = null;
            sb = new StringBuilder();

            unreachableNodes.Sort(m_nodeByName);
            unexecutedNodes.Sort(m_nodeByName);
            inconsistentSerialConvergences.Sort(m_stepNodeByName);

            foreach (IPfcNode node in unreachableNodes)
            {
                if (!node.PredecessorNodes.TrueForAll(n => GetValidationData(n).ValidationToken == null))
                {
                    string narrative = string.Format("Node {0}, along with others that follow it, is unreachable.",
                        node.Name);
                    m_errorList.Add(new PfcValidationError("Unreachable PFC Node", narrative, node));
                    sb.AppendLine(narrative);
                }
            }

            foreach (IPfcNode node in unexecutedNodes)
            {
                if (!node.PredecessorNodes.TrueForAll(n => GetValidationData(n).ValidationToken == null))
                {
                    string narrative = string.Format("Node {0} failed to run.", node.Name);
                    m_errorList.Add(new PfcValidationError("Unexecuted PFC Node", narrative, node));
                    sb.AppendLine(narrative);
                }
            }

            inconsistentSerialConvergences.Sort((n1, n2) => Comparer.Default.Compare(n1.Name, n2.Name));

            foreach (IPfcStepNode node in inconsistentSerialConvergences)
            {
                string narrative =
                    string.Format(
                        "Branch paths (serial convergences) into {0} do not all have the same validation token, meaning they came from different branches (serial divergences).",
                        node.Name);
                m_errorList.Add(new PfcValidationError("Serial Di/Convergence Mismatch", narrative, node));
                sb.AppendLine(narrative);
            }

            foreach (ValidationToken vt in tokensWithLiveChildren)
            {

                List<IPfcNode> liveNodes = new List<IPfcNode>();
                foreach (ValidationToken vt2 in vt.ChildNodes.Where(n => n.AlternatePathsOpen > 0))
                {
                    liveNodes.Add(vt2.Origin);
                }

                int nParallelsOpen = vt.ChildNodes.Count();

                string narrative =
                    string.Format(
                        "Under {0}, there {1} {2} parallel branch{3} that did not complete - {4} began at {5}.",
                        vt.Origin.Name,
                        nParallelsOpen == 1 ? "is" : "are",
                        nParallelsOpen,
                        nParallelsOpen == 1 ? "" : "es",
                        nParallelsOpen == 1 ? "it" : "they",
                        StringOperations.ToCommasAndAndedList<IPfcNode>(liveNodes, n => n.Name)
                        );

                m_errorList.Add(new PfcValidationError("Uncompleted Parallel Branches", narrative, vt.Origin));
                sb.AppendLine();

            }

            if (m_diagnostics)
            {
                Console.WriteLine(sb.ToString());
            }
        }

        private NodeValidationData GetValidationData(IPfcNode node)
        {
            return node.UserData as NodeValidationData;
        }

        private LinkValidationData GetValidationData(IPfcLinkElement link)
        {
            return link.UserData as LinkValidationData;
        }

        public IEnumerable<PfcValidationError> Errors
        {
            get { return m_errorList; }
        }

        #region Support classes, events, delegates and enums.

        public class PfcValidationError : IModelError
        {

            private IPfcNode m_subject;
            private object m_target;
            private string m_narrative;
            private string m_name;

            public PfcValidationError(string name, string narrative, IPfcNode subject)
            {
                m_target = null;
                m_name = name;
                m_narrative = narrative;
                m_subject = subject;
            }

            #region IModelError Members

            public Exception InnerException
            {
                get { return null; }
            }

            public bool AutoClear
            {
                get { return true; }
            }

            #endregion

            #region INotification Members

            public string Name
            {
                get { return m_name; }
            }

            public string Narrative
            {
                get { return m_narrative; }
            }

            public object Target
            {
                get { return m_target; }
            }

            public object Subject
            {
                get { return m_subject; }
            }

            public int Priority
            {
                get { return 0; }
            }

            #endregion

            public IPfcNode SubjectNode
            {
                get { return m_subject; }
            }

            public delegate bool NodeDiscriminator(IPfcNode node);

            /// <summary>
            /// Nameses the of subjects neighbor nodes.
            /// </summary>
            /// <param name="distanceLimit">The distance limit - only go this far forward or back, searching for nodes.</param>
            /// <param name="discriminator">The discriminator. Takes an integer,</param>
            /// <returns></returns>
            public string NamesOfSubjectsNeighborNodes(int distanceLimit, NodeDiscriminator discriminator)
            {

                List<string> nodesBefore = new List<string>();
                List<string> nodesAfter = new List<string>();

                Queue<IPfcNode> preds = new Queue<IPfcNode>();
                foreach (IPfcNode node in m_subject.PredecessorNodes)
                    preds.Enqueue(node);
                for (int i = 0; i < distanceLimit; i++)
                {
                    int limit = preds.Count;
                    for (int j = 0; j < limit; j++)
                    {
                        IPfcNode candidate = preds.Dequeue();
                        if (discriminator(candidate))
                        {
                            nodesBefore.Add(candidate.Name);
                        }
                        else
                        {
                            foreach (IPfcNode node in candidate.PredecessorNodes)
                                preds.Enqueue(node);
                        }
                    }
                }

                Queue<IPfcNode> succs = new Queue<IPfcNode>();
                foreach (IPfcNode node in m_subject.SuccessorNodes)
                    succs.Enqueue(node);
                for (int i = 0; i < distanceLimit; i++)
                {
                    int limit = succs.Count;
                    for (int j = 0; j < limit; j++)
                    {
                        IPfcNode candidate = succs.Dequeue();
                        if (discriminator(candidate))
                        {
                            nodesAfter.Add(candidate.Name);
                        }
                        else
                        {
                            foreach (IPfcNode node in candidate.SuccessorNodes)
                                succs.Enqueue(node);
                        }
                    }
                }

                string before;
                if (nodesAfter.Count > 0)
                {
                    before = string.Format("is before {0}", StringOperations.ToCommasAndAndedList(nodesAfter));
                }
                else
                {
                    before = "has no recognizable successors";
                }
                string after;
                if (nodesBefore.Count > 0)
                {
                    after = string.Format("is after {0}", StringOperations.ToCommasAndAndedList(nodesBefore));
                }
                else
                {
                    after = "has no recognizable predecessors";
                }
                string retval = string.Format("The node {0} {1} and {2}.", m_subject.Name, before, after);
                return retval;
            }
        }

        internal enum InputRole
        {
            ParallelConvergence,
            SerialConvergence,
            PassIn,
            StartNode
        };

        internal enum OutputRole
        {
            ParallelDivergence,
            SerialDivergence,
            PassOut,
            TerminalNode
        };

        internal class LinkValidationData
        {

            private object m_userData;
            private IPfcLinkElement m_link;

            public static void Attach(IPfcLinkElement link)
            {
                new LinkValidationData()._Attach(link);
            }

            public static void Detach(IPfcLinkElement link)
            {
                LinkValidationData vd = link.UserData as LinkValidationData;
                if (vd != null)
                    link.UserData = vd.m_userData;
            }

            private void _Attach(IPfcLinkElement link)
            {
                m_link = link;
                m_userData = link.UserData;
                link.UserData = this;
            }

            public bool[] NodesBelow;

        }

        internal class NodeValidationData
        {

            private object m_userData;
            private IPfcNode m_node;

            public static void Attach(IPfcNode node)
            {
                new NodeValidationData()._Attach(node);
            }

            public static void Detach(IPfcNode node)
            {
                NodeValidationData vd = node.UserData as NodeValidationData;
                if (vd != null)
                    node.UserData = vd.m_userData;
            }

            internal ValidationToken ValidationToken { get; set; }

            internal InputRole InputRole { get; set; }
            internal OutputRole OutputRole { get; set; }

            public bool NodeHasRun { get; set; }

            public bool? IsInPath { get; set; }

            public int DequeueCount { get; set; }

            public IPfcNode DivergenceNode { get; set; }

            public IPfcNode ClosureNode { get; set; }

            private NodeValidationData()
            {
            }

            private void _Attach(IPfcNode node)
            {
                m_node = node;
                m_userData = node.UserData;
                node.UserData = this;

                if (node.PredecessorNodes.Count == 0)
                {
                    InputRole = InputRole.StartNode;
                }
                else if (node.PredecessorNodes.Count == 1)
                {
                    InputRole = InputRole.PassIn;
                }
                else if (node.ElementType == PfcElementType.Step)
                {
                    InputRole = InputRole.SerialConvergence;
                }
                else
                {
                    InputRole = InputRole.ParallelConvergence;
                }

                if (node.SuccessorNodes.Count == 0)
                {
                    OutputRole = OutputRole.TerminalNode;
                }
                else if (node.SuccessorNodes.Count == 1)
                {
                    OutputRole = OutputRole.PassOut;
                }
                else if (node.ElementType == PfcElementType.Step)
                {
                    OutputRole = OutputRole.SerialDivergence;
                }
                else
                {
                    OutputRole = OutputRole.ParallelDivergence;
                }
            }
        }

        internal class ValidationToken : TreeNode<ValidationToken>
        {

            #region Private Fields

            private static int _nToken = 0;
            private IPfcNode m_origin = null;
            private int m_openAlternatives;

            #endregion

            public string Name { get; set; }

            public ValidationToken(IPfcNode origin)
            {
                m_origin = origin;
                Name = string.Format("Token_{0}", _nToken++);
                IsSelfReferential = true; // Defines a behavior in the underlying TreeNode.
                m_openAlternatives = 1;
            }

            public IPfcNode Origin
            {
                get { return m_origin; }
            }

            public int Generation
            {
                get { return Parent == null ? 0 : Parent.Payload.Generation + 1; }
            }

            public void IncrementAlternatePathsOpen()
            {
                m_openAlternatives++;
            }

            public void DecrementAlternatePathsOpen()
            {
                m_openAlternatives--;
            }

            public int AlternatePathsOpen
            {
                get { return m_openAlternatives + (AnyChildLive ? 1 : 0); }
            }

            public bool AnyChildLive
            {
                get
                {
                    foreach (ValidationToken vt in ChildNodes)
                    {
                        if (vt.AlternatePathsOpen > 0)
                            return true;
                    }
                    return false;
                }
            }

            public override string ToString()
            {
                return Name;
            }

        }

        private class QueueData
        {
            public QueueData(IPfcNode from, IPfcNode to)
            {
                From = from;
                To = to;
            }

            public IPfcNode From;
            public IPfcNode To;

            public override string ToString()
            {
                return string.Format("{0} -> {1}", From.Name, To.Name);
            }
        }

        /// <summary>
        /// Not used. Kept b/c the concept is useful.
        /// </summary>
        internal class DependencyGraph
        {
            private int m_maxOrdinal;
            private bool[,] m_dependencies;
            private bool m_dirty;
            public bool Diagnostics { get; set; }

            public DependencyGraph(int nElements)
            {
                m_maxOrdinal = nElements;
                m_dependencies = new bool[m_maxOrdinal, m_maxOrdinal];
                m_dirty = false;
            }

            public void AddDependency(int independent, int dependent)
            {
                m_dependencies[independent, dependent] = true;
                m_dirty = true;
            }

            public bool IsDependent(int independent, int dependent)
            {
                if (m_dirty)
                {
                    Resolve();
                    m_dirty = false;
                }
                return (independent != dependent) && m_dependencies[independent, dependent];
            }

            public bool[,] GetMatrix()
            {
                if (m_dirty)
                {
                    Resolve();
                    m_dirty = false;
                }
                return m_dependencies;
            }

            private void Resolve()
            {
                bool dirty = true;
                while (dirty)
                {
                    dirty = false;
                    if (Diagnostics)
                        Console.WriteLine("\r\nCommencing sweep.");
                    for (int indep = 0; indep < m_maxOrdinal; indep++)
                    {
                        for (int dep = 0; dep < m_maxOrdinal; dep++)
                        {
                            if (Diagnostics)
                                Console.WriteLine("Checking ({0},{1}) ... {2}.", indep, dep, m_dependencies[indep, dep]);
                            if (m_dependencies[indep, dep] == true)
                            {
                                for (int c = 0; c < m_maxOrdinal; c++)
                                {
                                    if (m_dependencies[dep, c] == true)
                                    {
                                        if (Diagnostics)
                                            Console.WriteLine("\tFound ({0},{1}) ... {2}.", dep, c,
                                                m_dependencies[dep, c]);
                                        if (m_dependencies[indep, c] == false)
                                        {
                                            if (Diagnostics)
                                                Console.WriteLine("\t\tSetting ({0},{1}) to true.", indep, c);
                                            m_dependencies[indep, c] = true;
                                            dirty = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private Comparison<IPfcNode> m_nodeByName =
            new Comparison<IPfcNode>(
                delegate (IPfcNode n1, IPfcNode n2)
                {
                    return Comparer.Default.Compare(n1.GraphOrdinal, n2.GraphOrdinal);
                });

        private Comparison<IPfcStepNode> m_stepNodeByName =
            new Comparison<IPfcStepNode>(
                delegate (IPfcStepNode n1, IPfcStepNode n2)
                {
                    return Comparer.Default.Compare(n1.GraphOrdinal, n2.GraphOrdinal);
                });

        #endregion
    }

    /// <summary>
    /// An exception that is thrown if there is a cycle in a dependency graph that has been analyzed.
    /// </summary>
    [Serializable]
    public class PFCValidityException : Exception
    {
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp

        #region protected ctors

        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected PFCValidityException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        #endregion

        private IProcedureFunctionChart m_pfc = null;

        /// <summary>
        /// Gets the members of the cycle.
        /// </summary>
        /// <value>The members of the cycle.</value>
        public IProcedureFunctionChart Pfc
        {
            get { return m_pfc; }
        }

        #region public ctors

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public PFCValidityException(IProcedureFunctionChart pfc)
        {
            m_pfc = pfc;
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="pfc">The members of the cycle.</param>
        public PFCValidityException(IProcedureFunctionChart pfc, string message) : base(message)
        {
            m_pfc = pfc;
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="pfc">The members of the cycle.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public PFCValidityException(IProcedureFunctionChart pfc, string message, Exception innerException)
            : base(message, innerException)
        {
            m_pfc = pfc;
        }

        #endregion

    }

}