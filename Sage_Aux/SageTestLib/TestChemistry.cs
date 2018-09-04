/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Materials.Chemistry {

	/// <summary>
	/// Summary description for zTestChemistry.
	/// </summary>
    [TestClass]
	public class Chemistry101 {
        public Chemistry101() {Init();}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}


        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test checks the basic reaction of two substances including the mixture temperature")]
        public void TestReactionBasics() {
            BasicReactionSupporter brs = new BasicReactionSupporter();
            Initialize(brs);

            Mixture mixture = new Mixture(null, "test mixture");
            mixture.OnReactionHappened += new ReactionHappenedEvent(mixture_OnReactionHappened);

            brs.MyReactionProcessor.Watch(mixture);
            mixture.AddMaterial(brs.MyMaterialCatalog["Hydrochloric Acid"].CreateMass(10, 20));
            mixture.AddMaterial(brs.MyMaterialCatalog["Caustic Soda"].CreateMass(12, 44));


            foreach ( IMaterial constituent in mixture.Constituents ) {
                Debug.WriteLine(constituent.ToString());
            }

            /* Calculation mass:
             * 0.5231 + 0.4769 = 0.2356 + 0.7644
             * 100 % Ractions = min(10 kg / 0.4769, 12 kg / 0.5231)	= about  20.97
             * --> Left over Caustic Soda = 12 kg - (20.97 * 0.5231)= about   1.03 kg
             *     Water = 20.97 * 0.2356							= about   4.94 kg
             *     Sodium Cloride = 20.97 * 0.7644					= about  16.03 kg
             *														=		 22.00 kg
             * Calculation temperature:
             * Hydrochloric Acid specific heat = 4.18
             * Caustic Soda specific heat = 2.55
             * Heat of Hydrochloric Acid is 10 kg * 2.55 * 20 dC	= about  510.00 dC
             * Heat of Caustic Soda is 12 kg * 4.18 * 44 dC			= about 2207.04 dC
             * Subtotal												= about 2717.04 dC
             * Divide by 10 * 2.55 + 12 * 4.18						= 75.66
             * Mixture temperature is 2717.04 / 75.66				= about   35.91 dC
             */

            Substance water = null;
            Substance sodium = null;
            Substance soda = null;
            foreach ( Substance s in mixture.Constituents ) {
                switch ( s.Name ) {
                    case "Water":
                        water = s;
                        break;
                    case "Sodium Chloride":
                        sodium = s;
                        break;
                    case "Caustic Soda":
                        soda = s;
                        break;
                }
            }

            Assert.IsTrue(Math.Abs(soda.Mass - 1.03) < 0.01, "The Caustic Soda part is not 2.88 kg");
            Assert.IsTrue(Math.Abs(water.Mass - 4.94) < 0.01, "The Water part is not 4.5 kg");
            Assert.IsTrue(Math.Abs(sodium.Mass - 16.03) < 0.01, "The Sodium Cloride part is not 14.61 kg");
            Console.WriteLine(mixture.Temperature);
            Assert.IsTrue(Math.Abs(mixture.Temperature - 35.91) < 0.01, "The temperature is not 33.09 degrees C");

        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test checks the basic reaction of two substances and a catalyst.")]
        public void TestCatalyticReactionBasics() {
            BasicReactionSupporter brs = new BasicReactionSupporter();
            Initialize(brs);

            int nReactions = 0;
            Mixture mixture = new Mixture(null, "test mixture");
            mixture.OnReactionHappened += new ReactionHappenedEvent(mixture_OnReactionHappened);
            mixture.OnReactionHappened += new ReactionHappenedEvent(delegate(ReactionInstance r) { nReactions++; });

            brs.MyReactionProcessor.Watch(mixture);
            mixture.AddMaterial(brs.MyMaterialCatalog["Hydrogen"].CreateMass(10, 20));
            mixture.AddMaterial(brs.MyMaterialCatalog["Oil"].CreateMass(12, 44));


            foreach ( IMaterial constituent in mixture.Constituents ) {
                Debug.WriteLine(constituent.ToString());
            }

            mixture.AddMaterial(brs.MyMaterialCatalog["Palladium"].CreateMass(1, 44));

            foreach ( IMaterial constituent in mixture.Constituents ) {
                Debug.WriteLine(constituent.ToString());
            }

            Assert.IsTrue(nReactions == 1, String.Format("Reaction occurred {0} times, but should have happened once.", nReactions));
            if ( nReactions != 1 ) {
                Console.WriteLine("Test failed. Catalytic reaction happened {0} times, but should only have happened once.", nReactions);
                if ( System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

            // Now check that a reaction without the catalyst does not happen.
            nReactions = 0;
            mixture = new Mixture(null, "test mixture");
            mixture.OnReactionHappened += new ReactionHappenedEvent(mixture_OnReactionHappened);
            mixture.OnReactionHappened += new ReactionHappenedEvent(delegate(ReactionInstance r) { nReactions++; });

            brs.MyReactionProcessor.Watch(mixture);
            mixture.AddMaterial(brs.MyMaterialCatalog["Hydrogen"].CreateMass(10, 20));
            mixture.AddMaterial(brs.MyMaterialCatalog["Oil"].CreateMass(12, 44));


            foreach ( IMaterial constituent in mixture.Constituents ) {
                Debug.WriteLine(constituent.ToString());
            }

            foreach ( IMaterial constituent in mixture.Constituents ) {
                Debug.WriteLine(constituent.ToString());
            }

            Assert.IsTrue(nReactions == 0, String.Format("Reaction occurred {0} times, but should not have happened.", nReactions));
            if ( nReactions != 0 ) {
                Console.WriteLine("Test failed. Catalytic reaction happened {0} times, but should not have happened.", nReactions);
                if ( System.Diagnostics.Debugger.IsAttached )
                    System.Diagnostics.Debugger.Break();
            }

        }

        [TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test checks that materials only can be combined, if there is a reaction definition")]
        public void TestRP_CombineAPI(){
			// AEL: I am not sure if we should test more here or if it is sufficient to check if reactions happened or not?
            BasicReactionSupporter brs = new BasicReactionSupporter();
            Initialize(brs);

            IMaterial m1 = brs.MyMaterialCatalog["Hydrochloric Acid"].CreateMass(10,20);
            IMaterial m2 = brs.MyMaterialCatalog["Caustic Soda"].CreateMass(12,44);
            IMaterial m3 = brs.MyMaterialCatalog["Nitrous Acid"].CreateMass(2,60);

            IMaterial resultA;
            ArrayList observedReactions, observedReactionInstances;

            Debug.WriteLine("Part A : Reaction should happen...");
            bool reactionAHappened = brs.MyReactionProcessor.CombineMaterials(new IMaterial[]{m1.Clone(),m2.Clone(),m3.Clone()},out resultA, out observedReactions, out observedReactionInstances);
            //bool reactionHappened = brs.MyReactionProcessor.CombineMaterials(new IMaterial[]{m1,m2,m3},out result, out observedReactions, out observedReactionInstances);

            Debug.WriteLine("Engine says that reactions " + (reactionAHappened?"happened.":"didn't happen."));

            Debug.WriteLine("\r\nObserved Reactions");
            foreach ( Reaction r in observedReactions ) {
                Debug.WriteLine(r.ToString());
            }

            Debug.WriteLine("\r\nObserved Reaction Instances");
            foreach ( ReactionInstance ri in observedReactionInstances ) {
                Debug.WriteLine(ri.ToString());
            }

            Debug.WriteLine("\r\nMixture");
            Debug.WriteLine(resultA.ToString());

			
			IMaterial resultB;

			Debug.WriteLine("Part B : Reaction should not happen...");
            bool reactionBHappened = brs.MyReactionProcessor.CombineMaterials(new IMaterial[]{m1.Clone(),m3.Clone()},out resultB, out observedReactions, out observedReactionInstances);

            Debug.WriteLine("Engine says that reactions " + (reactionBHappened?"happened.":"didn't happen.")); 

            Debug.WriteLine("\r\nObserved Reactions");
            foreach ( Reaction r in observedReactions ) {
                Debug.WriteLine(r.ToString());
            }

            Debug.WriteLine("\r\nObserved Reaction Instances");
            foreach ( ReactionInstance ri in observedReactionInstances ) {
                Debug.WriteLine(ri.ToString());
            }

            Debug.WriteLine("\r\nMixture");
            Debug.WriteLine(resultB.ToString());

            Assert.IsTrue(!reactionBHappened,"Reaction B should not have happened");

        }

        private void Initialize(BasicReactionSupporter brs){

            MaterialCatalog mcat = brs.MyMaterialCatalog;
            ReactionProcessor rp = brs.MyReactionProcessor;

            mcat.Add(new MaterialType(null, "Water", Guid.NewGuid(),1.0000, 4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Hydrochloric Acid", Guid.NewGuid(),1.1890,2.5500,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Caustic Soda", Guid.NewGuid(),2.0000,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Chloride", Guid.NewGuid(),2.1650,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sulfuric Acid 98%", Guid.NewGuid(),1.8420,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Hydroxide", Guid.NewGuid(),1.3000,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Sulfate", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Nitrous Acid", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Nitrite", Guid.NewGuid(),2.3800,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Nitrite", Guid.NewGuid(),1.9150,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Aluminum Hydroxide", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Ammonia", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Ammonium Hydroxide", Guid.NewGuid(),1.0000,4.1800,MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Bromine", Guid.NewGuid(), 3.1200, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Palladium", Guid.NewGuid(), 3.1200, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Hydrogen", Guid.NewGuid(), 3.1200, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Oil", Guid.NewGuid(), 3.1200, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Hydrogenated Oil", Guid.NewGuid(), 3.1200, 4.1800, MaterialState.Liquid));


            Reaction r1 = new Reaction(null,"Reaction 1",Guid.NewGuid());
            r1.AddReactant(mcat["Caustic Soda"],0.5231);
            r1.AddReactant(mcat["Hydrochloric Acid"],0.4769);
            r1.AddProduct(mcat["Water"],0.2356);
            r1.AddProduct(mcat["Sodium Chloride"],0.7644);
            rp.AddReaction(r1);

            Reaction r2 = new Reaction(null,"Reaction 2",Guid.NewGuid());
            r2.AddReactant(mcat["Sulfuric Acid 98%"],0.533622);
            r2.AddReactant(mcat["Potassium Hydroxide"],0.466378);
            r2.AddProduct(mcat["Water"],0.171333);
            r2.AddProduct(mcat["Potassium Sulfate"],0.828667);
            rp.AddReaction(r2);

            Reaction r3 = new Reaction(null,"Reaction 3",Guid.NewGuid());
            r3.AddReactant(mcat["Caustic Soda"],0.459681368);
            r3.AddReactant(mcat["Nitrous Acid"],0.540318632);
            r3.AddProduct(mcat["Water"],0.207047552);
            r3.AddProduct(mcat["Sodium Nitrite"],0.792952448);
            rp.AddReaction(r3);

            Reaction r4 = new Reaction(null, "Reaction 4", Guid.NewGuid());
            r4.AddReactant(mcat["Potassium Hydroxide"], 0.544102);
            r4.AddReactant(mcat["Nitrous Acid"], 0.455898);
            r4.AddProduct(mcat["Water"], 0.174698);
            r4.AddProduct(mcat["Potassium Nitrite"], 0.825302);
            rp.AddReaction(r4);

            Reaction r5 = new Reaction(null, "Reaction 5", Guid.NewGuid());
            r5.AddReactant(mcat["Palladium"], 1);
            r5.AddReactant(mcat["Hydrogen"], 1);
            r5.AddReactant(mcat["Oil"], 1);
            r5.AddProduct(mcat["Hydrogenated Oil"], 2);
            r5.AddProduct(mcat["Palladium"], 1);
            rp.AddReaction(r5);

        }

		private void mixture_OnReactionHappened(ReactionInstance reactionInstance) {
			Debug.WriteLine("Observed reaction called  " + reactionInstance.Reaction.ToString() + "...");
			Debug.WriteLine("Instance-specific reaction is " + reactionInstance.InstanceSpecificReaction.ToString() + "...");
			Debug.WriteLine("Instance-specific name is " + reactionInstance.InstanceSpecificReactionString() + "...");
			Debug.WriteLine("--- REACTANTS ---");
			foreach ( Reaction.ReactionParticipant rp in reactionInstance.InstanceSpecificReaction.Reactants ) {
				Debug.WriteLine(rp.MaterialType.Name + " : " + rp.Mass + " kg.");
			}

			Debug.WriteLine("--- PRODUCTS ---");
			foreach ( Reaction.ReactionParticipant rp in reactionInstance.InstanceSpecificReaction.Products ) {
				Debug.WriteLine(rp.MaterialType.Name + " : " + rp.Mass + " kg.");
			} 
		}
	}
}
