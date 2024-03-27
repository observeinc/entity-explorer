using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsDataset : ObsCompositeObject
    {
        public string kind { get; set; }
        public string path { get; set; }
        public string package { get; set; }
        public string source { get; set; }
        public string iconUrl { get; set; }
        public string version { get; set; }

        public string validFromField { get; set; }
        public string validToField { get; set; }
        public string labelField { get; set; }

        public List<ObsFieldDefinition> Fields { get; set; } = new List<ObsFieldDefinition>(16);
        public List<ObsFieldDefinition> PrimaryKeys { get; set; } = new List<ObsFieldDefinition>(0);
        // public List<ObsFieldDefinition> Keys { get; set; } = new List<ObsFieldDefinition>(0);
        public List<ObsRelatedKey> RelatedKeys { get; set; } = new List<ObsRelatedKey>(0);
        public List<ObsRelatedKey> ForeignKeys { get; set; } = new List<ObsRelatedKey>(0);

        public ObsStage OutputStage { get; set; }
        public List<ObsStage> Stages { get; set; } = new List<ObsStage>(0);
        public Dictionary<string, ObsStage> AllStagesDict { get; set; }

        public ObsAccelerationInfo Acceleration { get; set; }

        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);
        public List<ObjectRelationship> StageObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public ObsCreditsTransform Transform1H { get; set; } = new ObsCreditsTransform() {Credits = 0};
        public ObsCreditsTransform Transform1D { get; set; } = new ObsCreditsTransform() {Credits = 0};
        public ObsCreditsTransform Transform1W { get; set; } = new ObsCreditsTransform() {Credits = 0};
        public ObsCreditsTransform Transform1M { get; set; } = new ObsCreditsTransform() {Credits = 0};

        public ObsCreditsQuery Query1H { get; set; } = new ObsCreditsQuery() {Credits = 0};
        public ObsCreditsQuery Query1D { get; set; } = new ObsCreditsQuery() {Credits = 0};
        public ObsCreditsQuery Query1W { get; set; } = new ObsCreditsQuery() {Credits = 0};
        public List<ObsCreditsQuery> Query1HUsers { get; set; } = new List<ObsCreditsQuery>();
        public List<ObsCreditsQuery> Query1DUsers { get; set; } = new List<ObsCreditsQuery>();
        public List<ObsCreditsQuery> Query1WUsers { get; set; } = new List<ObsCreditsQuery>();

        public string Settings
        {
            get
            {
                return JSONHelper.getStringValueOfObjectFromJToken(this._raw, "effectiveSettings");
            }
        }

        public string AccelerationInfo
        {
            get
            {
                return JSONHelper.getStringValueOfObjectFromJToken(this._raw, "accelerationInfo");
            }
        }

        public override string ToString()
        {
            return String.Format(
                "ObsDataset: {0}/{1}/{2}",
                this.name,
                this.id,
                this.ObjectType);
        }

        public ObsDataset () {}

        public ObsDataset(JObject entityObject, AuthenticatedUser currentUser) : base (entityObject)
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
            this.kind = JSONHelper.getStringValueFromJToken(entityObject, "kind");
            this.path = JSONHelper.getStringValueFromJToken(entityObject, "path");
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");
            this.source = JSONHelper.getStringValueFromJToken(entityObject, "source");
            this.iconUrl = JSONHelper.getStringValueFromJToken(entityObject, "iconUrl");
            this.version = JSONHelper.getStringValueFromJToken(entityObject, "version");

            this.validFromField = JSONHelper.getStringValueFromJToken(entityObject, "validFromField");
            this.validToField = JSONHelper.getStringValueFromJToken(entityObject, "validToField");
            this.labelField = JSONHelper.getStringValueFromJToken(entityObject, "labelField");

            // Categorize the datasets with our handy dandy enums
            switch (this.source)
            {
                case "app/":
                    this.OriginType = ObsObjectOriginType.App;
                    break;

                case "api/web":
                    this.OriginType = ObsObjectOriginType.User;
                    break;

                case "external/":
                    this.OriginType = ObsObjectOriginType.External;
                    break;

                default:
                    this.OriginType = ObsObjectOriginType.Unknown;
                    break;
            }

            switch (this.kind)
            {
                case "Event":
                    this.ObjectType = ObsCompositeObjectType.Dataset | ObsCompositeObjectType.EventDataset;
                    break;

                case "Resource":
                    this.ObjectType = ObsCompositeObjectType.Dataset | ObsCompositeObjectType.ResourceDataset;
                    break;

                case "Interval":
                    this.ObjectType = ObsCompositeObjectType.Dataset | ObsCompositeObjectType.IntervalDataset;
                    break;
                
                default:
                    this.ObjectType = ObsCompositeObjectType.Dataset;
                    break;
            }
            if (this.source.StartsWith("system/datastream") == true || this.source.StartsWith("system/Observe Bootstrap") == true)
            {
                this.ObjectType = this.ObjectType | ObsCompositeObjectType.DatastreamDataset;
                this.OriginType = ObsObjectOriginType.DataStream;
            }
            else if (this.source.StartsWith("system/metric-sma") == true)
            {
                this.ObjectType = this.ObjectType | ObsCompositeObjectType.MetricSMADataset;
                this.OriginType = ObsObjectOriginType.System;
            }
            else if (this.source.StartsWith("terraform/") == true)
            {
                this.OriginType = ObsObjectOriginType.Terraform;
            }            
            if (this.package == "monitor")
            {
                this.ObjectType = this.ObjectType | ObsCompositeObjectType.MonitorSupportDataset;
                this.OriginType = ObsObjectOriginType.System;
            }
            
            JArray interfacesArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "interfaces");
            if (interfacesArray != null)
            {
                foreach (JObject interfaceObject in interfacesArray)
                {
                    string interfacePath = JSONHelper.getStringValueFromJToken(interfaceObject, "path");
                    switch (interfacePath)
                    {
                        case "metric":
                            this.ObjectType = this.ObjectType | ObsCompositeObjectType.InterfaceMetricDataset;
                            break;

                        case "log":
                            this.ObjectType = this.ObjectType | ObsCompositeObjectType.InterfaceLogDataset;
                            break;
                        
                        case "action_notification":
                        case "materialized_notification":
                            // These are in the background autocreated monitor datasets
                            // ObsCompositeObjectType.MonitorSupportDataset, already taken care of via package=monitor 
                            break;

                        default:
                            break;
                    }
                }
            }

            // Parse columns/fields
            JObject typeDefObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "typedef");
            if (typeDefObject != null)
            {
                JObject defObject = (JObject)JSONHelper.getJTokenValueFromJToken(typeDefObject, "def");
                if (defObject != null)
                {
                    // "fields": [
                    //     {
                    //         "name": "host",
                    //         "type": {
                    //         "rep": "string",
                    //         "nullable": true,
                    //         "def": null,
                    //         "__typename": "ObjectFieldType"
                    //         },
                    //         "isEnum": true,
                    //         "isSearchable": true,
                    //         "isHidden": false,
                    //         "isConst": true,
                    //         "isMetric": false,
                    //         "__typename": "ObjectFieldDef"
                    //     }                    
                    JArray fieldsArray = (JArray)JSONHelper.getJTokenValueFromJToken(defObject, "fields");
                    if (fieldsArray != null)
                    {
                        this.Fields = new List<ObsFieldDefinition>(fieldsArray.Count);
                        foreach (JObject fieldObject in fieldsArray)
                        {
                            this.Fields.Add(new ObsFieldDefinition(fieldObject, this));
                        }
                    }
                }
            }

            // Parse primary keys
            JArray primaryKeysArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "primaryKey");
            if (primaryKeysArray != null)
            {
                // "primaryKey": [
                //      "host",
                //      "datacenter"
                //  ]
                this.PrimaryKeys = new List<ObsFieldDefinition>(primaryKeysArray.Count);
                foreach (JToken primaryKeyToken in primaryKeysArray)
                {
                    ObsFieldDefinition fieldDefinitionForPrimaryKey = this.Fields.Where(f => f.name == primaryKeyToken.ToString()).FirstOrDefault();
                    if (fieldDefinitionForPrimaryKey != null)
                    {
                        this.PrimaryKeys.Add(fieldDefinitionForPrimaryKey);
                    }
                }
            }

            // Parse keys
            // TODO Figure this out, it is an array of an array
            // JArray keysArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "keys");
            // if (keysArray != null)
            // {
            //     //   "keys": [
            //     //     [
            //     //       "host",
            //     //       "datacenter",
            //     //       "ip_address"
            //     //     ]
            //     //   ],
            //     this.Keys = new List<FieldDefinition>(keysArray.Count);
            //     foreach (JToken keyToken in keysArray)
            //     {
            //         FieldDefinition fieldDefinitionForKey = this.Fields.Where(f => f.name == keyToken.ToString()).FirstOrDefault();
            //         if (fieldDefinitionForKey != null)
            //         {
            //             this.Keys.Add(fieldDefinitionForKey);
            //         }
            //     }
            // }
        }

        public void AddRelatedKeys(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;
            
            // Parse related keys with pointers to other datasets
            JArray relatedKeysArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "relatedKeys");
            if (relatedKeysArray != null)
            {
                //   "relatedKeys": [
                //     {
                //       "targetDataset": "41251154",
                //       "srcFields": [
                //         "environment",
                //         "company",
                //         "tracker_id"
                //       ],
                //       "dstFields": [
                //         "environment",
                //         "company",
                //         "tracker_id"
                //       ],
                //       "label": "Tracker",
                //       "__typename": "RelatedKey"
                //     }
                //   ]                
                this.RelatedKeys = new List<ObsRelatedKey>(relatedKeysArray.Count);
                foreach (JObject relatedKeyObject in relatedKeysArray)
                {
                    ObsDataset targetDataset = null;
                    if (allDatasetsDict.TryGetValue(JSONHelper.getStringValueFromJToken(relatedKeyObject, "targetDataset"), out targetDataset) == true)
                    {
                        this.RelatedKeys.Add(new ObsRelatedKey(relatedKeyObject, this, targetDataset));
                    }
                }
            }
        }

        public void AddForeignKeys(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;
            
            // Parse foreign keys with pointers to other datasets
            JArray foreignKeysArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "foreignKeys");
            if (foreignKeysArray != null)
            {
                //   "foreignKeys": [
                //     {
                //       "targetDataset": "41218650",
                //       "srcFields": [
                //         "host",
                //         "datacenter"
                //       ],
                //       "dstFields": [
                //         "host",
                //         "datacenter"
                //       ],
                //       "id": "Host",
                //       "targetStageLabel": null,
                //       "targetLabelFieldName": "host",
                //       "__typename": "ForeignKey"
                //     },               
                this.ForeignKeys = new List<ObsRelatedKey>(foreignKeysArray.Count);
                foreach (JObject relatedKeyObject in foreignKeysArray)
                {
                    ObsDataset targetDataset = null;
                    if (allDatasetsDict.TryGetValue(JSONHelper.getStringValueFromJToken(relatedKeyObject, "targetDataset"), out targetDataset) == true)
                    {
                        this.ForeignKeys.Add(new ObsRelatedKey(relatedKeyObject, this, targetDataset));
                    }
                }
            }
        }        
    
        public void AddStages(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;

            JObject transformObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "transform");
            if (transformObject != null)
            {
                JObject currentTransformObject = (JObject)JSONHelper.getJTokenValueFromJToken(transformObject, "current");
                if (currentTransformObject != null)
                {
                    JObject queryObject = (JObject)JSONHelper.getJTokenValueFromJToken(currentTransformObject, "query");
                    if (queryObject != null)
                    {
                        // "query": {
                        // "outputStage": "stage-ixmo8ezy",
                        // "stages": [{
                        //         "id": "stage-a4s6wuuh",
                        //         "params": {},
                        //         "pipeline": "// OPAL OPAL OPAL",
                        //         "input": [{
                        //                 "inputName": "Server/Fluentbit Logs",
                        //                 "inputRole": "Data",
                        //                 "datasetId": "41218643",
                        //                 "datasetPath": null,
                        //                 "stageId": "",
                        //                 "__typename": "InputDefinition"
                        //             }
                        //         ],
                        //         "__typename": "StageQuery"
                        //     }

                        JArray stagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(queryObject, "stages");
                        if (stagesArray != null)
                        {
                            // Populate the stages
                            foreach (JObject stageObject in stagesArray)
                            {
                                this.Stages.Add(new ObsStage(stageObject, this));
                            }
                            this.AllStagesDict = this.Stages.ToDictionary(s => s.id, s => s);
                        }

                        // Link the stages to stages and datasets
                        foreach (ObsStage stage in this.Stages)
                        {
                            // All stages in Dataset should be marked as visible
                            stage.visible = true;
                            stage.PopulateExternalDatasetInternalStageRelationships(allDatasetsDict, this.AllStagesDict, null);
                            this.StageObjectRelationships.AddRange(stage.ExternalObjectRelationships);
                        }

                        ObsStage outputStage = null;
                        if (this.AllStagesDict.TryGetValue(JSONHelper.getStringValueFromJToken(queryObject, "outputStage"), out outputStage) == true)
                        {
                            this.OutputStage = outputStage;
                        }
                    }
                }
            }
        }

        public void PopulateExternalDatasetRelationships(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;
            
            // Parse related datasets for inputs
            JArray inputDatasetsArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "inputs");
            if (inputDatasetsArray != null)
            {
                // DatasetInputDataset
                //   "inputs": [
                //     {
                //       "datasetId": "41218411",
                //       "inputRole": "Data",
                //       "__typename": "DatasetInputDataset"
                //     },
                //     {
                //       "datasetId": "41218643",
                //       "inputRole": "Data",
                //       "__typename": "DatasetInputDataset"
                //     }
                //   ]
                foreach (JObject inputDatasetObject in inputDatasetsArray)
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

                    ObsDataset relatedDataset = null;
                    if (allDatasetsDict.TryGetValue(JSONHelper.getStringValueFromJToken(inputDatasetObject, "datasetId"), out relatedDataset) == true)
                    {
                        this.ExternalObjectRelationships.Add(new ObjectRelationship(String.Format("{0} to {1} as {2}", relatedDataset.ToString(), this, relationshipType), this, relatedDataset, relationshipType));
                    }
                }
            }
            
            if (this.ObjectType == ObsCompositeObjectType.MonitorSupportDataset)
            {
                // TODO decide what to do 
            }
            else if (this.ObjectType == ObsCompositeObjectType.MetricSMADataset)
            {
                // description [string]: "Metric SMA dataset generated by dataset 41236014"
                // I don't think we need to do anything special here, the Metric SMA datasets refer to the target dataset is in the input
            }
        }

        public void AddAccelerationInfo(Dictionary<string, ObsDataset> allDatasetsDict, Dictionary<string, ObsMonitor> allMonitorsDict)
        {
            JObject entityObject = this._raw;

            JObject accelerationInfoObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "accelerationInfo");
            if (accelerationInfoObject != null)
            {
                this.Acceleration = new ObsAccelerationInfo(accelerationInfoObject, this, allDatasetsDict, allMonitorsDict);
            }
        }

        public List<ObjectRelationship> GetRelationshipsOfRelated(ObsStage interestingObject)
        {
            return this.StageObjectRelationships.Where(r => r.RelatedObject == interestingObject).ToList();
        }

        public List<ObjectRelationship> GetRelationshipsOfRelated(ObsStage interestingObject, ObsObjectRelationshipType relationshipType)
        {
            return this.StageObjectRelationships.Where(r => r.RelatedObject == interestingObject && r.RelationshipType == relationshipType).ToList();
        }
    }
}