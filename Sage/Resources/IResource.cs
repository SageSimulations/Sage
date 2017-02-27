/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
// ReSharper disable UnusedMemberInSuper.Global
#pragma warning disable 1587

/// <summary>
/// A resource is anything that is used by a process, and for which that use must be granted. If its availability can affect the
/// progress through a process (such as a batch or campaign), it is a resource.
/// <para/>Resources have two orthogonal classifications. Resources are discrete or continuous, and resources are consumable or persistent. 
/// <para/>A continuous resource is a resource that can be acquired and released fractionally, and for which the acquired resource does not
/// have an identity separate from the whole, such as acquiring 24.5 lbs of steam from a 200 lb steam header, or 40 kg of sodium nitrite
/// from inventory.
/// <para/>A discrete resource is a resource that can only be acquired 1 at a time (or 2 at a time, etc. – e.g. in integral quantities),
/// and for which each integrally acquired resource has an identity and state, such as a piece of equipment, a barrel or a mixer.
/// <para/>A consumable resource is a resource for which consumption and provision events are decoupled – that is, once a consumable
/// resource has been removed from the pool, there is no guarantee that it will be returned to the pool. Typically, consumable resources
/// are taken from a pool, and never returned. A different entity or set of entities is typically responsible for reprovisioning the pool,
/// and the resources with which it reprovisions are unrelated to those formerly removed from the pool. While a consumable resource is not
/// persistent, the pool (probably an Inventory object) is persistent.
/// <para/>A persistent resource is a resource that is not consumed. It is taken from the pool, and later returned to the pool.
/// <para/>Resource Object Model
/// <para/>Creating and working with resources requires three types of entities – ResourceManager, Resource and ResourceRequest objects.
/// ResourceManager objects are any that correctly implement IResourceManager, Resource objects are any that correctly implement
/// IResource, and ResourceRequest object are any that correctly implement IResourceRequest. For convenience, the framework provides
/// several implementations. Resource, ResourceManager and ResourceRequest are implementations of their respective interfaces, and
/// SelfManagingResource is a class that implements both IResourceManager and IResource, and allows easy representation of an object
/// that need not be part of a pool of related resources.
/// <para/>Resource Acquisition
/// <para/>Resources are assigned to one or more pools, which conceptually are entities that are implementers of IResourceManager.
/// A resource pool is a resource manager, from which a resource user will request a resource, through an object (essentially an agent).
/// The resource desirer submits a resource request to the appropriate resource pool. The resource manager begins handing the resources
/// to the request one by one, and the request scores each one. The request may examine any attribute of the resource (in addition to
/// anything else the request is coded to consider), but must score each object. The score is a double. Double.MinValue means &quot;No,
/// this is totally unsuitable,&quot; double.MaxValue means &quot;This is perfect. Give it to me now – I don’t need to see any more.&quot;
/// Any double in between is seen as a scalar value judgment, and if no resources received a double.MaxValue score, then after all
/// resources have been scored, the one with the highest value is granted.
/// <para/>
/// In general an intended resource user (the requester) creates a resource request and submits it to a resource manager that has
/// responsibility for the type (or instance) of resource it desires. In processing the request, the resource manager begins handing
/// the resources to the request one by one, and the request scores each one. The request may examine any attribute of the resource
/// (in addition to anything else the request is coded to consider), but must score each object. The score is a double. Double.MinValue
/// means &quot;No, this is totally unsuitable,&quot; double.MaxValue means &quot;This is perfect. Give it to me now – I don’t need to
/// see any more.&quot; Any double in between is seen as a scalar value judgment. During this processing the ResourceManager reserves
/// any suitable resources one at a time, unreserving them only after reserving the next one if a better one is found, until finally
/// when the best one has been found, it is acquired on behalf of the requster. If no suitable resource is requested and the request was
/// made with the blocking flag set to true, the resource manager holds the request (and the caller's thread) until the resource can
/// be granted. If the request was made with the blocking flag set to false, the call returns with the ResourceRequest's ResourceObtained
/// property set to null, and no further attempt is made by the ResourceManager to satisfy the request.
/// 
/// The requester uses the resource, and when it is done doing so, may or may not (depending on if the resource is replenishable)
/// releases the resource back to the pool.
/// <para/>ResourceEvents allow the application to gather information on resource acquisitions, releases, etc. ResourceEventRecords
/// persist these events, and ResourceEventRecordFilters allow perusal of collections of ResourceEventRecords.
/// <para/>Access regulators may be used to allow a resource manager to require a key be submitted with a resource request, and to
/// permit that resource manager to allow or deny access based on that key. This is useful for earmarking - a technique where resources
/// are &quot;earmarked&quot; for use by a specific class of requestors. For example, a campaign ( a series of batches) begins, and all
/// vessels that are used by that campaign are earmarked for that campaign, so that no other campaign may acquire the vessels, thereby
/// avoiding a need to clean that vessel in the middle of a batch.
/// <para/>ResourceTrackers are used to track the levels and events associated with a specific resource. A ResourceTrackerAggregator
/// collects the data from more than one resource.
/// </summary>
namespace Highpoint.Sage.Resources {

    /// <summary>
    /// A delegate implemented by an event that is fired by a resource.
    /// </summary>
    /// <param name="resource">The resource, typically, to whom the event is taking place.</param>
	public delegate void ResourceEvent(IResource resource);

    /// <summary>
    /// A delegate implemented by an event that is fired by a resource, but related to a resource request.
    /// </summary>
    /// <param name="irr">The resource request within whose scope the event is taking place.</param>
    /// <param name="resource">The resource, typically, to whom the event is taking place.</param>
    public delegate void ResourceStatusEvent(IResourceRequest irr, IResource resource);
    
