/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Graphs.PFC {
    /// <summary>
    /// The PfcAnalyst is a static class that provides analytical helper methods.
    /// </summary>
    public class PfcAnalyst {

        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PFC.Analyst");

        #region Path-Related Queries
        public static int AssignWeightsForBroadestNonLoopingPath(ProcedureFunctionChart pfc) {

            pfc.Nodes.ForEach(n => n.NodeColor = NodeColor.White);
            pfc.Links.ForEach(n => n.Priority = null);

            PfcStep start = (PfcStep)pfc.GetStartSteps()[0];
            int retval = WeightAssignmentPropagationForBroadestNonLoopingPath(start);
            pfc.Links.ForEach(n => { if (n.Priority < 0) n.Priority = int.MinValue - n.Priority; });
            return retval;
        }

        private static int WeightAssignmentPropagationForBroadestNonLoopingPath(PfcStep step) {

            if (step.NodeColor == NodeColor.Black) return int.MinValue;

            if (!step.Successors.Any()) return 1; // We've reached a terminal step. (strictly, should be a transition.)

            step.NodeColor = NodeColor.Black;
            foreach (PfcLink link in step.Successors) {
                if (link.Priority == null) {
                    link.Priority = WeightAssignmentPropagationForBroadestNonLoopingPath(( (PfcTransition)link.Successor ));
                }
            }
            step.NodeColor = NodeColor.White;

            return step.Successors.Max(n => n.Priority.Value) + 1;

        }


        private static int WeightAssignmentPropagationForBroadestNonLoopingPath(PfcTransition trans) {
            if (trans.NodeColor == NodeColor.Black) return int.MinValue;

            if (trans.Successors.Count == 0) return 1; // We've reached a terminal transition.

            trans.NodeColor = NodeColor.Black;
            foreach (PfcLink link in trans.Successors) {
                if (link.Priority == null) {
                    link.Priority = WeightAssignmentPropagationForBroadestNonLoopingPath(( (PfcStep)link.Successor ));
                }
            }
            trans.NodeColor = NodeColor.White;

            int total = 0;
            foreach (PfcLink link in trans.Successors) {
                if (link.Priority < 0 ) {
                    total = trans.Successors.Where(n => n.Priority != null && n.Priority.Value < 0).Max(n => n.Priority).Value + 1;
                    break;
                } else {
                    total += link.Priority.Value; 
                }
            }
            return total;
        }

        #endregion

        #region Structure-related Queries

        /// <summary>
        /// Determines whether the specified link is preceded by a transition.
        /// </summary>
        /// <param name="link">The specified link.</param>
        /// <returns>
        /// 	<c>true</c> if the specified link is preceded by a transition.; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPostTransitionLink(IPfcLinkElement link) {
            if (link.Predecessor == null) {
                return false;
            } else {
                return link.Predecessor.ElementType.Equals(PfcElementType.Transition);
            }
        }

        /// <summary>
        /// Determines whether the specified link is followed by a transition.
        /// </summary>
        /// <param name="link">The specified link.</param>
        /// <returns>
        /// 	<c>true</c> if the specified link is followed by a transition.; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPreTransitionLink(IPfcLinkElement link) {
            if (link.Successor == null) {
                return false;
            } else {
                return link.Successor.ElementType.Equals(PfcElementType.Transition);
            }
        }

        /// <summary>
        /// Determines whether the specified element is the sole successor of its immediate precedent node. If immediate predecessor is
        /// null, this method returns false.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>
        /// 	<c>true</c> if the specified link is the sole successor of its one immediate precedent node; otherwise, (if there
        /// are any number of predecessors other than one, or if the one predecessor node has any number but one successor nodes)<c>false</c>.
        /// </returns>
        public static bool IsSoleSuccessor(IPfcElement element) {
            if (element == null) {
                return false;
            }
            if (element.ElementType.Equals(PfcElementType.Link)) {
                IPfcLinkElement link = (IPfcLinkElement)element;
                if (link.Predecessor == null) {
                    return false;
                } else {
                    return link.Predecessor.Successors.Count == 1;
                }
            } else {
                IPfcNode node = (IPfcNode)element;
                return node.PredecessorNodes.Count == 1 && node.PredecessorNodes[0].SuccessorNodes.Count == 1;
            }
        }

        /// <summary>
        /// Determines whether the specified element is a part of a path that has parallel paths. This algorithm
        /// goes up only one level - that is, if it's part of a series divergence that, itself, is in a path that
        /// is part of a parallel divergence, then the result will still be false.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>
        /// 	<c>true</c> if the specified element is a part of a path that has parallel paths; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasParallelPaths(IPfcElement element) {
            if (element == null) {
                return false;
            }
            if (element.ElementType.Equals(PfcElementType.Link)) {
                IPfcLinkElement linkElement = (IPfcLinkElement)element;
                if (linkElement.Predecessor != null && linkElement.Predecessor.ElementType.Equals(PfcElementType.Transition) && linkElement.Predecessor.Successors.Count > 1) {
                    return true;
                } else {
                    return HasParallelPaths(((IPfcLinkElement)element).Predecessor);
                }
            } else {
                IPfcNode prevDivergenceNode = GetPrevDivergenceNode((IPfcNode)element);
                return prevDivergenceNode != null && prevDivergenceNode.ElementType.Equals(PfcElementType.Transition);
            }
        }

        /// <summary>
        /// Determines whether the specified element is a part of a path that has alternate paths. This algorithm
        /// goes up only one level - that is, if it's part of a parallel divergence that, itself, is in a path that
        /// is part of a series divergence, then the result will still be false.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>
        /// 	<c>true</c> if the specified element is a part of a path that has parallel paths; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAlternatePaths(IPfcElement element) {
            if (element == null) {
                return false;
            }
            if (element.ElementType.Equals(PfcElementType.Link)) {
                IPfcLinkElement linkElement = (IPfcLinkElement)element;
                if (linkElement.Predecessor != null && linkElement.Predecessor.ElementType.Equals(PfcElementType.Step) && linkElement.Predecessor.Successors.Count > 1) {
                    return true;
                } else {
                    return HasAlternatePaths(((IPfcLinkElement)element).Predecessor);
                }
            } else {
                IPfcNode prevDivergenceNode = GetPrevDivergenceNode((IPfcNode)element);
                return prevDivergenceNode != null && prevDivergenceNode.ElementType.Equals(PfcElementType.Step);
            }
        }

        /// <summary>
        /// Determines whether the specified element is the last element on a path. That is, if deletion of this
        /// element (and its preceding and following links) would not leave dead-end nodes in the graph, it is considered
        /// to be the last element in the path.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>
        /// 	<c>true</c> if the specified element is the last element on a path; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLastElementOnPath(IPfcElement element) {

            if (element.ElementType.Equals(PfcElementType.Link)) {
                IPfcNode pre = ((IPfcLinkElement)element).Predecessor;
                if (pre.SuccessorNodes.Count == 1) {
                    return false;
                }

                IPfcNode post = ((IPfcLinkElement)element).Successor;
                if (post.PredecessorNodes.Count == 1) {
                    return false;
                }
            } else {
                foreach (IPfcNode pre in ((IPfcNode)element).PredecessorNodes) {
                    if (pre.SuccessorNodes.Count == 1) {
                        return false;
                    }
                }
                foreach (IPfcNode post in ((IPfcNode)element).SuccessorNodes) {
                    if (post.PredecessorNodes.Count == 1) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether the specified element is the last element on a path, and that path is an alternate path.
        /// That is, if deletion of this element (and its preceding and following links) would not leave dead-end nodes
        /// in the graph, it is considered to be the last element in the path.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>
        /// 	<c>true</c> if the specified element is the last element on an alternate path; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLastElementOnAlternatePath(IPfcElement element) {
            return HasAlternatePaths(element) && IsLastElementOnPath(element);
        }

        /// <summary>
        /// Determines whether the specified element is the last element on a path, and that path is a parallel path.
        /// That is, if deletion of this element (and its preceding and following links) would not leave dead-end nodes
        /// in the graph, it is considered to be the last element in the path.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>
        /// 	<c>true</c> if the specified element is the last element on a parallel path; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLastElementOnParallelPath(IPfcElement element) {
            return HasParallelPaths(element) && IsLastElementOnPath(element);
        }

        /// <summary>
        /// Gets the convergence node (step or transtion) for the specified divergence node. This assumes
        /// that outbound paths all diverged at the same node, and will converge at the same node as well.
        /// </summary>
        /// <param name="divergenceNode">The divergence node.</param>
        /// <returns>The convergence node, if this node is a divergence node, otherwise null.</returns>
        public static IPfcNode GetConvergenceNodeFor(IPfcNode divergenceNode) {
            if (divergenceNode.SuccessorNodes.Count < 2) {
                return null;
            }

            return (IPfcNode)GetJoinNodeForParallelPath(divergenceNode.SuccessorNodes[0]);
        }

        /// <summary>
        /// Gets the divergence node (step or transtion) for the specified convergence node. This assumes
        /// that outbound paths all diverged at the same node, and will converge at the same node as well.
        /// </summary>
        /// <param name="convergenceNode">The convergence node.</param>
        /// <returns>
        /// The join node, if the provided node is a convergence node, otherwise null.
        /// </returns>
        public static IPfcNode GetDivergenceNodeFor(IPfcNode convergenceNode) {
            if (convergenceNode.PredecessorNodes.Count < 2) {
                return null;
            }

            return (IPfcNode)GetDivergenceElementForParallelPath(convergenceNode.PredecessorNodes[0]);
        }

        /// <summary>
        /// Gets the join step that brings the path of the specified element and any parallel alternate paths together.
        /// If the specified node is not a member of a path with alternates, then this method returns null.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>The join element, if any - otherwise, null.</returns>
        public static IPfcStepNode GetJoinNodeForAlternatePaths(IPfcElement element) {
            return GetJoinNodeForParallelPath(element) as IPfcStepNode;
        }

        /// <summary>
        /// Gets the join transition that brings the path of the specified element and any parallel simultaneous paths together.
        /// If the specified node is not a member of a path with simultaneous paths, then this method returns null.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>The join transition, if any - otherwise, null.</returns>
        public static IPfcTransitionNode GetJoinTransitionForSimultaneousPaths(IPfcElement element) {
            return GetJoinNodeForParallelPath(element) as IPfcTransitionNode;
        }

        /// <summary>
        /// Gets the join element that brings the path of the specified element and any parallel paths (whether from series
        /// or parallel divergences) together. If the specified node is not a member of a path with simultaneous paths, then
        /// this method returns null.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>The join element, if any - otherwise, null.</returns>
        public static IPfcElement GetJoinNodeForParallelPath(IPfcElement element) {
            // Algorithm: Find the divergence node. Do a traversal for each outbound path until there
            // are no more nodes (end of path) or we've been there before (loopback). On encountering
            // each node for the first time under each path, increment a counter for that path.
            // 
            // The first time we encounter a node whose count is the number of diverging paths from the divergence
            // node, we've found the convergence node.

            IPfcNode node = element as IPfcNode;
            if (node == null) {
                node = ((IPfcLinkElement)element).Successor;
            }
            IPfcNode prevDivNode = GetPrevDivergenceNode((IPfcNode)element);

            Dictionary<IPfcNode, int> hitCounts = new Dictionary<IPfcNode, int>();

            IPfcNode convergenceNode = null;

            if (prevDivNode == null) {
                return null;
            }

            int nParallelPaths = prevDivNode.SuccessorNodes.Count;
            foreach (IPfcNode firstNodeInPath in prevDivNode.SuccessorNodes) {
                List<IPfcNode> beenThere = new List<IPfcNode>();
                Traverse(nParallelPaths, firstNodeInPath, beenThere, hitCounts, ref convergenceNode);
            }

            return convergenceNode;
        }

        /// <summary>
        /// Gets the divergence element where the path of the specified element and any parallel paths (whether from series
        /// or parallel) diverge. If the specified node is not a member of a path with simultaneous paths, then
        /// this method returns null.
        /// </summary>
        /// <param name="element">The specified element.</param>
        /// <returns>The join element, if any - otherwise, null.</returns>
        public static IPfcElement GetDivergenceElementForParallelPath(IPfcElement element) {
            return GetPrevDivergenceNode(element);
        }

        /// <summary>
        /// Gets a list of the nodes to which this node (the origin parameter) may link and retain a legal PFC structure.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <returns></returns>
        public static List<IPfcNode> GetPermissibleTargetsForLinkFrom(IPfcNode origin) {
            //DateTime dt = DateTime.Now;
            
            List<IPfcNode> retval = new List<IPfcNode>();
            List<IPfcNode> candidates = new List<IPfcNode>(origin.Parent.Nodes);
            foreach (IPfcNode target in candidates) {
                if ( IsTargetNodeLegal(origin,target) ) {
                    retval.Add(target);
                }
            }

            //TimeSpan duration = dt - DateTime.Now;

            return retval;
        }

        public static bool IsTargetNodeLegal(IPfcNode origin, IPfcNode target) {
            bool retval = false;
            IProcedureFunctionChart parent = origin.Parent;

            // We only evaluate step-to-step links, or transition-to-transition links,
            // meaning that we must always add a shim node between them.
            if (origin.ElementType.Equals(target.ElementType)) {

                if (s_diagnostics) {
                    Console.WriteLine("Before: " + StringOperations.ToCommasAndAndedListOfNames<IPfcNode>(parent.Nodes));
                }

                IPfcLinkElement link1, link2;
                IPfcNode shimNode;
                parent.Bind(origin, target, out link1, out shimNode, out link2, false);

                if (s_diagnostics) {
                    Console.WriteLine("During: " + StringOperations.ToCommasAndAndedListOfNames<IPfcNode>(parent.Nodes));
                }

                PfcValidator validator = new PfcValidator(parent);
                retval = validator.PfcIsValid();

                if (shimNode != null) {
                    parent.Unbind(origin, shimNode,true);
                    parent.Unbind(shimNode, target, true);
                    parent.UpdateStructure();
                } else {
                    parent.Unbind(origin, target);
                }

                if (s_diagnostics) {
                    Console.WriteLine("After: " + StringOperations.ToCommasAndAndedListOfNames<IPfcNode>(parent.Nodes));
                }

                parent.ElementFactory.Retract();
        
            } else {
                return false;
            }

            return retval;
        }

        /// <summary>
        /// Gets the start step from the provided PFC.
        /// </summary>
        /// <param name="pfc">The PFC.</param>
        /// <returns>The start step.</returns>
        public static IPfcStepNode GetStartStep(IProcedureFunctionChart pfc) {

            foreach (IPfcStepNode step in pfc.Steps ) {
                if (step.SuccessorNodes.Count == 0) {
                    return step;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the finish step from the provided PFC. Assumes that there is only one.
        /// </summary>
        /// <param name="pfc">The PFC.</param>
        /// <returns>The finish step.</returns>
        public static IPfcStepNode GetFinishStep(IProcedureFunctionChart pfc) {

            foreach (IPfcStepNode step in pfc.Steps) {
                if (step.PredecessorNodes.Count == 0) {
                    return step;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the primary path forward from the provided starting point node. The primary path is
        /// the path that is comprised of all of the highest-priority links out of each node encountered.
        /// </summary>
        /// <param name="startPoint">The starting point.</param>
        /// <param name="stepsOnly">if set to <c>true</c> it returns steps only. Otherwise, it returns all nodes.</param>
        /// <returns>The primary path.</returns>
        public static List<IPfcNode> GetPrimaryPath(IPfcNode startPoint, bool stepsOnly) {
            List<IPfcNode> retval = new List<IPfcNode>();
            IPfcNode cursor = startPoint;
            while (true) {
                if (retval.Contains(cursor)) {
                    int firstElementInLoop = retval.IndexOf(cursor);
                    ArrayList loopers = new ArrayList();
                    for (int i = firstElementInLoop; i < retval.Count; i++) {
                        loopers.Add(retval[i]);
                    }
                    string looperString = StringOperations.ToCommasAndAndedList(loopers);
                    throw new ApplicationException("Primary path contains a loop, which consists of " + looperString + "!");
                }

                if (!stepsOnly || (cursor.ElementType.Equals(PfcElementType.Step))) {
                    retval.Add(cursor);
                }

                if (cursor.Successors.Count == 0) {
                    break;
                }
                cursor = cursor.SuccessorNodes[0];
            }
            return retval;
        }

        /// <summary>
        /// Gets the primary path forward from the provided starting point node. The primary path is
        /// the path that is comprised on all of the highest-priority links out of each node encountered.
        /// This path consists of
        /// </summary>
        /// <param name="startPoint">The starting point.</param>
        /// <param name="stepsOnly">if set to <c>true</c> it returns steps only. Otherwise, it returns all nodes.</param>
        /// <returns>The primary path as a string.</returns>
        public static string GetPrimaryPathAsString(IPfcNode startPoint, bool stepsOnly) {
            List<IPfcNode> primaryPathList = GetPrimaryPath(startPoint, stepsOnly);
            string primaryPath = StringOperations.ToCommasAndAndedListOfNames<IPfcNode>(primaryPathList);

            return primaryPath;
        }

        public static Dictionary<IPfcNode, int> GetNodeDepths(ProcedureFunctionChart pfc) {
            Dictionary<IPfcNode, int> depths = new Dictionary<IPfcNode, int>();
            //List<IPfcStepNode> nodes = 
            //Debug.Assert( nodes.Count == 1, "A PFC was passed into PFCAnalyst.GetNodeDepths(...) that had " + nodes.Count + " finish steps. This is illegal." );
            IPfcNode node = pfc.GetFinishTransition( );
            GetDepth(node, ref depths);
            return depths;
        }

        private static int GetDepth(IPfcNode node, ref Dictionary<IPfcNode, int> depths) {
            if (depths.ContainsKey(node)) {
                return depths[node];
            } else if (node.PredecessorNodes.Count == 0) {
                depths[node] = 0;
                return 0;
            } else {
                int maxPred = int.MinValue;
                foreach (IPfcNode pred in node.PredecessorNodes) {
                    maxPred = Math.Max(maxPred, GetDepth(pred, ref depths));
                }
                depths[node] = maxPred + 1;
                return maxPred + 1;
            }
        }



        #endregion Structure-related Queries

        #region Target-Related Queries

        private static IPfcNode GetPrevParallelDivergenceNode(IPfcNode origin) {
            IPfcNode ppdn = GetPrevDivergenceNode(origin);
            if (ppdn != null) {
                while (ppdn.ElementType.Equals(PfcElementType.Step) || ppdn.PredecessorNodes.Count == 0) {
                    ppdn = GetPrevDivergenceNode(ppdn);
                    if (ppdn == null) {
                        break;
                    }
                }
            }
            return ppdn;
        }

        #endregion Target-Related Queries

        #region Dependency Checker Stuff
        
        ///// <summary>
        ///// Sorts the pfcNodes in the provided list in order of their execution dependencies.
        ///// </summary>
        ///// <param name="nodes">The nodes.</param>
        ///// <param name="topToBottom">if set to <c>true</c> [top to bottom].</param>
        ///// <returns></returns>
        //public static List<IPfcNode> SortByDependencies(List<IPfcNode> nodes, bool topToBottom) {
        //    DependencySorter<IPfcNode>.ParentGetter parentGetter = null;
        //    if (topToBottom) {
        //        parentGetter = new DependencySorter<IPfcNode>.ParentGetter(delegate(IPfcNode node) { return node.SuccessorNodes; });
        //    } else {
        //        parentGetter = new DependencySorter<IPfcNode>.ParentGetter(delegate(IPfcNode node) { return node.PredecessorNodes; });
        //    }
        //    DependencySorter<IPfcNode> depsorter = new DependencySorter<IPfcNode>(nodes, parentGetter);
        //    return depsorter.DependencySequence;
        //}

        //class DependencySorter<T> : IEnumerable<T> where T : Highpoint.Sage.SimCore.IHasName {

        //    public delegate List<T> ParentGetter(T t);

        //    private List<T> m_vertices;
        //    private Dictionary<T, Vertex<T>> m_dictionary;

        //    public DependencySorter(List<T> list, ParentGetter parentGetter) {
        //        m_dictionary = new Dictionary<T, Vertex<T>>();

        //        foreach (T t in list) {
        //            Vertex<T> vertex = new Vertex<T>(t, m_dictionary);
        //        }

        //        foreach (Vertex<T> v in m_dictionary.Values) {
        //            v.SetParents(parentGetter(v.Element));
        //        }

        //        System.Collections.ArrayList vertices = new System.Collections.ArrayList();
        //        foreach (Vertex<T> vt in m_dictionary.Values) {
        //            vertices.Add(vt);
        //        }
        //        //Dependencies.IDependencyVertex

        //        Dependencies.GraphSequencer gs = new Highpoint.Sage.Dependencies.GraphSequencer();
        //        gs.AddVertices(vertices);
        //        System.Collections.IList retval = gs.GetServiceSequenceList();

        //        m_vertices = new List<T>();
        //        foreach (Vertex<T> v in gs.GetServiceSequenceList()) {
        //            m_vertices.Add(v.Element);
        //        }
        //    }

        //    public List<T> DependencySequence {
        //        get {
        //            return m_vertices;
        //        }
        //    }

        //    class Vertex<T1> : Dependencies.IDependencyVertex where T1 : Highpoint.Sage.SimCore.IHasName {
        //        System.Collections.ArrayList m_parents;
        //        Dictionary<T1, Vertex<T1>> m_dictionary;
        //        T1 m_element;
        //        public Vertex(T1 t, Dictionary<T1, Vertex<T1>> dict) {
        //            m_element = t;
        //            m_dictionary = dict;
        //            m_dictionary.Add(m_element, this);
        //        }

        //        public T1 Element { get { return m_element; } }
        //        #region IDependencyVertex Members

        //        /// <summary>
        //        /// An IComparable that determines how otherwise equal vertices
        //        /// are to be sorted. Note that 'otherwise equal' means that the
        //        /// vertices are equal after a dependency analysis is done,
        //        /// and that both are independent of each other in the graph.
        //        /// </summary>
        //        /// <value></value>
        //        public IComparable SortCriteria {
        //            get { return m_element.Name; }
        //        }

        //        public System.Collections.ICollection ParentsList {
        //            get { return m_parents; }
        //        }

        //        public void SetParents(List<T1> parents) {

        //            m_parents = new System.Collections.ArrayList();

        //            foreach (T1 t in parents) {
        //                m_parents.Add(m_dictionary[t]);
        //            }
        //        }

        //        #endregion
        //    }

        //    #region IEnumerable<T> Members

        //    public IEnumerator<T> GetEnumerator() {
        //        return m_vertices.GetEnumerator();
        //    }

        //    #endregion

        //    #region IEnumerable Members

        //    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        //        return m_vertices.GetEnumerator();
        //    }

        //    #endregion
        //}

        #endregion Dependency Checker Stuff

        /// <summary>
        /// Creates a dictionary of transition to step mappings, such that by using this.Values, one can obtain steps
        /// that are recognizable to the user, and by using this[usersStepChoice], one can obtain the original transition.
        /// </summary>
        /// <param name="transitions">The IPfcNodeList that contains the transitions that govern the desired behaviors.</param>
        /// <param name="precedingStep">if set to <c>true</c> returns a preceding step of the transition, if false, a following step.</param>
        /// <returns>A dictionary of Step-to-Transition mappings.</returns>
        public static Dictionary<IPfcStepNode, IPfcTransitionNode> GetTransitionToStepMappings(PfcNodeList transitions, bool precedingStep) {
            Dictionary<IPfcStepNode, IPfcTransitionNode> retval = new Dictionary<IPfcStepNode, IPfcTransitionNode>();
            foreach (IPfcTransitionNode transition in transitions) {
                Debug.Assert(transition.ElementType.Equals(PfcElementType.Transition), "The nodes passed in to the GetTransitionToStepMappings must be Transitions.");

                if (precedingStep) {
                    if (transition.PredecessorNodes.Count > 0 && transition.PredecessorNodes[0].ElementType.Equals(PfcElementType.Transition)) {
                        retval.Add((IPfcStepNode)transition.PredecessorNodes[0], (IPfcTransitionNode)transition);
                    }
                } else {
                    if (transition.SuccessorNodes.Count > 0 && transition.SuccessorNodes[0].ElementType.Equals(PfcElementType.Transition)) {
                        retval.Add((IPfcStepNode)transition.SuccessorNodes[0], (IPfcTransitionNode)transition);
                    }
                }
            }
            return retval;
        }

        #region Private (Support) methods

        private static bool AllNodes(IPfcNode node) { return true; }
        private static bool StepsOnly(IPfcNode node) { return node.ElementType.Equals(PfcElementType.Step); }
        private static bool TransitionsOnly(IPfcNode node) { return node.ElementType.Equals(PfcElementType.Transition); }

        private static IPfcNode GetPrevDivergenceNode(IPfcElement element) {
            List<IPfcNode> beenThere = new List<IPfcNode>();
            if (element.ElementType.Equals(PfcElementType.Link)) {
                beenThere.Add(((IPfcLinkElement)element).Successor);
                return GetPrevDivergenceNode(((IPfcLinkElement)element).Successor, 0, beenThere);
            } else {
                beenThere.Add((IPfcNode)element);
                return GetPrevDivergenceNode((IPfcNode)element, 0, beenThere);
            }
        }

        private static IPfcNode GetPrevDivergenceNode(IPfcNode node, int convergenceLevel, List<IPfcNode> beenThere) {

            if (s_diagnostics) {
                Console.WriteLine("Looking for PDN's of " + node.Name + ".");
            }

            Debug.Assert(beenThere.Count > 0,
                "Calling GetPrevDivergenceNode with an empty 'beenThere' list can result in an infinite loop, if the prevDivergenceNode is in a loop.");

            #region # # #   Algorithm    # # #

            // ALGORITHM:
            // NOTE: Per Adam and Steve, this is a QUICK AND DIRTY algorithm, neither comprehensive nor
            // rigorous, and intended only to satisfy the limited use cases in Steve C's Modeler Requirements 
            // document. We have a  very short delivery timeline, and this QAD is a concession in order to 
            // meet that timeline. THIS IS A KLUDGE. DIRTY REMAINS LONG AFTER QUICK IS FORGOTTEN.
            // ---------------------------------------------------------------------------------------------
            // For each predecessor, P, to the argument 'node', 
            // 1.) If P has more than one successor, it's a divergence node, so decrement convergenceLevel.
            // 2.) If convergenceLevel is now -1, then P is the prev divergence node, so return P.
            // 3.) If P has more than one predecessor, it's a convergence node, so increment convergenceLevel.
            // 4.) Ask P for it's previous convergence node. This is done recursively.
            //     If its previous convergence node is null, or we've already seen this node, it either
            //     has no convergence node, or is itself a convergence node, which doesn't qualify for 
            // If we trace back through a loop to a node that is the origin (argument 'node') node, then
            // we return null, as that predecessor path has no previous divergence node.
            // ---------------------------------------------------------------------------------------------

            #endregion # # #   Algorithm    # # # 

            //if (beenThere.Contains(node)) { // We've looped back to a node we've visited before, so return null.
            //    return null; // Nothing promising here...
            //} Can't do this since we just added P in the preceding stack layer, it's now 'node', and we'll always return null.

            foreach (IPfcNode p in node.PredecessorNodes) {

                // If we haven't seen this node, remember it. If we have, then skip it.
                if (beenThere.Contains(p)) {
                    continue;
                } else {
                    beenThere.Add(p);
                }

                // If P has more than one successor, it's a divergence node, so decrement convergenceLevel.
                if (p.SuccessorNodes.Count > 1) {
                    convergenceLevel--;
                }

                // If convergenceLevel is now -1, then P is the prev divergence node, so return P.
                if (convergenceLevel == -1) {
                    return p;
                }

                // If P has more than one predecessor, it's a convergence node, so increment convergenceLevel.
                if (p.PredecessorNodes.Count > 1) {
                    convergenceLevel++;
                }

                IPfcNode pprime = GetPrevDivergenceNode(p, convergenceLevel, beenThere);

                if (pprime != null) {
                    return pprime;
                }

            }

            return null;
        }

        private static void Traverse(int nParPaths, IPfcNode currentNode, List<IPfcNode> beenThere, Dictionary<IPfcNode, int> hitCounts, ref IPfcNode convergenceNode) {

            // If we've been to this node already, the outbound path has rejoined,
            // and all downstream nodes have been accounted.
            if (beenThere.Contains(currentNode)) {
                return;
            }

            // If we've already identified the convergence node, we're done.
            if (convergenceNode != null) {
                return;
            }

            // We're at this node for the first time in this outbound path from divergence node.
            // Annotate it visited for this path, and increment its hit count.
            beenThere.Add(currentNode);
            if (!hitCounts.ContainsKey(currentNode)) {
                hitCounts.Add(currentNode, 0);
            }
            hitCounts[currentNode]++;

            if (hitCounts[currentNode] == nParPaths) {
                convergenceNode = currentNode;
            } else {
                // Proceed with the traversal.
                foreach (IPfcNode downstream in currentNode.SuccessorNodes) {
                    Traverse(nParPaths, downstream, beenThere, hitCounts, ref convergenceNode);
                }
            }
        }

        /// <summary>
        /// Gets the 'from' node's successors that are at the same depth that the 'from' node is at. Depth 
        /// increases when a path enters a parallel divergence, and decreases when it exits that divergence.
        /// Traversal terminates when it encounters a node it has encountered before - therefore, a way of 
        /// bounding the traversal is to add the terminal nodes into the 'beenThere' list before making the 
        /// initial call.
        /// </summary>
        /// <param name="beenThere">A list of the nodes already encountered in this traversal.</param>
        /// <param name="retval">The list of nodes that are zero-depth peers of the initial 'from' node.</param>
        /// <param name="from">The current 'from' node. This is the traversal node.</param>
        /// <param name="depth">The current traversal depth.</param>
        /// <param name="nodeFilter">The node filter - applied to all zero-depth nodes to see if they are
        /// acceptable to add to the retval list.</param>
        private static void GetZeroDepthSuccessors(PfcNodeList beenThere, PfcNodeList retval, IPfcNode from, int depth, Predicate<IPfcNode> nodeFilter) {
            if (s_diagnostics) {
                Console.WriteLine("Checking zero-depth successors from " + from.Name + ", which is at depth " + depth + ".");
            }
            if (beenThere.Contains(from)) {
                return;
            } else {
                beenThere.Add(from);
            }

            if (from.SuccessorNodes.Count > 1 && from.ElementType.Equals(PfcElementType.Transition) ) {
                depth++;
            }

            foreach (IPfcNode to in from.SuccessorNodes) {
                if (to.PredecessorNodes.Count > 1 && to.ElementType.Equals(PfcElementType.Transition)) {
                    depth--;
                }
                if (depth == 0 && nodeFilter(to) && !retval.Contains(to)) {
                    retval.Add(to);
                }
                GetZeroDepthSuccessors(beenThere, retval, to, depth, nodeFilter);
            }
        }

        /// <summary>
        /// Gets the 'from' node's predecessors that are at the same depth that the 'from' node is at. Depth 
        /// increases when a path enters a parallel divergence, and decreases when it exits that divergence.
        /// Traversal terminates when it encounters a node it has encountered before - therefore, a way of 
        /// bounding the traversal is to add the terminal nodes into the 'beenThere' list before making the 
        /// initial call.
        /// </summary>
        /// <param name="beenThere">A list of the nodes already encountered in this traversal.</param>
        /// <param name="retval">The list of nodes that are zero-depth peers of the initial 'from' node.</param>
        /// <param name="from">The current 'from' node. This is the traversal node.</param>
        /// <param name="depth">The current traversal depth.</param>
        /// <param name="nodeFilter">The node filter - applied to all zero-depth nodes to see if they are
        /// acceptable to add to the retval list.</param>
        private static void GetZeroDepthPredecessors(PfcNodeList beenThere, PfcNodeList retval, IPfcNode from, int depth, Predicate<IPfcNode> nodeFilter) {
            if (s_diagnostics) {
                Console.WriteLine("Checking zero-depth predecessors from " + from.Name + ", which is at depth " + depth + ".");
            }
            if (beenThere.Contains(from)) {
                return;
            } else {
                beenThere.Add(from);
            }

            if (from.PredecessorNodes.Count > 1 && from.ElementType.Equals(PfcElementType.Transition)) {
                depth--;
            }

            foreach (IPfcNode to in from.PredecessorNodes) {
                if (to.SuccessorNodes.Count > 1 && to.ElementType.Equals(PfcElementType.Transition) ) {
                    depth++;
                }
                if (depth == 0 && nodeFilter(to) && !retval.Contains(to)) {
                    retval.Add(to);
                }
                GetZeroDepthPredecessors(beenThere, retval, to, depth, nodeFilter);
            }
        }

        #endregion Private (Support) methods


        public static List<IPfcNode> GetNodesOnBroadestNonLoopingPath(ProcedureFunctionChart pfc, bool restoreOldLinkPriorities = true) {
            Dictionary<PfcLink, int?> oldVal = null;
            if (restoreOldLinkPriorities) {
                oldVal = new Dictionary<PfcLink, int?>();
                pfc.Links.ForEach(n => oldVal.Add((PfcLink)n, ((PfcLink)n).Priority));
            }
            AssignWeightsForBroadestNonLoopingPath(pfc);
            List<IPfcNode> retval = GetNodesOnPriorityPath(pfc);
            if (restoreOldLinkPriorities) {
                foreach (PfcLink link in oldVal.Keys) { link.Priority = oldVal[link]; }
            }
            return retval;
        }

        /// <summary>
        /// Gets the nodes on the already-established priority path. At a branch, the link with the highest priority
        /// is followed.
        /// </summary>
        /// <param name="pfc">The PFC.</param>
        /// <returns></returns>
        public static List<IPfcNode> GetNodesOnPriorityPath(ProcedureFunctionChart pfc) {

            // Do a breadth-first traversal, determining the sequence of operation steps executed in each unit.
            List<IPfcNode> sequence = new List<IPfcNode>();
            Queue<IPfcNode> working = new Queue<IPfcNode>();
            pfc.Nodes.ForEach(n => n.NodeColor = NodeColor.White);
            IPfcNode starter = pfc.GetStartSteps()[0];
            starter.NodeColor = NodeColor.Gray;
            working.Enqueue(starter);

            IPfcNode current;
            while (working.Count() > 0) {
                current = working.Dequeue();
                if ( s_diagnostics ) Console.WriteLine("Dequeueing {0}, leaving {1} elements in queue.", current.Name, working.Count());
                Debug.Assert(current.NodeColor == NodeColor.Gray);
                if (current.Successors.Count() == 0) {
                    current.NodeColor = NodeColor.Black; // It's the last one.
                    sequence.Add(current);
                    if (s_diagnostics) Console.WriteLine("\tIt's the last one in the queue.");
                } else {
                    // If it's a step node, then it's a pass-in, or it's a serial convergence.
                    // Either way, we only need one predecessor to be black. And it will be.
                    if (current is IPfcStepNode || current.PredecessorNodes.TrueForAll(n => n.NodeColor == NodeColor.Black)) {
                        if (s_diagnostics) Console.WriteLine("\tAdvancing.");
                        current.NodeColor = NodeColor.Black;
                        // We can advance.
                        if (current.SuccessorNodes.Count() == 1 || current is IPfcTransitionNode) {
                            // Single follower, or parallel divergence, all successors are enqueued.
                            foreach (IPfcNode next in current.SuccessorNodes) {
                                if (next.NodeColor == NodeColor.White) {
                                    next.NodeColor = NodeColor.Gray;
                                    if (s_diagnostics) Console.WriteLine("\t\tEnqueueing {0}.", next.Name);
                                    working.Enqueue(next);
                                }
                            }
                        } else { // Serial divergence - enqueue only the highest priority node.
                            IPfcNode next = current.Successors.OrderByDescending(n => n.Priority).First().Successor;
                            if (next.NodeColor == NodeColor.White) {
                                next.NodeColor = NodeColor.Gray;
                                if (s_diagnostics) Console.WriteLine("\t\tEnqueueing {0}.", next.Name);
                                working.Enqueue(next);
                            }
                            //System.Diagnostics.Debug.Assert(next.SuccessorNodes.Count() < 2); // Duality violation, if not.
                        }
                        current.NodeColor = NodeColor.Black;
                        if (s_diagnostics) Console.WriteLine("\t->Logging execution of {0}.", current.Name);
                        sequence.Add(current);
                    } else {
                        working.Enqueue(current); // All preds are not black. Must try again later.
                    }
                }
            }

            return sequence;
        }
    }
}