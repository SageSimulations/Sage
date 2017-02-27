/* This source code licensed under the GNU Affero General Public License */
using System;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global

// TODO: Convert this to Tempated types.

namespace Highpoint.Sage.Mathematics {
	/// <summary>
	/// A Base class for a 1-dimensional histogram. Since this derives from a base-level interface
	/// that is intended for all histograms, indices are specified as an array of integers. So for
	/// a 1-D histogram, bin #3 would be referred to as having index int[]{3}. For a 2-D histogram,
	/// bin 4,2 would be referred to as having index int[]{4,2}. In addition, bins are separated into
	/// three categories, None, OffScaleLow, InRange, OffScaleHigh, and All. These are flags that can
	/// be and'ed together. Most queries can be applied to a range of bins, or to a full category or
	/// set of categories.
	/// </summary>
	public abstract class Histogram1D_Base : IHistogram {

		#region >>> Local Private Variables. <<<
        /// <summary>
        /// The raw data array that provides the underlying histogram data.
        /// </summary>
		protected Array m_rawData;
		/// <summary>
		/// The bins that contain the count of points in each bin.
		/// </summary>
        protected int[] m_bins;
        /// <summary>
        /// The count of data points whose values were less than the low bound.
        /// </summary>
		protected int LowBin;
        /// <summary>
        /// The count of data points whose values were greater than the high bound.
        /// </summary>
        protected int HighBin;
        /// <summary>
        /// The number of bins in this Histogram.
        /// </summary>
		protected int NumBins;
        /// <summary>
        /// The value of the low boundary. All data points that are less than this value are tallied into the m_lowBin bin.
        /// </summary>
		protected object m_lowBound;
        /// <summary>
        /// The value of the high boundary. All data points that are greater than this value are tallied into the m_highBin bin.
        /// </summary>
        protected object m_highBound;
		private LabelProvider m_labelProvider;
		#endregion
		
		/// <summary>
		/// Creates a 1D histogram.
		/// </summary>
		/// <param name="rawData">An array of 1D data that contains the data to be binned.</param>
		/// <param name="lowBound">The data that represents the low bound of the histogram.</param>
		/// <param name="highBound">The data that represents the high bound of the histogram.</param>
		/// <param name="nBins">The number of bins that the data will pe placed in, between lo-bound and hi-bound.</param>
		/// <param name="name">The name of the histogram.</param>
		/// <param name="guid">The guid of the histogram.</param>
		// ReSharper disable once PublicConstructorInAbstractClass
		public Histogram1D_Base(Array rawData, object lowBound, object highBound, int nBins, string name, Guid guid){
			m_rawData = rawData;
			m_lowBound = lowBound;
			m_highBound = highBound;
			m_name = name;
			Guid = guid;
			NumBins = nBins;
			m_labelProvider = DefaultLabelProvider;
		}
		
		#region IHistogram Members
		/// <summary>
		/// Counts the number of entries in a given range (low bin, in-band bins or high bin.)
		/// </summary>
		/// <param name="hbc">An enumerator that describes whether the count is for low, in-band, or high bins.</param>
		/// <returns>The number of entries that fall in the specified range.</returns>
		public int CountEntries(HistogramBinCategory hbc) {
			int nEntries = 0;
			if ( hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All ) nEntries += LowBin;
			if ( hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All ) nEntries += HighBin;
			if ( hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All ) nEntries += CountEntries(new[]{0},new[]{NumBins});
			return nEntries;
		}

		/// <summary>
		/// Counts the number of entries in a given range of bins.
		/// </summary>
		/// <param name="lowBounds">The index of the lowest bin to count.</param>
		/// <param name="highbounds">The index of the highest bin to count.</param>
		/// <returns>The number of entries that fall in the specified range.</returns>
		public int CountEntries(int[] lowBounds, int[] highbounds) {
			if ( lowBounds.Rank != 1 || highbounds.Rank != 1) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");
			int low = lowBounds[0];
			int high = highbounds[0];
			int nEntries = 0;
			for ( int i = low ; i < high ; i++ ) {
				nEntries += m_bins[i];
			}
			return nEntries;
		}

