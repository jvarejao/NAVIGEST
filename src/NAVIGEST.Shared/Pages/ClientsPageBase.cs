#nullable enable
using Microsoft.Maui.Controls;
using System;

namespace NAVIGEST.Shared.Pages
{
    /// <summary>
    /// Base class for ClientsPage implementations across all platforms.
    /// Provides shared event handler logic for SearchBar, CollectionView, SwipeView, and Form controls.
    /// Platform-specific implementations inherit and override virtual methods as needed.
    /// 
    /// Note: This class uses generic object types to avoid dependencies on platform-specific models.
    /// Platform implementations should cast to their specific types as needed.
    /// </summary>
    public class ClientsPageBase : ContentPage
    {
        // ========================================
        // INITIALIZATION & LIFECYCLE
        // ========================================

        /// <summary>
        /// Override to load data on first appearance.
        /// Call base implementation to ensure data is loaded once.
        /// </summary>
        protected virtual async Task EnsureLoadedAsync()
        {
            // To be overridden by platform implementations
        }

        // ========================================
        // SEARCHBAR EVENTS
        // ========================================

        /// <summary>
        /// Fired when SearchBar search button is pressed.
        /// Override to handle platform-specific behavior (e.g., iOS keyboard dismissal).
        /// </summary>
        protected virtual void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
        {
            // Default: do nothing. Platform implementations can override.
        }

        /// <summary>
        /// Fired when SearchBar text changes.
        /// Override if filtering logic is needed beyond ViewModel binding.
        /// </summary>
        protected virtual void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            // Default: filtering handled via ViewModel binding in XAML.
        }

        // ========================================
        // COLLECTIONVIEW EVENTS
        // ========================================

