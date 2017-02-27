/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
#if COMPACT_FRAMEWORK
using SortedList = Highpoint.Sage.CF.SortedList;
#endif

namespace Highpoint.Sage.SimCore {

	/// <summary>
	/// Interface implemented by all synchronizers. A Synchronizer is an object
	/// that is capable of making sure that things that could otherwise take place
	/// at differing times, occur at the same time.
	/// </summary>
	public interface ISynchronizer {
		/// <summary>
		/// Acquires a Synchronization Channel. A synch channel is used once by one
		/// object that wishes to be synchronized. Once all channels that have been
		/// acquired from a synchronizer have had their 'Synchronize' methods called,
		/// all channels' users are allowed to proceed.
		/// </summary>
		/// <param name="sequence">A sequence indicator that determines which synch
		/// channels' owners are instructed to proceed first. Lesser IComparables go
		/// first.</param>
		/// <returns>A Synch Channel with the assigned priority.</returns>
		ISynchChannel GetSynchChannel(IComparable sequence);
	}

	/// <summary>
	/// Implemented by a synch channel. A synch channel is obtained from a Synchronizer, and
    /// is a one-use, one-client mechanism for gating execution. Once all clients that have 
    /// acquired synch channels have called 'Synchronize' on those channels, they are all allowed
    /// to proceed, in the order implied by the Sequencer IComparable.
	/// </summary>
	public interface ISynchChannel {
		/// <summary>
		/// Called by a synch channel to indicate that it is ready to proceed. It will be
		/// allowed to do so once all clients have called this method.
		/// </summary>
		void Synchronize();

        /// <summary>
        /// Gets a sequencer that can be used in a Sort operation to determine the order in which the 
        /// clients are allowed to proceed.
        /// </summary>
        /// <value>The sequencer.</value>
		IComparable Sequencer{ get; }
	}

	/// <summary>
	/// Synchronizes a series of threads that are all running on DetachableEvent handlers.
	/// The clients will call Synchronize on DetachableEvent threads, and the threads will
	/// pause until all open channels contain paused threads. Then, in the specified sequence,
	/// all clients will be resumed.
	/// </summary>
	public class DetachableEventSynchronizer {
		private IModel m_model;
		private IExecutive m_exec;
		private ArrayList m_synchChannels;
		private SortedList m_waiters;

		/// <summary>
		/// Creates a DetachableEventSynchronizer and attaches it to the specified executive.
		/// </summary>
		/// <param name="model">The model that owns the executive that will be managing all of the Detachable Events
		/// that are to be synchronized.</param>
		public DetachableEventSynchronizer(IModel model){
			m_model = model;
			m_exec = m_model.Executive;
			m_synchChannels = new ArrayList();
			m_waiters = new SortedList();
		}

		/// <summary>
		/// Acquires a Synchronization Channel. A synch channel is used once by one
		/// object that wishes to be synchronized. Once all channels that have been
		/// acquired from a synchronizer have had their 'Synchronize' methods called,
		/// all channels' users are allowed to proceed. Note that the constructor need
		/// not be called from a DetachableEvent thread - but Synchronize(...) will
		/// need to be.
		/// </summary>
		/// <param name="sequencer">A sequence indicator that determines which synch
		/// channels' owners are instructed to proceed first.</param>
		/// <returns>A Synch Channel with the assigned priority.</returns>
		public ISynchChannel GetSynchChannel(IComparable sequencer){
			ISynchChannel synchChannel = new SynchChannel(this,sequencer);
			m_synchChannels.Add(synchChannel);
			return synchChannel;
		}

		private void LogSynchronization(object sortKey, IDetachableEventController idec, SynchChannel sc){
			if ( m_waiters.ContainsValue(idec) ) {
				throw new ApplicationException("Synchronize(...) called on a SynchChannel that is already waiting.");
			}
			if ( !m_synchChannels.Contains(sc) ) {
				throw new ApplicationException("SynchChannel applied to a synchronizer that did not own it - serious error.");
			}

			if ( (m_waiters.Count + 1) == m_synchChannels.Count ) {
				m_exec.RequestEvent(new ExecEventReceiver(LaunchAll),m_exec.Now,m_exec.CurrentPriorityLevel,null);
			}
			m_waiters.Add(sortKey,idec);
            idec.SetAbortHandler(new DetachableEventAbortHandler(idec_AbortionEvent));
			idec.Suspend();
            idec.ClearAbortHandler();
		}

		private void LaunchAll(IExecutive exec, object userData){
			foreach ( IDetachableEventController idec in m_waiters.Values ) {
				idec.Resume();
			}
		}

        /// A synch channel is obtained from a Synchronizer, and
        /// is a one-use, one-client mechanism for gating execution. Once all clients that have 
        /// acquired synch channels have called 'Synchronize' on those channels, they are all allowed
        /// to proceed, in the order implied by the Sequencer IComparable.
        internal class SynchChannel : ISynchChannel
        {

            #region Private Fields

            private DetachableEventSynchronizer m_ds;
            private IComparable m_sortKey;

            #endregion 

            /// <summary>
            /// Creates a new instance of the <see cref="T:SynchChannel"/> class.
            /// </summary>
            /// <param name="ds">The <see cref="Highpoint.Sage.SimCore.DetachableEventSynchronizer"/>.</param>
            /// <param name="sortKey">The sort key.</param>
			public SynchChannel(DetachableEventSynchronizer ds, IComparable sortKey){
				m_ds = ds;
				m_sortKey = sortKey;
			}

            /// <summary>
            /// Gets a sequencer that can be used in a Sort operation to determine the order in which the
            /// clients of a Synchronizer are allowed to proceed.
            /// </summary>
            /// <value>The sequencer.</value>
			public IComparable Sequencer { get { return m_sortKey; } }

			#region ISynchronizer Members
            /// <summary>
            /// Called by a synch channel to indicate that it is ready to proceed. It will be
            /// allowed to do so once all clients have called this method.
            /// </summary>
			public void Synchronize() {
				IDetachableEventController idec = m_ds.m_exec.CurrentEventController;
				m_ds.LogSynchronization(m_sortKey,idec,this);
			}
			#endregion
		}

        private void idec_AbortionEvent(IExecutive exec, IDetachableEventController idec, params object[] args) {
			string narrative = "A synchronizer failed to complete. It had " + m_synchChannels.Count 
				+ " synch channels - following is the stack trace:\r\n" + new StackTrace(true);
			m_model.AddWarning(new GenericModelWarning("Synchronizer Aborted",narrative,this,null));
		}
	}
}
