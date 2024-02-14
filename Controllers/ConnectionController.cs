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
            ConnectionConnectViewModel viewModel = new ConnectionConnectViewModel(null);
            viewModel.AllUsers = this.CommonControllerMethods.GetAllUsers();
            
            switch (HttpContext.Request.Method)
            {
                case "GET":
                    break;

                case "POST":
                    if (HttpContext.Request.Form["buttonRemove"].ToString().Length > 0)
                    {
                        // Remove the saved credentials from the list
                        viewModel.AllUsers.RemoveAll(u => u.UniqueID == HttpContext.Request.Form["buttonRemove"].ToString());
                        this.CommonControllerMethods.SaveAllUsers(viewModel.AllUsers);
                        return RedirectToAction("Connect", "Connection");
                    }
                    else if (HttpContext.Request.Form["buttonConnect"].ToString().Length > 0)
                    {
                        // Parse user environments
                        AuthenticatedUser currentUser = getAuthenticatedUserEnvironment(HttpContext.Request.Form["textBoxEnvironment"], HttpContext.Request.Form["textBoxUsername"]);

                        switch (HttpContext.Request.Form["buttonConnect"].ToString())
                        {
                            case "ConnectPassword":
                                authenticateUserPassword(currentUser, HttpContext.Request.Form["textBoxPassword"]);
                                getAuthenticatedUserDetails(currentUser);
                                break;

                            case "ConnectToken":
                                authenticateUserToken(currentUser, HttpContext.Request.Form["textBoxToken"]);
                                getAuthenticatedUserDetails(currentUser);
                                break;

                            case "ConnectDelegateStart":
                                JObject ssoAuthStartResultsObject = authenticateUserSSOStart(currentUser);
                                if (ssoAuthStartResultsObject != null)
                                {
                                    viewModel.Username = currentUser.UserName;
                                    viewModel.Environment = currentUser.CustomerEnvironmentUrl.ToString();
                                    viewModel.DelegateToken = JSONHelper.getStringValueFromJToken(ssoAuthStartResultsObject, "serverToken");
                                    viewModel.DelegateURL = JSONHelper.getStringValueFromJToken(ssoAuthStartResultsObject, "url");

                                    return View(viewModel);
                                }
                                break;

                            case "ConnectDelegateComplete":
                                if (authenticateUserSSOComplete(currentUser, HttpContext.Request.Form["textboxDelegateToken"]) == true)
                                {
                                    getAuthenticatedUserDetails(currentUser);
                                }
                                else
                                {
                                    viewModel.Username = currentUser.UserName;
                                    viewModel.Environment = currentUser.CustomerEnvironmentUrl.ToString();
                                    viewModel.DelegateToken = HttpContext.Request.Form["textboxDelegateToken"];
                                    viewModel.DelegateURL = HttpContext.Request.Form["textboxDelegateURL"];

                                    ViewData["ErrorMessage"] = "Not yet done";

                                    return View(viewModel);
                                }
                                break;

                            default:
                                break;
                        }

                        loggerConsole.Info("Welcome user {0} ({1}) in customer {2} ({3})", currentUser.DisplayName, currentUser.UserName, currentUser.CustomerLabel, currentUser.CustomerName);

                        // Cache this user in all environments list
                        viewModel.AllUsers.Add(currentUser);
                        viewModel.AllUsers = viewModel.AllUsers.DistinctBy(u => u.UniqueID).ToList();
                        this.CommonControllerMethods.SaveAllUsers(viewModel.AllUsers);

                        viewModel.CurrentUser = currentUser;

                        CommonControllerMethods.enrichTrace(currentUser);

                        return RedirectToAction("Index", "Home", new {userid = currentUser.UniqueID});
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

    // public IActionResult SSO()
    // {
    //     Stopwatch stopWatch = new Stopwatch();
    //     stopWatch.Start();
       
    //     try
    //     {
    //         ConnectionSSOViewModel viewModel = new ConnectionSSOViewModel(null);
    //         viewModel.Environment = HttpContext.Request.Query["environment"];
    //         viewModel.Username = HttpContext.Request.Query["username"];
    //         viewModel.DelegateURL = HttpContext.Request.Query["deletegateurl"];
            
    //         switch (HttpContext.Request.Method)
    //         {
    //             case "GET":
    //                 AuthenticatedUser currentUser = getAuthenticatedUserEnvironment(viewModel.Environment, viewModel.Username);
    //                 if (authenticateUserSSOComplete(currentUser, HttpContext.Request.Query["deletegatetoken"]) == true)
    //                 {
    //                     getAuthenticatedUserDetails(currentUser);
    //                     // complete authentication

    //                     throw new NotImplementedException("autheticated here! good job! finish this daniel");
    //                 }

    //                 break;

    //             case "POST":
    //                 break;
    //         }

    //         return View(viewModel);
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.Error(ex);
    //         loggerConsole.Error(ex);

    //         ViewData["ErrorMessage"] = ex.Message;

    //         return View(new ConnectionSSOViewModel(null));
    //     }
    //     finally
    //     {
    //         stopWatch.Stop();

    //         logger.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
    //         loggerConsole.Trace("{0}:{1}/{2}: total duration {3:c} ({4} ms)", HttpContext.Request.Method, this.ControllerContext.RouteData.Values["controller"], this.ControllerContext.RouteData.Values["action"], stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
    //     }
    // }

    private AuthenticatedUser getAuthenticatedUserEnvironment(string environment, string userName)
    {
        AuthenticatedUser currentUser = new AuthenticatedUser();

        if (userName == null || userName.Length == 0)
        {
            throw new AuthenticationException("User name is empty");
        }
        
        currentUser.UserName = userName;
        logger.Info("currentUser.UserName={0}", currentUser.UserName);

        // Parse the environment into customer deployment

        if (environment == null || environment.Length == 0)
        {
            throw new AuthenticationException("Account field is empty");
        }
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
        if (currentUser.CustomerEnvironmentUrl == null)
        {
            throw new AuthenticationException("Unable to determine the URL from the passed environment");
        }

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
    
        return currentUser;    
    }

    private bool authenticateUserPassword(AuthenticatedUser currentUser, string userPassword)
    {
        if (userPassword == null || userPassword.Length == 0)
        {
            throw new AuthenticationException("User password is empty");
        }

        loggerConsole.Info("Authenticating {0} via password for customer {1} in {2} environment accessible at {3}", currentUser.UserName, currentUser.CustomerName, currentUser.Deployment, currentUser.CustomerEnvironmentUrl);

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
        string authenticationResults = ObserveConnection.Authenticate_Username_Password(currentUser, userPassword);
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

        return true;
    }

    private bool authenticateUserToken(AuthenticatedUser currentUser, string userAuthToken)
    {
        if (userAuthToken == null || userAuthToken.Length == 0)
        {
            throw new AuthenticationException("User authentication token is empty");
        }

        loggerConsole.Info("Authenticating {0} via token for customer {1} in {2} environment accessible at {3}", currentUser.UserName, currentUser.CustomerName, currentUser.Deployment, currentUser.CustomerEnvironmentUrl);

        currentUser.AuthToken = userAuthToken;

        currentUser.AuthenticatedOn = DateTime.UtcNow;
        logger.Info("currentUser.AuthenticatedOn={0}", currentUser.AuthenticatedOn);

        return true;
    }

    private JObject authenticateUserSSOStart(AuthenticatedUser currentUser)
    {
        loggerConsole.Info("Starting authenticating {0} via SSO for customer {1} in {2} environment accessible at {3}", currentUser.UserName, currentUser.CustomerName, currentUser.Deployment, currentUser.CustomerEnvironmentUrl);

        string authenticationResults = ObserveConnection.Authenticate_SSO_Start(currentUser);
        if (authenticationResults.Length == 0)
        {
            throw new InvalidOperationException(String.Format("Invalid response on authenticate user {0}", currentUser.UserName));
        }

        // Was the start of getting credentials good?
        JObject authenticationResultsObject = JObject.Parse(authenticationResults);
        if (JSONHelper.getBoolValueFromJToken(authenticationResultsObject, "ok") == false)
        {                    
            // {
            //     "ok": false,
            //     "message": "The email address \"xdaniel.odievich@observeinc.com\" does not match customer id ###."
            // }
            // {
            //     "ok": false,
            //     "message": "Malformed login request"
            // }
            // {
            //     "ok": false,
            //     "message": "Unsupported integration"
            // }                    
            throw new InvalidCredentialException(String.Format("Unable to authenticate user {0} because of {1}", currentUser.UserName, JSONHelper.getStringValueFromJToken(authenticationResultsObject, "message")));
        }

        // If we got here, we have good username and request was submitted
        // {
        //     "ok": true,
        //     "url": "https://######.observe-o2.com/settings/account?expectedUid=1799&serverToken=MSX3...2YPW",
        //     "serverToken": "MSX3...2YPW"
        // }
        string serverToken = JSONHelper.getStringValueFromJToken(authenticationResultsObject, "serverToken");
        if (serverToken.Length == 0)
        {
            throw new InvalidDataException(String.Format("No authentication token available for user {0}", currentUser.UserName));
        }
        logger.Info("serverToken={0}", serverToken);

        string acceptanceUrl = JSONHelper.getStringValueFromJToken(authenticationResultsObject, "url");
        if (acceptanceUrl.Length == 0)
        {
            throw new InvalidDataException(String.Format("No authentication token available for user {0}", currentUser.UserName));
        }
        logger.Info("acceptanceUrl={0}", serverToken);

        return authenticationResultsObject;
    }

    private bool authenticateUserSSOComplete(AuthenticatedUser currentUser, string delegateToken)
    {
        if (delegateToken == null || delegateToken.Length == 0)
        {
            throw new AuthenticationException("Delegate token is empty");
        }

        loggerConsole.Info("Completing authenticating {0} via SSO for customer {1} in {2} environment accessible at {3}", currentUser.UserName, currentUser.CustomerName, currentUser.Deployment, currentUser.CustomerEnvironmentUrl);

        string authenticationResults = ObserveConnection.Authenticate_SSO_Complete(currentUser, delegateToken);
        if (authenticationResults.Length == 0)
        {
            throw new InvalidOperationException(String.Format("Invalid response on authenticate user {0}", currentUser.UserName));
        }

        // Was the end of getting credentials good?
        JObject authenticationResultsObject = JObject.Parse(authenticationResults);
        if (JSONHelper.getBoolValueFromJToken(authenticationResultsObject, "ok") == false)
        {             
            // Nothing pending       
            // {
            //     "ok": false,
            //     "message": "No pending login with token \"EGNUI...RBYGP\" exists."
            // }
            throw new InvalidCredentialException(String.Format("Unable to authenticate user {0} because of {1}", currentUser.UserName, JSONHelper.getStringValueFromJToken(authenticationResultsObject, "message")));
        }

        // have the user accepted the token yet?
        bool tokenSettled = JSONHelper.getBoolValueFromJToken(authenticationResultsObject, "settled");
        logger.Info("token settled={0}", tokenSettled);
        if (tokenSettled == false)
        {
            // Not yet accepted
            // {
            //     "ok": true,
            //     "settled": false
            // }
            // nope they haven't
            return false;
        }
        else
        {
            // Denied
            // {
            //     "ok": true,
            //     "settled": true,
            //     "message": "Login denied."
            // }
            // Accepted
            // {
            //     "ok": true,
            //     "settled": true,
            //     "accessKey": "Pl....E5",
            //     "message": "Login verified."
            // }        
            string tokenMessage = JSONHelper.getStringValueFromJToken(authenticationResultsObject, "message");
            logger.Info("token message={0}", tokenMessage);

            currentUser.AuthToken = JSONHelper.getStringValueFromJToken(authenticationResultsObject, "accessKey");
            if (currentUser.AuthToken.Length == 0)
            {
                if (tokenMessage == "Login denied.")
                {
                    throw new InvalidCredentialException(String.Format("Unable to authenticate user {0} because of {1}", currentUser.UserName, tokenMessage));
                }
                return false;
            }
            else
            {
                currentUser.AuthenticatedOn = DateTime.UtcNow;
                logger.Info("currentUser.AuthenticatedOn={0}", currentUser.AuthenticatedOn);

                return true;
            }
        }
    }

    private void getAuthenticatedUserDetails(AuthenticatedUser currentUser)
    {
        #region More details on user such as label and workspace

        string currentUserResults = ObserveConnection.currentUser(currentUser);
        if (currentUserResults.Length == 0)
        {
            throw new InvalidDataException(String.Format("Invalid response on currentUser for {0}", currentUser));
        }

        JObject currentUserResultsObject = JObject.Parse(currentUserResults);
        JObject currentUserObject = (JObject)JSONHelper.getJTokenValueFromJToken(currentUserResultsObject["data"], "currentUser");
        if (currentUserObject == null)
        {
            throw new AuthenticationException("Unable to get authenticated user details from query.CurrentUser");
        }
        else if (currentUserObject != null)
        {
            currentUser.UserID = JSONHelper.getStringValueFromJToken(currentUserObject, "id");
            currentUser.DisplayName = JSONHelper.getStringValueFromJToken(currentUserObject, "label");
            currentUser.CustomerLabel = JSONHelper.getStringValueFromJToken(JSONHelper.getJTokenValueFromJToken(currentUserObject, "customer"), "label");

            // There is only ever 1 workspace ID right now
            JArray workspaceArrayToken = (JArray)JSONHelper.getJTokenValueFromJToken(currentUserObject, "workspaces");
            if (workspaceArrayToken != null)
            {
                foreach (JObject workspaceObject in workspaceArrayToken)
                {
                    currentUser.WorkspaceID = JSONHelper.getStringValueFromJToken(workspaceObject, "id");
                    currentUser.WorkspaceName = JSONHelper.getStringValueFromJToken(workspaceObject, "label");
                }
            }            
        }

        logger.Info("currentUser.UserID={0}", currentUser.UserID);
        logger.Info("currentUser.DisplayName={0}", currentUser.DisplayName);
        logger.Info("currentUser.CustomerLabel={0}", currentUser.CustomerLabel);
        logger.Info("currentUser.WorkspaceID={0}", currentUser.WorkspaceID);

        #endregion
    }

}
