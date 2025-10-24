namespace NAVIGEST.Android.Pages
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
// Cdigo Windows especfico (exemplo: animaes, navegao, layouts)
#endif
#if ANDROID
// Cdigo Android especfico (exemplo: animaes, navegao, layouts)
#endif
#if IOS
// Cdigo iOS especfico (exemplo: animaes, navegao, layouts)
#endif