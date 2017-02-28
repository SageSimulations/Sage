/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using System.Collections;
using Highpoint.Sage.Utility;
using System.Text;
using System.Collections.Generic;

namespace Highpoint.Sage.Scheduling
{

    public delegate void TimePeriodChange(ITimePeriod who, TimePeriod.ChangeType howChanged);

	public class TimePeriod : ITimePeriod {

		public enum ChangeType { StartTime, Duration, EndTime, All };
		public enum Relationship { 
			StartsBeforeStartOf, 
			StartsOnStartOf, 
			StartsAfterStartOf, 
			StartsBeforeEndOf, 
			StartsOnEndOf, 
			StartsAfterEndOf,
			EndsBeforeStartOf,
			EndsOnStartOf,
			EndsAfterStartOf,
			EndsBeforeEndOf,
			EndsOnEndOf,
			EndsAfterEndOf }
	
#region Private Fields
		private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("TimePeriod");
        private bool m_supportsReactiveAdjustment = true;
		private Milestone m_startMilestone;
		private Milestone m_endMilestone;
		private TimeSpan m_duration;
		private bool m_hasDuration;
		private TimeAdjustmentMode m_adjustmentMode;
		private Stack m_adjustmentModeStack;
		private ArrayList m_adjustmentModeRelationships;
		private string m_name;
		private Guid m_guid;
		private string m_description = "";
		private static readonly string s_default_Name = "TimePeriod";
		private MilestoneRelationship_Strut m_inferenceRelationship;
        private enum MsRel { Before, On, After }
        private ISupportsCorrelation m_subject;
        private object m_modifier;
#endregion

#region Constructors
        public TimePeriod(DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode)
            : this(s_default_Name, Guid.NewGuid(), startTime, endTime, adjustmentMode, true) { }

        public TimePeriod(DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode, bool supportsReactiveAdjustment)
            : this(s_default_Name, Guid.NewGuid(), startTime, endTime, adjustmentMode, supportsReactiveAdjustment) { }

        public TimePeriod(string name, Guid guid, DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode)
            : this(name, guid, startTime, endTime, adjustmentMode, true) { }

        public TimePeriod(string name, Guid guid, DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode, bool supportsReactiveAdjustment) {
			m_name = name;
			m_guid = guid;
			m_startMilestone = new Milestone(m_name+".Start",Guid.NewGuid(),startTime);
			m_endMilestone = new Milestone(m_name+".End",Guid.NewGuid(),endTime);
			m_duration = endTime - startTime;
			m_hasDuration = true;
            m_supportsReactiveAdjustment = supportsReactiveAdjustment;
			Init();
			AdjustmentMode = adjustmentMode;
		}

		public TimePeriod(DateTime startTime, TimeSpan duration, TimeAdjustmentMode adjustmentMode)
			:this(s_default_Name,Guid.NewGuid(),startTime,duration,adjustmentMode){}

		public TimePeriod(string name, Guid guid, DateTime startTime, TimeSpan duration, TimeAdjustmentMode adjustmentMode){
			m_name = name;
			m_guid = guid;
			m_startMilestone = new Milestone(m_name+".Start",Guid.NewGuid(),startTime);
			m_endMilestone = new Milestone(m_name+".End",Guid.NewGuid(),startTime+duration);
			m_duration = duration;
			m_hasDuration = true;
			Init();
			AdjustmentMode = adjustmentMode;
		}

		public TimePeriod(TimeSpan duration, DateTime endTime, TimeAdjustmentMode adjustmentMode)
			:this(s_default_Name,Guid.NewGuid(),duration,endTime,adjustmentMode){}
		public TimePeriod(string name, Guid guid, TimeSpan duration, DateTime endTime, TimeAdjustmentMode adjustmentMode){
			m_name = name;
			m_guid = guid;
			m_startMilestone = new Milestone(m_name+".Start",Guid.NewGuid(),endTime-duration);
			m_endMilestone = new Milestone(m_name+".End",Guid.NewGuid(),endTime);
			m_duration = duration;
			m_hasDuration = true;
			Init();
			AdjustmentMode = adjustmentMode;
		}


		public TimePeriod(TimeAdjustmentMode adjustmentMode)
			:this(s_default_Name, Guid.NewGuid(), adjustmentMode){}
		public TimePeriod(string name, Guid guid, TimeAdjustmentMode adjustmentMode){
            m_name = name;
            m_guid = guid;
            m_startMilestone = new Milestone(m_name + ".Start", Guid.NewGuid(), DateTime.MinValue, false);
			m_endMilestone = new Milestone(m_name+".End",Guid.NewGuid(),DateTime.MaxValue,false);
			m_duration = TimeSpan.MaxValue;
			m_hasDuration = false;
			Init();
			AdjustmentMode = adjustmentMode;
		}

