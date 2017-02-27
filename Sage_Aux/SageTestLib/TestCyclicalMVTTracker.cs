/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Materials;
using Highpoint.Sage.Materials.Chemistry;

//using Pfizer.MAI.Modeler.SOM;
//using Pfizer.MAI.Modeler.SOM.Behaviors;

namespace SchedulerDemoMaterial {

    /// <summary>
    /// Tests charge sources.
    /// </summary>
    [TestClass]
    public class MVTTrackerTester {

        public MVTTrackerTester(){Init();}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Trace.WriteLine( "Done." );
		}
		
        /// <summary>
        /// Exercises an MVTTracker.
        /// </summary>
		[TestMethod] public void TestMVTTracker(){
			Highpoint.Sage.SimCore.Model model = new Highpoint.Sage.SimCore.Model("MVTTracker model");
			BasicReactionSupporter brs = new BasicReactionSupporter();
			InitializeForTesting(brs);

			Mixture current = new Mixture(model,"current",Guid.NewGuid());
			current.AddMaterial(brs.MyMaterialCatalog["Water"].CreateMass(150,30));
			current.AddMaterial(brs.MyMaterialCatalog["Aluminum Hydroxide"].CreateMass(200,35));

			Mixture inflow = new Mixture(model,"inflow",Guid.NewGuid());
			inflow.AddMaterial(brs.MyMaterialCatalog["Acetone"].CreateMass(1500,70));
			inflow.AddMaterial(brs.MyMaterialCatalog["Hexane"].CreateMass(2000,90));

			Mixture outflow = new Mixture(model,"inflow",Guid.NewGuid());
			outflow.AddMaterial(brs.MyMaterialCatalog["Acetone"].CreateMass(1400,17));
			outflow.AddMaterial(brs.MyMaterialCatalog["Hexane"].CreateMass(1900,17));
			outflow.AddMaterial(brs.MyMaterialCatalog["Aluminum Hydroxide"].CreateMass(200,17));

			MassVolumeTracker cmvt = new MassVolumeTracker(brs.MyReactionProcessor);
			cmvt.SetInitialMixture(current);
			cmvt.SetInflowMixture(inflow);
			cmvt.SetOutflowMixture(outflow);
			cmvt.SetVesselCapacity(1000);

			cmvt.Process();

			//Trace.WriteLine("Temperatures: " + cmvt.TemperatureHistory.ToString());
			Trace.WriteLine("Masses      : " + cmvt.MassHistory.ToString());
			Trace.WriteLine("Volumes     : " + cmvt.VolumeHistory.ToString());

		}

        /// <summary>
        /// Exercises an MVTTracker.
        /// </summary>
        [TestMethod]
        public void TestMVTrackerWithNullMixtures() {
            Highpoint.Sage.SimCore.Model model = new Highpoint.Sage.SimCore.Model("MVTTracker model");
            BasicReactionSupporter brs = new BasicReactionSupporter();
            InitializeForTesting(brs);

            Mixture current = new Mixture(model, "current", Guid.NewGuid());
            current.AddMaterial(brs.MyMaterialCatalog["Water"].CreateMass(150, 30));
            current.AddMaterial(brs.MyMaterialCatalog["Aluminum Hydroxide"].CreateMass(200, 35));

            MassVolumeTracker cmvt = new MassVolumeTracker(brs.MyReactionProcessor);
            cmvt.SetInitialMixture(null);
            cmvt.SetInflowMixture(null);
            cmvt.SetOutflowMixture(null);
            cmvt.SetVesselCapacity(1000);

            cmvt.Process();

            //Trace.WriteLine("Temperatures: " + cmvt.TemperatureHistory.ToString());
            Trace.WriteLine("Masses      : " + cmvt.MassHistory.ToString());
            Trace.WriteLine("Volumes     : " + cmvt.VolumeHistory.ToString());

            System.Diagnostics.Debug.Assert(cmvt.MassHistory.ToString().Equals("[0/0/0/0]"));
            System.Diagnostics.Debug.Assert(cmvt.VolumeHistory.ToString().Equals("[0/0/0/0]"));
        }

		/// <summary>
		/// Initializes the specified model by populating the material catalog and reaction
		/// processor with sample material types and reactions.
		/// </summary>
		/// <param name="brs">The instance of ISupportsReactions that we will load..</param>
		private static void InitializeForTesting(BasicReactionSupporter brs){
			LoadSampleCatalog(brs.MyMaterialCatalog);
			LoadSampleReactions(brs);
		}

		/// <summary>
		/// Loads the material catalog in the model with sample materials.
		/// </summary>
		/// <param name="mcat">the Material Catalog to be initialized.</param>
		private static void LoadSampleCatalog(MaterialCatalog mcat){

			mcat.Add(new MaterialType(null, "Ethyl Acetate", Guid.NewGuid(),0.9020,1.9230,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Hydrochloric Acid", Guid.NewGuid(),1.1890,2.5500,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Ethanol", Guid.NewGuid(),0.8110,3.0000,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Hexane", Guid.NewGuid(),0.6700,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Triethylamine", Guid.NewGuid(),0.7290,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "MTBE", Guid.NewGuid(),0.7400,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Isopropyl Alcohol", Guid.NewGuid(),0.7860,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Acetone", Guid.NewGuid(),0.7899,4.1800,MaterialState.Liquid));
			mcat.Add(new MaterialType(null, "Methanol", Guid.NewGuid(),0.7920,4.1800,MaterialState.Liquid));
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
			mcat.Add(new MaterialType(null, "Water", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
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

			Trace.WriteLine(" ... sample substances loaded.");
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

	}
}
