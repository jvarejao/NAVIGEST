using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Popups;
using NAVIGEST.macOS.Services;
using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.PageModels
{
    public class ServiceStatusPageModel : BindableObject
    {
        private ObservableCollection<ServiceStatus> _statusList = new();
        public ObservableCollection<ServiceStatus> StatusList
        {
            get => _statusList;
            set { _statusList = value; OnPropertyChanged(); }
        }

        public ICommand BackCommand { get; }
        public ICommand AddStatusCommand { get; }
        public ICommand EditStatusCommand { get; }
        public ICommand DeleteStatusCommand { get; }
        public ICommand ManageColorsCommand { get; }

        public ServiceStatusPageModel()
        {
            BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            AddStatusCommand = new Command(async () => await OnAddStatusAsync());
            EditStatusCommand = new Command<ServiceStatus>(async s => await OnEditStatusAsync(s));
            DeleteStatusCommand = new Command<ServiceStatus>(OnDeleteStatus);
            ManageColorsCommand = new Command(async () => await Shell.Current.GoToAsync("config.cores"));
            
            LoadData();
        }

        private async void LoadData()
        {
            var list = await DatabaseService.GetServiceStatusAsync();
            StatusList = new ObservableCollection<ServiceStatus>(list);
        }

        private async Task OnAddStatusAsync()
        {
            var nomePopup = new SimpleTestPopup("Novo Estado", "Nome do estado:");
            var nomeEstado = await Shell.Current.ShowPopupAsync(nomePopup) as string;
            if (string.IsNullOrWhiteSpace(nomeEstado)) return;

            var colorResult = await Shell.Current.ShowPopupAsync(new ColorPickerPopup()) as Cor;
            if (colorResult == null || string.IsNullOrWhiteSpace(colorResult.IdCor)) return;
            var color = colorResult.IdCor;

            await DatabaseService.AddServiceStatusAsync(new ServiceStatus { Descricao = nomeEstado, Cor = color });
            LoadData();
        }

        private async Task OnEditStatusAsync(ServiceStatus status)
        {
            if (status == null) return;

            var nomePopup = new SimpleTestPopup("Editar Estado", "Nome do estado:", status.Descricao);
            var novoNome = await Shell.Current.ShowPopupAsync(nomePopup) as string;
            if (novoNome == null) return; // cancel

            var colorResult = await Shell.Current.ShowPopupAsync(new ColorPickerPopup()) as Cor;

            var corFinal = status.Cor;
            if (colorResult != null && !string.IsNullOrWhiteSpace(colorResult.IdCor))
            {
                corFinal = colorResult.IdCor;
            }

            status.Descricao = string.IsNullOrWhiteSpace(novoNome) ? status.Descricao : novoNome;
            status.Cor = corFinal;

            await DatabaseService.UpdateServiceStatusAsync(status);
            LoadData();
        }

        private async void OnDeleteStatus(ServiceStatus status)
        {
            if (status == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Eliminar", $"Tem a certeza que deseja eliminar '{status.Descricao}'?", "Sim", "Não");
            if (confirm)
            {
                await DatabaseService.DeleteServiceStatusAsync(status.ID);
                LoadData();
            }
        }

    }
}
