using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Observe.EntityExplorer;
using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Models;
using Observe.EntityExplorer.Tools;

#nullable enable
namespace Observe.EntityExplorer.Controllers;

public class ToolsController : Controller
{
    public IConfiguration Configuration { get; }
    public IMemoryCache MemoryCache { get; }
    public IWebHostEnvironment HostingEnvironment { get; }
    private readonly ILogger<HomeController> _logger;

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");

    private CommonControllerMethods CommonControllerMethods { get; }

    public ToolsController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this.HostingEnvironment = hostingEnvironment;
        this.MemoryCache = memoryCache;

        this.CommonControllerMethods = new CommonControllerMethods(ToolsController.logger, this.Configuration, this, this.MemoryCache);
    }


    public IActionResult DatasetComparison(string userid, string baselineDatasetId, string optimizedDatasetId, string[]? dashboardIds)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        AuthenticatedUser currentUser = null;
        ObserveEnvironment observeEnvironment = null;
        DatasetComparisonViewModel viewModel = null;

        try
        {
            currentUser = this.CommonControllerMethods.GetUser(userid);
            if (currentUser == null)
            {
                return RedirectToAction("Connect", "Connection");
            }

            observeEnvironment = this.CommonControllerMethods.GetObserveEnvironment(currentUser);
            if (observeEnvironment == null)
            {
                throw new Exception("Unable to retrieve the Observe Environment from cache or server");
            }

            viewModel = new DatasetComparisonViewModel(currentUser, observeEnvironment);
            viewModel.BaselineDatasetId = baselineDatasetId ?? string.Empty;
            viewModel.OptimizedDatasetId = optimizedDatasetId ?? string.Empty;
            viewModel.BuildId = System.IO.File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss");

            if (!string.IsNullOrEmpty(viewModel.BaselineDatasetId) && observeEnvironment.AllDatasetsDict.TryGetValue(viewModel.BaselineDatasetId, out var baseline))
            {
                viewModel.BaselineDataset = baseline;
                viewModel.BaselineDashboards = observeEnvironment.GetRelationshipsOfRelated(baseline, ObsObjectRelationshipType.ProvidesData)
                    .Concat(observeEnvironment.GetRelationshipsOfRelated(baseline, ObsObjectRelationshipType.Linked))
                    .Select(r => r.ThisObject)
                    .OfType<ObsDashboard>()
                    .Distinct()
                    .OrderBy(d => d.name)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(viewModel.OptimizedDatasetId) && observeEnvironment.AllDatasetsDict.TryGetValue(viewModel.OptimizedDatasetId, out var optimized))
            {
                viewModel.OptimizedDataset = optimized;
            }

            return View(viewModel);
        }
        finally
        {
            stopWatch.Stop();
            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
        }

    public static string BuildStageQuery(ObsStage stage, ObsDataset dataset, string workspaceName)
    {
        string datasetPath = dataset.path;
        if (!datasetPath.StartsWith(workspaceName + ".", StringComparison.OrdinalIgnoreCase))
        {
            datasetPath = $"{workspaceName}.{datasetPath}";
        }

        string sanitized = stage.pipeline.Replace("\r", string.Empty);

        HashSet<string> paramIds = new();
        IEnumerable<ObsParameter> parentParams = Enumerable.Empty<ObsParameter>();

        if (stage.Parent is ObsDashboard dash && dash.AllParametersDict != null)
        {
            parentParams = dash.AllParametersDict.Values;
        }
        else if (stage.Parent is ObsMonitor mon && mon.AllParametersDict != null)
        {
            parentParams = mon.AllParametersDict.Values;
        }

        foreach (var p in parentParams)
        {
            paramIds.Add(p.id);
        }

        foreach (var rel in stage.ExternalObjectRelationships)
        {
            if (rel.RelatedObject is ObsParameter param)
            {
                paramIds.Add(param.id);
            }
        }

        // Also parse the pipeline for parameter references by name or id
        var regex = new System.Text.RegularExpressions.Regex(@"\$([A-Za-z0-9_]+)");
        foreach (System.Text.RegularExpressions.Match m in regex.Matches(stage.pipeline))
        {
            string token = m.Groups[1].Value;
            var param = parentParams.FirstOrDefault(p => p.id == token || p.name == token);
            if (param != null)
            {
                paramIds.Add(param.id);
            }
        }

        Dictionary<string, object?> paramValues = new();
        foreach (var id in paramIds)
        {
            paramValues[id] = null;
        }

        var stageObj = new
        {
            input = new[] { new { inputName = "data", datasetPath } },
            stageID = "stageExec",
            pipeline = sanitized,
            @params = paramValues
        };

        var queryObj = new
        {
            query = new
            {
                outputStage = "stageExec",
                stages = new[] { stageObj }
            },
            rowCount = "100"
        };

        return System.Text.Json.JsonSerializer.Serialize(queryObj);
    }

    public async Task RunDatasetComparison(string userid, string baselineDatasetId, string optimizedDatasetId, [FromQuery] string[] dashboardIds)
    {
        var user = this.CommonControllerMethods.GetUser(userid);
        if (user == null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        var env = this.CommonControllerMethods.GetObserveEnvironment(user);
        if (env == null)
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }
        if (!env.AllDatasetsDict.TryGetValue(baselineDatasetId, out var baseline) ||
            !env.AllDatasetsDict.TryGetValue(optimizedDatasetId, out var optimized))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        foreach (var res in DatasetComparisonRunner.CompareIncremental(user, env, baseline, optimized, dashboardIds))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(res);
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }
        await Response.WriteAsync("data: done\n\n");
    }
}
