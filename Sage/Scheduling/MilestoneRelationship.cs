/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using Trace = System.Diagnostics.Debug;

namespace Highpoint.Sage.Scheduling {

    /// <summary>
    /// Describes the relationship between two milestones, a dependent and an independent.
    /// </summary>
    public enum RelationshipType { 
        /// <summary>
        /// 
        /// </summary>
        LTE, 
        EQ, 
        GTE
        //    , 
        //LTE_O, 
        //EQ_O, 
        //GTE_O 
    };
    
    /// <summary>
    /// This is an abstract class from which all MilestoneRelationships are derived.<b></b>
    /// A MilestoneRelationship represents a relationship between a dependent milestone
    /// such as "Oven Heatup Finishes" and an independent one such as "Bake Cookies."
    /// In this case, the relationship would be a MilestoneRelationship_GTE(heatupDone,startBaking);<b></b>
    /// meaning that if the heatupDone milestone is changed, then the startBaking milestone will
    /// also be adjusted, if the change resulted in startBaking occurring before heatupDone.
    /// </summary>
    public abstract class MilestoneRelationship {

#region Private Fields
        private Stack m_enabled;
        /// <summary>
        /// The dependent milestone affected by this milestone.
        /// </summary>
        protected IMilestone m_dependent;
        /// <summary>
        /// The independent milestone monitored by this milestone.
        /// </summary>
        protected IMilestone m_independent;
        /// <summary>
        /// A list of the reciprocal relationships to this relationship.
        /// </summary>
        protected ArrayList m_reciprocals = s_empty_List;
        private static readonly ArrayList s_empty_List = ArrayList.ReadOnly(new ArrayList());
#endregion

        /// <summary>
        /// Imposes changes on the dependent milestone, if the independent one changes.
        /// </summary>
        /// <param name="independent">The one that might be changed to kick off this rule.</param>
        /// <param name="dependent">The one upon which a resulting change is imposed by this rule.</param>
        public MilestoneRelationship(IMilestone dependent, IMilestone independent) {
            m_independent = independent;
            m_dependent = dependent;
            m_enabled = new Stack();
            m_enabled.Push(true);
            if (m_independent != null)
                m_independent.AddRelationship(this);
            if (m_dependent != null)
                m_dependent.AddRelationship(this);
        }

        /// <summary>
        /// Detaches this relationship from the two milestones.
        /// </summary>
        public void Detach() {
            if (m_independent != null)
                m_independent.RemoveRelationship(this);
            if (m_dependent != null)
                m_dependent.RemoveRelationship(this);
        }

        /// <summary>
        /// Gets the dependent milestone.
        /// </summary>
        /// <value>The dependent milestone.</value>
        public IMilestone Dependent { get { return m_dependent; } }

        /// <summary>
        /// Gets the independent milestone.
        /// </summary>
        /// <value>The independent milestone.</value>
        public IMilestone Independent { get { return m_independent; } }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MilestoneRelationship"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get { return (bool)m_enabled.Peek(); } set { PopEnabled(); PushEnabled(value); } }

        /// <summary>
        /// Models a reaction to a movement of the independent milestone, and provides minimum and maximum acceptable
        /// DateTime values for the dependent milestone.
        /// </summary>
        /// <param name="independentNewValue">The independent new value.</param>
        /// <param name="minDateTime">The minimum acceptable DateTime value for the dependent milestone.</param>
        /// <param name="maxDateTime">The maximum acceptable DateTime value for the dependent milestone.</param>
        public abstract void Reaction(DateTime independentNewValue, out DateTime minDateTime, out DateTime maxDateTime);

        /// <summary>
        /// Pushes the enabled state of this relationship.
        /// </summary>
        /// <param name="newValue">if set to <c>true</c> [new value].</param>
        public void PushEnabled(bool newValue) { m_enabled.Push(newValue); }
        
        /// <summary>
        /// Pops the enabled state of this relationship.
        /// </summary>
        public void PopEnabled() { m_enabled.Pop(); }

#region Reciprocal Management
        /// <summary>
        /// Gets a relationship that is the reciprocal, if applicable, of this one. If there is no reciprocal,
        /// then this returns null.
        /// </summary>
        /// <value>The reciprocal.</value>
        public abstract MilestoneRelationship Reciprocal { get; }

        /// <summary>
        /// A reciprocal is a secondary relationship that should not fire if this one fired.
        /// A good example is a strut where one strut pins A to 5 mins after B, and another pins B to
        /// 5 mins before A.
        /// </summary>
        /// <param name="reciprocal">The reciprocal relationship.</param>
        public void AddReciprocal(MilestoneRelationship reciprocal) {
            if (m_reciprocals == s_empty_List) {
                m_reciprocals = new ArrayList();
            }
            m_reciprocals.Add(reciprocal);
        }

        /// <summary>
        /// Removes the specified reciprocal relationship from this relationship.
        /// </summary>
        /// <param name="reciprocal">The reciprocal.</param>
        public void RemoveReciprocal(MilestoneRelationship reciprocal) {
            if (m_reciprocals.Contains(reciprocal))
                m_reciprocals.Remove(reciprocal);
        }

