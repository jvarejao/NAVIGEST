using NAVIGEST.iOS.Models;
using CommunityToolkit.Mvvm.Input;

namespace NAVIGEST.iOS.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}