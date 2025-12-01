using Microsoft.Maui.Controls;
using Microcharts;
using SkiaSharp;

namespace NAVIGEST.macOS.Pages
{
    public partial class TestChartPage : ContentView
    {
        public TestChartPage()
        {
            InitializeComponent();
            InitializeChart();
        }

        private void InitializeChart()
        {
            var entries = new[]
            {
                new ChartEntry(200)
                {
                    Label = "Jan",
                    ValueLabel = "200",
                    Color = SKColor.Parse("#2c3e50")
                },
                new ChartEntry(400)
                {
                    Label = "Feb",
                    ValueLabel = "400",
                    Color = SKColor.Parse("#77d065")
                },
                new ChartEntry(100)
                {
                    Label = "Mar",
                    ValueLabel = "100",
                    Color = SKColor.Parse("#b455b6")
                },
                new ChartEntry(500)
                {
                    Label = "Apr",
                    ValueLabel = "500",
                    Color = SKColor.Parse("#3498db")
                }
            };

            TestChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 14,
                LineMode = LineMode.Straight,
                PointMode = PointMode.Circle,
                PointSize = 10,
            };
        }
    }
}