		private void Init(){
            if (m_supportsReactiveAdjustment) {
                m_adjustmentModeStack = new Stack();
                m_adjustmentModeRelationships = new ArrayList();

                // These two are always present, and always active, therefore we do not put them in the
                // arraylist of internal (i.e. clearable) relationships. A TimePeriod can NEVER end before it starts.
                MilestoneRelationship mr1 = new MilestoneRelationship_LTE(StartMilestone, EndMilestone);
                MilestoneRelationship mr2 = new MilestoneRelationship_GTE(EndMilestone, StartMilestone);
                mr1.AddReciprocal(mr2);
                mr2.AddReciprocal(mr1);
            }
		}
#endregion

#region Adjustment Mode Management
		/// <summary>
		/// This property determines how the triad of start, duration &amp; finish are kept up-to-date
		/// as individual properties are set and changed.
		/// </summary>
		public TimeAdjustmentMode AdjustmentMode { 
			get { 
				return m_adjustmentMode;
			}
			set {
                if (m_supportsReactiveAdjustment) {
                    // Clear existing relationships.
                    foreach (MilestoneRelationship mr in m_adjustmentModeRelationships) {
                        mr.Detach();
                    }

                    m_inferenceRelationship = null;
                    MilestoneRelationship relationship;
                    switch (value) {
                        case TimeAdjustmentMode.None:
                            break;

                        case TimeAdjustmentMode.FixedStart:
                            relationship = new MilestoneRelationship_Pin(null, StartMilestone);
                            m_adjustmentModeRelationships.Add(relationship);
                            break;

                        case TimeAdjustmentMode.FixedDuration:
                            MilestoneRelationship fwd = new MilestoneRelationship_Strut(StartMilestone, EndMilestone);
                            m_adjustmentModeRelationships.Add(fwd);
                            MilestoneRelationship rev = new MilestoneRelationship_Strut(EndMilestone, StartMilestone);
                            m_adjustmentModeRelationships.Add(rev);
                            fwd.AddReciprocal(rev);
                            rev.AddReciprocal(fwd);
                            break;

                        case TimeAdjustmentMode.FixedEnd:
                            relationship = new MilestoneRelationship_Pin(null, EndMilestone);
                            m_adjustmentModeRelationships.Add(relationship);

                            break;
                        case TimeAdjustmentMode.InferStartTime:
                            m_inferenceRelationship = new MilestoneRelationship_Strut(StartMilestone, EndMilestone);
                            relationship = m_inferenceRelationship;
                            m_adjustmentModeRelationships.Add(relationship);
                            break;

                        case TimeAdjustmentMode.InferDuration:
                            break;

                        case TimeAdjustmentMode.InferEndTime:
                            m_inferenceRelationship = new MilestoneRelationship_Strut(EndMilestone, StartMilestone);
                            relationship = m_inferenceRelationship;
                            m_adjustmentModeRelationships.Add(relationship);
                            break;

                        case TimeAdjustmentMode.Locked:
                            relationship = new MilestoneRelationship_Pin(null, StartMilestone);
                            m_adjustmentModeRelationships.Add(relationship);
                            relationship = new MilestoneRelationship_Pin(null, EndMilestone);
                            m_adjustmentModeRelationships.Add(relationship);
                            break;
                    }
                }
				m_adjustmentMode = value;

//				_Debug.WriteLine("In " + this.Name + " mode was just set to " + m_adjustmentMode.ToString() +".");
//				_Debug.WriteLine("Adjustment mode Relationships are ...");
//				foreach ( MilestoneRelationship mr in m_adjustmentModeRelationships ) _Debug.WriteLine("\t" + mr.ToString());
			}
		}

		/// <summary>
		/// Pushes the current time period adjustment mode onto a stack, substituting a provided mode. This
		/// must be paired with a corresponding Pop operation.
		/// </summary>
		/// <param name="tam">The time period adjustment mode that is to temporarily take the place of the current one.</param>
		public void PushAdjustmentMode(TimeAdjustmentMode tam){
			m_adjustmentModeStack.Push(m_adjustmentMode);
			AdjustmentMode = tam;
		}

		/// <summary>
		/// Pops the previous time period adjustment mode off a stack, and sets this Time Period's adjustment mode to that value.
		/// </summary>
		/// <returns>The newly-popped time period adjustment mode.</returns>
		public TimeAdjustmentMode PopAdjustmentMode(){
			AdjustmentMode = (TimeAdjustmentMode)m_adjustmentModeStack.Pop();
			return AdjustmentMode;
		}
#endregion

        private void GetRelationshipParameters(Relationship relationship, ITimePeriod otherTimePeriod, out IMilestone a, out MsRel m, out IMilestone b) {
            switch (relationship) {
                case Relationship.StartsBeforeStartOf:
                    a = StartMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.StartsOnStartOf:
                    a = StartMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.StartsAfterStartOf:
                    a = StartMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.StartsBeforeEndOf:
                    a = StartMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.StartsOnEndOf:
                    a = StartMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.StartsAfterEndOf:
                    a = StartMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.EndsBeforeStartOf:
                    a = EndMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.EndsOnStartOf:
                    a = EndMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.EndsAfterStartOf:
                    a = EndMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.EndsBeforeEndOf:
                    a = EndMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.EndsOnEndOf:
                    a = EndMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.EndsAfterEndOf:
                    a = EndMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.EndMilestone;
                    break;
                default:
                    a = null;
                    b = null;
                    m = MsRel.On;
                    throw new ApplicationException("Error - unrecognized TimePeriod relationship " + relationship + " referenced in " + Name);
            }
        }

