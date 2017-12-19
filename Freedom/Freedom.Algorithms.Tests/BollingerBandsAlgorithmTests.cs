using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Freedom.Algorithms;
using System.Collections.Generic;

namespace Freedom.Algorithms.Tests
{
    [TestClass]
    public class BollingerBandsAlgorithmTests
    {
        [TestMethod]
        public void StandardDeviationTest()
        {
            var metabolicRateOfFemaleFulmars = new List<double> { 727.7, 1086.5, 1091, 1361.3, 1490.5, 1956.1 };

            var bb = new BollingerBandsAlgorithm();
            var stdDev = bb.StandardDeviation(metabolicRateOfFemaleFulmars);

            Assert.AreEqual(420.96, Math.Round(stdDev, 2));
        }

        [TestMethod]
        public void MovingAverageTest()
        {
            var values = new List<double> { 9, 100, 2, 35, 4, 96 };
            var bb = new BollingerBandsAlgorithm();
            var actual = bb.MovingAverage(values, 2);

            Assert.AreEqual(50, actual);
        }
    }
}
