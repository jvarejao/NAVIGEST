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
        // C�digo Windows espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if ANDROID
        // C�digo Android espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
#if IOS
        // C�digo iOS espec�fico (exemplo: anima��es, navega��o, layouts)
#endif
    }
}