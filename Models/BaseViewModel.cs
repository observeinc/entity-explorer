using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models;

public class BaseViewModel
{
    public ObserveEnvironment ObserveEnvironment { get; set; }
    public AuthenticatedUser CurrentUser { get; set; }

    public BaseViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment)
    {
        this.CurrentUser = currentUser;
        this.ObserveEnvironment = observeEnvironment;
    }
}
