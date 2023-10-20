using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsFieldDefinition : ObsObject
    {
        public string type { get; set; }
        
        public bool isNullable { get; set; }
        public bool isEnum { get; set; }
        public bool isSearchable { get; set; }
        public bool isHidden { get; set; }
        public bool isConst { get; set; }
        public bool isMetric { get; set; }

        public ObsCompositeObject Parent { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsFieldDefinition: {0}/{1} in {2}",
                this.name,
                this.type,
                this.Parent);
        }

        public ObsFieldDefinition () {}

        public ObsFieldDefinition(JObject entityObject, ObsCompositeObject parentObject)
        {
            this._raw = entityObject;

            // typedef.def.fields[].field
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            JObject typeObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "type");
            if (typeObject != null)
            {
                this.type = JSONHelper.getStringValueFromJToken(typeObject, "rep");
                this.isNullable = JSONHelper.getBoolValueFromJToken(typeObject, "nullable");
            }
            this.isEnum = JSONHelper.getBoolValueFromJToken(entityObject, "isEnum");            
            this.isSearchable = JSONHelper.getBoolValueFromJToken(entityObject, "isSearchable");            
            this.isHidden = JSONHelper.getBoolValueFromJToken(entityObject, "isHidden");            
            this.isConst = JSONHelper.getBoolValueFromJToken(entityObject, "isConst");            
            this.isMetric = JSONHelper.getBoolValueFromJToken(entityObject, "isMetric");            

            this.Parent = parentObject;
        }
    }
}