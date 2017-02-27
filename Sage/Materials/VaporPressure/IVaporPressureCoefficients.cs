/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure {

	/// <summary>
	/// Determines if, in a certain situation, a set of coefficients, and therefore the
	/// calculation mechanism that uses those coefficients, can be used.
	/// </summary>
	public interface IEmissionCoefficients {
		/// <summary>
		/// Determines if, in a certain situation, a set of coefficients, and therefore the
		/// calculation mechanism that uses those coefficients, can be used.
		/// </summary>
		/// <param name="temperature">The temperature of the mixture being assessed, in degrees Kelvin.</param>
		/// <returns></returns>
		bool IsSufficientlySpecified(double temperature);
	}

    public interface IAntoinesCoefficients : IEmissionCoefficients {
        double GetPressure(double temperature, TemperatureUnits tu, PressureUnits resultUnits);
        double GetTemperature(double pressure, PressureUnits pu, TemperatureUnits resultUnits);
        /// <summary>
        /// Gets or sets the pressure units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The pressure units.</value>
        PressureUnits PressureUnits { 
            get; 
            set; 
        }
        /// <summary>
        /// Gets or sets the temperature units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The temperature units.</value>
        TemperatureUnits TemperatureUnits { 
            get; 
            set;
        }
    }

    public interface IAntoinesCoefficients3 : IAntoinesCoefficients {
		double A { get; }
		double B { get; }
		double C { get; }
	}

    public interface IAntoinesCoefficientsExt : IAntoinesCoefficients {
		double C1 { get; }
		double C2 { get; }
		double C3 { get; }
		double C4 { get; }
		double C5 { get; }
		double C6 { get; }
		double C7 { get; }
		double C8 { get; }
		double C9 { get; }
	}
}
