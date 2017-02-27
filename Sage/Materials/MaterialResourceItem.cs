/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Resources;
using GuidOps = Highpoint.Sage.Utility.GuidOps;
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable EventNeverSubscribedTo.Global

namespace Highpoint.Sage.Materials.Chemistry {
    // TODO: Either convert Material to a substance, or allow this to handle mixtures.

    /// <summary>
    /// Class MaterialResourceItem is a resource pool that contains a quantity of a substance, and 
    /// acts as a resource manager for that substance, processing Material Resource Requests.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Resources.IResource" />
    /// <seealso cref="Highpoint.Sage.Resources.IResourceManager" />
    /// <seealso cref="Highpoint.Sage.Resources.IHasControllableCapacity" />
    public class MaterialResourceItem : IResource, IResourceManager, IHasControllableCapacity {

        #region Private fields
		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("MaterialResourceItem");

        private IModel m_model;
        private IMaterial m_material;
        private string m_name;
        private Guid m_guid;
        private readonly ArrayList m_waiters;
        private static readonly ArrayList s_empty_List = ArrayList.ReadOnly(new ArrayList());

        private readonly ResourceRequestAbortEvent m_onResourceRequestAborting;

        private readonly double m_initialCapacity = 0.0;
        private readonly double m_initialQuantity = 0.0;
        private double m_capacity = 0.0;
        // private double m_available = 0.0; <-- This is done via m_material.Mass

