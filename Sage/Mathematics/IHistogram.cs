/* This source code licensed under the GNU Affero General Public License */

using System;
using Highpoint.Sage.SimCore;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics {

    /// <summary>
    /// Returns true if the data in a certain Histogram bin meet a certain criteria.
    /// </summary>
    /// <param name="data">The histogram data.</param>
    /// <param name="coordinates">The coordinates of the bin in the data.</param>
    /// <returns>True if the data in a certain Histogram bin meet a certain criteria.</returns>
	public delegate bool HistogramDataFilter(Array data, int[] coordinates);
	/// <summary>
	/// Returns a string that characterizes a bin in a Histogram located at the given dimensional coordinates.
	/// </summary>
	/// <param name="coordinates">The coordinates of the bin whose label is desired.</param>
    /// <returns>A string that characterizes a bin in a Histogram located at the given dimensional coordinates.</returns>
    public delegate string LabelProvider(int[] coordinates);


	/// <summary>
	/// A flag enumerator that specifies which bins in a histogram the caller is referring to.
	/// </summary>
    [ Flags ]
	public enum HistogramBinCategory : byte { 
        /// <summary>
        /// No bins are to be, or were, included in the operation.
        /// </summary>
        None = 0,
        /// <summary>
        /// Only the off-scale-low bin is to be, or was, included in the operation.
        /// </summary>
        OffScaleLow = 0x01,
        /// <summary>
        /// All in-range bins are to be, or were, included in the operation.
        /// </summary>
        InRange=0x02,
        /// <summary>
        /// Only the off-scale-high bin is to be, or was, included in the operation.
        /// </summary>
        OffScaleHigh=0x04,
        /// <summary>
        /// All bins are to be, or were, included in the operation.
        /// </summary>
        All=0x07 }


    /// <summary>
    /// Implemented by an object that processes raw data items into bins and presents some
    /// basic statistics on those bins.
    /// </summary>
	public interface IHistogram : IHasIdentity {
        /// <summary>
        /// Gets or sets the raw data that comprises this Histogram.
        /// </summary>
        /// <value>The raw data.</value>
		Array RawData { get; set; }
        /// <summary>
        /// Gets the bins that are a part of this Histogram.
        /// </summary>
        /// <value>The bins.</value>
		Array Bins { get; }
        /// <summary>
        /// Gets the number of dimensions in this Histogram (a linear histogram is 1-dimensional.
        /// </summary>
        /// <value>The dimension.</value>
		int   Dimension{ get; }
        /// <summary>
        /// Clears this Histogram.
        /// </summary>
		void  Clear();
        /// <summary>
        /// Recalculates this Histogram, resulting in new bins and counts.
        /// </summary>
		void  Recalculate();
        /// <summary>
        /// Recalculates the Histogram with new high &amp; low bounds, resulting in new bins and counts.
        /// </summary>
        /// <param name="lowBounds">The low bounds of the Histogram.</param>
        /// <param name="highBounds">The high bounds of the Histogram.</param>
        /// <param name="nBins">The number of bins.</param>
		void  Recalculate(Array lowBounds, Array highBounds, int nBins);
        /// <summary>
        /// Counts the entries in the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>The number of entries in the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.</returns>
		int CountEntries(HistogramBinCategory hbc);
        /// <summary>
        /// Counts the entries in the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns></returns>
		int CountEntries(int[] lowBounds, int[] highbounds);
//		int EquivalentEntries(HistogramBinCategory hbc); 
//		int EquivalentEntries(int[] lowBounds, int[] highbounds); 
        /// <summary>
        /// Returns the index of the biggest bin in each dimension, among the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>. 
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>he index of the biggest bin in each dimension, among the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.</returns>
		int[] BiggestBin(HistogramBinCategory hbc);
        /// <summary>
        /// Returns the index of the biggest bin in each dimension, among the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns>Tthe index of the biggest bin in each dimension, among the bins identified by the given low and high bounds.</returns>
		int[] BiggestBin(int[] lowBounds, int[] highbounds);
        /// <summary>
        /// Returns the index of the smallest bin in each dimension, among the bins identified  the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>. 
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>The index of the smallest bin in each dimension, among the bins identified  the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.</returns>
        int[] SmallestBin(HistogramBinCategory hbc);
        /// <summary>
        /// Returns the index of the smallest bin in each dimension, among the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns>Tthe index of the smallest bin in each dimension, among the bins identified by the given low and high bounds.</returns>
        int[] SmallestBin(int[] lowBounds, int[] highbounds);

        /// <summary>
        /// Gets the low bound of the Histogram.
        /// </summary>
        /// <value>The low bound.</value>
		object LowBound { get; }
        /// <summary>
        /// Gets the high bound of the Histogram..
        /// </summary>
        /// <value>The high bound.</value>
		object HighBound { get; }

        /// <summary>
        /// Gets or sets the label provider.
        /// </summary>
        /// <value>The label provider.</value>
		LabelProvider LabelProvider { get; set; }
        /// <summary>
        /// Gets the label for the bin at the specified coordiantes.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>
        /// The label for the bin at the specified coordiantes.
        /// </returns>
        string GetLabel(int[] coordinates);
        /// <summary>
        /// This returns a value that indicates how far a specified bin's count
        /// deviates from the 'expected' count - note that it is only relevant if
        /// the histogram was expected to have been uniform.
        /// </summary>
        /// <param name="coordinates">An integer array that specifies the coordinates of
        /// the bin of interest. Histogram analysis of a Histogram1D_&lt;anything&gt; must be
        /// on a 1 dimensional array, therefore, this array must be of rank 1.
        /// </param>
        /// <returns> a value that indicates how far a specified bin's count
        /// deviates from the 'expected' count.</returns>
        object Error(int[] coordinates);

        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>The sum of values.</returns>
        object SumEntries(HistogramBinCategory hbc);

        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The high bounds.</param>
        /// <returns>The sum of values.</returns>
        object SumEntries(int[] lowBounds, int[] highbounds);//
	}
}



