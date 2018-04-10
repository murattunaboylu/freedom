using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Freedom.DataAccessLayer;

namespace Freedom.SimulatorServices.Controllers
{
    public class Stats
    {
        public decimal NetProfit { get; set; }
        public double NetProfitPercentage { get; set; }
        public double Exposure { get; set; }
        public double NetRiskAdjustedReturn { get; set; }
        public double MarketRiskAdjustedReturn { get; set; }
        public decimal Market { get; set; }
        public decimal Target { get; set; }
        public double WinRatio { get; set; }
        public int TradePairCount { get; set; }
        public decimal MaxWin { get; set; }
        public decimal MaxLoss { get; set; }
        public decimal MeanWin { get; set; }
        public decimal MeanLoss { get; set; }
        public double MaxHoldInHours { get; set; }
        public double MeanHoldInHours { get; set; }
        public double MinHoldInHours { get; set; }

        public Stats(List<Order> orders, Account account, List<OHLC> dataPoints)
        {
            if (orders !=null && orders.Any())
            {
                if (orders.First().Type != "Buy")
                    throw new StrategyException($"Strategy first order is {orders.First().Type} but should be Buy");

                var lastBuyPrice = 0m;

                //Clean up so we have only Buy-Sell pairs
                if (orders.Last().Type == "Buy")
                {
                    lastBuyPrice = orders.Last().Price;
                    orders.Remove(orders.Last());
                }


                //Sense-check
                if (orders.Count % 2 != 0)
                    throw new StrategyException($"Strategy have odd trades. Failed pairing. {orders.Count}");

                if (orders.Count(o => o.Type == "Buy") != orders.Count(o => o.Type == "Sell"))
                    throw new StrategyException($"Unbalanced trades. Failed pairing. Buys {orders.Count(o => o.Type == "Buy")} vs Sells {orders.Count(o => o.Type == "Sell")}");

                var tradePairs = new List<TradePair>();

                //Pair buy and sell
                foreach (var order in orders)
                {
                    Debug.WriteLine((order.Type == "Buy" ? "-" : "+") + order.Price);

                    if (order.Type == "Buy")
                    {
                        //TODO: Catch consecutive Buy orders

                        tradePairs.Add(new TradePair() { BuyOrder = order });
                    }
                    else
                    {
                        //Double sell
                        if (tradePairs.Last().SellOrder != null)
                            throw new StrategyException($"Consecutive sell orders last {tradePairs.Last().SellOrder} and current {order.Price}");

                        tradePairs.Last().SellOrder = order;
                    }

                }

                //Calculate cumulated returns (investing the return as principal)
                //If last order is Buy then sell it back from the same price - no profit on the last buy
                NetProfit = (account.Euro == 0 ? (decimal)account.BitCoin * lastBuyPrice : account.Euro) - 10000;

                //Net profit %
                NetProfitPercentage = (double)(NetProfit / 10000) * 100;

                var wins = tradePairs.Where(p => p.SellOrder.Price > p.BuyOrder.Price).ToList();
                var losses = tradePairs.Where(p => p.SellOrder.Price <= p.BuyOrder.Price).ToList();
                WinRatio = wins.Count() / (double)tradePairs.Count() * 100;

                TradePairCount = tradePairs.Count();

                //Checking for anomalies
                if (wins.Any())
                {
                    MaxWin = wins.Max(p => p.SellOrder.Price - p.BuyOrder.Price);
                    MeanWin = wins.Average(p => p.SellOrder.Price - p.BuyOrder.Price);
                }

                if (losses.Any())
                {
                    MaxLoss = losses.Min(p => p.SellOrder.Price - p.BuyOrder.Price);
                    MeanLoss = losses.Average(p => p.SellOrder.Price - p.BuyOrder.Price);
                }

                var sumHoldInHours = 0d;

                if (TradePairCount > 0)
                {
                    //Risk metrics - max hold
                    MaxHoldInHours = tradePairs.Max(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                    MeanHoldInHours = tradePairs.Average(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                    MinHoldInHours = tradePairs.Min(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                    sumHoldInHours = tradePairs.Sum(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                }

                //Exposure %
                var totalSimulationDurationInHours = (dataPoints.First().Start - dataPoints.Last().Start).TotalHours;

                Exposure = sumHoldInHours / totalSimulationDurationInHours * 100;

                NetRiskAdjustedReturn = NetProfitPercentage / Exposure * 100;
            }
        }

        private class TradePair
        {
            public Order BuyOrder { get; set; }
            public Order SellOrder { get; set; }
        }

        private class StrategyException : ApplicationException
        {
            public StrategyException(string message) : base(message)
            {

            }
        }
    }
}