        private readonly double m_initialTemperature = 0.0;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialResourceItem"/> class with its name being the material type name, and the guid being arbitrarily generated.
        /// </summary>
        /// <param name="model">The model in which the MaterialResourceItem runs.</param>
        /// <param name="mt">The MaterialType of the substance managed in this MaterialResourceItem.</param>
        /// <param name="initialQuantity">The initial quantity of the substance.</param>
        /// <param name="initialTemp">The initial temperature of the substance.</param>
        /// <param name="initialCapacity">The initial capacity of the MaterialResourceItem to hold the substance.</param>
        /// <exception cref="System.ApplicationException">A MaterialResourceItem cannot contain a spec with the same Guid as that of its own core material type.</exception>
        public MaterialResourceItem(IModel model, MaterialType mt, double initialQuantity, double initialTemp, double initialCapacity)
			:this(model,"Material Resource Item : " + mt.Name,Guid.NewGuid(),mt,initialQuantity,initialTemp,initialCapacity,null){}

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialResourceItem"/> class with its name being the material type name.
        /// </summary>
        /// <param name="model">The model in which the MaterialResourceItem runs.</param>
        /// <param name="guid">The unique identifier of the MaterialResourceItem.</param>
        /// <param name="mt">The MaterialType of the substance managed in this MaterialResourceItem.</param>
        /// <param name="initialQuantity">The initial quantity of the substance.</param>
        /// <param name="initialTemp">The initial temperature of the substance.</param>
        /// <param name="initialCapacity">The initial capacity of the MaterialResourceItem to hold the substance.</param>
        /// <exception cref="System.ApplicationException">A MaterialResourceItem cannot contain a spec with the same Guid as that of its own core material type.</exception>
		public MaterialResourceItem(IModel model, Guid guid, MaterialType mt, double initialQuantity, double initialTemp, double initialCapacity)
			:this(model,"Material Resource Item : " + mt.Name,guid,mt,initialQuantity,initialTemp,initialCapacity,null){}

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialResourceItem"/> class with its name being the material type name.
        /// </summary>
        /// <param name="model">The model in which the MaterialResourceItem runs.</param>
        /// <param name="guid">The unique identifier of the MaterialResourceItem.</param>
        /// <param name="mt">The MaterialType of the substance managed in this MaterialResourceItem.</param>
        /// <param name="initialQuantity">The initial quantity of the substance.</param>
        /// <param name="initialTemp">The initial temperature of the substance.</param>
        /// <param name="initialCapacity">The initial capacity of the MaterialResourceItem to hold the substance.</param>
        /// <exception cref="System.ApplicationException">A MaterialResourceItem cannot contain a spec with the same Guid as that of its own core material type.</exception>
		[Obsolete("Change to use MaterialResourceItem(IModel model, Guid guid, MaterialType mt, double initialQuantity, double initialTemp, double initialCapacity) API instead.")]
		public MaterialResourceItem(IModel model, MaterialType mt, double initialQuantity, double initialTemp, double initialCapacity, Guid guid)
			:this(model,"Material Resource Item : " + mt.Name,guid,mt,initialQuantity,initialTemp,initialCapacity,null){}

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialResourceItem"/> class.
        /// </summary>
        /// <param name="model">The model in which the MaterialResourceItem runs.</param>
        /// <param name="name">The name under which the MaterialResourceItem will be known.</param>
        /// <param name="guid">The unique identifier of the MaterialResourceItem.</param>
        /// <param name="mt">The MaterialType of the substance managed in this MaterialResourceItem.</param>
        /// <param name="initialQuantity">The initial quantity of the substance.</param>
        /// <param name="initialTemp">The initial temperature of the substance.</param>
        /// <param name="initialCapacity">The initial capacity of the MaterialResourceItem to hold the substance.</param>
        /// <param name="materialSpecGuids">The material specification guids. See Material Specifications tech note.</param>
        /// <exception cref="System.ApplicationException">A MaterialResourceItem cannot contain a spec with the same Guid as that of its own core material type.</exception>
        public MaterialResourceItem(IModel model, string name, Guid guid, MaterialType mt, double initialQuantity, double initialTemp, double initialCapacity, ICollection materialSpecGuids) {
			if ( materialSpecGuids == null ) materialSpecGuids = s_empty_List;
			m_guid = guid;
			m_name = name;
			m_model = model;
			m_initialCapacity = initialCapacity;
			m_initialQuantity = initialQuantity;
			m_initialTemperature = initialTemp;
			MaterialType = mt;
			MaterialSpecificationGuids = materialSpecGuids;
			m_waiters = new ArrayList();
			Initialize();
			if ( s_diagnostics ) m_material.MaterialChanged+=m_material_MaterialChanged;
			m_onResourceRequestAborting = OnResourceRequestAborting;

			// Ensure that no specifications contain the same Guid as the MRI's core material type.

            foreach(object obj in MaterialSpecificationGuids) {
			    Guid msGuid;
			    if ( obj is Guid ) {
					msGuid = (Guid)obj;
				} else {
					msGuid = (Guid)((DictionaryEntry)obj).Key;
				}
				if ( msGuid == MaterialType.Guid ) throw new ApplicationException("A MRI cannot contain a spec with the same Guid as that of its own core material type.");
			}

			if ( m_model != null ) {
                if (model is IModelWithResources) {
                    ( (IModelWithResources)m_model ).OnNewResourceCreated(this);
                }
				m_model.ModelObjects.Add(guid,this);
			}
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
        /// The amount of capacity this MaterialResourceItem began with.
        /// </summary>
        public double InitialCapacity => m_initialCapacity;

        /// <summary>
        /// The initial temperature of the material in this MaterialResourceItem.
        /// </summary>
        public double InitialTemperature => m_initialTemperature;

        /// <summary>
        /// The amount of material this MaterialResourceItem began with.
        /// </summary>
        public double InitialAvailable => m_initialQuantity;

        /// <summary>
        /// Gets the collection of material specification guids. See the Material Specifications tech note.
        /// </summary>
        /// <value>The material specification guids.</value>
        public ICollection MaterialSpecificationGuids { get; }

        /// <summary>
		/// The type of the material this MRI holds.
		/// </summary>
		public MaterialType MaterialType { get; }

        #region Utility Functions

		/// <summary>
		/// Function to check if a guid is contained in this MRI. See 'g' below for possiblitites
		/// </summary>
		/// <param name="g">Can be a MRI guid, material type guid, a material specification, 
		/// or an XOR of material type and specification guids</param>
		/// <returns>True if found</returns>
		public bool ContainsGuid(Guid g) {
			if(m_guid == g || MaterialType.Guid == g) return true;
			else {
			    // ReSharper disable once LoopCanBeConvertedToQuery
				foreach(Guid specGuid in MaterialSpecificationGuids) {
					if(specGuid != MaterialType.Guid 
						&& ( specGuid == g || GuidOps.XOR(MaterialType.Guid, specGuid) == g ) ) return true;
				} // end foreach specGuid
			} // end !mri.Guid == g
			return false;
		} // end ContainsGuid

		/// <summary>
		/// Performs an XOR on mri's MT and Specs to give a unique key into the contents.
		/// </summary>
		/// <param name="mri">The MRI to search</param>
		/// <returns>Guid defining the unqueness of the mri</returns>
		public static Guid ContentsXOR(MaterialResourceItem mri) {
			return ContentsXOR(mri.MaterialType.Guid, mri.MaterialSpecificationGuids);
		}

		/// <summary>
		/// Performs an XOR on mri's MT and Specs to give a unique key into the contents.
		/// </summary>
		/// <param name="mtGuid">Guid of the MaterialType</param>
		/// <param name="specGuid">Guid of the MaterialSpecification</param>
		/// <returns>Guid representing the XOR of the two guids</returns>
		public static Guid ContentsXOR(Guid mtGuid, Guid specGuid) {
			return ContentsXOR(mtGuid, new ArrayList(new[] { specGuid }));
		}

		/// <summary>
		/// Performs an XOR on mri's MT and Specs to give a unique key into the contents.
		/// </summary>
		/// <param name="mtGuid">Guid of the MaterialType</param>
		/// <param name="specGuids">Guids for the list of MaterialSpecifications</param>
		/// <returns>Guid defining the unqueness of the Guids</returns>
		public static Guid ContentsXOR(Guid mtGuid, ICollection specGuids) {
			Guid returnGuid = mtGuid;
		    // ReSharper disable once LoopCanBeConvertedToQuery
			foreach(Guid specGuid in specGuids) returnGuid = GuidOps.XOR(returnGuid, specGuid);

			return returnGuid;
		}

        #endregion Utility Functions

        #region IResource Members

        /// <summary>
        /// Gets or sets the manager of the resource.
        /// </summary>
        /// <value>The manager.</value>
        /// <exception cref="System.NotSupportedException">A MaterialResourceItem is a self-managed resource, and therefore cannot be assigned a resource manager (since it already has its own...).</exception>
        public IResourceManager Manager {
			get {
				return this;
			}
			set {
				throw new NotSupportedException("A MaterialResourceItem is a self-managed resource, and therefore cannot be assigned a resource manager (since it already has its own...).");
			}
		}

        /// <summary>
        /// Gets a value indicating whether this instance is discrete. A discrete resource is allocated in integral amounts, such as cartons or drums.
        /// </summary>
        /// <value><c>true</c> if this instance is discrete; otherwise, <c>false</c>.</value>
        public bool IsDiscrete => false;

        /// <summary>
        /// Gets a value indicating whether this instance is persistent. A persistent resource is returned to the pool after it is used.
        /// </summary>
        /// <value><c>true</c> if this instance is persistent; otherwise, <c>false</c>.</value>
        public bool IsPersistent => false;

        /// <summary>
        /// Gets a value indicating whether this instance is atomic. And atomic resource is allocated all-or-none, such as a vehicle.
        /// </summary>
        /// <value><c>true</c> if this instance is atomic; otherwise, <c>false</c>.</value>
        public bool IsAtomic => false;

        /// <summary>
        /// Resets this instance, returning it to its initial capacity and availability.
        /// </summary>
        public void Reset() {
			Initialize();
		}

		private bool AttemptExecution(IResourceRequest request ){
			double proposedNewAmountAvailable = m_material.Mass - request.QuantityDesired;
			if ( proposedNewAmountAvailable < (-PermissibleOverbook) || proposedNewAmountAvailable > m_capacity ) return false;
			request.QuantityObtained = request.QuantityDesired;
			request.ResourceObtained = this;
		    Substance material = m_material as Substance;
		    if ( material != null ) {
				material.Remove(request.QuantityDesired);
			} else if ( m_material is Mixture ) {
				((Mixture)m_material).RemoveMaterial(request.QuantityDesired);
			} else {
				Debug.Assert(false,"Unknown IMaterial type : " + m_material.GetType().Name);
			}
			request.ResourceObtainedFrom = this;
			return true;
		}

		private void DoRollback(IResourceRequest request ){
		    // ReSharper disable once PossibleUnintendedReferenceComparison (Intended reference comparison.)
			if ( request.ResourceObtained != this ) {
				throw new ResourceMismatchException(request, 
					this,
					ResourceMismatchException.MismatchType.UnReserve);
			}

			Substance addBackIn = (Substance)((Substance)m_material).MaterialType.CreateMass(request.QuantityObtained,m_material.Temperature);
		    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
		    Substance material = (Substance) m_material;
		    //if ( material != null ) {
				material.Add(addBackIn);
			/*} else if ( m_material is Mixture ) {
				((Mixture)m_material).AddMaterial(addBackIn);
			} else {
				Debug.Assert(false,"Unknown IMaterial type : " + m_material.GetType().Name);
			}*/
			request.ResourceObtained = null;
			request.ResourceObtainedFrom = null;
			request.QuantityObtained = 0;
		}


        /// <summary>
        /// Reserves the specified request. Removes it from availability, but not from the pool. This is typically an intermediate state held during resource negotiation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if the resource was successfully reserved, <c>false</c> otherwise.</returns>
        public bool Reserve(IResourceRequest request) {
		    ResourceRequested?.Invoke(request,this);
		    RequestEvent?.Invoke(request,this);
		    lock ( this ) {
				if ( AttemptExecution(request) ){
				    ReservedEvent?.Invoke(request,this);
				    ResourceReserved?.Invoke(request,this);
				    return true;
				}
				return false;
			}           
		}

        /// <summary>
        /// Unreserves the specified request. Returns it to availability.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Unreserve(IResourceRequest request) {
			lock ( this ) {
				DoRollback(request);
			    UnreservedEvent?.Invoke(request,this);
			    ResourceUnreserved?.Invoke(request,this);
			}
			while ( m_waiters.Count > 0 ) {
				IDetachableEventController dec = (IDetachableEventController)m_waiters[0];
				m_waiters.RemoveAt(0);
				dec.Resume();
			}
		}

