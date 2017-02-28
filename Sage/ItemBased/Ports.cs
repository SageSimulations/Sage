/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using _Debug = System.Diagnostics.Debug;
using System.Collections.ObjectModel;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.ItemBased.Ports {

    //TODO: We cast input & output ports as SimpleIn & SimpleOuts to get port control. This is not optimal. Need IInputPortOwnerController and IOutputPortOwnerController.

    /// <summary>
    /// This delegate receives the object passed in on a port and the set of choices to which it can be
    /// passed.
    /// </summary>
    public delegate IPort PortSelector(object data, IPortSet portSet);

    /// <summary>
    /// This is implemented by a method that will be paying attention to
    /// a port. PortData events include those occurring when data is 
    /// presented to a port, accepted by a port, or rejected by a port.
    /// </summary>
    public delegate void PortDataEvent(object data, IPort where);

    /// <summary>
    /// This is the signature of a listener to a port. PortEvents are
    /// fired when data becomes available on a port, when a port has just
    /// been pulled from or pushed to, or when someone has tried to pull
    /// from an empty port.
    /// </summary>
    public delegate void PortEvent(IPort port);

    /// <summary>
    ///  Implemented by a method designed to respond to the arrival of data
    ///  on a port.
    /// </summary>
    public delegate bool DataArrivalHandler(object data, IInputPort port);
    /// <summary>
    /// Implemented by a method designed to provide data on an external
    /// entity's requesting it from a port.
    /// </summary>
    public delegate object DataProvisionHandler(IOutputPort port, object selector);

    [FlagsAttribute]
    public enum PortDirection {
        Input,
        Output
    }

    public class GeneralPortChannelInfo : IPortChannelInfo {
        private string m_name;
        private PortDirection m_direction;
        public GeneralPortChannelInfo(string name, PortDirection direction) {
            m_name = name;
            m_direction = direction;
        }


        #region IPortChannelInfo Members

        public PortDirection Direction {
            get { return m_direction; }
        }

        public string TypeName {
            get { return m_name; }
        }

        #endregion

        public static GeneralPortChannelInfo StandardInput { get { return _stdinput; } }
        public static GeneralPortChannelInfo StandardOutput { get { return _stdoutput; } }
        public static List<IPortChannelInfo> StdInputOnly { get { return _stdinputonlylist; } }
        public static List<IPortChannelInfo> StdOutputOnly { get { return _stdoutputonlylist; } }
        public static List<IPortChannelInfo> StdInputAndOutput { get { return _stdinandoutlist; } }

        private static GeneralPortChannelInfo _stdinput = new GeneralPortChannelInfo("Input", PortDirection.Input);
        private static GeneralPortChannelInfo _stdoutput = new GeneralPortChannelInfo("Output", PortDirection.Output);
        private static List<IPortChannelInfo> _stdinputonlylist = new List<IPortChannelInfo>(new IPortChannelInfo[] { _stdinput });
        private static List<IPortChannelInfo> _stdoutputonlylist = new List<IPortChannelInfo>(new IPortChannelInfo[] { _stdoutput });
        private static List<IPortChannelInfo> _stdinandoutlist = new List<IPortChannelInfo>(new IPortChannelInfo[] { _stdinput, _stdoutput });
    }

    /// <summary>
    /// Contains and provides IPort objects based on keys. PortOwner objects (those
    /// which implement IPortOwner) will typically (though not necessarily) contain one
    /// of these.
    /// </summary>
    public class PortSet : IPortSet, IXmlPersistable {

        #region Private fields

        private IComparer<IPort> m_sortOrderComparer = null;
        private List<IPort> m_sortedPorts = null;

        private Hashtable m_ports;
        private ArrayList m_presentedListeners;
        private ArrayList m_acceptedListeners;
        private ArrayList m_rejectedListeners;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PortSet"/> class.
        /// </summary>
        /// <param name="useCaseInsensitiveKeys">if set to <c>true</c> the portSet will use case insensitive keys.</param>
        public PortSet(bool useCaseInsensitiveKeys) {
            if (useCaseInsensitiveKeys) {
                m_ports = new Hashtable();
            } else {
                m_ports = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
            }
            m_presentedListeners = new ArrayList();
            m_acceptedListeners = new ArrayList();
            m_rejectedListeners = new ArrayList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortSet"/> class with case-sensitive keys.
        /// </summary>
        public PortSet() : this(false) { }

        /// <summary>
        /// Adds a port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        public void AddPort(IPort port) {

            if (m_ports.ContainsValue(port)) {
                return; // This PO already owns the port.
            }

            if (this[port.Name] == null) {
                int ndx = 0;
                if (port.Index == GenericPort.UnassignedIndex) {
                    foreach (IPort p in m_ports.Values) ndx = Math.Max(ndx, p.Index);
                    port.Index = ndx + 1;
                }
                m_ports.Add(port.Guid, port);
                SortedPorts = null;
                foreach (PortDataEvent dce in m_presentedListeners)
                    port.PortDataPresented += dce;
                foreach (PortDataEvent dce in m_acceptedListeners)
                    port.PortDataAccepted += dce;
                foreach (PortDataEvent dce in m_presentedListeners)
                    port.PortDataRejected += dce;
                if (m_bcmListeners != null)
                    foreach (PortEvent pe in m_bcmListeners)
                        port.BeforeConnectionMade += pe;
                if (m_bcbListeners != null)
                    foreach (PortEvent pe in m_bcbListeners)
                        port.BeforeConnectionBroken += pe;
                if (m_acmListeners != null)
                    foreach (PortEvent pe in m_acmListeners)
                        port.AfterConnectionMade += pe;
                if (m_acbListeners != null)
                    foreach (PortEvent pe in m_acbListeners)
                        port.AfterConnectionBroken += pe;

                if (PortAdded != null) {
                    PortAdded(port);
                }

            } else {
                string msg = string.Format("Caller attempting to add a second port with name {0} to {1}.", port.Name, port.Owner);
                throw new ApplicationException(msg);
            }
        }

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective.
        /// </summary>
        /// <param name="port">The port to be removed from the portSet.</param>
        public void RemovePort(IPort port) {

            if (!m_ports.ContainsValue(port)) {
                return; // This PO does not own the port.
            }

            m_ports.Remove(port.Guid);
            SortedPorts = null;
            foreach (PortDataEvent dce in m_presentedListeners)
                port.PortDataPresented -= dce;
            foreach (PortDataEvent dce in m_acceptedListeners)
                port.PortDataAccepted -= dce;
            foreach (PortDataEvent dce in m_presentedListeners)
                port.PortDataRejected -= dce;
            if (m_bcmListeners != null)
                foreach (PortEvent pe in m_bcmListeners)
                    port.BeforeConnectionMade -= pe;
            if (m_bcbListeners != null)
                foreach (PortEvent pe in m_bcbListeners)
                    port.BeforeConnectionBroken -= pe;
            if (m_acmListeners != null)
                foreach (PortEvent pe in m_acmListeners)
                    port.AfterConnectionMade -= pe;
            if (m_acbListeners != null)
                foreach (PortEvent pe in m_acbListeners)
                    port.AfterConnectionBroken -= pe;

            if (PortRemoved != null) {
                PortRemoved(port);
            }

        }

        /// <summary>
        /// Fired when a port has been added to this IPortSet.
        /// </summary>
        public event PortEvent PortAdded;

        /// <summary>
        /// Fired when a port has been removed from this IPortSet.
        /// </summary>
        public event PortEvent PortRemoved;


        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        public void ClearPorts() {
            ArrayList ports = new ArrayList(m_ports.Values);
            foreach (IPort port in ports) {
                RemovePort(port);
            }
        }

        /// <summary>
        /// This event is fired when data is presented to any input port in this
        /// PortSet from outside, or to any output port from inside.
        /// </summary>
        public event PortDataEvent PortDataPresented {
            add {
                m_presentedListeners.Add(value);
                foreach (IPort port in m_ports)
                    port.PortDataPresented += value;
            }
            remove {
                m_presentedListeners.Remove(value);
                foreach (IPort port in m_ports)
                    port.PortDataPresented -= value;
            }
        }

        /// <summary>
        /// This event is fired whenever any input port accepts data presented to it
        /// from outside or any output port accepts data presented to it from inside. 
        /// </summary>
        public event PortDataEvent PortDataAccepted {
            add {
                m_acceptedListeners.Add(value);
                foreach (IPort port in m_ports)
                    port.PortDataAccepted += value;
            }
            remove {
                m_acceptedListeners.Remove(value);
                foreach (IPort port in m_ports)
                    port.PortDataAccepted -= value;
            }
        }

        /// <summary>
        /// This event is fired whenever an input port rejects data that is presented
        /// to it from outside or an output port rejects data that is presented to it
        /// from inside.
        /// </summary>
        public event PortDataEvent PortDataRejected {
            add {
                m_rejectedListeners.Add(value);
                foreach (IPort port in m_ports)
                    port.PortDataRejected += value;
            }
            remove {
                m_rejectedListeners.Remove(value);
                foreach (IPort port in m_ports)
                    port.PortDataRejected -= value;
            }
        }

        #region Port Made/Broken Event Management
        private ArrayList m_bcmListeners, m_acmListeners, m_bcbListeners, m_acbListeners;
        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent BeforeConnectionMade {
            add {
                if (m_bcmListeners == null)
                    m_bcmListeners = new ArrayList();
                m_bcmListeners.Add(value);
                foreach (IPort port in m_ports)
                    port.BeforeConnectionMade += value;
            }
            remove {
                m_bcmListeners.Remove(value);
                foreach (IPort port in m_ports)
                    port.BeforeConnectionMade -= value;
            }
        }

        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent AfterConnectionMade {
            add {
                if (m_acmListeners == null)
                    m_acmListeners = new ArrayList();
                m_acmListeners.Add(value);
                foreach (IPort port in m_ports)
                    port.AfterConnectionMade += value;
            }
            remove {
                m_acmListeners.Remove(value);
                foreach (IPort port in m_ports)
                    port.AfterConnectionMade -= value;
            }
        }


        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
        public event PortEvent BeforeConnectionBroken {
            add {
                if (m_bcbListeners == null)
                    m_bcbListeners = new ArrayList();
                m_bcbListeners.Add(value);
                foreach (IPort port in m_ports)
                    port.BeforeConnectionBroken += value;
            }
            remove {
                m_bcbListeners.Remove(value);
                foreach (IPort port in m_ports)
                    port.BeforeConnectionBroken -= value;
            }
        }

        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
        public event PortEvent AfterConnectionBroken {
            add {
                if (m_acbListeners == null)
                    m_acbListeners = new ArrayList();
                m_acbListeners.Add(value);
                foreach (IPort port in m_ports)
                    port.AfterConnectionBroken += value;
            }
            remove {
                m_acbListeners.Remove(value);
                foreach (IPort port in m_ports)
                    port.AfterConnectionBroken -= value;
            }
        }
        #endregion

        /// <summary>
        /// Returns a collection of the keys that belong to ports known to this PortSet.
        /// </summary>
        public ICollection PortKeys { get { return m_ports.Keys; } }

        /// <summary>
        /// Looks up the key associated with a particular port.
        /// </summary>
        /// <param name="port">The port for which we want the key.</param>
        /// <returns>The key for the provided port.</returns>
        [Obsolete("Ports use their Guids as the key.")]
        public Guid GetKey(IPort port) {
            return port.Guid;
        }

        /// <summary>
        /// Gets the count of all kinds of ports in this collection.
        /// </summary>
        /// <value>The count.</value>
        public int Count {
            [DebuggerStepThrough]
            get {
                return m_ports.Count;
            }
        }

        /// <summary>
        /// Returns the port associated with the provided key.
        /// </summary>
        public IPort this[Guid key] {
            [DebuggerStepThrough]
            get { return (IPort)m_ports[key]; }
        }

        /// <summary>
        /// Gets the <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/> with the specified index, i.
        /// </summary>
        /// <value>The <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/>.</value>
        public IPort this[int i] { 
            [DebuggerStepThrough]
            get {
                return SortedPorts[i];
            }
        }

        /// <summary>
        /// Returns the port associated with the provided name.
        /// </summary>
        public IPort this[string name] {
            get {
                foreach (IPort port in m_ports.Values) {
                    if (port.Name.Equals(name)) {
                        return port;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Provides an enumerator over the IPort instances.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator() { return SortedPorts.GetEnumerator(); }

        /// <summary>
        /// Gets the output ports owned by this PortSet.
        /// </summary>
        /// <value>The output ports.</value>
        public ReadOnlyCollection<IOutputPort> Outputs {
            get {
                List<IOutputPort> outs = new List<IOutputPort>();
                SortedPorts.ForEach(delegate(IPort port) { if (port is IOutputPort) outs.Add((IOutputPort)port); });
                return outs.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the input ports owned by this PortSet.
        /// </summary>
        /// <value>The input ports.</value>
        public ReadOnlyCollection<IInputPort> Inputs {
            get {
                List<IInputPort> ins = new List<IInputPort>();
                SortedPorts.ForEach(delegate(IPort port) { if (port is IInputPort) ins.Add((IInputPort)port); });
                return ins.AsReadOnly();
            }
        }

        #region IXmlPersistable Members

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void SerializeTo(XmlSerializationContext xmlsc) {
            xmlsc.StoreObject("Ports", m_ports);
        }

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc) {
            m_ports = (Hashtable)xmlsc.LoadObject("Ports");
        }

        #endregion

        /// <summary>
        /// Gets or sets the internal list of sorted ports.
        /// </summary>
        /// <value>The sorted ports.</value>
        private List<IPort> SortedPorts {
            get {
                if (m_sortedPorts == null) {
                    m_sortedPorts = new List<IPort>();
                    foreach (IPort port in m_ports.Values) {
                        m_sortedPorts.Add(port);
                    }
                    if (m_sortOrderComparer != null) {
                        m_sortedPorts.Sort(m_sortOrderComparer);
                    }
                }
                return m_sortedPorts;
            }
            set { m_sortedPorts = value; }
        }

        /// <summary>
        /// Sorts the ports based on one element of their Out-of-band data sets.
        /// Following a return from this call, the ports will be in the order requested.
        /// The "T" parameter will usually be int, double or string, but it must
        /// represent the IComparable-implementing type of the data stored under the
        /// provided OOBDataKey.
        /// </summary>
        /// <param name="oobDataKey">The oob data key.</param>
        public void SetSortOrder<T>(object oobDataKey) where T : IComparable {
            m_sortOrderComparer = new ByOobDataComparer<T>(oobDataKey);
        }

        class ByOobDataComparer<T> : IComparer<IPort> where T : IComparable {
            private object m_oobDataKey;
            public ByOobDataComparer(object oobDataKey) {
                m_oobDataKey = oobDataKey;
            }


            #region IComparer<IPort> Members

            public int Compare(IPort x, IPort y) {
                object obx = x.GetOutOfBandData(m_oobDataKey);
                object oby = y.GetOutOfBandData(m_oobDataKey);

                if (obx == null && oby == null) {
                    return 0;
                }

                IComparable icx = obx as IComparable;
                IComparable icy = oby as IComparable;

                if ( icx == null && icy == null ) {
                    string errMsg = string.Format("Attempt to sort port list on key {0} which is of type {1}, which does not implement IComparable and it must do so, in order to sort on it.",
                        m_oobDataKey,(obx==null?oby.GetType().FullName:obx.GetType().FullName));
                    throw new ApplicationException(errMsg);
                }



                if (icx == null) {
                    return -1;
                }

                if (icy == null) {
                    return 1;
                }

                return icx.CompareTo(icy);
            }

            #endregion
        }
    }

    /// <summary>
    /// Base class implementation for ports.
    /// </summary>
    public abstract class GenericPort : IPort {

        #region Private Fields
        private IPortOwner m_owner;
        private IConnector m_connector;
        private int m_makeBreakListeners = 0;
        private bool m_intrinsic = false;
        private object m_defaultOutOfBandData;
        private Hashtable m_outOfBandData;
        private int m_portIndex = UnassignedIndex;
        #endregion

        protected bool HasBeenDetached = false;

        /// <summary>
        /// Creates a port with a given owner. It is the responsibility of the creator to add the port to the
        /// owner's PortSet.
        /// </summary>
        /// <param name="model">The model in which the port exists.</param>
        /// <param name="name">The name of the port.</param>
        /// <param name="guid">The GUIDof the port.</param>
        /// <param name="owner">The IPortOwner that will own this port.</param>
        public GenericPort(IModel model, string name, Guid guid, IPortOwner owner) {
            if (name == null || name.Equals(string.Empty) && owner != null) {
                name = GetNextName(owner);
            }
            InitializeIdentity(model, name, null, guid);

            m_owner = owner;
            // The following was removed 20070322 when as a result of AddPort API additions, it was seen that
            // an AddPort resulted in creation of a Port, and subequent callback into another AddPort API. This
            // was duplicitous and ambiguous. Henceforth, 
            //if (m_owner != null) {
            //    m_owner.AddPort(this);
            //}
            if (m_owner != null && m_owner.Ports[guid] == null) {
                owner.AddPort(this);
            }

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Detaches any data arrival, peek, push, etc. handlers.
        /// </summary>
        public abstract void DetachHandlers();

        /// <summary>
        /// Gets the next port name for the specified portOwner. If has (Input_0, Input_3 and Input_9) next is Input_10.
        /// </summary>
        /// <param name="owner">The prospective new IPortOwner for the port in question.</param>
        /// <returns></returns>
        private string GetNextName(IPortOwner owner) {
            int i = 0;
            foreach (IPort port in owner.Ports) {
                if (port.Name.StartsWith(PortPrefix)) {
                    int tmp = 0;
                    if (int.TryParse(port.Name.Substring(PortPrefix.Length), out tmp)) {
                        i = Math.Max(i, tmp);
                    }
                }
            }
            return PortPrefix + i;
        }

        /// <summary>
        /// Gets the default naming prefix for all ports of this type.
        /// </summary>
        /// <value>The port prefix.</value>
        protected abstract string PortPrefix { get; }

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        public int Index { get { return m_portIndex; } set { m_portIndex = value; } }

        /// <summary>
        /// The connector, if any, to which this port is attached. If there is already a connector,
        /// then the setter is allowed to set Connector to &lt;null&gt;. Thereafter, the setter will
        /// be permitted to set the connector to a new value. This is to prevent accidentally
        /// overwriting a connection in code.
        /// </summary>
        public IConnector Connector {
            [DebuggerStepThrough]get {
                return m_connector;
            }
            set {
                // We want to prevent overwriting an existing connection.
                if (m_connector != null && value != null) {
                    if (m_connector.Equals(value))
                        return;
                    #region Create an informative text message.
                    string myKey = Key.ToString();
                    string myOwner = PossibleIHasIdentityAsString(m_owner);
                    string peerKey;
                    string peerOwner;
                    string newPeerKey;
                    string newPeerOwner;
                    if (this is IInputPort) {
                        peerKey = Connector.Upstream == null ? "<null>" : Connector.Upstream.Key.ToString();
                        peerOwner = Connector.Upstream == null ? "<null>" : PossibleIHasIdentityAsString(Connector.Upstream.Owner);
                        newPeerKey = value.Upstream == null ? "<null>" : value.Upstream.Key.ToString();
                        newPeerOwner = value.Upstream == null ? "<null>" : PossibleIHasIdentityAsString(value.Upstream.Owner);
                    } else {
                        peerKey = Connector.Downstream == null ? "<null>" : Connector.Downstream.Key.ToString();
                        peerOwner = Connector.Downstream == null ? "<null>" : PossibleIHasIdentityAsString(Connector.Downstream.Owner);
                        newPeerKey = value.Downstream == null ? "<null>" : value.Downstream.Key.ToString();
                        newPeerOwner = value.Downstream == null ? "<null>" : PossibleIHasIdentityAsString(value.Downstream.Owner);
                    }

                    string errMsg;
                    try {
                        errMsg = string.Format("Trying to add a connector to a port {0} on {1}, where that port is already connected "
                            + "to another port {2} on {3}. The connector being added is connected on the other end, to {4} on {5}",
                            /* my guid/port key */ myKey,
                            myOwner,
                            /* his guid/port key */ peerKey,
                            peerOwner,
                            /* new other's guid/port key */ newPeerKey,
                            newPeerOwner);
                    } catch (NullReferenceException nre) {
                        errMsg = "Trying to add a connector to a port that already has one.\r\n" + nre.Message;
                    }
                    #endregion
                    throw new ApplicationException(errMsg);
                }

                if (m_makeBreakListeners > 0) {
                    if (value == null && m_beforeConnectionBroken != null)
                        m_beforeConnectionBroken(this);
                    if (value != null && m_beforeConnectionMade != null)
                        m_beforeConnectionMade(this);
                    m_connector = value;
                    if (value == null && m_afterConnectionBroken != null)
                        m_afterConnectionBroken(this);
                    if (value != null && m_afterConnectionMade != null)
                        m_afterConnectionMade(this);
                } else {
                    m_connector = value;
                }
            }
        }

        #region Port Made/Broken Event Management
        private event PortEvent m_beforeConnectionMade;
        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent BeforeConnectionMade {
            add {
                m_makeBreakListeners++;
                m_beforeConnectionMade += value;
            }
            remove {
                m_makeBreakListeners--;
                m_beforeConnectionMade -= value;
            }
        }

        private event PortEvent m_afterConnectionMade;
        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent AfterConnectionMade {
            add {
                m_makeBreakListeners++;
                m_afterConnectionMade += value;
            }
            remove {
                m_makeBreakListeners--;
                m_afterConnectionMade -= value;
            }
        }


        private event PortEvent m_beforeConnectionBroken;
        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
        public event PortEvent BeforeConnectionBroken {
            add {
                m_makeBreakListeners++;
                m_beforeConnectionBroken += value;
            }
            remove {
                m_makeBreakListeners--;
                m_beforeConnectionBroken -= value;
            }
        }

        private event PortEvent m_afterConnectionBroken;
        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
        public event PortEvent AfterConnectionBroken {
            add {
                m_makeBreakListeners++;
                m_afterConnectionBroken += value;
            }
            remove {
                m_makeBreakListeners--;
                m_afterConnectionBroken -= value;
            }
        }
        #endregion

        /// <summary>
        /// This port's owner.
        /// </summary>
        public IPortOwner Owner { get { return m_owner; } }

        /// <summary>
        /// Returns the key by which this port is known to its owner.
        /// </summary>
        /// <value></value>
        public Guid Key { get { return Guid; } }

        /// <summary>
        /// This event fires when data is presented on a port. For an input port, this
        /// implies presentation by an outsider, and for an output port, it implies 
        /// presentation by the port owner.
        /// </summary>
        public event PortDataEvent PortDataPresented;

        /// <summary>
        /// This event fires when data is accepted by a port. For an input port, this
        /// implies acceptance by an outsider, and for an output port, it implies 
        /// acceptance by the port owner.
        /// </summary>
        public event PortDataEvent PortDataAccepted;

        /// <summary>
        /// This event fires when data is rejected by a port. For an input port, this
        /// implies rejection by an outsider, and for an output port, it implies 
        /// rejection by the port owner.
        /// </summary>
        public event PortDataEvent PortDataRejected;

        /// <summary>
        /// Handler for arrival of data. For an output port, this will be the PortOwner
        /// presenting data to the port, for an input port, it will be the IPort's peer
        /// presenting data through the connector.
        /// </summary>
        /// <param name="data">The data being transmitted.</param>
        protected void OnPresentingData(object data) { 
            if (PortDataPresented != null) PortDataPresented(data, this);
        }

        /// <summary>
        /// Handler for the acceptance of data. For an output port, this will be the port
        /// accepting data from the port owner, and for an input port, it will be the port's peer
        /// accepting data offered through the connector by this port.
        /// </summary>
        /// <param name="data">The data being transmitted.</param>
        protected void OnAcceptingData(object data) {
            if (PortDataAccepted != null) PortDataAccepted(data, this);
        }

        /// <summary>
        /// Handler for the acceptance of data. For an output port, this will be the port
        /// accepting data from the port owner, and for an input port, it will be the port's peer
        /// accepting data offered through the connector by this port.
        /// </summary>
        /// <param name="data">The data being transmitted.</param>
        protected void OnRejectingData(object data) {
            if (PortDataRejected != null) PortDataRejected(data, this);
        }

        /// <summary>
        /// Returns the default out-of-band data from this port. Out-of-band data
        /// is data that is not material that is to be transferred out of, or into,
        /// this port, but rather context, type, or other metadata to the transfer
        /// itself.
        /// </summary>
        /// <returns>The default out-of-band data from this port.</returns>
        public object GetOutOfBandData() {
            return m_defaultOutOfBandData;
        }

        /// <summary>
        /// Returns out-of-band data from this port. Out-of-band data is data that is
        /// not material that is to be transferred out of, or into, this port, but
        /// rather context, type, or other metadata to the transfer itself.
        /// </summary>
        /// <param name="key">The key of the sought metadata.</param>
        /// <returns></returns>
        public object GetOutOfBandData(object key) {
            if (m_outOfBandData == null)
                return null;
            if (!m_outOfBandData.ContainsKey(key)) {
                return null;
            }
            return m_outOfBandData[key];
        }

        /// <summary>
        /// Sets the default out-of-band data.
        /// </summary>
        /// <param name="defaultOobData">The default out-of-band data.</param>
        public void SetDefaultOutOfBandData(object defaultOobData) {
            m_defaultOutOfBandData = defaultOobData;
        }

        /// <summary>
        /// Sets an out-of-band data item based on its key.
        /// </summary>
        /// <param name="key">The key through which the out-of-band data is to be returned.</param>
        /// <param name="outOfBandData">The out-of-band data associated with the above key.</param>
        public void SetOutOfBandData(object key, object outOfBandData) {
            if (m_outOfBandData == null)
                m_outOfBandData = new Hashtable();
            if (m_outOfBandData.Contains(key)) {
                m_outOfBandData[key] = outOfBandData;
            } else {
                m_outOfBandData.Add(key, outOfBandData);
            }
        }

        /// <summary>
        /// Returns the peer of this port. A port's peer is the port
        /// that is at the other end of the connector to which this
        /// port is attached, or null if there is no attached conenctor
        /// or if there is no port on the other end.
        /// </summary>
        public IPort Peer {
            get {
                if (m_connector == null)
                    return null;
                if (m_connector.Upstream == null)
                    return null;
                if (m_connector.Upstream.Equals(this))
                    return m_connector.Downstream;
                return m_connector.Upstream;
            }
        }

        /// <summary>
        /// Gets and sets a value indicating whether this <see cref="IPort"/> is intrinsic. An intrinsic
        /// port is a hard-wired part of its owner. It is there when its owner is created, and
        /// cannot be removed.
        /// </summary>
        /// <value><c>true</c> if intrinsic; otherwise, <c>false</c>.</value>
        public bool Intrinsic {
            get {
                return m_intrinsic;
            }
            set {
                m_intrinsic = value;
            }
        }

        /// <summary>
        /// When a port index is this value upon being added to a PortSet, that PortSet will assign a sequential index value.
        /// </summary>
        public static int UnassignedIndex = -1;

        #region Implementation of IModelObject
        private IModel m_model;
        private string m_name = null;
        private Guid m_guid = Guid.Empty;
        private string m_description = null;
        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        /// <value></value>
        public string Name { [DebuggerStepThrough]get { return m_name; } }
        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid { [DebuggerStepThrough] get { return m_guid; } }
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value></value>
        public IModel Model { [DebuggerStepThrough] get { return m_model; } }
        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
        public string Description => (m_description ?? ("No description for " + m_name));

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

        protected void DetachedPortInUse(){
            string errMsg = string.Format("The port {0}, owned by {1} is being used by someone, but it has been detached from its owner.",
                Name, PossibleIHasIdentityAsString(Owner));
            throw new ApplicationException(errMsg);
        }

        private string PossibleIHasIdentityAsString(object obj) {
            if (obj == null) {
                return "<null>";
            } else if (obj is IHasIdentity) {
                return ( (IHasIdentity)m_owner ).Name;
            } else {
                return obj.GetType().ToString();
            }
        }
    }

    /// <summary>
    /// Class InputPortProxy is a class that represents to an outer container
    /// the functionality of an input port on an internal port owner. This can be used
    /// to expose the externally-visible ports from a network of blocks that is
    /// being represented as one container-level block.    /// </summary>
    public class InputPortProxy : IInputPort {

        #region Private Fields
        private IInputPort m_ward;
        private IPortOwner m_owner;
        private IConnector m_externalConnector;
        private IConnector m_internalConnector;
        private IOutputPort m_wardPartner;
        private PortSet m_portSet;
        private IPortOwner m_internalPortOwner;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="InputPortProxy"/> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:InputPortProxy"/> will run.</param>
        /// <param name="name">The name of the new <see cref="T:InputPortProxy"/>.</param>
        /// <param name="description">The description of the new <see cref="T:InputPortProxy"/>.</param>
        /// <param name="guid">The GUID of the new <see cref="T:InputPortProxy"/>.</param>
        /// <param name="owner">The owner of this proxy port.</param>
        /// <param name="ward">The ward - the internal port which this proxy port will represent.</param>
        public InputPortProxy(IModel model, string name, string description, Guid guid, IPortOwner owner, IInputPort ward) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);

            m_portSet = new PortSet();
            m_internalPortOwner = new PortOwnerProxy(name + ".Internal");
            m_wardPartner = new SimpleOutputPort(model, name + ".WardPartner", Guid.NewGuid(), m_internalPortOwner, new DataProvisionHandler(_takeHandler), new DataProvisionHandler(_peekHandler));
            m_ward = ward;
            m_internalConnector = new BasicNonBufferedConnector(m_wardPartner, m_ward);
            m_owner = owner;
            m_externalConnector = null;

            IMOHelper.RegisterWithModel(this);
        }

        private object _takeHandler(IOutputPort from, object selector) {
            if (m_externalConnector != null) {
                return m_externalConnector.Take(selector);
            } else {
                return false;
            }
        }

        private object _peekHandler(IOutputPort from, object selector) {
            if (m_externalConnector != null) {
                return m_externalConnector.Peek(selector);
            } else {
                return false;
            }
        }

        #region IInputPort Members

        /// <summary>
        /// This method attempts to place the provided data object onto the port from
        /// upstream of its owner. It will succeed if the port is unoccupied, or if
        /// the port is occupied and the port permits overwrites.
        /// </summary>
        /// <param name="obj">the data object</param>
        /// <returns>True if successful. False if it fails.</returns>
        public bool Put(object obj) {
            if (m_internalConnector != null) {
                return m_internalConnector.Put(obj);
            } else {
                return false;
            }
        }

        /// <summary>
        /// This is called by a peer to let the input port know that there is data
        /// available at the peer, in case the input port wants to pull the data.
        /// </summary>
        public void NotifyDataAvailable() {
            if (m_internalConnector != null) {
                m_internalConnector.NotifyDataAvailable();
            }
        }

        /// <summary>
        /// This sets the DataArrivalHandler that this port will use, replacing the current
        /// one. This should be used only by objects under the control of, or owned by, the
        /// IPortOwner that owns this port.
        /// </summary>
        /// <value>The new dataArrivalHandler.</value>
        public DataArrivalHandler PutHandler {
            get {
                return m_ward.PutHandler;
            }
            set {
                m_ward.PutHandler = value;
            }
        }

        #endregion

        #region IPort Members

        /// <summary>
        /// This property represents the connector object that this port is associated with.
        /// </summary>
        /// <value></value>
        public IConnector Connector {
            get {
                return m_externalConnector;
            }
            set {
                m_externalConnector = value;
                if (value == null) {
                    m_ward.Connector = null;
                } else {
                    m_ward.Connector = m_internalConnector;
                }
            }
        }

        /// <summary>
        /// This property contains the owner of the port.
        /// </summary>
        /// <value></value>
        public IPortOwner Owner {
            get { return m_owner; }
        }

        /// <summary>
        /// Returns the key by which this port is known to its owner.
        /// </summary>
        /// <value></value>
        public Guid Key {
            get { return Guid; }
        }

        /// <summary>
        /// This property returns the port at the other end of the connector to which this
        /// port is connected, or null, if there is no connector, and/or no port on the
        /// other end of a connected connector.
        /// </summary>
        /// <value></value>
        public IPort Peer {
            get { return Connector.Upstream; }
        }

        /// <summary>
        /// Returns the default out-of-band data from this port. Out-of-band data
        /// is data that is not material that is to be transferred out of, or into,
        /// this port, but rather context, type, or other metadata to the transfer
        /// itself.
        /// </summary>
        /// <returns>
        /// The default out-of-band data from this port.
        /// </returns>
        public object GetOutOfBandData() {
            return m_ward.GetOutOfBandData();
        }

        /// <summary>
        /// Returns out-of-band data from this port. Out-of-band data is data that is
        /// not material that is to be transferred out of, or into, this port, but
        /// rather context, type, or other metadata to the transfer itself.
        /// </summary>
        /// <param name="selector">The key of the sought metadata.</param>
        /// <returns>The desired out-of-band metadata.</returns>
        public object GetOutOfBandData(object selector) {
            return m_ward.GetOutOfBandData(selector);
        }

        /// <summary>
        /// Gets and sets a value indicating whether this <see cref="IPort"/> is intrinsic. An intrinsic
        /// port is a hard-wired part of its owner. It is there when its owner is created, and
        /// cannot be removed.
        /// </summary>
        /// <value><c>true</c> if intrinsic; otherwise, <c>false</c>.</value>
        public bool Intrinsic {
            get {
                return m_ward.Intrinsic;
            }
        }

        public void DetachHandlers() {
            IInputPort iip = m_internalConnector.Downstream;
            IPortOwner ipo = iip.Owner;
            m_internalConnector.Disconnect();
            ipo.RemovePort(iip);
            iip.DetachHandlers();
        }

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        public int Index { get { return m_ward.Index; } set { m_ward.Index = value; } }

        #endregion

        #region IPortEvents Members

        public event PortDataEvent PortDataPresented {
            add {
                m_ward.PortDataPresented += value;
            }
            remove {
                m_ward.PortDataPresented -= value;
            }
        }

        public event PortDataEvent PortDataAccepted {
            add {
                m_ward.PortDataAccepted += value;
            }
            remove {
                m_ward.PortDataAccepted -= value;
            }
        }

        public event PortDataEvent PortDataRejected {
            add {
                m_ward.PortDataRejected += value;
            }
            remove {
                m_ward.PortDataRejected -= value;
            }
        }

        public event PortEvent BeforeConnectionMade {
            add {
                m_ward.BeforeConnectionMade += value;
            }
            remove {
                m_ward.BeforeConnectionMade -= value;
            }
        }

        public event PortEvent AfterConnectionMade {
            add {
                m_ward.AfterConnectionMade += value;
            }
            remove {
                m_ward.AfterConnectionMade -= value;
            }
        }

        public event PortEvent BeforeConnectionBroken {
            add {
                m_ward.BeforeConnectionBroken += value;
            }
            remove {
                m_ward.BeforeConnectionBroken -= value;
            }
        }

        public event PortEvent AfterConnectionBroken {
            add {
                m_ward.AfterConnectionBroken += value;
            }
            remove {
                m_ward.AfterConnectionBroken -= value;
            }
        }


        #endregion

        #region Implementation of IModelObject
        private string m_name = null;
        private Guid m_guid = Guid.Empty;
        private IModel m_model;
		private string m_description = null;
        
        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model { [DebuggerStepThrough] get { return m_model; } }
       
        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name { [DebuggerStepThrough]get { return m_name; } }
        
        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
		public string Description { [DebuggerStepThrough] get { return ((m_description==null)?("No description for " + m_name):m_description); } }
        
        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid { [DebuggerStepThrough] get { return m_guid; } }

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

    }

    /// <summary>
    /// A simple implementation of input port. A data arrival handler may be defined to
    /// react to data that has been pushed from its peer - if none is provided, then it 
    /// cannot accept pushed data, (i.e. can only pull data from its peer at the request
    /// of its owner.) 
    /// </summary>
    public class SimpleInputPort : GenericPort, IInputPort {
        /// <summary>
        /// Creates a simple input port with a specified owner and handler to be called
        /// when data arrives on the port. If the handler is null, then an internal handler
        /// is used that, in effect, refuses delivery of the data.
        /// It is the responsibility of the creator to add the port to the owner's PortSet.
        /// </summary>
        /// <param name="model">The model in which this port participates.</param>
        /// <param name="name">The name of the port. This is typically required to be unique within an owner.</param>
        /// <param name="guid">The GUID of the port - also known to the PortOwner as the port's Key.</param>
        /// <param name="owner">The IPortOwner that owns this port.</param>
        /// <param name="dah">The DataArrivalHandler that will respond to data arriving on
        /// this port having been pushed from its peer.</param>
        public SimpleInputPort(IModel model, string name, Guid guid, IPortOwner owner, DataArrivalHandler dah)
            : base(model, name, guid, owner) {
            if (dah != null) {
                m_dataArrivalHandler = dah;
            } else {
                m_dataArrivalHandler = new DataArrivalHandler(CantAcceptPushedData);
            }
        }

        /// <summary>
        /// This event is fired when new data is available to be taken from a port.
        /// </summary>
        public event PortEvent DataAvailable;

        private bool CantAcceptPushedData(object data, IInputPort ip) { return false; }
        private DataArrivalHandler m_dataArrivalHandler;

        #region Implementation of IInputPort
        /// <summary>
        /// Called by this port's peer when it is pushing data to this port.
        /// </summary>
        /// <param name="newData">The data being pushed to the port from its peer.</param>
        /// <returns>true if this port is accepting the data, otherwise false.</returns>
        public bool Put(object newData) {
            if (HasBeenDetached) {
                DetachedPortInUse();
            }
            OnPresentingData(newData);
            bool b = m_dataArrivalHandler(newData, this);
            if (b)
                OnAcceptingData(newData);
            else
                OnRejectingData(newData);
            return b;
        }

        /// <summary>
        /// Called by the peer output port to let the input port know that data is available
        /// on the output port, in case the input port wants to pull that data.
        /// </summary>
        public void NotifyDataAvailable() {
            if (HasBeenDetached) {
                DetachedPortInUse();
            }
            if (DataAvailable != null)
                DataAvailable(this);
        }

        /// <summary>
        /// This sets the DataArrivalHandler that this port will use, replacing the current
        /// one. This should be used only by objects under the control of, or owned by, the
        /// IPortOwner that owns this port.
        /// </summary>
        /// <value>The DataArrivalHandler.</value>
        public DataArrivalHandler PutHandler {
            get {
                return m_dataArrivalHandler;
            }
            set {
                m_dataArrivalHandler = value;
            }
        }

        #endregion

        /// <summary>
        /// Gets the default naming prefix for all ports of this type.
        /// </summary>
        /// <value>The port prefix.</value>
        protected override string PortPrefix {
            get { return "Input_"; }
        }

        /// <summary>
        /// The port owner can use this API to look at, but not remove, what is on
        /// the upstream port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>A reference to the object, if any, that is on the upstream port.</returns>
        public object OwnerPeek(object selector) {
            if (HasBeenDetached) {
                DetachedPortInUse();
            }
            return Connector.Peek(selector);
        }
        /// <summary>
        /// The owner of an Input Port uses this to remove an object from the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The object that heretofore was on the input port.</returns>
        public object OwnerTake(object selector) {
            if (HasBeenDetached) {
                DetachedPortInUse();
            }
            object obj = Connector.Take(selector);
            if (obj != null) {
                OnPresentingData(obj);
                OnAcceptingData(obj);
            }
            return obj;
        }

        /// <summary>
        /// Detaches this input port's data arrival handler.
        /// </summary>
        public override void DetachHandlers() {
            m_dataArrivalHandler = null;
        }
    }

    /// <summary>
    /// Class PlaceholderPortOwner is a class to which the duties of PortOwner can be delegated.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.ItemBased.Ports.IPortOwner" />
    /// <seealso cref="Highpoint.Sage.SimCore.IHasName" />
    internal class PortOwnerProxy : IPortOwner, IHasName {
        private string m_name;

        private PortSet m_portSet = new PortSet();

        public PortOwnerProxy(string name) {
            m_name = name;
        }
        #region IPortOwner Members

        /// <summary>
        /// Adds a user-created port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        public void AddPort(IPort port) {
            m_portSet.AddPort(port);
        }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective. Implementers are responsible to refuse removal of a port that
        /// is a hard property exposed (e.g. this.InputPort0), since it will remain
        /// accessible via that property.
        /// </summary>
        /// <param name="port">The port.</param>
        public void RemovePort(IPort port) {
            m_portSet.RemovePort(port);
        }

        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        public void ClearPorts() {
            m_portSet.ClearPorts();
        }

        /// <summary>
        /// A PortSet containing the ports that this port owner owns.
        /// </summary>
        /// <value>The ports.</value>
        public IPortSet Ports {
            get { return m_portSet; }
        }

        #endregion

        #region IHasName Members

        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        /// <value>The name.</value>
        public string Name {
             get { return m_name; }
        }

        #endregion
    }

    /// <summary>
    /// Class OutputPortProxy is a class that represents to an outer container
    /// the functionality of a port on an internal port owner. This can be used
    /// to expose the externally-visible ports from a network of blocks that is
    /// being represented as one container-level block. 
    /// </summary>
    /// <seealso cref="Highpoint.Sage.ItemBased.Ports.IOutputPort" />
    public class OutputPortProxy : IOutputPort {

        #region Private Fields
        /// <summary>
        /// The m ward
        /// </summary>
        private IOutputPort m_ward;
        /// <summary>
        /// The m owner
        /// </summary>
        private IPortOwner m_owner;
        /// <summary>
        /// The m external connector
        /// </summary>
        private IConnector m_externalConnector;
        /// <summary>
        /// The m internal connector
        /// </summary>
        private IConnector m_internalConnector;
        /// <summary>
        /// The m ward partner
        /// </summary>
        private IInputPort m_wardPartner;
        /// <summary>
        /// The m port set
        /// </summary>
        private PortSet m_portSet;
        /// <summary>
        /// The m internal port owner
        /// </summary>
        private IPortOwner m_internalPortOwner;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputPortProxy" /> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:OutputPortProxy" /> will run.</param>
        /// <param name="name">The name of the new <see cref="T:OutputPortProxy" />.</param>
        /// <param name="description">The description of the new <see cref="T:OutputPortProxy" />.</param>
        /// <param name="guid">The GUID of the new <see cref="T:OutputPortProxy" />.</param>
        /// <param name="owner">The owner of this proxy port.</param>
        /// <param name="ward">The ward - the internal port which this proxy port will represent.</param>
        public OutputPortProxy(IModel model, string name, string description, Guid guid, IPortOwner owner, IOutputPort ward) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);

            m_portSet = new PortSet();
            m_internalPortOwner = new PortOwnerProxy(name + ".PortOwner");
            m_wardPartner = new SimpleInputPort(model, name + ".WardPartner", Guid.NewGuid(), m_internalPortOwner, new DataArrivalHandler(_dataArrivalHandler));
            m_ward = ward;
            m_internalConnector = new BasicNonBufferedConnector(m_ward, m_wardPartner);
            m_owner = owner;
            m_externalConnector = null;

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Datas the arrival handler.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="inputPort">The input port.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool _dataArrivalHandler(object data, IInputPort inputPort) {
            if (m_externalConnector != null) {
                return m_externalConnector.Put(data);
            } else {
                return false;
            }
        }

        #region IOutputPort Members

        /// <summary>
        /// This method removes and returns the current contents of the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of the port.</returns>
        public object Take(object selector) {
            return m_ward.Take(selector);
        }

        /// <summary>
        /// True if Peek can be expected to return meaningful data.
        /// </summary>
        /// <value><c>true</c> if this instance is peekable; otherwise, <c>false</c>.</value>
        public bool IsPeekable {
            get {
                return m_ward.IsPeekable;
            }
        }

        /// <summary>
        /// Nonconsumptively returns the contents of this port. A subsequent Take
        /// may or may not produce the same object, if, for example, the stuff
        /// produced from this port is time-sensitive.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of this port. Null if this port is not peekable.</returns>
        public object Peek(object selector) {
            return m_ward.Peek(selector);
        }

        /// <summary>
        /// This event fires when data has been made available on this port.
        /// </summary>
        public event PortEvent DataAvailable { 
            add { m_ward.DataAvailable += value; }
            remove { m_ward.DataAvailable -= value; }
        }

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to take data from this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The take handler.</value>
        public DataProvisionHandler TakeHandler {
            get {
                return m_ward.TakeHandler;
            }
            set {
                m_ward.TakeHandler = value;
            }
        }

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to peek at data on this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The peek handler.</value>
        public DataProvisionHandler PeekHandler {
            get {
                return m_ward.PeekHandler;
            }
            set {
                m_ward.PeekHandler = value;
            }
        }

        #endregion

        #region IPort Members

        /// <summary>
        /// This property represents the connector object that this port is associated with.
        /// </summary>
        /// <value>The connector.</value>
        public IConnector Connector {
            get {
                return m_externalConnector;
            }
            set {
                m_externalConnector = value;
                if (value == null) {
                    m_ward.Connector = null;
                } else {
                    m_ward.Connector = m_internalConnector;
                }
            }
        }

        /// <summary>
        /// This property contains the owner of the port.
        /// </summary>
        /// <value>The owner.</value>
        public IPortOwner Owner {
            get { return m_owner; }
        }

        /// <summary>
        /// Returns the key by which this port is known to its owner.
        /// </summary>
        /// <value>The key.</value>
        public Guid Key {
            get { return Guid; }
        }

        /// <summary>
        /// This property returns the port at the other end of the connector to which this
        /// port is connected, or null, if there is no connector, and/or no port on the
        /// other end of a connected connector.
        /// </summary>
        /// <value>The peer.</value>
        public IPort Peer {
            get { return Connector.Upstream; }
        }

        /// <summary>
        /// Returns the default out-of-band data from this port. Out-of-band data
        /// is data that is not material that is to be transferred out of, or into,
        /// this port, but rather context, type, or other metadata to the transfer
        /// itself.
        /// </summary>
        /// <returns>The default out-of-band data from this port.</returns>
        public object GetOutOfBandData() {
            return m_ward.GetOutOfBandData();
        }

        /// <summary>
        /// Returns out-of-band data from this port. Out-of-band data is data that is
        /// not material that is to be transferred out of, or into, this port, but
        /// rather context, type, or other metadata to the transfer itself.
        /// </summary>
        /// <param name="selector">The key of the sought metadata.</param>
        /// <returns>The desired out-of-band metadata.</returns>
        public object GetOutOfBandData(object selector) {
            return m_ward.GetOutOfBandData(selector);
        }

        /// <summary>
        /// Gets and sets a value indicating whether this <see cref="IPort" /> is intrinsic. An intrinsic
        /// port is a hard-wired part of its owner. It is there when its owner is created, and
        /// cannot be removed.
        /// </summary>
        /// <value><c>true</c> if intrinsic; otherwise, <c>false</c>.</value>
        public bool Intrinsic {
            get {
                return m_ward.Intrinsic;
            }
        }

        /// <summary>
        /// Detaches this output port's data peek and take handler.
        /// </summary>
        public void DetachHandlers() {
            IOutputPort iop = m_internalConnector.Upstream;
            IPortOwner ipo = iop.Owner;
            m_internalConnector.Disconnect();
            ipo.RemovePort(iop);
            iop.DetachHandlers();
        }

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        /// <value>The index.</value>
        public int Index { get { return m_ward.Index; } set { m_ward.Index = value; } }


        #endregion

        #region IPortEvents Members

        /// <summary>
        /// This event fires when data is presented on a port. For an input port, this
        /// implies presentation by an outsider, and for an output port, it implies
        /// presentation by the port owner.
        /// </summary>
        public event PortDataEvent PortDataPresented {
            add {
                m_ward.PortDataPresented += value;
            }
            remove {
                m_ward.PortDataPresented -= value;
            }
        }

        /// <summary>
        /// This event fires when data is accepted by a port. For an input port, this
        /// implies acceptance by the port owner, and for an output port, it implies
        /// acceptance by an outsider.
        /// </summary>
        public event PortDataEvent PortDataAccepted {
            add {
                m_ward.PortDataAccepted += value;
            }
            remove {
                m_ward.PortDataAccepted -= value;
            }
        }

        /// <summary>
        /// This event fires when data is rejected by a port. For an input port, this
        /// implies rejection by the port owner, and for an output port, it implies
        /// rejection by an outsider.
        /// </summary>
        public event PortDataEvent PortDataRejected {
            add {
                m_ward.PortDataRejected += value;
            }
            remove {
                m_ward.PortDataRejected -= value;
            }
        }

        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent BeforeConnectionMade {
            add {
                m_ward.BeforeConnectionMade += value;
            }
            remove {
                m_ward.BeforeConnectionMade -= value;
            }
        }

        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent AfterConnectionMade {
            add {
                m_ward.AfterConnectionMade += value;
            }
            remove {
                m_ward.AfterConnectionMade -= value;
            }
        }

        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
        public event PortEvent BeforeConnectionBroken {
            add {
                m_ward.BeforeConnectionBroken += value;
            }
            remove {
                m_ward.BeforeConnectionBroken -= value;
            }
        }

        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
        public event PortEvent AfterConnectionBroken {
            add {
                m_ward.AfterConnectionBroken += value;
            }
            remove {
                m_ward.AfterConnectionBroken -= value;
            }
        }


        #endregion

        #region Implementation of IModelObject
        /// <summary>
        /// The m name
        /// </summary>
        private string m_name = null;
        /// <summary>
        /// The m unique identifier
        /// </summary>
        private Guid m_guid = Guid.Empty;
        /// <summary>
        /// The m model
        /// </summary>
        private IModel m_model;
        /// <summary>
        /// The m description
        /// </summary>
        private string m_description = null;

        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model { [DebuggerStepThrough] get { return m_model; } }

        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name { [DebuggerStepThrough]get { return m_name; } }

        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
        public string Description => (m_description ?? ("No description for " + m_name));

        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid { [DebuggerStepThrough] get { return m_guid; } }

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

    }

    /// <summary>
    /// A simple implementation of output port. A data provision handler may be defined to
    /// react to a data take request from its peer - if none is provided, then it 
    /// cannot accept a data take request, (i.e. it can only provide data as a push, driven by
    /// the port owner.) A similar handler, with the same conditions, is provided for handling
    /// a 'peek' request. If no data provision handler has been provided, either request
    /// will return null. 
    /// </summary>
    public class SimpleOutputPort : GenericPort, IOutputPort {
        private DataProvisionHandler m_nullSupplier;
        private DataProvisionHandler m_takeHandler;
        private DataProvisionHandler m_peekHandler;
        /// <summary>
        /// Creates a simple output port.
        /// It is the responsibility of the creator to add the port to the owner's PortSet.
        /// </summary>
        /// <param name="model">The model in which this port participates.</param>
        /// <param name="name">The name of the port. This is typically required to be unique within an owner.</param>
        /// <param name="guid">The GUID of the port - also known to the PortOwner as the port's Key.</param>
        /// <param name="owner">The IPortOwner that will own this port.</param>
        /// <param name="takeHandler">The delegate that will be called when a peer calls 'Take()'. Null is okay.</param>
        /// <param name="peekHandler">The delegate that will be called when a peer calls 'Peek()'. Null is okay.</param>
        public SimpleOutputPort(IModel model, string name, Guid guid, IPortOwner owner, DataProvisionHandler takeHandler, DataProvisionHandler peekHandler)
            : base(model, name, guid, owner) {
            m_nullSupplier = new DataProvisionHandler(SupplyNullData);
            m_takeHandler = ( takeHandler != null ? takeHandler : m_nullSupplier );
            m_peekHandler = ( peekHandler != null ? peekHandler : m_nullSupplier );
        }

        private object SupplyNullData(IOutputPort op, object selector) { return null; }

        /// <summary>
        /// Gets the default naming prefix for all ports of this type.
        /// </summary>
        /// <value>The port prefix.</value>
        protected override string PortPrefix {
            get { return "Output_"; }
        }

        #region Implementation of IOutputPort

        /// <summary>
        /// This method removes and returns the current contents of the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of the port.</returns>
        public object Take(object selector) {
            object data = m_takeHandler(this, selector);
            if (data != null) {
                OnPresentingData(data);
                OnAcceptingData(data);
            }
            return data;
        }

        /// <summary>
        /// True if Peek can be expected to return meaningful data.
        /// </summary>
        public bool IsPeekable { get { return m_peekHandler != m_nullSupplier; } }

        /// <summary>
        /// Nonconsumptively returns the contents of this port. A subsequent Take
        /// may or may not produce the same object, if, for example, the stuff
        /// produced from this port is time-sensitive.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>
        /// The current contents of this port. Null if this port is not peekable.
        /// </returns>
        public object Peek(object selector) { return m_peekHandler(this, selector); }

        /// <summary>
        /// This event is fired when new data is available to be taken from a port.
        /// </summary>
        public event PortEvent DataAvailable;

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to take data from this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The take handler.</value>
        public DataProvisionHandler TakeHandler {
            get {
                return m_takeHandler;
            }
            set {
                m_takeHandler = value;
            }
        }

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to peek at data on this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The peek handler.</value>
        public DataProvisionHandler PeekHandler {
            get {
                return m_peekHandler;
            }
            set {
                m_peekHandler = value;
            }
        }

        #endregion

        /// <summary>
        /// Called by the port owner to put data on the port.
        /// </summary>
        /// <param name="newData">The object that is new data to be placed on the port.</param>
        /// <returns>True if the port was able to accept the data.</returns>
        public bool OwnerPut(object newData) {
            if ( HasBeenDetached ) DetachedPortInUse();
            OnPresentingData(newData); // Fires the PortDataPresented event. No return value.
            bool b = false;
            if (Connector != null) {
                // If we have a connector, present the data to it. Otherwise, just fire the rejection event.
                b = Connector.Put(newData);
            }
            if (b)
                OnAcceptingData(newData);
            else
                OnRejectingData(newData);
            return b;
        }

        /// <summary>
        /// This method is called when a Port Owner passively provides data objects - that is, it has
        /// a port on which it makes data available, but it expects others to pull from that port,
        /// rather than it pushing data to the port's peers. So, for example, a queue might call this
        /// method (a) when it is ready to discharge an object from the queue to an output port, or
        /// (b) immediately following an object being pulled from the output port, if there is another
        /// waiting right behind it.
        /// </summary>
        public void NotifyDataAvailable() {
            if ( HasBeenDetached ) DetachedPortInUse();
            if (DataAvailable != null)
                DataAvailable(this);
            if (Connector != null)
                Connector.NotifyDataAvailable();
        }

        /// <summary>
        /// Detaches this input port's data arrival handler.
        /// </summary>
        public override void DetachHandlers() {
            m_takeHandler = null;
            m_peekHandler = null;
            HasBeenDetached = true;
        }
    }

    /// <summary>
    /// Class PortManagementFacade provides one object from which the managers for all of a group of ports owned by one owner
    /// can be obtained. It is a convenience class.
    /// </summary>
    public class PortManagementFacade {

        private Dictionary<IInputPort, InputPortManager> m_inputPortManagers;
        private Dictionary<IOutputPort, OutputPortManager> m_outputPortManagers;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortManagementFacade"/> class with the ports owned by the specified port owner.
        /// </summary>
        /// <param name="portOwner">The port owner.</param>
        public PortManagementFacade(IPortOwner portOwner) {
            m_inputPortManagers = new Dictionary<IInputPort,InputPortManager>();
            m_outputPortManagers = new Dictionary<IOutputPort,OutputPortManager>();
            foreach (IInputPort iip in portOwner.Ports.Inputs) {
                m_inputPortManagers.Add(iip, new InputPortManager((SimpleInputPort)iip));
            }
            foreach (IOutputPort iop in portOwner.Ports.Outputs) {
                m_outputPortManagers.Add(iop, new OutputPortManager((SimpleOutputPort)iop));
            }
        }

        /// <summary>
        /// Obtains the manager for a specified input port.
        /// </summary>
        /// <param name="iip">The input port.</param>
        /// <returns>InputPortManager.</returns>
        public InputPortManager ManagerFor(IInputPort iip) { return m_inputPortManagers[iip]; }
        /// <summary>
        /// Obtains the manager for a specified output port.
        /// </summary>
        /// <param name="iop">The output port.</param>
        /// <returns>OutputPortManager.</returns>
        public OutputPortManager ManagerFor(IOutputPort iop) { return m_outputPortManagers[iop]; }
    }

    /// <summary>
    /// Class PortManager is an abstract class that sets up some of the basic functionality of
    /// both input port managers and output port managers.
    /// </summary>
    public abstract class PortManager {

        /// <summary>
        /// The diagnostics switch. Set in the Sage Config file.
        /// </summary>
        protected static bool Diagnostics = Sage.Diagnostics.DiagnosticAids.Diagnostics("PortManager");

        /// <summary>
        /// Types of buffer persistence. How long the data is buffered for the port.
        /// </summary>
        public enum BufferPersistence
        {
            /// <summary>
            /// The data is not buffered. If it is not read in the call that sets it, it is lost.
            /// </summary>
            None,
            /// <summary>
            /// The data is buffered until it is read or overwritten.
            /// </summary>
            UntilRead,
            /// <summary>
            /// The data is buffered until it is overwritten.
            /// </summary>
            UntilWrite
        }

        /// <summary>
        /// The buffer persistence
        /// </summary>
        protected BufferPersistence m_bufferPersistence;

        /// <summary>
        /// Gets or sets the data buffer persistence.
        /// </summary>
        /// <value>The data buffer persistence.</value>
        public BufferPersistence DataBufferPersistence { get { return m_bufferPersistence; } set { m_bufferPersistence = value; } }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public abstract void ClearBuffer();

        /// <summary>
        /// Gets a value indicating whether this port is connected.
        /// </summary>
        /// <value><c>true</c> if this port is connected; otherwise, <c>false</c>.</value>
        public abstract bool IsPortConnected { get; }

    }

    public class InputPortManager : PortManager {

        #region Behavioral Enumerations
        public enum DataReadSource { Buffer, BufferOrPull, Pull }
        public enum DataWriteAction { Ignore, Store, StoreAndInvalidate, Push } 
        #endregion

        #region Private fields
        private SimpleInputPort m_sip;
        private DataReadSource m_readSource;
        private DataWriteAction m_writeAction;
        private List<OutputPortManager> m_dependents = null;
        private object m_buffer = null;
        #endregion

        #region Constructors
        public InputPortManager(SimpleInputPort sip)
            : this(sip, DataWriteAction.StoreAndInvalidate, BufferPersistence.UntilWrite, DataReadSource.BufferOrPull) { }

        public InputPortManager(SimpleInputPort sip, DataWriteAction writeResponse, BufferPersistence bufferPersistence, DataReadSource readSource) {
            m_bufferPersistence = bufferPersistence;
            m_sip = sip;
            m_readSource = readSource;
            m_writeAction = writeResponse;
            m_sip.PutHandler = new DataArrivalHandler(PutHandler);
        }
        #endregion

        public DataReadSource ReadSource { get { return m_readSource; } set { m_readSource = value; } }
        
        public DataWriteAction WriteAction { get { return m_writeAction; } set { m_writeAction = value; } }
        
        public void SetDependents(params OutputPortManager[] dependents) {
            if (!(dependents.Length == 0 || m_dependents == null || m_dependents.Count == 0)) {
                string ownerBlock = m_sip.Owner is IHasIdentity ? ((IHasIdentity)m_sip.Owner).Name : "a block";
                string msg = string.Format("Calling SetDependents on {1} clears {0} existent dependents. Call SetDependents(); first without dependents to indicate intentional clearance.",
                m_dependents.Count, ownerBlock);
                Debug.Assert(false, msg);            
            }
            m_dependents = new List<OutputPortManager>(dependents);
            foreach (OutputPortManager opm in m_dependents) {
                opm.AddPeers(m_dependents.ToArray());
            }
        }
        
        public object Value {
            get {
                if ( Diagnostics ) _Debug.WriteLine(string.Format("Block {0}, port {1} being asked to give its value.", ((IHasIdentity)m_sip.Owner).Name, m_sip.Name));
                object retval;
                switch (m_readSource) {
                    case DataReadSource.Buffer:
                        retval = m_buffer;
                        break;
                    case DataReadSource.BufferOrPull:
                        if (m_buffer != null) {
                            retval = m_buffer;
                        } else {
                            // Pull into the buffer.
                            m_buffer = m_sip.OwnerTake(null);
                            retval = m_buffer;
                        }
                        break;
                    case DataReadSource.Pull:
                        m_buffer = m_sip.OwnerTake(null);
                        retval = m_buffer;
                        break;
                    default:
                        throw new ApplicationException(String.Format("Unhandled value {0} of InputSource enumeration in an InputPortManager.", m_readSource));
                }

                switch (m_bufferPersistence) {
                    case BufferPersistence.None:
                        m_buffer = null;
                        break;
                    case BufferPersistence.UntilRead:
                        m_buffer = null;
                        break;
                    case BufferPersistence.UntilWrite:
                        break;
                    default:
                        break;
                }

                return retval;
            }
            set {
                switch (m_writeAction) {
                    case DataWriteAction.Ignore:
                        break;
                    case DataWriteAction.Store:
                        m_buffer = value;
                        break;
                    case DataWriteAction.StoreAndInvalidate:
                        m_buffer = value;
                        if (m_dependents == null) {
                            // TODO: Make this universal. (I.E. Require developer always to set values.)
                            throw new ApplicationException(string.Format("Block type {0} forgot to set dependents for input port {1}.", m_sip.Owner.GetType().Name, m_sip.Name));
                        }
                        m_dependents.ForEach(n => n.BufferValid = false);
                        break;
                    case DataWriteAction.Push:
                        m_buffer = value;
                        if (m_dependents == null) {
                            throw new ApplicationException(string.Format("Push-on-write specified on a port with no dependents. Specify dependents for {0}", m_sip.Name));
                        } else {
                            m_dependents.ForEach(n => n.Push());
                        }
                        break;
                    default:
                        break;
                }

            }
        }

        public override void ClearBuffer() { m_buffer = null; }

        public override bool IsPortConnected { get { return m_sip.Connector != null; } }

        private bool PutHandler(object data, IInputPort port) {
            Value = data;
            return true;
        }
    }

    public class OutputPortManager : PortManager {

        #region Private fields
        private SimpleOutputPort m_sop;
        private object m_buffer;
        private bool m_bufferValid;
        private Action m_valueComputeMethod;
        private List<OutputPortManager> m_peers;
        #endregion

        #region Constructors
        public OutputPortManager(SimpleOutputPort sop) : this(sop, BufferPersistence.UntilWrite) { }

        public OutputPortManager(SimpleOutputPort sop, BufferPersistence obp) {
            m_sop = sop;
            m_sop.PeekHandler = new DataProvisionHandler(OnPeek);
            m_sop.TakeHandler = new DataProvisionHandler(OnTake);
            m_buffer = null;
            m_bufferValid = false;
            m_valueComputeMethod = null;
            m_bufferPersistence = obp;
            m_peers = new List<OutputPortManager>();
        } 
        #endregion

        public Action ComputeFunction {
            get {
                if (m_valueComputeMethod == null) throw new ApplicationException(string.Format("Unspecified ComputeFunction on port {0} of {1}.", m_sop.Name, m_sop.Owner));
                return m_valueComputeMethod;
            }
            set { 
                m_valueComputeMethod = value;
            }
        }

        public void Push(bool recompute = true) {
            if (recompute || !BufferValid) { ComputeFunction(); }
            m_sop.OwnerPut(Buffer);
        }

        public object OnPeek(IOutputPort iop, object data) { return m_buffer; }

        public object OnTake(IOutputPort iop, object data) { return Buffer; }

        public bool BufferValid { get { return m_bufferValid; } set { m_bufferValid = value; if ( !m_bufferValid) m_buffer = null; } }

        public object Buffer {
            get {
                if (Diagnostics) _Debug.WriteLine(string.Format("Block {0}, port {1} being asked to give its value - the buffer {2} valid.", ((IHasIdentity)m_sop.Owner).Name, m_sop.Name, BufferValid ? "is" : "is not"));
                if (!BufferValid) {
                    try {
                        object oldValue = m_buffer;
                        ComputeFunction();
                        object newValue = m_buffer;
                        if (ValueHasChanged(oldValue, newValue)) PushAllBut(this);

                    } catch (NullReferenceException nre) {
                        string ownerName = m_sop.Owner as IHasIdentity != null ? (m_sop.Owner as IHasIdentity).Name : "<unknown block>";
                        List<string> problemPorts = new List<string>();
                        foreach (SimpleInputPort sip in m_sop.Owner.Ports.Inputs) {
                            if (sip.OwnerTake(null) == null) {
                                string peerPortName = sip.Peer.Name;
                                string peerOwnerName = sip.Peer.Owner as IHasIdentity != null ? (sip.Peer.Owner as IHasIdentity).Name : "<unknown block>";
                                problemPorts.Add(string.Format("{0}, connected upstream to port \"{1}\" on block \"{2}\"", sip.Name, peerPortName, peerOwnerName));
                            }
                        }

                        string msg = string.Format("The block \"{0}\" was unable to complete its compute function, probably because an upstream source was unable (or unrequested) to deliver a value in response to a pull. Suspect ports are {1}.",
                            ownerName, StringOperations.ToCommasAndAndedList(problemPorts));

                        m_sop.Model.AddError(new GenericModelError("Compute function failure", msg, m_sop.Owner, StringOperations.ToCommasAndAndedList(problemPorts)));
                        throw new ApplicationException(msg, nre);
                    }
                }
                object retval = m_buffer;
                if (Diagnostics) _Debug.WriteLine(string.Format("Block {0}, port {1} provided value {2}", ((IHasIdentity)m_sop.Owner).Name, m_sop.Name, retval));
                switch (m_bufferPersistence) {
                    case BufferPersistence.None:
                    case BufferPersistence.UntilRead:
                        m_buffer = null;
                        BufferValid = false;
                        break;
                    case BufferPersistence.UntilWrite:
                        break;
                    default:
                        break;
                }

                return retval;
            }
            set {
                m_buffer = value; BufferValid = true;
            }
        }

        public override bool IsPortConnected { get { return m_sop.Connector != null; } }

        internal void AddPeers(params OutputPortManager[] opms) {
            foreach ( OutputPortManager opm in opms ) {
                if ( !m_peers.Contains(opm) && opm != this ) m_peers.Add(opm);
            }
        }

        private void PushAllBut(OutputPortManager instigator) {
            foreach (OutputPortManager peer in m_peers ){
                if (peer != instigator) peer.Push(false);
            }
        }

        private bool ValueHasChanged(object oldValue, object newValue) {
            if (oldValue == newValue) return false;
            if (oldValue == null && newValue != null) return true;
            if (newValue == null && oldValue != null) return true;
            return !oldValue.Equals(newValue);
        }

        private void DetachableEventAbortHandler(IExecutive exec, IDetachableEventController idec, params object[] args) { }

        public override void ClearBuffer() { m_buffer = null; BufferValid = false; }

    }
}
