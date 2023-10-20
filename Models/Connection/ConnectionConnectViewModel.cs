using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Models
{
    public class ConnectionConnectViewModel : BaseViewModel
    {
        public List<AuthenticatedUser> AllUsers { get; set; } = new List<AuthenticatedUser>(0);

        public ConnectionConnectViewModel(AuthenticatedUser currentUser) : base(currentUser, null)
        {
            
        }
    }
}
