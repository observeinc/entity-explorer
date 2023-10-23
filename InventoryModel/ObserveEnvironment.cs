using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Observe.EntityExplorer.DataObjects;
using System;
using System.Text;
using System.Net;

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

        // All Dashboards, Worksheets and Datasets
        public List<ObsCompositeObject> ObserveObjects { get; set; } = new List<ObsCompositeObject>(256);
        // All relationships between Dashboards, Worksheets and Datasets, as well as between Stages in Dashboards, Worksheets and Datasets
        public List<ObjectRelationship> ObjectRelationships { get; set; } = new List<ObjectRelationship>(256 * 8);
        
        // Individual entities by type
        public Dictionary<string, ObsDataset> AllDatasetsDict { get; set; }
        public Dictionary<string, ObsDashboard> AllDashboardsDict { get; set; }
        public Dictionary<string, ObsWorksheet> AllWorksheetsDict { get; set; }
        public Dictionary<string, ObsMonitor> allMonitorsDict { get; set; }
        
        public ObserveEnvironment () {}

        public ObserveEnvironment(AuthenticatedUser currentUser)
        {
            this.CustomerLabel = currentUser.CustomerLabel;
            this.CustomerEnvironmentUrl = currentUser.CustomerEnvironmentUrl;
            this.CustomerName = currentUser.CustomerName;
            this.Deployment = currentUser.Deployment;

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

            // Get all dashboards
            List<ObsDashboard> allDashboards = getAllDashboards(currentUser);
            this.AllDashboardsDict = allDashboards.ToDictionary(d => d.id, d => d);

            // Enrich their parameters and relationships
            foreach (ObsDashboard dashboard in allDashboards)
            {
                dashboard.AddStages(this.AllDatasetsDict);
                // dashboard.AddRelatedKeys(this.AllDatasetsDict);
                // dashboard.AddForeignKeys(this.AllDatasetsDict);
                dashboard.PopulateExternalDatasetRelationships(this.AllDatasetsDict);
                this.ObjectRelationships.AddRange(dashboard.ExternalObjectRelationships);
            }
            this.ObserveObjects.AddRange(allDashboards);

            this.LoadedOn = DateTime.UtcNow;
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
                }
            }
        }

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
                            switch (datasetsGroupedByOriginTypeGroup.Key)
                            {
                                case ObsObjectOriginType.DataStream:
                                    sb.AppendLine("  subgraph cluster_ds_datastream {");
                                    sb.AppendFormat("    label=\"🎏 DataStreams ({0})\"", allDatasetsInGroup.Count).AppendLine();
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

                                        sb.AppendFormat("  subgraph cluster_ds_app_{0} {{", allAppDatasetsGroupedByPackageGroup.Key).AppendLine();
                                        sb.AppendFormat("    label=\"📊 App Datasets [{0}] ({1})\" style=\"filled\" fillcolor=\"lightyellow\"", allAppDatasetsGroupedByPackageGroup.Key, allDatasetsInAppGroup.Count).AppendLine();
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
                                    sb.AppendFormat("    label=\"📈 Metric Support Datasets ({0})\" style=\"filled\" fillcolor=\"seashell1\"", obsDatasetsMetric.Count).AppendLine();
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
                                    sb.AppendFormat("    label=\"🛤️ Terraformed Datasets ({0})\" style=\"filled\" fillcolor=\"linen\"", allDatasetsInGroup.Count).AppendLine();
                                    foreach(ObsDataset dataset in allDatasetsInGroup)
                                    {
                                        if (dataset == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dataset)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.User:
                                    sb.AppendLine("  subgraph cluster_ds_user {");
                                    sb.AppendFormat("    label=\"👋 User Datasets ({0})\" style=\"filled\" fillcolor=\"palegreen\"", allDatasetsInGroup.Count).AppendLine();
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
                            ObsCompositeObject parentDatasetOrDashboardOrWorksheetOfThisGroup = stagesGroupedByParentDatasetOrDashboardGroup.Key;
                            if (parentDatasetOrDashboardOrWorksheetOfThisGroup is ObsDataset)
                            {
                                ObsDataset parentDatasetOfThisGroup = (ObsDataset)parentDatasetOrDashboardOrWorksheetOfThisGroup;
                                List<ObsStage> allStagesInGroup = stagesGroupedByParentDatasetOrDashboardGroup.ToList();

                                sb.AppendFormat("  subgraph cluster_dataset_{0} {{", getGraphVizNodeName(parentDatasetOfThisGroup, false)).AppendLine();
                                sb.AppendFormat("    label=\"🎈🎫 Dataset {0}\" style=\"filled\" fillcolor=\"lavender\"", WebUtility.HtmlEncode(parentDatasetOfThisGroup.name)).AppendLine();
                                sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parentDatasetOfThisGroup)).AppendLine();
                                foreach(ObsStage stage in allStagesInGroup)
                                {
                                    sb.AppendFormat("    {0}", getGraphVizNodeDefinition(stage)).AppendLine();
                                }
                                sb.AppendLine("  }");
                            }
                            else if (parentDatasetOrDashboardOrWorksheetOfThisGroup is ObsDashboard)
                            {
                                ObsDashboard parentDashboardOfThisGroup = (ObsDashboard)parentDatasetOrDashboardOrWorksheetOfThisGroup;
                                List<ObsStage> allStagesInGroup = stagesGroupedByParentDatasetOrDashboardGroup.ToList();

                                sb.AppendFormat("  subgraph cluster_dashboard_{0} {{", getGraphVizNodeName(parentDashboardOfThisGroup, false)).AppendLine();
                                sb.AppendFormat("    label=\"🎈📈 Dashboard {0}\" style=\"filled\" fillcolor=\"lavender\"", WebUtility.HtmlEncode(parentDashboardOfThisGroup.name)).AppendLine();
                                sb.AppendFormat("    {0}", getGraphVizNodeDefinition(parentDashboardOfThisGroup)).AppendLine();
                                foreach(ObsStage stage in allStagesInGroup)
                                {
                                    sb.AppendFormat("    {0}", getGraphVizNodeDefinition(stage)).AppendLine();
                                }
                                sb.AppendLine("  }");
                            }
                            else if (parentDatasetOrDashboardOrWorksheetOfThisGroup is ObsWorksheet)
                            {
                                throw new NotImplementedException("ObsWorksheet grouping not implemented yet");
                            }
                            else
                            {
                                // Something else?
                            }
                        }

                        break;

                    case "ObsDashboard":
                        List<ObsDashboard> allDashboards = allObjectsGroupedByTypeGroup.Cast<ObsDashboard>().ToList();

                        var dashboardsGroupedByOriginType = allDashboards.GroupBy(d => d.OriginType);
                        foreach (var dashboardsGroupedByOriginTypeGroup in dashboardsGroupedByOriginType)
                        {
                            List<ObsDashboard> allDashboardsInGroup = dashboardsGroupedByOriginTypeGroup.ToList();
                            switch (dashboardsGroupedByOriginTypeGroup.Key)
                            {
                                case ObsObjectOriginType.App:
                                    var allAppDashboardsGroupedByPackage = allDashboardsInGroup.GroupBy(d => d.package);
                                    foreach (var allAppDashboardsGroupedByPackageGroup in allAppDashboardsGroupedByPackage)
                                    {
                                        List<ObsDashboard> allDatasetsInAppGroup = allAppDashboardsGroupedByPackageGroup.ToList();

                                        sb.AppendFormat("  subgraph cluster_da_app_{0} {{", allAppDashboardsGroupedByPackageGroup.Key).AppendLine();
                                        sb.AppendFormat("    label=\"📊 App Dashboards [{0}] ({1})\" style=\"filled\" fillcolor=\"wheat\"", allAppDashboardsGroupedByPackageGroup.Key, allDatasetsInAppGroup.Count).AppendLine();
                                        foreach(ObsDashboard dashboard in allDatasetsInAppGroup)
                                        {
                                            if (dashboard == interestingObject) continue;
                                            sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dashboard)).AppendLine();
                                        }
                                        sb.AppendLine("  }");
                                    }

                                    break;

                                case ObsObjectOriginType.System:
                                    sb.AppendLine("  subgraph cluster_da_system {");
                                    sb.AppendFormat("    label=\"📈 System Dashboards ({0})\" style=\"filled\" fillcolor=\"mistyrose\"", allDashboardsInGroup.Count).AppendLine();
                                    foreach(ObsDashboard dashboard in allDashboardsInGroup)
                                    {
                                        if (dashboard == interestingObject) continue;
                                        sb.AppendFormat("    {0}", getGraphVizNodeDefinition(dashboard)).AppendLine();
                                    }
                                    sb.AppendLine("  }");

                                    break;

                                case ObsObjectOriginType.User:
                                    sb.AppendLine("  subgraph cluster_da_user {");
                                    sb.AppendFormat("    label=\"👋 User Dashboards ({0})\" style=\"filled\" fillcolor=\"ivory\"", allDashboardsInGroup.Count).AppendLine();
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
                
                default:
                    throw new NotImplementedException(String.Format("{0} graph output not supported yet", interestingObject.GetType().Name));
            }

            sb.AppendLine("");

            // Output edges
            sb.AppendLine("// Edges");
            foreach (ObjectRelationship relationship in allRelationships)
            {
                sb.AppendLine(getGraphVizEdgeDefinition(relationship));
            }
            
            // Close
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
            string nodeIcon = "";
            if ((dataset.ObjectType & ObsCompositeObjectType.EventDataset) == ObsCompositeObjectType.EventDataset)
            {
                nodeColor = "purple";
                nodeIcon = "📅";
            }
            else if ((dataset.ObjectType & ObsCompositeObjectType.ResourceDataset) == ObsCompositeObjectType.ResourceDataset)
            {
                nodeColor = "blue";
                nodeIcon = "🗃";
            }
            else if ((dataset.ObjectType & ObsCompositeObjectType.IntervalDataset) == ObsCompositeObjectType.IntervalDataset)
            {
                nodeColor = "maroon";
                nodeIcon = "⏲";
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
            string nodeIcon = "❓";

            switch (stage.type)
            {
                case "table":
                    nodeIcon = "📑";
                    break;

                case "timeseries":
                    nodeIcon = "📉";
                    break;

                case "bar":
                    nodeIcon = "📊";
                    break;

                case "circular":
                    nodeIcon = "🥧";
                    break;

                case "stacked_area":
                    nodeIcon = "🗻";
                    break;

                case "singlevalue":
                    nodeIcon = "#️⃣";
                    break;

                case "list":
                    nodeIcon = "📜";
                    break;

                case "valueovertime":
                    nodeIcon = "⏳";
                    break;

                 case "gantt":
                    // Until we expose this in product I am not displaying an icon
                    break;

                default:
                    break;
            }

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

            // Put 1KB of OPAL into the tooltip
            string toolTip = stage.pipeline;
            int truncateTo = 1024 - 8;
            if (toolTip.Length > truncateTo)
            {
                toolTip = String.Format("{0}...{1}", toolTip.Substring(0, truncateTo), toolTip.Length);
            }

            return String.Format("{0} [label=\"{1}{2} [{3}]\" shape=\"{4}\" color=\"{5}\" tooltip=\"{6}\"]", 
                getGraphVizNodeName(stage), 
                nodeIcon, 
                WebUtility.HtmlEncode(wordWrap(stage.name, 16, new char[] { ' '})), 
                stage.type,
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
                return String.Format("{0}MO_{1}{0}", quoteCharacter, interestingObject.id);
            }
            else if (interestingObject is ObsWorksheet)
            {
                return String.Format("{0}WS_{1}{0}", quoteCharacter, interestingObject.id);
            }
            else if (interestingObject is ObsStage)
            {
                return String.Format("{0}ST_{1}_{2}{0}", quoteCharacter, ((ObsStage)interestingObject).Parent.id, interestingObject.id);
            }

            return String.Empty;
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
    }
}
