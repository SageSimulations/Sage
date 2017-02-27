/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using K=Highpoint.Sage.Materials.Chemistry.Emissions.Tester.Constants;
using PN=Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;


namespace Highpoint.Sage.Materials.Chemistry.Emissions {

	public class Tester {
		#region Private Fields
		private bool m_lateBound;
		private double m_initialMixtureTemperature;
		private BasicReactionSupporter m_brs;
		private Mixture m_initialMixture;
		private Mixture m_currentMixture;
		private Mixture m_lastEmission;
		private Mixture m_aggregateEmissions;
		private double m_condensorOrControlTemp; 
		private double m_tempOfTankLiquidInitial; 
		private double m_tempOfTankLiquidFinal;
		private double m_fillVolume;
		private double m_freeSpaceVolume;
		private double m_initialVesselPressure;
		private double m_finalVesselPressure;
		private double m_batchCycleTimeForSweep;
		private double m_gasSweepRateInSCFM;
		private double m_numberOfMolesOfGasEvolved;
		private double m_leakRateOfAirIntoSystem;
		private double m_batchCycleTimeForVacuumOperation;
		private double m_vacuumSystemPressureIn;
		#endregion

	public class Constants : Highpoint.Sage.Materials.Chemistry.Constants {
		public static double kgPerPound = 0.453592;
		public static double pascalsPer_mmHg = 133.322;
		public static double pascalsPerAtmosphere = 101325.0;
		public static double cubicFtPerGallon = 0.134;
		public static double litersPerGallon = 3.7854118;
		public static double cubicFtPerCubicMeter = 35.314667;
	}

		public Tester(BasicReactionSupporter brs, bool lateBound){
			m_brs = brs;
			m_lateBound = lateBound;
			Reset();
		}

			
		public void Reset(){
			m_initialMixture = new Mixture("Test mixture");
			m_aggregateEmissions = new Mixture("Aggregate emissions");
			// Defaults from WebEmit.
			SetParams(35.0,40.0,40.0,35.0,200000,581,790,760,1.0,0.1,5.0,1.0,1.0,500);
		}

			
		public void SetParams(
			double condensorOrControlTemp,
			double initialMixtureTemperature,
			double tempOfTankLiquidInitial, 
			double tempOfTankLiquidFinal,
			double fillVolume,
			double freeSpaceVolume,
			double initialVesselPressure,
			double finalVesselPressure,
			double batchCycleTimeForSweep,
			double gasSweepRateInSCFM,
			double numberOfMolesOfGasEvolved,
			double leakRateOfAirIntoSystem,
			double batchCycleTimeForVacuumOperation,
			double vacuumSystemPressureIn){
			m_condensorOrControlTemp = condensorOrControlTemp;
			m_initialMixtureTemperature = initialMixtureTemperature;
			m_tempOfTankLiquidInitial = tempOfTankLiquidInitial; 
			m_tempOfTankLiquidFinal = tempOfTankLiquidFinal;
			m_fillVolume = fillVolume;
			m_freeSpaceVolume = freeSpaceVolume;
			m_initialVesselPressure = initialVesselPressure;
			m_finalVesselPressure = initialVesselPressure;
			m_batchCycleTimeForSweep = batchCycleTimeForSweep;
			m_gasSweepRateInSCFM = gasSweepRateInSCFM;
			m_numberOfMolesOfGasEvolved = numberOfMolesOfGasEvolved;
			m_leakRateOfAirIntoSystem = leakRateOfAirIntoSystem;
			m_batchCycleTimeForVacuumOperation = batchCycleTimeForVacuumOperation;
			m_vacuumSystemPressureIn = vacuumSystemPressureIn;

		}

			
		public void AddGallons(string name, double numGallons){
			double liters   = K.litersPerGallon * numGallons;
			MaterialType mt = m_brs.MyMaterialCatalog[name];
			double kg       = liters * mt.SpecificGravity;

			m_initialMixture.AddMaterial(mt.CreateMass(kg,m_tempOfTankLiquidInitial));
			m_initialMixture.Temperature = m_initialMixtureTemperature;
			m_currentMixture = (Mixture)m_initialMixture.Clone();
		}

