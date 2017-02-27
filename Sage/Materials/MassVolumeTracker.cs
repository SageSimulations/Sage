/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Materials {

    /// <summary>
    /// Used to determine Mass and Volume high and low values during a change in mixture.
    /// We model a mixture into which charges and discharges are performed with a high-level limit
    /// equal to the vessel's capacity. The charges and discharges are done in equal proportions during
    /// a number of cycles necessary to charge all of the inflows and discharge all of the outflows. With
    /// each charge and discharge, we log the high and low levels for Mass and Volume.
    /// Reactions may take place as a result of the charges.
    /// All mixtures passed in are cloned within this tracker before manipulation.
    /// </summary>
    public class MassVolumeTracker {

        #region Private Fields
        private double m_capacity;
        private DoubleTracker m_massHistory;
        private DoubleTracker m_volumeHistory;
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("MVTracker");
        private Mixture m_initial, m_inflow, m_outflow;
        private ReactionProcessor m_reactionProcessor;
        private bool m_inflowFirst;
        private bool m_cyclical;
        #endregion Private Fields

        /// <summary>
        /// Creates a MVTracker without initial mixture, transfers or capacity specified. The way this
        /// is used is to turn off level tracking in a mixture/vessel, then add &amp; remove what is specified (add, then remove),
        /// then log the tracker's high and low marks, and finally turn level tracking back on.
        /// Note - this is done inside the SOMTask's DoOperations(...) method, and takes the form of<p></p>
        /// <code>this.GetSOD(graphContext).SetChangeLogging(true);</code>
        /// </summary>
        /// <param name="rp">The ReactionProcessor that knows of any reactions that will take place. Can be null.</param>
        public MassVolumeTracker(ReactionProcessor rp) : this(null, null, null, double.NaN, rp) { }

        /// <summary>
        /// Creates a MVTracker without initial mixture, transfers or capacity specified. The way this
        /// is used is to turn off level tracking in a mixture/vessel, then add &amp; remove what is specified (add, then remove),
        /// then log the tracker's high and low marks, and finally turn level tracking back on.
        /// Note - this is done inside the SOMTask's DoOperations(...) method, and takes the form of<p></p>
        /// <code>this.GetSOD(graphContext).SetChangeLogging(true);</code>
        /// </summary>
        /// <param name="vesselCapacity">The capacity of the vessel in which the mixture is being handled.</param>
        /// <param name="rp">The ReactionProcessor that knows of any reactions that will take place. Can be null.</param>
        public MassVolumeTracker(ReactionProcessor rp, double vesselCapacity) : this(null, null, null, vesselCapacity, rp) { }

        /// <summary>
        /// Creates a MVTracker with a full complement of parameters.
        /// </summary>
        /// <param name="initial">The initial mixture.</param>
        /// <param name="inflow">The inflowing mixture.</param>
        /// <param name="outflow">The outflowing mixture.</param>
        /// <param name="capacity">The capacity of the vessel.</param>
        /// <param name="rp">The ReactionProcessor that knows of any reactions that will take place. Can be null.</param>
        public MassVolumeTracker(Mixture initial, Mixture inflow, Mixture outflow, double capacity, ReactionProcessor rp) {
            m_massHistory = new DoubleTracker();
            m_volumeHistory = new DoubleTracker();
            SetInitialMixture(initial == null ? new Mixture() : (Mixture)initial.Clone());
            SetInflowMixture(inflow == null ? new Mixture() : (Mixture)inflow.Clone());
            SetOutflowMixture(outflow == null ? new Mixture() : (Mixture)outflow.Clone());
            m_reactionProcessor = rp;
            m_capacity = capacity;
            m_cyclical = true;
            m_inflowFirst = true;
        }

        /// <summary>
        /// Sets the initial mixture in the modeled vessel.
        /// </summary>
        /// <param name="initial">The initial mixture in the modeled vessel.</param>
        public void SetInitialMixture(Mixture initial) {
            if (initial != null) {
                m_initial = (Mixture)initial.Clone();
            } else {
                m_initial = new Mixture();
            }
        }

        /// <summary>
        /// Sets the inflowing mixture in the modeled vessel.
        /// </summary>
        /// <param name="inflow">The inflowing mixture in the modeled vessel.</param>
        public void SetInflowMixture(Mixture inflow) {
            if (inflow != null) {
                m_inflow = (Mixture)inflow.Clone();
            } else {
                m_inflow = new Mixture();
            }
        }

        /// <summary>
        /// Sets the outflowing mixture in the modeled vessel.
        /// </summary>
        /// <param name="outflow">The outflowing mixture in the modeled vessel.</param>
        public void SetOutflowMixture(Mixture outflow) {
            if (outflow != null) {
                m_outflow = (Mixture)outflow.Clone();
            } else {
                m_outflow = new Mixture();
            }
        }

        /// <summary>
        /// Sets the volumetric capacity of the modeled vessel.
        /// </summary>
        /// <param name="capacity">The volumetric capacity of the modeled vessel.</param>
        public void SetVesselCapacity(double capacity) {
            m_capacity = capacity;
        }

        /// <summary>
        /// Performs the inflow/outflow cycle analysis. After this runs, consult the Mass, Volume and Temperature history members.
        /// </summary>
        public void Process() {

            m_massHistory.Reset();
            m_volumeHistory.Reset();

            Mixture contents = (Mixture)m_initial.Clone();
            if (m_reactionProcessor != null)
                m_reactionProcessor.Watch(contents);

            Mixture inflow = m_inflow==null?new Mixture():(Mixture)m_inflow.Clone();
            Mixture outflow = m_outflow == null ? new Mixture() : (Mixture)m_outflow.Clone();

            double tiv = inflow.Volume;
            double tim = inflow.Mass;
            double tov = outflow.Volume;
            double tom = outflow.Mass;

            bool useMinidumps = true;
            //bool useMinidumps = false;
            //if ( ( tiv + contents.Volume - tov > m_capacity ) ) useMinidumps = true;

            RecordMvt(contents);
            if (m_cyclical) {
                if (m_inflowFirst) {
                    if (s_diagnostics)
                        Console.WriteLine("Initial - total volume = " + contents.Volume + ", mixture is " + contents);
                    while (inflow.Mass > 0.0 || outflow.Mass > 0.0) {

                        // Perform charge
                        if (inflow.Mass > 0.0) {
                            double chgMass = Math.Min(( m_capacity - contents.Volume ) * ( tim / tiv ), inflow.Mass);
                            double pctOfInFlow = chgMass / inflow.Mass;
                            contents.AddMaterial(inflow.RemoveMaterial(chgMass));
                            if (s_diagnostics)
                                Console.WriteLine("After charge - total volume = " + contents.Volume + ", mixture is " + contents);
                            RecordMvt(contents);
                        }

                        // Perform discharge
                        if (outflow.Mass > 0.0) {
                            double dischgMass = Math.Min(( m_capacity - contents.Volume ) * ( tom / tov ), outflow.Mass);
                            Mixture extract = (Mixture)outflow.RemoveMaterial(dischgMass);
                            foreach (Substance substance in extract.Constituents) {
                                contents.RemoveMaterial(substance.MaterialType, extract.Mass);
                            }
                            if (s_diagnostics)
                                Console.WriteLine("After discharge - total volume = " + contents.Volume + ", mixture is " + contents);
                            RecordMvt(contents);
                        }

                        if (useMinidumps)
                            contents.Clear();
                    }
                } else {
                    throw new NotImplementedException("Only able to model inflow-first, cyclical behavior.");
                }
            } else {
                throw new NotImplementedException("Only able to model inflow-first, cyclical behavior.");
            }
            if (m_reactionProcessor != null)
                m_reactionProcessor.Ignore(contents);
        }

        private void RecordMvt(Mixture mixture) {
            m_massHistory.Register(mixture.Mass);
            m_volumeHistory.Register(mixture.Volume);
            //m_temperatureHistory.Register(mixture.Temperature);
        }

        /// <summary>
        /// DoubleTracker that provides the initial, min, max and final mass values of the mixture in the vessel.
        /// </summary>
        public DoubleTracker MassHistory { get { return m_massHistory; } }

        /// <summary>
        /// DoubleTracker that provides the initial, min, max and final volume values of the mixture in the vessel.
        /// </summary>
        public DoubleTracker VolumeHistory { get { return m_volumeHistory; } }

        //		/// <summary>
        //		/// DoubleTracker that provides the initial, min, max and final temperature values of the mixture in the vessel.
        //		/// </summary>
        //		public DoubleTracker TemperatureHistory { get { return m_temperatureHistory; } }

    }
}
