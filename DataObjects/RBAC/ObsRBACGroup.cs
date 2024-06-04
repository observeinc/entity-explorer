using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsRBACGroup : ObsRBACObject
    {
        public override string ToString()
        {
            return String.Format(
                "ObsGroup: {0}/{1}",
                this.name,
                this.ID);
        }

        public ObsRBACGroup () {}

        public ObsRBACGroup(JObject entityObject) : base (entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            // Typical format is o::111775605936:rbacgroup:8000017122
            this.ID = this.id.Split(":").Last();
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");
        
            if (this.name == "reader" || this.name == "writer" || this.name == "admin" || this.name == "manage_own_objects")
            {
                this.OriginType = ObsObjectOriginType.System;
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
    }
}