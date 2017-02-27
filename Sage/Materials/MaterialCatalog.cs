/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using Highpoint.Sage.Persistence;

namespace Highpoint.Sage.Materials.Chemistry
{

    /// <summary>
    /// Class MaterialCatalog stands alone, or serves as a base class for any object that manages instances of <see cref="MaterialType"/>. 
    /// In this case, to &quot;manage&quot; means to be a point of focus to supply a requester with a material type
    /// that is specified by name ur unique ID (Guid). This is often kept at the model level in a model that 
    /// represents or contains chemical reactions.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Persistence.IXmlPersistable" />
    public class MaterialCatalog : IXmlPersistable
    {

        #region Private Fields

        private Hashtable m_materialTypesByName = new Hashtable();
        private Hashtable m_materialTypesByGuid = new Hashtable();

        #endregion

        /// <summary>
        /// Adds the specified <see cref="MaterialType"/> to this MaterialCatalog.
        /// </summary>
        /// <param name="mt">The MaterialType.</param>
        /// <exception cref="System.ApplicationException">
        /// SiteScheduleModelBuilder reports creating  + mt + , when there is already a material type,  + mtPre +  of the same name in the model.
        /// or
        /// SiteScheduleModelBuilder reports creating  + mt + , when there is already a material type,  + mtPre +  of the same guid in the model.
        /// </exception>
        public void Add(MaterialType mt)
        {
            if (m_materialTypesByName.ContainsKey(mt.Name))
            {
                MaterialType mtPre = (MaterialType) m_materialTypesByName[mt.Name];
                throw new ApplicationException("SiteScheduleModelBuilder reports creating " + mt +
                                               ", when there is already a material type, " + mtPre +
                                               " of the same name in the model.");
            }
            else
            {
                m_materialTypesByName.Add(mt.Name, mt);
            }

            if (m_materialTypesByGuid.Contains(mt.Guid))
            {
                MaterialType mtPre = (MaterialType) m_materialTypesByGuid[mt.Guid];
                throw new ApplicationException("SiteScheduleModelBuilder reports creating " + mt +
                                               ", when there is already a material type, " + mtPre +
                                               " of the same guid in the model.");
            }
            else
            {
                m_materialTypesByGuid.Add(mt.Guid, mt);
            }
        }

        /// <summary>
        /// Determines whether this MaterialCatalog contains the specified MaterialType. 
        /// Different instances of Material Types are considered equal if they have the same name and guid.
        /// </summary>
        /// <param name="mt">The MaterialType instance.</param>
        /// <returns><c>true</c> if this MaterialCatalog contains the specified MaterialType; otherwise, <c>false</c>.</returns>
        public bool Contains(MaterialType mt)
        {
            return (m_materialTypesByGuid.ContainsValue(mt) && m_materialTypesByName.ContainsValue(mt));
        }

        /// <summary>
        /// Determines whether this MaterialCatalog contains a MaterialType with the specified name.
        /// </summary>
        /// <param name="mtName">Name of the MaterialType.</param>
        /// <returns><c>true</c> if this MaterialCatalog contains a MaterialType with the specified name; otherwise, <c>false</c>.</returns>
        public bool Contains(string mtName)
        {
            return (m_materialTypesByName.ContainsKey(mtName));
        }

        /// <summary>
        /// Determines whether this MaterialCatalog contains a MaterialType with the specified Guid.
        /// </summary>
        /// <param name="mtGuid">The mt unique identifier.</param>
        /// <returns><c>true</c> if this MaterialCatalog contains a MaterialType with the specified Guid; otherwise, <c>false</c>.</returns>
        public bool Contains(Guid mtGuid)
        {
            return (m_materialTypesByGuid.ContainsKey(mtGuid));
        }

        /// <summary>
        /// Gets the <see cref="MaterialType"/> with the specified unique identifier.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>MaterialType.</returns>
        public MaterialType this[Guid guid] => (MaterialType) m_materialTypesByGuid[guid];

        /// <summary>
        /// Gets the <see cref="MaterialType"/> with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>MaterialType.</returns>
        public MaterialType this[string name] => (MaterialType) m_materialTypesByName[name];

        /// <summary>
        /// Gets the collection of material types contained in this MaterialCatalog.
        /// </summary>
        /// <value>The material types.</value>
        public ICollection MaterialTypes => m_materialTypesByName.Values;

        /// <summary>
        /// Clears this instance - removes all contained MaterialTypes.
        /// </summary>
        public void Clear()
        {
            m_materialTypesByGuid.Clear();
            m_materialTypesByName.Clear();
        }

        /// <summary>
        /// Removes the material type with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        public void Remove(string name)
        {
            Guid guid = ((MaterialType) m_materialTypesByName[name]).Guid;
            m_materialTypesByGuid.Remove(guid);
            m_materialTypesByName.Remove(name);
        }

        /// <summary>
        /// Removes the material type with the specified Guid.
        /// </summary>
        /// <param name="guid">The specified Guid.</param>
        public void Remove(Guid guid)
        {
            string name = ((MaterialType) m_materialTypesByName[guid]).Name;
            m_materialTypesByGuid.Remove(guid);
            m_materialTypesByName.Remove(name);
        }

        #region >>> Serialization Support <<< 

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("MaterialTypesByName", m_materialTypesByName);
            xmlsc.StoreObject("MaterialTypesByGuid", m_materialTypesByGuid);
        }

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            m_materialTypesByName = (Hashtable) xmlsc.LoadObject("MaterialTypesByName");
            m_materialTypesByGuid = (Hashtable) xmlsc.LoadObject("MaterialTypesByGuid");
        }

        #endregion

    }
}