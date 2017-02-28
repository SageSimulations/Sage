/* This source code licensed under the GNU Affero General Public License */

using System;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using System.Collections.Specialized;
using Highpoint.Sage.Utility.Mementos;
using Highpoint.Sage.Persistence;
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable VirtualMemberNeverOverriden.Global

namespace Highpoint.Sage.SimCore {

	/// <summary>
	/// An interface that, when implemented by an element in a smart property bag,
	/// provides a hint to diagnostic routines, particularly with respect to generating output.
	/// </summary>
	public interface ISPBTreeNode {
		/// <summary>
		/// True if this entry in a SmartPropertyBag is a leaf node.
		/// </summary>
		bool IsLeaf { get; }
	}

	/// <summary>
	/// A structure that is used in the creation of a hierarchical dictionary. Such
	/// a dictionary structure can contain smart property bags and other dictionaries
	/// as leaf nodes, as well as having IDictionaries implementing the tree-like
	/// structure of the root dictionary. This is used so that a node in a SmartPropertyBag
	/// can have an atomic meaning in a real-world sense (such as a temperature controller
	/// on a piece of equipment, but still be implemented as an IDictionary or even a
	/// SmartPropertyBag.
	/// </summary>
	public struct HierarchicalDictionaryEntry : IXmlPersistable {
		private object m_key;
		private object m_value;
		private bool m_isLeaf;
		/// <summary>
		/// Creates a HierarchicalDictionaryEntry.
		/// </summary>
		/// <param name="key">The key by which the object is known in the dictionary.</param>
		/// <param name="val">The object value of the entry in the dictionary.</param>
		/// <param name="isLeaf">True if this is a semantic leaf-node.</param>
		public HierarchicalDictionaryEntry(object key, object val, bool isLeaf){
			m_key = key;
			m_isLeaf = isLeaf;
			m_value = val;
		}
		/// <summary>
		/// The key by which the object is known in the dictionary.
		/// </summary>
		public object Key => m_key;

	    /// <summary>
		/// The object value of the entry in the dictionary.
		/// </summary>
		public object Value => m_value;

	    /// <summary>
		/// True if this is a semantic leaf-node.
		/// </summary>
		public bool IsLeaf => m_isLeaf;

	    #region >>> IXmlPersistable Support

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public void SerializeTo(XmlSerializationContext xmlsc){
			xmlsc.StoreObject("key",m_key);
			xmlsc.StoreObject("Value",m_value);
			xmlsc.StoreObject("IsLeaf",m_isLeaf);
		}
        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public void DeserializeFrom(XmlSerializationContext xmlsc){
			m_key = xmlsc.LoadObject("key");
			m_value = xmlsc.LoadObject("Value");
			m_isLeaf = (bool)xmlsc.LoadObject("IsLeaf");
		}


		#endregion
	}


	/// <summary>
	/// NOTE: This is an older class whose utility may have been subsumed into other,
	/// newer .NET classes such as tuples and the mechanisms for lambda expressions.
	/// However, it is used in various places in the library, so it is retained.
	/// <para/>
	/// A SmartPropertyBag (SPB) is at its basic level, a collection of name/value pairs.
	/// The power of a smart property bag lies in the fact that entries in the bag
	/// can be any of a number of specialized types - 
	/// <para/><b>Simple data (Value) : </b>Any primitive that is convertible to a double, string or boolean.
	/// <para/><b>Expression : </b>An expression is a string such as "X + Y" that, when
	/// queried (assuming that X and Y are entries in the bag) is evaluated and returns 
	/// the result of the evaluation.
	/// <para/><b>Alias : </b>A name value pair that points to an entry in another SPB.
	/// <para/><b>Delegate : </b>A delegate to a method that returns a double. When this entry 
	/// is requested, the delegate is called and the resulting value is returned.
	/// <para/><b>SPB : </b>An entry in a SPB may be another SPB, which effectively
	/// becomes a child of this SPB. Thus, a SPB representing a truck may contain several
	/// other SPBs representing each load placed on that truck, and thereafter, the key
	/// "TruckA.LoadB.Customer" will retrieve, for example, a string containing the name or
	/// ID of the customer for whom load B is destined.<p></p>
	/// <para/><b>ISnapShottable : </b>Any arbitrary object can be stored in a SPB if it
	/// implements the ISnapshottable interface. The SPB enables storage of booleans, doubles,
	/// and strings through the use of internal classes SPBBooleanWrapper, SPBDoubleValueWrapper,
	/// and  SPBStringWrapper, respectively - each of which implements ISnapshottable.
	/// <hr></hr>
	/// A SmartPropertyBag can also be write-locked, allowing it temporarily or permanently
	/// to be read but not written.<p></p>
	/// A SmartPropertyBag also maintains a memento, an object that records the SPB's internal
	/// state and can restore that state at any time. This is useful for a model element
	/// that may need to be "rolled back" to a prior state. This memento is only recalculated
	/// when it is requested, and only the portions that have changed are re-recorded into the
	/// memento.<p></p>
	/// A SmartPropertyBag is useful when an application is required to support significant
	/// configurability at run time. An example might be a modeling tool that incorporates the
	/// concept of a vessel for chemical manufacturing, but does not know at the time of app
	/// design, all of the characteristics that will be of interest in that vessel, and all
	/// of the attachments that will be made available on that vessel at a later time or by
	/// the designer using the application.
	/// </summary>
	public class SmartPropertyBag : ISupportsMementos, IHasWriteLock, IEnumerable, ISPBTreeNode, IXmlPersistable {

        #region Private Fields
        private readonly MementoHelper m_ssh;
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("SmartPropertyBag");
        private readonly WriteLock m_writeLock = new WriteLock(true);
        private readonly IDictionary m_dictionary = new Hashtable();
        private IMemento m_memento;

        #endregion Private Fields

        #region Constructors
        /// <summary>
        /// Creates a SmartPropertyBag.
        /// </summary>
        public SmartPropertyBag() {
            m_ssh = new MementoHelper(this, true);
        }

        #endregion Constructors

		/// <summary>
		/// Any item added to a SPB must implement this interface.
		/// </summary>
        public interface IHasValue : ISupportsMementos, IHasWriteLock {
            /// <summary>
            /// Retrieves the underlying value object contained in this entry.
            /// </summary>
            /// <returns>The underlying value object contained in this entry.</returns>
            object GetValue();
        }
		
		/// <summary>
		/// Fired whenever the memento maintained by this SPB has changed.
		/// </summary>
		public event MementoChangeEvent MementoChangeEvent { 
			add { 
				if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
				m_ssh.MementoChangeEvent+=value;
			}
			remove { 
				if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
				m_ssh.MementoChangeEvent-=value; }
		}

