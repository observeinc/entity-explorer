using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsMonitor : ObsCompositeObject
    {
        public string kind { get; set; }
        public string package { get; set; }
        public string iconUrl { get; set; }
        
        public string comment { get; set; }

        public bool IsTemplate { get; set; }
        public bool IsEnabled { get; set; }

        public ObsStage OutputStage { get; set; }
        public List<ObsStage> Stages { get; set; } = new List<ObsStage>(8);
        public Dictionary<string, ObsStage> AllStagesDict { get; set; }

        public List<ObsParameter> Parameters { get; set; } = new List<ObsParameter>(0);
        public Dictionary<string, ObsParameter> AllParametersDict { get; set; }

        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);
        public List<ObjectRelationship> StageObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public int NumStages
        { 
            get
            {
                JObject queryObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "query");
                if (queryObject != null)
                {
                    JArray stagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(queryObject, "stages");
                    if (stagesArray != null)
                    {
                        return stagesArray.Count;
                    }
                }
                return 0;
            }
        }

        public int NumActions
        { 
            get
            {
                JArray actionsArray = (JArray)JSONHelper.getJTokenValueFromJToken(this._raw, "actions");
                if (actionsArray != null)
                {
                    return actionsArray.Count;
                }
                return 0;
            }
        }

        public string Settings
        {
            get
            {
                return JSONHelper.getStringValueOfObjectFromJToken(this._raw, "effectiveSettings");
            }
        }

        public string Notification
        {
            get
            {
                return JSONHelper.getStringValueOfObjectFromJToken(this._raw, "notificationSpec");
            }
        }

        public string ActiveMonitorInfo
        {
            get
            {
                return JSONHelper.getStringValueOfObjectFromJToken(this._raw, "activeMonitorInfo");
            }
        }

        public override string ToString()
        {
            return String.Format(
                "ObsMonitor: {0}/{1}/{2}",
                this.name,
                this.id,
                this.ObjectType);
        }

        public ObsMonitor () {}

        public ObsMonitor(JObject entityObject, AuthenticatedUser currentUser) : base (entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            if (this.name.Contains("/") == true)
            {
                this.package = this.name.Split('/')[0].Replace("(TEMPLATE) ", "");
            }
            else
            {
                this.package = String.Empty;
            }
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");
            this.iconUrl = JSONHelper.getStringValueFromJToken(entityObject, "iconUrl");

            this.IsTemplate = JSONHelper.getBoolValueFromJToken(entityObject, "isTemplate");
            this.comment = JSONHelper.getStringValueFromJToken(entityObject, "comment");

            this.ObjectType = ObsCompositeObjectType.Monitor;

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
            switch (this.package)
            {
                case "usage":
                    this.OriginType = ObsObjectOriginType.System;
                    break;

                case "aws":
                case "Azure":
                case "GCP":
                case "github":
                case "Jenkins":
                case "kubernetes":
                case "Orca":
                case "OpenTelemetry":
                case "Server":
                case "":
                default:
                    break;
            }

            JObject ruleObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "rule");
            if (ruleObject != null)
            {
                // MonitorRuleKind
                // Threshold
                // Log
                // Change
                // Facet
                // Count
                // Promote
                this.kind = JSONHelper.getStringValueFromJToken(ruleObject, "ruleKind");
                switch (this.kind)
                {
                    case "Threshold":
                        this.ObjectType = ObsCompositeObjectType.Monitor | ObsCompositeObjectType.MetricThresholdMonitor;
                        break;

                    case "Log":
                        this.ObjectType = ObsCompositeObjectType.Monitor | ObsCompositeObjectType.LogThresholdMonitor;
                        break;

                    case "Change":
                        // Deprecated in the UI. 
                        break;

                    case "Count":
                        this.ObjectType = ObsCompositeObjectType.Monitor | ObsCompositeObjectType.ResourceCountThresholdMonitor;
                        break;

                    case "Promote":
                        this.ObjectType = ObsCompositeObjectType.Monitor | ObsCompositeObjectType.PromotionMonitor;
                        break;

                    case "Facet":
                        this.ObjectType = ObsCompositeObjectType.Monitor | ObsCompositeObjectType.ResourceTextValueMonitor;
                        break;

                    default:
                        break;
                }

            }

            JObject activeMonitorInfoObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "activeMonitorInfo");
            if (activeMonitorInfoObject != null)
            {
                JObject statusInfoObject = (JObject)JSONHelper.getJTokenValueFromJToken(activeMonitorInfoObject, "statusInfo");
                if (statusInfoObject != null)
                {

                    string state = JSONHelper.getStringValueFromJToken(statusInfoObject, "status");
                    switch (state)
                    {
                        case "Monitoring":
                            this.IsEnabled = true;
                            break;

                        case "Stopped":
                            this.IsEnabled = false;
                            break;
                        default:
                            this.IsEnabled = false;
                            break;
                    }
                }
            }
        }
    
        public void AddSupportingDatasets(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;

            JObject activeMonitorInfoObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject , "activeMonitorInfo");
            if (activeMonitorInfoObject != null)
            {
                JArray supportingDatasetsArray = (JArray)JSONHelper.getJTokenValueFromJToken(activeMonitorInfoObject, "generatedDatasetIds");
                if (supportingDatasetsArray != null)
                {
                    foreach (JObject supportingDatasetObject in supportingDatasetsArray)
                    {
                        ObsDataset supportingDataset = null;
                        if (allDatasetsDict.TryGetValue(JSONHelper.getStringValueFromJToken(supportingDatasetObject, "datasetId"), out supportingDataset) == true)
                        {
                            this.ExternalObjectRelationships.Add(new ObjectRelationship(String.Format("{0} to {1} as {2} in role {3}", supportingDataset.ToString(), this, ObsObjectRelationshipType.ProvidesData, JSONHelper.getStringValueFromJToken(supportingDatasetObject, "role")), this, supportingDataset, ObsObjectRelationshipType.ProvidesData));
                        }
                    }
                }
            }
        }

        public void AddStages(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;

            JObject queryObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "query");
            if (queryObject != null)
            {
                // "query": {
                //         "outputStage": "stage-ckbbu3cc",
                //         "stages": [{
                //                 "id": "stage-x6duh0w0",
                //                 "params": null,
                //                 "pipeline": "OPAL OPAL OPAL",
                //                 "input": [{
                //                         "inputName": "CloudSRE XBP Metrics_41250084",
                //                         "inputRole": "Data",
                //                         "datasetId": "41250084",
                //                         "datasetPath": null,
                //                         "stageId": "",
                //                         "__typename": "InputDefinition"
                //                     }
                //                 ],
                //                 "__typename": "StageQuery"
                //             }

                JArray stagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(queryObject, "stages");
                if (stagesArray != null)
                {
                    // Populate the stages
                    foreach (JObject stageObject in stagesArray)
                    {
                        this.Stages.Add(new ObsStage(stageObject, this));
                    }
                    this.AllStagesDict = this.Stages.ToDictionary(s => s.id, s => s);
                    
                    // Link the stages to stages and datasets
                    foreach (ObsStage stage in this.Stages)
                    {
                        // All stages in Monitor should be marked as visible on graphs, they are never shown in UI but still
                        stage.visible = true;
                        stage.PopulateExternalDatasetInternalStageRelationships(allDatasetsDict, this.AllStagesDict, null);
                        this.StageObjectRelationships.AddRange(stage.ExternalObjectRelationships);
                    }
                }

                ObsStage outputStage = null;
                if (this.AllStagesDict.TryGetValue(JSONHelper.getStringValueFromJToken(queryObject, "outputStage"), out outputStage) == true)
                {
                    this.OutputStage = outputStage;
                }
            }
        }

        public void PopulateExternalDatasetRelationships()
        {
            List<ObjectRelationship> objectRelationshipsToDatasets = this.StageObjectRelationships.Where(r => r.RelatedObject is ObsDataset).ToList();
            foreach(ObjectRelationship relationship in objectRelationshipsToDatasets)
            {
                this.ExternalObjectRelationships.Add(new ObjectRelationship(relationship.name, this, relationship.RelatedObject, relationship.RelationshipType));
            }
            this.ExternalObjectRelationships = this.ExternalObjectRelationships.Distinct().ToList();
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