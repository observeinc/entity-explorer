using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Models;

namespace Observe.EntityExplorer.Controllers;

public class SearchController : Controller
{
    public IConfiguration Configuration { get; }
    public IMemoryCache MemoryCache { get; }
    public IWebHostEnvironment HostingEnvironment { get; }
    private readonly ILogger<HomeController> _logger;

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");
    
    private CommonControllerMethods CommonControllerMethods { get; }

    public SearchController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this.HostingEnvironment = hostingEnvironment; 
        this.MemoryCache = memoryCache;

        this.CommonControllerMethods = new CommonControllerMethods(SearchController.logger, this.Configuration, this, this.MemoryCache);
    }

    public IActionResult Results(
        string userid,
        string id,
        string query)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        try
        {
            AuthenticatedUser currentUser = this.CommonControllerMethods.GetUser(userid);
            if (currentUser == null)
            {
                return RedirectToAction("Connect", "Connection");
            }
            ObserveEnvironment observeEnvironment = this.CommonControllerMethods.GetObserveEnvironment(currentUser);
            if (observeEnvironment == null)
            {
                throw new Exception("Unable to retrieve the Observe Environment from cache or server");
            }
            SearchResultsViewModel viewModel = new SearchResultsViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    viewModel.SearchQuery = query;

                    // Populate all dataset stages that haven't been populated yet - we lazy load them in advance
                    observeEnvironment.PopulateAllDatasetStages(currentUser);

                    viewModel.SearchResults = new List<ObsStage>(256);

                    foreach (ObsDataset entity in observeEnvironment.AllDatasetsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => StringMatches(s.pipeline, query) == true).ToList());
                    }

                    foreach (ObsDashboard entity in observeEnvironment.AllDashboardsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => StringMatches(s.pipeline, query) == true).ToList());
                    }

                    foreach (ObsMonitor entity in observeEnvironment.AllMonitorsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => StringMatches(s.pipeline, query) == true).ToList());
                    }

                    foreach (ObsWorksheet entity in observeEnvironment.AllWorksheetsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => StringMatches(s.pipeline, query) == true).ToList());
                    }

                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new SearchResultsViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    private bool StringMatches(string stringToSearch, string searchCriteria)
    {
        foreach (Match match in Regex.Matches(stringToSearch, searchCriteria))
        {
            if (match.Success && match.Groups.Count > 0)
            {
                return true;
            }
        }
        return false;
    }
}