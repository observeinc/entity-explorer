using System.Diagnostics;
using System.IO;
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
    
    private string authCacheFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".observe");
    

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

    public List<AuthenticatedUser> GetAllUsers()
    {
        List<AuthenticatedUser> allUsers = new List<AuthenticatedUser>();
        
        // Load AuthenticatedUser from cookie if it exists
        try
        {
            //string serializedData = this.Controller.Request.Cookies["Observe.EntityExplorer.AllUsers"];
            string authCacheFilePath = Path.Combine(authCacheFolderPath, "observe-entity-explorer.auth-cache.json");
            if (Path.Exists(authCacheFilePath) == true)
            {
                string serializedData = File.ReadAllText(authCacheFilePath);
                if (serializedData != null && serializedData.Length > 0)
                {
                    allUsers = JsonConvert.DeserializeObject<List<AuthenticatedUser>>(serializedData);
                }
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
        Directory.CreateDirectory(authCacheFolderPath);
        string authCacheFilePath = Path.Combine(authCacheFolderPath, "observe-entity-explorer.auth-cache.json");
        File.WriteAllText(authCacheFilePath, JsonConvert.SerializeObject(allUsers, Formatting.Indented), Encoding.UTF8);
        // Save all users into cookie
        // CookieOptions cookieOptions = new CookieOptions();
        // cookieOptions.Expires = DateTimeOffset.Now.AddDays(28);
        // this.Controller.HttpContext.Response.Cookies.Append("Observe.EntityExplorer.AllUsers", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(allUsers))), cookieOptions);
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
            observeEnvironment = new ObserveEnvironment(currentUser, this.Controller.HttpContext);

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

    public void enrichTrace(AuthenticatedUser currentUser)
    {
        //Activity.Current.AddTag("currentUser", currentUser.ToString());
        Activity.Current.AddTag("currentUser", currentUser.UserName);
        Activity.Current.AddTag("currentUserID", currentUser.UserID);
        Activity.Current.AddTag("currentUserAuthenticatedOn", currentUser.AuthenticatedOn.ToString("u"));
    }

    public void enrichTrace(ObserveEnvironment observeEnvironment)
    {
        //Activity.Current.AddTag("observeEnvironment", observeEnvironment.ToString());
        Activity.Current.AddTag("observeEnvironmentName", observeEnvironment.CustomerName);
        Activity.Current.AddTag("observeEnvironmentLabel", observeEnvironment.CustomerLabel);
        Activity.Current.AddTag("observeEnvironmentUrl", observeEnvironment.CustomerEnvironmentUrl);
        Activity.Current.AddTag("observeEnvironmentLoadedOn", observeEnvironment.LoadedOn.ToString("u"));
    }

    public void enrichTrace(ObsDataset thisObject)
    {
        Activity.Current.AddTag("currentObject", thisObject.ToString());
    }

    public void enrichTrace(ObsMonitor thisObject)
    {
        Activity.Current.AddTag("currentObject", thisObject.ToString());
    }

    public void enrichTrace(ObsDashboard thisObject)
    {
        Activity.Current.AddTag("currentObject", thisObject.ToString());
    }

    public void enrichTrace(ObsWorksheet thisObject)
    {
        Activity.Current.AddTag("currentObject", thisObject.ToString());
    }

    public void enrichTrace(Observe.EntityExplorer.Models.SearchResultsViewModel viewModel)
    {
        Activity.Current.AddTag("searchQuery", viewModel.SearchQuery);
        Activity.Current.AddTag("resultsFound", viewModel.SearchResults.Count);
    }
}
