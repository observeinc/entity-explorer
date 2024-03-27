using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class DetailsWorksheetViewModel : BaseViewModel
    {
        public ObsWorksheet CurrentWorksheet { get; set; }

        public DetailsWorksheetViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment) : base(currentUser, observeEnvironment)
        {
            
        }
    }
}