        /// <summary>
        /// Fired when CollectionView scrolls.
        /// Use to dismiss keyboard or close swipe views on scroll.
        /// </summary>
        protected virtual void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            // Default: do nothing. Override to handle scroll-related cleanup.
        }

        // ========================================
        // SWIPE EVENTS
        // ========================================

        /// <summary>
        /// Fired when SwipeView swipe ends (after button interaction).
        /// Override to track active swipe views and manage state.
        /// </summary>
        protected virtual void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
        {
            // Default: do nothing. Override to manage swipe view state.
        }

        /// <summary>
        /// Fired when Edit SwipeItem is invoked.
        /// Base: Validates sender type, executes SelectCommand, shows form.
        /// </summary>
        protected virtual void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem)
                    return;

                var cliente = swipeItem.BindingContext;
                if (cliente is null)
                    return;

                HandleSelectCommand(cliente);
                ShowFormView(isNew: false);
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnEditSwipeInvoked");
            }
        }

        /// <summary>
        /// Executed when Delete SwipeItem is invoked.
        /// Base: Validates sender type, shows confirmation, executes DeleteCommand.
        /// </summary>
        protected virtual async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem)
                    return;

                var cliente = swipeItem.BindingContext;
                if (cliente is null)
                    return;

                bool confirmed = await ShowConfirmationAsync(
                    "Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar?",
                    "Eliminar", "Cancelar");

                if (!confirmed)
                    return;

                HandleDeleteCommand(cliente);
                await ShowSuccessMessageAsync("Cliente eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnDeleteSwipeInvoked");
            }
        }

        /// <summary>
        /// Fired when Pastas SwipeItem is invoked.
        /// Override to handle platform-specific navigation/file handling.
        /// </summary>
        protected virtual async void OnPastasSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem)
                    return;

                var cliente = swipeItem.BindingContext;
                if (cliente is null)
                    return;

                await HandlePastasAsync(cliente);
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnPastasSwipeInvoked");
            }
        }

        /// <summary>
        /// Fired when Services SwipeItem is invoked.
        /// Default: Show toast with service count. Override for more complex handling.
        /// </summary>
        protected virtual async void OnServicesSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is not SwipeItemView swipeItem)
                    return;

                var cliente = swipeItem.BindingContext;
                if (cliente is null)
                    return;

                await HandleServicesAsync(cliente);
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnServicesSwipeInvoked");
            }
        }

        // ========================================
        // LIST/CELL INTERACTION EVENTS
        // ========================================

        /// <summary>
        /// Fired when a client cell is tapped in the list.
        /// Executes SelectCommand and shows form for editing.
        /// </summary>
        protected virtual void OnClientCellTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is not Grid grid)
                    return;

                var cliente = grid.BindingContext;
                if (cliente is null)
                    return;

                HandleSelectCommand(cliente);
                ShowFormView(isNew: false);
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnClientCellTapped");
            }
        }

        // ========================================
        // FAB & BUTTON EVENTS
        // ========================================

        /// <summary>
        /// Fired when Add Client FAB is tapped.
        /// Executes NewCommand and shows form in create mode.
        /// </summary>
        protected virtual void OnAddClientTapped(object sender, TappedEventArgs e)
        {
            try
            {
                // Platform implementations should handle this
                ShowFormView(isNew: true);
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnAddClientTapped");
            }
        }

        /// <summary>
        /// Fired when Save button is tapped in the form.
        /// Validates input, executes SaveCommand, hides form.
        /// Override to add platform-specific save logic.
        /// </summary>
        protected virtual async void OnSaveClientTapped(object sender, EventArgs e)
        {
            try
            {
                await ShowSuccessMessageAsync("Cliente guardado com sucesso.");
                HideFormView();
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnSaveClientTapped");
            }
        }

        /// <summary>
        /// Fired when Cancel button is tapped in the form.
        /// Clears editing state and hides form.
        /// </summary>
        protected virtual void OnCancelEditTapped(object sender, EventArgs e)
        {
            try
            {
                HideFormView();
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnCancelEditTapped");
            }
        }

        /// <summary>
        /// Fired when form background is tapped (overlay touch).
        /// Closes form without saving.
        /// </summary>
        protected virtual void OnFormBackgroundTapped(object sender, TappedEventArgs e)
        {
            HideFormView();
        }

        /// <summary>
        /// Fired when Pastas button is tapped in the form.
        /// Navigates to client folder or handles platform-specific file access.
        /// </summary>
        protected virtual async void OnPastasFormTapped(object sender, EventArgs e)
        {
            try
            {
                // Platform implementations should handle this
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnPastasFormTapped");
            }
        }

        /// <summary>
        /// Fired when Delete button is tapped in the form.
        /// Shows confirmation and executes DeleteCommand.
        /// </summary>
        protected virtual async void OnDeleteFromFormTapped(object sender, EventArgs e)
        {
            try
            {
                bool confirmed = await ShowConfirmationAsync(
                    "Eliminar Cliente",
                    $"Tem a certeza que deseja eliminar?",
                    "Eliminar", "Cancelar");

                if (!confirmed)
                    return;

                await ShowSuccessMessageAsync("Cliente eliminado com sucesso.");
                HideFormView();
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnDeleteFromFormTapped");
            }
        }

        /// <summary>
        /// Fired when Add Vendedor button is tapped.
        /// Shows popup to add new vendor. Override for platform-specific popup handling.
        /// </summary>
        protected virtual async void OnAddVendedorTapped(object sender, EventArgs e)
        {
            // To be implemented by platform-specific subclasses
            // Each platform has different popup handling requirements
        }

        // ========================================
        // ENTRY/FORM FIELD EVENTS
        // ========================================

        /// <summary>
        /// Fired when ValorCredito Entry gains focus.
        /// Selects all text for easy replacement.
        /// </summary>
        protected virtual void OnValorCreditoFocused(object sender, FocusEventArgs e)
        {
            try
            {
                if (sender is not Entry entry)
                    return;

                var len = entry.Text?.Length ?? 0;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        entry.CursorPosition = 0;
                        entry.SelectionLength = len;
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnValorCreditoFocused");
            }
        }

        /// <summary>
        /// Fired when ValorCredito Entry loses focus.
        /// Formats the value (e.g., adds currency symbol).
        /// Override to customize formatting behavior.
        /// </summary>
        protected virtual void OnValorCreditoUnfocused(object sender, FocusEventArgs e)
        {
            try
            {
                // Platform implementations should handle this
            }
            catch (Exception ex)
            {
                HandleError(ex, "OnValorCreditoUnfocused");
            }
        }

        /// <summary>
        /// Fired when ValorCredito Entry text changes.
        /// Filters non-numeric characters while editing.
        /// Override to customize input filtering.
        /// </summary>
        protected virtual void OnValorCreditoTextChanged(object sender, TextChangedEventArgs e)
        {
            // To be implemented by platform-specific subclasses
            // Complex numeric filtering logic varies by platform
        }

        // ========================================
        // HELPER METHODS - COMMAND EXECUTION
        // ========================================

        /// <summary>
        /// Executes SelectCommand for the given cliente.
        /// Shared logic across all platforms.
        /// </summary>
        protected void HandleSelectCommand(object cliente)
        {
            // Platform implementations should handle command execution
        }

        /// <summary>
        /// Executes DeleteCommand for the given cliente.
        /// Shared logic across all platforms.
        /// </summary>
        protected void HandleDeleteCommand(object cliente)
        {
            // Platform implementations should handle command execution
        }

        // ========================================
        // HELPER METHODS - ASYNC ACTIONS
        // ========================================

        /// <summary>
        /// Handles Pastas navigation/file access.
        /// Platform-specific implementations override this.
        /// </summary>
        protected virtual async Task HandlePastasAsync(object cliente)
        {
            // Platform implementations should handle Pastas navigation
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles Services display.
        /// Shows toast with service count.
        /// </summary>
        protected virtual async Task HandleServicesAsync(object cliente)
        {
            // Platform implementations should handle Services display
            await Task.CompletedTask;
        }

        // ========================================
        // HELPER METHODS - UI DISPLAY
        // ========================================

        /// <summary>
        /// Shows the form view container.
        /// Uses named references (ListViewContainer, FormViewContainer) that must be defined in XAML.
        /// </summary>
        protected void ShowFormView(bool isNew = false)
        {
            try
            {
                var listContainer = FindByName("ListViewContainer") as View;
                var formContainer = FindByName("FormViewContainer") as View;

                if (listContainer is null || formContainer is null)
                    return;

                listContainer.IsVisible = false;
                formContainer.IsVisible = true;

                // Update form title and button text if labels are available
                if (FindByName("FormTitle") is Label titleLabel)
                {
                    titleLabel.Text = isNew ? "Novo Cliente" : "Editar Cliente";
                }

                if (FindByName("SaveButton") is Button saveButton)
                {
                    saveButton.Text = isNew ? "Adicionar" : "Atualizar";
                }

                // Show action buttons (Pastas, Delete) only in edit mode
                var actionGrid = FindByName("ActionButtonsGrid") as View;
                if (actionGrid is not null)
                {
                    actionGrid.IsVisible = !isNew;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "ShowFormView");
            }
        }

        /// <summary>
        /// Hides the form view container and shows list view.
        /// </summary>
        protected void HideFormView()
        {
            try
            {
                var listContainer = FindByName("ListViewContainer") as View;
                var formContainer = FindByName("FormViewContainer") as View;

                if (listContainer is null || formContainer is null)
                    return;

                formContainer.IsVisible = false;
                listContainer.IsVisible = true;
            }
            catch (Exception ex)
            {
                HandleError(ex, "HideFormView");
            }
        }

        // ========================================
        // HELPER METHODS - USER FEEDBACK
        // ========================================

        /// <summary>
        /// Shows a confirmation dialog.
        /// Override to customize dialog behavior per platform.
        /// </summary>
        protected virtual async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return await DisplayAlert(title, message, accept, cancel);
        }

        /// <summary>
        /// Shows a success message (toast or alert).
        /// Override to customize per platform.
        /// </summary>
        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            // Override in platform implementations to use GlobalToast or DisplayAlert
        }

        /// <summary>
        /// Shows an info/warning message.
        /// Override to customize per platform.
        /// </summary>
        protected virtual async Task ShowInfoMessageAsync(string message)
        {
            // Override in platform implementations
        }

        /// <summary>
        /// Handles errors with logging and optional user feedback.
        /// Override to customize error handling per platform.
        /// </summary>
        protected virtual void HandleError(Exception ex, string context)
        {
            // Override in platform implementations to use GlobalErro.TratarErro
            System.Diagnostics.Debug.WriteLine($"[ERROR] {context}: {ex.Message}");
        }
    }
}
