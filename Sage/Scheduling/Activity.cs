/* This source code licensed under the GNU Affero General Public License */


#if NYRFPT
namespace Highpoint.Sage.Scheduling
{
	/// <summary>
	/// An activity is something that takes time, and may encompass other activities. So an activity has 
	/// one or more TimePeriodAspects and zero or more children that can define its duration.
	/// </summary>
	public class Activity : TreeNode<Activity>, IHasIdentity /*, IXmlSerializable*/
	{
        /// <summary>
        /// Describes the actions to be taken impacting time periods when a new child activity is added to this activity.
        /// </summary>
        public enum NewChildAccomodationMode {
            /// <summary>
            /// Adjusts the parent start and end times so that it starts at-or-before the start of the child,
            /// and ends at-or-after the end of the child.
            /// </summary>
            AdjustParentToChildren,
            /// <summary>
            /// Addition, removal of children has no effect on this Avtivity's start &amp; end points.
            /// </summary>
            IgnoreChildren
        };

#region Private Fields
        private readonly Dictionary<object,ITimePeriod> m_timePeriods;
        private readonly NewChildAccomodationMode m_ncaMode;
        private Hashtable m_properties;

#endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
		public Activity(string name, Guid guid) {
			Name = name;
			Guid = guid;
            m_timePeriods = new Dictionary<object, ITimePeriod>();
            m_properties = null;
			m_ncaMode = NewChildAccomodationMode.AdjustParentToChildren;
            GainedChild += Activity_GainedChild;
            LostChild += Activity_LostChild;
            SetPayload(this);
		}

        /// <summary>
        /// Adds the time period.
        /// </summary>
        /// <param name="aspectKey">The aspect key.</param>
        /// <param name="timePeriod">The time period.</param>
		public void AddTimePeriod(object aspectKey, ITimePeriod timePeriod){
			m_timePeriods.Add(aspectKey,timePeriod);
		}

        /// <summary>
        /// Gets the property dictionary associated with this activity.
        /// </summary>
        /// <value>The properties.</value>
        public IDictionary Properties => m_properties ?? (m_properties = new Hashtable());

	    /// <summary>
        /// Determines whether the scuedule for the specified aspect (e.g. planned or actual) of this activity 
        /// is valid. If it is not, then when it completes, the textWriter will contain a description of why not.
        /// </summary>
        /// <param name="tw">The textWriter that declares why the time period's schedule might not be valid.</param>
        /// <param name="timePeriodKey">The time period key.</param>
        /// <returns>true if the schedule under this activity is valid.</returns>
		public bool Validate(System.IO.TextWriter tw, object timePeriodKey){

			bool allValid = false;

			while ( !allValid ) {
                List<IMilestone> milestones = GatherAllMilestones(timePeriodKey);

                List<MilestoneRelationship> relationships = new List<MilestoneRelationship>();
				foreach ( IMilestone ms in milestones ) {
					foreach ( MilestoneRelationship mr in ms.Relationships ) {
						if ( !relationships.Contains(mr) ) relationships.Add(mr);
					}
				}

				allValid = true;
				foreach ( MilestoneRelationship mr in relationships ) {
					if ( !mr.IsSatisfied() ) {
						allValid = false;
					    tw?.WriteLine(mr.ToString());
					}
				}
			}

			return true;
		}

        /// <summary>
        /// Produces a list of all milestones that are a part of this activity, for the specified aspect.
        /// </summary>
        /// <param name="aspectKey">The aspect key that describes which aspect we are interested in.</param>
        /// <returns></returns>
        public List<IMilestone> GatherAllMilestones(object aspectKey) {
            List<IMilestone> msl = new List<IMilestone>();
            _GatherAllMilestones(ref msl, this, aspectKey);

			return msl;
		}

        private static void _GatherAllMilestones(ref List<IMilestone> list, Activity activity, object aspectKey) {

            IMilestone sm = ( activity.GetTimePeriodAspect(aspectKey) ).StartMilestone;
            IMilestone em = ( activity.GetTimePeriodAspect(aspectKey) ).EndMilestone;

            if (!list.Contains(sm)) {
                list.Add(sm);
            }
            if (!list.Contains(em)) {
                list.Add(em);
            }
            foreach (var treeNode in activity.Children)
            {
                Activity child = (Activity) treeNode;
                _GatherAllMilestones(ref list, child, aspectKey);
            }
        }

        private void Activity_GainedChild(ITreeNode<Activity> self, ITreeNode<Activity> subject) {
            if (m_ncaMode.Equals(NewChildAccomodationMode.AdjustParentToChildren)) {
                // for each Aspect, my start occurs at-or-before subject's start,
                //              and my finish occurs at-or-after subject's finish.
                List<object> missingAspectKeys = null;
                foreach (object aspectKey in m_timePeriods.Keys) {
                    if (subject.Payload.GetTimePeriodAspect(aspectKey) == null) {
                        missingAspectKeys = new List<object> {aspectKey};
                    } else {
                        m_timePeriods[aspectKey].AddRelationship(TimePeriod.Relationship.StartsBeforeStartOf, subject.Payload.GetTimePeriodAspect(aspectKey));
                        m_timePeriods[aspectKey].AddRelationship(TimePeriod.Relationship.EndsAfterEndOf, subject.Payload.GetTimePeriodAspect(aspectKey));
                    }
                    if (missingAspectKeys != null) {
                        string msg = string.Format("Adding a child activity without time periods with matching aspects to a parent that is set to adjust " +
                            "itself to its children is illegal. You are adding a child without a time period corresponding to the parent's {0} aspect{1}.",
                            StringOperations.ToCommasAndAndedList(missingAspectKeys, n=>n.ToString()),
                            (missingAspectKeys.Count > 1?"s":""));
                        throw new ApplicationException(msg);
                    }
                }
            }

        }

        private void Activity_LostChild(ITreeNode<Activity> self, ITreeNode<Activity> subject) {
            if (m_ncaMode.Equals(NewChildAccomodationMode.AdjustParentToChildren)) {
                foreach (object aspectKey in m_timePeriods.Keys) {
                    m_timePeriods[aspectKey].RemoveRelationship(TimePeriod.Relationship.StartsBeforeStartOf, subject.Payload.GetTimePeriodAspect(aspectKey));
                    m_timePeriods[aspectKey].RemoveRelationship(TimePeriod.Relationship.EndsAfterEndOf, subject.Payload.GetTimePeriodAspect(aspectKey));
                }
            }
        }

#region IHasTimePeriodAspects Members

        public ITimePeriod GetTimePeriodAspect(object aspectKey) {
            if (m_timePeriods.ContainsKey(aspectKey)) {
                return m_timePeriods[aspectKey];
            } else {
                return null;
            }
        }

#endregion

#region IModelObject Members

	    public string Name { get; }
	    public Guid Guid { get; }

	    /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model { get; } = null;

	    //private string m_description;
		public string Description => "";

#endregion

	}
}
#endif