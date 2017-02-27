/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Resources
{
	/// <summary>
	/// ResourceException is the base class for other resource exceptions - it simply contains the message, the manager, the request, and the resource involved.
	/// </summary>
	public class ResourceException : Exception {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceException"/> class.
        /// </summary>
        /// <param name="msg">The message to be reported with the exception.</param>
        /// <param name="resourceRequest">The resource request.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="resourceManager">The resource manager.</param>
        protected ResourceException(string msg, IResourceRequest resourceRequest, IResource resource, IResourceManager resourceManager):base(msg){
            ResourceRequest = resourceRequest;
            Resource = resource;
            ResourceManager = resourceManager;
        }

        /// <summary>
        /// Gets the resource manager.
        /// </summary>
        /// <value>The resource manager.</value>
        public IResourceManager ResourceManager { get; }

        /// <summary>
        /// Gets the resource request.
        /// </summary>
        /// <value>The resource request.</value>
        public IResourceRequest ResourceRequest { get; }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        /// <value>The resource.</value>
        public IResource        Resource { get; }
	}

    /// <summary>
    /// Class ResourcePoolInsufficientException is fired when there are insufficient resources available to a resource manager to ever satisfy the request.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Resources.ResourceException" />
    public class ResourcePoolInsufficientException : ResourceException {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolInsufficientException"/> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager.</param>
        /// <param name="request">The request.</param>
        public ResourcePoolInsufficientException(IResourceManager resourceManager, IResourceRequest request):
            base("Insufficient resources available to pool " + resourceManager.Name + " to ever satisfy the request.",request,null,resourceManager){}
    }


    /// <summary>
    /// Class ResourceMismatchException is fired when code tries to release or unreserve a resource with a ResourceRequest that doesn't own it.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Resources.ResourceException" />
    public class ResourceMismatchException : ResourceException {
        public enum MismatchType { Release, UnReserve }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceMismatchException"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="type">The type (release or unreserve).</param>
        public ResourceMismatchException(IResourceRequest request, IResource resource, MismatchType type):
            base("Trying to release or unreserve a resource with a ResourceRequest that doesn't own it.",request,resource,resource.Manager){
            Operation = type;
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <value>The operation.</value>
        public MismatchType Operation { get; }
    }
}
