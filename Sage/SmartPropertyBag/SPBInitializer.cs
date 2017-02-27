/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using Highpoint.Sage.Persistence;
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.SimCore {

	/// <summary>
	/// An SPBInitializer is an object that can be used to automatically initialize a
	/// SmartPropertyBag. For example, if the need was to add some user-defined fields to each piece
	/// of equipment (modeled as a SPB) as it was created, the following code would be
	/// useful:
	/// <code>
	/// // Presume that m_initializers was a hashtable of initializers...
	/// public void InitializeEquipment(SOMEquipment equipment){
	///     foreach (SPBInitializer initializer in m_initializers.Values){
	///         initializer.Initialize(equipment);
	///     }
	/// }
	/// </code>
	/// This class is typically derived-from, with serial number incrementation, driver
	/// or operator name assignment, etc, provided in a custom method.
	/// <p></p>
	/// <b>Note : </b>The initializer, once created, is configured to initialize only
	/// one way, e.g. creating an entry called 'Mixer' that is set to "Off".
	/// </summary>
	public class SPBInitializer : IXmlPersistable {
		//TODO: Make this an interface, and the bool, string and double, implementations in classes.

		/// <summary>
		/// The key to be initialized when this initializer runs.
		/// </summary>
		public string Key { get; set; }

	    /// <summary>
		/// The boolean value to be placed under the specified key in the SPB when
		/// this initializer runs.
		/// </summary>
		public bool   BoolValue { get; set; }

	    /// <summary>
		/// The string value to be placed under the specified key in the SPB when
		/// this initializer runs.
		/// </summary>
		public string StringValue { get; set; }

	    /// <summary>
		/// The double value to be placed under the specified key in the SPB when
		/// this initializer runs.
		/// </summary>
		public double DoubleValue { get; set; }

	    /// <summary>
		/// If this is true, and a SPB is being initialized, the field will be created
		/// if it does not already exist.
		/// </summary>
	    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
		public bool   ForceCreation { get; set; }

	    public enum InitType { Bool, Double, String }
		private InitType m_initType;

	    /// <summary>
		/// Creates a SPBInitializer with the specified key and value, with a
		/// flag indicating to force the creation of the field if necessary.
		/// </summary>
		/// <param name="key">The key that, in a SPB, will be initialized
		/// by this initializer.</param>
		/// <param name="val">The double value that will be initialized into
		/// the specified field.</param>
		/// <param name="forceCreation">If this is true, the field will be
		/// created in the SPB if it does not already exist.</param>
		public SPBInitializer(string key, double val, bool forceCreation) {
			Key = key;
			DoubleValue = val;
			ForceCreation = forceCreation;
			m_initType = InitType.Double;
		}

		/// <summary>
		/// Returns the type of this initializer, whether _bool, _double, or _string.
		/// </summary>
		public InitType InitializationType => m_initType;

	    /// <summary>
		/// Creates a SPBInitializer with the specified key and value, with a
		/// flag indicating to force the creation of the field if necessary.
		/// </summary>
		/// <param name="key">The key that, in a SPB, will be initialized by
		/// this initializer.</param>
		/// <param name="val">The double value that will be initialized into the
		/// specified field.</param>
		/// <param name="forceCreation">If this is true, the field will be created
		/// in the SPB if it does not already exist.</param>
		public SPBInitializer(string key, bool val, bool forceCreation)	{
			Key = key;
			BoolValue = val;
			ForceCreation = forceCreation;
			m_initType = InitType.Bool;
		}
		/// <summary>
		/// Creates a SPBInitializer with the specified key and value, with a
		/// flag indicating to force the creation of the field if necessary.
		/// </summary>
		/// <param name="key">The key that, in a SPB, will be initialized by 
		/// this initializer.</param>
		/// <param name="val">The string value that will be initialized into 
		/// the specified field.</param>
		/// <param name="forceCreation">If this is true, the field will be 
		/// created in the SPB if it does not already exist.</param>
		public SPBInitializer(string key, string val, bool forceCreation) {
			Key = key;
			StringValue = val;
			ForceCreation = forceCreation;
			m_initType = InitType.String;
		}

		/// <summary>
		/// Initializes a SPB with the type, name and contents of the field
		/// depicted in this SmartPropertyBagInitializer.
		/// </summary>
		/// <param name="spb">The SmartPropertyBag to be initialized.</param>
		public void Initialize(SmartPropertyBag spb){
			string[] keys = Key.Split('.');
		    SmartPropertyBag theSpb = spb;
		    int i;
			for ( i = 0 ; i < keys.Length-1 ; i++ ) {
				try {
					SmartPropertyBag parent = theSpb;
					theSpb = (SmartPropertyBag)theSpb[keys[i]];
					if ( theSpb == null ) {
						if ( ForceCreation ) {
							theSpb = new SmartPropertyBag();
							parent.AddChildSPB(keys[i],theSpb);
						} else {
							throw new SmartPropertyBagException("Attempt to navigate to spb named " + keys[i] + ", which does not exist.");
						}
					}
				} catch ( InvalidCastException ) {
					throw new SmartPropertyBagException("Cannot add a child to sub-property " + keys[i] + " since it is not a SmartPropertyBag.");
				}
			}
               
			// If the SPB already has an entry with the specified key, set the value.
			if ( theSpb.Contains(keys[i]) ) {
				switch ( m_initType ) {
					case InitType.Bool:	theSpb[keys[i]] = BoolValue;		break;
					case InitType.Double:	theSpb[keys[i]] = DoubleValue;	break;
					case InitType.String:	theSpb[keys[i]] = StringValue;	break;
				}
			} else { // If it does not have such an entry, and we've been told to force creation,
				// then we create a new entry and set the value.
				if ( ForceCreation ) {
					switch ( m_initType ) {
						case InitType.Bool:	theSpb.AddBoolean(keys[i],BoolValue);	break;
						case InitType.Double:	theSpb.AddValue(keys[i],DoubleValue);	break;
						case InitType.String:	theSpb.AddString(keys[i],StringValue);	break;
					}
				}
			}
		}
	
		/// <summary>
		/// Constructor for use in initialization.
		/// </summary>
		public SPBInitializer(){}

		#region IXmlPersistable Members
		public virtual void SerializeTo(XmlSerializationContext xmlsc) {
			xmlsc.StoreObject("key",Key);
			xmlsc.StoreObject("InitType",m_initType);
			switch ( m_initType ) {
				case InitType.Bool: {
					xmlsc.StoreObject("Value",BoolValue);		break;
				}
				case InitType.Double: {
					xmlsc.StoreObject("Value",DoubleValue);	break;
				}
				case InitType.String: {
					xmlsc.StoreObject("Value",StringValue);	break;
				}
				default: {
					throw new ApplicationException("Serialization attempted on unknown type of InitType");
				}
			}
		}

		public virtual void DeserializeFrom(XmlSerializationContext xmlsc) {
			Key = (string)xmlsc.LoadObject("key");
			m_initType = (InitType)xmlsc.LoadObject("InitType");
			switch ( m_initType ) {
				case InitType.Bool: {
					BoolValue = (bool)xmlsc.LoadObject("Value");		break;
				}
				case InitType.Double: {
					DoubleValue = (double)xmlsc.LoadObject("Value");	break;
				}
				case InitType.String: {
					StringValue = (string)xmlsc.LoadObject("Value");	break;
				}
				default: {
					throw new ApplicationException("deserialization attempted on unknown type of InitType");
				}

			}
		}

		#endregion
		/*
		private InitType m_initType;
		private string m_key;            // "Child.Grandchild.PropertyName"
		private bool   m_boolValue;      // bool
		private string m_stringValue;    // "Fred"
		private double m_doubleValue;    // 4.5
		private bool   m_forceCreation;  // true to create Children if necessary.
		*/

	}

	public class SPBInitializerComparer : IComparer {
		#region IComparer Members

		public int Compare(object x, object y) {
            SPBInitializer spbiX = (SPBInitializer)x;
            SPBInitializer spbiY = (SPBInitializer)y;
			return Comparer.Default.Compare(spbiX.Key,spbiY.Key);
		}

		#endregion

	}
}