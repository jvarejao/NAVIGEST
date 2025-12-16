using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NAVIGEST.iOS.Models;

#if ANDROID
using Android.App;
using Android.Content;
using Android.Util;
#endif

namespace NAVIGEST.iOS.ViewModels
{
    public partial class HoursEntryViewModel : ObservableObject
    {
        // Delegate que a Page injeta para abrir o editor de notas
        public Func<TimeEntry, System.Threading.Tasks.Task<string?>>? RequestNotesEditorAsync { get; set; }

        // Header
        [ObservableProperty] private ObservableCollection<Collaborator> collaborators = new();
        [ObservableProperty] private Collaborator? selectedCollaborator;

        public ObservableCollection<string> Months { get; } =
            new(new[] { "01 - Janeiro", "02 - Fevereiro", "03 - Março", "04 - Abril", "05 - Maio", "06 - Junho",
                        "07 - Julho", "08 - Agosto", "09 - Setembro", "10 - Outubro", "11 - Novembro", "12 - Dezembro" });

        [ObservableProperty] private string? selectedMonth;
        [ObservableProperty] private string yearText = DateTime.Now.Year.ToString();
        [ObservableProperty] private string hourRateText = ""; // admin
        [ObservableProperty] private bool isAdmin = false;

        [ObservableProperty] private string inlineMessage = "";

        // Ações
        [ObservableProperty] private bool hasChanges = false;
        [ObservableProperty] private bool canPaste = false;

        // Tabela
        [ObservableProperty] private ObservableCollection<TimeEntry> rows = new();

        // Listas de apoio globais
        [ObservableProperty] private ObservableCollection<Client> clients = new();
        [ObservableProperty] private ObservableCollection<CostCenter> costCenters = new();

        // Totais
        public string TotalHoursText => $"Horas: {Rows.Sum(r => r.Hours):0.##}";
        public string TotalOvertimeText => $"Extras: {Rows.Sum(r => r.Overtime):0.##}";
        public string TotalCostText => $"Custo Estimado: {((Rows.Sum(r => r.Hours) + Rows.Sum(r => r.Overtime)) * GetHourRate()):0.##} €";

        public HoursEntryViewModel()
        {
            try
            {
                // MOCKS (layout)
                Collaborators.Add(new Collaborator { Id = 1, Name = "Ana Silva" });
                Collaborators.Add(new Collaborator { Id = 2, Name = "Bruno Costa" });

                Clients.Add(new Client { Id = 10, Name = "Cliente A" });
                Clients.Add(new Client { Id = 20, Name = "Cliente B" });

                CostCenters.Add(new CostCenter { Id = 100, ClientId = 10, Name = "CC A1" });
                CostCenters.Add(new CostCenter { Id = 101, ClientId = 10, Name = "CC A2" });
                CostCenters.Add(new CostCenter { Id = 200, ClientId = 20, Name = "CC B1" });

                SelectedMonth = $"{DateTime.Now:MM} - {GetMonthName(DateTime.Now.Month)}";

                var today = DateTime.Today;

                var r1 = new TimeEntry(today) { Hours = 4, Overtime = 0, AddDelText = "ADD", ParentVm = this };
                var r2 = new TimeEntry(today.AddDays(1)) { Hours = 3.5, Overtime = 1, AddDelText = "ADD", ParentVm = this };

                Rows.Add(r1);
                Rows.Add(r2);

                UpdateTotals();
                ValidateAll();
            }
            catch (System.Exception ex) { TratarErro(ex); }
        }

        // ===== Commands de página =====

