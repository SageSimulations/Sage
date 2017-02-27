/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore {
    /// <summary>
    /// Interface IResettable is implemented by any <see cref="IModelObject" /> that can be reset.
    /// </summary>
    public interface IResettable {
        /// <summary>
        /// Performs a reset operation on this instance.
        /// </summary>
        void Reset();
    }
}
