﻿using System;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Models;

namespace Observe.EntityExplorer.Controllers;

public class DetailsController : Controller
{
    public IConfiguration Configuration { get; }
    public IMemoryCache MemoryCache { get; }
    public IWebHostEnvironment HostingEnvironment { get; }
    private readonly ILogger<HomeController> _logger;

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");
    
    private CommonControllerMethods CommonControllerMethods { get; }

    public DetailsController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this.HostingEnvironment = hostingEnvironment; 
        this.MemoryCache = memoryCache;

        this.CommonControllerMethods = new CommonControllerMethods(DetailsController.logger, this.Configuration, this, this.MemoryCache);
    }

    public IActionResult Dataset(
        string userid,
        string id)
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
            DetailsDatasetViewModel viewModel = new DetailsDatasetViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    ObsDataset thisDataset = null;
                    if (observeEnvironment.AllDatasetsDict.TryGetValue(id, out thisDataset) == false)
                    {
                        throw new KeyNotFoundException(String.Format("Unable to retrieve the Observe Dataset {0} from Observe Environment", id));
                    }
                    observeEnvironment.PopulateDatasetStages(currentUser, thisDataset);
                    viewModel.CurrentDataset = thisDataset;
                     
                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel.CurrentDataset);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new DetailsDatasetViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult DatasetOpalExplorer(
        string userid,
        string id)
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
            DetailsDatasetViewModel viewModel = new DetailsDatasetViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    ObsDataset thisDataset = null;
                    if (observeEnvironment.AllDatasetsDict.TryGetValue(id, out thisDataset) == false)
                    {
                        throw new KeyNotFoundException(String.Format("Unable to retrieve the Observe Dataset {0} from Observe Environment", id));
                    }
                    observeEnvironment.PopulateDatasetStages(currentUser, thisDataset);
                    viewModel.CurrentDataset = thisDataset;
                     
                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel.CurrentDataset);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new DetailsDatasetViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Monitor(
        string userid,
        string id)
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
            DetailsMonitorViewModel viewModel = new DetailsMonitorViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    ObsMonitor thisMonitor = null;
                    if (observeEnvironment.AllMonitorsDict.TryGetValue(id, out thisMonitor) == false)
                    {
                        throw new KeyNotFoundException(String.Format("Unable to retrieve the Observe Monitor {0} from Observe Environment", id));
                    }
                    viewModel.CurrentMonitor = thisMonitor;
                     
                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel.CurrentMonitor);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new DetailsMonitorViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Monitor2(
        string userid,
        string id)
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
            DetailsMonitor2ViewModel viewModel = new DetailsMonitor2ViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    ObsMonitor2 thisMonitor = null;
                    if (observeEnvironment.AllMonitors2Dict.TryGetValue(id, out thisMonitor) == false)
                    {
                        throw new KeyNotFoundException(String.Format("Unable to retrieve the Observe Monitor {0} from Observe Environment", id));
                    }
                    viewModel.CurrentMonitor = thisMonitor;
                     
                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel.CurrentMonitor);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new DetailsMonitor2ViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Dashboard(
        string userid,
        string id)
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
            DetailsDashboardViewModel viewModel = new DetailsDashboardViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    ObsDashboard thisDashboard = null;
                    if (observeEnvironment.AllDashboardsDict.TryGetValue(id, out thisDashboard) == false)
                    {
                        throw new KeyNotFoundException(String.Format("Unable to retrieve the Observe Dashboard {0} from Observe Environment", id));
                    }
                    viewModel.CurrentDashboard = thisDashboard;
                     
                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel.CurrentDashboard);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new DetailsDashboardViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Metric(
        string userid,
        string id)
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
            DetailsMetricViewModel viewModel = new DetailsMetricViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            id = Encoding.UTF8.GetString(Convert.FromBase64String(id)); 

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    ObsMetric thisMetric = null;
                    if (observeEnvironment.AllMetricsDict.TryGetValue(id, out thisMetric) == false)
                    {
                        throw new KeyNotFoundException(String.Format("Unable to retrieve the Observe Metric {0} from Observe Environment", id));
                    }
                    viewModel.CurrentMetric = thisMetric;

                    // Populate all dataset stages that haven't been populated yet - we lazy load them in advance
                    observeEnvironment.PopulateAllDatasetStages(currentUser);

                    viewModel.SearchResults = new List<ObsStage>(256);

                    foreach (ObsDataset entity in observeEnvironment.AllDatasetsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => s.Metrics.Contains(thisMetric) == true).ToList());
                    }

                    foreach (ObsDashboard entity in observeEnvironment.AllDashboardsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => s.Metrics.Contains(thisMetric) == true).ToList());
                    }

                    foreach (ObsMonitor entity in observeEnvironment.AllMonitorsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => s.Metrics.Contains(thisMetric) == true).ToList());
                    }

                    foreach (ObsMonitor2 entity in observeEnvironment.AllMonitors2Dict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => s.Metrics.Contains(thisMetric) == true).ToList());
                    }

                    foreach (ObsWorksheet entity in observeEnvironment.AllWorksheetsDict.Values)
                    {
                        viewModel.SearchResults.AddRange(entity.Stages.Where(s => s.Metrics.Contains(thisMetric) == true).ToList());
                    }

                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel.CurrentMetric);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new DetailsMetricViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Worksheet(
        string userid,
        string id)
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
            DetailsWorksheetViewModel viewModel = new DetailsWorksheetViewModel(currentUser, observeEnvironment);

            CommonControllerMethods.enrichTrace(currentUser);
            CommonControllerMethods.enrichTrace(observeEnvironment);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    ObsWorksheet thisWorksheet = null;
                    if (observeEnvironment.AllWorksheetsDict.TryGetValue(id, out thisWorksheet) == false)
                    {
                        throw new KeyNotFoundException(String.Format("Unable to retrieve the Observe Dashboard {0} from Observe Environment", id));
                    }
                    viewModel.CurrentWorksheet = thisWorksheet;
                     
                    break;

                case "POST":
                    break;
            }

            CommonControllerMethods.enrichTrace(viewModel.CurrentWorksheet);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new DetailsWorksheetViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }    
}