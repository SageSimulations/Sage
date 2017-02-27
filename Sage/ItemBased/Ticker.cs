/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.ItemBased {

    /// <summary>
    /// A class that generates a <see cref="Highpoint.Sage.ItemBased.PulseEvent"/> at a specified periodicity.
    /// </summary>
    public class Ticker : IPulseSource
    {

        #region Private Fields

        private IPeriodicity m_periodicity;
        private IModel m_model;
        private bool m_running = false;
        private bool m_autoStart;
        private long m_nPulses;
        private long m_nPulsesRemaining;
        private ExecEventReceiver m_execEventReceiver;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:Ticker"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="periodicity">The periodicity of the ticker.</param>
        /// <param name="autoStart">if set to <c>true</c> the ticker will start automatically, immediately on model start, and cycle indefinitely.</param>
		public Ticker(IModel model, IPeriodicity periodicity, bool autoStart):this(model,periodicity,autoStart,long.MaxValue){}

        /// <summary>
        /// Creates a new instance of the <see cref="T:Ticker"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="periodicity">The periodicity of the ticker.</param>
        /// <param name="autoStart">if set to <c>true</c> the ticker will start automatically, immediately on model start, and cycle indefinitely.</param>
        /// <param name="nPulses">The number of pulses to be served.</param>
		public Ticker(IModel model, IPeriodicity periodicity, bool autoStart, long nPulses){
			m_periodicity = periodicity;
			m_model = model;
			m_autoStart = autoStart;
			m_nPulses = nPulses;
			m_execEventReceiver = new ExecEventReceiver(OnExecEvent);
			if ( autoStart ) {
				m_model.Starting += new ModelEvent(OnModelStarting);
			}
			m_model.Stopping += new ModelEvent(OnModelStopping);
		}
		
        private void OnModelStarting(IModel model){
			if ( m_autoStart ) Start();
		}
		private void OnModelStopping(IModel model){
			Stop();
		}
        /// <summary>
        /// Starts this instance.
        /// </summary>
		public void Start(){
			m_nPulsesRemaining = m_nPulses;
			m_running = true;
			ScheduleNextEvent();
		}
        /// <summary>
        /// Stops this instance.
        /// </summary>
		public void Stop(){
			m_running = false;
		}                                   // TODO: Need an IDateTimeDistribution & an ITimeSpanDistribution.
		
        private void ScheduleNextEvent(){
			if ( !m_running ) return;
			if ( m_nPulsesRemaining == 0 ) return;
			TimeSpan waitDuration = m_periodicity.GetNext();
			m_model.Executive.RequestEvent(m_execEventReceiver,m_model.Executive.Now+waitDuration,0.0,null);
			m_nPulsesRemaining--;
		}
		private void OnExecEvent(IExecutive exec, object userData){
			//Console.WriteLine(exec.Now + " : firing ticker.");
			if ( PulseEvent != null ) PulseEvent();
			if ( m_running ) ScheduleNextEvent();
		}

        /// <summary>
        /// Fired when this Ticker pulses.
        /// </summary>
 		public event PulseEvent PulseEvent;
	}    
}