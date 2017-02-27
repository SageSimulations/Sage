/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// Struct NameValuePair contains a string name, and an object value.
    /// </summary>
    public struct NameValuePair {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValuePair"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public NameValuePair(string name, object value) {
            Name = name;
            Value = value;
        }
    }
    /// <summary>
    /// Struct NameValuePair contains a string name, and a value of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct NameValuePair<T> {
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public T Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValuePair{T}"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public NameValuePair(string name, T value) {
            Name = name;
            Value = value;
        }
    }
}
