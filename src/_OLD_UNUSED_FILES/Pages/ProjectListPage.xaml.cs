namespace AppLoginMaui.Pages
{
    public partial class ProjectListPage : ContentPage
    {
        public ProjectListPage(ProjectListPageModel model)
        {
            BindingContext = model;
            InitializeComponent();
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