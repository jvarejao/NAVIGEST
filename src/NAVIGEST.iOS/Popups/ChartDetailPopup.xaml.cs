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
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;
using SkiaSharp;

namespace NAVIGEST.iOS.Popups;

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
    public ObservableCollection<RectangularSection> MonthlySections { get; set; } = new();

    public ICommand AnnualChartClickCommand { get; }

    private bool _isNavigating = false;

    public ChartDetailPopup(Colaborador colab, List<MonthlyHoursData> annualData, int year)
    {
        InitializeComponent();
        _colaborador = colab;
        _annualData = annualData;
        _year = year;

        // Use object to be safe against type mismatches
        AnnualChartClickCommand = new RelayCommand<object>(OnAnnualChartClicked);
        
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

    private async void OnAnnualChartClicked(object? parameter)
    {
        if (_isNavigating) return;

        try
        {
            var points = parameter as IEnumerable<ChartPoint>;
            if (points == null || !points.Any()) return;

            var point = points.FirstOrDefault();
            if (point == null) return;

            int monthIndex = point.Index; // 0-11
            if (monthIndex >= 0 && monthIndex < Months.Count)
            {
                var monthItem = Months[monthIndex];
                await SelectMonth(monthItem);
            }
        }
        catch (Exception ex)
        {
            NAVIGEST.iOS.GlobalErro.TratarErro(ex);
        }
    }

    private async void OnMonthSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isNavigating) return;

        try 
        {
            if (e.CurrentSelection.FirstOrDefault() is MonthItem item)
            {
                await SelectMonth(item);
            }
        }
        catch (Exception ex)
        {
            NAVIGEST.iOS.GlobalErro.TratarErro(ex);
        }
    }

    private async Task SelectMonth(MonthItem item)
    {
        if (_isNavigating) return;
        _isNavigating = true;

        try
        {
            if (item == null) return;

            foreach (var m in Months) m.IsSelected = false;
            item.IsSelected = true;
            SelectedMonth = item;
            
            // Refresh UI for selection (hacky if not using proper MVVM notification for items)
            // Ideally MonthItem should implement INotifyPropertyChanged
            
            if (TitleLabel != null) TitleLabel.Text = $"Detalhe: {item.Name} {_year}";
            if (SubtitleLabel != null) SubtitleLabel.Text = "Carregando dados diários...";
            
            // Give the chart time to finish processing the click/tooltip animation before hiding it
            await Task.Delay(300);

            if (AnnualChartContainer != null) AnnualChartContainer.IsVisible = false;
            if (MonthlyChartContainer != null) MonthlyChartContainer.IsVisible = true;

            await LoadDailyDataAsync(item.Number);
            
            if (SubtitleLabel != null) SubtitleLabel.Text = "Visualização diária";
        }
        catch (Exception ex)
        {
            NAVIGEST.iOS.GlobalErro.TratarErro(ex);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async Task LoadDailyDataAsync(int month)
    {
        try
        {
            if (_colaborador == null)
            {
                if (SubtitleLabel != null) SubtitleLabel.Text = "Erro: Colaborador não identificado.";
                return;
            }

            var dailyData = await DatabaseService.GetDailyHoursStatsAsync(_colaborador.ID, month, _year);
            
            // Ensure UI updates are on Main Thread
            MainThread.BeginInvokeOnMainThread(() => 
            {
                try 
                {
                    var labels = dailyData.Select(d => d.Dia.ToString()).ToArray();

                    MonthlySeries.Clear();
                    
                    // 1. Horas Reais (Verde)
                    MonthlySeries.Add(new LineSeries<double>
                    {
                        Values = dailyData.Select(d => d.HorasNormais).ToArray(),
                        Name = "Reais",
                        Fill = null,
                        GeometrySize = 8,
                        Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                        GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 }
                    });

                    // 2. Horas Ideais (Cinza tracejado)
                    MonthlySeries.Add(new LineSeries<double>
                    {
                        Values = dailyData.Select(d => d.HorasIdeais).ToArray(),
                        Name = "Ideais",
                        Fill = null,
                        GeometrySize = 0,
                        Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2, PathEffect = new DashEffect(new float[] { 6, 6 }) }
                    });

                    // 3. Horas Extras (Laranja)
                    MonthlySeries.Add(new LineSeries<double>
                    {
                        Values = dailyData.Select(d => d.HorasExtras).ToArray(),
                        Name = "Extra",
                        Fill = null,
                        GeometrySize = 5,
                        Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                        GeometryStroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 }
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

                    // Highlight Weekends
                    MonthlySections.Clear();
                    for (int i = 0; i < dailyData.Count; i++)
                    {
                        var day = dailyData[i];
                        if (day.Data.DayOfWeek == DayOfWeek.Saturday || day.Data.DayOfWeek == DayOfWeek.Sunday)
                        {
                            MonthlySections.Add(new RectangularSection
                            {
                                Xi = i - 0.5,
                                Xj = i + 0.5,
                                Fill = new SolidColorPaint(new SKColor(200, 200, 200, 50)) // Light gray background
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    NAVIGEST.iOS.GlobalErro.TratarErro(ex);
                }
            });
        }
        catch (Exception ex)
        {
            if (SubtitleLabel != null) SubtitleLabel.Text = "Erro ao carregar dados.";
            NAVIGEST.iOS.GlobalErro.TratarErro(ex);
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

    private async void OnDebugClicked(object sender, EventArgs e)
    {
        try
        {
            var popup = new LogViewerPopup();
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open debug popup: {ex}");
        }
    }

    private async void OnSwipedLeft(object sender, SwipedEventArgs e)
    {
        // Next Month
        if (SelectedMonth == null) return;
        int nextIndex = SelectedMonth.Number; // Current is 1-12, index in list is 0-11. So next index is just Number.
        if (nextIndex < Months.Count)
        {
            await SelectMonth(Months[nextIndex]);
        }
    }

    private async void OnSwipedRight(object sender, SwipedEventArgs e)
    {
        // Previous Month
        if (SelectedMonth == null) return;
        int prevIndex = SelectedMonth.Number - 2; // Current is 1-12. Prev index is Number - 2.
        if (prevIndex >= 0)
        {
            await SelectMonth(Months[prevIndex]);
        }
    }
}

public partial class MonthItem : ObservableObject
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    
    [ObservableProperty]
    private bool _isSelected;
}
