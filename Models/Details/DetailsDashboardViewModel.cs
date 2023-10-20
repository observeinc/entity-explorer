using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class DetailsDashboardViewModel : BaseViewModel
    {
        public ObsDashboard CurrentDashboard { get; set; }

        public DetailsDashboardViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment) : base(currentUser, observeEnvironment)
        {
            
        }
    }
}
