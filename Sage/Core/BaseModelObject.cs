/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.SimCore
{
    public class BaseModelObject : IModelObject
    {
        public BaseModelObject(IModel model, string name, Guid guid, string description = "")
        {
            InitializeIdentity(model, name, description, guid);
            model.AddModelObject(this);
        }

        #region Implementation of IModelObject
        private string m_name = null;
        private Guid m_guid = Guid.Empty;
        private IModel m_model = null;
        private string m_description = null;

        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model { [System.Diagnostics.DebuggerStepThrough] get { return m_model; } }

        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name { [System.Diagnostics.DebuggerStepThrough]get { return m_name; } }

        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
        public string Description { [System.Diagnostics.DebuggerStepThrough] get { return (m_description ?? ("No description for " + m_name)); } }

        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid { [System.Diagnostics.DebuggerStepThrough] get { return m_guid; } }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The IModelObject's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }
        #endregion

    }
}
