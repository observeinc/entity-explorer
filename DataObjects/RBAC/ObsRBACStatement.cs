using System.Text;
using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsRBACStatement : ObsRBACObject
    {
        public string role { get; set; }

        public new string name 
        {
            get 
            {
                // Going to make something like that
                // Viewer|Editor|Manager|etc granted by test_group [test_group description] to All|dataset|specific object

                StringBuilder sb = new StringBuilder(64);
                
                sb.Append(this.role);
                sb.Append(" granted by ");
                
                if (this.SubjectAll == true)
                {
                    sb.Append("All");
                }
                else if (this.SubjectGroup != null)
                {
                    sb.Append(this.SubjectGroup.name);
                    if (this.SubjectGroup.description.Length > 0)
                    {
                        sb.AppendFormat(" [{0}]", this.SubjectGroup.description);
                    }
                }

                sb.Append(" to ");

                if (this.ObjectAll == true)
                {
                    sb.Append("All ");
                }
                else if (this.ObjectOwn == true)
                {
                    sb.Append("Own ");
                }
                if (this.ObjectType.Length > 0)
                {
                    sb.Append(this.ObjectType);
                }
                else if (this.ObjectObject != null)
                {
                    sb.Append(this.ObjectObject.ToString());
                }

                return sb.ToString();                
            }
        }

        public ObsUser SubjectUser { get; set; }
        public ObsRBACGroup SubjectGroup { get; set; }
        public bool SubjectAll { get; set; }
        public string SubjectSort { get; set; }

        public ObsCompositeObject ObjectObject { get; set; }
        public string ObjectType { get; set; }
        public bool ObjectAll { get; set; }
        public bool ObjectOwn { get; set; }
        public string ObjectSort { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsRBACStatement: {0}/{1}/{2}",
                this.role,
                this.description,
                this.id);
        }

        public ObsRBACStatement () {}

        public ObsRBACStatement(JObject entityObject) : base (entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            // Typical format is o::111775605936:rbacstatement:8000017483
            this.ID = this.id.Split(":").Last();
            this.role = JSONHelper.getStringValueFromJToken(entityObject, "role");
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");

            JObject subjectObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "subject");
            if (subjectObject != null)
            {
                this.SubjectAll = JSONHelper.getBoolValueFromJToken(subjectObject, "all");
                if (this.SubjectAll == true) this.SubjectSort = "all";
            }

            JObject objectObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "object");
            if (objectObject != null)
            {
                this.ObjectAll = JSONHelper.getBoolValueFromJToken(objectObject, "all");
                if (this.ObjectAll == true) this.ObjectSort = "all";
                this.ObjectOwn = JSONHelper.getBoolValueFromJToken(objectObject, "owner");
                if (this.ObjectOwn == true) this.ObjectSort = "own";
                this.ObjectType = JSONHelper.getStringValueFromJToken(objectObject, "type");
                if (this.ObjectType.Length > 0) this.ObjectSort = "this.ObjectType";
            }

            if (this.description.StartsWith("Default", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                this.OriginType = ObsObjectOriginType.System;
            }
            else if (this.description.Contains("Caused to be created by Observe administrator", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                this.OriginType = ObsObjectOriginType.App;
            }
            else if (this.description.Contains("SAML provisioned", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                this.OriginType = ObsObjectOriginType.SAML;
            }
            else
            {
                this.OriginType = ObsObjectOriginType.User;
            }
        }

        public void AddSubject(Dictionary<string, ObsRBACGroup> allGroupsDict, Dictionary<string, ObsUser> allUsersDict)
        {
            JObject subjectObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "subject");
            if (subjectObject != null)
            {
                ObsRBACGroup targetGroup = null;
                if (allGroupsDict.TryGetValue(JSONHelper.getStringValueFromJToken(subjectObject, "groupId"), out targetGroup) == true)
                {
                    this.SubjectGroup = targetGroup;
                    this.SubjectSort = targetGroup.id;
                }
                ObsUser targetUser = null;
                if (allUsersDict.TryGetValue(JSONHelper.getStringValueFromJToken(subjectObject, "userId"), out targetUser) == true)
                {
                    this.SubjectUser = targetUser;
                    this.SubjectSort = targetUser.id;
                }
            }
        }

        public void AddObject(List<ObsCompositeObject> observeObjects)
        {
            // We do not know which type of the object this is. Let's search through them all
            JObject objectObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "object");
            if (objectObject != null)
            {
                string objectId = JSONHelper.getStringValueFromJToken(objectObject, "objectId");
                if (objectId.Length > 0)
                {
                    this.ObjectObject = observeObjects.Where(o => o.id == objectId).FirstOrDefault();
                    this.ObjectSort = objectId;
                }
            }
        }

    }
}