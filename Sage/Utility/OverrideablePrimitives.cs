/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility {
	
	#region >>> Overridable Value Classes <<<
	/// <summary>
	/// A struct that shadows a double in, for example, a temperature controller, and indicates
	/// whether that double is to be read as its default state, or as an overridden value.
	/// </summary>
	public struct OverrideDouble {
        private double m_doubleVal;

        /// <summary>
        /// Indicates true if this object's initial value has been overridden.
        /// </summary>
        public bool Override { get; set; }

	    /// <summary>
		/// The double value contained in this Overridable. Override is set to true if this value is set.
		/// </summary>
		public double DoubleValue { get { return m_doubleVal; } set { Override = true; m_doubleVal = value; } }
	}

    /// <summary>
    /// A struct that shadows a boolean in, for example, a temperature controller, and indicates
    /// whether that boolean is to  is to be read as its default state, or as an overridden value.
    /// </summary>
    public struct OverrideBool {
		private bool m_boolValue;

	    /// <summary>
	    /// Indicates true if this object's initial value has been overridden.
	    /// </summary>
	    public bool Override { get; set; }

	    /// <summary>
		/// The bool value contained in this Overridable. Override is set to true if this value is set.
		/// </summary>
		public bool BoolValue { get { return m_boolValue; } set { Override = true; m_boolValue = value; } }
	}
	#endregion
}
