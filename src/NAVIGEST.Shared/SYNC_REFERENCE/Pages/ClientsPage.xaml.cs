// SYNC REFERENCE - ClientsPage.xaml.cs
// ANDROID REFERENCE - Last update: 2025-11-09
// 
// This file contains reference code from Android implementation.
// Copy-paste for reference when implementing in other platforms.
// See: /docs/PLATFORM_CHANGES/ANDROID_CHANGES.md
// 
// DO NOT USE DIRECTLY - FOR REFERENCE ONLY

using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using NAVIGEST.Shared.Models;
using NAVIGEST.Shared.Services;

namespace NAVIGEST.Android.Pages
{
    // ============================================================================
    // HELPERS - ShowConfirmAsync Pattern
    // ============================================================================
    
    /// <summary>
    /// Helper to display a confirmation dialog safely on UI thread.
    /// 
    /// Purpose: Wrap DisplayAlert in MainThread.InvokeOnMainThreadAsync to avoid 
    /// deadlocks when called from SwipeItemView.Invoked handlers or other event handlers.
    /// 
    /// Usage:
    /// var confirm = await ShowConfirmAsync("Title", "Message", "Accept", "Cancel");
    /// if (confirm) { /* do something */ }
    /// 
    /// CRITICAL: This pattern is required in Android to avoid DisplayAlert hanging.
    /// </summary>
    private static Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var root = GetRootPage();
            if (root is null) return false;
            return await root.DisplayAlert(title, message, accept, cancel);
        });
    }

    /// <summary>
    /// Get the root page of the navigation hierarchy.
    /// Handles: Normal navigation, FlyoutPage, Shell.
    /// </summary>
    private static Page? GetRootPage()
    {
        if (Application.Current?.MainPage is NavigationPage navPage)
            return navPage.RootPage;
        
        if (Application.Current?.MainPage is FlyoutPage flyoutPage)
            return flyoutPage.Detail;
        
        return Application.Current?.MainPage;
    }

    // ============================================================================
    // EVENT HANDLERS - Delete with Confirmation
    // ============================================================================

    /// <summary>
    /// Invoked when SwipeItemView Delete button is pressed in CollectionView.
    /// 
    /// Flow:
    /// 1. Extract Cliente from SwipeItemView.BindingContext
    /// 2. Show confirmation dialog: "Tem a certeza que deseja eliminar 'NomeCliente'?"
    /// 3. If confirmed: Execute vm.DeleteCommand (fire-and-forget, no await)
    /// 4. If cancelled: Return without doing anything
    /// 
    /// CRITICAL: 
    /// - Must be `async void` to allow `await ShowConfirmAsync()`
    /// - DO NOT `await vm.DeleteCommand.Execute()` - causes deadlock!
    /// - Use `vm.DeleteCommand.Execute()` fire-and-forget instead
    /// 
    /// Test:
    /// 1. Swipe cliente in list
    /// 2. See "Tem a certeza que deseja eliminar 'NomeCliente'?"
    /// 3. Click "Cancelar" → swipe closes, cliente not deleted
    /// 4. Click "Eliminar" → cliente deleted and disappears
    /// </summary>
    private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not SwipeItemView siv || siv.BindingContext is not Cliente cliente)
                return;

            // Show confirmation dialog
            var confirm = await ShowConfirmAsync("Eliminar Cliente",
                $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
                "Eliminar", "Cancelar");

            if (!confirm) return;

            if (BindingContext is ClientsPageModel vm)
            {
                if (vm.DeleteCommand?.CanExecute(cliente) == true)
                {
                    vm.DeleteCommand.Execute(cliente);  // Fire-and-forget, no await!
                }
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    /// <summary>
    /// Invoked when Delete button is pressed in the client edit form.
    /// 
    /// Flow:
    /// 1. Verify vm.Editing is not null (current client being edited)
    /// 2. Show confirmation dialog: "Tem a certeza que deseja eliminar 'NomeCliente'?"
    /// 3. If confirmed: Execute vm.DeleteCommand, then HideFormView()
    /// 4. If cancelled: Return without doing anything
    /// 
    /// CRITICAL:
    /// - Must be `async void` to allow `await ShowConfirmAsync()`
    /// - DO NOT `await vm.DeleteCommand.Execute()` - causes deadlock!
    /// - Use `vm.DeleteCommand.Execute()` fire-and-forget instead
    /// 
    /// Test:
    /// 1. Open cliente edit form (click on cliente in list)
    /// 2. Click "Eliminar" button at bottom
    /// 3. See "Tem a certeza que deseja eliminar 'NomeCliente'?"
    /// 4. Click "Cancelar" → form stays open, cliente not deleted
    /// 5. Click "Eliminar" → cliente deleted and form closes
    /// </summary>
    private async void OnDeleteFromFormTapped(object sender, EventArgs e)
    {
        try
        {
            if (BindingContext is ClientsPageModel vm && vm.Editing is not null)
            {
                // Show confirmation dialog
                var confirm = await ShowConfirmAsync("Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar '{vm.Editing.CLINOME}'?",
                    "Eliminar", "Cancelar");

                if (!confirm) return;

                if (vm.DeleteCommand?.CanExecute(vm.Editing) == true)
                {
                    vm.DeleteCommand.Execute(vm.Editing);  // Fire-and-forget, no await!
                    HideFormView();
                }
            }
        }
        catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }
}
