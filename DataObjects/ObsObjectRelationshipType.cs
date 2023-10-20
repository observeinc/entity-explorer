namespace Observe.EntityExplorer.DataObjects
{
    public enum ObsObjectRelationshipType
    {
        Unknown,
        // This stage/dataset depends on the related dataset for data, thus deriving from it
        ProvidesData,
        // This stage/dataset links to the related dataset (set_link)
        Linked,
        // This dataset/worksheet/dashboard has a parameter sourced from related dataset/stage
        Provides_Parameter,
        // This monitor depends on related dataset
        Is_Dependent_On
    }
}