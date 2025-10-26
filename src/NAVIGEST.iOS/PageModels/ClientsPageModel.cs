#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;
using System;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using NAVIGEST.Shared.Helpers;

namespace NAVIGEST.iOS.PageModels;

public class ClientsPageModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");
    private const int TelefoneBodyMaxLength = 20;

    public sealed class DialCodeItem
    {
        private DialCodeItem(string shortCode, string country, string prefix, string flagEmoji)
        {
            ShortCode = (shortCode ?? string.Empty).ToUpperInvariant();
            Country = country ?? string.Empty;
            FlagEmoji = flagEmoji ?? string.Empty;
            NormalizedPrefix = NormalizePrefix(prefix);
        }

        public string ShortCode { get; }
        public string Country { get; }
        public string FlagEmoji { get; }
        public string NormalizedPrefix { get; }

        public string PickerDisplay
        {
            get
            {
                var prefix = NormalizedPrefix;
                var code = ShortCode;

                string baseDisplay = string.IsNullOrWhiteSpace(code)
                    ? (string.IsNullOrWhiteSpace(prefix) ? Country : prefix)
                    : string.IsNullOrWhiteSpace(prefix) ? code : $"{code} {prefix}";

                if (string.IsNullOrWhiteSpace(baseDisplay))
                    baseDisplay = Country;

                return string.IsNullOrEmpty(FlagEmoji) ? baseDisplay : $"{FlagEmoji} {baseDisplay}";
            }
        }

        public static DialCodeItem Create(string iso2, string country, string prefix) =>
            new(iso2, country, prefix, BuildFlagEmoji(iso2));

        public static DialCodeItem CreateNoPrefix() => new(string.Empty, "Sem indicativo", string.Empty, "🌐");

        public static DialCodeItem CreateCustom(string prefix) =>
            new(string.Empty, $"Indicativo {NormalizePrefix(prefix)}", prefix, string.Empty);

        public static string NormalizePrefix(string? prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return string.Empty;

            var trimmed = prefix.Trim();
            if (trimmed.StartsWith("00"))
                trimmed = trimmed[2..];

            trimmed = trimmed.TrimStart('+');
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            return digits.Length == 0 ? string.Empty : "+" + digits;
        }

        private static string BuildFlagEmoji(string iso2)
        {
            if (string.IsNullOrWhiteSpace(iso2) || iso2.Length != 2)
                return string.Empty;

            var upper = iso2.ToUpperInvariant();
            const int baseCodePoint = 0x1F1E6;
            return string.Concat(
                char.ConvertFromUtf32(baseCodePoint + (upper[0] - 'A')),
                char.ConvertFromUtf32(baseCodePoint + (upper[1] - 'A')));
        }
    }

    private readonly List<Cliente> _all = new();
    public ObservableCollection<Cliente> Clientes { get; } = new();
    public ObservableCollection<Cliente> Filtered => Clientes;
    public ObservableCollection<string> Vendedores { get; } = new();
    public ObservableCollection<DialCodeItem> DialCodes { get; } = new();

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
            _codigoPreview = false;
            EditModel = _selectedCliente?.Clone() ?? NewClienteTemplate();
            if (EditModel != null)
                EditModel.VALORCREDITO = FormatValorCredito(EditModel.VALORCREDITO);
            SyncPhoneFieldsFromModel();
            OnPropertyChanged(nameof(Editing));
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
    public Command<Cliente> DeleteCommand { get; }
    public Command SearchCommand { get; }
    public Command RefreshCommand { get; }
    public Command<Cliente> PastasCommand { get; }
    public Command<Cliente> SelectCommand { get; }

    public string PastasButtonText =>
        (EditModel?.PastasSincronizadas ?? false) ? "Abrir Pastas" : "Criar Pastas";

    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _loadCts;

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

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
    }

    // Flag para saber se o código mostrado é apenas pré-visualização
    private bool _codigoPreview;

    public ClientsPageModel()
    {
        NewCommand = new Command(async () => await OnNewAsync());
        ClearCommand = new Command(OnClear);
        SaveCommand = new Command(async () => await OnSaveAsync());
        DeleteCommand = new Command<Cliente>(async (c) => 
        { 
            if (c != null) 
            { 
                SelectedCliente = c;
                await OnDeleteAsync();
            }
        });
        SearchCommand = new Command(ApplyFilterImmediate);
        RefreshCommand = new Command(async () => await LoadAsync(force: true));
        PastasCommand = new Command<Cliente>(async (c) => 
        { 
            if (c != null) 
            { 
                SelectedCliente = c;
                await OnPastasAsync();
            }
        });
        SelectCommand = new Command<Cliente>(c => { if (c != null) SelectedCliente = c; });

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

    private async Task OnNewAsync()
    {
        SelectedCliente = null;
        EditModel = NewClienteTemplate();
        _codigoPreview = false;
        SetPhoneFieldsWithoutSync(GetDefaultDialCode(), string.Empty);

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
        SetPhoneFieldsWithoutSync(GetDefaultDialCode(), string.Empty);
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

    public async Task OnSaveAsync()
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
                    Clientes.Add(cloned);
                    SelectedCliente = cloned;
                }
                else
                {
                    Copy(EditModel, existing);
                    Normalize(existing);
                    existing.VALORCREDITO = FormatValorCredito(existing.VALORCREDITO);
                    var vis = Clientes.FirstOrDefault(c => c.CLICODIGO == existing.CLICODIGO);
                    if (vis != null)
                    {
                        var idx = Clientes.IndexOf(vis);
                        if (idx >= 0) Clientes[idx] = existing;
                    }
                    if (SelectedCliente?.CLICODIGO == existing.CLICODIGO)
                        SelectedCliente = existing;
                }
                await AppShell.DisplayToastAsync("Cliente guardado.", ToastTipo.Sucesso, 2500);
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

    public async Task OnDeleteAsync()
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
                var toRemove = Clientes.FirstOrDefault(c => c.CLICODIGO == code);
                if (toRemove != null) Clientes.Remove(toRemove);
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

    public async Task OnPastasAsync() => await OnCreateFoldersAsync();

    private async Task OnCreateFoldersAsync()
    {
        if (EditModel is null) return;

        if (string.IsNullOrWhiteSpace(EditModel.CLICODIGO))
        {
            await OnSaveAsync();
            if (string.IsNullOrWhiteSpace(EditModel.CLICODIGO))
            {
                await AppShell.DisplayToastAsync("Gerar/guardar cliente antes de continuar.");
                return;
            }
        }

#if ANDROID
        await AppShell.DisplayToastAsync("Criação de pastas só disponível em Desktop.");
        return;
#endif

        if (EditModel.PastasSincronizadas)
        {
            await AppShell.DisplayToastAsync("Pastas já criadas.");
            return;
        }

        try
        {
            var (ok, _) = await DatabaseService.EnsureClientePastasAsync(EditModel);
            if (ok)
            {
                SetPastasSincronizadas(true);
                await AppShell.DisplayToastAsync("Pastas criadas com sucesso!");
            }
            else
            {
                await AppShell.DisplayToastAsync("Falha ao criar pastas.");
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao criar pastas.");
        }
        OnPropertyChanged(nameof(Editing));
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
        Clientes.Clear();
        foreach (var c in items)
            Clientes.Add(c);
        if (prev != null)
            SelectedCliente = Clientes.FirstOrDefault(c => c.CLICODIGO == prev);
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
        PastasSincronizadas = false
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
        c.VENDEDOR = Clean(c.VENDEDOR);
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
        if (EditModel.PastasSincronizadas == valor) return;
        EditModel.PastasSincronizadas = valor;
        var cache = _all.FirstOrDefault(c => c.CLICODIGO == EditModel.CLICODIGO);
        if (cache != null) cache.PastasSincronizadas = valor;
        OnPropertyChanged(nameof(Editing));
        OnPropertyChanged(nameof(PastasButtonText));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

