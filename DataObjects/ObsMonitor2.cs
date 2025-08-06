using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsMonitor2 : ObsCompositeObject
    {
        public string kind { get; set; }
        public string package { get; set; }
        public string iconUrl { get; set; }
        
        public bool IsEnabled { get; set; }

        public ObsStage OutputStage { get; set; }
        public List<ObsStage> Stages { get; set; } = new List<ObsStage>(8);
        public Dictionary<string, ObsStage> AllStagesDict { get; set; }

        public List<ObsParameter> Parameters { get; set; } = new List<ObsParameter>(0);
        public Dictionary<string, ObsParameter> AllParametersDict { get; set; }

        public ObsDataset SupportingDataset { get; set; }
        
        public List<ObjectRelationship> ExternalObjectRelationships { get; set; } = new List<ObjectRelationship>(8);
        public List<ObjectRelationship> StageObjectRelationships { get; set; } = new List<ObjectRelationship>(8);

        public ObsCreditsMonitor Transform1H { get; set; } = new ObsCreditsMonitor() {Credits = 0};
        public ObsCreditsMonitor Transform1D { get; set; } = new ObsCreditsMonitor() {Credits = 0};
        public ObsCreditsMonitor Transform1W { get; set; } = new ObsCreditsMonitor() {Credits = 0};
        public ObsCreditsMonitor Transform1M { get; set; } = new ObsCreditsMonitor() {Credits = 0};

        public long NumStages
        { 
            get
            {
                JObject definitionObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "definition");
                if (definitionObject != null)
                {
                    JObject queryObject = (JObject)JSONHelper.getJTokenValueFromJToken(definitionObject, "inputQuery");
                    if (queryObject != null)
                    {
                        JArray stagesArray = (JArray)JSONHelper.getJTokenValueFromJToken(queryObject, "stages");
                        if (stagesArray != null)
                        {
                            return stagesArray.Count;
                        }
                    }
                }
                return 0;
            }
        }

        public long NumActions
        { 
            get
            {
                JArray actionsArray = (JArray)JSONHelper.getJTokenValueFromJToken(this._raw, "actionRules");
                if (actionsArray != null)
                {
                    return actionsArray.Count;
                }
                return 0;
            }
        }

        public override string ToString()
        {
            return String.Format(
                "ObsMonitor2: {0}/{1}/{2}",
                this.name,
                this.id,
                this.ObjectType);
        }

        public ObsMonitor2 () {}

        public ObsMonitor2(JObject entityObject) : base (entityObject)
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

            this.description = JSONHelper.getStringValueFromJToken(entityObject, "description");

            this.ObjectType = ObsCompositeObjectType.Monitor2;

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

            this.kind = JSONHelper.getStringValueFromJToken(entityObject, "ruleKind");
            switch (this.kind)
            {
                case "Threshold":
                    this.ObjectType = ObsCompositeObjectType.Monitor2 | ObsCompositeObjectType.MetricThresholdMonitor;
                    break;

                case "Count":
                    this.ObjectType = ObsCompositeObjectType.Monitor2 | ObsCompositeObjectType.ResourceCountThresholdMonitor;
                    break;

                case "Promote":
                    this.ObjectType = ObsCompositeObjectType.Monitor2 | ObsCompositeObjectType.PromotionMonitor;
                    break;

                default:
                    break;
            }

            this.IsEnabled = !JSONHelper.getBoolValueFromJToken(entityObject, "disabled");
        }

        public void AddSupportingDataset(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;

            JObject metaObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject , "meta");
            if (metaObject != null)
            {
                ObsDataset supportingDataset = null;
                if (allDatasetsDict.TryGetValue(JSONHelper.getStringValueFromJToken(metaObject, "outputDatasetID"), out supportingDataset) == true)
                {
                    this.ExternalObjectRelationships.Add(new ObjectRelationship(String.Format("{0} to {1} as {2}", supportingDataset.ToString(), this, ObsObjectRelationshipType.ProvidesData), this, supportingDataset, ObsObjectRelationshipType.ProvidesData));
                    this.SupportingDataset = supportingDataset;
                }
            }
        }

        public void AddStages(Dictionary<string, ObsDataset> allDatasetsDict)
        {
            JObject entityObject = this._raw;

            JObject definitionObject = (JObject)JSONHelper.getJTokenValueFromJToken(this._raw, "definition");
            if (definitionObject != null)
            {
                JObject queryObject = (JObject)JSONHelper.getJTokenValueFromJToken(definitionObject, "inputQuery");
                if (queryObject != null)
                {
                    // "inputQuery": {
                    //   "outputStage": "stage-tlybd3ap",
                    //   "stages": [
                    //     {
                    //       "id": "stage-tlybd3ap",
                    //       "params": {},
                    //       "pipeline": "@scheduled_history <- @\"github/Workflow run\"{\n  filter true\n      and FIELDS.workflow_run.event = \"schedule\"\n      and int64(FIELDS.workflow_run.run_attempt) = 1\n      and action = \"completed\"\n      and status = \"completed\"\n  \n  pick_col\n      BUNDLE_TIMESTAMP\n      , workflow_name\n      , repo\n      , gap:duration(\n            window(lag(BUNDLE_TIMESTAMP, 1), group_by(repo, workflow_name), frame(back:14d)),\n            BUNDLE_TIMESTAMP\n      ),\n      last_run:format_time(window(max(BUNDLE_TIMESTAMP), group_by(repo, workflow_name), frame(back:14d)), 'YYYY-MM-DD\"T\"HH24:MI:SSTZH:TZM')\n\n  \n  make_col max_gap:window(max(gap), group_by(repo, workflow_name), frame(back:14d))\n}\n\n@short_term <- @scheduled_history {\n  make_resource\n      options(expiry:14d),\n      last_run,\n      primary_key(repo, workflow_name),\n      valid_for(max_gap + 6h)\n}\n\n@long_term <- @scheduled_history {\n  make_resource\n      options(expiry:14d),\n      last_run,\n      primary_key(repo, workflow_name),\n      valid_for(max_gap+1d)\n}\n\n@ <- @long_term {\n    not_exists frame(back:14d), repo=@short_term.repo, workflow_name=@short_term.workflow_name\n}",
                    //       "layout": {
                    //         "cardLinks": [],
                    //         "dataLinks": [],
                    //         "dataTableViewState": {
                    //           "cellValuePresentation": [],
                    //           "columnOrderOverride": [
                    //             [
                    //               0,
                    //               "Valid From"
                    //             ],
                    //             [
                    //               1,
                    //               "Valid To"
                    //             ]
                    //           ],
                    //           "columnVisibility": [],
                    //           "columnWidths": [],
                    //           "defaultColumnWidth": 70,
                    //           "disableFixedLeftColumns": false,
                    //           "minColumnWidth": 60,
                    //           "preserveCellAndRowSelection": true,
                    //           "rowHeights": [],
                    //           "selection": {
                    //             "cells": {},
                    //             "columnSelectSequence": [],
                    //             "columns": {},
                    //             "highlightState": {},
                    //             "rows": {},
                    //             "selectionType": "table"
                    //           },
                    //           "tableWidth": 1701,
                    //           "viewType": "Auto"
                    //         },
                    //         "disableOutput": false,
                    //         "index": 0,
                    //         "inputList": [
                    //           {
                    //             "datasetId": "41804797",
                    //             "id": "query-input-f4wlvuyl",
                    //             "inputName": "github/Workflow run",
                    //             "inputRole": "Data",
                    //             "isUserInput": true
                    //           }
                    //         ],
                    //         "isInternal": false,
                    //         "managers": [
                    //           {
                    //             "id": "fjbrj3d2",
                    //             "isDisabled": true,
                    //             "type": "Vis",
                    //             "vis": {
                    //               "config": {
                    //                 "annotations": [
                    //                   {
                    //                     "enabled": true,
                    //                     "id": "",
                    //                     "source": {
                    //                       "data": {
                    //                         "color": "var(--alarm-level-error)",
                    //                         "orientation": "x",
                    //                         "position": {
                    //                           "end": 1736259763000,
                    //                           "start": 1736194963000
                    //                         },
                    //                         "type": "range"
                    //                       },
                    //                       "description": "Notification triggered",
                    //                       "type": "static"
                    //                     }
                    //                   }
                    //                 ],
                    //                 "color": "Default",
                    //                 "hideGridLines": false,
                    //                 "legend": {
                    //                   "type": "list",
                    //                   "visible": true
                    //                 },
                    //                 "type": "xy",
                    //                 "xConfig": {},
                    //                 "yConfig": {
                    //                   "visible": true
                    //                 }
                    //               },
                    //               "source": {
                    //                 "table": {
                    //                   "transformType": "none",
                    //                   "type": "xy"
                    //                 },
                    //                 "type": "table"
                    //               },
                    //               "type": "timeseries"
                    //             }
                    //           }
                    //         ],
                    //         "queryPresentation": {
                    //           "initialRollupFilter": {
                    //             "mode": "Last"
                    //           },
                    //           "resultKinds": [
                    //             "ResultKindSchema",
                    //             "ResultKindData",
                    //             "ResultKindVolumeStats",
                    //             "ResultKindColumnStats"
                    //           ],
                    //           "rollup": {}
                    //         },
                    //         "selectedStepId": "step-i1sdh0n0",
                    //         "serializable": true,
                    //         "steps": [
                    //           {
                    //             "customName": "Input",
                    //             "customSummary": "github/Workflow run",
                    //             "id": "step-c9otuddi",
                    //             "index": 0,
                    //             "isPinned": false,
                    //             "opal": [],
                    //             "type": "InputStep"
                    //           },
                    //           {
                    //             "customSummary": "",
                    //             "id": "step-i1sdh0n0",
                    //             "index": 1,
                    //             "isPinned": false,
                    //             "opal": [
                    //               "@scheduled_history <- @\"github/Workflow run\"{",
                    //               "  filter true",
                    //               "      and FIELDS.workflow_run.event = \"schedule\"",
                    //               "      and int64(FIELDS.workflow_run.run_attempt) = 1",
                    //               "      and action = \"completed\"",
                    //               "      and status = \"completed\"",
                    //               "  ",
                    //               "  pick_col",
                    //               "      BUNDLE_TIMESTAMP",
                    //               "      , workflow_name",
                    //               "      , repo",
                    //               "      , gap:duration(",
                    //               "            window(lag(BUNDLE_TIMESTAMP, 1), group_by(repo, workflow_name), frame(back:14d)),",
                    //               "            BUNDLE_TIMESTAMP",
                    //               "      ),",
                    //               "      last_run:format_time(window(max(BUNDLE_TIMESTAMP), group_by(repo, workflow_name), frame(back:14d)), 'YYYY-MM-DD\"T\"HH24:MI:SSTZH:TZM')",
                    //               "",
                    //               "  ",
                    //               "  make_col max_gap:window(max(gap), group_by(repo, workflow_name), frame(back:14d))",
                    //               "}",
                    //               "",
                    //               "@short_term <- @scheduled_history {",
                    //               "  make_resource",
                    //               "      options(expiry:14d),",
                    //               "      last_run,",
                    //               "      primary_key(repo, workflow_name),",
                    //               "      valid_for(max_gap + 6h)",
                    //               "}",
                    //               "",
                    //               "@long_term <- @scheduled_history {",
                    //               "  make_resource",
                    //               "      options(expiry:14d),",
                    //               "      last_run,",
                    //               "      primary_key(repo, workflow_name),",
                    //               "      valid_for(max_gap+1d)",
                    //               "}",
                    //               "",
                    //               "@ <- @long_term {",
                    //               "    not_exists frame(back:14d), repo=@short_term.repo, workflow_name=@short_term.workflow_name",
                    //               "}"
                    //             ],
                    //             "type": "unknown"
                    //           }
                    //         ],
                    //         "type": "table",
                    //         "viewModel": {
                    //           "builderOpalTab": "OPAL",
                    //           "inspectRailCollapsed": true,
                    //           "inspectRailPinned": false,
                    //           "inspectRailWidth": 640,
                    //           "railCollapseState": {
                    //             "inputsOutputs": false,
                    //             "minimap": false,
                    //             "note": true,
                    //             "script": true
                    //           },
                    //           "showTimeRuler": true,
                    //           "stageTab": "table"
                    //         }
                    //       },
                    //       "input": [
                    //         {
                    //           "inputName": "github/Workflow run",
                    //           "inputRole": "Data",
                    //           "datasetId": "41804797",
                    //           "datasetPath": null,
                    //           "stageId": "",
                    //           "__typename": "InputDefinition"
                    //         }
                    //       ],
                    //       "__typename": "StageQuery"
                    //     }
                    //   ]
                    // }
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
                        // All stages in Monitor should be marked as visible on graphs, they are never shown in UI but still
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