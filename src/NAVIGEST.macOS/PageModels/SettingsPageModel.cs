using System.Windows.Input;
using CommunityToolkit.Maui.Views;

namespace NAVIGEST.macOS.PageModels
{
    public class SettingsPageModel
    {
        public ICommand OpenUsersCommand { get; }
        public ICommand OpenDBConfigCommand { get; }
        public ICommand OpenFileServerConfigCommand { get; }
        public ICommand OpenLanguageCommand { get; }
        public ICommand OpenServiceStatusCommand { get; }
        public ICommand BackCommand { get; }

        public SettingsPageModel()
        {
            // Assuming routes are registered in AppShell
            OpenUsersCommand = new Command(async () => await Shell.Current.GoToAsync("config.utilizadores"));
            OpenDBConfigCommand = new Command(async () => await Shell.Current.GoToAsync("config.db"));
            OpenFileServerConfigCommand = new Command(async () => await Shell.Current.GoToAsync("config.fileserver"));
            OpenLanguageCommand = new Command(async () => 
            {
                var popup = new NAVIGEST.macOS.Popups.LanguageSelectionPopup();
                var result = await Shell.Current.ShowPopupAsync(popup);
                
                if (result is string cultureCode)
                {
                    await NAVIGEST.macOS.Helpers.LanguageHelper.ChangeLanguageAndRestart(cultureCode);
                }
            });
            OpenServiceStatusCommand = new Command(async () => await Shell.Current.GoToAsync("config.servicestatus"));
            BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        }
    }
}
