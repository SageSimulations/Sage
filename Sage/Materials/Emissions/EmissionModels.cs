/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using K = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.Constants;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;
using VPC = Highpoint.Sage.Materials.Chemistry.VaporPressure.VaporPressureCalculator;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming

namespace Highpoint.Sage.Materials.Chemistry.Emissions {

	/// <summary>
	/// Characterizes a parameter that is required by an emissions model.
	/// </summary>
	[Serializable]
	public class EmissionParam {
		private string m_name;
		private string m_description;
        /// <summary>
        /// Creates a new instance of the <see cref="T:EmissionParam"/> class for serialization purposes.
        /// </summary>
		protected EmissionParam(){} // for serialization.
		public EmissionParam( string name, string description){
			m_name = name;
			m_description = description;
		}
        /// <summary>
        /// Gets or sets the name of the <see cref="T:EmissionParam"/>.
        /// </summary>
        /// <value>The name of the <see cref="T:EmissionParam"/>.</value>
		public string Name { get { return m_name; } set { m_name = value; } }
        /// <summary>
        /// Gets or sets the description of the <see cref="T:EmissionParam"/>.
        /// </summary>
        /// <value>The description of the <see cref="T:EmissionParam"/>.</value>
		public string Description { get { return m_description; } set { m_description = value; } }
	}


	/// <summary>
	/// This interface is implemented by a class that is capable of acting as an emissions model, computing the
	/// amount of material partitioned off as emissions, as a result of a specified situation.
	/// </summary>
	public interface IEmissionModel {
		/// <summary>
		/// Computes the effects of the emission.
		/// </summary>
		/// <param name="initial">The initial mixture.</param>
		/// <param name="final">The final mixture, after emissions are removed.</param>
		/// <param name="emission">The mixture that is emitted.</param>
		/// <param name="modifyInPlace">If this is true, then the emissions are removed from the initial mixture,
		/// and upon return from the call, the initial mixture will reflect the contents after the emission has taken place.</param>
		/// <param name="parameters">This is a hashtable of name/value pairs that represents all of the parameters necessary
		/// to describe this particular emission model, such as pressures, temperatures, gas sweep rates, etc.</param>
		void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission,
			bool modifyInPlace,
			Hashtable parameters);

		/// <summary>
		/// This is the list of names by which this emission model is specified, such as "Gas Sweep", "Vacuum Dry", etc.
		/// </summary>
		string[] Keys { get; }

		EmissionParam[] Parameters { get; }
		string ModelDescription { get; }

