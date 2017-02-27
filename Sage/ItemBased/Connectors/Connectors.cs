/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections.Generic;
using System.Xml;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Persistence;
using System.Xml.Linq;

namespace Highpoint.Sage.ItemBased.Connectors {

    /// <summary>
    /// An enumeration of the different types of connectors that can be created by a ConnectorFactory.
    /// </summary>
    public enum ConnectorType {
        /// <summary>
        /// The <see cref="BasicNonBufferedConnector"/> class.
        /// </summary>
        BasicNonBuffered
    }

    public class ConnectorFactory {

        private static string _prefix = "Connector_";
        private static Dictionary<Guid, ConnectorFactory> _connectorFactories = new Dictionary<Guid, ConnectorFactory>();

        // private IModel m_model;
        private int m_nextConnectorNumber = -1;

        private static ConnectorFactory ForModel(IModel model) {
            Guid key = GuidForModel(model);
            if (!_connectorFactories.ContainsKey(key)) {
                _connectorFactories.Add(key, new ConnectorFactory(model));
            }
            return _connectorFactories[key];
        }

        private static Guid GuidForModel(IModel model) {
            return ( model == null ? Guid.Empty : model.Guid );
        }

        private ConnectorFactory(IModel model) {
            if (model != null) {
                UpdateNextConnectorNumber(model, null);
            }
        }

        private void UpdateNextConnectorNumber(IModel model, string name = null) {
            List<string> names = new List<string>();
            if (!string.IsNullOrEmpty(name)) names.Add(name);
            foreach (IModelObject imo in model.ModelObjects.Values) {
                IConnector conn = imo as IConnector;
                if (conn != null && conn.Name != null && conn.Name.StartsWith(_prefix)) {
                    names.Add(conn.Name);
                }
            }

            foreach (string nm in names) {
                int connNum;
                if (int.TryParse(nm.Substring(_prefix.Length), out connNum)) {
                    m_nextConnectorNumber = Math.Max(m_nextConnectorNumber, connNum);
                }
            }
        }

        public static IConnector Connect(IPort p1, IPort p2) {
            return Connect(p1, p2, ConnectorType.BasicNonBuffered, null);
        }

        public static IConnector Connect(IPort p1, IPort p2, string name) {
            return Connect(p1, p2, ConnectorType.BasicNonBuffered, name);
        }

        public static IConnector Connect(IPort p1, IPort p2, ConnectorType connType) {
            return Connect(p1, p2, ConnectorType.BasicNonBuffered, null);
        }

        public static IConnector Connect(IPort p1, IPort p2, ConnectorType connType, string name) {
            if (p1 == null || p2 == null) {
                throw new ApplicationException("Attempt to connect " + GetName(p1) + " to " + GetName(p2));
            }

            Debug.Assert(p1.Model == p2.Model);

            return ForModel(p1.Model)._Connect(p1, p2, connType, name);
        }

        private IConnector _Connect(IPort p1, IPort p2, ConnectorType connType, string name) {

            if (p1.Model != p2.Model) {
                string msg = string.Format("Trying to connect port {0} on object {1} to port {2} on object {3}, but they appear to be in different models.",
                    p1.Name, p1.Owner, p2.Name, p2.Owner);
                throw new ApplicationException(msg);
            }

            if (name != null) {
                
            }

            if (name == null || name.Equals(string.Empty)) {
                name = _prefix + ( ++m_nextConnectorNumber );
            } else {
                ForModel(p1.Model).UpdateNextConnectorNumber(p1.Model, name);
            }

            if (connType.Equals(ConnectorType.BasicNonBuffered)) {
                return new BasicNonBufferedConnector(p1.Model, name, null, Guid.NewGuid(), p1, p2);
            } else {
                throw new ApplicationException("Unknown connector type requested.");
            }
        }

        private static string GetName(IPort port) {
            if (port == null)
                return "<null>";
            if (port.Owner == null)
                return port + ", with a <null> owner";
            if (port.Owner is IHasIdentity)
                return port + ", owned by " + ( (IHasIdentity)port.Owner ).Name;
            return port + ", owned by " + port.Owner;
        }
    }

