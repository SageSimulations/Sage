/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

#pragma warning disable 1587
/// <summary>
/// The Mementos namespace contains an implementation of a Memento pattern - that is, an object that implements ISupportsMememtos
/// is capable of generating and maintaining mementos, which are representations of internal state at a given time that can be
/// used to restore the object's internal state to a previous set of values. MementoHelper exists to simplify implementation of the
/// ISupportsMememtos interface.
/// </summary>
#pragma warning restore 1587
namespace Highpoint.Sage.Utility.Mementos {

	/// <summary>
	/// Implemented by a method that will listen for changes in the form or
	/// contents of a memento.
	/// </summary>
    public delegate void MementoChangeEvent(ISupportsMementos rootChange);

    /// <summary>
    /// Implemented by an object that supports Mementos.
    /// </summary>
	public interface ISupportsMementos {

		/// <summary>
		/// Retrieves a memento from the object.
		/// </summary>
        IMemento Memento { get; set; }

		/// <summary>
		/// Reports whether the object has changed relative to its memento
		/// since the last memento was recorded.
		/// </summary>
        bool HasChanged { get; }

		/// <summary>
		/// Fired when the memento contents will have changed. This does not
		/// imply that the memento <i>has</i> changed, since the memento is
		/// recorded, typically, only on request. It <i>does</i> imply that if
		/// you ask for a memento, it will be in some way different from any
		/// memento you might have previously acquired.
		/// </summary>
        event MementoChangeEvent MementoChangeEvent;

		/// <summary>
		/// Indicates whether this object can report memento changes to its
		/// parent. (Mementos can contain other mementos.) 
		/// </summary>
        bool ReportsOwnChanges { get; }

		/// <summary>
		/// Returns true if the two mementos are semantically equal.
		/// </summary>
		/// <param name="otherGuy">The other memento implementer.</param>
		/// <returns>True if the two mementos are semantically equal.</returns>
        bool Equals(ISupportsMementos otherGuy);
    }

    public delegate void MementoEvent(IMemento memento);

	/// <summary>
	/// Implemented by any object that can act as a memento.
	/// </summary>
    public interface IMemento {

		/// <summary>
		/// Creates an empty copy of whatever object this memento can reconstitute. Some
		/// mementos are only able to reconstitute into their source objects (they can only
		/// be used to restore state in the same object), and these mementos will return a
		/// reference to that object.)
		/// </summary>
        ISupportsMementos CreateTarget();

		/// <summary>
		/// Loads the contents of this Memento into the provided object.
		/// </summary>
		/// <param name="ism">The object to receive the contents of the memento.</param>
        void Load(ISupportsMementos ism);

		/// <summary>
		/// Emits an IDictionary form of the memento that can be, for example, dumped to
		/// Trace.
		/// </summary>
		/// <returns>An IDictionary form of the memento.</returns>
        IDictionary GetDictionary();

		/// <summary>
		/// Returns true if the two mementos are semantically equal.
		/// </summary>
		/// <param name="otheOneMemento">The memento this one should compare itself to.</param>
		/// <returns>True if the mementos are semantically equal.</returns>
		bool Equals(IMemento otheOneMemento);

        /// <summary>
        /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
        /// </summary>
        event MementoEvent OnLoadCompleted;

        /// <summary>
        /// This holds a reference to the memento, if any, that contains this memento.
        /// </summary>
        IMemento Parent { get; set; }
    }

    /// <summary>
	/// A class that will perform much of the bookkeeping required to implement
	/// the ISupportsMementos interface, including child management, change tracking
	/// and memento generation.
	/// </summary>
	public class MementoHelper {

