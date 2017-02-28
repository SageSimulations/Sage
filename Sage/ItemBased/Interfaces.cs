/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.SimCore;
using System.Collections.ObjectModel;

// 20080130 : Considered a generic version of ports (and therefore, blocks), but
// decided not to. It would give me type safety, but introduce a lot of new issues,
// like differentiation of multiple port sets (by type), requirement to know the
// type irrespective of whether you cared (such as when responding to a (now-)typed
// event). I can achieve the same thing by adding Type info to the PortChannelInfo,
// and refusing connections where both ends declare incompatible types...

namespace Highpoint.Sage.ItemBased.Ports
{

    /// <summary>
    /// Interface implemented by any object that exposes ports.
    /// </summary>
    public interface IPortOwner
    {
        /// <summary>
        /// Adds a user-created port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        void AddPort(IPort port);

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port.</returns>
        IPort AddPort(string channelTypeName);

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel with the provided Guid.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port.</returns>
        IPort AddPort(string channelTypeName, Guid guid);

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        List<IPortChannelInfo> SupportedChannelInfo { get; }

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective. Implementers are responsible to refuse removal of a port that
        /// is a hard property exposed (e.g. this.InputPort0), since it will remain
        /// accessible via that property.
        /// </summary>
        /// <param name="port">The port.</param>
        void RemovePort(IPort port);
        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        void ClearPorts();
        /// <summary>
        /// A PortSet containing the ports that this port owner owns.
        /// </summary>
        IPortSet Ports { get; }
    }

    /// <summary>
    /// An interface implemented by a PortSet. Permits indexing to a port by key.
    /// </summary>
    public interface IPortSet : IEnumerable, IPortEvents
    {
        /// <summary>
        /// Permits a caller to retrieve a port by its guid.
        /// </summary>
        IPort this[Guid key] { get; }
        /// <summary>
        /// Permits a caller to retrieve a port by its name.
        /// </summary>
        IPort this[string name] { get; }
        /// <summary>
        /// Gets the <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/> with the specified index, i.
        /// </summary>
        /// <value>The <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/>.</value>
        IPort this[int i] { get; }
        /// <summary>
        /// Adds a port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        void AddPort(IPort port);

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective.
        /// </summary>
        /// <param name="port">The port to be removed from the portSet.</param>
        void RemovePort(IPort port);

        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        void ClearPorts();

        /// <summary>
        /// Fired when a port has been added to this IPortSet.
        /// </summary>
        event PortEvent PortAdded;

        /// <summary>
        /// Fired when a port has been removed from this IPortSet.
        /// </summary>
        event PortEvent PortRemoved;

        /// <summary>
        /// Returns a collection of the keys that belong to ports known to this PortSet.
        /// </summary>
        ICollection PortKeys { get; }

        /// <summary>
        /// Looks up the key associated with a particular port.
        /// </summary>
        /// <param name="port">The port for which we want the key.</param>
        /// <returns>The key for the provided port.</returns>
        [Obsolete("Ports use their Guids as the key.")]
        Guid GetKey(IPort port);

        /// <summary>
        /// Gets the count of all kids of ports in this collection.
        /// </summary>
        /// <value>The count.</value>
        int Count { get; }

        /// <summary>
        /// Gets the output ports owned by this PortSet.
        /// </summary>
        /// <value>The output ports.</value>
        ReadOnlyCollection<IOutputPort> Outputs { get; }

        /// <summary>
        /// Gets the input ports owned by this PortSet.
        /// </summary>
        /// <value>The input ports.</value>
        ReadOnlyCollection<IInputPort> Inputs { get; }

        /// <summary>
        /// Sorts the ports based on one element of their Out-of-band data sets.
        /// Following a return from this call, the ports will be in the order requested.
        /// The &quot;T&quot; parameter will usually be int, double or string, but it must
        /// represent the IComparable-implementing type of the data stored under the
        /// provided OOBDataKey.
        /// </summary>
        /// <param name="oobDataKey">The oob data key.</param>
        void SetSortOrder<T>(object oobDataKey) where T : IComparable;

    }

    /// <summary>
    /// Interface IPortChannelInfo specifies information about what travels on the port, and in
    /// what direction. Examples might be "Input" or "Output", or maybe "Control", "Kanban", etc.
    /// </summary>
    public interface IPortChannelInfo
    {
        /// <summary>
        /// Gets the direction of flow across the port.
        /// </summary>
        /// <value>The direction.</value>
        PortDirection Direction { get; }
        /// <summary>
        /// Gets the name of the type - usually "Input" or "Output", but could be "Control", "Kanban", etc..
        /// </summary>
        /// <value>The name of the type.</value>
        string TypeName { get; }
    }

    /// <summary>
    /// An interface describing the events that are fired by all IPort objects.
    /// </summary>
    public interface IPortEvents
    {
        /// <summary>
        /// This event fires when data is presented on a port. For an input port, this
        /// implies presentation by an outsider, and for an output port, it implies 
        /// presentation by the port owner.
        /// </summary>
        event PortDataEvent PortDataPresented;