    public interface IConnector : IModelObject, System.ComponentModel.INotifyPropertyChanged, IXElementSerializable {
        /// <summary>
        /// Gets the upstream port.
        /// </summary>
        /// <value>The upstream port.</value>
        IOutputPort Upstream { get; }
        /// <summary>
        /// Gets the downstream port.
        /// </summary>
        /// <value>The downstream port.</value>
        IInputPort Downstream { get; }
        /// <summary>
        /// Disconnects this connector from its upstream and downstream ports, and then removes them from their owners, if possible.
        /// </summary>
        void Disconnect();
        /// <summary>
        /// Connects the specified port, p1 (the upstream port) to the specified port, p2 (the downstream port.)
        /// </summary>
        /// <param name="p1">The upstream port.</param>
        /// <param name="p2">The downstream port.</param>
        void Connect(IPort p1, IPort p2);
        /// <summary>
        /// Called by the upstream port to inform the connector, and thereby the downstream port,
        /// that an item is available for pull by its owner.
        /// </summary>
            void NotifyDataAvailable();
        /// <summary>
        /// Retrieves the default out-of-band data for this port. This data is set via an API on GenericPort.
        /// </summary>
        /// <returns>The default out-of-band data for this port.</returns>
        object GetOutOfBandData();
        /// <summary>
        /// Retrieves the out-of-band data corresponding to the provided key, for this port.
        /// This data is set via an API on GenericPort.
        /// </summary>
        /// <param name="key">The key (such as "Priority") associated with this port's out of band data.</param>
        /// <returns>The out-of-band data corresponding to the provided key.</returns>
        object GetOutOfBandData(object key);
        /// <summary>
        /// Gets a value indicating whether this connector is peekable. The downstream port will call this API,
        /// resulting in a passed-through call to the upstream port, where it will declare whether it supports the
        /// 'peek' operation.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is peekable; otherwise, <c>false</c>.
        /// </value>
        bool IsPeekable { get; }
        /// <summary>
        /// Propagates a 'Peek' operation through this connector to the upstream port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The object or item, if any, available on the upstream port. The item is left on the port.</returns>
        object Peek(object selector);
        /// <summary>
        /// Propagates a 'Take' operation through this connector to the upstream port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The object or item, if any, available on the upstream port. The item is removed from the port.</returns>
        object Take(object selector);
        /// <summary>
        /// Puts the specified data onto the downstream port, if possible.
        /// </summary>
        /// <param name="data">The item or data to be put to the downstream port.</param>
        /// <returns>true if the put operation was successful, otherwise (if the port was blocked), false.</returns>
        bool Put(object data);

        /// <summary>
        /// Gets or sets a value indicating whether the connector is currently in use.
        /// </summary>
        /// <value><c>true</c> if [in use]; otherwise, <c>false</c>.</value>
        bool InUse { get; set; }
    }
    
    public class BasicNonBufferedConnector : IConnector {
        
        #region Private Fields
        private IOutputPort m_upstream;
        private IInputPort m_downstream;
        private bool m_inUse;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicNonBufferedConnector"/> class.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        public BasicNonBufferedConnector(IModel model, string name, string description, Guid guid, IPort input, IPort output) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
            Connect(input, output);
            IMOHelper.RegisterWithModel(this);
            m_inUse = false;
        }

        /// <summary>
        /// For prelude to deserialization only.
        /// </summary>
        public BasicNonBufferedConnector() {
            m_inUse = false;
        }

        public BasicNonBufferedConnector(IPort input, IPort output) {
            Connect(input, output);
            m_inUse = false;
        }

        public IInputPort Downstream { get { return m_downstream; } }
        public IOutputPort Upstream  { get { return m_upstream; } }

        public void Connect(IPort p1, IPort p2){
            if ( m_upstream != null || m_downstream != null ) throw new ApplicationException("Trying to connect an already-connected port.");
            if ( p1 is IInputPort && p2 is IOutputPort ) {
                Attach((IInputPort)p1, (IOutputPort)p2);
            } else if ( p2 is IInputPort && p1 is IOutputPort ) {
                Attach((IInputPort)p2, (IOutputPort)p1);
            } else {
                throw new ApplicationException("Trying to connect non-compatible ports " + p1.GetType() + " and " + p2.GetType());
            }
            //Console.WriteLine("Just connected " + ((IHasIdentity)m_upstream.Owner).Name + "." + m_upstream.Key + " to " + ((IHasIdentity)m_downstream.Owner).Name + "." + m_downstream.Key);
            //Console.WriteLine(Upstream.Connector + ", " + Downstream.Connector);
        }

