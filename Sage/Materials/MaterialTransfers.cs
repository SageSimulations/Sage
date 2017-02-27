/* This source code licensed under the GNU Affero General Public License */

using System;
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Mathematics.Scaling;

namespace Highpoint.Sage.Materials {

    /// <summary>
    /// This interface is implemented by an object that will be used to extract material 
    /// from a mixture. Note that the implementer will actually change the source material in
    /// doing so.
    /// </summary>
    public interface IMaterialExtractor {
        /// <summary>
        /// Extracts a substance or mixture from another substance or mixture.
        /// </summary>
        IMaterial GetExtract(IMaterial source);
    }

    /// <summary>
    /// This interface is implemented by any object that intends to help perform a material transfer
    /// by extracting the material of interest, and declaring how long it will take to transfer.
    /// </summary>
    public interface IMaterialTransferHelper : IMaterialExtractor, SimCore.ICloneable {
        /// <summary>
        /// Indicates the duration of the transfer.
        /// </summary>
		TimeSpan Duration{ get; }
        /// <summary>
        /// Indicates the mass of material that will be involved in the transfer.
        /// </summary>
        double Mass{ get; }
        /// <summary>
        /// Indicates the MaterialType of the material that will be involved in the transfer. If it is
        /// null, then the specified mass will be transferred, but it will be of the mixture specified
        /// in the target mixture.
        /// </summary>
        MaterialType MaterialType{ get; }
    }

    /// <summary>
    /// Represents a request for material transfer, specified as mass of a particular substance. 
    /// </summary>
    public class MaterialTransferSpecByMass : IMaterialTransferHelper, IScalable {
        private MaterialType m_materialType;
        private double m_mass;
        private TimeSpan m_duration;

        private IDoubleScalingAdapter m_massScaler = null;
        private ITimeSpanScalingAdapter m_durationScaler = null;

        /// <summary>
        /// Gets the mass scaler associated with this MaterialTransferSpecByMass.
        /// </summary>
        /// <value>The mass scaler.</value>
		public IDoubleScalingAdapter MassScaler { get { return m_massScaler; } }
        /// <summary>
        /// Gets the duration scaler associated with this MaterialTransferSpecByMass.
        /// </summary>
        /// <value>The duration scaler.</value>
		public ITimeSpanScalingAdapter DurationScaler { get { return m_durationScaler; } }

        /// <summary>
        /// Creates a MaterialTransferSpecByMass that will transfer a specified mass of a
        /// specified type of material, over a specified duration. It is presumed that duration
        /// and mass do NOT scale, so if you want them to, you will need to add the
        /// appropriate scaling adapters.
        /// </summary>
        /// <param name="matlType">The material type to be transferred.</param>
        /// <param name="mass">The base amount (before scaling) of the material to transfer.</param>
        /// <param name="duration">The base duration (before scaling) of the transfer.</param>
        public MaterialTransferSpecByMass(MaterialType matlType, double mass, TimeSpan duration){
            m_materialType = matlType;
            m_mass = mass;
            m_duration = duration;
        }

        /// <summary>
        /// Provides this transferSpec with a scaling adapter that will scale the transfer duration.<p></p>
        /// As an example, adding a TimeSpanLinearScalingAdapter with a linearity of 1.0 will cause
        /// duration to scale precisely in proportion to the aggregate scale provided in the Rescale operation.
        /// </summary>
        /// <param name="tsa">The ITimeSpanScalingAdapter that will provide timespan scaling for this transfer.</param>
        public void SetDurationScalingAdapter(ITimeSpanScalingAdapter tsa){
            m_durationScaler = tsa;
        }

        /// <summary>
        /// Provides this transferSpec with a scaling adapter that will scale the mass to be transferred.<p></p>
        /// As an example, adding a DoubleLinearScalingAdapter with a linearity of 1.0 will cause
        /// mass to scale precisely in proportion to the aggregate scale provided in the Rescale operation.
        /// </summary>
        /// <param name="dsa">The scaling adapter that will perform mass scaling for this transfer spec.</param>
        public void SetMassScalingAdapter(IDoubleScalingAdapter dsa){
            m_massScaler = dsa;
        }

        /// <summary>
        /// The material type to be transferred.
        /// </summary>
		public MaterialType MaterialType { get {return m_materialType; } }

        /// <summary>
        /// The mass to be transferred. This value will reflect any scaling operations that have been done.
        /// </summary>
		public double Mass{ get {return m_mass; } }

        /// <summary>
        /// The duration of the transfer. This value will reflect any scaling operations that have been done.
        /// </summary>
		public TimeSpan Duration{ get {return m_duration; } }

