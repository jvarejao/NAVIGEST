using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.PageModels
{
    public class AnalyticsDashboardViewModel : INotifyPropertyChanged
    {
        private readonly string[] _months = CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.AbbreviatedMonthNames
            .Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();

        private static readonly CultureInfo CurrencyCulture = new("pt-PT");

        private int _selectedMonth;
        private int _selectedYear;
        private bool _comparePreviousYear;
        private bool _isLoading;
        private MonthOption? _selectedMonthOption;
        private readonly int _minYear;
        private readonly int _maxYear;

        public ObservableCollection<int> Years { get; } = new();
        public ObservableCollection<MonthOption> MonthOptions { get; } = new();
        public ObservableCollection<AnalyticsMetricCard> SummaryCards { get; } = new();
        public ObservableCollection<DashboardChartPoint> RevenueVsCost { get; } = new();
        public ObservableCollection<DashboardChartPoint> CashFlow { get; } = new();
        public ObservableCollection<DashboardTopItem> TopClients { get; } = new();
        public ObservableCollection<DashboardTopItem> TopProducts { get; } = new();
        public ObservableCollection<DashboardTopItem> TopSellers { get; } = new();

        public Command<int> SelectMonthCommand { get; }
        public Command IncreaseYearCommand { get; }
        public Command DecreaseYearCommand { get; }

        public MonthOption? SelectedMonthOption
        {
            get => _selectedMonthOption;
            set
            {
                if (_selectedMonthOption == value) return;
                _selectedMonthOption = value;
                if (value != null && value.Index != _selectedMonth)
                {
                    _selectedMonth = value.Index;
                    OnPropertyChanged(nameof(SelectedMonth));
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeaderSubtitle));
                _ = LoadAsync();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (_selectedMonth == value) return;
                _selectedMonth = value;
                SelectedMonthOption = MonthOptions.ElementAtOrDefault(_selectedMonth);
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeaderSubtitle));
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear == value) return;
                _selectedYear = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeaderSubtitle));
                OnPropertyChanged(nameof(CompareYearLabel));
                _ = LoadAsync();
            }
        }

        public bool ComparePreviousYear
        {
            get => _comparePreviousYear;
            set
            {
                if (_comparePreviousYear == value) return;
                _comparePreviousYear = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CompareYearLabel));
                _ = LoadAsync();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string HeaderSubtitle
        {
            get
            {
                var monthLabel = (SelectedMonth >= 0 && SelectedMonth < MonthOptions.Count)
                    ? MonthOptions[SelectedMonth].Label
                    : _selectedMonth >= 0 && _selectedMonth < _months.Length ? Capitalize(_months[_selectedMonth]) : string.Empty;
                return string.IsNullOrWhiteSpace(monthLabel) ? _selectedYear.ToString() : $"{monthLabel} {_selectedYear}";
            }
        }

        public string CompareYearLabel => $"Comparar ano {_selectedYear - 1}";

        public AnalyticsDashboardViewModel()
        {
            foreach (var (m, idx) in _months.Select((m, i) => (m, i)))
                MonthOptions.Add(new MonthOption(idx, Capitalize(m).ToUpperInvariant()));

            var currentYear = DateTime.Now.Year;
            for (int y = currentYear - 3; y <= currentYear; y++) Years.Add(y);
            _minYear = Years.Min();
            _maxYear = Years.Max();

            _selectedMonth = Math.Max(0, DateTime.Now.Month - 1);
            _selectedYear = currentYear;

            SelectMonthCommand = new Command<int>(m => SelectedMonth = m);
            IncreaseYearCommand = new Command(() => SelectedYear = Math.Min(_selectedYear + 1, _maxYear));
            DecreaseYearCommand = new Command(() => SelectedYear = Math.Max(_selectedYear - 1, _minYear));

            SelectedMonthOption = MonthOptions.ElementAtOrDefault(_selectedMonth);

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var data = await DatabaseService.GetDashboardFinancialDataAsync(_selectedMonth + 1, _selectedYear);
                BuildSummaryCards(data.Summary);
                BuildRevenueVsCost(data.Monthly, data.PrevMonthly);
                BuildCashFlow(data.Monthly);
                BuildTopLists(data.TopClients, data.TopProducts, data.TopSellers);
                OnPropertyChanged(nameof(HeaderSubtitle));
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

        private void BuildSummaryCards(DatabaseService.DashboardSummary summary)
        {
            SummaryCards.Clear();

            var revenue = summary?.Revenue ?? 0;
            var expenses = summary?.Expenses ?? 0;
            var cash = summary?.Cash ?? 0;
            var margin = revenue - expenses;
            var marginPct = revenue == 0 ? 0 : margin / revenue;

            var prevRevenue = ComparePreviousYear ? summary?.PrevRevenue ?? 0 : revenue;
            var prevExpenses = ComparePreviousYear ? summary?.PrevExpenses ?? 0 : expenses;

            SummaryCards.Add(new AnalyticsMetricCard("Receitas", FormatCurrency(revenue), FormatDelta(revenue, prevRevenue), revenue >= prevRevenue));
            SummaryCards.Add(new AnalyticsMetricCard("Despesas", FormatCurrency(expenses), FormatDelta(expenses, prevExpenses), expenses <= prevExpenses));
            SummaryCards.Add(new AnalyticsMetricCard("Margem", FormatCurrency(margin), $"{marginPct:P1}", marginPct >= 0.2m));
            SummaryCards.Add(new AnalyticsMetricCard("Caixa", FormatCurrency(cash), ComparePreviousYear ? "Compara ano-1" : "Recebimentos", true));
        }

        private void BuildRevenueVsCost(IEnumerable<DatabaseService.DashboardMonthlyStat> stats, IEnumerable<DatabaseService.DashboardMonthlyStat> prevStats)
        {
            RevenueVsCost.Clear();
            var items = Enumerable.Range(1, 12)
                .Select(m => stats.FirstOrDefault(s => s.Month == m) ?? new DatabaseService.DashboardMonthlyStat { Month = m })
                .ToList();

            var prevItems = Enumerable.Range(1, 12)
                .Select(m => prevStats.FirstOrDefault(s => s.Month == m) ?? new DatabaseService.DashboardMonthlyStat { Month = m })
                .ToList();

            var maxAbs = items.Select(i => Math.Max(Math.Abs(i.Revenue), Math.Abs(i.Cost))).DefaultIfEmpty(1m).Max();
            if (ComparePreviousYear)
            {
                var prevMax = prevItems.Select(i => Math.Abs(i.Revenue)).DefaultIfEmpty(1m).Max();
                maxAbs = Math.Max(maxAbs, prevMax);
            }

            foreach (var item in items)
            {
                var label = _months.ElementAtOrDefault(item.Month - 1) ?? item.Month.ToString();
                var revenueDisplay = FormatCurrency(item.Revenue);
                var costDisplay = FormatCurrency(item.Cost);
                var display = $"{revenueDisplay} / {costDisplay}";
                var prev = prevItems.FirstOrDefault(p => p.Month == item.Month);
                var prevRevenue = prev?.Revenue ?? 0;
                var point = new DashboardChartPoint(
                    Capitalize(label),
                    item.Revenue,
                    displayOverride: display,
                    secondaryValue: item.Cost,
                    secondaryDisplay: costDisplay)
                {
                    Height = NormalizeHeight(item.Revenue, maxAbs),
                    SecondaryHeight = NormalizeHeight(item.Cost, maxAbs),
                    PrevHeight = ComparePreviousYear ? NormalizeHeight(prevRevenue, maxAbs) : 0,
                    PrevValue = prevRevenue
                };

                RevenueVsCost.Add(point);
            }
        }

        private void BuildCashFlow(IEnumerable<DatabaseService.DashboardMonthlyStat> stats)
        {
            CashFlow.Clear();
            var items = Enumerable.Range(1, 12)
                .Select(m => stats.FirstOrDefault(s => s.Month == m) ?? new DatabaseService.DashboardMonthlyStat { Month = m })
                .ToList();

            var maxAbs = items.Select(i => Math.Abs(i.Cash - i.Cost)).DefaultIfEmpty(1m).Max();

            foreach (var item in items)
            {
                var net = item.Cash - item.Cost;
                var label = _months.ElementAtOrDefault(item.Month - 1) ?? item.Month.ToString();
                var point = new DashboardChartPoint(Capitalize(label), net, FormatCurrency(net))
                {
                    Height = NormalizeHeight(net, maxAbs),
                    IsNegative = net < 0
                };
                CashFlow.Add(point);
            }
        }

        private void BuildTopLists(IEnumerable<DatabaseService.DashboardTopAmount> clients, IEnumerable<DatabaseService.DashboardTopAmount> products, IEnumerable<DatabaseService.DashboardTopAmount> sellers)
        {
            TopClients.Clear();
            TopProducts.Clear();
            TopSellers.Clear();

            AddTopItems(TopClients, clients);
            AddTopItems(TopProducts, products);
            AddTopItems(TopSellers, sellers);
        }

        private void AddTopItems(ObservableCollection<DashboardTopItem> target, IEnumerable<DatabaseService.DashboardTopAmount> source)
        {
            var list = source?.ToList() ?? new List<DatabaseService.DashboardTopAmount>();
            var max = list.Count == 0 ? 1 : list.Max(i => i.Total);

            foreach (var item in list)
            {
                target.Add(new DashboardTopItem(item.Name, FormatCurrency(item.Total), ToBarWidth(item.Total, max)));
            }
        }

        private static int ToBarWidth(decimal value, decimal max)
        {
            if (max <= 0) return 0;
            var ratio = (double)(value / max);
            var width = ratio * 60;
            return (int)Math.Round(Math.Clamp(width, 8, 60), MidpointRounding.AwayFromZero);
        }

        private static double NormalizeHeight(decimal value, decimal maxAbs)
        {
            if (maxAbs <= 0) return 0;
            if (value == 0) return 0;

            var ratio = (double)(Math.Abs(value) / maxAbs);
            ratio = Math.Clamp(ratio, 0, 1);

            const double minHeight = 16; // keep tiny bars visible when there is a non-zero value
            const double maxHeight = 120;

            return minHeight + ratio * (maxHeight - minHeight);
        }

        internal static string FormatCurrency(decimal value)
        {
            var formatted = value.ToString("N2", CurrencyCulture);
            return formatted + "€";
        }

        private static string FormatDelta(decimal current, decimal previous)
        {
            if (previous == 0) return string.Empty;
            var delta = current - previous;
            var pct = delta / Math.Abs(previous);
            var arrow = delta >= 0 ? "▲" : "▼";
            return $"{arrow} {pct:P1}";
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AnalyticsMetricCard
    {
        public AnalyticsMetricCard(string title, string value, string detail, bool isPositive)
        {
            Title = title;
            Value = value;
            Detail = detail;
            IsPositive = isPositive;
        }

        public string Title { get; }
        public string Value { get; }
        public string Detail { get; }
        public bool IsPositive { get; }
    }

    public class DashboardChartPoint
    {
        public DashboardChartPoint(string label, decimal value, string? displayOverride = null, decimal? secondaryValue = null, string? secondaryDisplay = null)
        {
            Label = label;
            Value = value;
            Display = displayOverride ?? AnalyticsDashboardViewModel.FormatCurrency(value);
            SecondaryValue = secondaryValue;
            SecondaryDisplay = secondaryValue.HasValue
                ? secondaryDisplay ?? AnalyticsDashboardViewModel.FormatCurrency(secondaryValue.Value)
                : string.Empty;
        }

        public string Label { get; }
        public decimal Value { get; }
        public string Display { get; }
        public decimal? SecondaryValue { get; }
        public string SecondaryDisplay { get; }
        public double Height { get; set; }
        public double SecondaryHeight { get; set; }
        public bool HasSecondary => SecondaryValue.HasValue;
        public bool IsNegative { get; set; }
        public double PrevHeight { get; set; }
        public decimal PrevValue { get; set; }
    }

    public class MonthOption
    {
        public MonthOption(int index, string label)
        {
            Index = index;
            Label = label;
        }

        public int Index { get; }
        public string Label { get; }
    }

    public class DashboardTopItem
    {
        public DashboardTopItem(string name, string value, int percent)
        {
            Name = name;
            Value = value;
            Percent = percent;
        }

        public string Name { get; }
        public string Value { get; }
        public int Percent { get; }
    }
}
