using NAVIGEST.macOS.Models;
using CommunityToolkit.Mvvm.Input;

namespace NAVIGEST.macOS.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}