		/// <summary>
		/// Delegate that is implemented by a delegate that is being added to the SPB, returning a double.
		/// </summary>
		public delegate double SPBDoubleDelegate();

		/// <summary>
		/// Indicates if write operations on this equipment are permitted.
		/// </summary>
		public bool IsWritable => m_writeLock.IsWritable;

	    /// <summary>
		/// Indicates if this SPB is a leaf (whether it contains entries). Fulfills
		/// obligation incurred by implementing TreeNode.
		/// </summary>
		public bool IsLeaf => false;

	    /// <summary>
		/// Allows the SPB to be treated as a writelock, to determine if it is write-protected.
		/// </summary>
		/// <param name="spb">The SPB whose writability is being queried.</param>
		/// <returns></returns>
		public static explicit operator WriteLock(SmartPropertyBag spb) {
			return spb.m_writeLock;
		}

        /// <summary>
        /// Determines whether this smart property bag contains the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if this smart property bag contains the specified key; otherwise, <c>false</c>.
        /// </returns>
		public bool Contains(object key)
        {
            string s = key as string;
            if ( s != null ) {
				return ExistsKey(s);
			} else {
			    // ReSharper disable once TailRecursiveCall
				return Contains(key); // TODO: Get code coverage here for a test.
			}
        }

        #region SPB Contents' Inner Classes - all but SPBExpressionWrapper and DoubleDelegateWrapper are persistable.
        private class SPBExpressionWrapper : IHasValue, ISPBTreeNode {
			public event MementoChangeEvent MementoChangeEvent { 
				add { m_ssh.MementoChangeEvent+=value; }
				remove { m_ssh.MementoChangeEvent-=value; }
			}
			private MementoHelper m_ssh;
			private IMemento m_memento;

			private SmartPropertyBag m_spb;
			private string[] m_args;
			private Evaluator m_evaluator;
			public SPBExpressionWrapper(SmartPropertyBag spb, Evaluator eval, string[] args){
				m_ssh = new MementoHelper(this,true);
				m_spb = spb;
				m_evaluator = eval;
				m_args = args;
				foreach ( string arg in args) {
					ISupportsMementos iss = spb.GetContentsOfKey(arg);
					m_ssh.AddChild(iss);
				}
				m_memento = new SPBExpressionWrapperMemento(this);
			}

			public bool Equals(ISupportsMementos otherGuy){
				if ( !( otherGuy is SPBExpressionWrapper ) ) return false;
				SPBExpressionWrapper spbew = (SPBExpressionWrapper)otherGuy;
				if ( m_spb==spbew.m_spb && m_evaluator.Equals(spbew.m_evaluator) ) return true;
				return false;
			}

			public IMemento Memento {
				get {
					return m_memento;
				}
				set {
					((SPBExpressionWrapperMemento)value).Load(this);
				}
			}

            #region Implementation of IHasValue
			public object GetValue(){
				object[] argvals = new object[m_args.Length];
				for ( int i = 0 ; i < m_args.Length ; i++ ) {
					double thisArgval = (double)((IHasValue)m_spb.GetContentsOfKey(m_args[i])).GetValue();
					argvals[i] = thisArgval;
				}
				object obj = m_evaluator(argvals);
				return (double)obj;
			}
			public bool IsLeaf => true;
		    public bool IsWritable => false;

		    #endregion

			public object GetSnapshot(){ return GetValue(); }

			public bool HasChanged => m_ssh.HasChanged;
// An expression only changes when its terms change. 

			public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

		    private class SPBExpressionWrapperMemento : IMemento {
		        private readonly SPBExpressionWrapper m_ew;                

				public SPBExpressionWrapperMemento(SPBExpressionWrapper ew){
					m_ew = ew;
				}

				public ISupportsMementos CreateTarget(){
					return m_ew;
				}

				public void Load(ISupportsMementos ism){
					SPBExpressionWrapper spbew = (SPBExpressionWrapper)ism;
					spbew.m_ssh = new MementoHelper(spbew,true);
					spbew.m_spb = m_ew.m_spb;
					spbew.m_evaluator = m_ew.m_evaluator;
					spbew.m_args = m_ew.m_args;
					foreach ( string arg in spbew.m_args) {
						ISupportsMementos iss = spbew.m_spb.GetContentsOfKey(arg);
						spbew.m_ssh.AddChild(iss);
					}
					spbew.m_memento = new SPBExpressionWrapperMemento(spbew);

                    OnLoadCompleted?.Invoke(this);
                }

				public IDictionary GetDictionary(){
					IDictionary retval = new ListDictionary();
					retval.Add("Value",m_ew.GetValue());
					return retval;
				}

				public bool Equals(IMemento otheOneMemento){
					if ( otheOneMemento == null ) return false;
					if ( this == otheOneMemento ) return true;
					if ( !(otheOneMemento is SPBExpressionWrapperMemento) ) return false;

					if ( m_ew == ((SPBExpressionWrapperMemento)otheOneMemento).m_ew ) return true;
					return false;
				}

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent { get; set; }
		    }
		}

		private class SPBAlias : IHasValue, ISPBTreeNode, IXmlPersistable {
			public event MementoChangeEvent MementoChangeEvent { 
				add { m_ssh.MementoChangeEvent+=value; }
				remove { m_ssh.MementoChangeEvent-=value; }
			}
			private MementoHelper m_ssh;
			private SmartPropertyBag m_spb;
			private IMemento m_memento;
			private string m_key;
			public SPBAlias(SmartPropertyBag whichBag, string key){
				m_ssh = new MementoHelper(this,true);
				m_ssh.AddChild(whichBag.GetContentsOfKey(key));
				m_spb = whichBag;
				m_key = key;
				m_memento = new SPBAliasMemento(this);
			}

			public string OtherKey => m_key;
		    public SmartPropertyBag OtherBag => m_spb;

		    public IMemento Memento {
				get {
					return m_memento;
				}
				set {
					((SPBAliasMemento)value).Load(this);
				}
			}

			public bool Equals(ISupportsMementos otherGuy){
				if ( !( otherGuy is SPBAlias ) ) return false;
				SPBAlias spba = (SPBAlias)otherGuy;
				if ( m_key.Equals(spba.m_key) && m_spb.Equals(spba.m_spb) ) return true;
				return false;
			}

            #region IHasValue Implementation
			public object GetValue(){
				object val = m_spb[m_key];
				if ( val is IHasValue ) val = ((IHasValue)val).GetValue();
				return val;
			}
			public bool IsLeaf => true;
		    public bool IsWritable => false;

		    #endregion

