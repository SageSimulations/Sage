/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;
using System.Collections.Generic;
// ReSharper disable PossibleMultipleEnumeration // TODO: Must address this.
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Highpoint.Sage.Mathematics
{

    public static class Extensions
    {
        public static double StandardDeviation(this IEnumerable<double> ld)
        {
            double avg = ld.Average();

            double count = 0.0;
            double devSquaredSum = 0.0;
            foreach (double d in ld)
            {
                devSquaredSum += Math.Pow((avg - d), 2);
                count++;
            }

            return Math.Sqrt(devSquaredSum / count);
        }

        public static double StandardDeviation<TSource>(this IEnumerable<TSource> ld, Func<TSource, double> transform)
        {
            double avg = ld.Average(transform);

            double count = 0.0;
            double devSquaredSum = 0.0;
            foreach (TSource ts in ld)
            {
                devSquaredSum += Math.Pow((avg - transform(ts)), 2);
                count++;
            }

            return Math.Sqrt(devSquaredSum / count);
        }

        public static IEnumerable<double> Bound(this IEnumerable<double> ld, double minBound, double maxBound)
        {
            return ld.Where(d => d >= minBound && d <= maxBound);
        }

        internal class SigmaBoundingContext {
            public double MinBound { get; set; }
            public double MaxBound { get; set; }
            public double Mean { get; set; }
            public double StdDev { get; set; }

            public bool IsValueInBounds(double val){
                double deviation = (val-Mean)/StdDev;
                return (deviation > 0 && deviation < MaxBound) ||
                       (deviation < 0 && -deviation < MinBound);
            }
        }

        public static IEnumerable<T> BoundBySigmas<T>(this IEnumerable<T> ld, Func<T, double> transform, double minBound, double maxBound) {
            double mean = ld.Average(transform);
            double stdev = ld.StandardDeviation(transform);
            SigmaBoundingContext context = new SigmaBoundingContext { Mean = mean, StdDev = stdev, MinBound = minBound, MaxBound = maxBound };
            return BoundBySigmasIter(ld, transform, context);
        }

        public static IEnumerable<T> BoundBySigmas<T>(this IEnumerable<T> ld, Func<T, double> transform, double minBound, double maxBound, ref object context) {
            if (context == null) {
                double mean = ld.Average(transform);
                double stdev = ld.StandardDeviation(transform);
                context = new SigmaBoundingContext() { Mean = mean, StdDev = stdev, MinBound = minBound, MaxBound = maxBound };
            }
            return BoundBySigmasIter(ld, transform, (SigmaBoundingContext)context);
        }

        private static IEnumerable<T> BoundBySigmasIter<T>(this IEnumerable<T> ld, Func<T, double> transform, SigmaBoundingContext context)
        {
            return ld.Where(item => context.IsValueInBounds(transform(item)));
        }

        public static IEnumerable<double> BoundBySigmas(this IEnumerable<double> ld, double minBound, double maxBound) {
            object context = null;
            return BoundBySigmas(ld, minBound, maxBound, ref context);
        }

        public static IEnumerable<double> BoundBySigmas(this IEnumerable<double> ld, double minBound, double maxBound, ref object context) {
            if (!ld.Any()) {
                return ld;
            }
            if (context == null) {
                double mean = ld.Average();
                double stdev = ld.StandardDeviation();
                context = new SigmaBoundingContext { Mean = mean, StdDev = stdev, MinBound = minBound, MaxBound = maxBound };
            }
            return BoundBySigmasIter(ld, (SigmaBoundingContext)context);
        }

        private static IEnumerable<double> BoundBySigmasIter(this IEnumerable<double> ld, SigmaBoundingContext context)
        {
            return ld.Where(context.IsValueInBounds);
        }

        public static IEnumerable<TSource> Bound<TSource>(this IEnumerable<TSource> lts, double minBound, double maxBound, Func<TSource, double> transform)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery - as written is much clearer.
            foreach (TSource ts in lts)
            {
                double d = transform(ts);
                if (d > minBound && d < maxBound)
                {
                    yield return ts;
                }
            }
        }

        public static double Mode<TSource>(this IEnumerable<TSource> lhi, Func<TSource, double> transform)
        {
            int ndx = lhi.Count();
            return ndx > 0 ? transform(lhi.ElementAt(ndx / 2)) : double.NaN;
        }

        public static double Skewness(this IEnumerable<double> ld )
        {
            double skewness = double.NaN;
            double avg = ld.Average();
            double stDevCubed = Math.Pow(ld.StandardDeviation(), 3);
            int nSamplesMinus1 = ld.Count(d => !double.IsNaN(d)) - 1;

            if (nSamplesMinus1 > 0)
            {
                double devCubedSum = 0.0;

                // ReSharper disable once LoopCanBeConvertedToQuery (Clearer as-written.)
                foreach (double d in ld)
                {
                    if (!double.IsNaN(d))
                    {
                        devCubedSum += Math.Pow((avg - d), 3);
                    }
                }

                skewness = devCubedSum / (nSamplesMinus1 * stDevCubed);
            }
            return skewness;
        }

        public static double Skewness<TSource>(this IEnumerable<TSource> lhi, Func<TSource, double> transform)
        {
            double skewness = double.NaN;
            double avg = lhi.Average(transform);
            double stDevCubed = Math.Pow(lhi.StandardDeviation(transform), 3);
            int nSamplesMinus1 = lhi.Count(n => !double.IsNaN(transform(n))) - 1;

            if (nSamplesMinus1 > 0)
            {
                double devCubedSum = 0.0;

                // ReSharper disable once LoopCanBeConvertedToQuery (Clearer as written.)
                foreach (double d in lhi.Select(transform))
                {
                    if (!double.IsNaN(d))
                    {
                        devCubedSum += Math.Pow((avg - d), 3);
                    }
                }

                skewness = devCubedSum / (nSamplesMinus1 * stDevCubed);
            }
            return skewness;
        }

        /// <summary>
        /// Given a list of source items, and a function that returns a double value for each, this
        /// method ascertains the double value that represents the given percentile in that population.
        /// For example, given a list of students, and a function that returns their grades, this method
        /// will return the grade that represents the 50th percentile across that population of students.
        /// </summary>
        /// <typeparam name="T">The type of items in the list of source items.</typeparam>
        /// <param name="srcItems">The source items for which we want to know the percentile.</param>
        /// <param name="percentile">The percentile on the interval [0.0, 100.0] at which we seek the value from the source items.</param>
        /// <param name="valueGetter">The function that ascertains the value of each source item.</param>
        /// <param name="interpolate">if set to <c>true</c>, we interpolate between located items' values.</param>
        /// <returns></returns>
        public static double GetValueAtPercentile<T>(this List<T> srcItems, double percentile, Func<T, double> valueGetter, bool interpolate) {
            System.Diagnostics.Debug.Assert(srcItems.Count > 0, "Percentile was requested from a population of zero items. Percentile source populations must have at least one member.");
            System.Diagnostics.Debug.Assert(percentile >= 0.0 && percentile <= 100.0, string.Format("Percentile was requested as {0} - it must be a double on the interval [0.0 ... 100.0]", percentile));
            percentile = percentile / 100.0;
            if (srcItems.Count == 1) {
                return valueGetter(srcItems[0]);
            } else {
                srcItems.Sort((t1, t2) => Comparer<double>.Default.Compare(valueGetter(t1), valueGetter(t2)));

                double index = (srcItems.Count - 1) * percentile;
                double lowNdx = Math.Floor(index);
                double hiNdx = Math.Ceiling(index);
                if (lowNdx == hiNdx) {
                    if (lowNdx == 0) {
                        hiNdx++;
                    } else {
                        lowNdx--;
                    }
                }
                double lowVal = valueGetter(srcItems[(int)lowNdx]);
                double hiVal = valueGetter(srcItems[(int)hiNdx]);
                if (interpolate) {
                    SmallDoubleInterpolable sdi = new SmallDoubleInterpolable(new[] { lowNdx, hiNdx }, new[] { lowVal, hiVal });
                    return sdi.GetYValue(index);
                } else {
                    return (index - lowNdx) < 0.5 ? lowVal : hiVal;
                }
            }
        }

        /// <summary>
        /// Given a list of source items, a target item of the same type as source items, and a 
        /// lambda expression that returns a double value (a score) from those items, this function
        /// will return a double that is the percentile into which that target value falls for the
        /// population represented by the list of source items.
        /// <para>Note1: Percentile is the percent of observed values in the srcItems that fall at
        /// <b>or below</b> the value of the targetItem.</para>
        /// <para>Note2: If this operation will be performed repeatedly on the same list, use the other form instead.</para>
        /// </summary>
        /// <typeparam name="T">The type of items in the list of source items.</typeparam>
        /// <param name="srcItems">The source items defining the percentile population.</param>
        /// <param name="targetItem">The item for whose score we want to know its percentile in the srcItems population.</param>
        /// <param name="valueGetter">The function that ascertains the value of each source item.</param>
        /// <returns>The percentile at which the target item falls.</returns>
        public static double GetPercentileForItem<T>(this List<T> srcItems, T targetItem, Func<T, double> valueGetter) {
            List<T> lclSrcItems = new List<T>(srcItems);
            System.Diagnostics.Debug.Assert(lclSrcItems.Count > 0, "Percentile was requested from a population of zero items. Percentile source populations must have at least one member.");
            lclSrcItems.Sort((t1, t2) => Comparer<double>.Default.Compare(valueGetter(t1), valueGetter(t2)));
            if (lclSrcItems.Contains(targetItem)) {
                // Faster method.
                int ndx = lclSrcItems.FindIndex(0, tgt => tgt.Equals(targetItem));
                while (ndx < lclSrcItems.Count && valueGetter(lclSrcItems[ndx]) == valueGetter(targetItem)) ndx++;
                return ndx / ((double)lclSrcItems.Count);
            } else {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the percentile for item. Note: with [0,1,2,3,3,3], 3 will be in the 50th percentile.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="srcItems">The _SRC items.</param>
        /// <param name="targetItem">The target item.</param>
        /// <param name="valueGetter">The value getter.</param>
        /// <param name="pd">The object that will hold state for repeated calls to this method on the same list.</param>
        /// <returns></returns>
        public static double GetPercentileForItem<T>(this List<T> srcItems, T targetItem, Func<T, double> valueGetter, ref object pd) {
            if (pd == null) {
                List<T> lclSrcItems = new List<T>(srcItems);
                lclSrcItems.Sort((t1, t2) => Comparer<double>.Default.Compare(valueGetter(t1), valueGetter(t2)));
                int nItems = lclSrcItems.Count;

                // Added the following to collapse repeated xvalues (with which it is impossible
                // to calculate a slope, dy/dx because dx==0). 
                List<double> data = new List<double>();
                List<double> percentiles = new List<double>();
                for (int i = 0; i < nItems; i++) {
                    double xval = valueGetter(lclSrcItems[i]);
                    if (data.Count == 0 || xval != data.Last()) {
                        data.Add(xval);
                        percentiles.Add(((double)i + 1) / nItems);
                    }
                }
                // The preceding was added.

                //double[] data = new double[nItems];
                //double[] percentiles = new double[nItems];
                //for (int i = 0; i < nItems; i++) {
                //    data[i] = valueGetter(srcItems[i]);
                //    percentiles[i] = ((double)i+1) / ((double)nItems);
                //}

                LinearDoubleInterpolator ldi = new LinearDoubleInterpolator();
                //ldi.SetData(data, percentiles);
                //_pd = new PercentileData() { min = data[0], ldi = ldi };
                ldi.SetData(data.ToArray(), percentiles.ToArray());
                pd = new PercentileData() { Min = data[0], Ldi = ldi };
            }
            PercentileData lclPd = (PercentileData)pd;

            return Math.Max(0, Math.Min(1, lclPd.Ldi.GetYValue(valueGetter(targetItem))));
        }
        internal class PercentileData { public double Min { get; set; } public LinearDoubleInterpolator Ldi { get; set; } };
    }
}
