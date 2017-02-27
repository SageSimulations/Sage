/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;

namespace Highpoint.Sage.Mathematics {
	/// <summary>
	/// Summary description for Histogram1D.
	/// </summary>
	public class Histogram1D_TimeSpan : Histogram1D_Base {
		#region >>> Local Private Variables. <<<
		private long[] m_binsSum;
		private long[] m_binsSumSquares;
		//private long m_lowSum;
		//private long m_lowSumSquares;
		//private long m_highSum;
		//private long m_highSumSquares;
		#endregion
		
		public Histogram1D_TimeSpan(TimeSpan[] rawData, TimeSpan lowBound, TimeSpan highBound, int nBins, string name, Guid guid)
			:base(rawData, lowBound, highBound, nBins, name, guid){}
		public Histogram1D_TimeSpan(TimeSpan[] rawData, TimeSpan lowBound, TimeSpan highBound, int nBins, string name):this(rawData,lowBound,highBound,nBins,name,Guid.Empty){}
		public Histogram1D_TimeSpan(TimeSpan[] rawData, TimeSpan lowBound, TimeSpan highBound, int nBins):this(rawData,lowBound,highBound,nBins,"",Guid.Empty){}
		public Histogram1D_TimeSpan(TimeSpan lowBound, TimeSpan highBound, int nBins, string name, Guid guid):this(null,lowBound,highBound,nBins,name,guid){}
		public Histogram1D_TimeSpan(TimeSpan lowBound, TimeSpan highBound, int nBins, string name):this(null,lowBound,highBound,nBins,name,Guid.Empty){}
		public Histogram1D_TimeSpan(TimeSpan lowBound, TimeSpan highBound, int nBins):this(null,lowBound,highBound,nBins,"",Guid.Empty){}

		
		#region IHistogram Members
		public override void Clear() {
			base.Clear();
			m_binsSum = null;
			m_binsSumSquares = null;
			//m_lowSum = 0;
			//m_lowSumSquares = 0;
			//m_highSum = 0;
			//m_highSumSquares = 0;
		}

		public override object SumEntries(HistogramBinCategory hbc) {
			long sumEntries = 0;
			bool sumLow = false;
			bool sumHigh = false;
			bool inRange = hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All;
			if ( hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All )sumLow = true;
			if ( hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All ) sumHigh = true;
		
			TimeSpan[] data = (TimeSpan[])m_rawData;
			TimeSpan lowBound = (TimeSpan)m_lowBound;
			TimeSpan highBound = (TimeSpan)m_highBound;

		    foreach (TimeSpan val in data)
		    {
		        if ( val < lowBound ) {
		            if ( sumLow ) sumEntries += val.Ticks;
		        } else if ( val >= highBound ) {
		            if ( sumHigh ) sumEntries += val.Ticks;
		        } else {
		            if ( inRange ) sumEntries += val.Ticks;
		        }
		    }
		    return TimeSpan.FromTicks(sumEntries);
		}

		public override object SumEntries(int[] lowBounds, int[] highBounds) {
			TimeSpan lowBound = (TimeSpan)LowBound;
			TimeSpan highBound = (TimeSpan)HighBound;
			long binIncrement = (highBound-lowBound).Ticks/NumBins;

			TimeSpan lowThreshold = TimeSpan.FromTicks(lowBound.Ticks + (lowBounds[0]*binIncrement));
			TimeSpan highThreshold = TimeSpan.FromTicks(lowBound.Ticks + (highBounds[0]*binIncrement));

			TimeSpan[] data = (TimeSpan[])m_rawData;
			long sumEntries = data.Select(t => t.Ticks).Where(val => val >= lowThreshold.Ticks && val < highThreshold.Ticks).Sum();
		    return TimeSpan.FromTicks(sumEntries);
		}

		public override object Error(int[] coordinates) {
			if ( coordinates.Rank != 1 ) throw new ArgumentException("coordinate data provided to a Histogram1D must be of rank 1.");
			int whichBin = coordinates[0];
			long binSum = m_binsSum[whichBin];
			long binSumSquared = m_binsSumSquares[whichBin];
			return ((binSum*binSum)/binSumSquared);
		}

		public override void Recalculate(){
		    m_bins = new int[NumBins];
			m_binsSum = new long[NumBins];
			m_binsSumSquares = new long[NumBins];
			TimeSpan[] rawData = (TimeSpan[])m_rawData;
			long lowBound = ((TimeSpan)LowBound).Ticks;
			long highBound = ((TimeSpan)HighBound).Ticks;
			if ( highBound.Equals(lowBound) ) throw new ApplicationException("Histogram has low bound equal to high bound. This is an error.");
			double binIncrement = (highBound-lowBound)/((double)NumBins);
			foreach (TimeSpan t in rawData)
			{
			    long dataPoint = t.Ticks;
			    if ( dataPoint < lowBound ) {
			        LowBin++;
			        // TODO: Add Mean and Standard Deviation of low & high bins.
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
			long highBound = ((TimeSpan)HighBound).Ticks;
			long lowBound = ((TimeSpan)LowBound).Ticks;
			long binIncrement = (highBound-lowBound)/NumBins;
			long lowBoundThisBin = lowBound + (whichBin*binIncrement);
			long highBoundThisBin = lowBound + ((whichBin+1)*binIncrement);
	
			//string fmtSpecifier = "f2";
			//string fmtString = "[{0:"+fmtSpecifier+"},{1:"+fmtSpecifier+"})";
			//return string.Format(fmtString,lowBoundThisBin,highBoundThisBin);
			return "[" + FormatTimeSpan(TimeSpan.FromTicks(lowBoundThisBin)) 
				+ ", " 
				+ FormatTimeSpan(TimeSpan.FromTicks(highBoundThisBin)) + ")"; 
		}

		private static string FormatTimeSpan(TimeSpan ts){
			if ( ts.TotalDays < 1.0 ) {
				return string.Format("{0:d2}:{1:d2}:{2:d2}",ts.Hours,ts.Minutes,ts.Seconds);
			} else {
				return string.Format("{0:d2}:{1:d2}:{2:d2}:{3:d2}", ts.Days,ts.Hours,ts.Minutes,ts.Seconds);
			}
		}
	}
}
