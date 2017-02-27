/* This source code licensed under the GNU Affero General Public License */

using System;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs.PFC {

    public abstract class PfcElement : IPfcElement {

        #region Private Fields
        private IModel m_model;
        private string m_name;
        private string m_description;
        private Guid m_guid;
        private object m_userData;
        private IProcedureFunctionChart m_parent = null;
        private Guid m_seid;
        #endregion Private Fields

        protected Predicate<PfcElement> StepsOnly = new Predicate<PfcElement>(delegate(PfcElement element) { return element.ElementType.Equals(PfcElementType.Step); });
        protected Predicate<PfcElement> TransOnly = new Predicate<PfcElement>(delegate(PfcElement element) { return element.ElementType.Equals(PfcElementType.Transition); });
        protected Predicate<PfcElement> LinksOnly = new Predicate<PfcElement>(delegate(PfcElement element) { return element.ElementType.Equals(PfcElementType.Link); });

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcElement"/> class.
        /// </summary>
        public PfcElement() : this(null, "", "", Guid.NewGuid()) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcElement"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name of this node.</param>
        /// <param name="description">The description for this node.</param>
        /// <param name="guid">The GUID of this node.</param>
        public PfcElement(IProcedureFunctionChart parent, string name, string description, Guid guid) {
            m_parent = parent;
            InitializeIdentity(parent.Model, name, description, guid);
            m_userData = null;
            IMOHelper.RegisterWithModel(this);
        }

        #endregion Constructors

        #region IResettable Members

        public abstract void Reset();

        #endregion

        #region IPfcElement Members
        /// <summary>
        /// Sets the name of this step node to the new value.
        /// </summary>
        /// <param name="newName">The new name.</param>
        public void SetName(string newName) {
            
            if (m_name == newName) {
                return;
            }

            if (Parent.ParticipantDirectory.Contains(newName)) {
                string msg = string.Format("Trying to set a {0} name from \"{1}\" to \"{2}\" - but the name \"{2}\" is already in use in this PFC.",
                    ElementType, Name, newName, Parent.ParticipantDirectory[newName].Type);

                throw new ApplicationException(msg);
            }

            string oldName = m_name;
            m_name = newName;

            if (Parent != null && Parent.ParticipantDirectory.Contains(m_guid)) {
                oldName = ((Expressions.DualModeString)Parent.ParticipantDirectory[m_guid]).Name;
                Parent.ParticipantDirectory.ChangeName(oldName, newName);
            }
        }

        /// <summary>
        /// Determines whether this instance is connected to anything upstream or downstream.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsConnected();

        /// <summary>
        /// The parent ProcedureFunctionChart of this node.
        /// </summary>
        /// <value></value>
        public IProcedureFunctionChart Parent {
            get {
                return m_parent;
            }
            set {
                m_parent = value;
            }
        }

        /// <summary>
        /// Gets the SEID, or Source Element ID of this element. If the PFC of which
        /// this element is a member is cloned, then this SEID will be the Guid of the element
        /// in the source PFC that is semantically/structurally equivalent to this one.
        /// </summary>
        /// <value>The SEID.</value>
        public Guid SEID {
            get { return m_seid; }
            internal set { m_seid = value; }
        }
     
        /// <summary>
        /// Gets the type of this element.
        /// </summary>
        /// <value>The type of the element.</value>
        public abstract PfcElementType ElementType { get; }

        /// <summary>
        /// Updates the portion of the structure of the SFC that relates to this element.
        /// This is called after any structural changes in the Sfc, but before the resultant data
        /// are requested externally.
        /// </summary>
        public abstract void UpdateStructure();

        /// <summary>
        /// Gets or sets some piece of arbitrary user data. This data is (currently) not serialized.
        /// </summary>
        /// <value>The user data.</value>
        public object UserData { get { return m_userData; } set { m_userData = value; } }

        #endregion

        #region Implementation of IModelObject

        /// <summary>
        /// The model that owns this SfcStep, or from which it gets time, etc. data.
        /// </summary>
        /// <value></value>
        public IModel Model { [System.Diagnostics.DebuggerStepThrough] get { return m_model; } }
        /// <summary>
        /// The user-friendly name for this SfcStep. Required to be unique if there is a Participant directory listing
        /// this element - which there will be, if the element is attached to a Pfc.
        /// </summary>
        /// <value></value>
        public string Name { [System.Diagnostics.DebuggerStepThrough] get { return m_name; } }
        /// <summary>
        /// The Guid for this SfcStep. Typically required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid { [System.Diagnostics.DebuggerStepThrough] get { return m_guid; } }
        /// <summary>
        /// The description for this SfcStep. Typically used for human-readable representations.
        /// </summary>
        /// <value>The SfcStep's description.</value>
        public string Description { [System.Diagnostics.DebuggerStepThrough] get { return m_description; } }

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model in which the task runs.</param>
        /// <param name="name">The name of the task.</param>
        /// <param name="description">The description of the task.</param>
        /// <param name="guid">The GUID of the task.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }

        #endregion

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() {
            return GetType().Name + " : " + Name + " {" + Guid + "} ";
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode() {
            return m_guid.GetHashCode();
        }
    }
}
