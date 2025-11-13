#nullable enable
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NAVIGEST.Android.PageModels;

public class ProductsPageModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly List<Product> _all = new();

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<ProductGroup> GroupedProducts { get; } = new();
    public ObservableCollection<ProductFamilyOption> Families { get; } = new();
    public ObservableCollection<ProductFamilyOption> Familias => Families;

    private ProductFamilyOption? _selectedFamily;
    public ProductFamilyOption? SelectedFamily
    {
        get => _selectedFamily;
        set
        {
            if (ReferenceEquals(_selectedFamily, value))
                return;

            _selectedFamily = value;
            if (Editing is not null)
                Editing.FamiliaSelecionada = value?.Codigo;

            OnPropertyChanged();
            OnPropertyChanged(nameof(Editing));
        }
    }

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (_selectedProduct == value) return;
            _selectedProduct = value;
            Debug.WriteLine($"[SELECT PRODUCT] {_selectedProduct?.Codigo}");
            OnPropertyChanged();
            EditModel = _selectedProduct?.Clone() ?? NewTemplate();
            SyncSelectedFamilyFromEditing();
        }
    }

    private Product _editModel = NewTemplate();
    public Product EditModel
    {
        get => _editModel;
        set
        {
            if (ReferenceEquals(_editModel, value)) return;
            _editModel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Editing));
            SyncSelectedFamilyFromEditing();
        }
    }

    public Product? Editing
    {
        get => EditModel;
        set
        {
            EditModel = value ?? NewTemplate();
            OnPropertyChanged(nameof(Editing));
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Filter));
            DebounceSearch();
        }
    }

    public string Filter
    {
        get => SearchText;
        set
        {
            if (SearchText == value) return;
            SearchText = value ?? string.Empty;
            OnPropertyChanged(nameof(Filter));
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public Command NewCommand { get; }
    public Command ClearCommand { get; }
    public Command SaveCommand { get; }
    public Command<Product?> DeleteCommand { get; }
    public Command SearchCommand { get; }
    public Command RefreshCommand { get; }
    public Command<Product> SelectCommand { get; }

    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _loadCts;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing == value) return;
            _isRefreshing = value;
            OnPropertyChanged();
        }
    }

    public ProductsPageModel()
    {
        NewCommand = new Command(async () => await OnNewAsync());
        ClearCommand = new Command(OnClear);
        SaveCommand = new Command(async () => await SaveAsync());
        DeleteCommand = new Command<Product?>(async product => await DeleteAsync(product));
        SearchCommand = new Command(ApplyFilterImmediate);
        RefreshCommand = new Command(async () =>
        {
            if (IsRefreshing) return;
            IsRefreshing = true;
            try
            {
                await LoadAsync(force: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        });
        SelectCommand = new Command<Product>(p =>
        {
            if (p == null) return;
            SelectedProduct = p;
        });
    }

    public async Task LoadAsync(bool force = false)
    {
        if (IsBusy) return;
        IsBusy = true;
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            if (force || _all.Count == 0)
            {
                _all.Clear();
                var list = await DatabaseService.GetProductsAsync(null, ct);
                foreach (var p in list)
                {
                    Normalize(p);
                    _all.Add(p);
                }
            }

            var fams = await DatabaseService.GetProductFamiliesAsync(ct);
            Families.Clear();
            foreach (var f in fams
                         .GroupBy(f => f.Codigo, StringComparer.OrdinalIgnoreCase)
                         .Select(g => g.First())
                         .OrderBy(f => f.Codigo, StringComparer.OrdinalIgnoreCase))
            {
                Families.Add(f);
            }

            SyncSelectedFamilyFromEditing();

            Repopulate(_all);
            if (Products.Count > 0 && SelectedProduct is null)
                SelectedProduct = Products.First();
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            StatusMessage = "Erro ao carregar produtos.";
            await AppShell.DisplayToastAsync("Erro ao carregar produtos.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ReloadFamiliesAsync(string? preferredCodigo = null)
    {
        try
        {
            var families = await DatabaseService.GetProductFamiliesAsync();
            var ordered = families
                .GroupBy(f => f.Codigo, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(f => f.Codigo, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Families.Clear();
            foreach (var family in ordered)
                Families.Add(family);

            var targetCodigo = preferredCodigo;
            if (string.IsNullOrWhiteSpace(targetCodigo))
            {
                targetCodigo = SelectedFamily?.Codigo ?? Editing?.FamiliaSelecionada;
            }

            if (!string.IsNullOrWhiteSpace(targetCodigo))
            {
                var match = Families.FirstOrDefault(f => string.Equals(f.Codigo, targetCodigo, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    SelectedFamily = match;
                    return;
                }
            }

            SyncSelectedFamilyFromEditing();
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao atualizar famílias.");
        }
    }

    private async Task OnNewAsync()
    {
        SelectedProduct = null;
        EditModel = NewTemplate();
        await TryGenerateCodigoAsync();
        EditModel.COLABORADOR = GetLoggedUserName();
        EditModel.Valor = FormatValor(EditModel.Valor);
        OnPropertyChanged(nameof(Editing));
        SelectedFamily = null;
    }

    private void OnClear()
    {
        SelectedProduct = null;
        EditModel = NewTemplate();
        EditModel.Valor = FormatValor(EditModel.Valor);
        OnPropertyChanged(nameof(Editing));
        SelectedFamily = null;
    }

    private async Task TryGenerateCodigoAsync()
    {
        if (!string.IsNullOrWhiteSpace(EditModel.PRODCODIGO)) return;
        try
        {
            EditModel.PRODCODIGO = await DatabaseService.GetNextProductCodigoAsync();
            OnPropertyChanged(nameof(Editing));
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Falha a gerar código.");
        }
    }

    public async Task<bool> SaveAsync()
    {
        if (EditModel is null)
            return false;

        if (string.IsNullOrWhiteSpace(EditModel.PRODCODIGO))
            await TryGenerateCodigoAsync();

        if (string.IsNullOrWhiteSpace(EditModel.COLABORADOR))
            EditModel.COLABORADOR = GetLoggedUserName();

        EditModel.Valor = FormatValor(EditModel.Valor);
        Normalize(EditModel);

        if (!Validate(EditModel, out string msg))
        {
            await AppShell.DisplayToastAsync(msg);
            StatusMessage = msg;
            return false;
        }

        try
        {
            var succeeded = await DatabaseService.UpsertProductAsync(EditModel);
            if (!succeeded)
            {
                await AppShell.DisplayToastAsync("Sem alterações.");
                return false;
            }

            var existing = _all.FirstOrDefault(p => string.Equals(p.PRODCODIGO, EditModel.PRODCODIGO, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var cloned = EditModel.Clone();
                Normalize(cloned);
                _all.Add(cloned);
            }
            else
            {
                Copy(EditModel, existing);
                Normalize(existing);
            }

            Repopulate(_all);

            SelectedProduct = Products.FirstOrDefault(p => string.Equals(p.PRODCODIGO, EditModel.PRODCODIGO, StringComparison.OrdinalIgnoreCase));
            await AppShell.DisplayToastAsync("Produto guardado.");
            StatusMessage = "Guardado.";
            return true;
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao guardar.");
            StatusMessage = "Erro ao guardar.";
            return false;
        }
        finally
        {
            OnPropertyChanged(nameof(Editing));
        }
    }

    public async Task<bool> DeleteAsync(Product? product = null)
    {
        var target = product ?? SelectedProduct;
        if (target is null)
            return false;

        try
        {
            var code = target.PRODCODIGO;
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var succeeded = await DatabaseService.DeleteProductAsync(code);
            if (!succeeded)
            {
                await AppShell.DisplayToastAsync("Nenhuma linha eliminada.");
                return false;
            }

            _all.RemoveAll(p => string.Equals(p.PRODCODIGO, code, StringComparison.OrdinalIgnoreCase));
            Repopulate(_all);

            SelectedProduct = Products.FirstOrDefault();
            EditModel = NewTemplate();
            await AppShell.DisplayToastAsync("Produto eliminado.");
            StatusMessage = "Eliminado.";
            return true;
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao eliminar.");
            return false;
        }
    }

    private void ApplyFilterImmediate()
    {
        var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q))
        {
            Repopulate(_all);
            return;
        }

        var filtered = _all.Where(p =>
            (p.Descricao ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (p.Codigo ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (p.Familia ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (p.Colaborador ?? string.Empty).ToLowerInvariant().Contains(q));

        Repopulate(filtered);
    }

    private void DebounceSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;
                MainThread.BeginInvokeOnMainThread(ApplyFilterImmediate);
            }
            catch { }
        }, token);
    }

    private void Repopulate(IEnumerable<Product> items)
    {
        var snapshot = items
            .Where(p => p is not null)
            .OrderBy(p => NormalizeFamilyTitle(p.Familia), StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Descricao, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Codigo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var previousCode = SelectedProduct?.PRODCODIGO;

        Products.Clear();
        GroupedProducts.Clear();

        foreach (var group in snapshot
                     .GroupBy(p => NormalizeFamilyTitle(p.Familia), StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var orderedItems = group
                .OrderBy(p => p.Descricao, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Codigo, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var item in orderedItems)
                Products.Add(item);

            GroupedProducts.Add(new ProductGroup(group.Key, orderedItems));
        }

        if (Products.Count == 0)
        {
            SelectedProduct = null;
            return;
        }

        var nextSelection = previousCode is not null
            ? Products.FirstOrDefault(p => string.Equals(p.PRODCODIGO, previousCode, StringComparison.OrdinalIgnoreCase))
            : Products.FirstOrDefault();

        if (nextSelection is not null)
            SelectedProduct = nextSelection;
    }

    private static bool TryParseValor(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var sanitized = raw.Replace("€", string.Empty, StringComparison.OrdinalIgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\s+", string.Empty);
        if (sanitized.Length == 0)
            return false;

        if (sanitized.Count(c => c == ',') == 0 && sanitized.Count(c => c == '.') > 1)
            sanitized = sanitized.Replace(".", string.Empty, StringComparison.Ordinal);

        if (!sanitized.Contains(',', StringComparison.Ordinal) && sanitized.Contains('.', StringComparison.Ordinal))
            sanitized = sanitized.Replace('.', ',');

        var normalized = sanitized.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatValor(string? raw)
    {
        if (!TryParseValor(raw, out var dec))
            return "0,00€";

        var inteiro = Math.Abs(dec).ToString("0", CultureInfo.InvariantCulture);
        inteiro = Regex.Replace(inteiro, @"\B(?=(\d{3})+(?!\d))", " ");
        var frac = (Math.Abs(dec) - Math.Truncate(Math.Abs(dec))).ToString("F2", CultureInfo.InvariantCulture).Split('.')[1];
        var sinal = dec < 0 ? "-" : string.Empty;
        return $"{sinal}{inteiro},{frac}€";
    }

    private void SyncSelectedFamilyFromEditing()
    {
        var code = Editing?.Familia?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            SelectedFamily = null;
            return;
        }

        var match = Families.FirstOrDefault(f => string.Equals(f.Codigo, code, StringComparison.OrdinalIgnoreCase));
        SelectedFamily = match;
    }

    private static string NormalizeFamilyTitle(string? familia)
    {
        var trimmed = familia?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "Sem família" : trimmed;
    }

    public void FormatValorOnBlur()
    {
        if (EditModel == null) return;
        try
        {
            var before = EditModel.Valor;
            EditModel.Valor = FormatValor(EditModel.Valor);
            if (before != EditModel.Valor)
                OnPropertyChanged(nameof(Editing));
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private static void Copy(Product src, Product dst)
    {
        dst.PRODCODIGO = src.PRODCODIGO;
        dst.PRODNOME = src.PRODNOME;
        dst.FAMILIA = src.FAMILIA;
        dst.COLABORADOR = src.COLABORADOR;
        dst.VALOR = src.VALOR;
    }

    private bool Validate(Product p, out string msg)
    {
        if (string.IsNullOrWhiteSpace(p.Descricao)) { msg = "Descrição obrigatória."; return false; }
        if (string.IsNullOrWhiteSpace(p.Familia)) { msg = "Família obrigatória."; return false; }
        if (!TryParseValor(p.Valor, out _))
        {
            msg = "Valor inválido.";
            return false;
        }

        msg = string.Empty;
        return true;
    }

    private static Product NewTemplate() => new()
    {
        PRODCODIGO = string.Empty,
        PRODNOME = string.Empty,
        FAMILIA = string.Empty,
        COLABORADOR = string.Empty,
        VALOR = "0,00€"
    };

    private static void Normalize(Product p)
    {
        static string? Clean(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s?.Trim();
            s = s.Trim();
            return Regex.Replace(s, @"\s{2,}", " ");
        }

        p.PRODCODIGO = p.PRODCODIGO?.Trim();
        p.PRODNOME = Clean(p.PRODNOME);
        p.FAMILIA = Clean(p.FAMILIA);
        p.COLABORADOR = Clean(p.COLABORADOR);
        p.VALOR = FormatValor(p.VALOR);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public async Task<(bool ok, string message, ProductFamilyOption? family)> AddFamilyAsync(string codigo, string descricao)
    {
        codigo = (codigo ?? string.Empty).Trim().ToUpperInvariant();
        descricao = (descricao ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(descricao))
            return (false, "Código e descrição obrigatórios.", null);

        try
        {
            bool persisted = false;
            try
            {
                persisted = await DatabaseService.UpsertProductFamilyAsync(codigo, descricao);
            }
            catch
            {
                // Ignora erros de persistência para fluxos ainda não disponíveis.
            }

            var option = new ProductFamilyOption(codigo, descricao);

            var existing = Families.FirstOrDefault(f => string.Equals(f.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
                Families.Remove(existing);

            Families.Add(option);

            var ordered = Families
                .OrderBy(f => f.Codigo, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Families.Clear();
            foreach (var f in ordered)
                Families.Add(f);

            SelectedFamily = option;

            await AppShell.DisplayToastAsync(persisted ? "Família adicionada." : "Família disponível localmente.");
            return (true, "Ok", option);
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            return (false, "Erro inesperado.", null);
        }
    }

    private static string GetLoggedUserName()
    {
        var name = UserSession.Current.User?.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name!;

        var remembered = Preferences.Default.Get<string>("last_username", "")?.Trim();
        if (!string.IsNullOrWhiteSpace(remembered))
            return remembered!;

        return Environment.UserName;
    }

}
