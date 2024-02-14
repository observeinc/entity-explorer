using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsAccelerationInfo : ObsObject
    {
        public string state { get; set; }

        // alwaysAccelerated: Boolean
        // Whether the dataset is "always accelerated", i.e., any query should hit
        // accelerated data. If this is true then acceleratedRangeStart and
        // targetAcceleratedRangeStart are not used.        
        public bool alwaysAccelerated { get; set; }
        
        // stalenessSeconds: Float
        // Staleness of the dataset (averaged over some moving window). 5min means we
        // may not return data received in the last 5 minutes. A float value in
        // seconds.
        // Empty if alwaysAccelerated is true.        
        public TimeSpan StalenessActual { get; set; }

        // targetStalenessSeconds: Float
        // The actual target staleness target of the dataset. Note that this can be
        // higher than the configured staleness target, due to decaying or credit
        // manager overrides. Also if
        // this value is different from the field above, it means the dataset is
        // hibernated.
        public TimeSpan StalenessTarget { get; set; }
        
        // configuredTargetStalenessSeconds: Float
        // Configured staleness target of the dataset. 2min means the staleness of
        // the dataset should not exceed 2mins. May differ from the originally
        // configured value of the dataset if Dataset.freshnessDesired is nil, in
        // which case we fill in a default, or if there is a layered setting override.        
        public TimeSpan StalenessConfigured { get; set; }

        // effectiveTargetStalenessSeconds: Float
        // The target staleness of this dataset when taking downstream dataset
        // staleness targets and credit manager overrides for this dataset into
        // account. Does not take into account decay or governor overrides for
        // downstream datasets.
        // Empty if alwaysAccelerated is true.        
        public TimeSpan StalenessEffective { get; set; }

        // minimumDownstreamTargetStaleness: MinimumDownstreamTargetStaleness
        // The minimum configured target staleness across all datasets downstream of
        // this dataset. Can be used to warn if the current or potential target
        // staleness values for this dataset will be ignored due to configured
        // staleness targets for downstream datasets.        
        public TimeSpan StalenessDownstream { get; set; }
        
        // effectiveOnDemandMaterializationLength: Int64!
        // Effective on demand materialization is either the configured override value
        // for the dataset or the default value from the transformer config.        
        public TimeSpan EffectiveOnDemandMaterializationLength { get; set; }

        public List<ObsTimeRange> AcceleratedRanges { get; set; } = new List<ObsTimeRange>(1);
        public List<ObsTimeRange> AcceleratedRangesTarget { get; set; } = new List<ObsTimeRange>(1);

        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public ObsCompositeObject Parent { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsAccelerationInfo: {0} ({1})",
                this.name,
                this.id);
        }

        public ObsAccelerationInfo () {}

        public ObsAccelerationInfo(JObject entityObject, ObsCompositeObject parentObject, Dictionary<string, ObsDataset> allDatasetsDict, Dictionary<string, ObsMonitor> allMonitorsDict)
        {
            this._raw = entityObject;

            this.Parent = parentObject;
            
            this.id = this.Parent.id;
            this.name = String.Format("{0} AccelerationInfo", this.Parent);
            
            this.state = JSONHelper.getStringValueFromJToken(entityObject, "state");
            this.alwaysAccelerated = JSONHelper.getBoolValueFromJToken(entityObject, "alwaysAccelerated");

            // This value seems to be in nanoseconds, to get to ticks must divide
            this.EffectiveOnDemandMaterializationLength = new TimeSpan(JSONHelper.getLongValueFromJToken(entityObject, "effectiveOnDemandMaterializationLength")/100);

            this.StalenessActual = new TimeSpan(0, 0, JSONHelper.getIntValueFromJToken(entityObject, "stalenessSeconds"));
            this.StalenessTarget = new TimeSpan(0, 0, JSONHelper.getIntValueFromJToken(entityObject, "targetStalenessSeconds"));
            this.StalenessConfigured = new TimeSpan(0, 0, JSONHelper.getIntValueFromJToken(entityObject, "configuredTargetStalenessSeconds"));
            this.StalenessEffective = new TimeSpan(0, 0, JSONHelper.getIntValueFromJToken(entityObject, "effectiveTargetStalenessSeconds"));
            
            JObject minimumDownstreamTargetStalenessObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "minimumDownstreamTargetStaleness");
            if (minimumDownstreamTargetStalenessObject != null)
            {
                this.StalenessDownstream = new TimeSpan(0, 0, JSONHelper.getIntValueFromJToken(minimumDownstreamTargetStalenessObject, "minimumDownstreamTargetStalenessSeconds"));
                
                JArray downstreamDatasetsArray = (JArray)JSONHelper.getJTokenValueFromJToken(minimumDownstreamTargetStalenessObject, "datasetIds");
                if (downstreamDatasetsArray != null)
                {
                    foreach (JToken datasetIdToken in downstreamDatasetsArray)
                    {
                        string datasetId = (string)datasetIdToken;
                        ObsDataset obsDataset_Downstream = null;
                        if (allDatasetsDict.TryGetValue(datasetId, out obsDataset_Downstream) == true)
                        {
                            // this.ExternalObjectRelationships.Add(new ObjectRelationship(obsDataset_Downstream.name, this.Parent, obsDataset_Downstream, ObsObjectRelationshipType.ProvidesData));
                            this.ExternalObjectRelationships.Add(new ObjectRelationship(obsDataset_Downstream.name, obsDataset_Downstream, this.Parent, ObsObjectRelationshipType.ProvidesData));
                        }
                    }
                }

                JArray downstreamMonitorsArray = (JArray)JSONHelper.getJTokenValueFromJToken(minimumDownstreamTargetStalenessObject, "monitorIds");
                if (downstreamMonitorsArray != null)
                {
                    foreach (JToken monitorIdToken in downstreamMonitorsArray)
                    {
                        string monitorId = (string)monitorIdToken;
                        ObsMonitor obsMonitor_Downstream = null;
                        if (allMonitorsDict.TryGetValue(monitorId, out obsMonitor_Downstream) == true)
                        {
                            // this.ExternalObjectRelationships.Add(new ObjectRelationship(obsMonitor_Downstream.name, this.Parent, obsMonitor_Downstream, ObsObjectRelationshipType.ProvidesData));
                            this.ExternalObjectRelationships.Add(new ObjectRelationship(obsMonitor_Downstream.name, obsMonitor_Downstream, this.Parent, ObsObjectRelationshipType.ProvidesData));
                        }
                    }
                }
            }

            JArray acceleratedRangesArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "acceleratedRanges");
            if (acceleratedRangesArray != null)
            {
                foreach (JObject acceleratedRangeObject in acceleratedRangesArray)
                {
                    this.AcceleratedRanges.Add (new ObsTimeRange(acceleratedRangeObject));
                }
            }

            JArray targetAcceleratedRangesArray = (JArray)JSONHelper.getJTokenValueFromJToken(entityObject, "targetAcceleratedRanges");
            if (targetAcceleratedRangesArray != null)
            {
                foreach (JObject acceleratedRangeObject in targetAcceleratedRangesArray)
                {
                    this.AcceleratedRangesTarget.Add (new ObsTimeRange(acceleratedRangeObject));
                }
            }
        }
    }
}