/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Mathematics {
	/// <summary>
    /// Histogram1D_DateTime is not yet implemented. Histogram1D_DateTime creates a one dimensional histogram from an array of DateTime data.
	/// </summary>
	public class Histogram1D_DateTime : Histogram1D_Base {
		#region >>> Local Private Variables. <<<
//		private Array m_rawData = null;
//		private int[] m_bins;
//		private int m_numBins;
//		private int m_lowBin;
//		private int m_highBin;
//		private double[] m_binsSumSquares;
//		private double[] m_binsSum;
//		private double m_lowSum;
//		private double m_lowSumSquares;
//		private double m_highSum;
//		private double m_highSumSquares;
//		private double m_lowBound;
//		private double m_highBound;
//		private LabelProvider m_labelProvider;
		#endregion
		
		public Histogram1D_DateTime(DateTime[] rawData, double lowBound, double highBound, int nBins, string name, Guid guid)
			:base(rawData, lowBound, highBound, nBins, name, guid){
			throw new NotSupportedException("Histogram1D_DateTime is not yet implemented.");
		}
        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        /// <param name="name">The name of the Histogram.</param>
		public Histogram1D_DateTime(DateTime[] rawData, double lowBound, double highBound, int nBins, string name):this(rawData,lowBound,highBound,nBins,name,Guid.Empty){}
        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
		public Histogram1D_DateTime(DateTime[] rawData, double lowBound, double highBound, int nBins):this(rawData,lowBound,highBound,nBins,"",Guid.Empty){}
        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        /// <param name="name">The name of the Histogram.</param>
        /// <param name="guid">The GUID of the Histogram.</param>
		public Histogram1D_DateTime(double lowBound, double highBound, int nBins, string name, Guid guid):this(null,lowBound,highBound,nBins,name,guid){}
        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        /// <param name="name">The name of the Histogram.</param>
		public Histogram1D_DateTime(double lowBound, double highBound, int nBins, string name):this(null,lowBound,highBound,nBins,name,Guid.Empty){}
        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
		public Histogram1D_DateTime(double lowBound, double highBound, int nBins):this(null,lowBound,highBound,nBins,"",Guid.Empty){}

		
		#region IHistogram Members

        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>The sum of values.</returns>
		public override object SumEntries(HistogramBinCategory hbc) {
			// TODO:  Add Histogram1D.SumEntries implementation
			return 0;
		}

        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The high bounds.</param>
        /// <returns>The sum of values.</returns>
		public override object SumEntries(int[] lowBounds, int[] highbounds) {
			// TODO:  Add Histogram1D.Highpoint.Sage.Mathematics.IHistogram.SumEntries implementation
			return 0;
		}

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
		public override object Error(int[] coordinates) {
//			if ( coordinates.Rank != 1 ) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");
//			int whichBin = coordinates[0];
//			double binSum = m_binsSum[whichBin];
//			double binSumSquared = m_binsSumSquares[whichBin];
//			return ((binSum*binSum)/binSumSquared);
			return null;
		}

        /// <summary>
        /// Recalculates this Histogram, resulting in new bins and counts.
        /// </summary>
		public override void Recalculate(){
//			//_Clear(false);
//			int length = m_rawData.Length;
//			m_bins = new int[m_numBins];
//			m_binsSum = new double[m_numBins];
//			m_binsSumSquares = new double[m_numBins];
//			double[] rawData = (double[])m_rawData;
//			double binIncrement = (m_highBound-m_lowBound)/((double)m_numBins);
//			for ( int i = 0 ; i < rawData.Length ; i++ ) {
//				double dataPoint = rawData[i];
//				if ( dataPoint < m_lowBound ) {
//					m_low++;
//					m_lowSum += dataPoint;
//					m_lowSumSquares += (dataPoint*dataPoint);
//				} else if ( dataPoint >= m_highBound ) {
//					m_high++;
//					m_highSum += dataPoint;
//					m_highSumSquares += (dataPoint*dataPoint);
//				} else {
//					int whichBin = (int)((dataPoint-m_lowBound)/binIncrement);
//					m_bins[whichBin]++;
//					m_binsSum[whichBin] += dataPoint;
//					m_binsSumSquares[whichBin] += (dataPoint*dataPoint);
//				}
//			}
		}
        /// <summary>
        /// Recalculates the Histogram with new high &amp; low bounds, resulting in new bins and counts.
        /// </summary>
        /// <param name="lowBounds">The low bounds of the Histogram.</param>
        /// <param name="highBounds">The high bounds of the Histogram.</param>
        /// <param name="nBins">The number of bins.</param>
		public override void Recalculate(Array lowBounds, Array highBounds, int nBins){
//			if ( lowBounds.Rank != 1 || highBounds.Rank != 1 ) {
//				throw new ArgumentException("Boundary data set into a Histogram1D must be of rank 1.");
//			}
//			m_lowBound = (double)lowBounds.GetValue(new int[]{0});
//			m_highBound = (double)highBounds.GetValue(new int[]{0});
//			m_numBins = nBins;
//			Recalculate();
		}
		#endregion

        /// <summary>
        /// Provides the default label provider for the specified coordinates.
        /// </summary>
        /// <param name="coords">The specified coordinates.</param>
        /// <returns></returns>
		public override string DefaultLabelProvider(int[] coords){
			if ( coords.Rank != 1 ) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");

			int whichBin = coords[0];
//			double binIncrement = (m_highBound-m_lowBound)/((double)m_numBins);
//			double lowBoundThisBin = m_lowBound + (whichBin*binIncrement);
//			double highBoundThisBin = m_lowBound + ((whichBin+1)*binIncrement);
//	
//			string fmtSpecifier = "f2";
//			string fmtString = "[{0:"+fmtSpecifier+"},{1:"+fmtSpecifier+"})";
//			return string.Format(fmtString,lowBoundThisBin,highBoundThisBin);
			return "Date Bin " + whichBin;
		}
	}
}
