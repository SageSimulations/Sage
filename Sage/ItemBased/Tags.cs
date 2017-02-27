/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Highpoint.Sage.ItemBased {

    /// <summary>
    /// A TagType contains metadata governing the use of Tags for a particular purpose. Tags can be
    /// constrained or not, extensible or not, and have a list of candidate values or not. 
    /// <b></b>Example 1: A tag named "LotID" would be unconstrained, and therefore, extensible. That
    /// is to say that the tag may hold any (string) value and therefore, any new value is acceptable.
    /// <b></b>Example 2: A tag might be of type "Rework", and be constrained to values "Yes" or "No",
    /// and not be extensible, i.e. with no provision for being able to add any other options.
    /// <b></b>Example 3: A tag might be of type "Flavor", and be constrained to "Chocolate", "Vanilla"
    /// and "Strawberry", but be extensible so that during execution, some dispatcher (or whatever) can
    /// add "Tutti-Frutti" to the list of acceptable values.
    /// <b></b>A TagType is used to create tags or its type.
    /// </summary>
    public interface ITagType {
        /// <summary>
        /// Gets the name of the tag type.
        /// </summary>
        /// <value>The name of the tag type.</value>
        string TypeName { get; }
        /// <summary>
        /// Gets the value candidates list for this tag type. If the tag type is unconstrained, it returns null.
        /// </summary>
        /// <value>The value candidates.</value>
        ReadOnlyCollection<string> ValueCandidates { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is constrained to a specific set of candidate values.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is constrained; otherwise, <c>false</c>.
        /// </value>
        bool isConstrained { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is extensible. An unconstrained tag type is by definition extensible.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is extensible; otherwise, <c>false</c>.
        /// </value>
        bool isExtensible { get; }
        /// <summary>
        /// Adds the value to the list of candidate values that tags of this type may take on. This will return false if the
        /// Tag Type is either not extensible, or not constrained.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if successful, false if the Tag Type is either not extensible, or not constrained.</returns>
        bool AddValueCandidate(string value);
        /// <summary>
        /// Creates a new tag of this type, with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        /// <returns>A new Tag.</returns>
        ITag CreateTag(string initialValue);
    }

    /// <summary>
    /// Implemented by an object that changes the values of the tags on service objects it handles.
    /// </summary>
    public interface IChangesTagsOnServiceObjects {
        /// <summary>
        /// Gets a list of the tag types that can be changed on service objects.
        /// </summary>
        /// <value>The tag types affected.</value>
        List<ITagType> TagTypesAffected { get; }
    }

    /// <summary>
    /// Implemented by an object that adds tags to service objects it handles.
    /// </summary>
    public interface IAddsTagsToServiceObjects {
        /// <summary>
        /// Gets a list of the tag types that can be added to service objects.
        /// </summary>
        /// <value>The tag types affected.</value>
        List<ITagType> TagTypesAffected { get; }
    }

    /// <summary>
    /// Implemented by an object (usually a service item) that has tags attached.
    /// </summary>
    public interface ITagHolder {
        /// <summary>
        /// Gets the tags held by this service item.
        /// </summary>
        /// <value>The tags.</value>
        TagList Tags { get; }
    }

    /// <summary>
    /// A list of tags.
    /// </summary>
    public class TagList : List<ITag> {

        /// <summary>
        /// Filters the list on those tags with the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A sublist consisting only of tags with the specified type.</returns>
        public List<ITag> FilterOn(ITagType type) {
            return FindAll(delegate(ITag tag) { return tag.TagType.Equals(type); });
        }

        /// <summary>
        /// Filters the list on those tags with the specified type.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>
        /// A sublist consisting only of tags with the specified type name.
        /// </returns>
        public List<ITag> FilterOn(string typeName) {
            return FindAll(delegate(ITag tag) { return tag.TagType.TypeName.Equals(typeName); });
        }

        /// <summary>
        /// Gets the <see cref="Highpoint.Sage.ItemBased.ITag"/> with the specified name.
        /// </summary>
        /// <value></value>
        public ITag this[string name] {
            get {
                return Find(delegate(ITag tag) { return tag.Name.Equals(name); });
            }
        }
    }

    /// <summary>
    /// A read only Tag.
    /// </summary>
    public interface IReadOnlyTag {
        ITagType TagType { get; }
        string Name { get; }
        string Value { get; }
    }
    /// <summary>
    /// A tag that can be read and written.
    /// </summary>
    public interface ITag : IReadOnlyTag {
        bool SetValue(string newValue);
    }

    /// <summary>
    /// This is a holder class for access to IComparers that can be used to sort tags and TagHolders in their lists.
    /// </summary>
    static class TagComparers {
        /// <summary>
        /// Returns an IComparer that compares objects that implement IHasTags, where the comparison is done against
        /// a specifically-named tag.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns></returns>
        public static IComparer<ITagHolder> TagsByValue(string tagName) {
            return new _TagsByValue(tagName);
        }

        class _TagsByValue : IComparer<ITagHolder> {
            private string m_tagName = null;
            public _TagsByValue(string tagName) {
                m_tagName = tagName;
            }
            #region IComparer<IHasTags> Members

            public int Compare(ITagHolder x, ITagHolder y) {
                string s1 = x.Tags[m_tagName].Value;
                string s2 = y.Tags[m_tagName].Value;
                if (s1 == null && s2 == null)
                    return 0;
                if (s1 == null)
                    return 1;
                if (s2 == null) {
                    return -1;
                }
                return s1.CompareTo(s2);
            }

            #endregion
        }
    }

    /// <summary>
    /// A TagType contains metadata governing the use of Tags for a particular purpose. Tags can be
    /// constrained or not, extensible or not, and have a list of candidate values or not. 
    /// <b></b>Example 1: A tag named "LotID" would be unconstrained, and therefore, extensible. That
    /// is to say that the tag may hold any (string) value and therefore, any new value is acceptable.
    /// <b></b>Example 2: A tag might be of type "Rework", and be constrained to values "Yes" or "No",
    /// and not be extensible, i.e. with no provision for being able to add any other options.
    /// <b></b>Example 3: A tag might be of type "Flavor", and be constrained to "Chocolate", "Vanilla"
    /// and "Strawberry", but be extensible so that during execution, some dispatcher (or whatever) can
    /// add "Tutti-Frutti" to the list of acceptable values.
    /// <b></b>A TagType is used to create tags or its type.
    /// </summary>
    class TagType : ITagType {

        #region Private Fields
        private string m_typeName;
        private List<string> m_values;
        private bool m_extensible;
        private bool m_constrained;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TagType"/> class.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        public TagType(string typeName) {
            m_typeName = typeName;
            m_values = new List<string>();
            m_extensible = true;
            m_constrained = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagType"/> class.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="extensible">if set to <c>true</c> [extensible].</param>
        /// <param name="values">The values.</param>
        public TagType(string typeName, bool extensible, params string[] values) {
            m_values = new List<string>(values);
            m_constrained = true;
            m_extensible = extensible;
        }

        #region ITagType Members

        /// <summary>
        /// Gets the name of the tag type.
        /// </summary>
        /// <value>The name of the tag type.</value>
        public string TypeName {
            get { return m_typeName; }
        }

        /// <summary>
        /// Gets the value candidates list for this tag type. If the tag type is unconstrained, it returns null.
        /// </summary>
        /// <value>The value candidates.</value>
        public ReadOnlyCollection<string> ValueCandidates {
            get {
                if (m_constrained) {
                    return m_values.AsReadOnly();
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is constrained to a specific set of candidate values.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is constrained; otherwise, <c>false</c>.
        /// </value>
        public bool isConstrained {
            get {
                return m_constrained;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is extensible. An unconstrained tag type is by definition extensible.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is extensible; otherwise, <c>false</c>.
        /// </value>
        public bool isExtensible {
            get {
                return (!m_constrained) || m_extensible;
            }
        }

        /// <summary>
        /// Adds the value to the list of candidate values that tags of this type may take on. This will return false if the
        /// Tag Type is either not extensible, or not constrained.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns></returns>
        public bool AddValueCandidate(string value) {
            if (!m_constrained)
                return false;

            if (!m_extensible)
                return false;

            m_values.Add(value);
            return true;
        }

        /// <summary>
        /// Creates a new tag of this type, with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        /// <returns>A new Tag.</returns>
        public ITag CreateTag(string initialValue) {
            if ( isConstrained ) {
                if ( !ValueCandidates.Contains(initialValue) ){
                    if ( isExtensible ) {
                        AddValueCandidate(initialValue);
                    } else {
                        string errMsg = string.Format("Attempting to create an instance of Tag Type {0} with initial value {1}, but it can only contain values of {2}, and is not extensible.",
                            TypeName, initialValue, Utility.StringOperations.ToCommasAndAndedList(m_values));
                        throw new ApplicationException(errMsg);
                    }
                }
            }

            Tag retval = new Tag(this);
            retval.SetValue(initialValue);
            return retval;
        }

        #endregion
    }

    class Tag : ITag {
        ITagType m_tagType;
        string m_value = "";

        public Tag(TagType tagType) {
            m_tagType = tagType;
            if (m_tagType.isConstrained) {
                m_value = m_tagType.ValueCandidates[0];
            }
        }
        #region ITag Members

        public bool SetValue(string newValue) {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IReadOnlyTag Members

        public ITagType TagType {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string Name {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string Value {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion
    }
}
