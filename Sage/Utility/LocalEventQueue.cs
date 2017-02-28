/* This source code licensed under the GNU Affero General Public License */
#define DEBUG_SORTING
//#define USING_CANNED_HEAP

// TODO: Convert to Canned Heap implementation.

using System;
using Highpoint.Sage.SimCore;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Utility
{
	/// <summary>
	/// This class represents a manager for events that are all generated and consumed within a
	/// local object (such as a tool's inner workings). It schedules only the soonest event, and
	/// ensures that when that soonest event is serviced, the next 'soonest' event will then be
	/// scheduled.
	/// <br></br>
	/// It currently requires that each event fed into it be the furthest-out event. In other words,
	/// it is a FIFO queue and assumes that its user will ensure temporal sequence in the events
	/// fed into it.
	/// <br></br>
	/// If you do not expect to be able to follow the temporal restrictions, use the LocalEventHeap
	/// class instead.
	/// </summary>
	public class LocalEventQueue : IHasName {
		private readonly IExecutive m_exec;
		private readonly string m_targetName;
		private readonly ExecEventReceiver m_eer;
		private readonly ExecEventReceiver m_dequeueHandler;
		private int m_capacity;
		private int m_head;
		private int m_tail;
		//private Heap m_heap;
		EventData[] m_circQueue;
		//EventDataCache m_edCache;

        /// <summary>
        /// Creates a new instance of the <see cref="T:LocalEventQueue"/> class.
        /// </summary>
        /// <param name="exec">The executive that this LocalEventQueue services.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="eer">The <see cref="Highpoint.Sage.SimCore.ExecEventReceiver"/> that this LocalEventQueue manages callbacks into.</param>
		public LocalEventQueue(IExecutive exec, int capacity, ExecEventReceiver eer){
			m_exec = exec;
			m_capacity = capacity;
			m_eer = eer;
			m_targetName = ((IHasName)m_eer.Target).Name;

			m_dequeueHandler = Dequeue;

			m_circQueue = new EventData[capacity];
			for ( int i = 0 ; i < capacity ; i++ ) m_circQueue[i]=new EventData();
			//m_edCache = new EventDataCache(capacity);
			m_head=0;
			m_tail=0;

#if USING_CANNED_HEAP
			m_heap = new Heap(Heap.HEAP_RULE.MinValue);
#endif

			if ( m_capacity < 1 ) throw new ArgumentException("LocalEventQueue created with a negative or zero capacity - it must be positive!");

		}

#if USING_CANNED_HEAP
		public void Enqueue(object what, DateTime when){
			//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Enqueueing " + when.ToString() );
			EventData ed = new EventData();
			ed.What = what;
			ed.When = when;
			m_heap.Enqueue(ed);

			EventData ed2 = (EventData)m_heap.Peek();
			if (!ed2.HasBeenScheduled ) {
				m_exec.RequestEvent(m_dequeueHandler,ed2.When,0.0,ed2.What);
				ed2.HasBeenScheduled = true;
				//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Scheduling " + ed2.ToString() );
			} else {
				//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Scheduling is unnecessary." );
			}
			//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Heap head is now (e) : " + ed2.ToString() );
		}
		private void Dequeue(IExecutive exec, object userData) {
			//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Dequeueing " + exec.Now.ToString() );
			EventData ed = (EventData)m_heap.Dequeue();
			System.Diagnostics.Debug.Assert(userData.Equals(ed.What));

			EventData ed2 = (EventData)m_heap.Peek();
			if (!ed2.HasBeenScheduled ) {
				m_exec.RequestEvent(m_dequeueHandler,ed2.When,0.0,ed2.What);
				ed2.HasBeenScheduled = true;
				//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Scheduling " + ed2.ToString() );
			} else {
				//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Scheduling is unnecessary." );
			}
			//if ( m_targetName.Equals("Furn_FastRmp_4") ) Console.WriteLine("Heap head is now (d) : " + ed2.ToString() );

			m_eer(exec,userData);

		}
#else
        /// <summary>
        /// Enqueues a callback to the ExecEventReceiver on the specified object for the specified time.
        /// </summary>
        /// <param name="what">The specified object.</param>
        /// <param name="when">The specified time.</param>
		public void Enqueue(object what, DateTime when){
            _Debug.Assert(!when.Equals(DateTime.MinValue));

			int ndxEmpty = m_tail;
			EventData empty = m_circQueue[ndxEmpty];

			int ndxCandidate = ndxEmpty-1;
			if ( ndxCandidate == -1 ) ndxCandidate += m_capacity;
			EventData candidate = m_circQueue[ndxCandidate];
			
			while ( ndxEmpty != m_head && candidate.When > when ) { // Slide the candidate down one, make rm for new ED in its place.
				// Slide the candidate down one, freeing up its slot.
				empty.What = candidate.What;
				empty.When = candidate.When;
				empty.HasBeenScheduled = candidate.HasBeenScheduled;
				// Move the pointers up one in the array.
				empty = candidate;
				ndxEmpty = ndxCandidate;
				ndxCandidate--;
				if ( ndxCandidate == -1 ) ndxCandidate += m_capacity;
				// update the candidate.
				candidate = m_circQueue[ndxCandidate];
			}

			// Once we've escaped the preceding loop, we insert the new ED at the insertion point, and advance the tail.
			empty.What = what;
			empty.When = when;
			empty.HasBeenScheduled = false;

			EventData edHead = m_circQueue[m_head];
			if ( !edHead.HasBeenScheduled ) {
				m_exec.RequestEvent(m_dequeueHandler,edHead.When,0.0,edHead.What);
				edHead.HasBeenScheduled = true;
			}

			m_tail++;
			if ( m_tail == m_capacity ) m_tail = 0;

			// If after advancing the tail point, the head & tail pointers
			// overlap, it means we're full and must expand the list capacity.
			if ( m_tail == m_head ){
				#region Expand the array

				int capacity = m_capacity*2;
				EventData[] tmp = new EventData[capacity];
				int ptr=0;
				
				do {
					tmp[ptr++] = m_circQueue[m_head++];
					if ( m_head == m_capacity ) m_head=0;
				} while ( m_head != m_tail);

				m_circQueue = tmp;

				for ( int i = m_capacity ; i < capacity ; i++ ) m_circQueue[i]=new EventData();
				
				m_capacity = capacity;
				m_tail = ptr;
				m_head = 0;

				#endregion 
			}
		}

        /// <summary>
        /// Dequeues the next callback to the ExecEventReceiver on the specified object for the specified time.
        /// This is called by the Executive, and happens when, and because, the specified time is 'Now'.
        /// </summary>
        /// <param name="exec">The exec.</param>
        /// <param name="userData">The user data provided as a callback from the Executive.</param>
		private void Dequeue(IExecutive exec, object userData) {
            _Debug.Assert(m_head!=m_tail);
            _Debug.Assert(m_circQueue[m_head].When.Equals(exec.Now));

			// Note: We want to leave the event data at the head of the list until it is actually serviced,
			// ...so once we have dequeued the event, we wipe out the head of the queue.
			m_circQueue[m_head].Clear();
			
			m_head++;
			if ( m_head == m_capacity ) m_head = 0;
			
			EventData ed = m_circQueue[m_head];
			if ( m_head!=m_tail && !ed.HasBeenScheduled ) {
				m_exec.RequestEvent(m_dequeueHandler,ed.When,0.0,ed.What);
				ed.HasBeenScheduled = true;
			}

			m_eer(m_exec,userData); // Things that happen in this handler may schedule a new event and/or move m_head...

		}
#endif

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
		public bool IsEmpty { 
			get {
#if USING_CANNED_HEAP
			    // ReSharper disable once ConvertPropertyToExpressionBody
				return m_heap.Count == 0;
#else
                // ReSharper disable once ConvertPropertyToExpressionBody
                return m_head == m_tail; 
#endif
			} 
		}

        /// <summary>
        /// Gets the earliest completion time - this is the time of the next event in this queue.
        /// </summary>
        /// <value>The time of the next event in this queue.</value>
		public DateTime EarliestCompletionTime {
 
			get {
#if USING_CANNED_HEAP
				EventData ed = (EventData)m_heap.Peek();
				if ( ed.When == DateTime.MinValue ) throw new ApplicationException("Attempt to peek into an empty heap.");
				if ( !ed.HasBeenScheduled ) throw new ApplicationException("Heap head has not been scheduled.");
				return ed.When;
#else
				if ( m_tail==m_head ) {
					throw new ApplicationException("Attempting to read the head time of an empty LocalEventQueue in " + ((IHasName)m_eer.Target).Name + ").");
				}
				return m_circQueue[m_head].When;
#endif
			}
		}

        /// <summary>
        /// Gets the latest completion time - this is the time of the last event in this queue.
        /// </summary>
        /// <value>The time of the last event in this queue.</value>
		public DateTime LatestCompletionTime {
			get {
#if USING_CANNED_HEAP
				THIS IS NOT PROVEN OUT.
				EventData ed = (EventData)m_heap.Peek();
				if ( ed.When == DateTime.MinValue ) throw new ApplicationException("Attempt to peek into an empty heap.");
				if ( !ed.HasBeenScheduled ) throw new ApplicationException("Heap head has not been scheduled.");
				return ed.When;
#else
				if ( m_tail==m_head ) {
					throw new ApplicationException("Attempting to read the tail time of an empty LocalEventQueue in " + ((IHasName)m_eer.Target).Name + ").");
				}
				int last = m_tail-1; if ( last < 0 ) last += m_capacity; 
				return m_circQueue[last].When;
#endif
			}
		}

        /// <summary>
        /// Gets the count of events in this queue.
        /// </summary>
        /// <value>The count of events in this queue.</value>
		public int Count {
			get { 
				int retval = m_tail - m_head;
				if ( retval < 0 ) retval += m_capacity;
				return retval;
			}
		}

		/// <summary>
		/// Gets the completion time of the nth event, where zeroth is soonest, 1st is next, and so on.
		/// If code requests an index off the end of the list, an argument exception is thrown.
		/// </summary>
		/// <param name="i">The index, n, of the completion time.</param>
		/// <returns>the completion time of the nth event</returns>
		public DateTime GetCompletionTime(int i){
            _Debug.Assert(i>=0);
#if USING_CANNED_HEAP
				THIS IS NOT PROVEN OUT.
				EventData ed = (EventData)m_heap.Peek();
				if ( ed.When == DateTime.MinValue ) throw new ApplicationException("Attempt to peek into an empty heap.");
				if ( !ed.HasBeenScheduled ) throw new ApplicationException("Heap head has not been scheduled.");
				return ed.When;
#else
			int ndx = m_head;
			ndx += i;
			if ( ndx >= m_capacity ) ndx -= m_capacity;
			return m_circQueue[ndx].When;
#endif
		}

        /// <summary>
        /// The user-friendly name for this object. Typically not required to be unique.
        /// </summary>
        /// <value></value>
		public string Name => "Local Event Queue for " + m_targetName;

	    /// <summary>
        /// A helper class that holds information about a previously requested event.
        /// </summary>
		class EventData : IComparable {
			
            public DateTime When;
			public object What;
			public bool HasBeenScheduled;

            /// <summary>
            /// Creates a new instance of the <see cref="T:EventData"/> class.
            /// </summary>
			public EventData(){
				Clear();
			}

            /// <summary>
            /// Clears data in this instance so it can be reused.
            /// </summary>
			public void Clear() {
				When = DateTime.MinValue;
				What = null;
				HasBeenScheduled = false;
			}

            /// <summary>
            /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
            /// </returns>
			public override string ToString() {
				return When + " : " + (What==null?"<null>":((What is IHasName?((IHasName)What).Name:What.ToString())));
			}

			#region IComparable Members

            /// <summary>
            /// Compares the current instance with another object of the same type.
            /// </summary>
            /// <param name="obj">An object to compare with this instance.</param>
            /// <returns>
            /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than obj. Zero This instance is equal to obj. Greater than zero This instance is greater than obj.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">obj is not the same type as this instance. </exception>
			public int CompareTo(object obj) {
				DateTime dt = ((EventData)obj).When;
				if ( When.Equals(dt) ) dt = dt + TimeSpan.FromTicks(1);
				return System.Collections.Comparer.Default.Compare(When.Ticks,dt.Ticks);
			}

			#endregion
		}

		#region (Commented Out) Event Data Cache

		//		private class EventDataCache {
		//			private int m_head = 0;
		//			private int m_tail = 0;
		//			private int m_numEventDatas;
		//			private EventData[] m_eventDataCache;
		//
		//			public EventDataCache():this(INITIAL_SIZE){}
		//			public EventDataCache(int initialEventCacheSize){
		//				m_numEventDatas = initialEventCacheSize;
		//				m_eventDataCache = new EventData[m_numEventDatas];
		//				for ( int i = 0 ; i < initialEventCacheSize ; i++ ) {
		//					m_eventDataCache[i] = new EventData();
		//				}
		//			}
		//
		//			public EventData Take(object what, DateTime when){
		//				if ( m_head == m_tail ) {
		//					// Queue is empty!
		//					m_tail = m_numEventDatas;
		//					m_head = 0;
		//					m_numEventDatas*=2;
		//					m_eventDataCache = new _ExecEvent[m_numEventDatas];
		//					for ( int i = m_head ; i < m_tail ; i++ ) {
		//						m_eventDataCache[i] = new EventData();
		//					}
		//				}
		//				EventData retval = m_eventDataCache[m_head++];
		//				if ( m_head == m_numEventDatas ) m_head = 0;
		//
		//				retval.What = what;
		//				retval.m_when = when;
		//
		//				return retval;
		//			}
		//
		//			public void Return(EventData eventData){
		//				m_eventDataCache[m_tail++] = eventData;
		//				if ( m_tail == m_numEventDatas ) m_tail = 0;
		//			}
		//		}

		#endregion 

	}
}
