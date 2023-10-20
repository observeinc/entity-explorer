using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsRelatedKey : ObsObject
    {
        public ObsCompositeObject SourceDataset { get; set; }
        public ObsCompositeObject TargetDataset { get; set; }

        public List<ObsFieldDefinition> SourceKeys { get; set; } = new List<ObsFieldDefinition>(0);
        public List<ObsFieldDefinition> TargetKeys { get; set; } = new List<ObsFieldDefinition>(0);

        public ObsCompositeObject Parent { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsRelatedKey: {0} between {1} and {2}",
                this.name,
                this.SourceDataset,
                this.TargetDataset);
        }

        public ObsRelatedKey () {}

        public ObsRelatedKey(JObject entityObject, ObsCompositeObject sourceDataset, ObsCompositeObject targetDataset)
        {
            this._raw = entityObject;

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
            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "label");
            if (this.name == String.Empty)
            {
                this.name = this.id;
            } 

            this.SourceDataset = sourceDataset;
            this.TargetDataset = targetDataset;

            if (this.SourceDataset != null && this.SourceDataset is ObsDataset)
            {
                JArray keysArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "srcFields");
                if (keysArray != null)
                {
                    this.SourceKeys = new List<ObsFieldDefinition>(keysArray.Count);
                    foreach (JToken keyToken in keysArray)
                    {
                        ObsFieldDefinition fieldDefinitionForKey = ((ObsDataset)this.SourceDataset).Fields.Where(f => f.name == keyToken.ToString()).FirstOrDefault();
                        if (fieldDefinitionForKey != null)
                        {
                            this.SourceKeys.Add(fieldDefinitionForKey);
                        }
                    }
                }
            }

            if (this.TargetDataset != null && this.TargetDataset is ObsDataset)
            {
                JArray keysArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "dstFields");
                if (keysArray != null)
                {
                    this.TargetKeys = new List<ObsFieldDefinition>(keysArray.Count);
                    foreach (JToken keyToken in keysArray)
                    {
                        ObsFieldDefinition fieldDefinitionForKey = ((ObsDataset)this.TargetDataset).Fields.Where(f => f.name == keyToken.ToString()).FirstOrDefault();
                        if (fieldDefinitionForKey != null)
                        {
                            this.TargetKeys.Add(fieldDefinitionForKey);
                        }
                    }
                }
            }

            this.Parent = sourceDataset;
        }
    }
}