/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Xml;
using System.Reflection;
using System.Configuration;
using System.Collections;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Materials.Chemistry.Emissions {

	public class EmissionsServiceConfigurationHandler : IConfigurationSectionHandler {

		public static readonly string EQUATION_SET_DEFAULT					= "CTG";
		public static readonly bool   IGNORE_UNKNOWN_MODEL_TYPES_DEFAULT	= false;
		public static readonly bool   ENABLED_DEFAULT						= true;
		public static readonly bool   PERMIT_OVER_EMISSION_DEFAULT			= false;
		public static readonly bool   PERMIT_UNDER_EMISSION_DEFAULT			= false;

		public object Create(object parent, object configContext, XmlNode section){

			Hashtable htConfig = new Hashtable();
			Hashtable htModels = new Hashtable();

			ArrayList failedReads = new ArrayList();
			XmlNode tmp = section.SelectSingleNode("EquationSet");

		    // ReSharper disable once MergeConditionalExpression
			htConfig.Add("EquationSet",(tmp==null?EQUATION_SET_DEFAULT:tmp.InnerText));

			tmp = section.SelectSingleNode("IgnoreUnknownModelTypes");
			if ( tmp == null ) failedReads.Add("IgnoreUnknownModelTypes");
			htConfig.Add("IgnoreUnknownModelTypes",(tmp==null?IGNORE_UNKNOWN_MODEL_TYPES_DEFAULT:bool.Parse(tmp.InnerText)));

			tmp = section.SelectSingleNode("Enabled");
			if ( tmp == null ) failedReads.Add("Enabled");
			htConfig.Add("Enabled",(tmp==null?ENABLED_DEFAULT:bool.Parse(tmp.InnerText)));

			tmp = section.SelectSingleNode("PermitOverEmission");
			if ( tmp == null ) failedReads.Add("PermitOverEmission");
			htConfig.Add("PermitOverEmission",(tmp==null?PERMIT_OVER_EMISSION_DEFAULT:bool.Parse(tmp.InnerText)));

			tmp = section.SelectSingleNode("PermitUnderEmission");
			if ( tmp == null ) failedReads.Add("PermitUnderEmission");
			htConfig.Add("PermitUnderEmission",(tmp==null?PERMIT_UNDER_EMISSION_DEFAULT:bool.Parse(tmp.InnerText)));

			if ( failedReads.Count > 0 ) {
				string missing = Utility.StringOperations.ToCommasAndAndedList(failedReads);
				string current = "\r\n" + Utility.DictionaryOperations.DumpDictionary("Emissions Settings",htConfig);
				string msg = string.Format("The following sections were not present in the Emissions Service configuration section:{0}. The following values are now in use: {1}.",missing, current);
				Console.WriteLine(msg);
			}

			htConfig.Add("Models",htModels);
			
			bool poe = (bool)htConfig["PermitOverEmission"];
			bool pue = (bool)htConfig["PermitUnderEmission"];
		    XmlNodeList tmpXmlNodeList = section.SelectNodes("Models/Model");
		    if (tmpXmlNodeList != null)
		        foreach ( XmlNode model in tmpXmlNodeList ) {
		            string assemblyString = model.Attributes["assembly"].InnerText;
		            Assembly.Load(assemblyString);
		            string typeString = model.Attributes["type"].InnerText;
		            Type type = Type.GetType(typeString);
		            try {
		                IEmissionModel iem = (IEmissionModel)type.GetConstructor(new Type[]{}).Invoke(new object[]{});
		                iem.PermitOverEmission = poe;
		                iem.PermitUnderEmission = pue;
		                foreach ( string key in iem.Keys ) htModels.Add(key,iem);
		            } catch ( Exception e ) {
		                // TODO: Handle this in an Exception Service.
		                Console.WriteLine(e);
		            }
		        }

		    return htConfig;
		}

	}

	public class EmissionsService {

        private static volatile EmissionsService _instance;
		private static readonly object s_padlock = new object();
		public static EmissionsService Instance {
			get {
				if ( _instance == null ) {
					lock(s_padlock){
						if ( _instance == null ) _instance = new EmissionsService();
					}
				}
				return _instance;
			}
		}

		private readonly Hashtable m_models;
		private readonly bool m_ignoreUnknownModelTypes;
		private readonly bool m_enabled;

	    private EmissionsService(Hashtable configData)
	    {
	        if (configData == null)
	        {
	            try
	            {
	                configData = (Hashtable) System.Configuration.ConfigurationManager.GetSection("EmissionsService");
	            }
	            catch (ConfigurationException)
	            {
	            }
	        }
	        if (configData == null)
	        {
	            if (UnitTestDetector.IsInUnitTest)
	            {
	                Hashtable models = new Hashtable();
	                configData = new Hashtable
	                {
	                    {"Enabled", true},
	                    {"Models", models},
	                    {"IgnoreUnknownModelTypes", true},
	                    {"EquationSet", "CTG"}
	                };

	                IEmissionModel[] emissionModels = new IEmissionModel[]{
	                    new AirDryModel(),
	                    new EvacuateModel(),
	                    new FillModel(),
	                    new GasEvolutionModel(),
	                    new GasSweepModel(),
	                    new HeatModel(),
	                    new MassBalanceModel(),
	                    new NoEmissionModel(),
	                    new VacuumDistillationModel(),
	                    new VacuumDistillationWScrubberModel(),
	                    new VacuumDryModel(),
	                    new PressureTransferModel()
	                };
	                foreach (IEmissionModel emissionModel in emissionModels)
	                {
	                    foreach (string key in emissionModel.Keys)
	                    {
	                        models.Add(key,emissionModel);
	                    }
	                }
                }
                else
	            {
	                m_enabled = false;
	                throw new Utility.InitFailureException(_msgMissingConfigSection);
	            }
	        }
	        string aes = (string) configData["EquationSet"] ?? "CTG";
	        EmissionModel.ActiveEquationSet =
	            (EmissionModel.EquationSet) Enum.Parse(typeof (EmissionModel.EquationSet), aes);
	        m_models = (Hashtable) configData["Models"];
	        m_ignoreUnknownModelTypes = (bool) configData["IgnoreUnknownModelTypes"];
            m_enabled = (bool)configData.ContainsKey("Enabled") && (bool)configData["Enabled"];

	    }

	    private EmissionsService():this(null){}

		public Hashtable KnownModels => m_models;

	    public bool Enabled => m_enabled;

	    public void ProcessEmissions(
			Mixture initial, 
			out Mixture final, 
			out Mixture emission, 
			bool modifyInPlace,
			string emissionModelKey,
			Hashtable parameters){

			final = null;
			emission = null;

			IEmissionModel em = (IEmissionModel)m_models[emissionModelKey];

			if ( em == null ) {
				if ( !m_ignoreUnknownModelTypes ) {
					string msg = "Emission Model Key \"" + emissionModelKey 
						+ "\" does not match known emission model keys, which include "; 
					msg += Utility.StringOperations.ToCommasAndAndedList(new ArrayList(m_models.Keys));
					throw new ApplicationException(msg);
				}
			} else {
				if ( m_enabled ) {
					em.Process(initial,out final, out emission, modifyInPlace, parameters);
				} else {
					final = (Mixture)initial.Clone();
					emission = new Mixture();
				}

			}
		}

        private static string _msgMissingConfigSection = @"The emissions service was started, but its configuration data is missing.

Please add the following two entries into your app.config file. Note, you can still have emissions turned off. (Enabled=false).

	<configSections>
	      <section name=""EmissionsService"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.EmissionsServiceConfigurationHandler, Sage""/>
	</configSections>

and (sample - yours may vary. These are the basic, installed models...)

  <EmissionsService>
    <IgnoreUnknownModelTypes>false</IgnoreUnknownModelTypes>
    <Enabled>true</Enabled>
    <PermitOverEmission>false</PermitOverEmission>
    <PermitUnderEmission>false</PermitUnderEmission>
    <EquationSet>CTG</EquationSet>
	<!-- CTG or MACT -->
    <Models>
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.AirDryModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.EvacuateModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.FillModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.GasEvolutionModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.GasSweepModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.HeatModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.MassBalanceModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.NoEmissionModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.VacuumDistillationModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.VacuumDistillationWScrubberModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.VacuumDryModel"" signed=""false"" encrypted=""false"" />
		<Model assembly=""Sage, Version=3.0.1010.14159, Culture=neutral, PublicKeyToken=28bc9c3da9cadb40"" type=""Highpoint.Sage.Materials.Chemistry.Emissions.PressureTransferModel"" signed=""false"" encrypted=""false"" />
    </Models>
  </EmissionsService>
";
	}
}
