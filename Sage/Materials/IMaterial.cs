/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility.Mementos;
using Highpoint.Sage.Persistence;

namespace Highpoint.Sage.Materials.Chemistry {

    /// <summary>
    /// Fired after a change in mass, constituents or temperature has taken place in this mixture. 
    /// </summary>
    public delegate void MaterialChangeListener(IMaterial material, MaterialChangeType type);

    /// <summary>
    /// An enumeration that describes the kind of change that has taken place in a material.
    /// </summary>
    public enum MaterialChangeType {
        /// <summary>
        /// The contents of the mixture changed.
        /// </summary>
        Contents,
        /// <summary>
        /// The temperature of the mixture changed.
        /// </summary>
        Temperature
    }

    /// <summary>
    /// Implemented by anything that is a material - Substances and Mixtures are current examples.
    /// </summary>
    public interface IMaterial : ISupportsMementos, IHasWriteLock, IXmlPersistable {

        /// <summary>
        /// Gets the type of the material.
        /// </summary>
        /// <value>The type of the material.</value>
        MaterialType MaterialType { get; }

        /// <summary>
        /// Gets the mass of the material in kilograms.
        /// </summary>
        /// <value>The mass of the material in kilograms.</value>
        double Mass { get; }

        /// <summary>
        /// Gets the volume of the material in liters.
        /// </summary>
        /// <value>The volume.</value>
        double Volume { get; }

        /// <summary>
        /// Gets the density of the material in kilograms per liter.
        /// </summary>
        /// <value>The density.</value>
		double Density { get; }

        /// <summary>
        /// Adds the specified number of joules of energy to the mixture.
        /// </summary>
        /// <param name="joules">The joules to add to the mixture.</param>
        void AddEnergy(double joules);

        /// <summary>
        /// Gets the specific heat of the mixture, in Joules per kilogram degree-K.
        /// </summary>
        /// <value>The specific heat.</value>
		double SpecificHeat { get; }
		
        /// <summary>
		/// Latent heat of vaporization - the heat energy required to vaporize one kilogram of this material. (J/kg)
		/// </summary>
		double LatentHeatOfVaporization { get; }
        
        /// <summary>
        /// Gets or sets the temperature in degrees Celsius (internally, temperatures are stored in degrees Kelvin.)
        /// </summary>
        /// <value>The temperature.</value>
		double Temperature { get; set; }   // Assumption - degrees Celsius (internally stored as degrees Kelvin).

        /// <summary>
        /// Gets the estimated boiling point at the specified pressure in pascals.
        /// </summary>
        /// <param name="atPressureInPascals">At pressure in pascals.</param>
        /// <returns></returns>
		double GetEstimatedBoilingPoint(double atPressureInPascals);

        /// <summary>
        /// Suspends the issuance of change events. When change events are resumed, one change event will be fired if
        /// the material has changed. This prevents a cascade of change events that would be issued, for example during
        /// the processing of a reaction.
        /// </summary>
        void SuspendChangeEvents();

        /// <summary>
        /// Fired after a material has changed its mass, constituents or temperature.
        /// </summary>
        event MaterialChangeListener MaterialChanged;

        /// <summary>
        /// Resumes the change events. When change events are resumed, one change event will be fired for each of temperature
        /// and/or contents, if that aspect of the material has changed. This prevents a cascade of change events that would
        /// be issued, for example during the processing of a reaction.
        /// </summary>
        /// <param name="issueSummaryEvents">if set to <c>true</c>, issues a summarizing event for each change type that has occurred.</param>
        void ResumeChangeEvents(bool issueSummaryEvents);

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        IMaterial Clone();
        
        /// <summary>
        /// Gets or sets the tag, which is a user-supplied data element.
        /// </summary>
        /// <value>The tag.</value>
        object Tag { get; set; }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// Uses caller-supplied format strings in forming the numbers representing mass and temperature.
        /// </summary>
        /// <param name="tempFmt">The temperature's numerical format string.</param>
        /// <param name="massFmt">The mass's numerical format string.</param>
        /// <returns></returns>
        string ToString(string tempFmt, string massFmt);

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </returns>
        string ToStringWithoutTemperature();
        
        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </summary>
        /// <param name="massFmt">The mass format string. For example, &quot;F2&quot; will display to two decimals.</param>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </returns>
        string ToStringWithoutTemperature(string massFmt);

    }
}