        /// <summary>
        /// Commands a rescale of the transfer spec's mass and duration to a scale factor of the originally
        /// defined size.
        /// </summary>
        /// <param name="aggregateScale">The scaling to be applied to the initally-defined values.</param>
        public void Rescale(double aggregateScale){
            if ( m_massScaler != null ) {
                m_massScaler.Rescale(aggregateScale);
                m_mass = m_massScaler.CurrentValue;
            }
            if ( m_durationScaler != null ) {
                m_durationScaler.Rescale(aggregateScale);
                m_duration = m_durationScaler.CurrentValue;
            }
        }
        
        /// <summary>
        /// Gets the material to be transferred by extracting it from the source material.
        /// </summary>
        /// <param name="source">The material from which the transfer is to be made.</param>
        /// <returns>The material to be transferred.</returns>
        public virtual IMaterial GetExtract(IMaterial source){

            double massToRemove = Mass;
            IMaterial retval = null;

            if ( m_materialType == null ) {
                // We're taking a mass of the whole substance or mixture.
                if ( source is Substance ) {
                    retval = ((Substance)source).Remove(massToRemove);
                } else {
                    retval = ((Mixture)source).RemoveMaterial(massToRemove);
                }
            } else {
                if ( source is Substance ) {
                    Substance s = ((Substance)source);
                    if ( s.MaterialType.Equals(m_materialType) ) {
                        retval = s.Remove(massToRemove);
                    } else {
                        return m_materialType.CreateMass(0,0);
                    }
                } else if ( source is Mixture ) {
                    Mixture m = ((Mixture)source);
                    retval = m.RemoveMaterial(m_materialType,massToRemove);
                    if (retval == null) {
                        retval = m_materialType.CreateMass(0, 0);
                    }
                } else {
                    throw new ApplicationException("Attempt to remove an unknown implementer of IMaterial from a mixture!");
                }
            }

			if ( retval == null ) {
				throw new ApplicationException("Unable to get extract from " + source);
			}

            //BUG: If the extract didn't get all it wanted, and time is scaled AND super- or sub-linear, the reported duration will be wrong.
			if ( retval.Mass != massToRemove && m_durationScaler != null ) {
                // We didn't get all the mass we wanted, so we will reset mass and duration to the amount we got.
                double currentScale = (double)m_durationScaler.CurrentValue.Ticks/(double)m_durationScaler.FullScaleValue.Ticks;
                double factor = retval.Mass/massToRemove;
                m_duration = TimeSpan.FromTicks((long)(m_duration.Ticks*factor));
            }
            return retval;
        }

		/// <summary>
		/// Clone operation allows a MTSBM object to be reused, thereby eliminating the need to
		/// re-specify each time the transfer is to take place.
		/// </summary>
		/// <returns>A clone of this instance.</returns>
		public virtual object Clone(){
			MaterialTransferSpecByMass mtsm = new MaterialTransferSpecByMass(m_materialType,m_mass, m_duration);
			if ( m_durationScaler != null ) mtsm.SetDurationScalingAdapter(m_durationScaler.Clone());
			if ( m_massScaler != null )     mtsm.SetMassScalingAdapter(m_massScaler.Clone());
			if ( CloneEvent != null )       CloneEvent(this,mtsm);
			return mtsm;
		}

        /// <summary>
        /// Fired after a cloning operation has taken place.
        /// </summary>
		public event CloneHandler CloneEvent;

        /// <summary>
        /// Provides a human-readable description of the transfer mass, material, and duration, scaled as requested.
        /// </summary>
        /// <returns>A human-readable description of the transfer mass, material, and duration, scaled as requested.</returns>
        public override string ToString(){
			string material = (m_materialType == null?"Entire Mixture":m_materialType.Name);
            return m_mass.ToString("F2") + " kg of " + material + ", which should take " + m_duration;
        }
    }
    /// <summary>
    /// Represents a request for material transfer, specified as a percentage of a particular
    /// material in the source mixture.  Stored in an arraylist keyed to the port on which it
    /// is to be effected, in the SOMTaskDetails.m_xferOutSpecLists hashtable.<p></p>
    /// <p></p>
    /// Note that a MaterialTransferSpecByPercentage is not scalable. It is, in effect, scaled
    /// by the amount of material in the source container.
    /// </summary>
    public class MaterialTransferSpecByPercentage : IMaterialTransferHelper {
        private MaterialType m_materialType;
        private double m_percentage;
        private TimeSpan m_durationPerKilogram;
        private TimeSpan m_duration;
        private double m_actualMass;

