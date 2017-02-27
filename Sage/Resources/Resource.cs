/* This source code licensed under the GNU Affero General Public License */

using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Persistence;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global (It is inherited by users' classes.)

namespace Highpoint.Sage.Resources {
    /// <summary>
    /// A resource represents something that is used by other objects in the completion of their tasks. It can be steam pressure,
    /// with real numbers of capacity used and returned, it can be a vehicle in a motor pool which is used in toto and returned,
    /// it can be the stock of syringes in an emergency room that are used one at a time and discarded, and separately replenished,
    /// and so on.
    /// </summary>
    public class Resource : IResource, IHasControllableCapacity, IXmlPersistable {
		
		#region Private Fields
        private double m_permissibleOverbook;
        private IResource m_wrappedByWhom;
        private string m_name;
		private Guid m_guid = Guid.Empty;
		private IModel m_model;
		#endregion

		/// <summary>
		/// Creates a new Resource. A resource is created with a capacity, and initial quantity available, and is
		/// granted in portions of that capacity, or if atomic, all-or-nothing. The IResourceRequest will specify
		/// a desired amount. If the IResourceRequest specifies a desired quantity less than the resource's capacity,
		/// and the resource is atomic, the IResourceRequest will be granted the full capacity of the resource.
		/// A self-managing resource is a resource that is responsible for granting access to itself.<p>This constructor
		/// allows the initial capacities and quantities available to be different from each other.</p>
		/// </summary>
		/// <param name="model">The model to which the Resource will belong.</param>
		/// <param name="name">The name of the Resource.</param>
		/// <param name="guid">The guid by which this resource will be known.</param>
		/// <param name="capacity">The capacity of the Resource. How much there is to be granted.</param>
		/// <param name="availability">The amount of this resource that is initially available.</param>
		/// <param name="isAtomic">True if the Resource is atomic. Atomicity infers that the resource is granted all-or-nothing.</param>
		/// <param name="isDiscrete">True if the Resource is discrete. Discreteness infers that the resource is granted in unitary amounts.</param>
		/// <param name="isPersistent">True if the Resource is persistent. Atomicity infers that the resource, once granted, must be returned to the pool.</param>
		public Resource(IModel model, string name, Guid guid, double capacity, double availability, bool isAtomic, bool isDiscrete, bool isPersistent) {
			m_model = model;
			m_name = name;
			m_guid = guid;
			Capacity = capacity;
			InitialCapacity = capacity;
			Available = availability;
			InitialAvailable = availability;
			IsAtomic = isAtomic;
			IsDiscrete = isDiscrete;
			IsPersistent = isPersistent;
			m_wrappedByWhom = this;
		    if (m_model == null) return;

		    IModelWithResources resources = m_model as IModelWithResources;
		    resources?.OnNewResourceCreated(this);
		    m_model.ModelObjects.Add(guid, this);
		}

        /// <summary>
        /// Creates a new Resource, wrapped by an implementer of IResource. This constructor is used if the
        /// resource being created is serving as a delegated-to token which represents some other resource.
        /// A resource is created with a capacity, and is granted in portions of that capacity, or if atomic,
        /// all-or-nothing. The IResourceRequest will specify a desired amount. If the IResourceRequest
        /// specifies a desired quantity less than the resource's capacity, and the resource is atomic, the
        /// IResourceRequest will be granted the full capacity of the resource. A self-managing resource
        /// is a resource that is responsible for granting access to itself.
        /// </summary>
        /// <param name="model">The model to which the Resource will belong.</param>
        /// <param name="name">The name of the Resource.</param>
        /// <param name="guid">The guid of the Resource.</param>
        /// <param name="capacity">The capacity of the Resource. How much there is to be granted.</param>
        /// <param name="availability">The initial available quantity of the resource.</param>
        /// <param name="isAtomic">True if the Resource is atomic. Atomicity infers that the resource is granted all-or-nothing.</param>
        /// <param name="isDiscrete">True if the Resource is discrete. Discreteness infers that the resource is granted in unitary amounts.</param>
        /// <param name="isPersistent">True if the Resource is persistent. Atomicity infers that the resource, once granted, must be returned to the pool.</param>
        /// <param name="wrappedByWhom">A reference to the outer object which this instance exists to represent.</param>
		public Resource(IModel model, string name, Guid guid, double capacity, double availability, bool isAtomic, bool isDiscrete, bool isPersistent, IResource wrappedByWhom) {
            InitializeIdentity(model, name, null, guid);

            InitialCapacity = capacity;
			Capacity = capacity;
			InitialAvailable = availability;
			Available = availability;
            IsAtomic = isAtomic;
			IsDiscrete = isDiscrete;
			IsPersistent = isPersistent;
            m_wrappedByWhom = wrappedByWhom;

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model this component runs in.</param>
        /// <param name="name">The name of this component.</param>
        /// <param name="description">The description for this component.</param>
        /// <param name="guid">The GUID of this component.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is discrete.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is discrete; otherwise, <c>false</c>.
        /// </value>
		public bool IsDiscrete { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is persistent.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is persistent; otherwise, <c>false</c>.
        /// </value>
		public bool IsPersistent { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is atomic.
        /// </summary>
        /// <value><c>true</c> if this instance is atomic; otherwise, <c>false</c>.</value>
		public bool IsAtomic { get; private set; }

        private bool AttemptRemovalFromService(IResourceRequest request ){
            if ( request.QuantityDesired <= (Available+m_permissibleOverbook ) ) {
                if ( IsAtomic ) {
                    request.QuantityObtained = Available;
                    Available = 0;
                } else {
                    Available -= request.QuantityDesired;
                    request.QuantityObtained = request.QuantityDesired;
                }
                request.ResourceObtained = m_wrappedByWhom;
                return true;
            } else {
                return false;
            }
        }

        private void AttemptReturnToService(IResourceRequest request ){
            if ( !Equals(request.ResourceObtained, m_wrappedByWhom) ) {
                throw new ResourceMismatchException(request, 
                    m_wrappedByWhom, 
                    ResourceMismatchException.MismatchType.UnReserve);
            }

            Available += (IsAtomic?Capacity:request.QuantityObtained);
            request.ResourceObtained = null;
            request.QuantityObtained = 0;
        }

        #region Implementation of IResource
        public bool Reserve(IResourceRequest request) {
			IResource originator = m_wrappedByWhom ?? this;
            RequestEvent?.Invoke(request,originator);
            bool bSuccess;
			lock ( this ) {
				bSuccess = AttemptRemovalFromService(request);
				if ( bSuccess ) ReservedEvent?.Invoke(request, originator);
            } 
			return bSuccess;
        }

        public void Unreserve(IResourceRequest request) {
			IResource originator = m_wrappedByWhom ?? this;
            RequestEvent?.Invoke(request,originator);
            lock ( this ) {
                AttemptReturnToService(request);
				request.ResourceObtainedFrom = null;
            }
            UnreservedEvent?.Invoke(request, originator);
        }

        public bool Acquire(IResourceRequest request) {
			IResource originator = m_wrappedByWhom ?? this;
            RequestEvent?.Invoke(request,originator);
            lock ( this ) {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
				if ( request.QuantityObtained != 0.0 ) request.Unreserve();
                if ( AttemptRemovalFromService(request) ){
                    AcquiredEvent?.Invoke(request, originator);
                    return true;
                } else return false;
            }
        }

        public void Release(IResourceRequest request) {
			IResource originator = m_wrappedByWhom ?? this;
            RequestEvent?.Invoke(request,originator);
            lock ( this ) {
                AttemptReturnToService(request);
                ReleasedEvent?.Invoke(request,originator);
            }
        }

        public IResourceManager Manager { get; set; }

        public double Capacity { get; set; }

        public double Available { get; set; }

        /// <summary>
		/// The capacity of this resource that will be in effect if the resource experiences a reset.
		/// </summary>
		public double InitialCapacity { get; }

        /// <summary>
		/// The quantity of this resource that will be available if the resource experiences a reset.
		/// </summary>
		public double InitialAvailable { get; }

        /// <summary>
		/// The amount by which it is permissible to overbook this resource.
		/// </summary>
		public double PermissibleOverbook {
			get {
				return m_permissibleOverbook;
			}
			set {
				if ( IsAtomic ) throw new ApplicationException("Overbooking permission cannot be applied to an atomic resource.");
				m_permissibleOverbook = value;
			}
		}

        public virtual void Reset(){
			Capacity = InitialCapacity;
			Available = InitialAvailable;
        }

		public event ResourceStatusEvent RequestEvent;
		public event ResourceStatusEvent ReservedEvent;
		public event ResourceStatusEvent UnreservedEvent;
        public event ResourceStatusEvent AcquiredEvent;
        public event ResourceStatusEvent ReleasedEvent;

        #endregion

        public string Name => m_name;
        private string m_description;
		/// <summary>
		/// A description of this Resource.
		/// </summary>
		public string Description => m_description ?? m_name;

        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value>The unique identifier.</value>
        public Guid Guid => m_guid;

        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => m_model;

        #region >>> Serialization Support <<< 
        /// <summary>
        /// Initializes a new empty instance of the <see cref="Resource"/> class - for deserialization only.
        /// </summary>
        public Resource(){}

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void SerializeTo(XmlSerializationContext xmlsc){
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("Guid",m_guid);
			xmlsc.StoreObject("Capacity",Capacity);
			xmlsc.StoreObject("Overbookability",m_permissibleOverbook);
			// We skip serialization of 'Available' - it'll be 100% on load.
			xmlsc.StoreObject("IsAtomic", IsAtomic);
			xmlsc.StoreObject("IsDiscrete", IsDiscrete);
			xmlsc.StoreObject("IsPersistent", IsPersistent);
			xmlsc.StoreObject("WrappedByWhom",m_wrappedByWhom);
			// We skip serialization of the manager - the manager will need to add it in on reconstitution.
		}

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc){
			m_model = (Model)xmlsc.ContextEntities["Model"];
			m_name = (string)xmlsc.LoadObject("Name");
			m_guid = (Guid)xmlsc.LoadObject("Guid");
			Capacity = (double)xmlsc.LoadObject("Capacity");
			m_permissibleOverbook = (double)xmlsc.LoadObject("Overbookability");
			// We skip serialization of 'Available' - it'll be 100% on load.
			IsAtomic = (bool)xmlsc.LoadObject("IsAtomic");
			IsDiscrete = (bool)xmlsc.LoadObject("IsDiscrete");
			IsPersistent = (bool)xmlsc.LoadObject("IsPersistent");
			m_wrappedByWhom = (IResource)xmlsc.LoadObject("WrappedByWhom");
			// We skip serialization of the manager - the manager will need to add it in on reconstitution.
		}
		#endregion
	}
}