		bool PermitOverEmission { get; set; }
		bool PermitUnderEmission { get; set; }
	}


	/// <summary>
	/// Base class for emission models, with a few helpful auxiliary methods &amp; constant values. It is not required that an
	/// Emission Model derive from this class.
	/// </summary>
	public abstract class EmissionModel : IEmissionModel {

        /// <summary>
        /// Contains constant strings that are to be used as keys for storing emissions parameters into 
        /// the parameters hashtable that holds the data pertinent to an emissions calculation.
        /// </summary>
		public class ParamNames {
			/// <summary>
            /// The key that identifies the Air Leak Duration.
			/// </summary>
            public static string AirLeakDuration_Min = "AirLeakDuration";
            /// <summary>
            /// The key that identifies the Air Leak Rate.
            /// </summary>
            public static string AirLeakRate_KgPerMin = "AirLeakRate";
            /// <summary>
            /// The key that identifies whether the Condenser is enabled.
            /// </summary>
            public static string CondenserEnabled = "CondenserEnabled";
            /// <summary>
            /// The key that identifies the Condenser Temperature.
            /// </summary>
            public static string CondenserTemperature_K = "CondenserTemperature";
            /// <summary>
            /// The key that identifies the ControlTemperature.
            /// </summary>
            public static string ControlTemperature_K = "ControlTemperature";
            /// <summary>
            /// The key that identifies the DesiredEmission.
            /// </summary>
            public static string DesiredEmission = "DesiredEmission";
            /// <summary>
            /// The key that identifies the Material Type Guid To Emit.
            /// </summary>
            public static string MaterialTypeGuidToEmit = "MaterialTypeGuidToEmit";
            /// <summary>
            /// The key that identifies the Material Spec Guid To Emit.
            /// </summary>
            public static string MaterialSpecGuidToEmit = "MaterialSpecGuidToEmit";
            /// <summary>
            /// The key that identifies the Material Fraction To Emit.
            /// </summary>
            public static string MaterialFractionToEmit = "MaterialFractionToEmit";
            /// <summary>
            /// The key that identifies the Material Mass To Emit.
            /// </summary>
            public static string MaterialMassToEmit = "MaterialMassToEmit";
            /// <summary>
            /// The key that identifies the Final Pressure.
            /// </summary>
            public static string FinalPressure_P = "FinalPressure";
            /// <summary>
            /// The key that identifies the Final Temperature.
            /// </summary>
            public static string FinalTemperature_K = "FinalTemperature";
            /// <summary>
            /// The key that identifies the Gas Sweep Duration.
            /// </summary>
            public static string GasSweepDuration_Min = "GasSweepDuration";
            /// <summary>
            /// The key that identifies the Gas Sweep Rate.
            /// </summary>
            public static string GasSweepRate_M3PerMin = "GasSweepRate";
            /// <summary>
            /// The key that identifies the Initial Pressure.
            /// </summary>
            public static string InitialPressure_P = "InitialPressure";
            /// <summary>
            /// The key that identifies the Initial Temperature.
            /// </summary>
            public static string InitialTemperature_K = "InitialTemperature";
            /// <summary>
            /// The key that identifies the Mass Of Dried Product Cake.
            /// </summary>
            public static string MassOfDriedProductCake_Kg = "MassOfDriedProductCake";
            /// <summary>
            /// The key that identifies the Material Guid To Volume Fraction.
            /// </summary>
            public static string MaterialGuidToVolumeFraction = "MaterialGuidToVolumeFraction";
            /// <summary>
            /// The key that identifies the Material To Add.
            /// </summary>
            public static string MaterialToAdd = "MaterialToAdd";
            /// <summary>
            /// The key that identifies the Moles Of Gas Evolved.
            /// </summary>
            public static string MolesOfGasEvolved = "MolesOfGasEvolved";
            /// <summary>
            /// The key that identifies the System Pressure.
            /// </summary>
            public static string SystemPressure_P = "SystemPressure";
            /// <summary>
            /// The key that identifies the Vacuum System Pressure.
            /// </summary>
            public static string VacuumSystemPressure_P = "VacuumSystemPressure";
            /// <summary>
            /// The key that identifies the Vessel Volume.
            /// </summary>
            public static string VesselVolume_M3 = "VesselVolume";
            /// <summary>
            /// The key that identifies the Fill Volume.
            /// </summary>
            public static string FillVolume_M3 = "FillVolume";
		}

		/// <summary>
		/// Determines which equation set is used by the emissions model. Default is CTG.
		/// </summary>
		public enum EquationSet {
			/// <summary>
			/// 1978 Control Technique Guidelines
			/// </summary>
			CTG,
			/// <summary>
			/// 1998 Pharmaceutical Maximum Achievable Control Technique guidelines.
			/// </summary>
			MACT 
		}


		/// <summary>
		/// Useful constants for emission model computations.
		/// </summary>
		public class Constants : Chemistry.Constants {
			/// <summary>
			/// Multiply a double representing the number of pounds (avoirdupois) of a substance by this, to get kilograms.
			/// </summary>
			public static double KgPerPound = 0.453592;
			/// <summary>
			/// Multiply a double representing the number of mm of mercury of pressure, to get pascals.
			/// </summary>
			public static double PascalsPerMmHg = 133.322;
			/// <summary>
			/// Multiply a double representing the number of Bar absolute of pressure, to get pascals.
			/// </summary>
			public static double PascalsPerBar = 100000.0;
			/// <summary>
			/// Multiply a double representing the number of gallons of volume, to get cubic feet.
			/// </summary>
			public static double CubicFtPerGallon = 0.134;
			/// <summary>
			/// Multiply a double representing the number of gallons of volume, to get liters.
			/// </summary>
			public static double LitersPerGallon = 3.7854118;
			/// <summary>
			/// Multiply a double representing the number of cubic feet of volume, to get cubic meters.
			/// </summary>
			public static double CubicFtPerCubicMeter = 35.314667;
			/// <summary>
			/// Multiply a double representing the number of liters of volume, to get cubic meters.
			/// </summary>
			public static double CubicMetersPerLiter = 0.001;
			/// <summary>
			/// Multiply a double representing the number of cubic meters of air at STP to get kg of air.
			/// This is derived from the facts that air's molecular weight (mass-weighted-average) is 28.97 grams per mole
			/// (see http://www.engineeringtoolbox.com/8_679.html) and that air occupies 22.4 liters per mole at STP
			/// (see http://www.epa.gov/nerlesd1/chemistry/ppcp/prefix.htm).<p></p>
			/// ((28.97 g/mole)/(1000 g/kg)) / ((22.4 liters/mole)*1000 liters/m^3) = 1.293304 kg/m^3
			/// </summary>
			public static double AirKgPerCubicMeterStp = 1.293304;

			/// <summary>
			/// The mass-weighted average molecular weight of air. See http://www.engineeringtoolbox.com/8_679.html
			/// </summary>
			public static double MolecularWeightOfAir = 28.97;
			#region Explanation of MolWtOfAir
			//http://www.engineeringtoolbox.com/8_679.html
			//Components in Dry Air
			//The two most dominant components in dry air are Oxygen and Nitrogen.
			//Oxygen has an 16 atomic unit mass and Nitrogen has a 14 atomic units mass.
			//Since both these elements are diatomic in air - O2 and N2, the molecular 
			//mass of Oxygen is 32 and the molecular mass of Nitrogen is 28.
			//
			//Since air is a mixture of gases the total mass can be estimated by adding
			//the weight of all major components as shown below: 
			//
			//Components in Dry Air Volume Ratio compared to Dry Air  Molecular Mass - M(kg/kmol)  Molecular Mass in Air  
			//Oxygen 0.2095 32.00 6.704 
			//Nitrogen 0.7809 28.02 21.88 
			//Carbon Dioxide 0.0003 44.01 0.013 
			//Hydrogen 0.0000005  2.02 0 
			//Argon 0.00933 39.94 0.373 
			//Neon 0.000018 20.18 0 
			//Helium 0.000005 4.00 0 
			//Krypton 0.000001 83.8 0 
			//Xenon 0.09 10-6 131.29 0 
			//Total Molecular Mass of Air 28.97 
			#endregion

		}

		
		#region Private Fields
		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Emissions.ModelParameterDumps");
		private readonly ArrayList m_errMsgs = new ArrayList();
		protected ArrayList ErrorMessages => m_errMsgs;

	    #endregion 

		/// <summary>
		/// Determines whether the engine uses 	CTG (1978 Control Technology Guidelines) or MACT (1998 Pharmaceutical Maximum Achievable Control Technology guidelines) equations for its computation.
		/// </summary>
		public static EquationSet ActiveEquationSet { get; set; } = EquationSet.CTG;

	    /// <summary>
		/// Performs initial bookkeeping in support of determining if an error occurred while reading late-bound parameters for an emissions model.
		/// </summary>
		protected void PrepareToReadLateBoundParameters() {
			m_errMsgs.Clear();
		}

		/// <summary>
		/// Attempts to read a parameter by name from the late-bound parameters list, providing an error message if the parameter is missing.
		/// </summary>
		/// <param name="variable">The double into which the read value is to be placed.</param>
		/// <param name="paramName">The string name of the parameter. Should be one of the EmissionModel.ParamNames entries.</param>
		/// <param name="parameters">The late-bound hashtable.</param>
		protected void TryToRead(ref double variable, string paramName, Hashtable parameters){
			if ( parameters.Contains(paramName) ) {
				variable = (double)parameters[paramName];
			} else {
				variable = double.NaN;
				m_errMsgs.Add("Attempt to read missing parameter, \"" + paramName + "\" from supplied parameters.\r\n");
			}
		}

		/// <summary>
		/// Attempts to read a parameter by name from the late-bound parameters list, providing an error message if the parameter is missing.
		/// </summary>
		/// <param name="variable">The double into which the read value is to be placed.</param>
		/// <param name="paramName">The string name of the parameter. Should be one of the EmissionModel.ParamNames entries.</param>
		/// <param name="parameters">The late-bound hashtable.</param>
		protected void TryToRead(ref Hashtable variable, string paramName, Hashtable parameters){
			if ( parameters.Contains(paramName) ) {
				variable = (Hashtable)parameters[paramName];
			} else {
				variable = null;
				m_errMsgs.Add("Attempt to read missing parameter, \"" + paramName + "\" from supplied parameters.\r\n");
			}
		}

		/// <summary>
		/// Attempts to read a parameter by name from the late-bound parameters list, providing an error message if the parameter is missing.
		/// </summary>
		/// <param name="variable">The double into which the read value is to be placed.</param>
		/// <param name="paramName">The string name of the parameter. Should be one of the EmissionModel.ParamNames entries.</param>
		/// <param name="parameters">The late-bound hashtable.</param>
		protected void TryToRead(ref Mixture variable, string paramName, Hashtable parameters){
			if ( parameters.Contains(paramName) ) {
				variable = (Mixture)parameters[paramName];
			} else {
				variable = null;
				m_errMsgs.Add("Attempt to read missing parameter, \"" + paramName + "\" from supplied parameters.\r\n");
			}
		}

		/// <summary>
		/// This is called after all parameter reads are done, in a late bound model execution. It forms and throws a
		/// MissingParameterException with an appropriate messasge if any of the parameter reads failed.
		/// </summary>
		protected void EvaluateSuccessOfParameterReads(){
			if ( m_errMsgs.Count == 0 ) return;

			string errMsg = "There was an error reading emissions parameters for emissions operation " + GetType().Name + ". The following issues were encountered:\r\n";
			foreach ( string err in m_errMsgs ) errMsg += err;

			throw new Utility.MissingParameterException(errMsg);
		}

	    /// <summary>
	    /// Creates a string that reports the process call, logging it to Trace.
	    /// </summary>
	    /// <param name="subject">The subject.</param>
	    /// <param name="initial">The initial.</param>
	    /// <param name="final">The final.</param>
	    /// <param name="emission">The emission.</param>
	    /// <param name="parameters">The parameters.</param>
	    protected void ReportProcessCall(EmissionModel subject,Mixture initial,Mixture final,Mixture emission,Hashtable parameters){
			if ( s_diagnostics ) { 
				string modelName = subject.Keys[0];
				string opStepName = (string)parameters["SomOpStepName"];
				parameters.Remove("SomOpStepName");
				Trace.WriteLine("\r\n> > > > > > > >  " + opStepName + " [" + modelName + "]");
				Trace.WriteLine("Initial : " + initial.Volume + " liters ( " + initial.Volume/K.LitersPerGallon + " Gallons ) , " + initial.Mass + " kg ( " + (initial.Mass/K.KgPerPound) + " lbm ).");
				foreach ( Substance s in initial.Constituents ) {
					Trace.WriteLine("\t\t" + s.MaterialType + " : " + s.Volume + " liters ( " + s.Volume/K.LitersPerGallon + " Gallons ) , " + s.Mass + " kg ( " + (s.Mass/K.KgPerPound) + " lbm ).");
				}
				Trace.WriteLine("Final   : " + final.Volume + " liters ( " + final.Volume/K.LitersPerGallon + " Gallons ) , " + final.Mass + " kg ( " + (final.Mass/K.KgPerPound) + " lbm ).");
				foreach ( Substance s in final.Constituents ) {
					Trace.WriteLine("\t\t" + s.MaterialType + " : " + s.Volume + " liters ( " + s.Volume/K.LitersPerGallon + " Gallons ) , " + s.Mass + " kg ( " + (s.Mass/K.KgPerPound) + " lbm ).");
				}
				Trace.WriteLine("Emission: " + emission.Volume + " liters ( " + emission.Volume/K.LitersPerGallon + " Gallons ) , " + emission.Mass + " kg ( " + (emission.Mass/K.KgPerPound) + " lbm ).");
				foreach ( Substance s in emission.Constituents ) {
					Trace.WriteLine("\t\t" + s.MaterialType + " : " + s.Volume + " liters ( " + s.Volume/K.LitersPerGallon + " Gallons ) , " + s.Mass + " kg ( " + (s.Mass/K.KgPerPound) + " lbm ).");
				}

				int longestKey = 0;
				foreach ( DictionaryEntry de in parameters ) if ( de.Key.ToString().Length > longestKey ) longestKey = de.Key.ToString().Length;

				foreach ( DictionaryEntry de in parameters ) {
					System.Text.StringBuilder sb = new System.Text.StringBuilder();
					string label = de.Key.ToString();
					sb.Append(label);
					for ( int i = label.Length ; i < longestKey+3 ; i++ ) sb.Append(" ");
					sb.Append(": " + de.Value + " ( " + Convert(de) + " ) ");
					Trace.WriteLine(sb.ToString());
				}
			}
		}

		private string Convert(DictionaryEntry de){
			double d;
			if ( !double.TryParse(de.Value.ToString(),System.Globalization.NumberStyles.Any,null,out d ) ) {
				//Console.WriteLine("Couldn't parse " + de.Value.ToString() + " ( key was " + de.Key + ".)" );
				return "";
			}
			switch (de.Key.ToString()){
				case "VacuumSystemPressure": {
					return "" + (d/K.PascalsPerMmHg) + " mmHg";
				}
				case "InitialPressure": {
					return "" + (d/K.PascalsPerMmHg) + " mmHg";
				}
				case "FinalPressure": {
					return "" + (d/K.PascalsPerMmHg) + " mmHg";
				}
				case "SystemPressure": {
					return "" + (d/K.PascalsPerMmHg) + " mmHg";
				}
				case "FillVolume": {
					return "" + (d/K.LitersPerGallon) + " Gallons";
				}
				case "VesselVolume": {
					return "" + (d/(K.CubicMetersPerLiter*K.LitersPerGallon)) + " Gallons";
				}
				case "CondenserTemperature": {
					return "" + (d+Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
				}
				case "FinalTemperature": {
					return "" + (d+Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
				}
				case "InitialTemperature": {
					return "" + (d+Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
				}
				case "ControlTemperature": {
					return "" + (d+Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
				}
				case "GasSweepDuration": {
					return "" + (d/60.0) + " hours";
				}
				case "AirLeakDuration": {
					return "" + (d/60.0) + " hours";
				}
				case "GasSweepRate": {
					return "" + (d*K.CubicFtPerCubicMeter) + " SCFM";	
				}
				case "AirLeakRate": {
					return "" + (d*(60.0/K.KgPerPound)) + " lbm per hour";
				}
				default: {
					return "";
				}
			}
		}

		private static bool _permitOverEmission;
		private static bool _permitUnderEmission;
		
		/// <summary>
		/// Computes the effects of the emission.
		/// </summary>
		/// <param name="initial">The initial mixture.</param>
		/// <param name="final">The final mixture, after emissions are removed.</param>
		/// <param name="emission">The mixture that is emitted.</param>
		/// <param name="modifyInPlace">If this is true, then the emissions are removed from the initial mixture,
		/// and upon return from the call, the initial mixture will reflect the contents after the emission has taken place.</param>
		/// <param name="parameters">This is a hashtable of name/value pairs that represents all of the parameters necessary
		/// to describe this particular emission model, such as pressures, temperatures, gas sweep rates, etc.</param>
		public abstract void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters);

		/// <summary>
		/// This is the list of names by which this emission model is specified, such as "Gas Sweep", "Vacuum Dry", etc.
		/// </summary>
		public abstract string[] Keys { get; }
		/// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public abstract EmissionParam[] Parameters { get; }
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public abstract string ModelDescription { get; }

        /// <summary>
        /// Gets the system pressure from the parameters hashtable. It may be stored under the PN.SystemPressure_P or failing that, the PN.FinalPressure_P key.
        /// </summary>
        /// <param name="parameters">The parameter hashtable.</param>
        /// <returns></returns>
		protected double GetSystemPressure(Hashtable parameters){
			double systemPressure = double.NaN;
			if ( parameters.Contains(PN.SystemPressure_P) ) {
				systemPressure = (double)parameters[PN.SystemPressure_P];
			} else if ( parameters.Contains(PN.FinalPressure_P) ) {
				//double initPressure = (double)parameters[PN.InitialPressure];
				double finalPressure = (double)parameters[PN.FinalPressure_P];
				systemPressure = finalPressure; //(finalPressure+initPressure)/2.0;
			} else {
				m_errMsgs.Add("Attempt to read missing parameters, either \"" + PN.SystemPressure_P + "\" or \"" + PN.FinalPressure_P + "\" from supplied parameters.\r\n");
			}
			return systemPressure;
		}

        /// <summary>
        /// Gets or sets a value indicating whether this emission model permits over emission - that is, emission of a quantity of material that is greater than what is present in the mixture, if the calculations dictate it.
        /// </summary>
        /// <value><c>true</c> if [permit over emission]; otherwise, <c>false</c>.</value>
		public bool PermitOverEmission { get { return _permitOverEmission; } set { _permitOverEmission = value; } }
        
        /// <summary>
        /// Gets or sets a value indicating whether this emission model permits under emission - that is, emission of a negative quantity, if the calculations dictate it.
        /// </summary>
        /// <value><c>true</c> if [permit under emission]; otherwise, <c>false</c>.</value>
		public bool PermitUnderEmission { get { return _permitUnderEmission; } set { _permitUnderEmission = value; } }


	}

	
	/// <summary>
	/// This model is used to calculate the emissions associated with drying solid product in a dryer 
	/// with no emission control equipment.  Thus, the model assumes that the entire solvent content 
	/// of the wet product cake is emitted to the atmosphere.  The calculation of the emission is then 
	/// a simple calculation based on the expected, or measured, dry product weight and the expected, 
	/// or measured, wet cake LOD (loss on drying).
	/// </summary>
    public class AirDryModel : EmissionModel {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Air Dry model on this API, the parameters hashtable
        /// must include the following entries (see the AirDry(...) method for details):<p></p>
        /// &quot;MassOfDriedProductCake&quot;, &quot;ControlTemperature&quot;, &quot;MaterialGuidToVolumeFraction&quot;.
        /// </summary>
        /// <param name="initial">The initial mixture on which the emission model is to run.</param>
        /// <param name="final">The final mixture that is delivered after the emission model has run.</param>
        /// <param name="emission">The mixture that is evolved in the process of the emission.</param>
        /// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
        /// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
        public override void Process(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            Hashtable parameters) {

            PrepareToReadLateBoundParameters();

            //			double massOfDriedProductCake = (double)parameters[PN.MassOfDriedProductCake_Kg];
            //			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
            //			Hashtable materialGuidToVolumeFraction = (Hashtable)parameters[PN.MaterialGuidToVolumeFraction];

            double massOfDriedProductCake = double.NaN;
            TryToRead(ref massOfDriedProductCake, PN.MassOfDriedProductCake_Kg, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            Hashtable materialGuidToVolumeFraction = null;
            TryToRead(ref materialGuidToVolumeFraction, PN.MaterialGuidToVolumeFraction, parameters);

            EvaluateSuccessOfParameterReads();

            AirDry(initial, out final, out emission, modifyInPlace, massOfDriedProductCake, controlTemperature, materialGuidToVolumeFraction);

            ReportProcessCall(this, initial, final, emission, parameters);

        }


        #region >>> Usability Support <<<
        private static readonly string s_description = "This model is used to calculate the emissions associated with drying solid product in a dryer with no emission control equipment.  Thus, the model assumes that the entire solvent content of the wet product cake is emitted to the atmosphere.  The calculation of the emission is then a simple calculation based on the expected, or measured, dry product weight and the expected, or measured, wet cake LOD (loss on drying).";
        private static readonly EmissionParam[] s_parameters =
            {
								   new EmissionParam(PN.MassOfDriedProductCake_Kg,"Kilogram mass of the post-drying product cake."),
								   new EmissionParam(PN.ControlTemperature_K,"Control (or condenser) temperature, in degrees Kelvin."),
								   new EmissionParam(PN.MaterialGuidToVolumeFraction,"A hashtable with the guids of materialTypes as keys, and the volumeFraction for that material type as values. VolumeFraction is the percent [0.0 to 1.0] of that material type in the offgas.")
							   };

        private static readonly string[] s_keys = { "Air Dry" };
        /// <summary>
        /// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
        /// </summary>
        public override string ModelDescription => s_description;

	    /// <summary>
        /// This is the list of parameters this model uses, and therefore expects as input.
        /// </summary>
        public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
        /// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
        /// that this model is to be called.
        /// </summary>
        public override string[] Keys => s_keys;

	    #endregion

        /// <summary>
        /// This model is used to calculate the emissions associated with drying solid product in a dryer 
        /// with no emission control equipment.  Thus, the model assumes that the entire solvent content 
        /// of the wet product cake is emitted to the atmosphere.  The calculation of the emission is then 
        /// a simple calculation based on the expected, or measured, dry product weight and the expected, 
        /// or measured, wet cake LOD (loss on drying).
        /// </summary>
        /// <param name="initial">The initial mixture that is dried.</param>
        /// <param name="final">An out-param that provides the mixture that results.</param>
        /// <param name="emission">An out-param that provides the emitted mixture.</param>
        /// <param name="modifyInPlace">True if the initial mixture provided is to be modified as a result of this call.</param>
        /// <param name="massOfDriedProductCake">Kilogram mass of the post-drying product cake.</param>
        /// <param name="controlTemperature">Control (or condenser) temperature, in degrees Kelvin.</param>
        /// <param name="materialGuidToVolumeFraction">A hashtable with the guids of materialTypes as keys, and the volumeFraction for that material type as values. VolumeFraction is the percent [0.0 to 1.0] of that material type in the offgas.</param>
        public void AirDry(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double massOfDriedProductCake,
            // ReSharper disable once UnusedParameter.Global
            double controlTemperature,
            Hashtable materialGuidToVolumeFraction
            ) {

            emission = new Mixture(initial.Name + " Air Dry emissions");
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();

            if (initial.Mass > 0.0) {
                double lossOnDryingPct = 1.0 - ( massOfDriedProductCake / initial.Mass );

                double aggDensity = 0.0;
                foreach (Substance substance in mixture.Constituents) {
                    MaterialType mt = substance.MaterialType;
                    double volFrac = (double)materialGuidToVolumeFraction[mt.Guid];
                    double density = substance.Density;
                    aggDensity += volFrac * density;
                }
                double kTerm = massOfDriedProductCake * ( lossOnDryingPct / ( 1.0 - lossOnDryingPct ) ) / aggDensity;

                if (double.IsPositiveInfinity(kTerm))
                    kTerm = 0.0;
                ArrayList substances = new ArrayList(mixture.Constituents);
                foreach (Substance substance in substances) {
                    MaterialType mt = substance.MaterialType;
                    double massOfSubstance = kTerm * (double)materialGuidToVolumeFraction[mt.Guid] * substance.Density;

                    if (!PermitOverEmission)
                        massOfSubstance = Math.Min(substance.Mass, massOfSubstance);
                    if (!PermitUnderEmission)
                        massOfSubstance = Math.Max(0, massOfSubstance);


                    Substance emitted = (Substance)mt.CreateMass(massOfSubstance, substance.Temperature);
                    Substance.ApplyMaterialSpecs(emitted, substance);
                    emission.AddMaterial(emitted);
                }

                foreach (Substance s in emission.Constituents) {
                    mixture.RemoveMaterial(s.MaterialType, s.Mass);
                }
            }
            final = mixture;
        }
    }

	
	/// <summary>
	/// This model is used to calculate emissions from the evacuation (or depressurizing)
	/// of the vessel containing a VOC and a “noncondensable” or “inert” gas.  The model
	/// assumes that the pressure in the vessel decreases linearly with time and that there
	/// is no air leakage into the vessel.  Further, the assumptions are made that the
	/// composition of the VOC mixture does not change during the evacuation and that there
	/// is no temperature change (isothermal expansion).  Finally, the vapor displaced from
	/// the vessel is saturated with the VOC vapor at exit temperature.
	/// </summary>
	public class EvacuateModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Evacuate model on this API, the parameters hashtable
		/// must include the following entries (see the Evacuate(...) method for details):<p></p>
		/// &quot;InitialPressure&quot;, &quot;FinalPressure&quot;, &quot;ControlTemperature&quot;, &quot;VesselVolume&quot;.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){
			
			PrepareToReadLateBoundParameters();

//			double initialPressure = (double)parameters[PN.InitialPressure_P];
//			double finalPressure = (double)parameters[PN.FinalPressure_P];
//			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
//			double vesselVolume = (double)parameters[PN.VesselVolume_M3];

			double initialPressure = double.NaN; TryToRead(ref initialPressure,PN.InitialPressure_P,parameters);
			double finalPressure = double.NaN; TryToRead(ref finalPressure,PN.FinalPressure_P,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);
			double vesselVolume = double.NaN; TryToRead(ref vesselVolume,PN.VesselVolume_M3,parameters);

			EvaluateSuccessOfParameterReads();
			Evacuate(initial,out final, out emission, modifyInPlace, initialPressure, finalPressure, controlTemperature, vesselVolume);

			ReportProcessCall(this,initial,final,emission,parameters);

		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used to calculate emissions from the evacuation (or depressurizing) of the vessel containing a VOC and a “noncondensable” or “inert” gas.  The model assumes that the pressure in the vessel decreases linearly with time and that there is no air leakage into the vessel.  Further, the assumptions are made that the composition of the VOC mixture does not change during the evacuation and that there is no temperature change (isothermal expansion).  Finally, the vapor displaced from the vessel is saturated with the VOC vapor at exit temperature.";
		private static readonly EmissionParam[] s_parameters
			= {
									 new EmissionParam(PN.InitialPressure_P,"The initial pressure of the system, in Pascals."),
									 new EmissionParam(PN.FinalPressure_P,"The final pressure of the system, in Pascals."),
									 new EmissionParam(PN.ControlTemperature_K,"The control, or condenser temperature, in degrees Kelvin."),
									 new EmissionParam(PN.VesselVolume_M3,"The volume of the vessel, in cubic meters.")
								 };

		private static readonly string[] s_keys = {"Evacuate"};
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="initialPressure">The initial pressure of the system, in Pascals.</param>
		/// <param name="finalPressure">The final pressure of the system, in Pascals.</param>
		/// <param name="controlTemperature">The control, or condenser temperature, in degrees Kelvin.</param>
		/// <param name="vesselVolume">The volume of the vessel, in cubic meters.</param>
		public void Evacuate (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace, 
			double initialPressure,
			double finalPressure,
			double controlTemperature,
			double vesselVolume
			){
			double vesselFreeSpace = vesselVolume - (initial.Volume * 0.001 /*convert liters to cubic meters*/) ;
			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			emission = new Mixture(initial.Name + " Evacuation emissions");

			double denom = 0.0;
			ArrayList substances = new ArrayList(mixture.Constituents);
			foreach ( Substance substance in substances ) {
				MaterialType mt = substance.MaterialType;
				double moleFraction = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
				double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt,controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				denom += moleFraction * vaporPressure;					
			}
			denom *= -2;
			denom += finalPressure;
			denom += initialPressure;

			double kTerm =  ( vesselFreeSpace * 2 * (initialPressure - finalPressure))/( Chemistry.Constants.MolarGasConstant * controlTemperature * denom);

			substances = new ArrayList(mixture.Constituents);
			foreach ( Substance substance in substances ) {
				MaterialType mt = substance.MaterialType;
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double massOfSubstance = molWt * molFrac * vaporPressure * kTerm; // grams, since molWt = grams per mole.
				massOfSubstance *= .001; // kilograms per gram.

				if ( !PermitOverEmission ) massOfSubstance = Math.Min(substance.Mass,massOfSubstance);
				if ( !PermitUnderEmission ) massOfSubstance = Math.Max(0,massOfSubstance);

				Substance emitted = (Substance)mt.CreateMass(massOfSubstance,substance.Temperature);
				Substance.ApplyMaterialSpecs(emitted,substance);
				emission.AddMaterial(emitted);
			}

			foreach ( Substance s in emission.Constituents ) {
				mixture.RemoveMaterial( s.MaterialType,s.Mass);
			}
			final = mixture;
		}

	}

	
	/// <summary>
	/// This model is used when any mixture is added to a vessel already containing a liquid or
	/// vapor VOC, and the vapor from that vessel is thereby emitted by displacement.  The model
	/// assumes that the volume of vapor displaced from the vessel is equal to the amount of
	/// material added to the vessel.  In addition, the vapor displaced from the vessel is
	/// saturated with the VOC vapor at the exit temperature.
	/// </summary>
	public class FillModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Fill model on this API, the parameters hashtable
		/// must include the following entries (see the Fill(...) method for details):<p></p>
		/// &quot;MaterialToAdd&quot; and &quot;ControlTemperature&quot;. 
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

//			Mixture materialToAdd = (Mixture)parameters[PN.MaterialToAdd];
//			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
			Mixture materialToAdd = null; TryToRead(ref materialToAdd,PN.MaterialToAdd,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);

			EvaluateSuccessOfParameterReads();

			Fill(initial,out final, out emission,modifyInPlace, materialToAdd, controlTemperature);

			ReportProcessCall(this,initial,final,emission,parameters);

		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used when any mixture is added to a vessel already containing a liquid or vapor VOC, and the vapor from that vessel is thereby emitted by displacement.  The model assumes that the volume of vapor displaced from the vessel is equal to the amount of material added to the vessel.  In addition, the vapor displaced from the vessel is saturated with the VOC vapor at the exit temperature.";
		private static readonly EmissionParam[] s_parameters =
			{
								   new EmissionParam(PN.MaterialToAdd,"The material to be added in the fill operation.The volume property of the material will be used to determine volume."),
								   new EmissionParam(PN.ControlTemperature_K,"The control, or condenser, temperature, in degrees Kelvin.")
							   };

		private static readonly string[] s_keys = {"Fill"};
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="materialToAdd">The material to be added in the fill operation.The volume property of the material will be used to determine volume.</param>
		/// <param name="controlTemperature">The control, or condenser, temperature, in degrees Kelvin.</param>
		public void Fill (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace, 
			Mixture materialToAdd,
			double controlTemperature
			){
			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			emission = new Mixture(initial.Name + " Fill emissions");

			double volumeOfMaterialAdded = materialToAdd.Volume /*, which is in liters*/ * .001 /*, to convert it to m^3.*/;

			ArrayList substances = new ArrayList(mixture.Constituents);
			foreach ( Substance substance in substances ) {
				MaterialType mt = substance.MaterialType;
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double massOfSubstance = volumeOfMaterialAdded * molWt * molFrac * vaporPressure / ( Chemistry.Constants.MolarGasConstant * controlTemperature); // grams, since molWt = grams per mole.
				massOfSubstance *= .001; // kilograms per gram.

				if ( !PermitOverEmission ) massOfSubstance = Math.Min(substance.Mass,massOfSubstance);
				if ( !PermitUnderEmission ) massOfSubstance = Math.Max(0,massOfSubstance);

				Substance emitted = (Substance)mt.CreateMass(massOfSubstance,substance.Temperature);
				Substance.ApplyMaterialSpecs(emitted,substance);
				emission.AddMaterial(emitted);
			}

			foreach ( Substance s in materialToAdd.Constituents ) {
				mixture.AddMaterial(s);
			}

			foreach ( Substance s in emission.Constituents ) {
				mixture.RemoveMaterial(s.MaterialType,s.Mass);
			}

			final = mixture;
		}

	}

	
	/// <summary>
	/// This model is used to calculate the emissions associated with the generation
	/// of a non-condensable gas as the result of a chemical reaction.  The model assumes
	/// that the gas is exposed to the VOC, becomes saturated with the VOC vapor at the
	/// exit temperature, and leaves the system.  The model also assumes that the system
	/// pressure is 760 mmHg, atmospheric pressure.  This model is identical to the Gas
	/// Sweep model, except that the non-condensable sweep gas (usually nitrogen) is replaced
	/// in this model by a non-condensable gas generated in situ. It is important to note that
	/// if the generated gas is itself a VOS, non-VOS or TVOS, then the emission of this gas
	/// must be accounted for by a separate model, usually the Mass Balance model.
	/// <p>For example, if n-butyllithium is used in a chemical reaction and generates butane
	/// gas as a byproduct, the evolution of butane gas causes emissions of the VOC present in
	/// the system.  These emissions can be modeled by the Gas Evolution model (to account for
	/// the emission of the VOC vapor which saturates the butane gas) and the Mass Balance
	/// model (to account for the emission of the VOC butane).</p>
	/// </summary>
	public class GasEvolutionModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Gas Evolution model on this API, the parameters hashtable
		/// must include the following entries (see the GasEvolution(...) method for details):<p></p>
		/// &quot;MolesOfGasEvolved&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
		/// is no entry under &quot;SystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
		/// and &quot;FinalPressure&quot; and uses their average.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

//			double nMolesEvolved = (double)parameters[PN.MolesOfGasEvolved];
//			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
//			double systemPressure = GetSystemPressure(parameters);

			double nMolesEvolved = double.NaN; TryToRead(ref nMolesEvolved,PN.MolesOfGasEvolved,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);
			double systemPressure = GetSystemPressure(parameters);
			
			EvaluateSuccessOfParameterReads();

			GasEvolution(initial,out final,out emission,modifyInPlace,nMolesEvolved,controlTemperature, systemPressure);
		
			ReportProcessCall(this,initial,final,emission,parameters);

		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used to calculate the emissions associated with the generation of a non-condensable gas as the result of a chemical reaction.  The model assumes that the gas is exposed to the VOC, becomes saturated with the VOC vapor at the exit temperature, and leaves the system.  The model also assumes that the system pressure is 760 mmHg, atmospheric pressure.  This model is identical to the Gas Sweep model, except that the non-condensable sweep gas (usually nitrogen) is replaced in this model by a non-condensable gas generated in situ.\r\n\r\nIt is important to note that if the generated gas is itself a VOS, non-VOS or TVOS, then the emission of this gas must be accounted for by a separate model, usually the Mass Balance model. For example, if n-butyllithium is used in a chemical reaction and generates butane gas as a byproduct, the evolution of butane gas causes emissions of the VOC present in the system.  These emissions can be modeled by the Gas Evolution model (to account for the emission of the VOC vapor which saturates the butane gas) and the Mass Balance model (to account for the emission of the VOC butane).";
		private static readonly EmissionParam[] s_parameters =
			{
								   new EmissionParam(PN.MolesOfGasEvolved,"The number of moles of gas evolved."),
								   new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees kelvin."),
								   new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\".")
							   };

		private static readonly string[] s_keys = {"Gas Evolution"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is used to calculate the emissions associated with the generation
		/// of a non-condensable gas as the result of a chemical reaction.  The model assumes
		/// that the gas is exposed to the VOC, becomes saturated with the VOC vapor at the
		/// exit temperature, and leaves the system.  The model also assumes that the system
		/// pressure is 760 mmHg, atmospheric pressure.  This model is identical to the Gas
		/// Sweep model, except that the non-condensable sweep gas (usually nitrogen) is replaced
		/// in this model by a non-condensable gas generated in situ. It is important to note that
		/// if the generated gas is itself a VOS, non-VOS or TVOS, then the emission of this gas
		/// must be accounted for by a separate model, usually the Mass Balance model.
		/// <p>For example, if n-butyllithium is used in a chemical reaction and generates butane
		/// gas as a byproduct, the evolution of butane gas causes emissions of the VOC present in
		/// the system.  These emissions can be modeled by the Gas Evolution model (to account for
		/// the emission of the VOC vapor which saturates the butane gas) and the Mass Balance
		/// model (to account for the emission of the VOC butane).</p>
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="nMolesEvolved">The number of moles of gas evolved.</param>
		/// <param name="controlTemperature">The control or condenser temperature, in degrees kelvin.</param>
		/// <param name="systemPressure">The pressure of the system (or vessel) in Pascals.</param>
		public void GasEvolution (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace, 
			double nMolesEvolved,
			double controlTemperature,
			double systemPressure
			){
			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			emission = new Mixture(initial.Name + " GasEvolution emissions");

			double spp = VPC.SumOfPartialPressures(mixture,controlTemperature);

			foreach ( Substance substance in mixture.Constituents ) {
				MaterialType mt = substance.MaterialType;
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double massOfSubstance = nMolesEvolved * molWt * molFrac * vaporPressure / ( systemPressure - spp );
				// At this point, massOfSubstance is in grams (since molWt is in grams per mole)
				massOfSubstance /= 1000;
				// now, mass is in kg.

				if ( !PermitOverEmission ) massOfSubstance = Math.Min(substance.Mass,massOfSubstance);
				if ( !PermitUnderEmission ) massOfSubstance = Math.Max(0,massOfSubstance);

				Substance emitted = (Substance)mt.CreateMass(massOfSubstance,controlTemperature+Chemistry.Constants.KELVIN_TO_CELSIUS);
				Substance.ApplyMaterialSpecs(emitted,substance);
				emission.AddMaterial(emitted);
			}

			foreach ( Substance s in emission.Constituents ) {
				mixture.RemoveMaterial(s.MaterialType,s.Mass);
			}
			final = mixture;
		}
	}

	
	/// <summary>
	/// This model is used to calculate the emissions associated with sweeping or purging
	/// a vessel or other piece of equipment with a non-condensable gas (nitrogen).
	/// The model assumes that the sweep gas enters the system at 25°C, becomes saturated
	/// with the VOC vapor at the exit temperature, and leaves the system.
	/// </summary>
	public class GasSweepModel : EmissionModel {
		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Gas Sweep model on this API, the parameters hashtable
		/// must include the following entries (see the GasSweep(...) method for details):<p></p>
		/// &quot;GasSweepRate&quot;, &quot;GasSweepDuration&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
		/// is no entry under &quot;SystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
		/// and &quot;FinalPressure&quot; and uses their average.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

//			double gasSweepRate = (double)parameters["GasSweepRate"];
//			double gasSweepDuration = (double)parameters["GasSweepDuration"];
//			double controlTemperature = (double)parameters["ControlTemperature"];
//			double systemPressure = GetSystemPressure(parameters);

			double gasSweepRate = double.NaN; TryToRead(ref gasSweepRate,PN.GasSweepRate_M3PerMin,parameters);
			double gasSweepDuration = double.NaN; TryToRead(ref gasSweepDuration,PN.GasSweepDuration_Min,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);
			double systemPressure = GetSystemPressure(parameters);

			EvaluateSuccessOfParameterReads();

			GasSweep(initial,out final,out emission,modifyInPlace,gasSweepRate,gasSweepDuration,controlTemperature,systemPressure);
			
			ReportProcessCall(this,initial,final,emission,parameters);
		
		}


		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used to calculate the emissions associated with sweeping or purging a vessel or other piece of equipment with a non-condensable gas (nitrogen). The model assumes that the sweep gas enters the system at 25°C, becomes saturated with the VOC vapor at the exit temperature, and leaves the system.";
		private static readonly EmissionParam[] s_parameters =
			{
								   new EmissionParam(PN.GasSweepRate_M3PerMin,"The gas sweep rate, in cubic meters per time unit."),
								   new EmissionParam(PN.GasSweepDuration_Min,"The gas sweep duration, in minutes."),
								   new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
								   new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\".")
							   };

		private static readonly string[] s_keys = {"Gas Sweep"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is used to calculate the emissions associated with sweeping or purging
		/// a vessel or other piece of equipment with a non-condensable gas (nitrogen).
		/// The model assumes that the sweep gas enters the system at 25°C, becomes saturated
		/// with the VOC vapor at the exit temperature, and leaves the system.
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="gasSweepRate">The gas sweep rate, in cubic meters per time unit.</param>
		/// <param name="gasSweepDuration">The gas sweep duration, in matching time units.</param>
		/// <param name="controlTemperature">The control or condenser temperature, in degrees Kelvin.</param>
		/// <param name="systemPressure">The system (vessel) pressure, in Pascals.</param>
		public void GasSweep (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace, 
			double gasSweepRate, /*meters^3 per minute*/
			double gasSweepDuration, /*minutes*/
			double controlTemperature,
			double systemPressure
			){
			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			emission = new Mixture(initial.Name + " GasSweep emissions");

			double spp = VPC.SumOfPartialPressures(mixture,controlTemperature);
			/* double gasVolume = gasSweepDuration * gasSweepRate;
			gas volume is now in cubic meters*/
			double constantPart = ( systemPressure * gasSweepDuration * gasSweepRate ) / ( Chemistry.Constants.MolarGasConstant * (systemPressure - spp ) * controlTemperature );

			foreach ( Substance substance in mixture.Constituents ) {
				MaterialType mt = substance.MaterialType;
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double massOfSubstance = molWt * molFrac * vaporPressure * constantPart;
				// At this point, massOfSubstance is in grams (since molWt is in grams per mole)
				massOfSubstance /= 1000;
				// now, massOfSubstance is in kg.

				if ( !PermitOverEmission ) massOfSubstance = Math.Min(substance.Mass,massOfSubstance);
				if ( !PermitUnderEmission ) massOfSubstance = Math.Max(0,massOfSubstance);

				Substance emitted = (Substance)mt.CreateMass(massOfSubstance,controlTemperature+Chemistry.Constants.KELVIN_TO_CELSIUS);
				Substance.ApplyMaterialSpecs(emitted,substance);
				emission.AddMaterial(emitted);
			}

			double saturation = 1.00;
			if ( ActiveEquationSet.Equals(EquationSet.MACT) ) {
				if ( gasSweepRate > /*100 scfm = */2.831685 /* cubic meters per minute */ ) saturation *= 0.25;
			}


			foreach ( Substance s in emission.Constituents ) {
				mixture.RemoveMaterial(s.MaterialType,s.Mass*saturation);
			}
			final = mixture;
		}
	}

	
	/// <summary>
	/// This model is used to calculate the emissions associated with the heating
	/// of a vessel or other piece of equipment containing a VOC and a non-condensable
	/// gas (nitrogen or air).  The model assumes that the non-condensable gas,
	/// saturated with the VOC mixture, is emitted from the vessel because of (1) the
	/// expansion of the gas upon heating and (2) an increase in the VOC vapor pressure.
	/// The emitted gas is saturated with the VOC mixture at the exit temperature, the
	/// condenser or receiver temperature.
	/// </summary>
	public class HeatModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Heat model on this API, the parameters hashtable
		/// must include the following entries (see the Heat(...) method for details):<p></p>
		/// &quot;ControlTemperature&quot;, &quot;InitialTemperature&quot;, &quot;FinalTemperature&quot;&quot;SystemPressure&quot; and &quot;FreeSpace&quot;. If there
		/// is no entry under &quot;SystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
		/// and &quot;FinalPressure&quot; and uses their average.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

			double controlTemperature = double.NaN; TryToRead(ref controlTemperature, PN.ControlTemperature_K,parameters);
			double initialTemperature = double.NaN; TryToRead(ref initialTemperature, PN.InitialTemperature_K,parameters);
			double finalTemperature = double.NaN; TryToRead(ref finalTemperature, PN.FinalTemperature_K,parameters);
			double systemPressure = GetSystemPressure(parameters);
			double vesselVolume = double.NaN; TryToRead(ref vesselVolume, PN.VesselVolume_M3,parameters);

			EvaluateSuccessOfParameterReads();

			Heat(initial,out final,out emission,modifyInPlace,controlTemperature,initialTemperature,finalTemperature,systemPressure,vesselVolume);
		
			ReportProcessCall(this,initial,final,emission,parameters);
		}


		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used to calculate the emissions associated with the heating of a vessel or other piece of equipment containing a VOC and a non-condensable gas (nitrogen or air).  The model assumes that the non-condensable gas, saturated with the VOC mixture, is emitted from the vessel because of (1) the expansion of the gas upon heating and (2) an increase in the VOC vapor pressure. The emitted gas is saturated with the VOC mixture at the exit temperature, the condenser or receiver temperature.";
		private static readonly EmissionParam[] s_parameters =
			{
				new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
				new EmissionParam(PN.InitialTemperature_K,"The initial temerature of the mixture in degrees Kelvin."),
				new EmissionParam(PN.FinalTemperature_K,"The final temperature of the mixture in degrees Kelvin."),
				new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\"."),
				new EmissionParam(PN.VesselVolume_M3,"The volume of the vessel, in cubic meters.")
			};
		private static readonly string[] s_keys = {"Heat"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is used to calculate the emissions associated with the heating
		/// of a vessel or other piece of equipment containing a VOC and a non-condensable
		/// gas (nitrogen or air).  The model assumes that the non-condensable gas,
		/// saturated with the VOC mixture, is emitted from the vessel because of (1) the
		/// expansion of the gas upon heating and (2) an increase in the VOC vapor pressure.
		/// The emitted gas is saturated with the VOC mixture at the exit temperature, the
		/// condenser or receiver temperature.
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="controlTemperature">The control or condenser temperature, in degrees Kelvin.</param>
		/// <param name="initialTemperature">The initial temerature of the mixture in degrees Kelvin.</param>
		/// <param name="finalTemperature">The final temperature of the mixture in degrees Kelvin.</param>
		/// <param name="systemPressure">The pressure of the system (vessel) in Pascals.</param>
        /// <param name="vesselVolume">The volume of the vessel, in cubic meters.</param>
		public void Heat (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			double controlTemperature,
			double initialTemperature,
			double finalTemperature, /* in degreesK */
			double systemPressure, /* in Pascals. */
			double vesselVolume /* in cubic meters. */
			){

			double freeSpace = vesselVolume - (initial.Volume * 0.001 /*convert liters to cubic meters*/) ;

			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			emission = new Mixture(initial.Name + " Heat emissions");

			double dsppi = systemPressure - VPC.SumOfPartialPressures(mixture,initialTemperature);
			double dsppf = systemPressure - VPC.SumOfPartialPressures(mixture,finalTemperature);
			double dsppc = systemPressure - VPC.SumOfPartialPressures(mixture,controlTemperature);

			double factor = ( freeSpace / dsppc ) * ((dsppi/initialTemperature) - (dsppf/finalTemperature));

			foreach ( Substance substance in mixture.Constituents ) {
				MaterialType mt = substance.MaterialType;
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double massOfSubstance = molWt * molFrac * vaporPressure * factor / Chemistry.Constants.MolarGasConstant;
				// At this point, massOfSubstance is in grams (since molWt is in grams per mole)
				massOfSubstance /= 1000;
				// now, massOfSubstance is in kg.

				if ( !PermitOverEmission ) massOfSubstance = Math.Min(substance.Mass,massOfSubstance);
				if ( !PermitUnderEmission ) massOfSubstance = Math.Max(0,massOfSubstance);

				Substance emitted = (Substance)mt.CreateMass(massOfSubstance,controlTemperature+Chemistry.Constants.KELVIN_TO_CELSIUS);
				Substance.ApplyMaterialSpecs(emitted,substance);
				emission.AddMaterial(emitted);
			}

			foreach ( Substance s in emission.Constituents ) {
				mixture.RemoveMaterial(s.MaterialType,s.Mass);
			}
			final = mixture;
		}
	}

	
	/// <summary>
	/// This model is used whenever an emission of a known mixture occurs
	/// during a particular operation.  The user must specify the mixture
	/// containing emission.  As an example, the butane emission from
	/// the reaction of n-butyllithium could be specified by using this model.
	/// However, the VOC emission caused by the evolution and emission of the
	/// butane would have to be calculated by the Gas Evolve model.
	/// </summary>
	public class MassBalanceModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Mass Balance model on this API, the parameters hashtable
		/// must include the following entry (see the MassBalance(...) method for details):<p></p>
		/// &quot;DesiredEmission&quot;.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			Mixture desiredEmission = (Mixture)parameters[PN.DesiredEmission];
			PrepareToReadLateBoundParameters();
			if ( desiredEmission == null ) {
				if ( parameters.ContainsKey(PN.MaterialTypeGuidToEmit) ) {
					Guid materialTypeGuid = (Guid)parameters[PN.MaterialTypeGuidToEmit];
					Guid materialSpecGuid = Guid.Empty;
					if ( parameters.ContainsKey(PN.MaterialSpecGuidToEmit) ) {
						materialSpecGuid = (Guid)parameters[PN.MaterialSpecGuidToEmit];
					}

					// Emission by fraction.
					if ( parameters.ContainsKey(PN.MaterialFractionToEmit) ) {
						double fraction = (double)parameters[PN.MaterialFractionToEmit];
						desiredEmission = new Mixture();
						foreach ( IMaterial material in initial.Constituents ) {
							if ( MaterialMatchesTypeAndSpec((Substance)material,materialTypeGuid,materialSpecGuid) ){
								Substance s = (Substance)material.Clone();
								s = s.Remove(s.Mass*fraction);
								desiredEmission.AddMaterial(s);
							}
						}
					// Emission by mass.
					} else if ( parameters.ContainsKey(PN.MaterialMassToEmit) ) {
						double massOut = (double)parameters[PN.MaterialMassToEmit];
						desiredEmission = new Mixture();
						foreach ( IMaterial material in initial.Constituents ) {
							if ( MaterialMatchesTypeAndSpec((Substance)material,materialTypeGuid,materialSpecGuid) ){
								Substance s = (Substance)material.Clone();
								s = s.Remove(massOut);
								desiredEmission.AddMaterial(s);
							}
						}
					} else {
						ErrorMessages.Add("Attempt to read missing parameters, \"" + PN.MaterialFractionToEmit + "\" (a double, [0.0->1.0]) or \"" + PN.MaterialMassToEmit + "\"  (a double representing kilograms) from supplied parameters. The parameter \"" + PN.DesiredEmission + "\" was also missing, although this is okay if the others are provided.\r\n");
					}
				} else {
					ErrorMessages.Add("Attempt to read missing parameter \"" + PN.MaterialTypeGuidToEmit + "\" (a double, [0.0->1.0]) or \"MaterialTypeGuidToEmit\" (a Guid representing a material type) from supplied parameters. The parameter \"" + PN.DesiredEmission + "\" was also missing, although this is okay if the others are provided. The parameter \"" + PN.DesiredEmission + "\" was also missing, although this is okay if the others are provided.\r\n");
				}
			}
			EvaluateSuccessOfParameterReads();

			MassBalance(initial, out final, out emission, modifyInPlace, desiredEmission);

			ReportProcessCall(this,initial,final,emission, parameters);
		}

		private bool MaterialMatchesTypeAndSpec(Substance s, Guid typeGuid, Guid specGuid){
			if ( !s.MaterialType.Guid.Equals(typeGuid) ) return false;
			if ( specGuid.Equals(Guid.Empty) || s.GetMaterialSpec(specGuid) > 0.0 ) return true;
			return false;
		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used whenever an emission of a known mixture occurs during a particular operation.  The user must specify the materials emitted from the initial mixture in the form of a mixture.  As an example, the butane emission from the reaction of n-butyllithium could be specified by using this model. However, the VOC emission caused by the evolution and emission of the butane would have to be calculated by the Gas Evolve model.";
		private static readonly EmissionParam[] s_parameters =
			{ new EmissionParam(PN.DesiredEmission,"The mixture that is to be removed from the initial mixture as emission.") };
		private static readonly string[] s_keys = {"Mass Balance"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is used whenever an emission of a known mixture occurs
		/// during a particular operation.  The user must specify the mixture
		/// containing emission.  As an example, the butane emission from
		/// the reaction of n-butyllithium could be specified by using this model.
		/// However, the VOC emission caused by the evolution and emission of the
		/// butane would have to be calculated by the Gas Evolve model.
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="desiredEmission">The mixture that is to be removed from the initial mixture as emission.</param>
		public void MassBalance (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Mixture desiredEmission
			){
			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();

			emission = (Mixture)desiredEmission.Clone();

			foreach ( Substance s in emission.Constituents ) {

				if ( !PermitUnderEmission && emission.ContainedMassOf(s.MaterialType) < 0 ) emission.RemoveMaterial(s.MaterialType);
				if ( !PermitOverEmission ) {
					// The emission cannot contain more than the quantity in the mixture.
					double overMass = Math.Min(0,emission.ContainedMassOf(s.MaterialType)-mixture.ContainedMassOf(s.MaterialType));
					if ( overMass > 0 ) emission.RemoveMaterial(s.MaterialType,overMass);
				}


				mixture.RemoveMaterial(s.MaterialType,emission.ContainedMassOf(s.MaterialType));
			}
			final = mixture;
		}
	}

	
	/// <summary>
	/// This model is a placeholder for operations that cause no emissions.
	/// </summary>
	public class NoEmissionModel : EmissionModel {
		
		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>The NoEmissions model is included for completeness, and has no entries that are
		/// required to be present in the parameters hashtable.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			NoEmission(initial, out final, out emission, modifyInPlace);
			
			ReportProcessCall(this,initial,final,emission,parameters);
		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is a placeholder for operations that cause no emissions.";
		private static readonly EmissionParam[] s_parameters = {};
		private static readonly string[] s_keys = {"No Emissions"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is a placeholder for operations that cause no emissions.
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		public void NoEmission (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission,
			bool modifyInPlace
			){
			final = modifyInPlace?initial:(Mixture)initial.Clone();
		    emission = new Mixture(initial.Name + " emissions") {Temperature = initial.Temperature};

			}
	}

	
	/// <summary>
	/// This model is used to calculate the emissions associated with vacuum
	/// operations.  The model assumes that air leaks into the system under
	/// vacuum, is exposed to the VOC, becomes saturated with the VOC vapor at
	/// the exit temperature, and leaves the system via the vacuum source.
	/// <p>The most important input parameter to this model is the leak rate of
	/// the air into the system.  If the leak rate for a particular piece of equipment
	/// has been measured, then this leak rate can be used.  On the other hand, if no
	/// leak rate information is available, EmitNJ will estimate the leak rate using
	/// the system volume entered by the user and industry standard leak rates for 
	/// 'commercially tight' systems.</p>
	/// </summary>
	public class VacuumDistillationModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Vacuum Distillation model on this API, the parameters hashtable
		/// must include the following entries (see the VacuumDistillation(...) method for details):<p></p>
		/// &quot;AirLeakRate&quot;, &quot;AirLeakDuration&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
		/// is no entry under &quot;VacuumSystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
		/// and &quot;FinalPressure&quot; and uses their average.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

//			double airLeakRate = (double)parameters[PN.AirLeakRate_KgPerMin];
//			double airLeakDuration = (double)parameters[PN.AirLeakDuration_Min];
//			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
//			double vacuumSystemPressure = (double)parameters[PN.VacuumSystemPressure_P];

			double airLeakRate = double.NaN; TryToRead(ref airLeakRate,PN.AirLeakRate_KgPerMin,parameters);
			double airLeakDuration = double.NaN; TryToRead(ref airLeakDuration,PN.AirLeakDuration_Min,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);
			double vacuumSystemPressure = double.NaN; TryToRead(ref vacuumSystemPressure,PN.VacuumSystemPressure_P,parameters);

			EvaluateSuccessOfParameterReads();

			VacuumDistillation(initial,out final,out emission,modifyInPlace,controlTemperature,vacuumSystemPressure,airLeakRate,airLeakDuration);
		
			ReportProcessCall(this,initial,final,emission,parameters);

		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used to calculate the emissions associated with vacuum operations.  The model assumes that air leaks into the system under vacuum, is exposed to the VOC, becomes saturated with the VOC vapor at the exit temperature, and leaves the system via the vacuum source. \r\nThe most important input parameter to this model is the leak rate of the air into the system.  If the leak rate for a particular piece of equipment has been measured, then this leak rate can be used.  On the other hand, if no leak rate information is available, EmitNJ will estimate the leak rate using the system volume entered by the user and industry standard leak rates for  'commercially tight' systems.";
		private static readonly EmissionParam[] s_parameters 
			= {
									 new EmissionParam(PN.AirLeakRate_KgPerMin,"Air leak rate into the system, in kilograms per time unit."),
									 new EmissionParam(PN.AirLeakDuration_Min,"Air leak rate into the system, in the AirLeakRate's time units."),
									 new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
									 new EmissionParam(PN.VacuumSystemPressure_P,"The pressure to which the vacuum system drives the vessel, in Pascals.")
								 };
		private static readonly string[] s_keys = {"Vacuum Distillation","Vacuum Distill"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is used to calculate the emissions associated with vacuum
		/// operations.  The model assumes that air leaks into the system under
		/// vacuum, is exposed to the VOC, becomes saturated with the VOC vapor at
		/// the exit temperature, and leaves the system via the vacuum source.
		/// <p>The most important input parameter to this model is the leak rate of
		/// the air into the system.  If the leak rate for a particular piece of equipment
		/// has been measured, then this leak rate can be used.  On the other hand, if no
		/// leak rate information is available, EmitNJ will estimate the leak rate using
		/// the system volume entered by the user and industry standard leak rates for 
		/// 'commercially tight' systems.</p>
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="controlTemperature">In degrees kelvin.</param>
		/// <param name="vacuumSystemPressure">In Pascals.</param>
		/// <param name="airLeakRate">In kilograms per time unit.</param>
		/// <param name="airLeakDuration">In matching time units.</param>
		public void VacuumDistillation (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			double controlTemperature,
			double vacuumSystemPressure, /* in Pascals. */
			double airLeakRate, /* in kilograms per time unit. */
			double airLeakDuration /* in matching time unit. */
			){
			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			emission = new Mixture(initial.Name + " Vacuum Distillation emissions");

			double kilogramsOfAir = airLeakDuration * airLeakRate;

			double dsppc = vacuumSystemPressure - VPC.SumOfPartialPressures(mixture,controlTemperature);

			double factor = (kilogramsOfAir/K.MolecularWeightOfAir)*(1.0/dsppc);

			foreach ( Substance substance in mixture.Constituents ) {
				MaterialType mt = substance.MaterialType;
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double massOfSubstance = molWt * molFrac * vaporPressure * factor;

				if ( !PermitOverEmission ) massOfSubstance = Math.Min(substance.Mass,massOfSubstance);
				if ( !PermitUnderEmission ) massOfSubstance = Math.Max(0,massOfSubstance);

				Substance emitted = (Substance)mt.CreateMass(massOfSubstance,controlTemperature+Chemistry.Constants.KELVIN_TO_CELSIUS);
				Substance.ApplyMaterialSpecs(emitted,substance);
				emission.AddMaterial(emitted);
			}

			foreach ( Substance s in emission.Constituents ) {
				mixture.RemoveMaterial(s.MaterialType,s.Mass);
			}
			final = mixture;
		}
	}

	
	/// <summary>
	/// This model is used to calculate the emissions associated with vacuum 
	/// distillation.  The calculation of the emission from the operation is 
	/// identical to that for the Vacuum Distill model.  This model incorporates 
	/// the effect of the vacuum jet scrubbers into the emission calculation.  
	/// The vacuum jet scrubbers are used to condense the steam exiting from the 
	/// vacuum jet but they also condense solvent vapors through direct contact 
	/// heat exchange.
	/// <p>The assumptions of the new model are similar to that of the existing 
	/// vacuum distill model.  Air leaks into the system under vacuum and becomes 
	/// saturated with solvent vapors.  With the vacuum distill model it is 
	/// assumed that condensation of some fraction of these vapors occurs in the
	/// primary condenser and any uncondensed vapor is exhausted to the atmosphere
	/// (via control devices, if any).  With this model, the vacuum jet scrubber
	/// acts as the final control device, assuming that a vacuum jet is being used
	/// to evacuate the system.  The vacuum jet scrubber condenses vapors, which
	/// remain uncondensed by the primary condenser, through direct contact heat
	/// exchange with the scrubber water.</p>
	/// </summary>
	public class VacuumDistillationWScrubberModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Vacuum Distillation With Scrubber model on this API, the parameters hashtable
		/// must include the following entries (see the VacuumDistillationWScrubber(...) method for details):<p></p>
		/// &quot;AirLeakRate&quot;, &quot;AirLeakDuration&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
		/// is no entry under &quot;SystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
		/// and &quot;FinalPressure&quot; and uses their average.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

//			double airLeakRate = (double)parameters[PN.AirLeakRate_KgPerMin];
//			double airLeakDuration = (double)parameters[PN.AirLeakDuration_Min];
//			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
//			double systemPressure = GetSystemPressure(parameters);
			
			double airLeakRate = double.NaN; TryToRead(ref airLeakRate,PN.AirLeakRate_KgPerMin,parameters);
			double airLeakDuration = double.NaN; TryToRead(ref airLeakDuration,PN.AirLeakDuration_Min,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);
			double systemPressure = GetSystemPressure(parameters);
			
			EvaluateSuccessOfParameterReads();

			VacuumDistillationWScrubber(initial,out final,out emission,modifyInPlace,controlTemperature,systemPressure,airLeakRate,airLeakDuration);

			ReportProcessCall(this,initial,final,emission,parameters);

		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used to calculate the emissions associated with vacuum distillation.  The calculation of the emission from the operation is identical to that for the Vacuum Distill model.  This model incorporates  the effect of the vacuum jet scrubbers into the emission calculation.  The vacuum jet scrubbers are used to condense the steam exiting from the  vacuum jet but they also condense solvent vapors through direct contact heat exchange.\r\nThe assumptions of the new model are similar to that of the existing vacuum distill model.  Air leaks into the system under vacuum and becomes saturated with solvent vapors.  With the vacuum distill model it is assumed that condensation of some fraction of these vapors occurs in the primary condenser and any uncondensed vapor is exhausted to the atmosphere (via control devices, if any).  With this model, the vacuum jet scrubber acts as the final control device, assuming that a vacuum jet is being used to evacuate the system.  The vacuum jet scrubber condenses vapors, which remain uncondensed by the primary condenser, through direct contact heat exchange with the scrubber water.";
		private static readonly EmissionParam[] s_parameters 
			= {
									 new EmissionParam(PN.AirLeakRate_KgPerMin,"Air leak rate into the system, in kilograms per time unit."),
									 new EmissionParam(PN.AirLeakDuration_Min,"Air leak rate into the system, in the AirLeakRate's time units."),
									 new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
									 new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\".")
								 };
		private static readonly string[] s_keys = {"Vacuum Distillation With Scrubber","Vacuum Distillation w/ Scrubber"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is used to calculate the emissions associated with vacuum 
		/// distillation.  The calculation of the emission from the operation is 
		/// identical to that for the Vacuum Distill model.  This model incorporates 
		/// the effect of the vacuum jet scrubbers into the emission calculation.  
		/// The vacuum jet scrubbers are used to condense the steam exiting from the 
		/// vacuum jet but they also condense solvent vapors through direct contact 
		/// heat exchange.
		/// <p>The assumptions of the new model are similar to that of the existing 
		/// vacuum distill model.  Air leaks into the system under vacuum and becomes 
		/// saturated with solvent vapors.  With the vacuum distill model it is 
		/// assumed that condensation of some fraction of these vapors occurs in the
		/// primary condenser and any uncondensed vapor is exhausted to the atmosphere
		/// (via control devices, if any).  With this model, the vacuum jet scrubber
		/// acts as the final control device, assuming that a vacuum jet is being used
		/// to evacuate the system.  The vacuum jet scrubber condenses vapors, which
		/// remain uncondensed by the primary condenser, through direct contact heat
		/// exchange with the scrubber water.</p>
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="controlTemperature">In degrees Kelvin.</param>
		/// <param name="systemPressure">In Pascals.</param>
		/// <param name="airLeakRate">In Kilograms per time unit.</param>
		/// <param name="airLeakDuration">In matching time units.</param>
		public void VacuumDistillationWScrubber (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			double controlTemperature,
			double systemPressure, /* in Pascals. */
			double airLeakRate, /* in kilograms per time unit. */
			double airLeakDuration /* in matching time unit. */
			){
			new VacuumDistillationModel().VacuumDistillation(initial,out final,out emission, modifyInPlace, controlTemperature, systemPressure, airLeakRate, airLeakDuration);
		}

	}

	
	/// <summary>
	/// This model is used to calculate the emissions associated with
	/// drying solid product in a vacuum dryer.  The calculation of the
	/// emission from the operation is identical to that for the Vacuum
	/// Distill model, except that the total calculated VOC emission cannot
	/// exceed the amount of VOC in the wet cake.
	/// </summary>
	public class VacuumDryModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Vacuum Dry model on this API, the parameters hashtable
		/// must include the following entries (see the VacuumDry(...) method for details):<p></p>
		/// &quot;AirLeakRate&quot;, &quot;AirLeakDuration&quot;, &quot;SystemPressure&quot;  &quot;MaterialGuidToVolumeFraction&quot;, &quot;MassOfDriedProductCake&quot; and &quot;ControlTemperature&quot;. If there
		/// is no entry under &quot;SystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
		/// and &quot;FinalPressure&quot; and uses their average.
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

//			double airLeakRate = (double)parameters[PN.AirLeakRate_KgPerMin];
//			double airLeakDuration = (double)parameters[PN.AirLeakDuration_Min];
//			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
//			double systemPressure = GetSystemPressure(parameters);
//			Hashtable materialGuidToVolumeFraction = (Hashtable)parameters[PN.MaterialGuidToVolumeFraction];
//			double massofDriedProductCake = (double)parameters[PN.MassOfDriedProductCake_Kg];

			double airLeakRate = double.NaN; TryToRead(ref airLeakRate,PN.AirLeakRate_KgPerMin,parameters);
			double airLeakDuration = double.NaN; TryToRead(ref airLeakDuration,PN.AirLeakDuration_Min,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);
			double systemPressure = GetSystemPressure(parameters);

			EvaluateSuccessOfParameterReads(); // Preceding values are required.

			// These two are optional. ////////////////////////////////////////////////////////////
			Hashtable materialGuidToVolumeFraction = null;
			if ( parameters.Contains(PN.MaterialGuidToVolumeFraction) ) {
				materialGuidToVolumeFraction = (Hashtable)parameters[PN.MaterialGuidToVolumeFraction];
			}

			double massofDriedProductCake = double.NaN;
			if ( parameters.Contains(PN.MassOfDriedProductCake_Kg) ) {
				massofDriedProductCake = (double)parameters[PN.MassOfDriedProductCake_Kg];
			}
			// ////////////////////////////////////////////////////////////////////////////////////

			VacuumDry(initial,out final,out emission,modifyInPlace,controlTemperature,systemPressure,airLeakRate,airLeakDuration,materialGuidToVolumeFraction,massofDriedProductCake);
		
			ReportProcessCall(this,initial,final,emission,parameters);

		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used to calculate the emissions associated with drying solid product in a vacuum dryer.  The calculation of the emission from the operation is identical to that for the Vacuum Distill model, except that the total calculated VOC emission cannot exceed the amount of VOC in the wet cake.";
		private static readonly EmissionParam[] s_parameters 
			= {
									 new EmissionParam(PN.AirLeakRate_KgPerMin,"Air leak rate into the system, in kilograms per time unit."),
									 new EmissionParam(PN.AirLeakDuration_Min,"Air leak rate into the system, in the AirLeakRate's time units."),
									 new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
									 new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\"."),
									 new EmissionParam(PN.MaterialGuidToVolumeFraction,"A hashtable with the guids of materialTypes as keys, and the volumeFraction for that material type as values. VolumeFraction is the percent [0.0 to 1.0] of that material type in the offgas."),
									 new EmissionParam(PN.MassOfDriedProductCake_Kg,"Kilogram mass of the post-drying product cake.")
								 };
		private static readonly string[] s_keys = {"Vacuum Dry"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion
 
		/// <summary>
		/// This model is used to calculate the emissions associated with
		/// drying solid product in a vacuum dryer.  The calculation of the
		/// emission from the operation is identical to that for the Vacuum
		/// Distill model, except that the total calculated VOC emission cannot
		/// exceed the amount of VOC in the wet cake.
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="controlTemperature">In degrees kelvin.</param>
		/// <param name="systemPressure">In Pascals.</param>
		/// <param name="airLeakRate">In kilograms per time unit.</param>
		/// <param name="airLeakDuration">In matching time units.</param>
		/// <param name="materialGuidToVolumeFraction">Hashtable with Material Type Guids as keys, and a double, [0..1] representing the fraction of that material present lost during the drying of the product cake.</param>
		/// <param name="massOfDriedProductCake">The final mass of the dried product cake.</param>
		public void VacuumDry (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			double controlTemperature,
			double systemPressure, /* in Pascals. */
			double airLeakRate, /* in kilograms per time unit. */
			double airLeakDuration, /* in matching time unit. */
			Hashtable materialGuidToVolumeFraction,
			double massOfDriedProductCake
			){

			Mixture finalVd, emissionVd;
			new VacuumDistillationModel().VacuumDistillation(initial,out finalVd,out emissionVd, false, controlTemperature, systemPressure, airLeakRate, airLeakDuration);
			
			// If we have data for the AirDry equation, we will calculate it, and use the larger mass of the two (VD vs. AD).
		    Mixture emissionAd;
		    if ( materialGuidToVolumeFraction != null )
		    {
		        Mixture finalAd;
		        new AirDryModel().AirDry(initial, out finalAd,out emissionAd,false,massOfDriedProductCake,controlTemperature,materialGuidToVolumeFraction);
		    }
		    else {
				emissionAd = new Mixture(); // Nothing emitted.
			}

			if ( materialGuidToVolumeFraction == null ) {
				emission = emissionVd; // If we couldn't calculate an air dry emission, we must use the vacuum dry value.
			} else {
				// Otherwise, we use the lesser of the Airdry and VacuumDry calculations.
				emission = emissionVd.Mass > emissionAd.Mass ? emissionAd : emissionVd;
			}

			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			foreach ( Substance s in emission.Constituents ) {
				double massOfSubstance = s.Mass;
				// No need to worry about overEmission - it was accounted for in the VacDist or AirDry models.
				mixture.RemoveMaterial(s.MaterialType,massOfSubstance);
			}
			final = mixture;

		}
	}

	
	/// <summary>
	/// This model is used when any material (solid or liquid) is added to a vessel
	/// already containing a liquid or vapor VOC, and the vapor from that vessel is
	/// thereby emitted by displacement.  The model assumes that the volume of vapor
	/// displaced from the vessel is equal to the amount of material added to the
	/// vessel.  In addition, the vapor displaced from the vessel is saturated with
	/// the VOC vapor at the exit temperature.
	/// </summary>
	public class PressureTransferModel : EmissionModel {

		/// <summary>
		/// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
		/// an emission model that it has never seen before.
		/// <p></p>In order to successfully call the Pressure Transfer model on this API, the parameters hashtable
		/// must include the following entries (see the PressureTransfer(...) method for details):<p></p>
		/// &quot;MaterialToAdd&quot; and &quot;ControlTemperature&quot;. 
		/// </summary>
		/// <param name="initial">The initial mixture on which the emission model is to run.</param>
		/// <param name="final">The final mixture that is delivered after the emission model has run.</param>
		/// <param name="emission">The mixture that is evolved in the process of the emission.</param>
		/// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
		/// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
		public override void Process(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			Hashtable parameters){

			PrepareToReadLateBoundParameters();

			Mixture materialToAdd = null; TryToRead(ref materialToAdd,PN.MaterialToAdd,parameters);
			double controlTemperature = double.NaN; TryToRead(ref controlTemperature,PN.ControlTemperature_K,parameters);

			EvaluateSuccessOfParameterReads();

			PressureTransfer(initial,out final, out emission,modifyInPlace, materialToAdd, controlTemperature);

			ReportProcessCall(this,initial,final,emission,parameters);

		}

		#region >>> Usability Support <<<
		private static readonly string s_description = "This model is used when any material (solid or liquid) is added to a vessel already containing a liquid or vapor VOC, and the vapor from that vessel is thereby emitted by displacement.  The model assumes that the volume of vapor displaced from the vessel is equal to the amount of material added to the vessel.  In addition, the vapor displaced from the vessel is saturated with the VOC vapor at the exit temperature.";
		private static readonly EmissionParam[] s_parameters 
			= {
									 new EmissionParam(PN.MaterialToAdd,"The material to be added in the fill operation.The volume property of the material will be used to determine volume."),
									 new EmissionParam(PN.ControlTemperature_K,"The control, or condenser, temperature in degrees Kelvin.")
								 };
		private static readonly string[] s_keys = {"Pressure Transfer"}; 
		/// <summary>
		/// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
		/// </summary>
		public override string ModelDescription => s_description;

	    /// <summary>
		/// This is the list of parameters this model uses, and therefore expects as input.
		/// </summary>
		public override EmissionParam[] Parameters => s_parameters;

	    /// <summary>
		/// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
		/// that this model is to be called.
		/// </summary>
		public override string[] Keys => s_keys;

	    #endregion

		/// <summary>
		/// This model is used when any material (solid or liquid) is added to a vessel
		/// already containing a liquid or vapor VOC, and the vapor from that vessel is
		/// thereby emitted by displacement.  The model assumes that the volume of vapor
		/// displaced from the vessel is equal to the amount of material added to the
		/// vessel.  In addition, the vapor displaced from the vessel is saturated with
		/// the VOC vapor at the exit temperature.
		/// </summary>
		/// <param name="initial">The mixture as it exists before the emission.</param>
		/// <param name="final">The resultant mixture after the emission.</param>
		/// <param name="emission">The mixture emitted as a result of this model.</param>
		/// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
		/// <param name="materialToAdd">The material to be added in the fill operation.The volume property of the material will be used to determine volume.</param>
		/// <param name="controlTemperature">The control, or condenser, temperature in degrees Kelvin.</param>
		public void PressureTransfer (
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace, 
			Mixture materialToAdd,
			double controlTemperature
			){
			Mixture mixture = modifyInPlace?initial:(Mixture)initial.Clone();
			emission = new Mixture(initial.Name + " Fill emissions");

			double volumeOfMaterialAdded = materialToAdd.Volume /*, which is in liters*/ * .001 /*, to convert it to m^3.*/;

			ArrayList substances = new ArrayList(mixture.Constituents);
			foreach ( Substance substance in substances ) {
				MaterialType mt = substance.MaterialType;
				double molWt = mt.MolecularWeight;
				double molFrac = mixture.GetMoleFraction(mt,MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double massOfSubstance = volumeOfMaterialAdded * molWt * molFrac * vaporPressure / ( Chemistry.Constants.MolarGasConstant * controlTemperature); // grams, since molWt = grams per mole.
				massOfSubstance *= .001; // kilograms per gram.

				if ( !PermitOverEmission ) massOfSubstance = Math.Min(massOfSubstance,substance.Mass);
				if ( !PermitUnderEmission ) massOfSubstance = Math.Max(0,massOfSubstance);


				Substance emitted = (Substance)mt.CreateMass(massOfSubstance,substance.Temperature);
				Substance.ApplyMaterialSpecs(emitted,substance);
				emission.AddMaterial(emitted);
			}

			foreach ( Substance s in materialToAdd.Constituents ) {
				mixture.AddMaterial(s);
			}

			foreach ( Substance s in emission.Constituents ) {
				mixture.RemoveMaterial( s.MaterialType,s.Mass);
			}

			final = mixture;
		}
	}
}
