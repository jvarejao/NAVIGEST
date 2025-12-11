#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;
using System;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

using NAVIGEST.macOS.Helpers;
using NAVIGEST.Shared.Resources.Strings;

namespace NAVIGEST.macOS.PageModels;

public class ClientsPageModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");
    private const int TelefoneBodyMaxLength = 20;

    private readonly List<Cliente> _all = new();
    public ObservableCollection<Cliente> Clientes { get; } = new();
    public ObservableCollection<DialCodeItem> DialCodes { get; } = new();
    private ObservableCollection<Cliente> _filtered = new();
    public ObservableCollection<Cliente> Filtered
    {
        get => _filtered;
        set
        {
            if (_filtered != value)
            {
                _filtered = value;
                OnPropertyChanged();
            }
        }
    }
    
    // Sugestões de nomes duplicados
    public ObservableCollection<Cliente> NameSuggestions { get; } = new();
    
    private bool _isSuggestionsVisible;
    public bool IsSuggestionsVisible
    {
        get => _isSuggestionsVisible;
        set
        {
            if (_isSuggestionsVisible != value)
            {
                _isSuggestionsVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public void UpdateNameSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            NameSuggestions.Clear();
            IsSuggestionsVisible = false;
            return;
        }

        var normalizedQuery = query.Trim().ToUpperInvariant();
        
        // Ignora o próprio cliente que estamos a editar
        var currentCode = EditModel?.CLICODIGO;

        var matches = _all
            .Where(c => c.CLINOME != null && 
                        c.CLINOME.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) &&
                        c.CLICODIGO != currentCode)
            .Take(5)
            .ToList();

        NameSuggestions.Clear();
        foreach (var m in matches)
            NameSuggestions.Add(m);

        IsSuggestionsVisible = NameSuggestions.Count > 0;
    }

    public ObservableCollection<string> Vendedores { get; } = new();

    private bool _suppressPhoneSync;
    private DialCodeItem? _selectedDialCodeItem;
    public DialCodeItem? SelectedDialCodeItem
    {
        get => _selectedDialCodeItem;
        set
        {
            if (Equals(_selectedDialCodeItem, value)) return;
            _selectedDialCodeItem = value;
            OnPropertyChanged();
            UpdateEditingTelefone();
            ApplyExternalFlagForDialCode(value);
            Debug.WriteLine($"[ClientsPageModel] SelectedDialCodeItem updated to: {value?.Country} ({value?.NormalizedPrefix})");
        }
    }

    private string _phoneBody = string.Empty;
    public string PhoneBody
    {
        get => _phoneBody;
        set
        {
            var prefix = SelectedDialCodeItem?.NormalizedPrefix ?? string.Empty;
            var sanitized = StripKnownPrefix(value ?? string.Empty, prefix);
            var normalized = NormalizePhoneBody(sanitized);
            if (_phoneBody == normalized) return;
            _phoneBody = normalized;
            OnPropertyChanged();
            UpdateEditingTelefone();
        }
    }

    private Cliente? _selectedCliente;
    public Cliente? SelectedCliente
    {
        get => _selectedCliente;
        set
        {
            if (_selectedCliente == value) return;
            _selectedCliente = value;
            Debug.WriteLine($"[SELECT] {_selectedCliente?.CLICODIGO}");
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(SaveButtonText));
            _codigoPreview = false;
            EditModel = _selectedCliente?.Clone() ?? NewClienteTemplate();
            if (EditModel != null)
                EditModel.VALORCREDITO = FormatValorCredito(EditModel.VALORCREDITO);
            SyncPhoneFieldsFromModel();
            OnPropertyChanged(nameof(Editing));
        }
    }
    private bool _isEditOverlayVisible;
    public bool IsEditOverlayVisible
    {
        get => _isEditOverlayVisible;
        set
        {
            if (_isEditOverlayVisible != value)
            {
                _isEditOverlayVisible = value;
                OnPropertyChanged();
            }
        }
    }


    private Cliente _editModel = NewClienteTemplate();
    public Cliente EditModel
    {
        get => _editModel;
        set
        {
            if (ReferenceEquals(_editModel, value)) return;
            _editModel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Editing));
            OnPropertyChanged(nameof(PastasButtonText));
        }
    }

    public Cliente? Editing
    {
        get => EditModel;
        set
        {
            EditModel = value ?? NewClienteTemplate();
            OnPropertyChanged(nameof(Editing));
        }
    }

    public bool IsNew => SelectedCliente == null;
    public bool CanDelete => !IsNew && IsFinancial;

    public string SaveButtonText => IsNew ? AppResources.Common_Save : AppResources.Common_Update;

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

    private string? _selectedVendedor;
    public string? SelectedVendedor
    {
        get => _selectedVendedor;
        set
        {
            if (_selectedVendedor == value) return;
            _selectedVendedor = value;
            if (EditModel != null)
                EditModel.VENDEDOR = _selectedVendedor;
            OnPropertyChanged();
        }
    }

    // Comandos
    public Command NewCommand { get; }
    public Command ClearCommand { get; }
    public Command SaveCommand { get; }
    public Command DeleteCommand { get; }
    public Command SearchCommand { get; }
    public Command RefreshCommand { get; }
    public Command PastasCommand { get; }
    public Command<Cliente> SelectCommand { get; }
    public Command<Cliente> OpenEditCommand { get; }
    public Command<Cliente> ServicesCommand { get; }
    public Command CloseEditCommand { get; }
    public Command DeleteEditingCommand { get; }
    public Command ClearSearchCommand { get; }


    public string PastasButtonText =>
        (EditModel?.PastasSincronizadas ?? false) ? "Abrir Pastas" : "Criar Pastas";

    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _loadCts;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
    }

    public bool IsFinancial => UserSession.Current.User.IsFinancial;

    // Flag para saber se o código mostrado é apenas pré-visualização
    private bool _codigoPreview;

    public ICommand OpenCountryPickerCommand { get; }

    public ClientsPageModel()
    {
        OpenCountryPickerCommand = new Command(async () => await OpenCountryPickerAsync());
        NewCommand = new Command(async () => await OnNewAsync());
        ClearCommand = new Command(OnClear);
        SaveCommand = new Command(async () => await OnSaveAsync());
        DeleteCommand = new Command<Cliente>(async c => 
        { 
            if (c != null) 
            {
                SelectedCliente = c;
                bool confirm = await AppShell.Current.DisplayAlert("Eliminar", $"Tem a certeza que deseja eliminar {c.CLINOME}?", "Eliminar", "Cancelar");
                if (confirm)
                {
                    await OnDeleteAsync();
                }
            } 
        });
        SearchCommand = new Command(ApplyFilterImmediate);
        RefreshCommand = new Command(async () => await LoadAsync(force: true));
        PastasCommand = new Command<Cliente>(async c => { if (c != null) SelectedCliente = c; await OnPastasAsync(); });
        SelectCommand = new Command<Cliente>(c => { if (c != null) SelectedCliente = c; });
        OpenEditCommand = new Command<Cliente>(c => 
        { 
            if (c != null) 
            {
                SelectedCliente = c;
                IsEditOverlayVisible = true;
            }
        });
        
        ServicesCommand = new Command<Cliente>(async c => 
        {
            if (c != null)
            {
                try 
                {
                    var vm = new ServicePageModel 
                    { 
                        FilterClientId = c.CLICODIGO,
                        FilterClientName = c.CLINOME
                    };
                    
                    // Navegar para a página de serviços, passando o ViewModel configurado
                    // O ServicePage irá chamar LoadAsync automaticamente
                    await AppShell.Current.Navigation.PushAsync(new Pages.ServicePage(vm));
                }
                catch (Exception ex)
                {
                    GlobalErro.TratarErro(ex);
                    await AppShell.DisplayToastAsync("Erro ao abrir serviços: " + ex.Message);
                }
            }
        });

        CloseEditCommand = new Command(() => IsEditOverlayVisible = false);
        
        DeleteEditingCommand = new Command(async () => {
            if (SelectedCliente != null) {
                bool confirm = await AppShell.Current.DisplayAlert(AppResources.Common_DeleteConfirmationTitle, string.Format(AppResources.Common_DeleteConfirmationMessageFormat, SelectedCliente.CLINOME), AppResources.Common_Delete, AppResources.Common_Cancel);
                if (confirm) {
                    await OnDeleteAsync();
                    IsEditOverlayVisible = false;
                }
            }
        });

        ClearSearchCommand = new Command(() => Filter = string.Empty);

        InitializeDialCodes();
        SetPhoneFieldsWithoutSync(GetDefaultDialCode(), string.Empty);
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
                var list = await DatabaseService.GetClientesAsync(null, ct);
                foreach (var c in list)
                {
                    Normalize(c);
                    c.VALORCREDITO = FormatValorCredito(c.VALORCREDITO);
                    _all.Add(c);
                }
            }

            if (Vendedores.Count == 0)
            {
                var vend = await DatabaseService.GetVendedoresAsync(ct);
                Vendedores.Clear();
                foreach (var v in vend)
                    Vendedores.Add(v?.Trim() ?? "");
            }

            Repopulate(_all);

            // Pré-seleciona primeiro cliente se nenhum
            if (Clientes.Count > 0 && SelectedCliente is null)
                SelectedCliente = Clientes.First();

            // Verificar pastas não sincronizadas (apenas se carregou dados)
            if (_all.Count > 0)
            {
                // Executa em background para não bloquear a UI imediata
                _ = Task.Run(async () => 
                {
                    await Task.Delay(1000); // Pequeno delay para deixar a UI estabilizar
                    MainThread.BeginInvokeOnMainThread(async () => await CheckForUnsyncedFoldersAsync());
                });
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao carregar clientes.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckForUnsyncedFoldersAsync()
    {
        // Ignora anulados e externos (se aplicável)
        var unsynced = _all.Where(c => !c.PastasSincronizadas && !c.ANULADO).ToList();
        
        if (unsynced.Count > 0)
        {
            bool sync = await AppShell.Current.DisplayAlert(AppResources.Sync_FolderTitle, 
                string.Format(AppResources.Sync_FolderMessage, unsynced.Count), 
                AppResources.Sync_Yes, AppResources.Sync_NotNow);
            
            if (sync)
            {
                IsBusy = true;
                int successCount = 0;
                int errorCount = 0;

                // Barra de progresso ou apenas toast? Vamos usar toast por enquanto.
                await AppShell.DisplayToastAsync(AppResources.Sync_Starting, ToastTipo.Info);

                foreach (var c in unsynced)
                {
                    try
                    {
                        var (ok, _) = await DatabaseService.EnsureClientePastasAsync(c);
                        if (ok)
                        {
                            // Atualiza o objeto na lista principal
                            c.PastasSincronizadas = true;
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch
                    {
                        errorCount++;
                    }
                }

                var msg = string.Format(AppResources.Sync_ReportMessage, successCount, errorCount);
                await AppShell.Current.DisplayAlert(AppResources.Sync_ReportTitle, msg, "OK");
            }
        }
    }

    private async Task OpenCountryPickerAsync()
    {
        var popup = new NAVIGEST.macOS.Popups.CountryCodePickerPopup(DialCodes, item =>
        {
            SelectedDialCodeItem = item;
        });
        await Shell.Current.Navigation.PushModalAsync(popup);
    }

    private async Task OnNewAsync()
    {
        IsEditOverlayVisible = true;
        SelectedCliente = null;
        EditModel = NewClienteTemplate();
        _codigoPreview = false;

        try
        {
            // “Peek” (não consome)
            var preview = await DatabaseService.PeekNextClienteCodigoAsync();
            EditModel.CLICODIGO = preview;
            _codigoPreview = true;
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            EditModel.CLICODIGO = string.Empty;
        }

        OnPropertyChanged(nameof(Editing));
    }

    private void OnClear()
    {
        SelectedCliente = null;
        EditModel = NewClienteTemplate();
        _codigoPreview = false;
        OnPropertyChanged(nameof(Editing));
    }

    private async Task EnsureCodigoDefinitivoAsync()
    {
        // Se já existe e não é preview, nada a fazer
        if (string.IsNullOrWhiteSpace(EditModel.CLICODIGO) || _codigoPreview)
        {
            try
            {
                var real = await DatabaseService.GetNextClienteCodigoAsync();
                var old = EditModel.CLICODIGO;
                EditModel.CLICODIGO = real;
                _codigoPreview = false;

                if (!string.IsNullOrWhiteSpace(old) && old != real)
                    await AppShell.DisplayToastAsync($"Código ajustado para {real}.", ToastTipo.Aviso, 2500);
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
                await AppShell.DisplayToastAsync("Falha a obter código definitivo.", ToastTipo.Erro, 2500);
            }
        }
    }

    private async Task OnSaveAsync()
    {
        if (EditModel is null) return;

        // Gera definitivo se estava em pré-visualização
        await EnsureCodigoDefinitivoAsync();
        if (string.IsNullOrWhiteSpace(EditModel.CLICODIGO))
        {
            await AppShell.DisplayToastAsync("Não foi possível gerar código.", ToastTipo.Erro, 2500);
            return;
        }

        Normalize(EditModel);

        if (!Validate(EditModel, out string msg))
        {
            await AppShell.DisplayToastAsync(msg, ToastTipo.Erro, 2500);
            return;
        }

        // Validação de duplicados (Nome)
        var duplicate = _all.FirstOrDefault(c => 
            c.CLINOME.Equals(EditModel.CLINOME, StringComparison.OrdinalIgnoreCase) && 
            c.CLICODIGO != EditModel.CLICODIGO);

        if (duplicate != null)
        {
            await AppShell.DisplayToastAsync($"Já existe um cliente com este nome: {duplicate.CLICODIGO}", ToastTipo.Erro, 4000);
            return;
        }

        UpdateEditingTelefone(force: true);

        bool existedBefore = _all.Any(c => c.CLICODIGO == EditModel.CLICODIGO);

        try
        {
            Normalize(EditModel);

            var ok = await DatabaseService.UpsertClienteAsync(EditModel);
            if (ok)
            {
                var existing = _all.FirstOrDefault(c => c.CLICODIGO == EditModel.CLICODIGO);
                if (existing == null)
                {
                    var cloned = EditModel.Clone();
                    Normalize(cloned);
                    cloned.VALORCREDITO = FormatValorCredito(cloned.VALORCREDITO);
                    _all.Add(cloned);
                    Filtered.Add(cloned);
                    SelectedCliente = cloned;
                }
                else
                {
                    Copy(EditModel, existing);
                    Normalize(existing);
                    existing.VALORCREDITO = FormatValorCredito(existing.VALORCREDITO);
                    var vis = Filtered.FirstOrDefault(c => c.CLICODIGO == existing.CLICODIGO);
                    if (vis != null)
                    {
                        var idx = Filtered.IndexOf(vis);
                        if (idx >= 0) Filtered[idx] = existing;
                    }
                    if (SelectedCliente?.CLICODIGO == existing.CLICODIGO)
                        SelectedCliente = existing;
                }
                await AppShell.DisplayToastAsync("Cliente guardado.", ToastTipo.Sucesso, 2500);
                IsEditOverlayVisible = false;
            }
            else
            {
                await AppShell.DisplayToastAsync("Sem alterações.", ToastTipo.Aviso, 2500);
            }

#if !(ANDROID || IOS)
            if (!EditModel.PastasSincronizadas)
            {
                try
                {
                    var (created, _) = await DatabaseService.EnsureClientePastasAsync(EditModel);
                    if (created)
                    {
                        EditModel.PastasSincronizadas = true;
                        var cache = _all.FirstOrDefault(c => c.CLICODIGO == EditModel.CLICODIGO);
                        if (cache != null) cache.PastasSincronizadas = true;
                        OnPropertyChanged(nameof(PastasButtonText));
                        OnPropertyChanged(nameof(Editing));
                    }
                }
                catch (Exception exAuto)
                {
                    Debug.WriteLine("[Pastas Auto] " + exAuto);
                }
            }
#endif
            if (!existedBefore)
                await TryAutoCreateFoldersAsync();
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao guardar.");
        }
        OnPropertyChanged(nameof(Editing));
    }

    private async Task TryAutoCreateFoldersAsync()
    {
#if ANDROID || IOS
        return;
#else
        try
        {
            if (EditModel is null) return;
            var (ok, _) = await DatabaseService.EnsureClientePastasAsync(EditModel);
            if (ok)
            {
                EditModel.PastasSincronizadas = true;
                var cache = _all.FirstOrDefault(c => c.CLICODIGO == EditModel.CLICODIGO);
                if (cache != null) cache.PastasSincronizadas = true;
                await AppShell.DisplayToastAsync("Pastas do cliente criadas.");
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Falha a criar pastas.");
        }
#endif
    }

    private async Task OnDeleteAsync()
    {
        if (SelectedCliente is null) return;

        try
        {
            var code = SelectedCliente.CLICODIGO;
            if (string.IsNullOrWhiteSpace(code)) return;

            var ok = await DatabaseService.DeleteClienteAsync(code);
            if (ok)
            {
                _all.RemoveAll(c => c.CLICODIGO == code);
                var toRemove = Filtered.FirstOrDefault(c => c.CLICODIGO == code);
                if (toRemove != null) Filtered.Remove(toRemove);
                SelectedCliente = null;
                EditModel = NewClienteTemplate();
                _codigoPreview = false;
                await AppShell.DisplayToastAsync("Cliente eliminado.");
            }
            else
            {
                await AppShell.DisplayToastAsync("Nenhuma linha eliminada.");
            }
        }
        catch (InvalidOperationException depEx)
        {
            await AppShell.DisplayToastAsync(depEx.Message);
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao eliminar.");
        }
    }

    private async Task OnPastasAsync()
    {
        if (EditModel is null) return;
        var c = EditModel;

        // Verificar ligação ao servidor antes de qualquer ação
        bool serverAvailable = false;
        try
        {
            var setup = await DatabaseService.GetSetupAsync();
            if (setup != null && !string.IsNullOrWhiteSpace(setup.CaminhoServidor))
            {
                var rootPath = FolderService.ResolvePath(setup.CaminhoServidor);
                if (Directory.Exists(rootPath))
                {
                    serverAvailable = true;
                }
            }
        }
        catch { }

        if (c.PastasSincronizadas)
        {
            // Se já tem visto verde
            if (!serverAvailable)
            {
                await AppShell.Current.DisplayAlert("Aviso", "Não é possível estabelecer ligação com o servidor de ficheiros.", "OK");
            }
            
            // Tenta abrir de qualquer forma (pode desencadear montagem ou falhar)
            await FolderService.OpenClientFolderAsync(c);
        }
        else
        {
            // Se tem X vermelho, pergunta se quer criar
            bool criar = await AppShell.Current.DisplayAlert(
                "Sincronizar Pastas", 
                $"As pastas para o cliente {c.CLINOME} não estão criadas/sincronizadas.\nDeseja criar a estrutura de pastas agora?", 
                "Criar", "Cancelar");

            if (criar)
            {
                if (!serverAvailable)
                {
                    await AppShell.Current.DisplayAlert("Aviso", "Não é possível estabelecer ligação com o servidor de ficheiros.", "OK");
                }

                IsBusy = true;
                try
                {
                    var (ok, msg) = await DatabaseService.EnsureClientePastasAsync(c);
                    if (ok)
                    {
                        SetPastasSincronizadas(true);

                        if (!string.IsNullOrEmpty(msg) && msg.Contains("já existia"))
                        {
                            await AppShell.Current.DisplayAlert("Informação", msg, "OK");
                        }
                        else
                        {
                            await AppShell.DisplayToastAsync("Pastas criadas com sucesso!", ToastTipo.Sucesso);
                        }

                        // Opcional: Abrir logo a pasta após criar
                        await FolderService.OpenClientFolderAsync(c);
                    }
                    else
                    {
                        await AppShell.DisplayToastAsync($"Erro: {msg}", ToastTipo.Erro, 4000);
                    }
                }
                catch (Exception ex)
                {
                    GlobalErro.TratarErro(ex);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    // Método antigo removido ou mantido como helper se necessário, mas OnPastasAsync agora faz tudo.
    private async Task OnCreateFoldersAsync()
    {
        // Redireciona para a nova lógica para manter compatibilidade se for chamado de outro lado
        await OnPastasAsync();
    }

    private void ApplyFilterImmediate()
    {
        var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q))
        {
            Repopulate(_all);
            return;
        }

        var filtered = _all.Where(c =>
            (c.CLINOME ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (c.CLICODIGO ?? string.Empty).ToLowerInvariant().Contains(q) ||
            (c.EMAIL ?? string.Empty).ToLowerInvariant().Contains(q));

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

    private void Repopulate(IEnumerable<Cliente> items)
    {
        var prev = SelectedCliente?.CLICODIGO;
        
        // Create new collection to avoid rapid UI updates (crash fix)
        Filtered = new ObservableCollection<Cliente>(items);

        // Sync Clientes (optional, but keeps it as a mirror if needed elsewhere)
        // But since Filtered is now the source of truth for the UI, we might not need to sync Clientes perfectly
        // However, let's keep Clientes in sync just in case other logic depends on it, 
        // but do it safely or just rely on Filtered.
        // Actually, Clientes was just a backing store for Filtered before. 
        // We can clear it and add items if we really want, but Filtered is what matters for the View.
        
        if (prev != null)
            SelectedCliente = Filtered.FirstOrDefault(c => c.CLICODIGO == prev);
    }

    private static void Copy(Cliente src, Cliente dst)
    {
        dst.CLICODIGO = src.CLICODIGO;
        dst.CLINOME = src.CLINOME;
        dst.TELEFONE = src.TELEFONE;
        dst.INDICATIVO = src.INDICATIVO;
        dst.EMAIL = src.EMAIL;
        dst.EXTERNO = src.EXTERNO;
        dst.ANULADO = src.ANULADO;
        dst.VENDEDOR = src.VENDEDOR;
        dst.VALORCREDITO = src.VALORCREDITO;
        dst.PastasSincronizadas = src.PastasSincronizadas;
        dst.ServicesCount = src.ServicesCount;
    }

    private bool Validate(Cliente c, out string msg)
    {
        if (string.IsNullOrWhiteSpace(c.CLINOME)) { msg = "Nome obrigatório."; return false; }
        if (string.IsNullOrWhiteSpace(c.TELEFONE)) { msg = "Telefone obrigatório."; return false; }
        if (string.IsNullOrWhiteSpace(c.EMAIL)) { msg = "Email obrigatório."; return false; }
        if (string.IsNullOrWhiteSpace(c.VENDEDOR)) { msg = "Vendedor obrigatório."; return false; }

        c.TELEFONE = NormalizePhoneBody(c.TELEFONE ?? string.Empty);
        if (string.IsNullOrWhiteSpace(c.TELEFONE)) { msg = "Telefone obrigatório."; return false; }
        if (c.TELEFONE.Length > TelefoneBodyMaxLength)
        {
            msg = $"Telefone demasiado longo (máx {TelefoneBodyMaxLength} caracteres sem indicativo).";
            return false;
        }

        c.VALORCREDITO = FormatValorCredito(c.VALORCREDITO);
        msg = string.Empty;
        return true;
    }

    private static Cliente NewClienteTemplate() => new()
    {
        CLICODIGO = string.Empty,
        CLINOME = string.Empty,
        TELEFONE = string.Empty,
        INDICATIVO = string.Empty,
        EMAIL = string.Empty,
        EXTERNO = false,
        ANULADO = false,
        VENDEDOR = string.Empty,
        VALORCREDITO = "0,00€",
        PastasSincronizadas = false,
        ServicesCount = 0
    };

    private static void Normalize(Cliente c)
    {
        static string? Clean(string? s, bool compress = true)
        {
            if (string.IsNullOrWhiteSpace(s)) return s?.Trim();
            s = s.Trim();
            return !compress ? s : System.Text.RegularExpressions.Regex.Replace(s, @"\s{2,}", " ");
        }
        c.CLICODIGO = Clean(c.CLICODIGO, compress: false);
        c.CLINOME = Clean(c.CLINOME);

        var telefoneClean = Clean(c.TELEFONE, compress: false);
        c.TELEFONE = NormalizePhoneBody(telefoneClean ?? string.Empty);
        c.INDICATIVO = DialCodeItem.NormalizePrefix(c.INDICATIVO);
        c.EMAIL = Clean(c.EMAIL, compress: false);

        var vendedor = Clean(c.VENDEDOR);
        c.VENDEDOR = string.IsNullOrWhiteSpace(vendedor)
            ? string.Empty
            : vendedor.ToUpperInvariant();

        c.VALORCREDITO = Clean(c.VALORCREDITO, compress: false);
    }

    private void InitializeDialCodes()
    {
        if (DialCodes.Count > 0) return;

        DialCodes.Add(DialCodeItem.CreateNoPrefix());
        foreach (var dial in CountryDialCodeData.All
                     .OrderBy(d => d.Iso2, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            DialCodes.Add(DialCodeItem.Create(dial.Iso2, dial.Name, dial.Prefix));
        }
    }

    private DialCodeItem GetDefaultDialCode()
        => DialCodes.FirstOrDefault(dc => dc.ShortCode == "PT") ?? DialCodes.First();

    private void SyncPhoneFieldsFromModel()
    {
        if (EditModel is null)
        {
            SetPhoneFieldsWithoutSync(GetDefaultDialCode(), string.Empty);
            return;
        }

        var telefoneRaw = EditModel.TELEFONE?.Trim() ?? string.Empty;
        var prefixFromModel = DialCodeItem.NormalizePrefix(EditModel.INDICATIVO);

        string prefix = prefixFromModel;
        string body = telefoneRaw;

        if (!string.IsNullOrEmpty(prefix))
        {
            if (!string.IsNullOrEmpty(body))
            {
                if (body.StartsWith(prefix, StringComparison.Ordinal))
                {
                    body = body[prefix.Length..].TrimStart();
                }
                else if (prefix.StartsWith("+", StringComparison.Ordinal))
                {
                    var alt = "00" + prefix[1..];
                    if (body.StartsWith(alt, StringComparison.Ordinal))
                        body = body[alt.Length..].TrimStart();
                }
            }
        }
        else
        {
            var split = SplitPhoneNumber(telefoneRaw);
            prefix = split.Prefix;
            body = split.Body;
        }

        var dialItem = EnsureDialCodeForPrefix(prefix);
        SetPhoneFieldsWithoutSync(dialItem, body);

        var normalized = dialItem.NormalizedPrefix;
        if (!string.Equals(EditModel.INDICATIVO ?? string.Empty, normalized, StringComparison.Ordinal))
            EditModel.INDICATIVO = normalized;
    }

    private DialCodeItem EnsureDialCodeForPrefix(string prefix)
    {
        var normalized = DialCodeItem.NormalizePrefix(prefix);
        if (string.IsNullOrEmpty(normalized))
            return DialCodes.First();

        var existing = DialCodes.FirstOrDefault(dc => dc.NormalizedPrefix == normalized);
        if (existing != null)
            return existing;

        var custom = DialCodeItem.CreateCustom(normalized);
        DialCodes.Add(custom);
        return custom;
    }

    private void SetPhoneFieldsWithoutSync(DialCodeItem dial, string body)
    {
        _suppressPhoneSync = true;
        _selectedDialCodeItem = dial;
        _phoneBody = NormalizePhoneBody(StripKnownPrefix(body ?? string.Empty, dial.NormalizedPrefix));
        OnPropertyChanged(nameof(SelectedDialCodeItem));
        OnPropertyChanged(nameof(PhoneBody));
        _suppressPhoneSync = false;
        ApplyExternalFlagForDialCode(dial);
    }

    private void UpdateEditingTelefone(bool force = false)
    {
        if (_suppressPhoneSync || EditModel is null) return;

        var rawPrefix = SelectedDialCodeItem?.NormalizedPrefix ?? string.Empty;
        var normalizedPrefix = DialCodeItem.NormalizePrefix(rawPrefix);
        var bodyInput = StripKnownPrefix(PhoneBody?.Trim() ?? string.Empty, normalizedPrefix);
        var normalizedBody = NormalizePhoneBody(bodyInput);

        var currentTelefone = EditModel.TELEFONE ?? string.Empty;
        var currentIndicativo = EditModel.INDICATIVO ?? string.Empty;

        bool changed = force;

        if (!string.Equals(_phoneBody, normalizedBody, StringComparison.Ordinal))
        {
            _suppressPhoneSync = true;
            _phoneBody = normalizedBody;
            OnPropertyChanged(nameof(PhoneBody));
            _suppressPhoneSync = false;
        }

        if (force || !string.Equals(currentTelefone, normalizedBody, StringComparison.Ordinal))
        {
            EditModel.TELEFONE = normalizedBody;
            changed = true;
        }

        if (!string.Equals(currentIndicativo, normalizedPrefix, StringComparison.Ordinal))
        {
            EditModel.INDICATIVO = normalizedPrefix;
            changed = true;
        }

        if (changed)
            OnPropertyChanged(nameof(Editing));

        ApplyExternalFlagForDialCode(_selectedDialCodeItem);
    }

    private void ApplyExternalFlagForDialCode(DialCodeItem? dialCode)
    {
        if (EditModel is null) return;

        var defaultDial = GetDefaultDialCode();
        var selected = dialCode ?? _selectedDialCodeItem;

        bool isDefault = true;

        if (selected != null)
        {
            bool samePrefix = !string.IsNullOrEmpty(selected.NormalizedPrefix) &&
                              string.Equals(selected.NormalizedPrefix, defaultDial.NormalizedPrefix, StringComparison.Ordinal);
            bool sameCode = !string.IsNullOrEmpty(selected.ShortCode) &&
                            string.Equals(selected.ShortCode, defaultDial.ShortCode, StringComparison.OrdinalIgnoreCase);
            bool hasPrefix = !string.IsNullOrEmpty(selected.NormalizedPrefix);

            isDefault = samePrefix || sameCode || !hasPrefix;
        }

        bool shouldBeExternal = !isDefault;
        if (EditModel.EXTERNO != shouldBeExternal)
        {
            EditModel.EXTERNO = shouldBeExternal;
            OnPropertyChanged(nameof(Editing));
        }
    }

    private static string StripKnownPrefix(string input, string normalizedPrefix)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var trimmed = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s{2,}", " ");

        if (!string.IsNullOrEmpty(normalizedPrefix))
        {
            if (trimmed.StartsWith(normalizedPrefix, StringComparison.Ordinal))
                return trimmed[normalizedPrefix.Length..].TrimStart();

            if (normalizedPrefix.StartsWith("+", StringComparison.Ordinal))
            {
                var alt = "00" + normalizedPrefix[1..];
                if (trimmed.StartsWith(alt, StringComparison.Ordinal))
                    return trimmed[alt.Length..].TrimStart();
            }
        }

        return trimmed;
    }

    private static string NormalizePhoneBody(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var trimmed = Regex.Replace(input.Trim(), "\\s{2,}", " ");
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        if (digits.Length == 9)
            trimmed = $"{digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6, 3)}";

        if (trimmed.Length > TelefoneBodyMaxLength)
            trimmed = trimmed.Substring(0, TelefoneBodyMaxLength).TrimEnd();

        return trimmed;
    }

    private static (string Prefix, string Body) SplitPhoneNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (string.Empty, string.Empty);

        var trimmed = Regex.Replace(raw.Trim(), "\\s{2,}", " ");

        if (trimmed.StartsWith("00"))
        {
            var withoutLeading = trimmed[2..];
            var match00 = Regex.Match(withoutLeading, @"^(?<code>\d{1,4})");
            if (match00.Success)
            {
                var code = "+" + match00.Groups["code"].Value;
                var rest = withoutLeading[match00.Length..].TrimStart();
                return (DialCodeItem.NormalizePrefix(code), rest);
            }
        }

        var match = Regex.Match(trimmed, @"^\+(?<code>\d{1,4})");
        if (match.Success)
        {
            var code = "+" + match.Groups["code"].Value;
            var rest = trimmed[match.Length..].TrimStart();
            return (DialCodeItem.NormalizePrefix(code), rest);
        }

        return (string.Empty, trimmed);
    }

    private static string FormatValorCredito(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "0,00€";
        var cleaned = raw.Replace("€", "").Trim().Replace(" ", "");
        if (cleaned.Count(c => c == ',') == 0 && cleaned.Count(c => c == '.') > 1)
            cleaned = cleaned.Replace(".", "");
        if (!cleaned.Contains(',') && cleaned.Contains('.'))
            cleaned = cleaned.Replace('.', ',');
        var parseCandidate = cleaned.Replace(".", "").Replace(',', '.');
        if (!decimal.TryParse(parseCandidate, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            return "0,00€";
        // Construir com espaço a cada grupo de 3 (milhares) + vírgula
        var inteiro = Math.Abs(dec).ToString("0", CultureInfo.InvariantCulture);
        inteiro = Regex.Replace(inteiro, @"\B(?=(\d{3})+(?!\d))", " ");
        var frac = (Math.Abs(dec) - Math.Truncate(Math.Abs(dec))).ToString("F2", CultureInfo.InvariantCulture).Split('.')[1];
        var sinal = dec < 0 ? "-" : string.Empty;
        return $"{sinal}{inteiro},{frac}€";
    }

    public void FormatValorCreditoOnBlur()
    {
        if (EditModel == null) return;
        try
        {
            var before = EditModel.VALORCREDITO;
            EditModel.VALORCREDITO = FormatValorCredito(EditModel.VALORCREDITO);
            if (before != EditModel.VALORCREDITO)
                OnPropertyChanged(nameof(Editing));
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private void SetPastasSincronizadas(bool valor)
    {
        if (EditModel == null) return;
        
        // Atualiza o EditModel (que é um clone)
        if (EditModel.PastasSincronizadas != valor)
        {
            EditModel.PastasSincronizadas = valor;
            OnPropertyChanged(nameof(Editing));
            OnPropertyChanged(nameof(PastasButtonText));
        }

        // Atualiza o item na lista principal (_all)
        var cache = _all.FirstOrDefault(c => c.CLICODIGO == EditModel.CLICODIGO);
        if (cache != null) 
        {
            cache.PastasSincronizadas = valor;
        }

        // Atualiza o item na lista filtrada (Filtered) que está ligada à UI
        // Isto garante que a UI atualiza mesmo que Filtered tenha instâncias diferentes (embora não devesse)
        var itemInFiltered = Filtered.FirstOrDefault(c => c.CLICODIGO == EditModel.CLICODIGO);
        if (itemInFiltered != null && itemInFiltered != cache)
        {
            itemInFiltered.PastasSincronizadas = valor;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