        /// <summary>
        /// Creates a MaterialTransferSpecByMass that will transfer a specified mass of a
        /// specified type of material, over a specified duration. It is presumed that duration
        /// and mass do NOT scale, so if you want them to, you will need to add the
        /// appropriate scaling adapters.
        /// </summary>
        /// <param name="matlType">The material type to be transferred.</param>
        /// <param name="percentage">The percentage of the material of the specified type in the source container.</param>
        /// <param name="durationPerKilogram">The timespan required to transfer each kilogram of material.</param>
        public MaterialTransferSpecByPercentage(MaterialType matlType, double percentage, TimeSpan durationPerKilogram){
            m_materialType = matlType;
            m_percentage = percentage;
            m_duration = TimeSpan.Zero;
            m_durationPerKilogram = durationPerKilogram;
        }

        /// <summary>
        /// The type of the material to be transferred.
        /// </summary>
		public virtual MaterialType MaterialType{ get {return m_materialType;}}
 
        /// <summary>
        /// The percentage of the material of the specified type that is found in the source container, that should be transferred.
        /// </summary>
		public virtual double Percentage { get {return m_percentage;}}

        /// <summary>
        /// The total duration of the transfer. Note that since this is dependent upon the mass,
        /// which is dependent on how much of the type of material was found in the source container,
        /// it will not be known correctly until after <b>GetExtract</b> is called.
        /// </summary>
		public virtual TimeSpan Duration { get {return m_duration;} }

        /// <summary>
        /// The total mass of the transfer. Note that since this is dependent upon how
        /// much of the type of material was found in the source container,
        /// it will not be known correctly until after <b>GetExtract</b> is called.
        /// </summary>
		public virtual double Mass { get {return m_actualMass;}}

		/// <summary>
		/// The timespan to allot for each kilogram transferred.
		/// </summary>
        public TimeSpan DurationPerKilogram { get { return m_durationPerKilogram; } }

        /// <summary>
        /// Gets the material to be transferred by extracting it from the source material.
        /// </summary>
        /// <param name="source">The material from which the transfer is to be made.</param>
        /// <returns>The material to be transferred.</returns>
        public virtual IMaterial GetExtract(IMaterial source){
            IMaterial retval = null;
            if ( m_materialType == null ) {
                double massToRemove = source.Mass*Percentage;
				if ( source is Substance ) {
                    retval = ((Substance)source).Remove(massToRemove);
                } else {
                    retval = ((Mixture)source).RemoveMaterial(massToRemove);
                }
            } else {
                if ( source is Substance ) {
                    Substance s = ((Substance)source);
                    if ( s.MaterialType.Equals(m_materialType) ) {
                        retval = s.Remove(s.Mass * m_percentage);
                    } else {
                        retval = m_materialType.CreateMass(0,0);
                    }
                } else if ( source is Mixture ) {
                    Mixture m = ((Mixture)source);
                    retval = m.RemoveMaterial(m_materialType, m.ContainedMassOf(m_materialType) * m_percentage);
                    if ( retval == null ) retval = m_materialType.CreateMass(0,0);
                } else {
                    throw new ApplicationException("Attempt to remove an unknown implementer of IMaterial from a mixture!");
                }
            }

            m_actualMass = retval.Mass;
            m_duration = TimeSpan.FromTicks((long)(m_durationPerKilogram.Ticks * retval.Mass));
            return retval;

        }

		/// <summary>
		/// Clone operation allows a MTSBP object to be reused, thereby eliminating the need to
		/// re-specify each time the transfer is to take place.
		/// </summary>
		/// <returns>A clone of this instance.</returns>
		public virtual object Clone(){
			MaterialTransferSpecByPercentage mtsp = new MaterialTransferSpecByPercentage(m_materialType,m_percentage, m_durationPerKilogram);
			if ( CloneEvent != null ) CloneEvent(this,mtsp);
			return mtsp;
		}

        /// <summary>
        /// Fired after a clone operation has taken place.
        /// </summary>
		public event CloneHandler CloneEvent;

        /// <summary>
        /// Provides a human-readable description of the transfer mass, material, and duration, scaled as requested.
        /// </summary>
        /// <returns>A human-readable description of the transfer mass, material, and duration, scaled as requested.</returns>
        public override string ToString(){
			string material = (m_materialType == null?"Entire Mixture":m_materialType.Name);
			return (m_percentage*100).ToString("F2") + "% of " + material + ", which should take " + m_duration;
        }

    }
}