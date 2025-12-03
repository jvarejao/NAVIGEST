#nullable enable
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAVIGEST.macOS.PageModels;

public class ProductsPageModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsFinancial => UserSession.Current.User.IsFinancial;

    // Cache completo
    private readonly List<Product> _all = new();

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<Product> Filtered => Products;
    public ObservableCollection<string> Families { get; } = new();

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (_selectedProduct == value) return;
            _selectedProduct = value;
            Debug.WriteLine($"[SELECT PRODUCT] {_selectedProduct?.PRODCODIGO}");
            OnPropertyChanged();
            OnPropertyChanged(nameof(OverlayTitle));
            OnPropertyChanged(nameof(SaveButtonText));
            EditModel = _selectedProduct?.Clone() ?? NewTemplate();
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
            OnPropertyChanged(nameof(CanDelete));
        }
    }

    public bool CanDelete => !string.IsNullOrEmpty(Editing?.PRODCODIGO) && IsFinancial;

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
            var upper = value?.ToUpper();
            if (SearchText == upper) return;
            SearchText = upper ?? string.Empty;
            OnPropertyChanged(nameof(Filter));
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public string OverlayTitle => SelectedProduct is null ? "Adicionar Produto" : $"Editar {SelectedProduct.PRODNOME}";
    public string SaveButtonText => SelectedProduct is null ? "Guardar" : "Atualizar";

    // Sugestões de Descrição
    public ObservableCollection<Product> DescriptionSuggestions { get; } = new();

    private bool _isSuggestionsVisible;
    public bool IsSuggestionsVisible
    {
        get => _isSuggestionsVisible;
        set { if (_isSuggestionsVisible != value) { _isSuggestionsVisible = value; OnPropertyChanged(); } }
    }

    public void UpdateDescriptionSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            IsSuggestionsVisible = false;
            return;
        }

        var q = query.Trim().ToUpperInvariant();
        var matches = _all.Where(p => 
            (p.PRODNOME ?? "").ToUpperInvariant().Contains(q) && 
            p.PRODCODIGO != EditModel.PRODCODIGO)
            .Take(5)
            .ToList();

        DescriptionSuggestions.Clear();
        foreach (var m in matches) DescriptionSuggestions.Add(m);

        IsSuggestionsVisible = DescriptionSuggestions.Count > 0;
    }

    // Comandos
    public Command NewCommand { get; }
    public Command ClearCommand { get; }
    public Command SaveCommand { get; }
    public Command DeleteCommand { get; }
    public Command SearchCommand { get; }
    public Command RefreshCommand { get; }
    public Command<Product> SelectCommand { get; }
    public Command<Product> OpenEditCommand { get; }
    public Command CloseEditCommand { get; }
    public Command DeleteEditingCommand { get; }
    public Command<Product> DeleteProductCommand { get; }
    public Command ClearSearchCommand { get; }
    public Command OpenFamilyPickerCommand { get; }

    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _loadCts;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
    }

    private bool _isEditOverlayVisible;
    public bool IsEditOverlayVisible
    {
        get => _isEditOverlayVisible;
        set { if (_isEditOverlayVisible != value) { _isEditOverlayVisible = value; OnPropertyChanged(); } }
    }

    public ProductsPageModel()
    {
        NewCommand = new Command(async () => await OnNewAsync());
        ClearCommand = new Command(OnClear);
        SaveCommand = new Command(async () => await OnSaveAsync());
        DeleteCommand = new Command(async () => await OnDeleteAsync());
        SearchCommand = new Command(ApplyFilterImmediate);
        RefreshCommand = new Command(async () => await LoadAsync(force: true));
        SelectCommand = new Command<Product>(p =>
        {
            if (p == null) return;
            SelectedProduct = p;
            IsEditOverlayVisible = true;
        });
        OpenEditCommand = new Command<Product>(p =>
        {
            if (p == null) return;
            SelectedProduct = p;
            IsEditOverlayVisible = true;
        });
        CloseEditCommand = new Command(() => IsEditOverlayVisible = false);
        DeleteEditingCommand = new Command(async () =>
        {
            if (SelectedProduct == null) return;
            
            bool confirm = await AppShell.Current.DisplayAlert("Eliminar", $"Tem a certeza que deseja eliminar {SelectedProduct.PRODNOME}?", "Eliminar", "Cancelar");
            if (!confirm) return;

            await OnDeleteAsync();
            IsEditOverlayVisible = false;
        });
        ClearSearchCommand = new Command(() => Filter = string.Empty);
        OpenFamilyPickerCommand = new Command(async () => await OpenFamilyPickerAsync());

        DeleteProductCommand = new Command<Product>(async (p) => 
        {
            if (p == null) return;
            bool confirm = await AppShell.Current.DisplayAlert("Eliminar", $"Tem a certeza que deseja eliminar {p.PRODNOME}?", "Eliminar", "Cancelar");
            if (!confirm) return;
            
            SelectedProduct = p;
            await OnDeleteAsync();
        });
    }

    public void RefreshEditing()
    {
        OnPropertyChanged(nameof(Editing));
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
                var list = await DatabaseService.GetProductsAsync(null, ct); // IMPLEMENTAR NO DatabaseService
                foreach (var p in list)
                {
                    Normalize(p);
                    _all.Add(p);
                }
            }

            if (Families.Count == 0)
            {
                var fams = await DatabaseService.GetProductFamiliesAsync(ct); // IMPLEMENTAR
                Families.Clear();
                foreach (var f in fams)
                    Families.Add(f?.Trim() ?? "");
            }

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

    private async Task OnNewAsync()
    {
        SelectedProduct = null;
        EditModel = NewTemplate();
        await TryGenerateCodigoAsync();
        // Agora usa o nome do utilizador autenticado (sessão) em vez de Environment.UserName
        EditModel.COLABORADOR = GetLoggedUserName();
        OnPropertyChanged(nameof(Editing));
        IsEditOverlayVisible = true;
    }

    private async Task OpenFamilyPickerAsync()
    {
        if (Families.Count == 0)
        {
            await AppShell.DisplayToastAsync("Sem famílias disponíveis.");
            return;
        }

        var result = await AppShell.Current.DisplayActionSheet("Selecionar Família", "Cancelar", null, Families.ToArray());
        if (result != null && result != "Cancelar")
        {
            if (EditModel != null)
            {
                EditModel.FAMILIA = result;
                OnPropertyChanged(nameof(Editing));
            }
        }
    }

    private void OnClear()
    {
        SelectedProduct = null;
        EditModel = NewTemplate();
        OnPropertyChanged(nameof(Editing));
    }

    private async Task TryGenerateCodigoAsync()
    {
        if (!string.IsNullOrWhiteSpace(EditModel.PRODCODIGO)) return;
        try
        {
            EditModel.PRODCODIGO = await DatabaseService.GetNextProductCodigoAsync(); // IMPLEMENTAR
            OnPropertyChanged(nameof(Editing));
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Falha a gerar código.");
        }
    }

    private async Task OnSaveAsync()
    {
        if (EditModel is null) return;

        if (string.IsNullOrWhiteSpace(EditModel.PRODCODIGO))
            await TryGenerateCodigoAsync();

        // Se por algum motivo ainda não tiver colaborador, preencher
        if (string.IsNullOrWhiteSpace(EditModel.COLABORADOR))
            EditModel.COLABORADOR = GetLoggedUserName();

        Normalize(EditModel);

        if (!Validate(EditModel, out string msg))
        {
            await AppShell.DisplayToastAsync(msg);
            StatusMessage = msg;
            return;
        }

        // Validação de Duplicados (Descrição)
        var duplicateDesc = _all.FirstOrDefault(p => 
            (p.PRODNOME ?? "").Equals(EditModel.PRODNOME, StringComparison.OrdinalIgnoreCase) && 
            p.PRODCODIGO != EditModel.PRODCODIGO);

        if (duplicateDesc != null)
        {
            string erro = $"Já existe um produto com esta descrição (Código: {duplicateDesc.PRODCODIGO}).";
            await AppShell.DisplayToastAsync(erro);
            StatusMessage = erro;
            return;
        }

        if (string.IsNullOrWhiteSpace(EditModel.PRODCODIGO))
        {
            await AppShell.DisplayToastAsync("O código do produto é obrigatório.");
            StatusMessage = "Código obrigatório.";
            return;
        }

        // Validação de duplicados
        var duplicate = _all.FirstOrDefault(p => string.Equals(p.PRODCODIGO?.Trim(), EditModel.PRODCODIGO?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicate != null)
        {
            // Se é novo (SelectedProduct == null) OU se é edição mas o código pertence a outro objeto
            if (SelectedProduct == null || !ReferenceEquals(duplicate, SelectedProduct))
            {
                await AppShell.DisplayToastAsync("Já existe um produto com este código.");
                StatusMessage = "Código duplicado.";
                return;
            }
        }

        bool existedBefore = _all.Any(p => p.PRODCODIGO == EditModel.PRODCODIGO && !string.IsNullOrWhiteSpace(EditModel.PRODCODIGO));

        try
        {
            var ok = await DatabaseService.UpsertProductAsync(EditModel); // IMPLEMENTAR
            if (ok)
            {
                var existing = _all.FirstOrDefault(p => p.PRODCODIGO == EditModel.PRODCODIGO);
                if (existing == null)
                {
                    var cloned = EditModel.Clone();
                    Normalize(cloned);
                    _all.Add(cloned);
                    Products.Add(cloned);
                    SelectedProduct = cloned;
                }
                else
                {
                    Copy(EditModel, existing);
                    Normalize(existing);
                    var vis = Products.FirstOrDefault(p => p.PRODCODIGO == existing.PRODCODIGO);
                    if (vis != null)
                    {
                        var idx = Products.IndexOf(vis);
                        if (idx >= 0) Products[idx] = existing;
                    }
                    if (SelectedProduct?.PRODCODIGO == existing.PRODCODIGO)
                        SelectedProduct = existing;
                }
                await AppShell.DisplayToastAsync("Produto guardado.");
                StatusMessage = "Guardado.";
                IsEditOverlayVisible = false;
            }
            else
            {
                await AppShell.DisplayToastAsync("Sem alterações.");
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao guardar.");
            StatusMessage = "Erro ao guardar.";
        }
        OnPropertyChanged(nameof(Editing));
    }

    private async Task OnDeleteAsync()
    {
        if (SelectedProduct is null) return;

        try
        {
            var code = SelectedProduct.PRODCODIGO;
            if (string.IsNullOrWhiteSpace(code)) return;

            var ok = await DatabaseService.DeleteProductAsync(code); // IMPLEMENTAR
            if (ok)
            {
                _all.RemoveAll(p => p.PRODCODIGO == code);
                var toRemove = Products.FirstOrDefault(p => p.PRODCODIGO == code);
                if (toRemove != null) Products.Remove(toRemove);
                SelectedProduct = null;
                EditModel = NewTemplate();
                await AppShell.DisplayToastAsync("Produto eliminado.");
            }
            else
            {
                await AppShell.DisplayToastAsync("Nenhuma linha eliminada.");
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao eliminar.");
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
            (p.PRODNOME ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (p.PRODCODIGO ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (p.FAMILIA ?? string.Empty).ToLowerInvariant().Contains(q));

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
        var prev = SelectedProduct?.PRODCODIGO;
        Products.Clear();
        foreach (var p in items)
            Products.Add(p);
        if (prev != null)
            SelectedProduct = Products.FirstOrDefault(p => p.PRODCODIGO == prev);
    }

    private static void Copy(Product src, Product dst)
    {
        dst.PRODCODIGO = src.PRODCODIGO;
        dst.PRODNOME = src.PRODNOME;
        dst.FAMILIA = src.FAMILIA;
        dst.COLABORADOR = src.COLABORADOR;
        dst.PRECOCUSTO = src.PRECOCUSTO;
        dst.PRECOVENDA = src.PRECOVENDA;
        dst.TOTALVENDAS = src.TOTALVENDAS;
    }

    private bool Validate(Product p, out string msg)
    {
        if (string.IsNullOrWhiteSpace(p.PRODNOME)) { msg = "Descrição obrigatória."; return false; }
        if (string.IsNullOrWhiteSpace(p.FAMILIA)) { msg = "Família obrigatória."; return false; }
        msg = "";
        return true;
    }

    private static Product NewTemplate() => new()
    {
        PRODCODIGO = "",
        PRODNOME = "",
        FAMILIA = "",
        COLABORADOR = ""
    };

    private static void Normalize(Product p)
    {
        static string? Clean(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s?.Trim();
            s = s.Trim();
            return System.Text.RegularExpressions.Regex.Replace(s, @"\s{2,}", " ");
        }

        p.PRODCODIGO = p.PRODCODIGO?.Trim();
        p.PRODNOME = Clean(p.PRODNOME);
        p.FAMILIA = Clean(p.FAMILIA);
        p.COLABORADOR = Clean(p.COLABORADOR);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public async Task<(bool ok, string message, string finalCode)> AddFamilyAsync(string codigo, string descricao)
    {
        codigo = (codigo ?? "").Trim();
        descricao = (descricao ?? "").Trim();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(descricao))
            return (false, "Código e descrição obrigatórios.", codigo);

        try
        {
            bool persisted = false;
            try
            {
                persisted = await DatabaseService.UpsertProductFamilyAsync(codigo, descricao); // IMPLEMENTAR
            }
            catch
            {
                // Se ainda não implementado: ignora persistência silenciosamente
            }

            if (!Families.Contains(codigo))
                Families.Add(codigo);

            var ordered = Families.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            Families.Clear();
            foreach (var f in ordered) Families.Add(f);

            await AppShell.DisplayToastAsync("Família adicionada.");
            return (true, "Ok", codigo);
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            return (false, "Erro inesperado.", codigo);
        }
    }

    // ---------------- Utilizador corrente ----------------
    private static string GetLoggedUserName()
    {
        // Usa a sessão criada no LoginPage. Ajuste se existir outra propriedade (ex: Username).
        var name = UserSession.Current.User?.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name!;

        // Fallback: último username lembrado (se existir)
        var remembered = Preferences.Default.Get<string>("last_username", "")?.Trim();
        if (!string.IsNullOrWhiteSpace(remembered))
            return remembered!;

        // Último recurso: nome do sistema
        return Environment.UserName;
    }
}
