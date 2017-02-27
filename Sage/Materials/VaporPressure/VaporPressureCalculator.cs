/* This source code licensed under the GNU Affero General Public License */
//#define WE_TRUST_HENRYS_LAW_IMPLEMENTATION

using System;
using K=Highpoint.Sage.Materials.Chemistry.Constants;
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable UnusedVariable

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{
	/// <summary>
	/// Summary description for VaporPressureCalculator.
	/// </summary>
	public class VaporPressureCalculator {

		// A material that is less that 5% of the mixture by mass, can be ignored if we don't have data on it.
		private static readonly double s_relevance_Threshold = 0.05;

        public static double MinPressure = 100.0;

        /// <summary>
        /// Computes and returns the vapor pressure of the specified material type in the
        /// mixture at the given temperature and pressure. The returned value is expressed
        /// in millimeters of mercury.
        /// </summary>
        /// <param name="mt">The material type of interest.</param>
        /// <param name="temperature">The temperature of the mixture and free space, in degrees Kelvin.</param>
        /// <param name="srcUnits">The SRC units.</param>
        /// <param name="resultUnits">The result units.</param>
        /// <returns>
        /// Vapor Pressure in millimeters of Mercury.
        /// </returns>
		public static double ComputeVaporPressure(MaterialType mt, double temperature, TemperatureUnits srcUnits, PressureUnits resultUnits){

            double vprl = mt.AntoinesLawCoefficientsExt.GetPressure(temperature, srcUnits, resultUnits);
            if (double.IsNaN(vprl)) {
                vprl = mt.AntoinesLawCoefficients3.GetPressure(temperature, srcUnits, resultUnits);
            }

			return vprl;
		}

		/// <summary>
		/// Computes and returns the vapor pressure of the specified material type in the
		/// mixture at the given temperature and pressure. The returned value is expressed
		/// in atmospheres. From the WebEmit spreadsheet, we are using the following
		/// algorithm:
		/// The means for calculating Vapor Pressure VP of Substance S in mixture M at temperature T and pressure P, is:
		/// <code>
		/// 
		/// VPHL is [Vapor Pressure by Henry's Law]
		/// VPRL is [Vapor Pressure by Raoult's Law]
		/// 
		/// if ( mole fraction of S in M is &gt; 10% ) {
		///     VPRL if we can, else VPHL
		/// } else {
		/// 	the lesser of VPRL &amp; VPHL
		/// }
		/// 
		/// If no legitimate answer can be derived, we will return double.NaN
		/// </code> 
		/// </summary>
		/// <param name="mt">The material type of interest.</param>
		/// <param name="mixture">The mixture under consideration.</param>
		/// <returns>Vapor Pressure in Atmospheres.</returns>
		public static double ComputeVaporPressure(MaterialType mt, Mixture mixture){
			return ComputeVaporPressure(mt, mixture.Temperature, TemperatureUnits.Celsius, PressureUnits.Atm);
		}
		
		/// <summary>
		/// Computes the sum of partial pressures of all of the substances in the specified mixture at the specified temerature. 
		/// </summary>
		/// <param name="mixture">The mixture with the substances whose partial pressures are to be added.</param>
		/// <param name="temperature">The temperature at which the partial pressures are to be calculated, in degrees kelvin.</param>
		/// <returns>The sum of partial pressures of the substances in the mixture, in Pascals, to be found above the mixture.</returns>
		public static double SumOfPartialPressures(Mixture mixture, double temperature){
			double sum = 0.0;
			foreach ( Substance substance in mixture.Constituents ) {

				// If no Antoine's coefficients exist that are usable, then we ignore the material.
				if ( substance.MaterialType.AntoinesLawCoefficientsExt.IsSufficientlySpecified(temperature) 
					|| substance.MaterialType.AntoinesLawCoefficients3.IsSufficientlySpecified(temperature) ){

					double tmp = PartialPressure(substance,temperature,mixture);

					if ( double.IsNaN(tmp) ) {
						if ( substance.Mass > s_relevance_Threshold * mixture.Mass ) {
							if ( mixture.Model != null ) {
								string name = "Unknown Vapor Pressure";
								string narrative = "Error calculating partial pressure of mixture " + mixture.Name + " due to unknown VP of constituent " + substance.Name + ".";

                                if (substance.MaterialType.MolecularWeight == 0) {
                                    narrative += (" This is because its molecular weight is set to zero. Please provide an appropriate value.");
                                }

                                SimCore.GenericModelError error = new SimCore.GenericModelError(name,narrative,typeof(VaporPressureCalculator),substance);
								mixture.Model.AddError(error);
							}
						}
					} else {
						sum += tmp;
					}
				}
			}
			return sum;
		}

		public static double ComputeBoilingPoint(Substance s, double atPressureInPascals){
			return ComputeBoilingPoint(s.MaterialType,atPressureInPascals);
		}

        /// <summary>
        /// Estimates the boiling point of the material type at the provided pressure. This is the point at
        /// which the partial pressure is equal to the external pressure.
        /// </summary>
        /// <param name="mt">The material type.</param>
        /// <param name="atPressureInPascals">The absolute pressure in pascals of the surrounding environment.</param>
        /// <returns></returns>
		public static double ComputeBoilingPoint(MaterialType mt, double atPressureInPascals){

            if (atPressureInPascals < MinPressure) {
                throw new VaporPressureException(mt.Name, atPressureInPascals, MinPressure);
            }

            double upper = double.NaN;
			double lower = double.NaN;

			double temperature = 273; // deg Kelvin

			double vp = ComputeVaporPressure(mt,temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
			if ( vp > atPressureInPascals ) {
				while ( vp > atPressureInPascals ) {
					upper = temperature;
					temperature -= 50.0;
                    vp = ComputeVaporPressure(mt, temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				}
				lower = temperature;
			} else {
				while ( vp < atPressureInPascals ) {
					lower = temperature;
					temperature += 50.0;
                    vp = ComputeVaporPressure(mt, temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				}
				upper = temperature;
			}

			temperature = (upper + lower)/2.0;
            vp = ComputeVaporPressure(mt, temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
			while ( ( upper - lower ) > 0.05 ) {
				if ( vp > atPressureInPascals ) upper = temperature;
				if ( vp < atPressureInPascals ) lower = temperature;
				temperature = (upper + lower)/2.0;
                vp = ComputeVaporPressure(mt, temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
			}
			return temperature + K.KELVIN_TO_CELSIUS;
		}

        /// <summary>
        /// Estimates the boiling point of the material types in the mixture at the provided pressure. This is the point at
        /// which the partial pressure is equal to the external pressure.
        /// </summary>
        /// <param name="m">The mixture whose boiling point is to be computed.</param>
        /// <param name="atPressureInPascals">The pressure in pascals of the surrounding environment.</param>
        /// <returns></returns>
        public static double ComputeBoilingPoint(Mixture m, double atPressureInPascals) {

            if (atPressureInPascals < MinPressure) {
                throw new VaporPressureException(m.ToString(),atPressureInPascals,MinPressure);
            }

			double upper = double.NaN;
			double lower = double.NaN;

			double temperature = 273; // deg Kelvin
			double vp = SumOfPartialPressures(m,temperature);
			if ( vp == 0.0 ) return double.MaxValue;

			if ( vp > atPressureInPascals ) {
				while ( vp > atPressureInPascals ) {
					upper = temperature;
					temperature -= 50.0;
					vp = SumOfPartialPressures(m,temperature);
				}
				lower = temperature;
			} else {
				while ( vp < atPressureInPascals ) {
					lower = temperature;
					temperature += 50.0;
					vp = SumOfPartialPressures(m,temperature);
				}
				upper = temperature;
			}

			temperature = (upper + lower)/2.0;
			vp = SumOfPartialPressures(m,temperature);
			while ( ( upper - lower ) > 0.5 ) {
				if ( vp > atPressureInPascals ) upper = temperature;
				if ( vp < atPressureInPascals ) lower = temperature;
				temperature = (upper + lower)/2.0;
				vp = SumOfPartialPressures(m,temperature);
			}
            return temperature + K.KELVIN_TO_CELSIUS;
		}

		
		#region Private Methods
		/// <summary>
		/// Computes the partial pressure of a specified substance in the volume above a specified mixture at a specified temperature.
		/// Note that we will ignore the temperature of the mixture.
		/// </summary>
		/// <param name="substance">The substance whose partial pressure we desire.</param>
		/// <param name="temperature">The temperature at which we wish to have the partial pressure computed, in degrees kelvin.</param>
		/// <param name="mixture">The mixture that specifies the other materials that are to be considered in the calculation.</param>
		/// <returns>The partial pressure of the specified substance in the volume above the specified mixture, in Pascals.</returns>
		private static double PartialPressure(Substance substance, double temperature, Mixture mixture){
			MaterialType mt = substance.MaterialType;
			if ( mt.STPState == MaterialState.Unknown || mt.STPState == MaterialState.Liquid ) {
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = ComputeVaporPressure(mt, temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				return molFrac * vaporPressure;
			} else {
				return 0.0;
			}
		}

        ///// <summary>
        ///// Computes vapor pressure, in Pascals, using extended Antoine's coefficients.
        ///// </summary>
        ///// <param name="ace">A structure containing extended Antoine's coefficients for the material being considered.</param>
        ///// <param name="temperatureInKelvin">Temperature in degrees kelvin.</param>
        ///// <returns>Vapor pressure, in mmHg.</returns>
        //private static double ComputeByAntoinesCoefficientsExt(IAntoinesCoefficientsExt ace, double temperatureInKelvin){
        //    if ( ace == null || !ace.IsSufficientlySpecified(temperatureInKelvin)) return double.NaN;
        //    return Math.Exp(ace.C1+(ace.C2/(temperatureInKelvin+ace.C3)) + (ace.C4*temperatureInKelvin) 
        //        + (ace.C5*Math.Log(temperatureInKelvin,Math.E)) + (ace.C6*Math.Pow(temperatureInKelvin,ace.C7)));
        //}

        ///// <summary>
        ///// Computes vapor pressure in Pascals, using 3-parameter Antoine's coefficients.
        ///// </summary>
        ///// <param name="ac3">A structure containing 3-parameter Antoine's coefficients for the material being considered.</param>
        ///// <param name="temperatureInKelvin">Temperature in degrees kelvin.</param>
        ///// <returns>Vapor pressure, in pascals.</returns>
        //private static double ComputeByAntoinesCoefficients3(IAntoinesCoefficients3 ac3, double temperatureInKelvin){
        //    return ac3.GetPressure(PressureUnits.Pascals, temperatureInKelvin, TemperatureUnits.Kelvin);
        //    //if ( ac3 == null || !ac3.IsSufficientlySpecified(temperatureInKelvin)) return double.NaN;
        //    //// Antoine's uses degrees C, not degrees K.
        //    //double tempC = temperatureInKelvin + K.KELVIN_TO_CELSIUS;
        //    //return Math.Pow(10, ( ac3.A - ( ac3.B / ( ac3.C + tempC ) ) ));
        //}
		#endregion
	}

    /// <summary>
    /// MissingParameterException is thrown when a required parameter is missing. Typically used in a late bound, read-from-name/value pair collection scenario.
    /// </summary>
    [Serializable]
    public class VaporPressureException : Exception {
        private readonly ReasonCode m_reason = ReasonCode.UnderPressure;
        // For best practice guidelines regarding the creation of new exception types, see
        //    https://msdn.microsoft.com/en-us/library/5b2yeyab(v=vs.110).aspx
        #region protected ctors
        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected VaporPressureException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #endregion
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public VaporPressureException() { }
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public VaporPressureException(string ofWhat, double atPressure, double minSupportedPressure)
        : base(string.Format("Attempt to compute vapor pressure of {0} at a pressure of {1} Pascals is unsupported. Please use a value greater than {2} Pascals for absolute pressure.",
                ofWhat, atPressure, minSupportedPressure)){
            m_reason = ReasonCode.UnderPressure;
        }
        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public VaporPressureException(string message) : base(message) { }
        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public VaporPressureException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
        public ReasonCode Reason => m_reason;

        public enum ReasonCode { UnderPressure }
    }
}


#region Algorithm Documentation
// From http://www.nist.gov/srd/webguide/nist87/87_1.htm
//
//...equations which represent vapor pressure as a function of temperature. 
//These parameters have been obtained by a least squares fit to published
//experimental or estimated data. They are used to generate tables of vapor
//pressures at regularly spaced temperatures or tables of temperatures at
//regularly spaced pressures.
//
// 
//
//At present the following two equations are used. 
//
//-Antoine equation
//
//log P=A-B/(C+T)
//
//--Extended Antoine equation
//
//log P = A - B/(C + T) + Dy÷n + Ey÷m + Fy÷k, where y = (T - To)/Tc
//
//In these equations P represents vapor pressure and T the temperature. 'To'
//is a boundary temperature. Below 'To' the simple Antoine equation is used,
//although different sets of parameters may be used for different ranges.
//Above 'To' the extended form is used. 'To' is the boiling point at some pressure
//in the range of 100 to 200 kPa. 'Tc' is the critical temperature and is, of
//course, the upper limit for calculating vapor pressures. 
//
//The terms A, B, C, n, E, and F are fitting parameters. D is the constant 0.434294
//and m and k are usually set to 8 and 12 respectively. Occasionally other values
//are required to obtain a satisfactory fit. In this Vapor Pressure File each data
//set consists of two numbers, auxiliary information and reference to the data source.
//
//The two numbers are temperature and vapor pressure. The term vapor pressure and
//boiling point have the same meaning and no distinction is made between them. 
//A normal boiling point is the temperature of the two-phase equilibrium at a pressure
//of one atmosphere (101.325 kilopascal). The following auxiliary data are also stored
//for each compound and used to generate additional information which accompanies the
//tables. 
#endregion
