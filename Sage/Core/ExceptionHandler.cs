/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// Delegate ExceptionHandler is implemented by anything that can handle an exception.
    /// If [true] is returned, the exception is not propagated any further.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="e">The e.</param>
    /// <param name="handled">if set to <c>true</c> [handled].</param>
    public delegate void ExceptionHandler(IModel model, Exception e, out bool handled);

}
