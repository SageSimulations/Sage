/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.ItemBased {

    /// <summary>
    /// An event that a pulse source fires. Anyone wanting to receive a 'Do It!' command implements this delegate.
    /// </summary>
    public delegate void PulseEvent();

    /// <summary>
    /// Implemented by an object that generates pulses, either periodic or random.
    /// </summary>
    public interface IPulseSource {
        /// <summary>
        /// Fired when a PulseSource delivers its 'Do It!' command.
        /// </summary>
        event PulseEvent PulseEvent;
    }

    public class PulseSource : IPulseSource, IDisposable {
        IModel m_model = null;
        IPeriodicity m_periodicity;
        bool m_initialPulse;
        ExecEventReceiver m_doPulse = null;
        public PulseSource(IModel model, IPeriodicity periodicity, bool initialPulse) {
            m_model = model;
            m_periodicity = periodicity;
            m_initialPulse = initialPulse;
            m_doPulse = new ExecEventReceiver(DoPulse);
            if (m_model.Executive.State.Equals(ExecState.Stopped) || m_model.Executive.State.Equals(ExecState.Finished) ) {
                m_model.Executive.ExecutiveStarted += new ExecutiveEvent(StartPulsing);
            } else {
                StartPulsing(model.Executive);
            }
        }

        private void StartPulsing(IExecutive exec) {
            if (m_initialPulse) { DoPulse(exec, null); } else { DoPause(exec,null); }
        }

        private void DoPause(IExecutive exec, object userData ) {
            DateTime nextPulse = exec.Now + TimeSpanOperations.Max(TimeSpan.Zero, m_periodicity.GetNext());
            exec.RequestDaemonEvent(m_doPulse, nextPulse, 0.0, null);
        }

        private void DoPulse(IExecutive exec, object userData) {
            if (PulseEvent != null) PulseEvent();
            DoPause(exec, null);
        }

        public event PulseEvent  PulseEvent;

        public IPeriodicity Periodicity { get { return m_periodicity; } set { m_periodicity = value; } }

        public void Dispose() {
            m_model.Executive.ExecutiveStarted -= new ExecutiveEvent(StartPulsing);
            m_model.Executive.UnRequestEvents(this);
        }
    }
}