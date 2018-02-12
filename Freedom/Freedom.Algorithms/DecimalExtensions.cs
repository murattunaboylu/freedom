using System;
using System.Collections.Generic;
using System.Linq;

namespace Freedom.Algorithms
{
    public static class DecimalExtensions
    {
        public static double StandardDeviation(this List<decimal> values)
        {
            var doubles = values.Select(x => (double)x).ToList();

            return StandardDeviation(doubles);
        }

        public static double StandardDeviation(this List<double> values)
        {
            double mean = values.Sum() / values.Count;
            var cumulativeSumOfSquaredDifferences = values.Select(x => Math.Pow((double)(x - mean), 2)).Sum();
            var standardDeviation = Math.Sqrt(cumulativeSumOfSquaredDifferences / (values.Count - 1));

            return standardDeviation;
        }
    }
}
