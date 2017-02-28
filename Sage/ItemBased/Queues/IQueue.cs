/* This source code licensed under the GNU Affero General Public License */

using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;

namespace Highpoint.Sage.ItemBased.Queues {

	public interface ISelectionStrategy {
		object GetNext(object context);
		ICollection Candidates { get; set; }
	}

	
	public delegate void QueueLevelChangeEvent(int previous, int current, IQueue queue);
	public delegate void QueueOccupancyEvent(IQueue hostQueue, object serviceItem);
	public delegate void QueueMilestoneEvent(IQueue queue);

	public interface IQueue : IPortOwner, IModelObject {
		IInputPort Input { get; }
		IOutputPort Output { get; }
		int Count { get ; }
        int MaxDepth { get; }
		event QueueMilestoneEvent QueueFullEvent;
		event QueueMilestoneEvent QueueEmptyEvent;
		event QueueLevelChangeEvent LevelChangedEvent;
		event QueueOccupancyEvent ObjectEnqueued;
		event QueueOccupancyEvent ObjectDequeued;
	}
}