		public void AddRelationship(Relationship relationship, ITimePeriod otherTimePeriod ){

            if (m_supportsReactiveAdjustment) {

                IMilestone a, b;
                MsRel m;
                GetRelationshipParameters(relationship, otherTimePeriod, out a, out m, out b);

                MilestoneRelationship mr1, mr2;
                switch (m) {
                    case MsRel.Before:
                        mr1 = new MilestoneRelationship_LTE(a, b);
                        mr2 = new MilestoneRelationship_GTE(b, a);
                        //a.AddRelationship(mr);
                        break;
                    case MsRel.On:
                        mr1 = new MilestoneRelationship_Strut(a, b);
                        mr2 = new MilestoneRelationship_Strut(b, a);
                        //a.AddRelationship(mr);
                        break;
                    case MsRel.After:
                        mr1 = new MilestoneRelationship_GTE(a, b);
                        mr2 = new MilestoneRelationship_LTE(b, a);
                        //a.AddRelationship(mr);
                        break;
                    default:
                        throw new ApplicationException("Error - unrecognized MS_REL " + m + " referenced in " + Name);
                }
                mr2.AddReciprocal(mr1);
                mr1.AddReciprocal(mr2);
                //			_Debug.WriteLine("External relationship added to " + this.Name + " : \"" + mr1.ToString() + "\".");
                //			_Debug.WriteLine("\t+recip relationship added to " + this.Name + " : \"" + mr2.ToString() + "\".");

            } else {
                throw new ApplicationException("Trying to add relationships to a TimePeriod that does not support reactive adjustment.");
            }
		}

        public void RemoveRelationship(Relationship relationship, ITimePeriod otherTimePeriod) {
            //IMilestone a, b;
            //MS_REL m;
            //GetRelationshipParameters(relationship, otherTimePeriod, out a, out m, out b);

            //MilestoneRelationship mr1, mr2;
            //switch (m) {
            //    case MS_REL.Before:
            //        foreach (MilestoneRelationship mr in a.Relationships) {
            //            //if ( 
            //        }
            //        mr1 = new MilestoneRelationship_LTE(a, b);
            //        mr2 = mr1.Reciprocal;
            //        //a.AddRelationship(mr);
            //        break;
            //    case MS_REL.On:
            //        mr1 = new MilestoneRelationship_Strut(a, b);
            //        mr2 = mr1.Reciprocal;
            //        //a.AddRelationship(mr);
            //        break;
            //    case MS_REL.After:
            //        mr1 = new MilestoneRelationship_GTE(a, b);
            //        mr2 = mr1.Reciprocal;
            //        //a.AddRelationship(mr);
            //        break;
            //    default:
            //        throw new ApplicationException("Error - unrecognized MS_REL " + m.ToString() + " referenced in " + this.Name);
            //}
            //mr2.Reciprocals.Remove(mr1);
            //mr1.Reciprocals.Remove(mr2);
            throw new NotImplementedException();
        }

#region Milestones
		/// <summary>
		/// The milestone that represents the starting of this time period.
		/// </summary>
		public IMilestone StartMilestone { get { return m_startMilestone; } }

		/// <summary>
		/// The milestone that represents the ending point of this time period.
		/// </summary>
		public IMilestone EndMilestone { get { return m_endMilestone; } }
#endregion

#region StartTime, Duration and EndTime
		/// <summary>
		/// Reads and writes the Start Time. When writing the start time, leaves the
		/// end time fixed, and adjusts duration.
		/// </summary>
		public virtual DateTime StartTime { 
			get { return m_startMilestone.DateTime; }  
			set {
				DateTime was = StartMilestone.DateTime;
				if ( value.Equals(was) && StartMilestone.Active ) return;
				try {
					//_Debug.WriteLine("Trying to move start of " + this.Name + " to " + value.ToString());
					StartMilestone.MoveTo(value);
					if ( ChangeEvent!= null ) ChangeEvent(this,ChangeType.StartTime,null);
				} catch ( MilestoneAdjustmentException mae ){
					StartMilestone.PushActiveSetting(false);
					StartMilestone.MoveTo(was);
					StartMilestone.PopActiveSetting();
					throw mae;
				}
			} 
		}
		
		/// <summary>
		/// Reads and writes the End Time. When writing the end time, leaves the
		/// start time fixed, and adjusts duration.
		/// </summary>
        public virtual DateTime EndTime { 
			get { return EndMilestone.DateTime; }  
			set {
				DateTime was = EndMilestone.DateTime;
				if ( value.Equals(was) && EndMilestone.Active ) return;
				try {
					EndMilestone.MoveTo(value);
					if ( ChangeEvent!= null ) ChangeEvent(this,ChangeType.EndTime,null);
				} catch ( MilestoneAdjustmentException mae ){
					EndMilestone.PushActiveSetting(false);
					EndMilestone.MoveTo(was);
					EndMilestone.PopActiveSetting();
					throw mae;
				}
			} 
		}
		
