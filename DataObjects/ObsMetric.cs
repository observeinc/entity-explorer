using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsMetric : ObsObject
    {
        public string datasetId { get; set; } = String.Empty;
        public string datasetPackage { get; set; } = String.Empty;
        public string datasetName { get; set; } = String.Empty;
        public string type { get; set; }
        public string unit { get; set; }
        public string rollup { get; set; }
        public string aggregate { get; set; }
        public TimeSpan interval { get; set; }
        public TimeSpan suggestedBucketSize { get; set; }
        public bool userDefined { get; set; }
        public string state { get; set; }

        public long cardinality { get; set; }
        public long numPoints { get; set; }
        public TimeSpan discoveredInterval { get; set; }
        public TimeSpan discoveredIntervalStdDev { get; set; }
        public DateTime lastReported { get; set; }

        public ObsCompositeObject Parent { get; set; }

        public string LinkLabels
        {
            get
            {
                JObject metricObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "metric");
                if (metricObject != null)
                {
                    JObject heuristicsObject = (JObject)JSONHelper.getJTokenValueFromJToken(metricObject, "heuristics");
                    if (heuristicsObject != null)
                    {
                        return JSONHelper.getStringValueOfObjectFromJToken(heuristicsObject, "validLinkLabels");
                    }
                }
                return String.Empty;
            }
        }

        public string Tags
        {
            get
            {
                JObject metricObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "metric");
                if (metricObject != null)
                {
                    JObject heuristicsObject = (JObject)JSONHelper.getJTokenValueFromJToken(metricObject, "heuristics");
                    if (heuristicsObject != null)
                    {
                        return JSONHelper.getStringValueOfObjectFromJToken(heuristicsObject, "tags");
                    }
                }
                return String.Empty;
            }
        }

        public long NumLinkLabels
        { 
            get
            {
                JObject metricObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "metric");
                if (metricObject != null)
                {
                    JObject heuristicsObject = (JObject)JSONHelper.getJTokenValueFromJToken(metricObject, "heuristics");
                    if (heuristicsObject != null)
                    {
                        JArray linkLabelsArray = (JArray)JSONHelper.getJTokenValueFromJToken(heuristicsObject, "validLinkLabels");
                        if (linkLabelsArray != null)
                        {
                            return linkLabelsArray.Count;
                        }
                    }
                }
                return 0;
            }
        }

        public long NumTags
        { 
            get
            {
                JObject metricObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "metric");
                if (metricObject != null)
                {
                    JObject heuristicsObject = (JObject)JSONHelper.getJTokenValueFromJToken(metricObject, "heuristics");
                    if (heuristicsObject != null)
                    {
                        JArray tagsArray = (JArray)JSONHelper.getJTokenValueFromJToken(heuristicsObject, "tags");
                        if (tagsArray != null)
                        {
                            return tagsArray.Count;
                        }
                    }
                }

                return 0;
            }
        }

        public override string ToString()
        {
            return String.Format(
                "ObsMetric: {0}/{1}",
                this.id,
                this.type);
        }

        public ObsMetric () {}

        public ObsMetric(JObject entityObject)
        {
            this._raw = entityObject;

            this.datasetId = JSONHelper.getStringValueFromJToken(entityObject, "datasetId");
            
            JObject metricObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "metric");
            if (metricObject != null)
            {
                this.name = JSONHelper.getStringValueFromJToken(metricObject, "name");
                this.type = JSONHelper.getStringValueFromJToken(metricObject, "type");
                this.unit = JSONHelper.getStringValueFromJToken(metricObject, "unit");
                this.description = JSONHelper.getStringValueFromJToken(metricObject, "description");
                this.rollup = JSONHelper.getStringValueFromJToken(metricObject, "rollup");
                this.aggregate = JSONHelper.getStringValueFromJToken(metricObject, "aggregate");
                this.state = JSONHelper.getStringValueFromJToken(metricObject, "state");

                this.interval = new TimeSpan(JSONHelper.getLongValueFromJToken(metricObject, "interval") / 100);
                this.suggestedBucketSize = new TimeSpan(JSONHelper.getLongValueFromJToken(metricObject, "suggestedBucketSize") / 100);

                this.userDefined = JSONHelper.getBoolValueFromJToken(metricObject, "userDefined");

                JObject heuristicsObject = (JObject)JSONHelper.getJTokenValueFromJToken(metricObject, "heuristics");
                if (heuristicsObject != null)
                {
                    this.cardinality = JSONHelper.getLongValueFromJToken(heuristicsObject, "cardinality");
                    this.numPoints = JSONHelper.getLongValueFromJToken(heuristicsObject, "numOfPoints");

                    this.discoveredInterval = new TimeSpan(JSONHelper.getLongValueFromJToken(heuristicsObject, "interval") / 100);
                    this.discoveredIntervalStdDev = new TimeSpan(JSONHelper.getLongValueFromJToken(heuristicsObject, "intervalStddev") / 100);

                    this.lastReported = JSONHelper.getDateTimeValueFromJToken(heuristicsObject, "lastReported");
                }
            }

            this.id = String.Format("{0}/{1}", this.datasetId, this.name);
        }

        public void AddSupportingDataset(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            ObsDataset parentDataset = null;
            if (allDatasetsDict.TryGetValue(this.datasetId, out parentDataset) == true)
            {
                this.Parent = parentDataset;
                this.datasetName = parentDataset.name;
                this.datasetPackage = parentDataset.package;

                // parentDataset.AddMetric(this);
            }
        }        
    }
}