		public void DoAirDry(Hashtable materialGuidToVolumeFraction, double massOfDriedProductCake, double controlTemperature){
			Console.WriteLine("Air Dry Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				string modelKey = "Air Dry";
				paramTable.Add(PN.MaterialGuidToVolumeFraction,materialGuidToVolumeFraction);
				paramTable.Add(PN.MassOfDriedProductCake_Kg,massOfDriedProductCake);
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new Highpoint.Sage.Materials.Chemistry.Emissions.AirDryModel().AirDry(m_currentMixture,out m_currentMixture,out m_lastEmission,true,massOfDriedProductCake,controlTemperature,materialGuidToVolumeFraction);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoFill(Mixture materialToAdd, double controlTemp){
			Console.WriteLine("Fill Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.MaterialToAdd,materialToAdd);
				paramTable.Add(PN.ControlTemperature_K,controlTemp);
				string modelKey = "Fill";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new FillModel().Fill(m_currentMixture,out m_currentMixture,out m_lastEmission,true,materialToAdd,controlTemp);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F2","F8"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoEvacuation(double initialVesselPressure, double finalVesselPressure, double controlTemperature, double vesselVolume){
			m_initialVesselPressure = initialVesselPressure;
			m_finalVesselPressure = finalVesselPressure;

			Console.WriteLine("Evacuation Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.InitialPressure_P,initialVesselPressure);
				paramTable.Add(PN.FinalPressure_P,finalVesselPressure);
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.VesselVolume_M3,vesselVolume);
				string modelKey = "Evacuate";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new EvacuateModel().Evacuate(m_currentMixture,out m_currentMixture,out m_lastEmission,true,initialVesselPressure,finalVesselPressure,controlTemperature,vesselVolume);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoGasEvolution(double nMolesEvolved, double controlTemperature, double systemPressure){
			Console.WriteLine("Gas Evolution Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.MolesOfGasEvolved,nMolesEvolved);
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.SystemPressure_P,systemPressure);
				string modelKey = "Gas Evolution";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new GasEvolutionModel().GasEvolution(m_currentMixture,out m_currentMixture,out m_lastEmission,true,nMolesEvolved,controlTemperature,systemPressure);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoGasSweep(double controlTemperature, double systemPressure, double sweepRate, double sweepDuration){
			Console.WriteLine("Gas Sweep Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.SystemPressure_P,systemPressure);
				paramTable.Add(PN.GasSweepRate_M3PerMin,sweepRate);
				paramTable.Add(PN.GasSweepDuration_Min,sweepDuration);
				string modelKey = "Gas Sweep";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new GasSweepModel().GasSweep(m_currentMixture,out m_currentMixture,out m_lastEmission,true,sweepRate,sweepDuration,controlTemperature,systemPressure);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoHeat(double controlTemperature,double initialTemperature,double finalTemperature,double systemPressure,double vesselVolume){
			Console.WriteLine("Heat Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.InitialTemperature_K,initialTemperature);
				paramTable.Add(PN.FinalTemperature_K,finalTemperature);
				paramTable.Add(PN.SystemPressure_P,systemPressure);
				paramTable.Add(PN.VesselVolume_M3,vesselVolume);
				string modelKey = "Heat";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new HeatModel().Heat(m_currentMixture,out m_currentMixture,out m_lastEmission,true,controlTemperature,initialTemperature,finalTemperature,systemPressure,vesselVolume);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

		
		public void DoMassBalance(Mixture desiredEmission){
			Console.WriteLine("Mass Balance Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.DesiredEmission,desiredEmission);
				string modelKey = "Mass Balance";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new MassBalanceModel().MassBalance(m_currentMixture,out m_currentMixture,out m_lastEmission,true,desiredEmission);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoNoEmissions(){
			Console.WriteLine("No Emissions Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				string modelKey = "No Emissions";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new NoEmissionModel().NoEmission(m_currentMixture,out m_currentMixture,out m_lastEmission,true);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoVacuumDistillation(double controlTemperature,double systemPressure, double airLeakRate, double airLeakDuration){
			Console.WriteLine("Vacuum Distillation Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.VacuumSystemPressure_P,systemPressure);
				paramTable.Add(PN.AirLeakRate_KgPerMin,airLeakRate);
				paramTable.Add(PN.AirLeakDuration_Min,airLeakDuration);
				string modelKey = "Vacuum Distillation";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new VacuumDistillationModel().VacuumDistillation(m_currentMixture,out m_currentMixture,out m_lastEmission,true,controlTemperature,systemPressure,airLeakRate,airLeakDuration);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoVacuumDistillationWScrubber(double controlTemperature,double systemPressure, double airLeakRate, double airLeakDuration){
			Console.WriteLine("Vacuum Distillation w/ Scrubber Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.SystemPressure_P,systemPressure);
				paramTable.Add(PN.AirLeakRate_KgPerMin,airLeakRate);
				paramTable.Add(PN.AirLeakDuration_Min,airLeakDuration);
				string modelKey = "Vacuum Distillation w/ Scrubber";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new VacuumDistillationWScrubberModel().VacuumDistillationWScrubber(m_currentMixture,out m_currentMixture,out m_lastEmission,true,controlTemperature,systemPressure,airLeakRate,airLeakDuration);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoVacuumDry(double controlTemperature,double systemPressure, double airLeakRate, double airLeakDuration, Hashtable materialGuidToVolumeFraction, double massOfDriedProductCake){
			Console.WriteLine("Vacuum Dry Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.SystemPressure_P,systemPressure);
				paramTable.Add(PN.AirLeakRate_KgPerMin,airLeakRate);
				paramTable.Add(PN.AirLeakDuration_Min,airLeakDuration);
				paramTable.Add(PN.MaterialGuidToVolumeFraction,materialGuidToVolumeFraction);
				paramTable.Add(PN.MassOfDriedProductCake_Kg,massOfDriedProductCake);
				string modelKey = "Vacuum Dry";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new VacuumDryModel().VacuumDry(m_currentMixture,out m_currentMixture,out m_lastEmission,true,controlTemperature,systemPressure,airLeakRate,airLeakDuration,materialGuidToVolumeFraction,massOfDriedProductCake);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString("F1","F4"));

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}

			
		public void DoPressureTransfer(Mixture materialToAdd, double controlTemperature){
			Console.WriteLine("Pressure Transfer Testing" + (m_lateBound?", late bound.":", early bound."));
			Console.WriteLine("Mixture is      : " + m_currentMixture.ToString());
			if ( m_lateBound ) {
				Hashtable paramTable = new Hashtable();
				paramTable.Add(PN.ControlTemperature_K,controlTemperature);
				paramTable.Add(PN.MaterialToAdd,materialToAdd);
				string modelKey = "Pressure Transfer";
				EmissionsService.Instance.ProcessEmissions(m_currentMixture,out m_currentMixture,out m_lastEmission,true,modelKey,paramTable);
			} else {
				new PressureTransferModel().PressureTransfer(m_currentMixture,out m_currentMixture,out m_lastEmission,true,materialToAdd,controlTemperature);
			}
			Console.WriteLine("Mixture becomes : " + m_currentMixture.ToString());
			Console.WriteLine("Emissions are   : " + m_lastEmission.ToString());

			m_aggregateEmissions.AddMaterial(m_lastEmission);
		}


		public void SetInitialTemperature(double temperature){
			m_initialMixtureTemperature = temperature;
			m_initialMixture.Temperature = temperature;
		}
		public Mixture InitialMixture { get { return m_initialMixture; } }
		public Mixture CurrentMixture { get { return m_currentMixture; } }
		public Mixture LastEmission { get { return m_lastEmission; } }
		public Mixture AggregateEmissions { get { return m_aggregateEmissions; } } 
	}
}