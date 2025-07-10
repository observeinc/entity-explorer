using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsCreditsQuery
    {
        public string DashboardID { get; set; }
        public string DashboardName { get; set; }
        public string DatasetID { get; set; }
        public string DatasetName { get; set; }
        public string PackageName { get; set; }
        public string UserName { get; set; }

        public decimal Credits { get; set; } = 0;

        public override string ToString()
        {
            return String.Format(
                "ObsCreditsQuery: {0} ({1}) {2} = {3}",
                this.DatasetName,
                this.DatasetID,
                this.UserName,
                this.Credits);
        }        
    }
}