        public void Disconnect(){
            Detach();
            m_inUse = false;
        }

        /// <summary>
        /// Called by the PortOwner after it has put new data on the port. It indicates that
        /// data is newly available on this port. Since it is a multicast event, by the time
        /// a recipient receives it, the newly-arrived data may be gone already.
        /// </summary>
        public void NotifyDataAvailable(){
            m_downstream.NotifyDataAvailable();
        }

        internal void Attach(IInputPort input, IOutputPort output){
            m_upstream = output;
            m_downstream = input;
            input.Connector = this;
            output.Connector = this;
        }

        internal void Detach(){
//			Trace.WriteLine("Setting " + ((IHasIdentity)m_upstream.Owner).Name + "'s connector to null.");
//			Trace.WriteLine("Setting " + m_upstream.Owner + "'s connector to null.");
            if ( m_upstream != null ) m_upstream.Connector = null;
//			Trace.WriteLine("Setting " + ((IHasIdentity)m_downstream.Owner).Name + "'s connector to null.");
//			Trace.WriteLine("Setting " + m_downstream.Owner + "'s connector to null.");
            if ( m_downstream != null ) m_downstream.Connector = null;
            m_upstream = null;
            m_downstream = null;
        }

        public object GetOutOfBandData(){ return m_downstream.GetOutOfBandData(); }
        public object GetOutOfBandData(object key){ return m_downstream.GetOutOfBandData(key); }
        public bool IsPeekable { get { return m_upstream.IsPeekable; } }
        public object Peek(object selector){ return m_upstream.Peek(selector); }
        public object Take(object selector){ return m_upstream.Take(selector); }
        public bool Put(object data){ return m_downstream.Put(data); }

        #region Member Variables

        private IModel m_model;
        private string m_name = String.Empty;
        private string m_description = String.Empty;
        private Guid m_guid = Guid.Empty;
        private IPort m_input = null;
        private IPort m_output = null;
        #endregion Member Variables

        #region Initialization
        //TODO: Replace all DESCRIPTION? tags with the appropriate text.
        //TODO: If this class is derived from another that implements IModelObject, remove the m_model, m_name, and m_guid declarations.
        //TODO: Make sure that what happens in any other ctors also happens in the Initialize method.
        
