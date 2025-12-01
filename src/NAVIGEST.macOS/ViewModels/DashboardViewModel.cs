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
using System.Collections.Generic;

namespace NAVIGEST.macOS.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
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
        private MonthlyHoursData? _selectedDetailMonth;

        [ObservableProperty]
        private bool _isAnnualView = true;

        public ICommand RefreshCommand { get; }
        public ICommand DrillDownCommand { get; }
        public ICommand SelectDetailMonthCommand { get; }
        public ICommand ShowAbsenceDetailsCommand { get; }
        public ICommand ShowAnnualViewCommand { get; }
        
        public DashboardViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            DrillDownCommand = new AsyncRelayCommand<MonthlyHoursData>(OnDrillDownAsync);
            SelectDetailMonthCommand = new AsyncRelayCommand<MonthlyHoursData>(OnDrillDownAsync);
            ShowAbsenceDetailsCommand = new AsyncRelayCommand<AbsenceSummary>(OnShowAbsenceDetailsAsync);
            ShowAnnualViewCommand = new RelayCommand(ShowAnnualView);
        }

        private void ShowAnnualView()
        {
            IsAnnualView = true;
            SelectedDetailMonth = null;
            foreach (var m in MonthlyData) m.IsSelected = false;
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

        [ObservableProperty]
        private int _selectedChartIndex = -1;

        partial void OnSelectedChartIndexChanged(int value)
        {
            if (value >= 0 && value < MonthlyData.Count)
            {
                var month = MonthlyData[value];
                _ = OnDrillDownAsync(month);
                
                // Reset selection to allow clicking the same month again
                MainThread.BeginInvokeOnMainThread(async () => 
                {
                    await Task.Delay(200);
                    SelectedChartIndex = -1;
                });
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

        [RelayCommand]
        private async Task ChartSelectionChanged(object? data)
        {
            if (data is MonthlyHoursData month)
            {
                await OnDrillDownAsync(month);
            }
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
                
                // Use GetDailyHoursStatsAsync as it returns DailyHoursData with AbsencePoint
                var daily = await DatabaseService.GetDailyHoursStatsAsync(
                    IsAllSelected ? null : SelectedColaborador?.ID, 
                    month.MesNumero, 
                    month.Ano);
                
                SelectedMonthDailyData.Clear();
                foreach(var d in daily) SelectedMonthDailyData.Add(d);
                
                IsAnnualView = false; // Switch to Daily View
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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
        private void CloseAbsenceDetail()
        {
            IsAbsenceDetailVisible = false;
        }
    }
}
