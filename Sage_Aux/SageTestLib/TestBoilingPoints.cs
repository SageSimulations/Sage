/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using System.IO;
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// ReSharper disable UnusedVariable

namespace Highpoint.Sage.Materials.Chemistry.BoilingPoints {

    [TestClass]
    public class BoilingPointTester {

        public BoilingPointTester(){Init();}

		private BasicReactionSupporter m_brs;

		[TestInitialize] 
		public void Init() {
			m_brs = new BasicReactionSupporter();

            string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)??".";
            string filename = directory + @"\..\..\..\SageTesting\PureComponentProperties.csv";
            Assert.IsTrue(File.Exists(filename), "Test data file not found - " + filename);

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

				if ( mt.Name.Equals("Hydrazine") ) {
					mt.SetAntoinesCoefficientsExt(76.858,-7245.2,0,0,-8.22,.0061557,1,double.NaN,double.NaN);
				}
				m_brs.MyMaterialCatalog.Add(mt);
			}
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		
		[TestMethod] 
        public void TestBoilingPoints(){
			Substance h2o = (Substance)m_brs.MyMaterialCatalog["Water"].CreateMass(1.0,31);

			double pressure_1Atm = 101325.0; // pascals.
			Console.WriteLine("BP of water is " + m_brs.MyMaterialCatalog["Water"].GetEstimatedBoilingPoint(pressure_1Atm) + ".");
			Console.WriteLine("BP of ethanol is " + m_brs.MyMaterialCatalog["Ethanol"].GetEstimatedBoilingPoint(pressure_1Atm) + ".");

			m_brs.MyMaterialCatalog.Add(new MaterialType(null,"Rock",Guid.NewGuid(),4.5,3.2,MaterialState.Solid,76,455));
			//			m_brs.MyMaterialCatalog["Rock"].SetVaporPressureCurveData(new double[]{1.0,2.0,3.0},new double[]{1.0,2.0,3.0});
			//			m_brs.MyMaterialCatalog["Rock"].SetAntoinesCoefficients3(3,6,9);
			//			m_brs.MyMaterialCatalog["Rock"].SetAntoinesCoefficientsExt(3,6);
			//			m_brs.MyMaterialCatalog["Rock"].SetAntoinesCoefficientsExt(3,6,9,12,15,18,21,24,27);
			//			m_brs.MyMaterialCatalog["Rock"].AddEmissionsClassifications(new string[]{"VOC","SARA","HAP","NATA","GHG","ODC"});


			Mixture m = new Mixture("Stone Soup");
			m.AddMaterial((Substance)m_brs.MyMaterialCatalog["Water"].CreateMass(10.0,37.0));
			m.AddMaterial((Substance)m_brs.MyMaterialCatalog["Rock"].CreateMass(10.0,37.0));
			m_brs.MyMaterialCatalog["Rock"].STPState = MaterialState.Solid;

			Console.WriteLine("BP of " + m.Name + " is " + m.GetEstimatedBoilingPoint(pressure_1Atm) + ".");

			m = new Mixture("Primordium");
			m.AddMaterial((Substance)m_brs.MyMaterialCatalog["Water"].CreateMass(10.0,37.0));
			m.AddMaterial((Substance)m_brs.MyMaterialCatalog["Ethanol"].CreateMass(10.0,37.0));

			Console.WriteLine("BP of " + m.Name + " is " + m.GetEstimatedBoilingPoint(pressure_1Atm) + ".");
		}

		[TestMethod] 
        public void TestBoilingPoints2(){


			m_brs.MyMaterialCatalog.Add(new MaterialType(null,"Tula4",Guid.NewGuid(),1.0,1.0,MaterialState.Solid,299,1.0));
			Substance heptane = (Substance)m_brs.MyMaterialCatalog["Heptane"].CreateMass(940,31);
			Substance methyleneChloride = (Substance)m_brs.MyMaterialCatalog["Methylene Chloride"].CreateMass(1036,31);
			Substance tula4 = (Substance)m_brs.MyMaterialCatalog["Tula4"].CreateMass(217.2,31);

			double pressure_1Atm = 101325.0; // pascals.
			Console.WriteLine("BP of Heptane is " + m_brs.MyMaterialCatalog["Heptane"].GetEstimatedBoilingPoint(pressure_1Atm) + ".");
			Console.WriteLine("BP of Methylene Chloride is " + m_brs.MyMaterialCatalog["Methylene Chloride"].GetEstimatedBoilingPoint(pressure_1Atm) + ".");
			Console.WriteLine("BP of Tula4 is " + m_brs.MyMaterialCatalog["Tula4"].GetEstimatedBoilingPoint(pressure_1Atm) + ".");

			Mixture m = new Mixture("Aamir's Soup");
			m.AddMaterial(heptane);
			m.AddMaterial(methyleneChloride);
			m.AddMaterial(tula4);

			Console.WriteLine("BP of " + m.Name + " is " + m.GetEstimatedBoilingPoint(pressure_1Atm) + ".");

			Substance water = (Substance)m_brs.MyMaterialCatalog["Water"].CreateMass(100,37);
			Console.WriteLine("\r\n...By the way, BP of water is " + water.GetEstimatedBoilingPoint(pressure_1Atm) + ".  ;-)");
		}

		[TestMethod] 
        public void TestBoilingPoints3(){

            MaterialType mtSodiumChloride = new MaterialType(null, "Potassium Carbonate", Guid.NewGuid(), 2.29, 4.17, MaterialState.Solid, 44.0, 1200);

            for (double d = 1.0; d >= 0.0; d -= .2) {

                Substance sodiumChloride = (Substance)mtSodiumChloride.CreateMass(100, 31);
                Substance acetone = (Substance)m_brs.MyMaterialCatalog["Acetone"].CreateMass(d*100, 31);

                double pressure_1Atm = 101325.0; // pascals.

                Mixture m = new Mixture("BP Tester");
                m.AddMaterial(sodiumChloride);
                m.AddMaterial(acetone);

                double ebp = m.GetEstimatedBoilingPoint(pressure_1Atm);
                Console.WriteLine("BP of " + m.ToString() + " is " + ebp + ".");
            }
		}

        [TestMethod]
        public void TestBoilingPointElevation() {
            m_brs.MyMaterialCatalog["Water"].EbullioscopicConstant = 0.512;
			double pressure_1Atm = 101325.0; // pascals.
            MaterialType salt = new MaterialType(null, "Sodium Chloride", Guid.NewGuid(), 1.0 / 2.6, 12.345, MaterialState.Solid, 58.443, 12.345);
            Mixture mixture = new Mixture();
            mixture.AddMaterial(m_brs.MyMaterialCatalog["Water"].CreateMass(1.0, 31));
            Console.WriteLine("Boiling point of " + mixture + " is " + mixture.GetEstimatedBoilingPoint(pressure_1Atm));

            for (int i = 0 ; i < 4 ; i++) {
                mixture.AddMaterial(salt.CreateMass(0.058443, 31));
                Console.WriteLine("Boiling point of " + mixture + " is " + mixture.GetEstimatedBoilingPoint(pressure_1Atm));
            }

        }

        private string[][] Load(string fileName) {
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