        [Initializer(InitializerAttribute.InitializationType.PreRun, "_Initialize")]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "inputPortOwner", RefType.Owned, typeof(IPortOwner), "The upstream port owner attached to this connector")]
            Guid inputPortOwner,
           [InitializerArg(1, "inputPortName", RefType.Owned, typeof(string), "The name of the port on the upstream port owner")]
            string inputPortName,
        [InitializerArg(2, "outputPortOwner", RefType.Owned, typeof(IPort), "The downstream port attached to this connector")]
            Guid outputPortOwner,
        [InitializerArg(3, "outputPortName", RefType.Owned, typeof(string), "The downstream port attached to this connector")]
            string outputPortName) {

            InitializeIdentity(model, name, description, guid);

            // Put here: Things that are done in the full constructor, but don't operate
            // on the arguments passed into that ctor or this initialize method.

            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(new Initializer(_Initialize), inputPortOwner, inputPortName, outputPortOwner, outputPortName);
        }

        /// <summary>
        /// Services needs in the first dependency-sequenced round of initialization.
        /// </summary>
        /// <param name="model">The model in which the initialization is taking place.</param>
        /// <param name="p">The array of objects that take part in this round of initialization.</param>
        public void _Initialize(IModel model, object[] p) {
            IPortOwner ipo = (IPortOwner)model.ModelObjects[p[0]];
            m_input = ipo.Ports[(string)p[1]];
            IPortOwner opo = (IPortOwner)model.ModelObjects[p[2]];
            m_output = ipo.Ports[(string)p[3]];
            Connect(m_input, m_output);
        }


        #endregion

        #region Implementation of IModelObject

        /// <summary>
        /// The model to which this BasicNonBufferedConnector belongs.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's description.</value>
        public IModel Model { [DebuggerStepThrough] get { return m_model; } }
        /// <summary>
        /// The Name for this BasicNonBufferedConnector. Typically used for human-readable representations.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's name.</value>
        public string Name { [DebuggerStepThrough] get { return m_name; } }
        /// <summary>
        /// The Guid of this BasicNonBufferedConnector.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's Guid.</value>
        public Guid Guid { [DebuggerStepThrough] get { return m_guid; } }
        /// <summary>
        /// The description for this BasicNonBufferedConnector. Typically used for human-readable representations.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's description.</value>
        public string Description => (m_description ?? ("No description for " + m_name));

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The BasicNonBufferedConnector's new model value.</param>
        /// <param name="name">The BasicNonBufferedConnector's new name value.</param>
        /// <param name="description">The BasicNonBufferedConnector's new description value.</param>
        /// <param name="guid">The BasicNonBufferedConnector's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        #endregion

        #region IConnector Members


        public bool InUse {
            get {
                return m_inUse;
            }
            set {
                if (m_inUse != value) {
                    m_inUse = value;
                    if (PropertyChanged != null) {
                        PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("InUse"));
                    }
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Prior to this call, you must have created the connector using the 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deserializationContext"></param>
        public void LoadFromXElement(XElement self, DeserializationContext deserializationContext) {

            IModel model = null;
            string connectorName = self.Attribute("connectorName").Value;
            string connectorDesc = self.Attribute("connectorDesc").Value;
            Guid connectorGuidWas = XmlConvert.ToGuid(self.Attribute("connectorGuid").Value);
            Guid connectorGuidIs = Guid.NewGuid();
            deserializationContext.SetNewGuidForOldGuid(connectorGuidWas, connectorGuidIs);
            IMOHelper.Initialize(ref m_model, model, ref m_name, connectorName, ref m_description, connectorDesc, ref m_guid, connectorGuidIs);
            IMOHelper.RegisterWithModel(this);

            XElement source = self.Element("Source");
            Guid upstreamOwnerGuidWas = XmlConvert.ToGuid(source.Attribute("guid").Value);
            Guid upstreamOwnerGuidIs = Guid.NewGuid();
            string upstreamPortName = source.Attribute("name").Value;
            IPortOwner usmb = (IPortOwner)deserializationContext.GetModelObjectThatHad(upstreamOwnerGuidWas);
            IOutputPort upstreamPort = (IOutputPort)usmb.Ports[upstreamPortName];

            XElement destination = self.Element("Destination");
            Guid downstreamOwnerGuidWas = XmlConvert.ToGuid(destination.Attribute("guid").Value);
            Guid downstreamOwnerGuidIs = Guid.NewGuid();
            string downstreamPortName = destination.Attribute("name").Value;
            IPortOwner dsmb = (IPortOwner)deserializationContext.GetModelObjectThatHad(downstreamOwnerGuidWas);
            IInputPort downstreamPort = (IInputPort)dsmb.Ports[downstreamPortName];

            Connect(upstreamPort, downstreamPort);
        }

        public XElement AsXElement(string name) {

            Guid upstreamOwnerGuid = Upstream.Owner!=null?Upstream.Owner is IModelObject?((IModelObject)Upstream.Owner).Guid:Guid.Empty:Guid.Empty;
            string upstreamPortName = Upstream!=null?Upstream.Name:string.Empty;

            Guid downstreamOwnerGuid = Downstream.Owner!=null?Downstream.Owner is IModelObject?((IModelObject)Downstream.Owner).Guid:Guid.Empty:Guid.Empty;
            string downstreamPortName = Downstream!=null?Downstream.Name:string.Empty;

            return new XElement(name,
                new XAttribute("connectorName", Name),
                new XAttribute("connectorDescription", Description),
                new XAttribute("connectorGuid", Guid.ToString()),

                new XElement("Source",
                    new XAttribute("guid", upstreamOwnerGuid),
                    new XAttribute("name", upstreamPortName)),

                new XElement("Destination",
                    new XAttribute("guid", downstreamOwnerGuid),
                    new XAttribute("name", downstreamPortName))
                );
        }
    }
}