        /// <summary>
        /// Acquires the specified request. Removes it from availability and from the resource pool.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if if the resource was successfully acquired, <c>false</c> otherwise.</returns>
        public bool Acquire(IResourceRequest request) {
            ResourceRequested?.Invoke(request,this);
            RequestEvent?.Invoke(request,this);
            lock ( this ) {
				if ( AttemptExecution(request) ){
				    AcquiredEvent?.Invoke(request,this);
				    ResourceAcquired?.Invoke(request,this);
				    return true;
				} else return false;
			}
		}

        /// <summary>
        /// Releases the specified request. Returns it to availability and the resource pool.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Release(IResourceRequest request) {
			lock ( this ) {
				DoRollback(request);
			    ReleasedEvent?.Invoke(request,this);
                ResourceReleased?.Invoke(request, this);
            }
			while ( m_waiters.Count > 0 ) {
				IDetachableEventController dec = (IDetachableEventController)m_waiters[0];
				m_waiters.RemoveAt(0);
				dec.Resume();
			}
		}

        /// <summary>
        /// Occurs when this resource has been requested.
        /// </summary>
        public event ResourceStatusEvent RequestEvent;

        /// <summary>
        /// Occurs when this resource has been reserved.
        /// </summary>
        public event ResourceStatusEvent ReservedEvent;