		/// <summary>
		/// Gets the duration of the time period.
		/// </summary>
		public virtual TimeSpan Duration { 
			get { 
				if ( m_startMilestone!=null && m_endMilestone!=null && m_startMilestone.Active && m_endMilestone.Active ) {
					return (m_endMilestone.DateTime - m_startMilestone.DateTime);
				} else {
					return m_duration;
				}
			} 
			set {
				DateTime startWas = StartMilestone.DateTime;
				DateTime endWas = EndMilestone.DateTime;
				m_hasDuration = true;
				if ( value.Equals(endWas-startWas) ) return;
				try {

					switch ( m_adjustmentMode ) {
						case TimeAdjustmentMode.None:
							m_duration = value;
							break;

						case TimeAdjustmentMode.FixedStart:
							EndMilestone.MoveTo(StartMilestone.DateTime+value);
							break;

						case TimeAdjustmentMode.InferEndTime:
							m_inferenceRelationship.Delta = value;
							break;

						case TimeAdjustmentMode.FixedDuration:
						case TimeAdjustmentMode.InferDuration:
						case TimeAdjustmentMode.Locked:
							throw new TimePeriodAdjustmentException("Cannot adjust duration when Time Period is set to " + m_adjustmentMode + " adjustment mode.");
							//break;

						case TimeAdjustmentMode.FixedEnd:
							StartMilestone.MoveTo(EndMilestone.DateTime-value);
							break;

						case TimeAdjustmentMode.InferStartTime:
							m_inferenceRelationship.Delta = -value;
							break;

						default:
							throw new ApplicationException("Unrecognized TimeAdjustmentMode specified - " + m_adjustmentMode + ".");
					}

					if ( ChangeEvent!= null ) ChangeEvent(this,ChangeType.Duration,null);

				} catch ( MilestoneAdjustmentException mae ){
					StartMilestone.PushActiveSetting(false);
					EndMilestone.PushActiveSetting(false);
					StartMilestone.MoveTo(startWas);
					EndMilestone.MoveTo(endWas);
					StartMilestone.PopActiveSetting();
					EndMilestone.PopActiveSetting();
					throw mae;
				}
			}
		}

		
		/// <summary>
		/// True if the time period has a determinate start time.
		/// </summary>
		public bool HasStartTime { get { return StartMilestone.Active; } }
		/// <summary>
		/// True if the time period has a determinate end time.
		/// </summary>
		public bool HasEndTime { get { return EndMilestone.Active; } }
		/// <summary>
		/// True if the time period has a determinate duration.
		/// </summary>
		public bool HasDuration { get { return m_hasDuration; } }

		/// <summary>
		/// Sets the start time to an indeterminate time.
		/// </summary>
		public void ClearStartTime(){
			StartMilestone.Active = false;
			StartMilestone.MoveTo(DateTime.MaxValue);
			if ( ChangeEvent!= null ) ChangeEvent(this,ChangeType.StartTime,null);
		}
		/// <summary>
		/// Sets the end time to an indeterminate time.
		/// </summary>
		public void ClearEndTime(){
			EndMilestone.Active = false;
			EndMilestone.MoveTo(DateTime.MaxValue);
			if ( ChangeEvent!= null ) ChangeEvent(this,ChangeType.EndTime,null);
		}
	
		/// <summary>
		/// Sets the duration to an indeterminate timespan.
		/// </summary>
		public void ClearDuration(){
			m_hasDuration = false;
			m_duration = TimeSpan.MaxValue;
			if ( ChangeEvent!= null ) ChangeEvent(this,ChangeType.Duration,null);
		}
#endregion

		public event ObservableChangeHandler ChangeEvent;