        #region private fields
        private readonly ISupportsMementos m_iss;
        private readonly MementoChangeEvent m_childChangeHandler;
        private readonly bool m_wrappeeReportsOwnChanges;
        private ArrayList m_children;        // children who report their own changes.
        private ArrayList m_problemChildren; // children who can't report their own changes.
        private bool m_hasChanged = true;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MementoHelper"/> class.
        /// </summary>
        /// <param name="iss">The memento supporter that wraps this helper.</param>
        /// <param name="wrappeeReportsOwnChanges">if set to <c>true</c> the memento supporter is able to report its own changes.</param>
        public MementoHelper(ISupportsMementos iss, bool wrappeeReportsOwnChanges){
            m_wrappeeReportsOwnChanges = wrappeeReportsOwnChanges;
            m_iss = iss;
            m_childChangeHandler = ReportChange;
        }

        /// <summary>
        /// Clears the state of this helper.
        /// </summary>
        public void Clear(){

			if ( m_children != null ) {
				foreach ( ISupportsMementos child in m_children ) child.MementoChangeEvent -= m_childChangeHandler; 
				m_children.Clear();
			}
            m_problemChildren?.Clear();


            m_hasChanged = true; // Forces the parent to regather a memento.
        }

        /// <summary>
        /// Informs the helper that the memento supporter that wraps this helper has gained a child.
        /// </summary>
        /// <param name="child">The child.</param>
        public void AddChild(ISupportsMementos child){
            if ( child.ReportsOwnChanges ) {
                if ( m_children == null ) m_children = new ArrayList();
				if ( !m_children.Contains(child) ) {
					m_children.Add(child);
					child.MementoChangeEvent+=m_childChangeHandler;
				}
            } else {
                if ( m_problemChildren == null ) m_problemChildren = new ArrayList();
				if ( !m_problemChildren.Contains(child) ) {
					m_problemChildren.Add(child);
				}
            }
            ReportChange();
        }

        /// <summary>
        /// Informs the helper that the memento supporter that wraps this helper has lost a child.
        /// </summary>
        /// <param name="child">The child.</param>
        public void RemoveChild(ISupportsMementos child){
            if ( m_children.Contains(child) ) child.MementoChangeEvent-=m_childChangeHandler;
            m_children?.Remove(child);
            m_problemChildren?.Remove(child);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the memento supporter that wraps this helper has changed.
        /// </summary>
        /// <value><c>true</c> if the memento supporter that wraps this helper has changed; otherwise, <c>false</c>.</value>
        public bool HasChanged { 
            get { 
                if ( m_problemChildren != null ) {
                    foreach ( ISupportsMementos child in m_problemChildren ) {
                        m_hasChanged |= child.HasChanged;
                    }
                }
                return m_hasChanged;
            }
            set { m_hasChanged = value; }
        }

        /// <summary>
        /// Called by the memento supporter that wraps this helper, to let it know that a change has occurred in its internal state.
        /// </summary>
        /// <param name="iss">The memento supporter which has changed.</param>
        private void ReportChange(ISupportsMementos iss){
            m_hasChanged = true;
            MementoChangeEvent?.Invoke(iss);
        }

        /// <summary>
        /// Called by the memento supporter that wraps this helper, to let it know that a change has occurred in its internal state.
        /// </summary>
        public void ReportChange(){
            m_hasChanged = true;
            MementoChangeEvent?.Invoke(m_iss);
        }

        /// <summary>
        /// Occurs when the memento supporter that wraps this helper has reported a change in its internal state.
        /// </summary>
        public event MementoChangeEvent MementoChangeEvent;

        /// <summary>
        /// Called by the memento supporter that wraps this helper, to let it know that a snapsot (a memento) has just been generated.
        /// </summary>
        public void ReportSnapshot(){
            m_hasChanged = false;
        }

        /// <summary>
        /// Gets a value indicating whether the memento supporter that wraps this helper reports its own changes.
        /// </summary>
        /// <value><c>true</c> if [reports own changes]; otherwise, <c>false</c>.</value>
        public bool ReportsOwnChanges => ( m_wrappeeReportsOwnChanges && m_problemChildren == null );
	}
}