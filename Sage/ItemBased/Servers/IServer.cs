/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;


namespace Highpoint.Sage.ItemBased.Servers {

	/// <summary>
	/// Implemented by an object wishing to receive notification of the commencement or completion
	/// of service of an object.
	/// </summary>
	public delegate void ServiceEvent(IServer server, object serviceObject);

	/// <summary>
	/// Implemented by an object wishing to participate in the decision of whether a service object
	/// can be serviced.
	/// </summary>
	public delegate bool ServiceRequestEvent(IServer server, object serviceObject);

	/// <summary>
	/// An object that implements IServer receives objects on an input port, and some time later,
	/// emits them from its Output port. If the model's SupportsServiceObjects property is set to
	/// true, and the received object is an implementer of IServiceObject, then that object's
	/// OnServiceBeginning and OnServiceCompleting events are fired as the object is received, and
	/// later, emitted.
	/// </summary>
	public interface IServer : IPortOwner, IModelObject {
		/// <summary>
		/// The port on which a new service item arrives.
		/// </summary>
		IInputPort Input { get; }
		/// <summary>
		/// The port to which a completed service item is discharged.
		/// </summary>
		IOutputPort Output { get; }

		/// <summary>
		/// Call this API to schedule the server to be placed in service at given time.
		/// </summary>
		/// <param name="dt">The time at which the server is to be placed in service.</param>
		void PlaceInServiceAt(DateTime dt);

		/// <summary>
		/// Places this server in service immediately.
		/// </summary>
		void PlaceInService();

		/// <summary>
		/// Call this API to schedule the server to be removed from service at given time.
		/// </summary>
		/// <param name="dt">The time at which the server is to be removed from service.</param>
		void RemoveFromServiceAt(DateTime dt);

		/// <summary>
		/// Removes this server from service.
		/// </summary>
		void RemoveFromService();

		/// <summary>
		/// Fired when service begins for a particular object.
		/// </summary>
		event ServiceEvent ServiceBeginning;

		/// <summary>
		/// Fired when service completes for a particular object.
		/// </summary>
		event ServiceEvent ServiceCompleted;

//	}
//
//	/// <summary>
//	/// An IPeriodicServer is a server that has an associated periodicity with which it services 
//	/// </summary>
//	public interface IPeriodicServer : IServer {
		/// <summary>
		/// The periodicity of the server.
		/// </summary>
		IPeriodicity Periodicity { get; set; }
	}

	/// <summary>
	/// Optional interface for a service object, in case it wants to be notified of its
	/// stages of participation with, or processing by, a server.
	/// </summary>
	public interface IServiceObject {
		void OnServiceBeginning(IServer server);
		void OnServiceCompleting(IServer server);
	}
}