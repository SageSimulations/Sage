/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// The EventedList class provides all of the standard List capabilities as well as the ability to
    /// emit events when the list changes its contents for whatever reason.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class EventedList<T> : IList<T> {

        // TODO: safeguard against possible multiple enumeration of IEnumerable.

        private readonly List<T> m_base;

        /// <summary>
        /// Signature of events that pertain only to an EventedList.
        /// </summary>
        /// <param name="list">The list.</param>
        public delegate void ListEvent(EventedList<T> list);

        /// <summary>
        /// Signature of events that pertain to an EventedList and one of its items.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="item"></param>
        public delegate void ItemEvent(EventedList<T> list, T item);

        /// <summary>
        /// Signature of events that pertain to an EventedList an old item and a new item - preesumably items added to, or removed from, that list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="oldItem"></param>
        /// <param name="newItem"></param>
        public delegate void ItemsEvent(EventedList<T> list, T oldItem, T newItem);

        /// <summary>
        /// Signature of events that pertain to an EventedList and a collection of items related to that list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="collection"></param>
        public delegate void CollectionEvent(EventedList<T> list, IEnumerable<T> collection);

        /// <summary>
        /// Signature of events that pertain only to an EventedList and a predicate to be applied to items in that list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="match"></param>
        public delegate void PredicateEvent(EventedList<T> list, Predicate<T> match);

        /// <summary>
        /// Signature of events that pertain to an EventedList and a numeric range of entries in that list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="startIndex">The start index of the range.</param>
        /// <param name="count">The number of entries in the range.</param>
        public delegate void RangeEvent(EventedList<T> list, int startIndex, int count);

        /// <summary>
        /// Fired when the list is about to add an item.
        /// </summary>
        public event ItemEvent AboutToAddItem;
        /// <summary>
        /// Fired when the list has just added an item.
        /// </summary>
        public event ItemEvent AddedItem;

        /// <summary>
        /// Fired when the list is about to remove an item.
        /// </summary>
        public event ItemEvent AboutToRemoveItem;
        /// <summary>
        /// Fired when the list has just removed an item.
        /// </summary>
        public event ItemEvent RemovedItem;

        /// <summary>
        /// Fired when the list is about to replace one item with another.
        /// </summary>
        public event ItemsEvent AboutToReplaceItem;
        /// <summary>
        /// Fired when the list has just replaced one item with another.
        /// </summary>
        public event ItemsEvent ReplacedItem;

        /// <summary>
        /// Fired when the list is about to add some items.
        /// </summary>
        public event CollectionEvent AboutToAddItems;
        /// <summary>
        /// Fired when the list has just added some items.
        /// </summary>
        public event CollectionEvent AddedItems;

        /// <summary>
        /// Fired when the list is about to remove some of its items.
        /// </summary>
        public event PredicateEvent AboutToRemoveItems;
        /// <summary>
        /// Fired when the list has just had some of its items removed.
        /// </summary>
        public event PredicateEvent RemovedItems;

        /// <summary>
        /// Fired when the list is about to remove a range of elements.
        /// </summary>
        public event RangeEvent AboutToRemoveRange;
        /// <summary>
        /// Fired when the list has just had a range of elements removed.
        /// </summary>
        public event RangeEvent RemovedRange;

        /// <summary>
        /// Fired when the list is about to be cleared of all of its members.
        /// </summary>
        public event ListEvent AboutToClear;
        /// <summary>
        /// Fired when the list has just been cleared.
        /// </summary>
        public event ListEvent Cleared;

        /// <summary>
        /// Fired when the list has just had its contents changed.
        /// </summary>
        public event ListEvent ContentsChanged;

        /// <summary>
        ///     Initializes a new instance of the System.Collections.Generic.List&lt;T&gt; class
        ///     that is empty and has the default initial capacity.
        /// </summary>
        public EventedList() {
            m_base = new List<T>();
        }

        /// Summary:
        ///     Initializes a new instance of the System.Collections.Generic.List&lt;T&gt; class
        ///     that is empty and has the specified initial capacity.
        ///
        /// Parameters:
        ///   capacity:
        ///     The number of elements that the new list can initially store.
        ///
        /// <summary>
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     capacity is less than 0.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public EventedList(int capacity) {
            m_base = new List<T>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventedList&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exception cref="T:System.ArgumentNullException">collection is null.</exception>
        public EventedList(IEnumerable<T> collection){
            m_base = new List<T>(collection);
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="T:System.Collections.Generic.List`1"></see>.
        /// </summary>
        /// <param name="item">The object to be added to the end of the <see cref="T:System.Collections.Generic.List`1"></see>. The value can be null for reference types.</param>
        public void Add(T item) {
            AboutToAddItem?.Invoke(this, item);
            m_base.Add(item);
            AddedItem?.Invoke(this, item);
            ContentsChanged?.Invoke(this);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="T:System.Collections.Generic.List`1"></see>.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the <see cref="T:System.Collections.Generic.List`1"></see>. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        /// <exception cref="T:System.ArgumentNullException">collection is null.</exception>
        public void AddRange(IEnumerable<T> collection) {
            AboutToAddItems?.Invoke(this, collection);

            m_base.AddRange(collection);

            AddedItems?.Invoke(this, collection);

            ContentsChanged?.Invoke(this);
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        public bool Remove(T item) {
            AboutToRemoveItem?.Invoke(this, item);

            bool retval = m_base.Remove(item);

            RemovedItem?.Invoke(this, item);
            ContentsChanged?.Invoke(this);

            return retval;
        }

        /// <summary>
        /// Removes all.
        /// </summary>
        /// <param name="match">The match.</param>
        public void RemoveAll(Predicate<T> match) {
            AboutToRemoveItems?.Invoke(this, match);
            m_base.RemoveAll(match);
            RemovedItems?.Invoke(this, match);
            ContentsChanged?.Invoke(this);
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="T:System.Collections.Generic.List`1"></see>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than 0.-or-index is equal to or greater than <see cref="P:System.Collections.Generic.List`1.Count"></see>.</exception>
        public void RemoveAt(int index) {
            T item = m_base[index];
            AboutToRemoveItem?.Invoke(this, item);
            m_base.RemoveAt(index);
            RemovedItem?.Invoke(this, item);
            ContentsChanged?.Invoke(this);
        }

        /// <summary>
        /// Removes a range of elements from the <see cref="T:System.Collections.Generic.List`1"></see>.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than 0.-or-count is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">index and count do not denote a valid range of elements in the <see cref="T:System.Collections.Generic.List`1"></see>.</exception>
        public void RemoveRange(int index, int count) {
            AboutToRemoveRange?.Invoke(this, index, count);

            m_base.RemoveRange(index, count);

            RemovedRange?.Invoke(this, index, count);

            ContentsChanged?.Invoke(this);
        }

        /// <summary>
        /// Removes all elements from the <see cref="T:System.Collections.Generic.List`1"></see>.
        /// </summary>
        public void Clear() {
            AboutToClear?.Invoke(this);

            m_base.Clear();

            Cleared?.Invoke(this);

            ContentsChanged?.Invoke(this);
        }

        /// <summary>
        /// Gets or sets the &lt;T&gt; at the specified index.
        /// Parameters:
        ///   index:
        ///     The zero-based index of the element to get or set.
        ///
        /// Returns:
        ///     The element at the specified index.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     index is less than 0.-or-index is equal to or greater than System.Collections.Generic.List&lt;T&gt;.Count.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public T this[int index] {
            get {
                return m_base[index];
            }
            set {
                T old = m_base[index];
                AboutToReplaceItem?.Invoke(this, old, value);
                m_base[index] = value;
                ReplacedItem?.Invoke(this, old, value);
                ContentsChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Inserts an element into the <see cref="T:System.Collections.Generic.List`1"></see> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert. The value can be null for reference types.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than 0.-or-index is greater than <see cref="P:System.Collections.Generic.List`1.Count"></see>.</exception>
        public void Insert(int index, T item) {
            AboutToAddItem?.Invoke(this, item);
            m_base.Insert(index, item);
            AddedItem?.Invoke(this, item);
            ContentsChanged?.Invoke(this);
        }

        /// <summary>
        /// Inserts the elements of a collection into the <see cref="T:System.Collections.Generic.List`1"></see> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="collection">The collection whose elements should be inserted into the <see cref="T:System.Collections.Generic.List`1"></see>. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than 0.-or-index is greater than <see cref="P:System.Collections.Generic.List`1.Count"></see>.</exception>
        /// <exception cref="T:System.ArgumentNullException">collection is null.</exception>
        public void InsertRange(int index, IEnumerable<T> collection) {
            AboutToAddItems?.Invoke(this, collection);

            m_base.InsertRange(index, collection);

            AddedItems?.Invoke(this, collection);
            ContentsChanged?.Invoke(this);
        }

        public bool IsReadOnly => false;

        /// Summary:
        ///     Gets or sets the total number of elements the internal data structure can
        ///     hold without resizing.
        ///
        /// Returns:
        ///     The number of elements that the System.Collections.Generic.List&lt;T&gt; can contain
        ///     before resizing is required.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     System.Collections.Generic.List&lt;T&gt;.Capacity is set to a value that is less
        ///     than System.Collections.Generic.List&lt;T&gt;.Count.
        public int Capacity { get { return m_base.Capacity; } set { m_base.Capacity = value; } }
        ///
        /// Summary:
        ///     Gets the number of elements actually contained in the System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Returns:
        ///     The number of elements actually contained in the System.Collections.Generic.List&lt;T&gt;.
        public int Count => m_base.Count;

        ///
        /// Summary:
        ///     Returns a read-only System.Collections.Generic.IList&lt;T&gt; wrapper for the current
        ///     collection.
        ///
        /// Returns:
        ///     A System.Collections.Generic.ReadOnlyCollection`1 that acts as a read-only
        ///     wrapper around the current System.Collections.Generic.List&lt;T&gt;.
        public System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly() { return m_base.AsReadOnly(); }
        ///
        /// Summary:
        ///     Searches the entire sorted System.Collections.Generic.List&lt;T&gt; for an element
        ///     using the default comparer and returns the zero-based index of the element.
        ///
        /// Parameters:
        ///   item:
        ///     The object to locate. The value can be null for reference types.
        ///
        /// Returns:
        ///     The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
        ///     if item is found; otherwise, a negative number that is the bitwise complement
        ///     of the index of the next element that is larger than item or, if there is
        ///     no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
        ///
        /// Exceptions:
        ///   System.InvalidOperationException:
        ///     The default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default cannot
        ///     find an implementation of the System.IComparable&lt;T&gt; generic interface or
        ///     the System.IComparable interface for type T.
        public int BinarySearch(T item) { return m_base.BinarySearch(item); }
        ///
        /// Summary:
        ///     Searches the entire sorted System.Collections.Generic.List&lt;T&gt; for an element
        ///     using the specified comparer and returns the zero-based index of the element.
        ///
        /// Parameters:
        ///   item:
        ///     The object to locate. The value can be null for reference types.
        ///
        ///   comparer:
        ///     The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
        ///     elements.-or-null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
        ///
        /// Returns:
        ///     The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
        ///     if item is found; otherwise, a negative number that is the bitwise complement
        ///     of the index of the next element that is larger than item or, if there is
        ///     no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
        ///
        /// Exceptions:
        ///   System.InvalidOperationException:
        ///     comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        ///     cannot find an implementation of the System.IComparable&lt;T&gt; generic interface
        ///     or the System.IComparable interface for type T.
        public int BinarySearch(T item, IComparer<T> comparer) { return m_base.BinarySearch(item, comparer); }
        ///
        /// Summary:
        ///     Searches a range of elements in the sorted System.Collections.Generic.List&lt;T&gt;
        ///     for an element using the specified comparer and returns the zero-based index
        ///     of the element.
        ///
        /// Parameters:
        ///   count:
        ///     The length of the range to search.
        ///
        ///   item:
        ///     The object to locate. The value can be null for reference types.
        ///
        ///   index:
        ///     The zero-based starting index of the range to search.
        ///
        ///   comparer:
        ///     The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
        ///     elements, or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
        ///
        /// Returns:
        ///     The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
        ///     if item is found; otherwise, a negative number that is the bitwise complement
        ///     of the index of the next element that is larger than item or, if there is
        ///     no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     index is less than 0.-or-count is less than 0.
        ///
        ///   System.InvalidOperationException:
        ///     comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        ///     cannot find an implementation of the System.IComparable&lt;T&gt; generic interface
        ///     or the System.IComparable interface for type T.
        ///
        ///   System.ArgumentException:
        ///     index and count do not denote a valid range in the System.Collections.Generic.List&lt;T&gt;.
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) { return m_base.BinarySearch(index, count, item, comparer); }
        ///
        /// Summary:
        ///     Determines whether an element is in the System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   item:
        ///     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        ///     can be null for reference types.
        ///
        /// Returns:
        ///     true if item is found in the System.Collections.Generic.List&lt;T&gt;; otherwise,
        ///     false.
        public bool Contains(T item) { return m_base.Contains(item); }
        ///
        /// Summary:
        ///     Converts the elements in the current System.Collections.Generic.List&lt;T&gt; to
        ///     another type, and returns a list containing the converted elements.
        ///
        /// Parameters:
        ///   converter:
        ///     A System.Converter&lt;TInput,TOutput&gt; delegate that converts each element from
        ///     one type to another type.
        ///
        /// Returns:
        ///     A System.Collections.Generic.List&lt;T&gt; of the target type containing the converted
        ///     elements from the current System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     converter is null.
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) { return m_base.ConvertAll(converter); }
        ///
        /// Summary:
        ///     Copies the entire System.Collections.Generic.List&lt;T&gt; to a compatible one-dimensional
        ///     array, starting at the beginning of the target array.
        ///
        /// Parameters:
        ///   array:
        ///     The one-dimensional System.Array that is the destination of the elements
        ///     copied from System.Collections.Generic.List&lt;T&gt;. The System.Array must have
        ///     zero-based indexing.
        ///
        /// Exceptions:
        ///   System.ArgumentException:
        ///     The number of elements in the source System.Collections.Generic.List&lt;T&gt; is
        ///     greater than the number of elements that the destination array can contain.
        ///
        ///   System.ArgumentNullException:
        ///     array is null.
        public void CopyTo(T[] array) { m_base.CopyTo(array); }
        ///
        /// Summary:
        ///     Copies the entire System.Collections.Generic.List&lt;T&gt; to a compatible one-dimensional
        ///     array, starting at the specified index of the target array.
        ///
        /// Parameters:
        ///   array:
        ///     The one-dimensional System.Array that is the destination of the elements
        ///     copied from System.Collections.Generic.List&lt;T&gt;. The System.Array must have
        ///     zero-based indexing.
        ///
        ///   arrayIndex:
        ///     The zero-based index in array at which copying begins.
        ///
        /// Exceptions:
        ///   System.ArgumentException:
        ///     arrayIndex is equal to or greater than the length of array.-or-The number
        ///     of elements in the source System.Collections.Generic.List&lt;T&gt; is greater than
        ///     the available space from arrayIndex to the end of the destination array.
        ///
        ///   System.ArgumentOutOfRangeException:
        ///     arrayIndex is less than 0.
        ///
        ///   System.ArgumentNullException:
        ///     array is null.
        public void CopyTo(T[] array, int arrayIndex) { m_base.CopyTo(array, arrayIndex); }
        ///
        /// Summary:
        ///     Copies a range of elements from the System.Collections.Generic.List&lt;T&gt; to
        ///     a compatible one-dimensional array, starting at the specified index of the
        ///     target array.
        ///
        /// Parameters:
        ///   array:
        ///     The one-dimensional System.Array that is the destination of the elements
        ///     copied from System.Collections.Generic.List&lt;T&gt;. The System.Array must have
        ///     zero-based indexing.
        ///
        ///   count:
        ///     The number of elements to copy.
        ///
        ///   arrayIndex:
        ///     The zero-based index in array at which copying begins.
        ///
        ///   index:
        ///     The zero-based index in the source System.Collections.Generic.List&lt;T&gt; at
        ///     which copying begins.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     array is null.
        ///
        ///   System.ArgumentOutOfRangeException:
        ///     index is less than 0.-or-arrayIndex is less than 0.-or-count is less than
        ///     0.
        ///
        ///   System.ArgumentException:
        ///     index is equal to or greater than the System.Collections.Generic.List&lt;T&gt;.Count
        ///     of the source System.Collections.Generic.List&lt;T&gt;.-or-arrayIndex is equal
        ///     to or greater than the length of array.-or-The number of elements from index
        ///     to the end of the source System.Collections.Generic.List&lt;T&gt; is greater than
        ///     the available space from arrayIndex to the end of the destination array.
        public void CopyTo(int index, T[] array, int arrayIndex, int count) { m_base.CopyTo(index, array, arrayIndex, count); }
        ///
        /// Summary:
        ///     Determines whether the System.Collections.Generic.List&lt;T&gt; contains elements
        ///     that match the conditions defined by the specified predicate.
        ///
        /// Parameters:
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements
        ///     to search for.
        ///
        /// Returns:
        ///     true if the System.Collections.Generic.List&lt;T&gt; contains one or more elements
        ///     that match the conditions defined by the specified predicate; otherwise,
        ///     false.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     match is null.
        public bool Exists(Predicate<T> match) { return m_base.Exists(match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the first occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The first element that matches the conditions defined by the specified predicate,
        ///     if found; otherwise, the default value for type T.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     match is null.
        public T Find(Predicate<T> match) { return m_base.Find(match); }
        ///
        /// Summary:
        ///     Retrieves the all the elements that match the conditions defined by the specified
        ///     predicate.
        ///
        /// Parameters:
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements
        ///     to search for.
        ///
        /// Returns:
        ///     A System.Collections.Generic.List&lt;T&gt; containing all the elements that match
        ///     the conditions defined by the specified predicate, if found; otherwise, an
        ///     empty System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     match is null.
        public List<T> FindAll(Predicate<T> match) { return m_base.FindAll(match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the zero-based index of the first occurrence within
        ///     the entire System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The zero-based index of the first occurrence of an element that matches the
        ///     conditions defined by match, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     match is null.
        public int FindIndex(Predicate<T> match) { return m_base.FindIndex(match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the zero-based index of the first occurrence within
        ///     the range of elements in the System.Collections.Generic.List&lt;T&gt; that extends
        ///     from the specified index to the last element.
        ///
        /// Parameters:
        ///   startIndex:
        ///     The zero-based starting index of the search.
        ///
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The zero-based index of the first occurrence of an element that matches the
        ///     conditions defined by match, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        ///
        ///   System.ArgumentNullException:
        ///     match is null.
        public int FindIndex(int startIndex, Predicate<T> match) { return m_base.FindIndex(startIndex, match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the zero-based index of the first occurrence within
        ///     the range of elements in the System.Collections.Generic.List&lt;T&gt; that starts
        ///     at the specified index and contains the specified number of elements.
        ///
        /// Parameters:
        ///   count:
        ///     The number of elements in the section to search.
        ///
        ///   startIndex:
        ///     The zero-based starting index of the search.
        ///
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The zero-based index of the first occurrence of an element that matches the
        ///     conditions defined by match, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        ///     is less than 0.-or-startIndex and count do not specify a valid section in
        ///     the System.Collections.Generic.List&lt;T&gt;.
        ///
        ///   System.ArgumentNullException:
        ///     match is null.
        public int FindIndex(int startIndex, int count, Predicate<T> match) { return m_base.FindIndex(startIndex, count, match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the last occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The last element that matches the conditions defined by the specified predicate,
        ///     if found; otherwise, the default value for type T.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     match is null.
        public T FindLast(Predicate<T> match) { return m_base.FindLast(match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the zero-based index of the last occurrence within
        ///     the entire System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The zero-based index of the last occurrence of an element that matches the
        ///     conditions defined by match, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     match is null.
        public int FindLastIndex(Predicate<T> match) { return m_base.FindLastIndex(match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the zero-based index of the last occurrence within
        ///     the range of elements in the System.Collections.Generic.List&lt;T&gt; that extends
        ///     from the first element to the specified index.
        ///
        /// Parameters:
        ///   startIndex:
        ///     The zero-based starting index of the backward search.
        ///
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The zero-based index of the last occurrence of an element that matches the
        ///     conditions defined by match, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        ///
        ///   System.ArgumentNullException:
        ///     match is null.
        public int FindLastIndex(int startIndex, Predicate<T> match) { return m_base.FindLastIndex(startIndex, match); }
        ///
        /// Summary:
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the zero-based index of the last occurrence within
        ///     the range of elements in the System.Collections.Generic.List&lt;T&gt; that contains
        ///     the specified number of elements and ends at the specified index.
        ///
        /// Parameters:
        ///   count:
        ///     The number of elements in the section to search.
        ///
        ///   startIndex:
        ///     The zero-based starting index of the backward search.
        ///
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
        ///     to search for.
        ///
        /// Returns:
        ///     The zero-based index of the last occurrence of an element that matches the
        ///     conditions defined by match, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     startIndex is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        ///     is less than 0.-or-startIndex and count do not specify a valid section in
        ///     the System.Collections.Generic.List&lt;T&gt;.
        ///
        ///   System.ArgumentNullException:
        ///     match is null.
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) { return m_base.FindLastIndex(startIndex, count, match); }
        ///
        /// Summary:
        ///     Performs the specified action on each element of the System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   action:
        ///     The System.Action&lt;T&gt; delegate to perform on each element of the System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     action is null.
        public void ForEach(Action<T> action) { m_base.ForEach(action); }
        ///
        /// Summary:
        ///     Returns an enumerator that iterates through the System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Returns:
        ///     A System.Collections.Generic.List&lt;T&gt;.Enumerator for the System.Collections.Generic.List&lt;T&gt;.
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return new Enumerator(m_base); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return new Enumerator(m_base); }


        ///
        /// Summary:
        ///     Creates a shallow copy of a range of elements in the source System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   count:
        ///     The number of elements in the range.
        ///
        ///   index:
        ///     The zero-based System.Collections.Generic.List&lt;T&gt; index at which the range
        ///     starts.
        ///
        /// Returns:
        ///     A shallow copy of a range of elements in the source System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     index is less than 0.-or-count is less than 0.
        ///
        ///   System.ArgumentException:
        ///     index and count do not denote a valid range of elements in the System.Collections.Generic.List&lt;T&gt;.
        public List<T> GetRange(int index, int count) { return m_base.GetRange(index, count); }
        ///
        /// Summary:
        ///     Searches for the specified object and returns the zero-based index of the
        ///     first occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   item:
        ///     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        ///     can be null for reference types.
        ///
        /// Returns:
        ///     The zero-based index of the first occurrence of item within the entire System.Collections.Generic.List&lt;T&gt;,
        ///     if found; otherwise, –1.
        public int IndexOf(T item) { return m_base.IndexOf(item); }
        ///
        /// Summary:
        ///     Searches for the specified object and returns the zero-based index of the
        ///     first occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        ///     that extends from the specified index to the last element.
        ///
        /// Parameters:
        ///   item:
        ///     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        ///     can be null for reference types.
        ///
        ///   index:
        ///     The zero-based starting index of the search.
        ///
        /// Returns:
        ///     The zero-based index of the first occurrence of item within the range of
        ///     elements in the System.Collections.Generic.List&lt;T&gt; that extends from index
        ///     to the last element, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        public int IndexOf(T item, int index) { return m_base.IndexOf(item, index); }
        ///
        /// Summary:
        ///     Searches for the specified object and returns the zero-based index of the
        ///     first occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        ///     that starts at the specified index and contains the specified number of elements.
        ///
        /// Parameters:
        ///   count:
        ///     The number of elements in the section to search.
        ///
        ///   item:
        ///     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        ///     can be null for reference types.
        ///
        ///   index:
        ///     The zero-based starting index of the search.
        ///
        /// Returns:
        ///     The zero-based index of the first occurrence of item within the range of
        ///     elements in the System.Collections.Generic.List&lt;T&gt; that starts at index and
        ///     contains count number of elements, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        ///     is less than 0.-or-index and count do not specify a valid section in the
        ///     System.Collections.Generic.List&lt;T&gt;.
        public int IndexOf(T item, int index, int count) { return m_base.IndexOf(item, index, count); }
        ///
        /// Summary:
        ///     Searches for the specified object and returns the zero-based index of the
        ///     last occurrence within the entire System.Collections.Generic.List&lt;T&gt;.
        ///
        /// Parameters:
        ///   item:
        ///     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        ///     can be null for reference types.
        ///
        /// Returns:
        ///     The zero-based index of the last occurrence of item within the entire the
        ///     System.Collections.Generic.List&lt;T&gt;, if found; otherwise, –1.
        public int LastIndexOf(T item) { return m_base.IndexOf(item); }
        ///
        /// Summary:
        ///     Searches for the specified object and returns the zero-based index of the
        ///     last occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        ///     that extends from the first element to the specified index.
        ///
        /// Parameters:
        ///   item:
        ///     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        ///     can be null for reference types.
        ///
        ///   index:
        ///     The zero-based starting index of the backward search.
        ///
        /// Returns:
        ///     The zero-based index of the last occurrence of item within the range of elements
        ///     in the System.Collections.Generic.List&lt;T&gt; that extends from the first element
        ///     to index, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.
        public int LastIndexOf(T item, int index) { return m_base.LastIndexOf(item, index); }
        ///
        /// Summary:
        ///     Searches for the specified object and returns the zero-based index of the
        ///     last occurrence within the range of elements in the System.Collections.Generic.List&lt;T&gt;
        ///     that contains the specified number of elements and ends at the specified
        ///     index.
        ///
        /// Parameters:
        ///   count:
        ///     The number of elements in the section to search.
        ///
        ///   item:
        ///     The object to locate in the System.Collections.Generic.List&lt;T&gt;. The value
        ///     can be null for reference types.
        ///
        ///   index:
        ///     The zero-based starting index of the backward search.
        ///
        /// Returns:
        ///     The zero-based index of the last occurrence of item within the range of elements
        ///     in the System.Collections.Generic.List&lt;T&gt; that contains count number of elements
        ///     and ends at index, if found; otherwise, –1.
        ///
        /// Exceptions:
        ///   System.ArgumentOutOfRangeException:
        ///     index is outside the range of valid indexes for the System.Collections.Generic.List&lt;T&gt;.-or-count
        ///     is less than 0.-or-index and count do not specify a valid section in the
        ///     System.Collections.Generic.List&lt;T&gt;.
        public int LastIndexOf(T item, int index, int count) { return m_base.LastIndexOf(item, index, count); }
        ///
        /// Summary:
        ///     Reverses the order of the elements in the entire System.Collections.Generic.List&lt;T&gt;.
        public void Reverse() { m_base.Reverse(); }
        ///
        /// Summary:
        ///     Reverses the order of the elements in the specified range.
        ///
        /// Parameters:
        ///   count:
        ///     The number of elements in the range to reverse.
        ///
        ///   index:
        ///     The zero-based starting index of the range to reverse.
        ///
        /// Exceptions:
        ///   System.ArgumentException:
        ///     index and count do not denote a valid range of elements in the System.Collections.Generic.List&lt;T&gt;.
        ///
        ///   System.ArgumentOutOfRangeException:
        ///     index is less than 0.-or-count is less than 0.
        public void Reverse(int index, int count) { m_base.Reverse(index, count); }
        ///
        /// Summary:
        ///     Sorts the elements in the entire System.Collections.Generic.List&lt;T&gt; using
        ///     the default comparer.
        ///
        /// Exceptions:
        ///   System.InvalidOperationException:
        ///     The default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default cannot
        ///     find an implementation of the System.IComparable&lt;T&gt; generic interface or
        ///     the System.IComparable interface for type T.
        public void Sort() { m_base.Sort(); }
        ///
        /// Summary:
        ///     Sorts the elements in the entire System.Collections.Generic.List&lt;T&gt; using
        ///     the specified System.Comparison&lt;T&gt;.
        ///
        /// Parameters:
        ///   comparison:
        ///     The System.Comparison&lt;T&gt; to use when comparing elements.
        ///
        /// Exceptions:
        ///   System.ArgumentException:
        ///     The implementation of comparison caused an error during the sort. For example,
        ///     comparison might not return 0 when comparing an item with itself.
        ///
        ///   System.ArgumentNullException:
        ///     comparison is null.
        public void Sort(Comparison<T> comparison) { m_base.Sort(comparison); }
        ///
        /// Summary:
        ///     Sorts the elements in the entire System.Collections.Generic.List&lt;T&gt; using
        ///     the specified comparer.
        ///
        /// Parameters:
        ///   comparer:
        ///     The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
        ///     elements, or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
        ///
        /// Exceptions:
        ///   System.ArgumentException:
        ///     The implementation of comparer caused an error during the sort. For example,
        ///     comparer might not return 0 when comparing an item with itself.
        ///
        ///   System.InvalidOperationException:
        ///     comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        ///     cannot find implementation of the System.IComparable&lt;T&gt; generic interface
        ///     or the System.IComparable interface for type T.
        public void Sort(IComparer<T> comparer) { m_base.Sort(comparer); }
        ///
        /// Summary:
        ///     Sorts the elements in a range of elements in System.Collections.Generic.List&lt;T&gt;
        ///     using the specified comparer.
        ///
        /// Parameters:
        ///   count:
        ///     The length of the range to sort.
        ///
        ///   index:
        ///     The zero-based starting index of the range to sort.
        ///
        ///   comparer:
        ///     The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
        ///     elements, or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
        ///
        /// Exceptions:
        ///   System.ArgumentException:
        ///     index and count do not specify a valid range in the System.Collections.Generic.List&lt;T&gt;.-or-The
        ///     implementation of comparer caused an error during the sort. For example,
        ///     comparer might not return 0 when comparing an item with itself.
        ///
        ///   System.ArgumentOutOfRangeException:
        ///     index is less than 0.-or-count is less than 0.
        ///
        ///   System.InvalidOperationException:
        ///     comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
        ///     cannot find implementation of the System.IComparable&lt;T&gt; generic interface
        ///     or the System.IComparable interface for type T.
        public void Sort(int index, int count, IComparer<T> comparer) { m_base.Sort(index, count, comparer); }
        ///
        /// Summary:
        ///     Copies the elements of the System.Collections.Generic.List&lt;T&gt; to a new array.
        ///
        /// Returns:
        ///     An array containing copies of the elements of the System.Collections.Generic.List&lt;T&gt;.
        public T[] ToArray() { return m_base.ToArray(); }
        ///
        /// Summary:
        ///     Sets the capacity to the actual number of elements in the System.Collections.Generic.List&lt;T&gt;,
        ///     if that number is less than a threshold value.
        public void TrimExcess() { m_base.TrimExcess(); }
        ///
        /// Summary:
        ///     Determines whether every element in the System.Collections.Generic.List&lt;T&gt;
        ///     matches the conditions defined by the specified predicate.
        ///
        /// Parameters:
        ///   match:
        ///     The System.Predicate&lt;T&gt; delegate that defines the conditions to check against
        ///     the elements.
        ///
        /// Returns:
        ///     true if every element in the System.Collections.Generic.List&lt;T&gt; matches the
        ///     conditions defined by the specified predicate; otherwise, false. If the list
        ///     has no elements, the return value is true.
        ///
        /// Exceptions:
        ///   System.ArgumentNullException:
        ///     match is null.
        public bool TrueForAll(Predicate<T> match) { return m_base.TrueForAll(match); }

        public static implicit operator List<T>(EventedList<T> elist) {
            return elist.m_base;
        }

        // Summary:
        //     Enumerates the elements of a System.Collections.Generic.List&lt;T&gt;.
        [Serializable]
        public struct Enumerator : IEnumerator<T> {
            private List<T>.Enumerator m_enumerator;
            public Enumerator(List<T> baseList) {
                m_enumerator = baseList.GetEnumerator();
            }
            // Summary:
            //     Gets the element at the current position of the enumerator.
            //
            // Returns:
            //     The element in the System.Collections.Generic.List&lt;T&gt; at the current position
            //     of the enumerator.
            public T Current => m_enumerator.Current;

            // Summary:
            //     Releases all resources used by the System.Collections.Generic.List&lt;T&gt;.Enumerator.
            public void Dispose() { m_enumerator.Dispose();  } 
            //
            // Summary:
            //     Advances the enumerator to the next element of the System.Collections.Generic.List&lt;T&gt;.
            //
            // Returns:
            //     true if the enumerator was successfully advanced to the next element; false
            //     if the enumerator has passed the end of the collection.
            //
            // Exceptions:
            //   System.InvalidOperationException:
            //     The collection was modified after the enumerator was created.
            public bool MoveNext() { return m_enumerator.MoveNext(); }

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current => m_enumerator.Current;

            bool System.Collections.IEnumerator.MoveNext() {
                return m_enumerator.MoveNext();
            }

            void System.Collections.IEnumerator.Reset() {
                throw new NotSupportedException();
            }

            #endregion
        }
    }
}