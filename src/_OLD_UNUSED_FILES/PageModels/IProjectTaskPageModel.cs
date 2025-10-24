using AppLoginMaui.Models;
using CommunityToolkit.Mvvm.Input;

namespace AppLoginMaui.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}