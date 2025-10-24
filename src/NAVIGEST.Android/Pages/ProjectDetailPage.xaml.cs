using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Pages
{
    public partial class ProjectDetailPage : ContentPage
    {
        public ProjectDetailPage(ProjectDetailPageModel model)
        {
            InitializeComponent();

            BindingContext = model;
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