			public bool HasChanged => m_ssh.HasChanged;

		    public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

		    #region IXmlPersistable Members

			/// <summary>
			/// A default constructor, to be used for creating an empty object prior to reconstitution from a serializer.
			/// </summary>
			public SPBAlias(){
				m_ssh = new MementoHelper(this,true);
				m_memento = new SPBAliasMemento(this);
			}

			/// <summary>
			/// Serializes this object to the specified XmlSerializatonContext.
			/// </summary>
			/// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
			public void SerializeTo(XmlSerializationContext xmlsc) {
				xmlsc.StoreObject("key",m_key);
				xmlsc.StoreObject("Aliased_Bag",m_spb);
			}

			/// <summary>
			/// Deserializes this object from the specified XmlSerializatonContext.
			/// </summary>
			/// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
			public void DeserializeFrom(XmlSerializationContext xmlsc) {
				m_spb = (SmartPropertyBag)xmlsc.LoadObject("Aliased_Bag");
				m_key = (string)xmlsc.LoadObject("key");
				m_ssh.AddChild(m_spb.GetContentsOfKey(m_key));
			}

			#endregion

			private class SPBAliasMemento : IMemento {

                #region Private Fields
                private readonly SPBAlias m_orig;
				private readonly object m_value;

			    #endregion

                public SPBAliasMemento(SPBAlias orig) {
					m_orig = orig;
					m_value = orig.GetValue();
				}
				public ISupportsMementos CreateTarget(){
					return m_orig;
				}

				public void Load(ISupportsMementos ism){
					SPBAlias alias = (SPBAlias)ism;
					alias.m_key = m_orig.m_key;
					alias.m_spb = m_orig.m_spb;
					alias.m_ssh = new MementoHelper(alias,true);
					alias.m_ssh.AddChild(alias.m_spb.GetContentsOfKey(alias.m_key));
					alias.m_memento = new SPBAliasMemento(alias);

				    OnLoadCompleted?.Invoke(this);
				}

				public IDictionary GetDictionary(){
					IDictionary retval = new ListDictionary();
					retval.Add("Value",m_value);
					return retval;
				}
				public bool Equals(IMemento otheOneMemento){
					if ( otheOneMemento == null ) return false;
					if ( this==otheOneMemento ) return true;
					if ( !(otheOneMemento is SPBAliasMemento )) return false;

					SPBAliasMemento spbamOtherGuy = (SPBAliasMemento)otheOneMemento;
					if ( spbamOtherGuy.m_orig.m_spb != m_orig.m_spb ) return false;
					if ( spbamOtherGuy.m_orig.m_key != m_orig.m_key ) return false;
					return spbamOtherGuy.m_value.Equals(m_value);
				}

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;
                
                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent { get; set; }
			}
			
		}

        private class SPBDoubleDelegateWrapper : IHasValue, ISPBTreeNode { // Not IXmlPersistable.
			public event MementoChangeEvent MementoChangeEvent { 
				add { m_ssh.MementoChangeEvent+=value; }
				remove { m_ssh.MementoChangeEvent-=value; }
			}
			SPBDoubleDelegate m_del;
			MementoHelper m_ssh;
			private object m_lastValue = new object();
			public SPBDoubleDelegateWrapper(SPBDoubleDelegate del){
				m_ssh = new MementoHelper(this,false);
				m_del = del;
			}

			public IMemento Memento {
				get {
					m_lastValue = GetValue();
					return new SPBDoubleDelegateWrapperMemento(this);
				}
				set {
					((SPBDoubleDelegateWrapperMemento)value).Load(this);
				}
			}

            #region Implementation of IHasValue
			public object GetValue(){ return m_del(); }
			public bool IsLeaf => true;
		    public bool IsWritable => false;

		    #endregion


			public bool Equals(ISupportsMementos otherGuy){
				if ( !( otherGuy is SPBDoubleDelegateWrapper ) ) return false;
				SPBDoubleDelegateWrapper spbdw = (SPBDoubleDelegateWrapper)otherGuy;
				if ( m_del==spbdw.m_del ) return true;
				return false;
			}

			public bool HasChanged => !m_del().Equals(m_lastValue);
		    public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

		    class SPBDoubleDelegateWrapperMemento : IMemento {

                #region Private Fields

		        private readonly SPBDoubleDelegateWrapper m_dw;
				private readonly double m_value;
                #endregion

                public SPBDoubleDelegateWrapperMemento(SPBDoubleDelegateWrapper dw){
					m_dw = dw;
					m_value = (double)dw.GetValue();
				}
				public ISupportsMementos CreateTarget(){
					return m_dw;
				}
				public void Load(ISupportsMementos ism){
					SPBDoubleDelegateWrapper spbdw = (SPBDoubleDelegateWrapper)ism;
					spbdw.m_ssh = new MementoHelper(spbdw,false);
					spbdw.m_del = m_dw.m_del;

				    OnLoadCompleted?.Invoke(this);
				}
				public IDictionary GetDictionary(){
					IDictionary retval = new ListDictionary();
					retval.Add("Value",m_value);
					return retval;
				}

				public bool Equals(IMemento otheOneMemento){
					if ( otheOneMemento == null ) return false;
					if ( this==otheOneMemento ) return true;
					if ( !( otheOneMemento is SPBDoubleDelegateWrapperMemento ) ) return false;
                    
					SPBDoubleDelegateWrapperMemento spbdwm = (SPBDoubleDelegateWrapperMemento)otheOneMemento;
					if ( spbdwm.m_dw.Equals(m_dw) && spbdwm.m_value.Equals(m_value) ) return true;
					return false;
				}

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent { get; set; }
		    }
		}

        private class SPBValueHolder : IHasValue, ISPBTreeNode, IXmlPersistable {
			public event MementoChangeEvent MementoChangeEvent {
				add { m_ssh.MementoChangeEvent+=value; }
				remove { m_ssh.MementoChangeEvent-=value; }
			}
			private readonly MementoHelper m_ssh;
			private double m_value;
			public SPBValueHolder(){
				m_ssh = new MementoHelper(this,true);
			}
			public IMemento Memento {
				get {
					return new SPBValueHolderMemento(m_value);
				}
				set {
					m_value = (double)((SPBValueHolderMemento)value).GetValue();
				}
			}
            
			public void SetValue(double val){
			    // ReSharper disable once CompareOfFloatsByEqualityOperator
				if ( val != m_value ) {
					m_value = val;
					m_ssh.ReportChange();
				}
			}

            #region Implementation of IHasValue
			public object GetValue(){ return m_value; }
			public bool IsLeaf => true;
		    public bool IsWritable => true;

		    #endregion

