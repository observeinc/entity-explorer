using Newtonsoft.Json;

namespace Observe.EntityExplorer.DataObjects
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AuthenticatedUser
    {
        [JsonProperty (PropertyName = "AO")]
        public DateTime AuthenticatedOn { get; set; }
        [JsonProperty (PropertyName = "AT")]
        public string AuthToken { get; set; }
        
        [JsonProperty (PropertyName = "UI")]
        public string UserID { get; set; }
        [JsonProperty (PropertyName = "WI")]
        public string WorkspaceID { get; set; }
        [JsonProperty (PropertyName = "WN")]
        public string WorkspaceName { get; set; }
        [JsonProperty (PropertyName = "UN")]
        public string UserName { get; set; }
        [JsonProperty (PropertyName = "DN")]
        public string DisplayName { get; set; }

        // Environment metadata
        // https://123580374103.observeinc.com/
        // CustomerName             = 123580374103
        // Customer Label           = whatever that customer is
        // Deployment               = PROD/STG/DEV
        // CustomerDeploymentUrl    = https://123580374103.observeinc.com/
        [JsonProperty (PropertyName = "URL")]
        public Uri CustomerEnvironmentUrl { get; set; }
        [JsonProperty (PropertyName = "CN")]
        public string CustomerName { get; set; } = "Unknown";
        [JsonProperty (PropertyName = "D")]
        public string Deployment { get; set; } = "Unknown";
        [JsonProperty (PropertyName = "CL")]
        public string CustomerLabel { get; set; } = "Unknown";
        
        public string UniqueID
        {
            get
            {
                return String.Format("{0}-{1}-{2}", this.UserName, this.UserID, this.CustomerName);
    //         return String.Format("{0}({1})@{2}", this.UserName, this.UserID, this.CustomerName);
            }
        }


        public override string ToString()
        {
            return String.Format(
                "{0} ({1})@{2} ({3}) authenticated on {4:u}",
                this.DisplayName,
                this.UserName,
                this.CustomerLabel,
                this.CustomerName,
                this.AuthenticatedOn);
        }        
    }
}