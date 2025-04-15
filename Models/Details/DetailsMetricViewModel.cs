using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class DetailsMetricViewModel : BaseViewModel
    {
        public ObsMetric CurrentMetric { get; set; }
        public List<ObsStage> SearchResults { get; set; }

        public DetailsMetricViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment) : base(currentUser, observeEnvironment)
        {
            
        }
    }
}
