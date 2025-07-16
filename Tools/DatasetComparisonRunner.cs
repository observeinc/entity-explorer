using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Controllers;
using Observe.EntityExplorer.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Observe.EntityExplorer.Tools
{
    public static class DatasetComparisonRunner
    {
        private static (List<string> headers, int count, List<string[]> sample) ParseSummary(string csv)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header,
                BadDataFound = null
            };

            using var reader = new StringReader(csv);
            using var csvReader = new CsvReader(reader, config);
            csvReader.Read();
            csvReader.ReadHeader();
            var headers = csvReader.HeaderRecord?.ToList() ?? new List<string>();
            int rows = 0;
            List<string[]> sample = new();
            while (csvReader.Read())
            {
                if (rows < 5)
                {
                    var rec = headers.Select(h => csvReader.GetField(h)).ToArray();
                    sample.Add(rec);
                }
                rows++;
            }
            return (headers, rows, sample);
        }

        public static IEnumerable<DatasetComparisonResult> CompareIncremental(
            AuthenticatedUser user,
            ObserveEnvironment env,
            ObsDataset baseline,
            ObsDataset optimized,
            IEnumerable<string> dashboardIds)
        {
            var relatedDashboards = env.GetRelationshipsOfRelated(baseline, ObsObjectRelationshipType.ProvidesData)
                .Concat(env.GetRelationshipsOfRelated(baseline, ObsObjectRelationshipType.Linked))
                .Select(r => r.ThisObject)
                .OfType<ObsDashboard>()
                .Where(d => dashboardIds.Contains(d.id))
                .Distinct();

            var relatedMonitors = env.GetRelationshipsOfRelated(baseline, ObsObjectRelationshipType.ProvidesData)
                .Concat(env.GetRelationshipsOfRelated(baseline, ObsObjectRelationshipType.Linked))
                .Select(r => r.ThisObject)
                .OfType<ObsMonitor>()
                .Distinct();

            foreach (var dash in relatedDashboards)
            {
                foreach (var stage in dash.Stages)
                {
                    bool usesDataset = stage.ExternalObjectRelationships.Any(r => r.RelatedObject is ObsDataset ds && ds.id == baseline.id);
                    if (!usesDataset) continue;
                    var res = new DatasetComparisonResult { SourceName = dash.name, StageName = stage.name, Query = stage.pipeline };
                    try
                    {
                    string qBase = ToolsController.BuildStageQuery(stage, baseline, user.WorkspaceName);
                    string baseCsv = ObserveConnection.executeQueryAndGetData(user, qBase, "5m");
                    string qOpt = ToolsController.BuildStageQuery(stage, optimized, user.WorkspaceName);
                    string optCsv = ObserveConnection.executeQueryAndGetData(user, qOpt, "5m");
                        (res.BaselineColumns, res.BaselineRowCount, res.BaselineSample) = ParseSummary(baseCsv);
                        (res.OptimizedColumns, res.OptimizedRowCount, res.OptimizedSample) = ParseSummary(optCsv);
                    res.BaselineCsv = baseCsv;
                    res.OptimizedCsv = optCsv;
                        res.Match = res.BaselineRowCount == res.OptimizedRowCount && res.BaselineColumns.SequenceEqual(res.OptimizedColumns);
                    }
                    catch (Exception ex)
                    {
                        res.Error = ex.Message;
                        if (ex.Message.Contains("based on unknown input dataset"))
                        {
                            res.Match = null;
                        }
                        else
                        {
                            res.Match = false;
                        }
                    }
                    yield return res;
                }
            }

            foreach (var monitor in relatedMonitors)
            {
                var stage = monitor.OutputStage;
                if (stage == null) continue;
                bool usesDataset = stage.ExternalObjectRelationships.Any(r => r.RelatedObject is ObsDataset ds && ds.id == baseline.id);
                if (!usesDataset) continue;
                var res = new DatasetComparisonResult { SourceName = monitor.name, StageName = stage.name, Query = stage.pipeline };
                try
                {
                    string qBase = ToolsController.BuildStageQuery(stage, baseline, user.WorkspaceName);
                    string baseCsv = ObserveConnection.executeQueryAndGetData(user, qBase, "5m");
                    string qOpt = ToolsController.BuildStageQuery(stage, optimized, user.WorkspaceName);
                    string optCsv = ObserveConnection.executeQueryAndGetData(user, qOpt, "5m");
                    (res.BaselineColumns, res.BaselineRowCount, res.BaselineSample) = ParseSummary(baseCsv);
                    (res.OptimizedColumns, res.OptimizedRowCount, res.OptimizedSample) = ParseSummary(optCsv);
                    res.BaselineCsv = baseCsv;
                    res.OptimizedCsv = optCsv;
                    res.Match = res.BaselineRowCount == res.OptimizedRowCount && res.BaselineColumns.SequenceEqual(res.OptimizedColumns);
                }
                catch (Exception ex)
                {
                    res.Error = ex.Message;
                    if (ex.Message.Contains("based on unknown input dataset"))
                    {
                        res.Match = null;
                    }
                    else
                    {
                        res.Match = false;
                    }
                }
                yield return res;
            }
        }

        public static List<DatasetComparisonResult> Compare(
            AuthenticatedUser user,
            ObserveEnvironment env,
            ObsDataset baseline,
            ObsDataset optimized,
            IEnumerable<string> dashboardIds)
        {
            return CompareIncremental(user, env, baseline, optimized, dashboardIds).ToList();
        }
    }
}
