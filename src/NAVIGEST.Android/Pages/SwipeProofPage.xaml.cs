#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace NAVIGEST.Android.Pages
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
            await DisplayAlert("[DBG]", "PASTAS (Command) CLICKED", "OK");

            // Simula esquema externo (troca por qfile:// quando testares no teu projeto)
            var uri = new Uri("qfile://open?path=/mnt/remote/CLIENTES/ABC123");

            try
            {
                var can = await Launcher.CanOpenAsync(uri);
                await DisplayAlert("[DBG]", $"CanOpen qfile = {can}", "OK");
                if (can)
                {
                    await Launcher.OpenAsync(uri);
                    await DisplayAlert("[DBG]", "OpenAsync(qfile) OK", "OK");
                }
                else
                {
                    await DisplayAlert("Qfile", $"Fallback visual para '{nome}'", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Qfile falhou: {ex.Message}", "OK");
            }
        }

        private void DoEdit(string? nome)
        {
            DisplayAlert("EDITAR", $"Abrir form de '{nome}'", "OK");
        }

        private async Task DoDeleteAsync(string? nome)
        {
            await DisplayAlert("[DBG]", "DELETE (Command) CLICKED", "OK");
            var confirm = await DisplayAlert("Eliminar", $"Apagar '{nome}'?", "Eliminar", "Cancelar");
            if (!confirm) return;

            if (nome != null && Items.Contains(nome))
            {
                Items.Remove(nome);
                await DisplayAlert("OK", "Eliminado.", "Fechar");
            }
        }
    }
}
