namespace NAVIGEST.Android.Pages
{
    public partial class TaskDetailPage : ContentPage
    {
        public TaskDetailPage(TaskDetailPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
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
    }
}