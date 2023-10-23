using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsStage : ObsObject
    {
        public string pipeline { get; set; }
        public bool visible { get; set; }
        public string type { get; set; }
        public string thumbnail { get; set; }

        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public ObsCompositeObject Parent { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsStage: {0} ({1})",
                this.name,
                this.id);
        }

        public ObsStage () {}

        public ObsStage(JObject entityObject, ObsCompositeObject parentObject)
        {
            this._raw = entityObject;
            
            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.pipeline = JSONHelper.getStringValueFromJToken(entityObject, "pipeline");
            JObject layoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "layout");
            if (layoutObject != null)
            {
                this.name = JSONHelper.getStringValueFromJToken(layoutObject, "label");
                this.thumbnail = JSONHelper.getStringValueFromJToken(layoutObject, "thumbnailUri");

                //this.appearance = JSONHelper.getStringValueFromJToken(layoutObject, "appearance");
                
                // table, timeseries, bar, circular,
                // this first part appears to always be table
                this.type = JSONHelper.getStringValueFromJToken(layoutObject, "type");
                JArray managersArray = (JArray)JSONHelper.getJTokenValueFromJToken(layoutObject, "managers");
                if (managersArray != null)
                {
                    foreach (JObject managerObject in managersArray)
                    {
                        // If there is a vis, one of the managers will be of type=Vis and would contain the type in vis.type
                        //{
                        //     "id": "gw0hrdym",
                        //     "isDisabled": false,
                        //     "type": "Vis",
                        //     "vis": {
                        //         "config": {}
                        //         ...
                        //         "type": "bar"
                        //     }
                        // }                        
                        string type = JSONHelper.getStringValueFromJToken(managerObject, "type");
                        if (type.ToLower() == "vis")
                        {
                            JObject visObject = (JObject)JSONHelper.getJTokenValueFromJToken(managerObject, "vis");
                            if (visObject != null)
                            {
                                this.type = JSONHelper.getStringValueFromJToken(visObject, "type");
                            }                            
                        }
                    }
                }
            }

            if (this.name == null || this.name.Length == 0)
            {
                this.name = string.Format("Unnamed {0}", this.id);
            }

            this.Parent = parentObject;
            if (this.Parent is ObsDataset)
            {
                this.visible = true;
            }
        }

        public void PopulateExternalDatasetAndInternalStageRelationships(Dictionary<string, ObsDataset> allDatasetsDict, Dictionary<string, ObsStage> allStagesDict)
        {
            JObject entityObject = this._raw;

            JArray inputDatasetsOrStagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "input");
            if (inputDatasetsOrStagesArray != null)
            {
                // "input": [{
                //         "inputName": "File Logs_-ix84",
                //         "inputRole": "Data",
                //         "datasetId": null,
                //         "datasetPath": null,
                //         "stageId": "stage-a4s6wuuh",
                //         "__typename": "InputDefinition"
                //     }, {
                //         "inputName": "Container Logs_-zact",
                //         "inputRole": "Data",
                //         "datasetId": null,
                //         "datasetPath": null,
                //         "stageId": "stage-jfwv9t7l",
                //         "__typename": "InputDefinition"
                //     }, 
                //     "inputName": "Telecom_41221900",
                //     "inputRole": "Data",
                //     "datasetId": "41221900",
                //     "datasetPath": null,
                //     "stageId": "",
                //     "__typename": "InputDefinition"
                // }                , {
                //         "inputName": "OpenTelemetry/Trace",
                //         "inputRole": "Reference",
                //         "datasetId": "41217828",
                //         "datasetPath": null,
                //         "stageId": "",
                //         "__typename": "InputDefinition"
                //     }
                // ]
                foreach (JObject inputDatasetObject in inputDatasetsOrStagesArray)
                {
                    string inputRole = JSONHelper.getStringValueFromJToken(inputDatasetObject, "inputRole");
                    ObsObjectRelationshipType relationshipType = ObsObjectRelationshipType.Unknown;
                    switch (inputRole)
                    {
                        case "Reference":
                            relationshipType = ObsObjectRelationshipType.Linked;
                            break;
                        
                        case "Data":
                            relationshipType = ObsObjectRelationshipType.ProvidesData;
                            break;

                        default:
                            throw new NotImplementedException(String.Format("{0} is not a known dataset relationship type", inputRole));
                    }

                    string inputName = JSONHelper.getStringValueFromJToken(inputDatasetObject, "inputName");

                    string datasetId = JSONHelper.getStringValueFromJToken(inputDatasetObject, "datasetId");
                    if (datasetId != null && datasetId.Length > 0)
                    {
                        ObsDataset relatedDataset = null;
                        if (allDatasetsDict.TryGetValue(datasetId, out relatedDataset) == true)
                        {
                            this.ExternalObjectRelationships.Add(new ObjectRelationship(inputName, this, relatedDataset, relationshipType));
                        }
                    }

                    string stageId = JSONHelper.getStringValueFromJToken(inputDatasetObject, "stageId");
                    if (stageId != null && stageId.Length > 0)
                    {
                        ObsStage relatedStage = null;
                        if (allStagesDict.TryGetValue(stageId, out relatedStage) == true)
                        {
                            this.ExternalObjectRelationships.Add(new ObjectRelationship(inputName, this, relatedStage, relationshipType));
                        }
                    }
                }
            }
        }
    }
}