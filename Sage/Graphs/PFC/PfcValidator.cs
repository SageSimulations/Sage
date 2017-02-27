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

        private static readonly bool s_use_Legacy_Validation = false;
        //private static readonly bool m_diagnostics = Highpoint.Sage.Diagnostics.DiagnosticAids.Diagnostics("PfcValidator");
        public static bool m_diagnostics = false;
        private IProcedureFunctionChart m_pfc;
        private bool? m_pfcIsValid = null;
        private Queue<QueueData> m_activePath;
        private ValidationToken m_root;
        private IPfcNode m_activeNodeFrom = null;
        private bool[,] m_dependencies;
        private List<PfcValidationError> m_errorList = null;
        private int m_maxGraphOrdinal = 0;

        #endregion

        public PfcValidator(IProcedureFunctionChart pfc)
        {
            if (s_use_Legacy_Validation)
            {
                _PfcValidator validator = new _PfcValidator(pfc);
                m_pfcIsValid = validator.PfcIsValid();
                m_errorList = (List<PfcValidationError>) validator.Errors;
            }
            else
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

            //foreach(IPfcNode node in m_pfc.Nodes)
            //    Console.WriteLine("{0} -> {1} ({2}) -> {3} {4}/{5}", node.PredecessorNodes.Count(), node.Name, node.GraphOrdinal, node.SuccessorNodes.Count(),
            //        node.PredecessorNodes.TrueForAll(n => n.SuccessorNodes.Contains(node)),
            //       node.SuccessorNodes.TrueForAll(n => n.PredecessorNodes.Contains(node)));

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

            } while (m_activePath.Count() > 0);
        }

        private bool ProcessSerialConvergence(IPfcNode node)
        {
            if (!GetValidationData(node).NodeHasRun)
            {
                // Don't decrement alt-open-paths, since we'll propagate this leg.
                return true;
            }
            else
            {
                GetValidationData(node).ValidationToken.DecrementAlternatePathsOpen();
                if (m_diagnostics)
                    Console.WriteLine("\tNot processing {0} further - we've already traversed.", node.Name);
                return false;
            }
        }

        private bool ProcessParallelConvergence(IPfcNode node)
        {
            // Only run it if it's the last encounter of a parallel convergence.
            if (GetValidationData(node).DequeueCount == node.PredecessorNodes.Count())
            {
                if (m_diagnostics)
                    Console.WriteLine("\tProcessing closure of {0}.", node.Name);
                //GetValidationData(m_activeNodeFrom).ValidationToken.DecrementAlternatePathsOpen();
                UpdateClosureToken(node);
                return true;
            }
            else
            {
                //GetValidationData(m_activeNodeFrom).ValidationToken.DecrementAlternatePathsOpen();
                if (m_diagnostics)
                    Console.WriteLine("\tNot processing {0} further - we'll encounter it again.", node.Name);
                return false;
            }
        }

        private void ProcessParallelDivergence(IPfcNode node)
        {
            ValidationToken nodeVt = GetValidationData(node).ValidationToken;
            foreach (IPfcNode succ in node.SuccessorNodes)
            {
                ValidationToken succVt = new ValidationToken(succ);
                nodeVt.AddChild(succVt);
                Enqueue(node, succ, succVt);
            }
            nodeVt.DecrementAlternatePathsOpen(); // I've been replaced by my childrens' collective path.
        }

        private void ProcessSerialDivergence(IPfcNode node)
        {
            ValidationToken nodeVt = GetValidationData(node).ValidationToken;
            foreach (IPfcNode succ in node.SuccessorNodes)
            {
                nodeVt.IncrementAlternatePathsOpen();
                Enqueue(node, succ, nodeVt);
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

        private IPfcNode Dequeue()
        {
            List<QueueData> tmp = new List<QueueData>(m_activePath);
            tmp.Sort(OnProcessingSequence);

            m_activePath.Clear();
            tmp.ForEach(n => m_activePath.Enqueue(n));
            QueueData qd = m_activePath.Dequeue();
            IPfcNode retval = qd.To;
            m_activeNodeFrom = qd.From;
            return retval;
        }

        private void UpdateClosureToken(IPfcNode closure)
        {

            ValidationToken replacementToken = ClosureToken(closure);

            replacementToken.IncrementAlternatePathsOpen();
            GetValidationData(closure).ValidationToken = replacementToken;
            if (m_diagnostics)
                Console.WriteLine("\t\tAssigning {0} ({1}) to {2} on its closure.", replacementToken.Name,
                    replacementToken.AlternatePathsOpen, closure.Name);

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

        private bool AllBackwardPathsContain(IPfcNode from, IPfcNode target)
        {
            m_pfc.Nodes.ForEach(n => GetValidationData(n).IsInPath = null);
            Stack<IPfcNode> path = new Stack<IPfcNode>();
            path.Push(from);
            return AllBackwardPathsContain(from, target, path);
        }

        /// <summary>
        /// Determines whether all backward paths from 'from' contain the node 'target.' If they do,
        /// and it is the first such encounter for a specific 'from' then it may be said that target
        /// is the divergence node for 'from.'
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="target">The target.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private bool AllBackwardPathsContain(IPfcNode from, IPfcNode target, Stack<IPfcNode> path)
        {
            NodeValidationData nvd = GetValidationData(from);
            if (nvd.IsInPath == null)
            {
                if (from.PredecessorNodes.Count() == 0)
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
                    foreach (IPfcNode pred in from.PredecessorNodes)
                    {
                        if (!AllBackwardPathsContain(pred, target, path))
                        {
                            nvd.IsInPath = false;
                            break;
                        }
                    }
                    //GetValidationData(from).DivergenceNode = target;
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
                    if (!nodeVd.NodeHasRun)
                        unexecutedNodes.Add(node);
                    if (node.ElementType == PfcElementType.Step && node.PredecessorNodes.Count() > 1)
                    {
                        ValidationToken vt = GetValidationData(node.PredecessorNodes[0]).ValidationToken;
                        if (!node.PredecessorNodes.TrueForAll(n => GetValidationData(n).ValidationToken.Equals(vt)))
                        {
                            inconsistentSerialConvergences.Add((IPfcStepNode) node);
                        }
                    }
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
                delegate(IPfcNode n1, IPfcNode n2)
                {
                    return Comparer.Default.Compare(n1.GraphOrdinal, n2.GraphOrdinal);
                });

        private Comparison<IPfcStepNode> m_stepNodeByName =
            new Comparison<IPfcStepNode>(
                delegate(IPfcStepNode n1, IPfcStepNode n2)
                {
                    return Comparer.Default.Compare(n1.GraphOrdinal, n2.GraphOrdinal);
                });

        #endregion
    }

#if true
    public class _PfcValidator
    {

        #region Reduction Rule Declarations

        public interface IReductionRule
        {
            /// <summary>
            /// Initializes the rule in reference to the specified graph.
            /// </summary>
            /// <param name="graph">The graph.</param>
            /// <returns>True if the graph is legal. If false, the engine will halt immediately.</returns>
            bool Initialize(List<ProxyNode> graph);

            /// <summary>
            /// Reduces the specified graph, if possible, according to the rule.
            /// </summary>
            /// <param name="graph">The graph.</param>
            /// <returns>True if reduction was possible.</returns>
            bool Reduce(List<ProxyNode> graph);

            /// <summary>
            /// Indicates if the graph is still valid. This is run after each reduction cycle.
            /// </summary>
            /// <param name="graph">The graph.</param>
            /// <returns></returns>
            bool StillValid(List<ProxyNode> graph);

        }

        public class ReductionRuleBase : IReductionRule
        {

            /// <summary>
            /// Condenses the list by removing orphaned nodes.
            /// </summary>
            /// <param name="graph">The graph.</param>
            protected void CondenseList(List<ProxyNode> graph)
            {
                for (int i = graph.Count - 1; i >= 0; i--)
                {
                    if (graph[i].Predecessors.Count == 0 && graph[i].Successors.Count == 0)
                    {
                        if (s_diagnostics)
                            Console.WriteLine("\t\t\tRemoving " + graph[i].Name + " for reason of " +
                                              graph[i].ReasonForElimination);
                        graph.RemoveAt(i);
                    }
                    else
                    {
                        graph[i].Predecessors.Sort(ProxyNode.NodeComparer);
                    }
                }
            }

            #region IReductionRule Members

            public virtual bool Initialize(List<ProxyNode> graph)
            {
                return true;
            }

            public virtual bool Reduce(List<ProxyNode> graph)
            {
                return false;
            }

            public virtual bool StillValid(List<ProxyNode> graph)
            {
                return true;
            }

            #endregion
        }

        /// <summary>
        /// Returns true if the PFC has a start step. No reduction is performed.
        /// </summary>
        public class RequiredStartStepReductionRule : ReductionRuleBase
        {

            #region IReductionRule Members

            public override bool Initialize(List<ProxyNode> graph)
            {
                // Next special case rule, create & abstract IValidityRule & use the following, and the reduction rule applicator
                // below it as the first two overall IValidityRules. (Remember bool bIsDestructive as a parameter of the rule.)
                bool bHasStartStep = false;
                graph.ForEach(delegate(ProxyNode node) { if (node.Predecessors.Count == 0) bHasStartStep = true; });
                if (!bHasStartStep)
                {
                    return false;
                }
                return true;
            }

            #endregion
        }

        /// <summary>
        /// On initialization, looks for duality violations (node has multiple successors,
        /// and one of them has multiple predecessors.) TODO: Remove this, as it exists
        /// already in the DualityLinkReductionRule below.
        /// During reduction, looks for nodes with only one predecessor and one successor,
        /// and removes those nodes, tying the predecessor to the successor.
        /// </summary>
        public class SequentialityReductionRule : ReductionRuleBase
        {

            public override bool Initialize(List<ProxyNode> graph)
            {

                foreach (ProxyNode node in graph)
                {
                    if (node.Successors.Count > 1)
                    {
                        foreach (ProxyNode successor in node.Successors)
                        {
                            if (successor.Predecessors.Count > 1)
                            {
                                if (s_diagnostics)
                                    Console.WriteLine("There is a duality violation on the link between " + node +
                                                      " and " + successor + ".");
                                return false;
                            }
                        }
                    }
                }

                return base.Initialize(graph);
            }

            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = false;
                foreach (ProxyNode node in graph)
                {

                    if (s_diagnostics)
                        Console.WriteLine("Inspecting " + node.Name);

                    if (node.Predecessors.Count == 1 && node.Successors.Count == 1)
                    {
                        node.ReasonForElimination = "Sequentiality";

                        ProxyNode pred = node.Predecessors[0];
                        ProxyNode succ = node.Successors[0];

                        if (s_diagnostics)
                            Console.WriteLine("Removing " + node.Name);

                        // Disconnect this node from the pred & succ.
                        pred.Successors.Remove(node);
                        succ.Predecessors.Remove(node);
                        pred.Successors.Add(succ);
                        succ.Predecessors.Add(pred);

                        if (node.IsTerminal)
                        {
                            if (s_diagnostics)
                                Console.WriteLine("Conveying terminalness of " + node.Name + " upstream to " + pred.Name);
                            pred.IsTerminal = true;
                        }

                        node.Predecessors.Clear();
                        node.Successors.Clear();
                        // ...so that it can be cleaned up in the condense cycle.

                        foundSuccess = true;
                    }
                    else
                    {
                        if (s_diagnostics)
                            Console.WriteLine("Skipping reduction of " + node.Name +
                                              " because its removal would result in a duality violation.");
                    }
                }

                CondenseList(graph);

                return foundSuccess;
            }

        }

        /// <summary>
        /// Terminal-node variant of sequentiality rule.
        /// If I am a terminal node, I can be removed.
        /// </summary>
        public class TerminalnessReductionRule : ReductionRuleBase
        {
            #region IReductionRule Members

            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = false;

                foreach (ProxyNode node in graph)
                {
                    //if (m_diagnostics)
                    //    Console.Write("Examining " + node.Name + "... ");
                    if (node.Successors.Count == 0)
                    {
                        foreach (ProxyNode pred in node.Predecessors)
                        {
                            pred.Successors.Remove(node);
                        }

                        if (s_diagnostics)
                            Console.WriteLine(" Removing " + node.Name + ", and making " + node.Predecessors[0].Name +
                                              " terminal.");
                        node.Predecessors.Clear();
                        foundSuccess = true;
                    }
                    else
                    {
                        //if (m_diagnostics)
                        //    Console.WriteLine("Skipping it.");
                    }
                }

                CondenseList(graph);

                return foundSuccess;
            }

            #endregion
        }

        /// <summary>
        /// Removes duplicate same-direction links between a predecessor and a successor.
        /// </summary>
        public class ClosednessReductionRule : ReductionRuleBase
        {

            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = false;

                foreach (ProxyNode node in graph)
                {
                    if (node.Predecessors.Count > 1)
                    {
                        for (int i = node.Predecessors.Count - 2; i >= 0; i--)
                        {
                            // Relies on the fact that the predecessor node list is sorted.
                            if (node.Predecessors[i].ElementType == node.ElementType)
                            {
                                if (node.Predecessors[i].Equals(node.Predecessors[i + 1]))
                                {
                                    node.ReasonForElimination = "Closedness";
                                    foundSuccess = true;
                                    node.Predecessors[i + 1].Successors.Remove(node);
                                    node.Predecessors.RemoveAt(i + 1);
                                }
                            }
                        }
                    }
                }
                CondenseList(graph);

                return foundSuccess;
            }

        }

        /// <summary>
        /// Removes duplicate same-direction links between a predecessor and a successor.
        /// </summary>
        public class SecondOrderClosednessReductionRule : ReductionRuleBase
        {

            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = false;

                foreach (ProxyNode node in graph)
                {
                    if (node.Predecessors.Count == 1 &&
                        node.Successors.Count == 1 &&
                        node.Predecessors[0].Successors.Count > 1 &&
                        node.Successors[0].Predecessors.Count > 1)
                    {

                        ProxyNode nodePred = node.Predecessors[0];
                        ProxyNode nodeSucc = node.Successors[0];
                        // Check to see if this node is parallel to another.
                        foreach (ProxyNode tmp in nodePred.Successors.Where(n => n != node))
                        {
                            if (tmp.Successors.Contains(nodeSucc))
                            {
                                // node is a short-circuit against tmp. Excise node.
                                System.Diagnostics.Debug.Assert(nodePred.Successors.Count > 1 &&
                                                                nodeSucc.Predecessors.Count > 1);
                                nodePred.Successors.Remove(node);
                                nodeSucc.Predecessors.Remove(node);
                                node.Predecessors.Clear();
                                node.Successors.Clear();
                                node.ReasonForElimination = "Second order Closedness.";
                                break;
                            }
                        }

                    }
                }
                CondenseList(graph);

                return foundSuccess;
            }

        }

        /// <summary>
        /// If A is followed by B and B is preceded only by A, and followed by C, D &amp; E,
        /// then we remove B, and add C, D &amp; E as successors to A.
        /// </summary>
        public class NotInUseAdjacencyReductionRule : ReductionRuleBase
        {

            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = false;

                foreach (ProxyNode node in graph)
                {
                    List<ProxyNode> successorsToAdd = new List<ProxyNode>();
                    List<ProxyNode> successorsToRemove = new List<ProxyNode>();

                    foreach (ProxyNode successor in node.Successors)
                    {
                        if (successor.ElementType == node.ElementType)
                        {
                            if (successor.Predecessors.Count == 1 && successor.Successors.Count > 1)
                            {
                                successor.ReasonForElimination = "Adjacency";
                                foundSuccess = true;
                                foreach (ProxyNode succSucc in successor.Successors)
                                {
                                    successorsToAdd.Add(succSucc);
                                }
                                successorsToRemove.Add(successor);
                            }
                        }
                    }

                    foreach (ProxyNode sta in successorsToAdd)
                    {
                        sta.Predecessors.Add(node);
                        node.Successors.Add(sta);
                    }

                    foreach (ProxyNode str in successorsToRemove)
                    {
                        foreach (ProxyNode strSucc in str.Successors)
                        {
                            strSucc.Predecessors.Remove(str);
                        }
                        foreach (ProxyNode strSucc in str.Predecessors)
                        {
                            strSucc.Successors.Remove(str);
                        }
                        str.Successors.Clear();
                        str.Predecessors.Clear();
                    }

                }

                CondenseList(graph);

                return foundSuccess;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public class AdjacencyReductionRule : ReductionRuleBase
        {
            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = true;

                while (foundSuccess)
                {
                    foundSuccess = false;

                    foreach (ProxyNode node in graph)
                    {

                        if (node.Successors.Count != 1)
                            continue;
                        if (node.Successors[0].Predecessors.Count != 1)
                            continue;

                        // We've found a pair.
                        ProxyNode alpha = node;
                        ProxyNode omega = node.Successors[0];

                        // Evaluate the reomvability of the pair.
                        if (
                            // Ensure we can bind one distal, on one side, to all distals on the other.
                            ( // alpha has one predecessor with one successor
                                (alpha.Predecessors.Count == 1 && alpha.Predecessors[0].Successors.Count == 1)
                                ||
                                // omega has one successor with one predecessor
                                (omega.Successors.Count == 1 && omega.Successors[0].Predecessors.Count == 1)
                                )
                            &&
                            // Ensure that there will not be a duality violation in doing so.
                            // All alpha predecessors have only one successor (alpha) or
                            // all omega successors have only one predecessor (omega)
                            (alpha.Predecessors.TrueForAll(n => n.Successors.Count == 1)
                             || omega.Successors.TrueForAll(n => n.Predecessors.Count == 1))
                            )
                        {
                            // Remove the pair, and bind all alpha-predecessors to omega-successors.
                            if (s_diagnostics)
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine(string.Format("Removing the pair {0}->{1} for _Adjacency.", alpha.Name,
                                    omega.Name));
                                sb.AppendLine(string.Format("\t{0} has {1} predecessors and {2} successors.", alpha.Name,
                                    alpha.Predecessors.Count, alpha.Successors.Count));
                                sb.AppendLine(string.Format("\t{0} has {1} predecessors and {2} successors.", omega.Name,
                                    omega.Predecessors.Count, omega.Successors.Count));
                                Console.WriteLine(sb.ToString());
                            }
                            alpha.ReasonForElimination = "_AdjacencyReductionRule";
                            omega.ReasonForElimination = "_AdjacencyReductionRule";
                            foreach (ProxyNode alphaPred in alpha.Predecessors)
                            {
                                alphaPred.Successors.Remove(alpha);
                                foreach (ProxyNode omegaSucc in omega.Successors)
                                {
                                    omegaSucc.Predecessors.Remove(omega);
                                    omegaSucc.Predecessors.Add(alphaPred);
                                    alphaPred.Successors.Add(omegaSucc);
                                }
                            }

                            alpha.Predecessors.Clear();
                            omega.Predecessors.Clear();
                            alpha.Successors.Clear();
                            omega.Successors.Clear();
                            foundSuccess = true;

                            break;
                        }
                        else
                        {
                            // Otherwise, if removal of the pair would cause duality, do not remove.
                            continue;
                        }
                    }
                }

                CondenseList(graph);

                return foundSuccess;
            }
        }

        /// <summary>
        /// Any link that has the same predecessor and successor, can be removed.
        /// </summary>
        public class NotInUseSelfLoopReductionRule : ReductionRuleBase
        {

            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = false;

                foreach (ProxyNode node in graph)
                {
                    if (node.ElementType.Equals(PfcElementType.Transition))
                    {
                        continue; // Self-loop around a transition is a token machine-gun.
                    }
                    while (node.Successors.Contains(node))
                    {
                        node.Successors.Remove(node);
                        foundSuccess = true;
                    }
                    while (node.Predecessors.Contains(node))
                    {
                        node.Predecessors.Remove(node);
                        foundSuccess = true;
                    }
                }
                return foundSuccess;
            }

        }

        /// <summary>
        /// Any link that has the same predecessor and successor, can be removed.
        /// </summary>
        public class SelfLoopReductionRule : ReductionRuleBase
        {

            public override bool Reduce(List<ProxyNode> graph)
            {
                bool foundSuccess = false;

                foreach (ProxyNode node in graph)
                {

                    foreach (ProxyNode succ in node.Successors)
                    {

                        if (succ.Successors.Contains(node))
                        {
                            // A self-loop.
                            if (s_diagnostics)
                            {
                                Console.WriteLine("Removing the link from {0}->{1} for _SelfLoop.", succ.Name, node.Name);
                            }
                            node.Predecessors.Remove(succ);
                            succ.Successors.Remove(node);
                        }
                    }

                }
                return foundSuccess;
            }

        }

        /// <summary>
        /// On initialization, looks for duality violations (node has multiple successors,
        /// and one of them has multiple predecessors.)
        /// </summary>
        public class DualityLinkReductionRule : ReductionRuleBase
        {

            public override bool Initialize(List<ProxyNode> graph)
            {
                foreach (ProxyNode node in graph)
                {
                    if (node.Successors.Count > 1)
                    {
                        foreach (ProxyNode successor in node.Successors)
                        {
                            if (successor.Predecessors.Count > 1)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }

        }

        #endregion

        #region Private Fields

        private List<PfcValidator.PfcValidationError> m_errorList = null;
        private PfcNodeList m_pfcNodeList = null;
        private Dictionary<IPfcNode, ProxyNode> m_proxyNodes = null;
        private List<IReductionRule> m_reductionRules = null;
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PFC.Validator");

        #endregion Private Fields

        public _PfcValidator(IProcedureFunctionChart pfc)
        {

            m_errorList = new List<PfcValidator.PfcValidationError>();

            m_proxyNodes = new Dictionary<IPfcNode, ProxyNode>();
            m_pfcNodeList = pfc.Nodes;

            foreach (IPfcNode node in m_pfcNodeList)
            {
                m_proxyNodes.Add(node, new ProxyNode(node));
            }

            foreach (IPfcNode node in m_pfcNodeList)
            {
                ProxyNode proxyNode = m_proxyNodes[node];
                foreach (IPfcNode predecessor in node.PredecessorNodes)
                {
                    proxyNode.Predecessors.Add(m_proxyNodes[predecessor]);
                }
                foreach (IPfcNode successor in node.SuccessorNodes)
                {
                    proxyNode.Successors.Add(m_proxyNodes[successor]);
                }

                proxyNode.Predecessors.Sort(ProxyNode.NodeComparer);
            }
            InitializeReductionRules();

        }

        /*
        public ProxyNode ProxyFor(IPfcNode node) { return m_proxyNodes[node]; }
        public List<ProxyNode> Nodes { get { return new List<ProxyNode>(m_proxyNodes.Values); } }
        */

        public bool PfcIsValid()
        {

            try
            {

                List<ProxyNode> graph = new List<ProxyNode>(m_proxyNodes.Values);

                #region Initialize rules, allow them to abort processing due to initial invalidity.

                foreach (IReductionRule irr in m_reductionRules)
                {
                    if (!irr.Initialize(graph))
                    {
                        return false;
                    }
                }

                #endregion Initialize rules, allow them to abort processing due to initial invalidity.

                bool foundSuccess = true;
                while (foundSuccess)
                {
                    foundSuccess = false;

                    if (s_diagnostics)
                        Console.WriteLine("Performing a reduction iteration.");

                    #region Perform all rules' reductions.

                    foreach (IReductionRule irr in m_reductionRules)
                    {
                        if (s_diagnostics)
                            Console.WriteLine("\tApplying " + irr.GetType().Name + "...");
                        foundSuccess |= irr.Reduce(graph);

                        //if (m_diagnostics)
                        //    graph.ForEach(delegate(ProxyNode node) { Console.WriteLine("\t\t" + node.ToString()); });
                    }

                    #endregion Perform all rules' reductions.

                    #region Ask all rules' opinion of graph validity after reductions.

                    foreach (IReductionRule irr in m_reductionRules)
                    {
                        if (!irr.StillValid(graph))
                        {
                            return false;
                        }
                    }

                    #endregion Ask all rules' opinion of graph validity after reductions.

                }

                if (s_diagnostics)
                {
                    SimplifyNames(graph);
                    graph.ForEach(delegate(ProxyNode node) { Console.WriteLine(node.ToString()); });
                }

                ResetProxies();

                if (graph.Count > 0)
                {
                    string msg = string.Format("The PFC could not be reduced beyond the following {0} nodes : {1}.",
                        graph.Count, StringOperations.ToCommasAndAndedList(graph, n => n.Name));
                    m_errorList.Add(new PfcValidator.PfcValidationError("Validation Error", msg, null));
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                m_errorList.Add(new PfcValidator.PfcValidationError("Exception while validating", e.Message, null));
                return false;
            }
        }

        public IEnumerable<PfcValidator.PfcValidationError> Errors
        {
            get { return m_errorList; }
        }

        private void SimplifyNames(List<ProxyNode> graph)
        {
            char c1 = 'A';
            char c2 = 'A';
            foreach (ProxyNode pn in graph)
            {
                string newName = pn.ElementType == PfcElementType.Step ? "S_" : "T_";
                newName += c1;
                newName += c2;

                Console.WriteLine("{0} <-- {1}", newName, pn.Name);
                pn.SetName(newName);

                c2 = (char) (c2 == 'Z' ? 'A' : c2 + 1);
                c1 = (char) (c2 == 'A' ? c1 + 1 : c1);

            }

        }

        private void ResetProxies()
        {
            foreach (ProxyNode proxyNode in m_proxyNodes.Values)
            {
                proxyNode.Predecessors.Clear();
                proxyNode.Successors.Clear();
                IPfcNode actualNode = proxyNode.Node;
                if (actualNode != null)
                {
                    foreach (IPfcNode pred in proxyNode.Node.PredecessorNodes)
                    {
                        proxyNode.Predecessors.Add(m_proxyNodes[pred]);
                    }

                    foreach (IPfcNode succ in proxyNode.Node.SuccessorNodes)
                    {
                        proxyNode.Successors.Add(m_proxyNodes[succ]);
                    }
                }
            }
        }

        private void InitializeReductionRules()
        {
            m_reductionRules = new List<IReductionRule>();
            //m_reductionRules.Add(new SequentialityReductionRule());
            m_reductionRules.Add(new TerminalnessReductionRule());
            m_reductionRules.Add(new ClosednessReductionRule());
            m_reductionRules.Add(new SecondOrderClosednessReductionRule());
            //m_reductionRules.Add(new AdjacencyReductionRule());
            m_reductionRules.Add(new AdjacencyReductionRule());
            //m_reductionRules.Add(new SelfLoopReductionRule());
            m_reductionRules.Add(new SelfLoopReductionRule());
            m_reductionRules.Add(new DualityLinkReductionRule());
            m_reductionRules.Add(new RequiredStartStepReductionRule());

        }

        public class ProxyNode : IHasName
        {
            private IPfcNode m_node = null;
            private List<ProxyNode> m_predecessors = null;
            private List<ProxyNode> m_successors = null;
            private string m_reasonForElimination = null;
            private bool m_isTerminal = false;
            private PfcElementType m_elementType;
            private string m_name = null;


            public ProxyNode(IPfcNode node)
            {
                m_node = node;
                m_elementType = node.ElementType;
                m_name = node.Name;

                m_predecessors = new List<ProxyNode>();
                m_successors = new List<ProxyNode>();
            }

            public ProxyNode(string name, PfcElementType elementType)
            {
                m_node = null;
                m_elementType = elementType;
                m_name = name;
                m_predecessors = new List<ProxyNode>();
                m_successors = new List<ProxyNode>();
            }

            public IPfcNode Node
            {
                get { return m_node; }
            }

            internal void SetName(string name)
            {
                m_name = name;
            }

            public List<ProxyNode> Predecessors
            {
                get { return m_predecessors; }
            }

            public List<ProxyNode> Successors
            {
                get { return m_successors; }
            }

            public PfcElementType ElementType
            {
                get { return m_elementType; }
            }

            public string ReasonForElimination
            {
                get
                {
                    if (m_reasonForElimination == null & Predecessors.Count == 0 && Successors.Count == 0)
                    {
                        return "Orphaned";
                    }
                    return m_reasonForElimination;
                }
                set { m_reasonForElimination = value; }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                sb.Append(StringOperations.ToCommasAndAndedListOfNames<ProxyNode>(Predecessors));
                sb.Append("] ");
                sb.Append(Name);
                sb.Append(" [");
                sb.Append(StringOperations.ToCommasAndAndedListOfNames<ProxyNode>(Successors));
                sb.Append("]");
                return sb.ToString();
            }

            public bool IsTerminal
            {
                get { return m_isTerminal; }
                set { m_isTerminal = value; }
            }

            public static IComparer<ProxyNode> NodeComparer
            {
                get { return _nodeComparer; }
            }

            private static IComparer<ProxyNode> _nodeComparer = new _NodeComparer();

            private class _NodeComparer : IComparer<ProxyNode>
            {
                private PfcNode.NodeComparer m_pfcNodeComparer = new PfcNode.NodeComparer();

                #region IComparer<ProxyNode> Members

                public int Compare(ProxyNode x, ProxyNode y)
                {
                    if (x.Node == null || y.Node == null)
                    {
                        return Comparer<int>.Default.Compare(x.GetHashCode(), y.GetHashCode());
                    }
                    return m_pfcNodeComparer.Compare(x.Node, y.Node);
                }

                #endregion
            }

            #region IHasName Members

            public string Name
            {
                get { return m_name; }
            }

            #endregion
        }
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
#endif // NOT_DEFINED
}