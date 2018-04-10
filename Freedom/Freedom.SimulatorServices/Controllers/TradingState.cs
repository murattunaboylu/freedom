namespace Freedom.SimulatorServices.Controllers
{
    public enum TradingState
    {
        Initial,
        MonitoringUpTrend,
        WaitForUpTrendPriceCorrection,
        WaitingToBuy,
        WaitingToSell,
        MonitoringDownTrend,

    }
}