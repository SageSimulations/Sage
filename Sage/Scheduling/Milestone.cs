/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.Scheduling {

	public interface IMilestone : IObservable, IHasIdentity {
        /// <summary>
        /// Adds one to the set of relationships that this milestone has with other milestones.
        /// </summary>
        /// <param name="relationship">The new relationship that involves this milestone.</param>
		void AddRelationship(MilestoneRelationship relationship);

        /// <summary>
        /// Removes one from the set of relationships that this milestone has with other milestones..
        /// </summary>
        /// <param name="relationship">The relationship to be removed.</param>
		void RemoveRelationship(MilestoneRelationship relationship);

        /// <summary>
        /// Moves the time of this milestone to the specified new DateTime.
        /// </summary>
        /// <param name="newDateTime">The new date time.</param>
		void MoveTo(DateTime newDateTime);

        /// <summary>
        /// Moves the time of this milestone by the amount of time specified.
        /// </summary>
        /// <param name="delta">The delta.</param>
		void MoveBy(TimeSpan delta);

        /// <summary>
        /// Gets the date &amp; time of this milestone.
        /// </summary>
        /// <value>The date time.</value>
		DateTime DateTime { get; }

        /// <summary>
        /// Gets the relationships that involve this milestone.
        /// </summary>
        /// <value>The relationships.</value>
		List<MilestoneRelationship> Relationships { get; }

		bool Active { get; set; }
		void PushActiveSetting(bool newSetting);
		void PopActiveSetting();


	}
	/// <summary>
	/// Summary description for Milestone.
	/// </summary>
	public class Milestone : IMilestone, IObservable {

		public enum ChangeType { Set, Enabled }

#region Private Fields
		private DateTime m_dateTime;
		private string m_name = null;
		private Guid m_guid = Guid.Empty;
		private string m_description = null;
		private List<MilestoneRelationship> m_relationships;
		private Stack m_activeStack;
        private bool m_isActive;
		private MilestoneMovementManager m_movementManager = null;
#endregion

#region Constructors
        /// <summary>
        /// Creates and initializes a new simple instance of the <see cref="Milestone"/> class set to a specific date &amp; time.
        /// </summary>
        /// <param name="dateTime">The date &amp; time.</param>
		public Milestone(DateTime dateTime):this("Milestone",Guid.NewGuid(),dateTime){}

        /// <summary>
        /// Creates and initializes a new simple instance of the <see cref="Milestone"/> class set to a specific date &amp; time, and
        /// with a specified name and guid.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="dateTime">The date time.</param>
		public Milestone(string name, Guid guid, DateTime dateTime):this(name,guid,dateTime,true){}

        /// <summary>
        /// Creates and initializes a new simple instance of the <see cref="Milestone"/> class set to a specific date &amp; time, and
        /// with a specified name and guid. This constructor also allows the newly created milestone to be initially active.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="active">if set to <c>true</c> this milestone is initially active.</param>
        public Milestone(string name, Guid guid, DateTime dateTime, bool active) {
			m_name = name;
			m_guid = guid;
			m_dateTime = dateTime;
            m_activeStack = null;
            m_relationships = null;
            m_isActive = active;
		}
#endregion

#region Relationship Management
        /// <summary>
        /// Gets the relationships that involve this milestone.
        /// </summary>
        /// <value>The relationships.</value>
		public List<MilestoneRelationship> Relationships { 
            get {
                if (m_relationships == null) {
                    m_relationships = new List<MilestoneRelationship>();
                }
                return m_relationships;
            }
        }

        public void AddRelationship(MilestoneRelationship relationship) {
#region Error Checking
            if (m_movementManager != null) {
                throw new ApplicationException("Cannot add or remove relationships while a network change is being processed.");
            }
            if (relationship.Dependent != this && relationship.Independent != this) {
                throw new ApplicationException("Cannot add a relationship to this milestone that does not involve it.");
            }
            if (Relationships.Contains(relationship)) {
                return;
            }
#endregion
            Relationships.Add(relationship);
        }

        public void RemoveRelationship(MilestoneRelationship relationship) {
#region Error Checking
            if (m_movementManager != null) {
                throw new ApplicationException("Cannot add or remove relationships while a network change is being processed.");
            }
#endregion
            Relationships.Remove(relationship);
        }
#endregion

#region Movement
        /// <summary>
        /// Moves the time of this milestone to the specified new DateTime.
        /// </summary>
        /// <param name="newDateTime">The new date time.</param>
		public void MoveTo(DateTime newDateTime){
			Active = true;
			MilestoneMovementManager.Adjust(this,newDateTime);
		}

        /// <summary>
        /// Moves the time of this milestone by the amount of time specified.
        /// </summary>
        /// <param name="delta">The delta.</param>
		public void MoveBy(TimeSpan delta){
			MilestoneMovementManager.Adjust(this,m_dateTime+delta);
		}
#endregion

#region Active state management

        public Stack ActiveStack {
            get {
                if (m_activeStack == null) {
                    m_activeStack = new Stack();
                    m_activeStack.Push(m_isActive);
                }
                return m_activeStack;
            }
        }

        public bool Active { 
			get { 
				return (bool)ActiveStack.Peek(); 
			} 

			set { 
				bool was = (bool)ActiveStack.Pop();
                ActiveStack.Push(value);
				if ( was != value ) NotifyEnabledChanged();
			}
		}

		
		public void PushActiveSetting(bool newSetting) {
            bool was = (bool)ActiveStack.Peek();
            ActiveStack.Push(newSetting);
			if ( was != newSetting ) NotifyEnabledChanged();
		}
		
		public void PopActiveSetting(){
            bool was = (bool)ActiveStack.Pop();
            bool newSetting = (bool)ActiveStack.Peek();
			if ( was != newSetting ) NotifyEnabledChanged();
			
		}
#endregion

        /// <summary>
        /// Gets the date &amp; time of this milestone.
        /// </summary>
        /// <value>The date time.</value>
        public DateTime DateTime {
            [DebuggerStepThrough]
            get { return m_dateTime; }
        }

#region IObservable Members and support methods.

		private void NotifyValueChanged(DateTime oldValue){
            ChangeEvent?.Invoke(this, ChangeType.Set, oldValue);
        }

		private void NotifyEnabledChanged(){
            ChangeEvent?.Invoke(this, ChangeType.Enabled, m_activeStack.Peek());
        }

		public event ObservableChangeHandler ChangeEvent;

#endregion

#region IHasIdentity Members
		public string Name { get { return m_name; } }
		public Guid Guid => m_guid;
		public string Description { get { return m_description; } }
#endregion
	
		public override string ToString() {
			return (((m_name==null||m_name.Length==0)?("Milestone : "):(m_name+" : ")) + m_dateTime);
		}

        public class MilestoneMovementManager {
            private static object _lock = new object();
            public static void Adjust(Milestone prospectiveMover, DateTime newValue) {
                if (!prospectiveMover.m_dateTime.Equals(newValue)) {
                    DateTime oldValue = prospectiveMover.m_dateTime;
                    prospectiveMover.m_dateTime = newValue;
                    prospectiveMover.NotifyValueChanged(oldValue);
                }
            }

            private static DateTime GetClosestDateTime(DateTime minDateTime, DateTime maxDateTime, DateTime proposedDateTime) {
                Debug.Assert(minDateTime <= maxDateTime, "GetClosestDateTime was passed a mnDateTime that was greater than the maxDateTime it was passed.");
                if (proposedDateTime < minDateTime) {
                    return minDateTime;
                }
                if (proposedDateTime > maxDateTime) {
                    return maxDateTime;
                }
                return proposedDateTime;
            }
			
        }

        /// <summary>
        /// The MilestoneMovementManager class is responsible for moving a requested milestone, and performing all inferred resultant movements.
        /// </summary>
		private class _MilestoneMovementManager {

			private static bool _debug = true;
			
#region Private Fields
			private static readonly object s_lock = new object();
			private static Hashtable _oldValues = new Hashtable();
			private static Stack _pushedDisablings = new Stack();
			private static Queue _changedMilestones = new Queue();
#endregion
			
			public static void Adjust(Milestone prospectiveMover, DateTime newValue){
				if (prospectiveMover.m_dateTime.Equals(newValue)) return;
				lock ( s_lock ) {
					if ( _debug ) Trace.WriteLine("Attempting to coordinate correct movement of " + prospectiveMover.Name + " from " + prospectiveMover.DateTime + " to " + newValue + ".");

					// Change, and then enqueue the first milestone.
					_oldValues.Add(prospectiveMover,prospectiveMover.DateTime);
					_changedMilestones.Enqueue(prospectiveMover);

					while ( prospectiveMover.m_dateTime != newValue ) { // TODO: This is a SLEDGEHAMMER. Figure out why root changes don't always hold.
						prospectiveMover.m_dateTime = newValue;
						// Propagate the change downstream (including any resultant changes.)
						Propagate();
					}

					// Finally, tell each changed milestone to fire it's change event.
					foreach ( Milestone changed in _oldValues.Keys ) changed.NotifyValueChanged((DateTime)_oldValues[changed]);

					// And reset the data structures for the next use.
					_oldValues.Clear();
					while ( _pushedDisablings.Count > 0 ) ((MilestoneRelationship)(_pushedDisablings.Pop())).PopEnabled();
					_changedMilestones.Clear();
				}
			}

			private static void Propagate(){
				while ( _changedMilestones.Count > 0 ) {
					Milestone ms = (Milestone)_changedMilestones.Dequeue();
					if ( _debug ) Trace.WriteLine("\tPerforming propagation of change to " + ms.Name);
					
#region Create a Hashtable of Lists - key is target Milestone, list contains relationships to that ms.
					Hashtable htol = new Hashtable();
					foreach ( MilestoneRelationship mr in ms.Relationships ) {
						if ( !mr.Enabled ) continue;              // Only enabled relationships can effect change.
						if ( mr.Dependent.Equals(ms) ) continue;  // Only relationships where we are the independent can effect change.
						//if ( m_debug ) Trace.WriteLine("\tConsidering " + mr.ToString());
						if ( !htol.Contains(mr.Dependent) ) htol.Add(mr.Dependent,new ArrayList());
						((ArrayList)htol[mr.Dependent]).Add(mr);  // We now have outbounds, grouped by destination milestone.
					}
#endregion

					//if ( m_debug ) Trace.WriteLine("\tPerforming change assessments for relationships that depend on " + ms.Name);
					
					// For each 'other' milestone with which this milestone has a relationship, we will
					// handle all of the relationships that this ms has with that one, as a group.
					bool fullData = false;
					foreach ( Milestone target in htol.Keys ) {
						if ( _debug ) {
							Trace.WriteLine("\t\tReconciling all relationships between " + ms.Name + " and " + target.Name);
							// E : RCV Liquid1.Xfer-In.Start and E : RCV Liquid1.Xfer-In.End

						}
						IList relationships = (ArrayList)htol[target];// Gives us a list of parallel relationships to the same downstream.

//						if ( ms.Name.Equals("B : RCV Liquid1.Xfer-In.Start") && target.Name.Equals("B : RCV Liquid1.Temp-Set.End") ) {
//							fullData = true;
//						}
						
						if ( fullData ) foreach ( MilestoneRelationship mr2 in relationships ) Trace.WriteLine(mr2.ToString());
						


						DateTime minDateTime = DateTime.MinValue;
						DateTime maxDateTime = DateTime.MaxValue;
						foreach ( MilestoneRelationship mr2 in relationships ) {
							/*foreach ( MilestoneRelationship recip in mr2.Reciprocals) {
								recip.PushEnabled(false);
								m_pushedDisablings.Push(recip);
							}*/
							if ( fullData ) if ( _debug ) Trace.WriteLine("\t\tAdjusting window to satisfy " + mr2);
							DateTime thisMinDt, thisMaxDt;
							mr2.Reaction(ms.DateTime,out thisMinDt,out thisMaxDt);       // Get the relationship's acceptable window.
							minDateTime = DateTimeOperations.Max(minDateTime,thisMinDt); // Narrow the range from below.
							maxDateTime = DateTimeOperations.Min(maxDateTime,thisMaxDt); // Narrow the range from above.
							if ( fullData ) if ( _debug ) Trace.WriteLine("\t\t\tThe window is now from " + minDateTime + " to " + maxDateTime + ".");
						}

						//if ( m_debug ) Trace.WriteLine("\t\tThe final window is from " + minDateTime + " to " + maxDateTime + ".");
						if ( minDateTime <= maxDateTime ) {
							DateTime newDateTime = GetClosestDateTime(minDateTime,maxDateTime,target.DateTime);
							if ( !target.DateTime.Equals(newDateTime) ) {
								if ( _debug ) Trace.WriteLine("\t\t\tWe will move " + target.Name + " from " + target.DateTime + " to " + newDateTime);
								if ( !_changedMilestones.Contains(target) ) _changedMilestones.Enqueue(target);
								if ( !_oldValues.Contains(target) ) _oldValues.Add(target,target.m_dateTime);
								if ( fullData ) if ( _debug ) Trace.WriteLine("\t\t\tThere are now " + _changedMilestones.Count + " milestones with changes to process.");
								target.m_dateTime = newDateTime;
							} else {
								if ( _debug ) Trace.WriteLine("\t\t\t" + target.Name + " stays put.");
							}
//						} else {
//							if ( m_debug ) Trace.WriteLine("\t\t\tThis is an unachievable window.");
//							throw new ApplicationException("Can't find a new datetime value for " + target.ToString());
						}

						fullData = false;
					}
				}
			}

			private static DateTime GetClosestDateTime(DateTime minDateTime,DateTime maxDateTime,DateTime proposedDateTime){
                Debug.Assert(minDateTime <= maxDateTime, "GetClosestDateTime was passed a mnDateTime that was greater than the maxDateTime it was passed.");
                if (proposedDateTime < minDateTime) {
                    return minDateTime;
                }
                if (proposedDateTime > maxDateTime) {
                    return maxDateTime;
                }
                return proposedDateTime;
            }
			
#region (Unused) Cyclic Dependency Checkers
#if UNUSED
			private static void CheckForCyclicDependencies(Milestone prospectiveMover){
				Console.WriteLine("******************************************************************************");
				Stack callStack = new Stack();
				_CheckForCyclicDependencies(prospectiveMover, ref callStack);
			}

			private static void _CheckForCyclicDependencies(IMilestone prospectiveMover, ref Stack callStack){
				if ( !callStack.Contains(prospectiveMover) ) {
					callStack.Push(prospectiveMover);
					foreach ( MilestoneRelationship mr in prospectiveMover.Relationships ) {
						if ( mr.Enabled && mr.Independent.Equals(prospectiveMover)) {
							foreach ( MilestoneRelationship recip in mr.Reciprocals ) recip.PushEnabled(false);
							callStack.Push(mr);
							_CheckForCyclicDependencies(mr.Dependent,ref callStack);
							foreach ( MilestoneRelationship recip in mr.Reciprocals ) recip.PopEnabled();
						}
					}
				
				} else {
					object obj;
					while ( callStack.Count > 0 ) {
						obj = callStack.Pop();
						if ( obj is IMilestone ) {
							IMilestone ms = (IMilestone)obj;
							Console.WriteLine("Milestone : " + ms);
						} else if ( obj is MilestoneRelationship ) {
							MilestoneRelationship mr = (MilestoneRelationship)obj;
							Console.WriteLine("Relationship : " + mr);
						}
					}
					Console.WriteLine();
				}
			}
#endif
#endregion
		}
	}

	public class MilestoneAdjustmentException : Exception {
		private MilestoneRelationship m_relationship;
		public MilestoneAdjustmentException(string msg, MilestoneRelationship relationship):base(msg){
			m_relationship = relationship;
		}

		public MilestoneRelationship Relationship { get { return m_relationship; } }
	}
}
