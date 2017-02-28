/* This source code licensed under the GNU Affero General Public License */
/*###############################################################################
#  Material previously published at http://builder.com.com/5100-6387_14-5025380.html
#  Highpoint Software Systems is a Wisconsin Limited Liability Corporation.
###############################################################################*/
using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.Dependencies {

    /// <summary>
    /// This interface is implemented by the class whose objects are to
    /// act as vertices in the Directed Acyclic Graph.
    /// </summary>
    public interface IDependencyVertex {

        /// <summary>
        /// An IComparable that determines how otherwise equal vertices
        /// are to be sorted. Note that 'otherwise equal' means that the 
        /// vertices are equal after a dependency analysis is done,
        /// and that both are independent of each other in the graph.
        /// </summary>
        IComparable SortCriteria{get;}

        /// <summary>
        /// This is a list of other vertices, that implement 
        /// IDependencyVertex as well, upon which this object depends. 
        /// These are all vertices that may need to be notified 
        /// before this implementer can generate a stable output. In other
        /// words, they are vertices that provide input to this vertex.
        /// </summary>
        ICollection PredecessorList { get; }

    }
}