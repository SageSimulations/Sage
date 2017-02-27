/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Scheduling;

namespace Highpoint.Sage.Materials.Chemistry.Emissions {
	/// <summary>
	/// IEmissionRecord is an interface implemented by items that represent a record of an emission event.
	/// <b>Note: IHasIdentity is a structure that has a Name, an optional Description and a Guid.</b>
	/// </summary>
	public interface IEmissionRecord {
		/// <summary>
		/// Emissions that occur in the same mixture will have the same Guid in this field.
		/// </summary>
		Guid EmissionId { get; }

		/// <summary>
		/// Represents the equipment, pipe, tank, etc, from which the emission originated, for example, "R13".
		/// </summary>
		IHasIdentity Source { get; }

		/// <summary>
		/// The type of emission, such as batch, continuous, tank, fugitive, or wastewater. 
		/// </summary>
		EmissionType EmissionType { get; }

		/// <summary>
		/// The collection of sources to which this source belongs, for example, "Building 80".
		/// </summary>
		IHasIdentity SourceGroup { get; }

		/// <summary>
		/// The Campaign in which this emission occurred. This is null if the implementing class is a template.
		/// </summary>
		IHasIdentity Campaign { get; }

		/// <summary>
		/// The recipe under which this emission occurred.
		/// </summary>
		IHasIdentity Recipe { get; }

		/// <summary>
		/// The Batch in which this emission occurred. This is null if the implementing class is a template.
		/// </summary>
		IHasIdentity Batch { get; }
		
		/// <summary>
		/// The duration of the batch or recipe in which this emission occurred.
		/// </summary>
		TimeSpan BatchDuration { get; }
		
		/// <summary>
		/// The cycle time of the batch or recipe in which this emission occurred.
		/// </summary>
		TimeSpan BatchCycleTime { get; }

		/// <summary>
		/// The Unit in which this emission occurred.
		/// </summary>
		IHasIdentity Unit { get; }

		/// <summary>
		/// The operation that resulted in this emission, for example, "114.00".
		/// </summary>
		IHasIdentity Operation { get; }
		
		/// <summary>
		/// The duration of the operation in which this emission occurred.
		/// </summary>
		TimeSpan OperationDuration { get; }

		/// <summary>
		/// The substance that was emitted.
		/// </summary>
		MaterialType Substance { get; }

		/// <summary>
		/// The mass of substance that was emitted.
		/// </summary>
		double Mass { get; }

		/// <summary>
		/// The vapor pressure of the substance that was emitted.
		/// </summary>
		double VaporPressure { get; }

		/// <summary>
		/// The time period over which this emission occurred.
		/// </summary>
		ITimePeriod EmissionPeriod { get; }

		/// <summary>
		/// An array of ControlDevices that represent the CDs that this emission *will go through* between emission from the source and emission to the environment.
		/// </summary>
		IControlDevice[] ControlDevices { get; }

		/// <summary>
		/// The MBO Ref of the operation above, for example, "R13 8c". Need to better genericize this one.
		/// </summary>
		string MboReference { get; }
	}

	/// <summary>
	/// Represents a piece of equipment whose job it is to reduce the presence of substances in a discharge stream.
	/// </summary>
	public interface IControlDevice : IHasIdentity {
		/// <summary>
		/// Returns the percent efficiency (0.0&lt;=retval&lt;=1.0) of the device on the substance of interest when in the described mixture stream.
		/// </summary>
		/// <param name="onWhat">The substance of interest.</param>
		/// <param name="wholeStream">The stream of interest.</param>
		/// <returns>The percent efficiency (0.0&lt;=retval&lt;=1.0) of the device</returns>
		double GetEfficiency(Substance onWhat, Mixture wholeStream);
	}

	/// <summary>
	/// An enumeration that shows the type of a particular emission.
	/// </summary>
	public enum EmissionType { 
		/// <summary>
		/// An emission that resulted from a step in a batch process.
		/// </summary>
		Batch,
		/// <summary>
		/// An emission that resulted from a continuous process.
		/// </summary>
		Continuous,
		/// <summary>
		/// An emission that resulted from an operation (such as a fill operation) performed on a storage tank.
		/// </summary>
		Tank,
		/// <summary>
		/// A fugitive emission such as through a flange coupling.
		/// </summary>
		Fugitive,
		/// <summary>
		/// An emission that happened as a result of a wastewater discharge.
		/// </summary>
		Wastewater 
	};
}
