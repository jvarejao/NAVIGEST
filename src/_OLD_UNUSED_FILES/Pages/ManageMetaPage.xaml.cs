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
// C�digo Windows espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if ANDROID
// C�digo Android espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if IOS
// C�digo iOS espec�fico (exemplo: anima��es, navega��o, layouts)
#endif