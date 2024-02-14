using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsCreditsMonitor
    {
        public string MonitorID { get; set; }
        public string MonitorName { get; set; }
        public string PackageName { get; set; }

        public decimal Credits { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsCreditsMonitor: {0} ({1}) = {2}",
                this.MonitorName,
                this.MonitorID,
                this.Credits);
        }        
    }
}