/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Xml;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Materials.Chemistry.Emissions;
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using K=Highpoint.Sage.Materials.Chemistry.EmissionModels.EmissionModelTester.Constants;
using PN=Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.EmissionModels {
	/// <summary>
	/// Summary description for zTestTemperatureController.
	/// </summary>
	[TestClass]
	public class EmissionModelTester	{

		public class Constants : Highpoint.Sage.Materials.Chemistry.Constants {
			public static double kgPerPound = 0.453592;
			public static double pascalsPer_mmHg = 133.322;
			public static double pascalsPerAtmosphere = 101325.0;
			public static double cubicFtPerGallon = 0.134;
			public static double litersPerGallon = 3.7854118;
			public static double cubicFtPerCubicMeter = 35.314667;
		}

        public static string TEST_FILE;
        public static string PROP_FILE;
		private BasicReactionSupporter m_brs;
		private Hashtable m_computedVaporPressureIn;

		public EmissionModelTester(){Init();}

		#region MSTest Goo
		[TestInitialize] 
		public void Init() {
			m_brs = new BasicReactionSupporter();
			m_computedVaporPressureIn = new Hashtable();

            string devRoot = SageTestLib.Utility.SAGE_ROOT;

            TEST_FILE = devRoot + @"\Sage_Aux\SageTesting\emissionTest_12345-10.xml";
            PROP_FILE = devRoot + @"\Sage_Aux\SageTesting\PureComponentProperties.csv";
            Assert.IsTrue(System.IO.File.Exists(TEST_FILE), "Test data file not found - " + TEST_FILE);
            Assert.IsTrue(System.IO.File.Exists(PROP_FILE), "Properties data file not found - " + PROP_FILE);


            string[][] data = Load(PROP_FILE);
			
			foreach ( string[] row in data ) {
				string name = row[0];
				string chemAbstractNum = row[1];
				string classification = row[2];
				double densityGramPerLiter = double.Parse(row[3]);
				double densityLbsPerGal = double.Parse(row[4]);
				double molWt = double.Parse(row[5]);
				//double diffusivityInAir = double.Parse(row[6]);
				string henrysLawConstant = row[7];
				string antoineA = row[8];
				string antoineB = row[9];
				string antoineC = row[10];
				double calcVPmmhg = double.Parse(row[11]);
				double calcVP_psi = double.Parse(row[12]);

				double specGrav = densityGramPerLiter / 1000.0; // kg/liter equiv to SpecGrav.
				MaterialType mt = new MaterialType(null,name,Guid.NewGuid(),specGrav,1.0,MaterialState.Liquid,molWt,1.0);
				if ( antoineA!="" && antoineB!="" && antoineC!="" ) {
                    mt.SetAntoinesCoefficients3(double.Parse(antoineA), double.Parse(antoineB), double.Parse(antoineC), PressureUnits.mmHg, TemperatureUnits.Celsius);
				}

				if ( mt.Name.Equals("Hydrazine") ) {
					mt.SetAntoinesCoefficientsExt(76.858,-7245.2,0,0,-8.22,.0061557,1,double.NaN,double.NaN);
				}
				m_brs.MyMaterialCatalog.Add(mt);
				m_computedVaporPressureIn.Add(mt.Name,calcVPmmhg);
			}

			MaterialType mt2 = new MaterialType(null,"Unknown",Guid.NewGuid(),1.0,1.0,MaterialState.Liquid,196.0,1.0);
			m_brs.MyMaterialCatalog.Add(mt2);

		}

		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		#endregion
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test runs many stored-off tests and assesses the correctness of the results.")]
		public void MegaTest(){

			EmissionsService es = EmissionsService.Instance;

			XmlDocument doc = new XmlDocument();
			doc.Load(TEST_FILE);

			ArrayList avgErrors = new ArrayList();
			foreach ( XmlNode testNode in doc.SelectNodes("/EmissionTests/Test") ) {

				Console.WriteLine("Running Test Scenario : " + testNode.Attributes["num"].InnerText);

				bool printedHeader = false;
				XmlNode stimNode = testNode.SelectSingleNode("Stimulus");
				XmlNode responseNode = testNode.SelectSingleNode("Response");

				foreach ( XmlNode modelResultNode in responseNode.SelectNodes("Model") ){
					string model = modelResultNode.Attributes["name"].InnerText;
					//if ( "No Emissions".Equals(model) ) System.Diagnostics.Debugger.Break();
					Tester tester = new Tester(m_brs,m_lateBound);

					foreach ( XmlNode materialNode in stimNode.SelectNodes("Material") ) {
						string matlName = materialNode.Attributes["name"].InnerText;
						string strGallons = materialNode.Attributes["quantity"].InnerText;
						tester.AddGallons(matlName,double.Parse(strGallons));
					}

					Hashtable parameters = new Hashtable();
					foreach ( XmlNode paramNode in stimNode.SelectNodes("Parameter") ){
						double valu = double.Parse(paramNode.Attributes["value"].InnerText);
						string name = paramNode.Attributes["name"].InnerText;

						#region Convert to appropriate units and store data in parameter hashtable
						switch ( name ) {
							case ("controlTemperature"):{
								parameters.Add(PN.ControlTemperature_K,valu+K.CELSIUS_TO_KELVIN);
								break;
							}
							case ("finalTankTemperature"):{
								parameters.Add(PN.FinalTemperature_K,valu+K.CELSIUS_TO_KELVIN);
								break;
							}
							case ("initialTankTemperature"):{
								parameters.Add(PN.InitialTemperature_K,valu+K.CELSIUS_TO_KELVIN);
								tester.SetInitialTemperature(valu);
								break;
							}
							case ("fillVolumeInGallons"):{
								//parameters.Add("FillVolume",valu*K.litersPerGallon);
								MaterialType mt = m_brs.MyMaterialCatalog["Unknown"];
								Mixture addend = new Mixture("Addend",Guid.NewGuid());
								double desiredVolume = valu * K.litersPerGallon;
								addend.AddMaterial(mt.CreateMass(desiredVolume,35.0)); // Only works because we know its density is 1.0 ...
								parameters.Add(PN.MaterialToAdd,addend);
								break;
							}
							case ("freeSpaceInGallons"):{
								//parameters.Add("FreeSpace",valu*K.litersPerGallon);
								// We are expecting "VesselVolume" instead.
								double vesselVolume = ((valu*K.litersPerGallon) + tester.CurrentMixture.Volume)*.001 /*m^3/liter*/;
								parameters.Add(PN.VesselVolume_M3,vesselVolume);
								break;
							}
							case ("initialPressureIn_mmHg"):{
								parameters.Add(PN.InitialPressure_P,valu*K.pascalsPer_mmHg);
								break;
							}
							case ("finalPressureIn_mmHg"):{
								parameters.Add(PN.FinalPressure_P,valu*K.pascalsPer_mmHg);
								break;
							}
							case ("batchCycleTimeForSweepInHours"):{
								parameters.Add(PN.GasSweepDuration_Min,valu*60.0);
								break;
							}
							case ("gasSweepRateInSCFM"):{
								parameters.Add(PN.GasSweepRate_M3PerMin,valu/K.cubicFtPerCubicMeter);
								break;
							}
							case ("numberOfMolesOfGasEvolved"):{
								parameters.Add(PN.MolesOfGasEvolved,valu);
								break;
							}
							case ("leakRateOfAirIntoSystem"):{
								//parameters.Add("VacuumOpLeakRateOfAir",valu*K.kgPerPound);
								parameters.Add(PN.AirLeakRate_KgPerMin,valu*K.kgPerPound);
								break;
							}
							case ("batchCycleTimeForVacuumOps"):{
								//parameters.Add("VacuumOpCycleTime",valu);
								parameters.Add(PN.AirLeakDuration_Min,valu);
								break;
							}
							case ("systemPressureForVacuumOpsIn_mmHg"):{
								parameters.Add(PN.VacuumSystemPressure_P,valu*K.pascalsPer_mmHg);
								break;
							}
							case ("systemPressure"):{
								parameters.Add(PN.SystemPressure_P,valu*K.pascalsPer_mmHg);
								break;
							}
							default: {
								Console.WriteLine("Unknown parameter " + valu + " encountered under key " + name);
								break;
							}
						}
						#endregion
						
					}

					if ( !printedHeader ) {
						foreach ( DictionaryEntry de in parameters ) {
							Console.WriteLine(" > " + de.Key + " = " + de.Value );
						}
						Console.WriteLine("Initial : " + tester.InitialMixture.ToString());
						printedHeader = true;
					}

					//if ( "Vacuum Distillation".Equals(model) ) System.Diagnostics.Debugger.Break();
					Console.WriteLine("\r\n\r\nTesting " + model);
					Console.WriteLine("                                        Initial   --- Emissions ---   Computation");
					Console.WriteLine("                                        Mixture   Expected  Actual    Error");
				//  Console.WriteLine("Acetic Acid.............................0.0386    0.0387    0.0386    0.2%");

					ArrayList originalSubstances = new ArrayList();
					foreach ( Substance s in tester.InitialMixture.Constituents) originalSubstances.Add(s.MaterialType);

					Mixture final, emission;
					es.ProcessEmissions(tester.InitialMixture,out final, out emission, false, model, parameters);
					//Console.WriteLine("Emission : " + emission.ToString());

					#region analyze resulting mixture to ensure that it is "Close Enough"
					Hashtable observed = new Hashtable();
					foreach ( MaterialType mt in originalSubstances ) {
						observed.Add(mt.Name,emission.ContainedMassOf(mt));
					}

					Hashtable expected = new Hashtable();
					foreach ( XmlNode goldStd in modelResultNode.SelectNodes("Material") ) {
						expected.Add(goldStd.Attributes["name"].InnerText,double.Parse(goldStd.Attributes["kilograms"].InnerText));
					}

					ArrayList alObserved = new ArrayList(observed.Keys);
					alObserved.Sort();
					double maxErr = 0.0;
					double avgErr = 0.0;
					ArrayList errors = new ArrayList();
					foreach ( string substance in alObserved ) {
						MaterialType mt = m_brs.MyMaterialCatalog[substance];
						double expectedKg = (double)expected[substance];
						double observedKg = (double)observed[substance];
						double err = Math.Abs((expectedKg-observedKg)/expectedKg)*100.0;
						if ( double.IsNaN(err) && expectedKg == 0.0 && observedKg == 0.0 ) err = 0.0;
						if ( err>maxErr ) maxErr = err;
						avgErr += err;
						errors.Add(err);

						System.Text.StringBuilder sb = new System.Text.StringBuilder();
						sb.Append(substance);
						for ( int i = 40 ; i > substance.Length ; i-- ) sb.Append(".");
						string initialKgStr = string.Format("{0:F4}",tester.InitialMixture.ContainedMassOf(mt));
						sb.Append(initialKgStr);
						for ( int i = 10 ; i > initialKgStr.Length ; i-- ) sb.Append(" ");
						string expectedKgStr = string.Format("{0:F4}",expectedKg);
						sb.Append(expectedKgStr);
						for ( int i = 10 ; i > expectedKgStr.Length ; i-- ) sb.Append(" ");
						string observedKgStr = string.Format("{0:F4}",observedKg);
						sb.Append(observedKgStr);
						for ( int i = 10 ; i > observedKgStr.Length ; i-- ) sb.Append(" ");
						sb.Append(string.Format("{0:F1}%",err));

						Console.WriteLine(sb.ToString());
					}

					avgErr/=alObserved.Count;
					double stdevErr = 0.0;
					foreach ( double err in errors ) {
						stdevErr += ((err-avgErr)*(err-avgErr));
					}
					stdevErr/=alObserved.Count;
					stdevErr = Math.Sqrt(stdevErr);

					string msg = string.Format("Max Error was {0:F1}%, Avg Error was = {1:F1}%, StDev Error was {2:F5}%.",maxErr,avgErr,stdevErr);
					Console.WriteLine(msg);

					avgErrors.Add(avgErr);
				}
				#endregion
			}

			double avgAvgErr = 0.0;
			double maxAvgErr = 0.0;
			foreach ( double avgErr in avgErrors ) {
				avgAvgErr += avgErr;
				if ( avgErr > maxAvgErr ) maxAvgErr = avgErr;
			}
			avgAvgErr/=avgErrors.Count;

			Console.WriteLine("\r\n\r\nFinal Report: After running " + avgErrors.Count + " models over " + (avgErrors.Count/8) + " random scenarios,\r\nthe average avgErr for a scenario was {0:F2}%, and the maximum avgErr is {1:F2}%.",avgAvgErr,maxAvgErr);
		}

		/*- <EmissionTests seed="12345">
- <Test num="0">
- <Stimulus>
  <Material name="Acetic Acid" quantity="0.667469348137951" /> 
  <Material name="Acetone" quantity="0.701595088793707" /> 
  <Material name="Acetonitrile" quantity="7.74765135149828" /> 
  <Material name="Avermectin Oils1" quantity="5.11139268759237" /> 
  <Material name="Butanol" quantity="7.97490558492714" /> 
  <Material name="n-Butyl Acetate" quantity="8.27308291023275" /> 
  <Material name="t-Butyldimethyl Silanol1" quantity="1.65958795308116" /> 
  <Material name="Cyclohexane" quantity="7.36130623489679" /> 
  <Material name="Dimethylaminopyridine1" quantity="2.6021636475819" /> 
  <Material name="Dimethylformamide" quantity="5.06004851081411" /> 
  <Material name="Dimethylsulfate" quantity="2.30273722312541" /> 
  <Material name="Dimethylsulfide" quantity="3.87140443263175" /> 
  <Material name="Dimethylsulfoxide" quantity="2.15980615101746" /> 
  <Material name="Dodecylbenzylsulfonic Acid1" quantity="0.208108848989061" /> 
  <Material name="Ethanol" quantity="1.34833602297508" /> 
  <Material name="Ethyl Acetate" quantity="7.40831733560577" /> 
  <Material name="Ethyl Ether" quantity="1.95091067904183" /> 
  <Material name="n-Ethyl Pyrolidone (NEP)" quantity="8.87039565894306" /> 
  <Material name="Ethyl-7-Chloro-2-Oxoheptanate1" quantity="8.61100613540551" /> 
  <Material name="Heptane" quantity="3.58865063804605" /> 
  <Material name="Hexane" quantity="1.03702260229598" /> 
  <Material name="Isoamyl Alcohol" quantity="5.29583051581673" /> 
  <Material name="Isopropanol" quantity="3.58755016866492" /> 
  <Material name="Isopropyl Acetate" quantity="7.07505481181436" /> 
  <Material name="Methane Sulfonic Acid" quantity="5.80474831899849" /> 
  <Material name="Methanol" quantity="6.20548322620126" /> 
  <Material name="Methyl Acetate" quantity="7.32496631672837" /> 
  <Material name="Methyl Ethyl Ketone" quantity="7.47882739989032" /> 
  <Material name="Methylene Chloride" quantity="3.96251060718787" /> 
  <Material name="Phenyl Phosphate1" quantity="2.45257239902978" /> 
  <Material name="n-Propanol" quantity="6.86345072317098" /> 
  <Material name="Tetrahydrofuran" quantity="2.10546193742448" /> 
  <Material name="Toluene" quantity="5.17537586631969" /> 
  <Material name="Triethylamine" quantity="4.85390965121515" /> 
  <Material name="Hydrazine" quantity="2.52805452911558" /> 
  <Material name="Chloroform" quantity="7.70778849148554" /> 
  <Material name="Pyrrolidine" quantity="0.0257893931240725" /> 
  <Material name="MTBE" quantity="4.83912114279304" /> 
  <Material name="Water" quantity="4.4795790335534" /> 
  <Parameter name="controlTemperature" value="19.7292085316634" /> 
  <Parameter name="finalTankTemperature" value="34.2315478130391" /> 
  <Parameter name="initialTankTemperature" value="29.0249545169179" /> 
  <Parameter name="fillVolumeInGallons" value="53990" /> 
  <Parameter name="freeSpaceInGallons" value="256.608102776393" /> 
  <Parameter name="initialPressureIn_mmHg" value="727.04827744842" /> 
  <Parameter name="finalPressureIn_mmHg" value="658.955729221439" /> 
  <Parameter name="batchCycleTimeForSweepInHours" value="0.869533596266775" /> 
  <Parameter name="gasSweepRateInSCFM" value="1.35268067654813" /> 
  <Parameter name="numberOfMolesOfGasEvolved" value="0.67566556398555" /> 
  <Parameter name="leakRateOfAirIntoSystem" value="1.35220055787461" /> 
  <Parameter name="batchCycleTimeForVacuumOps" value="1.14435457561368" /> 
  <Parameter name="systemPressureForVacuumOpsIn_mmHg" value="723.166682907923" /> 
  </Stimulus>
- <Response>
- <Model name="Fill">
  <Material name="Acetic Acid" poundsMass="1.42166891426726" /> 
  <Material name="Acetone" poundsMass="7.37478370052351" /> 
  <Material name="Acetonitrile" poundsMass="43.8329763652033" /> 
  <Material name="Avermectin Oils1" poundsMass="0.564352344627757" /> 
  <Material name="Butanol" poundsMass="9.04982368259913" /> 
  <Material name="n-Butyl Acetate" poundsMass="22.7032485859357" /> 
  <Material name="t-Butyldimethyl Silanol1" poundsMass="0.564352344627757" /> 
  <Material name="Cyclohexane" poundsMass="83.3333333333333" /> 
  <Material name="Dimethylaminopyridine1" poundsMass="0.564352344627757" /> 
  <Material name="Dimethylformamide" poundsMass="0.328949680576043" /> 
  <Material name="Dimethylsulfate" poundsMass="0.965274866093065" /> 
  <Material name="Dimethylsulfide" poundsMass="90.7692307692307" /> 
  <Material name="Dimethylsulfoxide" poundsMass="0.604724266625852" /> 
  <Material name="Dodecylbenzylsulfonic Acid1" poundsMass="0.564352344627757" /> 
  <Material name="Ethanol" poundsMass="2.42094379422006" /> 
  <Material name="Ethyl Acetate" poundsMass="96.1538461538462" /> 
  <Material name="Ethyl Ether" poundsMass="76.2820512820513" /> 
  <Material name="n-Ethyl Pyrolidone (NEP)" poundsMass="0.387834473532694" /> 
  <Material name="Ethyl-7-Chloro-2-Oxoheptanate1" poundsMass="0.564352344627757" /> 
  <Material name="Heptane" poundsMass="65.6410256410256" /> 
  <Material name="Hexane" poundsMass="70.5128205128205" /> 
  <Material name="Isoamyl Alcohol" poundsMass="4.81580587395878" /> 
  <Material name="Isopropanol" poundsMass="15.3191689685323" /> 
  <Material name="Isopropyl Acetate" poundsMass="93.3333333333333" /> 
  <Material name="Methane Sulfonic Acid" poundsMass="0.000854219632020942" /> 
  <Material name="Methanol" poundsMass="9.27650154459892" /> 
  <Material name="Methyl Acetate" poundsMass="99.1025641025641" /> 
  <Material name="Methyl Ethyl Ketone" poundsMass="86.025641025641" /> 
  <Material name="Methylene Chloride" poundsMass="142.948717948718" /> 
  <Material name="Phenyl Phosphate1" poundsMass="0.564352344627757" /> 
  <Material name="n-Propanol" poundsMass="21.3177275690714" /> 
  <Material name="Tetrahydrofuran" poundsMass="32.4675281878225" /> 
  <Material name="Toluene" poundsMass="49.0471273338219" /> 
  <Material name="Triethylamine" poundsMass="69.6153846153846" /> 
  <Material name="Hydrazine" poundsMass="0.0187453227769477" /> 
  <Material name="Chloroform" poundsMass="160.384615384615" /> 
  <Material name="Pyrrolidine" poundsMass="0.0187053892326599" /> 
  <Material name="MTBE" poundsMass="79.2307692307692" /> 
  <Material name="Water" poundsMass="0.848155753551298" /> 
  </Model>*/

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Fill computations across many materials.")]
		
		public void TestMultiMaterialFill(){
			Tester tester = new Tester(m_brs,m_lateBound);

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(
@"<Initial><Material name=""Acetic Acid"" quantity=""0.667469348137951"" />
  <Material name=""Acetone"" quantity=""0.701595088793707"" />
  <Material name=""Acetonitrile"" quantity=""7.74765135149828"" /> 
  <Material name=""Avermectin Oils1"" quantity=""5.11139268759237"" /> 
  <Material name=""Butanol"" quantity=""7.97490558492714"" /> 
  <Material name=""n-Butyl Acetate"" quantity=""8.27308291023275"" /> 
  <Material name=""t-Butyldimethyl Silanol1"" quantity=""1.65958795308116"" /> 
  <Material name=""Cyclohexane"" quantity=""7.36130623489679"" /> 
  <Material name=""Dimethylaminopyridine1"" quantity=""2.6021636475819"" /> 
  <Material name=""Dimethylformamide"" quantity=""5.06004851081411"" />
  <Material name=""Dimethylsulfate"" quantity=""2.30273722312541"" /> 
  <Material name=""Dimethylsulfide"" quantity=""3.87140443263175"" /> 
  <Material name=""Dimethylsulfoxide"" quantity=""2.15980615101746"" /> 
  <Material name=""Dodecylbenzylsulfonic Acid1"" quantity=""0.208108848989061"" /> 
  <Material name=""Ethanol"" quantity=""1.34833602297508"" /> 
  <Material name=""Ethyl Acetate"" quantity=""7.40831733560577"" /> 
  <Material name=""Ethyl Ether"" quantity=""1.95091067904183"" /> 
  <Material name=""n-Ethyl Pyrolidone (NEP)"" quantity=""8.87039565894306"" /> 
  <Material name=""Ethyl-7-Chloro-2-Oxoheptanate1"" quantity=""8.61100613540551"" /> 
  <Material name=""Heptane"" quantity=""3.58865063804605"" /> 
  <Material name=""Hexane"" quantity=""1.03702260229598"" />
  <Material name=""Isoamyl Alcohol"" quantity=""5.29583051581673"" /> 
  <Material name=""Isopropanol"" quantity=""3.58755016866492"" /> 
  <Material name=""Isopropyl Acetate"" quantity=""7.07505481181436"" /> 
  <Material name=""Methane Sulfonic Acid"" quantity=""5.80474831899849"" /> 
  <Material name=""Methanol"" quantity=""6.20548322620126"" /> 
  <Material name=""Methyl Acetate"" quantity=""7.32496631672837"" /> 
  <Material name=""Methyl Ethyl Ketone"" quantity=""7.47882739989032"" /> 
  <Material name=""Methylene Chloride"" quantity=""3.96251060718787"" /> 
  <Material name=""Phenyl Phosphate1"" quantity=""2.45257239902978"" /> 
  <Material name=""n-Propanol"" quantity=""6.86345072317098"" /> 
  <Material name=""Tetrahydrofuran"" quantity=""2.10546193742448"" /> 
  <Material name=""Toluene"" quantity=""5.17537586631969"" /> 
  <Material name=""Triethylamine"" quantity=""4.85390965121515"" /> 
  <Material name=""Hydrazine"" quantity=""2.52805452911558"" /> 
  <Material name=""Chloroform"" quantity=""7.70778849148554"" /> 
  <Material name=""Pyrrolidine"" quantity=""0.0257893931240725"" /> 
  <Material name=""MTBE"" quantity=""4.83912114279304"" /> 
  <Material name=""Water"" quantity=""4.4795790335534"" />
  </Initial>");

			foreach ( XmlNode node in doc.SelectNodes("/Initial/Material") ){
				tester.AddGallons(node.Attributes["name"].InnerText,double.Parse(node.Attributes["quantity"].InnerText));
			}

			// Create a mixture with a 200,000 gallon volume.
			MaterialType mt = m_brs.MyMaterialCatalog["Unknown"];
			Mixture addend = new Mixture("Addend",Guid.NewGuid());
			double desiredVolume = 53990 * K.litersPerGallon;
			addend.AddMaterial(mt.CreateMass(desiredVolume,35.0));
			//Console.WriteLine("There are " + (addend.Volume/K.litersPerGallon) + " gallons of " + mt.Name + " being added.");
			
			double controlTemperature = 19.7292085 + K.CELSIUS_TO_KELVIN;

			tester.DoFill(addend,controlTemperature);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (40.00 deg C) of 8.8577 kg of Chloroform, 8.8237 kg of Methylene Chloride, 6.6420 kg of Dimethylsulfide, 5.6593 kg of Methyl Acetate, 2.9698 kg of Ethyl Ether, 2.4233 kg of Ethyl Acetate, 2.3301 kg of Methanol, 2.1779 kg of Cyclohexane, 2.1184 kg of Acetonitrile, 2.0769 kg of Methyl Ethyl Ketone, 1.9382 kg of MTBE, 1.4243 kg of Isopropyl Acetate, 1.3159 kg of Tetrahydrofuran, 0.6678 kg of Triethylamine, 0.5016 kg of Acetone, 0.4785 kg of Toluene, 0.4370 kg of Isopropanol, 0.4052 kg of Hexane, 0.3934 kg of n-Propanol, 0.3838 kg of Water, 0.3808 kg of Heptane, 0.2876 kg of n-Butyl Acetate, 0.2262 kg of Ethanol, 0.1583 kg of Butanol, 0.1309 kg of Hydrazine, 0.0645 kg of Dimethylformamide, 0.0475 kg of Isoamyl Alcohol, 0.0386 kg of Acetic Acid, 0.0087 kg of Ethyl-7-Chloro-2-Oxoheptanate1, 0.0055 kg of Dimethylsulfoxide, 0.0051 kg of Avermectin Oils1, 0.0051 kg of Pyrrolidine, 0.0050 kg of n-Ethyl Pyrolidone (NEP), 0.0047 kg of Dimethylsulfate, 0.0026 kg of Dimethylaminopyridine1, 0.0025 kg of Phenyl Phosphate1, 0.0017 kg of t-Butyldimethyl Silanol1, 0.0002 kg of Dodecylbenzylsulfonic Acid1 and 0.0000 kg of Methane Sulfonic Acid";
			EvaluateResults(knownGood,"Fill",emission);
		}

		
		#region Early Bound Tests
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the AirDry computations.")]
		public void TestAirDry(){

			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",10);
			tester.AddGallons("Water",10);

			Hashtable materialGuidToVolumeFraction = new Hashtable();
			materialGuidToVolumeFraction.Add(m_brs.MyMaterialCatalog["Water"].Guid,0.5);
			materialGuidToVolumeFraction.Add(m_brs.MyMaterialCatalog["Dimethylsulfide"].Guid,0.5);

			double massOfDriedProductCake = tester.CurrentMixture.Mass*.6;
			double controlTemperature = 35+Constants.CELSIUS_TO_KELVIN;
			tester.DoAirDry(materialGuidToVolumeFraction,massOfDriedProductCake,controlTemperature);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (40.00 deg C) of 15.1318 kg of Water and 12.8457 kg of Dimethylsulfide";
			EvaluateResults(knownGood,"Air Dry",emission);

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Evacuate computations.")]
		public void TestEvacuate(){

			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",5000);
			tester.AddGallons("Water",100);
			
			double initialSystemPressure = 790.0 * K.pascalsPer_mmHg;
			double finalSystemPressure = 760.0 * K.pascalsPer_mmHg;
			double controlTemperature = 35 + K.CELSIUS_TO_KELVIN;
			double vesselVolume = ((581 /*free space*/ * K.litersPerGallon) + tester.CurrentMixture.Volume) * .001; // Convert liters to cubic meters.
			
			tester.DoEvacuation(initialSystemPressure, finalSystemPressure, controlTemperature, vesselVolume);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (40.00 deg C) of 1.1219 kg of Dimethylsulfide and 0.0016 kg of Water";
			EvaluateResults(knownGood,"Evacuate",emission);
		}

		
		private void EvaluateResults(string knownGood, string testName, Mixture emission){
			string result = emission.ToString("F2","F4");
		    Assert.AreEqual(knownGood,result, 
		        String.Format("{0} test failed - result was {1} but should have been {2}.", testName, result, knownGood));
		}

		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Fill computations.")]
		public void TestFill(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",10);
			tester.AddGallons("Water",10);

			// Create a mixture with a 200,000 gallon volume.
			MaterialType mt = m_brs.MyMaterialCatalog["Unknown"];
			Mixture addend = new Mixture("Addend",Guid.NewGuid());
			double desiredVolume = 200000 * K.litersPerGallon;
			addend.AddMaterial(mt.CreateMass(desiredVolume,35.0));
			//Console.WriteLine("There are " + (addend.Volume/K.litersPerGallon) + " gallons of " + mt.Name + " being added.");
			
			double controlTemperature = 35 + K.CELSIUS_TO_KELVIN;

			tester.DoFill(addend,controlTemperature);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (40.00 deg C) of 32.1142 kg of Dimethylsulfide and 24.0567 kg of Water";
			EvaluateResults(knownGood,"Fill",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Evolution computations.")]
		public void TestGasEvolution(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			double controlTemperature = 35 + K.CELSIUS_TO_KELVIN;
			double systemPressure = 760 * K.pascalsPer_mmHg;
			double nMolesEvolved = 5.0;

			tester.DoGasEvolution(nMolesEvolved,controlTemperature,systemPressure);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (35.00 deg C) of 0.3391 kg of Dimethylsulfide and 0.0048 kg of Water";
			EvaluateResults(knownGood,"Gas Evolution",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestGasSweep(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			double controlTemperature = 35 + K.CELSIUS_TO_KELVIN;
			double systemPressure = 760 * K.pascalsPer_mmHg;
			double sweepRate = 0.1 /*SCFM*/ / K.cubicFtPerCubicMeter; 
			double sweepDuration = 60/*minutes*/;

			tester.DoGasSweep(controlTemperature,systemPressure,sweepRate,sweepDuration);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (35.00 deg C) of 0.4557 kg of Dimethylsulfide and 0.0065 kg of Water";
			EvaluateResults(knownGood,"Gas Sweep",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestHeat(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			double controlTemperature = 35 + K.CELSIUS_TO_KELVIN;
			double initialTemperature = 35 + K.CELSIUS_TO_KELVIN;
			double finalTemperature = 75 + K.CELSIUS_TO_KELVIN;
			double systemPressure = 760 * K.pascalsPer_mmHg;
			double vesselFreeSpace = 581 /*Gallons*/ * K.litersPerGallon * .001 /*m^3 per liter*/;
			double vesselVolume = vesselFreeSpace + (tester.InitialMixture.Volume * .001 /*m^3 per liter*/);

			tester.DoHeat(controlTemperature,initialTemperature,finalTemperature,systemPressure,vesselVolume);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (35.00 deg C) of 6.1601 kg of Dimethylsulfide and 0.0875 kg of Water";
			EvaluateResults(knownGood,"Heat",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestMassBalance(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			Mixture desiredEmissions = new Mixture("Mass Balance desired emissions");
			desiredEmissions.AddMaterial(m_brs.MyMaterialCatalog["Water"].CreateMass(1,37));
			desiredEmissions.AddMaterial(m_brs.MyMaterialCatalog["Dimethylsulfide"].CreateMass(5,37));
			tester.DoMassBalance(desiredEmissions);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (37.00 deg C) of 5.0000 kg of Dimethylsulfide and 1.0000 kg of Water";
			EvaluateResults(knownGood,"Mass Balance",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestNoEmissions(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			tester.DoNoEmissions();

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (40.00 deg C) of nothing.";
			EvaluateResults(knownGood,"No Emissions",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestVacuumDistillation(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			double controlTemperature = 35.0 + K.CELSIUS_TO_KELVIN;
			double leakRateOfAir = 1.0 /*lbm per hour*/ * K.kgPerPound;
			double leakDuration =  1.0; /*hour*/
			double systemPressure = 500 * K.pascalsPer_mmHg;

			tester.DoVacuumDistillation(controlTemperature,systemPressure,leakRateOfAir,leakDuration);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (35.00 deg C) of 3.9899 kg of Dimethylsulfide and 0.0567 kg of Water";
			EvaluateResults(knownGood,"Vacuum Distillation",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestVacuumDistillationWScrubber(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			double controlTemperature = 35.0 + K.CELSIUS_TO_KELVIN;
			double leakRateOfAir = 1.0 /*lbm per hour*/ * K.kgPerPound;
			double leakDuration =  1.0; /*hour*/
			double systemPressure = 760 * K.pascalsPer_mmHg;

			tester.DoVacuumDistillationWScrubber(controlTemperature,systemPressure,leakRateOfAir,leakDuration);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (35.00 deg C) of 1.0619 kg of Dimethylsulfide and 0.0151 kg of Water";
			EvaluateResults(knownGood,"Vacuum Distillation w/ Scrubber",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestVacuumDry(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",500);
			tester.AddGallons("Water",100);

			double controlTemperature = 35.0 + K.CELSIUS_TO_KELVIN;
			double leakRateOfAir = 1.0 /*lbm per hour*/ * K.kgPerPound;
			double leakDuration =  1.0; /*hour*/
			double systemPressure = 760 * K.pascalsPer_mmHg;

			Hashtable materialGuidToVolumeFraction = new Hashtable();
			materialGuidToVolumeFraction.Add(m_brs.MyMaterialCatalog["Water"].Guid,0.5);
			materialGuidToVolumeFraction.Add(m_brs.MyMaterialCatalog["Dimethylsulfide"].Guid,0.5);

			double massOfDriedProductCake = tester.CurrentMixture.Mass*.6;

			tester.DoVacuumDry(controlTemperature,systemPressure,leakRateOfAir,leakDuration,materialGuidToVolumeFraction,massOfDriedProductCake);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (35.00 deg C) of 1.0619 kg of Dimethylsulfide and 0.0151 kg of Water";
			EvaluateResults(knownGood,"Vacuum Dry",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations.")]
		public void TestPressureTransfer(){
			Tester tester = new Tester(m_brs,m_lateBound);

			tester.AddGallons("Dimethylsulfide",10);
			tester.AddGallons("Water",10);

			// Create a mixture with a 200,000 gallon volume.
			MaterialType mt = m_brs.MyMaterialCatalog["Unknown"];
			Mixture addend = new Mixture("Addend",Guid.NewGuid());
			double desiredVolume = 200000 * K.litersPerGallon;
			addend.AddMaterial(mt.CreateMass(desiredVolume,35.0));
			//Console.WriteLine("There are " + (addend.Volume/K.litersPerGallon) + " gallons of " + mt.Name + " being added.");
			
			double controlTemperature = 35 + K.CELSIUS_TO_KELVIN;

			tester.DoPressureTransfer(addend,controlTemperature);

			Mixture emission = tester.LastEmission;
			Mixture resultant = tester.CurrentMixture;

			string knownGood = "Mixture (40.00 deg C) of 32.1142 kg of Dimethylsulfide and 24.0567 kg of Water";
			EvaluateResults(knownGood,"Pressure Transfer",emission);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test tests nothing.")]
		public void TestNothing(){
			// Nothing model is not implemented.
		}
		#endregion

		#region Late Bound Tests
		private bool m_lateBound = false;
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the AirDry computations through the late-bound API.")]
		public void TestLateBoundAirDry(){
			m_lateBound = true;
			TestAirDry();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Evacuate computations through the late-bound API.")]
		public void TestLateBoundEvacuate(){
			m_lateBound = true;
			TestEvacuate();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Fill computations through the late-bound API.")]
		public void TestLateBoundFill(){
			m_lateBound = true;
			TestFill();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Evolution computations through the late-bound API.")]
		public void TestLateBoundGasEvolution(){
			m_lateBound = true;
			TestGasEvolution();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundGasSweep(){
			m_lateBound = true;
			TestGasSweep();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundHeat(){
			m_lateBound = true;
			TestHeat();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundMassBalance(){
			m_lateBound = true;
			TestMassBalance();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundNoEmissions(){
			m_lateBound = true;
			TestNoEmissions();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundVacuumDistillation(){
			m_lateBound = true;
			TestVacuumDistillation();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundVacuumDistillationWScrubber(){
			m_lateBound = true;
			TestVacuumDistillationWScrubber();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundVacuumDry(){
			m_lateBound = true;
			TestVacuumDry();
			m_lateBound = false;
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test evaluates correctness of the Gas Sweep computations through the late-bound API.")]
		public void TestLateBoundPressureTransfer(){
			m_lateBound = true;
			TestPressureTransfer();
			m_lateBound = false;
		}
		#endregion

		private string[][] Load(string fileName){
			System.IO.StreamReader reader = new System.IO.StreamReader(fileName);
			ArrayList lines = new ArrayList();

			string line;
			char[] comma = new char[]{','};
			while ( (line = reader.ReadLine() ) != null ) {
				if (!line.StartsWith(";") && line.Length!=0 ) lines.Add(line.Split(comma));
			}

			reader.Close();

			return (string[][])lines.ToArray(typeof(string[]));
		}
	}
}