using Newtonsoft.Json;

namespace Observe.EntityExplorer.DataObjects
{
    public class AuthenticatedUser
    {
        public DateTime AuthenticatedOn { get; set; }
        public string AuthToken { get; set; }
        
        public string UserID { get; set; }
        public string WorkspaceID { get; set; }
        public string WorkspaceName { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }

        // Environment metadata
        // https://123580374103.observeinc.com/
        // CustomerName             = 123580374103
        // Customer Label           = whatever that customer is
        // Deployment               = PROD/STG/DEV
        // CustomerDeploymentUrl    = https://123580374103.observeinc.com/
        public Uri CustomerEnvironmentUrl { get; set; }
        public string CustomerName { get; set; } = "Unknown";
        public string Deployment { get; set; } = "Unknown";
        public string CustomerLabel { get; set; } = "Unknown";
        
        public string UniqueID
        {
            get
            {
                return String.Format("{0}-{1}-{2}", this.UserName, this.UserID, this.CustomerName);
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