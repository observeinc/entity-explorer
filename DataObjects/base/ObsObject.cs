using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsObject
    {
        public string id { get; set; }
        public string name { get; set; }

        public string description { get; set; }

        public JObject _raw { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsObject: {0} ({1})",
                this.name,
                this.id);
        }        
    }
}