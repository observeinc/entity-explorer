using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsDashboard : ObsCompositeObject
    {
        public string package { get; set; }
        public string iconUrl { get; set; }

        public List<ObsStage> Stages { get; set; } = new List<ObsStage>(0);
        public Dictionary<string, ObsStage> AllStagesDict { get; set; }

        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);
        public List<ObjectRelationship> StageObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public int NumStages
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

        public int NumSections
        { 
            get
            {
                JObject layoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "layout");
                if (layoutObject != null)
                {
                    JObject gridLayoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(layoutObject, "gridLayout");
                    if (gridLayoutObject != null)
                    {
                        JArray sectionsArray = (JArray)JSONHelper.getJTokenValueFromJToken(gridLayoutObject, "sections");
                        if (sectionsArray != null)
                        {
                            return sectionsArray.Count;
                        }
                    }
                }
                return 0;
            }
        }

        public int NumWidgets
        { 
            get
            {
                int numWidgets = 0;
                JObject layoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "layout");
                if (layoutObject != null)
                {
                    JObject gridLayoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(layoutObject, "gridLayout");
                    if (gridLayoutObject != null)
                    {
                        JArray sectionsArray = (JArray)JSONHelper.getJTokenValueFromJToken(gridLayoutObject, "sections");
                        if (sectionsArray != null)
                        {
                            foreach (JObject sectionObject in sectionsArray)
                            {
                                JArray widgetsArray = (JArray)JSONHelper.getJTokenValueFromJToken(sectionObject, "items");
                                if (widgetsArray != null)
                                {
                                    numWidgets = numWidgets + widgetsArray.Count;
                                }
                            }
                        }
                    }
                }
                return numWidgets;
            }
        }

        public int NumParameters
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
                "ObsDashboard: {0}/{1}/{2}",
                this.name,
                this.id,
                this.ObjectType);
        }

        public ObsDashboard () {}

        public ObsDashboard(JObject entityObject, AuthenticatedUser currentUser) : base (entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            if (this.name.Contains("/") == true)
            {
                this.package = this.name.Split('/')[0];
            }
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");
            this.iconUrl = JSONHelper.getStringValueFromJToken(entityObject, "iconUrl");

            this.ObjectType = ObsCompositeObjectType.Dashboard;

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

        public void AddStages(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;

            JArray stagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "stages");
            if (stagesArray != null)
            {
                // Populate the stages
                foreach (JObject stageObject in stagesArray)
                {
                    this.Stages.Add(new ObsStage(stageObject, this));
                }
                this.AllStagesDict = this.Stages.ToDictionary(s => s.id, s => s);
                
                // Check the stage visibility
                // layout.sections[].items[].card.stageId are all visible
                JObject layoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "layout");
                if (layoutObject != null)
                {
                    JObject gridLayoutObject = (JObject)JSONHelper.getJTokenValueFromJToken(layoutObject, "gridLayout");
                    if (gridLayoutObject != null)
                    {
                        JArray sectionsArray = (JArray)JSONHelper.getJTokenValueFromJToken(gridLayoutObject, "sections");
                        if (sectionsArray != null)
                        {
                            foreach (JObject sectionObject in sectionsArray)
                            {
                                JArray widgetsArray = (JArray)JSONHelper.getJTokenValueFromJToken(sectionObject, "items");
                                if (widgetsArray != null)
                                {
                                    foreach (JObject widgetsObject in widgetsArray)
                                    {
                                        JObject cardObject = (JObject)JSONHelper.getJTokenValueFromJToken(widgetsObject, "card");
                                        if (cardObject != null)
                                        {
                                            // when the stageId in the card/widget is present, that means it is displayed on the dashboard
                                            string stageId = JSONHelper.getStringValueFromJToken(cardObject, "stageId");
                                            if (stageId != null && stageId.Length > 0)
                                            {
                                                ObsStage widgetStage = null;
                                                if (this.AllStagesDict.TryGetValue(stageId, out widgetStage))
                                                {
                                                    widgetStage.visible = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Link the stages to stages and datasets
                foreach (ObsStage stage in this.Stages)
                {
                    stage.PopulateExternalDatasetAndInternalStageRelationships(allDatasetsDict, this.AllStagesDict);
                    this.StageObjectRelationships.AddRange(stage.ExternalObjectRelationships);
                }
            }
        }

        public void PopulateExternalDatasetRelationships(Dictionary<string, ObsDataset> allDatasetsDict)
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