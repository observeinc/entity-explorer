﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Models;

namespace Observe.EntityExplorer.Controllers;

public class SelectController : Controller
{
    public IConfiguration Configuration { get; }
    public IMemoryCache MemoryCache { get; }
    public IWebHostEnvironment HostingEnvironment { get; }
    private readonly ILogger<HomeController> _logger;

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");
    
    private CommonControllerMethods CommonControllerMethods { get; }

    public SelectController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this.HostingEnvironment = hostingEnvironment; 
        this.MemoryCache = memoryCache;

        this.CommonControllerMethods = new CommonControllerMethods(SelectController.logger, this.Configuration, this, this.MemoryCache);
    }

    public IActionResult Dataset(
        string userid)
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
                ViewData["ErrorMessage"] = "Unable to retrieve the Observe Environment from cache or server";
            }

            return View(new BaseViewModel(currentUser, observeEnvironment));
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new BaseViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Monitor(
        string userid)
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
            BaseViewModel viewModel =  new BaseViewModel(currentUser, null);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    // List<Dataset> allDatasets = this.CommonControllerMethods.getMonitorDatasets(viewModel.CurrentUser);
                    // viewModel.AllDatasets = allDatasets;
                    break;

                case "POST":
                    break;
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            loggerConsole.Error(ex);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new BaseViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Dashboard(
        string userid)
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
                ViewData["ErrorMessage"] = "Unable to retrieve the Observe Environment from cache or server";
            }

            return View(new BaseViewModel(currentUser, observeEnvironment));
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            loggerConsole.Error(ex, ex.Message);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new BaseViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    public IActionResult Worksheet(
        string userid)
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
            BaseViewModel viewModel = new BaseViewModel(currentUser, null);

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    // List<Worksheet> allWorksheets = this.CommonControllerMethods.getWorksheets(viewModel.CurrentUser);
                    // viewModel.AllWorksheets = allWorksheets;
                    break;

                case "POST":
                    break;
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            loggerConsole.Error(ex);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new BaseViewModel(null, null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}