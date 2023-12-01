using System.Net.Http.Headers;
using NLog;
using NLog.Web;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Observe.EntityExplorer

{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Early init of NLog to allow startup and exception logging, before host is built
            var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                var configuration = builder.Configuration
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.json", optional: false)
                    .AddJsonFile($"appsettings.Development.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Add services to the container.
                builder.Services.AddControllersWithViews();

                // Memory Cache
                builder.Services.AddDistributedMemoryCache();

                // NLog: Setup NLog for Dependency injection
                builder.Logging.ClearProviders();
                builder.Host.UseNLog();
                
                // OTEL telemetry setup
                string serviceName = "Observe-Entity-Explorer";
                string serviceVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                string observeEndpoint = configuration["ObserveEntityExplorer:Usage:Collector"];
                string observeToken = configuration["ObserveEntityExplorer:Usage:Token"];
                
                builder.Logging.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, null, serviceVersion )
                        .AddAttributes(getOTELResourceAttributes())
                        .AddEnvironmentVariableDetector())
                        .AddConsoleExporter()
                        .AddOtlpExporter(opt =>
                        {
                            if (observeEndpoint != null && observeToken != null)
                            {
                                setOtlpExporterOptions(opt, serviceName, serviceVersion, observeEndpoint, observeToken);
                            }
                        });
                });
                builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource => 
                        resource.AddService(serviceName, null, serviceVersion)
                        .AddAttributes(getOTELResourceAttributes())
                        .AddEnvironmentVariableDetector())
                    .WithTracing(tracing => tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter()
                        .AddOtlpExporter(opt =>
                        {
                            if (observeEndpoint != null && observeToken != null)
                            {
                                setOtlpExporterOptions(opt, serviceName, serviceVersion, observeEndpoint, observeToken);
                            }
                        }));

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Home/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                //app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthorization();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                app.Run();
            }
            catch (Exception exception)
            {
                // NLog: catch setup errors
                logger.Error(exception, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }            
        }

        private static KeyValuePair<string, object>[] getOTELResourceAttributes()
        {
            return new KeyValuePair<string, object>[]
            {
                new("process.id", Environment.ProcessId),
                new("process.executable.path", Environment.ProcessPath),
                new("process.command_line", Environment.CommandLine),
                new("process.command_args", Environment.GetCommandLineArgs()),
                new("process.current.directory", Environment.CurrentDirectory),
                new("process.runtime.version", Environment.Version.ToString()),
                new("process.username", Environment.UserName),
                new("process.arch", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()),
                new("host.name", Environment.MachineName),
                new("host.arch", System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()),
                new("os.version", Environment.OSVersion.ToString())
            };
        }

        private static void setOtlpExporterOptions(OtlpExporterOptions opt, string serviceName, string serviceVersion, string observeEndpoint, string observeToken)
        {
            opt.Endpoint = new Uri(String.Format("{0}/v1/traces", observeEndpoint));
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.HttpClientFactory = () =>
            {
                HttpClient client = new HttpClient();

                var productValue = new ProductInfoHeaderValue(serviceName, serviceVersion);
                var commentValue = new ProductInfoHeaderValue("(https://www.observeinc.com/)");
                client.DefaultRequestHeaders.UserAgent.Add(productValue);
                client.DefaultRequestHeaders.UserAgent.Add(commentValue);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", observeToken);
                return client;
            };
        }
    }
}