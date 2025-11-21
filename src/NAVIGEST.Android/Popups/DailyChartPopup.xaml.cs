using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using NAVIGEST.Android.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace NAVIGEST.Android.Popups
{
    public partial class DailyChartPopup : Popup
    {
        public DailyChartPopup(MonthlyHoursData month, List<DailyHoursData> days)
        {
            InitializeComponent();
            TitleLabel.Text = $"Detalhe: {month.Mes} {month.Ano}";
            
            var labels = days.Select(d => d.Dia.ToString()).ToArray();

            DailyChart.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = days.Select(d => d.HorasNormais).ToArray(),
                    Name = "Reais",
                    Fill = null,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                    GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 }
                },
                new LineSeries<double>
                {
                    Values = days.Select(d => d.HorasIdeais).ToArray(),
                    Name = "Ideais",
                    Fill = null,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2, PathEffect = new DashEffect(new float[] { 6, 6 }) }
                },
                new LineSeries<double>
                {
                    Values = days.Select(d => d.HorasExtras).ToArray(),
                    Name = "Extra",
                    Fill = null,
                    GeometrySize = 5,
                    Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 }
                }
            };

            DailyChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 0,
                    TextSize = 12
                }
            };
            
            DailyChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => $"{value}h"
                }
            };
        }

        private void OnCloseClicked(object sender, System.EventArgs e)
        {
            Close();
        }
    }
}
