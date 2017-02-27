/* This source code licensed under the GNU Affero General Public License */
#pragma warning disable 1587
using Highpoint.Sage.SimCore;
/// <summary>
/// Sage® includes a Port-and-Connector architecture that allows the developer to create
/// a familiar “create objects with ports and wire the ports together with connectors” model.
/// There are very few constraints placed on the kind of object that can serve as owner of
/// such ports – basically, it must be able to enumerate ports, and inform the ports of their
/// owner through a registration protocol.<para/>
/// Ports serve to transfer data objects between entities such as products between assembly
/// stations in a manufacturing simulation. They present objects to, and accept objects from,
/// the world outside of a port owner. They fire events when data arrives or is sent, and
/// support out-of-band data, which is a set of alternate date elements that usually pertain
/// to the data object being transmitted. As an example, a vat, transferring 100 kg of sucrose
/// mixture to another vat, might present a Mixture object on its output port, and a TimeSpan
/// object on an out of band channel called “MinTransferDuration”.<para/>
/// Note that for values that do not represent transfers, do not change through being read, or
/// for which an IObservable construct serves sufficiently (such as when
/// modeling production rates instead of discrete product in a manufacturing environment), a
/// simple field and event are more than adequate to communicate data between entites. In
/// other words, unlike many block and port architectures, this is one of several first-class
/// means you have at your disposal for communicating information between participants in
/// your simulation.
/// </summary>
namespace Highpoint.Sage.ItemBased {
    interface IServiceItem {
        SmartPropertyBag Properties { get; }
    }
}
