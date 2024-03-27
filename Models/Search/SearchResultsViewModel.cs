using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class SearchResultsViewModel : BaseViewModel
    {
        public string SearchQuery { get; set; }
        public List<ObsStage> SearchResults { get; set; }

        public SearchResultsViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment) : base(currentUser, observeEnvironment)
        {
            
        }
    }
}
