/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Mathematics {


    /// <summary>
    /// This class returns the &quot;correct&quot; representation of a number from a set of fractions,
    /// identified by the first N digits in the mantissa. So, if this class is instantiated with fractions
    /// up to ninths (halves to ninths), and five digits, 5.333382 will return 5.333382, but 5.3333382 will
    /// return 5.333333333333333. 5.999996 will return 6.0, and 7.000001 will return 7.0. This is useful for
    /// performing corrections when values are arrived at through computation where it is possible that the
    /// value could be a low-order rational number such as 5 1/3, or 6, but the computation results in
    /// 5.3333391 or 6.000000215, or 5.99999938.
    /// </summary>
    public class Rationalizer {

#region Private Fields
        private Dictionary<double, double> m_ratios = new Dictionary<double, double>();
        private int m_numPlaces;
#endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="Rationalizer"/> class to a set of fractions and a
        /// mantissa sensitivity.
        /// </summary>
        /// <param name="denominatorRange">The range of fractions that will be examined for.</param>
        /// <param name="numPlaces">The number of places that will be examined in the mantissa.</param>
        public Rationalizer(int denominatorRange, int numPlaces) {
            m_numPlaces = numPlaces;
            for (double den = 1 ; den < 10 ; den++) {
                for (double num = 1 ; num < den ; num++) {
                    double val = num / den;
                    double key = Math.Truncate(val * Math.Pow(10, m_numPlaces)) / Math.Pow(10, m_numPlaces);
                    if (m_ratios.ContainsKey(key)) {
                        //Console.WriteLine("{1}/{2} = {0}, but we already have this.", val, num, den);
                    } else {
                        //Console.WriteLine("{1}/{2} = {0}, we will key on {0:F6}", val, num, den, key);
                        m_ratios.Add(key, val);
                    }
                }
            }
            m_ratios.Add(Math.Round(Math.Truncate(Math.Pow(10, m_numPlaces) - 1) / Math.Pow(10, m_numPlaces), m_numPlaces), 1.0);
            m_ratios.Add(0.0, 0.0);
        }

        /// <summary>
        /// Returns the rationalized value corresponding to the number that was supplied.
        /// </summary>
        /// <param name="valArg">The number to be examined.</param>
        /// <returns>The rationalized value</returns>
        public double Rationalize(double valArg) {
            double sgn = Math.Sign(valArg);
            double val = Math.Abs(valArg);
            double floor = Math.Floor(val);
            double fractional = val - floor;
            double key = Math.Truncate(fractional * Math.Pow(10, m_numPlaces)) / Math.Pow(10, m_numPlaces);
            if (m_ratios.ContainsKey(key)) {
                fractional = m_ratios[key];
                valArg = sgn * ( floor + fractional );
            }
            return valArg;
        }
    }
}