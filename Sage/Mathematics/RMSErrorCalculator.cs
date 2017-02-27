/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Highpoint.Sage.Mathematics
{
    public static class RMSErrorCalculator
    {
        public static double Calculate(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Cannot calculate RMS Error between two arrays of unequal length.");
            return Math.Sqrt(a.Select((t, i) => Math.Pow(t - b[i], 2.0)).Sum())/a.Length;
        }

        public static double Calculate(List<double> a, List<double> b)
        {
            if (a.Count !=b.Count)
            throw new ArgumentException("Cannot calculate RMS Error between two arrays of unequal length.");
            return Math.Sqrt(a.Select((t, i) => Math.Pow(t - b[i], 2.0)).Sum())/ a.Count;
        }
    }
}