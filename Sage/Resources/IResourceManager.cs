/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using Highpoint.Sage.SimCore;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace Highpoint.Sage.Resources {
    /// <summary>
    /// Interface IResourceManager is implemented by an object that manages the 
    /// granting and recovery of resources. It executes a protocol for finding 
    /// the best resource for a given resource request.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.SimCore.IModelObject" />
    public interface IResourceManager : IModelObject {

        /// <summary>
        /// Tries to reserve a resource that satisfies the specified resource request.
        /// </summary>
        /// <param name="resourceRequest">The resource request.</param>
        /// <param name="blockAwaitingAcquisition">if set to <c>true</c> the event thread will block awaiting reservation of a resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Reserve(IResourceRequest resourceRequest, bool blockAwaitingAcquisition);

        /// <summary>
        /// Unreserves the resource attached to the specified resource request.
        /// </summary>
        /// <param name="resourceRequest">The resource request.</param>
        void Unreserve(IResourceRequest resourceRequest);

        /// <summary>
        /// Tries to acquire a resource that satisfies the specified resource request.
        /// </summary>
        /// <param name="resourceRequest">The resource request.</param>
        /// <param name="blockAwaitingAcquisition">if set to <c>true</c> the event thread will block awaiting acquisition of a resource.</param>
        /// <returns><c>true</c> if a satisfactory resource was acquired, <c>false</c> otherwise.</returns>
        bool Acquire(IResourceRequest resourceRequest, bool blockAwaitingAcquisition);

        /// <summary>
        /// Releases the resource attached (by previous acqusition) to the specified resource request.
        /// </summary>
        /// <param name="resourceRequest">The resource request.</param>
        void Release(IResourceRequest resourceRequest);

        /// <summary>
        /// Gets the resources owned by this Resource Manager.
        /// </summary>
        /// <value>The resources.</value>
        IList Resources { get; }

        /// <summary>
        /// Gets a value indicating whether this resource manager supports prioritized requests.
        /// </summary>
        /// <value><c>true</c> if [supports prioritized requests]; otherwise, <c>false</c>.</value>
        bool SupportsPrioritizedRequests { get; }

        /// <summary>
        /// Fired when a resource request is received.
        /// </summary>
		event ResourceStatusEvent ResourceRequested;

        /// <summary>
        /// Fired when a resource is acquired and thereby removed from the pool.
        /// </summary>
		event ResourceStatusEvent ResourceAcquired;

        /// <summary>
        /// Fired when a resource is released back into the pool.
        /// </summary>
        event ResourceStatusEvent ResourceReleased;

        /// <summary>
        /// Fired when a resource is added to the pool.
        /// </summary>
        event ResourceManagerEvent ResourceAdded;

        /// <summary>
        /// Fired when a resource is removed from the pool.
        /// </summary>
        event ResourceManagerEvent ResourceRemoved;

        /// <summary>
        /// Gets or sets the access regulator, which is an object that can allow or deny
        /// individual ResourceRequests access to specified resources.
        /// </summary>
        /// <value>The access regulator.</value>
        IAccessRegulator AccessRegulator { set; get; }
    }
}
