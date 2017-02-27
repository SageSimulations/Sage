/* This source code licensed under the GNU Affero General Public License */
/*###############################################################################
#  Material previously published at http://builder.com.com/5100-6387_14-5025380.html
###############################################################################*/

using Trace = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.Dependencies {
    /// <summary>
    /// Implemented by an object that perform a sequence determination
    /// across a collection of vertices that implement IDependencyVertex.
    /// </summary>
    public interface ISequencer {

        /// <summary>
        /// Call this with a collection of IDependencyVertex objects to 
        /// add them to the sequencer for consideration next time there 
        /// is a request for a service sequence list.
        /// </summary>
        /// <param name="vertices">A collection of IDependencyVertex 
        /// objects</param>
        void AddVertices(ICollection vertices);

        /// <summary>
        /// Call this with an IDependencyVertex object to add it to the 
        /// sequencer for consideration next time there is a request for
        /// a service sequence list.
        /// </summary>
        /// <param name="vertex">An IDependencyVertex object</param>
        void AddVertex(IDependencyVertex vertex);

        /// <summary>
        /// Returns an ordered list of vertices in which the order is the 
        /// order in which the vertices should be serviced.
        /// </summary>
        IList GetServiceSequenceList();
    }

}