        /// <summary>
        /// This event fires when data is accepted by a port. For an input port, this
        /// implies acceptance by the port owner, and for an output port, it implies 
        /// acceptance by an outsider.
        /// </summary>
        event PortDataEvent PortDataAccepted;

        /// <summary>
        /// This event fires when data is rejected by a port. For an input port, this
        /// implies rejection by the port owner, and for an output port, it implies 
        /// rejection by an outsider.
        /// </summary>
        event PortDataEvent PortDataRejected;

        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
        event PortEvent BeforeConnectionMade;

        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
        event PortEvent AfterConnectionMade;

        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
        event PortEvent BeforeConnectionBroken;

        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
        event PortEvent AfterConnectionBroken;
    }

    /// <summary>
    /// This interface specifies the methods common to all types of ports, that are visible 
    /// to objects other than the owner of the port.
    /// </summary>
    public interface IPort : IPortEvents, IModelObject
    {
        /// <summary>
        /// This property represents the connector object that this port is associated with.
        /// </summary>
        IConnector Connector { get; set; }

        /// <summary>
        /// This property contains the owner of the port.
        /// </summary>
        IPortOwner Owner { get; }

        /// <summary>
        /// Returns the key by which this port is known to its owner.
        /// </summary>
        Guid Key { get; }

        /// <summary>
        /// This property returns the port at the other end of the connector to which this
        /// port is connected, or null, if there is no connector, and/or no port on the
        /// other end of a connected connector.
        /// </summary>
        IPort Peer { get; }

        /// <summary>
        /// Returns the default out-of-band data from this port. Out-of-band data
        /// is data that is not material that is to be transferred out of, or into,
        /// this port, but rather context, type, or other metadata to the transfer
        /// itself.
        /// </summary>
        /// <returns>The default out-of-band data from this port.</returns>
        object GetOutOfBandData();

        /// <summary>
        /// Returns out-of-band data from this port. Out-of-band data is data that is
        /// not material that is to be transferred out of, or into, this port, but
        /// rather context, type, or other metadata to the transfer itself.
        /// </summary>
        /// <param name="selector">The key of the sought metadata.</param>
        /// <returns>The desired out-of-band metadata.</returns>
        object GetOutOfBandData(object selector);

        /// <summary>
        /// Gets a value indicating whether this <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/> is intrinsic. An intrinsic
        /// port is a hard-wired part of its owner. It is there when its owner is created, and
        /// cannot be removed.
        /// </summary>
        /// <value><c>true</c> if intrinsic; otherwise, <c>false</c>.</value>
        bool Intrinsic { get; }

        /// <summary>
        /// Detaches this port's data handlers.
        /// </summary>
        void DetachHandlers();

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        int Index { get; set; }

    }

    /// <summary>
    /// IInputPort is the portion of an InputPort that is intended to be visible
    /// and accessible from outside the scope of its owner.
    /// </summary>
    public interface IInputPort : IPort
    {
        /// <summary>
        /// This method attempts to place the provided data object onto the port from
        /// upstream of its owner. It will succeed if the port is unoccupied, or if
        /// the port is occupied and the port permits overwrites.
        /// </summary>
        /// <param name="obj">the data object</param>
        /// <returns>True if successful. False if it fails.</returns>
        bool Put(object obj); // True if accepted.

        /// <summary>
        /// This is called by a peer to let the input port know that there is data
        /// available at the peer, in case the input port wants to pull the data.
        /// </summary>
        void NotifyDataAvailable();

        /// <summary>
        /// This sets the PutHandler that this port will use, replacing the current
        /// one. This should be used only by objects under the control of, or owned by, the
        /// IPortOwner that owns this port.
        /// </summary>
        /// <value>The new PutHandler.</value>
        DataArrivalHandler PutHandler { get; set; }
    }

    /// <summary>
    /// IOutputPort is the portion of an output port that is intended to be visible 
    /// and accessible from outside the scope of its owner. 
    /// </summary>
    public interface IOutputPort : IPort
    {
        /// <summary>
        /// This method removes and returns the current contents of the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of the port.</returns>
        object Take(object selector);

        /// <summary>
        /// True if Peek can be expected to return meaningful data.
        /// </summary>
        bool IsPeekable { get; }

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
        object Peek(object selector);

        /// <summary>
        /// This event is fired when new data is available to be taken from a port.
        /// </summary>
        event PortEvent DataAvailable;


        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to take data from this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The take handler.</value>
        DataProvisionHandler TakeHandler { get; set; }

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to peek at data on this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The peek handler.</value>
        DataProvisionHandler PeekHandler { get; set; }

    }

    /// <summary>
    /// This interface is implemented by any object that can choose ports. It is useful in
    /// constructing an autonomous route navigator, route strategy object, or transportation
    /// manager.
    /// </summary>
    public interface IPortSelector
    {
        /// <summary>
        /// Selects a port from among a presented set of ports.
        /// </summary>
        /// <param name="portSet">The Set of ports.</param>
        /// <returns>The selected port.</returns>
        IPort SelectPort(IPortSet portSet);
    }

}
