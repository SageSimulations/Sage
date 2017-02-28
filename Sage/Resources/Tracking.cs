/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using System.Linq;
using Highpoint.Sage.SimCore;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources {

	/// <summary>
	/// An enumeration of the types of transactions that can take place, involving a resource.
	/// </summary>
	public enum ResourceAction { 
		/// <summary>
		/// The resource was requested. This does not infer success.
		/// </summary>
		Request, 
		/// <summary>
		/// The resource was reserved. This indicates that the resource was taken out of general availability, but not granted.
		/// </summary>
		Reserved, 
		/// <summary>
		/// The resource was unreserved. This indicates that the resource was placed back into general availability after having been reserved.
		/// </summary>
		Unreserved, 
		/// <summary>
		/// The resource was acquired. This indicates that all or part of the resource's capacity was removed from general availability.
		/// </summary>
		Acquired,
		/// <summary>
		/// The resource was released. This means that all or part of its capacity was placed back into general availability.
		/// </summary>
		Released }

	
    /// <summary>
    /// Implemented by anything that gathers ResourceEventRecords on a specific resource or resources.
    /// </summary>
	public interface IResourceTracker : IEnumerable {

		/// <summary>
		/// Clears all ResourceEventRecords.
		/// </summary>
		void Clear();

		/// <summary>
		/// Turns on tracking for this ResourceTracker. This defaults to 'true', and
		/// allEnabled must also be true, in order for a ResourceTracker to track.
		/// </summary>
		bool Enabled { get ; set; } 

		/// <summary>
		/// Allows for the setting of the active filter on the records
		/// </summary>
		ResourceEventRecordFilter Filter { set; }

		/// <summary>
		/// Returns all records that have been collected
		/// </summary>
		ICollection EventRecords { get; }

		/// <summary>
		/// The initial value(s) of all resources that are being tracked
		/// </summary>
		double InitialAvailable { get; }
	}

    /// <summary>
    /// This class is the baseline implementation of <see cref="Highpoint.Sage.Resources.IResourceTracker"/>. It watches
    /// a specified resource over a model run, and creates &amp; collects <see cref="Highpoint.Sage.Resources.ResourceEventRecord"/>s on the activities of that resource.
    /// </summary>
	public class ResourceTracker : IResourceTracker {

        #region Private Fields
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("ResourceTracker");
        private readonly ArrayList m_record;
        private readonly IResource m_target;
        private readonly IModel m_model;
        private ResourceEventRecordFilter m_rerFilter;

        #endregion 

		/// <summary>
		/// Tracks utilization of a particular resource.
		/// </summary>
		/// <param name="model">The parent model to which the resource, and this tracker, will belong.</param>
		/// <param name="target">The resource that this tracker will track.</param>
		public ResourceTracker(IModel model, IResource target){
			m_model = model;
			m_target = target;
			m_rerFilter = ResourceEventRecordFilters.AllEvents;
			m_target.RequestEvent   += m_target_RequestEvent;
			m_target.ReservedEvent  += m_target_ReservedEvent;
			m_target.UnreservedEvent+= m_target_UnreservedEvent;
			m_target.AcquiredEvent  += m_target_AcquiredEvent;
			m_target.ReleasedEvent  += m_target_ReleasedEvent;
			m_record = new ArrayList();
			if ( s_diagnostics ) _Debug.WriteLine(m_model.Executive.Now + " : Created a Resource Tracker focused on " + m_target.Name + " (" + m_target.Guid + ").");
		}

		/// <summary>
		/// The resource that this tracker is tracking.
		/// </summary>
		public IResource Resource => m_target;

        /// <summary>
		/// Returns an enumerator across all ResourceEventRecords.
		/// </summary>
		/// <returns>An enumerator across all ResourceEventRecords.</returns>
		public IEnumerator GetEnumerator(){ return m_record.GetEnumerator(); }

		/// <summary>
		/// Returns all event records that have been collected
		/// </summary>
		public ICollection EventRecords => ArrayList.ReadOnly(m_record);

        /// <summary>
		/// The InitialAvailable(s) of all resources that are being tracked
		/// </summary>
		public double InitialAvailable => Resource.InitialAvailable;

        /// <summary>
		/// Clears all ResourceEventRecords.
		/// </summary>
		public void Clear(){ m_record.Clear(); }

		/// <summary>
		/// Turns on tracking for this ResourceTracker. This defaults to 'true', and
		/// allEnabled must also be true, in order for a ResourceTracker to track.
		/// </summary>
		public bool Enabled { get; set; } = true;

        /// <summary>
		/// If false, all Trackers will be disabled. If true, then the individual tracker's setting governs.
		/// </summary>
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
		public static bool GlobalEnabled { get; set; } = true;

        /// <summary>
		/// The filter is given a look at each prospective Record, and allowed to decide whether it is to
		/// be logged or not. In conjunction with simply not adding a resource to the tracker, you can achieve
		/// fine-grained control of the contents of a resource activity log.
		/// </summary>
		public ResourceEventRecordFilter Filter { set{ m_rerFilter = value; } }

        #region Private Members
        private void m_target_RequestEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Request);
            }
        }

        private void m_target_ReservedEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Reserved);
            }
        }

        private void m_target_UnreservedEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Unreserved);
            }
        }

        private void m_target_AcquiredEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Acquired);
            }
        }

        private void m_target_ReleasedEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Released);
            }
        }

        private void LogEvent(IResource resource, IResourceRequest irr, ResourceAction action)
        {
            if (s_diagnostics) _Debug.WriteLine(m_model.Executive.Now + " : Resource Tracker " + m_target.Name
                                   + " (" + m_target.Guid + ") logged " + action
                                   + " with " + irr.QuantityDesired + ".");
            ResourceEventRecord rer = new ResourceEventRecord(m_model.Executive.Now, resource, irr, action);
            if (m_rerFilter == null || m_rerFilter(rer))
            {
                m_record.Add(rer);
                if (s_diagnostics) _Debug.WriteLine("\tLogged.");
            }
            else
            {
                if (s_diagnostics) _Debug.WriteLine("\tFiltered out.");
            }
        }

        #endregion 

	}

    /// <summary>
    /// A static holder for some static and stateless <see cref="Highpoint.Sage.Resources.ResourceEventRecordFilter"/>s.
    /// </summary>
	public static class ResourceEventRecordFilters {
        
        /// <summary>
        /// A filter that filters out requests, allowing the actual Acquire &amp; Release events to pass.
        /// </summary>
        /// <value>The filter.</value>
		public static ResourceEventRecordFilter FilterOutRequests => m_filterOutRequests;

        /// <summary>
        /// A filter that gets the acquire and release events only.
        /// </summary>
        /// <value>The acquire and release events only.</value>
		public static ResourceEventRecordFilter AcquireAndReleaseOnly => m_acquireAndReleaseOnly;

        /// <summary>
        /// Gets all events.
        /// </summary>
        /// <value>All events.</value>
		public static ResourceEventRecordFilter AllEvents => m_allEvents;

        #region Private Members
        private static bool m_acquireAndReleaseOnly(ResourceEventRecord candidate)
        {
            return (candidate.Action == ResourceAction.Acquired || candidate.Action == ResourceAction.Released);
        }
        private static bool m_filterOutRequests(ResourceEventRecord candidate)
        {
            return (candidate.Action != ResourceAction.Request);
        }
        private static bool m_allEvents(ResourceEventRecord candidate)
        {
            return true;
        }

        #endregion 

	}

	/// <summary>
	/// A MultiResourceTracker is a resource tracker (gathers copies of resource event records) that
	/// can monitor multiple resources during a simulation.
	/// </summary>
	public class MultiResourceTracker : IResourceTracker {

        #region Private Members
        private readonly ArrayList m_record;
        private readonly ArrayList m_targets;
        private readonly IModel m_model;
        private bool m_enabled = true;
        private static bool _allEnabled = true;
        private ResourceEventRecordFilter m_rerFilter;

        #endregion 

		/// <summary>
		/// Tracks utilization of a particular resource.
		/// </summary>
		/// <param name="model">The parent model to which the resource, and this tracker, will belong.</param>
		public MultiResourceTracker(IModel model){
			m_model = model;
			m_record = new ArrayList();
			m_targets = new ArrayList();
			m_rerFilter = ResourceEventRecordFilters.AllEvents;
		}

        /// <summary>
        /// Creates a new instance of the <see cref="T:MultiResourceTracker"/> class.
        /// </summary>
        /// <param name="trackers">The trackers that are aggregated by this <see cref="T:MultiResourceTracker"/>.</param>
		public MultiResourceTracker(IResourceTracker[] trackers){
			m_model = null;
            m_targets = new ArrayList(trackers);
            m_rerFilter = ResourceEventRecordFilters.AllEvents;
        }

        /// <summary>
        /// Adds the specified resources to those being monitored by this tracker.
        /// </summary>
        /// <param name="targets">The IResource entities that are to be tracked.</param>
        public void AddTargets(params IResource[] targets){
			foreach ( IResource target in targets) {
				AddTarget(target);
			}
		}

		/// <summary>
		/// Adds the specified resource to those being monitored by this tracker.
		/// </summary>
		/// <param name="target"></param>
		public void AddTarget(IResource target){
			m_targets.Add(target);
			target.RequestEvent   += target_RequestEvent;
			target.ReservedEvent  += target_ReservedEvent;
			target.UnreservedEvent+= target_UnreservedEvent;
			target.AcquiredEvent  += target_AcquiredEvent;
			target.ReleasedEvent  += target_ReleasedEvent;
		}

		/// <summary>
		/// Returns an enumerator across all ResourceEventRecords.
		/// </summary>
		/// <returns>An enumerator across all ResourceEventRecords.</returns>
		[Obsolete("Use EventRecords getter instead")]
		public IEnumerator GetEnumerator(){ return m_record.GetEnumerator(); }
		
        /// <summary>
		/// Returns all event records that have been collected
		/// </summary>
		public ICollection EventRecords => ArrayList.ReadOnly(m_record);

	    /// <summary>
		/// The sum of the InitialAvailable(s) of all resources that are being tracked
		/// </summary>
		public double InitialAvailable { 
			get
			{
			    return m_targets.Cast<IResource>().Sum(rsc => rsc.InitialAvailable);
			}
		}
		
        /// <summary>
		/// Clears all ResourceEventRecords.
		/// </summary>
		public void Clear(){ m_record.Clear(); }
		
        /// <summary>
		/// Turns on tracking for this ResourceTracker. This defaults to 'true', and
		/// allEnabled must also be true, in order for a ResourceTracker to track.
		/// </summary>
		public bool Enabled { get { return m_enabled; } set { m_enabled = value; } } 

        /// <summary>
		/// If false, all Trackers will be disabled. If true, then the individual tracker's setting governs.
		/// </summary>
		public static bool GlobalEnabled { get { return _allEnabled; } set { _allEnabled = value; } } 

		/// <summary>
		/// The filter is given a look at each prospective Record, and allowed to decide whether it is to
		/// be logged or not. In conjunction with simply not adding a resource to the tracker, you can achieve
		/// fine-grained control of the contents of a resource activity log.
		/// </summary>
		public ResourceEventRecordFilter Filter { set { m_rerFilter = value; } }

		/// <summary>
		/// Loads a collection of resource event records, and then sorts them using the provided comparer.
		/// </summary>
		/// <param name="bulkRecords">The collection of resource records to be added to this collection.</param>
		/// <param name="clearAllFirst">If true, this tracker's ResourceEventRecord internal collection is cleared out before the new records are added.</param>
		/// <param name="sortCriteria">An IComparer that can compare ResourceEventRecord objects. See ResourceEventRecord.By...() methods.</param>
		public void BulkLoad(ICollection bulkRecords, bool clearAllFirst, IComparer sortCriteria ){
			if ( clearAllFirst ) m_record.Clear();
			m_record.AddRange(bulkRecords);
			if ( sortCriteria != null ) m_record.Sort(sortCriteria);
		}

		/// <summary>
		/// Loads a collection of resource event records, and then sorts them by serial number in ascending order.
		/// </summary>
		/// <param name="bulkRecords">The collection of resource records to be added to this collection.</param>
		/// <param name="clearAllFirst">If true, this tracker's ResourceEventRecord internal collection is cleared out before the new records are added.</param>
		public void BulkLoad(ICollection bulkRecords, bool clearAllFirst){
			BulkLoad(bulkRecords,clearAllFirst,ResourceEventRecord.BySerialNumber(false));
		}

		/// <summary>
		/// Loads a collection of resource event records, and then sorts them by serial number in ascending order.
		/// </summary>
		/// <param name="bulkRecords">The collection of resource records to be added to this collection.</param>
		public void BulkLoad(ICollection bulkRecords){
			BulkLoad(bulkRecords,true,ResourceEventRecord.BySerialNumber(false));
		}

        #region Private Members
        private void target_RequestEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && m_enabled)
            {
                LogEvent(resource, irr, ResourceAction.Request);
            }
        }

        private void target_ReservedEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && m_enabled)
            {
                LogEvent(resource, irr, ResourceAction.Reserved);
            }
        }

        private void target_UnreservedEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && m_enabled)
            {
                LogEvent(resource, irr, ResourceAction.Unreserved);
            }
        }

        private void target_AcquiredEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && m_enabled)
            {
                LogEvent(resource, irr, ResourceAction.Acquired);
            }
        }

        private void target_ReleasedEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && m_enabled)
            {
                LogEvent(resource, irr, ResourceAction.Released);
            }
        }

        private void LogEvent(IResource resource, IResourceRequest irr, ResourceAction action)
        {
            ResourceEventRecord rer = new ResourceEventRecord(m_model.Executive.Now, resource, irr, action);
            if (m_rerFilter != null && m_rerFilter(rer)) m_record.Add(rer);
        }

        #endregion 

	}

