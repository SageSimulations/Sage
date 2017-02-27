/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;

namespace Highpoint.Sage.Mathematics {
	/// <summary>
	/// Summary description for Histogram1D.
	/// </summary>
	public class Histogram1D_Double  : Histogram1D_Base {
		#region >>> Local Private Variables. <<<
		private double[] m_binsSum;
		private double[] m_binsSumSquares;
		//private double m_lowSum;
		//private double m_lowSumSquares;
		//private double m_highSum;
		//private double m_highSumSquares;
		#endregion
		
		public Histogram1D_Double(double[] rawData, double lowBound, double highBound, int nBins, string name, Guid guid)
			:base(rawData, lowBound, highBound, nBins, name, guid){}
		public Histogram1D_Double(double[] rawData, double lowBound, double highBound, int nBins, string name):this(rawData,lowBound,highBound,nBins,name,Guid.Empty){}
		public Histogram1D_Double(double[] rawData, double lowBound, double highBound, int nBins):this(rawData,lowBound,highBound,nBins,"",Guid.Empty){}
		public Histogram1D_Double(double lowBound, double highBound, int nBins, string name, Guid guid):this(null,lowBound,highBound,nBins,name,guid){}
		public Histogram1D_Double(double lowBound, double highBound, int nBins, string name):this(null,lowBound,highBound,nBins,name,Guid.Empty){}
		public Histogram1D_Double(double lowBound, double highBound, int nBins):this(null,lowBound,highBound,nBins,"",Guid.Empty){}

		
		#region IHistogram Members
		public override void Clear() {
			base.Clear();
			m_binsSum = null;
			m_binsSumSquares = null;
			//m_lowSum = 0.0;
			//m_lowSumSquares = 0.0;
			//m_highSum = 0.0;
			//m_highSumSquares = 0.0;
		}

		public override object SumEntries(HistogramBinCategory hbc) {
			double sumEntries = 0.0;
			bool sumLow = false;
			bool sumHigh = false;
			if ( hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All )sumLow = true;
			if ( hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All ) sumHigh = true;
			bool inRange = hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All;
		
			double[] data = (double[])m_rawData;
			double lowBound = (double)m_lowBound;
			double highBound = (double)m_highBound;

		    foreach (double val in data)
		    {
		        if ( val < lowBound ) {
		            if ( sumLow ) sumEntries += val;
		        } else if ( val >= highBound ) {
		            if ( sumHigh ) sumEntries += val;
		        } else {
		            if ( inRange ) sumEntries += val;
		        }
		    }
		    return sumEntries;
		}

		public override object SumEntries(int[] lowBounds, int[] highBounds) {
			double lowBound = (double)LowBound;
			double highBound = (double)HighBound;
			double binIncrement = (highBound-lowBound)/NumBins;

			double lowThreshold = lowBound + (lowBounds[0]*binIncrement);
			double highThreshold = lowBound + (highBounds[0]*binIncrement);

			double[] data = (double[])m_rawData;
		    return data.Where(val => val >= lowThreshold && val < highThreshold).Sum();
		}

		/// <summary>
		/// This returns a double that indicates how far a specified bin's count
		/// deviates from the 'expected' count - note that it is only relevant if
		/// the histogram was expected to have been uniform.
		/// </summary>
		/// <param name="coordinates">An integer array that specifies the coordinates of
		/// the bin of interest. Histogram analysis of a Histogram1D_&lt;anything&gt; must be
		/// on a 1 dimensional array, therefore, this array must be of rank 1.
        /// </param>
		/// <returns> a double that indicates how far a specified bin's count
		/// deviates from the 'expected' count.</returns>
		public override object Error(int[] coordinates) {
			if ( coordinates.Rank != 1 ) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");
			int whichBin = coordinates[0];
			double binSum = m_binsSum[whichBin];
			double binSumSquared = m_binsSumSquares[whichBin];
			return ((binSum*binSum)/binSumSquared);
		}

		public override void Recalculate(){
			m_bins = new int[NumBins];
			m_binsSum = new double[NumBins];
			m_binsSumSquares = new double[NumBins];
			double[] rawData = (double[])m_rawData;
			double lowBound = (double)LowBound;
			double highBound = (double)HighBound;
			double binIncrement = (highBound-lowBound)/NumBins;
			foreach (double dataPoint in rawData)
			{
			    if ( double.IsInfinity(dataPoint) || double.IsNaN(dataPoint) ) {
			        throw new ApplicationException("Datapoint was " + dataPoint + " in histogram analysis.");
			    }
			    if ( dataPoint < lowBound ) {
			        LowBin++;
			        //m_lowSum += dataPoint;
			        //m_lowSumSquares += (dataPoint*dataPoint);
			    } else if ( dataPoint >= highBound ) {
			        HighBin++;
			        //m_highSum += dataPoint;
			        //m_highSumSquares += (dataPoint*dataPoint);
			    } else {
			        int whichBin = (int)((dataPoint-lowBound)/binIncrement);
			        m_bins[whichBin]++;
			        m_binsSum[whichBin] += dataPoint;
			        m_binsSumSquares[whichBin] += (dataPoint*dataPoint);
			    }
			}
		}
		public override void Recalculate(Array lowBounds, Array highBounds, int nBins){
			if ( lowBounds.Rank != 1 || highBounds.Rank != 1 ) {
				throw new ArgumentException("Boundary data set into a Histogram1D must be of rank 1.");
			}
			m_lowBound =  lowBounds.GetValue(new[]{0});
			m_highBound = highBounds.GetValue(new[]{0});
			NumBins = nBins;
			Recalculate();
		}
		#endregion

		public override string DefaultLabelProvider(int[] coords){
			if ( coords.Rank != 1 ) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");

			int whichBin = coords[0];
			double highBound = (double)HighBound;
			double lowBound = (double)LowBound;
			double binIncrement = (highBound-lowBound)/NumBins;
			double lowBoundThisBin = lowBound + (whichBin*binIncrement);
			double highBoundThisBin = lowBound + ((whichBin+1)*binIncrement);
	
			string fmtSpecifier = "f2";
			string fmtString = "[{0:"+fmtSpecifier+"}->{1:"+fmtSpecifier+"})";
			return string.Format(fmtString,lowBoundThisBin,highBoundThisBin);
		}

		public string DefaultLabelProviderWithError(int[] coords){
			string fmtSpecifier = "f2";
			string fmtString = " - Err({0:"+fmtSpecifier+"})";
			return DefaultLabelProvider(coords) + string.Format(fmtString,Error(coords));
		}


	}
}