        /// <summary>
        /// Clears the reciprocal relationships.
        /// </summary>
        public void ClearReciprocal() {
            if (m_reciprocals != s_empty_List)
                m_reciprocals.Clear();
        }

        /// <summary>
        /// Gets the reciprocal relationships.
        /// </summary>
        /// <value>The reciprocals.</value>
        public IList Reciprocals { get { return ArrayList.ReadOnly(m_reciprocals); } }
#endregion

#region Correctness Checking
        /// <summary>
        /// Assesses the initial satisfaction of this relationship for ctor.
        /// </summary>
        protected void AssessInitialCorrectnessForCtor() {
            if (!IsSatisfied()) {
                Detach();
                string msg = "Relationship " + ToString() + ", applied to "
                  + m_dependent.Name + "(" + m_dependent.DateTime + "), and "
                  + m_independent.Name + "(" + m_independent.DateTime + ") is not initially satisfied.";
                throw new ApplicationException(msg);
            }
        }

        /// <summary>
        /// Determines whether this relationship is currently satisfied.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is satisfied; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsSatisfied();
#endregion

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj) {
            MilestoneRelationship mr = obj as MilestoneRelationship;
            if (mr == null) {
                return false;
            } else if ( GetType()!=mr.GetType() ) {
                return false;
            } else if ( Object.Equals(Dependent,mr.Dependent) && Object.Equals(Independent,mr.Independent) ) {
                return true;
            } else {
                return base.Equals(obj);
            }
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode() {
            return ( Dependent == null ? 0 : Dependent.GetHashCode() ) ^ ( Independent == null ? 0 : Independent.GetHashCode() );
        }
    }

    /// <summary>
    /// Ensures that the dependent is always at the same offset to the independent as when it was initially established.
    /// </summary>
    public class MilestoneRelationship_Strut : MilestoneRelationship {
        private TimeSpan m_delta;
        public MilestoneRelationship_Strut(IMilestone dependent, IMilestone independent)
            : base(dependent, independent) {
            m_delta = dependent.DateTime - independent.DateTime;
            AssessInitialCorrectnessForCtor();
        }

        public TimeSpan Delta {
            get { return m_delta; }
            set {
                m_delta = value;
                m_dependent.MoveTo(m_independent.DateTime + m_delta);
            }
        }

        /// <summary>
        /// Gets a relationship that is the reciprocal, if applicable, of this one. If there is no reciprocal,
        /// then this returns null.
        /// </summary>
        /// <value>The reciprocal.</value>
        public override MilestoneRelationship Reciprocal {
            get {
                return new MilestoneRelationship_Strut(Independent, Dependent);
            }
        }

        /// <summary>
        /// Determines whether this relationship is currently satisfied.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is satisfied; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsSatisfied() {
            return ( !Enabled || m_delta == ( m_dependent.DateTime - m_independent.DateTime ) );
        }