        public ICommand LoadMonthCommand => new RelayCommand(() =>
        {
            try
            {
                InlineMessage = $"A carregar {SelectedMonth} / {YearText}… (layout)";
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public ICommand SaveMonthCommand => new RelayCommand(() =>
        {
            try
            {
                InlineMessage = "Guardado (layout).";
                HasChanges = false;
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public ICommand DeleteMonthCommand => new RelayCommand(() =>
        {
            try
            {
                InlineMessage = "Mês eliminado (layout).";
                Rows.Clear();
                UpdateTotals();
                HasChanges = true;
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public ICommand CopyRowsCommand => new RelayCommand(() =>
        {
            try
            {
                CanPaste = Rows.Any(r => r.IsSelected);
                InlineMessage = CanPaste ? "Linhas copiadas (layout)." : "Nenhuma linha selecionada.";
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public ICommand PasteRowsCommand => new RelayCommand(() =>
        {
            try
            {
                var selected = Rows.Where(r => r.IsSelected).ToList();
                foreach (var it in selected)
                {
                    var clone = it.Clone();
                    clone.ParentVm = this; // garante callbacks
                    Rows.Add(clone);
                }
                HasChanges = selected.Any();
                InlineMessage = selected.Any() ? "Linhas coladas (layout)." : "Nada para colar.";
                UpdateTotals();
                ValidateAll();
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public ICommand ExportMonthCommand => new RelayCommand(() =>
        {
            try
            {
                InlineMessage = $"Exportação do mês {SelectedMonth} / {YearText} (layout).";
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public ICommand ExportYearCommand => new RelayCommand(() =>
        {
            try
            {
                InlineMessage = $"Exportação do ano {YearText} (layout).";
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public IRelayCommand<TimeEntry> AddDelCommand => new RelayCommand<TimeEntry>(item =>
        {
            try
            {
                if (item is null) return;

                if (item.AddDelText?.Equals("DEL", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Rows.Remove(item);
                    HasChanges = true;
                    UpdateTotals();
                    ValidateAll();
                    return;
                }

                // ADD: insere abaixo com pré-preenchimento
                var newRow = new TimeEntry(item.Date)
                {
                    SelectedClient = item.SelectedClient,
                    SelectedCostCenter = item.SelectedCostCenter,
                    Hours = item.Hours,
                    Overtime = item.Overtime,
                    Notes = item.Notes,
                    AddDelText = "ADD",
                    ParentVm = this
                };

                var index = Rows.IndexOf(item);
                if (index >= 0 && index < Rows.Count - 1)
                    Rows.Insert(index + 1, newRow);
                else
                    Rows.Add(newRow);

                HasChanges = true;
                UpdateTotals();
                ValidateAll();
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        public IRelayCommand<TimeEntry> EditNotesCommand => new AsyncRelayCommand<TimeEntry>(async item =>
        {
            try
            {
                if (item is null) return;
                if (RequestNotesEditorAsync == null)
                {
                    InlineMessage = "Editor de notas indisponível.";
                    return;
                }

                var res = await RequestNotesEditorAsync(item);
                if (res != null)
                {
                    item.Notes = res;
                    HasChanges = true;
                    ValidateRow(item);
                }
            }
            catch (System.Exception ex) { TratarErro(ex); }
        });

        // ===== Utilitários =====

        internal void UpdateTotals()
        {
            OnPropertyChanged(nameof(TotalHoursText));
            OnPropertyChanged(nameof(TotalOvertimeText));
            OnPropertyChanged(nameof(TotalCostText));
        }

        internal void ValidateAll()
        {
            foreach (var r in Rows)
                ValidateRow(r);
        }

        internal void ValidateRow(TimeEntry r)
        {
            bool invalid =
                (r.Hours < 0 || r.Hours > 24) ||
                (r.Overtime < 0 || r.Overtime > 24) ||
                (r.SelectedClient == null && (r.Hours > 0 || r.Overtime > 0)) ||
                (r.SelectedCostCenter == null && (r.Hours > 0 || r.Overtime > 0));

            r.HasError = invalid;

            // Botão ADD/DEL: se não há dados essenciais, mostra DEL para permitir limpar
            r.AddDelText = invalid || (r.SelectedClient == null && r.SelectedCostCenter == null && r.Hours == 0 && r.Overtime == 0 && string.IsNullOrWhiteSpace(r.Notes))
                ? "DEL" : "ADD";
        }

        private string GetMonthName(int m) =>
            System.Globalization.CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetMonthName(m);

        private double GetHourRate() => double.TryParse(HourRateText, out var v) ? v : 0;

        private void TratarErro(System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            InlineMessage = "Ocorreu um erro (layout).";
        }
    }

    // ====== MODELOS ======
    public partial class TimeEntry : ObservableObject
    {
        public TimeEntry(DateTime date) { Date = date; }

        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private DateTime date;
        [ObservableProperty] private string addDelText = "ADD";

        [ObservableProperty] private Client? selectedClient;
        partial void OnSelectedClientChanged(Client? value)
        {
            OnPropertyChanged(nameof(FilteredCostCenters));
            ParentVm?.ValidateRow(this);
        }

        [ObservableProperty] private CostCenter? selectedCostCenter;
        partial void OnSelectedCostCenterChanged(CostCenter? value)
        {
            ParentVm?.ValidateRow(this);
        }

        [ObservableProperty] private double hours;
        partial void OnHoursChanged(double value)
        {
            ParentVm?.UpdateTotals();
            ParentVm?.ValidateRow(this);
        }

        [ObservableProperty] private double overtime;
        partial void OnOvertimeChanged(double value)
        {
            ParentVm?.UpdateTotals();
            ParentVm?.ValidateRow(this);
        }

        [ObservableProperty] private string? notes;
        partial void OnNotesChanged(string? value)
        {
            ParentVm?.ValidateRow(this);
        }

        [ObservableProperty] private bool hasError;

        // Ligação reversa simples para poder chamar validações/totais
        internal HoursEntryViewModel? ParentVm { get; set; }

        public string HoursText
        {
            get => Hours == 0 ? "" : Hours.ToString("0.##");
            set => Hours = double.TryParse(value, out var v) ? v : 0;
        }

        public string OvertimeText
        {
            get => Overtime == 0 ? "" : Overtime.ToString("0.##");
            set => Overtime = double.TryParse(value, out var v) ? v : 0;
        }

        // Filtra por cliente atual (atualiza quando SelectedClient muda)
        public ObservableCollection<CostCenter> FilteredCostCenters =>
            new(AppLevelStaticLists.CostCenters.Where(cc =>
                SelectedClient == null || cc.ClientId == SelectedClient.Id));

        public TimeEntry Clone()
        {
            return new TimeEntry(Date)
            {
                IsSelected = false,
                AddDelText = this.AddDelText,
                SelectedClient = this.SelectedClient,
                SelectedCostCenter = this.SelectedCostCenter,
                Hours = this.Hours,
                Overtime = this.Overtime,
                Notes = this.Notes,
                ParentVm = this.ParentVm
            };
        }
    }

    public class Collaborator { public int Id { get; set; } public string Name { get; set; } = ""; }
    public class Client { public int Id { get; set; } public string Name { get; set; } = ""; }
    public class CostCenter { public int Id { get; set; } public int ClientId { get; set; } public string Name { get; set; } = ""; }

    internal static class AppLevelStaticLists
    {
        public static readonly ObservableCollection<CostCenter> CostCenters = new(
            new[]
            {
                new CostCenter { Id=100, ClientId=10, Name="CC A1" },
                new CostCenter { Id=101, ClientId=10, Name="CC A2" },
                new CostCenter { Id=200, ClientId=20, Name="CC B1" },
            }
        );
    }
}
