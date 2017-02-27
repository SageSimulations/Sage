/* This source code licensed under the GNU Affero General Public License */

using Trace = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.Graphs {

#if DEBUG
	/// <summary>
	/// PMData is Post-Mortem data, data that indicates which vertices and edges
	/// have fired in a particular graph execution. It does not exist in a non-
	/// debug build.
	/// </summary>
    public class PmData {
        /// <summary>
        /// A list of the vertices in a graph that were fired in a given run of  the graph.
        /// </summary>
        public ArrayList VerticesFired = new ArrayList();
        /// <summary>
        /// A list of the edges in a graph that were fired in a given run of  the graph.
        /// </summary>
        public ArrayList EdgesFired = new ArrayList();
    }
#endif

    /// <summary>
    /// An enumeration, the members of which describe types of structure change.
    /// </summary>
	public enum StructureChangeType {
         /// <summary>
         /// A post edge was added.
         /// </summary>
		AddPostEdge, 
        /// <summary>
        /// A predecessor edge was added.
        /// </summary>
		AddPreEdge, 
        /// <summary>
        /// A predecessor edge was removed.
        /// </summary>
		RemovePreEdge, 
        /// <summary>
        /// A post edge was removed.
        /// </summary>
		RemovePostEdge, 
        /// <summary>
        /// A costart was added.
        /// </summary>
		AddCostart, 
        /// <summary>
        /// A co-finish was added.
        /// </summary>
		AddCofinish, 
        /// <summary>
        /// A co-start was removed.
        /// </summary>
		RemoveCostart,
        /// <summary>
        /// A co-finish was removed.
        /// </summary>
		RemoveCofinish,
        /// <summary>
        /// A child edge was added.
        /// </summary>
		AddChildEdge, 
        /// <summary>
        /// A child edge was removed.
        /// </summary>
		RemoveChildEdge,
        /// <summary>
        /// A new synchronizer was added.
        /// </summary>
		NewSynchronizer, 
        /// <summary>
        /// An unknown change was made.
        /// </summary>
		Unknown 
	}

    /// <summary>
    /// A class that holds a collection of static methods which provide abstraced data about StructureChangeTypes.
    /// </summary>
	public class StructureChangeTypeSvc {
		private StructureChangeTypeSvc(){}
        /// <summary>
        /// Determines whether StructureChangeType was a pre-edge change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a predecessor-edge.
        /// </returns>
		public static bool IsPreEdgeChange(StructureChangeType sct){
			return sct.Equals(StructureChangeType.AddPreEdge) || sct.Equals(StructureChangeType.RemovePreEdge);
		}
        /// <summary>
        /// Determines whether StructureChangeType was a post-edge change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a successor-edge.</returns>
		public static bool IsPostEdgeChange(StructureChangeType sct){
			return sct.Equals(StructureChangeType.AddPostEdge) || sct.Equals(StructureChangeType.RemovePostEdge);
		}

        /// <summary>
        /// Determines whether the StructureChangeType signifies a co-start change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a co-start.</returns>
        public static bool IsCostartChange(StructureChangeType sct) {
			return sct.Equals(StructureChangeType.AddCostart) || sct.Equals(StructureChangeType.RemoveCostart);
		}
        /// <summary>
        /// Determines whether the StructureChangeType signifies a co-finish change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a co-finish.</returns>
        public static bool IsCofinishChange(StructureChangeType sct) {
			return sct.Equals(StructureChangeType.AddCofinish) || sct.Equals(StructureChangeType.RemoveCofinish);
		}
        /// <summary>
        /// Determines whether the StructureChangeType signifies a change in a child.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies a change in a child.</returns>
        public static bool IsChildChange(StructureChangeType sct) {
			return sct.Equals(StructureChangeType.AddChildEdge) || sct.Equals(StructureChangeType.RemoveChildEdge);
		}
        /// <summary>
        /// Determines whether the StructureChangeType signifies a change in a synchronizer.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies a change in a synchronizer.</returns>
        public static bool IsSynchronizerChange(StructureChangeType sct) {
			return sct.Equals(StructureChangeType.NewSynchronizer);
		}

        /// <summary>
        /// Determines whether the StructureChangeType signifies an addition.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies an addition.</returns>
        public static bool IsAdditionChange(StructureChangeType sct) {
			return sct.Equals(StructureChangeType.AddPostEdge) 
				|| sct.Equals(StructureChangeType.AddPreEdge)
				|| sct.Equals(StructureChangeType.AddCostart)
				|| sct.Equals(StructureChangeType.AddCofinish)
				|| sct.Equals(StructureChangeType.NewSynchronizer)
				|| sct.Equals(StructureChangeType.AddChildEdge);
		}

        /// <summary>
        /// Determines whether the StructureChangeType signifies a removal.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies a removal.</returns>
        public static bool IsRemovalChange(StructureChangeType sct) {
			return sct.Equals(StructureChangeType.RemovePostEdge) 
				|| sct.Equals(StructureChangeType.RemovePreEdge)
				|| sct.Equals(StructureChangeType.RemoveCostart)
				|| sct.Equals(StructureChangeType.RemoveCofinish)
				|| sct.Equals(StructureChangeType.RemoveChildEdge);
		}
	}

	/// <summary>
	/// Implemented by events that are fired when graph structure changes.
	/// </summary>
	public delegate void StructureChangeHandler(object obj, StructureChangeType sct, bool isPropagated);

    /// <summary>
    /// Implemented by an object that is a part of a graph structure.
    /// </summary>
	public interface IPartOfGraphStructure {
        /// <summary>
        /// Fired when the structure of the graph changes.
        /// </summary>
		event StructureChangeHandler StructureChangeHandler;
		//void PropagateStructureChange(object obj, StructureChangeType sct, bool isPropagated);
	}
}
