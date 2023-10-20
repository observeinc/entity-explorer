using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsCompositeObject : ObsObject
    {
        public DateTime createdDate { get; set; }
        public ObsUser createdBy { get; set; }
        public DateTime updatedDate { get; set; }
        public ObsUser updatedBy { get; set; }

        public ObsCompositeObjectType ObjectType { get; set; } = ObsCompositeObjectType.Unknown;

        public ObsObjectOriginType OriginType { get; set; } = ObsObjectOriginType.Unknown;

        public ObsCompositeObject () {}

        public ObsCompositeObject(JObject entityObject)
        {
            this.createdBy = new ObsUser((JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "createdByInfo"));
            this.createdDate = JSONHelper.getDateTimeValueFromJToken(entityObject, "createdDate");
            this.updatedBy = new ObsUser((JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "updatedByInfo"));
            this.updatedDate = JSONHelper.getDateTimeValueFromJToken(entityObject, "updatedDate");
        }
    }
}