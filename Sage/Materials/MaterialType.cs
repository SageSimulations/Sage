/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Specialized;
using Highpoint.Sage.Materials.Chemistry.Emissions;
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using Highpoint.Sage.Materials.Emissions;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using Trace = System.Diagnostics.Debug;

namespace Highpoint.Sage.Materials.Chemistry {

    public class MaterialType : SmartPropertyBag, IModelObject {

		/// <summary>
		/// A delegate that indicates acceptance of a material type, for example, for inclusion in a mole fraction or partial pressure computation.
		/// This delegate returns true to indicate that the material passes the filter criteria.
		/// </summary>
		public delegate bool Filter(MaterialType mt);

		public static Filter FilterAcceptAll = AcceptAll;
		public static Filter FilterAcceptLiquidOnly = LiquidOnly;
		public static Filter FilterAcceptLiquidOrUnknownOnly = LiquidOrUnknownOnly;

		private static bool AcceptAll(MaterialType mt){return true;}
		private static bool LiquidOnly(MaterialType mt){return mt.STPState.Equals(MaterialState.Liquid);}
		private static bool LiquidOrUnknownOnly(MaterialType mt){return mt.STPState.Equals(MaterialState.Liquid) || mt.STPState.Equals(MaterialState.Unknown);}


		private double m_specificGravity;
        private double m_specificHeat;
		private double m_latentHeatOfVaporization;
		private double m_molecularWeight;
		protected MaterialState StpState;
        private double m_vanTHoffFactor = s_default_Van_T_Hoff_Factor;
        private double m_ebullioscopicConstant = s_default_Ebullioscopy_Constant;

		//private SmallDoubleInterpolable m_vaporPressure;
		private ListDictionary m_emissionClassifications;
		private IAntoinesCoefficients3 m_antoinesCoefficients3 = new AntoinesCoefficients3Impl();
		private IAntoinesCoefficientsExt m_antoinesCoefficientsExt = new AntoinesCoefficientsExt();
		
		#region INITIALIZERS FOR UNKNOWN MATERIAL PROPERTIES

        private static readonly double   s_default_Van_T_Hoff_Factor = 2;
        private static readonly double   s_default_Ebullioscopy_Constant = 3;

		public static readonly double	 UNKNOWN_MOLECULAR_WEIGHT = double.NaN;
		private static readonly double   s_default_Lhov = double.NaN;

		private static readonly double   s_default_Lhov_Unknown = 2260.0;
		private static readonly double   s_default_Lhov_Solid = 2260.0;
		private static readonly double   s_default_Lhov_Liquid = 2260.0;
		private static readonly double   s_default_Lhov_Gas = 2260.0;
		private static readonly double   s_default_Molwt_Unknown = 50.0;
		private static readonly double   s_default_Molwt_Solid = 150.0;
		private static readonly double   s_default_Molwt_Liquid = 50.0;
		private static readonly double   s_default_Molwt_Gas = 15.0;
		private static readonly double   s_default_Specheat_Unknown = 4184.0; // Joules per kilogram degree-Kelvin
		private static readonly double   s_default_Specheat_Solid = 4184.0;
		private static readonly double   s_default_Specheat_Liquid = 4184.0;
		private static readonly double   s_default_Specheat_Gas = 4184.0;
		private static readonly double   s_default_Specgrav_Unknown = 1.0;
		private static readonly double   s_default_Specgrav_Solid = 1.0;
		private static readonly double   s_default_Specgrav_Liquid = 1.0;
		private static readonly double   s_default_Specgrav_Gas = 1.0;

        private static string _fmtStrUnknownParam = "MaterialType {0} has an \"unknown\" (double.NaN) value for {1}. Choosing a suitable default value of {2}.";
        private static string _fmtStrIllegalZeroValParam = "The value of \"{1}\" for material {0} has been provided as zero - this is illegal. If desired, Double.NaN may be provided, and it will be replaced with a default value.";

		#endregion

		/// <summary>
		/// Default constructor for use in serialization. After deserialization, the model
		/// must be set by the entity reconstituting the material - it is not serialized
		/// explicitly with the material type, since that would prevent the material type
		/// from being deserialized into another model.
		/// </summary>
		public MaterialType(){

		}

