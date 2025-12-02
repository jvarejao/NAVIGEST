using NAVIGEST.macOS.PageModels;

namespace NAVIGEST.macOS.Pages
{
    public partial class FileServerSetupPage : ContentPage
    {
        public FileServerSetupPage()
        {
            InitializeComponent();
            BindingContext = new FileServerSetupPageModel();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is FileServerSetupPageModel vm)
            {
                await vm.LoadAsync();
            }
        }
    }
}
