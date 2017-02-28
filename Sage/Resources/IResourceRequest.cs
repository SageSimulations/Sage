/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Globalization;
using Highpoint.Sage.SimCore;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Resources {

	/// <summary>
	/// This is a method that, given a list of objects implementing IResource,
	/// chooses the 'best' one. 
	/// </summary>
	public delegate IResource ResourceSelectionStrategy(IList candidates);

	/// <summary>
	/// This is the signature of the event that is fired when a Resource request is aborted.
	/// </summary>
	public delegate void ResourceRequestAbortEvent(IResourceRequest request, IExecutive exec, IDetachableEventController idec);

	/// <summary>
	/// This is the signature of the event that is fired when a Resource request changes its priority.
	/// </summary>
	public delegate void RequestPriorityChangeEvent(IResourceRequest request, double oldPriority, double newPriority);

	/// <summary>
	/// This is the signature of the callback that is invoked when a resource request, executed without a block and
	/// initially refused, is eventually deemed grantable, and as well, later, to notify the requester that its request
	/// has been granted.
	/// </summary>
	public delegate bool ResourceRequestCallback(IResourceRequest resourceRequest);

    public enum RequestStatus { Free, Reserved, Acquired }
	/// <summary>
	/// IResourceRequest is an interface implemented by a class that is able
	/// to request a resource. This is typically an agent employed by the
	/// resource user itself. A resource request is submitted to a resource
	/// manager, whose job it is to mediate a process whereby the resource
	/// request selects, and is granted (or not) access to that resource. 
	/// </summary>
	public interface IResourceRequest : IComparable {

        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        double GetScore( IResource resource );

		/// <summary>
		/// This property represents the quantity this request is to remove from the resource's
		/// 'Available' capacity.
		/// </summary>
		double QuantityDesired { get; }

		/// <summary>
		/// This is a key that will be used to see if the resource manager is allowed to
		/// grant a given resource to the requester. It is used in conjunction with resource earmarking.
		/// (See IAccessRegulator) 
		/// </summary>
		object Key { get; }

		/// <summary>
		/// An indication of the priority of this request. A larger number indicates a higher priority.
		/// </summary>
		double Priority { get; set; } 

		/// <summary>
		/// An event that is fired if the priority of this request is changed.
		/// </summary>
		event RequestPriorityChangeEvent PriorityChangeEvent;

		/// <summary>
		/// If non-null, this infers a specific, needed resource.
		/// </summary>
		IResource RequiredResource { get; set; }

		/// <summary>
		/// This property represents the quantity this request actually removed from the resource's
		/// 'Available' capacity. It is filled in by the granting authority.
		/// </summary>
		double QuantityObtained { get; set; }

		/// <summary>
		/// This is a reference to the actual resource that was obtained.
		/// </summary>
		IResource ResourceObtained { get; set; }

        /// <summary>
        /// Gets the status of this resource request.
        /// </summary>
        /// <value>The status.</value>
        RequestStatus Status { get; set; }

		/// <summary>
		/// This is a reference to the resource manager that granted access to the resource.
		/// </summary>
		IResourceManager ResourceObtainedFrom { get; set; }

		/// <summary>
		/// This is a reference to the object requesting the resource.
		/// </summary>
		IHasIdentity Requester { get; set; }

		/// <summary>
		/// This is the resource selection strategy that is to be used by the resource
		/// manager to select the resource to be granted from the pool of available
		/// resources.
		/// </summary>
		ResourceSelectionStrategy ResourceSelectionStrategy { get; }

		/// <summary>
		/// Reserves a resource from the specified resource manager, or the provided default manager, if none is provided in this call.
		/// </summary>
		/// <param name="resourceManager">The resource manager from which the resource is desired. Can be null, if a default manager has been provided.</param>
		/// <param name="blockAwaitingReservation">If true, this call blocks until the resource is available.</param>
		/// <returns>true if the reservation was successful, false otherwise.</returns>
		bool Reserve(IResourceManager resourceManager, bool blockAwaitingReservation);
        
		/// <summary>
		/// Releases the resource previously obtained by this ResourceRequest.
		/// </summary>
		void Unreserve();

		/// <summary>
		/// Acquires a resource from the specified resource manager, or the provided default manager,
		/// if none is provided in this call. If the request has already successfully reserved a resource,
		/// then the reservation is revoked and the acquisition is honored in one atomic operation.
		/// </summary>
		/// <param name="resourceManager">The resource manager from which the resource is desired. Can be null, if a default manager has been provided.</param>
		/// <param name="blockAwaitingAcquisition">If true, this call blocks until the resource is available.</param>
		/// <returns>true if the acquisition was successful, false otherwise.</returns>
		bool Acquire(IResourceManager resourceManager, bool blockAwaitingAcquisition);
        
		/// <summary>
		/// Releases the resource previously obtained by this ResourceRequest.
		/// </summary>
		void Release();

		/// <summary>
		/// This method is called if the resource request is pending, and gets aborted, for
		/// example due to resource deadlocking. It can be null, in which case no deadlock
		/// detection is provided for the implementing type of ResourceRequest.
		/// </summary>
		DetachableEventAbortHandler AbortHandler { get; }

		/// <summary>
		/// Typically fires as a result of the RequestAbortHandler being called. In that method,
		/// it picks up the IResourceRequest identity, and is passed on through this event, which
		/// includes the IResourceRequest.
		/// </summary>
		event ResourceRequestAbortEvent ResourceRequestAborting;

		/// <summary>
		/// Creates a fresh replica of this resource request, without any of the in-progress data. This replica can
		/// be used to generate another, similar resource request that can acquire its own resource.
		/// </summary>
		ResourceRequestSource Replicate { get; }

		/// <summary>
		/// This is the resource manager from which a resource is obtained if none is provided in the reserve or
		/// acquire API calls.
		/// </summary>
		IResourceManager DefaultResourceManager { get; set; }

		/// <summary>
		/// This callback is called when a request, made with a do-not-block specification, that was initially
		/// refused, is finally deemed grantable, and provides the callee (presumably the original requester) 
		/// with an opportunity to say, "No, I don't want that any more", or perhaps to get ready for receipt
		/// of the resource in question.
		/// </summary>
		ResourceRequestCallback AsyncGrantConfirmationCallback { get; set; }

		/// <summary>
		/// Called after a resource request is granted asynchronously.
		/// </summary>
		ResourceRequestCallback AsyncGrantNotificationCallback { get; set; }

		/// <summary>
		/// Data maintained by this resource request on behalf of the requester.
		/// </summary>
		object UserData { set; get; }
    }

	/// <summary>
	/// A class that implements IModelWarning, and is intended to contain data on a resource request that
	/// was aborted due to deadlock or starvation, at the end of a model run.
	/// The creator of this class must add the instance into the Model's Warnings collection.
	/// </summary>
	public class TerminalResourceRequestAbortedWarning : IModelWarning {
        #region Private fields
        private readonly IDetachableEventController m_idec;
		private readonly IExecutive m_exec;
        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:TerminalResourceRequestAbortedWarning"/> class.
        /// </summary>
        /// <param name="exec">The executive under whose control this warning occurred.</param>
        /// <param name="mgr">The Resource Manager from which the resource was obtained.</param>
        /// <param name="req">The request through which the resource was obtained.</param>
        /// <param name="idec">The <see cref="Highpoint.Sage.SimCore.IDetachableEventController"/> that controls the thread in which the resource was last manipulated.</param>
        public TerminalResourceRequestAbortedWarning(IExecutive exec, IResourceManager mgr, IResourceRequest req, IDetachableEventController idec){
			m_idec = idec;
			ResourceRequest = req;
			ResourceManager = mgr;
			m_exec = exec;
			Narrative = GetNarrative();
		}

		#region IModelWarning Members

		public string Name => "Resource Request Aborted";

	    public string Narrative { get; }

	    /// <summary>
		/// Returns the IResourceManager that was unable to satisfy the request.
		/// </summary>
		public object Target => ResourceManager;

	    /// <summary>
		/// Returns the IResourceRequest that was unsatisfied.
		/// </summary>
		public object Subject => ResourceRequest;

	    /// <summary>
        /// Gets or sets the priority of the notification.
        /// </summary>
        /// <value>The priority.</value>
        public double Priority { get; set; }

	    #endregion

		/// <summary>
			/// Returns the IResourceManager that was unable to satisfy the request.
			/// </summary>
		public IResourceManager ResourceManager { get; }

	    /// <summary>
		/// Returns the IResourceRequest that was unsatisfied.
		/// </summary>
		public IResourceRequest ResourceRequest { get; }

	    /// <summary>
        /// Gets the narrative of this warning.
        /// </summary>
        /// <returns></returns>
		private string GetNarrative(){
	        string byWhom = ResourceRequest.Requester == null ? "<unknown requester>" : string.Format("{0} [{1}]", ResourceRequest.Requester.Name, ResourceRequest.Requester.Guid);
			string whenRequested = "<unknown>";
			if ( m_idec != null ) {
				whenRequested = m_idec.RootEvent.When.ToString(CultureInfo.CurrentCulture);
			} else {
				//System.Diagnostics.Debugger.Break();
			}
			
			string whenAborted = "<unknown abort time>";
			if ( m_exec != null ) whenAborted   = m_exec.Now.ToString(CultureInfo.CurrentCulture);

			if ( m_idec?.SuspendedStackTrace != null ) {
				return string.Format("The simulation ended at time {0}, with no more events. There was a request made at time {1} " 
					+ "by {2} of the resource manager {3}, but it was never able to service the request. The call was made from:\r\n",
					whenAborted,whenRequested,byWhom,ResourceManager.Name);
			} else {
				return string.Format("The simulation ended at time {0}, with no more events. There was a request made at time {1} " 
					+ "by {2} of the resource manager {3}, but it was never able to service the request.",
					whenAborted,whenRequested,byWhom,ResourceManager.Name);
			}
		}
	}

	/// <summary>
	/// MultiRequestProcessor provides ways to manipulate multiple resource requests at the same time.
	/// All requests must have a default resource manager specified, unless otherwise indicated in the
	/// specific API.
	/// </summary>
	public class MultiRequestProcessor {

        /// <summary>
        /// Replicates the specified requests.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
		public static IResourceRequest[] Replicate(ref IResourceRequest[] requests){
			IResourceRequest[] replicates = new IResourceRequest[requests.Length];
			for ( int i = 0 ; i < requests.Length ; i++ ) {
				replicates[i] = requests[i].Replicate();
			}
			return replicates;
		}

		/// <summary>
		/// Acquires all of the resources referred to in the array of requests,
		/// or if it cannot, it acquires none of them. If the blocking parameter is
		/// true, it keeps trying until it is successful. Otherwise, it tries once,
		/// and returns immediately, indicating success or failure.
		/// </summary>
		/// <param name="requests">The resource requests on which this processor is to operate.</param>
		/// <param name="blockAwaitingAcquisition">If true, this call blocks until the resource is available.</param>
		/// <returns>true if the acquisition was successful, false otherwise.</returns>
		public static bool AcquireAll(ref IResourceRequest[] requests, bool blockAwaitingAcquisition){
			if ( blockAwaitingAcquisition ) return AcquireAllWithWait(ref requests);

			bool successful = true;
			int i = 0;
			for ( ; i < requests.GetLength(0) ; i++ ) {
			    // ReSharper disable once CompareOfFloatsByEqualityOperator
				if ( requests[i].QuantityObtained == requests[i].QuantityDesired ) continue; // Already reserved.
				if ( !requests[i].Reserve(null,false) ) {
					successful = false;
					break;
				}
			}

			if ( successful ) i--; // walked off the end - get back to last live index.
			for ( ; i >= 0 ; --i ) {
				lock(requests[i].ResourceObtained){
					requests[i].Unreserve();
					if ( successful ) {
						requests[i].Acquire(null,false);
					}
				}
			}
			return successful;
		}

		/// <summary>
		/// Reserves all of the resources referred to in the array of requests,
		/// or if it cannot, it acquires none of them. If the blocking parameter is
		/// true, it keeps trying until it is successful. Otherwise, it tries once,
		/// and returns immediately, indicating success or failure.
		/// </summary>
		/// <param name="requests">The resource requests on which this processor is to operate.</param>
		/// <param name="blockAwaitingAcquisition">If true, this call blocks until the resource is available.</param>
		/// <returns>true if the reservation was successful, false otherwise.</returns>
		public static bool ReserveAll(ref IResourceRequest[] requests, bool blockAwaitingAcquisition){
			if ( blockAwaitingAcquisition ) return ReserveAllWithWait(ref requests);

			bool successful = true;
			ArrayList successes = new ArrayList(requests.Length);
			int i = 0;
			for ( ; i < requests.GetLength(0) ; i++ ) {
				if ( !requests[i].Reserve(null,false) ) {
					successful = false;
					break;
				}
				successes.Add(requests[i]);
			}
			if ( !successful ) {
				foreach ( IResourceRequest irr in successes ) irr.Unreserve();
			}
			return successful;
		}

		/// <summary>
		/// Releases all of the resources in the provided ResourceRequests.
		/// </summary>
		public static void ReleaseAll(ref IResourceRequest[] requests){
			foreach ( IResourceRequest irr in requests ) irr.Release();
		}

		/// <summary>
		/// Unreserves all of the resources in the provided ResourceRequests.
		/// </summary>
		public static void UnreserveAll(ref IResourceRequest[] requests)
		{
		    foreach (IResourceRequest irr in requests)
		    {
		        irr.Unreserve();
		    }
		}


	    private static bool ReserveAllWithWait(ref IResourceRequest[] requests){

			#region >>> Acquire all resources without deadlock. <<<
			// We will maintain a queue of resource requirements. The first one in the
			// queue is reserved with a wait-lock, and subsequent RP's are reserved
			// without a wait lock. If a reservation succeeds, then the RP is requeued
			// at the end of the queue. If it fails, then all RP's in the queue are
			// unreserved, and the next attempt begins at the beginning of the queue.
			// -
			// Note that in this next attempt, the one for whom reservation has most
			// recently failed is still at the head of the queue, and is the one that
			// is reserved with a wait-lock.

			Hashtable successes = new Hashtable();
			Queue rscQueue = new Queue();

			#region >>> Load the queue with the resource requests. <<< 
			foreach (IResourceRequest irr in requests)
			    rscQueue.Enqueue(irr);

	        #endregion

			bool nextIsMaster = true;
			while ( rscQueue.Count > 0 ){
				IResourceRequest rp = (IResourceRequest)rscQueue.Peek();
				if ( successes.Contains(rp) ) break; // We've acquired all of them.
				bool rpSucceeded = rp.Reserve(null,nextIsMaster);
				nextIsMaster = !rpSucceeded; // If failed, the next time through, the head of the q will be master.
				if ( !rpSucceeded ) {
					foreach ( IResourceRequest reset in rscQueue ) {
						if ( successes.Contains(rp) ) reset.Unreserve();
					}
					successes.Clear();
				} else {
					rscQueue.Enqueue(rscQueue.Dequeue()); // Send the successful request to the back of the queue.
					successes.Add(rp,rp);
				}
			}
//			if ( rscQueue.Count == 0 ) {
//				_Debug.WriteLine("No resources were requested.");
//			}
			#endregion

			return true;
		}
        
		private static bool AcquireAllWithWait(ref IResourceRequest[] requests){

			if ( ReserveAllWithWait(ref requests) ) {
				foreach ( IResourceRequest rrq in requests ) {
					//rrq.Unreserve();
					rrq.Acquire(null,true);
				}
				return true;
			}

			return false;
		}
	}
}
