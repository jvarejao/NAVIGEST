using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;
using Microsoft.Maui.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.Kernel;
using SkiaSharp;

namespace NAVIGEST.Android.PageModels
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

        public async Task InitializeAsync()
        {
            await LoadCollaboratorsAsync();
            // Default to "All" or the first one? 
            // Requirement says: "Picker to choose: Specific or ALL".
            // Let's add a dummy "ALL" collaborator to the list or handle it separately.
            // Better to handle it by adding a special item to the list.
        }

        private async Task LoadCollaboratorsAsync()
        {
            IsLoading = true;
            try
            {
                var list = await DatabaseService.GetColaboradoresAsync();
                Colaboradores.Clear();
                
                // Add "TODOS" option
                Colaboradores.Add(new Colaborador { ID = -1, Nome = "TODOS OS COLABORADORES" });
                
                foreach (var c in list)
                {
                    Colaboradores.Add(c);
                }

                SelectedColaborador = Colaboradores.FirstOrDefault(); // Select "TODOS" by default
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading collaborators: {ex.Message}");
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

                // 2. Charts Data (Only for Single, or maybe aggregate for All?)
                // Req: "If ALL... General summary... If ONE... Chart"
                if (IsSingleSelected)
                {
                    var monthly = await DatabaseService.GetMonthlyHoursStatsAsync(colabId, SelectedYear);
                    MonthlyData = new ObservableCollection<MonthlyHoursData>(monthly);

                    // Configure LiveCharts
                    var labels = monthly.Select(m => m.Mes).ToArray();
                    
                    Series = new ISeries[]
                    {
                        new LineSeries<double>
                        {
                            Values = monthly.Select(m => m.HorasNormais).ToArray(),
                            Name = "Reais",
                            Fill = null,
                            GeometrySize = 10,
                            Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                            GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 }
                        },
                        new LineSeries<double>
                        {
                            Values = monthly.Select(m => m.HorasIdeais).ToArray(),
                            Name = "Ideais",
                            Fill = null,
                            GeometrySize = 0, 
                            Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2, PathEffect = new DashEffect(new float[] { 6, 6 }) }
                        },
                        new LineSeries<double>
                        {
                            Values = monthly.Select(m => m.HorasExtras).ToArray(),
                            Name = "Extra",
                            Fill = null,
                            GeometrySize = 5,
                            Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 }
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
                        new Axis
                        {
                            Labeler = value => $"{value}h"
                        }
                    };
                }
                else
                {
                    MonthlyData.Clear();
                    Series = Array.Empty<ISeries>();
                    XAxes = Array.Empty<Axis>();
                    YAxes = Array.Empty<Axis>();
                }

                // 3. Absence & Client Data (For both)
                var absences = await DatabaseService.GetAbsenceStatsAsync(colabId, SelectedYear);
                AbsenceData = new ObservableCollection<AbsenceSummary>(absences);

                var clients = await DatabaseService.GetClientHoursStatsAsync(colabId, SelectedYear);
                ClientData = new ObservableCollection<ClientHoursSummary>(clients);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnDrillDownAsync(MonthlyHoursData? data)
        {
            if (data == null || !IsSingleSelected) return;

            // Open Popup with Daily Chart
            // We need to fetch daily data for that month
            var dailyData = await DatabaseService.GetDailyHoursStatsAsync(SelectedColaborador?.ID, data.MesNumero, data.Ano);
            
            // Show Popup (Need to implement Popup Service or similar interaction)
            // For now, we can use Shell.Current.ShowPopupAsync if we have the popup
            // Or trigger an event.
            // Let's assume we pass this to the View to handle the popup display.
            
            // Since we are in ViewModel, we should use a service or Messenger. 
            // But for simplicity in this context, let's expose an event or use a weak reference to the page.
            // Or better, let the View bind to this command and handle the UI part if possible, 
            // but the data fetching is here.
            
            // Let's use a simple event for now
            RequestOpenDailyPopup?.Invoke(this, (data, dailyData));
        }

        private async Task OnShowAbsenceDetailsAsync(AbsenceSummary? absence)
        {
            if (absence == null) return;
            
            int? colabId = IsAllSelected ? null : SelectedColaborador?.ID;
            var details = await DatabaseService.GetAbsenceDetailsAsync(colabId, absence.Tipo, SelectedYear);
            
            RequestOpenAbsencePopup?.Invoke(this, (absence, details));
        }

        private void OnDataPointerDown(IEnumerable<ChartPoint>? points)
        {
            var point = points?.FirstOrDefault();
            if (point == null) return;

            int index = point.Index;
            if (index >= 0 && index < MonthlyData.Count)
            {
                var item = MonthlyData[index];
                DrillDownCommand.Execute(item);
            }
        }

        public event EventHandler<(MonthlyHoursData Month, System.Collections.Generic.List<DailyHoursData> Days)>? RequestOpenDailyPopup;
        public event EventHandler<(AbsenceSummary Summary, System.Collections.Generic.List<string> Details)>? RequestOpenAbsencePopup;
    }
}
