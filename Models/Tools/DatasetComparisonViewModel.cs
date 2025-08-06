using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Tools;

#nullable enable

namespace Observe.EntityExplorer.Models
{
    public class DatasetComparisonViewModel : BaseViewModel
    {
        public string BaselineDatasetId { get; set; } = string.Empty;

        public ObsDataset? BaselineDataset { get; set; }

        public string BuildId { get; set; } = string.Empty;

        public string OptimizedDatasetId { get; set; } = string.Empty;

        public ObsDataset? OptimizedDataset { get; set; }

        public List<ObsDashboard> BaselineDashboards { get; set; } = new();

        public List<DatasetComparisonResult> Results { get; set; } = new();

        public DatasetComparisonViewModel(AuthenticatedUser currentUser, ObserveEnvironment observeEnvironment)
            : base(currentUser, observeEnvironment)
        {
        }
    }
}
