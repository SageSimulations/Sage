/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SimCore;
using Trace = System.Diagnostics.Debug;

namespace Highpoint.Sage.ItemBased.Queues.DataCollectors
{
	/// <summary>
	/// Summary description for WaitingTime.
	/// </summary>
	public class WaitingTime : IModelObject
	{
		private IQueue m_hostQueue;
		private ArrayList m_data;
		private Hashtable m_occupants;
		private int m_nBins;
		private Histogram1D_TimeSpan m_hist = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitingTime"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="hostQueue">The host queue.</param>
        /// <param name="nBins">The number of bins into which to divide the waiting time.</param>
		public WaitingTime(IModel model, string name, Guid guid, IQueue hostQueue,int nBins)
		{
            InitializeIdentity(model, name, null, guid);
            
            m_nBins = nBins;
			m_hostQueue = hostQueue;
			m_hostQueue.ObjectEnqueued+=new QueueOccupancyEvent(m_hostQueue_ObjectEnqueued);
			m_hostQueue.ObjectDequeued+=new QueueOccupancyEvent(m_hostQueue_ObjectDequeued);
			m_data = new ArrayList();
			m_occupants = new Hashtable();
            
            IMOHelper.RegisterWithModel(this);
		}

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model this component runs in.</param>
        /// <param name="name">The name of this component.</param>
        /// <param name="description">The description for this component.</param>
        /// <param name="guid">The GUID of this component.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

		public void Reset(){
			m_data.Clear();
			m_hist = null;
		}

		public Histogram1D_TimeSpan Histogram {
			get {
				if ( m_data.Count == 0 ) return new Histogram1D_TimeSpan(TimeSpan.MinValue,TimeSpan.MaxValue,1);
				TimeSpan[] rawdata = new TimeSpan[m_data.Count];
				TimeSpan min = TimeSpan.MaxValue;
				TimeSpan max = TimeSpan.MinValue;
				int ndx = 0;
				foreach ( TimeSpan data in m_data ) {
					rawdata[ndx++] = data;
					if ( data < min ) min = data;
					if ( data > max ) max = data;
				}
				if ( min == max ) max += TimeSpan.FromMinutes(m_nBins);
				m_hist = new Histogram1D_TimeSpan(rawdata,min,max,m_nBins,m_name);
				m_hist.Recalculate();
				return m_hist;
			}
		}

		private void m_hostQueue_ObjectEnqueued(IQueue hostQueue, object serviceItem) {
			m_occupants.Add(serviceItem,m_model.Executive.Now);
			//Trace.WriteLine(m_model.Executive.Now + " : " + this.Name + " enqueueing " + serviceItem + ". It currently has " + hostQueue.Count + " occupants.");
		}

		private void m_hostQueue_ObjectDequeued(IQueue hostQueue, object serviceItem) {
			DateTime entry = (DateTime)m_occupants[serviceItem];
			m_occupants.Remove(serviceItem);
			TimeSpan duration = m_model.Executive.Now-entry;
			//Trace.WriteLine(m_model.Executive.Now + " : " + this.Name + " dequeueing " + serviceItem + " after " + duration + ". It currently has " + hostQueue.Count + " occupants.");
			m_data.Add(duration);
			m_hist = null;
		}

		#region Implementation of IModelObject
        private string m_name = null;
        public string Name { get { return m_name; } }
		private string m_description = null;
		/// <summary>
		/// A description of this WaitingTime Histogram.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}
		private Guid m_guid = Guid.Empty;
        public Guid Guid => m_guid;
        private IModel m_model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => m_model;
		#endregion

	}
}
