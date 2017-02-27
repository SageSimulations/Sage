/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore {

	/// <summary>
	/// Implemented in a method that is to be called after a cloning operation occurs.
	/// </summary>
    public delegate void CloneHandler(object original, object clone);

	/// <summary>
	/// ICloneable is implemented by an object that is capable of being cloned.
	/// </summary>
    public interface ICloneable {

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>System.Object.</returns>
        object Clone();

		/// <summary>
		/// CloneHandler is an event that is fired after a cloning operation is completed.
		/// </summary>
        event CloneHandler CloneEvent;
    }
}