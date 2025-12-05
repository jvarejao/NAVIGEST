#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

using NAVIGEST.Shared.Resources.Strings;

namespace NAVIGEST.iOS.Pages
{
    public partial class SwipeProofPage : ContentPage
    {
        // Dados simples para testar
        public ObservableCollection<string> Items { get; } =
            new ObservableCollection<string> { "Cliente A", "Cliente B", "Cliente C" };

        // Commands usados diretamente pelos SwipeItem
        public ICommand OpenPastasCommand { get; }
        public ICommand EditClientCommand { get; }
        public ICommand DeleteClientCommand { get; }

        public SwipeProofPage()
        {
            // Os commands TÊM de existir antes do InitializeComponent (bindings)
            OpenPastasCommand  = new Command<string>(async s => await DoPastasAsync(s));
            EditClientCommand  = new Command<string>(DoEdit);
            DeleteClientCommand= new Command<string>(async s => await DoDeleteAsync(s));

            BindingContext = this;
            InitializeComponent();
        }

        private async Task DoPastasAsync(string? nome)
        {
            await DisplayAlert("[DBG]", "PASTAS (Command) CLICKED", AppResources.Common_OK);

            // Simula esquema externo (troca por qfile:// quando testares no teu projeto)
            var uri = new Uri("qfile://open?path=/mnt/remote/CLIENTES/ABC123");

            try
            {
                var can = await Launcher.CanOpenAsync(uri);
                await DisplayAlert("[DBG]", $"CanOpen qfile = {can}", AppResources.Common_OK);
                if (can)
                {
                    await Launcher.OpenAsync(uri);
                    await DisplayAlert("[DBG]", "OpenAsync(qfile) OK", AppResources.Common_OK);
                }
                else
                {
                    await DisplayAlert("Qfile", $"Fallback visual para '{nome}'", AppResources.Common_OK);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(AppResources.Common_Error, string.Format(AppResources.SwipeProofPage_QfileError, ex.Message), AppResources.Common_OK);
            }
        }

        private void DoEdit(string? nome)
        {
            DisplayAlert(AppResources.SwipeProofPage_EditTitle, string.Format(AppResources.SwipeProofPage_EditMessage, nome), AppResources.Common_OK);
        }

        private async Task DoDeleteAsync(string? nome)
        {
            await DisplayAlert("[DBG]", "DELETE (Command) CLICKED", AppResources.Common_OK);
            var confirm = await DisplayAlert(AppResources.Common_Delete, string.Format(AppResources.SwipeProofPage_DeleteConfirm, nome), AppResources.Common_Delete, AppResources.Common_Cancel);
            if (!confirm) return;

            if (nome != null && Items.Contains(nome))
            {
                Items.Remove(nome);
                await DisplayAlert(AppResources.Common_OK, AppResources.SwipeProofPage_Deleted, AppResources.Common_Close);
            }
        }
    }
}