			public bool HasChanged => m_ssh.HasChanged;
		    public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

		    public static explicit operator double(SPBValueHolder spbvh){
				return spbvh.m_value;
			}

			public bool Equals(ISupportsMementos otherGuy){
			    SPBValueHolder spbvh = otherGuy as SPBValueHolder;
				return m_value==spbvh?.m_value;
			}

			#region >>> Serialization Support (incl. IXmlPersistable Members) <<<
			public void SerializeTo(XmlSerializationContext xmlsc) {
				//base.SerializeTo(node,xmlsc);
				xmlsc.StoreObject("Value",m_value);
			}

			public void DeserializeFrom(XmlSerializationContext xmlsc) {
				//base.DeserializeFrom(xmlsc);
					m_value = (double)xmlsc.LoadObject("Value");
			}
			#endregion

			class SPBValueHolderMemento : IMemento {

                #region Private Fields
                private readonly double m_value;

			    #endregion

                public SPBValueHolderMemento(double val) {
					m_value = val;
				}
				public ISupportsMementos CreateTarget(){
					SPBValueHolder vh = new SPBValueHolder();
					vh.SetValue(m_value);
					return vh;
				}
				public object GetValue(){
					return m_value;
				}
				public IDictionary GetDictionary(){
					IDictionary retval = new ListDictionary();
					retval.Add("Value",m_value);
					return retval;
				}

				public bool Equals(IMemento otheOneMemento){
					if ( otheOneMemento == null ) return false;
					if ( this == otheOneMemento ) return true;

				    if ( m_value == (otheOneMemento as SPBValueHolderMemento)?.m_value ) return true;

					return false;
				}

				public void Load(ISupportsMementos ism){
					((SPBValueHolder)ism).m_value = m_value;

				    OnLoadCompleted?.Invoke(this);
				}

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent { get; set; }
			}
		}

        private class SPBStringHolder : IHasValue, ISPBTreeNode, IXmlPersistable {
			public event MementoChangeEvent MementoChangeEvent {
				add { m_ssh.MementoChangeEvent+=value; }
				remove { m_ssh.MementoChangeEvent-=value; }
			}
			private readonly MementoHelper m_ssh;
			private string m_value;
			public SPBStringHolder(){
				m_ssh = new MementoHelper(this,true);
			}
			public IMemento Memento {
				get {
					return new SPBStringHolderMemento(m_value);
				}
				set {
					m_value = (string)((SPBStringHolderMemento)value).GetValue();
				}
			}
            
			public void SetValue(string val){
				if ( ! val.Equals(m_value) ) {
					m_value = val;
					m_ssh.ReportChange();
				}
			}

            #region Implementation of IHasValue
			public object GetValue(){ return m_value; }
			public bool IsLeaf => true;
		    public bool IsWritable => true;

		    #endregion

			public bool HasChanged => m_ssh.HasChanged;
		    public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

		    public static explicit operator string(SPBStringHolder spbvh){
				return spbvh.m_value;
			}

			public bool Equals(ISupportsMementos otherGuy){
				if ( !( otherGuy is SPBStringHolder ) ) return false;
				SPBStringHolder spbvh = (SPBStringHolder)otherGuy;
				return m_value.Equals(spbvh.m_value);
			}

			#region >>> Serialization Support (incl. IXmlPersistable Members) <<<
			public void SerializeTo(XmlSerializationContext xmlsc) {
				//base.SerializeTo(node,xmlsc);
				xmlsc.StoreObject("Value",m_value);
			}

			public void DeserializeFrom(XmlSerializationContext xmlsc) {
				//base.DeserializeFrom(xmlsc);
				m_value = (string)xmlsc.LoadObject("Value");
			}
			#endregion

			class SPBStringHolderMemento : IMemento {

                #region Private Fields
                private readonly string m_value;

			    #endregion

                public SPBStringHolderMemento(string val) {
					m_value = val;
				}
				public ISupportsMementos CreateTarget(){
					SPBStringHolder sh = new SPBStringHolder();
					sh.SetValue(m_value);
					return sh;
				}
				public object GetValue(){
					return m_value;
				}
				public IDictionary GetDictionary(){
					IDictionary retval = new ListDictionary();
					retval.Add("Value",m_value);
					return retval;
				}

				public bool Equals(IMemento otheOneMemento){
					if ( otheOneMemento == null ) return false;
					if ( this == otheOneMemento ) return true;
					if ( !( otheOneMemento is SPBStringHolderMemento ) ) return false;

					if ( m_value.Equals(((SPBStringHolderMemento)otheOneMemento).m_value) ) return true;

					return false;
				}

				public void Load(ISupportsMementos ism){
					((SPBStringHolder)ism).m_value = m_value;

				    OnLoadCompleted?.Invoke(this);
				}

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent { get; set; }
			}
		}

        private class SPBBooleanHolder : IHasValue, ISPBTreeNode, IXmlPersistable {
			public event MementoChangeEvent MementoChangeEvent {
				add { m_ssh.MementoChangeEvent+=value; }
				remove { m_ssh.MementoChangeEvent-=value; }
			}
			private readonly MementoHelper m_ssh;
			private bool m_value;
			public SPBBooleanHolder(){
				m_ssh = new MementoHelper(this,true);
			}
			public IMemento Memento {
				get {
					return new SPBBooleanHolderMemento(m_value);
				}
				set {
					m_value = (bool)((SPBBooleanHolderMemento)value).GetValue();
				}
			}
            
			public void SetValue(bool val){
				if ( ! val.Equals(m_value) ) {
					m_value = val;
					m_ssh.ReportChange();
				}
			}

            #region Implementation of IHasValue
			public object GetValue(){
				return m_value;
			}
			public bool IsLeaf => true;
		    public bool IsWritable => true;

		    #endregion

			public bool HasChanged => m_ssh.HasChanged;
		    public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

		    public static explicit operator bool(SPBBooleanHolder spbbh){
				return spbbh.m_value;
			}

			public bool Equals(ISupportsMementos otherGuy){
				if ( !( otherGuy is SPBBooleanHolder ) ) return false;
				SPBBooleanHolder spbbh = (SPBBooleanHolder)otherGuy;
				return m_value.Equals(spbbh.m_value);
			}

			#region >>> Serialization Support (incl. IXmlPersistable Members) <<<
			public void SerializeTo(XmlSerializationContext xmlsc) {
				//base.SerializeTo(node,xmlsc);
				xmlsc.StoreObject("Value",m_value);
			}

			public void DeserializeFrom(XmlSerializationContext xmlsc) {
				//base.DeserializeFrom(xmlsc);
				m_value = (bool)xmlsc.LoadObject("Value");
			}
			#endregion

