/* This source code licensed under the GNU Affero General Public License */
#if INCLUDE_WIP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Highpoint.Sage.Persistence {

    /// <summary>
    /// This is used to support the close/save mechanism.
    /// </summary>
    public interface IDirtyable {
        bool IsDirty { get; }
        event Func<bool> DirtyableComponent;
    }
}
#endif