using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.Kernel;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;
using SkiaSharp;

namespace NAVIGEST.Android.Popups;

public partial class ChartDetailPopup : Popup
{
    private readonly Colaborador _colaborador;
    private readonly List<MonthlyHoursData> _annualData;
    private readonly int _year;

    public ObservableCollection<MonthItem> Months { get; set; } = new();
    public MonthItem? SelectedMonth { get; set; }

    public ISeries[] AnnualSeries { get; set; } = Array.Empty<ISeries>();
    public Axis[] AnnualXAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] AnnualYAxes { get; set; } = Array.Empty<Axis>();

    public ObservableCollection<ISeries> MonthlySeries { get; set; } = new();
    public ObservableCollection<Axis> MonthlyXAxes { get; set; } = new();
    public ObservableCollection<Axis> MonthlyYAxes { get; set; } = new();

    public ICommand AnnualChartClickCommand { get; }

    public ChartDetailPopup(Colaborador colab, List<MonthlyHoursData> annualData, int year)
    {
        InitializeComponent();
        _colaborador = colab;
        _annualData = annualData;
        _year = year;

        AnnualChartClickCommand = new RelayCommand<IEnumerable<ChartPoint>>(OnAnnualChartClicked);
        
        SetupAnnualChart();
        SetupMonthsList();
        
        BindingContext = this;
    }

    private void SetupAnnualChart()
    {
        var labels = _annualData.Select(m => m.Mes).ToArray();
        
        AnnualSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = _annualData.Select(m => m.HorasNormais).ToArray(),
                Name = "Reais",
                Fill = null,
                GeometrySize = 10,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 }
            },
            new LineSeries<double>
            {
                Values = _annualData.Select(m => m.HorasIdeais).ToArray(),
                Name = "Ideais",
                Fill = null,
                GeometrySize = 0, 
                Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2, PathEffect = new DashEffect(new float[] { 6, 6 }) }
            },
            new LineSeries<double>
            {
                Values = _annualData.Select(m => m.HorasExtras).ToArray(),
                Name = "Extra",
                Fill = null,
                GeometrySize = 5,
                Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 }
            }
        };

        AnnualXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                TextSize = 12
            }
        };
        
        AnnualYAxes = new Axis[]
        {
            new Axis { Labeler = value => $"{value}h" }
        };
    }

    private void SetupMonthsList()
    {
        var culture = new System.Globalization.CultureInfo("pt-PT");
        for (int i = 1; i <= 12; i++)
        {
            Months.Add(new MonthItem 
            { 
                Number = i, 
                Name = culture.DateTimeFormat.GetAbbreviatedMonthName(i).ToUpper(),
                IsSelected = false 
            });
        }
    }

    private void OnAnnualChartClicked(IEnumerable<ChartPoint>? points)
    {
        try
        {
            var point = points?.FirstOrDefault();
            if (point == null) return;

            int monthIndex = point.Index; // 0-11
            if (monthIndex >= 0 && monthIndex < Months.Count)
            {
                var monthItem = Months[monthIndex];
                SelectMonth(monthItem);
            }
        }
        catch (Exception ex)
        {
            NAVIGEST.Android.GlobalErro.TratarErro(ex);
        }
    }

    private void OnMonthSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is MonthItem item)
        {
            SelectMonth(item);
        }
    }

    private async void SelectMonth(MonthItem item)
    {
        if (item == null) return;

        foreach (var m in Months) m.IsSelected = false;
        item.IsSelected = true;
        SelectedMonth = item;
        
        // Refresh UI for selection (hacky if not using proper MVVM notification for items)
        // Ideally MonthItem should implement INotifyPropertyChanged
        
        TitleLabel.Text = $"Detalhe: {item.Name} {_year}";
        SubtitleLabel.Text = "Carregando dados diários...";
        
        AnnualChartContainer.IsVisible = false;
        MonthlyChartContainer.IsVisible = true;

        await LoadDailyDataAsync(item.Number);
        
        SubtitleLabel.Text = "Visualização diária";
    }

    private async Task LoadDailyDataAsync(int month)
    {
        try
        {
            if (_colaborador == null)
            {
                SubtitleLabel.Text = "Erro: Colaborador não identificado.";
                return;
            }

            var dailyData = await DatabaseService.GetDailyHoursStatsAsync(_colaborador.ID, month, _year);
            
            var labels = dailyData.Select(d => d.Dia.ToString()).ToArray();

            MonthlySeries.Clear();
            MonthlySeries.Add(new ColumnSeries<double>
            {
                Values = dailyData.Select(d => d.HorasNormais).ToArray(),
                Name = "Reais",
                Fill = new SolidColorPaint(SKColors.Green),
                Stroke = null
            });
            MonthlySeries.Add(new ColumnSeries<double>
            {
                Values = dailyData.Select(d => d.HorasExtras).ToArray(),
                Name = "Extra",
                Fill = new SolidColorPaint(SKColors.Orange),
                Stroke = null
            });

            MonthlyXAxes.Clear();
            MonthlyXAxes.Add(new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                TextSize = 10
            });

            MonthlyYAxes.Clear();
            MonthlyYAxes.Add(new Axis { Labeler = value => $"{value}h" });
        }
        catch (Exception ex)
        {
            SubtitleLabel.Text = "Erro ao carregar dados.";
            NAVIGEST.Android.GlobalErro.TratarErro(ex);
        }
    }

    private void OnBackToAnnualClicked(object sender, EventArgs e)
    {
        AnnualChartContainer.IsVisible = true;
        MonthlyChartContainer.IsVisible = false;
        TitleLabel.Text = "Evolução Anual";
        SubtitleLabel.Text = "Toque num mês para ver detalhes";
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
}

public partial class MonthItem : ObservableObject
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    
    [ObservableProperty]
    private bool _isSelected;
}
