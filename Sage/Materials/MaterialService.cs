/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Resources;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.Connectors;
using System.Collections.Generic;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Materials {
    /// <summary>
    /// A MaterialService manages a set of connection tokens (a discrete, replenishable resource), 
    /// an available capacity (a continuous, replenishable resource) and a set of compartments
    /// which are MaterialResourceItems. A Compartment can be thought of as a "bucket" with
    /// material in it, and a specified capacity &amp; overbooking setting.  Overbooking means that
    /// you can, for example, take more than is actually there.
    /// <para/>
    /// There is a mode setting called "Wildcard mode". When set to true, and a charge or
    /// discharge is requested involving a material that the MaterialService does not already have,
    /// a compartment is created with an infinite capacity for the desired material, and
    /// infinite overbooking permitted so that it may receive or supply as much as you need.
    /// The quantity will start at zero, and will always reflect the amount that has been put
    /// in (if positive) or taken out (if negative). When "Wildcard" is set to false, In order
    /// to complete a requested activity, (charge or discharge) there must be a compartment
    /// of the correct MaterialTypes, and with sufficient quantity, if requesting a charge,
    /// or capacity, if requesting a discharge.
    /// </summary>
    public class MaterialService : IModelObject, IPortOwner {

        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("MaterialService");

        #region >>> Private Fields <<<
        private MaterialCatalog m_materialCatalog;
        private Hashtable m_materials;
        private SelfManagingResource m_serviceTokens;
        private SelfManagingResource m_deliveryCapacity;
        private bool m_autocreateMaterialCompartments = false;
        private double m_defaultMaterialTemperature;
        private Guid m_transferTableKey;
        private Guid m_transferTableKeyMask = new Guid("13e0467f-893b-4e9c-83e1-466e627dc82b");
        #endregion

        #region >>> Constructors <<<

        /// <summary>
        /// Default constructor for serialization only.
        /// </summary>
        public MaterialService() { }

        /// <summary>
        /// Creates a MaterialService. A MaterialService models a system that provides some service or material, but
        /// may only be able to service a limited number of clients, and perhaps only at a limited supply rate.
        /// </summary>
        /// <param name="model">The model to which this MaterialService belongs.</param>
        /// <param name="name">The name of this MaterialService.</param>
        /// <param name="guid">The guid of this MaterialService.</param>
        /// <param name="nSvcTokens">The number of clients this MaterialService can service at the same time.</param>
        /// <param name="maxDeliveryRate">The maximum kilograms per minute that this MaterialService can provide.
        /// Note - if physical materials are not delivered, this value can be any units desired.</param>
        /// <param name="materialCatalog">The material catalog from which are drawn the materials in this MaterialService.</param>
        /// <param name="defaultMaterialTemperature">The temperature at which materials will be auto-created.</param>
        public MaterialService(IModel model, string name, Guid guid, int nSvcTokens, double maxDeliveryRate, MaterialCatalog materialCatalog, double defaultMaterialTemperature) {
            InitializeIdentity(model, name, null, guid);

            m_transferTableKey = GuidOps.XOR(Guid, m_transferTableKeyMask);

            m_materialCatalog = materialCatalog;
            m_defaultMaterialTemperature = defaultMaterialTemperature;
            Debug.Assert(m_materialCatalog != null, "Material Catalog provided to MaterialService was null.");
            m_materials = new Hashtable();
            #region >>> Establish service tokens and delivery rate resources, if needed. <<<
            if (nSvcTokens != int.MaxValue) {
                m_serviceTokens = new SelfManagingResource(model, name + ".ServiceTokens", Guid.NewGuid(), nSvcTokens, false, true, true);
            } else {
                m_serviceTokens = null;
            }
            if (maxDeliveryRate != double.MaxValue) {
                m_deliveryCapacity = new SelfManagingResource(model, name + ".DeliveryCapacity", Guid.NewGuid(), maxDeliveryRate, false, false, true);
            } else {
                m_deliveryCapacity = null;
            }
            #endregion

            IMOHelper.RegisterWithModel(this);
        }

        #endregion

        /// <summary>
        /// Returns true if the rsc is any of the child resources (ServiceTokenDispenser, 
        /// CapacityDispenser or MaterialResourceItems) of this MaterialService.
        /// </summary>
        /// <param name="rsc">The candidate child resource.</param>
        /// <returns>true if the rsc is any of the child resources (ServiceTokenDispenser, 
        /// CapacityDispenser or MaterialResourceItems) of this MaterialService.</returns>
        public bool IsSubResource(IResource rsc) {
            if (rsc == null)
                return false;
            if (m_deliveryCapacity != null && m_deliveryCapacity.Equals(rsc))
                return true;
            if (m_serviceTokens != null && m_serviceTokens.Equals(rsc))
                return true;
            foreach (MaterialResourceItem mri in m_materials.Values) {
                if (rsc.Equals(mri))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The number of clients this MaterialService can service at the same time.
        /// </summary>
        public int NumberOfServiceTokens {
            get {
                if (m_serviceTokens == null)
                    return int.MaxValue;
                return (int)m_serviceTokens.Capacity;
            }
            set {
                if (m_serviceTokens == null) {
                    m_serviceTokens = new SelfManagingResource(m_model, m_name + ".ServiceTokens", Guid.NewGuid(), value, false, true, true);
                } else {
                    m_serviceTokens.Capacity = value;
                }
            }
        }

        /// <summary>
        /// The maximum kilograms per minute that this MaterialService can provide.
        /// </summary>
        public double MaxDeliveryRate {
            get {
                if (m_deliveryCapacity == null)
                    return double.MaxValue;
                return (int)m_serviceTokens.Capacity;
            }
        }

        /// <summary>
        /// Returns a collection of MaterialResourceItem objects that represent the material compartments.
        /// </summary>
        public ICollection Compartments { get { return m_materials.Values; } }

        #region Add Compartments
        /// <summary>
        /// Adds a compartment for the specified material type. The MaterialResourceItem
        /// will be the provider and/or receiver of material of the specified type.
        /// </summary>
        /// <param name="mri">The MaterialResourceItem that will supply and absorb material
        /// of its type on behalf of this MaterialService.</param>
        public void AddCompartment(MaterialResourceItem mri) {
            if (m_materials.Contains(mri.MaterialType)) {
                throw new ApplicationException("MaterialService " + m_name + " already contains a compartment for " +
                    mri.MaterialType.Name + " registered under guid " + mri.Guid);
            } else {
                Guid key = GetAggregateKey(mri.MaterialType.Guid, mri.MaterialSpecificationGuids);
                m_materials.Add(key, mri);
            }
        }

        /// <summary>
        /// Adds a compartment for the specified material type. Creates a MaterialResourceItem
        /// which will be the provider and/or receiver of material of the specified type.
        /// </summary>
        /// <param name="model">The model that contains the material catalog that holds the material type whose guid is listed below.</param>
        /// <param name="materialTypeGuid">The guid of the intended material type which will be in this compartment.</param>
        /// <param name="initialQuantity">How many kilograms of this material will be in this compartment to begin with.</param>
        /// <param name="initialTemp">The initial temperature of the material in the new material compartment.</param>
        /// <param name="initialCapacity">How many kilograms of the material this compartment will be able to hold.</param>
        /// <param name="compartmentGuid">The guid that will identify this compartment.</param>
        public void AddCompartment(IModel model, Guid materialTypeGuid, double initialQuantity, double initialTemp, double initialCapacity, Guid compartmentGuid) {
            MaterialType mt = m_materialCatalog[materialTypeGuid];
            if (mt == null) {
                throw new ApplicationException("MaterialService cannot be created with the material whose Guid is " +
                    materialTypeGuid + " since the model does not know of such a material.");
            }
            MaterialResourceItem mri = new MaterialResourceItem(model, compartmentGuid, mt, initialQuantity, initialTemp, initialCapacity);
            AddCompartment(mri);
        }

        /// <summary>
        /// Adds a compartment for the specified material type and specifications. Creates a MaterialResourceItem
        /// which will be the provider and/or receiver of material of the specified type and specifications.
        /// </summary>
        /// <param name="model">The model that contains the material catalog that holds the material type whose guid is listed below.</param>
        /// <param name="materialTypeGuid">The guid of the intended material type which will be in this compartment.</param>
        /// <param name="materialSpecifications">A Collection of guids, or a collection of DictionaryEntry objects for which the keys are guids. These guids represent the materialSpecs that characterize the material.</param>
        /// <param name="initialQuantity">How many kilograms of this material will be in this compartment to begin with.</param>
        /// <param name="initialTemp">The initial temperature of the material in the new material compartment.</param>
        /// <param name="initialCapacity">How many kilograms of the material this compartment will be able to hold.</param>
        /// <param name="compartmentGuid">The guid that will identify this compartment.</param>
        public void AddCompartment(IModel model, Guid materialTypeGuid, ICollection materialSpecifications, double initialQuantity, double initialTemp, double initialCapacity, Guid compartmentGuid) {
            MaterialType mt = m_materialCatalog[materialTypeGuid];
            if (mt == null) {
                throw new ApplicationException("A MaterialService compartment cannot be created with the material whose Guid is " +
                    materialTypeGuid + " since the model does not know of such a material.");
            }

            ArrayList matlSpecGuids = new ArrayList();
            if (materialSpecifications != null && materialSpecifications.Count != 0) {
                foreach (object obj in materialSpecifications) {
                    if (obj is Guid) {
                        matlSpecGuids.Add(obj);
                    } else if (obj is DictionaryEntry && ( (DictionaryEntry)obj ).Key is Guid) {
                        matlSpecGuids.Add(( (DictionaryEntry)obj ).Key);
                    } else {
                        throw new ApplicationException("Attempt to specify a compartment by MaterialType "
                            + "and something other than a collection of [DictionaryEntries with Guids as "
                            + "Keys] or [Guids] - these are the only two constructs that can be used. They "
                            + " are intended to represent MaterialSpecifications or their guids.");
                    }
                }

                MaterialResourceItem mri = new MaterialResourceItem(model, "Material Resource Item : " + mt.Name, compartmentGuid, mt, initialQuantity, initialTemp, initialCapacity, matlSpecGuids);
                AddCompartment(mri);
            }
        }

        /// <summary>
        /// Adds a compartment for the specified material type, specifications, initial quantity, temperature &amp; capacity. Creates a MaterialResourceItem
        /// which will be the provider and/or receiver of material of the specified type and specifications.
        /// </summary>
        /// <param name="model">The model that contains the material catalog that holds the material type whose guid is listed below.</param>
        /// <param name="materialTypeGuid">The guid of the intended material type which will be in this compartment.</param>
        /// <param name="materialSpecificationGuid">The single Guid that denotes the material specification of material in this compartment.</param>
        /// <param name="initialQuantity">How many kilograms of this material will be in this compartment to begin with.</param>
        /// <param name="initialTemp">The initial temperature of the material in the new material compartment.</param>
        /// <param name="initialCapacity">How many kilograms of the material this compartment will be able to hold.</param>
        /// <param name="compartmentGuid">The guid that will identify this compartment.</param>
        public void AddCompartment(IModel model, Guid materialTypeGuid, Guid materialSpecificationGuid, double initialQuantity, double initialTemp, double initialCapacity, Guid compartmentGuid) {
            ArrayList spec = new ArrayList();
            spec.Add(materialSpecificationGuid);
            AddCompartment(model, materialTypeGuid, spec, initialQuantity, initialTemp, initialCapacity, compartmentGuid);
        }

        /// <summary>
        /// If true, this MaterialService will automatically create material compartments
        /// and provide them with an inexhaustible supply of material, if a material
        /// that was hitherto unknown is requested.
        /// </summary>
        public bool AutocreateMaterialCompartments {
            get { return m_autocreateMaterialCompartments; }
            set { m_autocreateMaterialCompartments = value; }
        }

        #endregion Add Compartments

        #region Get Compartments
        /// <summary>
        /// Gets the MaterialResourceItem that is acting as the compartment for the
        /// specified material type. Creates one anew if there was not one, and this
        /// MaterialService has its AutocreateMaterialCompartments parameter set to true.
        /// Will return null if there is no compartment with the given material type.
        /// </summary>
        /// <param name="materialTypeGuid">The guid of the material type whose compartment we desire.</param>
        /// <param name="materialSpecification">The single guid that describes the material spec we seek on the material type.</param>
        /// <returns>The MaterialResourceItem that is acting as the compartment for the
        /// specified material type.</returns>
        public MaterialResourceItem GetCompartment(Guid materialTypeGuid, Guid materialSpecification) {
            MaterialType mt = (MaterialType)m_materialCatalog[materialTypeGuid];
            if (mt == null) {
                throw new ApplicationException("A MaterialService compartment cannot be created with the material whose Guid is " +
                    materialTypeGuid + " since the model does not know of such a material.");
            }
            ArrayList materialSpecs = new ArrayList();
            materialSpecs.Add(materialSpecification);
            return GetCompartment(mt, materialSpecs);
        }

        /// <summary>
        /// Gets the MaterialResourceItem that is acting as the compartment for the
        /// specified material type. Creates one anew if there was not one, and this
        /// MaterialService has its AutocreateMaterialCompartments parameter set to true.
        /// Will return null if there is no compartment with the given material type.
        /// </summary>
        /// <param name="mt">The material type whose compartment we desire.</param>
        /// <returns>The MaterialResourceItem that is acting as the compartment for the
        /// specified material type.</returns>
        public MaterialResourceItem GetCompartment(MaterialType mt) {
            return GetCompartment(mt, null);
        }

        /// <summary>
        /// Gets the MaterialResourceItem that is acting as the compartment for the
        /// specified material type. Creates one anew if there was not one, and this
        /// MaterialService has its AutocreateMaterialCompartments parameter set to true.
        /// Will return null if there is no compartment with the given material type.
        /// </summary>
        /// <param name="mt">The material type whose compartment we desire.</param>
        /// <param name="materialSpecifications">A collection of the material specifications 
        /// that are to be applied to this compartment.</param>
        /// <returns>The MaterialResourceItem that is acting as the compartment for the
        /// specified material type.</returns>
        public MaterialResourceItem GetCompartment(MaterialType mt, ICollection materialSpecifications) {

            Guid key = GetAggregateKey(mt.Guid, materialSpecifications);

            MaterialResourceItem mri = (MaterialResourceItem)m_materials[key];
            if (mri == null && m_autocreateMaterialCompartments) {
                double temperature = m_defaultMaterialTemperature;

                #region Get MaterialSpecifications as an array of guids.
                // Callers can pass in an array of Guids, or they can pass in an array of MaterialSpecification guid/value pairs.
                // we need to make sure that what gets passed to the MRI ctor is an array of Guids.
                if (materialSpecifications == null)
                    materialSpecifications = new ArrayList();
                if (materialSpecifications.Count > 0) {
                    IEnumerator enumer = materialSpecifications.GetEnumerator();
                    enumer.MoveNext();
                    if (enumer.Current is DictionaryEntry) {
                        ArrayList temp = new ArrayList();
                        foreach (DictionaryEntry de in materialSpecifications)
                            temp.Add(de.Key);
                        materialSpecifications = temp;
                    }
                }

                #endregion Get MaterialSpecifications as an array of guids.
                mri = new MaterialResourceItem(m_model, "MaterialService Compartment : " + mt.Name, Guid.NewGuid(), mt, 0.0, temperature, double.MaxValue, materialSpecifications);

                mri.PermissibleOverbook = double.MaxValue;
                m_materials.Add(key, mri);
            }
            return mri;
        }
        #endregion Get Compartments

        #region Connection Management
        /// <summary>
        /// Creates an IConnection between this MaterialService and the specifed port.
        /// </summary>
        /// <param name="otherGuysPort">The port to which this MaterialService is to be connected.</param>
        public void EstablishConnection(IPort otherGuysPort) {
            Guid myPortKey = GuidOps.XOR(otherGuysPort.Key, Guid);
            IPort myPort = Ports[myPortKey];
            if (myPort != null && myPort.Peer != null && myPort.Peer.Equals(otherGuysPort))
                return; // Only bother if there's not already a cnxn there.

            if (myPort != null) {
                DestroyConnection(otherGuysPort); // Leaves both my, and the other guy's port there, but disconnects it.
            } else {
                if (otherGuysPort is IOutputPort) {
                    int ndx = 0;
                    while (Ports["Input port from " + otherGuysPort.Name + "." + ndx] != null) {
                        ndx++;
                    }
                    myPort = new SimpleInputPort(Model, "Input port from " + otherGuysPort.Name + "." + ndx, myPortKey, this, new DataArrivalHandler(OnMaterialArrived));
                } else if (otherGuysPort is IInputPort) {
                    int ndx = 0;
                    while (Ports["Output port to " + otherGuysPort.Name + "." + ndx] != null) {
                        ndx++;
                    }
                    myPort = new SimpleOutputPort(Model, "Output port to " + otherGuysPort.Name + "." + ndx, myPortKey, this, null, null);
                } else {
                    Debug.Assert(false, "Ambiguous Port Directionality", "Connection attempted to port that is neither an IInpupPort nor an IOutputPort.");
                }
            }
            // Ports.AddPort(myPort); <-- Done in port's ctor.
            ConnectorFactory.Connect(otherGuysPort, myPort);
        }

        /// <summary>
        /// Destroys the connection between this MaterialService and the specified port.
        /// </summary>
        /// <param name="otherGuysPort">The port to which this service has a connection.</param>
        public void DestroyConnection(IPort otherGuysPort) {
            Guid myPortKey = GuidOps.XOR(otherGuysPort.Key, Guid);
            IPort myPort = Ports[myPortKey];

            // If I have a port, and it's got a connector, and is either not connected to
            // anyone else, or connected to the other guy's port, disconnect it. This way,
            // it gets disconnected in all cases other than that it is connected to someone
            // it does not expect to be at the other end.
            if (myPort != null && myPort.Connector != null && ( myPort.Peer == null || myPort.Peer.Equals(otherGuysPort) )) {
                if (myPort.Connector != null) {
                    myPort.Connector.Disconnect();
                    //Ports.RemovePort(myPort);
                }
            }
        }
        #endregion Connection Management

        protected Guid GetAggregateKey(Guid mtGuid, ICollection materialSpecifications) {
            Guid key = mtGuid;
            if (materialSpecifications != null) {
                foreach (object obj in materialSpecifications) {
                    if (obj is Guid) {
                        key = GuidOps.XOR(key, (Guid)obj);
                    } else if (obj is DictionaryEntry && ( (DictionaryEntry)obj ).Key is Guid) {
                        key = GuidOps.XOR(key, (Guid)( (DictionaryEntry)obj ).Key);
                    } else {
                        throw new ApplicationException("Attempt to specify a compartment by MaterialType "
                            + "and something other than a collection of [DictionaryEntries with Guids as "
                            + "Keys] or [Guids] - these are the only two constructs that can be used. They "
                            + " are intended to represent MaterialSpecifications or their guids.");
                    }
                }
            }
            return key;
        }

        protected SelfManagingResource DeliveryCapacity { get { return m_deliveryCapacity; } }

        protected SelfManagingResource ServiceTokens { get { return m_serviceTokens; } }

        private bool OnMaterialArrived(object material, IInputPort ip) {
            return false;
        }

        private object m_tag = null;
        /// <summary>
        /// Tag object is for holding user-specified references.
        /// </summary>
        public object Tag { get { return m_tag; } set { m_tag = value; } }

        #region IModelObject Members
        private string m_name = null;
        /// <summary>
        /// The name of this MaterialService.
        /// </summary>
        public string Name { get { return m_name; } }
        private string m_description = null;
        /// <summary>
        /// A description of this MaterialService.
        /// </summary>
        public string Description {
            get { return m_description == null ? m_name : m_description; }
        }
        private Guid m_guid = Guid.Empty;
        /// <summary>
        /// The Guid of this MaterialService.
        /// </summary>
        public Guid Guid => m_guid;
        private IModel m_model;
        /// <summary>
        /// The Model to which this MaterialService belongs.
        /// </summary>
        public IModel Model => m_model;

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The IModelObject's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        #endregion

        #region IPortOwner Members

        private PortSet m_myPortSet = new PortSet();
        /// <summary>
        /// The PortSet that contains all ports currently registered with this
        /// MaterialService. The MaterialService will temporarily create and register ports
        /// with itself as needed to service charge/discharge requests.
        /// </summary>
        public IPortSet Ports {
            get {
                return m_myPortSet;
            }
        }

        /// <summary>
        /// Adds a Port to this MaterialService's PortSet.
        /// </summary>
        /// <param name="port">The port that is being registered.</param>
        public void AddPort(IPort port) {
            m_myPortSet.AddPort(port);
        }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { throw new NotImplementedException(); /*Implement AddPort(string channel); */}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { throw new NotImplementedException(); /*Implement AddPort(string channel); */}

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

        /// <summary>
        /// Unregisters a port from this MaterialService's PortSet.
        /// </summary>
        /// <param name="port">The port to be removed.</param>
        public void RemovePort(IPort port) {
            m_myPortSet.RemovePort(port);
        }

        /// <summary>
        /// Unregisters all ports from this PortSet.
        /// </summary>
        public void ClearPorts() {
            m_myPortSet.ClearPorts();
        }

        #endregion

        /// <summary>
        /// Performs setup for utilization of this MaterialService. This involves acquiring a
        /// service token (if we are limited to a given number of available tokens), reserving the
        /// material that will be transferred (if we are going to physically transfer materials),
        /// and reserving the delivery capacity (if we are limiting overall available capacity).
        /// </summary>
        /// <param name="material">The material we will be transferring. Null if no material will be transferred.</param>
        /// <param name="deliveryRate">The amount of capacity this request will use. Units are kilograms per minute, by default.</param>
        /// <param name="otherGuysPort">The resource-user's desired source or sink port.</param>
        /// <returns>
        /// An opaque object that must be fed back to this MaterialService during subsequent stages
        /// of the usage of this operation.
        /// </returns>
        public object Setup(IMaterial material, double deliveryRate, IPort otherGuysPort) {
            return Setup(material, deliveryRate, otherGuysPort, false);
        }

        /// <summary>
        /// Performs setup for utilization of this MaterialService. This involves acquiring a
        /// service token (if we are limited to a given number of available tokens), reserving the
        /// material that will be transferred (if we are going to physically transfer materials),
        /// and reserving the delivery capacity (if we are limiting overall available capacity).
        /// A current limitation of this class is that its use must be entirely contained within
        /// the scope of one SOMTask.
        /// </summary>
        /// <param name="material">The material we will be transferring. Null if no material will 
        /// be transferred.</param>
        /// <param name="deliveryRate">The amount of capacity this request will use. Units are 
        /// kilograms per minute, by default.</param>
        /// <param name="otherGuysPort">The resource-user's desired source or sink port.</param>
        /// <param name="createConnection">if set to <c>true</c> this setup will create a connection 
        /// for the transfer, and destroy the connection after the transfer is completed.</param>
        /// <returns>
        /// An opaque object that must be fed back to this MaterialService during subsequent stages
        /// of the usage of this operation.
        /// </returns>
        public object Setup(IMaterial material, double deliveryRate, IPort otherGuysPort, bool createConnection) {

            if (createConnection) {
                EstablishConnection(otherGuysPort);
            }

            IPort myPort = Ports[GuidOps.XOR(otherGuysPort.Guid, Guid)];
            string reason = "";
            #region >>> First, verify that this request is achievable in all ways other than materials.<<<

            if (myPort == null) {
                reason += "The MaterialService " + Name + " was asked to transfer to " + ( (IHasIdentity)otherGuysPort.Owner ).Name + ", but there does not seem to be a connection between the two.";
            }

            if (( DeliveryCapacity != null ) && deliveryRate > DeliveryCapacity.Capacity) {
                reason += "This MaterialService has a delivery capacity of " + DeliveryCapacity.Capacity + ", and cannot support the requested delivery rate, " + deliveryRate + ".\r\n";
            }

            if (otherGuysPort is IInputPort && otherGuysPort is IOutputPort) {
                reason += "Cannot determine directionality of the transfer, since the partner port is bidirectional.\r\n";
            }

            if (!( otherGuysPort is IInputPort || otherGuysPort is IOutputPort )) {
                reason += "Cannot determine directionality of the transfer, since the partner port, "
                    + ( otherGuysPort == null ? "<null>" : ( "\"" + otherGuysPort + "\"" ) ) + " is of an unrecognized type, or is null.\r\n";
            }

            if (otherGuysPort == null) {
                reason += "Provided port is null.\r\n";
            }

            #endregion

            MaterialResourceRequest.Direction direction;
            #region >>> Determine directionality of the transfer. <<<
            if (otherGuysPort is IOutputPort) {
                direction = MaterialResourceRequest.Direction.Augment;
            } else /*if ( otherGuysPort is IInputPort ) */ {
                direction = MaterialResourceRequest.Direction.Deplete;
            }
            #endregion

            ArrayList substances = new ArrayList();
            if (material is Substance)
                substances.Add(material);
            else
                substances.AddRange(( (Mixture)material ).Constituents);

            ArrayList alMaterialResourceItems = new ArrayList();
            MaterialResourceItem[] mria = new MaterialResourceItem[substances.Count];
            #region >>> Now, make sure it is achievable based on the materials requested or offered. <<<

            for (int i = 0 ; i < substances.Count ; i++) {// ( Substance substance in substances ) {

                MaterialType mt = ( (Substance)substances[i] ).MaterialType;
                double mass = ( (Substance)substances[i] ).Mass;
                ICollection matlSpecs = ( (Substance)substances[i] ).GetMaterialSpecs();

                MaterialResourceItem mri = null; // Populated in the following region.

                if (mt != null) {
                    mri = GetCompartment(mt, matlSpecs);
                    if (mri == null) {
                        reason += "This resource does not contain " + mt.Name + " with the requested specifications. ";
                    } else {
                        if (mass > mri.Capacity) {
                            reason += "More material has been requested than the capacity "
                                + "of the compartment containing it.\r\n";
                        }
                    }
                    alMaterialResourceItems.Add(mri);
                }
            }

            if (reason.Length > 0) {
                throw new ApplicationException(Name + " : Error : " + reason);
            }

            mria = (MaterialResourceItem[])alMaterialResourceItems.ToArray(typeof(MaterialResourceItem));
            #endregion

            IResourceRequest strr, crr;
            MaterialResourceRequest[] mrra;
            ArrayList alMaterialResourceRequests = new ArrayList();
            #region >>> Create all Resource requests (str, crr and mrr[]).<<<
            #region >>> Create Service Token Request (strr). <<<
            strr = new SimpleResourceRequest(1.0);
            strr.Requester = (IHasIdentity)otherGuysPort.Owner;
            #endregion

            #region >>> Create Material Resource Request Array (mrr). <<<
            foreach (Substance substance in substances) {
                MaterialResourceRequest mrr = new MaterialResourceRequest(null, substance.MaterialType, substance.GetMaterialSpecs(), substance.Mass, direction);
                mrr.Requester = (IHasIdentity)otherGuysPort.Owner;
                alMaterialResourceRequests.Add(mrr);
            }
            mrra = (MaterialResourceRequest[])alMaterialResourceRequests.ToArray(typeof(MaterialResourceRequest));
            #endregion

            #region >>> Create Capacity Resource Request (crr). <<<
            crr = new SimpleResourceRequest(deliveryRate);
            crr.Requester = (IHasIdentity)otherGuysPort.Owner;
            #endregion
            #endregion

            #region >>> Acquire all resources without deadlock. <<<
            // We will maintain a queue of reservationPairs. The first one in the
            // queue is reserved with a wait-lock, and subsequent RP's are reserved
            // without a wait lock. If a reservation succeeds, then the RP is requeued
            // at the end of the queue. If it fails, then all RP's in the queue are
            // unreserved, and the next attempt begins at the beginning of the queue.
            // -
            // Note that in this next attempt, the one for whom reservation has most
            // recently failed is still at the head of the queue, and is the one that
            // is reserved with a wait-lock.

            Queue rscQueue = new Queue();

            #region >>> Load the queue with the token request, material requests and capacity request. <<<
            if (ServiceTokens != null && strr != null)
                rscQueue.Enqueue(new ReservationPair(strr, ServiceTokens));
            for (int i = 0 ; i < mrra.Length ; i++) {
                rscQueue.Enqueue(new ReservationPair(mrra[i], mria[i]));
            }
            if (DeliveryCapacity != null && crr != null)
                rscQueue.Enqueue(new ReservationPair(crr, DeliveryCapacity));
            #endregion

            bool nextIsMaster = true;
            while (true && rscQueue.Count > 0) {
                ReservationPair rp = (ReservationPair)rscQueue.Peek();
                if (rp.Succeeded)
                    break; // We've acquired all of them.
                rp.Succeeded = rp.ResourceManager.Reserve(rp.ResourceRequest, nextIsMaster);
                nextIsMaster = !rp.Succeeded; // If failed, the next time through, the head of the q will be master.
                if (!rp.Succeeded) {
                    foreach (ReservationPair resetPair in rscQueue) {
                        if (resetPair.Succeeded) {
                            resetPair.ResourceManager.Unreserve(resetPair.ResourceRequest);
                            resetPair.Succeeded = false;
                        }
                    }
                } else {
                    rscQueue.Enqueue(rscQueue.Dequeue()); // Send the successful reservation to the back of the queue.
                }
            }
            if (rscQueue.Count == 0) {
                if (s_diagnostics)
                    Trace.WriteLine("No resources were requested.");
            } else {
                foreach (ReservationPair rp in rscQueue) {
                    if (!( rp.ResourceRequest is MaterialResourceRequest )) {
                        rp.ResourceManager.Unreserve(rp.ResourceRequest);
                        rp.ResourceManager.Acquire(rp.ResourceRequest, true);
                    } // We acquire the token and the capacity now, but acquire the material itself at Execution
                }
            }
            #endregion

            return new AcquisitionKey(deliveryRate, myPort, strr, mrra, crr);
        }

        public virtual IDictionary GetTransferTable(IDictionary graphContext) {
            IDictionary xferTable = (IDictionary)graphContext[m_transferTableKey];
            if ( xferTable == null ) {
                xferTable = new Hashtable();
                graphContext.Add(m_transferTableKey, xferTable);
            }
            return xferTable;
        }

        /// <summary>
        /// Performs the actual transfers in or out of this MaterialService. Setup must have been 
        /// completed beforehand, and teardown must follow completion of this call.
        /// </summary>
        /// <param name="graphContext">The graphContext of the current batch.</param>
        /// <param name="key">The object that was returned as the key from the preceding Setup call.</param>
        public void Execute(IDictionary graphContext, object key) {
            IDictionary xferTable = GetTransferTable(graphContext);
            AcquisitionKey acqKey = (AcquisitionKey)key;
            if (acqKey.MyPort is IOutputPort) { // We're charging material to the other guy.
                #region >>> Algorithm description. <<<
                // This involves two things - first, we extract the materials from the MRIs,
                // then we create a MaterialTransfer with those materials in it and place the
                // MaterialTransfer in the xferTable keyed to the connector.
                #endregion
                #region >>> Process Charge to SCR's Partner <<<
                Mixture charge = new Mixture(Model, "Transfer from " + Name, Guid.NewGuid());
                for (int i = 0 ; i < acqKey.Amrr.Length ; i++) {
                    MaterialResourceRequest mrr = acqKey.Amrr[i];
                    MaterialResourceItem mri = GetCompartment(mrr.MaterialType, mrr.MaterialSpecs);
                    double mass, temperature;
                    lock (mri) {
                        mri.Unreserve(mrr);
                        if (mri.Acquire(mrr, false)) {
                            mass = mrr.QuantityObtained;
                            temperature = m_defaultMaterialTemperature;
                            Substance chargeSubstance = (Substance)mrr.MaterialType.CreateMass(mass, temperature);
                            chargeSubstance.SetMaterialSpecs(mrr.MaterialSpecs);
                            charge.AddMaterial(chargeSubstance);
                        } else {
                            throw new ApplicationException("Could not acquire an already-reserved amount of material in MaterialService charge.");
                        }
                    }
                }
                double minutes = charge.Mass / acqKey.DeliveryRate;
                TimeSpan duration = TimeSpan.FromMinutes(double.IsNaN(minutes) ? 0 : minutes);
                #region Diagnostics
                if (s_diagnostics) {
                    Trace.WriteLine(Name + " is transferring out " + charge + " over " + duration + ", ");
                    Trace.WriteLine("\t and adding a MaterialTransfer to xferTable under key " + acqKey.MyPort.Connector.GetHashCode());
                }
                #endregion
                if (xferTable.Contains(acqKey.MyPort.Connector)) {
                    //Trace.WriteLine("Removing acquisition key for " + acqKey.m_amrr[0].MaterialType.Name);
                    xferTable.Remove(acqKey.MyPort.Connector);
                }
                xferTable.Add(acqKey.MyPort.Connector, NewMaterialTransfer(charge, duration));
                acqKey.GraphContext = graphContext;
                #endregion

            } else if (acqKey.MyPort is IInputPort) { // We're receiving material from the other guy.
                #region >>> Algorithm description. <<<
                // This involves taking the MaterialTransfer object out of the transferTable, cycling through
                // the substances in the MaterialTransfer's mixture, and adding each one to the appropriate
                // MaterialResourceItem. Adding the material to the MRI will involve creating and executing 
                // an augmentation request.
                #endregion

                //				TransferManager xferMgr = ((SOMModel)m_model).TransferManager;
                //				object xferKey = xferMgr.GetTransferKey(acqKey.m_myPort.Peer);
                //				if ( xferKey != null ) {
                //					xferMgr.WaitForTransferKey(graphContext,xferKey);
                //				}

                #region >>> Process Receipt of Discharge from SCR's Partner <<<
                MaterialTransfer mt = (MaterialTransfer)xferTable[acqKey.MyPort.Connector];
                #region Diagnostics
                if (s_diagnostics)
                    Trace.WriteLine(Name + " is transferring in " + mt.Mixture + " over " + mt.DestinationDuration);
                #endregion
                foreach (MaterialResourceRequest mrr in acqKey.Amrr) {
                    MaterialResourceItem mri = GetCompartment(mrr.MaterialType, mrr.MaterialSpecs);
                    lock (mri) {
                        mri.Unreserve(mrr);
                        mri.Acquire(mrr, true);
                    }
                }
                acqKey.GraphContext = graphContext;
                #endregion

            } else {
                throw new ApplicationException("Do not know how to execute with a port type of " + acqKey.MyPort.GetType());
            }

        }

        /// <summary>
        /// Releases all ports, capacity and connector tokens, and removes the MaterialTransfer
        /// object from the transferTable. This call must correspond 1-to-1 with any setup and 
        /// execute calls, and must follow the Execute(...) call.
        /// </summary>
        /// <param name="key">The object that was returned as the key from the original Setup call.</param>
        public void Teardown(object key) {
            Teardown(key, false);
        }

        /// <summary>
        /// Releases all ports, capacity and connector tokens, and removes the MaterialTransfer
        /// object from the transferTable. This call must correspond 1-to-1 with any setup and
        /// execute calls, and must follow the Execute(...) call.
        /// </summary>
        /// <param name="key">The object that was returned as the key from the original Setup call.</param>
        /// <param name="destroyConnection">if set to <c>true</c> the teardown will destroy the connection.</param>
        public void Teardown(object key, bool destroyConnection) {
            AcquisitionKey acqKey = (AcquisitionKey)key;

            if (DeliveryCapacity != null)
                acqKey.Dacrr.Release();
            foreach (IResourceRequest mrr in acqKey.Amrr)
                mrr.Release();
            if (ServiceTokens != null)
                acqKey.Strr.Release();

            if (destroyConnection) {
                DestroyConnection(acqKey.MyPort.Peer);
            }

        }

        /// <summary>
        /// Used to keep track of the resource requests associated with a material service request.
        /// </summary>
        protected class AcquisitionKey {
            #region >>> Public Fields <<<
            public double DeliveryRate;
            public IPort MyPort;
            public IResourceRequest Strr;
            public MaterialResourceRequest[] Amrr;
            public IResourceRequest Dacrr;
            public IDictionary GraphContext;
            #endregion
            public AcquisitionKey(/*IMaterial material,*/double deliveryRate, IPort myPort, IResourceRequest strr, MaterialResourceRequest[] amrr, IResourceRequest dacrr) {
                DeliveryRate = deliveryRate;
                MyPort = myPort;
                Strr = strr;
                Amrr = amrr;
                Dacrr = dacrr;
                GraphContext = null;
            }
        }

        /// <summary>
        /// A pair of request/target objects, used in a queue to successfully reserve all resources before 
        /// acquiring any of them. All-or-none.
        /// </summary>
        protected class ReservationPair {
            #region >>> Private Fields <<<
            private IResourceRequest m_resourceRequest;
            private IResourceManager m_resourceManager;
            private bool m_succeeded;
            #endregion

            public ReservationPair(IResourceRequest resourceRequest, IResourceManager resourceManager) {
                m_resourceRequest = resourceRequest;
                m_resourceManager = resourceManager;
                m_succeeded = false;
            }
            public IResourceRequest ResourceRequest { get { return m_resourceRequest; } set { m_resourceRequest = value; } }
            public IResourceManager ResourceManager { get { return m_resourceManager; } set { m_resourceManager = value; } }
            public bool Succeeded { get { return m_succeeded; } set { m_succeeded = value; } }
        }

        public virtual MaterialTransfer NewMaterialTransfer(Mixture mixture, TimeSpan duration) {
            return new MaterialTransfer(mixture, duration);
        }
    }

    public class MaterialTransfer {

        #region Private Fields
        private Mixture m_mixture;
        private TimeSpan m_sourceDuration;
        private TimeSpan m_destinationDuration;
        #endregion Private Fields

        /// <summary>
        /// Creates a MaterialTransfer object.
        /// </summary>
        /// <param name="mixture">The mixture being transferred.</param>
        /// <param name="duration">The duration of the transfer.</param>
        public MaterialTransfer(Mixture mixture, TimeSpan duration) {
            m_mixture = mixture;
            SourceDuration = duration;
            DestinationDuration = duration;
        }

        /// <summary>
        /// The mixture being transferred.
        /// </summary>
        public Mixture Mixture { get { return m_mixture; } }

        /// <summary>
        /// The amount of time it takes for the source to output the mixture represented in this Transfer.
        /// </summary>
        public TimeSpan SourceDuration {
            set {
                m_sourceDuration = value;
            }
            get {
                return m_sourceDuration;
            }
        }
        /// <summary>
        /// The amount of time it takes for the sink to receive the mixture represented in this Transfer.
        /// </summary>
        public TimeSpan DestinationDuration {
            set {
                m_destinationDuration = value;
            }
            get {
                return m_destinationDuration;
            }
        }
    }
}