			class SPBBooleanHolderMemento : IMemento {

                #region Private Fields
                private readonly bool m_value;

			    #endregion

                public SPBBooleanHolderMemento(bool val) {
					m_value = val;
				}
				public ISupportsMementos CreateTarget(){
					SPBBooleanHolder bh = new SPBBooleanHolder();
					bh.SetValue(m_value);
					return bh;
				}
				public object GetValue(){
					return m_value;
				}
				public IDictionary GetDictionary(){
					IDictionary retval = new ListDictionary();
					retval.Add("Value",m_value);
					return retval;
				}

				public bool Equals(IMemento otheOneMemento){
					if ( otheOneMemento == null ) return false;
					if ( this == otheOneMemento ) return true;
					if ( !( otheOneMemento is SPBBooleanHolderMemento ) ) return false;

					if ( m_value.Equals(((SPBBooleanHolderMemento)otheOneMemento).m_value) ) return true;

					return false;
				}

				public void Load(ISupportsMementos ism){
					((SPBBooleanHolder)ism).m_value = m_value;

				    OnLoadCompleted?.Invoke(this);
				}

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent { get; set; }
			}
		}

//		internal class SPBEnumerator : IEnumerator {
//
//			IEnumerator m_enumerator;
//			public SPBEnumerator(SmartPropertyBag spb){
//				m_enumerator = spb.m_dictionary.GetEnumerator();
//			}
//		
//			#region Implementation of IEnumerator
//			public void Reset() {
//				m_enumerator.Reset();
//			}
//			public bool MoveNext() {
//				return m_enumerator.MoveNext();
//			}
//			public object Current {
//				get {
//					return new SPBDictionaryEntry(m_enumerator.Current);
//				}
//			}
//			#endregion
//		}

        #endregion
		/// <summary>
		/// Retrieves an enumerator that cycles through all of the entries in this SPB.
		/// If the entry is not a leaf node, then it can have its enumerator invoked,
		/// allowing that entry's child list to be walked, and so forth.
		/// <p/>
		/// <code>
		/// private void DumpEnumerable( IEnumerable enumerable, int depth ) {
		///		foreach ( HierarchicalDictionaryEntry hde in enumerable ) {
		///			for ( int i = 0 ; i &lt; depth ; i++ ) Trace.Write("\t");
		///			Trace.Write(hde.Key.ToString() + ", ");
		///			Trace.Write(hde.Value.GetType() + ", ");
		///			if ( hde.IsLeaf ) {
		///				Trace.Write(hde.Value.ToString());
		///				if ( hde.Value is double ) {
		///		 			Trace.WriteLine(" &lt;NOTE: this is a double.&gt;"); 
		///				} else {
		///					Trace.WriteLine("");
		///				}
		///			} else {
		///				Trace.WriteLine("");
		///				DumpEnumerable((IEnumerable)hde.Value,depth+1);
		///			}
		///		}
		/// </code>
		/// </summary>
		/// <returns>An enumerator that cycles through all of the entries in this SPB.</returns>
		public IEnumerator GetEnumerator(){
			ArrayList al = new ArrayList();
			foreach ( DictionaryEntry de in m_dictionary ) {
			    IHasValue value = de.Value as IHasValue;
			    object val = (value != null?value.GetValue():de.Value);
			    ISPBTreeNode node = de.Value as ISPBTreeNode;
			    bool isLeaf = !(node != null && !node.IsLeaf);
				al.Add(new HierarchicalDictionaryEntry(de.Key,val,isLeaf));
			}
			return al.GetEnumerator();
		}

		#region Methods for adding things to, and removing them from, the SPB.

		/// <summary>
		/// Adds an expression to this SPB. 
		/// </summary>
		/// <param name="key">The key by which this expression will be known. (e.g. "PackagingRate")</param>
		/// <param name="expression">The expression. (e.g. "InFlowRate*SelectedPackageSize")</param>
		/// <param name="argNames">The names of the arguments in the expression (e.g. new string[]{"InFlowRate","SelectedPackageSize"}</param>
		public void AddExpression(string key, string expression, string[] argNames)
		{
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			Evaluator eval = EvaluatorFactory.CreateEvaluator("",expression,argNames);
			SPBExpressionWrapper spbew = new SPBExpressionWrapper(this,eval,argNames);
			AddSPBEntry(key,spbew);
			m_ssh.AddChild(spbew);
		}

		/// <summary>
		/// Adds an alias to this SPB. An alias points to an entry in another SPB. The other SPB
		/// need not be a child of this SPB.
		/// </summary>
		/// <param name="key">The key in this SPB by which this alias will be known.</param>
		/// <param name="otherBag">The SPB to which this alias points.</param>
		/// <param name="otherKey">The key in the otherBag that holds the aliased object.</param>
		public void AddAlias(string key, SmartPropertyBag otherBag, string otherKey){
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			object obj = otherBag[otherKey];
			if ( obj == null ) throw new ApplicationException("SmartPropertyBag aliasing a key to a nonexistent 'other' key.");
			ISupportsMementos iss = new SPBAlias(otherBag,otherKey);
			AddSPBEntry(key,iss);
			m_ssh.AddChild(iss);
		}
		
		/// <summary>
		/// Adds a child SPB to this SPB. A child SPB is one that is owned by this bag,
		/// and whose entries can be treated as sub-entries of this bag. For example, a
		/// if a bag, representing a pallet, were to contain another SPB under the key
		/// of "Crates", and that SPB contained one SPB for each crate (one of which was
		/// keyed as "123-45", and that SPB had a string keyed as "SKU" and another keyed
		/// as "Batch", then the following code would retrieve the SKU directly:
		/// <code>string theSKU = (string)myPallet["Crates.123-45.SKU"];</code>
		/// </summary>
		/// <param name="key">The key by which the child SPB is going to be known.</param>
		/// <param name="spb">The child SPB.</param>
		public void AddChildSPB(string key, SmartPropertyBag spb){
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			AddSPBEntry(key,spb);
			m_ssh.AddChild(spb);
			m_writeLock.AddChild((WriteLock)spb);
		}

		/// <summary>
		/// Adds a value (convertible to double) to the SPB under a specified key.
		/// </summary>
		/// <param name="key">The key by which the value will known and/or retrieved.</param>
		/// <param name="valConvertibleToDouble">An object that is convertible to a double.</param>
		public void AddValue(string key, object valConvertibleToDouble) {
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			SPBValueHolder spbvh = new SPBValueHolder();
			spbvh.SetValue(Convert.ToDouble(valConvertibleToDouble));
			AddSPBEntry(key,spbvh);
			m_ssh.AddChild(spbvh);
		}

