using Microsoft.Maui.Controls;
using NAVIGEST.iOS.PageModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using NAVIGEST.iOS; // GlobalToast / GlobalErro

namespace NAVIGEST.iOS.Pages;

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

    private void OnProductSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (BindingContext is ProductsPageModel vm && e.CurrentSelection?.FirstOrDefault() is object item)
            {
                if (vm.SelectCommand?.CanExecute(item) == true)
                    vm.SelectCommand.Execute(item);
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    // Overlay picker (mobile/tablet)
    private void OnOpenProductPicker(object sender, EventArgs e)
    {
        ProductPickerOverlay.IsVisible = true;
    }

    private void OnCloseProductPicker(object sender, EventArgs e)
    {
        ProductPickerOverlay.IsVisible = false;
    }

    private void OnProductPickerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (BindingContext is ProductsPageModel vm && e.CurrentSelection?.FirstOrDefault() is object item)
            {
                if (vm.SelectCommand?.CanExecute(item) == true)
                    vm.SelectCommand.Execute(item);
                ProductPickerOverlay.IsVisible = false;
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
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
            ShowAddFamilyError("Código e descrição obrigatórios.");
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
            FamilyPicker.SelectedItem = finalCode;
            await GlobalToast.ShowAsync("Família adicionada.", ToastTipo.Sucesso, 1600);
        }
        catch (Exception ex)
        {
            ShowAddFamilyError("Erro ao guardar família.");
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            await GlobalToast.ShowAsync("Erro ao guardar família.", ToastTipo.Erro, 2500);
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

#if WINDOWS
    // Código Windows específico (exemplo: animações, navegação, layouts)
#endif
#if ANDROID
    // Código Android específico (exemplo: animações, navegação, layouts)
#endif
#if IOS
    // Código iOS específico (exemplo: animações, navegação, layouts)
#endif
}