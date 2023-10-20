using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Observe.EntityExplorer.DataObjects;

namespace Observe.EntityExplorer.Controllers;

public class CommonControllerMethods
{
    public IConfiguration Configuration { get; }
    public IMemoryCache MemoryCache { get; }

    private Logger logger = LogManager.GetCurrentClassLogger();
    private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");

    private Controller Controller { get; }

    public CommonControllerMethods(Logger logger, IConfiguration configuration, Controller controller, IMemoryCache memoryCache)
    {
        this.logger = logger;
        this.Configuration = configuration;
        this.Controller = controller;
        this.MemoryCache = memoryCache;
    }

    public AuthenticatedUser GetUser(string userid)
    {
        List<AuthenticatedUser> allUsers = GetAllUsers();
        AuthenticatedUser authenticatedUser = allUsers.Where(u => u.UniqueID == userid).FirstOrDefault();
        return authenticatedUser;
    }

    // public AuthenticatedUser GetCurrentUser()
    // {
    //     AuthenticatedUser currentUser = null;
        
    //     // Load AuthenticatedUser from cookie if it exists
    //     try
    //     {
    //         string serializedData = this.Controller.Request.Cookies["Observe.EntityExplorer.CurrentUser"];
    //         if (serializedData != null && serializedData.Length > 0)
    //         {
    //             currentUser = JsonConvert.DeserializeObject<AuthenticatedUser>(Encoding.UTF8.GetString(Convert.FromBase64String(serializedData)));
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.Error(ex);
    //         loggerConsole.Error(ex);
    //     }

    //     return currentUser;
    // }

    // public void SaveCurrentUser(AuthenticatedUser currentUser)
    // {
    //     if (currentUser != null)
    //     {
    //         // Save AuthenticatedUser into cookie
    //         CookieOptions cookieOptions = new CookieOptions();
    //         cookieOptions.Expires = DateTimeOffset.Now.AddDays(7);
    //         this.Controller.HttpContext.Response.Cookies.Append("Observe.EntityExplorer.CurrentUser", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(currentUser))), cookieOptions);
    //     }
    //     else
    //     {
    //         this.Controller.HttpContext.Response.Cookies.Delete("Observe.EntityExplorer.CurrentUser");
    //     }
    // }

    public List<AuthenticatedUser> GetAllUsers()
    {
        List<AuthenticatedUser> allUsers = new List<AuthenticatedUser>();
        
        // Load AuthenticatedUser from cookie if it exists
        try
        {
            string serializedData = this.Controller.Request.Cookies["Observe.EntityExplorer.AllUsers"];
            if (serializedData != null && serializedData.Length > 0)
            {
                allUsers = JsonConvert.DeserializeObject<List<AuthenticatedUser>>(Encoding.UTF8.GetString(Convert.FromBase64String(serializedData)));
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            loggerConsole.Error(ex);
        }

        return allUsers;
    }

    public void SaveAllUsers(List<AuthenticatedUser> allUsers)
    {
        // Save all users into cookie
        CookieOptions cookieOptions = new CookieOptions();
        cookieOptions.Expires = DateTimeOffset.Now.AddDays(28);
        this.Controller.HttpContext.Response.Cookies.Append("Observe.EntityExplorer.AllUsers", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(allUsers))), cookieOptions);
    }

    public ObserveEnvironment GetObserveEnvironment(AuthenticatedUser currentUser) 
    {
        ObserveEnvironment observeEnvironment = null;

        string observeEnvironmentCacheKey = String.Format("ObserveEnvironment-{0}", currentUser.UniqueID);
        if (this.MemoryCache.TryGetValue(observeEnvironmentCacheKey, out observeEnvironment) == true)
        {
            logger.Info(
                "Found ObserveEnvironment {0} / {1} / {2} / {3} in {4} cache, valid as of {5:u}", 
                observeEnvironment.CustomerEnvironmentUrl, 
                observeEnvironment.CustomerName,
                observeEnvironment.CustomerLabel,
                observeEnvironment.Deployment,
                observeEnvironmentCacheKey, 
                observeEnvironment.LoadedOn);

            return observeEnvironment;
        }
        else
        {
            logger.Info("No {0} ObserveEnvironment in cache, will create", observeEnvironmentCacheKey);

            // Let's build it 
            observeEnvironment = new ObserveEnvironment(currentUser);

            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(3));
            this.MemoryCache.Set(observeEnvironmentCacheKey, observeEnvironment, cacheEntryOptions);

            logger.Info(
                "Cached ObserveEnvironment {0} / {1} / {2} / {3} in {4} cache, valid as of {5:u}", 
                observeEnvironment.CustomerEnvironmentUrl, 
                observeEnvironment.CustomerName,
                observeEnvironment.CustomerLabel,
                observeEnvironment.Deployment,
                observeEnvironmentCacheKey, 
                observeEnvironment.LoadedOn);

            return observeEnvironment;
        }
    }

    public void RemoveObserveEnvironment(AuthenticatedUser currentUser)
    {
        string observeEnvironmentCacheKey = String.Format("ObserveEnvironment-{0}", currentUser.UniqueID);
        this.MemoryCache.Remove(observeEnvironmentCacheKey);
        logger.Info("Removed {0} cache", observeEnvironmentCacheKey);
    }
}
