/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using System.Linq;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Persistence;


namespace Highpoint.Sage.Materials.Chemistry {

    /// <summary>
    /// Interface IHasMaterials is implemented by any object that serves as a librarian for material types, which are held in a MaterialCatalog.
    /// </summary>
    public interface IHasMaterials {
        MaterialCatalog   MyMaterialCatalog   { get; }
        /// <summary>
        /// Registers the material catalog.
        /// </summary>
        /// <param name="mcat">The mcat.</param>
        void RegisterMaterialCatalog(MaterialCatalog mcat);
	}

	/// <summary>
	/// ISupportsReactions is an entity that keeps track of reactions and materials, and provides each
	/// a place to acquire references to the other. If you define a reaction with Potassium in it, and
	/// use the Potassium material type to create some potassium, you can be sure the potassium will be
	/// able to react if the material is made from the MaterialCatalog, and the reaction is stored in
	/// the ReactionProcessor of, the same instance of ISupportsReactions.
	/// </summary>
    public interface ISupportsReactions : IHasMaterials {
        ReactionProcessor MyReactionProcessor { get; }
        void RegisterReactionProcessor(ReactionProcessor rp);
    }

	/// <summary>
	/// A BasicReactionSupporter is used for testing. It is the simplest class that can implement the
	/// ISupportsReactions interface (it also implements IXmlPersistable...)
	/// </summary>
    public class BasicReactionSupporter : ISupportsReactions, IXmlPersistable {
        private ReactionProcessor m_reactionProcessor;
        private MaterialCatalog   m_materialCatalog;
        /// <summary>
		/// The simplest class that implements the ISupportsReactions interface (it also implements IXmlPersistable...)
        /// </summary>
		public BasicReactionSupporter()
        {
            RegisterReactionProcessor(new ReactionProcessor());
            RegisterMaterialCatalog(new MaterialCatalog());

        }
        public ReactionProcessor MyReactionProcessor => m_reactionProcessor;
	    public MaterialCatalog   MyMaterialCatalog => m_materialCatalog;

	    public void RegisterReactionProcessor(ReactionProcessor rp){
            if ( m_reactionProcessor == null ) m_reactionProcessor = rp;
            else throw new ApplicationException("Attempt to register a new reaction processor into a BasicReactionSupporter that already has one. This is prohibited.");
        }
        public void RegisterMaterialCatalog(MaterialCatalog materialCatalog)
        {
            if ( m_materialCatalog == null ) m_materialCatalog = materialCatalog;
            else throw new ApplicationException("Attempt to register a new material catalog into a BasicReactionSupporter that already has one. This is prohibited.");
		}
		#region IXmlPersistable Members

		/// <summary>
		/// Serializes this object to the specified XmlSerializatonContext.
		/// </summary>
		/// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
        public void SerializeTo(XmlSerializationContext xmlsc) {
            if (xmlsc != null) {
                xmlsc.StoreObject("MaterialCatalog", m_materialCatalog);
                xmlsc.StoreObject("ReactionProcessor", m_reactionProcessor);
            } else {
                throw new ApplicationException("SerializeTo(...) called with a null XmlSerializationContext.");
            }
        }

		/// <summary>
		/// Deserializes this object from the specified XmlSerializatonContext.
		/// </summary>
		/// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
		public void DeserializeFrom(XmlSerializationContext xmlsc) {
			m_materialCatalog = (MaterialCatalog)xmlsc.LoadObject("MaterialCatalog");
			m_reactionProcessor = (ReactionProcessor)xmlsc.LoadObject("ReactionProcessor");
		}

		#endregion
	}


	/// <summary>
	/// Delegate implemented by a method that wants to be called when a reaction is added to
	/// or removed from, a ReactionProcessor.
	/// </summary>
    public delegate void ReactionProcessorEvent(ReactionProcessor rxnProcessor, Reaction reaction);
    
	/// <summary>
	/// A reaction processor knows of a set of chemical reactions, and watches a set of mixtures.
	/// Whenever a material is added to, or removed from, a mixture, the reaction processor examines
	/// that mixture to see if any of the reactions it knows of are capable of occurring. If any are,
	/// then it proceeds to execute that reaction, eliminating the appropriate quantity of reactants,
	/// generating the appropriate quantity of products (or vice versa) and changing the mixture's
	/// thermal characteristics.
	/// </summary>
	public class ReactionProcessor : IHasIdentity, IXmlPersistable {

        public event ReactionProcessorEvent ReactionAddedEvent;
        public event ReactionProcessorEvent ReactionRemovedEvent;

        private readonly ArrayList m_reactions = new ArrayList();
        private readonly bool m_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("ReactionProcessor");

		public ReactionProcessor() {}

        public void AddReaction(Reaction reaction){
            if ( !reaction.IsValid ) throw new ReactionDefinitionException(reaction);
            if ( !m_reactions.Contains(reaction) ) {
                m_reactions.Add(reaction);
                ReactionAddedEvent?.Invoke(this,reaction);
            }
        }

        public void RemoveReaction(Reaction reaction){
            if ( m_reactions.Contains(reaction) ) {
                m_reactions.Remove(reaction);
                ReactionRemovedEvent?.Invoke(this,reaction);
            }
        }

        public ArrayList Reactions => ArrayList.ReadOnly(m_reactions);

	    public Reaction GetReaction(Guid rxnGuid)
        {
            return m_reactions.Cast<Reaction>().FirstOrDefault(rxn => rxn.Guid.Equals(rxnGuid));
        }

