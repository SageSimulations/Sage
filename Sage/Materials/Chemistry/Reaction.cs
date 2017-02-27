/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.Utility;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Highpoint.Sage.Materials.Chemistry
{
	/// <summary>
	/// Implemented by a method that can be notified when a reaction is going to happen.
	/// </summary>
    public delegate void ReactionGoingToHappenEvent(Reaction reaction, Mixture mixture);
	/// <summary>
	/// Implemented by a method that can be notified after a reaction has happened.
	/// </summary>
	public delegate void ReactionHappenedEvent(ReactionInstance reactionInstance);

	/// <summary>
	/// A Reaction is a class that characterizes a chemical transformation. A reaction represents that
	/// when it occurs, 'a' mass units of substance 'A' reacts with 'b' nass units of substance 'B' to
	/// yield 'c' mass units of substance 'C' and 'd' mass units of substance 'D'. Additionally, it 
	/// represents that the reaction will habben with a forward execution percentage of 'x' percent, and
	/// were it to execute 100%, it would yield or consume 'y' energy units - permitting it to be an
	/// endothermic or exothermic reaction.
	/// </summary>
    public class Reaction : IModelObject, IXmlPersistable {
        private ArrayList m_reactants = new ArrayList();
        private ArrayList m_products  = new ArrayList();
        private double m_rxPct  = 1.0;
        private double m_energy;
		
		/// <summary>
		/// This is a guid that is XOR'ed with the Reaction's guid to obtain the initial guid of its reaction instances.
		/// </summary>
		public static readonly Guid DEFAULT_RI_GUIDMASK = new Guid("{A23ED536-E841-4e4d-B087-5DC62FC0140C}");
		private Guid m_nextRiGuid;

                
        //private bool m_diagnostics = false;
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Reactions");

        /// <summary>
        /// Fired before a reaction is processed.
        /// </summary>
        public event ReactionGoingToHappenEvent ReactionGoingToHappenEvent;
        
        /// <summary>
        /// Fired after a reaction is processed.
        /// </summary>
        public event ReactionHappenedEvent ReactionHappenedEvent;

        /// <summary>
        /// Creates a new instance of the <see cref="T:Reaction"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
		public Reaction(IModel model, string name, Guid guid){
            InitializeIdentity(model, name, null, guid);

            if ( model != null ) model.Starting+=model_Starting;
			m_model = model;
			m_nextRiGuid = GuidOps.XOR(m_guid,DEFAULT_RI_GUIDMASK);

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
        /// Defines a reactant in this reaction.
        /// </summary>
        /// <param name="material">The material type that is to be the reactant.</param>
        /// <param name="mass">The mass of reactant that participates in one unit of the reaction.</param>
		public void AddReactant(MaterialType material, double mass){
            m_reactants.Add(new ReactionParticipant(material,mass));
            IdentifyCatalysts();
        }

        private void IdentifyCatalysts() {

            foreach ( ReactionParticipant rp in m_reactants ) rp.IsCatalyst = false;
            foreach ( ReactionParticipant rp in m_products ) rp.IsCatalyst = false;

            foreach ( ReactionParticipant reactant in m_reactants ) {
                foreach ( ReactionParticipant product in m_products ) {
                    if ( reactant.MaterialType.Equals(product.MaterialType) && reactant.Mass.Equals(product.Mass)) {
                        reactant.IsCatalyst = true;
                        product.IsCatalyst = true;
                    }
                }
            }
        }

        /// <summary>
        /// Defines a product in this reaction.
        /// </summary>
        /// <param name="material">The material that is to be a product of this reaction.</param>
        /// <param name="mass">The mass of product that is produced by one unit of this reaction.</param>
        public void AddProduct(MaterialType material, double mass)
        {
            m_products.Add(new ReactionParticipant(material,mass));
            IdentifyCatalysts();
        }

        /// <summary>
        /// Gets the reactants of this reaction.
        /// </summary>
        /// <value>The reactants.</value>
        public IList Reactants => ArrayList.ReadOnly(m_reactants);

	    /// <summary>
        /// Gets the products of this reaction.
        /// </summary>
        /// <value>The products.</value>
        public IList Products => ArrayList.ReadOnly(m_products);

	    /// <summary>
        /// Gets or sets the expected percent completion of this reaction.
        /// </summary>
        /// <value>The percent completion.</value>
        public double PercentCompletion { set { m_rxPct = value; } get { return m_rxPct; } }

        /// <summary>
        /// Gets or sets the heat of reaction of this reaction. This is the number of joules added to, or removed from, the reaction when it occurs in the quantities specified.
        /// number of joules added to, or removed from, the reaction when it occurs in the quantities and direction specified. In other words, a reaction defined as 
        /// 1 X + 2 Y &lt;--&gt; 3 Z liberates 2.4 joules when 1 mole of X and 2 moles of Y react to completion.
        /// </summary>
        /// <value>The heat of reaction.</value>
        public double HeatOfReaction { set { m_energy = value; } get { return m_energy; } }

	    public bool IsValid
	    {
	        get
	        {
                #region First determine if the reaction contains only catalysts. If so, it will continue ad infinitum.
                bool retval = false;
                foreach (ReactionParticipant rp in m_reactants)
                {
                    if (!rp.IsCatalyst) retval = true;
                }
                foreach (ReactionParticipant rp in m_reactants)
                {
                    if (!rp.IsCatalyst) retval = true;
                }
                #endregion

	            return retval;
	        }
        }
        /// <summary>
        /// Causes the reaction to take place, if possible, in (and perform modifications to, the provided target mixture.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public ReactionInstance React(Mixture target){
            // We will handle a reaction's completion percentage/equilibrium
            // calculation by first using the reverse reaction to move all of
            // the already-existent (in proportion) products over to the
            // reactant side, and then move rxnPct % back to the product side.
            double fwdScale = double.MaxValue;
            double revScale = double.MaxValue;

            if ( m_reactants.Count != 0 ) {
                foreach ( ReactionParticipant rp in m_reactants ) {

                    double howMuch = target.ContainedMassOf(rp.MaterialType);
                    if ( howMuch > 0 && rp.IsCatalyst ) howMuch = double.MaxValue;
                    fwdScale = Math.Min(fwdScale, howMuch / rp.Mass);
                    if ( s_diagnostics )
                        Trace.WriteLine("target contains " + howMuch + " of reactant " + rp.MaterialType.Name + " so fwdScale is " + fwdScale);

                }
                if ( fwdScale == double.MaxValue ) fwdScale = 0.0;
            } else fwdScale = 0.0;
			fwdScale *= m_rxPct;

            if ( m_products.Count != 0 ) {
                foreach ( ReactionParticipant rp in m_products ) {

                    double howMuch = target.ContainedMassOf(rp.MaterialType);
                    if ( howMuch > 0 && rp.IsCatalyst ) howMuch = double.MaxValue;
                    revScale = Math.Min(revScale, howMuch / rp.Mass);
                    if ( s_diagnostics )
                        Trace.WriteLine("target contains " + howMuch + " of product " + rp.MaterialType.Name + " so revScale is " + revScale);
                }

                if ( revScale == double.MaxValue )
                    revScale = 0.0;
            } else
                revScale = 0.0;
			revScale *= (1-m_rxPct);

            // If we're going to undo and then redo the same thing, the reaction didn't happen.
			if (( fwdScale == 0.0 && revScale == 0.0 ) || ( Math.Abs(fwdScale-revScale) < 0.000001 )) {
				if ( s_diagnostics ) Trace.WriteLine("Reaction " + Name + " won't happen in mixture " + target);
				return null;
			}
            
			if ( s_diagnostics ) Trace.WriteLine("Mixture is " + target);
			if ( s_diagnostics ) Trace.WriteLine("Reaction " + Name + " is happening. " + ToString());

            ReactionGoingToHappenEvent?.Invoke(this,target);
            target.ReactionGoingToHappen(this);

            Mixture mixtureWas = (Mixture)target.Clone();
            target.SuspendChangeEvents();

			if ( s_diagnostics ) {
			    Trace.WriteLine(revScale > 0.0 ? "Performing reverse reaction..." : "Reverse reaction won't happen.");
			}
            if ( revScale > 0.0 ) React(target,m_products,m_reactants,revScale);

			if ( s_diagnostics ) {
			    Trace.WriteLine(fwdScale > 0.0 ? "Performing forward reaction..." : "Forward reaction won't happen.");
			}
			if ( fwdScale > 0.0 ) React(target,m_reactants,m_products,fwdScale*m_rxPct);

            // Percent completion is pulled out of the Reaction object.
			
            ReactionInstance ri = new ReactionInstance(this,fwdScale,revScale,m_nextRiGuid);
			m_nextRiGuid = GuidOps.Increment(m_nextRiGuid);

            ReactionHappenedEvent?.Invoke(ri);
            target.ReactionHappened(ri);

            target.AddEnergy(m_energy*(fwdScale-revScale));

            target.ResumeChangeEvents(!mixtureWas.ToString("F2", "F3").Equals(target.ToString("F2", "F3"))); // Only emit summary events if the mixture has changed.

            return ri;
        }

        /// <summary>
        /// Reacts in the specified mix.
        /// </summary>
        /// <param name="mix">The mixture in which the reaction is to take place.</param>
        /// <param name="from">The <see cref="Highpoint.Sage.Materials.Chemistry.Reaction.ReactionParticipant"/>s that are eliminated.</param>
        /// <param name="to">The <see cref="Highpoint.Sage.Materials.Chemistry.Reaction.ReactionParticipant"/>s that are created.</param>
        /// <param name="scale">The scale of the reaction.</param>
        protected void React(Mixture mix, ArrayList from, ArrayList to, double scale ){

            foreach ( ReactionParticipant rp in from ) {
                if ( s_diagnostics ) Trace.WriteLine("Eliminating " + (rp.Mass*scale) + " of " + rp.MaterialType.Name + " from mixture.");
                mix.RemoveMaterial(rp.MaterialType,rp.Mass*scale);
            }

            foreach ( ReactionParticipant rp in to ) {
                if ( s_diagnostics ) Trace.WriteLine("Adding " + (rp.Mass*scale) + " of " + rp.MaterialType.Name + " to mixture.");
                mix.AddMaterial(rp.MaterialType.CreateMass(rp.Mass*scale,mix.Temperature));
            }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString(){
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for ( int i = 0 ; i < Reactants.Count ; i++ ) {
                sb.Append(((ReactionParticipant)Reactants[i]).ToString(1));
                if ( i < Reactants.Count-1 ) sb.Append(" + ");
            }
            sb.Append(" ==> ");
            for ( int i = 0 ; i < Products.Count ; i++ ) {
                sb.Append(((ReactionParticipant)Products[i]).ToString(1));
                if ( i < Products.Count-1 ) sb.Append(" + ");
            }
            return sb.ToString();
        }

	    /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>The tag.</value>
        public object Tag { get; set; }

	    /// <summary>
		/// Describes the role a material plays in a reaction.
		/// </summary>
        public enum MaterialRole { 
			/// <summary>
			/// This material is a reactant.
			/// </summary>
			Reactant,
			/// <summary>
			/// This material is a product.
			/// </summary>
			Product, 
			/// <summary>
			/// This material is both a reactant and a product (e.g. a catalyst).
			/// </summary>
			Either 
		};

        #region >>> Implementation of IHasIdentity <<<
		private IModel m_model;
		/// <summary>
		/// The model to which this reaction belongs.
		/// </summary>
		public IModel Model => m_model;

		private string m_name;
		/// <summary>
		/// The name of this reaction.
		/// </summary>
		public string Name => m_name;

	    private string m_description;
		/// <summary>
		/// A description of this reaction.
		/// </summary>
		public string Description => m_description ?? m_name;

	    private Guid m_guid = Guid.Empty;
		/// <summary>
		/// The Guid by which this reaction will be known.
		/// </summary>
		public Guid Guid => m_guid;
        #endregion

		#region >>> Serialization Support <<< 
        /// <summary>
        /// Creates a new instance of the <see cref="T:Reaction"/> class.
        /// </summary>
		public Reaction(){}
        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public void SerializeTo(XmlSerializationContext xmlsc){
			xmlsc.StoreObject("Name",m_name);
			xmlsc.StoreObject("Guid",m_guid);
			xmlsc.StoreObject("Energy",m_energy);
			xmlsc.StoreObject("Products",m_products);
			xmlsc.StoreObject("Reactants",m_reactants);
			xmlsc.StoreObject("ReactionPercentage",m_rxPct);
		}
        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public void DeserializeFrom(XmlSerializationContext xmlsc){
			m_name = (string)xmlsc.LoadObject("Name");
			m_guid = (Guid)xmlsc.LoadObject("Guid");
			m_energy = (double)xmlsc.LoadObject("Energy");
			m_products = (ArrayList)xmlsc.LoadObject("Products");
			m_reactants = (ArrayList)xmlsc.LoadObject("Reactants");
			m_rxPct = (double)xmlsc.LoadObject("ReactionPercentage");
		}
		#endregion


		/// <summary>
		/// A ReactionParticipant is a struct that represents a material type and a mass, depicting
		/// the type and representative quantity of a material that will take place in a reaction. Note
		/// that the quantities are specified in proportion only.
		/// </summary>
        public class ReactionParticipant : IXmlPersistable {
            private MaterialType m_type;
            private double m_mass;

            /// <summary>
            /// Creates a new instance of the <see cref="T:ReactionParticipant"/> class.
            /// </summary>
            /// <param name="mt">The mt.</param>
            /// <param name="mass">The mass.</param>
			public ReactionParticipant(MaterialType mt, double mass){
                if ( mt == null ) throw new ApplicationException("Reaction participant specified with null MaterialType.");
                if ( mass == 0.0 ) throw new ApplicationException("Reaction participant specified with a 0.0 kg mass.");
                m_type = mt; m_mass = mass;
            }
            /// <summary>
            /// Gets the type of the material.
            /// </summary>
            /// <value>The type of the material.</value>
            public MaterialType MaterialType => m_type;

		    /// <summary>
            /// Gets the mass of the material.
            /// </summary>
            /// <value>The mass.</value>
            public double Mass => m_mass;

		    /// <summary>
            /// A reaction participant is a catalyst if it exists in equal amounts as a Reactant and a Product in the same
            /// reaction. In this case, it must be present, but does not participate quantitatively.
            /// </summary>
            public bool IsCatalyst { get; set; }
            /// <summary>
            /// Returns the fully qualified type name of this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.String"></see> containing a fully qualified type name.
            /// </returns>
            public override string ToString(){
                return m_mass + " kg. of " + m_type.Name;
            }
            /// <summary>
            /// Returns a string that represents this RactionParticipant at the specified scale.
            /// </summary>
            /// <param name="scale">The scale.</param>
            /// <returns></returns>
            public string ToString(double scale){
                return (scale*m_mass) + " kg. of " + m_type.Name;
            }

			#region >>> Serialization Support <<< 
			// public ReactionParticipant(){} // Structs cannot contain explicit parameterless constructors

            /// <summary>
            /// Stores this object to the specified XmlSerializationContext.
            /// </summary>
            /// <param name="xmlsc">The specified XmlSerializationContext.</param>
			public void SerializeTo(XmlSerializationContext xmlsc){
				xmlsc.StoreObject("MaterialType",m_type);
				xmlsc.StoreObject("Mass",m_mass);
			}
            /// <summary>
            /// Reconstitutes this object from the specified XmlSerializationContext.
            /// </summary>
            /// <param name="xmlsc">The specified XmlSerializationContext.</param>
			public void DeserializeFrom(XmlSerializationContext xmlsc){
				m_type = (MaterialType)xmlsc.LoadObject("MaterialType");
				m_mass = (double)xmlsc.LoadObject("Mass");
			}
			#endregion

		}

		private void model_Starting(IModel theModel) {
			m_nextRiGuid = GuidOps.XOR(m_guid,DEFAULT_RI_GUIDMASK);
		}
	}

    /// <summary>
    /// An exception that is thrown if there is a cycle in a dependency graph that has been analyzed.
    /// </summary>
    [Serializable]
    public class ReactionDefinitionException : Exception
    {
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp


        #region protected ctors
        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ReactionDefinitionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #endregion

        private Reaction m_reaction = null;
        /// <summary>
        /// Gets the members of the cycle.
        /// </summary>
        /// <value>The members of the cycle.</value>
        public Reaction Reaction { get { return m_reaction; } }
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public ReactionDefinitionException(Reaction reaction) : base(string.Format("{0} is not valid.", reaction.ToString()))
        {
            m_reaction = reaction;
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reaction">The reaction.</param>
        public ReactionDefinitionException(string message, Reaction reaction) : base(message)
        {
            m_reaction = reaction;
        }
        #endregion
    }

}
