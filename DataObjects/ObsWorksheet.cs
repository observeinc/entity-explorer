using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsWorksheet : ObsCompositeObject
    {
        public string package { get; set; }
        public string iconUrl { get; set; }

        public List<ObsStage> Stages { get; set; } = new List<ObsStage>(8);
        public Dictionary<string, ObsStage> AllStagesDict { get; set; }

        public List<ObsParameter> Parameters { get; set; } = new List<ObsParameter>(0);
        public Dictionary<string, ObsParameter> AllParametersDict { get; set; }

        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);
        public List<ObjectRelationship> StageObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public long NumStages
        { 
            get
            {
                JArray stagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(this._raw, "stages");
                if (stagesArray != null)
                {
                    return stagesArray.Count;
                }
                return 0;
            }
        }

        public long NumParameters
        { 
            get
            {
                JArray parametersArray = (JArray)JSONHelper.getJTokenValueFromJToken(this._raw, "parameters");
                if (parametersArray != null)
                {
                    return parametersArray.Count;
                }
                return 0;
            }
        }

        public override string ToString()
        {
            return String.Format(
                "ObsWorksheet: {0}/{1}/{2}",
                this.name,
                this.id,
                this.ObjectType);
        }

        public ObsWorksheet () {}

        public ObsWorksheet(JObject entityObject) : base (entityObject)
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
            this.iconUrl = JSONHelper.getStringValueFromJToken(entityObject, "iconUrl");

            this.ObjectType = ObsCompositeObjectType.Worksheet;

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
        }

        public void AddStagesAndParameters(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;

            JObject layoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "layout");

            // First do the stages
            JArray stagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "stages");
            if (stagesArray != null)
            {
                // Populate the stages
                foreach (JObject stageObject in stagesArray)
                {
                    ObsStage stage = new ObsStage(stageObject, this);
                    
                    // Check the stage visibility
                    if (layoutObject != null)
                    {
                        string appearance = JSONHelper.getStringValueFromJToken(layoutObject, "appearance");
                        if (appearance == "VISIBLE")
                        {
                            stage.visible = true;
                        }
                        else if (appearance == "COLLAPSED")
                        {
                            stage.visible = false;
                        }
                    }

                    this.Stages.Add(stage);
                }
                this.AllStagesDict = this.Stages.ToDictionary(s => s.id, s => s);

            }

            // Let's do the parameters
            if (layoutObject != null)
            {
                JArray parametersArray = (JArray)JSONHelper.getJTokenValueFromJToken(layoutObject, "parameters");
                if (parametersArray != null)
                {
                    // Populate the parameters
                    foreach (JObject parameterObject in parametersArray)
                    {
                        this.Parameters.Add(new ObsParameter(parameterObject, this, allDatasetsDict, this.AllStagesDict));
                    }
                    this.AllParametersDict = this.Parameters.ToDictionary(p => p.id, p => p);
                }
            }

            // Link the stages to stages, parent datasets and parameters they may use
            foreach (ObsStage stage in this.Stages)
            {
                // All stages in Worksheet should be marked as visible
                stage.visible = true;                
                // Stage to stage
                // Stage to dataset
                stage.PopulateExternalDatasetInternalStageRelationships(allDatasetsDict, this.AllStagesDict, this.Parameters);
                // Which stages use this parameter
                stage.PopulateParametersRelationships(this.Parameters);
                this.StageObjectRelationships.AddRange(stage.ExternalObjectRelationships);
            }

            // Link the parameters to stages and parent datasets
            foreach (ObsParameter parameter in this.Parameters)
            {
                this.StageObjectRelationships.AddRange(parameter.ExternalObjectRelationships);
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