		/// <summary>
		/// The data that represents the low bound of the in-band range.
		/// </summary>
		public object LowBound => m_lowBound;

	    /// <summary>
		/// The data that represents the high bound of the in-band range.
		/// </summary>
		public object HighBound => m_highBound;

	    /// <summary>
		/// Returns the index of the bin that contains the most entries, selected from
		/// a specified set of bins.
		/// </summary>
        /// <param name="hbc">The <see cref="HistogramBinCategory"/> that specifies the bins of interest.</param>
        /// <returns>The index of the bin that contains the most entries.</returns>
		public int[] BiggestBin(HistogramBinCategory hbc) {
			int biggestBinNum = 0;
			int biggestBinCount = int.MaxValue;
		    if (hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All)
		    {
		        biggestBinNum = BiggestBin(new[] {0}, new[] {m_bins.Length})[0];
		        biggestBinCount = m_bins[biggestBinNum];
		    }

		    if ( hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All ) {
				if ( LowBin > biggestBinCount ) {
					biggestBinNum = int.MinValue;
					biggestBinCount = LowBin;
				}
			}
			if ( hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All ) {
				if ( HighBin > biggestBinCount ) {
					biggestBinNum = int.MaxValue;
				}
			}
			return new[]{biggestBinNum};
		}

        /// <summary>
        /// Returns the index of the bin that contains the most entries, selected from
        /// the bins between the requested low and high index bins.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns>
        /// The indexes of the bin that contains the most entries.
        /// </returns>
        public int[] BiggestBin(int[] lowBounds, int[] highbounds) {
			if ( lowBounds.Rank != 1 || highbounds.Rank != 1) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");
			int low = lowBounds[0];
			int high = highbounds[0];
			int biggestBinNum = 0;
			int biggestBinCount = int.MinValue;
			for ( int i = low; i < high ; i++ ) {
				if ( m_bins[i] >= biggestBinCount ) continue;
				biggestBinCount = m_bins[i];
				biggestBinNum = i;
			}
			return new []{biggestBinNum};
		}

        /// <summary>
        /// Returns the index of the bin that contains the most entries, selected from
        /// a specified set of bins.
        /// </summary>
        /// <param name="hbc">The <see cref="HistogramBinCategory"/> that specifies the bins of interest.</param>
        /// <returns>The indexes of the bin that contains the fewest entries.</returns>
        public int[] SmallestBin(HistogramBinCategory hbc) {
			int smallestBinNum = 0;
			int smallestBinCount = int.MaxValue;
			if ( hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All ) {
				smallestBinNum = SmallestBin(new[]{0},new[]{m_bins.Length})[0];
				smallestBinCount = m_bins[smallestBinNum];
			}
			 
			if ( hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All ) {
				if ( LowBin < smallestBinCount ) {
					smallestBinNum = int.MinValue;
					smallestBinCount = LowBin;
				}
			}
			if ( hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All ) {
				if ( HighBin < smallestBinCount ) {
					smallestBinNum = int.MaxValue;
				}
			}
			return new[]{smallestBinNum};
		}

        /// <summary>
        /// Returns the index of the bin that contains the fewest entries, selected from
        /// the bins between the requested low and high index bins.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns>
        /// The indexes of the bin that contains the fewest entries.
        /// </returns>
        public int[] SmallestBin(int[] lowBounds, int[] highbounds) {
			if ( lowBounds.Rank != 1 || highbounds.Rank != 1) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");
			int low = lowBounds[0];
			int high = highbounds[0];
			int smallestBinNum = 0;
			int smallestBinCount = int.MaxValue;
			for ( int i = low; i < high ; i++ ) {
				if ( m_bins[i] >= smallestBinCount ) continue;
				smallestBinCount = m_bins[i];
				smallestBinNum = i;
			}
			return new[]{smallestBinNum};
		}