		/// <summary>
		/// Constructor for MaterialType. Uses default values for all unspecified (see larger
		/// ctor) properties, marks Molecular Weight as unknown.
		/// </summary>
		/// <param name="model">The model to which this MaterialType will belong.</param>
		/// <param name="name">The name of this MaterialType.</param>
		/// <param name="guid">The Guid of this MaterialType.</param>
		/// <param name="specificGravity">The specific gravity associated with material of this type.</param>
		/// <param name="specificHeat">The specific heat associated with material of this type.</param>
		/// <param name="stpState">The state of the material at standard temperature and pressure.</param>
		public MaterialType(IModel model, string name, Guid guid, double specificGravity, double specificHeat ,MaterialState stpState)
			:this(model,name,guid,specificGravity,specificHeat,stpState,UNKNOWN_MOLECULAR_WEIGHT,
			s_default_Lhov){}

		/// <summary>
		/// Constructor for MaterialType. Uses default values for all unspecified (see larger constructor) properties.
		/// </summary>
		/// <param name="model">The model to which this MaterialType will belong.</param>
		/// <param name="name">The name of this MaterialType.</param>
		/// <param name="guid">The Guid of this MaterialType.</param>
		/// <param name="specificGravity">The specific gravity associated with material of this type.</param>
		/// <param name="specificHeat">The specific heat associated with material of this type.</param>
		/// <param name="stpState">The state of the material at standard temperature and pressure.</param>
		/// <param name="molecularWeight">The molecular weight of this substance.</param>
		public MaterialType(IModel model, string name, Guid guid, double specificGravity, double specificHeat, MaterialState stpState, double molecularWeight)
			:this(model,name,guid,specificGravity,specificHeat,stpState,molecularWeight,
			s_default_Lhov){}

