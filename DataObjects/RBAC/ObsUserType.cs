namespace Observe.EntityExplorer.DataObjects
{
    [Flags]
    public enum ObsUserType
    {
        Unknown        = 0,
        System         = 1 << 1,
        Email          = 1 << 2,
        SAML           = 1 << 3
    }    
}