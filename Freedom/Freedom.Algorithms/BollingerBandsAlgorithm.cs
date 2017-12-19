using System;
using System.Collections.Generic;
using System.Linq;

namespace Freedom.Algorithms
{
    public class BollingerBandsAlgorithm
    {
        public double StandardDeviation(List<decimal> values)
        {
            var doubles = values.Select(x => (double)x).ToList();

            return StandardDeviation(doubles);
        }

        public double StandardDeviation(List<double> values)
        {
            double mean = values.Sum() / values.Count;
            var cumulativeSumOfSquaredDifferences = values.Select(x => Math.Pow((double)(x - mean), 2)).Sum();
            var standardDeviation = Math.Sqrt(cumulativeSumOfSquaredDifferences / (values.Count - 1));

            return standardDeviation;
        }

        public double MovingAverage(List<double> values, int n)
        {
            return values.Skip(values.Count - n).Take(n).Sum() / n;
        }

        public Result Calculate(List<decimal> values, int period, int sigmaWeight)
        {
            if (values.Count < period)
                return new Result() { UpperBand = (double)values.First(), LowerBand = (double)values.First(), PercentB = 0, Bandwidth = 0 };

            var doubles = values.Select(x => (double)x).ToList();

            var stdDev = StandardDeviation(doubles);
            var movingAverage = MovingAverage(doubles, period);

            //MA + Ksig
            var upperBand = movingAverage + sigmaWeight * stdDev;
            var lowerBand = movingAverage - sigmaWeight * stdDev;
            
            var result = new Result();
            result.UpperBand = upperBand;
            result.LowerBand = lowerBand;

            //%b
            result.PercentB = (doubles.First() - lowerBand) / (upperBand - lowerBand) * 100;

            //Normalized bandwidth
            result.Bandwidth = (upperBand - lowerBand) / movingAverage * 100;

            return result;
        }

        public class Result
        {
            public double PercentB { get; set; }
            public double Bandwidth { get; set; }
            public double UpperBand { get; set; }
            public double LowerBand { get; set; }
        }
    }
}
