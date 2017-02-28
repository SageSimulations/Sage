/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.Utility.Mementos;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using K = Highpoint.Sage.Materials.Chemistry.Constants;
using System.Collections.Generic;
using System.Linq;
using Highpoint.Sage.Materials.Chemistry.VaporPressure;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable RedundantAssignment

namespace Highpoint.Sage.Materials.Chemistry {

    /// <summary>
    /// Implemented by anything that can hold a mixture, and has a capacity.
    /// </summary>
    public interface IContainer {

        /// <summary>
        /// Gets the mixture.
        /// </summary>
        /// <value>The mixture.</value>
        Mixture Mixture { get; }

        /// <summary>
        /// Gets the capacity in cubic meters.
        /// </summary>
        /// <value>The capacity.</value>
        double Capacity { get; }

        /// <summary>
        /// Gets the pressure.
        /// </summary>
        /// <value>The pressure.</value>
        double Pressure { get; }

    }

    /// <summary>
    /// A Mixture is a collection of consituent materials that share the same temperature. Reactions can
    /// take place within a mixture, if that mixture is being watched by a <see cref="Highpoint.Sage.Materials.Chemistry.ReactionProcessor"/>.
    /// </summary>
	[Serializable]
    public class Mixture : IMaterial {

        /// <summary>
        /// Fired after a change in mass, constituents or temperature has taken place in this mixture. 
        /// </summary>
        public event MaterialChangeListener MaterialChanged;
        /// <summary>
        /// Fires before a reaction takes place in this mixture.
        /// </summary>
        public event ReactionGoingToHappenEvent OnReactionGoingToHappen;
        /// <summary>
        /// Fires after a reaction has taken place in this mixture.
        /// </summary>
        public event ReactionHappenedEvent OnReactionHappened;

        #region Private Fields
        private Hashtable m_constituentSubstances = new Hashtable();
        private string m_name;
        private double m_temp = double.NaN;
        private readonly WriteLock m_writeLock = new WriteLock(true);
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Mixture");
        private static readonly bool s_breakOnIsNaNTemp = Diagnostics.DiagnosticAids.Diagnostics("TemperatureIsNaNBreak");
        private IMemento m_memento;
        private MaterialChangeDistiller m_eventDistiller;
        private IUpdater m_updater = s_dummyUpdater;
        private static readonly IUpdater s_dummyUpdater = new NullUpdater();
        #endregion 
        
        #region Constructors
        /// <summary>
        /// Default Constructor for deserialization.
        /// </summary>
        public Mixture() : this(null, null, Guid.NewGuid()) { }
        /// <summary>
        /// Creates a new instance of the <see cref="T:Mixture"/> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="T:Mixture"/>.</param>
        public Mixture(string name) : this(null, name, Guid.NewGuid()) { }
        /// <summary>
        /// Creates a new instance of the <see cref="T:Mixture"/> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="T:Mixture"/>.</param>
        /// <param name="guid">The GUID of the <see cref="T:Mixture"/>.</param>
        public Mixture(string name, Guid guid) : this(null, name, guid) { }
        /// <summary>
        /// Creates a new instance of the <see cref="T:Mixture"/> class.
        /// </summary>
        /// <param name="model">The model in which the <see cref="T:Mixture"/> will exist.</param>
        /// <param name="name">The name of the <see cref="T:Mixture"/>.</param>
        public Mixture(IModel model, string name) : this(model, name, Guid.NewGuid()) { }
        /// <summary>
        /// Creates a new instance of the <see cref="T:Mixture"/> class.
        /// </summary>
        /// <param name="model">The model in which the <see cref="T:Mixture"/> will exist.</param>
        /// <param name="name">The name of the <see cref="T:Mixture"/>.</param>
        /// <param name="guid">The GUID of the <see cref="T:Mixture"/>.</param>
        public Mixture(IModel model, string name, Guid guid) {
            Model = model;
            m_name = name;
            Guid = guid;
            m_ssh = new MementoHelper(this, true);
            RecomputeTemperature(0.0);
        }
        #endregion 

        /// <summary>
        /// Creates a mixture from the specified constituents.
        /// </summary>
        /// <param name="constituents">The constituents.</param>
        /// <returns></returns>
		public static Mixture Create(params IMaterial[] constituents){
			Mixture mixture = new Mixture();
			foreach ( IMaterial constituent in constituents) mixture.AddMaterial(constituent.Clone());
			return mixture;
		}

        /// <summary>
        /// Indicates if write operations on this equipment are permitted.
        /// </summary>
        public bool IsWritable => m_writeLock.IsWritable;

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public IMaterial Clone(){
            Update();
            Mixture mixture = new Mixture(Model,m_name);
            foreach ( Substance s in m_constituentSubstances.Values ) {
                mixture.AddMaterial(s.Clone());
            }
            return mixture;
        }

        private void Update()
        {
            m_updater?.DoUpdate(this);
        }

        public IUpdater Updater {
            set {
                m_updater = value ?? s_dummyUpdater;
            }
            get { return m_updater; }
        }

        /// <summary>
        /// Removes all substances from this mixture.
        /// </summary>
        public void Clear(){
            m_updater.DoUpdate(this);
            if (m_constituentSubstances.Count > 0) {
                if (!m_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, m_writeLock);
                m_constituentSubstances.Clear();
                m_ssh.ReportChange();
                MaterialChanged?.Invoke(this, MaterialChangeType.Contents);
                m_writeLock.ClearChildren();
                RecomputeTemperature(0.0);
            }
            m_updater.Detach(this);
		}