        /// <summary>
        /// Occurs when this resource has been unreserved.
        /// </summary>
        public event ResourceStatusEvent UnreservedEvent;

        /// <summary>
        /// Occurs when this resource has been acquired.
        /// </summary>
        public event ResourceStatusEvent AcquiredEvent;

        /// <summary>
        /// Occurs when this resource has been released.
        /// </summary>
        public event ResourceStatusEvent ReleasedEvent;

		#endregion

		#region Implementation of IResourceManager
		//		/// <summary>
		//		/// Unreserves the resource specified by the resource request.
		//		/// </summary>
		//		/// <param name="resourceRequest">The IResourceRequest that specifies the resource to unreserve.</param>
		//		public void Unreserve(Highpoint.Sage.Resources.IResourceRequest resourceRequest) {
		//			m_resourceManager.Unreserve(resourceRequest);
		//		}
		//
		//		/// <summary>
		//		/// Releases the resource specified by the resource request.
		//		/// </summary>
		//		/// <param name="resourceRequest">The IResourceRequest that specifies the resource to release.</param>
		//		public void Release(Highpoint.Sage.Resources.IResourceRequest resourceRequest) {
		//			m_resourceManager.Release(resourceRequest);
		//		}

		/// <summary>
		/// Reserves a resource according to the specified resource request, either blocking until successful, or
		/// returning &lt;null&gt; if the resource is not immediately available. 
		/// </summary>
		/// <param name="resourceRequest">The IResourceRequest that specifies the criteria by which to select the resource.</param>
		/// <param name="blockAwaitingAcquisition">If true, request will suspend until granted. If false, will return false if unable to fulfill.</param>
		/// <returns>True if granted, false if not granted.</returns>
		public bool Reserve(IResourceRequest resourceRequest, bool blockAwaitingAcquisition) {
			if ( blockAwaitingAcquisition ) {
				
				IDetachableEventController dec = Model.Executive.CurrentEventController;
				if ( dec == null ) throw new ApplicationException("Someone tried to call Reserve(..., true) while not in a detachable event. This is not allowed.");

				dec.SetAbortHandler(resourceRequest.AbortHandler);
				resourceRequest.ResourceRequestAborting+=m_onResourceRequestAborting;
				
				while ( true ) {
					if ( Reserve(resourceRequest) ) break;
					m_waiters.Add(dec);
					dec.Suspend();
                    dec.ClearAbortHandler();
				}
				return true;
			} else {
				return Reserve(resourceRequest);
			}
		}