#if INCLUDE_WIP
    public class StaticResourceTracker : IResourceTracker
    {

        #region Private Fields

#endregion

        public StaticResourceTracker(ICollection resourceEventRecords, double initialAvailable = 0){
			EventRecords = resourceEventRecords;
			InitialAvailable = initialAvailable;
		}

        #region IResourceTracker Members
		        public void Clear() { EventRecords = new ArrayList();} // Not sure why someone would want to do this on a static tracker.

		        public bool Enabled { get { return false; } set { throw new NotImplementedException(); } }

		        public ResourceEventRecordFilter Filter { set { throw new NotImplementedException(); } }

		        public ICollection EventRecords { get; private set; }

                /// <summary>
                /// The initial value(s) of all resources that are being tracked
                /// </summary>
                /// <value></value>
		        public double InitialAvailable { get; }

        #endregion

        #region IEnumerable Members

                /// <summary>
                /// Returns an enumerator that iterates through a collection.
                /// </summary>
                /// <returns>
                /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
                /// </returns>
		        public IEnumerator GetEnumerator() {
			        return EventRecords.GetEnumerator();
		        }

        #endregion

	}
#endif

	/// <summary>
	/// Class that consolidates a collection of IResourceTrackers
	/// </summary>
    public class ResourceTrackerAggregator : IResourceTracker
    {

#region Private Fields

        private readonly ArrayList m_records;
        private readonly ArrayList m_targets;

#endregion

		/// <summary>
		/// Standard constructor
		/// </summary>
		/// <param name="trackers">The trackers to consolidate</param>
		public ResourceTrackerAggregator(IEnumerable trackers){
			m_records = new ArrayList();
			m_targets = new ArrayList();

		    // ReSharper disable once LoopCanBePartlyConvertedToQuery (Much clearer this way.)
			foreach(IResourceTracker rt in trackers) {
				foreach(ResourceEventRecord rer in rt.EventRecords) {
					if(!m_targets.Contains(rer.Resource)) m_targets.Add(rer.Resource);
					m_records.Add(rer);
				} // end foreach rer
			} // end foreach rt

			m_records.Sort(ResourceEventRecord.BySerialNumber(false));
		} // end ResourceTrackerAggregator

#region IResourceTracker Members

		/// <summary>
		/// Clears all ResourceEventRecords.
		/// </summary>
		public void Clear() {
			throw new NotImplementedException("Cannot clear this tracker's record collection. Create a new one if you have a new aggregation to represent.");
		}
		/// <summary>
		/// Turns on tracking for this ResourceTracker. This defaults to 'true', and
		/// allEnabled must also be true, in order for a ResourceTracker to track.
		/// </summary>
		public bool Enabled {
			get { return false; }
			set { throw new NotImplementedException("Cannot enable this tracker to perform further tracking. It is an aggregated record collection only.");}
		}
		/// <summary>
		/// Allows for the setting of the active filter on the records
		/// </summary>
		public ResourceEventRecordFilter Filter {
			set { throw new NotImplementedException("Cannot change the filter on this tracker to perform further tracking. It is an aggregated record collection only.");}
		}
		/// <summary>
		/// Returns all event records that have been collected
		/// </summary>
		public ICollection EventRecords => ArrayList.ReadOnly(m_records);

	    /// <summary>
		/// The InitialAvailable(s) of all resources that are being tracked
		/// </summary>
		public double InitialAvailable { 
			get
			{
			    return m_targets.Cast<IResource>().Sum(rsc => rsc.InitialAvailable);
			}
		}

#endregion

#region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
		[Obsolete("Use EventRecords getter instead")]
		public IEnumerator GetEnumerator() {
			return m_records.GetEnumerator();
		}


#endregion

	}

    /// <summary>
    /// Implemented by anything that can filter ResourceEventRecords.
    /// </summary>
    /// <param name="candidate">The ResourceEventRecord for consideration.</param>
    /// <returns>true if the ResourceEventRecord is to be passed through the filter, false if it is to be filtered out.</returns>
	public delegate bool ResourceEventRecordFilter(ResourceEventRecord candidate);

    /// <summary>
    /// A record that represents the details of a transaction involving a resource. These include the various <see cref="Highpoint.Sage.Resources.ResourceAction"/>s.
    /// </summary>
    public class ResourceEventRecord
    {

#region Private Fields

        private readonly IResource m_resource;
        private double m_quantityDesired;
        private double m_quantityObtained;
        private double m_capacity;
        private object m_tag;
        private Guid m_tagGuid;
        private IEditor m_myEditor;

#endregion

        /// <summary>
        /// Constructs a record of a resource transaction.
        /// </summary>
        /// <param name="when">The time (model time) that the transaction took place.</param>
        /// <param name="resource">The resource against which this transaction took place.</param>
        /// <param name="irr">The resource request that initiated this transaction.</param>
        /// <param name="action">The type of <see cref="Highpoint.Sage.Resources.ResourceAction"/> that took place.</param>
        public ResourceEventRecord(DateTime when, IResource resource, IResourceRequest irr, ResourceAction action)
        {
			m_resource = resource;
			ResourceGuid = resource.Guid;
			When = when;
			m_quantityDesired = irr.QuantityDesired;
			m_quantityObtained = irr.QuantityObtained;
			m_capacity = resource.Capacity;
			Available = resource.Available;
			Requester = irr.Requester;
			RequesterGuid = irr.Requester?.Guid ?? Guid.Empty;
			Action = action;
			m_tag = null;
			m_tagGuid = Guid.Empty;
			SerialNumber = Utility.SerialNumberService.GetNext();
		}

        /// <summary>
        /// Constructs a record of a resource transaction.
        /// </summary>
        /// <param name="when">The time (model time) that the transaction took place.</param>
        /// <param name="resourceGuid">The GUID of the resource against which this transaction took place.</param>
        /// <param name="desired">The quantity that was desired of the specified resource.</param>
        /// <param name="obtained">The quantity that was obtained of the specified resource.</param>
        /// <param name="capacity">The capacity of the specified resource after this transaction took place.</param>
        /// <param name="available">The amount available of the specified resource after this transaction took place.</param>
        /// <param name="requesterGuid">The GUID of the requester.</param>
        /// <param name="action">The type of <see cref="Highpoint.Sage.Resources.ResourceAction"/> that took place.</param>
		public ResourceEventRecord(DateTime when, Guid resourceGuid, double desired, double obtained, double capacity, double available, Guid requesterGuid, ResourceAction action ) {
			When = when;
			m_resource = null;
			ResourceGuid = resourceGuid;
			m_quantityDesired = desired;
			m_quantityObtained = obtained;
			m_capacity = capacity;
			Available = available;
			Requester = null;
			RequesterGuid = requesterGuid;
			Action = action;
			m_tag = null;
			m_tagGuid = Guid.Empty;
			SerialNumber = Utility.SerialNumberService.GetNext();
		}

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source">Source RER to use as as a base</param>
        /// <param name="replacementResource">The resource that replaces the resource from the source RER in the newly constructed ResourceEventRecord.</param>
		public ResourceEventRecord(ResourceEventRecord source, IResource replacementResource) {
			When = source.When;
			ResourceGuid = source.ResourceGuid;
			m_quantityDesired = source.QuantityDesired;
			m_quantityObtained = source.QuantityObtained;
			m_capacity = source.Capacity;
			Available = source.Available;
			Requester = source.Requester;
			RequesterGuid = source.RequesterGuid;
			Action = source.Action;
			m_tag = source.Tag;
			m_tagGuid = source.TagGuid;
			SerialNumber = source.SerialNumber;

			m_resource = replacementResource;
			ResourceGuid = replacementResource.Guid;
		}

        /// <summary>
        /// Ancillary data for consumption by client code.
        /// </summary>
        /// <value>The tag GUID.</value>
		public Guid TagGuid { 
			get { return m_tagGuid; }
			set { 
				m_tag = null;
				m_tagGuid = value;
			} 
		}

        /// <summary>
        /// Ancillary data for consumption by client code.
        /// </summary>
        /// <value>The tag.</value>
		public object Tag {
			get { return m_tag; }
			set {
				m_tag = value;
			    IHasIdentity tag = m_tag as IHasIdentity;
			    if ( tag != null ) m_tagGuid = tag.Guid;
			}
		}
		
        /// <summary>
		/// The resource against which this event transpired.
		/// </summary>
		public IResource Resource => m_resource;

        /// <summary>
		/// The guid of the resource against which this event transpired.
		/// </summary>
		public Guid ResourceGuid { get; }

        /// <summary>
		/// The time that the event transpired.
		/// </summary>
		public DateTime When { get; }

        /// <summary>
		/// The quantity of resource that was desired by the resource request.
		/// </summary>
		public double QuantityDesired => m_quantityDesired;

        /// <summary>
		/// The amount of resource granted to the requester.
		/// </summary>
		public double QuantityObtained => m_quantityObtained;

        /// <summary>
		/// The capacity of the resource at the time of the request.
		/// </summary>
		public double Capacity => m_capacity;

        /// <summary>
		/// The amount of the resource that was available AFTER the request was handled.
		/// </summary>
		public double Available { get; private set; }

        /// <summary>
		/// The identity of the entity that requested the resource.
		/// </summary>
		public IHasIdentity Requester { get; }

        /// <summary>
		/// The identity of the entity that requested the resource.
		/// </summary>
        [Obsolete("Use \"Requester\" instead.",false)]
		public IHasIdentity ByWhom => Requester;

        /// <summary>
		/// The identity of the entity that requested the resource.
		/// </summary>
		public Guid RequesterGuid { get; }

        /// <summary>
		/// The type of resource action that took place (Request, Reserved, Unreserved, Acquired, Released).
		/// </summary>
		public ResourceAction Action { get; }

        /// <summary>
		/// The serial number of this Resource Event Record.
		/// </summary>
		public long SerialNumber { get; }

        /// <summary>
		/// Returns a string representation of this transaction.
		/// </summary>
		/// <returns>A string representation of this transaction.</returns>
		public override string ToString() {
			return When + " : " + m_resource.Name + ", " + m_quantityObtained 
				+ ", " + m_quantityDesired + ", " + m_capacity + ", " + Available + ", " 
				+ (Requester==null?"<unknown>":Requester.Name) + ", " + Action;
		}

		/// <summary>
		/// Returns a detailed string representation of this transaction.
		/// </summary>
		/// <returns>A detailed string representation of this transaction.</returns>
		public string Detail() {
			string tagString = "";
		    IHasIdentity tag = m_tag as IHasIdentity;
		    if ( tag != null ) {
				tagString =", " + tag.Name + "(" + tag.Guid + ")";
			}

			return When + " : " + (m_resource==null?"<unknown>":m_resource.Name) + ", " + m_quantityObtained 
				+ ", " + m_quantityDesired + ", " + m_capacity + ", " + Available + ", " 
				+ (Requester==null?"<unknown>":Requester.Name) + ", " + Action + tagString;
		}

		/// <summary>
		/// Returns a string representation of a header for a table of ResourceEventRecords, identifying the columns.
		/// </summary>
		/// <returns>A string representation of a header for a table of ResourceEventRecords, identifying the columns.</returns>
		public static string ToStringHeader(){
			return "When\tName\tObtained\tDesired \tCapacity\tAvailable\tByWhom\tTransactType";
		}
       
        /// <summary>
        /// Gets the object that provides editing capability into this RER.
        /// </summary>
        /// <value>The editor.</value>
		public IEditor Editor => m_myEditor ?? (m_myEditor = new RerEditor(this));

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their Resource Names.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
		public static IComparer ByResourceName(bool reverse) { return new SortByResourceName(reverse); }

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their times of occurrence.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
        public static IComparer ByTime(bool reverse) { return new SortByTime(reverse); }

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their Action types.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
        public static IComparer ByAction(bool reverse) { return new SortByAction(reverse); }

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their serial numbers.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
        public static IComparer BySerialNumber(bool reverse) { return new SortBySerialNumber(reverse); } 

        /// <summary>
        /// An abstract class from which all Resource Event Record Comparers inherit.
        /// </summary>
		public abstract class RerComparer : IComparer {
			private readonly int m_reverse;
            /// <summary>
            /// Creates a new instance of the <see cref="T:RERComparer"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
            protected RerComparer(bool reverse){m_reverse = reverse?-1:1;}
#region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			public abstract int Compare(object x, object y);
#endregion
			protected int Flip(int i){return i * m_reverse;}
		}

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their Resource Names.
        /// </summary>
        public class SortByResourceName : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByResourceName"/> class.
            /// </summary>
			public SortByResourceName():base(false){}
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByResourceName"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortByResourceName(bool reverse):base(reverse){}
#region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			public override int Compare(object x, object y) {
				return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).Resource.Name,((ResourceEventRecord)y).Resource.Name));
			}
