/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure {

    public enum PressureUnits { mmHg, Pascals, Bar, Atm } // Defaults are mmHg and Celsius 
    public enum TemperatureUnits { Celsius, Kelvin }

	/// <summary>
    /// Antoine's coefficients which are expressed in °C and mmHg.
	/// </summary>
	public class AntoinesCoefficients3Impl : IAntoinesCoefficients3 {

		#region Private Fields
		private Double m_a;
		private Double m_b;
		private Double m_c;
        private PressureUnits m_pu;
        private TemperatureUnits m_tu;
		#endregion
		
		#region Constructors
		public AntoinesCoefficients3Impl(){
			m_a = Double.NaN;
			m_b = Double.NaN;
			m_c = Double.NaN;
            m_pu = PressureUnits.mmHg;
            m_tu = TemperatureUnits.Celsius;

        }

        public AntoinesCoefficients3Impl(double a, double b, double c, PressureUnits spu, TemperatureUnits stu) {
            m_a = a;
            m_b = b;
            m_c = c;
            m_pu = PressureUnits.mmHg;
            m_tu = TemperatureUnits.Celsius;
        }

		#endregion

		#region IEmissionCoefficients Members
		public bool IsSufficientlySpecified(double temperature){
			return !(Double.IsNaN(m_a)||Double.IsNaN(m_b)||Double.IsNaN(m_c));
		}
		#endregion

        #region IAntoinesCoefficients Members

        public double GetPressure(double temperature, TemperatureUnits tu, PressureUnits resultUnits) {
            temperature = ConvertTemperature(temperature, tu, m_tu);
            double pressure = Math.Pow(10, ( A - ( B / ( temperature + C ) ) ));
            return ConvertPressure(pressure, m_pu, resultUnits);
        }

        public double GetTemperature(double pressure, PressureUnits pu, TemperatureUnits resultUnits) {
            pressure = ConvertPressure(pressure, pu, m_pu);
            double temperature = B / ( A - Math.Log10(pressure) ) - C;
            return ConvertTemperature(temperature, m_tu, resultUnits);
        }



        private double ConvertPressure(double pressure, PressureUnits srcUnits, PressureUnits resultUnits) {
            if (srcUnits != resultUnits) {
                switch (srcUnits) { // Convert it to mmHg : ref: http://physics.nist.gov/Pubs/SP811/appenB9.html#PRESSURE
                    case PressureUnits.Bar:
                        pressure *= (133.3224/*mmHg-per-Pascal*/ / 100000/*Bar-per-Pascal*/ );
                        break;
                    case PressureUnits.mmHg:
                        break;
                    case PressureUnits.Atm:
                        pressure *= ( 101325/*Atm-per-Pascal*/ / 133.3224/*mmHg-per-Pascal*/);
                        break;
                    case PressureUnits.Pascals:
                        pressure /= 133.3224/*mmHg-per-Pascal*/;
                        break;
                }
                switch (resultUnits) {// Convert it from mmHg
                    case PressureUnits.Bar:
                        pressure /= (133.3224/*mmHg-per-Pascal*/ / 100000/*Bar-per-Pascal*/ );
                        break;
                    case PressureUnits.mmHg:
                        break;
                    case PressureUnits.Atm:
                        pressure /= ( 101325/*Atm-per-Pascal*/ / 133.3224/*mmHg-per-Pascal*/);
                        break;
                    case PressureUnits.Pascals:
                        pressure *= 133.3224/*mmHg-per-Pascal*/;
                        break;
                }
            }
            return pressure;
        }

        private double ConvertTemperature(double temperature, TemperatureUnits srcUnits, TemperatureUnits resultUnits) {
            if (srcUnits != resultUnits) {
                switch (srcUnits) { // Convert to celsius
                    case TemperatureUnits.Celsius:
                        break;
                    case TemperatureUnits.Kelvin:
                        temperature -= 273.15;
                        break;
                }
                switch (resultUnits) { // Convert from celsius
                    case TemperatureUnits.Celsius:
                        break;
                    case TemperatureUnits.Kelvin:
                        temperature += 273.15;
                        break;
                }
            }
            return temperature;
        }

        /// <summary>
        /// Gets or sets the pressure units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The pressure units.</value>
        public PressureUnits PressureUnits {
            get {
                return m_pu;
            }
            set {
                m_pu = value;
            }
        }

        /// <summary>
        /// Gets or sets the temperature units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The temperature units.</value>
        public TemperatureUnits TemperatureUnits {
            get {
                return m_tu;
            }
            set {
                m_tu = value;
            }
        }

        #endregion

		#region IAntoinesCoefficients3 Members

		public Double A {
			get {
				return m_a;
			}
		}

		public Double B {
			get {
				return m_b;
			}
		}

		public Double C {
			get {
				return m_c;
			}
		}

		#endregion

    }

    /// <summary>
    /// Extended Antoine Coefficients are always, and only, specified in °C and mmHg.
    /// </summary>
	public class AntoinesCoefficientsExt : IAntoinesCoefficientsExt {

		#region Public Constants
		public static readonly Double DEFAULT_C3 = 0.0;
		public static readonly Double DEFAULT_C4 = 0.0;
		public static readonly Double DEFAULT_C5 = 0.0;
		public static readonly Double DEFAULT_C6 = 0.0;
		public static readonly Double DEFAULT_C7 = 0.0;
		public static readonly Double DEFAULT_C8 = 0.0;
		public static readonly Double DEFAULT_C9 = 1000.0;
		#endregion

		#region Private Fields
		private Double m_c1;
		private Double m_c2;
		private Double m_c3 = DEFAULT_C3;
		private Double m_c4 = DEFAULT_C4;
		private Double m_c5 = DEFAULT_C5;
		private Double m_c6 = DEFAULT_C6;
		private Double m_c7 = DEFAULT_C7;
		private Double m_c8 = DEFAULT_C8;
		private Double m_c9 = DEFAULT_C9;
        private PressureUnits m_pu;
        private TemperatureUnits m_tu;
		#endregion

		#region Constructors
		public AntoinesCoefficientsExt(){
            m_pu = PressureUnits.mmHg;
            m_tu = TemperatureUnits.Celsius;
			m_c1 = Double.NaN;
			m_c2 = Double.NaN;
		}

		public AntoinesCoefficientsExt(double c1, double c2, PressureUnits pu, TemperatureUnits tu){
            m_pu = pu;
            m_tu = tu;
            m_c1 = c1;
			m_c2 = c2;
		}

        public AntoinesCoefficientsExt(double c1, double c2, double c3, double c4, double c5, double c6, double c7, double c8, double c9, PressureUnits pu, TemperatureUnits tu) {
            m_pu = pu;
            m_tu = tu;
            m_c1 = c1;
			m_c2 = c2;
			if ( !double.IsNaN(c3) ) m_c3 = c3;
			if ( !double.IsNaN(c4) ) m_c4 = c4;
			if ( !double.IsNaN(c5) ) m_c5 = c5;
			if ( !double.IsNaN(c6) ) m_c6 = c6;
			if ( !double.IsNaN(c7) ) m_c7 = c7;
			if ( !double.IsNaN(c8) ) m_c8 = c8;
			if ( !double.IsNaN(c9) ) m_c9 = c9;
		}
		#endregion

		#region IEmissionCoefficients Members
		public bool IsSufficientlySpecified(double temperature) {
			// c8 & c9 are in degrees C
			double ctemp = temperature + Constants.KELVIN_TO_CELSIUS;
			return ( ctemp > m_c8 && ctemp < m_c9) && !(Double.IsNaN(m_c1)||Double.IsNaN(m_c2));
		}
		#endregion

		#region IAntoinesCoefficientsExt Members

		public Double C1 {
			get {
				return m_c1;
			}
		}

		public Double C2 {
			get {
				return m_c2;
			}
		}

		public Double C3 {
			get {
				return m_c3;
			}
		}

		public Double C4 {
			get {
				return m_c4;
			}
		}

		public Double C5 {
			get {
				return m_c5;
			}
		}

		public Double C6 {
			get {
				return m_c6;
			}
		}

		public Double C7 {
			get {
				return m_c7;
			}
		}

		public Double C8 {
			get {
				return m_c8;
			}
		}

		public Double C9 {
			get {
				return m_c9;
			}
		}

		#endregion

        #region IAntoinesCoefficients Members

        public double GetPressure(double temperature, TemperatureUnits tu, PressureUnits resultUnits) {
            double retval = double.NaN;
            temperature = ConvertTemperature(temperature, tu, TemperatureUnits.Kelvin);
            if (IsSufficientlySpecified(temperature)) {
                double pressure = Math.Exp(C1 + ( C2 / ( temperature + C3 ) ) + ( C4 * temperature )
                    + ( C5 * Math.Log(temperature, Math.E) ) + ( C6 * Math.Pow(temperature, C7) ));

                retval = ConvertPressure(pressure, PressureUnits.Pascals, resultUnits);
            }
            return retval;
        }
                    
        private double ConvertPressure(double pressure, PressureUnits srcUnits, PressureUnits resultUnits) {
            if (srcUnits != resultUnits) {
                switch (srcUnits) { // Convert it to mmHg : ref: http://physics.nist.gov/Pubs/SP811/appenB9.html#PRESSURE
                    case PressureUnits.Bar:
                        pressure *= (133.3224/*mmHg-per-Pascal*/ / 100000/*Bar-per-Pascal*/ );
                        break;
                    case PressureUnits.mmHg:
                        break;
                    case PressureUnits.Atm:
                        pressure *= ( 101325/*Atm-per-Pascal*/ / 133.3224/*mmHg-per-Pascal*/);
                        break;
                    case PressureUnits.Pascals:
                        pressure /= 133.3224/*mmHg-per-Pascal*/;
                        break;
                }
                switch (resultUnits) {// Convert it from mmHg
                    case PressureUnits.Bar:
                        pressure /= (133.3224/*mmHg-per-Pascal*/ / 100000/*Bar-per-Pascal*/ );
                        break;
                    case PressureUnits.mmHg:
                        break;
                    case PressureUnits.Atm:
                        pressure /= ( 101325/*Atm-per-Pascal*/ / 133.3224/*mmHg-per-Pascal*/);
                        break;
                    case PressureUnits.Pascals:
                        pressure *= 133.3224/*mmHg-per-Pascal*/;
                        break;
                }
            }
            return pressure;
        }

        private double ConvertTemperature(double temperature, TemperatureUnits srcUnits, TemperatureUnits resultUnits) {
            if (srcUnits != resultUnits) {
                switch (srcUnits) { // Convert to celsius
                    case TemperatureUnits.Celsius:
                        break;
                    case TemperatureUnits.Kelvin:
                        temperature -= 273.15;
                        break;
                }
                switch (resultUnits) { // Convert from celsius
                    case TemperatureUnits.Celsius:
                        break;
                    case TemperatureUnits.Kelvin:
                        temperature += 273.15;
                        break;
                }
            }
            return temperature;
        }

        /// <summary>
        /// Gets or sets the pressure units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The pressure units.</value>
        public PressureUnits PressureUnits {
            get {
                return m_pu;
            }
            set {
                m_pu = value;
            }
        }

        /// <summary>
        /// Gets or sets the temperature units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The temperature units.</value>
        public TemperatureUnits TemperatureUnits {
            get {
                return m_tu;
            }
            set {
                m_tu = value;
            }
        }

        public double GetTemperature(double pressure, PressureUnits pu, TemperatureUnits resultUnits) {
            throw new NotImplementedException();
        }

        #endregion
    }
}

