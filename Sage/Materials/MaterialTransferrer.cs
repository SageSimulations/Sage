/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Materials.Chemistry;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Materials {

    /// <summary>
    /// An implementer makes on-request updates to one or more materials.
    /// </summary>
    public interface IUpdater {
        /// <summary>
        /// Performs the update operation that this implementer performs.
        /// </summary>
        /// <param name="initiator">The material to be updated (along with any dependent materials.)</param>
        void DoUpdate(IMaterial initiator);
        /// <summary>
        /// Causes this updater no longer to perform alterations on the targeted mixture. This may not be implemented in some cases.
        /// </summary>
        /// <param name="detachee">The detachee.</param>
        void Detach(IMaterial detachee);
    }

    /// <summary>
    /// A dummy that makes no updates to any materials.
    /// </summary>
    internal class NullUpdater : IUpdater {
        /// <summary>
        /// Performs the update operation that this implementer performs.
        /// </summary>
        /// <param name="initiator">The material to be updated (along with any dependent materials.)</param>
        public void DoUpdate(IMaterial initiator) { }
        /// <summary>
        /// Causes this updater no longer to perform alterations on the targeted mixture. This may not be implemented in some cases.
        /// </summary>
        /// <param name="detachee">The detachee.</param>
        public void Detach(IMaterial detachee) { }
    }

    /// <summary>
    /// Performs transferral of material from one mixture to another.
    /// </summary>
    public class MaterialTransferrer : IUpdater {

        public class TypeSpec {

            #region Private Fields
            private MaterialType m_mt;
            private double m_mass;
            #endregion Private Fields

            public TypeSpec(MaterialType mtype, double mass) {
                m_mass = mass;
                m_mt = mtype;
            }

            public MaterialType MaterialType { get { return m_mt; } }
            public double Mass { get { return m_mass; } }

            public static List<TypeSpec> FromMixture(Mixture exemplar) {
                List<TypeSpec> typeSpecs = new List<TypeSpec>();
                foreach (Substance s in exemplar.Constituents) {
                    typeSpecs.Add(new TypeSpec(s.MaterialType, s.Mass));
                }
                return typeSpecs;
            }
        }

        #region Private Fields
        private Mixture m_from;
        private Mixture m_to;
        private List<TypeSpec> m_what;
        private TimeSpan m_duration;
        private IModel m_model;
        private long m_completionKey = long.MinValue;
        private long m_startTicks = long.MinValue;
        private long m_endTicks = long.MinValue;
        private long m_lastUpdateTicks = long.MinValue;
        private double m_lastFraction = 0.0;
        private bool m_inProcess = false;
        private List<IDetachableEventController> m_startWaiters;
        private List<IDetachableEventController> m_endWaiters;
        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransferrer"/> class.
        /// </summary>
        /// <param name="model">The model in which the update is to be run.</param>
        /// <param name="from">The source mixture.</param>
        /// <param name="to">The destination mixture.</param>
        /// <param name="what">The exemplar representing what is to be transferred.</param>
        /// <param name="duration">The transfer duration.</param>
        public MaterialTransferrer(IModel model, ref Mixture from, ref Mixture to, Mixture what, TimeSpan duration)
            : this(model, ref from, ref to, TypeSpec.FromMixture(what), duration) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransferrer"/> class.
        /// </summary>
        /// <param name="model">The model in which the update is to be run.</param>
        /// <param name="from">The source mixture.</param>
        /// <param name="to">The destination mixture.</param>
        /// <param name="typespecs">The list of typespecs representing what is to be transferred.</param>
        /// <param name="duration">The transfer duration.</param>
        public MaterialTransferrer(IModel model, ref Mixture from, ref Mixture to, List<TypeSpec> typespecs, TimeSpan duration) {
            m_startWaiters = new List<IDetachableEventController>();
            m_endWaiters = new List<IDetachableEventController>();
            m_model = model;
            m_from = from;
            m_to = to;
            m_what = typespecs;
        }

        /// <summary>
        /// Starts the transfer that this MaterialTransferrer represents.
        /// </summary>
        public void Start() {

            if (m_completionKey != long.MinValue) {
                throw new ApplicationException("An already used MaterialTransferrer was asked to start a second time. This is an error.");
            }

            m_startWaiters.ForEach(delegate(IDetachableEventController waiter) { waiter.Resume(); });
            m_startWaiters.Clear();
            m_from.Updater = this;
            m_to.Updater = this;
            m_startTicks = m_model.Executive.Now.Ticks;
            m_lastUpdateTicks = m_startTicks;
            DateTime end = m_model.Executive.Now + m_duration;
            m_endTicks = end.Ticks;
            m_completionKey = m_model.Executive.RequestEvent(new ExecEventReceiver(_Update), end, 0.0, null);

        }

        /// <summary>
        /// Blocks the caller's detachable event thread until this transfer has started.
        /// </summary>
        public void BlockTilStart() {
            _Debug.Assert(m_model.Executive.CurrentEventType == ExecEventType.Detachable);
            if (m_completionKey == long.MinValue/*i.e. it has not started*/) {
                IDetachableEventController waiter = m_model.Executive.CurrentEventController;
                m_startWaiters.Add(waiter);
                waiter.Suspend();
            }
        }

        /// <summary>
        /// Blocks the caller's detachable event thread until this transfer has finished.
        /// </summary>
        public void BlockTilDone() {
            _Debug.Assert(m_model.Executive.CurrentEventType == ExecEventType.Detachable);
            if (m_completionKey > m_model.Executive.Now.Ticks) {
                m_endWaiters.Add(m_model.Executive.CurrentEventController);
                m_model.Executive.CurrentEventController.Suspend();
            }
        }

        private void _Update(IExecutive exec, object userData) {
            if (!m_inProcess) {
                m_inProcess = true;
                double thisFraction = ((double)( m_model.Executive.Now.Ticks - m_startTicks )) / ((double)m_duration.Ticks);
                double transferFraction = thisFraction - m_lastFraction;
                if (transferFraction > 0) {
                    foreach (TypeSpec ts in m_what) {
                        if (ts.Mass > 0) {
                            IMaterial extract = m_from.RemoveMaterial(ts.MaterialType, ( ts.Mass * transferFraction ));
                            m_to.AddMaterial(extract);
                        }
                    }
                }

                if (m_model.Executive.Now.Ticks >= m_endTicks) {
                    m_endWaiters.ForEach(delegate(IDetachableEventController waiter) { waiter.Resume(); });
                    m_endWaiters.Clear();
                }

                m_lastFraction = thisFraction;
                m_inProcess = false;
            }
        }

        private void ReleaseWaiters(List<IDetachableEventController> waiters) {
            waiters.ForEach(delegate(IDetachableEventController waiter) { waiter.Resume(); });
            waiters.Clear();
        }

        #region IUpdater Members

        /// <summary>
        /// Performs the update operation that this implementer performs.
        /// </summary>
        /// <param name="initiator">The initiator.</param>
        public void DoUpdate(IMaterial initiator) {
            _Update(null, null);
        }

        /// <summary>
        /// Causes this updater no longer to perform alterations on the targeted mixture. This is not implemented in this class, and will throw an exception.
        /// </summary>
        /// <param name="detachee">The detachee.</param>
        public void Detach(IMaterial detachee) {
            throw new NotImplementedException();
        }

        #endregion
    }
}