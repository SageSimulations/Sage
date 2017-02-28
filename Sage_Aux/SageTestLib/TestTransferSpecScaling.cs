/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.Mathematics.Scaling;
using _Debug = System.Diagnostics.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Materials.Chemistry
{
	/// <summary>
	/// Summary description for TransferSpecTester101.
	/// </summary>
	[TestClass]
    public class TransferSpecTester101	{
        public TransferSpecTester101(){Init();}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}
		
		[TestMethod]public void TestNonScaledByMassTransfer(){
            TSTestJig tj = new TSTestJig(100,100);
            _Debug.WriteLine(tj.Mixture);
            MaterialTransferSpecByMass msbm = new MaterialTransferSpecByMass(tj.H2O_Type,50,TimeSpan.FromMinutes(50));
            IMaterial effluent = msbm.GetExtract(tj.Mixture);
            _Debug.WriteLine(effluent);
            _Debug.WriteLine(tj.Mixture);
        }

        [TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks the different settings for scaling mass and time of a recipe")]
		public void TestMassScaling(){
            LinearScalingAdapterTest(2,.5,double.NaN,75,TimeSpan.FromMinutes(50));
            LinearScalingAdapterTest(1,1,double.NaN,50,TimeSpan.FromMinutes(50));
            LinearScalingAdapterTest(.5,2,double.NaN,0,TimeSpan.FromMinutes(50));
            LinearScalingAdapterTest(2,2,double.NaN,100,TimeSpan.FromMinutes(50)); // We will have run out of water in the effluent!
            LinearScalingAdapterTest(.5,.5,double.NaN,37.5,TimeSpan.FromMinutes(50));
            
            LinearScalingAdapterTest(2,.5,1.0,75,TimeSpan.FromMinutes(100));
            LinearScalingAdapterTest(1,1,1.0,50,TimeSpan.FromMinutes(50));
            LinearScalingAdapterTest(.5,2,1.0,0,TimeSpan.FromMinutes(25));
            //LinearScalingAdapterTest(2,2,1.0,100,TimeSpan.FromMinutes(100)); // We will have run out of water in the effluent!
            LinearScalingAdapterTest(.5,.5,1.0,37.5,TimeSpan.FromMinutes(25));

        }

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Checks another settings for scaling mass and time of a recipe")]
		public void TestScalingWithGreedAndSuperLinearDurationScaling(){
            LinearScalingAdapterTest(2,2,.5,100,TimeSpan.FromMinutes(50)); // We will have run out of water in the effluent!
        }

        private void LinearScalingAdapterTest(double scale, double massLinearity, double durationLinearity, double expectedWaterMass, TimeSpan expectedDuration){
            _Debug.WriteLine("We define a spec to test the removal of 50 kg of water from a mixture of 100 kg Water, 100 kg NaNO2.");
            TSTestJig tj = new TSTestJig(100,100);
            MaterialTransferSpecByMass msbm = new MaterialTransferSpecByMass(tj.H2O_Type,50,TimeSpan.FromMinutes(50));
            if ( !massLinearity.Equals(double.NaN) ) {
                _Debug.WriteLine("We provide the spec with a mass scaling adapter with linearity = " + massLinearity.ToString("F2"));
                msbm.SetMassScalingAdapter(new DoubleLinearScalingAdapter(msbm.Mass,massLinearity));
            } else {
                _Debug.WriteLine("The spec will have no mass scaling.");
            }
            if ( !durationLinearity.Equals(double.NaN) ) {
                _Debug.WriteLine("We provide the spec with a duration scaling adapter with linearity = " + durationLinearity.ToString("F2"));
                msbm.SetDurationScalingAdapter(new TimeSpanLinearScalingAdapter(msbm.Duration,durationLinearity));
            } else {
                _Debug.WriteLine("The spec will have no duration scaling.");
            }
            _Debug.WriteLine("We now scale the request by " + scale.ToString("F2"));
            msbm.Rescale(scale);
            IMaterial effluent = msbm.GetExtract(tj.Mixture);
            _Debug.WriteLine("In the end, we obtained... " + effluent + " in " + msbm.Duration);
            _Debug.WriteLine("... leaving " + tj.Mixture + "");
            _Debug.WriteLine("We expected " + expectedWaterMass + " kg of water in " + expectedDuration + "\r\n\r\n");

            _Debug.Assert(true == effluent.Mass.Equals(expectedWaterMass), "Water mass is not the expected one.");
            _Debug.Assert(true == TimeSpan.FromTicks(Math.Abs(msbm.Duration.Ticks-expectedDuration.Ticks)) < TimeSpan.FromMilliseconds(50), "Duration is bigger then the expeced one.");
        }

        internal class TSTestJig {
            private Mixture m_mixture;
            private BasicReactionSupporter m_brs;
            public Mixture Mixture { get { return m_mixture; } }
            public MaterialType H2O_Type { get { return m_brs.MyMaterialCatalog["Water"]; } }
            public MaterialType NANO2_Type { get { return m_brs.MyMaterialCatalog["Sodium Nitrite"]; } }

            public void Reset(double mH2O, double mNANO2){
                m_mixture.Clear();
                m_mixture.AddMaterial(m_brs.MyMaterialCatalog["Water"].CreateMass(mH2O,20)); // Add 250 kg.
                m_mixture.AddMaterial(m_brs.MyMaterialCatalog["Sodium Nitrite"].CreateMass(mNANO2,20)); // Add 100 kg NaNO2.
            }

            public TSTestJig(double mH2O, double mNANO2){
                m_brs = new BasicReactionSupporter();
                Initialize(m_brs);
                m_mixture = new Mixture(null,"Test Mixture");
                m_brs.MyReactionProcessor.Watch(m_mixture);

                m_mixture.AddMaterial(m_brs.MyMaterialCatalog["Water"].CreateMass(mH2O,20)); // Add 250 kg.
                m_mixture.AddMaterial(m_brs.MyMaterialCatalog["Sodium Nitrite"].CreateMass(mNANO2,20)); // Add 100 kg NaNO2.
            }



            private void Initialize(BasicReactionSupporter brs){

                MaterialCatalog mcat = brs.MyMaterialCatalog;
                ReactionProcessor rp = brs.MyReactionProcessor;

                //                        Model  Name    Guid           SpecGrav SpecHeat
                mcat.Add(new MaterialType(null, "Water", Guid.NewGuid(),1.0000,  4.1800,MaterialState.Liquid));
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
                mcat.Add(new MaterialType(null, "Bromine", Guid.NewGuid(),3.1200,4.1800,MaterialState.Liquid));

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

                Reaction r4 = new Reaction(null,"Reaction 4",Guid.NewGuid());
                r4.AddReactant(mcat["Potassium Hydroxide"],0.544102);
                r4.AddReactant(mcat["Nitrous Acid"],0.455898);
                r4.AddProduct(mcat["Water"],0.174698);
                r4.AddProduct(mcat["Potassium Nitrite"],0.825302);
                rp.AddReaction(r4);

            }
        }
	}
}