		/// <summary>
		/// Acquires a resource according to the specified resource request, either blocking until successful, or
		/// returning &lt;null&gt; if the resource is not immediately available. 
		/// </summary>
		/// <param name="resourceRequest">The IResourceRequest that specifies the criteria by which to select the resource.</param>
		/// <param name="blockAwaitingAcquisition">If true, request will suspend until granted. If false, will return false if unable to fulfill.</param>
		/// <returns>True if granted, false if not granted.</returns>
		public bool Acquire(IResourceRequest resourceRequest, bool blockAwaitingAcquisition) {
			if ( blockAwaitingAcquisition ) {

				IDetachableEventController dec = Model.Executive.CurrentEventController;
				if ( dec == null ) throw new ApplicationException("Someone tried to call Acquire(..., true) while not in a detachable event. This is not allowed.");
				
				dec.SetAbortHandler(resourceRequest.AbortHandler);
				resourceRequest.ResourceRequestAborting+=m_onResourceRequestAborting;
				
				while ( true ) {
					if ( Acquire(resourceRequest) ) break;
					m_waiters.Add(dec);
					dec.Suspend();
                    dec.ClearAbortHandler();
				}
				return true;
			} else {
				return Acquire(resourceRequest);
			}
		}

        /// <summary>
        /// Fired when a resource request is received.
        /// </summary>
        public event ResourceStatusEvent ResourceRequested;
        /// <summary>
        /// Fired when a resource is reserved.
        /// </summary>
        public event ResourceStatusEvent ResourceReserved;
        /// <summary>
        /// Fired when a resource is unreserved.
        /// </summary>
        public event ResourceStatusEvent ResourceUnreserved;
        /// <summary>
        /// Fired when a resource is acquired and thereby removed from the pool.
        /// </summary>
        public event ResourceStatusEvent ResourceAcquired;
        /// <summary>
        /// Fired when a resource is released back into the pool.
        /// </summary>
        public event ResourceStatusEvent ResourceReleased;
        /// <summary>
        /// Fired when a resource is added to the pool.
        /// </summary>
        public event ResourceManagerEvent ResourceAdded;
        /// <summary>
        /// Fired when a resource is removed from the pool.
        /// </summary>
        public event ResourceManagerEvent ResourceRemoved;

