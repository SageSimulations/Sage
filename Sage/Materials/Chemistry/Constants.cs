/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable InconsistentNaming
namespace Highpoint.Sage.Materials.Chemistry {

    /// <summary>
    /// A class that holds useful chemistry constants.
    /// </summary>
	public class Constants {

		// http://physics.nist.gov/cuu/Constants/Table/allascii.txt
		public static readonly double MolarGasConstant = 8.314472; // J mol^-1 K^-1
		
		// http://www.mpch-mainz.mpg.de/~sander/res/henry-conv.html
		public static readonly double HenrysLawConstant = 2.479E06; /*Henry's law const in l Pa mole^-1*/
		
        /// <summary>
        /// Add this to a Celsius value to get its Kelvin equivalent.
        /// </summary>
		public static readonly double CELSIUS_TO_KELVIN = 273.15;

        /// <summary>
        /// Add this to a Kelvin value to get its Celsius equivalent.
        /// </summary>
        public static readonly double KELVIN_TO_CELSIUS = -273.15;
	}
}
