using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class DetailsMonitorViewModel : BaseViewModel
    {
        public ObsMonitor CurrentMonitor { get; set; }

        public DetailsMonitorViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment) : base(currentUser, observeEnvironment)
        {
            
        }
    }
}
