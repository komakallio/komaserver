using ASCOM.Alpaca;
using ASCOM.Common;
using KomaDome;
using KomaDome.DomeRestApi;
using KomaSafetyMonitor;
using KomaSafetyMonitor.SafetyRestApi;
using Microsoft.Extensions.Options;
using Refit;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;

namespace KomaAlpacaServer
{
    public class Program
    {
        //You should offer a way for the end user to customize this via the command line so it can be changed in the case of a collision.
        //This supports --urls=http://*:port by default.
        internal const int DefaultPort = 12345;

        internal const string Manufacturer = "Komakallio";

        internal const string ServerName = "Komakallio Alpaca Server";
        internal const string ServerVersion = "1.0";

        internal static ASCOM.Common.Interfaces.ILogger? Logger;

        internal static IHostApplicationLifetime? Lifetime;

        public static void Main(string[] args)
        {
            //First fill in information for your driver in the Alpaca Configuration Class. Some of these you may want to store in a user changeable settings file.
            //Then fill in the ToDos in this file. Each is marked with a //ToDo
            //You shouldn't need to do anything in the Startup and Logging or Finish Building and Start Server regions

            //For Debug ConsoleLogger is very nice. For production TraceLogger is recommended.
            Logger = new ASCOM.Tools.ConsoleLogger();

            //This region contains startup and logging features, most of the time you shouldn't need to customize this
            //You can add custom Command Line arguments here
            #region Startup and Logging

            Logger.LogInformation($"{ServerName} version {ServerVersion}");
            Logger.LogInformation($"Running on: {RuntimeInformation.OSDescription}.");

            //If already running start browser
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //Already running, start the browser, detects based on port in use
                    var con1 = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Where(con => con.LocalEndPoint.Port == ServerSettings.ServerPort);
                    if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Any(con => con.LocalEndPoint.Port == ServerSettings.ServerPort && (con.State == TcpState.Listen || con.State == TcpState.Established)))
                    {
                        Logger.LogInformation("Detected driver port already open, starting web browser on IP and Port. If this fails something else is using the port");
                        StartBrowser(ServerSettings.ServerPort);
                        return;
                    }
                }
                else
                {
                    Assembly? entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null)
                    {
                        if (Process.GetProcessesByName(entryAssembly.Location).Length > 1)
                        {
                            Logger.LogInformation("Detected driver already running, starting web browser on IP and Port");
                            StartBrowser(ServerSettings.ServerPort);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                return;
            }

            //Reset all stored settings if requested
            if (args?.Any(str => str.Contains("--reset")) ?? false)
            {
                Logger.LogInformation("Reseting Settings");
                ServerSettings.Reset();

                //If you have any device settings you should reset them as well or add a specific reset command.

                return;
            }

            //Turn off Authentication. Once off the user can change the password and re-enable authentication
            if (args?.Any(str => str.Contains("--reset-auth")) ?? false)
            {
                Logger.LogInformation("Turning off Authentication to allow password reset.");
                ServerSettings.UseAuth = false;
                Logger.LogInformation("Authentication off, you can change the password and then re-enable Authentication.");
            }

            if (args?.Any(str => str.Contains("--local-address")) ?? false)
            {
                Console.WriteLine($"http://localhost:{ServerSettings.ServerPort}");
            }

            if (!args?.Any(str => str.Contains("--urls")) ?? true)
            {
                args ??= [];

                Logger.LogInformation("No startup url args detected, binding to saved server settings.");

                var temparray = new string[args.Length + 1];

                args.CopyTo(temparray, 0);

                string startupURLArg = "--urls=http://";

                //If set to allow remote access bind to all local ips, otherwise bind only to localhost
                if (ServerSettings.AllowRemoteAccess)
                {
                    startupURLArg += "*";
                }
                else
                {
                    startupURLArg += "localhost";
                }

                startupURLArg += ":" + ServerSettings.ServerPort;

                Logger.LogInformation("Startup URL args: " + startupURLArg);

                temparray[args.Length] = startupURLArg;

                args = temparray;
            }

            var builder = WebApplication.CreateBuilder(args ?? []);

            #endregion Startup and Logging

            //Attach the logger
            ASCOM.Alpaca.Logging.AttachLogger(Logger);

            //Load the configuration
            ASCOM.Alpaca.DeviceManager.LoadConfiguration(new AlpacaConfiguration());

            #region Finish Building and Start server

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            //Load any xml comments for this program, this helps with swagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            //Add Swagger for the APIs
            ASCOM.Alpaca.Razor.StartupHelpers.ConfigureSwagger(builder.Services, xmlPath);
            //Set default behaviors for Alpaca APIs
            ASCOM.Alpaca.Razor.StartupHelpers.ConfigureAlpacaAPIBehavoir(builder.Services);
            //Use Authentication
            ASCOM.Alpaca.Razor.StartupHelpers.ConfigureAuthentication(builder.Services);
            //Add User Service
            builder.Services.AddScoped<IUserService, Data.UserService>();

            builder.Services.Configure<SafetyMonitorOptions>(builder.Configuration.GetSection(nameof(SafetyMonitorOptions)));
            builder.Services.Configure<DomeOptions>(builder.Configuration.GetSection(nameof(DomeOptions)));

            builder.Services.AddMemoryCache();
            builder.Services.AddRefitClient<ISafetyMonitorApi>().ConfigureHttpClient(c =>
            {
                var options = new SafetyMonitorOptions();
                builder.Configuration.GetSection(nameof(SafetyMonitorOptions)).Bind(options);
                c.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
            });

            builder.Services.AddRefitClient<IDomeApi>().ConfigureHttpClient(c =>
            {
                var options = new DomeOptions();
                builder.Configuration.GetSection(nameof(DomeOptions)).Bind(options);
                c.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
            });
            builder.Services.AddSingleton<ISafetyStatusSource, SafetyStatusCache>();
            builder.Services.AddSingleton<SafetyMonitor>();
            var app = builder.Build();
            ASCOM.Alpaca.DeviceManager.LoadSafetyMonitor(0, app.Services.GetRequiredService<SafetyMonitor>(), "Komakallio Safety Monitor", ServerSettings.GetDeviceUniqueId("SafetyMonitor", 0));
            var domeApi = app.Services.GetRequiredService<IDomeApi>();
            var domeUsers = app.Services.GetRequiredService<IOptions<DomeOptions>>().Value.Users;
            for (int i = 0; i < domeUsers.Length; i++)
            {
                var dome = new Dome(domeApi, domeUsers[i]);
                // TODO: Encode friendly names for each pier in the appsettings file
                ASCOM.Alpaca.DeviceManager.LoadDome(i, dome, $"Komakallio Dome ({domeUsers[i]})", ServerSettings.GetDeviceUniqueId("Dome", i));
            }


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            //Start Swagger on the Swagger endpoints if enabled.
            ASCOM.Alpaca.Razor.StartupHelpers.ConfigureSwagger(app);

            //Configure Discovery
            ASCOM.Alpaca.Razor.StartupHelpers.ConfigureDiscovery(app);

            //Allow authentication, either Cookie or Basic HTTP Auth
            ASCOM.Alpaca.Razor.StartupHelpers.ConfigureAuthentication(app);

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();

            app.MapControllers();

            app.MapFallbackToPage("/_Host");

            if (ServerSettings.AutoStartBrowser)
            {
                try
                {
                    StartBrowser(ServerSettings.ServerPort);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex.Message);
                }
            }

            #endregion Finish Building and Start server

            Lifetime = app.Lifetime;

            // Put code here that should run at shutdown
            Lifetime.ApplicationStopping.Register(() =>
            {
                Logger.LogInformation($"{ServerName} Stopping");
            });

            //Start the Alpaca Server
            app.Run();
        }

        /// <summary>
        /// Starts the system default handler (normally a browser) for local host and the current port.
        /// </summary>
        /// <param name="port"></param>
        internal static void StartBrowser(int port)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = string.Format("http://localhost:{0}", port),
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}