/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Materials.Emissions
{
    /// <summary>
    /// An enumeration that contains all of the emissions classifications that we know about.
    /// </summary>
	public enum EmissionsClassifications { 
		/// <summary>
		/// Volatile Organic Compound
		/// </summary>
		Voc,
		/// <summary>
		/// Superfund Amendment and Reauthorization Act Toxic Release Inventory
		/// </summary>
		SaraTri,
		/// <summary>
		/// Hazardous Air Pollutant
		/// </summary>
		Hap,
		/// <summary>
		/// National Air Toxics Assessment
		/// </summary>
		Nata,
		/// <summary>
		/// Greenhouse Gas
		/// </summary>
		Ghg,
		/// <summary>
		/// Ozone Depleting Compound
		/// </summary>
		Odc
	}

	/// <summary>
	/// Marker class whose derived classes indicate Classifications of emissions that are created by a
	/// specific material type. These are attached to specific material types, and are used to identify
	/// the reporting requirements relevant to that material type.
	/// </summary>
	public class EmissionsClassification {
        /// <summary>
        /// Gets the name of the Emissions Classification.
        /// </summary>
        /// <value>The name.</value>
		public string Name => GetType().Name;
	}

	public class VolatileOrganicCompound : EmissionsClassification {

	}
	public class SaraToxicReleaseInventory : EmissionsClassification {

	}
	public class HazardousAirPollutant : EmissionsClassification {

	}
	public class NationalAirToxicsAssessment : EmissionsClassification {

	}
	public class GreenhouseGas : EmissionsClassification {

	}
	public class OzoneDepletingCompound : EmissionsClassification {

	}
}