        /// <summary>
        /// Constructor for MaterialType. Provides a way for the user to specify all of the detailed
        /// characteristics of the material.
        /// </summary>
        /// <param name="model">The model to which this MaterialType will belong.</param>
        /// <param name="name">The name of this MaterialType.</param>
        /// <param name="guid">The Guid of this MaterialType.</param>
        /// <param name="specificGravity">The specific gravity associated with material of this type.</param>
        /// <param name="specificHeat">The specific heat associated with material of this type.</param>
        /// <param name="stpState">State of the material at Standard Temperature &amp; Pressure conditions.</param>
        /// <param name="molecularWeight">The molecular weight.</param>
        /// <param name="latentHeatOfVaporization">The latent heat of vaporization associated with material of this type. J/kg.</param>
		public MaterialType(IModel model, string name, Guid guid, 
			double specificGravity, 
			double specificHeat,
			MaterialState stpState,
			double molecularWeight,
			double latentHeatOfVaporization
			){
			m_name = name;
			m_guid = guid;
			m_model = model;
			STPState = stpState;
			SetSpecificGravity(specificGravity); // kilogram per liter.
			SetSpecificHeat(specificHeat); // Joules per Kilogram-degree K.
			SetMolecularWeight(molecularWeight);
			SetLatentHeatOfVaporization(latentHeatOfVaporization); // Joules per Kilogram.
			m_emissionClassifications = null;

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model this component runs in.</param>
        /// <param name="name">The name of this component.</param>
        /// <param name="description">The description for this component.</param>
        /// <param name="guid">The GUID of this component.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        /// <summary>
        /// Gets or sets the state of the material at Standard Temperature and Pressure.
        /// </summary>
        /// <value>The state of the material at Standard Temperature and Pressure.</value>
		public MaterialState STPState {
			get {
				return StpState;
			}
			set {
				StpState = value; 
			}
		}

		#region EmissionsClassification Support

		/// <summary>
		/// Accepts emissions classifications for this material types. Appropriate string values are SARA, HAP, NATA, VOC, GHG, and ODC.
		/// </summary>
		/// <param name="classifications">An array of strings that represent the emissions classifications for this material type.</param>
		public void AddEmissionsClassifications(string[] classifications){
			ArrayList al = new ArrayList();
			EmissionsClassification ec;
			foreach ( string classification in classifications ) {
				ec = null;
				if ( classification.Equals("SARA") ){
					ec = new SaraToxicReleaseInventory();
				} else if ( classification.Equals("HAP") ){
					ec = new HazardousAirPollutant();

				} else if ( classification.Equals("NATA") ){
					ec = new NationalAirToxicsAssessment();

				} else if ( classification.Equals("VOC") ){
					ec = new VolatileOrganicCompound();

				} else if ( classification.Equals("GHG") ){
					ec = new GreenhouseGas();

				} else if ( classification.Equals("ODC") ){
					ec = new OzoneDepletingCompound();
				} else {
					if ( m_model != null ){
						m_model.AddError(new GenericModelError("Unknown Emissions Classification","An emissions classification keyed as " + classification + " was added to the material type for " + Name + ", but was not recognized. It will be ignored.",this,null));
					}
				}
				if ( ec != null ) al.Add(ec);
			}

			EmissionsClassification[] eca 
				= (EmissionsClassification[])al.ToArray(typeof(EmissionsClassification));
			AddEmissionsClassifications(eca);
		}

		/// <summary>
		/// Tags this material type as having zero or more Emissions Classifications. Any class that
		/// derives from EmissionsClassification may be included in this collection. 
		/// </summary>
		/// <param name="classifications">An array of EmissionsClassification objects.</param>
		public void AddEmissionsClassifications(EmissionsClassification[] classifications){
			// Make sure that our EmissionsClassification is not null.
			if ( m_emissionClassifications == null ) {
				m_emissionClassifications = new ListDictionary();
			}

			EmissionsClassificationCatalog ecc = (EmissionsClassificationCatalog)m_model.Parameters["EmissionsClassificationCatalog"];
			if ( m_model != null && ecc == null ) {
				ecc = new EmissionsClassificationCatalog();
				m_model.Parameters.Add("EmissionsClassificationCatalog",ecc);
			}

			ArrayList alec = new ArrayList(classifications);
			foreach ( EmissionsClassification ec in alec ) {
				if ( ecc != null && !ecc.IsReadOnly && !ecc.Contains(ec.Name) ) ecc.Add(ec.Name,ec);
				m_emissionClassifications.Add(ec.Name,ec);
			}
		}

		public void ClearEmissionsClassifications(){ 
			m_emissionClassifications.Clear();
		}

		public IDictionary EmissionsClassifications { get { return m_emissionClassifications; } }
		#endregion

		#region Vapor Pressure Coefficients
		#region Antoine's Law Coefficients (3)
        public void SetAntoinesCoefficients3(double a, double b, double c) {
            SetAntoinesCoefficients3(a, b, c, PressureUnits.mmHg, TemperatureUnits.Celsius);
        }

        public void SetAntoinesCoefficients3(double a, double b, double c, PressureUnits spu, TemperatureUnits stu) {
            m_antoinesCoefficients3 = new AntoinesCoefficients3Impl(a, b, c, spu, stu);
        }

        public IAntoinesCoefficients3 AntoinesLawCoefficients3 { get { return m_antoinesCoefficients3; } }
		#endregion

		#region Antoine's Law Coefficients (extended)
        public void SetAntoinesCoefficientsExt(double c1, double c2, double c3, double c4, double c5, double c6, double c7, double c8, double c9) {
            m_antoinesCoefficientsExt = new AntoinesCoefficientsExt(c1, c2, c3, c4, c5, c6, c7, c8, c9, PressureUnits.mmHg, TemperatureUnits.Celsius);
        }

        public void SetAntoinesCoefficientsExt(double c1, double c2) {
            m_antoinesCoefficientsExt = new AntoinesCoefficientsExt(c1, c2, PressureUnits.mmHg, TemperatureUnits.Celsius);
        }

        public void SetAntoinesCoefficientsExt(double c1, double c2, double c3, double c4, double c5, double c6, double c7, double c8, double c9, PressureUnits pu, TemperatureUnits tu) {
            m_antoinesCoefficientsExt = new AntoinesCoefficientsExt(c1, c2, c3, c4, c5, c6, c7, c8, c9, pu, tu);
        }

        public void SetAntoinesCoefficientsExt(double c1, double c2, PressureUnits pu, TemperatureUnits tu) {
            m_antoinesCoefficientsExt = new AntoinesCoefficientsExt(c1, c2, pu, tu);
        }

        /// <summary>
		/// Contains the nine coefficients for the extended form of antoine's law. These are expressed in 
		/// </summary>
		public IAntoinesCoefficientsExt AntoinesLawCoefficientsExt { get { return m_antoinesCoefficientsExt; } }
		#endregion
		#endregion

		// We do not use the Vapor Pressure curve capability, and will not, until we see a stronger need. 
//		/// <summary>
//		/// Sets the X and Y data points associated with a vapor pressure versus temperature curve.
//		/// </summary>
//		/// <param name="vaporPressureTemperaturesInCelsius">The vapor pressure temperatures associated with material of this type.
//		/// This is an array of temperatures that correlate to the pressures in the following argument, and are used to create
//		/// an object that performs linear interpolation between the two to provide service of vapor pressure at a given temperature.</param>
//		/// <param name="vaporPressuresInPascals">The vapor pressures associated with material of this type.
//		/// This is an array of pressures that correlate to the temperatures in the preceding argument, and are used to create
//		/// an object that performs linear interpolation between the two to provide service of vapor pressure at a given temperature.</param>
//		public void SetVaporPressureCurveData(double[] vaporPressureTemperaturesInCelsius, double[] vaporPressuresInPascals){
//			m_vaporPressure = new SmallDoubleInterpolable(vaporPressureTemperaturesInCelsius,vaporPressuresInPascals);
//		}

		/// <summary>
		/// Returns the vapor pressure of this material when at the specified temperature - irrespective of the mixture in which it is contained.
		/// </summary>
		/// <param name="tempInCelsius">The temperature, in celsius, of the mixture.</param>
		/// <returns>The vapor pressure of this material.</returns>
		public double GetVaporPressure(double tempInCelsius ) {
			double vp = VaporPressureCalculator.ComputeVaporPressure(this,tempInCelsius, TemperatureUnits.Celsius, PressureUnits.Pascals);
//			if ( double.IsNaN(vp) && m_vaporPressure != null ) {
//				vp = m_vaporPressure.GetYValue(tempInCelsius);
//			}
			return vp;
		}

		/// <summary>
		/// Returns the vapor pressure of this material (in mmHg) when at the specified temperature and in the specified mixture.
		/// </summary>
		/// <param name="tempInCelsius">The temperature, in celsius, of the mixture.</param>
		/// <param name="hostMixture">The mixture in which this material type is contained.</param>
		/// <returns>The vapor pressure of this material in mmHg.</returns>
		public double GetVaporPressure(double tempInCelsius, Mixture hostMixture){
			return VaporPressureCalculator.ComputeVaporPressure(this,hostMixture);
		}

		/// <summary>
		/// Estimates a boiling point for the substance.
		/// </summary>
		/// <param name="atPressureInPascals">The pressure at which the boiling point is desired.</param>
		/// <returns>The estimated boiling point, in degrees celsius.</returns>
		public double GetEstimatedBoilingPoint(double atPressureInPascals){
			return VaporPressureCalculator.ComputeBoilingPoint(this,atPressureInPascals);
		}

        /// <summary>
        /// In physical chemistry, the van 't Hoff factor i is the number of moles of solute actually in
        /// solution per mole of solid solute added. Roughly, this is the number of ions a molecule of
        /// solute breaks into, when dissolved.
        /// </summary>
        /// <value>The van_t_ hoff factor.</value>
        public double VanTHoffFactor{
            get { return m_vanTHoffFactor; }
            set { m_vanTHoffFactor = value; }
        }

        /// <summary>
        /// Gets or sets the ebullioscopy constant. Ebullioscopic constant (Eb) is the constant that expresses the 
        /// amount by which the boiling point Tb of a solvent is raised by a solute, through the relation delta_Tb = i x Eb x b
        /// where i is the van ' Hoff factor, and b is the molality of the solute.
        /// </summary>
        /// <value>The ebullioscopy constant.</value>
        public double EbullioscopicConstant {
            get { return m_ebullioscopicConstant; }
            set { m_ebullioscopicConstant = value; }
        }

		/// <summary>
		/// Returns the specific gravity of the material in kilograms per liter.
		/// </summary>
		public double SpecificGravity { get { return m_specificGravity; } } 

		internal void SetSpecificGravity(double specificGravity){
			m_specificGravity = specificGravity;
            if(m_specificGravity == 0.0) {
                string msg = string.Format(_fmtStrIllegalZeroValParam, Name, "Specific Gravity");
                if(m_model != null) {
                    m_model.AddError(new GenericModelError("Parameter Error", msg, this, null));
                } else {
                    throw new ApplicationException(msg);
                }
            } else if(Double.IsNaN(m_specificGravity)) {
				if ( StpState.Equals(MaterialState.Unknown) ) m_specificGravity = s_default_Specgrav_Unknown;
				else if ( StpState.Equals(MaterialState.Solid) ) m_specificGravity = s_default_Specgrav_Solid;
				else if ( StpState.Equals(MaterialState.Liquid) ) m_specificGravity = s_default_Specgrav_Liquid;
				else if ( StpState.Equals(MaterialState.Gas) ) m_specificGravity = s_default_Specgrav_Gas;
				else { throw new ApplicationException("Unknown MaterialState encountered " + StpState); }
				string msg = string.Format(_fmtStrUnknownParam,Name,"Specific Gravity",m_specificGravity);
				if ( m_model != null ) m_model.AddWarning(new GenericModelWarning("Unknown MaterialType Parameter",msg,this,null));
			}
		}
		/// <summary>
		/// Returns the specific heat of the material in Joules per Kilogram-degree K.
		/// </summary>
		public double SpecificHeat { get { return m_specificHeat; } } 

		internal void SetSpecificHeat(double specificHeat){
			m_specificHeat = specificHeat;
            if(m_specificHeat == 0.0) {
                string msg = string.Format(_fmtStrIllegalZeroValParam, Name, "Specific Heat");
                if(m_model != null) {
                    m_model.AddError(new GenericModelError("Parameter Error", msg, this, null));
                } else {
                    throw new ApplicationException(msg);
                }
            } else if(Double.IsNaN(m_specificHeat)) {
				if ( StpState.Equals(MaterialState.Unknown) ) m_specificHeat = s_default_Specheat_Unknown;
				else if ( StpState.Equals(MaterialState.Solid) ) m_specificHeat = s_default_Specheat_Solid;
				else if ( StpState.Equals(MaterialState.Liquid) ) m_specificHeat = s_default_Specheat_Liquid;
				else if ( StpState.Equals(MaterialState.Gas) ) m_specificHeat = s_default_Specheat_Gas;
				else { throw new ApplicationException("Unknown MaterialState encountered " + StpState); }
				string msg = string.Format(_fmtStrUnknownParam,Name,"Specific Heat",m_specificHeat);
				if ( m_model != null ) m_model.AddWarning(new GenericModelWarning("Unknown MaterialType Parameter",msg,this,null));
			}
		}
		/// <summary>
		/// Returns the molecular weight of the material.
		/// </summary>
		public double MolecularWeight { get { return m_molecularWeight; } }

		internal void SetMolecularWeight(double molecularWeight){
			m_molecularWeight = molecularWeight;
            if(m_molecularWeight == 0.0) {
                string msg = string.Format(_fmtStrIllegalZeroValParam, Name, "Molecular Weight");
                if(m_model != null) {
                    m_model.AddError(new GenericModelError("Parameter Error", msg, this, null));
                } else {
                    throw new ApplicationException(msg);
                }
            } else if(Double.IsNaN(m_molecularWeight)) {
				if ( StpState.Equals(MaterialState.Unknown) ) m_molecularWeight = s_default_Molwt_Unknown;
				else if ( StpState.Equals(MaterialState.Solid) ) m_molecularWeight = s_default_Molwt_Solid;
				else if ( StpState.Equals(MaterialState.Liquid) ) m_molecularWeight = s_default_Molwt_Liquid;
				else if ( StpState.Equals(MaterialState.Gas) ) m_molecularWeight = s_default_Molwt_Gas;
				else { throw new ApplicationException("Unknown MaterialState encountered " + StpState); }
				string msg = string.Format(_fmtStrUnknownParam,Name,"Molecular Weight",m_molecularWeight);
				if ( m_model != null ) m_model.AddWarning(new GenericModelWarning("Unknown MaterialType Parameter",msg,this,null));
			}
		}
		/// <summary>
		/// Returns the Latent Heat Of Vaporization of the material in Joules per Kilogram.
		/// </summary>
		public double LatentHeatOfVaporization { get { return m_latentHeatOfVaporization; } }
		internal void SetLatentHeatOfVaporization(double latentHeatOfVaporization){
			m_latentHeatOfVaporization = latentHeatOfVaporization;
            if(m_latentHeatOfVaporization == 0.0) {
                string msg = string.Format(_fmtStrIllegalZeroValParam, Name, "Latent Heat of Vaporization");
                if(m_model != null) {
                    m_model.AddError(new GenericModelError("Parameter Error", msg, this, null));
                } else {
                    throw new ApplicationException(msg);
                }
            } else if ( Double.IsNaN(m_latentHeatOfVaporization) ) {
				if ( StpState.Equals(MaterialState.Unknown) ) m_latentHeatOfVaporization = s_default_Lhov_Unknown;
				else if ( StpState.Equals(MaterialState.Solid) ) m_latentHeatOfVaporization = s_default_Lhov_Solid;
				else if ( StpState.Equals(MaterialState.Liquid) ) m_latentHeatOfVaporization = s_default_Lhov_Liquid;
				else if ( StpState.Equals(MaterialState.Gas) ) m_latentHeatOfVaporization = s_default_Lhov_Gas;
				else { throw new ApplicationException("Unknown MaterialState encountered " + StpState); }
				string msg = string.Format(_fmtStrUnknownParam,Name,"Latent Heat of Vaporization",m_latentHeatOfVaporization);
				if ( m_model != null ) m_model.AddWarning(new GenericModelWarning("Unknown MaterialType Parameter",msg,this,null));
			}
		}

		public virtual IMaterial CreateMass(double kilograms, double temp){
            Substance substance = new Substance(this,kilograms,temp);
			substance.State = StpState;
			return substance;
        }

        /// <summary>
        /// Ascertains equality between this one and the other one.
        /// </summary>
        /// <param name="otherOne">The other one.</param>
        /// <returns><c>true</c> if this one and the other one are equal, <c>false</c> otherwise.</returns>
        public bool Equals(MaterialType otherOne){
			if ( !m_name.Equals(otherOne.m_name) ) return false;
			if ( !m_guid.Equals(otherOne.m_guid) ) return false;
			return true;
        }

        #region Implementation of IModelObject
        private string m_name;
        public string Name { get { return m_name; } }
		private string m_description;
		/// <summary>
		/// A description of this Material Type.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}
		private Guid m_guid = Guid.Empty;
        public Guid Guid => m_guid;
        private IModel m_model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model { get { return m_model; } protected set { m_model = value; } }
        #endregion
    
		#region >>> IXmlPersistable members <<<
		/// <summary>
		/// Stores this object to the specified XmlSerializationContext.
		/// </summary>
		/// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public override void SerializeTo(XmlSerializationContext xmlsc){
			base.SerializeTo(xmlsc);
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("Guid",m_guid);
			xmlsc.StoreObject("SpecificGravity",m_specificGravity);
			xmlsc.StoreObject("SpecificHeat",m_specificHeat);
			xmlsc.StoreObject("LHOV",m_latentHeatOfVaporization);
		}
		/// <summary>
		/// Reconstitutes this object from the specified XmlSerializationContext.
		/// </summary>
		/// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public override void DeserializeFrom(XmlSerializationContext xmlsc){
			base.DeserializeFrom(xmlsc);
			m_name = (string)xmlsc.LoadObject("Name");
			m_guid = (Guid)xmlsc.LoadObject("Guid");
			m_specificGravity = (double)xmlsc.LoadObject("SpecificGravity");
			m_specificHeat = (double)xmlsc.LoadObject("SpecificHeat");
			m_latentHeatOfVaporization = (double)xmlsc.LoadObject("LHOV");
		}
		#endregion

