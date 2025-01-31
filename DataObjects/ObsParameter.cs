using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsParameter : ObsObject
    {
        public string dataType { get; set; }
        public string viewType { get; set; }
        public string defaultValue { get; set; }
        public string sourceType { get; set; }
        public bool allowEmpty { get; set; }
        public ObsObject SourceObject { get; set; }
        public string sourceColumn { get; set; }

        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public ObsCompositeObject Parent { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsParameter: {0} ({1}) using {2}, datatype {3} in {4} [{5}]",
                this.name,
                this.viewType,
                this.sourceType,
                this.dataType,
                this.Parent.name,
                this.Parent.id);
        }

        public ObsParameter () {}

        public ObsParameter(JObject entityObject, ObsCompositeObject parentObject, Dictionary<string, ObsDataset> allDatasetsDict, Dictionary<string, ObsStage> allStagesDict)
        {
            this._raw = entityObject;

            // ID is the name of the variable
            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            this.viewType = JSONHelper.getStringValueFromJToken(entityObject, "viewType");
            JToken sourceTypeToken = JSONHelper.getJTokenValueFromJToken(entityObject, "source");
            if (sourceTypeToken != null)
            {
                if (sourceTypeToken.Type == JTokenType.Object)
                {
                    this.sourceType = JSONHelper.getStringValueFromJToken(sourceTypeToken, "type");
                }
                else if (sourceTypeToken.Type == JTokenType.String)
                {
                    this.sourceType = JSONHelper.getStringValueFromJToken(entityObject, "source");
                }
            }
            if (this.sourceType == null || this.sourceType.Length == 0) sourceType = "UserInput";
            this.allowEmpty = JSONHelper.getBoolValueFromJToken(entityObject, "allowEmpty");
            
            JObject valueKindObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "valueKind");
            JObject defaultValueObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "defaultValue");
            // Assume that this is just a string value
            this.dataType = "STRING";
            if (defaultValueObject != null)
            {
                this.dataType = JSONHelper.getStringValueFromJToken(valueKindObject, "type");
            }

            switch (this.viewType)
            {
                // Resource instance menu (from Dataset mapped to column)
                case "resource-input":
                    this.defaultValue = JSONHelper.getStringValueOfObjectFromJToken(defaultValueObject, "link");
                    this.sourceType = "Dataset";
                    if (valueKindObject != null)
                    {
                        string datasetId_resource_input = JSONHelper.getStringValueFromJToken(valueKindObject, "keyForDatasetId");
                        ObsDataset obsDataset_resource_input = null;
                        if (allDatasetsDict.TryGetValue(datasetId_resource_input, out obsDataset_resource_input) == true)
                        {
                            this.SourceObject = obsDataset_resource_input;
                            this.ExternalObjectRelationships.Add(new ObjectRelationship(obsDataset_resource_input.name, this, obsDataset_resource_input, ObsObjectRelationshipType.ProvidesData));
                        }
                        JObject linkObject = (JObject)JSONHelper.getJTokenValueFromJToken(defaultValueObject, "link");
                        if (linkObject != null)
                        {
                            this.sourceColumn = JSONHelper.getStringValueOfObjectFromJToken(linkObject, "primaryKeyValue");
                        }
                    }

                    break;

                // Filtered dataset parameters
                case "input":
                    this.defaultValue = JSONHelper.getStringValueOfObjectFromJToken(defaultValueObject, "datasetref");
                    this.sourceType = "Dataset";
                    string datasetId_input = JSONHelper.getStringValueFromJToken(entityObject, "datasetId");
                    ObsDataset obsDataset_input = null;
                    if (allDatasetsDict.TryGetValue(datasetId_input, out obsDataset_input) == true)
                    {
                        this.SourceObject = obsDataset_input;
                        this.ExternalObjectRelationships.Add(new ObjectRelationship(obsDataset_input.name, this, obsDataset_input, ObsObjectRelationshipType.ProvidesData));
                    }

                    break;

                // Dropdown (from Dataset, Stage or custom values)
                case "single-select":
                    this.defaultValue = JSONHelper.getStringValueFromJToken(defaultValueObject, this.dataType.ToLower());
                    
                    switch (this.sourceType)
                    {
                        case "Dataset":
                            string datasetId = JSONHelper.getStringValueFromJToken(entityObject, "sourceDatasetId");
                            ObsDataset obsDataset = null;
                            if (allDatasetsDict.TryGetValue(datasetId, out obsDataset) == true)
                            {
                                this.SourceObject = obsDataset;
                                this.ExternalObjectRelationships.Add(new ObjectRelationship(obsDataset.name, this, obsDataset, ObsObjectRelationshipType.ProvidesData));
                            }
                            this.sourceColumn = JSONHelper.getStringValueFromJToken(entityObject, "sourceColumnId");
                            
                            break;
                        
                        case "Stage":
                            // Getting data for dropdown from another stage
                            string stageId = JSONHelper.getStringValueFromJToken(entityObject, "sourceStageId");
                            ObsStage obsStage = null;
                            if (allStagesDict.TryGetValue(stageId, out obsStage) == true)
                            {
                                this.SourceObject = obsStage;
                                this.ExternalObjectRelationships.Add(new ObjectRelationship(obsStage.name, this, obsStage, ObsObjectRelationshipType.ProvidesData));
                            }
                            this.sourceColumn = JSONHelper.getStringValueFromJToken(entityObject, "sourceColumnId");

                            break;

                        case "CustomData":
                            // User provided name/value pairs
                            this.sourceColumn = JSONHelper.getStringValueOfObjectFromJToken(entityObject, "sourceCustomData");
                            
                            break;

                        default:
                            break;
                    }


                    break;

                // Textbox with string
                case "text":
                    this.defaultValue = JSONHelper.getStringValueFromJToken(defaultValueObject, this.dataType.ToLower());
                    break;

                // Textbox with number
                case "numeric":
                    this.defaultValue = JSONHelper.getStringValueFromJToken(defaultValueObject, this.dataType.ToLower());
                    break;

                default:
                    break;
            }

            this.Parent = parentObject;
        }

    }
}