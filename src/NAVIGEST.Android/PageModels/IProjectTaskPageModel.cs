using NAVIGEST.Android.Models;
using CommunityToolkit.Mvvm.Input;

namespace NAVIGEST.Android.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}