/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using K = Highpoint.Sage.Materials.Chemistry.VaporPressure.VaporPressureTester.Constants;

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{
    /// <summary>
    /// Summary description for zTestTemperatureController.
    /// </summary>
    [TestClass]
	public class VaporPressureTester	{

		public class Constants : Highpoint.Sage.Materials.Chemistry.Constants {
			public static double kgPerPound = 0.453592;
			public static double pascalsPer_mmHg = 133.322;
			public static double cubicFtPerGallon = 0.134;
			public static double litersPerGallon = 3.7854118;
		}

		private BasicReactionSupporter m_brs;
		private Hashtable m_computedVaporPressureInPascals;

		public VaporPressureTester(){Init();}

		[TestInitialize] 
		public void Init() {
			m_brs = new BasicReactionSupporter();
			m_computedVaporPressureInPascals = new Hashtable();

            string filename = Environment.GetEnvironmentVariable("SAGE_ROOT");
            Assert.IsNotNull(filename, "environment variable \"SAGE_ROOT\" must point to the directory with the Sage solution files.");

            filename += @"\Sage_Aux\SageTesting\PureComponentProperties.csv";

            Assert.IsTrue(System.IO.File.Exists(filename), "Test data file not found - " + filename);

            string[][] data = Load(filename);
			
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
				MaterialType mt = new MaterialType(null,name,Guid.NewGuid(),1.0,1.0,MaterialState.Liquid,molWt,1.0);
				if ( antoineA!="" && antoineB!="" && antoineC!="" ) {
                    mt.SetAntoinesCoefficients3(double.Parse(antoineA), double.Parse(antoineB), double.Parse(antoineC), PressureUnits.mmHg, TemperatureUnits.Celsius);
				}
				//if (  henrysLawConstant != "" ) {
				//	mt.SetHenrysLawCoefficients(double.Parse(henrysLawConstant));
				//}

				if ( mt.Name.Equals("Hydrazine") ) {
					mt.SetAntoinesCoefficientsExt(76.858,-7245.2,0,0,-8.22,.0061557,1,double.NaN,double.NaN);
				}
				m_brs.MyMaterialCatalog.Add(mt);
				m_computedVaporPressureInPascals.Add(mt.Name,calcVPmmhg*K.pascalsPer_mmHg);
			}
		}

		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}

		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test runs a series of known scenarios, and compares their VP's to known-correct values.")]
		public void TestKnownVaporPressureValues(){
			string[] knownBad = new string[]{};//"Hydrazine"};
			ArrayList untestables = new ArrayList(knownBad);
			ArrayList testables = new ArrayList(m_brs.MyMaterialCatalog.MaterialTypes);

			foreach ( MaterialType mt in testables ) {
				if ( untestables.Contains(mt.Name) ) continue;
				double temperature = 35+K.CELSIUS_TO_KELVIN;
				double vp = VaporPressureCalculator.ComputeVaporPressure(mt,temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double svp = (double)m_computedVaporPressureInPascals[mt.Name];
				double pctError = Math.Abs(vp-svp)/Math.Max(vp,svp);
				string msg = string.Format("Computed VP({0}) at {1:F2} deg C was {2:F2}, refData was {3:F2}, for a {4:F2} percent error.",mt.Name,temperature,vp,svp,pctError*100);
				Trace.WriteLine(msg);
				System.Diagnostics.Debug.Assert(pctError < 0.01,"Vapor Pressure",mt.Name + " Vapor Pressure at " + temperature + " deg C == " + svp + " Pascals");
			}
		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test runs a vapor pressure computation on Hydrazine, which uses a Henry's Law computation.")]
        public void TestHenrysLawScenario() {
            MaterialType mt = m_brs.MyMaterialCatalog["Hydrazine"];
            double temperature = 35 + K.CELSIUS_TO_KELVIN;
            double vp = VaporPressureCalculator.ComputeVaporPressure(mt, temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
            double svp = (double)m_computedVaporPressureInPascals[mt.Name];
            double pctError = Math.Abs(vp - svp) / Math.Max(vp, svp);
            string msg = string.Format("Computed VP({0}) at {1:F2} deg C was {2:F2}, refData was {3:F2}, for a {4:F2} percent error.", mt.Name, temperature, vp, svp, pctError * 100);
            Trace.WriteLine(msg);
            System.Diagnostics.Debug.Assert(pctError < 0.01, "Vapor Pressure", mt.Name + " Vapor Pressure at 35 deg C == " + svp + " Pascals");
        }

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test runs a series of computations against known water vapor pressure computations.")]
		public void TestEmpiricalWaterVaporPressureScenarios(){
			double vp;
			MaterialType mt = m_brs.MyMaterialCatalog["Water"];
			foreach ( double[] row in empiricalVPOfWater ) {
				double temperature = row[0]+K.CELSIUS_TO_KELVIN;
				//if ( temperature != 35 ) continue;
				double empiricalVP = row[1];
				empiricalVP = empiricalVP * 1000.0; // kPa -> Pa.
                vp = VaporPressureCalculator.ComputeVaporPressure(mt, temperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
				double pctError = Math.Abs(vp-empiricalVP)/Math.Max(vp,empiricalVP);
				string msg = string.Format("Computed VP({0}) at {1:F2} deg C was {2:F2}, empirical was {3:F2}, for a {4:F2} percent error.",mt.Name,temperature,vp,empiricalVP,pctError*100);
				Trace.WriteLine(msg);
				System.Diagnostics.Debug.Assert(pctError < 0.01,"Water Vapor Pressure","Water Vapor Pressure at " + temperature + " deg C == " + empiricalVP );
			}

		}

		// http://www.psigate.ac.uk/newsite/reference/plambeck/chem2/p01045.htm
		// [0] = temperature (degC), [1] = vapor pressure (kPa), [2] = density (kg/m3).
		private static double[][] empiricalVPOfWater = new double[][]{
																		 new double[]{0.01,0.61173,0.99978},
																		 new double[]{1,   0.65716,0.99985},
																		 new double[]{4,   0.81359,0.99995},
																		 new double[]{5,   0.87260,0.99994},
																		 new double[]{10,   1.2281, 0.99969},
																		 new double[]{15,   1.7056, 0.99909},
																		 new double[]{20,   2.3388, 0.99819},
																		 new double[]{25,   3.1691, 0.99702},
																		 new double[]{30,   4.2455, 0.99561},
																		 new double[]{35,   5.6267, 0.99399},
																		 new double[]{40,   7.3814, 0.99217},
																		 new double[]{45,   9.5898, 0.99017},
																		 new double[]{50,   12.344, 0.98799},
																		 new double[]{55,   15.752, 0.98565},
																		 new double[]{60,   19.932, 0.98316},
																		 new double[]{65,   25.022, 0.98053},
																		 new double[]{70,   31.176, 0.97775},
																		 new double[]{75,   38.563, 0.97484},
																		 new double[]{80,   47.373, 0.97179},
																		 new double[]{85,   57.815, 0.96991},
																		 new double[]{90,   70.117, 0.96533},
																		 new double[]{95,   84.529, 0.96192},
																		 new double[]{100,  101.32,  0.95839},
																		 new double[]{105,  120.79,  0.95475}
																	 };


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