        /// <summary>
        /// Models a reaction to a movement of the independent milestone, and provides minimum and maximum acceptable
        /// DateTime values for the dependent milestone.
        /// </summary>
        /// <param name="independentNewValue">The independent new value.</param>
        /// <param name="minDateTime">The minimum acceptable DateTime value for the dependent milestone.</param>
        /// <param name="maxDateTime">The maximum acceptable DateTime value for the dependent milestone.</param>
        public override void Reaction(DateTime independentNewValue, out DateTime minDateTime, out DateTime maxDateTime) {
            minDateTime = independentNewValue + m_delta;
            maxDateTime = independentNewValue + m_delta;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() {
            string howMuch = TimeSpan.FromMinutes(Math.Abs(m_delta.TotalMinutes)).ToString();
            string relation = m_delta > TimeSpan.Zero ? ( howMuch + " after " ) : ( howMuch + " before " );
            if (m_delta.Equals(TimeSpan.Zero))
                relation = " when ";
            return Dependent.Name + " occurs " + relation + Independent.Name + " occurs.";
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj) {
            return base.Equals(obj) && m_delta==((MilestoneRelationship_Strut)obj).m_delta;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

    }

    /// <summary>
    /// Ensures that the dependent is always at a less-than-or-equal time to the independent.
    /// </summary>
    public class MilestoneRelationship_LTE : MilestoneRelationship {
        TimeSpan m_delta;
        public MilestoneRelationship_LTE(IMilestone dependent, IMilestone independent)
            : base(dependent, independent) {
            m_delta = independent.DateTime - dependent.DateTime;
            AssessInitialCorrectnessForCtor();
        }

        /// <summary>
        /// Models a reaction to a movement of the independent milestone, and provides minimum and maximum acceptable
        /// DateTime values for the dependent milestone.
        /// </summary>
        /// <param name="independentNewValue">The independent new value.</param>
        /// <param name="minDateTime">The minimum acceptable DateTime value for the dependent milestone.</param>
        /// <param name="maxDateTime">The maximum acceptable DateTime value for the dependent milestone.</param>
        public override void Reaction(DateTime independentNewValue, out DateTime minDateTime, out DateTime maxDateTime) {
            minDateTime = DateTime.MinValue;
            maxDateTime = independentNewValue;
        }

        /// <summary>
        /// Determines whether this relationship is currently satisfied.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is satisfied; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsSatisfied() {
            return ( !Enabled || m_dependent.DateTime <= m_independent.DateTime );
        }

        /// <summary>
        /// Gets a relationship that is the reciprocal, if applicable, of this one. If there is no reciprocal,
        /// then this returns null.
        /// </summary>
        /// <value>The reciprocal.</value>
        public override MilestoneRelationship Reciprocal {
            get {
                return new MilestoneRelationship_GTE(Independent, Dependent);
            }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() {
            return Dependent.Name + " occurs before or when " + Independent.Name + " occurs.";
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj) {
            return base.Equals(obj) && m_delta == ( (MilestoneRelationship_LTE)obj ).m_delta;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

    }

    /// <summary>
    /// Ensures that the dependent is always at a greater-than-or-equal time to the independent.
    /// </summary>
    public class MilestoneRelationship_GTE : MilestoneRelationship {
        TimeSpan m_delta;
        public MilestoneRelationship_GTE(IMilestone dependent, IMilestone independent)
            : base(dependent, independent) {
            m_delta = independent.DateTime - dependent.DateTime;
            AssessInitialCorrectnessForCtor();
        }

        /// <summary>
        /// Models a reaction to a movement of the independent milestone, and provides minimum and maximum acceptable
        /// DateTime values for the dependent milestone.
        /// </summary>
        /// <param name="independentNewValue">The independent new value.</param>
        /// <param name="minDateTime">The minimum acceptable DateTime value for the dependent milestone.</param>
        /// <param name="maxDateTime">The maximum acceptable DateTime value for the dependent milestone.</param>
        public override void Reaction(DateTime independentNewValue, out DateTime minDateTime, out DateTime maxDateTime) {
            minDateTime = independentNewValue;
            maxDateTime = DateTime.MaxValue;
        }

        /// <summary>
        /// Determines whether this relationship is currently satisfied.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is satisfied; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsSatisfied() {
            return ( !Enabled || m_dependent.DateTime >= m_independent.DateTime );
        }

        /// <summary>
        /// Gets a relationship that is the reciprocal, if applicable, of this one. If there is no reciprocal,
        /// then this returns null.
        /// </summary>
        /// <value>The reciprocal.</value>
        public override MilestoneRelationship Reciprocal {
            get {
                return new MilestoneRelationship_LTE(Independent, Dependent);
            }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() {
            return Dependent.Name + " occurs when or after " + Independent.Name + " occurs.";
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj) {
            return base.Equals(obj) && m_delta == ( (MilestoneRelationship_GTE)obj ).m_delta;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Ensures that the independent milestone is not permitted to move.
    /// </summary>
    public class MilestoneRelationship_Pin : MilestoneRelationship {
        private DateTime m_independentDateTime;
        public MilestoneRelationship_Pin(IMilestone dependent, IMilestone independent)
            : base(dependent, independent) {
            if (m_dependent != null)
                throw new ApplicationException("The MilestoneRelationship_Pin relationship uses only the independent milestone, and you have specified a dependent one. The dependent milestone should be null.");
            m_independentDateTime = m_independent.DateTime;
            AssessInitialCorrectnessForCtor();
        }

        /// <summary>
        /// Models a reaction to a movement of the independent milestone, and provides minimum and maximum acceptable
        /// DateTime values for the dependent milestone.
        /// </summary>
        /// <param name="independentNewValue">The independent new value.</param>
        /// <param name="minDateTime">The minimum acceptable DateTime value for the dependent milestone.</param>
        /// <param name="maxDateTime">The maximum acceptable DateTime value for the dependent milestone.</param>
        public override void Reaction(DateTime independentNewValue, out DateTime minDateTime, out DateTime maxDateTime) {
            throw new ApplicationException("Cannot move " + m_independent + " - it is frozen.");
        }

        /// <summary>
        /// Gets a relationship that is the reciprocal, if applicable, of this one. If there is no reciprocal,
        /// then this returns null.
        /// </summary>
        /// <value>The reciprocal.</value>
        public override MilestoneRelationship Reciprocal {
            get {
                return null;
            }
        }

        /// <summary>
        /// Determines whether this relationship is currently satisfied.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is satisfied; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsSatisfied() {
            return ( !Enabled || m_independentDateTime == m_independent.DateTime );
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() {
            return Dependent.Name + " is frozen at " + Dependent.DateTime + ".";
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj) {
            return base.Equals(obj) && m_independentDateTime == ( (MilestoneRelationship_Pin)obj ).m_independentDateTime;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}


//Trace.WriteLine("Independent : " + m_independent.Name + " @ " + m_independent.DateTime.ToString());
//Trace.WriteLine("Dependent   : " + m_dependent.Name   + " @ " +   m_dependent.DateTime.ToString());
//Trace.WriteLine("Delta       : " + m_delta.ToString());
//Trace.WriteLine(this.ToString());
