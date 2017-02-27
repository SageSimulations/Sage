/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility
{
	/// <summary>
	/// This interface is implemented by an object that is required to support index numbers.
	/// That is, another (perhaps manager) object will want to refer to objects of this type
	/// by index numbers that it, itself, assigns.
	/// </summary>
	public interface ISupportsIndexes {
		/// <summary>
		/// A growable array of index number locations.
		/// </summary>
		uint[] Index { get; }
		/// <summary>
		/// Causes the object to grow its index array.
		/// </summary>
		void GrowIndex();
	}

	/// <summary>
	/// Implemented by a class that is capable of allocating &amp; assigning index slots in a population of ISupportsIndexes objects.
	/// </summary>
	public interface IIndexingService {
		/// <summary>
		/// Acquires a slot in the index number array for the caller's use.
		/// </summary>
		/// <param name="tgts">An array of the objects that are to be included in this index - they must all implement the ISupportsIndexes interface.</param>
		/// <returns></returns>
		uint GetIndexSlot(object[] tgts);

        /// <summary>
        /// Acquires a slot in the index number array for the caller's use.
        /// </summary>
        /// <param name="tgts">The objects for whom an index slot is desired.</param>
        /// <returns></returns>
		uint GetIndexSlot(ISupportsIndexes[] tgts);
	}

	public class BasicIndexingService : IIndexingService {

        /// <summary>
        /// Acquires a slot in the index number array for the caller's use.
        /// </summary>
        /// <param name="tgts">An array of the objects that are to be included in this index - they must all implement the ISupportsIndexes interface.</param>
        /// <returns>System.UInt32.</returns>
        /// <exception cref="ApplicationException"></exception>
        public uint GetIndexSlot(object[] tgts){
			ISupportsIndexes[] tmp = new ISupportsIndexes[tgts.Length];
			try {
				for ( int i = 0 ; i < tgts.Length ; i++ ) {
                    tmp[i] = (ISupportsIndexes)tgts[i];
				}			
			} catch ( InvalidCastException ) {
				throw new ApplicationException(_nonSfmmoIndexRequested);
			}
			return GetIndexSlot(tmp);
		}

        /// <summary>
        /// Acquires a slot in the index number array for the caller's use.
        /// </summary>
        /// <param name="tgts">The objects for whom an index slot is desired.</param>
        /// <returns>System.UInt32.</returns>
        /// <exception cref="Highpoint.Sage.Utility.IndexingFailedException"></exception>
        public uint GetIndexSlot(ISupportsIndexes[] tgts){
				int i;
				uint minNdxSize = uint.MaxValue;
				for ( i = 0 ; i < tgts.Length ; i++ ) {
					ISupportsIndexes sfamo = tgts[i];
					if ( sfamo.Index == null ) sfamo.GrowIndex();
					minNdxSize = (uint)Math.Min(minNdxSize,sfamo.Index.Length);
				}

				uint assigned = uint.MaxValue;
				while ( assigned == uint.MaxValue ) {
					bool[] inUse = new bool[minNdxSize];
					Array.Clear(inUse,0,inUse.Length);
					for ( i = 0 ; i < tgts.Length ; i++ ) {
						uint[] ia = tgts[i].Index;
						for ( int j = 0; j < minNdxSize ; j++ ) {
							inUse[j] &= (ia[j]>0); // TODO: This is gonna be much faster w/ pointer arithmetic.
						}
					}

					for ( i = 0 ; i < tgts.Length ; i++ ) {
						if ( !inUse[i] ) {
							assigned = (uint)i;
							break;
						}
					}
				}

				if ( assigned == uint.MaxValue ) throw new IndexingFailedException(_indexingFailed);
				
				return assigned;
		}

		private static string _nonSfmmoIndexRequested = "Index requested on an object that is not an implementer of IModelObject.";
		private static string _indexingFailed = "Indexing failed to obtain a requested indexing slot.";
		
	}

    /// <summary>
    /// Class IndexingFailedException - thrown when indexing has failed.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class IndexingFailedException : Exception {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingFailedException"/> class.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public IndexingFailedException(string msg):base(msg){}
	}
}
