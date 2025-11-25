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
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using SkiaSharp;

namespace NAVIGEST.macOS.Popups;

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
        
        // Adjust size for macOS
        var displayInfo = DeviceDisplay.MainDisplayInfo;
        var width = displayInfo.Width / displayInfo.Density;
        var height = displayInfo.Height / displayInfo.Density;
        
        // Use 80% of screen size, but clamp to reasonable limits
        var targetWidth = Math.Min(width * 0.8, 1200);
        var targetHeight = Math.Min(height * 0.8, 900);
        
        // Size = new Microsoft.Maui.Graphics.Size(targetWidth, targetHeight);

        _colaborador = colab;
        _annualData = annualData;
        _year = year;

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
            Console.WriteLine(ex);
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
            Console.WriteLine(ex);
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
            
            if (TitleLabel != null) TitleLabel.Text = $"Detalhe: {item.Name} {_year}";
            if (SubtitleLabel != null) SubtitleLabel.Text = "Carregando dados diários...";
            
            await Task.Delay(300);

            if (AnnualChartContainer != null) AnnualChartContainer.IsVisible = false;
            if (MonthlyChartContainer != null) MonthlyChartContainer.IsVisible = true;

            await LoadDailyDataAsync(item.Number);
            
            if (SubtitleLabel != null) SubtitleLabel.Text = "Visualização diária";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
            
            MainThread.BeginInvokeOnMainThread(() => 
            {
                try 
                {
                    var labels = dailyData.Select(d => d.Dia.ToString()).ToArray();

                    MonthlySeries.Clear();
                    
                    MonthlySeries.Add(new LineSeries<double>
                    {
                        Values = dailyData.Select(d => d.HorasNormais).ToArray(),
                        Name = "Reais",
                        Fill = null,
                        GeometrySize = 8,
                        Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                        GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 }
                    });

                    MonthlySeries.Add(new LineSeries<double>
                    {
                        Values = dailyData.Select(d => d.HorasIdeais).ToArray(),
                        Name = "Ideais",
                        Fill = null,
                        GeometrySize = 0,
                        Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2, PathEffect = new DashEffect(new float[] { 6, 6 }) }
                    });

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
                                Fill = new SolidColorPaint(new SKColor(200, 200, 200, 50))
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }
        catch (Exception ex)
        {
            if (SubtitleLabel != null) SubtitleLabel.Text = "Erro ao carregar dados.";
            Console.WriteLine(ex);
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
        // ((CommunityToolkit.Maui.Views.Popup)this).Close();
        // Temporary workaround for build
        try {
             var method = this.GetType().GetMethod("Close");
             method?.Invoke(this, new object?[] { null });
        } catch {}
    }

    private void OnDebugClicked(object sender, EventArgs e)
    {
        // Debug popup not implemented in macOS yet
        Console.WriteLine("Debug clicked");
    }

    private async void OnSwipedLeft(object sender, SwipedEventArgs e)
    {
        if (SelectedMonth == null) return;
        int nextIndex = SelectedMonth.Number; 
        if (nextIndex < Months.Count)
        {
            await SelectMonth(Months[nextIndex]);
        }
    }

    private async void OnSwipedRight(object sender, SwipedEventArgs e)
    {
        if (SelectedMonth == null) return;
        int prevIndex = SelectedMonth.Number - 2; 
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
