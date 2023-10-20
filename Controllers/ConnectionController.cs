using System.Diagnostics;
using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Observe.EntityExplorer.DataObjects;
using Observe.EntityExplorer.Models;
using NLog;
using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.Controllers;

public class ConnectionController : Controller
{
    public IConfiguration Configuration { get; }
    public IMemoryCache MemoryCache { get; }
    public IWebHostEnvironment HostingEnvironment { get; }
    private readonly ILogger<ConnectionController> _logger;

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");

    private CommonControllerMethods CommonControllerMethods { get; }

    public ConnectionController(ILogger<ConnectionController> logger, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this.HostingEnvironment = hostingEnvironment; 
        this.MemoryCache = memoryCache;
        
        this.CommonControllerMethods = new CommonControllerMethods(ConnectionController.logger, this.Configuration, this, this.MemoryCache);
    }

    public IActionResult Connect()
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
       
        try
        {
            // AuthenticatedUser currentUserOrig = this.CommonControllerMethods.GetCurrentUser();
            ConnectionConnectViewModel viewModel = new ConnectionConnectViewModel(null);
            viewModel.AllUsers = this.CommonControllerMethods.GetAllUsers();
            
            switch (HttpContext.Request.Method)
            {
                case "GET":
                    break;

                case "POST":                   
                    if (HttpContext.Request.Form["buttonConnect"].ToString() == "ConnectWithCredentials")
                    {
                        // Authenticate user from credentials
                        AuthenticatedUser currentUser = null;
                        currentUser = authenticateUser(HttpContext.Request.Form["textBoxEnvironment"], HttpContext.Request.Form["textBoxUsername"], HttpContext.Request.Form["textBoxPassword"]);
                                                                
                        // Cache this user in all environments list
                        viewModel.AllUsers.Add(currentUser);
                        viewModel.AllUsers = viewModel.AllUsers.DistinctBy(u => u.UniqueID).ToList();
                        this.CommonControllerMethods.SaveAllUsers(viewModel.AllUsers);

                        // Save credentials for subsequent pages
                        //this.CommonControllerMethods.SaveCurrentUser(currentUser);
                        viewModel.CurrentUser = currentUser;

                        return RedirectToAction("Index", "Home", new {userid = currentUser.UniqueID});

                    }
                    // else if (HttpContext.Request.Form["buttonConnect"].ToString().Length > 0)
                    // {
                    //     // Authenticate from list of cached credentials
                    //     AuthenticatedUser currentUser = viewModel.AllUsers.Where(u => u.UniqueID == HttpContext.Request.Form["buttonConnect"].ToString()).FirstOrDefault();
                    //     if (currentUser != null)
                    //     {
                    //         // Save credentials for subsequent pages
                    //         this.CommonControllerMethods.SaveCurrentUser(currentUser);
                    //         viewModel.CurrentUser = currentUser;
                    //         return RedirectToAction("Index", "Home");
                    //     }
                    //     else
                    //     {
                    //         throw new Exception(String.Format("Can't find {0} in the list of cached authenticated users", HttpContext.Request.Form["buttonConnect"].ToString()));
                    //     }
                    // }
                    else if (HttpContext.Request.Form["buttonRemove"].ToString().Length > 0)
                    {
                        // Remove the saved credentials from the list
                        viewModel.AllUsers.RemoveAll(u => u.UniqueID == HttpContext.Request.Form["buttonRemove"].ToString());
                        this.CommonControllerMethods.SaveAllUsers(viewModel.AllUsers);
                        return RedirectToAction("Connect", "Connection");
                    }

                    break;                    
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            loggerConsole.Error(ex);

            ViewData["ErrorMessage"] = ex.Message;

            return View(new ConnectionConnectViewModel(null));
        }
        finally
        {
            stopWatch.Stop();

            logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }
    }

