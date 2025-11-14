using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using NAVIGEST.Android; // GlobalToast / GlobalErro
using NAVIGEST.Android.Popups;
using CommunityToolkit.Maui.Views;

namespace NAVIGEST.Android.Pages;

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

    // -------- Swipe Actions --------
    private async void OnEditSwipeInvoked(object sender, EventArgs e)
    {
        try
        {
            if (sender is SwipeItemView siv && siv.BindingContext is object item)
            {
                if (BindingContext is ProductsPageModel vm && vm.SelectCommand?.CanExecute(item) == true)
                {
                    vm.SelectCommand.Execute(item);
                    await Task.Delay(100);
                    ShowFormView();
                }
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private void OnDeleteSwipeInvoked(object sender, EventArgs e)
    {
        try
        {
            if (sender is SwipeItemView siv && siv.BindingContext is object item)
            {
                if (BindingContext is ProductsPageModel vm && vm.DeleteCommand?.CanExecute(item) == true)
                    vm.DeleteCommand.Execute(item);
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    // -------- Cell Tap --------
    private void OnProductCellTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Grid grid && grid.BindingContext is object item)
            {
                if (BindingContext is ProductsPageModel vm && vm.SelectCommand?.CanExecute(item) == true)
                {
                    vm.SelectCommand.Execute(item);
                    // Delay para dar tempo ao ViewModel de atualizar os bindings
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(100);
                        ShowFormView();
                        UpdateDeleteButtonVisibility();
                    });
                }
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    // -------- Add Product FAB --------
    private void OnAddProductTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (BindingContext is ProductsPageModel vm && vm.NewCommand?.CanExecute(null) == true)
            {
                vm.NewCommand.Execute(null);
                ShowFormView();
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    // -------- Form Controls --------
    private void ShowFormView()
    {
        if (ListViewContainer is not null && FormViewContainer is not null)
        {
            ListViewContainer.IsVisible = false;
            FormViewContainer.IsVisible = true;
        }
        UpdateDeleteButtonVisibility();
    }

    private void UpdateDeleteButtonVisibility()
    {
        if (BindingContext is ProductsPageModel vm && DeleteFormButton is not null)
        {
            // Mostrar botão de delete apenas se há um produto selecionado
            DeleteFormButton.IsVisible = !string.IsNullOrWhiteSpace(vm.Editing?.Codigo);
            
            // Atualizar título do form
            if (FormTitle is not null)
            {
                FormTitle.Text = string.IsNullOrWhiteSpace(vm.Editing?.Codigo) ? "Novo Produto" : "Editar Produto";
            }
        }
    }

    private void OnSaveProductTapped(object sender, EventArgs e)
    {
        try
        {
            if (BindingContext is ProductsPageModel vm && vm.SaveCommand?.CanExecute(null) == true)
            {
                vm.SaveCommand.Execute(null);
                HideFormView();
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private void OnCancelEditTapped(object sender, EventArgs e)
    {
        HideFormView();
    }

    private void HideFormView()
    {
        if (ListViewContainer is not null && FormViewContainer is not null)
        {
            FormViewContainer.IsVisible = false;
            ListViewContainer.IsVisible = true;
        }
    }

    private void OnFormBackgroundTapped(object sender, TappedEventArgs e)
    {
        HideFormView();
    }

    private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        // Handle scroll if needed
    }

    private void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
    {
        // Search completed
    }

    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        // Filtering is done through binding in ViewModel
    }

    // -------- Familia Management --------
    private void OnAddFamiliaTapped(object sender, EventArgs e)
    {
        try
        {
            if (BindingContext is ProductsPageModel vm)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var popup = new ProductFamiliesListPopup();
                        var result = await Application.Current.MainPage.ShowPopupAsync(popup);
                        
                        if (result is ProductFamilyListResult familyResult && familyResult.SelectedFamily is not null)
                        {
                            vm.SelectedFamily = familyResult.SelectedFamily;
                            if (familyResult.RefreshRequested)
                            {
                                await vm.ReloadFamiliesAsync(familyResult.SelectedFamily?.Codigo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GlobalErro.TratarErro(ex, mostrarAlerta: false);
                        await AppShell.DisplayToastAsync($"Erro: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private void OnPrecoCustoFocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry)
            {
                var len = entry.Text?.Length ?? 0;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try { entry.CursorPosition = 0; entry.SelectionLength = len; } catch { }
                });
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private void OnPrecoCustoUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (BindingContext is ProductsPageModel vm)
                vm.FormatValorOnBlur();
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private void OnDeleteFromFormTapped(object sender, EventArgs e)
    {
        try
        {
            if (BindingContext is ProductsPageModel vm && vm.DeleteCommand?.CanExecute(null) == true)
            {
                vm.DeleteCommand.Execute(null);
                HideFormView();
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }


#if WINDOWS
    // Cdigo Windows especfico (exemplo: animaes, navegao, layouts)
#endif
#if ANDROID
    // Cdigo Android especfico (exemplo: animaes, navegao, layouts)
#endif
#if IOS
    // Cdigo iOS especfico (exemplo: animaes, navegao, layouts)
#endif
}