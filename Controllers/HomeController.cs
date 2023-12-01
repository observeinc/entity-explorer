using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Models;

namespace Observe.EntityExplorer.Controllers;

public class HomeController : Controller
{
    public IConfiguration Configuration { get; }
    public IMemoryCache MemoryCache { get; }
    public IWebHostEnvironment HostingEnvironment { get; }
    private readonly ILogger<HomeController> _logger;

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");
    
    private CommonControllerMethods CommonControllerMethods { get; }

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this.HostingEnvironment = hostingEnvironment; 
        this.MemoryCache = memoryCache;

        this.CommonControllerMethods = new CommonControllerMethods(HomeController.logger, this.Configuration, this, this.MemoryCache);
    }

    public IActionResult Index(
        string userid,
        string id
    )
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

            switch (HttpContext.Request.Method)
            {
                case "GET":
                    break;

                case "POST":
                    if (HttpContext.Request.Form["buttonRefresh"].ToString().Length > 0)
                    {
                        this.CommonControllerMethods.RemoveObserveEnvironment(currentUser);
                        observeEnvironment = this.CommonControllerMethods.GetObserveEnvironment(currentUser);
                        if (observeEnvironment == null)
                        {
                            throw new Exception("Unable to retrieve the Observe Environment from cache or server");
                        }
                    }
                    else if (HttpContext.Request.Form["buttonExportToExcel"].ToString().Length > 0)
                    {
                        throw new NotImplementedException("buttonExportToExcel not implemented yet");
                    }
                    break;
            }

            Activity.Current.AddTag("currentUser", currentUser.ToString());
            Activity.Current.AddTag("observeEnvironment", observeEnvironment.ToString());
            
            return View(new BaseViewModel(currentUser, observeEnvironment));
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