    /// <summary>
    /// A delegate implemented by an event that is fired, usually, by a resource manager.
    /// </summary>
    /// <param name="irm">The resource manager within whose scope the event is taking place.</param>
    /// <param name="resource">The resource, typically, to whom the event is taking place.</param>
    public delegate void ResourceManagerEvent(IResourceManager irm, IResource resource);
	
    /// <summary>
    /// Implemented by a method that can generate or return a Resource Request.
    /// </summary>
    /// <returns>a Resource Request.</returns>
    public delegate IResourceRequest ResourceRequestSource();

    /// <summary>
    /// Implemented by a class (usually a resource) that has a quantity that can be considered
    /// capacity.
    /// </summary>
	public interface IHasCapacity {

		/// <summary>
		/// The capacity of this resource that will be in effect if the resource experiences a reset.
		/// </summary>
		double InitialCapacity { get; }

		/// <summary>
		/// The current capacity of this resource - how much 'Available' can be, at its highest value.
		/// </summary>
		double Capacity { get; }

		/// <summary>
		/// The amount of a resource that can be acquired over and above the amount that is actually there.
		/// It is illegal to set PermissibleOverbook quantity on an atomic resource, since atomicity implies
		/// that all or none are granted anyway.
		/// </summary>
		double PermissibleOverbook { get; }

		/// <summary>
		/// The quantity of this resource that will be available if the resource experiences a reset.
		/// </summary>
		double InitialAvailable { get; }

		/// <summary>
		/// How much of this resource is currently available to service requests.
		/// </summary>
		double Available { get; }
	}

    /// <summary>
    /// Implemented by a class (usually a resource) that has a quantity that can be considered
    /// capacity. In the case of IHasControllableCapacity, though, the current capacity (Available)
    /// and maximum capacity (Capacity) can be overridden.
    /// and 
    /// </summary>
    public interface IHasControllableCapacity {

		/// <summary>
		/// The current capacity of this resource - how much 'Available' can be at its highest value.
		/// </summary>
		double Capacity { get; set; }

		/// <summary>
		/// The amount of a resource that can be acquired over and above the amount that is actually there.
		/// It is illegal to set PermissibleOverbook quantity on an atomic resource, since atomicity implies
		/// that all or none are granted anyway.
		/// </summary>
		double PermissibleOverbook { get; set; }

		/// <summary>
		/// How much of this resource is currently available to service requests.
		/// </summary>
		double Available { get; set; }
	}

    /// <summary>
    /// An implementer of IResource is an object that can act as a resource.
    /// </summary>
	public interface IResource : IModelObject, IHasCapacity {

        /// <summary>
        /// Gets or sets the manager of the resource.
        /// </summary>
        /// <value>The manager.</value>
        IResourceManager Manager { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is discrete. A discrete resource is allocated in integral amounts, such as cartons or drums.
        /// </summary>
        /// <value><c>true</c> if this instance is discrete; otherwise, <c>false</c>.</value>
        bool IsDiscrete { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is persistent. A persistent resource is returned to the pool after it is used.
        /// </summary>
        /// <value><c>true</c> if this instance is persistent; otherwise, <c>false</c>.</value>
        bool IsPersistent { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is atomic. And atomic resource is allocated all-or-none, such as a vehicle.
        /// </summary>
        /// <value><c>true</c> if this instance is atomic; otherwise, <c>false</c>.</value>
        bool IsAtomic { get; }

        /// <summary>
        /// Resets this instance, returning it to its initial capacity and availability.
        /// </summary>
        void Reset();

        /// <summary>
        /// Reserves the specified request. Removes it from availability, but not from the pool. This is typically an intermediate state held during resource negotiation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if the resource was successfully reserved, <c>false</c> otherwise.</returns>
        bool Reserve ( IResourceRequest request );

        /// <summary>
        /// Unreserves the specified request. Returns it to availability.
        /// </summary>
        /// <param name="request">The request.</param>
        void Unreserve ( IResourceRequest request );

        /// <summary>
        /// Acquires the specified request. Removes it from availability and from the resource pool.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if if the resource was successfully acquired, <c>false</c> otherwise.</returns>
        bool Acquire ( IResourceRequest request );

        /// <summary>
        /// Releases the specified request. Returns it to availability and the resource pool.
        /// </summary>
        /// <param name="request">The request.</param>
        void Release ( IResourceRequest request );

        /// <summary>
        /// Occurs when this resource has been requested.
        /// </summary>
        event ResourceStatusEvent RequestEvent;

        /// <summary>
        /// Occurs when this resource has been reserved.
        /// </summary>
		event ResourceStatusEvent ReservedEvent;

        /// <summary>
        /// Occurs when this resource has been unreserved.
        /// </summary>
		event ResourceStatusEvent UnreservedEvent;

        /// <summary>
        /// Occurs when this resource has been acquired.
        /// </summary>
		event ResourceStatusEvent AcquiredEvent;

        /// <summary>
        /// Occurs when this resource has been released.
        /// </summary>
		event ResourceStatusEvent ReleasedEvent;

	}

    /// <summary>
    /// Implemented by a model that manages resources.
    /// </summary>
    public interface IModelWithResources {

        /// <summary>
        /// Must be called by the creator when a new resource is created.
        /// </summary>
        /// <param name="resource">The resource.</param>
        void OnNewResourceCreated(IResource resource);

        /// <summary>
        /// Event that is fired when a new resource has been created.
        /// </summary>
        event ResourceEvent ResourceCreatedEvent;
    }
}