        /// <summary>
        /// Initializes this mixture within the specified model. Could theoretically return an ITransitionFailureReason,
        /// but there is no way this operation can fail, so it always returns null.
        /// </summary>
        /// <param name="model">The model in which this mixture exists.</param>
        /// <returns>Null.</returns>
        // ReSharper disable once UnusedParameter.Global
        public ITransitionFailureReason Initialize(IModel model)
        {
            if ( s_diagnostics ) _Debug.WriteLine("Clearing mixture " + Name );
            Clear(); // Checks for writability, too.
            return null;
        }

        /// <summary>
        /// Gets the constituents (Materials) that comprise this mixture.
        /// </summary>
        /// <value>The constituents.</value>
        public ICollection Constituents { get { m_updater.DoUpdate(this); return m_constituentSubstances.Values; } }

        /// <summary>
        /// Returns true if the two mixtures are semantically equal.
        /// </summary>
        /// <param name="otherGuy">The other mixture.</param>
        /// <returns>
        /// True if the two mixtures are semantically equal.
        /// </returns>
        public bool Equals(ISupportsMementos otherGuy) {
            Mixture otherMixture = otherGuy as Mixture;
            if (m_constituentSubstances.Count != otherMixture?.m_constituentSubstances.Count)
                return false;
            Update();
            foreach (DictionaryEntry de in m_constituentSubstances) {
                if (!otherMixture.m_constituentSubstances.Contains(de.Key))
                    return false;
                Substance hisSubstance = (Substance)otherMixture.m_constituentSubstances[de.Key];
                if (!hisSubstance.Equals((Substance)de.Value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the containeds mass of the specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType"/>.
        /// </summary>
        /// <param name="type">The specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType"/>.</param>
        /// <returns></returns>
        public double ContainedMassOf(MaterialType type){
            Update();
            Substance sub = (Substance)m_constituentSubstances[type.Name];
            if ( sub == null ) return 0.0;
            return sub.Mass;
        }

        /// <summary>
        /// Gets the mole fraction of the specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType"/> in this mixture,
        /// with the calculations counting only the materials that pass the <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType.Filter"/>.
        /// </summary>
        /// <param name="mt">The specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType"/>.</param>
        /// <param name="tf">The specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType.Filter"/>.</param>
        /// <returns></returns>
		public double GetMoleFraction(MaterialType mt, MaterialType.Filter tf){
            Update();
            double total = 0.0;
			double ofInterest = 0.0;
			foreach ( Substance substance in Constituents ) {
				if ( tf(substance.MaterialType) ) {
					if ( substance.MaterialType.Equals(mt) ) ofInterest += (substance.Mass / substance.MaterialType.MolecularWeight);
					total += (substance.Mass / substance.MaterialType.MolecularWeight);
				}
			}

			return total==0?0.0:ofInterest/total;
		}

        /// <summary>
        /// Gets the mole fraction of the specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType"/> in this mixture,
        /// with the calculations counting all present material types.
        /// </summary>
        /// <param name="mt">The specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType"/>.</param>
        /// <returns>The mole fraction of the specified <see cref="Highpoint.Sage.Materials.Chemistry.MaterialType"/> in this mixture</returns>
        public double GetMoleFraction(MaterialType mt) {
			return GetMoleFraction(mt,MaterialType.FilterAcceptAll);
		}

		/// <summary>
		/// Returns the number of moles of the specified material type that exist in a liter of the mixture.
		/// </summary>
		/// <param name="mt">The specified material type</param>
		/// <returns>Moles per liter of the specified material type.</returns>
		public double GetMolarConcentration(MaterialType mt){
            Update();
			double nMoles = double.NaN;
			foreach ( Substance substance in Constituents ) {
				if ( substance.MaterialType.Equals(mt) ) {
					nMoles = substance.Mass / mt.MolecularWeight;
				}
			}

			return nMoles/Volume;
		}

        /// <summary>
        /// Gets the material type of the mixture - always null.
        /// </summary>
        /// <value>The type of the material.</value>
		public MaterialType MaterialType => null;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name of the mixture.</value>
        public string Name => m_name;

        /// <summary>
        /// Gets the mass of the mixture.
        /// </summary>
        /// <value>The mass.</value>
        public double Mass {
            get { // cache this.
                Update();
                return m_constituentSubstances.Values.Cast<Substance>().Sum(substance => substance.Mass);
            }
        }

        /// <summary>
        /// Gets or sets the temperature of the mixture.
        /// </summary>
        /// <value>The temperature.</value>
        public double Temperature { 
            get {
                Update();
                return m_temp - K.CELSIUS_TO_KELVIN;
            }
            set {
                Update();
#if DEBUG
				if ( s_breakOnIsNaNTemp && double.IsNaN(value) ) Debugger.Break();
#endif
				double temp = m_temp;
                if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
                m_temp = value+K.CELSIUS_TO_KELVIN;
                foreach ( Substance s in m_constituentSubstances.Values ) {
                    s.Temperature = value;
                }
				if ( temp != m_temp ) {
					m_ssh.ReportChange();
				    MaterialChanged?.Invoke(this,MaterialChangeType.Temperature);
				}
            }
        }

		/// <summary>
		/// The specific heat of the mixture, in Joules per kilogram degree-K.
		/// </summary>
		public double SpecificHeat {
			get {
                Update();
				// Aggregate specific heat is for i all substances, c-total = Sum(m-sub-i * c-sub-i)/Sum(m-sub-i)
				double sumMici = 0.0;  // Sum(m-sub-i * c-sub-i)
				double mt = 0.0;        // Sum(m-sub-i)
				foreach ( Substance s in m_constituentSubstances.Values ) {
					sumMici += s.Mass*s.SpecificHeat;
					mt += s.Mass;
				}
				return ( mt==0?double.NaN:sumMici/mt );
			}
		}

		/// <summary>
        /// The Latent Heat Of Vaporization of the mixture, in Joules per kilogram.
		/// </summary>
		public double LatentHeatOfVaporization {
			get {
                Update();
				// Aggregate LHoV is for i all substances, lhov-total = Sum(m-sub-i * lhov-sub-i)/Sum(m-sub-i)
				double sumMili = 0.0;  // Sum(m-sub-i * lhov-sub-i)
				double mt = 0.0;        // Sum(m-sub-i)
				foreach ( Substance s in m_constituentSubstances.Values ) {
					sumMili += s.Mass*s.LatentHeatOfVaporization;
					mt += s.Mass;
				}
				return ( mt==0?double.NaN:sumMili/mt );
			}
		}

        /// <summary>
        /// Gets or sets the volume of the mixture, in liters. If the mixture is a non-gaseous mixture,
        /// then gases are presumed to be in solution, and therefore to have zero incremental volume. If
        /// the mixture is all gaseous, though, the volumes of the simply gases are simply added together.
        /// </summary>
        /// <value>The volume of the mixture, in liters.</value>
		public double Volume {
            get {
                Update();
                double vol = 0.0;
                bool allGases = true;
                foreach (Substance substance in m_constituentSubstances.Values) {
                    allGases &= ( substance.State == MaterialState.Gas );
                    vol += substance.Volume;
                }

                if (!allGases) {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (Substance substance in m_constituentSubstances.Values) {
                        if (substance.State == MaterialState.Gas) {
                            vol -= substance.Volume;
                        }
                    }
                }

                return vol;
            }
            set { throw new ApplicationException("Not implemented.");}
        }

        /// <summary>
        /// Gets the density of the material in kilograms per liter.
        /// </summary>
        /// <value>The density.</value>
		public double Density => Mass/Volume;

        /// <summary>
		/// Estimates a boiling point for the substance.
		/// </summary>
		/// <param name="atPressureInPascals">Pressure (in Pascals) at which the BP is desired.</param>
		/// <returns>Temperature in degrees Celsius.</returns>
        public double GetEstimatedBoilingPoint(double atPressureInPascals) {

            double retval = VaporPressureCalculator.ComputeBoilingPoint(this, atPressureInPascals);

            #region Perform boiling point elevation, if necessary

            // Perform boiling point elevation. We calculate the mole-fraction-weighted kb (ebullioscopic constant)
            // for solvents and van 't Hoff factor for solutes, and then use them in the following relationship:
            // From http://en.wikipedia.org/wiki/Colligative and several other sites,
            // 
            // Boiling point elevation
            // Boiling Point of Total = Boiling Point of solvent + deltaTb
            // 
            // where
            // 
            // deltaTb = (molality * i) * Kb ,
            // (Kb = ebullioscopic constant, which is 0.51°C kg/mol for the boiling point of water; i = Van 't Hoff factor)
            // 
            double totalMassOfLiquid = 0.0;
            double totalMolesOfLiquid = 0.0;
            double ebullioscopicConstant = 0.0;

            double totalMolesOfSolid = 0.0;

            Update();
            foreach (Substance s in Constituents) {
                switch (s.State) {
                    case MaterialState.Unknown:
                        break;
                    case MaterialState.Solid:
                        totalMolesOfSolid += s.Moles * s.MaterialType.VanTHoffFactor;
                        break;
                    case MaterialState.Liquid:
                        totalMassOfLiquid += s.Mass;
                        totalMolesOfLiquid += s.Moles;
                        ebullioscopicConstant += (s.Moles * s.MaterialType.EbullioscopicConstant);
                        break;
                    case MaterialState.Gas:
                        break;
                    default:
                        break;
                }
            }

            if (totalMolesOfSolid > 0 && totalMolesOfLiquid > 0) {

                ebullioscopicConstant /= totalMolesOfLiquid;

                double deltaT = (totalMolesOfSolid / totalMassOfLiquid) /*Molality*/ * ebullioscopicConstant;

                retval += deltaT;
            }
            #endregion Perform boiling point elevation, if necessary

            return retval;
        }

        /// <summary>
        /// Estimates a boiling point for the substance.
        /// </summary>
        /// <param name="pressure">The pressure.</param>
        /// <param name="pressureUnits">The pressure units.</param>
        /// <returns>Temperature in degrees Celsius.</returns>
        public double GetEstimatedBoilingPoint(double pressure, PressureUnits pressureUnits) {
            // Must convert to Pascals
            switch (pressureUnits) {
                case PressureUnits.Atm:
                    pressure *= 101325;
                    break;
                case PressureUnits.Bar:
                    pressure *= 100000;
                    break;
                case PressureUnits.mmHg:
                    pressure *= 133.322;
                    break;
                case PressureUnits.Pascals:
                    break;
            }

            return GetEstimatedBoilingPoint(pressure);
        }



        /// <summary>
        /// Suspends the issuance of change events. When change events are resumed, one change event will be fired if
        /// the material has changed. This prevents a cascade of change events that would be issued, for example during
        /// the processing of a reaction.
        /// </summary>
        public void SuspendChangeEvents(){
            if (m_eventDistiller == null) {
                m_eventDistiller = new MaterialChangeDistiller();
            }
            m_eventDistiller.Hold(ref MaterialChanged);
        }

        /// <summary>
        /// Resumes the change events. When change events are resumed, one change event will be fired if
        /// the material has changed. This prevents a cascade of change events that would be issued, for example during
        /// the processing of a reaction.
        /// </summary>
        /// <param name="issueSummaryEvents">if set to <c>true</c>, issues summarizing event for each change type that has occurred.</param>
        public void ResumeChangeEvents(bool issueSummaryEvents){
            Update();
            if (m_eventDistiller == null) {
                m_eventDistiller = new MaterialChangeDistiller();
            }
            m_eventDistiller.Release(ref MaterialChanged, issueSummaryEvents);
        }

        /// <summary>
        /// Adds the specified material to the mixture.
        /// </summary>
        /// <param name="materialToAdd">The material to add.</param>
        public void AddMaterial( IMaterial materialToAdd ) {
            Update();
            if (!m_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, m_writeLock);
            Substance substanceToAdd = materialToAdd as Substance;
            if ( substanceToAdd != null ) {
                AddSubstance(substanceToAdd);
            } else { // Adding a mixture
                foreach ( Substance s in ((Mixture)materialToAdd).m_constituentSubstances.Values ) {
                    AddSubstance(s);
                }
            }
            RecomputeTemperature(0.0);
            m_ssh.ReportChange();
        }

        internal void ReactionGoingToHappen(Reaction reaction)
        {
            OnReactionGoingToHappen?.Invoke(reaction,this);
        }

        internal void ReactionHappened(ReactionInstance ri)
        {
            OnReactionHappened?.Invoke(ri);
        }

        private void AddSubstance(Substance substanceToAdd){
            Update();
            // (it's private) if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
            if ( substanceToAdd.Mass == 0 ) {
                // TODO: Figure out how to deal with this!
                return;
            }
            Substance s = (Substance)m_constituentSubstances[substanceToAdd.Name];
            double temperature = Temperature;
            if ( s != null ) {                      //Augment an existing substance.
                s.Add(substanceToAdd);
            } else {                                // Add a new substance.
                m_constituentSubstances.Add(substanceToAdd.Name,substanceToAdd);
                m_writeLock.AddChild((WriteLock)substanceToAdd);
            }
            RecomputeTemperature(0.0);
            if (Temperature != temperature)
            {
                MaterialChanged?.Invoke(this, MaterialChangeType.Temperature);
            }
            m_ssh.ReportChange();
            MaterialChanged?.Invoke(this,MaterialChangeType.Contents);
        }

        /// <summary>
        /// Removes all of the specified substance from the mixture.
        /// </summary>
        /// <param name="s">The substance to be removed.</param>
        /// <returns></returns>
        public IMaterial RemoveMaterial(Substance s) {
            Update();
            if (!m_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, m_writeLock);
            IMaterial matl = RemoveMaterial(s.MaterialType, s.Mass);
            WriteLock wl;
            Substance substance = matl as Substance;
            if (substance != null)
                wl = (WriteLock)substance;
            else if (matl is Mixture)
                wl = (WriteLock)(Mixture)matl;
            else
                throw new ApplicationException("Unknown IMaterial type encountered.");
            m_writeLock.RemoveChild(wl);
            wl.SetWritable(true);
            return matl;
        }
        /// <summary>
        /// Removes all of the specified material from the mixture.
        /// </summary>
        /// <param name="m">The mixture to be removed.</param>
        /// <returns></returns>
        public IMaterial RemoveMaterial(Mixture m) {
            Update();
            if (!m_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, m_writeLock);
            Mixture retval = new Mixture();
            foreach (Substance s in m.Constituents) {
                Substance extract = (Substance)RemoveMaterial(s.MaterialType, s.Mass);
                if (extract != null) {
                    WriteLock wl = (WriteLock)extract;
                    m_writeLock.RemoveChild(wl);
                    wl.SetWritable(true);
                    retval.AddMaterial(extract);
                }
            }
            return retval;
        }

        /// <summary>
        /// Removes all of the specified material from the mixture.
        /// </summary>
        /// <param name="matlType">Type of the material.</param>
        /// <returns></returns>
        public IMaterial RemoveMaterial(MaterialType matlType) {
            Update();
            if (!m_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, m_writeLock);
            IMaterial matl = RemoveMaterial(matlType,double.MaxValue);
			WriteLock wl;
            Substance substance = matl as Substance;
            if ( substance != null ) wl = (WriteLock)substance;
			else if ( matl is Mixture ) wl = (WriteLock)(Mixture)matl;
			else throw new ApplicationException("Unknown IMaterial type encountered.");
			m_writeLock.RemoveChild(wl);
			wl.SetWritable(true);
            return matl;
        }

        /// <summary>
        /// Removes the specified mass of the specified material from the mixture. If more is requested removed than is present, all that is present, is removed.
        /// </summary>
        /// <param name="matlType">Type of the specified material.</param>
        /// <param name="mass">The mass of the specified material to be removed.</param>
        /// <returns></returns>
        public IMaterial RemoveMaterial( MaterialType matlType, double mass ) {
            Update();
            IMaterial retval = null;
            if (mass > 0 && ContainedMassOf(matlType) > 0) {
                if (!m_writeLock.IsWritable) throw new WriteProtectionViolationException(this, m_writeLock);

                IMaterial requestedMaterial = matlType.CreateMass(mass, Temperature);

                Substance material = requestedMaterial as Substance;
                if (material != null) { // Removing a single substance.
                    retval = RemoveSubstance(material);
                } else {                                // removing a mixture.
                    Mixture removed = new Mixture(Model, Name);
                    foreach (Substance s in ( (Mixture)requestedMaterial ).m_constituentSubstances.Values) {
                        Substance removee = (Substance)RemoveSubstance(s);
                        if (removee != null) {
                            removed.AddSubstance(removee);
                            m_writeLock.RemoveChild((WriteLock)removee);
                            ( (WriteLock)removee ).SetWritable(true);
                        }
                    }
                    retval = removed;
                }
                m_ssh.ReportChange();
                MaterialChanged?.Invoke(this, MaterialChangeType.Contents);
            }
            return retval ?? (retval = matlType.CreateMass(0.0, 17.0));
        }

        /// <summary>
        /// Removes the specified mass of the mixture in even proportions of all material types. If more is requested removed than is present, all that is present, is removed.
        /// </summary>
        /// <param name="mass">The mass of the mixture to be removed.</param>
        /// <returns></returns>
        public IMaterial RemoveMaterial(double mass) {
            Update();
            Mixture effluent = new Mixture(Model, Name);

            if (mass > 0) {
                if (!m_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, m_writeLock);
                double proportionOfTotal = mass / Mass;
                if (proportionOfTotal > 1.0)
                    proportionOfTotal = 1.0; // Can't remove more than is there.


                ArrayList materialsToRemove = new ArrayList();
                foreach (Substance substance in m_constituentSubstances.Values) {
                    if (s_diagnostics)
                        _Debug.WriteLine("Adding " + substance.MaterialType.Name + " to the list of things to make go away.");
                    if (substance.Mass > 0)
                        materialsToRemove.Add(substance);
                }

                foreach (Substance substance in materialsToRemove) {
                    IMaterial removee = RemoveMaterial(substance.MaterialType, proportionOfTotal * substance.Mass);
                    if (removee == null) {
                        if (s_diagnostics) {
                            _Debug.WriteLine("My mass is " + Mass + " and the mass argument passed in was " + mass);
                            _Debug.WriteLine(( new StackTrace() ).ToString());
                            _Debug.WriteLine("Removee was null, trying to remove " + ( proportionOfTotal * substance.Mass ) + " kg of " + substance.MaterialType.Name + " from " + ToString());
                        }
                    } else {
                        if (s_diagnostics) {
                            _Debug.WriteLine("Removing " + removee + " leaving " + Mass);
                        }
                        effluent.AddMaterial(removee);

                    }
                }

                // TODO: material change type of "Homogeneous Removal". (Does not trigger reaction search.)
                MaterialChanged?.Invoke(this, MaterialChangeType.Contents);
                m_ssh.ReportChange();
            }
            return effluent;
        }

		/// <summary>
		/// Returns an estimate of the amount and constituents of this mixture that would exist in a vapor space of the given size at the given temperature.
		/// </summary>
		/// <param name="volumeInM3">The volume of the vapor space, in cubic meters.</param>
		/// <param name="temperatureInK">The temperature of the vapor space, in degrees Kelvin.</param>
		/// <returns>...see comment above.</returns>
		public Mixture GetVaporFor(double volumeInM3, double temperatureInK){
            Update();
            Mixture vsMix = new Mixture(Model, Name + ".VaporSpace", Guid.NewGuid());
				
			double v  = volumeInM3;
			double T  = temperatureInK;
			double rt = Constants.MolarGasConstant * T;

			foreach ( Substance substance in Constituents ) {
				MaterialType mt = substance.MaterialType;
				double molecularWeight = mt.MolecularWeight;
				double moleFraction = GetMoleFraction(mt);
				double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt,T, TemperatureUnits.Kelvin, PressureUnits.Pascals);
		
				double kgSubstance = v*molecularWeight*moleFraction*vaporPressure/(1000*rt);

				Substance vapor = (Substance)mt.CreateMass(kgSubstance,T+K.KELVIN_TO_CELSIUS);
				Substance.ApplyMaterialSpecs(vapor,substance);
				vsMix.AddMaterial(vapor);
			}

			return vsMix;
		}

        private IMaterial RemoveSubstance( Substance requestedSubstance ) {
            Update();
            if (!m_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, m_writeLock);
            if ( requestedSubstance.Name.Equals("")) { // Removing all substances.
                Mixture m = new Mixture(Model,Name);
                double howMuchOfIt = requestedSubstance.Mass/Mass;
                foreach ( Substance s in m_constituentSubstances.Values ) {
                    Substance removee = s.Remove(howMuchOfIt*s.Mass);
                    if ( howMuchOfIt == 1.0 ) {
                        m_writeLock.RemoveChild((WriteLock)s);
                        m_constituentSubstances.Remove(s.Name);
                    }
                    m.AddSubstance(removee);
                }
                m_ssh.ReportChange();
                return m;
            } else {
                Substance s = (Substance)m_constituentSubstances[requestedSubstance.Name];
                if ( s != null ) {
                    requestedSubstance = s.Remove(requestedSubstance);
                    if ( s.Mass == 0.0 ) {
                        m_writeLock.RemoveChild((WriteLock)s);
                        m_constituentSubstances.Remove(requestedSubstance.Name);
                    }
                    m_ssh.ReportChange();
                    return requestedSubstance;
                } else {
                    /* Do something dramatic by way of refusal. */ return null;
                }
            }
        }

        /// <summary>
        /// Adds the specified number of joules of energy to the mixture.
        /// </summary>
        /// <param name="joules">The joules to add to the mixture.</param>
        public void AddEnergy(double joules){
            Update();
            if (!m_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, m_writeLock);
            RecomputeTemperature(joules);
        }

        /// <summary>
        /// Recomputes the temperature of the mixture after adding in the supplemental energy.
        /// </summary>
        /// <param name="supplementalEnergy">The supplemental energy, in Joules, to be added to the mixture prior to temperature recomputation.</param>
        protected void RecomputeTemperature(double supplementalEnergy){
            Update();
            if (!m_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, m_writeLock);
			if ( m_constituentSubstances.Values.Count.Equals(0) ) {
				m_temp = K.CELSIUS_TO_KELVIN+20.0;
				return;
			}
            double energy = 0.0;
            double divisor = 0.0;
            foreach ( Substance s in m_constituentSubstances.Values ) {
                energy += s.Energy;
                divisor += s.MaterialType.SpecificHeat*s.Mass;
            }
            m_temp = (energy+supplementalEnergy)/divisor;
            foreach ( Substance s in m_constituentSubstances.Values ) {
                s.Temperature = Temperature; // Expects it to be set in degrees C
            }
        }

        /// <summary>
        /// Obtains the <see cref="Highpoint.Sage.SimCore.WriteLock"/> that represents this mixture.
        /// </summary>
        /// <param name="mixture">The mixture.</param>
        /// <returns>The <see cref="Highpoint.Sage.SimCore.WriteLock"/> that represents this mixture.</returns>
        public static explicit operator WriteLock(Mixture mixture) {
            return mixture.m_writeLock;
        }

        /// <summary>
        /// Retrieves a memento from the mixture.
        /// </summary>
        /// <value></value>
        public IMemento Memento {
            get {
                Update();
                if (s_diagnostics)
                    _Debug.WriteLine("Snapshotting mixture " + Name + " which has " + m_constituentSubstances.Count + " substances in it.");
                if ( !m_ssh.HasChanged && m_memento != null ) return m_memento;
                m_memento = new MixtureMemento(this);
                m_ssh.ReportSnapshot();
                return m_memento;
            }
            set {
                Update();
                ( (MixtureMemento)value ).Load(this);
            }
        }

        /// <summary>
        /// Class MixtureMemento creates a moment-in-time snapshot (see Memento design pattern) of a mixture.
        /// </summary>
        /// <seealso cref="Highpoint.Sage.Utility.Mementos.IMemento" />
        public class MixtureMemento : IMemento {

            #region Private Fields
            private readonly Mixture m_mixture;
            private readonly double m_massAtRecordedTime;
            private readonly double m_volumeAtRecordedTime;
            private readonly double m_tempAtRecordedTime;
            private readonly IDictionary m_substanceMementos = new Hashtable();

            #endregion 

            /// <summary>
            /// Creates a new instance of the <see cref="T:MixtureMemento"/> class.
            /// </summary>
            /// <param name="mixture">The mixture.</param>
            public MixtureMemento(Mixture mixture){
                m_mixture = mixture;
                m_massAtRecordedTime = m_mixture.Mass;
                m_volumeAtRecordedTime = m_mixture.Volume;
                m_tempAtRecordedTime = m_mixture.Temperature;
                foreach ( Substance substance in mixture.m_constituentSubstances.Values ) {
                    m_substanceMementos.Add(substance.Name,substance.Memento);
                }
            }

            /// <summary>
            /// Creates the instance of Mixture around which this mixture was created.
            /// </summary>
            /// <returns></returns>
            public ISupportsMementos CreateTarget(){
                return m_mixture;
            }

            /// <summary>
            /// Loads the contents of this Memento into the Mixture.
            /// </summary>
            /// <param name="ism">The Mixture to receive the contents of the memento.</param>
            public void Load(ISupportsMementos ism){
                Mixture mixture = (Mixture)ism;

                mixture.SuspendChangeEvents();

               // if ( mixture != m_mixture ) throw new ApplicationException("Trying to restore state into a different mixture than the one it came from.");
                mixture.m_constituentSubstances.Clear();

				if ( m_massAtRecordedTime == 0 ) {
					// There is no mass, so the temperature will not be set by the properties of its constituent substances.
					// Even though it does not matter to the energy balance, we set the temperature of the (empty) mixture
					// to the temperature it was at the time it was recorded.
					mixture.Temperature = m_tempAtRecordedTime;
				} else {
					foreach ( IMemento sm in m_substanceMementos.Values ) {
						mixture.AddSubstance((Substance)sm.CreateTarget());
					}
				}

                IMemento memento = this;
                while (memento.Parent != null) {
                    memento = memento.Parent;
                }
                memento.OnLoadCompleted += memento_OnLoadCompleted;

                OnLoadCompleted?.Invoke(this);
            }

            private void memento_OnLoadCompleted(IMemento memento) {
                m_mixture.ResumeChangeEvents(true);
            }

            /// <summary>
            /// Emits an IDictionary form of the memento that can be, for example, dumped to
            /// Trace.
            /// </summary>
            /// <returns>An IDictionary form of the memento.</returns>
            public IDictionary GetDictionary(){
                // If >= 10 entries, use a hashtable. If < 10 entries, use a ListDictionary.
                IDictionary retval;
                if ( m_mixture.Constituents.Count>=10 ) {
                    retval = new Hashtable();
                } else {
                    retval = new System.Collections.Specialized.ListDictionary();
                }
                retval.Add("Mass",m_massAtRecordedTime);
                retval.Add("Volume",m_volumeAtRecordedTime);
                retval.Add("Temp",m_tempAtRecordedTime);

                foreach ( IMemento sm in m_substanceMementos.Values ) {
                    Substance substance = (Substance)sm.CreateTarget();
                    retval.Add(substance.Name,sm.GetDictionary());
                }
                return retval;
            }

            /// <summary>
            /// Determines if this Mixture is equal to the specified Mixture.
            /// </summary>
            /// <param name="otherMemento">The specified Mixture.</param>
            /// <returns></returns>
            public bool Equals(IMemento otherMemento){
                if ( otherMemento == null ) return false;
                if ( this == otherMemento ) return true;

                MixtureMemento mmog = otherMemento as MixtureMemento;
                if ( m_substanceMementos.Count!=mmog?.m_substanceMementos.Count ) return false;
                
                foreach ( DictionaryEntry de in m_substanceMementos ) {
                    if ( !mmog.m_substanceMementos.Contains(de.Key) ) return false;
                    if ( !((IMemento)de.Value).Equals((IMemento)mmog.m_substanceMementos[de.Key])) return false;
                }

                return true;
            }

            /// <summary>
            /// This holds a reference to the memento, if any, that contains this memento.
            /// </summary>
            /// <value></value>
            public IMemento Parent { get; set; }

            /// <summary>
            /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
            /// </summary>
            public event MementoEvent OnLoadCompleted;

        }

        private readonly MementoHelper m_ssh;

        /// <summary>
        /// Fired when the memento changes.
        /// </summary>
        public event MementoChangeEvent MementoChangeEvent {
            add { m_ssh.MementoChangeEvent+=value; }
            remove { m_ssh.MementoChangeEvent-=value; }
        }
        /// <summary>
        /// Reports whether the Mixture has changed relative to its memento
        /// since the last memento was recorded.
        /// </summary>
        /// <value></value>
        public bool HasChanged => m_ssh.HasChanged;

        /// <summary>
        /// Indicates whether this Mixture can report memento changes to its
        /// parent. (Mementos can contain other mementos.)
        /// </summary>
        /// <value></value>
        public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.Mixture"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.Mixture"></see>.
        /// </returns>
        public override string ToString() {
            Update();
            return ToString("F1", "F2");
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </returns>
        public string ToStringWithoutTemperature() {
            Update();
            return ToStringWithoutTemperature("F2");
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </summary>
        /// <param name="massFmt">The mass format string. For example, &quot;F2&quot; will display to two decimals.</param>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </returns>
        public string ToStringWithoutTemperature(string massFmt) {
            Update();
            List<Substance> subs = m_constituentSubstances.Values.Cast<Substance>().ToList();

            subs.Sort(Substance.ByMassThenName);
            subs.Reverse(); // Want decreasing mass.

            string retval = "";
            if (subs.Count == 0) {
                retval += "nothing.";
            } else {
                List<string> fmttdSubs = new List<string>();
                subs.ForEach(delegate(Substance substance) { fmttdSubs.Add(substance.ToStringWithoutTemperature(massFmt)); });
                retval += Utility.StringOperations.ToCommasAndAndedList(fmttdSubs);
            }
			return retval;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// Uses caller-supplied format strings in forming the numbers representing mass and temperature.
        /// </summary>
        /// <param name="tempFmt">The temperature's numerical format string.</param>
        /// <param name="massFmt">The mass's numerical format string.</param>
        /// <returns></returns>
		public string ToString(string tempFmt, string massFmt){
            return "Mixture (" + Temperature.ToString(tempFmt) + " deg C) of " + ToStringWithoutTemperature(massFmt);
		}

        private object m_tag;
        /// <summary>
        /// Gets or sets the tag, which is a user-supplied data element.
        /// </summary>
        /// <value>The tag.</value>
        public object Tag { get { return m_tag; } set { m_tag = value; } }

        #region Implementation of IModelObject

        /// <summary>
        /// Gets the GUID of the Mixture.
        /// </summary>
        /// <value>The GUID.</value>
        public Guid Guid { get; private set; }

        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model { get; }

        #endregion

		#region >>> IXmlPersistable members <<<
		/// <summary>
		/// Stores this object to the specified XmlSerializationContext.
		/// </summary>
		/// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public void SerializeTo(XmlSerializationContext xmlsc){
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("Guid",Guid);
			xmlsc.StoreObject("Substances",m_constituentSubstances);
		}
		/// <summary>
		/// Reconstitutes this object from the specified XmlSerializationContext.
		/// </summary>
		/// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public void DeserializeFrom(XmlSerializationContext xmlsc){
			m_name = (string)xmlsc.LoadObject("Name");
			Guid = (Guid)xmlsc.LoadObject("Guid");
			m_constituentSubstances = (Hashtable)xmlsc.LoadObject("Substances");
		}
        #endregion

        /// <summary>
        /// Class MaterialChangeDistiller is an intermediary between a mixture and its change event listeners. 
        /// When Hold() is called, it captures and stores all change events. When release is called, if 
        /// issueSummaryEvents is set to true, it creates a set of summary events that summarize each of the 
        /// change types (mass, temperature and constituents) into the minimum number of events necessary to
        /// represent those changes. This is useful for ongoing processes when you only want to log material
        /// changes at the start and end of a complex series of steps.
        /// </summary>
        public class MaterialChangeDistiller {

            #region Private Fields
            private MaterialChangeListener m_realHandler;
            private event MaterialChangeListener Filter;
            private readonly bool m_autoDigest;
            private readonly IExecutive m_exec;
            private readonly List<Entry> m_changesTranspired = new List<Entry>();
            private int m_attaches;
            #endregion Private Fields

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialChangeDistiller"/> class.
            /// </summary>
            public MaterialChangeDistiller() {
                Filter = OnMaterialChanged;
            }

            /// <summary>
            /// Does auto digests where all changes occurring in one timeslice are rolled into one event at the end of the timeslice.
            /// </summary>
            /// <param name="exec">The executive that governs time advancement.</param>
            /// <param name="mcl">The material change listener that will be fired at the end of the timeslice.</param>
            public MaterialChangeDistiller(IExecutive exec, ref MaterialChangeListener mcl) : this() {
                m_exec = exec;
                m_realHandler = mcl;
                mcl = Filter;
                m_autoDigest = true;
            }


            /// <summary>
            /// Begins to aggregate material change events coming from the specified listener.
            /// </summary>
            /// <param name="mcl">The specified listener.</param>
            /// <exception cref="ApplicationException"></exception>
            public void Hold(ref MaterialChangeListener mcl){
                if (!m_autoDigest) {
                    m_realHandler = mcl;
                    mcl = Filter;
                } else {
                    throw new ApplicationException(string.Format(s_exception_Message,"held", "hold 'til"));
                }
            }

            /// <summary>
            /// Summarizes and releases the aggregated material change events that came from the specified listener.
            /// </summary>
            /// <param name="mcl">The specified listener.</param>
            /// <param name="issueSummaryEvents">if set to <c>true</c>issue summary events. Otherwise, simply discard the change notifications.</param>
            /// <exception cref="ApplicationException"></exception>
            public void Release(ref MaterialChangeListener mcl, bool issueSummaryEvents) {
                if (mcl == null) throw new ArgumentNullException(nameof(mcl));
                mcl = m_realHandler;
                if (!m_autoDigest) {
                    _Release(ref mcl, issueSummaryEvents);
                } else {
                    throw new ApplicationException(string.Format(s_exception_Message,"released", "release at"));
                }
            }

            private void _Release(ref MaterialChangeListener mcl, bool issueSummaryEvents) {

                Distill(m_changesTranspired);

                if (mcl != null && issueSummaryEvents) {
                    List<Entry> entries = new List<Entry>(m_changesTranspired);
                    entries.Sort(EntryComparer);
                    foreach (Entry entry in entries) {
                        mcl(entry.Material, entry.Mct);
                    }
                }
            }

            private int EntryComparer(Entry x, Entry y)
            {
                int tmp;
                if ((tmp = Comparer<Enum>.Default.Compare(x.Mct, y.Mct)) != 0) return tmp;
                if ((tmp = Comparer<double>.Default.Compare(x.Material.Mass, y.Material.Mass)) != 0) return tmp;
                return string.Compare(x.ToString(),y.ToString(),StringComparison.InvariantCulture);
            }

            private void Distill(List<Entry> changesTranspired) {
                Entry contents = changesTranspired.FindLast(e => e.Mct == MaterialChangeType.Contents);
                Entry temperature = changesTranspired.FindLast(e => e.Mct == MaterialChangeType.Temperature);
                changesTranspired.Clear();
                if (contents != null)
                    changesTranspired.Add(contents);
                if (temperature != null)
                    changesTranspired.Add(temperature);
            }

            /// <summary>
            /// Called when the material this distiller is listening to has changed, and this distiller has been told to report those changes.
            /// </summary>
            /// <param name="theMaterial">The material.</param>
            /// <param name="mct">The MCT.</param>
            public void OnMaterialChanged(IMaterial theMaterial, MaterialChangeType mct) {
                
                Entry e = new Entry(theMaterial,mct);
                if (!m_changesTranspired.Contains(e)) {
                    m_changesTranspired.Add(e);
                }

                if (m_autoDigest) {
                    if (m_attaches++ == 0) {
                        m_exec.ClockAboutToChange += m_exec_ClockAboutToChange;
                    }
                }
            }

            void m_exec_ClockAboutToChange(IExecutive exec) {
                _Release(ref m_realHandler, true);
                m_exec.ClockAboutToChange -= m_exec_ClockAboutToChange;
                m_attaches = 0;
            }

            internal class Entry {
                public readonly IMaterial Material;
                public readonly MaterialChangeType Mct;

                public Entry( IMaterial m, MaterialChangeType mct){Material=m;Mct=mct;}
                public override string ToString(){return "Material Change : " + Material + ", change type " + Mct; }
                public override bool Equals(object obj) {
                    Entry otherOne = (Entry)obj;
                    return otherOne.Material.Equals(Material) && otherOne.Mct.Equals(Mct);
                }
                public override int GetHashCode() {
                    return Material.GetHashCode() ^ Mct.GetHashCode();
                }
            }

            private static readonly string s_exception_Message = "MaterialChangeDistiller set to AutoDigest being explicitly {0} - this is illegal. Such MCD's always {1} the end of the clock cycle.";

        }

        /// <summary>
        /// Gets the substance of the specified material type from the mixture that this MaterialChangeDistiller is watching.
        /// </summary>
        /// <param name="materialType">Type of the material.</param>
        /// <returns>Substance.</returns>
        public Substance GetSubstance(MaterialType materialType)
        {
            return m_constituentSubstances[materialType.Name] as Substance;
        }
    }
}