        /// <summary>
        /// Gets and sets the object that provides the name of a specified bin.
        /// </summary>
        /// <value>The label provider.</value>
		public LabelProvider LabelProvider { 
			get{ return m_labelProvider;  } 
			set{ m_labelProvider = value; }
		}

        /// <summary>
        /// Gets the label for the bin at the specified coordiantes.
        /// </summary>
        /// <param name="coords">The coordinates of the desired bin.</param>
        /// <returns>The label for the bin at the specified coordiantes.</returns>
		public string GetLabel(int[] coords){
			return m_labelProvider(coords);
		}

        /// <summary>
        /// Gets or sets the raw data that comprises this Histogram.
        /// </summary>
        /// <value>The raw data.</value>
		public Array RawData {
			get {
				return m_rawData;
			}
			set {
				if ( value.Rank != 1) throw new ArgumentException("Raw data set into a Histogram1D must be of rank 1.");
				Clear();
				m_rawData = value;
				Recalculate();
			}
		}

        /// <summary>
        /// Gets the bins that are a part of this Histogram.
        /// </summary>
        /// <value>The bins.</value>
		public Array Bins => m_bins;

	    /// <summary>
        /// Gets the number of dimensions in this Histogram (a linear histogram is 1-dimensional).
        /// </summary>
        /// <value>The dimension.</value>
		public int Dimension => 1;

	    /// <summary>
        /// Clears this Histogram.
        /// </summary>
		public virtual void Clear() {
			m_bins = null;
			LowBin = 0;
			LowBin = 0;
		}

        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given <see cref="HistogramBinCategory"/>.
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>The sum of values.</returns>
		public abstract object SumEntries(HistogramBinCategory hbc);
        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The high bounds.</param>
        /// <returns>The sum of values.</returns>
		public abstract object SumEntries(int[] lowBounds, int[] highbounds);
        /// <summary>
        /// This returns a value that indicates how far a specified bin's count
        /// deviates from the 'expected' count - note that it is only relevant if
        /// the histogram was expected to have been uniform.
        /// </summary>
        /// <param name="coordinates">An integer array that specifies the coordinates of
        /// the bin of interest. Histogram analysis of a Histogram1D_&lt;anything&gt; must be
        /// on a 1 dimensional array, therefore, this array must be of rank 1.</param>
        /// <returns>
        /// a value that indicates how far a specified bin's count
        /// deviates from the 'expected' count.
        /// </returns>
		public abstract object Error(int[] coordinates);

        /// <summary>
        /// Recalculates this Histogram, resulting in new bins and counts.
        /// </summary>
        public abstract void Recalculate();
        /// <summary>
        /// Recalculates the Histogram with new high &amp; low bounds, resulting in new bins and counts.
        /// </summary>
        /// <param name="lowBounds">The low bounds of the Histogram.</param>
        /// <param name="highBounds">The high bounds of the Histogram.</param>
        /// <param name="nBins">The number of bins.</param>
		public abstract void Recalculate(Array lowBounds, Array highBounds, int nBins);
        /// <summary>
        /// Provides the default label provider for the specified coordinates.
        /// </summary>
        /// <param name="coords">The specified coordinates.</param>
        /// <returns></returns>
		public abstract string DefaultLabelProvider(int[] coords);
		#endregion

		#region IHasIdentity Members

		private readonly string m_name;
        /// <summary>
        /// The name for this object. Not typically required to be unique.
        /// </summary>
        /// <value>The object's name.</value>
		public string Name => m_name;

	    private readonly string m_description = null;
		/// <summary>
		/// A description of this Histogram1D_Base.
		/// </summary>
		public string Description => m_description ?? m_name;

	    /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value>The object's Guid</value>
		public Guid Guid { get; }

	    #endregion
	
	}
}
