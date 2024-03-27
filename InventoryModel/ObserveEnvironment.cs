using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Observe.EntityExplorer.DataObjects;
using System;
using System.Text;
using System.Net;
using Newtonsoft.Json.Bson;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace Observe.EntityExplorer
{
    public class ObserveEnvironment
    {
        public Uri CustomerEnvironmentUrl { get; set; }
        public string CustomerName { get; set; } = "Unknown";
        public string Deployment { get; set; } = "Unknown";
        public string CustomerLabel { get; set; } = "Unknown";

        public DateTime LoadedOn { get; set; }

        private Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");

        // All Dashboards, Monitors, Worksheets and Datasets
        public List<ObsCompositeObject> ObserveObjects { get; set; } = new List<ObsCompositeObject>(256);
        // All relationships between Dashboards, Monitors, Worksheets and Datasets, as well as between Stages in Dashboards, Monitors, Worksheets and Datasets
        public List<ObjectRelationship> ObjectRelationships { get; set; } = new List<ObjectRelationship>(256 * 8);
        
        // Individual entities by type
        public Dictionary<string, ObsDataset> AllDatasetsDict { get; set; }
        public Dictionary<string, ObsDashboard> AllDashboardsDict { get; set; }
        public Dictionary<string, ObsWorksheet> AllWorksheetsDict { get; set; }
        public Dictionary<string, ObsMonitor> AllMonitorsDict { get; set; }

        public override string ToString()
        {
            return String.Format(
                "{0} ({1}) in {2} is at {3} loaded on {4:u}",
                this.CustomerLabel,
                this.CustomerName,
                this.Deployment,
                this.CustomerEnvironmentUrl,
                this.LoadedOn);
        }         
        public ObserveEnvironment () {}

        public ObserveEnvironment(AuthenticatedUser currentUser)
        {
            this.CustomerLabel = currentUser.CustomerLabel;
            this.CustomerEnvironmentUrl = currentUser.CustomerEnvironmentUrl;
            this.CustomerName = currentUser.CustomerName;
            this.Deployment = currentUser.Deployment;

            #region Datasets 

            // Get all datasets
            List<ObsDataset> allDatasets = getAllDatasets(currentUser);
            this.AllDatasetsDict = allDatasets.ToDictionary(d => d.id, d => d);
            
            // Enrich their columns and relationships
            foreach (ObsDataset dataset in allDatasets)
            {
                dataset.AddRelatedKeys(this.AllDatasetsDict);
                dataset.AddForeignKeys(this.AllDatasetsDict);
                dataset.PopulateExternalDatasetRelationships(this.AllDatasetsDict);                
                this.ObjectRelationships.AddRange(dataset.ExternalObjectRelationships);
            }

            this.ObserveObjects.AddRange(allDatasets);

            #endregion

            #region Dashboards

            // Get all dashboards
            List<ObsDashboard> allDashboards = getAllDashboards(currentUser);
            this.AllDashboardsDict = allDashboards.ToDictionary(d => d.id, d => d);

            // Enrich their parameters and relationships
            foreach (ObsDashboard dashboard in allDashboards)
            {
                dashboard.AddStagesAndParameters(this.AllDatasetsDict);
                dashboard.PopulateExternalDatasetRelationships();
                this.ObjectRelationships.AddRange(dashboard.ExternalObjectRelationships);
            }
            
            this.ObserveObjects.AddRange(allDashboards);

            #endregion

            #region Monitors

            // Get all monitors
            List<ObsMonitor> allMonitors = getAllMonitors(currentUser);
            this.AllMonitorsDict = allMonitors.ToDictionary(d => d.id, d => d);

            // Enrich their parameters and relationships
            foreach (ObsMonitor monitor in allMonitors)
            {
                monitor.AddSupportingDatasets(this.AllDatasetsDict);
                monitor.AddStages(this.AllDatasetsDict);
                monitor.PopulateExternalDatasetRelationships();
                this.ObjectRelationships.AddRange(monitor.ExternalObjectRelationships);
            }

            this.ObserveObjects.AddRange(allMonitors);

            #endregion

            #region Worksheets

            // Get all worksheets
            List<ObsWorksheet> allWorksheets = getAllWorksheets(currentUser);
            this.AllWorksheetsDict = allWorksheets.ToDictionary(d => d.id, d => d);

            // Looks like for some environments query.worksheetSearch does not return stages, but the query.worksheet does.
            // If we see no stages, maybe that's what's going on? So retrieve it again and sub out the raw data
            List<ObsWorksheet> allWorksheetsWithoutStages = allWorksheets.Where(w => w.NumStages == 0).ToList();
            var allWorksheetsWithoutStagesChunks = allWorksheetsWithoutStages.Chunk(10);
            Parallel.ForEach<ObsWorksheet[], int>(
                allWorksheetsWithoutStagesChunks,
                new ParallelOptions(), 
                () => { 
                    // init
                    return 0;
                },
                (chunkOfWorksheets, loopState, subtotal) => {
                    // Body
                    for (int i = 0; i <= chunkOfWorksheets.Length - 1; i++)
                    {
                        ObsWorksheet worksheet = chunkOfWorksheets[i];
                        ObsWorksheet worksheetSingle = getWorksheet(currentUser, worksheet.id);
                        if (worksheetSingle != null)
                        {
                            worksheet._raw = worksheetSingle._raw;
                        }
                    }
                    return 0;
                },
                c => {
                    // Finally
                }
            );

            // Enrich their parameters and relationships
            foreach (ObsWorksheet worksheet in allWorksheets)
            {
                worksheet.AddStagesAndParameters(this.AllDatasetsDict);
                worksheet.PopulateExternalDatasetRelationships();
                this.ObjectRelationships.AddRange(worksheet.ExternalObjectRelationships);
            }

            this.ObserveObjects.AddRange(allWorksheets);

            #endregion

            #region Dataset and Monitor datasets acceleration info

            foreach (ObsDataset dataset in allDatasets)
            {
                dataset.AddAccelerationInfo(this.AllDatasetsDict, this.AllMonitorsDict);
            }

            foreach (ObsMonitor monitor in allMonitors)
            {
                monitor.AddAccelerationInfo(this.AllDatasetsDict, this.AllMonitorsDict);
            }

            #endregion

            #region Get usage data 

            #region Usage - transform

            // Get 1 hour data
            List<ObsCreditsTransform> transformUsage1hList = getUsageTransform(currentUser, 1);
            foreach (ObsCreditsTransform usageRow in transformUsage1hList)
            {
                ObsDataset thisDataset = null;
                if (this.AllDatasetsDict.TryGetValue(usageRow.DatasetID, out thisDataset) == true)
                {
                    thisDataset.Transform1H = usageRow;
                }
            }

            // Get 1 day data
            List<ObsCreditsTransform> transformUsage1dList = getUsageTransform(currentUser, 24);
            foreach (ObsCreditsTransform usageRow in transformUsage1dList)
            {
                ObsDataset thisDataset = null;
                if (this.AllDatasetsDict.TryGetValue(usageRow.DatasetID, out thisDataset) == true)
                {
                    thisDataset.Transform1D = usageRow;
                }
            }

            // Get 1 week data
            List<ObsCreditsTransform> transformUsage1wList = getUsageTransform(currentUser, 24 * 7);
            foreach (ObsCreditsTransform usageRow in transformUsage1wList)
            {
                ObsDataset thisDataset = null;
                if (this.AllDatasetsDict.TryGetValue(usageRow.DatasetID, out thisDataset) == true)
                {
                    thisDataset.Transform1W = usageRow;
                }
            }

            // // Get 1 month data
            // List<ObsCreditsTransform> transformUsage1mList = getUsageTransform(currentUser, 24 * 30);
            // foreach (ObsCreditsTransform transformUsage in transformUsage1wList)
            // {
            //     ObsDataset thisDataset = null;
            //     if (this.AllDatasetsDict.TryGetValue(transformUsage.DatasetID, out thisDataset) == true)
            //     {
            //         thisDataset.Transform1M = transformUsage;
            //     }
            // }

            #endregion

            #region Usage - monitor

            List<ObsCreditsMonitor> monitorUsage1hList = getUsageMonitor(currentUser, 1);
            foreach (ObsCreditsMonitor usageRow in monitorUsage1hList)
            {
                ObsMonitor thisMonitor = null;
                if (this.AllMonitorsDict.TryGetValue(usageRow.MonitorID, out thisMonitor) == true)
                {
                    thisMonitor.Transform1H = usageRow;
                }
            }

            // Get 1 day data
            List<ObsCreditsMonitor> monitorUsage1dList = getUsageMonitor(currentUser, 24);
            foreach (ObsCreditsMonitor usageRow in monitorUsage1dList)
            {
                ObsMonitor thisMonitor = null;
                if (this.AllMonitorsDict.TryGetValue(usageRow.MonitorID, out thisMonitor) == true)
                {
                    thisMonitor.Transform1D = usageRow;
                }
            }

            // Get 1 week data
            List<ObsCreditsMonitor> monitorUsage1wList = getUsageMonitor(currentUser, 24 * 7);
            foreach (ObsCreditsMonitor usageRow in monitorUsage1wList)
            {
                ObsMonitor thisMonitor = null;
                if (this.AllMonitorsDict.TryGetValue(usageRow.MonitorID, out thisMonitor) == true)
                {
                    thisMonitor.Transform1W = usageRow;
                }
            }

            #endregion

            #region Usage - query

            List<ObsCreditsQuery> queryUsage1hList = getUsageQuery(currentUser, 1);
            foreach (ObsCreditsQuery usageRow in queryUsage1hList)
            {
                ObsDataset thisDataset = null;
                if (this.AllDatasetsDict.TryGetValue(usageRow.DatasetID, out thisDataset) == true)
                {
                    thisDataset.Query1H.Credits = thisDataset.Query1H.Credits + usageRow.Credits; 
                    thisDataset.Query1HUsers.Add(usageRow);
                }
            }

            // Get 1 day data
            List<ObsCreditsQuery> queryUsage1dList = getUsageQuery(currentUser, 24);
            foreach (ObsCreditsQuery usageRow in queryUsage1dList)
            {
                ObsDataset thisDataset = null;
                if (this.AllDatasetsDict.TryGetValue(usageRow.DatasetID, out thisDataset) == true)
                {
                    thisDataset.Query1D.Credits = thisDataset.Query1D.Credits + usageRow.Credits; 
                    thisDataset.Query1DUsers.Add(usageRow);
                }
            }

            // Get 1 week data
            List<ObsCreditsQuery> queryUsage1wList = getUsageQuery(currentUser, 24 * 7);
            foreach (ObsCreditsQuery usageRow in queryUsage1wList)
            {
                ObsDataset thisDataset = null;
                if (this.AllDatasetsDict.TryGetValue(usageRow.DatasetID, out thisDataset) == true)
                {
                    thisDataset.Query1W.Credits = thisDataset.Query1W.Credits + usageRow.Credits; 
                    thisDataset.Query1WUsers.Add(usageRow);
                }
            }
            foreach (ObsDataset dataset in allDatasets)
            {
                dataset.Query1HUsers = dataset.Query1HUsers.OrderBy(q => q.UserName).ToList();
                dataset.Query1DUsers = dataset.Query1DUsers.OrderBy(q => q.UserName).ToList();
                dataset.Query1WUsers = dataset.Query1WUsers.OrderBy(q => q.UserName).ToList();
            }                        

            #endregion

            #endregion

            this.LoadedOn = DateTime.UtcNow;
        }

        public void PopulateAllDatasetStages(AuthenticatedUser currentUser)        
        {
            List<ObsDataset> allDatasetsWithoutStages = this.AllDatasetsDict.Values.Where(d => d.Stages.Count == 0).ToList();
            allDatasetsWithoutStages.RemoveAll(d => (d.ObjectType & ObsCompositeObjectType.DatastreamDataset) == ObsCompositeObjectType.DatastreamDataset);
            allDatasetsWithoutStages.RemoveAll(d => d.OriginType == ObsObjectOriginType.External);
            var allDatasetsWithoutStagesChunks = allDatasetsWithoutStages.Chunk(10);
            Parallel.ForEach<ObsDataset[], int>(
                allDatasetsWithoutStagesChunks,
                new ParallelOptions(), 
                () => { 
                    // init
                    return 0;
                },
                (chunkOfDatasets, loopState, subtotal) => {
                    // Body
                    for (int i = 0; i <= chunkOfDatasets.Length - 1; i++)
                    {
                        ObsDataset dataset = chunkOfDatasets[i];
                        PopulateDatasetStages(currentUser, dataset);
                    }
                    return 0;
                },
                c => {
                    // Finally
                }
            );            
        }

        public void PopulateDatasetStages(AuthenticatedUser currentUser, ObsDataset dataset)
        {
            // If we haven't populated the dataset stages, do so now
            if (dataset.Stages.Count == 0)
            {
                ObsDataset datasetFull = getDataset(currentUser, dataset.id);
                if (datasetFull != null)
                {
                    dataset._raw = datasetFull._raw;

                    dataset.AddStages(this.AllDatasetsDict);
                    dataset.AddAccelerationInfo(this.AllDatasetsDict, this.AllMonitorsDict);
                }
            }
        }

        #region Relationships between objects

        public List<ObsCompositeObject> GetObjectsOfType(ObsCompositeObjectType observeObjectType)
        {
            return this.ObserveObjects.Where(o => (o.ObjectType & observeObjectType) == observeObjectType).ToList();
        }

        public List<ObjectRelationship> GetRelationshipsOfThis(ObsObject interestingObject, ObsObjectRelationshipType relationshipType)
        {
            return this.ObjectRelationships.Where(r => r.ThisObject == interestingObject && r.RelationshipType == relationshipType).ToList();
        }

        public List<ObjectRelationship> GetRelationshipsOfRelated(ObsObject interestingObject, ObsObjectRelationshipType relationshipType)
        {
            return this.ObjectRelationships.Where(r => r.RelatedObject == interestingObject && r.RelationshipType == relationshipType).ToList();
        }

        public List<ObjectRelationship> GetAllAncestorRelationshipsOfThis(ObsObject interestingObject)
        {
            List<ObjectRelationship> allRelationships = new List<ObjectRelationship>(16);
            List<ObsObject> visitedObjects = new List<ObsObject>();
            this.GetAllAncestorRelationshipsOfThis(interestingObject, allRelationships, visitedObjects);
            allRelationships = allRelationships.Distinct().ToList();
            allRelationships = allRelationships.OrderBy(r => r.ToString()).ToList();
            return allRelationships;
        }

        public List<ObjectRelationship> GetAllDescendantRelationshipsOfRelated(ObsObject interestingObject)
        {
            List<ObjectRelationship> allRelationships = new List<ObjectRelationship>(16);
            List<ObsObject> visitedObjects = new List<ObsObject>();
            this.GetAllDescendantRelationshipsOfRelated(interestingObject, allRelationships, visitedObjects);
            allRelationships = allRelationships.Distinct().ToList();
            allRelationships = allRelationships.OrderBy(r => r.ToString()).ToList();
            return allRelationships;
        }

        public List<ObjectRelationship> GetAllMonitorSupportDatasetRelationships(ObsMonitor interestingObject)
        {
            List<ObjectRelationship> selectedRelationships = new List<ObjectRelationship>();
            foreach (ObjectRelationship objectRelationship in interestingObject.ExternalObjectRelationships)
            {
                if (objectRelationship.RelatedObject is ObsDataset)
                {
                    ObsDataset relatedDataset = (ObsDataset)objectRelationship.RelatedObject;
                    if ((relatedDataset.ObjectType & ObsCompositeObjectType.MonitorSupportDataset) == ObsCompositeObjectType.MonitorSupportDataset)
                    {
                        selectedRelationships.Add(objectRelationship);
                    }
                }
            }
            return selectedRelationships;
        }

        public void GetAllAncestorRelationshipsOfThis(ObsObject interestingObject, List<ObjectRelationship> allRelationships, List<ObsObject> visitedObjects)
        {
            List<ObjectRelationship> ancestorRelationships = this.ObjectRelationships.Where(r => r.ThisObject == interestingObject).ToList();
            allRelationships.AddRange(ancestorRelationships);
            foreach (ObjectRelationship ancestorRelationship in ancestorRelationships)
            {
                ObsObject nextObject = ancestorRelationship.RelatedObject;
                if (visitedObjects.Contains(nextObject) == false)
                {
                    visitedObjects.Add(nextObject);
                    GetAllAncestorRelationshipsOfThis(nextObject, allRelationships, visitedObjects);
                }
                else
                {
                    // seen this object, not going to go to avid circular reference/stack overflow
                }
            }
        }

        public void GetAllDescendantRelationshipsOfRelated(ObsObject interestingObject, List<ObjectRelationship> allRelationships, List<ObsObject> visitedObjects)
        {
            List<ObjectRelationship> descendantRelationships = this.ObjectRelationships.Where(r => r.RelatedObject == interestingObject).ToList();
            allRelationships.AddRange(descendantRelationships);
            foreach (ObjectRelationship descendantRelationship in descendantRelationships)
            {
                ObsObject nextObject = descendantRelationship.ThisObject;
                if (visitedObjects.Contains(nextObject) == false)
                {
                    visitedObjects.Add(nextObject);
                    GetAllDescendantRelationshipsOfRelated(nextObject, allRelationships, visitedObjects);
                }
                else
                {
                    // seen this object, not going to go to avid circular reference/stack overflow
                }
            }
        }

        #endregion

        #region Relationship rendering

        public string RenderGraphOfRelationships(ObsObject interestingObject, List<ObjectRelationship> allRelationships)
        {
            StringBuilder sb = new StringBuilder(128*100);

            // Start
            sb.AppendFormat("digraph observe_entity_explorer {{rankdir=LR node [shape=\"rect\"] label=\"{0}\"", interestingObject).AppendLine();

            sb.AppendLine("");
            sb.AppendLine("// Nodes");

            // Get all unique objects
            List<ObsObject> allObjects1 = allRelationships.Select(r => r.ThisObject).ToList();
            List<ObsObject> allObjects2 = allRelationships.Select(r => r.RelatedObject).ToList();
            List<ObsObject> allObjects = new List<ObsObject>(allObjects1.Count + allObjects2.Count);
            allObjects.AddRange(allObjects1);
            allObjects.AddRange(allObjects2);
            allObjects = allObjects.Distinct().ToList();

            // Output nodes
            var allObjectsGroupedByType = allObjects.GroupBy(o => o.GetType());
            foreach (var allObjectsGroupedByTypeGroup in allObjectsGroupedByType)
            {
                switch (allObjectsGroupedByTypeGroup.Key.Name)
                {
                    case "ObsDataset":
                        List<ObsDataset> allDatasets = allObjectsGroupedByTypeGroup.Cast<ObsDataset>().ToList();
                        var datasetsGroupedByOriginType = allDatasets.GroupBy(d => d.OriginType);
                        foreach (var datasetsGroupedByOriginTypeGroup in datasetsGroupedByOriginType)
                        {
                            List<ObsDataset> allDatasetsInGroup = datasetsGroupedByOriginTypeGroup.ToList();
                            string iconForGroup = getIconOriginType(datasetsGroupedByOriginTypeGroup.Key);
                            switch (datasetsGroupedByOriginTypeGroup.Key)
                            {
                                case ObsObjectOriginType.DataStream:
                                    sb.AppendLine("  subgraph cluster_ds_datastream {");
                                    sb.AppendFormat("    label=\"{0} DataStreams ({1})\"", iconForGroup, allDatasetsInGroup.Count).AppendLine();
                                    foreach(ObsDataset dataset in allDatasetsInGroup)
                                    {
                                        if (dataset == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.App:
                                    var allAppDatasetsGroupedByPackage = allDatasetsInGroup.GroupBy(d => d.package);
                                    foreach (var allAppDatasetsGroupedByPackageGroup in allAppDatasetsGroupedByPackage)
                                    {
                                        List<ObsDataset> allDatasetsInAppGroup = allAppDatasetsGroupedByPackageGroup.ToList();

                                        sb.AppendFormat("  subgraph cluster_ds_app_{0} {{", escapeGraphVizObjectNameForSubGraph(allAppDatasetsGroupedByPackageGroup.Key)).AppendLine();
                                        sb.AppendFormat("    label=\"{0} App Datasets [{1}] ({2})\" style=\"filled\" fillcolor=\"lightyellow\"", iconForGroup, allAppDatasetsGroupedByPackageGroup.Key, allDatasetsInAppGroup.Count).AppendLine();
                                        foreach(ObsDataset dataset in allDatasetsInAppGroup)
                                        {
                                            if (dataset == interestingObject) continue;
                                            sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                        }
                                        sb.AppendLine("  }");
                                    }

                                    break;

                                case ObsObjectOriginType.System:
                                    List<ObsDataset> obsDatasetsMetric = allDatasetsInGroup.Where(d => (d.ObjectType & ObsCompositeObjectType.MetricSMADataset) == ObsCompositeObjectType.MetricSMADataset).ToList();
    
                                    sb.AppendLine("  subgraph cluster_ds_metric_support {");
                                    sb.AppendFormat("    label=\"{0} Metric Support Datasets ({1})\" style=\"filled\" fillcolor=\"paleturquoise\"", iconForGroup, obsDatasetsMetric.Count).AppendLine();
                                    foreach(ObsDataset dataset in obsDatasetsMetric)
                                    {
                                        if (dataset == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    List<ObsDataset> obsDatasetsMonitor = allDatasetsInGroup.Where(d => (d.ObjectType & ObsCompositeObjectType.MonitorSupportDataset) == ObsCompositeObjectType.MonitorSupportDataset).ToList();
    
                                    sb.AppendLine("  subgraph cluster_ds_monitor_support {");
                                    sb.AppendFormat("    label=\"📟 Monitor Support Datasets ({0})\" style=\"filled\" fillcolor=\"seashell\"", obsDatasetsMonitor.Count).AppendLine();
                                    foreach(ObsDataset dataset in obsDatasetsMonitor)
                                    {
                                        if (dataset == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.Terraform:
                                    sb.AppendLine("  subgraph cluster_ds_user_terraform {");
                                    sb.AppendFormat("    label=\"{0} Terraformed Datasets ({1})\" style=\"filled\" fillcolor=\"linen\"", iconForGroup, allDatasetsInGroup.Count).AppendLine();
                                    foreach(ObsDataset dataset in allDatasetsInGroup)
                                    {
                                        if (dataset == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.User:
                                    sb.AppendLine("  subgraph cluster_ds_user {");
                                    sb.AppendFormat("    label=\"{0} User Datasets ({1})\" style=\"filled\" fillcolor=\"palegreen\"", iconForGroup, allDatasetsInGroup.Count).AppendLine();
                                    foreach(ObsDataset dataset in allDatasetsInGroup)
                                    {
                                        if (dataset == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.External:
                                    sb.AppendLine("  subgraph cluster_ds_external {");
                                    sb.AppendFormat("    label=\"{0} External Datasets ({1})\" style=\"filled\" fillcolor=\"cyan2\"", iconForGroup, allDatasetsInGroup.Count).AppendLine();
                                    foreach(ObsDataset dataset in allDatasetsInGroup)
                                    {
                                        if (dataset == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                default:
                                    break;
                            }
                        }

                        break;

                    case "ObsStage":
                        List<ObsStage> allStages = allObjectsGroupedByTypeGroup.Cast<ObsStage>().ToList();
                        var stagesGroupedByParentDatasetOrDashboard = allStages.GroupBy(d => d.Parent);
                        foreach (var stagesGroupedByParentDatasetOrDashboardGroup in stagesGroupedByParentDatasetOrDashboard)
                        {
                            ObsCompositeObject parentObjectOfThisGroup = stagesGroupedByParentDatasetOrDashboardGroup.Key;
                            if (parentObjectOfThisGroup is ObsDataset)
                            {
                                ObsDataset parentDataset = (ObsDataset)parentObjectOfThisGroup;

                                sb.AppendFormat("  subgraph cluster_dataset_{0} {{", getGraphVizNodeName(parentDataset, false)).AppendLine();
                                sb.AppendFormat("    label=\"🎈🎫 Dataset {0}\" style=\"filled\" fillcolor=\"lavender\"", WebUtility.HtmlEncode(parentDataset.name)).AppendLine();
                                sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parentDataset)).AppendLine();
                                
                                sb.AppendFormat("    // Stages").AppendLine();
                                foreach(ObsStage stage in parentDataset.Stages)
                                {
                                    sb.AppendFormat("    {0}", getGraphVizNodeDefinition(stage)).AppendLine();
                                }
                                sb.AppendLine("  }");
                            }
                            else if (parentObjectOfThisGroup is ObsDashboard)
                            {
                                ObsDashboard parentDashboard = (ObsDashboard)parentObjectOfThisGroup;

                                sb.AppendFormat("  subgraph cluster_dashboard_{0} {{", getGraphVizNodeName(parentDashboard, false)).AppendLine();
                                sb.AppendFormat("    label=\"🎈📈 Dashboard {0}\" style=\"filled\" fillcolor=\"lavender\"", WebUtility.HtmlEncode(parentDashboard.name)).AppendLine();
                                sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parentDashboard)).AppendLine();

                                if (parentDashboard.Parameters.Count > 0)
                                {
                                    sb.AppendFormat("    // Parameters").AppendLine();
                                    sb.AppendFormat("    subgraph cluster_dashboard_{0}_parameters {{", getGraphVizNodeName(parentDashboard, false)).AppendLine();
                                    sb.AppendFormat("    label=\"Parameters ({0})\"", parentDashboard.Parameters.Count).AppendLine();
                                    foreach(ObsParameter parameter in parentDashboard.Parameters)
                                    {
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parameter)).AppendLine();
                                    }
                                    sb.AppendLine("    }");
                                }

                                sb.AppendFormat("    // Stages").AppendLine();
                                foreach(ObsStage stage in parentDashboard.Stages)
                                {
                                    sb.AppendFormat("    {0}", getGraphVizNodeDefinition(stage)).AppendLine();
                                }

                                sb.AppendLine("  }");
                            }
                            else if (parentObjectOfThisGroup is ObsMonitor)
                            {
                                ObsMonitor parentMonitor = (ObsMonitor)parentObjectOfThisGroup;
                                
                                sb.AppendFormat("  subgraph cluster_monitor_{0} {{", getGraphVizNodeName(parentMonitor, false)).AppendLine();
                                sb.AppendFormat("    label=\"🎈📟 Monitor {0}\" style=\"filled\" fillcolor=\"lavender\"", WebUtility.HtmlEncode(parentMonitor.name)).AppendLine();
                                sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parentMonitor)).AppendLine();
                                
                                sb.AppendFormat("    // Stages").AppendLine();
                                foreach(ObsStage stage in parentMonitor.Stages)
                                {
                                    sb.AppendFormat("    {0}", getGraphVizNodeDefinition(stage)).AppendLine();
                                }
                                sb.AppendLine("  }");
                            }
                            else if (parentObjectOfThisGroup is ObsWorksheet)
                            {
                                ObsWorksheet parentWorksheet = (ObsWorksheet)parentObjectOfThisGroup;
                                
                                sb.AppendFormat("  subgraph cluster_worksheet_{0} {{", getGraphVizNodeName(parentWorksheet, false)).AppendLine();
                                sb.AppendFormat("    label=\"🎈📝 Worksheet {0}\" style=\"filled\" fillcolor=\"lavender\"", WebUtility.HtmlEncode(parentWorksheet.name)).AppendLine();
                                sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parentWorksheet)).AppendLine();

                                if (parentWorksheet.Parameters.Count > 0)
                                {
                                    sb.AppendFormat("    // Parameters").AppendLine();
                                    sb.AppendFormat("    subgraph cluster_worksheet_{0}_parameters {{", getGraphVizNodeName(parentWorksheet, false)).AppendLine();
                                    sb.AppendFormat("    label=\"Parameters ({0})\"", parentWorksheet.Parameters.Count).AppendLine();
                                    foreach(ObsParameter parameter in parentWorksheet.Parameters)
                                    {
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parameter)).AppendLine();
                                    }
                                    sb.AppendLine("    }");
                                }

                                sb.AppendFormat("    // Stages").AppendLine();
                                foreach(ObsStage stage in parentWorksheet.Stages)
                                {
                                    sb.AppendFormat("    {0}", getGraphVizNodeDefinition(stage)).AppendLine();
                                }

                                sb.AppendLine("  }");
                            }
                            else
                            {
                                // What else can it be?
                                throw new NotImplementedException(String.Format("{0} is not a container object type that is supported", parentObjectOfThisGroup.GetType()));
                            }
                        }

                        break;

                    case "ObsParameter":
                        // fall through. Parameters handled via Stages right above
                        break;

                    case "ObsDashboard":
                        List<ObsDashboard> allDashboards = allObjectsGroupedByTypeGroup.Cast<ObsDashboard>().ToList();
                        var dashboardsGroupedByOriginType = allDashboards.GroupBy(d => d.OriginType);
                        foreach (var dashboardsGroupedByOriginTypeGroup in dashboardsGroupedByOriginType)
                        {
                            List<ObsDashboard> allDashboardsInGroup = dashboardsGroupedByOriginTypeGroup.ToList();
                            string iconForGroup = getIconOriginType(dashboardsGroupedByOriginTypeGroup.Key);
                            switch (dashboardsGroupedByOriginTypeGroup.Key)
                            {
                                case ObsObjectOriginType.App:
                                    var allAppDashboardsGroupedByPackage = allDashboardsInGroup.GroupBy(d => d.package);
                                    foreach (var allAppDashboardsGroupedByPackageGroup in allAppDashboardsGroupedByPackage)
                                    {
                                        List<ObsDashboard> allDashboardsInAppGroup = allAppDashboardsGroupedByPackageGroup.ToList();

                                        sb.AppendFormat("  subgraph cluster_da_app_{0} {{", escapeGraphVizObjectNameForSubGraph(allAppDashboardsGroupedByPackageGroup.Key)).AppendLine();
                                        sb.AppendFormat("    label=\"{0} App Dashboards [{1}] ({2})\" style=\"filled\" fillcolor=\"thistle\"", iconForGroup, allAppDashboardsGroupedByPackageGroup.Key, allDashboardsInAppGroup.Count).AppendLine();
                                        foreach(ObsDashboard dashboard in allDashboardsInAppGroup)
                                        {
                                            if (dashboard == interestingObject) continue;
                                            sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dashboard)).AppendLine();
                                        }
                                        sb.AppendLine("  }");
                                    }

                                    break;

                                case ObsObjectOriginType.System:
                                    sb.AppendLine("  subgraph cluster_da_system {");
                                    sb.AppendFormat("    label=\"{0} System Dashboards ({1})\" style=\"filled\" fillcolor=\"mistyrose\"", iconForGroup, allDashboardsInGroup.Count).AppendLine();
                                    foreach(ObsDashboard dashboard in allDashboardsInGroup)
                                    {
                                        if (dashboard == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dashboard)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.User:
                                    sb.AppendLine("  subgraph cluster_da_user {");
                                    sb.AppendFormat("    label=\"{0} User Dashboards ({1})\" style=\"filled\" fillcolor=\"olivedrab2\"", iconForGroup, allDashboardsInGroup.Count).AppendLine();
                                    foreach(ObsDashboard dashboard in allDashboardsInGroup)
                                    {
                                        if (dashboard == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dashboard)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                default:
                                    break;
                            }                        
                        }
                        
                        break;

                    case "ObsMonitor":
                        List<ObsMonitor> allMonitors = allObjectsGroupedByTypeGroup.Cast<ObsMonitor>().ToList();
                        var monitorsGroupedByOriginType = allMonitors.GroupBy(d => d.OriginType);
                        foreach (var monitorsGroupedByOriginTypeGroup in monitorsGroupedByOriginType)
                        {
                            List<ObsMonitor> allMonitorsInGroup = monitorsGroupedByOriginTypeGroup.ToList();
                            string iconForGroup = getIconOriginType(monitorsGroupedByOriginTypeGroup.Key);
                            switch (monitorsGroupedByOriginTypeGroup.Key)
                            {
                                case ObsObjectOriginType.App:
                                    var allAppMonitorsGroupedByPackage = allMonitorsInGroup.GroupBy(d => d.package);
                                    foreach (var allAppMonitorsGroupedByPackageGroup in allAppMonitorsGroupedByPackage)
                                    {
                                        List<ObsMonitor> allMonitorsInAppGroup = allAppMonitorsGroupedByPackageGroup.ToList();

                                        sb.AppendFormat("  subgraph cluster_mon_app_{0} {{", escapeGraphVizObjectNameForSubGraph(allAppMonitorsGroupedByPackageGroup.Key)).AppendLine();
                                        sb.AppendFormat("    label=\"{0} App Monitors [{1}] ({2})\" style=\"filled\" fillcolor=\"wheat\"", iconForGroup, allAppMonitorsGroupedByPackageGroup.Key, allMonitorsInAppGroup.Count).AppendLine();
                                        foreach(ObsMonitor monitor in allMonitorsInAppGroup)
                                        {
                                            if (monitor == interestingObject) continue;
                                            sb.AppendFormat("    {0}", getGraphVizNodeDefinition(monitor)).AppendLine();
                                        }
                                        sb.AppendLine("  }");
                                    }

                                    break;

                                case ObsObjectOriginType.System:
                                    sb.AppendLine("  subgraph cluster_mon_system {");
                                    sb.AppendFormat("    label=\"{0} System Monitors ({1})\" style=\"filled\" fillcolor=\"oldlace\"", iconForGroup, allMonitorsInGroup.Count).AppendLine();
                                    foreach(ObsMonitor monitor in allMonitorsInGroup)
                                    {
                                        if (monitor == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(monitor)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.User:
                                    sb.AppendLine("  subgraph cluster_mon_user {");
                                    sb.AppendFormat("    label=\"{0} User Monitors ({1})\" style=\"filled\" fillcolor=\"lightcyan\"", iconForGroup, allMonitorsInGroup.Count).AppendLine();
                                    foreach(ObsMonitor monitor in allMonitorsInGroup)
                                    {
                                        if (monitor == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(monitor)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;
                            }
                        }
                        
                        break;

                    case "ObsWorksheet":
                        List<ObsWorksheet> allWorksheets = allObjectsGroupedByTypeGroup.Cast<ObsWorksheet>().ToList();
                        var worksheetsGroupedByOriginType = allWorksheets.GroupBy(d => d.OriginType);
                        foreach (var worksheetsGroupedByOriginTypeGroup in worksheetsGroupedByOriginType)
                        {
                            List<ObsWorksheet> allWorksheetsInGroup = worksheetsGroupedByOriginTypeGroup.ToList();
                            string iconForGroup = getIconOriginType(worksheetsGroupedByOriginTypeGroup.Key);
                            switch (worksheetsGroupedByOriginTypeGroup.Key)
                            {
                                case ObsObjectOriginType.App:
                                    var allAppWorksheetsGroupedByPackage = allWorksheetsInGroup.GroupBy(d => d.package);
                                    foreach (var allAppWorksheetsGroupedByPackageGroup in allAppWorksheetsGroupedByPackage)
                                    {
                                        List<ObsWorksheet> allWorksheetsInAppGroup = allAppWorksheetsGroupedByPackageGroup.ToList();

                                        sb.AppendFormat("  subgraph cluster_wks_app_{0} {{", escapeGraphVizObjectNameForSubGraph(allAppWorksheetsGroupedByPackageGroup.Key)).AppendLine();
                                        sb.AppendFormat("    label=\"{0} App Worksheets [{1}] ({2})\" style=\"filled\" fillcolor=\"burlywood\"", iconForGroup, allAppWorksheetsGroupedByPackageGroup.Key, allWorksheetsInAppGroup.Count).AppendLine();
                                        foreach(ObsWorksheet worksheet in allWorksheetsInAppGroup)
                                        {
                                            if (worksheet == interestingObject) continue;
                                            sb.AppendFormat("    {0}", getGraphVizNodeDefinition(worksheet)).AppendLine();
                                        }
                                        sb.AppendLine("  }");
                                    }

                                    break;

                                case ObsObjectOriginType.System:
                                    sb.AppendLine("  subgraph cluster_wks_system {");
                                    sb.AppendFormat("    label=\"{0} System Worksheets ({1})\" style=\"filled\" fillcolor=\"blanchedalmond\"", iconForGroup, allWorksheetsInGroup.Count).AppendLine();
                                    foreach(ObsWorksheet worksheet in allWorksheetsInGroup)
                                    {
                                        if (worksheet == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(worksheet)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.User:
                                    sb.AppendLine("  subgraph cluster_wks_user {");
                                    sb.AppendFormat("    label=\"{0} User Worksheets ({1})\" style=\"filled\" fillcolor=\"mediumseagreen\"", iconForGroup, allWorksheetsInGroup.Count).AppendLine();
                                    foreach(ObsWorksheet worksheet in allWorksheetsInGroup)
                                    {
                                        if (worksheet == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(worksheet)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                default:
                                    break;
                            }                        
                        }

                        break;

                    default:
                        throw new NotImplementedException(String.Format("{0} graph output not supported yet", allObjectsGroupedByTypeGroup.Key.Name));
                        //break;
                }
            }

            // Output THIS node in a special way, redefining it from previous view
            sb.AppendLine("");
            sb.AppendLine("// The main focus entity");
            switch (interestingObject.GetType().Name)
            {
                case "ObsDataset":
                    ObsDataset dataset = (ObsDataset)interestingObject;
                    sb.AppendLine(getGraphVizNodeDefinition(dataset, true)).AppendLine();
                    break;

                case "ObsDashboard":
                    ObsDashboard dashboard = (ObsDashboard)interestingObject;
                    sb.AppendLine(getGraphVizNodeDefinition(dashboard, true)).AppendLine();
                    break;
                
                case "ObsMonitor":
                    ObsMonitor monitor = (ObsMonitor)interestingObject;
                    sb.AppendLine(getGraphVizNodeDefinition(monitor, true)).AppendLine();
                    break;

                case "ObsWorksheet":
                    ObsWorksheet worksheet = (ObsWorksheet)interestingObject;
                    sb.AppendLine(getGraphVizNodeDefinition(worksheet, true)).AppendLine();
                    break;

                case "ObsObject":
                    if (interestingObject.id == "-1")
                    {

                    };
                    break;

                default:
                    throw new NotImplementedException(String.Format("{0} graph output not supported yet", interestingObject.GetType().Name));
            }

            sb.AppendLine("");

            // Output edges
            sb.AppendLine("// Edges");
            foreach (ObjectRelationship relationship in allRelationships)
            {
                // Check if we're outputting incoming links to the focus object that has stages
                if (relationship.ThisObject == interestingObject)
                {
                    // Out->here, so skip the double-linking. The links to Stages will explain what's going on
                    sb.AppendLine();
                }
                else if (relationship.RelatedObject == interestingObject)
                {
                    // here->out
                    sb.AppendLine(getGraphVizEdgeDefinition(relationship));
                }
                else
                {
                    sb.AppendLine(getGraphVizEdgeDefinition(relationship));
                }
            }
            
            // Close the entire doc
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string getGraphVizNodeDefinition(ObsDataset dataset)
        {
            return getGraphVizNodeDefinition(dataset, false);
        }

        private string getGraphVizNodeDefinition(ObsDataset dataset, bool highlight)
        {
            string nodeColor = "black";
            string nodeIcon = getIconType(dataset);
            if ((dataset.ObjectType & ObsCompositeObjectType.EventDataset) == ObsCompositeObjectType.EventDataset)
            {
                nodeColor = "purple";
            }
            else if ((dataset.ObjectType & ObsCompositeObjectType.ResourceDataset) == ObsCompositeObjectType.ResourceDataset)
            {
                nodeColor = "blue";
            }
            else if ((dataset.ObjectType & ObsCompositeObjectType.IntervalDataset) == ObsCompositeObjectType.IntervalDataset)
            {
                nodeColor = "maroon";
            }

            string nodeShape = "rectangle";
            switch (dataset.OriginType)
            {
                case ObsObjectOriginType.DataStream:
                    nodeColor = "green";
                    nodeShape = "hexagon";
                    break;
                case ObsObjectOriginType.App:
                    break;
                case ObsObjectOriginType.System:
                    nodeShape = "diamond";
                    break;
                case ObsObjectOriginType.External:
                    nodeShape = "pentagon";
                    break;
                case ObsObjectOriginType.Terraform:
                    break;
                case ObsObjectOriginType.User:
                    break;
                default:
                    break;
            }

            if (highlight == true)
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\" tooltip=\"{5}\" style=\"filled\" fillcolor=\"pink\"]", getGraphVizNodeName(dataset), nodeIcon, WebUtility.HtmlEncode(dataset.name.Replace("/", "/\n")), nodeShape, nodeColor, WebUtility.HtmlEncode(dataset.description));
            }
            else
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\"]", getGraphVizNodeName(dataset), nodeIcon, WebUtility.HtmlEncode(dataset.name.Replace("/", "/\n")), nodeShape, nodeColor);
            }
        }

        private string getGraphVizNodeDefinition(ObsStage stage)
        {
            string nodeIcon = getIconWidgetType(stage);

            string nodeColor = "black";
            if (stage.visible == false)
            {
                nodeColor = "gray";
                nodeIcon = String.Format("{0}🙈", nodeIcon);
            }

            if (stage.Parent is ObsDataset)
            {
                if (stage == ((ObsDataset)stage.Parent).OutputStage)
                {
                    nodeIcon = String.Format("{0}🏁", nodeIcon);
                }
            }

            string nodeShape = "parallelogram";

            // Put ~1KB of OPAL into the tooltip, and add length and number of lines
            StringBuilder toolTip = new StringBuilder(1024-32);
            int truncateTo = 1024 - 32;
            if (stage.pipeline.Length > truncateTo)
            {
                toolTip.Append(stage.pipeline.Substring(0, truncateTo));
                toolTip.Append("...");
            }
            else
            {
                toolTip.Append(stage.pipeline);
            }
            toolTip.AppendLine();
            toolTip.AppendFormat("Len:{0} ", stage.pipeline.Length);
            toolTip.AppendFormat("Lines:{0}", stage.pipeline.Split("\n").Length);
            
            return String.Format("{0} [label=\"{1}{2} [{3}]\" shape=\"{4}\" color=\"{5}\" tooltip=\"{6}\"]", 
                getGraphVizNodeName(stage), 
                nodeIcon, 
                WebUtility.HtmlEncode(wordWrap(stage.name, 16, new char[] { ' '})), 
                stage.type,
                nodeShape, 
                nodeColor, 
                WebUtility.HtmlEncode(toolTip.ToString()));
        }

        private string getGraphVizNodeDefinition(ObsParameter parameter)
        {
            string nodeIcon = getIconParameterType(parameter);

            // TODO maybe change color based on the source of the parameter (Dataset, Stage, UserInput)
            string nodeColor = "black";

            string nodeShape = "ellipse";

            string toolTip = String.Format("Source Column: {0}, Default Value: {1}", parameter.sourceColumn, parameter.defaultValue);

            return String.Format("{0} [label=\"{1}{2} (${3}) [{4}]\" shape=\"{5}\" color=\"{6}\" tooltip=\"{7}\"]", 
                getGraphVizNodeName(parameter), 
                nodeIcon, 
                WebUtility.HtmlEncode(wordWrap(parameter.name, 16, new char[] { ' '})),
                WebUtility.HtmlEncode(parameter.id),
                parameter.dataType,
                nodeShape, 
                nodeColor, 
                WebUtility.HtmlEncode(toolTip));
        }

        private string getGraphVizNodeDefinition(ObsDashboard dashboard)
        {
            return getGraphVizNodeDefinition(dashboard, false);
        }

        private string getGraphVizNodeDefinition(ObsDashboard dashboard, bool highlight)
        {
            string nodeColor = "black";
            string nodeIcon = "📈";
            string nodeShape = "tab";

            if (highlight == true)
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\" style=\"filled\" fillcolor=\"pink\"]", getGraphVizNodeName(dashboard), nodeIcon, WebUtility.HtmlEncode(dashboard.name.Replace("/", "/\n")), nodeShape, nodeColor);
            }
            else
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\"]", getGraphVizNodeName(dashboard), nodeIcon, WebUtility.HtmlEncode(dashboard.name.Replace("/", "/\n")), nodeShape, nodeColor);
            }
        }

        private string getGraphVizNodeDefinition(ObsMonitor monitor)
        {
            return getGraphVizNodeDefinition(monitor, false);
        }

        private string getGraphVizNodeDefinition(ObsMonitor monitor, bool highlight)
        {
            string nodeColor = "black";
            ObsCompositeObjectType objectType = ObsCompositeObjectType.Unknown;

            if ((monitor.ObjectType & ObsCompositeObjectType.MetricThresholdMonitor) == ObsCompositeObjectType.MetricThresholdMonitor)
            {
                objectType = ObsCompositeObjectType.MetricThresholdMonitor;
            }
            else if ((monitor.ObjectType & ObsCompositeObjectType.LogThresholdMonitor) == ObsCompositeObjectType.LogThresholdMonitor)
            {
                objectType = ObsCompositeObjectType.LogThresholdMonitor;
            }
            else if ((monitor.ObjectType & ObsCompositeObjectType.ResourceCountThresholdMonitor) == ObsCompositeObjectType.ResourceCountThresholdMonitor)
            {
                objectType = ObsCompositeObjectType.ResourceCountThresholdMonitor;
            }
            else if ((monitor.ObjectType & ObsCompositeObjectType.PromotionMonitor) == ObsCompositeObjectType.PromotionMonitor)
            {
                objectType = ObsCompositeObjectType.PromotionMonitor;
            }
            else if ((monitor.ObjectType & ObsCompositeObjectType.ResourceTextValueMonitor) == ObsCompositeObjectType.ResourceTextValueMonitor)
            {
                objectType = ObsCompositeObjectType.ResourceTextValueMonitor;
            }

            string nodeIcon = getIconMonitorType(objectType);

            string nodeShape = "folder";

            if (monitor.IsEnabled == false)
            {
                nodeColor = "gray";
            }

            if (highlight == true)
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\" style=\"filled\" fillcolor=\"pink\"]", getGraphVizNodeName(monitor), nodeIcon, WebUtility.HtmlEncode(monitor.name.Replace("/", "/\n")), nodeShape, nodeColor);
            }
            else
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\"]", getGraphVizNodeName(monitor), nodeIcon, WebUtility.HtmlEncode(monitor.name.Replace("/", "/\n")), nodeShape, nodeColor);
            }
        }

        private string getGraphVizNodeDefinition(ObsWorksheet worksheet)
        {
            return getGraphVizNodeDefinition(worksheet, false);
        }

        private string getGraphVizNodeDefinition(ObsWorksheet worksheet, bool highlight)
        {
            string nodeColor = "black";
            string nodeIcon = "📝";
            string nodeShape = "trapezium";

            if (highlight == true)
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\" style=\"filled\" fillcolor=\"pink\"]", getGraphVizNodeName(worksheet), nodeIcon, WebUtility.HtmlEncode(worksheet.name.Replace("/", "/\n")), nodeShape, nodeColor);
            }
            else
            {
                return String.Format("{0} [label=\"{1}{2}\" shape=\"{3}\" color=\"{4}\"]", getGraphVizNodeName(worksheet), nodeIcon, WebUtility.HtmlEncode(worksheet.name.Replace("/", "/\n")), nodeShape, nodeColor);
            }
        }

        private string getGraphVizEdgeDefinition(ObjectRelationship relationship)
        {
            string edgeColor = "black";

            switch (relationship.RelationshipType)
            {
                case ObsObjectRelationshipType.ProvidesData:
                    break;

                case ObsObjectRelationshipType.Linked:
                    edgeColor = "blue";
                    break;

                case ObsObjectRelationshipType.ProvidesParameter:
                    edgeColor = "burlywood";
                    break;

                default:
                    break;
            }

            return String.Format("{0}->{1} [color=\"{2}\" tooltip=\"{3}\"]", 
                getGraphVizNodeName(relationship.RelatedObject), 
                getGraphVizNodeName(relationship.ThisObject),
                edgeColor,
                WebUtility.HtmlEncode(relationship.name));
        }

        private string getGraphVizNodeName(ObsObject interestingObject)
        {
            return getGraphVizNodeName(interestingObject, true);
        }

        private string getGraphVizNodeName(ObsObject interestingObject, bool doubleQuoted)
        {
            string quoteCharacter = "\"";
            if (doubleQuoted == false) quoteCharacter = String.Empty;
            
            if (interestingObject is ObsDataset)
            {
                return String.Format("{0}DS_{1}{0}", quoteCharacter, interestingObject.id);
            }
            else if (interestingObject is ObsDashboard)
            {
                return String.Format("{0}DA_{1}{0}", quoteCharacter, interestingObject.id);
            }
            else if (interestingObject is ObsMonitor)
            {
                return String.Format("{0}MON_{1}{0}", quoteCharacter, interestingObject.id);
            }
            else if (interestingObject is ObsWorksheet)
            {
                return String.Format("{0}WKS_{1}{0}", quoteCharacter, interestingObject.id);
            }
            else if (interestingObject is ObsStage)
            {
                return String.Format("{0}STG_{1}_{2}{0}", quoteCharacter, ((ObsStage)interestingObject).Parent.id, interestingObject.id);
            }
            else if (interestingObject is ObsParameter)
            {
                return String.Format("{0}PRM_{1}_{2}{0}", quoteCharacter, ((ObsParameter)interestingObject).Parent.id, interestingObject.id);
            }

            return String.Empty;
        }

        private string escapeGraphVizObjectNameForSubGraph(string potentialName)
        {
            char[] charsToEscape = new char[] { ' ', '\t', '.', '+', '-', '(', ')', '[', ']', '\"', '\'', '{', '}', '!', '<', '>', '~', '`', '*', '$', '#', '@', '!', '\\', '/', ':', ';', ',', '?', '^', '%', '&', '|', '\n', '\r', '\v', '\f', '\0' };
            string escapedName = potentialName;
            foreach (char charToEscape in charsToEscape)
            {
                escapedName = escapedName.Replace(charToEscape, '_');
            }
            return escapedName;
        }

		private string wordWrap(string text, int width, params char[] wordBreakChars)
		{
            char[] _wordBreakChars = new char[] { ' ', '_', '\t', '.', '+', '-', '(', ')', '[', ']', '\"', /*'\'',*/ '{', '}', '!', '<', '>', '~', '`', '*', '$', '#', '@', '!', '\\', '/', ':', ';', ',', '?', '^', '%', '&', '|', '\n', '\r', '\v', '\f', '\0' };
			if (string.IsNullOrEmpty(text) || 0 == width || width>=text.Length)
				return text;
			if (null == wordBreakChars || 0 == wordBreakChars.Length)
				wordBreakChars = _wordBreakChars;
			var sb = new StringBuilder();
			var sr = new StringReader(text);
			string line;
			var first = true;
			while(null!=(line=sr.ReadLine())) 
			{
				var col = 0;
				if (!first)
				{
					sb.AppendLine();
					col = 0;
				}
				else
					first = false;
				var words = line.Split(wordBreakChars);

				for(var i = 0; i<words.Length; i++)
				{
					var word = words[i];
					if (0 != i)
					{
						sb.Append(" ");
						++col;
					}
					if (col+word.Length>width)
					{
						sb.AppendLine();
						col = 0;
					}
					sb.Append(word);
					col += word.Length;
				}
			}
			return sb.ToString();
		}

        #endregion

        #region Icon rendering for various object type labels

        internal string getIconType(ObsDataset obsDataset)
        {
            return obsDataset.kind switch
            {
                "Interval" => "⏲", "Event" => "📅", "Resource" => "🗃", _ => "❓"
            };
        }

        internal string getIconMonitorType(ObsCompositeObjectType objectType)
        {
            return objectType switch
            {
                ObsCompositeObjectType.MetricThresholdMonitor => "📈", ObsCompositeObjectType.LogThresholdMonitor => "📜", ObsCompositeObjectType.ResourceCountThresholdMonitor => "🍫", ObsCompositeObjectType.PromotionMonitor => "🕙", ObsCompositeObjectType.ResourceTextValueMonitor => "🏆", _ => "❓"
            };
        }

        internal string getIconOriginType(ObsCompositeObject obsObject)
        {
            return getIconOriginType(obsObject.OriginType);
        }

        internal string getIconOriginType(ObsObjectOriginType obsObjectOriginType)
        {
            return obsObjectOriginType switch 
            {
                ObsObjectOriginType.System => "⚙️", ObsObjectOriginType.App => "📊", ObsObjectOriginType.User => "👋", ObsObjectOriginType.DataStream => "🎏", ObsObjectOriginType.Terraform => "🛤️", ObsObjectOriginType.External => "❄️", _ => "❓"
            };
        }

        internal string getIconEnabled(ObsMonitor obsMonitor)
        {
            return obsMonitor.IsEnabled switch
            {
                true => "✅", false => "❌"
            };
        }

        internal string getIconWidgetType(ObsStage obsStage)
        {
            return obsStage.type switch
            {
                "table" => "📑", "timeseries" => "📉", "bar" => "📊", "circular" => "🥧", "stacked_area" => "🗻", "singlevalue" => "#️⃣", "list" => "📜", "valueovertime" => "⏳", "gantt" => "📐", _ => ""
            };
        }

        internal string getIconParameterType(ObsParameter obsParameter)
        {
            return obsParameter.viewType switch
            {
                "resource-input" => "🛆", "single-select" => "⛛", "text" => "🔤", "numeric" => "#️⃣", "input" => "🌫️", _ => "❓"
            };
        }

        internal string getIconFieldType(ObsFieldDefinition obsFieldDefinition)
        {
            return obsFieldDefinition.type switch
            {
                "timestamp" => "🕘", "duration" => "⏰", "string" => "📝", "int64" => "⑽", "float64" => "⒑", "object" => "🎛", "variant" => "💫", "array" => "🔢", "bool" => "❓", _ => " "
            };
        }

        internal string getDatasetURLPartType(ObsDataset obsDataset)
        {
            return obsDataset.kind switch
            {
                "Event" => "event", "Resource" => "resource", "Interval" => "event", _ => "fallthroughdonttknow"
            };
        }

        #endregion

        private List<ObsDataset> getAllDatasets(AuthenticatedUser currentUser)
        {
            string entitySearchResults = ObserveConnection.datasetSearch_all(currentUser);
            if (entitySearchResults.Length == 0)
            {
                throw new InvalidDataException(String.Format("Invalid response on datasetSearch_all for {0}", currentUser));
            }

            JObject entitySearchResultsObject = JObject.Parse(entitySearchResults);
            JArray entitySearchArray = (JArray)JSONHelper.getJTokenValueFromJToken(entitySearchResultsObject["data"], "datasetSearch");

            List<ObsDataset> datasetsList = new List<ObsDataset>(0);
            if (entitySearchArray != null)
            {
                datasetsList = new List<ObsDataset>(entitySearchArray.Count);
                logger.Info("Number of Datasets={0}", entitySearchArray.Count);

                foreach (JObject entitySearchObject in entitySearchArray)
                {
                    JObject datasetObject = (JObject)JSONHelper.getJTokenValueFromJToken(entitySearchObject, "dataset"); 
                    if (datasetObject != null)
                    {
                        ObsDataset dataset = new ObsDataset(datasetObject, currentUser);

                        datasetsList.Add(dataset);

                        logger.Trace("Dataset={0}", dataset);
                        loggerConsole.Trace("Found {0}", dataset);
                    }
                }
                
                datasetsList = datasetsList.OrderBy(d => d.package).ThenBy(d => d.name).ToList();
            }

            return datasetsList;
        }

        private ObsDataset getDataset(AuthenticatedUser currentUser, string datasetId)
        {
            string entitySearchResults = ObserveConnection.dataset_single(currentUser, datasetId);
            if (entitySearchResults.Length == 0)
            {
                throw new InvalidDataException(String.Format("Invalid response on dataset_single for {0}", currentUser));
            }

            JObject entitySearchResultsObject = JObject.Parse(entitySearchResults);
            JObject datasetObject = (JObject)JSONHelper.getJTokenValueFromJToken(entitySearchResultsObject["data"], "dataset");
            if (datasetObject != null)
            {
                ObsDataset dataset = new ObsDataset(datasetObject, currentUser);

                logger.Trace("Dataset={0}", dataset);
                loggerConsole.Trace("Found {0}", dataset);

                return dataset;
            }

            return null;
        }

        internal List<ObsDashboard> getAllDashboards(AuthenticatedUser currentUser)
        {
            string entitySearchResults = ObserveConnection.dashboardSearch_all(currentUser, currentUser.WorkspaceID);
            if (entitySearchResults.Length == 0)
            {
                throw new InvalidDataException(String.Format("Invalid response on dashboardSearch_all for {0}", currentUser));
            }

            JObject entitySearchResultsObject = JObject.Parse(entitySearchResults);
            JArray entitySearchArray = (JArray)JSONHelper.getJTokenValueFromJToken(JSONHelper.getJTokenValueFromJToken(entitySearchResultsObject["data"], "dashboardSearch"), "dashboards");

            List<ObsDashboard> dashboardsList = new List<ObsDashboard>(0);
            if (entitySearchArray != null)
            {
                dashboardsList = new List<ObsDashboard>(entitySearchArray.Count);
                logger.Info("Number of Dashboards={0}", entitySearchArray.Count);

                foreach (JObject entitySearchObject in entitySearchArray)
                {
                    JObject dashboardObject = (JObject)JSONHelper.getJTokenValueFromJToken(entitySearchObject, "dashboard"); 
                    if (dashboardObject != null)
                    {
                        ObsDashboard dashboard = new ObsDashboard(dashboardObject, currentUser);

                        dashboardsList.Add(dashboard);

                        logger.Trace("Dashboard={0}", dashboard);
                        loggerConsole.Trace("Found {0}", dashboard);
                    }
                }
                
                dashboardsList = dashboardsList.OrderBy(d => d.package).ThenBy(d => d.name).ToList();
            }

            return dashboardsList;
        }

        internal List<ObsMonitor> getAllMonitors(AuthenticatedUser currentUser)
        {
            string entitySearchResults = ObserveConnection.monitorSearch_all(currentUser, currentUser.WorkspaceID);
            if (entitySearchResults.Length == 0)
            {
                throw new InvalidDataException(String.Format("Invalid response on monitorSearch_all for {0}", currentUser));
            }

            JObject entitySearchResultsObject = JObject.Parse(entitySearchResults);
            JArray entitySearchArray = (JArray)JSONHelper.getJTokenValueFromJToken(entitySearchResultsObject["data"], "monitorsInWorkspace");

            List<ObsMonitor> monitorsList = new List<ObsMonitor>(0);
            if (entitySearchArray != null)
            {
                monitorsList = new List<ObsMonitor>(entitySearchArray.Count);
                logger.Info("Number of Monitors={0}", entitySearchArray.Count);

                foreach (JObject entitySearchObject in entitySearchArray)
                {
                    ObsMonitor monitor = new ObsMonitor(entitySearchObject, currentUser);

                    monitorsList.Add(monitor);

                    logger.Trace("Monitor={0}", monitor);
                    loggerConsole.Trace("Found {0}", monitor);
                }
                
                monitorsList = monitorsList.OrderBy(d => d.package).ThenBy(d => d.name).ToList();
            }

            return monitorsList;
        }        

        internal List<ObsWorksheet> getAllWorksheets(AuthenticatedUser currentUser)
        {
            string entitySearchResults = ObserveConnection.worksheetSearch_all(currentUser, currentUser.WorkspaceID);
            if (entitySearchResults.Length == 0)
            {
                throw new InvalidDataException(String.Format("Invalid response on worksheetSearch_all for {0}", currentUser));
            }

            JObject entitySearchResultsObject = JObject.Parse(entitySearchResults);
            JArray entitySearchArray = (JArray)JSONHelper.getJTokenValueFromJToken(JSONHelper.getJTokenValueFromJToken(entitySearchResultsObject["data"], "worksheetSearch"), "worksheets");

            List<ObsWorksheet> worksheetsList = new List<ObsWorksheet>(0);
            if (entitySearchArray != null)
            {
                worksheetsList = new List<ObsWorksheet>(entitySearchArray.Count);
                logger.Info("Number of Worksheets={0}", entitySearchArray.Count);

                foreach (JObject entitySearchObject in entitySearchArray)
                {
                    JObject dashboardObject = (JObject)JSONHelper.getJTokenValueFromJToken(entitySearchObject, "worksheet"); 
                    if (dashboardObject != null)
                    {
                        ObsWorksheet worksheet = new ObsWorksheet(dashboardObject, currentUser);

                        worksheetsList.Add(worksheet);

                        logger.Trace("Worksheet={0}", worksheet);
                        loggerConsole.Trace("Found {0}", worksheet);
                    }
                }
                
                worksheetsList = worksheetsList.OrderBy(d => d.package).ThenBy(d => d.name).ToList();
            }

            return worksheetsList;
        }

        private ObsWorksheet getWorksheet(AuthenticatedUser currentUser, string worksheetId)
        {
            string entitySearchResults = ObserveConnection.worksheet_single(currentUser, worksheetId);
            if (entitySearchResults.Length == 0)
            {
                throw new InvalidDataException(String.Format("Invalid response on worksheet_single for {0}", currentUser));
            }

            JObject entitySearchResultsObject = JObject.Parse(entitySearchResults);
            JObject worksheetObject = (JObject)JSONHelper.getJTokenValueFromJToken(entitySearchResultsObject["data"], "worksheet");
            if (worksheetObject != null)
            {
                ObsWorksheet worksheet = new ObsWorksheet(worksheetObject, currentUser);

                logger.Trace("Worksheet={0}", worksheet);
                loggerConsole.Trace("Found {0}", worksheet);

                return worksheet;
            }

            return null;
        }

        internal List<ObsCreditsTransform> getUsageTransform(AuthenticatedUser currentUser, int intervalHours)
        {
            List<ObsCreditsTransform> usageList = new List<ObsCreditsTransform>(1000);
            try
            {
                string queryResults = ObserveConnection.getUsageData_transform(currentUser, intervalHours);
                if (queryResults.Length == 0)
                {
                    throw new InvalidDataException(String.Format("Invalid response on getUsageData_transform for {0}", currentUser));
                }

                usageList = CsvHelperHelper.ReadListFromCSVString<ObsCreditsTransform>(queryResults);

            }
            catch (WebException ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);
            }

            return usageList;
        }

        internal List<ObsCreditsMonitor> getUsageMonitor(AuthenticatedUser currentUser, int intervalHours)
        {
            List<ObsCreditsMonitor> usageList = new List<ObsCreditsMonitor>(1000);
            try
            {
                string queryResults = ObserveConnection.getUsageData_monitor(currentUser, intervalHours);
                if (queryResults.Length == 0)
                {
                    throw new InvalidDataException(String.Format("Invalid response on getUsageData_monitor for {0}", currentUser));
                }

                usageList = CsvHelperHelper.ReadListFromCSVString<ObsCreditsMonitor>(queryResults);

            }
            catch (WebException ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);
            }

            return usageList;
        }

        internal List<ObsCreditsQuery> getUsageQuery(AuthenticatedUser currentUser, int intervalHours)
        {
            List<ObsCreditsQuery> usageList = new List<ObsCreditsQuery>(1000);
            try
            {
                string queryResults = ObserveConnection.getUsageData_query(currentUser, intervalHours);
                if (queryResults.Length == 0)
                {
                    throw new InvalidDataException(String.Format("Invalid response on getUsageData_query for {0}", currentUser));
                }

                usageList = CsvHelperHelper.ReadListFromCSVString<ObsCreditsQuery>(queryResults);

            }
            catch (WebException ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);
            }

            return usageList;
        }
    }
}
