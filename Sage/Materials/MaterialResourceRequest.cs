/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Resources;
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable ConvertToAutoProperty

namespace Highpoint.Sage.Materials.Chemistry {


    /// <summary>
    /// Class MaterialResourceRequest is a Resource request that requests a specified quantity of a specified material type be added to or removed from a specified MaterialResourceItem.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Resources.IResourceRequest" />
    public class MaterialResourceRequest : IResourceRequest {
        /// <summary>
        /// Enum Direction specified whether a resource request aimed at a MaterialResourceItem 
        /// intends to take from (deplete) or add to (augment) the quantity of substance in 
        /// that MaterialResourceItem.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// The resource request intends to take from (deplete) the quantity of substance in a MaterialResourceItem.
            /// </summary>
            Deplete,
            /// <summary>
            /// The resource request intends to add to (augment) the quantity of substance in a MaterialResourceItem.
            /// </summary>
            Augment
        }

		#region Private Fields
		private readonly MaterialType m_materialType;
		private readonly double m_quantityDesired;
        private readonly ICollection m_materialSpecs;

        #endregion 

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialResourceRequest"/> class.
        /// </summary>
        /// <param name="mt">The Material Type being requested.</param>
        /// <param name="quantity">The quantity of Material being requested.</param>
        /// <param name="direction">The <see cref="MaterialResourceRequest.Direction "/> of the request - Augment or Deplete.</param>
		public MaterialResourceRequest(MaterialType mt, double quantity, Direction direction)
			:this(null,mt,null,quantity,direction){}

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialResourceRequest"/> class.
        /// </summary>
        /// <param name="byWhom">The identity of the entity making the reqest.</param>
        /// <param name="mt">The Material Type being requested.</param>
        /// <param name="quantity">The quantity of Material being requested.</param>
        /// <param name="direction">The <see cref="MaterialResourceRequest.Direction "/> of the request - Augment or Deplete.</param>
        public MaterialResourceRequest(IHasIdentity byWhom, MaterialType mt, double quantity, Direction direction)
			:this(byWhom,mt,null,quantity,direction){}

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialResourceRequest"/> class.
        /// </summary>
        /// <param name="byWhom">The identity of the entity making the reqest.</param>
        /// <param name="mt">The Material Type being requested.</param>
        /// <param name="materialSpecs">The material specs, if any, being requested. Note: See the tech note on Material Specifications.</param>
        /// <param name="quantity">The quantity of Material being requested.</param>
        /// <param name="direction">The <see cref="MaterialResourceRequest.Direction "/> of the request - Augment or Deplete.</param>
        public MaterialResourceRequest(IHasIdentity byWhom, MaterialType mt, ICollection materialSpecs, double quantity, Direction direction){
			AsyncGrantConfirmationCallback = DefaultGrantConfirmationRequest_Refuse;

            Status = RequestStatus.Free;
			Replicate = DefaultReplicator;
			Requester = byWhom;
			m_materialType = mt;
			m_materialSpecs = materialSpecs ?? new ArrayList();
			if ( direction.Equals(Direction.Deplete) ) {
				m_quantityDesired = quantity;
			} else {
				m_quantityDesired = -quantity;
			}
		}

        #region IResourceRequest Members

        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        public double GetScore(IResource resource) {
			// If it's the right type of material, it's perfect. Otherwise, throw it back.
		    MaterialResourceItem item = resource as MaterialResourceItem;
		    if ( item != null && item.MaterialType.Equals(m_materialType)) {
				if ( QuantityDesired < 0 /* augmenting */ || (resource.Available+resource.PermissibleOverbook) >= QuantityDesired ) {
					return double.MaxValue;
				}
			}
			return double.MinValue;
		}

		#region Priority Management
		public event RequestPriorityChangeEvent PriorityChangeEvent;
		private double m_priority = 0.0;
		public double Priority {
			get { return m_priority; }
			set {
				double oldValue = m_priority;
				m_priority = value;
			    PriorityChangeEvent?.Invoke(this,oldValue,m_priority);
			}
		}
		#endregion

		public double QuantityDesired => m_quantityDesired;

        public object Key => null;

        public IResource RequiredResource {
			get {
				return null;
			}
			set { throw new NotSupportedException(); }
		}

		public double QuantityObtained { get; set; } = 0.0;

        public IResource ResourceObtained { get; set; }

        /// <summary>
        /// Gets the status of this resource request.
        /// </summary>
        /// <value>The status.</value>
        public RequestStatus Status { get; set; }

        public IResourceManager ResourceObtainedFrom { get; set; }

        public IHasIdentity Requester { get; set; }

        public ResourceSelectionStrategy ResourceSelectionStrategy => null;

        public bool Acquire(IResourceManager resourceManager, bool blockAwaitingAcquisition) {
			return resourceManager.Acquire(this,blockAwaitingAcquisition);
		}

		public void Release() {
		}

		public bool Reserve(IResourceManager resourceManager, bool blockAwaitingAcquisition) {
			throw new NotImplementedException("Reserve and Unreserve functionality in MaterialResourceRequests has not been implemented.");
		}

		public void Unreserve() {
			throw new NotImplementedException("Reserve and Unreserve functionality in MaterialResourceRequests has not been implemented.");
		}

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

        private static bool DefaultGrantConfirmationRequest_Refuse(IResourceRequest request) { return false; }

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

        /// <summary>
        /// Gets the type of the material.
        /// </summary>
        /// <value>The type of the material.</value>
        public MaterialType MaterialType => m_materialType;

        /// <summary>
        /// Gets the material specs.
        /// </summary>
        /// <value>The material specs.</value>
        public ICollection MaterialSpecs => m_materialSpecs;

        /// <summary>
		/// Creates a fresh replica of this resource request, without any of the in-progress data. This replica can
		/// be used to generate another, similar resource request that can acquire its own resource.
		/// </summary>
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global (User may want to create and assign their own, non-default replicator.
		public ResourceRequestSource Replicate { get; set; }

        /// <summary>
        /// This is the resource manager from which a resource is obtained if none is provided in the reserve or
        /// acquire API calls.
        /// </summary>
        public IResourceManager DefaultResourceManager { 
			get {
				return ResourceObtainedFrom;				
			}
			set {
				ResourceObtainedFrom = value;
			}
		}

        private IResourceRequest DefaultReplicator(){
			return new MaterialResourceRequest(Requester,m_materialType,m_quantityDesired,Direction.Deplete);
		}


        private void OnRequestAborting(IExecutive exec, IDetachableEventController idec, params object[] args)
        {
            ResourceRequestAborting?.Invoke(this, exec, idec);
        }

	}
}