        /// <summary>
        /// Gets or sets the access regulator, which is an object that can allow or deny
        /// individual ResourceRequests access to specified resources. Note - MaterialResourceItem does not support using an access regulator.
        /// </summary>
        /// <value>The access regulator.</value>
        /// <exception cref="System.NotSupportedException">A MaterialResourceItem does not support an access regulator.</exception>
        public IAccessRegulator AccessRegulator { 
			set{ throw new NotSupportedException("A MaterialResourceItem does not support an access regulator."); }
			get{ return null; }
		}

        /// <summary>
        /// Gets the resources owned by this Resource Manager.
        /// </summary>
        /// <value>The resources.</value>
        public IList Resources { 
			get {
			    ArrayList al = new ArrayList {this};
			    return al;
			}
		}

        /// <summary>
        /// Gets a value indicating whether this resource manager supports prioritized requests. Note - MaterialResourceItem does not.
        /// </summary>
        /// <value><c>true</c> if [supports prioritized requests]; otherwise, <c>false</c>.</value>
        public bool SupportsPrioritizedRequests => false;

// Some day...

		#endregion

		#region IModelObject Members

        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => m_model;

        #region IHasIdentity Members

		public string Name => m_name;

        private string m_description = null;
		/// <summary>
		/// A description of this MaterialResourceItem
		/// </summary>
		public string Description => m_description ?? m_name;

        public Guid Guid => m_guid;

        #endregion

        #endregion

        #region IHasCapacity Members

        /// <summary>
        /// The current capacity of this resource - how much 'Available' can be, at its highest value.
        /// </summary>
        /// <value>The capacity.</value>
        public double Capacity {
			get {
				return m_capacity;
			}
			set {
				m_capacity = value;
			}
		}

        /// <summary>
        /// How much of this resource is currently available to service requests.
        /// </summary>
        /// <value>The available.</value>
        public double Available {
			get {
				return m_material.Mass;
			}
			set {
				Substance substance = (Substance)m_material;
				// We want to change the mass of material in m_mri.Material.
				double delta = value - substance.Mass;
				if ( delta > 0 ) {
					substance.Add((Substance)MaterialType.CreateMass(delta,m_initialTemperature));
				    ResourceAdded?.Invoke(Manager,this);
				} else {
					substance.Remove(-delta);
				    ResourceRemoved?.Invoke(Manager,this);
				}
			}
		}

		/// <summary>
		/// The amount by which it is permissible to overbook this resource.
		/// </summary>
		public double PermissibleOverbook { get; set; } = 0.0;

        #endregion

        /// <summary>
        /// Gets or sets the tag - an arbitrary object attached to this one.
        /// </summary>
        /// <value>The tag.</value>
        public object Tag { get; set; } = null;

        private void Initialize(){
			m_capacity = m_initialCapacity;
			lock (this)
			{
			    m_material = MaterialType.CreateMass(m_initialQuantity,m_initialTemperature);
			}
			((Substance)m_material).SetMaterialSpecs(MaterialSpecificationGuids);
		}

		private void m_material_MaterialChanged(IMaterial material, MaterialChangeType type) {
		    // ReSharper disable once RedundantJumpStatement (Used in diagnostics)
			if ( type == MaterialChangeType.Temperature ) return;
			//Trace.WriteLine(m_model.Executive.Now + " : mixture in " + this.Name + " is now " + m_material + ", after a change of " + type);
		}

        private void OnResourceRequestAborting(IResourceRequest request, IExecutive exec, IDetachableEventController idec) {
			m_model.AddWarning(new TerminalResourceRequestAbortedWarning(exec,this,request,idec));
		}
	}
}