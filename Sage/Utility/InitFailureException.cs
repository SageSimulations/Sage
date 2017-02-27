/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility {
	/// <summary>
	/// Marker class for use as an exception fired on initialization failure.
	/// </summary>
	public class InitFailureException : Exception {
        /// <summary>
        /// Creates a new instance of the <see cref="T:InitFailureException"/> class.
        /// </summary>
        /// <param name="msg">The message to be reported.</param>
		public InitFailureException(string msg) : base(msg) {}
	}
}
