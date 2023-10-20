using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsUser : ObsObject
    {
        public string comment { get; set; }
        public string email { get; set; }
        public DateTime expirationTime { get; set; }
        public string label { get; set; }
        public string role { get; set; }
        public string status { get; set; }
        public string timezone { get; set; }
        public string defaultWorkspaceId { get; set; }
        public List<string> type { get; set; }

        public override string ToString()
        {
            if (this.email == String.Empty)
            {
                return String.Format(
                    "ObsUser: {0}/{1}",
                    this.label,
                    this.id);
            }
            else
            {
                return String.Format(
                    "ObsUser: {0}/{1}/{2})",
                    this.label,
                    this.email,
                    this.id);
            }
        }

        public ObsUser () {}

        public ObsUser(JObject entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            if (this.id == String.Empty) this.id = JSONHelper.getStringValueFromJToken(entityObject, "userId");
            JArray typeArrayToken = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "type");
            if (typeArrayToken != null)
            {
                this.type = typeArrayToken.ToObject<List<string>>();
            }
            else
            {
                this.type = new List<string>();
                this.type.Add("unknown");
            }
            this.email = JSONHelper.getStringValueFromJToken(entityObject, "email");
            this.label = JSONHelper.getStringValueFromJToken(entityObject, "label");
            if (this.label == String.Empty) this.label = JSONHelper.getStringValueFromJToken(entityObject, "userLabel");
            this.name = this.label;
            this.timezone = JSONHelper.getStringValueFromJToken(entityObject, "timezone");
            if (this.timezone == String.Empty) this.timezone = JSONHelper.getStringValueFromJToken(entityObject, "userTimezone");
            this.status = JSONHelper.getStringValueFromJToken(entityObject, "status");
            this.role = JSONHelper.getStringValueFromJToken(entityObject, "role");
            this.comment = JSONHelper.getStringValueFromJToken(entityObject, "comment");
            this.expirationTime = JSONHelper.getDateTimeValueFromJToken(entityObject, "expirationTime");

            JArray workspaceArrayToken = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "workspaces");
            if (workspaceArrayToken != null)
            {
                foreach (JObject workspaceObject in workspaceArrayToken)
                {
                    this.defaultWorkspaceId = JSONHelper.getStringValueFromJToken(workspaceObject, "id");
                }
            }
        }
    }
}