    private AuthenticatedUser authenticateUser(string environment, string userName, string userPassword)
    {
        AuthenticatedUser currentUser = new AuthenticatedUser();

        currentUser.UserName = userName;
        logger.Info("currentUser.UserName={0}", currentUser.UserName);

        #region Parse the environment into customer deployment

        logger.Info("textBoxEnvironment={0}", environment);

        // For customer:
        //      https://123580374103.observeinc.com
        //      https://111775605936.observe-eng.com
        // Environment can be:
        // Customer ID: 123580374103, 111775605936
        //    if this is passed, assume our main deployment observeinc.com
        // Host name: 123580374103.observeinc.com, 111775605936.observe-eng.com
        // URL: https://123580374103.observeinc.com, https://111775605936.observe-eng.com

        // First, check if it is URL
        try
        {
            Uri environmentUri = new Uri(environment);
            currentUser.CustomerEnvironmentUrl = environmentUri;
            
            logger.Info("textBoxEnvironment is a full URL");
        }
        catch (UriFormatException ex)
        {
            logger.Info("textBoxEnvironment is not a valid URI");
            logger.Warn(ex);
        }

        // Second, check if it just the customer name
        if (currentUser.CustomerEnvironmentUrl == null)
        {
            if (environment.IndexOf('.') == -1)
            {
                logger.Info("textBoxEnvironment is a customer name. Assuming prod deployment");

                // The customer names have no periods in them
                // Assume that we are in the prod deployment
                UriBuilder uriBuilder = new UriBuilder(Uri.UriSchemeHttps, String.Format("{0}.observeinc.com", environment));
                currentUser.CustomerEnvironmentUrl = uriBuilder.Uri;
            }
            else
            {
                logger.Info("textBoxEnvironment is a host name. Adding scheme");

                // User passed hostname without http and so on
                UriBuilder uriBuilder = new UriBuilder(Uri.UriSchemeHttps, environment);
                currentUser.CustomerEnvironmentUrl = uriBuilder.Uri;
            }
        }
        logger.Info("currentUser.CustomerEnvironmentUrl={0}", currentUser.CustomerEnvironmentUrl);

        // Now that we have it parsed, identify the Customer components
        string[] customerDeploymentTokens = currentUser.CustomerEnvironmentUrl.Host.Split('.');
        currentUser.CustomerName = customerDeploymentTokens[0];
        string potentialDeployment = currentUser.CustomerEnvironmentUrl.Host.Substring(currentUser.CustomerName.Length + 1).ToLower();
        switch (potentialDeployment)
        {
            case "observeinc.com":
                currentUser.Deployment = "PROD";
                break;
            case "observe-eng.com":
                currentUser.Deployment = "ENG";
                break;
            case "observe-staging.com":
                currentUser.Deployment = "STG";
                break;
            default:
                currentUser.Deployment = potentialDeployment;
                break;
        }

        logger.Info("currentUser.CustomerName={0}", currentUser.CustomerName);
        logger.Info("currentUser.Deployment={0}", currentUser.Deployment);

        #endregion

        #region Authentication process

        // SSO or not?
        if (true)
        {
            loggerConsole.Info("Authenticating {0} via password for customer {1} in {2} environment is accessible at {3}", currentUser.UserName, currentUser.CustomerName, currentUser.Deployment, currentUser.CustomerEnvironmentUrl);

            #region Example results
            // {
            //     "code": 200,
            //     "status": "ok",
            //     "message": "OK",
            //     "data": {
            //         "ok": true,
            //         "authToken": "a;sldjas;djaslkdjaslkdjaslasdja",
            //         "userInfo": {
            //             "userId": "17590",
            //             "userName": "Daniel Odievich",
            //             "userTimezone": "",
            //             "userStatus": "2",
            //             "userRole": "admin"
            //         }
            //     }
            // }                    
            // 
            // {
            //     "code": 401,
            //     "status": "error",
            //     "message": "No such email and password combination exists for the given customer.",
            //     "data": {
            //         "ok": false,
            //         "message": "No such email and password combination exists for the given customer."
            //     }
            // }
            #endregion
            string authenticationResults = ObserveConnection.Authenticate_Username_Password(currentUser.CustomerEnvironmentUrl, currentUser.UserName, userPassword);
            if (authenticationResults.Length == 0)
            {
                throw new InvalidOperationException(String.Format("Invalid response on authenticate user {0}", currentUser.UserName));
            }

            // Were the credentials good?
            JObject authenticationResultsObject = JObject.Parse(authenticationResults);
            if (JSONHelper.getBoolValueFromJToken(authenticationResultsObject, "ok") == false)
            {                    
                // {
                //     "ok": false,
                //     "message": "No such email and password combination exists for the given customer."
                // }
                throw new InvalidCredentialException(String.Format("Unable to authenticate user {0} because of {1}", currentUser.UserName, JSONHelper.getStringValueFromJToken(authenticationResultsObject, "message")));
            }

            // If we got here, we have good credentials and first step is good
            // {
            //     "ok": true,
            //     "access_key": "qDogme6EYnDvycbi-XXXXXXXXXXW"
            // }
            currentUser.AuthToken = JSONHelper.getStringValueFromJToken(authenticationResultsObject, "access_key");
            if (currentUser.AuthToken.Length == 0)
            {
                throw new InvalidDataException(String.Format("No authentication token available for user {0}", currentUser.UserName));
            }
            logger.Info("currentUser.AuthToken={0}", currentUser.AuthToken);

            currentUser.AuthenticatedOn = DateTime.UtcNow;
            logger.Info("currentUser.AuthenticatedOn={0}", currentUser.AuthenticatedOn);
        }
        else
        {
            throw new NotImplementedException("SSO not implemented yet");
        }

        #endregion

        #region More details on user such as label and workspace

        string currentUserResults = ObserveConnection.currentUser(currentUser);
        if (currentUserResults.Length == 0)
        {
            throw new InvalidDataException(String.Format("Invalid response on currentUser for {0}", currentUser));
        }

        JObject currentUserResultsObject = JObject.Parse(currentUserResults);
        JObject currentUserObject = (JObject)JSONHelper.getJTokenValueFromJToken(currentUserResultsObject["data"], "currentUser");
        if (currentUserObject != null)
        {
            currentUser.UserID = JSONHelper.getStringValueFromJToken(currentUserObject, "id");;
            currentUser.DisplayName = JSONHelper.getStringValueFromJToken(currentUserObject, "label");;
            currentUser.CustomerLabel = JSONHelper.getStringValueFromJToken(JSONHelper.getJTokenValueFromJToken(currentUserObject, "customer"), "label");

            // There is only ever 1 workspace ID right now
            JArray workspaceArrayToken = (JArray)JSONHelper.getJTokenValueFromJToken(currentUserObject, "workspaces");
            if (workspaceArrayToken != null)
            {
                foreach (JObject workspaceObject in workspaceArrayToken)
                {
                    currentUser.WorkspaceID = JSONHelper.getStringValueFromJToken(workspaceObject, "id");
                }
            }            
        }

        logger.Info("currentUser.UserID={0}", currentUser.UserID);
        logger.Info("currentUser.DisplayName={0}", currentUser.DisplayName);
        logger.Info("currentUser.CustomerLabel={0}", currentUser.CustomerLabel);
        logger.Info("currentUser.WorkspaceID={0}", currentUser.WorkspaceID);

        #endregion 

        loggerConsole.Info("Welcome user {0} ({1}) in customer {2} ({3})", currentUser.DisplayName, currentUser.UserName, currentUser.CustomerLabel, currentUser.CustomerName);

        return currentUser;
    }
}
