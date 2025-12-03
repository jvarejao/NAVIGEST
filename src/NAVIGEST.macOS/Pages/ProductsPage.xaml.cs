using Microsoft.Maui.Controls;
using NAVIGEST.macOS.PageModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using NAVIGEST.macOS; // GlobalToast / GlobalErro

namespace NAVIGEST.macOS.Pages;

public partial class ProductsPage : ContentPage
{
    private bool _initialLoaded;

    public ProductsPage(ProductsPageModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await EnsureLoadAsync();
    }

    public ProductsPage() : this(ResolveViewModel()) { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await EnsureLoadAsync();
    }

    public async Task InitializeForHostAsync() => await EnsureLoadAsync();

    private async Task EnsureLoadAsync()
    {
        if (BindingContext is not ProductsPageModel vm) return;
        if (_initialLoaded && vm.Products.Count > 0) return;
        if (vm.IsBusy) return;

        try
        {
            await vm.LoadAsync(force: false);
            _initialLoaded = true;
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            await GlobalToast.ShowAsync("Falha ao carregar produtos.", ToastTipo.Erro, 2500);
        }
    }

    private static ProductsPageModel ResolveViewModel()
    {
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<ProductsPageModel>();
            return vm ?? new ProductsPageModel();
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            return new ProductsPageModel();
        }
    }



    private void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Models.Product product && BindingContext is ProductsPageModel vm)
        {
            vm.OpenEditCommand.Execute(product);
        }
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Models.Product product && BindingContext is ProductsPageModel vm)
        {
            vm.DeleteProductCommand.Execute(product);
        }
    }

    // ---------- List Picker Logic ----------
    private List<string> _allFamilies = new();

    private void OnFamilyPickerTapped(object sender, EventArgs e)
    {
        if (BindingContext is not ProductsPageModel vm) return;
        
        _allFamilies = vm.Families.ToList();
        ListPickerCollectionView.ItemsSource = _allFamilies;
        ListPickerTitleLabel.Text = "Selecionar Família";
        ListPickerSearchEntry.Text = string.Empty;
        ListPickerOverlay.IsVisible = true;
    }

    private void OnCloseListPickerClicked(object sender, EventArgs e)
    {
        ListPickerOverlay.IsVisible = false;
    }

    private void OnListPickerItemTapped(object sender, EventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is string selected && BindingContext is ProductsPageModel vm)
        {
            if (vm.EditModel != null)
            {
                vm.EditModel.FAMILIA = selected;
                vm.RefreshEditing();
            }
            ListPickerOverlay.IsVisible = false;
        }
    }

    private void OnListPickerSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var term = e.NewTextValue?.Trim() ?? "";
        ListPickerClearButton.IsVisible = !string.IsNullOrEmpty(term);

        if (string.IsNullOrWhiteSpace(term))
        {
            ListPickerCollectionView.ItemsSource = _allFamilies;
        }
        else
        {
            ListPickerCollectionView.ItemsSource = _allFamilies
                .Where(f => f.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void OnListPickerClearSearchClicked(object sender, EventArgs e)
    {
        ListPickerSearchEntry.Text = string.Empty;
    }

    // ---------- Popup Adicionar Família ----------
    private void OnAddFamilyClicked(object sender, EventArgs e)
    {
        AddFamilyErrorLabel.IsVisible = false;
        NewFamilyCodeEntry.Text = string.Empty;
        NewFamilyNameEntry.Text = string.Empty;
        AddFamilyOverlay.IsVisible = true;
        NewFamilyCodeEntry.Focus();
    }

    private void OnCancelAddFamily(object sender, EventArgs e)
    {
        AddFamilyOverlay.IsVisible = false;
    }

    private async void OnSaveAddFamily(object sender, EventArgs e)
    {
        if (BindingContext is not ProductsPageModel vm) return;

        var code = NewFamilyCodeEntry.Text?.Trim() ?? "";
        var name = NewFamilyNameEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            ShowAddFamilyError("C�digo e descri��o obrigat�rios.");
            return;
        }

        BtnSaveAddFamily.IsEnabled = false;
        try
        {
            var (ok, msg, finalCode) = await vm.AddFamilyAsync(code, name);
            if (!ok)
            {
                ShowAddFamilyError(msg);
                return;
            }

            AddFamilyOverlay.IsVisible = false;
            if (vm.EditModel != null)
            {
                vm.EditModel.FAMILIA = finalCode;
                vm.RefreshEditing();
            }
            await GlobalToast.ShowAsync("Família adicionada.", ToastTipo.Sucesso, 1600);
        }
        catch (Exception ex)
        {
            ShowAddFamilyError("Erro ao guardar fam�lia.");
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            await GlobalToast.ShowAsync("Erro ao guardar fam�lia.", ToastTipo.Erro, 2500);
        }
        finally
        {
            BtnSaveAddFamily.IsEnabled = true;
        }
    }

    private void ShowAddFamilyError(string msg)
    {
        AddFamilyErrorLabel.Text = msg;
        AddFamilyErrorLabel.IsVisible = true;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrEmpty(entry.Text))
        {
            var upper = entry.Text.ToUpper();
            if (entry.Text != upper)
            {
                entry.Text = upper;
                entry.CursorPosition = upper.Length;
            }
        }
    }

    private void OnDescriptionTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrEmpty(entry.Text))
        {
            var upper = entry.Text.ToUpper();
            if (entry.Text != upper)
            {
                entry.Text = upper;
                entry.CursorPosition = upper.Length;
            }
        }
    }

    private void OnPriceUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            string text = entry.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            // Remove currency symbol and whitespace if present
            string cleanText = text.Replace("€", "").Trim();

            // Normaliza: substitui ponto por vírgula para suportar input com ponto
            string normalized = cleanText.Replace(".", ",");

            // Tenta fazer parse usando cultura PT (vírgula como decimal)
            if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-PT"), out decimal value))
            {
                // Formata de volta para o padrão PT com símbolo de moeda (ex: 1 245,50 €)
                entry.Text = value.ToString("C2", new System.Globalization.CultureInfo("pt-PT"));
            }
        }
    }

#if WINDOWS
    // C�digo Windows espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if ANDROID
    // C�digo Android espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if IOS
    // C�digo iOS espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
}