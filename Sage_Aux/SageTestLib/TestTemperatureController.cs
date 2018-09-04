/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Materials.Thermodynamics;
using IContainer = Highpoint.Sage.Materials.Chemistry.IContainer;

namespace Highpoint.Sage.Thermodynamics {
	/// <summary>
	/// Summary description for zTestTemperatureController.
	/// </summary>
	[TestClass]
	public class TemperatureControllerTester101	{
		public TemperatureControllerTester101(){Init();}

		private static readonly TemperatureControllerMode CONST_TSRC = TemperatureControllerMode.ConstantT;
		private static readonly TemperatureControllerMode CONST_DLTA = TemperatureControllerMode.ConstantDeltaT;
		private static readonly TemperatureControllerMode CONST_RAMP = TemperatureControllerMode.Constant_RampRate;

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
		
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 20 degreesC to 60 degrees C using the CONST Delta method")]
		public void TestTCConstDeltaTargetingUp(){
			Debug.WriteLine("\r\nTesting temperature drive up from constant delta.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(70.0,20.0,34.0,60.0,5.0,01.0,CONST_DLTA,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 20 degreesC to 60 degrees C using the CONST Delta method")]
		public void TestTCConstDeltaTargetingUp2(){
			Debug.WriteLine("\r\nTesting temperature drive up from constant delta.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(60.0,25.0,34.0,65.0,5.0,01.0,CONST_DLTA,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 20 degreesC to 60 degrees C using the CONST TSRC method")]
		public void TestTCConstTSrcTargetingUp(){
			Debug.WriteLine("\r\nTesting temperature drive up from constant TSrc.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(70.0,20.0,34.0,60.0,5.0,01.0,CONST_TSRC,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 20 degreesC to 60 degrees C using CONST TSRC and src==setpoint")]
        [ExpectedException(typeof(Highpoint.Sage.Materials.Thermodynamics.TemperatureController.IncalculableTimeToSetpointException))]
		public void TestTCConstTSrcTargetingLevel(){
			Debug.WriteLine("\r\nTesting temperature drive up from constant TSrc.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(60.0,20.0,34.0,60.0,5.0,01.0,CONST_TSRC,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 20 degreesC to 60 degrees C using the CONST RampRate method")]
		public void TestTCConstTRampRateTargetingUp(){
			Debug.WriteLine("\r\nTesting temperature drive up from constant RampRate.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(70.0,20.0,34.0,60.0,5.0,01.0,CONST_RAMP,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 17 degreesC to 35 degrees C using the CONST RampRate method")]
		public void TestTCConstTRampRateKlendathu(){
			Debug.WriteLine("\r\nTesting temperature drive up from constant RampRate.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(17.0,17.0,34.0,35.0,5.0,00.0,CONST_RAMP,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test cools off a mix from 70 degreesC to 50 degrees C using the CONST Delta method")]
		public void TestTCConstDeltaTargetingDown(){
			Debug.WriteLine("\r\nTesting temperature drive down from constant delta.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(20.0,70.0,34.0,50.0,5.0,01.0,CONST_DLTA,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test cools off a mix from 70 degreesC to 50 degrees C using the CONST TSRC method")]
		public void TestTCConstTSrcTargetingDown(){
			Debug.WriteLine("\r\nTesting temperature drive down from constant TSrc.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(20.0,70.0,34.0,50.0,5.0,01.0,CONST_TSRC,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test cools off a mix from 70 degreesC to 50 degrees C using the CONST RampRate method")]
		public void TestTCConstTRampRateTargetingDown(){
			Debug.WriteLine("\r\nTesting temperature drive down from constant RampRate.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(20.0,70.0,34.0,50.0,5.0,01.0,CONST_RAMP,true);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test cools off a mix from 70 degreesC to 60 degrees C due to ambient 34 degreesC using the CONST Delta method")]
		public void TestTCDriftDown(){
			Debug.WriteLine("\r\nTesting temperature downdrift due to ambient.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(90.0,70.0,34.0,60.0,5.0,01.0,CONST_DLTA,false);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 34 degreesC to 44 degrees C due to ambient 70 degreesC using the CONST Delta method")]
		public void TestTCDriftUp(){
			Debug.WriteLine("\r\nTesting temperature updrift due to ambient.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(90.0,34.0,70.0,44.0,5.0,01.0,CONST_DLTA,false);

			_TestTargeting(tj);
		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 34 degreesC for a longer period of time then to reach 60 degrees C using the CONST Delta method")]
		public void TestOverShootWhileOff(){
			Debug.WriteLine("\r\nTesting what happens when temperature overshoots the setpoint.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(90.0,34.0,70.0,60.0,5.0,01.0,CONST_DLTA,false);
            
			TimeSpan ts = tj.TempCtrl.TimeNeededToReachTargetTemp();
			Debug.Write("We need " + ts + " to reach setpoint. We will drive the system for ");
			ts += TimeSpan.FromSeconds(ts.TotalSeconds*.25);
			Debug.WriteLine(ts);

			Debug.WriteLine(tj.Mixture.ToString());
			tj.TempCtrl.ImposeEffectsOfDuration(ts);
			Debug.WriteLine(tj.Mixture.ToString());

            Assert.IsTrue(false == tj.MixTempWithinTolerance, "");

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test cools off a mix from 34 degreesC for a longer period of time then to reach 30 degrees C using the CONST Delta method")]
		public void TestUnderShootWhileOff(){
			Debug.WriteLine("\r\nTesting what happens when temperature undershoots the setpoint.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(20.0,34.0,20.0,30.0,5.0,01.0,CONST_DLTA,false);
            
			TimeSpan ts = tj.TempCtrl.TimeNeededToReachTargetTemp();
            Debug.Write("We need " + ts + " to reach setpoint. We will drive the system for ");
			ts += TimeSpan.FromSeconds(ts.TotalSeconds*.75);
			Debug.WriteLine(ts);

			Debug.WriteLine(tj.Mixture.ToString());
			tj.TempCtrl.ImposeEffectsOfDuration(ts);
			Debug.WriteLine(tj.Mixture.ToString());

            Assert.IsTrue(false == tj.MixTempWithinTolerance, "");

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test heats a mix from 34 degreesC for a longer period of time then to reach 60 degrees C using the CONST Delta method")]
		public void TestOverShootWhileOn(){
			Debug.WriteLine("\r\nTesting what happens when temperature overshoots the setpoint.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(90.0,34.0,70.0,60.0,5.0,01.0,CONST_DLTA,true);
            
			TimeSpan ts = tj.TempCtrl.TimeNeededToReachTargetTemp();
            Debug.Write("We need " + ts + " to reach setpoint. We will drive the system for ");
			ts += TimeSpan.FromSeconds(ts.TotalSeconds*.25);
			Debug.WriteLine(ts);

			Debug.WriteLine(tj.Mixture.ToString());
			tj.TempCtrl.ImposeEffectsOfDuration(ts);
			Debug.WriteLine(tj.Mixture.ToString());

            Assert.IsTrue(true == tj.MixTempWithinTolerance, "");

		}

		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("This test cools off a mix from 34 degreesC for a longer period of time then to reach 30 degrees C using the CONST Delta method")]
		public void TestUnderShootWhileOn(){
			Debug.WriteLine("\r\nTesting what happens when temperature undershoots the setpoint.");
			//                           SRC  MIX  AMB  SET  RMP ERR  MODE       ENBL
			TCTestJig tj = new TCTestJig(20.0,34.0,20.0,30.0,5.0,01.0,CONST_DLTA,true);
            
			TimeSpan ts = tj.TempCtrl.TimeNeededToReachTargetTemp();
            Debug.Write("We need " + ts + " to reach setpoint. We will drive the system for ");
			ts += TimeSpan.FromSeconds(ts.TotalSeconds*.25);
			Debug.WriteLine(ts);

			Debug.WriteLine(tj.Mixture.ToString());
			tj.TempCtrl.ImposeEffectsOfDuration(ts);
			Debug.WriteLine(tj.Mixture.ToString());

            Assert.IsTrue(true == tj.MixTempWithinTolerance, "");

		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test cools off a mix from 34 degreesC for a longer period of time then to reach 30 degrees C using the CONST Delta method")]
        public void TestReplicateFailAfterTurningOffTCEnabled() {
            Debug.WriteLine("\r\nTesting what happens when temperature control is turned off, but ambient cannot drive achievement of the setpoint.");

            TCTestJig tj;
            TimeSpan ts;
            bool fail = false;

            foreach (TemperatureControllerMode tcMode in Enum.GetValues(typeof(TemperatureControllerMode))) {
                
                // Set specific pre-conditions.
                tj = new TCTestJig(5.0, 34.0, 20.0, 10.0, 5.0, 01.0, tcMode, true);

                // Make sure they're achievable with TC on.
                ts = tj.TempCtrl.TimeNeededToReachTargetTemp();

                // Turn TC off.
                tj.TempCtrl.TCEnabled = false;

                try {
                    ts = tj.TempCtrl.TimeNeededToReachTargetTemp();
                    Console.WriteLine("Success: No error generated when TCMode is {0}.", tcMode);
                } catch (TemperatureController.IncalculableTimeToSetpointException) {
                    fail = true;
                    Console.WriteLine("FAIL: Error generated when TCMode is {0}.", tcMode);
                }
            }

            Assert.IsTrue(!fail);

        }

        



		private void _TestTargeting(TCTestJig tj){

			TimeSpan ts = tj.TempCtrl.TimeNeededToReachTargetTemp();

			Debug.WriteLine("Mixture temp   = " + tj.Mixture.Temperature);
			Debug.WriteLine("Setpoint temp  = " + tj.TempCtrl.TCSetpoint);
			Debug.WriteLine("Source temp    = " + tj.TempCtrl.TCSrcTemperature);
			Debug.WriteLine("Ambient temp   = " + tj.TempCtrl.AmbientTemperature);
			Debug.WriteLine("TCSys Delta T  = " + tj.TempCtrl.TCSrcDelta);
			Debug.WriteLine("RampRate       = " + (tj.TempCtrl.TCTemperatureRampRate.DegreesKelvin/tj.TempCtrl.TCTemperatureRampRate.PerTimePeriod.TotalMinutes) + " degrees per minute.");
			Debug.WriteLine("TCSys Mode     = " + tj.TempCtrl.TCMode);
			Debug.WriteLine("TCSys is " + (tj.TempCtrl.TCEnabled?"enabled.":"disabled."));
			string driveString = tj.TempCtrl.TCEnabled?"be driven to":"drift to";
			Debug.WriteLine("Time needed for mixture to " + driveString + " a target temperature of " + tj.TempCtrl.TCSetpoint + " is " + ts);

			Debug.WriteLine("At time 00:00:00, " + tj.Mixture);
			tj.TempCtrl.ImposeEffectsOfDuration(ts);
			Debug.WriteLine("At time " + ts + ", " + tj.Mixture);
            
			if ( !tj.TempCtrl.TCEnabled ) {
                Assert.IsTrue(tj.MixTempWithinTolerance, "In tolerance at the specified time.");
			}

			if ( tj.TempCtrl.TCEnabled ) {

			    tj.TempCtrl.ImposeEffectsOfDuration(ts);
			    Debug.WriteLine("At twice the time " + ts + ", " + tj.Mixture);
            
                Assert.IsTrue(tj.MixTempWithinTolerance, "Still in tolerance at twice the specified time (Temperature Control is enabled.)");
            }


        }

		internal class TCTestJig {
			private Mixture m_mixture;
		    private double m_err;
			public Mixture Mixture { get { return m_mixture; } }
			private TemperatureController m_tempController;
			public TemperatureController TempCtrl { get { return m_tempController; } }
            
			public TCTestJig(double tSrc, double tMix, double tAmb, double tSet, double rampRatePerMinute, double err, TemperatureControllerMode tcMode, bool tcEnabled){
				BasicReactionSupporter brs = new BasicReactionSupporter();
				Initialize(brs);
				m_mixture = new Mixture(null,"Test Mixture");
				brs.MyReactionProcessor.Watch(m_mixture);
				Container container = new Container(1000,m_mixture); // Container full volume is 1000 liters.
				m_tempController = new TemperatureController(container);
				
				m_mixture.AddMaterial(brs.MyMaterialCatalog["Water"].CreateMass(250,tMix)); // Add 250 kg.
				m_mixture.AddMaterial(brs.MyMaterialCatalog["Sodium Nitrite"].CreateMass(100,tMix)); // Add 100 kg NaNO2.

				m_tempController.AmbientTemperature = tAmb; // degreeC

                // Error band functionality has been obsoleted.
				//m_tempController.ErrorBand = err;         // +/- err degreeC dead band.
			    m_err = err; // Used for acceptability of non-precise results.

				m_tempController.SetAmbientThermalConductance(.30,.25); // .25 W/degreeC
				m_tempController.SetAmbientThermalConductance(.60,.50); // .50 W/degreeC
				m_tempController.SetAmbientThermalConductance(.90,.75); // .75 W/degreeC
				m_tempController.SetThermalConductance(.30,.40); // .4 W/degreeC
				m_tempController.SetThermalConductance(.60,.80); // .8 W/degreeC
				m_tempController.SetThermalConductance(.90,.120); // 1.2 W/degreeC
				
				m_tempController.TCEnabled = tcEnabled; // Temperature control system is on.
				m_tempController.TCMode = tcMode; // Temperature control system maintains a constant deltaT, or a constant tSrc.
				m_tempController.TCSetpoint = tSet; 
				m_tempController.TCSrcTemperature = tSrc; // Syltherm (e.g.) temperature.
				m_tempController.TCSrcDelta = tSrc; // To be used if/when the system is in constant delta mode.
				m_tempController.TCTemperatureRampRate = new TemperatureRampRate(5.0,TimeSpan.FromMinutes(1));
			}

			public bool MixTempWithinTolerance {
				get { 
					return (Math.Abs(m_mixture.Temperature - m_tempController.TCSetpoint) <= m_err);
				}
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

	internal class Container : IContainer {
		private double m_volume;
		private Mixture m_mixture;
		public Container(double volume, Mixture mixture){
			m_volume = volume;
			m_mixture = mixture;
		}


        #region IContainer Members

		public Mixture Mixture { get { return m_mixture; } }
		public double Capacity { get { return m_volume;  } }

        public double Pressure {
            get { throw new Exception("The method or operation is not implemented."); }
        }
        #endregion

    }
}