		/// <summary>
		/// Adds a string value to the SPB under a specified key.
		/// </summary>
		/// <param name="key">The key by which the string value will known and/or retrieved.</param>
		/// <param name="val">The string that will be stored in the SPB.</param>
		public void AddString(string key, string val)
		{
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			SPBStringHolder spbsh = new SPBStringHolder();
			spbsh.SetValue(val);
			AddSPBEntry(key,spbsh);
			m_ssh.AddChild(spbsh);
		}

		/// <summary>
		/// Adds a boolean value to the SPB under a specified key.
		/// </summary>
		/// <param name="key">The key by which the boolean value will known and/or retrieved.</param>
		/// <param name="val">The boolean that will be stored in the SPB.</param>
		public void AddBoolean(string key, bool val)
		{
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			SPBBooleanHolder spbbh = new SPBBooleanHolder();
			spbbh.SetValue(val);
			AddSPBEntry(key,spbbh);
			m_ssh.AddChild(spbbh);
		}

		/// <summary>
		/// Any object can be stored in a SPB if it implements ISupportsMementos. This API
		/// performs such storage.
		/// </summary>
		/// <param name="key">The key under which the ISupportsMementos implementer is to 
		/// be known.</param>
		/// <param name="iss">the object that implements ISupportsMementos.</param>
		public void AddSnapshottable(string key, ISupportsMementos iss) {
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			AddSPBEntry(key,iss);
			m_ssh.AddChild(iss);
            // TODO: Get test coverage on the following code.
		    // ReSharper disable once SuspiciousTypeConversion.Global
			WriteLock cwl = iss as WriteLock;
			if ( cwl != null ) m_writeLock.AddChild(cwl);
		}

		/// <summary>
		/// Adds a delegate to the SPB under a specified key. When this entry is retrieved
		/// from the SPB, it will first be located by key, and then be evaluated by calling
		/// it, and the value returned from the delegate invocation will be returned to the
		/// entity calling into the SPB. Example:
		/// <code>
		/// SPBDoubleDelegate spbdd = new SPBDoubleDelegate(this.GetAValue);
		/// mySPB.AddDelegate("someValue",spbdd); // Add the delegate to the SPB.
		/// double theValue = mySPB["someValue"]; // calls into 'this.GetAValue()' and returns the answer.
		/// </code>
		/// </summary>
		/// <param name="key">The key by which the delegate's value will known and/or retrieved.</param>
		/// <param name="val">The delegate that will be stored in the SPB.</param>
		public void AddDelegate(string key, SPBDoubleDelegate val) {
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			SPBDoubleDelegateWrapper spbdw = new SPBDoubleDelegateWrapper(val);
			AddSPBEntry(key,spbdw);
			m_ssh.AddChild(spbdw);
		}

		/// <summary>
		/// Removes an object from this SPB.
		/// </summary>
		/// <param name="key">The key of the object that is being removed.</param>
		public void Remove(string key) {
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			ISupportsMementos child = GetContentsOfKey(key);
            // TODO: Get test coverage on the following code.
            // ReSharper disable once SuspiciousTypeConversion.Global
            WriteLock cwl = child as WriteLock;
			if ( cwl != null ) m_writeLock.RemoveChild(cwl);
			m_ssh.RemoveChild(child);
			m_dictionary.Remove(key);
		}

		/// <summary>
		/// Removes all objects from the SPB.
		/// </summary>
		public void Clear(){
			if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
			ArrayList keys = new ArrayList();
			foreach ( DictionaryEntry de in m_dictionary ) keys.Add(de.Key);
			foreach ( string key in keys) {
				WriteLock cwl = m_dictionary[key] as WriteLock;
				if ( cwl != null ) m_writeLock.RemoveChild(cwl);
				Remove(key);
			}
			m_ssh.ReportChange();
		}

		#endregion
		
        /// <summary>
		/// Retrieves an entry from this SPB. Compound keys may be specified if appropriate.
		/// For example, if a bag, representing a pallet, were to contain another SPB under
		/// the key of "Crates", and that SPB contained one SPB for each crate (one of which
		/// was keyed as "123-45", and that SPB had a string keyed as "SKU" and another keyed
		/// as "Batch", then the following code would retrieve the SKU directly:
		/// <code>string theSKU = (string)myPallet["Crates.123-45.SKU"];</code>
		/// </summary>
		public virtual object this[string key] 
		{
			get {
				key=key.Trim();
				// NOTE: Do not use a key with a '.' in it.
				if ( key.IndexOf('.') != -1 ) { // TODO: Speed this up. Checks all key values in all tables, currently.
					string myKey = key.Substring(0,key.IndexOf('.'));
					string subsKey = key.Substring(key.IndexOf('.')+1);
					object subbag = GetContentsOfKey(myKey);
				    SmartPropertyBag bag = subbag as SmartPropertyBag;
				    if ( bag != null ) {
						return bag[subsKey];
					} else {
						string msg = "SmartPropertyBag called with key, \"" + key + "\", but the contents " +
							"of key \"" + myKey + "\" was not a SmartPropertyBag, so the key \"" + subsKey +
							"\" cannot be retrieved from it.";
						throw new SmartPropertyBagContentsException(msg);
					}
				}
				object retval = m_dictionary[key];
			    IHasValue value = retval as IHasValue;
			    return value != null ? value.GetValue() : retval;
			}
			set {
				if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
				key=key.Trim();
				if ( key.IndexOf('.') != -1 ) {
					// It's formatted to be a set from a subsidiary SPB.
					string myKey = key.Substring(0,key.IndexOf('.'));
					string subsKey = key.Substring(key.IndexOf('.')+1);
					object subbag = GetContentsOfKey(myKey);
				    SmartPropertyBag bag = subbag as SmartPropertyBag;
				    if ( bag != null ) {
						bag[subsKey] = value;
					} else {
						string msg = "SmartPropertyBag called with key, \"" + key + "\", but the contents " +
							"of key \"" + myKey + "\" was not a SmartPropertyBag, so the key \"" + subsKey +
							"\" cannot be retrieved from it.";
						throw new SmartPropertyBagContentsException(msg);
					}
				} else { // The specified contents are located in this SPB
					try {
						object val = GetContentsOfKey(key);
					    SPBValueHolder holder = val as SPBValueHolder;
					    if ( holder != null ) holder.SetValue(Convert.ToDouble(value));
						else if ( val is SPBStringHolder ) ((SPBStringHolder)val).SetValue((string)value);
						else
						{
						    (val as SPBBooleanHolder)?.SetValue(Convert.ToBoolean(value));
						}
					} catch ( InvalidCastException ){
						throw new SmartPropertyBagContentsException("Error setting value into a SPB. Can set data only into a key " + 
							"that contains a data element convertible to System.Double.");
					} catch ( FormatException ){
						throw new SmartPropertyBagContentsException("Error setting value into a SPB. Must be convertible to a double.");
					}
				}
			}
		}

