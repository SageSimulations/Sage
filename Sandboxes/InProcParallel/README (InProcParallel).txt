To run this sandbox, set InProcParallel to the StartUp project.

InProcParallel spreads a nominal supply chain model across a number of cores, 
running in the same process. Market Segments place orders, Fulfillment 
Centers fulfill them, and the FCs replenishing the whole SKU family when 
any SKU item is below a safety stock level, from Replenishers (factories)
associated with the SKU family of which it's running low. The number of
cores to use is specified in 'InProcParallelLib.TotallyBogusConfigMechanism,'
and if you specify more cores than you have, or less than zero, you'll get
what you deserve for crossing the streams.
	
The project relies on partitioning of the problem into the chattier subsets
of objects. The several market segments each place orders into their most
local distribution centers, so segments and their centers are run on the same
core. Replenishers are currently spread more-or-less evenly across the cores.

This is optimistic concurrency - all cores blast ahead as fast as they can,
until one needs to read data from, or write data to, something running on 
another. Consider A as an object running on one core, and B as an object 
running on another. 
	
If A tries to read from B, and B is ahead, running in A's future, then B
provides the value from its own history (i.e. "What was the requested value 
at that time?") If A tries to read from B, and B is running in A's past, B 
simply blocks A's request until they are contemporary, and then presents its
current value. (i.e. "I'm not there yet - wait for it. ...  Here it is.")

If A tries to write to B, and B is in A's future, B first rolls back its 
executive (server of temporal events, and arbiter of what "now" means) to 
the time of the requested write, then sets the value, and proceeds. If A tries
to write to B, and B is in A's past, the write is blocked until B catches
up to A.

There are a number of anti-deadlock mechanisms in the CoExecutor and 
ParallelExecutive classes in the Highpoint.Sage.SimCore.Parallel namespace.

This is preliminary. It has not been optimized, it still has bugs, there are
additional things to be done to reduce the size (and performance sap) of rollbacks,
and there are likely whole new ways to look at it. This was an exploratory effort.

SOME IDEAS FOR IMPROVEMENT: 

Probabilistically throttle the least-busy (and therefore temporally-fastest) 
	executives so they don't run ahead so far, only to be dragged back when someone
	tries to write to them.
Better data structure for future event list to speed up rollbacks.
Persist and dispose of immutable history (i.e. all execs are past it, and therefore
	none will roll back to it.
Let execs that are not in need of rollback keep running until they reach the rollback time.
When an exec is unblocked repeatedly in a future-read call, don't keep re-blocking it
	and re-un-blocking it.
Remove revocation event. Only used for unblocking a future read block, no longer needed.
Perhaps throw the whole rollback algorithm at TLA+? 
It may even be better to use a managed-cadence pessimistic run, with selective data collection.