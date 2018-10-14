TODO:
Better data structure for future event list to speed up rollbacks.
Persist immutable history.
Let execs that are not in need of rollback (i.e. have not reached the target time) keep running until they reach the rollback time.
When an exec is unblocked repeatedly in a future-read call, don't keep re-blocking it and re-un-blocking it.
Remove revocation event. Only used for unblocking a future read block, no longer needed.
Perhaps throw the rollback algorithm at TLA+ or EPPlus?