#endregion
		}

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their times of occurrence.
        /// </summary>
        public class SortByTime : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByTime"/> class.
            /// </summary>
			public SortByTime():base(false){}
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByTime"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortByTime(bool reverse):base(reverse){}
#region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			public override int Compare(object x, object y) {
				return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).When,((ResourceEventRecord)y).When));
			}

#endregion
		}

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their serial numbers.
        /// </summary>
        public class SortBySerialNumber : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortBySerialNumber"/> class.
            /// </summary>
			public SortBySerialNumber():base(false){}
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortBySerialNumber"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortBySerialNumber(bool reverse):base(reverse){}
#region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			public override int Compare(object x, object y) {
				return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).SerialNumber,((ResourceEventRecord)y).SerialNumber));
			}

#endregion
		}

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their Action types.
        /// </summary>
        public class SortByAction : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByAction"/> class.
            /// </summary>
			public SortByAction():base(false){}
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByAction"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortByAction(bool reverse):base(reverse){}
#region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			public override int Compare(object x, object y) {
				return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).Action,((ResourceEventRecord)y).Action));
			}

#endregion
		}

        /// <summary>
        /// Implemented by an object that can set the values of a ResourceEventRecord. Typically granted by the ResourceEventRecord itself, so that the RER can control who is able to modify it.
        /// </summary>
		public interface IEditor {
            /// <summary>
            /// Sets the available quantity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetAvailable(double newValue);
            /// <summary>
            /// Sets the capacity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetCapacity(double newValue);
            /// <summary>
            /// Sets the quantity desired of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetQuantityDesired(double newValue);
            /// <summary>
            /// Sets the quantity obtained of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetQuantityObtained(double newValue);
		}

        /// <summary>
        /// This ResourceEventRecord implementation's internal implementation of IEditor.
        /// </summary>
		internal class RerEditor : IEditor {
			private readonly ResourceEventRecord m_rer;
            /// <summary>
            /// Creates a new instance of the <see cref="T:RerEditor"/> class.
            /// </summary>
            /// <param name="rer">The rer.</param>
			internal RerEditor(ResourceEventRecord rer){
				m_rer = rer;
			}
            /// <summary>
            /// Sets the available quantity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetAvailable(double newValue){ m_rer.Available = newValue; }
            /// <summary>
            /// Sets the capacity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetCapacity(double newValue){ m_rer.m_capacity = newValue; }
            /// <summary>
            /// Sets the quantity desired of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetQuantityDesired(double newValue){ m_rer.m_quantityDesired = newValue; }
            /// <summary>
            /// Sets the quantity obtained of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetQuantityObtained(double newValue){ m_rer.m_quantityObtained = newValue; }

		}
	}
}