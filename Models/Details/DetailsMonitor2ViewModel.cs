using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class DetailsMonitor2ViewModel : BaseViewModel
    {
        public ObsMonitor2 CurrentMonitor { get; set; }

        public DetailsMonitor2ViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment) : base(currentUser, observeEnvironment)
        {
            
        }
    }
}
