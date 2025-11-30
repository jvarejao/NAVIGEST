using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using Microsoft.Maui.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.Kernel;
using SkiaSharp;
using System.Collections.Generic;

namespace NAVIGEST.macOS.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private ISeries[] _series = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _xAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] _yAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private ObservableCollection<Colaborador> _colaboradores = new();

        [ObservableProperty]
        private ObservableCollection<Colaborador> _filteredColaboradores = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCollaboratorName))]
        private Colaborador? _selectedColaborador;

        public string SelectedCollaboratorName => SelectedColaborador?.Nome ?? "Todos";

        [ObservableProperty]
        private bool _isAllSelected;

        [ObservableProperty]
        private bool _isSingleSelected;

        [ObservableProperty]
        private DashboardMetrics _metrics = new();

        [ObservableProperty]
        private ObservableCollection<MonthlyHoursData> _monthlyData = new();

        [ObservableProperty]
        private ObservableCollection<AbsenceSummary> _absenceData = new();

        [ObservableProperty]
        private ObservableCollection<ClientHoursSummary> _clientData = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _selectedYear = DateTime.Now.Year;

        // UI State Properties
        [ObservableProperty]
        private bool _isCollaboratorPickerVisible;

        [ObservableProperty]
        private string _collaboratorSearchText = string.Empty;

        [ObservableProperty]
        private bool _isChartDetailVisible;

        [ObservableProperty]
        private string _selectedMonthDetailTitle = string.Empty;

        [ObservableProperty]
        private ObservableCollection<DailyHoursData> _selectedMonthDailyData = new();

        [ObservableProperty]
        private bool _isAbsenceDetailVisible;

        [ObservableProperty]
        private string _selectedAbsenceDetailTitle = string.Empty;

        [ObservableProperty]
        private AbsenceSummary? _selectedAbsenceSummary;

        [ObservableProperty]
        private ObservableCollection<string> _selectedAbsenceDetails = new();


        [ObservableProperty]
        private ISeries[] _detailSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _detailXAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] _detailYAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private MonthlyHoursData? _selectedDetailMonth;

        public ICommand RefreshCommand { get; }
        public ICommand DrillDownCommand { get; }
        public ICommand SelectDetailMonthCommand { get; }
        public ICommand ShowAbsenceDetailsCommand { get; }
        public ICommand DataPointerDownCommand { get; }
        
        public DashboardViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            DrillDownCommand = new AsyncRelayCommand<MonthlyHoursData>(OnDrillDownAsync);
            SelectDetailMonthCommand = new AsyncRelayCommand<MonthlyHoursData>(OnDrillDownAsync);
            ShowAbsenceDetailsCommand = new AsyncRelayCommand<AbsenceSummary>(OnShowAbsenceDetailsAsync);
            DataPointerDownCommand = new AsyncRelayCommand<IEnumerable<ChartPoint>>(OnDataPointerDownAsync);
        }

        public async Task InitializeAsync()
        {
            await LoadCollaboratorsAsync();
        }

        private async Task LoadCollaboratorsAsync()
        {
            IsLoading = true;
            try
            {
                var list = await DatabaseService.GetColaboradoresAsync();
                Colaboradores.Clear();
                
                // Add "ALL" option
                var all = new Colaborador { ID = -1, Nome = "TODOS" };
                Colaboradores.Add(all);

                foreach (var c in list) Colaboradores.Add(c);

                SelectedColaborador = all; // Default to ALL
                FilteredColaboradores = new ObservableCollection<Colaborador>(Colaboradores);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedColaboradorChanged(Colaborador? value)
        {
            if (value == null) return;
            IsAllSelected = value.ID == -1;
            IsSingleSelected = !IsAllSelected;
            _ = LoadDataAsync();
        }

        partial void OnCollaboratorSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredColaboradores = new ObservableCollection<Colaborador>(Colaboradores);
            }
            else
            {
                var filtered = Colaboradores.Where(c => c.Nome.Contains(value, StringComparison.OrdinalIgnoreCase));
                FilteredColaboradores = new ObservableCollection<Colaborador>(filtered);
            }
        }

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                int? colabId = IsAllSelected ? null : SelectedColaborador?.ID;

                // 1. Metrics
                Metrics = await DatabaseService.GetDashboardMetricsAsync(colabId, SelectedYear);

                // 2. Monthly Data (Chart)
                var monthly = await DatabaseService.GetMonthlyHoursStatsAsync(colabId, SelectedYear);
                MonthlyData.Clear();
                foreach (var m in monthly) MonthlyData.Add(m);

                UpdateChart();

                // 3. Absences
                var absences = await DatabaseService.GetAbsenceStatsAsync(colabId, SelectedYear);
                AbsenceData.Clear();
                foreach (var a in absences) AbsenceData.Add(a);

                // 4. Clients (Top 5)
                var clients = await DatabaseService.GetClientHoursStatsAsync(colabId, SelectedYear);
                ClientData.Clear();
                foreach (var c in clients) ClientData.Add(c);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateChart()
        {
            var labels = MonthlyData.Select(m => m.Mes).ToArray();
            
            Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = MonthlyData.Select(m => m.HorasNormais).ToArray(),
                    Name = "Reais",
                    Fill = null,
                    GeometrySize = 10,
                    Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                    GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 }
                },
                new LineSeries<double>
                {
                    Values = MonthlyData.Select(m => m.HorasIdeais).ToArray(),
                    Name = "Ideais",
                    Fill = null,
                    GeometrySize = 0, 
                    Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2, PathEffect = new DashEffect(new float[] { 6, 6 }) }
                },
                new LineSeries<double>
                {
                    Values = MonthlyData.Select(m => m.HorasExtras).ToArray(),
                    Name = "Extra",
                    Fill = null,
                    GeometrySize = 5,
                    Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                    GeometryStroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 }
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 0,
                    TextSize = 12,
                    MinStep = 1
                }
            };

            YAxes = new Axis[]
            {
                new Axis { Labeler = value => $"{value}h" }
            };
        }

        private async Task OnDrillDownAsync(MonthlyHoursData? month)
        {
            if (month == null || !IsSingleSelected || SelectedColaborador == null) return;

            try
            {
                // Update selection state
                foreach (var m in MonthlyData)
                {
                    m.IsSelected = (m == month);
                }

                SelectedDetailMonth = month; // Update selection for UI
                var days = await DatabaseService.GetDailyHoursStatsAsync(SelectedColaborador.ID, month.MesNumero, SelectedYear);
                
                SelectedMonthDetailTitle = $"{month.Mes} {month.Ano} - Detalhes";
                SelectedMonthDailyData.Clear();
                foreach(var d in days) SelectedMonthDailyData.Add(d);
                
                UpdateDetailChart(days);

                IsChartDetailVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void UpdateDetailChart(List<DailyHoursData> days)
        {
            var labels = days.Select(d => d.Data.Day.ToString()).ToArray();

            DetailSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = days.Select(d => d.HorasNormais).ToArray(),
                    Name = "Reais",
                    Fill = null,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                    GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 }
                },
                new LineSeries<double>
                {
                    Values = days.Select(d => 8.0).ToArray(), // Assuming 8h ideal per day
                    Name = "Ideais",
                    Fill = null,
                    GeometrySize = 0, 
                    Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) }
                },
                new LineSeries<double>
                {
                    Values = days.Select(d => d.HorasExtras).ToArray(),
                    Name = "Extra",
                    Fill = null,
                    GeometrySize = 4,
                    Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                    GeometryStroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 }
                },
                new ColumnSeries<double>
                {
                    Values = days.Select(d => d.IsAbsent ? 8.0 : 0).ToArray(),
                    Name = "Ausência",
                    Fill = new SolidColorPaint(SKColors.Red.WithAlpha(100)), // Semi-transparent red
                    Stroke = null,
                    MaxBarWidth = 20
                }
            };

            DetailXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 0,
                    TextSize = 12,
                    MinStep = 1,
                    ForceStepToMin = true,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(50)) { StrokeThickness = 1 }
                }
            };

            DetailYAxes = new Axis[]
            {
                new Axis { Labeler = value => $"{value}h" }
            };
        }

        private async Task OnShowAbsenceDetailsAsync(AbsenceSummary? summary)
        {
            if (summary == null) return;
            try
            {
                int? colabId = IsAllSelected ? null : SelectedColaborador?.ID;
                var details = await DatabaseService.GetAbsenceDetailsAsync(colabId, summary.Tipo, SelectedYear);
                
                SelectedAbsenceSummary = summary;
                SelectedAbsenceDetailTitle = $"{summary.Tipo} - Detalhes";
                SelectedAbsenceDetails.Clear();
                foreach(var d in details) SelectedAbsenceDetails.Add(d);
                
                IsAbsenceDetailVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task OnDataPointerDownAsync(IEnumerable<ChartPoint>? points)
        {
            if (points == null || !points.Any()) return;
            
            var point = points.First();
            var index = point.Index;
            
            if (index >= 0 && index < MonthlyData.Count)
            {
                var monthData = MonthlyData[index];
                await OnDrillDownAsync(monthData);
            }
        }

        [RelayCommand]
        private void OpenCollaboratorPicker()
        {
            FilteredColaboradores = new ObservableCollection<Colaborador>(Colaboradores);
            CollaboratorSearchText = string.Empty;
            IsCollaboratorPickerVisible = true;
        }

        [RelayCommand]
        private void CloseCollaboratorPicker()
        {
            IsCollaboratorPickerVisible = false;
        }

        [RelayCommand]
        private void SelectCollaborator(Colaborador colab)
        {
            SelectedColaborador = colab;
            IsCollaboratorPickerVisible = false;
        }

        [RelayCommand]
        private void CloseChartDetail()
        {
            IsChartDetailVisible = false;
        }

        [RelayCommand]
        private void CloseAbsenceDetail()
        {
            IsAbsenceDetailVisible = false;
        }
    }
}
