using Microsoft.Maui.Controls;
using NAVIGEST.iOS.PageModels;
using NAVIGEST.iOS.Models;
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

    // ---------- Popup Adicionar Fam�lia ----------
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
            FamilyPicker.SelectedItem = finalCode;
            await GlobalToast.ShowAsync("Fam�lia adicionada.", ToastTipo.Sucesso, 1600);
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

    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        var product = (sender as Button)?.CommandParameter as Product
                      ?? (sender as Element)?.BindingContext as Product;
        if (product == null)
        {
            await DisplayAlert("Erro", "Produto não identificado.", "OK");
            return;
        }

        if (BindingContext is ProductsPageModel vm)
        {
            if (vm.SelectCommand?.CanExecute(product) == true)
            {
                vm.SelectCommand.Execute(product);
            }

            // Scroll para o form
            if (vm.SelectedProduct != product)
                vm.SelectedProduct = product;
        }
    }

    private async void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        var product = (sender as Button)?.CommandParameter as Product
                      ?? (sender as Element)?.BindingContext as Product;
        if (product == null)
        {
            await DisplayAlert("Erro", "Produto não identificado.", "OK");
            return;
        }

        var confirm = await DisplayAlert("Eliminar Produto",
            $"Tem a certeza que deseja eliminar '{product.PRODNOME}'?",
            "Eliminar", "Cancelar");

        if (!confirm) return;

        await DisplayAlert("Eliminar", "Funcionalidade de delete ainda não disponível nesta página.", "OK");
    }

    // Tap na célula – abre edição
    private void OnProductCellTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is Grid grid && grid.BindingContext is Product product)
            {
                if (BindingContext is ProductsPageModel vm &&
                    vm.SelectCommand?.CanExecute(product) == true)
                {
                    vm.SelectCommand.Execute(product);
                }

                if (BindingContext is ProductsPageModel vm2)
                    vm2.SelectedProduct = product;
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        // Placeholder para scroll events se necessário
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