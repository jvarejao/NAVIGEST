using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.Services;

namespace NAVIGEST.macOS.PageModels
{
    public class FileServerSetupPageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private Setup? _setup;
        public Setup? Setup
        {
            get => _setup;
            set { _setup = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public FileServerSetupPageModel()
        {
            SaveCommand = new Command(async () => await OnSaveAsync());
            CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Setup = await DatabaseService.GetSetupAsync();
                if (Setup == null)
                {
                    await AppShell.DisplayToastAsync("Configuração não encontrada.", ToastTipo.Erro);
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnSaveAsync()
        {
            if (Setup == null) return;
            IsBusy = true;
            try
            {
                var success = await DatabaseService.UpdateSetupAsync(Setup);
                if (success)
                {
                    await AppShell.DisplayToastAsync("Configuração guardada com sucesso!", ToastTipo.Sucesso);
                    await Shell.Current.Navigation.PopAsync();
                }
                else
                {
                    await AppShell.DisplayToastAsync("Erro ao guardar configuração.", ToastTipo.Erro);
                }
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