		/// <summary>
		/// Retrieves the contents of a key known to exist in THIS SPB.
		/// Throws a SmartPropertyBagContentsException if the key does
		/// not exist in this bag.
		/// </summary>
		/// <param name="key">The key under which the lookup is to be
		/// performed. Compound keys are not permitted, here.</param>
		/// <returns>The contents of the key.</returns>
		protected ISupportsMementos GetContentsOfKey(string key){
			ISupportsMementos contents = (ISupportsMementos)m_dictionary[key];
			if ( contents == null ) {
				string msg = "Application code called SmartPropertyBag with a key, \"" + key + 
					"\", that does not exist in this SmartPropertyBag.";
				throw new SmartPropertyBagContentsException(msg);
			}
			return contents;
		}

		/// <summary>
		/// Returns true if this dictionary (or any dictionary below it)
		/// contains a value stored under this key.
		/// </summary>
		/// <param name="key">The key under which the lookup is to be
		/// performed. Compound keys are not permitted, here.</param>
		/// <returns>The contents of the key.</returns>
		protected bool ExistsKey(string key){
			int firstDotNdx = key.IndexOf('.');
			if ( firstDotNdx != -1 ) {
				string lclKey = key.Substring(0,firstDotNdx);
				string subKey = key.Substring(firstDotNdx+1,key.Length-firstDotNdx-1);
				ISupportsMementos ism = (ISupportsMementos)this[lclKey];
			    SmartPropertyBag bag = ism as SmartPropertyBag;
			    if (bag != null) {
                    return bag.ExistsKey(subKey);
                } else {
                    return false;
                }
			}
			return m_dictionary.Contains(key);
		}

        private void AddSPBEntry(string key, object payload) {
            int firstDotNdx = key.IndexOf('.');
            if (firstDotNdx != -1) {
                string lclKey = key.Substring(0, firstDotNdx);
                string subKey = key.Substring(firstDotNdx + 1, key.Length - firstDotNdx - 1);
                ISupportsMementos ism = (ISupportsMementos)this[lclKey];
                if (ism == null) {
                    AddChildSPB(lclKey, new SmartPropertyBag());
                    ism = (ISupportsMementos)this[lclKey];
                } else if (!(ism is SmartPropertyBag)) {
                    throw new ApplicationException("Attempt to add a key to SmartPropertyBag, " + key + ", but the key, " + lclKey + " already exists, and is a leaf node - no sub-bag can be created.");
                } else {
                    // It's an SPB that already exists, so we're golden.
                }
                ((SmartPropertyBag)ism).AddSPBEntry(subKey, payload);
            } else {
                // No 'dot', so it's a leaf-node add.
                m_dictionary.Add(key, payload);
            }
        }


		/// <summary>
        /// Retrieves the memento of this SPB. Includes all state from this bag,
        /// other bags' aliased entries, and child bags, as well as the mementos
        /// from any entry that implements ISupportsMementos. Optimizations are
        /// applied such that a minimum of computation is required to perform the
        /// extraction.
        /// </summary>
		public IMemento Memento {
			get {
				if ( m_ssh.HasChanged ) {
					m_memento = new SmartPropertyBagMemento(this);
					m_ssh.ReportSnapshot();
				}
				return m_memento;
			}
			set {
				if ( !m_writeLock.IsWritable ) throw new WriteProtectionViolationException(this,m_writeLock);
				if ( !m_ssh.HasChanged && m_memento.Equals(value) ) return;
				SmartPropertyBagMemento spbm = (SmartPropertyBagMemento)value;
				spbm.Load(this);              
			}
		}

		/// <summary>
		/// True if this SPB has changed in any way since the last time it was
		/// snapshotted.
		/// </summary>
		public bool HasChanged => m_ssh.HasChanged;

	    /// <summary>
		/// True if this SPB is capable of reporting its own changes.
		/// </summary>
		public bool ReportsOwnChanges => m_ssh.ReportsOwnChanges;

	    /// <summary>
		/// Returns true if the two SPBs are semantically equal. (In other words,
		/// if both have the same entries, and each evaluates as being '.Equal()'
		/// to its opposite, then the two bags are equal.)
		/// </summary>
		/// <param name="otherGuy">The other SPB. If it is an ISupportsMementos that
		/// is not also a SPB, it will return false.</param>
		/// <returns>True if the two SPBs are semantically equal.</returns>
		public bool Equals(ISupportsMementos otherGuy){
	        SmartPropertyBag spb = otherGuy as SmartPropertyBag;
			if ( m_dictionary.Count!=spb?.m_dictionary.Count ) return false;
			foreach ( DictionaryEntry de in spb.m_dictionary ) {
				if ( !m_dictionary.Contains(de.Key) ) return false;
				ISupportsMementos myValue = (ISupportsMementos)m_dictionary[de.Key];

				if ( !(((ISupportsMementos)de.Value).Equals(myValue))) return false;
			}
			return true;
		}

		private static bool DictionariesAreEqual(IDictionary dict1, IDictionary dict2){
			if ( dict1 == null && dict2 == null ) {
				//Trace.WriteLine("Both are null.");
				return true;
			}
			if ( dict1 == null || dict2 == null ) {
				//Trace.WriteLine("One or the other is null.");
				return false;
			}
			if ( dict1.Count != dict2.Count ) {
				if ( s_diagnostics ) {
					Trace.WriteLine("Two dictionaries have a different item count.");
					foreach ( DictionaryEntry de in dict1 ) Trace.WriteLine(de.Key + ", " + de.Value );
					foreach ( DictionaryEntry de in dict2 ) Trace.WriteLine(de.Key + ", " + de.Value );
				}
				return false;
			}
			foreach ( DictionaryEntry de in dict1 ) {
				//Trace.WriteLine("Comparing " + de.Key.ToString());
				if ( !dict2.Contains(de.Key) ) return false;
				object val1 = de.Value;
				object val2 = dict2[de.Key];
				if ( val1 == null && val2 == null ) continue;
				if ( val1 == null || val2 == null ) return false;
                //Trace.WriteLine("Both have it. One is " + val1.ToString() + ", and the other is " + val2.ToString());
                IDictionary d1 = val1 as IDictionary;
                IDictionary d2 = val2 as IDictionary;
                if ( d1 != null && d2 != null) {
					//Trace.WriteLine("Performing dictionary comparison of " + val1 + " and " + val2 );
					if ( !DictionariesAreEqual(d1, d2)) return false;
				} else {
					// it's an object.
					//Trace.WriteLine("Comparing non-dictionary items " + val1.ToString() + ", and " + val2.ToString());
					if ( !val1.Equals(val2) ) return false;
				}
			}
			return true;
		}

