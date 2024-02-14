using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsTimeRange : ObsObject
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public TimeSpan duration { get; set; }

        public ObsTimeRange () {}

        public ObsTimeRange(JObject entityObject)
        {
            this._raw = entityObject;

            this.start = JSONHelper.getDateTimeValueFromJToken(entityObject, "start");
            this.end = JSONHelper.getDateTimeValueFromJToken(entityObject, "end");

            this.duration = this.end - this.start;
        }


        public override string ToString()
        {
            return String.Format(
                "ObsTimeRange: {0:u} - {1:u} ({2:c})",
                this.start,
                this.end, 
                this.duration);
        }        
    }
}