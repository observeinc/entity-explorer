using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class DetailsDatasetViewModel : BaseViewModel
    {
        public ObsDataset CurrentDataset { get; set; }

        public DetailsDatasetViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment) : base(currentUser, observeEnvironment)
        {
            
        }
    }
}