        private class SmartPropertyBagMemento : IMemento {

            #region Private Fields
            private readonly IDictionary m_mementoDict;
			private readonly SmartPropertyBag m_spb;

		    #endregion

            public SmartPropertyBagMemento(SmartPropertyBag spb){
				m_spb = spb;
				if ( spb.m_dictionary.Count <= 10 ) {
					m_mementoDict = new ListDictionary();
				} else {
					m_mementoDict = new Hashtable();
				}
				foreach (DictionaryEntry de in spb.m_dictionary )
				{
				    ISupportsMementos value = de.Value as ISupportsMementos;
				    if ( value != null ) {
						ISupportsMementos val = value;
                        IMemento memento = val.Memento;
                        memento.Parent = this;
						m_mementoDict.Add(de.Key,memento);
					} else {
						throw new ApplicationException("Trying to snapshot a " + de.Value +", which is not snapshottable.");
					}
				}
            }

			public void Load(ISupportsMementos ism){
				SmartPropertyBag spb = (SmartPropertyBag)ism;
				spb.m_dictionary.Clear();
				spb.m_ssh.Clear();

                ////// (FIXED)H_A_C_K: 20070220
                ////// The following is in place because: When reloading unit state, we can, depending on the
                ////// order in which keys appear in the memento dictionary, restore the mixture before restoring
                ////// the volume in which that mixture is contained. This would not be a problem, except that 
                ////// the mixture recalculates its characteristics based on the volume of the container in which
                ////// it is contained - which, if it has not yet been restored, can lead to a null reference. Thus, 
                ////// the h_a_c_k is that we force mixture to restore last, after volume, so that the restoration & 
                //////recalculation has everything it needs. Better solution will be worked on immediately to follow.
                ////ArrayList dictEntries = new ArrayList();
                ////foreach ( DictionaryEntry de in m_mementoDict ) {
                ////    if ( !de.Key.Equals("Mixture") ) {
                ////        dictEntries.Add(de);
                ////    }
                ////}
                ////foreach (DictionaryEntry de in m_mementoDict) {
                ////    if (de.Key.Equals("Mixture")) {
                ////        dictEntries.Add(de);
                ////    }
                ////}
                // FIX (20071124) : An ISupportsMementos implementer can be loaded from a properly-selected IMemento.
                // The problem comes in when that implementer's initialization requires access to another 
                // ISupportsMementos implementer that has not yet been reconstituted. So we are adding an
                // OnLoadCompleted event and IMemento Parent { get; } field to IMemento so that a load method that
                // needs to perform some activity that depends on something else in the deserialization train, can
                // register for a callback after deserialization completes.
                // 

                ArrayList dictEntries = new ArrayList();
                foreach (DictionaryEntry de in m_mementoDict) {
                    dictEntries.Add(de);
                }

                foreach (DictionaryEntry de in dictEntries) {

                    if ( s_diagnostics ) Trace.WriteLine("Reloading " + spb + " with " + de.Key + " = " + de.Value);
					string key = (string)de.Key;
					ISupportsMementos child = ((IMemento)de.Value).CreateTarget();
					((IMemento)de.Value).Load(child);
                    
					spb.AddSnapshottable(key,child);
				}
				spb.m_memento = this;
				spb.m_ssh.HasChanged = false;

			    OnLoadCompleted?.Invoke(this);
			}
            
			public ISupportsMementos CreateTarget(){
				m_spb.m_dictionary.Clear();
				foreach ( DictionaryEntry de in m_mementoDict ) {
					string key = (string)de.Key;
					object val = ((IMemento)de.Value).CreateTarget();
					m_spb.AddSPBEntry(key,val);
				}
				return m_spb;
			}

			public IDictionary GetDictionary(){
				Hashtable retval = new Hashtable();
				foreach ( DictionaryEntry de in m_mementoDict ) {
					retval.Add(de.Key,((IMemento)de.Value).GetDictionary());
				}
				return retval;
			}
			
            public bool Equals(IMemento otheOneMemento){
				if ( otheOneMemento == null ) return false;
				return DictionariesAreEqual(GetDictionary(),otheOneMemento.GetDictionary());
			}

            /// <summary>
            /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
            /// </summary>
            public event MementoEvent OnLoadCompleted;

            /// <summary>
            /// This holds a reference to the memento, if any, that contains this memento.
            /// </summary>
            /// <value></value>
            public IMemento Parent { get; set; }
		}
		#region >>> Serialization Support (incl. IXmlPersistable Members) <<<
        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public virtual void SerializeTo(XmlSerializationContext xmlsc) {
			//base.SerializeTo(node,xmlsc);
			xmlsc.StoreObject("EntryCount",m_dictionary.Count);

			int i = 0;
			foreach ( DictionaryEntry de in m_dictionary ) {
				xmlsc.StoreObject("Entry_"+i,de);
				i++;
			}
		}

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public virtual void DeserializeFrom(XmlSerializationContext xmlsc) {
			//base.DeserializeFrom(xmlsc);
			int entryCount = (int)xmlsc.LoadObject("EntryCount");
			
			for ( int i = 0 ; i < entryCount ; i++ ) {
				DictionaryEntry de = (DictionaryEntry)xmlsc.LoadObject("Entry_"+i);
				AddSPBEntry((string)de.Key,de.Value);
			}
		}
		#endregion
	}

	/// <summary>
	/// A general exception that is fired by a SPB for reasons specific to the SPB.
	/// The message will provide an explanation of the error.
	/// </summary>
    public class SmartPropertyBagException : Exception {
        /// <summary>
        /// Creates a SmartPropertyBagException with a given message.
        /// </summary>
        /// <param name="msg">The message that will be associate with this SmartPropertyBagException</param>
		public SmartPropertyBagException(string msg):base(msg){}
    }

	/// <summary>
	/// An exception that is fired when the SPB is asked to add/remove/retrieve a
	/// key that is inappropriate for some reason. 	The message will provide an
	/// explanation of the error.
	/// </summary>
    public class SmartPropertyBagContentsException : SmartPropertyBagException {
		/// <summary>
		/// Creates a SmartPropertyBagContentsException with a given message.
		/// </summary>
		/// <param name="msg">The message that will be associate with this SmartPropertyBagContentsException</param>
		public SmartPropertyBagContentsException(string msg):base(msg){}
    }
}