        public override string ToString() {
            return string.Format("{0}{1} [{2}->{3}->{4}]",
                m_subject == null ? "" : m_subject.Name,
                m_modifier == null ? "" : "("+m_modifier+")",
                StartMilestone.Active ? StartMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M",
                ( ( StartMilestone.Active && EndMilestone.Active ) ? Duration.ToString() : "--:--:--" ),
                EndMilestone.Active ? EndMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M");
        }

		public static TimePeriod operator +(TimePeriod a, TimePeriod b){

			DateTime startTime = DateTime.MaxValue;
			bool hasStartTime = a.HasStartTime || b.HasStartTime;
			if ( hasStartTime ) {
				if ( a.HasStartTime ) startTime = DateTimeOperations.Min(startTime,a.StartTime);
				if ( b.HasStartTime ) startTime = DateTimeOperations.Min(startTime,b.StartTime);
			}

			DateTime endTime = DateTime.MaxValue;
			bool hasEndTime = a.HasEndTime || b.HasEndTime;
			if ( hasEndTime ) {
				if ( a.HasEndTime ) endTime = DateTimeOperations.Max(endTime,a.EndTime);
				if ( b.HasEndTime ) endTime = DateTimeOperations.Max(endTime,b.EndTime);
			}

            // We will infer duration.
			bool hasDuration = hasStartTime && hasEndTime;

			// If both adjustment modes match, we will use that mode. Otherwise, we default to "None".
			TimeAdjustmentMode tam = TimeAdjustmentMode.None;
			if ( a.AdjustmentMode.Equals(b.AdjustmentMode) ) tam = a.AdjustmentMode;

			TimePeriod tp = new TimePeriod(TimeAdjustmentMode.None);

			if ( hasStartTime ) tp.StartTime = startTime;
			if ( hasEndTime ) tp.EndTime = endTime;
			if ( hasDuration ) tp.Duration = endTime - startTime;

			tp.AdjustmentMode = tam;

			return tp;
		}

#region IHasIdentity Members
		public string Name { get { return m_name; } }
		public Guid Guid => m_guid;
		public string Description { get { return m_description; } }
#endregion

#region ITimePeriod Members

        public ISupportsCorrelation Subject {
            [DebuggerStepThrough]
            get { 
                return m_subject;
            }
            [DebuggerStepThrough]
            set {
                m_subject = value;
            }
        }

        public object Modifier {
            [DebuggerStepThrough]
            get { 
                return m_modifier;
            }
            [DebuggerStepThrough]
            set {
                m_modifier = value;
            }
        }
#endregion

        internal IEnumerator<ITimePeriod> GetDepthFirstEnumerator() { yield return this; }
        internal IEnumerator<ITimePeriod> GetBreadthFirstEnumerator() { yield return this; }
    }

	
	public class TimePeriodEnvelope : ITimePeriod {
		
#region Private Fields
		private List<ITimePeriod> m_childTimePeriods;
		private Milestone m_startMilestone;
		private Milestone m_endMilestone;
		private string m_name;
		private Guid m_guid;
		private string m_description = "";
		private static readonly string s_default_Name = "TimePeriodEnvelope";
        private ISupportsCorrelation m_subject;
        private object m_modifier;
#endregion

#region Constructors
		public TimePeriodEnvelope():this(s_default_Name,Guid.NewGuid(),true){}
        public TimePeriodEnvelope(string name, Guid guid) : this(name,guid,true){}
        public TimePeriodEnvelope(string name, Guid guid, bool supportsReactiveAdjustment) {
			m_name = name;
			m_guid = guid;
            m_childTimePeriods = new List<ITimePeriod>();
			m_startMilestone = new Milestone(name+".Start",guid,DateTime.MinValue,false);
            m_endMilestone = new Milestone(name + ".End", guid, DateTime.MaxValue, false);

			// These two are always present, and always active, therefore we do not put them in the
			// arraylist of internal (i.e. clearable) relationships. A TimePeriod can NEVER end before it starts.
            if (supportsReactiveAdjustment) {
                new MilestoneRelationship_LTE(StartMilestone, EndMilestone);
                new MilestoneRelationship_GTE(EndMilestone, StartMilestone);
            }
		}
#endregion

#region Add & Remove Time Periods
        public void AddTimePeriod(ITimePeriod childTimePeriod) {
            m_childTimePeriods.Add(childTimePeriod);
            childTimePeriod.StartMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
            childTimePeriod.EndMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
            Update();
        }

        public void AddTimePeriods(IEnumerable<ITimePeriod> childTimePeriods) {
            foreach (ITimePeriod childTimePeriod in childTimePeriods) {
                m_childTimePeriods.Add(childTimePeriod);
                childTimePeriod.StartMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
                childTimePeriod.EndMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
            }
            Update();
        }

        public void RemoveTimePeriods(IEnumerable<ITimePeriod> childTimePeriods) {
            foreach (ITimePeriod childTimePeriod in childTimePeriods) {
                m_childTimePeriods.Remove(childTimePeriod);
                childTimePeriod.StartMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
                childTimePeriod.EndMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
            }
            Update();
        }

        public void RemoveTimePeriod(ITimePeriod childTimePeriod) {
            m_childTimePeriods.Remove(childTimePeriod);
            childTimePeriod.StartMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
            childTimePeriod.EndMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
            Update();
        }

#endregion

		private void Update(){

            DateTime earliest = DateTime.MaxValue;
            m_childTimePeriods.ForEach(delegate(ITimePeriod tp) { earliest = DateTimeOperations.Min(earliest, tp.StartMilestone.DateTime); });
            DateTime latest = DateTime.MinValue;
            m_childTimePeriods.ForEach(delegate(ITimePeriod tp) { latest = DateTimeOperations.Max(latest, tp.EndMilestone.DateTime); });

            if (!earliest.Equals(DateTime.MaxValue)) {
                m_startMilestone.MoveTo(earliest);
            }
            if (!latest.Equals(DateTime.MinValue)) {
                m_endMilestone.MoveTo(latest);
            }

            // We will deal with this later. For now, we only support as-is time periods and the
            // only milestones that move are those in TimePeriodEnvelopes - they are set only
            // when a child is added to, or removed from, an envelope.
            /*
			bool hadStartTime = false;
			DateTime earliest = DateTime.MaxValue;
			bool hadEndTime = false;
			DateTime latest = DateTime.MinValue;

			foreach ( ITimePeriod childTimePeriod in m_childTimePeriods ) {
				if ( childTimePeriod.HasStartTime ) {
					hadStartTime = true;
					earliest = DateTimeOperations.Min(earliest,childTimePeriod.StartTime);
                }
				if ( childTimePeriod.HasEndTime ) {
					hadEndTime = true;
					latest = DateTimeOperations.Max(earliest,childTimePeriod.EndTime);
				}
			}

			bool msStartActive = StartMilestone.Active;
			DateTime msStartDateTime = StartMilestone.DateTime;
			bool msEndActive = EndMilestone.Active;
			DateTime msEndDateTime = EndMilestone.DateTime;
			try {
                if (hadStartTime) {
                    StartMilestone.Active = true;
                    StartMilestone.MoveTo(earliest);
                }
                if (hadEndTime) {
                    EndMilestone.Active = hadEndTime;
                    EndMilestone.MoveTo(latest);
                }
			} catch ( MilestoneAdjustmentException mae ) {
				StartMilestone.PushActiveSetting(false);
				EndMilestone.PushActiveSetting(false);
				StartMilestone.MoveTo(msStartDateTime);
				StartMilestone.Active = msStartActive;
				EndMilestone.MoveTo(msEndDateTime);
				EndMilestone.Active = msEndActive;
				StartMilestone.PopActiveSetting();
				EndMilestone.PopActiveSetting();
				throw mae;
			} */

		}
	
#region Milestone & Time getters
		/// <summary>
		/// Gets the start time of the time period.
		/// </summary>
		public DateTime StartTime { get { return m_startMilestone.DateTime; } }
		/// <summary>
		/// Gets the end time of the time period.
		/// </summary>
		public DateTime EndTime { get { return m_endMilestone.DateTime; } }
		/// <summary>
		/// Gets the duration of the time period.
		/// </summary>
		public TimeSpan Duration { get { return m_endMilestone.DateTime-m_startMilestone.DateTime; } }

