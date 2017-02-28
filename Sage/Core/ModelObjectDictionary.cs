/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.Utility;
using System.Collections.Generic;

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// An event that notifies listeners of something happening to an IModelObject.
    /// </summary>
    /// <param name="imo">The IModelObject of interest.</param>
    public delegate void ModelObjectEvent(IModelObject imo);

    /// <summary>
    /// An event that notifies listeners of something happening related to a specified Guid.
    /// </summary>
    /// <param name="key">The Guid of interest.</param>
    public delegate void GuidEvent(Guid key);

    /// <summary>
    /// A ModelObjectDictionary is a passive directory of objects owned by this model. It not 
    /// intended to be the primary holder of references, but rather to be a navigation aid 
    /// among objects that already "belong" to the model, and that are removed once the model
    /// no longer wants/needs them. As long as someone, somewhere (presumably a hierarchy owned
    /// somehow &amp; somewhere in the model) has a reference to all useful IModelObjects, they
    /// will be available in this dictionary. However, in order to clean out this dictionary 
    /// automatically as objects are no longer in use in the model, it uses a WeakReference
    /// to track the object. Thus, as soon as no one else has a reference to the object, it 
    /// will be cleaned out of this dictionary as soon as the next Garbage Collection sweep.
    /// </summary>
    public class ModelObjectDictionary : IDictionary {

        #region private fields
        private IDictionary m_dictionary;
        private static bool _bDumpedBanner = false;
        private static Guid _dumpGuid = Guid.Empty;
        #endregion

        /// <summary>
        /// ModelObjectDictionary is a dictionary that keeps references to all ModelObjects
        /// and can retrieve those references based on the ModelObjects' Guids. It fires an
        /// event when asked to retrieve an object that it does not have already stored.
        /// </summary>
        public ModelObjectDictionary() : this(false) { }

        /// <summary>
        /// ModelObjectDictionary is a dictionary that keeps references to all ModelObjects
        /// and can retrieve those references based on the ModelObjects' Guids. It fires an
        /// event when asked to retrieve an object that it does not have already stored.
        /// </summary>
        /// <param name="retainHardReference">if set to <c>true</c> the dictionary uses a
        /// Hashtable instead of a WeakHashtable. This means that this dictionary can be used
        /// to maintain connections to the objects it contains, but it also means that the
        /// developer is responsible for explicitly removing objects from this dictionaary if
        /// they are no longer desired.</param>
        public ModelObjectDictionary(bool retainHardReference) {
            if (retainHardReference) {
                m_dictionary = new Hashtable();
            } else {
                m_dictionary = new WeakHashtable();
            }
        }

        /// <summary>
        /// Deletes the object in this dictionary with the specified key, replacing it
        /// with the new entry.
        /// </summary>
        /// <param name="keyForCurrentEntry">The key for current entry.</param>
        /// <param name="newEntryToReplaceIt">The new entry to replace it.</param>
        private void Delete(Guid keyForCurrentEntry, IModelObject newEntryToReplaceIt) {
            IHasIdentity imo = (IHasIdentity)m_dictionary[keyForCurrentEntry];
            m_dictionary.Remove(keyForCurrentEntry);
            string oldOne = imo.Name;
            string newOne = ( (IHasIdentity)newEntryToReplaceIt ).Name;
            // TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
            if (!_bDumpedBanner || imo.Guid.Equals(_dumpGuid)) {
                _dumpGuid = imo.Guid;
                _Debug.WriteLine(s_banner_Message);
                Exception e = new Exception();
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
                _Debug.WriteLine("ModelObjectDictionary just removed " + oldOne + ", under Guid "
                    + keyForCurrentEntry + " to make way for " + newOne + " under Guid "
                    + ( (IHasIdentity)newEntryToReplaceIt ).Guid + ".\r\n"
                    + "\tThe offending code was at:" + st);
                _bDumpedBanner = true;
            }
        }

        /// <summary>
        /// Retrieves all model objects that satisfy the predicate.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        public IEnumerable<IModelObject> FindAll(Predicate<IModelObject> predicate) {
            foreach (IModelObject obj in m_dictionary.Values) {
                if (predicate(obj)) {
                    yield return obj;
                }
            }
        }

        /// <summary>
        /// Retrieves a depth-first iterator over all nodes in this PFC that satisfy the predicate.
        /// </summary>
        /// <param name="mustbeExactTypeMatch">if set to <c>true</c>, a returned IModelObject mustbe exact type match to the provided type.</param>
        /// <returns></returns>
        public IEnumerable<T> FindByType<T>(bool mustbeExactTypeMatch) {
            foreach (IModelObject obj in m_dictionary.Values) {
                if (( mustbeExactTypeMatch && typeof(T).Equals(obj.GetType()) ) || typeof(T).IsAssignableFrom(obj.GetType())) {
                    yield return (T)obj;
                }
            }
        }

        /// <summary>
        /// Fired when a model object is added to this ModelObjectDictionary
        /// </summary>
        public event ModelObjectEvent NewModelObjectAdded;

        /// <summary>
        /// Fired when a model object is removed from this ModelObjectDictionary
        /// </summary>
        public event ModelObjectEvent ExistingModelObjectRemoved;

        /// <summary>
        /// This event is fired any time someone asks for a model object, and the
        /// ModelObjectDictionary does not have a record of such an object.
        /// </summary>
        public event GuidEvent UnknownModelObjectRequested;

        #region IDictionary Members

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object is read-only; otherwise, false.</returns>
        public bool IsReadOnly {
            get {
                return m_dictionary.IsReadOnly;
            }
        }

        /// <summary>
        /// Returns an <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionaryEnumerator"></see> object for the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionaryEnumerator"></see> object for the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </returns>
        public IDictionaryEnumerator GetEnumerator() {
            return m_dictionary.GetEnumerator();
        }

        /// <summary>
        /// Retrieves the ModelObject whose key is the specified Guid. Guid.Empty is considered
        /// an explicit request for a null return, and adding an object with Guid.Empty as the
        /// key is considered illegal. All other Guids, if not contained in the dictionary, will
        /// result in the firing of the UnknownModelObjectRequested event.
        /// </summary>
        public IModelObject this[Guid key] {
            get {
                object obj = m_dictionary[key];
                if (obj == null && !( (Guid)key ).Equals(Guid.Empty) && UnknownModelObjectRequested != null) {
                    UnknownModelObjectRequested((Guid)key);
                }
                return (IModelObject)obj;
            }
            set {
                if (m_dictionary.Contains(key))
                    Delete(key, value);
                Add(key, value);
            }
        }

        /// <summary>
        /// Retrieves the ModelObject whose key is the specified Guid. Guid.Empty is considered
        /// an explicit request for a null return, and adding an object with Guid.Empty as the
        /// key is considered illegal. All other Guids, if not contained in the dictionary, will
        /// result in the firing of the UnknownModelObjectRequested event.
        /// </summary>
        public object this[object key] {
            get {
                _Debug.Assert(key is Guid);
                return this[(Guid)key];
            }
            set {
                _Debug.Assert(key is Guid);
                _Debug.Assert(value is IModelObject);
                this[(Guid)key] = (IModelObject)value;
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object is read-only.-or- The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> has a fixed size. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        public void Remove(Guid key) {
            if (m_dictionary.Contains(key)) {
                IModelObject imo = (IModelObject)m_dictionary[key];
                m_dictionary.Remove(key);
                if (ExistingModelObjectRemoved != null) {
                    ExistingModelObjectRemoved(imo);
                }
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object is read-only.-or- The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> has a fixed size. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        public void Remove(object key) {
            _Debug.Assert(key is Guid);
            Remove((Guid)key);
        }

        /// <summary>
        /// Determines whether the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.</param>
        /// <returns>
        /// true if the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        public bool Contains(Guid key) {
            return m_dictionary.Contains(key);
        }

        /// <summary>
        /// Determines whether the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.</param>
        /// <returns>
        /// true if the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        public bool Contains(object key) {
            _Debug.Assert(key is Guid);
            return Contains((Guid)key);
        }

        /// <summary>
        /// Removes all elements from the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object is read-only. </exception>
        public void Clear() {
            m_dictionary.Clear();
        }

        /// <summary>
        /// Gets an <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object containing the values in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object containing the values in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.</returns>
        public ICollection Values {
            get {
                return m_dictionary.Values;
            }
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <param name="key">The <see cref="T:System.Object"></see> to use as the key of the element to add.</param>
        /// <param name="value">The <see cref="T:System.Object"></see> to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> is read-only.-or- The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> has a fixed size. </exception>
        public void Add(Guid key, IModelObject value) {
            if (m_dictionary.Contains(key)) {
                Delete(key, value);
            }
            m_dictionary.Add(key, value);
            if (NewModelObjectAdded != null)
                NewModelObjectAdded((IModelObject)value);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <param name="key">The <see cref="T:System.Object"></see> to use as the key of the element to add.</param>
        /// <param name="value">The <see cref="T:System.Object"></see> to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object. </exception>
        /// <exception cref="T:System.ArgumentNullException">key is null. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> is read-only.-or- The <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> has a fixed size. </exception>
        public void Add(object key, object value) {
            _Debug.Assert(key is Guid);
            _Debug.Assert(value is IModelObject);
            Add((Guid)key, (IModelObject)value);

        }

        /// <summary>
        /// Gets an <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object containing the keys of the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object containing the keys of the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object.</returns>
        public ICollection Keys {
            get {
                return m_dictionary.Keys;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object has a fixed size; otherwise, false.</returns>
        public bool IsFixedSize {
            get {
                return m_dictionary.IsFixedSize;
            }
        }

        #endregion

        #region ICollection Members

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> is synchronized (thread safe); otherwise, false.</returns>
        public bool IsSynchronized { get { return m_dictionary.IsSynchronized; } }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>The number of elements contained in the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see>.</returns>
        public int Count { get { return m_dictionary.Count; } }

        /// <summary>
        /// Copies the elements of the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> to an <see cref="T:System.Array"></see>, starting at a particular <see cref="T:System.Array"></see> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"></see> that is the destination of the elements copied from <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see>. The <see cref="T:System.Array"></see> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">array is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">array is multidimensional.-or- index is equal to or greater than the length of array.-or- The number of elements in the source <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> is greater than the available space from index to the end of the destination array. </exception>
        /// <exception cref="T:System.InvalidCastException">The type of the source <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see> cannot be cast automatically to the type of the destination array. </exception>
        public void CopyTo(Array array, int index) { m_dictionary.CopyTo(array, index); }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:Highpoint.Sage.SimCore.ModelObjectDictionary"></see>.</returns>
        public object SyncRoot { get { return m_dictionary.SyncRoot; } }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="Highpoint.Sage.SimCore.ModelObjectDictionary"></see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return m_dictionary.GetEnumerator();
        }

        #endregion

        private static readonly string s_banner_Message =
@"
***********************************************************************************
* Warning - there are objects being created and added to the model that have the  *
* same GUID identifiers as other objects that have already been added - this is   *
* a problem - the Model's ModelObjectDictionary will have no record of any but    *
* the last object registered under a particular GUID. This needs to be fixed as   *
* soon as possible. We will report only on the first time this happens, but there *
* are probably more. Following is a stack trace dump of the first occurrence.     *
***********************************************************************************";

    }
}