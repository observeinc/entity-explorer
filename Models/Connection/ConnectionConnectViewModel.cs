using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class ConnectionConnectViewModel : BaseViewModel
    {
        public List<AuthenticatedUser> AllUsers { get; set; } = new List<AuthenticatedUser>(0);

        public string Environment { get; set; } 

        public string Username { get; set; } 

        public string DelegateURL { get; set; } 

        public string DelegateToken { get; set; } 

        public ConnectionConnectViewModel(AuthenticatedUser currentUser) : base(currentUser, null)
        {
            
        }
    }
}
