using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;
using Microsoft.Maui.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.Kernel;
using SkiaSharp;
using System.Collections.Generic;

namespace NAVIGEST.iOS.ViewModels
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
        private Colaborador? _selectedColaborador;

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

        public ICommand RefreshCommand { get; }
        public ICommand DrillDownCommand { get; }
        public ICommand ShowAbsenceDetailsCommand { get; }
        public ICommand DataPointerDownCommand { get; }
        public ICommand OpenChartDetailCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            DrillDownCommand = new AsyncRelayCommand<MonthlyHoursData>(OnDrillDownAsync);
            ShowAbsenceDetailsCommand = new AsyncRelayCommand<AbsenceSummary>(OnShowAbsenceDetailsAsync);
            DataPointerDownCommand = new RelayCommand<IEnumerable<ChartPoint>>(OnDataPointerDown);
            OpenChartDetailCommand = new AsyncRelayCommand(OnOpenChartDetailAsync);
        }

        private async Task OnOpenChartDetailAsync()
        {
            if (!IsSingleSelected || SelectedColaborador == null) return;
            
            // Trigger event to open popup
            RequestOpenChartDetail?.Invoke(this, (SelectedColaborador, MonthlyData.ToList(), SelectedYear));
        }

        public event EventHandler<(Colaborador Colab, List<MonthlyHoursData> Data, int Year)>? RequestOpenChartDetail;
        public event EventHandler<(MonthlyHoursData Month, List<DailyHoursData> Days)>? RequestOpenDailyPopup;
        public event EventHandler<(AbsenceSummary Summary, List<string> Details)>? RequestOpenAbsencePopup;

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
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
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
                GlobalErro.TratarErro(ex);
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
                    TextSize = 12
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
                var days = await DatabaseService.GetDailyHoursStatsAsync(SelectedColaborador.ID, month.MesNumero, SelectedYear);
                RequestOpenDailyPopup?.Invoke(this, (month, days));
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
            }
        }

        private async Task OnShowAbsenceDetailsAsync(AbsenceSummary? summary)
        {
            if (summary == null) return;
            try
            {
                int? colabId = IsAllSelected ? null : SelectedColaborador?.ID;
                var details = await DatabaseService.GetAbsenceDetailsAsync(colabId, summary.Tipo, SelectedYear);
                RequestOpenAbsencePopup?.Invoke(this, (summary, details));
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
            }
        }

        private void OnDataPointerDown(IEnumerable<ChartPoint>? points)
        {
            // Handle chart click if needed
        }
    }
}