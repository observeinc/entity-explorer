using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsDatastream : ObsCompositeObject
    {
        public string source { get; set; }
        public string iconUrl { get; set; }

        public bool IsEnabled { get; set; }

        public long RetentionDays { get; set; } = 0;

        public List<ObsToken> Tokens { get; set; } = new List<ObsToken>(0);
        public Dictionary<string, ObsToken> AllTokensDict { get; set; }

        public ObsDataset DatastreamDataset { get; set; }
        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public override string ToString()
        {
            return String.Format(
                "ObsDatastream: {0}/{1}/{2}",
                this.name,
                this.id,
                this.ObjectType);
        }

        public ObsDatastream () {}
    
        public ObsDatastream(JObject entityObject) : base (entityObject)
        {
            this._raw = entityObject;

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            this.iconUrl = JSONHelper.getStringValueFromJToken(entityObject, "iconUrl");
            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");

            this.ObjectType = ObsCompositeObjectType.Datastream;

            string managedById = JSONHelper.getStringValueFromJToken(entityObject, "managedById");
            if (managedById.Length > 0)
            {
                if (managedById == this.id)
                {
                    this.OriginType = ObsObjectOriginType.System;
                }
                else
                {
                    this.OriginType = ObsObjectOriginType.App;
                }
            }
            else
            {
                this.OriginType = ObsObjectOriginType.User;
            }

            string state = JSONHelper.getStringValueFromJToken(entityObject, "state");
            switch (state)
            {
                case "enabled":
                    this.IsEnabled = true;
                    break;
                    
                default:
                    this.IsEnabled = false;
                    break;
            }

            JObject effectiveSettingsObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "effectiveSettings");
            if (effectiveSettingsObject != null)
            {
                JObject dataRetentionObject = (JObject)JSONHelper.getJTokenValueFromJToken(effectiveSettingsObject, "dataRetention");
                if (dataRetentionObject != null)
                {
                    this.RetentionDays = JSONHelper.getIntValueFromJToken(dataRetentionObject, "periodDays");
                }
            }
        }

        public void AddDatastreamTokens()
        {
            JObject entityObject = this._raw;

            // Parse related datasets for inputs
            JArray datastreamTokensArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "tokens");
            if (datastreamTokensArray != null)
            {
                foreach (JObject datastreamTokenObject in datastreamTokensArray)
                {
                    // Doing defensive coding here for the times when same token might be returned twice by our api
                    ObsToken token = new ObsToken(datastreamTokenObject, this);
                    if (this.Tokens.Exists(t => t.id == token.id) == false)
                    {
                        this.Tokens.Add(token);
                    }
                }
            }
            JArray datastreamPollersArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "pollers");
            if (datastreamPollersArray != null)
            {
                foreach (JObject datastreamTokenObject in datastreamPollersArray)
                {
                    ObsToken token = new ObsToken(datastreamTokenObject, this);
                    if (this.Tokens.Exists(t => t.id == token.id) == false)
                    {
                        this.Tokens.Add(token);
                    }
                }
            }
            JArray datastreamFiledropsArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "filedrops");
            if (datastreamFiledropsArray != null)
            {
                foreach (JObject datastreamTokenObject in datastreamFiledropsArray)
                {
                    ObsToken token = new ObsToken(datastreamTokenObject, this);
                    if (this.Tokens.Exists(t => t.id == token.id) == false)
                    {
                        this.Tokens.Add(token);
                    }
                }
            }
            foreach (ObsToken token in this.Tokens)
            {
                this.ExternalObjectRelationships.Add(new ObjectRelationship(String.Format("{0} to {1} as {2}", this, token, ObsObjectRelationshipType.ProvidesData), this, token, ObsObjectRelationshipType.ProvidesData));
            }
            this.AllTokensDict = this.Tokens.ToDictionary(s => s.id, s => s);
        }

        public void PopulateExternalDatasetRelationships(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;
                        
            ObsDataset relatedDataset = null;
            if (allDatasetsDict.TryGetValue(JSONHelper.getStringValueFromJToken(entityObject, "datasetId"), out relatedDataset) == true)
            {
                this.DatastreamDataset = relatedDataset;
                this.ExternalObjectRelationships.Add(new ObjectRelationship(String.Format("{0} to {1} as {2}", this, relatedDataset.ToString(), ObsObjectRelationshipType.ProvidesData), relatedDataset, this, ObsObjectRelationshipType.ProvidesData));
            }
            else
            {
                this.DatastreamDataset = new ObsDataset{id = "-1"};
            }
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