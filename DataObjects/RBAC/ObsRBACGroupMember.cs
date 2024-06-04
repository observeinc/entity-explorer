using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsRBACGroupMember : ObsRBACObject
    {
        public new string name 
        {
            get 
            {
                StringBuilder sb = new StringBuilder(64);
                
                sb.Append(" TODO TODO ");
                
                return sb.ToString();                
            }
        }

        public ObsRBACGroup ParentGroup { get; set; }

        public ObsRBACObject ChildObject { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsRBACGroupMember: {0}/{1}/{2}",
                this.ParentGroup,
                this.ChildObject,
                this.id);
        }

        public ObsRBACGroupMember () {}

        public ObsRBACGroupMember(JObject entityObject) : base (entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            // Typical format is o::111775605936:rbacgroupmember:8000001124
            this.ID = this.id.Split(":").Last();
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");

            if (this.description.StartsWith("Default membership", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                this.OriginType = ObsObjectOriginType.System;
            }
            else if (this.description.StartsWith("Provisioned by \"RBAC\" for", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                this.OriginType = ObsObjectOriginType.App;
            }
            else if (this.description.StartsWith("SAML provisioned", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                this.OriginType = ObsObjectOriginType.SAML;
            }
            else if (this.description.Contains("set by user update", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                this.OriginType = ObsObjectOriginType.User;
            }            
            else
            {
                this.OriginType = ObsObjectOriginType.User;
            }
        }

        public void AddParentGroup(Dictionary<string, ObsRBACGroup> allGroupsDict)
        {
            ObsRBACGroup targetGroup = null;
            if (allGroupsDict.TryGetValue(JSONHelper.getStringValueFromJToken(this._raw, "groupId"), out targetGroup) == true)
            {
                this.ParentGroup = targetGroup;
            }
        }

        public void AddChildGroupOrUser(Dictionary<string, ObsRBACGroup> allGroupsDict, Dictionary<string, ObsUser> allUsersDict)
        {
            string groupId = JSONHelper.getStringValueFromJToken(this._raw, "memberGroupId");
            if (groupId.Length > 0)
            {
                ObsRBACGroup targetGroup = null;
                if (allGroupsDict.TryGetValue(groupId, out targetGroup) == true)
                {
                    this.ChildObject = targetGroup;
                }
                else
                {
                    ObsRBACGroup obsRBACGroup = new ObsRBACGroup();
                    obsRBACGroup.id = groupId;
                    obsRBACGroup.ID = obsRBACGroup.id;
                    obsRBACGroup.name = "Deleted group";
                    obsRBACGroup.OriginType = ObsObjectOriginType.Unknown;                    
                    this.ChildObject = obsRBACGroup;
                }
            }
            string userId = JSONHelper.getStringValueFromJToken(this._raw, "memberUserId");
            if (userId.Length > 0)
            {
                ObsUser targetUser = null;
                if (allUsersDict.TryGetValue(userId, out targetUser) == true)
                {
                    this.ChildObject = targetUser;
                }
                else
                {
                    ObsUser obsUser = new ObsUser();
                    obsUser.id = userId;
                    obsUser.ID = obsUser.id;
                    obsUser.status = "Deleted";

                    Match match = Regex.Match(this.description, "Provisioned by .* for \"(.*)\"", RegexOptions.IgnoreCase);
                    if (match.Success == true && match.Groups.Count > 1)
                    {
                        obsUser.name = String.Format("Deleted user {0}", match.Groups[1].Value);
                    }
                    else
                    {
                        obsUser.name = "Deleted user";
                    }
                    
                    obsUser.UserType = ObsUserType.Unknown;
                    
                    this.ChildObject = obsUser;
                }
            }
        }        
    }
}