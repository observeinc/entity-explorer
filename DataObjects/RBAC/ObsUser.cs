using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsUser : ObsRBACObject
    {
        public string comment { get; set; }
        public string email { get; set; }
        public DateTime expirationTime { get; set; }
        public string label { get; set; }
        public string role { get; set; }
        public string status { get; set; }
        public string timezone { get; set; }
        public string defaultWorkspaceId { get; set; }
        public ObsUserType UserType { get; set; } = ObsUserType.Unknown;

        public override string ToString()
        {
            return String.Format(
                "ObsUser: {0}/{1}/{2}/{3}",
                this.label,
                this.email,
                this.id,
                this.UserType);
        }

        public ObsUser () {}

        public ObsUser(JObject entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            if (this.id == String.Empty) this.id = JSONHelper.getStringValueFromJToken(entityObject, "userId");
            this.ID = this.id;
            
            this.UserType = ObsUserType.Unknown;
            JArray typeArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "type");
            if (typeArray != null)
            {
                foreach (JToken typeValue in typeArray)
                {
                    string typeValueString = typeValue.ToString();
                    switch (typeValueString)
                    {
                        case "UserTypeEmail":
                            this.UserType = this.UserType | ObsUserType.Email;
                            break;

                        case "UserTypeSystem":
                            this.UserType = this.UserType | ObsUserType.System;
                            break;

                        case "UserTypeSaml2":
                            this.UserType = this.UserType | ObsUserType.SAML;
                            break;

                        default:
                            break;
                    }
                }
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