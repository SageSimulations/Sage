/* This source code licensed under the GNU Affero General Public License */

using System;
using Highpoint.Sage.Persistence;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics {

    /// <summary>
    /// Implemented by an object that provides an interpolatable Y value for some set of X values, where the specific requested x may not be known to the object.
    /// </summary>
	public interface IInterpolable {

        /// <summary>
        /// Gets the Y value that corresponds to the specified x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <returns></returns>
        double GetYValue(double xValue);
    }

    /// <summary>
    /// Implemented by an object that provides an interpolatable Y value for some set of X values, where the specific requested x may not be known to the object - in addition, at run time, additional known (x,y) values can be provided.
    /// </summary>
    public interface IWriteableInterpolable : IInterpolable {
        /// <summary>
        /// Sets the y value for the specified known x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <param name="yValue">The y value.</param>
        void SetYValue(double xValue, double yValue);
    }

    /// <summary>
    /// Implemented by an object that performs interpolations on two arrays of doubles (an x and a y array).
    /// </summary>
	public interface IDoubleInterpolator {
        /// <summary>
        /// Sets the data used by this interpolator.
        /// </summary>
        /// <param name="xvals">The xvals.</param>
        /// <param name="yvals">The yvals.</param>
		void SetData(double[] xvals, double [] yvals);
        /// <summary>
        /// Gets a value indicating whether this instance has data.
        /// </summary>
        /// <value><c>true</c> if this instance has data; otherwise, <c>false</c>.</value>
		bool HasData { get; }
        /// <summary>
        /// Gets the Y value for the specified x value.
        /// </summary>
        /// <param name="xValue">The X value.</param>
        /// <returns></returns>
		double GetYValue(double xValue);
	}

    /// <summary>
    /// Implemented by an object that performs linear interpolations on two arrays of doubles (an x and a y array).
    /// </summary>
	public class LinearDoubleInterpolator : IDoubleInterpolator {
		private double[] m_xVals, m_yVals;
		private bool m_hasData;

		#region IDoubleInterpolator Members

        /// <summary>
        /// Sets the data used by this interpolator.
        /// </summary>
        /// <param name="xvals">The xvals.</param>
        /// <param name="yvals">The yvals.</param>
		public void SetData(double[] xvals, double[] yvals) {
			m_xVals = xvals;
			m_yVals = yvals;
			m_hasData = true;
            if (m_xVals.Length != m_yVals.Length) throw new ArgumentException("XValue and YValue arrays are of unequal length.");
            if (m_xVals.Length < 2) throw new ArgumentException(string.Format("Illegal attempt to configure an interpolator on {0} data points.", m_xVals.Length));
            
            for (int i = 0; i < xvals.Length-1; i++) {
                if (m_xVals[i] >= m_xVals[i + 1]) {
                    throw new ArgumentException(string.Format("Illegal attempt to configure an interpolator with non-monotonic x values (index {0}={1} and index {2}={3}).", i, m_xVals[i], i + 1, m_xVals[i + 1]));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has data.
        /// </summary>
        /// <value><c>true</c> if this instance has data; otherwise, <c>false</c>.</value>
        public bool HasData => m_hasData;

        /// <summary>
        /// Gets the Y value for the specified x value.
        /// </summary>
        /// <param name="xValue">The X value.</param>
        /// <returns></returns>
		public double GetYValue(double xValue) {
            int lowerNdx = 0;
			while ( ( lowerNdx+2 < m_xVals.Length ) && ( m_xVals[lowerNdx+1] < xValue ) ) lowerNdx++;

			// Did we walk off the end (i.e. our lower index is the last element in the array?)
            if (double.IsNaN(m_xVals[lowerNdx + 1])) {
                lowerNdx--;
            }

			double upperX = m_xVals[lowerNdx+1];
			double upperY = m_yVals[lowerNdx+1];
			double lowerX = m_xVals[lowerNdx];
			double lowerY = m_yVals[lowerNdx];
			double slope = (upperY-lowerY)/(upperX-lowerX);
			double intcp = lowerY-(slope*lowerX);

			return (slope*xValue)+intcp;
		}

		#endregion

	}
    /// <summary>
    /// Implemented by an object that performs cosine interpolations on two arrays of doubles (an x and a y array).
    /// </summary>
    public class CosineDoubleInterpolator : IDoubleInterpolator
    {
		private double[] m_xVals, m_yVals;
		private bool m_hasData;

		#region IDoubleInterpolator Members

        /// <summary>
        /// Sets the data used by this interpolator.
        /// </summary>
        /// <param name="xvals">The xvals.</param>
        /// <param name="yvals">The yvals.</param>
		public void SetData(double[] xvals, double[] yvals) {
			m_xVals = xvals;
			m_yVals = yvals;
			m_hasData = true;
			if (m_xVals.Length != m_yVals.Length) throw new ArgumentException("XValue and YValue arrays are of unequal length.");
            if (m_xVals.Length < 2) throw new ArgumentException(string.Format("Illegal attempt to configure an interpolator on {0} data points.", m_xVals.Length));

		}

        /// <summary>
        /// Gets a value indicating whether this instance has data.
        /// </summary>
        /// <value><c>true</c> if this instance has data; otherwise, <c>false</c>.</value>
		public bool HasData => m_hasData;

        /// <summary>
        /// Gets the Y value for the specified x value.
        /// </summary>
        /// <param name="xValue">The X value.</param>
        /// <returns></returns>
		public double GetYValue(double xValue) {

			int lowerNdx = 0;
			while ( m_xVals[lowerNdx+1] < xValue ) lowerNdx++;

			// Did we walk off the end (i.e. our lower index is the last element in the array?)
			if ( double.IsNaN(m_xVals[lowerNdx+1]) ) lowerNdx--;

            double upperX = m_xVals[lowerNdx+1];
            double upperY = m_yVals[lowerNdx+1];
            double lowerX = m_xVals[lowerNdx];
            double lowerY = m_yVals[lowerNdx];
			double mu = (xValue-lowerX)/(upperX-lowerX);
			double mu2 = (1-Math.Cos(mu*Math.PI))/2.0;
			return lowerY*(1-mu2)+upperY*mu2;
		}

		#endregion

	}

    /// <summary>
    /// This class provides an interpolable data set that uses a linear interpolation
    /// with slope discontinuities at each data point, if the preceding and following
    /// line segments are differently-sloped.
    /// </summary>
	public class SmallDoubleInterpolable : IWriteableInterpolable, IXmlPersistable {
		private double[] m_xVals, m_yVals;
		private int m_nEntries;
		private readonly IDoubleInterpolator m_interpolator;

        /// <summary>
        /// Constructor for an uninitialized SmallDoubleInterpolable, for persistence operations.
        /// </summary>
		public SmallDoubleInterpolable(){}

        /// <summary>
        /// Creates a new instance of the <see cref="T:SmallDoubleInterpolable"/> class which will contain a specified number of data points.
        /// </summary>
        /// <param name="nPoints">The number of data points.</param>
		public SmallDoubleInterpolable(int nPoints){
			m_xVals = new double[nPoints];
			m_yVals = new double[nPoints];
			foreach ( double[] da in new[] {m_xVals,m_yVals} ) {
				for ( int i = 0 ; i < da.Length ; i++ ) da[i] = double.NaN;
			}
			m_nEntries = 0;
			m_interpolator = new LinearDoubleInterpolator();
			m_interpolator.SetData(m_xVals,m_yVals);
		}

        /// <summary>
        /// Creates a new instance of the <see cref="T:SmallDoubleInterpolable"/> class with a specified number of points and a provided interpolator.
        /// </summary>
        /// <param name="nPoints">The n points.</param>
        /// <param name="idi">The doubleInterpolator that this <see cref="T:SmallDoubleInterpolable"/> will use.</param>
		public SmallDoubleInterpolable(int nPoints, IDoubleInterpolator idi):this(nPoints){
			m_interpolator = idi;
			if ( !m_interpolator.HasData ) {
				m_interpolator.SetData(m_xVals,m_yVals);
			}
		}

		/// <summary>
		/// Creates and initializes a SmallDoubleInterpolable from two arrays of correlated
		/// X and Y values.
		/// </summary>
		/// <param name="xVals"></param>
		/// <param name="yVals"></param>
		public SmallDoubleInterpolable(double[] xVals, double[] yVals):this(xVals.Length){
            if (xVals.Length != yVals.Length) throw new ArgumentException("SmallDoubleInterpolable being initialized with unequal-length arrays.");
            if (xVals.Length < 2) throw new ArgumentException(string.Format("Illegal attempt to configure an interpolator on {0} data points.", xVals.Length));
            for (int i = 0; i < xVals.Length; i++) SetYValue(xVals[i], yVals[i]);
			// Faster, but depends on values occurring in increasing order.
			//			m_xVals = (double[])xVals.Clone();
			//			m_yVals = (double[])yVals.Clone();
			//			m_nEntries = xVals.Length;
		}
        /// <summary>
        /// Creates and initializes a SmallDoubleInterpolablefrom two arrays of correlated
        /// X and Y values.
        /// </summary>
        /// <param name="xVals">The correlated x values.</param>
        /// <param name="yVals">The correlated y values.</param>
        /// <param name="idi">The IDoubleInterpolator to be used to discern Y values between known x values.</param>
		public SmallDoubleInterpolable(double[] xVals, double[] yVals, IDoubleInterpolator idi):this(xVals,yVals){
			m_interpolator = idi;
			m_interpolator.SetData(xVals,yVals);
		}

        /// <summary>
        /// Gets the Y value that corresponds to the specified x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <returns></returns>
		public double GetYValue(double xValue){
			return m_interpolator.GetYValue(xValue);
		}
        /// <summary>
        /// Sets the y value for the specified known x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <param name="yValue">The y value.</param>
		public void SetYValue(double xValue, double yValue){
			if ( double.IsNaN(xValue) || double.IsNaN(yValue) ) {
				throw new ApplicationException("Cannot use double.NaN as an X or a Y value in an interpolable.");
			}
			if ( double.IsInfinity(xValue) || double.IsInfinity(yValue) ) {
				throw new ApplicationException("Cannot use double.Infinity values as an X or a Y value in an interpolable.");
			}

			// 1.) Find where the new number belongs.
			int insertionPoint = 0;
            for (; (insertionPoint < m_xVals.Length && m_xVals[insertionPoint] < xValue); insertionPoint++)
            {
            }

			// 2.) If it's an insert, see if we have room. If not, then make room,
			//     and move all data points above the insertion point, up one slot.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
			if (insertionPoint == m_xVals.Length || m_xVals[insertionPoint] != xValue) {
				if ( m_nEntries == (m_xVals.Length-1) ) {
					double[] xTmp = m_xVals;
					double[] yTmp = m_yVals;
					m_xVals = new double[xTmp.Length*2];
					m_yVals = new double[yTmp.Length*2];
					Array.Copy(xTmp,m_xVals,xTmp.Length);
					Array.Copy(yTmp,m_yVals,yTmp.Length);
					// Set unused values to double.NaN
					for ( int i = yTmp.Length ; i < m_yVals.Length ; i++ ) {
						m_xVals[i] = double.NaN;
						m_yVals[i] = double.NaN;
					}
					m_interpolator.SetData(m_xVals,m_yVals);
				}

				// Move stuff up to make room.
				for ( int i = m_nEntries ; i >= insertionPoint ; i-- ) {
					m_xVals[i+1] = m_xVals[i];
					m_yVals[i+1] = m_yVals[i];
				}
				m_nEntries++;
			}

			// 3.) Finally place the value.
			m_xVals[insertionPoint] = xValue;
			m_yVals[insertionPoint] = yValue;

		}

        #region IXmlPersistable Members

		/// <summary>
		/// Serializes this object to the specified XmlSerializatonContext.
		/// </summary>
		/// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
		public void SerializeTo(XmlSerializationContext xmlsc) {
			xmlsc.StoreObject("NumberOfEntries",m_nEntries);
			//xmlsc.StoreObject("XVals",m_xVals);
			//xmlsc.StoreObject("YVals",m_yVals);
			for ( int i = 0 ; i < m_nEntries ; i++ ) {
				xmlsc.StoreObject("XVals_"+i,m_xVals[i]);
				xmlsc.StoreObject("YVals_"+i,m_yVals[i]);
			}
		}

		/// <summary>
		/// Deserializes this object from the specified XmlSerializatonContext.
		/// </summary>
		/// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
		public void DeserializeFrom(XmlSerializationContext xmlsc) {
			m_nEntries = (int)xmlsc.LoadObject("NumberOfEntries");
			m_xVals = new double[m_nEntries];
			m_yVals = new double[m_nEntries];
			for ( int i = 0 ; i < m_nEntries ; i++ ) {
				m_xVals[i] = (double)xmlsc.LoadObject("XVals_"+i);
				m_yVals[i] = (double)xmlsc.LoadObject("YVals_"+i);
			}
		}

		#endregion
	}
}
