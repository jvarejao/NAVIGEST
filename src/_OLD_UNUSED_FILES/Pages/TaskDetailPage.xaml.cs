namespace AppLoginMaui.Pages
{
    public partial class TaskDetailPage : ContentPage
    {
        public TaskDetailPage(TaskDetailPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
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
    }
}