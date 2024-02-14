using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsCreditsTransform
    {
        public string DatasetID { get; set; }
        public string DatasetName { get; set; }
        public string PackageName { get; set; }

        public decimal Credits { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsCreditsTransform: {0} ({1}) = {2}",
                this.DatasetName,
                this.DatasetID,
                this.Credits);
        }        
    }
}