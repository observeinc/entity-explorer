namespace Observe.EntityExplorer.DataObjects
{
    public enum ObsObjectRelationshipType
    {
        Unknown,
        // This stage/dataset depends on the related dataset for data, thus deriving from it
        ProvidesData,
        // This stage/dataset links to the related dataset (set_link)
        Linked,
        // This parameter is used by Stage OPAL code, likely as filter
        ProvidesParameter
    }
}