		/// <summary>
		/// True if the time period has a determinate start time.
		/// </summary>
		public bool HasStartTime {  get { return m_startMilestone.Active; }  }
		/// <summary>
		/// True if the time period has a determinate end time.
		/// </summary>
		public bool HasEndTime {  get { return m_endMilestone.Active; }  }
		/// <summary>
		/// True if the time period has a determinate duration.
		/// </summary>
		public bool HasDuration {  get { return m_startMilestone.Active && m_endMilestone.Active; }  }

		/// <summary>
		/// The milestone that represents the starting of this time period.
		/// </summary>
		public IMilestone StartMilestone { 
			get { 
				return m_startMilestone; 
			} 
		}

		/// <summary>
		/// The milestone that represents the ending point of this time period.
		/// </summary>
		public IMilestone EndMilestone { 
			get { 
				return m_endMilestone; 
			} 
		}
#endregion

#region IHasIdentity Members
		public string Name { get { return m_name; } }
		public Guid Guid => m_guid;
		public string Description { get { return m_description; } }
#endregion

		/// <summary>
		/// Determines what inferences are to be made about the other two settings when one
		/// of the settings (start, duration, finish times) is changed.
		/// </summary>
		public TimeAdjustmentMode AdjustmentMode { get { return TimeAdjustmentMode.InferDuration; } }

		private void Milestone_ChangeEvent(object whoChanged, object whatChanged, object howChanged) {
			Update();
		}
		
#region ITimePeriod Members

		DateTime ITimePeriodBase.StartTime {
			get {
				return m_startMilestone.DateTime;
			}
			set {
				throw new ApplicationException("TimePeriodEnvelope is Read-only.");
			}
		}

		DateTime ITimePeriodBase.EndTime {
			get {
				return EndMilestone.DateTime;
			}
			set {
				throw new ApplicationException("TimePeriodEnvelope is Read-only.");
			}
		}

        TimeSpan ITimePeriodBase.Duration {
            [DebuggerStepThrough]
            get {
				return m_endMilestone.DateTime-m_startMilestone.DateTime;
			}
			set {
				throw new ApplicationException("TimePeriodEnvelope is Read-only.");
			}
		}

		public void ClearStartTime() {
			throw new ApplicationException("TimePeriodEnvelope is Read-only.");
		}

		public void ClearEndTime() {
			throw new ApplicationException("TimePeriodEnvelope is Read-only.");
		}

		public void ClearDuration() {
			throw new ApplicationException("TimePeriodEnvelope is Read-only.");
		}

		TimeAdjustmentMode ITimePeriod.AdjustmentMode {
			get {
				return TimeAdjustmentMode.None;
			}
			set {
				throw new ApplicationException("TimePeriodEnvelope is Read-only.");
			}
		}

		public void PushAdjustmentMode(TimeAdjustmentMode tam) {
			throw new ApplicationException("TimePeriodEnvelope is Read-only.");
		}

		public TimeAdjustmentMode PopAdjustmentMode() {
			throw new ApplicationException("TimePeriodEnvelope is Read-only.");
		}

		public void AddRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod) {
			throw new ApplicationException("TimePeriodEnvelope is Read-only.");
		}

        public void RemoveRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod) {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        public ISupportsCorrelation Subject {
            get {
                return m_subject;
            }
            set {
                m_subject = value;
            }
        }

        public object Modifier {
            get {
                return m_modifier;
            }
            set {
                m_modifier = value;
            }
        }

#endregion

        /// <summary>
        /// Returns an iterator that traverses the descendant payloads breadth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<ITimePeriod> BreadthFirstEnumerable => new Enumerable<ITimePeriod>(GetBreadthFirstEnumerator());

	    private IEnumerator<ITimePeriod> GetBreadthFirstEnumerator() {
            Queue<ITimePeriod> todo = new Queue<ITimePeriod>();
            todo.Enqueue(this);
            while (todo.Count > 0) {
                ITimePeriod tp = todo.Dequeue();
                if (tp is TimePeriodEnvelope) {
                    ( (TimePeriodEnvelope)tp ).Children.ForEach(delegate(ITimePeriod kid) { todo.Enqueue(kid); });
                }
                yield return tp;
            }
        }

        /// <summary>
        /// Returns an iterator that traverses the descendant payloads depth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<ITimePeriod> DepthFirstEnumerable {
            get {
                return new Enumerable<ITimePeriod>(GetDepthFirstEnumerator());
            }
        }