		public override string ToString() {
			return "MaterialType:"+m_name;
		}

		private object m_tag;
		public object Tag { get { return m_tag; } set { m_tag = value; } }

	}

}

// Also see : http://www.digitaldutch.com/unitconverter/

/* from http://www.rwc.uc.edu/koehler/biophys/8c.html
Heat is most often measured in "calories" (cal). A calorie is 4.186 J; it is the amount of heat
needed to raise one gram of water 1K. A dietary "Calorie" (Cal) is 1000 calories, and we
distinguish between the two by the capitalization. 

The temperature of a substance changes as heat energy is added to it. The "heat capacity" (C)
of an object is the ratio of change in heat to change in temperature, and the "specific heat" (c)
of a substance is the heat capacity per unit mass. We therefore have 

deltaQ = m c deltaT.

The specific heat of water is 1 cal / g K by definition. That of ice is .51 and of water vapor
is .48 (at constant pressure). Ice and water vapor (steam) are alternate "phases" of water. The
specific heat of human tissue is .85. That of air is .23. 

For a given substance at a given pressure, phase changes occur at well-defined temperatures. For
water at standard atmospheric pressure (at the surface of the earth), those are 273.15K and 373.15K
(0 C and 100C, the freezing and boiling points, which define the Celsius degree, and therefore the
Kelvin). For a given substance, the heat change per unit mass required for a phase transition is
called the "latent heat" (of either "fusion" or vaporization) L. This means that 

deltaQ = m L.

The latent heat of fusion (freezing) of water is 80 cal / g, and the latent heat of vaporization
of water is 540 cal / g at 100 C. Note that the heat added or lost during a phase change does not
affect the temperature during the phase change. Icewater is at 0 C until all of the water has frozen;
when melting, it is at 0 C until all of the ice has melted. Likewise, water is at 100 C until all the
water has boiled away; if the water / steam system is in a closed environment, the steam is at 100 C
until the water has all evaporated. Hence we can graph the temperature vs heat of a substance: 

*/