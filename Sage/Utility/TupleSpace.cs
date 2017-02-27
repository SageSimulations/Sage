/* This source code licensed under the GNU Affero General Public License */
#if INCLUDE_WIP
//#define EXEC_DEPENDENT
// TODO: 1.) Create exec-independent variant of TupleSpace...
// TODO: 2.) Add wildcarding in read & take APIs.
// TODO: 3.) Add n-tuple support.

// ReSharper disable InconsistentlySynchronizedField

namespace Highpoint.Sage.Utility {

#region <<< (Commented-out) multi-entry exchange - can handle multiple entries w/ same key. >>>
#if NOT_DEFINED
	/// <summary>
	/// Summary description for TupleSpace.
	/// </summary>
	public class TupleSpace : Exc {
#region >>> Private Fields <<<
		private IExecutive m_exec;
		private HashtableOfLists m_ts;
		private HashtableOfLists m_waitersToRead;
		private HashtableOfLists m_waitersToTake;
#endregion
		public TupleSpace(IExecutive exec) {
			m_exec = exec;
			// TODO: Add a GracefulAbort(...) to IDetachableEventController. 
			// exec.ExecutiveFinished +=new ExecutiveEvent(exec_ExecutiveFinished);
			m_ts = new HashtableOfLists();
			m_waitersToRead = new HashtableOfLists();
			m_waitersToTake = new HashtableOfLists();
		}
#region ITupleSpace Members

		public bool PermitsDuplicateKeys { get { return true; } }

		public void Post(ITuple tuple) {
			m_ts.Add(tuple.Key,tuple);
			tuple.OnPosted(this);
			if ( TuplePosted != null ) TuplePosted(this,tuple);
			IList wtr = m_waitersToRead[tuple.Key];
			IList wtt = m_waitersToTake[tuple.Key];
			m_waitersToRead.Clear(tuple.Key);
			m_waitersToTake.Clear(tuple.Key);
			foreach ( IDetachableEventController idec in wtr ) idec.Resume(double.MaxValue);
			foreach ( IDetachableEventController idec in wtt ) idec.Resume(double.MaxValue-double.Epsilon);
		}

		public void Post(object key, object data) {
			Post(new TupleWrapper(key,data));
		}

		public ITuple Read(object key, bool blocking) {
			ITuple retval = (blocking?BlockingRead(key):NonBlockingRead(key));
			return retval;
		}

		public ITuple Take(object key, bool blocking) {
			ITuple retval = (blocking?BlockingTake(key):NonBlockingTake(key));
			return retval;
		}

		public event TupleEvent TuplePosted;

		public event TupleEvent TupleRead;

		public event TupleEvent TupleTaken;

#endregion

		private ITuple BlockingRead(object key){
			lock ( m_ts ) {
				while ( true ){
					ITuple tuple = NonBlockingRead(key);
					if ( tuple != null ) return tuple;
					
#region Wait 'til next post against this key.
					IDetachableEventController idec = m_exec.CurrentEventController;
					m_waitersToRead.Add(key,idec);
					idec.Suspend();
#endregion
				}
			}
		}
		private ITuple NonBlockingRead(object key){
			IList list = m_ts[key];
			if ( list.Count > 0 ) {
				ITuple tuple = (ITuple)list[0];
				tuple.OnRead(this);
				if ( TupleRead != null ) TupleRead(this,tuple);
				return tuple;
			}
			else return null;
		}
		private ITuple BlockingTake(object key){
			lock ( m_ts ) {
				while ( true ){
					ITuple tuple = NonBlockingTake(key);
					if ( tuple != null ) return tuple;

#region Wait 'til next post against this key.
					IDetachableEventController idec = m_exec.CurrentEventController;
					m_waitersToTake.Add(key,idec);
					idec.Suspend();
#endregion
				}
			}
		}
		private ITuple NonBlockingTake(object key){
			IList list = m_ts[key];
			if ( list.Count > 0 ) {
				ITuple tuple = (ITuple)list[0];
				list.RemoveAt(0);
				tuple.OnTaken(this);
				if ( TupleTaken != null ) TupleTaken(this,tuple);
				return tuple;
			}
			else return null;
		}

		
		private class TupleWrapper : ITuple {
			private object m_key;
			private object m_data;
			public TupleWrapper(object key, object data){
				m_key = key;
				m_data = data;
			}

#region ITuple Members

			public object Key {
				get {
					return m_key;
				}
			}

			public object Data {
				get {
					return m_data;
				}
			}

			public void OnPosted(ITupleSpace ts) {}

			public void OnRead(ITupleSpace ts) {}

			public void OnTaken(ITupleSpace ts) {}

#endregion
		}

		private void exec_ExecutiveFinished(IExecutive exec) {
			//			foreach ( IDetachableEventController idec in m_waitersToRead ) idec.GracefulAbort();
			//			foreach ( IDetachableEventController idec in m_waitersToTake ) idec.GracefulAbort();
			//			m_waitersToRead.Clear();
			//			m_waitersToTake.Clear();
		}
	}
#endif
#endregion
}
#endif