        private IEnumerator<ITimePeriod> GetDepthFirstEnumerator() {
            yield return this;
            foreach (ITimePeriod kid in Children) {
                if (kid is TimePeriodEnvelope) {
                    IEnumerator<ITimePeriod> tpe = ( (TimePeriodEnvelope)kid ).GetDepthFirstEnumerator();
                    while (tpe.MoveNext()) {
                        yield return tpe.Current;
                    }
                } else {
                    yield return kid;
                }
            }
        }

        /// <summary>
        /// Gets the list of children. Do not modify this.
        /// </summary>
        /// <value>The children.</value>
        public List<ITimePeriod> Children { get { return m_childTimePeriods; } }

        public override string ToString() {
            return string.Format("{0}{1} [{2}->{3}->{4}]",
                m_subject == null ? "" : m_subject.Name,
                m_modifier == null ? "" : "("+m_modifier+")",
                StartMilestone.Active ? StartMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M",
                ((StartMilestone.Active&&EndMilestone.Active)?Duration.ToString():"--:--:--"),
                EndMilestone.Active ? EndMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M");
        }

#region IObservable Members

#pragma warning disable 67 // Ignore it if this event is not used. It's a framework, and this event may be for clients.
        public event ObservableChangeHandler ChangeEvent;
#pragma warning restore 67
#endregion

        public static string ToString(ITimePeriod root, TimePeriodSorter sortChildrenBy) {
            StringBuilder sb = new StringBuilder();
            ToString(ref sb, root, sortChildrenBy, 0);
            return sb.ToString();
        }

        private static void ToString(ref StringBuilder sb, ITimePeriod itp, TimePeriodSorter sortChildrenBy, int depth) {
            string indent = StringOperations.Spaces(depth * 4);
            if (itp is TimePeriodEnvelope) {
                TimePeriodEnvelope tpe = (TimePeriodEnvelope)itp;
                sb.Append(string.Format("{0}<TimePeriodEnvelope subject=\"{1}\"modifier=\"{2}\" start=\"{3}\" end=\"{4}\">\r\n",
                    indent,
                    tpe.Subject.Name,
                    tpe.Modifier,
                    tpe.StartTime,
                    tpe.EndTime));

                tpe.m_childTimePeriods.Sort(TimePeriodSorter.ByIncreasingStartTime);
                foreach (ITimePeriod child in tpe.m_childTimePeriods) {
                    ToString(ref sb, child, sortChildrenBy, depth + 1);
                }
                sb.Append(string.Format("{0}</TimePeriodEnvelope>\r\n", indent));
            } else if (itp is TimePeriod) {
                TimePeriod tp = (TimePeriod)itp;
                sb.Append(string.Format("{0}<TimePeriodEnvelope subject=\"{1}\"modifier=\"{2}\" start=\"{3}\" end=\"{4}\" duration=\"{5}\">\r\n",
                    indent,
                    tp.Subject.Name,
                    tp.Modifier,
                    tp.StartTime,
                    tp.Duration,
                    tp.EndTime));
            }
        }
	}
		
#if NOT_DEFINED
	/// <summary>
	/// A TimePeriodAspectSelector is useful for when one is working with a group of objects each 
	/// with time periods, but does not want to explicity select them - essentially, "give me the best
	/// indication of time period that you have", with a certain priority.<p/>
	/// For example, if we are working with a list of tasks that have planned and actual time period aspects,
	/// and we want the best time period data available for any given task, we would wrap the operation
	/// in a TimePeriodAspectSelector whose filterCriteria argument was new object[]{TaskAspectKey.Actual,TaskAspectKey.Planned};<p/>
	/// This would cause any time data requested to come from the TaskAspectKey.Actual aspect if it were
	/// defined there, and only from the TaskAspectKey.Planned if the desired data were defined by that one,
	/// and not by the preferred one.<p/>
	/// This is intended to be defined and used as follows:
	/// <code></code>
	/// </summary>
	public class TimePeriodAspectSelector : ITimePeriod {
		private IHasTimePeriodAspects m_ihti = null;
		private string m_name = null;
		private string m_description = null;
		private Guid m_guid = Guid.Empty;
		private object[] m_filterCriteria;
		/// <summary>
		/// Creates a TimePeriodAspectSelector with the specified array of filter criteria. The aspects
		/// whose keys are earlier in the array have priority over those later in the array, and any that
		/// are not in the array are not considered. The elements of the array must all be legitimate
		/// aspect keys.
		/// </summary>
		/// <param name="filterCriteria">An array of valid Aspect Keys for the targets being queried. An aspect 
		/// key is any object under which an ITimePeriod might have been stored in an object that implements
		/// IHasTimePeriodAspects.</param>
		public TimePeriodAspectSelector(object[] filterCriteria){
			m_ihti = null;
			m_filterCriteria = filterCriteria;
		}

		/// <summary>
		/// Sets the object whose time period aspects are being queried to a new value.
		/// </summary>
		/// <param name="ihti">The new object being queried for data from its time period aspects.</param>
		/// <returns>Itself, so that it can be used as <code>DateTime startTime = myTimePeriodAspectSelector.SetTarget(newTask).StartTime;</code></returns>
		public TimePeriodAspectSelector SetTarget(IHasTimePeriodAspects ihti){
			m_ihti = ihti;
			return this;
		}

