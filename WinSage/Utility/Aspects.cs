/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// Defines support for attaching descriptive aspects to objects.
    /// </summary>
    public interface IHasAspects {
        bool HasAspect(object aspectType);
        void AddAspect(IAspect aspect);
        void RemoveAspect(object aspectType);
        void RemoveAspect(IAspect aspect);
        void ClearAspects();
        ICollection Aspects { get; }
    }

    public interface IBlendable {
        void Blend(IBlendable withWhat);
    }

    public interface ISeparable {
        ISeparable Separate(object how);
    }

    public interface IAspect : ICloneable, IBlendable, ISeparable {
        ICollection Constituents { get; }
        object AspectType { get; }
        string Name { get; }
        object Value { get; }
        double Weight { get; }
    }
}
