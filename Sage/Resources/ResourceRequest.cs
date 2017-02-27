/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Resources {
	public abstract class ResourceRequest : IResourceRequest {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRequest"/> class.
        /// </summary>
        /// <param name="quantityDesired">The quantity of resource desired.</param>
        protected ResourceRequest(double quantityDesired){
            Status = RequestStatus.Free;
			QuantityDesired = quantityDesired;
			AsyncGrantConfirmationCallback = DefaultGrantConfirmationRequest_Refuse;

            // TODO: Fix bad design - overridable method call in constructor.
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
			Replicate = GetDefaultReplicator();
		}

        #region Implementation of IResourceRequest
        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        public abstract double GetScore(IResource resource);

        #region Priority Management
        /// <summary>
        /// An event that is fired if the priority of this request is changed.
        /// </summary>
        public event RequestPriorityChangeEvent PriorityChangeEvent;
		private double m_priority;
        /// <summary>
        /// An indication of the priority of this request. A larger number indicates a higher priority.
        /// </summary>
        /// <value>The priority.</value>
        public double Priority {
			get { return m_priority; }
			set {
				double oldValue = m_priority;
				m_priority = value;
			    PriorityChangeEvent?.Invoke(this,oldValue,m_priority);
			}
		}
        #endregion

        /// <summary>
        /// Reserves the specified resource manager.
        /// </summary>
        /// <param name="resourceManager">The resource manager.</param>
        /// <param name="blockAwaitingAcquisition">if set to <c>true</c> [block awaiting acquisition].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ApplicationException">Acquire API on resource request was called with neither an explicit nor a default resource manager.</exception>
        public bool Reserve(IResourceManager resourceManager, bool blockAwaitingAcquisition) {
			if ( resourceManager == null && DefaultResourceManager == null ) {
				throw new ApplicationException("Acquire API on resource request was called with neither an explicit nor a default resource manager.");
			}

			if ( resourceManager == null ) resourceManager = DefaultResourceManager;
			if ( resourceManager.Reserve(this,blockAwaitingAcquisition) ){
				ResourceObtainedFrom = resourceManager;
				return true;
			}
			return false;
		}

        /// <summary>
        /// Acquires a resource from the specified resource manager, or the provided default manager,
        /// if none is provided in this call. If the request has already successfully reserved a resource,
        /// then the reservation is revoked and the acquisition is honored in one atomic operation.
        /// </summary>
        /// <param name="resourceManager">The resource manager from which the resource is desired. Can be null, if a default manager has been provided.</param>
        /// <param name="blockAwaitingAcquisition">If true, this call blocks until the resource is available.</param>
        /// <returns>true if the acquisition was successful, false otherwise.</returns>
        /// <exception cref="ApplicationException">Acquire API on resource request was called with neither an explicit nor a default resource manager.</exception>
        public bool Acquire(IResourceManager resourceManager, bool blockAwaitingAcquisition) {
			if ( resourceManager == null && DefaultResourceManager == null ) {
				throw new ApplicationException("Acquire API on resource request was called with neither an explicit nor a default resource manager.");
			}

			if ( resourceManager == null ) resourceManager = DefaultResourceManager;
			
			if ( resourceManager.Acquire(this,blockAwaitingAcquisition) ){
				ResourceObtainedFrom = resourceManager;
				return true;
			}
			return false;
		}

        /// <summary>
        /// This is a key that will be used to see if the resource manager is allowed to
        /// grant a given resource to the requester. It is used in conjunction with resource earmarking.
        /// (See IAccessRegulator)
        /// </summary>
        /// <value>The key.</value>
        public object Key { set; get; }

        /// <summary>
        /// This is a reference to the resource manager that granted access to the resource.
        /// </summary>
        /// <value>The resource obtained from.</value>
        public IResourceManager ResourceObtainedFrom { get; set; }

	    /// <summary>
        /// Releases the resource previously obtained by this ResourceRequest.
        /// </summary>
        public void Unreserve() {
			ResourceObtainedFrom.Unreserve(this);
		}

        /// <summary>
        /// Releases the resource previously obtained by this ResourceRequest.
        /// </summary>
        public void Release() {
			IResourceManager tmp = ResourceObtainedFrom;
			ResourceObtainedFrom = null;
			tmp.Release(this); // If there are others waiting, then m_ResourceManager will be reassigned in here.
		}

        /// <summary>
        /// Chooses a resource from the specified candidates.
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <returns>IResource.</returns>
        // ReSharper disable once UnusedParameter.Global
        // ReSharper disable once VirtualMemberNeverOverriden.Global
        public virtual IResource Choose( IList candidates){ return null; }

        /// <summary>
        /// This property represents the quantity this request is to remove from the resource's
        /// 'Available' capacity.
        /// </summary>
        /// <value>The quantity desired.</value>
        public double QuantityDesired {
            [System.Diagnostics.DebuggerStepThrough]
		    get;
        }

        /// <summary>
        /// This property represents the quantity this request actually removed from the resource's
        /// 'Available' capacity. It is filled in by the granting authority.
        /// </summary>
        /// <value>The quantity obtained.</value>
        public double QuantityObtained {
            [System.Diagnostics.DebuggerStepThrough]
	        get;
            [System.Diagnostics.DebuggerStepThrough]
	        set;
        }

        /// <summary>
        /// If non-null, this infers a specific, needed resource.
        /// </summary>
        /// <value>The required resource.</value>
        public IResource RequiredResource {
            [System.Diagnostics.DebuggerStepThrough]
	        get;
            [System.Diagnostics.DebuggerStepThrough]
	        set;
        }

        /// <summary>
        /// This is a reference to the object requesting the resource.
        /// </summary>
        /// <value>The requester.</value>
        public IHasIdentity Requester
	    {
	        [System.Diagnostics.DebuggerStepThrough] get; [System.Diagnostics.DebuggerStepThrough] set;
        }

        /// <summary>
        /// This is a reference to the actual resource that was obtained.
        /// </summary>
        /// <value>The resource obtained.</value>
        public IResource ResourceObtained {
            [System.Diagnostics.DebuggerStepThrough]
		    get;
            [System.Diagnostics.DebuggerStepThrough]
		    set;
        }

	    /// <summary>
        /// Gets the status of this resource request.
        /// </summary>
        /// <value>The status.</value>
        public RequestStatus Status { get; set; }

        /// <summary>
        /// This is the resource selection strategy that is to be used by the resource
        /// manager to select the resource to be granted from the pool of available
        /// resources.
        /// </summary>
        /// <value>The resource selection strategy.</value>
        public virtual ResourceSelectionStrategy ResourceSelectionStrategy => null;

	    /// <summary>
		/// This method is called if the resource request is pending, and gets aborted, for
		/// example due to resource deadlocking. It can be null, in which case no deadlock
		/// detection is provided for the implementing type of ResourceRequest.
        /// </summary>
		public DetachableEventAbortHandler AbortHandler => OnRequestAborting;

	    /// <summary>
		/// Typically fires as a result of the RequestAbortHandler being called. In that method,
		/// it picks up the IResourceRequest identity, and is passed on through this event, which
		/// includes the IResourceRequest.
		/// </summary>
		public event ResourceRequestAbortEvent ResourceRequestAborting;

	    /// <summary>
		/// Creates a fresh replica of this resource request, without any of the in-progress data. This replica can
		/// be used to generate another, similar resource request that can acquire its own resource.
		/// </summary>
	    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
		public ResourceRequestSource Replicate { get; set; }

        /// <summary>
        /// Gets the default replicator to be used in creating a copy of this resource request. Might look, for example,
        /// like this: 
        /// <code>protected override ResourceRequestSource GetDefaultReplicator()
        ///       {
        ///            return () => new VehicleRequest(m_seatsNeeded) { DefaultResourceManager = DefaultResourceManager };
        /// }
        /// </code>
        /// </summary>
        /// <returns>ResourceRequestSource.</returns>
        protected abstract ResourceRequestSource GetDefaultReplicator();

		/// <summary>
		/// This is the resource manager from which a resource is obtained if none is provided in the reserve or
		/// acquire API calls.
		/// </summary>
		public IResourceManager DefaultResourceManager { get; set; }

	    /// <summary>
		/// This callback is called when a request, made with a do-not-block specification, that was initially
		/// refused, is finally deemed grantable, and provides the callee (presumably the original requester) 
		/// with an opportunity to say, "No, I don't want that any more", or perhaps to get ready for receipt
		/// of the resource in question.
		/// </summary>
		public ResourceRequestCallback AsyncGrantConfirmationCallback { get; set; }

	    /// <summary>
		/// Called after a resource request is granted asynchronously.
		/// </summary>
		public ResourceRequestCallback AsyncGrantNotificationCallback { get; set; }

	    /// <summary>
		/// Data maintained by this resource request on behalf of the requester.
		/// </summary>
		public object UserData { get; set; }

	    // ReSharper disable once VirtualMemberNeverOverriden.Global
	    protected virtual bool DefaultGrantConfirmationRequest_Refuse(IResourceRequest request) => false;

        #endregion

        #region IComparable implementation
        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order.</returns>
        public int CompareTo(object obj){
			IResourceRequest irr = (IResourceRequest)obj;
			int retval = Comparer.Default.Compare(Priority,irr.Priority);
			if ( retval == 0 ) retval = Comparer.Default.Compare(QuantityDesired,irr.QuantityDesired);
			if ( retval == 0 ) retval = -1;
			return retval;
		}
		#endregion

        private void OnRequestAborting(IExecutive exec, IDetachableEventController idec, params object[] args)
        {
            ResourceRequestAborting?.Invoke(this,exec,idec);
        }
	}

	/// <summary>
	/// A SimpleResourceRequest requests a specified quantity of whatever is in a
	/// resource manager. It assumes the resources to be homogenenous (i.e. any
	/// offered resource is immediately accepted.)
	/// </summary>
	public class SimpleResourceRequest : ResourceRequest {

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleResourceRequest"/> class.
        /// </summary>
        /// <param name="howMuch">The how much.</param>
        public SimpleResourceRequest(double howMuch):base(howMuch){}

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleResourceRequest"/> class.
        /// </summary>
        /// <param name="howMuch">The how much.</param>
        /// <param name="fromWhere">From where.</param>
        public SimpleResourceRequest(double howMuch, IResourceManager fromWhere):this(howMuch){
			DefaultResourceManager = fromWhere;
		}

        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        public override double GetScore(IResource resource){
			return Double.MaxValue;
		}

		protected override ResourceRequestSource GetDefaultReplicator() {
			return DefaultReplicator;
		}

		private IResourceRequest DefaultReplicator(){
			IResourceRequest irr = new SimpleResourceRequest(QuantityDesired);
			irr.DefaultResourceManager = DefaultResourceManager;
			return irr;
		}

	}

    /// <summary>
    /// A GuidSelectiveResourceRequest requests a specified quantity of a guid-specified
    /// resource from its manager. It assumes the resources to be unique to the given Guid.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Resources.ResourceRequest" />
    public class GuidSelectiveResourceRequest : ResourceRequest {

		private Guid m_requiredRscGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuidSelectiveResourceRequest"/> class.
        /// </summary>
        /// <param name="whichResource">Guid which indicates which resource is desired.</param>
        /// <param name="howMuch">How much of the resource is desired.</param>
        /// <param name="key">The key that will be used to see if the resource manager is allowed to
        /// grant a given resource to the requester.</param>
        public GuidSelectiveResourceRequest(Guid whichResource, double howMuch, object key):base(howMuch){
			Key = key;
			m_requiredRscGuid = whichResource;
		}

        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        public override double GetScore(IResource resource){
			if ( m_requiredRscGuid.Equals(resource.Guid) ) return Double.MaxValue;
			return Double.MinValue;
		}

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() {
			return "GuidSelectiveResourceRequest targeted at Guid = " + m_requiredRscGuid;
		}

        /// <summary>
        /// Gets a Guid which indicates which resource is desired.
        /// </summary>
        /// <value>The which resource.</value>
        public Guid WhichResource => m_requiredRscGuid;

        /// <summary>
        /// Gets or sets the required resource unique identifier.
        /// </summary>
        /// <value>The required resource unique identifier.</value>
        public Guid RequiredRscGuid
        {
            get
            {
                return m_requiredRscGuid;
            }

            set
            {
                m_requiredRscGuid = value;
            }
        }

        /// <summary>
        /// Gets the default replicator.
        /// </summary>
        /// <returns>ResourceRequestSource.</returns>
        protected override ResourceRequestSource GetDefaultReplicator() {
			return DefaultReplicator;
		}

        private IResourceRequest DefaultReplicator(){
		    GuidSelectiveResourceRequest irr = new GuidSelectiveResourceRequest(m_requiredRscGuid, QuantityDesired, Key)
		    {
		        DefaultResourceManager = DefaultResourceManager
		    };
		    return irr;
		}
	}
}
