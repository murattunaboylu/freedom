using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

namespace Freedom.Backtesting
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var chart = new Chart();
            chart.ImageLocation = "~/TempImages/ChartPic_#SEQ(300,3)";
            chart.ChartAreas.Add(new ChartArea("MainArea"));
            var series = new Series() {ChartType = SeriesChartType.Candlestick};
            chart.Series.Add(series);
            //l h o c
            series.Points.AddY(1.2, 3, 1, 1.2);
            series.Points.AddY(4.5, 5, 1.5, 4.6);
            series.Points.AddY(2.5, 7, 3, 6);
            series.Points.AddY(6, 9, 6, 7);
            series.Points.AddY(3, 12, 9, 7);
            ChartPlaceHolder.Controls.Add(chart);
        }
    }
}