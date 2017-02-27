/* This source code licensed under the GNU Affero General Public License */
#if NODELETE
using System;
using System.Collections;

namespace Highpoint.Sage.Utility {
	/// <summary>
	/// Implemented by something that has aspects. Aspects are attached to
	/// objects and describe some aspect of that object such as the source of a material.
	/// Aspects are of certain types - in the example above, the object would have
	/// an aspect of type "Source". That aspect may retain several values,
	/// and these values are carried through the changes in that material by its implementation
	/// of the IBlendable and ICloneable interfaces.
	/// </summary>
	public interface IHasAspects {
		/// <summary>
		/// Returns true if this object has an aspect of the specified type.
		/// </summary>
		/// <param name="aspectType">The type of aspect such as "Source".</param>
		/// <returns>True if this object has an aspect of the specified type.</returns>
		bool HasAspect(object aspectType);

		/// <summary>
		/// Adds an aspect of the specified type.
		/// </summary>
		/// <param name="aspect">The aspect to be added. If there are existing
		/// aspects of the same type already attached to this object, the new one 
		/// will be blended in, in the amount appropriate to the new and existing aspects'
		/// weighting values.</param>
		void AddAspect(IAspect aspect);

		/// <summary>
		/// Removes all aspects that have the given type from this object.
		/// </summary>
        /// <param name="aspectType">The aspect type to be removed.</param>
		void RemoveAspect(object aspectType);

		/// <summary>
		/// Removes the specified aspect from this object. If there are more than one
		/// aspect of this type, the aspect or aspects that are Equal()
		/// to the provided aspect will be separated from the others, and the others
		/// will remain.
		/// </summary>
		/// <param name="aspect">The aspect to be removed.</param>
		void RemoveAspect(IAspect aspect);

		/// <summary>
		/// Removes all aspects associated with this object.
		/// </summary>
		void ClearAspects();

		/// <summary>
		/// A collection of all of the aspects of this object. This is a read-only
		/// collection.
		/// </summary>
		ICollection Aspects { get; }
	}

	/// <summary>
	/// This interface is implemented by objects that can be blended, and while two or more
	/// such objects that have been blended then take on the same fundamental identity, some 
	/// aspect of them, such as the source from which a material was drawn (such as 
	/// Source:CityWater, Source:DeionizedWater), or the lot number of a batch of drug 
	/// product (Lot:12654398,Lot:99864832), is retained from both. 
	/// </summary>
	public interface IBlendable {
		/// <summary>
		/// Blends this object with the other object, (typically of the same type),
		/// which typically retains some aspects of each original object. The blend
		/// operation is commutative - (objA.Blend(ObjB).Equals(B.Blend(ObjA)) is true.
		/// </summary>
		/// <param name="withWhat">The object to be blended into this one.</param>
		void Blend(IBlendable withWhat);
	}

	/// <summary>
	/// Implemented by an object that can be separated into two parts, each of which is
	/// fundamentally the same, but may have been separated according to some aspect of
	/// the object. For example, a substance that has been separated by granule size, or
	/// a combined lot of drug product that has been separated by lot number, would
	/// implement ISeparable.
	/// --> Note: This will be stubbed out until we need it.
	/// </summary>
	public interface ISeparable {
		/// <summary>
		/// Separates an object into two parts, based on some criteria.
		/// </summary>
		/// <param name="how">The means by which the separation will occur.</param>
		/// <returns></returns>
		ISeparable Separate(object how);
	}

	/// <summary>
	/// An aspect is a composite object that has a specified type. An aspect characterizes
	/// an object in a way or ways that are not fundamental to its data or behavior, but
	/// are secondary in nature. Water is water regardless of where it came from, but adding
	/// the aspect type "Source", a value of "City" and a weight of 45 to a water instance
	/// that is 45 kg, describes its source. Afterward, adding 30 kg of water that has an 
	/// aspect of type "Source", a value of "Deionized" and a weight of 30, updates the resultant
	/// water object to reflect that its "Source" geneology is 45/(45+30) derived from "City" and
	/// 30/(45+30) derived from "Deionized".
	/// </summary>
	public interface IAspect : ICloneable, IBlendable, ISeparable {
		ICollection Constituents { get; }
		object AspectType { get; }
		string Name { get; }
		object Value { get; }
		double Weight { get; }
	}
}
#endif