		/// <summary>
		/// The milestone that represents the starting of this time period.
		/// </summary>
		public IMilestone StartMilestone { 
			get { 
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasStartTime ) return itro.StartMilestone;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).StartMilestone;
			} 
		}

		/// <summary>
		/// The milestone that represents the ending point of this time period.
		/// </summary>
		public IMilestone EndMilestone { 
			get { 
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasStartTime ) return itro.EndMilestone;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).EndMilestone;
			} 
		}

		/// <summary>
		/// Gets the best available defined start time of the time period.
		/// </summary>
		public DateTime StartTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasStartTime ) return itro.StartTime;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).StartTime;
			} 
		}
		/// <summary>
		/// Gets the best available defined end time of the time period.
		/// </summary>
		public DateTime EndTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasEndTime ) return itro.EndTime;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).EndTime;
			} 
		}
		/// <summary>
		/// Gets the best available defined duration of the time period.
		/// </summary>
		public TimeSpan Duration { 
			get{
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasDuration ) return itro.Duration;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).Duration;
			} 
		}

		/// <summary>
		/// True if any of the time period aspects has a determinate start time.
		/// </summary>
		public bool HasStartTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return true;
				}
				return false;
			} 
		}
		/// <summary>
		/// True if any of the time period aspects has a determinate end time.
		/// </summary>
		public bool HasEndTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasEndTime ) return true;
				}
				return false;
			} 
		}
		/// <summary>
		/// True if any of the time period aspects has a determinate duration.
		/// </summary>
		public bool HasDuration {
			get{
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasDuration ) return true;
				}
				return false;
			} 
		}

		public TimeAdjustmentMode AdjustmentMode {
			get { 
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					return itro.AdjustmentMode;
				}
				return TimeAdjustmentMode.None;
			}
		}

#region IHasIdentity Members
		public string Name { get { return m_name; } }
		public Guid Guid => m_guid;
		public string Description { get { return m_description; } }
#endregion

#region ITimePeriod Members

		DateTime Highpoint.Sage.Scheduling.ITimePeriod.StartTime {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).StartTime;
				}
				return DateTime.MinValue;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.StartTime setter implementation
			}
		}

		DateTime Highpoint.Sage.Scheduling.ITimePeriod.EndTime {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).StartTime;
				}
				return DateTime.MinValue;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.EndTime setter implementation
			}
		}

		TimeSpan Highpoint.Sage.Scheduling.ITimePeriod.Duration {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).Duration;
				}
				return TimeSpan.MaxValue;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.Duration setter implementation
			}
		}

		public void ClearStartTime() {
			// TODO:  Add TimePeriodAspectSelector.ClearStartTime implementation
		}

		public void ClearEndTime() {
			// TODO:  Add TimePeriodAspectSelector.ClearEndTime implementation
		}

		public void ClearDuration() {
			// TODO:  Add TimePeriodAspectSelector.ClearDuration implementation
		}

		Highpoint.Sage.Scheduling.TimeAdjustmentMode Highpoint.Sage.Scheduling.ITimePeriod.AdjustmentMode {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).AdjustmentMode;
				}
				return TimeAdjustmentMode.None;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.AdjustmentMode setter implementation
			}
		}

		public void PushAdjustmentMode(Highpoint.Sage.Scheduling.TimeAdjustmentMode tam) {
			// TODO:  Add TimePeriodAspectSelector.PushAdjustmentMode implementation
		}

		public Highpoint.Sage.Scheduling.TimeAdjustmentMode PopAdjustmentMode() {
			// TODO:  Add TimePeriodAspectSelector.PopAdjustmentMode implementation
			return TimeAdjustmentMode.None;
		}

		public void AddRelationship(Highpoint.Sage.Scheduling.TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod) {
			// TODO:  Add TimePeriodAspectSelector.AddRelationship implementation
		}

        public void RemoveRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod) {
            // TODO:  Add TimePeriodAspectSelector.AddRelationship implementation
        }

#endregion

#region IObservable Members
#pragma warning disable 67 // Ignore it if this event is not used. It's a framework, and this event may be for clients.
        public event Highpoint.Sage.Utility.ObservableChangeHandler ChangeEvent;
#pragma warning restore 67
#endregion

#region ITimePeriod Members

        private IHasIdentity m_subject;
        private object m_modifier;
        public void SetSubjectAndModifier(IHasIdentity subject, object modifier) {
            m_subject = subject;
            m_modifier = modifier;
        }

        public IHasIdentity Subject {
            get {
                return m_subject;
            }
        }

        public object Modifier {
            get {
                return m_modifier;
            }
        }

#endregion
    }
#endif

	public class TimePeriodAdjustmentException : Exception {
		public TimePeriodAdjustmentException(string msg):base(msg){}
	}

    /// <summary>
    /// Class Enumerable provides a wrapper for an IEnumerable of T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.Generic.IEnumerable{T}" />
    class Enumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerator<T> m_enumerator;
        public Enumerable(IEnumerator<T> enumerator)
        {
            m_enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_enumerator;
        }
    }
}
