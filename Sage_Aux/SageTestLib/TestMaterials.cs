/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Diagnostics;
using Highpoint.Sage.Materials.Chemistry.VaporPressure;

namespace Highpoint.Sage.Materials.Chemistry  {

	[TestClass]
	public class MaterialTester {

		public MaterialTester(){Init();}
        
		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Test to remove material from a mixture")]
		public void TestRemoval(){
			Model model = new Model("Test Model",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();
			InitializeForTesting(brs);

			Mixture mixture = new Mixture(model,"Contents of vat 1",Guid.NewGuid());
			MaterialCatalog cat = brs.MyMaterialCatalog;

			mixture.AddMaterial(cat["Acetone"].CreateMass(100,20));
			mixture.AddMaterial(cat["Potassium Sulfate"].CreateMass(100,20));
			mixture.AddMaterial(cat["Ammonia"].CreateMass(100,20));
			Debug.WriteLine("Mixture has the following stuff...");
			DiagnosticAids.DumpMaterial(mixture);
            Assert.IsTrue(mixture.Mass.Equals(300D),"Mixture is not 300 kg");

			Debug.WriteLine("Removing 100 kg of Acetone.");
			IMaterial matl = mixture.RemoveMaterial(cat["Acetone"],100);
			DiagnosticAids.DumpMaterial(matl);
            Assert.IsTrue(mixture.Mass.Equals(200D),"Mixture is not 200 kg");
			Debug.WriteLine("Remaining is the following mixture:");
			DiagnosticAids.DumpMaterial(mixture);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Test composite construction of material")]
		public void TestCompositeConstruction(){
			BasicReactionSupporter brs = new BasicReactionSupporter();
			InitializeForTesting(brs);
			MaterialCatalog cat = brs.MyMaterialCatalog;

			Mixture mixture = Mixture.Create(cat["Acetone"].CreateMass(100,20),
				cat["Potassium Sulfate"].CreateMass(100,20),
				cat["Ammonia"].CreateMass(100,20));

			Debug.WriteLine("Mixture has the following stuff...");
			DiagnosticAids.DumpMaterial(mixture);
            Assert.IsTrue(mixture.Mass.Equals(300D),"Mixture is not 300 kg");

			Debug.WriteLine("Removing 100 kg of Acetone.");
			IMaterial matl = mixture.RemoveMaterial(cat["Acetone"],100);
			DiagnosticAids.DumpMaterial(matl);
            Assert.IsTrue(mixture.Mass.Equals(200D),"Mixture is not 200 kg");
			Debug.WriteLine("Remaining is the following mixture:");
			DiagnosticAids.DumpMaterial(mixture);
		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test adds and removes substances to and from a mixture.  When adding the different substances with different masses and temperatures a temperature computation is performed.")]
                public void TestCombinatorics() {
            Model model = new Model("Test Model", Guid.NewGuid());
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);

            Mixture mixture = new Mixture(model, "Contents of vat 1", Guid.NewGuid());

            MaterialCatalog cat = brs.MyMaterialCatalog;
            AddSubstance(ref mixture, cat["Nitrous Acid"], 100, 20);
            AddSubstance(ref mixture, cat["Potassium Hydroxide"], 150, 41);
            AddSubstance(ref mixture, cat["Water"], 100, 100);
            Assert.IsTrue(mixture.Mass.Equals(350D), "Mass is not 350 kg");
            Assert.IsTrue(Math.Abs(mixture.Temperature - ( 18150D / 350D )) < 0.00001, "Temperature is not 51.86724 C.");

            IMaterial matl = mixture.RemoveMaterial(cat["Nitrous Acid"]);
            Debug.WriteLine("Removing all avaliable " + matl.MaterialType.Name);
            DiagnosticAids.DumpMaterial(mixture);
            Assert.IsTrue(mixture.Mass.Equals(250D), "Mass is not 250 kg");

            Debug.WriteLine("Adding " + matl.MaterialType.Name + " back in.");
            mixture.AddMaterial(matl);
            DiagnosticAids.DumpMaterial(mixture);
            Assert.IsTrue(mixture.Mass.Equals(350D), "Mass is not 350 kg");

            Debug.WriteLine("Removing 50 kg of the " + matl.MaterialType.Name);
            matl = mixture.RemoveMaterial(matl.MaterialType, 50.0);
            DiagnosticAids.DumpMaterial(mixture);
            Assert.IsTrue(mixture.Mass.Equals(300D), "Mass is not 300 kg");

        }
        [TestMethod]

        [Highpoint.Sage.Utility.FieldDescription("Test volumes of mixtures with various combinations of liquids and gases.")]
        public void TestVolumetricsOfDissolvedGases() {
            Model model = new Model("Test Model", Guid.NewGuid());
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);
            brs.MyMaterialCatalog.Add(new MaterialType(model, "Nitrous Oxide", Guid.NewGuid(), .001, 5, MaterialState.Gas));
            brs.MyMaterialCatalog.Add(new MaterialType(model, "Pixie Breath", Guid.NewGuid(), .001, 5, MaterialState.Gas));

            Mixture mixture = new Mixture(model, "Contents of vat 1", Guid.NewGuid());

            MaterialCatalog cat = brs.MyMaterialCatalog;
            AddSubstance(ref mixture, cat["Nitrous Oxide"], 100, 20);

            Assert.IsTrue(mixture.Volume.Equals(100000D), "Mass is not 10000 liters");

            AddSubstance(ref mixture, cat["Water"], 100, 50);
            Assert.IsTrue(mixture.Volume.Equals(100D), "Mass is not 100 liters");

            RemoveSubstance(ref mixture, cat["Water"], 100);
            Assert.IsTrue(mixture.Volume.Equals(100000D), "Mass is not 10000 liters");

            AddSubstance(ref mixture, cat["Pixie Breath"], 100, 20);
            Assert.IsTrue(mixture.Volume.Equals(200000D), "Mass is not 100 liters");

        }

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks the calculation of a simple mixing process.")]
		public void TestReactions(){
			Model model = new Model("Test Model",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();

			LoadSampleCatalog(brs.MyMaterialCatalog);

			// Define reaction
			Reaction r = new Reaction(null,"Reaction",Guid.NewGuid());
			r.AddReactant(brs.MyMaterialCatalog["Potassium Hydroxide"],0.544102);
			r.AddReactant(brs.MyMaterialCatalog["Nitrous Acid"],0.455898);
			r.AddProduct(brs.MyMaterialCatalog["Water"],0.174698);
			r.AddProduct(brs.MyMaterialCatalog["Potassium Nitrite"],0.825302);
			brs.MyReactionProcessor.AddReaction(r);
			
			Mixture mixture = new Mixture(model,"Contents of vat 1",Guid.NewGuid());
			brs.MyReactionProcessor.Watch(mixture);

			Debug.WriteLine("Adding two reactants to the mixture.");
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Potassium Hydroxide"],150,41);
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Nitrous Acid"],100,20);

			/* Calculation:
			 * 0.544102 + 0.455898 = 0.174698 + 0.825302
			 * 100 % Ractions = min(150kg / 0.544102, 100 kg / 0.455898) = about 219.35
			 * --> Left over Pot Hyd = 150 kg - (219.35 * 0.544102)	= about  30.65 kg
			 *     Water = 219.35 * 0.174698						= about  38.32 kg
			 *     Pot Nit = 219.35 * 0.825302						= about 181.03 kg
			 *														=		250.00 kg
			 */
			Substance water = null;
			Substance potassiumH = null;
			Substance potassiumN = null;
			foreach ( Substance s in mixture.Constituents ) {
				switch (s.Name) {
					case "Water": water = s; break;
					case "Potassium Hydroxide": potassiumH = s; break;
					case "Potassium Nitrite": potassiumN = s; break;
				}
			}

            Assert.IsTrue(Math.Abs(potassiumH.Mass - 30.65) < 0.01,"The Potassium Hidroxide part is not 30.65 kg");
            Assert.IsTrue(Math.Abs(water.Mass - 38.32) < 0.01,"The Water part is not 38.32 kg");
            Assert.IsTrue(Math.Abs(potassiumN.Mass - 181.03) < 0.01,"The Potassium Nitrite part is not 181.03 kg");

			Debug.WriteLine(mixture);

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks that two reactions can be defined in series")]
		public void TestSecondaryReactions(){
			Model model = new Model("Hello, world.",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();

			LoadSampleCatalog(brs.MyMaterialCatalog);

			// Add more substances to catalog
			brs.MyMaterialCatalog.Add(new MaterialType(model,"Tuberus Magnificus",Guid.NewGuid(),1.02,4.9,MaterialState.Solid,22.5));
			brs.MyMaterialCatalog.Add(new MaterialType(model,"French Fried Potatoes",Guid.NewGuid(),1.02,5.1,MaterialState.Liquid,22.8));

			// Define reactions
			Reaction r1 = new Reaction(null,"Reaction 1",Guid.NewGuid());
			r1.AddReactant(brs.MyMaterialCatalog["Caustic Soda"],0.5231);
			r1.AddReactant(brs.MyMaterialCatalog["Hydrochloric Acid"],0.4769);
			r1.AddProduct(brs.MyMaterialCatalog["Water"],0.2356);
			r1.AddProduct(brs.MyMaterialCatalog["Sodium Chloride"],0.7644);
			brs.MyReactionProcessor.AddReaction(r1);

			Reaction r2 = new Reaction(null,"Make French Fries",Guid.NewGuid());
			r2.AddReactant(brs.MyMaterialCatalog["Sodium Chloride"],0.1035);
			r2.AddReactant(brs.MyMaterialCatalog["Tuberus Magnificus"],0.8965);
			r2.AddProduct(brs.MyMaterialCatalog["French Fried Potatoes"],1.000);
			brs.MyReactionProcessor.AddReaction(r2);

			Mixture mixture = new Mixture(model,"Contents of vat 1",Guid.NewGuid());
			brs.MyReactionProcessor.Watch(mixture);

			AddSubstance(ref mixture,brs.MyMaterialCatalog["Caustic Soda"],40,20.0);
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Hydrochloric Acid"],40,20.0);
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Tuberus Magnificus"],40,20.0);
			mixture.Temperature = 100.0;

			Substance water = null;
			Substance hydrochloricAcid = null;
			Substance sodiumCloride = null;
			Substance ff = null;
			foreach ( Substance s in mixture.Constituents ) {
				switch (s.Name) {
					case "Water": water = s; break;
					case "Hydrochloric Acid": hydrochloricAcid = s; break;
					case "Sodium Chloride": sodiumCloride = s; break;
					case "French Fried Potatoes": ff = s; break;
				}
			}

            Assert.IsTrue(Math.Abs(hydrochloricAcid.Mass - 3.53) < 0.01,"The Hydrochloric Acid part is not 3.53 kg");
            Assert.IsTrue(Math.Abs(water.Mass - 18.01) < 0.01,"The Water part is not 18.01 kg");
            Assert.IsTrue(Math.Abs(sodiumCloride.Mass - 53.83) < 0.01,"The Sodium Cloride part is not 53.83 kg");
            Assert.IsTrue(Math.Abs(ff.Mass - 44.62) < 0.01,"The French Fries part is not 44.62 kg");
			
			Debug.WriteLine(mixture);

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks if mass can be removed from the mixture within a specified time")]
		public void TestRemovalByMassFromMixture(){
			Model model = new Model("Hello, world.",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();

			LoadSampleCatalog(brs.MyMaterialCatalog);

			// Define reactions
			Reaction r = new Reaction(null,"Reaction",Guid.NewGuid());
			r.AddReactant(brs.MyMaterialCatalog["Caustic Soda"],0.5231);
			r.AddReactant(brs.MyMaterialCatalog["Hydrochloric Acid"],0.4769);
			r.AddProduct(brs.MyMaterialCatalog["Water"],0.2356);
			r.AddProduct(brs.MyMaterialCatalog["Sodium Chloride"],0.7644);
			brs.MyReactionProcessor.AddReaction(r);

			Mixture mixture = new Mixture(model,"Test mixture 1",Guid.NewGuid());
			brs.MyReactionProcessor.Watch(mixture);
            
			// Add substances
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Caustic Soda"],40,20.0);
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Hydrochloric Acid"],40,20.0);
			mixture.Temperature = 100.0;

			// Definitions to retrive substances from mixture
			Substance water = null;
			Substance hydrochloricAcid = null;
			Substance sodiumCloride = null;
			foreach ( Substance su in mixture.Constituents ) {
				switch (su.Name) {
					case "Water": water = su; break;
					case "Hydrochloric Acid": hydrochloricAcid = su; break;
					case "Sodium Chloride": sodiumCloride = su; break;
				}
			}

            Assert.IsTrue(Math.Abs(hydrochloricAcid.Mass - 3.53) < 0.01,"The Hydrochloric Acid part is not 3.53 kg");
            Assert.IsTrue(Math.Abs(water.Mass - 18.01) < 0.01,"The Water part is not 18.01 kg");
            Assert.IsTrue(Math.Abs(sodiumCloride.Mass - 58.45) < 0.01,"The Sodium Cloride part is not 58.45 kg");
			
			// Remove 10 kg of water
			MaterialTransferSpecByMass tsbm = new MaterialTransferSpecByMass(brs.MyMaterialCatalog["Water"],10,TimeSpan.FromMinutes(5));

			Debug.WriteLine("Want to remove " + tsbm);
			IMaterial removee = tsbm.GetExtract(mixture);
			Debug.WriteLine("Successful in removing " + removee + ".\r\nWhat remains is ");
			Debug.WriteLine(mixture);

			foreach ( Substance su in mixture.Constituents ) {
				switch (su.Name) {
					case "Water": water = su; break;
					case "Hydrochloric Acid": hydrochloricAcid = su; break;
					case "Sodium Chloride": sodiumCloride = su; break;
				}
			}

            Assert.IsTrue(Math.Abs(hydrochloricAcid.Mass - 3.53) < 0.01, "The Hydrochloric Acid part is not 3.53 kg");
            Assert.IsTrue(Math.Abs(water.Mass - 8.01) < 0.01, "The Water part is not 8.01 kg");
            Assert.IsTrue(Math.Abs(sodiumCloride.Mass - 58.45) < 0.01, "The Sodium Chloride part is not 58.45 kg");
            Assert.IsTrue(tsbm.Duration.Equals(new TimeSpan(0, 0, 5, 0, 0)), "Removing 10 kg Water part did not take 5 Min");

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks if mass can be removed from a substance within a specified time")]
		public void TestRemovalByMassFromSubstance() {
			Model model = new Model("Hello, world.",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();

			LoadSampleCatalog(brs.MyMaterialCatalog);

			// Now try to remove by mass from a substance.
			Debug.WriteLine("\r\nWe now work on a substance.");
			MaterialType mt = brs.MyMaterialCatalog["Hydrochloric Acid"];
			Substance s = (Substance)mt.CreateMass(100,20);

			Debug.WriteLine("We have " + s);

			MaterialTransferSpecByMass tsbm = new MaterialTransferSpecByMass(brs.MyMaterialCatalog["Hydrochloric Acid"],10,TimeSpan.FromMinutes(5));

			Debug.WriteLine("Want to remove " + tsbm);
			IMaterial removee = tsbm.GetExtract(s);
			Debug.WriteLine("Successful in removing " + removee + ".\r\nWhat remains is ");
			Debug.WriteLine(s);

            Assert.IsTrue(Math.Abs(s.Mass - 90.00) < 0.01,"The Water part is not 90 kg");
            Assert.IsTrue(tsbm.Duration.Equals(new TimeSpan(0,0,5,0,0)), "Removing 10 kg Water part did not take 5 Min");

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks if mass in percentage can be removed from the mixture within a specified time")]
		public void TestRemovalByPercentageFromMixture(){
			Model model = new Model("Hello, world.",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();

			LoadSampleCatalog(brs.MyMaterialCatalog);

			// Define reactions
			Reaction r = new Reaction(null,"Reaction",Guid.NewGuid());
			r.AddReactant(brs.MyMaterialCatalog["Caustic Soda"],0.5231);
			r.AddReactant(brs.MyMaterialCatalog["Hydrochloric Acid"],0.4769);
			r.AddProduct(brs.MyMaterialCatalog["Water"],0.2356);
			r.AddProduct(brs.MyMaterialCatalog["Sodium Chloride"],0.7644);
			brs.MyReactionProcessor.AddReaction(r);

			Mixture mixture = new Mixture(model,"Test mixture 1",Guid.NewGuid());
			brs.MyReactionProcessor.Watch(mixture);
            
			// Add substances
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Caustic Soda"],40,20.0);
			AddSubstance(ref mixture,brs.MyMaterialCatalog["Hydrochloric Acid"],40,20.0);
			mixture.Temperature = 100.0;

			// Definitions to retrive substances from mixture
			Substance water = null;
			Substance hydrochloricAcid = null;
			Substance sodiumCloride = null;
			foreach ( Substance su in mixture.Constituents ) {
				switch (su.Name) {
					case "Water": water = su; break;
					case "Hydrochloric Acid": hydrochloricAcid = su; break;
					case "Sodium Chloride": sodiumCloride = su; break;
				}
			}

            Assert.IsTrue(Math.Abs(hydrochloricAcid.Mass - 3.53) < 0.01,"The Hydrochloric Acid part is not 3.53 kg");
            Assert.IsTrue(Math.Abs(water.Mass - 18.01) < 0.01,"The Water part is not 18.01 kg");
            Assert.IsTrue(Math.Abs(sodiumCloride.Mass - 58.45) < 0.01,"The Sodium Cloride part is not 58.45 kg");
			
			// Duration for removing mass given is per 1 kg
			MaterialTransferSpecByPercentage tsbp = new MaterialTransferSpecByPercentage(brs.MyMaterialCatalog["Water"],.5,TimeSpan.FromMinutes(5));

			IMaterial removee = tsbp.GetExtract(mixture);
			Debug.WriteLine("Want to remove " + tsbp);
			Debug.WriteLine("Successful in removing " + removee + ".\r\nWhat remains is ");
			Debug.WriteLine(mixture);

			// Now try to remove by mass from a substance.
			foreach ( Substance su in mixture.Constituents ) {
				switch (su.Name) {
					case "Water": water = su; break;
					case "Hydrochloric Acid": hydrochloricAcid = su; break;
					case "Sodium Chloride": sodiumCloride = su; break;
				}
			}

            Assert.IsTrue(Math.Abs(hydrochloricAcid.Mass - 3.53) < 0.01,"The Hydrochloric Acid part is not 3.53 kg");
            Assert.IsTrue(Math.Abs(water.Mass - 9.00) < 0.01,"The Water part is not 9.00 kg");
            Assert.IsTrue(Math.Abs(sodiumCloride.Mass - 58.45) < 0.01,"The Sodium Cloride part is not 58.45 kg");
            Assert.IsTrue(tsbp.Duration.Minutes == 45,"Removing 50% Water part did not take 5 Min"); // there are also a few seconds and miliseconds

		}
      
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks if mass in percentage can be removed from a substance within a specified time")]
		public void TestRemovalByPercentageFromSubstance() {
			Model model = new Model("Hello, world.",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();

			LoadSampleCatalog(brs.MyMaterialCatalog);

			Debug.WriteLine("\r\nWe now work on a substance.");
			MaterialType mt = brs.MyMaterialCatalog["Hydrochloric Acid"];
			Substance s = (Substance)mt.CreateMass(100,20);

			Debug.WriteLine("We have " + s);

			MaterialTransferSpecByPercentage tsbp = new MaterialTransferSpecByPercentage(brs.MyMaterialCatalog["Hydrochloric Acid"],.75,TimeSpan.FromMinutes(5));

			IMaterial removee = tsbp.GetExtract(s);
			Debug.WriteLine("Want to remove " + tsbp);
			Debug.WriteLine("Successful in removing " + removee + ".\r\nWhat remains is ");
			Debug.WriteLine(s);

            Assert.IsTrue(Math.Abs(s.Mass - 25.00) < 0.01,"The Water part is not 25 kg");
            Assert.IsTrue(tsbp.Duration.Hours == 6 && tsbp.Duration.Minutes == 15,"Removing 75% Water part did not take 5 Min");

		}
		
		public void TestVaporSpaceMixtureHandling(){
			Model model = new Model("Hello, world.",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();
			LoadSampleCatalog(brs.MyMaterialCatalog);

			Mixture mixture = new Mixture(model,"VaporTester",Guid.NewGuid());
			mixture.AddMaterial(brs.MyMaterialCatalog["Methanol"].CreateMass(200,37));
			mixture.AddMaterial(brs.MyMaterialCatalog["Methylene Chloride"].CreateMass(200,37));
			mixture.AddMaterial(brs.MyMaterialCatalog["Water"].CreateMass(200,37));

			double T = 315;
			double V = 2.0;
			Console.WriteLine("Starting with " + mixture + " and estimating vapor at " + T + " degrees C in a " + V + " cubic meter space.");
			Mixture vs = mixture.GetVaporFor(V,T);

			Console.WriteLine(vs.ToString());

		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the computed boiling point of a mixture with multiple materials each with valid antoines' coefficients.")]
        public void TestMixtureBoilingPointAntoines() {
            Model model = new Model("Test Model", Guid.NewGuid());
            MaterialType methanol = new MaterialType(null, "Methanol", Guid.NewGuid(), 0.7920, 4.1800, MaterialState.Liquid, 32);
            MaterialType water = new MaterialType(null, "Water", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid);

            methanol.SetAntoinesCoefficients3(7.879, 1473.1, 230, PressureUnits.mmHg, TemperatureUnits.Celsius);
            water.SetAntoinesCoefficients3(8.040, 1715.1, 232.4, PressureUnits.mmHg,TemperatureUnits.Celsius);

            Console.WriteLine("Materials are specified as percent-by-mass. Pressure is 1 ATM.");
            Console.WriteLine("Water, Methanol, BoilingPoint");
            for (double d = 0 ; d <= 100.0 ; d += 5.0) {
                Mixture m = new Mixture();
                m.AddMaterial(water.CreateMass(d, 273));
                m.AddMaterial(methanol.CreateMass(100.0 -d, 273));
                double bp = VaporPressure.VaporPressureCalculator.ComputeBoilingPoint(m, 101325);

                Debug.Assert(bp > 64 && bp < 101);
                Console.WriteLine("{0},{1},{2}", d, 100.0 - d, bp);
            }

        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test adds and removes substances to and from a mixture, ensuring material specifications are handled properly.")]
        public void TestMaterialSpecifications() {
			Model model = new Model("Test Model",Guid.NewGuid());
			BasicReactionSupporter brs = new BasicReactionSupporter();
			InitializeForTesting(brs);
			
			Guid city = Guid.NewGuid();
			Guid dist = Guid.NewGuid();
			Guid perr = Guid.NewGuid();
			MaterialType waterType = (MaterialType)brs.MyMaterialCatalog["Water"];

			Substance w1 = (Substance)waterType.CreateMass(100,37);
			w1.SetMaterialSpec(city,100);
			Debug.WriteLine("\r\nCreating 100 kg of City Water (" + city + ").");
			DumpMaterialSpecs(w1);

			Substance w2 = (Substance)waterType.CreateMass(40,37);
			w2.SetMaterialSpec(dist,40);
			Debug.WriteLine("\r\nCreating 40 kg of Distilled Water (" + dist + ").");
			DumpMaterialSpecs(w2);

			Debug.WriteLine("\r\nAdding city water to distilled water.");
			w1.Add(w2);
			DumpMaterialSpecs(w1);

			Debug.WriteLine("\r\nRemoving 60 kg of the blended water.");
			Substance w3 = w1.Remove(60);
			DumpMaterialSpecs(w1);
			DumpMaterialSpecs(w3);

			Debug.WriteLine("\r\nCloning the blended water.");
			Substance w4 = (Substance)w3.Clone();
			DumpMaterialSpecs(w4);

			Debug.WriteLine("\r\nPurifying the cloned blended water.");
			w4.ConvertMaterialSpec(dist,city);
			Debug.WriteLine("Purified clone:");
			DumpMaterialSpecs(w4);
			Debug.WriteLine("...and original water:");
			DumpMaterialSpecs(w3);

			double amtDistilled = w1.GetMaterialSpec(dist);
			Debug.WriteLine("\r\nThere are " + amtDistilled + " kg of distilled water.");

			w1.ConvertMaterialSpec(dist,city);
			Debug.WriteLine("\r\nConverting distilled water to city water.");
			DumpMaterialSpecs(w1);

		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test adds and removes substances to and from a mixture, ensuring material specifications are handled properly.")]
        public void TestChangeNotifications() {

            Model model = new Model("Test Model", Guid.NewGuid());
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);

            MaterialType waterType = (MaterialType)brs.MyMaterialCatalog["Water"];

            string results = string.Empty;
            Substance w1 = (Substance)waterType.CreateMass(100, 37);
            w1.MaterialChanged += new MaterialChangeListener(delegate(IMaterial material, MaterialChangeType type) {
                results += string.Format("{0} changed in {1}\r\n", material, type); 
            });

            w1.Temperature = 85.0;
            w1.Add((Substance)waterType.CreateMass(100, 37));
            Assert.IsTrue(results.Equals(RESULT1));

            results = string.Empty;
            w1.SuspendChangeEvents();
            w1.Temperature = 95.0;
            w1.Add((Substance)waterType.CreateMass(100, 37));
            w1.ResumeChangeEvents(false);
            Assert.IsTrue(results.Equals(string.Empty));

            results = string.Empty;
            w1.SuspendChangeEvents();
            w1.Temperature = 95.0;
            w1.Add((Substance)waterType.CreateMass(100, 37));
            w1.ResumeChangeEvents(true);
            Assert.IsTrue(results.Equals(RESULT2));

            // NOW, SAME TEST, BUT ON A MIXTURE INSTEAD.
            MaterialType acetoneType = (MaterialType)brs.MyMaterialCatalog["Acetone"];

            Mixture m1 = new Mixture("My Goo");
            m1.MaterialChanged += new MaterialChangeListener(delegate(IMaterial material, MaterialChangeType type) {
                results += string.Format("{0} changed in {1}\r\n", material, type);
            });

            results = string.Empty;
            m1.AddMaterial((Substance)waterType.CreateMass(100, 37));
            m1.AddMaterial((Substance)acetoneType.CreateMass(100, 45));
            Assert.IsTrue(results.Equals(RESULT3));

            results = string.Empty;
            m1.SuspendChangeEvents();
            m1.AddMaterial((Substance)waterType.CreateMass(100, 99));
            m1.AddMaterial((Substance)acetoneType.CreateMass(100, 99));
            Assert.IsTrue(results.Equals(string.Empty));
            m1.ResumeChangeEvents(false);
            Assert.IsTrue(results.Equals(string.Empty));

            results = string.Empty;
            m1.SuspendChangeEvents();
            m1.AddMaterial((Substance)waterType.CreateMass(100, 99));
            m1.AddMaterial((Substance)acetoneType.CreateMass(100, 99));
            Assert.IsTrue(results.Equals(string.Empty));
            m1.ResumeChangeEvents(true);
            Assert.IsTrue(results.Equals(RESULT4));

        }

        private static readonly string RESULT1 = "Substance (85.0 deg C) of 100.00 kg of Water changed in Temperature\r\nSubstance (61.0 deg C) of 200.00 kg of Water changed in Temperature\r\nSubstance (61.0 deg C) of 200.00 kg of Water changed in Contents\r\n";
        private static readonly string RESULT2 = "Substance (80.5 deg C) of 400.00 kg of Water changed in Contents\r\nSubstance (80.5 deg C) of 400.00 kg of Water changed in Temperature\r\n";
        private static readonly string RESULT3 = "Mixture (37.0 deg C) of 100.00 kg of Water changed in Temperature\r\nMixture (37.0 deg C) of 100.00 kg of Water changed in Contents\r\nMixture (41.0 deg C) of 100.00 kg of Water and 100.00 kg of Acetone changed in Temperature\r\nMixture (41.0 deg C) of 100.00 kg of Water and 100.00 kg of Acetone changed in Contents\r\n";
        private static readonly string RESULT4 = "Mixture (79.7 deg C) of 300.00 kg of Water and 300.00 kg of Acetone changed in Contents\r\nMixture (79.7 deg C) of 300.00 kg of Water and 300.00 kg of Acetone changed in Temperature\r\n";

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test adds and removes substances to and from a mixture, ensuring material specifications are handled properly.")]
        public void TestReactions2()
        {

            Model model = new Model("Test Model", Guid.NewGuid());
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);

            MaterialType waterType = (MaterialType)brs.MyMaterialCatalog["Water"];
            MaterialType ethanolType = (MaterialType)brs.MyMaterialCatalog["Ethanol"];

            Reaction r1 = new Reaction(model, "Reaction 1", Guid.NewGuid());
                r1.AddReactant(brs.MyMaterialCatalog["Water"], 2.0);
                r1.AddProduct(brs.MyMaterialCatalog["Ethanol"], 2.0);
                r1.HeatOfReaction = 0;
                brs.MyReactionProcessor.AddReaction(r1);

            string actualResults = "";
            Mixture m = new Mixture(model, "Mixture");

            r1.ReactionGoingToHappenEvent += new ReactionGoingToHappenEvent(delegate (Reaction reaction, Mixture mixture) {
                actualResults += ("\r\nBefore = " + m.ToString("F2", "F4"));
            });
            r1.ReactionHappenedEvent += new ReactionHappenedEvent(delegate (ReactionInstance reactionInstance) {
                actualResults += ("\r\nAfter = " + m.ToString("F2", "F4") + "\r\n");
            });

            brs.MyReactionProcessor.Watch(m);

            m.AddMaterial(waterType.CreateMass(100, 37));

            m.AddMaterial(waterType.CreateMass(10, 37));

            Console.WriteLine(actualResults);
            string expected =
                "\r\nBefore = Mixture (37.00 deg C) of 100.0000 kg of Water\r\nAfter = Mixture (37.00 deg C) of 100.0000 kg of Ethanol\r\n\r\nBefore = Mixture (37.00 deg C) of 100.0000 kg of Ethanol and 10.0000 kg of Water\r\nAfter = Mixture (37.00 deg C) of 110.0000 kg of Ethanol\r\n";
            Assert.AreEqual(expected, actualResults);

        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test exception throw on illegal reaction definition.")]
        [ExpectedException(typeof(ReactionDefinitionException), "Permitted creation of a faulty reaction (same product and reactants.)")]
        public void TestBadReactionDefinition1()
        {

            Model model = new Model("Test Model", Guid.NewGuid());
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);

            MaterialType waterType = (MaterialType)brs.MyMaterialCatalog["Water"];

            Reaction r1 = new Reaction(model, "Reaction 1", Guid.NewGuid());
            r1.AddReactant(brs.MyMaterialCatalog["Water"], 2.0);
            r1.AddProduct(brs.MyMaterialCatalog["Water"], 2.0);
            r1.HeatOfReaction = 0;
            brs.MyReactionProcessor.AddReaction(r1);

        }


        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test adds and removes substances to and from a mixture, ensuring material specifications are handled properly.")]
        public void TestReactions3() {
            Model model = new Model("Test Model", Guid.NewGuid());
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);

            MaterialType potassiumSulfateType = (MaterialType)brs.MyMaterialCatalog["Potassium Sulfate"];
            MaterialType acetoneType = (MaterialType)brs.MyMaterialCatalog["Acetone"];
            MaterialType hexaneType = (MaterialType)brs.MyMaterialCatalog["Hexane"];


            Reaction r1 = new Reaction(model, "Reaction 1", Guid.NewGuid());
            r1.AddReactant(potassiumSulfateType, 1.0);
            r1.AddReactant(acetoneType, 2.5);
            r1.AddProduct(hexaneType, 3.0);
            r1.AddProduct(acetoneType, 0.5);
            r1.HeatOfReaction = 0;

            brs.MyReactionProcessor.AddReaction(r1);

            Mixture m = new Mixture(model, "Mixture");
            string results = "";

            r1.ReactionGoingToHappenEvent += new ReactionGoingToHappenEvent(delegate(Reaction reaction, Mixture mixture) {
                results += ("\r\nBefore = " + m.ToString("F2","F4"));
            });
            r1.ReactionHappenedEvent += new ReactionHappenedEvent(delegate(ReactionInstance reactionInstance) {
                results += ("\r\nAfter = " + m.ToString("F2", "F4") + "\r\n");
            });


            brs.MyReactionProcessor.Watch(m);

            Console.WriteLine(r1.ToString());

            m.AddMaterial(potassiumSulfateType.CreateMass(100, 37));
            m.AddMaterial(acetoneType.CreateMass(100, 37));

            Console.WriteLine(results);
           Assert.AreEqual(results, EXPECTED_3);
        }

	    private static string EXPECTED_3 =
	        "\r\nBefore = Mixture (37.00 deg C) of 100.0000 kg of Potassium Sulfate and 100.0000 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 120.0000 kg of Hexane, 60.0000 kg of Potassium Sulfate and 20.0000 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 120.0000 kg of Hexane, 60.0000 kg of Potassium Sulfate and 20.0000 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 144.0000 kg of Hexane, 52.0000 kg of Potassium Sulfate and 4.0000 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 144.0000 kg of Hexane, 52.0000 kg of Potassium Sulfate and 4.0000 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 148.8000 kg of Hexane, 50.4000 kg of Potassium Sulfate and 0.8000 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 148.8000 kg of Hexane, 50.4000 kg of Potassium Sulfate and 0.8000 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 149.7600 kg of Hexane, 50.0800 kg of Potassium Sulfate and 0.1600 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 149.7600 kg of Hexane, 50.0800 kg of Potassium Sulfate and 0.1600 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 149.9520 kg of Hexane, 50.0160 kg of Potassium Sulfate and 0.0320 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 149.9520 kg of Hexane, 50.0160 kg of Potassium Sulfate and 0.0320 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 149.9904 kg of Hexane, 50.0032 kg of Potassium Sulfate and 0.0064 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 149.9904 kg of Hexane, 50.0032 kg of Potassium Sulfate and 0.0064 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 149.9981 kg of Hexane, 50.0006 kg of Potassium Sulfate and 0.0013 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 149.9981 kg of Hexane, 50.0006 kg of Potassium Sulfate and 0.0013 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 149.9996 kg of Hexane, 50.0001 kg of Potassium Sulfate and 0.0003 kg of Acetone\r\n\r\nBefore = Mixture (37.00 deg C) of 149.9996 kg of Hexane, 50.0001 kg of Potassium Sulfate and 0.0003 kg of Acetone\r\nAfter = Mixture (37.00 deg C) of 149.9999 kg of Hexane, 50.0000 kg of Potassium Sulfate and 0.0001 kg of Acetone\r\n";

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test has a supplier push to a consumer over a time period.")]
        public void TestMaterialTransferrer() {

            #region Create and initialize the model
            Model model = new Model("Test Model", Guid.NewGuid());
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);
            #endregion Create and initialize the model

            #region Create handles to material types
            MaterialType potassiumSulfateType = (MaterialType)brs.MyMaterialCatalog["Potassium Sulfate"];
            MaterialType acetoneType = (MaterialType)brs.MyMaterialCatalog["Acetone"];
            MaterialType hexaneType = (MaterialType)brs.MyMaterialCatalog["Hexane"];
            MaterialType waterType = (MaterialType)brs.MyMaterialCatalog["Water"];
            #endregion Create handles to material types

            #region Create the source mixture
            Mixture from = new Mixture("From");
            from.AddMaterial(potassiumSulfateType.CreateMass(100, 37));
            from.AddMaterial(acetoneType.CreateMass(100, 37));
            from.AddMaterial(hexaneType.CreateMass(100, 37));
            #endregion Create the source mixture

            #region Create the destination mixture
            Mixture to = new Mixture("To");
            to.AddMaterial(waterType.CreateMass(55, 50));
            #endregion Create the destination mixture

            #region Create the mixture that will act as the exemplar
            Mixture what = new Mixture("What");
            what.AddMaterial(potassiumSulfateType.CreateMass(25, 37));
            what.AddMaterial(acetoneType.CreateMass(35, 37));
            what.AddMaterial(hexaneType.CreateMass(45, 37));
            #endregion Create the mixture that will act as the exemplar

            TimeSpan duration = TimeSpan.FromHours(2.0);
            MaterialTransferrer mxfr = new MaterialTransferrer(model, ref from, ref to, what, duration);

            // Create the start event for the transfer.
            DateTime startAt = new DateTime(2009, 2, 23, 3, 0, 0);
            model.Executive.RequestEvent(delegate(IExecutive exec, object userData) { mxfr.Start(); }, startAt, 0.0, null);
            
            // Create a pause-til-start event.
            model.Executive.RequestEvent(delegate(IExecutive exec, object userData) {
                Console.WriteLine("Waiting for start.");
                mxfr.BlockTilStart();
                Console.WriteLine("Start has occurred!");
            }, startAt - TimeSpan.FromHours(2.0), 0.0, null, ExecEventType.Detachable);

            // Create a pause-til-done event.
            model.Executive.RequestEvent(delegate(IExecutive exec, object userData) {
                Console.WriteLine("Waiting for finish.");
                mxfr.BlockTilDone();
                Console.WriteLine("Finish has occurred!");
            }, startAt - TimeSpan.FromHours(2.0), 0.0, null, ExecEventType.Detachable);


            // Create a series of (unrelated) events that will force updates during the transfer.
            DateTime update = startAt;
            while (update <= ( startAt + duration )) {
                model.Executive.RequestEvent(delegate(IExecutive exec, object userData) { Console.WriteLine("{2} : From = {0}\r\nTo = {1}", from, to, exec.Now); }, update, 0.0, null);
                update += TimeSpan.FromMinutes(20.0);
            }

            model.Start();

        }

        private void DumpMaterialSpecs(Substance s) {
			Debug.WriteLine(s.ToString());
			foreach ( DictionaryEntry de in s.GetMaterialSpecs() ) Debug.WriteLine("\t"+de.Key + " : " + de.Value);
		}
		
		#region Private Support Goo
		private void AddSubstance(ref Mixture mixture, MaterialType matType, double mass, double temp){
			IMaterial matl = matType.CreateMass(mass,temp);
			double energy = (temp+Highpoint.Sage.Materials.Chemistry.Constants.CELSIUS_TO_KELVIN)*mass*matType.SpecificHeat;
			Debug.WriteLine(String.Format("Adding {0} - {1} kg, {2} C, and {3} liters. ({4} Joules of thermal energy)",matl.MaterialType.Name,matl.Mass,matl.Temperature,matl.Volume,energy));
			mixture.AddMaterial(matl);

			Debug.WriteLine("Mixture is now:");
			DiagnosticAids.DumpMaterial(mixture);
		}

		private void RemoveSubstance(ref Mixture mixture, MaterialType matType, double mass){
			IMaterial matl = matType.CreateMass(mass,0.0);
			Debug.WriteLine(String.Format("Removing {0} - {1} kg ({2} liters).",matl.MaterialType.Name,matl.Mass,matl.Volume));
			IMaterial whatIGot = mixture.RemoveMaterial(matl.MaterialType,mass);
			Debug.WriteLine(String.Format("I got {0} - {1} kg, {2} C, and {3} liters.",whatIGot.MaterialType.Name,whatIGot.Mass,whatIGot.Temperature,whatIGot.Volume));
			Debug.WriteLine("Mixture is now:");
			DiagnosticAids.DumpMaterial(mixture);
		}


		/// <summary>
		/// Initializes the specified model by populating the material catalog and reaction
		/// processor with sample material types and reactions.
		/// </summary>
		/// <param name="brs">The instance of ISupportsReactions that we will load..</param>
		public static void InitializeForTesting(BasicReactionSupporter brs){
			LoadSampleCatalog(brs.MyMaterialCatalog);
			LoadSampleReactions(brs);
		}

		/// <summary>
		/// Loads the material catalog in the model with sample materials.
		/// </summary>
		/// <param name="mcat">the Material Catalog to be initialized.</param>
		private static void LoadSampleCatalog(MaterialCatalog mcat){

			//Console.WriteLine("Warning: Specification is creating a MaterialType without propagating its molecular weight, emissions classifications or VaporPressure constants.");
			mcat.Add(new MaterialType(null, "Ethyl Acetate", Guid.NewGuid(),0.9020,1.9230,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Hydrochloric Acid", Guid.NewGuid(),1.1890,2.5500,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Ethanol", Guid.NewGuid(),0.8110,3.0000,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Hexane", Guid.NewGuid(),0.6700,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Triethylamine", Guid.NewGuid(),0.7290,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "MTBE", Guid.NewGuid(),0.7400,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Isopropyl Alcohol", Guid.NewGuid(),0.7860,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Acetone", Guid.NewGuid(),0.7899,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Propyl Alcohol", Guid.NewGuid(),0.8035,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Butanol", Guid.NewGuid(),0.8100,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Aluminum Hydroxide", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Ammonia", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Ammonium Hydroxide", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Carbon Dioxide", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Manganese Sulfate", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Nitrous Acid", Guid.NewGuid(), 1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Potassium Phosphate", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Potassium Sulfate", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Sulfate", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Manganese Dioxide", Guid.NewGuid(),1.3000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Potassium Hydroxide", Guid.NewGuid(),1.3000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Bromide", Guid.NewGuid(),1.3000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Bisulfite", Guid.NewGuid(),1.4800,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Titanium Dioxide", Guid.NewGuid(),1.5000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Titanium Tetrachloride", Guid.NewGuid(),1.5000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Nitrate", Guid.NewGuid(),1.6000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Phosphoric Acid", Guid.NewGuid(),1.6850,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Sulfide", Guid.NewGuid(),1.8580,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Caustic Soda", Guid.NewGuid(),2.0000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Chloride", Guid.NewGuid(),2.1650,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Bicarbonate", Guid.NewGuid(),2.2000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Nitrite", Guid.NewGuid(),2.3800,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Potassium Nitrite", Guid.NewGuid(),1.9150,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sodium Carbonate", Guid.NewGuid(),2.5330,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Pot. Permanganate", Guid.NewGuid(),2.7000,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Bromine", Guid.NewGuid(),3.1200,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Sulfuric Acid", Guid.NewGuid(),1.8420,4.1800,MaterialState.Liquid));
			
			mcat.Add(new MaterialType(null, "Methanol", Guid.NewGuid(),0.7920,4.1800,MaterialState.Liquid,32));
			mcat.Add(new MaterialType(null, "Methylene Chloride", Guid.NewGuid(),2.15,4.1800,MaterialState.Liquid,85));
			mcat.Add(new MaterialType(null, "Water", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));

            mcat["Methanol"].SetAntoinesCoefficients3(7.879, 1473.1, 230, PressureUnits.mmHg, TemperatureUnits.Celsius);
            mcat["Methylene Chloride"].SetAntoinesCoefficients3(7.263, 1222.7, 238.4, PressureUnits.mmHg, TemperatureUnits.Celsius);
            mcat["Water"].SetAntoinesCoefficients3(8.040, 1715.1, 232.4, PressureUnits.mmHg, TemperatureUnits.Celsius);

			Debug.WriteLine(" ... sample substances loaded.");
		}    

		/// <summary>
		/// Loads the reaction processor in the model with sample reactions.
		/// </summary>
		/// <param name="brs">The instance of ISupportsReactions that we will load..</param>
		private static void LoadSampleReactions(BasicReactionSupporter brs){

			Reaction r1 = new Reaction(null,"Reaction 1",Guid.NewGuid());
			r1.AddReactant(brs.MyMaterialCatalog["Caustic Soda"],0.5231);
			r1.AddReactant(brs.MyMaterialCatalog["Hydrochloric Acid"],0.4769);
			r1.AddProduct(brs.MyMaterialCatalog["Water"],0.2356);
			r1.AddProduct(brs.MyMaterialCatalog["Sodium Chloride"],0.7644);
			brs.MyReactionProcessor.AddReaction(r1);

			Reaction r2 = new Reaction(null,"Reaction 2",Guid.NewGuid());
			r2.AddReactant(brs.MyMaterialCatalog["Sulfuric Acid"],0.533622);
			r2.AddReactant(brs.MyMaterialCatalog["Potassium Hydroxide"],0.466378);
			r2.AddProduct(brs.MyMaterialCatalog["Water"],0.171333);
			r2.AddProduct(brs.MyMaterialCatalog["Potassium Sulfate"],0.828667);
			brs.MyReactionProcessor.AddReaction(r2);

			Reaction r3 = new Reaction(null,"Reaction 3",Guid.NewGuid());
			r3.AddReactant(brs.MyMaterialCatalog["Caustic Soda"],0.459681368);
			r3.AddReactant(brs.MyMaterialCatalog["Nitrous Acid"],0.540318632);
			r3.AddProduct(brs.MyMaterialCatalog["Water"],0.207047552);
			r3.AddProduct(brs.MyMaterialCatalog["Sodium Nitrite"],0.792952448);
			brs.MyReactionProcessor.AddReaction(r3);

			Reaction r4 = new Reaction(null,"Reaction 4",Guid.NewGuid());
			r4.AddReactant(brs.MyMaterialCatalog["Potassium Hydroxide"],0.544102);
			r4.AddReactant(brs.MyMaterialCatalog["Nitrous Acid"],0.455898);
			r4.AddProduct(brs.MyMaterialCatalog["Water"],0.174698);
			r4.AddProduct(brs.MyMaterialCatalog["Potassium Nitrite"],0.825302);
			brs.MyReactionProcessor.AddReaction(r4);

		}

		#endregion
	}
}
