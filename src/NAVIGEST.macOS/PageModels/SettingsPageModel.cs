using System.Windows.Input;

namespace NAVIGEST.macOS.PageModels
{
    public class SettingsPageModel
    {
        public ICommand OpenUsersCommand { get; }
        public ICommand OpenDBConfigCommand { get; }
        public ICommand OpenFileServerConfigCommand { get; }
        public ICommand BackCommand { get; }

        public SettingsPageModel()
        {
            // Assuming routes are registered in AppShell
            OpenUsersCommand = new Command(async () => await Shell.Current.GoToAsync("config.utilizadores"));
            OpenDBConfigCommand = new Command(async () => await Shell.Current.GoToAsync("config.db"));
            OpenFileServerConfigCommand = new Command(async () => await Shell.Current.GoToAsync("config.fileserver"));
            BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        }
    }
}
