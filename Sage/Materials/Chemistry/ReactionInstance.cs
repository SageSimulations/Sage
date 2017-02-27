/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Materials.Chemistry {
    /// <summary>
    /// A ReactionInstance is created every time a reaction takes place. It records
    /// what reaction took place, how much of the reaction took place (i.e. percent
    /// completion) in both the forward and reverse directions, and the percent of
    /// completion the reaction had been told to attempt to accomplish, in case the
    /// value were to be changed in the reaction, later (such as as a result of a
    /// change in temperature.)
    /// </summary>
    public class ReactionInstance {

        private readonly Reaction m_reaction;
        private readonly double m_fwdScale;
        private readonly double m_revScale;

        public ReactionInstance(Reaction reaction, double fwdScale, double revScale, Guid rxnInstanceGuid) {
			m_reaction = reaction;
			m_fwdScale = fwdScale;
			m_revScale = revScale;
			Guid     = rxnInstanceGuid;
		}

//		public ReactionInstance(Reaction reaction, double fwdScale, double revScale)
//			:this(reaction,fwdScale,revScale,Guid.NewGuid()){}

		public Reaction Reaction => m_reaction;

        private Reaction m_isReaction;
		public Reaction InstanceSpecificReaction {
			get { 
				if ( m_isReaction == null ) {
					double scale = (m_revScale-m_fwdScale);
					m_isReaction = new Reaction(m_reaction.Model,"Instance Specific Reaction",Guid.NewGuid());
					foreach ( Reaction.ReactionParticipant rp in m_reaction.Reactants ) {
						m_isReaction.AddReactant(rp.MaterialType,rp.Mass*(scale));
					}
					foreach ( Reaction.ReactionParticipant rp in m_reaction.Products ) {
						m_isReaction.AddProduct(rp.MaterialType,rp.Mass*(-scale));
					}
				}
				return m_isReaction;
			}
		}

        public double ForwardScale => m_fwdScale;

        public double ReverseScale => m_revScale;

        public double PercentCompletion => m_reaction.PercentCompletion;

        public override string ToString(){
            return "Reaction " + m_reaction.Name + " occurred with a forward scale of " + m_fwdScale + " and a reverse scale of " + m_revScale + ". Reaction ran to " + (m_reaction.PercentCompletion*100d) + "% completion.";
        }

        public string InstanceSpecificReactionString(){
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for ( int i = 0 ; i < m_reaction.Reactants.Count ; i++ ) {
                sb.Append(((Reaction.ReactionParticipant)m_reaction.Reactants[i]).ToString(m_fwdScale-m_revScale));
                if ( i < m_reaction.Reactants.Count-1 ) sb.Append(" + ");
            }
            sb.Append(" <==> ");
            for ( int i = 0 ; i < m_reaction.Products.Count ; i++ ) {
                sb.Append(((Reaction.ReactionParticipant)m_reaction.Products[i]).ToString(m_fwdScale-m_revScale));
                if ( i < m_reaction.Products.Count-1 ) sb.Append(" + ");
            }
            return sb.ToString();
        }

        public Guid Guid { get; }
    }
}
