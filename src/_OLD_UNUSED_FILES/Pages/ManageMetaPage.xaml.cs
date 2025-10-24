namespace AppLoginMaui.Pages
{
    public partial class ManageMetaPage : ContentPage
    {
        public ManageMetaPage(ManageMetaPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}

#if WINDOWS
// Código Windows específico (exemplo: animações, navegação, layouts)
#endif
#if ANDROID
// Código Android específico (exemplo: animações, navegação, layouts)
#endif
#if IOS
// Código iOS específico (exemplo: animações, navegação, layouts)
#endif