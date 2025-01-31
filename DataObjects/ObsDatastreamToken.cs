using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsToken : ObsCompositeObject
    {
        public string package { get; set; }
        public string source { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime firstIngest { get; set; }
        public DateTime lastIngest { get; set; }
        public DateTime lastError { get; set; }

        public long NumStats { get; set; } = 0;
        public long NumObservations { get; set; } = 0;
        public long NumBytes { get; set; } = 0;

        public string kind { get; set; }

        public ObsCompositeObject Parent { get; set; }

        public string Errors
        {
            get
            {
                JObject statsObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "stats");
                if (statsObject != null)
                {
                    return JSONHelper.getStringValueOfObjectFromJToken(statsObject, "errors");
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public override string ToString()
        {
            return String.Format(
                "ObsToken: {0}/{1}/{2}",
                this.name,
                this.id,
                this.ObjectType);
        }

        public ObsToken () {}
    
        public ObsToken(JObject entityObject, ObsCompositeObject parentObject) : base (entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            if (this.name.Contains("/") == true)
            {
                this.package = this.name.Split('/')[0];
            }
            else
            {
                this.package = String.Empty;
            }
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");

            // Get the origin
            // managedById contains the Application ID
            string managedById = JSONHelper.getStringValueFromJToken(entityObject, "managedById");
            if (managedById.Length > 0)
            {
                this.OriginType = ObsObjectOriginType.App;
            }
            else
            {
                this.OriginType = ObsObjectOriginType.User;
            }

            this.IsEnabled = !JSONHelper.getBoolValueFromJToken(entityObject, "disabled");

            JObject statsObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "stats");
            if (statsObject != null)
            {
                this.firstIngest = JSONHelper.getDateTimeValueFromJToken(statsObject, "firstIngest");
                this.lastIngest = JSONHelper.getDateTimeValueFromJToken(statsObject, "lastIngest");
                this.lastError = JSONHelper.getDateTimeValueFromJToken(statsObject, "lastError");

                JArray observationsArray = (JArray)JSONHelper.getJTokenValueFromJToken(statsObject, "observations");
                if (observationsArray != null)
                {
                    this.NumStats = observationsArray.Count;
                    
                    foreach (JObject observationObject in observationsArray)
                    {
                        this.NumObservations += JSONHelper.getLongValueFromJToken(observationObject, "value");
                    }
                }

                JArray bytesArray = (JArray)JSONHelper.getJTokenValueFromJToken(statsObject, "volumeBytes");
                if (bytesArray != null)
                {
                    foreach (JObject observationObject in bytesArray)
                    {
                        this.NumBytes += JSONHelper.getLongValueFromJToken(observationObject, "value");
                    }
                }
            }

            string __typename = JSONHelper.getStringValueFromJToken(entityObject, "__typename");
            switch (__typename)
            {
                case "DatastreamToken":
                    this.ObjectType = ObsCompositeObjectType.Token | ObsCompositeObjectType.IngestToken;
                    this.kind = "token";
                    break;

                case "Filedrop":
                    this.ObjectType = ObsCompositeObjectType.Token | ObsCompositeObjectType.FiledropToken;
                    this.kind = "filedrop";
                    break;

                case "Poller":
                    this.ObjectType = ObsCompositeObjectType.Token | ObsCompositeObjectType.PollerToken;
                    this.kind = "poller";
                    break;

                default:
                    // ?? What could it be?
                    this.ObjectType = ObsCompositeObjectType.Token;
                    break;
            }

            this.Parent = parentObject;
        }

        public void PopulateExternalDatasetRelationships(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;
            
            throw new NotImplementedException();

            // // Parse related datasets for inputs
            // JArray inputDatasetsArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "inputs");
            // if (inputDatasetsArray != null)
            // {
            //     // DatasetInputDataset
            //     //   "inputs": [
            //     //     {
            //     //       "datasetId": "41218411",
            //     //       "inputRole": "Data",
            //     //       "__typename": "DatasetInputDataset"
            //     //     },
            //     //     {
            //     //       "datasetId": "41218643",
            //     //       "inputRole": "Data",
            //     //       "__typename": "DatasetInputDataset"
            //     //     }
            //     //   ]
            //     foreach (JObject inputDatasetObject in inputDatasetsArray)
            //     {
            //         string inputRole = JSONHelper.getStringValueFromJToken(inputDatasetObject, "inputRole");
            //         ObsObjectRelationshipType relationshipType = ObsObjectRelationshipType.Unknown;
            //         switch (inputRole)
            //         {
            //             case "Reference":
            //                 relationshipType = ObsObjectRelationshipType.Linked;
            //                 break;
                        
            //             case "Data":
            //                 relationshipType = ObsObjectRelationshipType.ProvidesData;
            //                 break;

            //             default:
            //                 throw new NotImplementedException(String.Format("{0} is not a known dataset relationship type", inputRole));
            //         }

            //         ObsDataset relatedDataset = null;
            //         if (allDatasetsDict.TryGetValue(JSONHelper.getStringValueFromJToken(inputDatasetObject, "datasetId"), out relatedDataset) == true)
            //         {
            //             this.ExternalObjectRelationships.Add(new ObjectRelationship(String.Format("{0} to {1} as {2}", relatedDataset.ToString(), this, relationshipType), this, relatedDataset, relationshipType));
            //         }
            //     }
            // }
            
            // if (this.ObjectType == ObsCompositeObjectType.MonitorSupportDataset)
            // {
            //     // TODO decide what to do 
            // }
            // else if (this.ObjectType == ObsCompositeObjectType.MetricSMADataset)
            // {
            //     // description [string]: "Metric SMA dataset generated by dataset 41236014"
            //     // I don't think we need to do anything special here, the Metric SMA datasets refer to the target dataset is in the input
            // }
        }

        public List<ObjectRelationship> GetRelationshipsOfRelated(ObsStage interestingObject)
        {
            throw new NotImplementedException();
            // return this.StageObjectRelationships.Where(r => r.RelatedObject == interestingObject).ToList();
        }

        public List<ObjectRelationship> GetRelationshipsOfRelated(ObsStage interestingObject, ObsObjectRelationshipType relationshipType)
        {
            throw new NotImplementedException();
            // return this.StageObjectRelationships.Where(r => r.RelatedObject == interestingObject && r.RelationshipType == relationshipType).ToList();
        }
    }
}