	    public bool CombineMaterials(IMaterial[] materialsToCombine){
            IMaterial result;
            ArrayList observedReactions;
            ArrayList observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine, out IMaterial result){
            ArrayList observedReactions;
            ArrayList observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine, out ArrayList observedReactions){
            IMaterial result;
            ArrayList observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine, out IMaterial result, out ArrayList observedReactions, out ArrayList observedReactionInstances){
            Mixture scratch = new Mixture(null,"scratch mixture");
            Watch(scratch);
            ReactionCollector rc = new ReactionCollector(scratch);
            foreach ( IMaterial material in materialsToCombine ) {
                scratch.AddMaterial(material);
            }
            observedReactions = rc.Reactions;
            observedReactionInstances = rc.ReactionInstances;
            rc.Disconnect();
            result = scratch;
            return ( observedReactions != null && observedReactions.Count > 0);
        }

        public void Watch(IMaterial material){
            material.MaterialChanged += OnMaterialChanged;
        }

		public void Ignore(IMaterial material){
            material.MaterialChanged -= OnMaterialChanged;
		}

        public IList GetReactionsByParticipant(MaterialType targetMt){
            return GetReactionsByFilter(targetMt,Reaction.MaterialRole.Either);
        }
        public IList GetReactionsByReactant(MaterialType targetMt){
            return GetReactionsByFilter(targetMt,Reaction.MaterialRole.Reactant);
        }
        public IList GetReactionsByProduct(MaterialType targetMt){
            return GetReactionsByFilter(targetMt,Reaction.MaterialRole.Product);
        }

        private IList GetReactionsByFilter( MaterialType targetMt, Reaction.MaterialRole filter){
            ArrayList reactions = new ArrayList();
            foreach ( Reaction reaction in Reactions ) {
                if ( filter == Reaction.MaterialRole.Either || filter == Reaction.MaterialRole.Reactant ) {
                    foreach ( Reaction.ReactionParticipant rp in reaction.Reactants ) {
                        if ( rp.MaterialType.Equals(targetMt) ) reactions.Add(reaction);
                    }
                }
                if ( filter == Reaction.MaterialRole.Either || filter == Reaction.MaterialRole.Product ) {
                    foreach ( Reaction.ReactionParticipant rp in reaction.Products ) {
                        if ( rp.MaterialType.Equals(targetMt) ) reactions.Add(reaction);
                    }
                }
            }
            return reactions;
        }


        public void OnMaterialChanged(IMaterial material, MaterialChangeType mct){
			if ( m_diagnostics ) _Debug.WriteLine("ReactionProcessor notified of change type " + mct + " to material " + material);
            if ( mct == MaterialChangeType.Contents )
            {
                Mixture tmpMixture = material as Mixture;
                if ( tmpMixture != null ) {
                    Mixture mixture = tmpMixture;
                    ReactionInstance ri = null;
                    if ( m_diagnostics ) _Debug.WriteLine("Processing change type " + mct + " to mixture " + mixture.Name);

                    // If multiple reactions could occur? Only the first happens, but then the next change allows the next reaction, etc.
                    foreach ( Reaction reaction in m_reactions ) {
                        if ( ri != null ) continue;
                        if ( m_diagnostics ) _Debug.WriteLine("Examining mixture for presence of reaction " + reaction.Name);
                        ri = reaction.React(mixture);
                    }
                }
            }
        }

	    public object Tag { get; set; }

	    #region >>> Implementation of IHasIdentity <<<
        private string m_name = "Reaction Processor";
		/// <summary>
		/// The name of this reaction processor.
		/// </summary>
        public string Name => m_name;

	    private readonly string m_description = null;
		/// <summary>
		/// A description of this Reaction Processor.
		/// </summary>
		public string Description => m_description ?? m_name;

	    /// <summary>
		/// The Guid by which this reaction processor will be known.
		/// </summary>
        public Guid Guid { get; } = Guid.Empty;

        #endregion


        private class ReactionCollector {
            readonly ArrayList m_reactions;
            readonly ArrayList m_reactionInstances;
            readonly Mixture m_mixture;
            readonly ReactionHappenedEvent m_reactionHandler;
            public ReactionCollector(Mixture mixture){
                m_mixture = mixture;
                m_reactions = new ArrayList();
                m_reactionInstances = new ArrayList();
                m_reactionHandler = OnReactionHappened;
                m_mixture.OnReactionHappened+=m_reactionHandler;
            }
            public void Disconnect(){
                m_mixture.OnReactionHappened-=m_reactionHandler;
            }

            private void OnReactionHappened(ReactionInstance ri){
                m_reactions.Add(ri.Reaction);
                m_reactionInstances.Add(ri);
            }
            public ArrayList Reactions => m_reactions;
            public ArrayList ReactionInstances => m_reactionInstances;
        }

		#region IXmlPersistable Members

		/// <summary>
		/// Serializes this object to the specified XmlSerializatonContext.
		/// </summary>
		/// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
		public void SerializeTo(XmlSerializationContext xmlsc) {
			xmlsc.StoreObject("Reactions",m_reactions);
		}

		/// <summary>
		/// Deserializes this object from the specified XmlSerializatonContext.
		/// </summary>
		/// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
		public void DeserializeFrom(XmlSerializationContext xmlsc) {

		}

		#endregion
	}
}
