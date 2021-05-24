using System.Linq;
using Serilog;
using Serilog.Core;
using Microsoft.Extensions.Configuration;
using System;

namespace SiteUpdater
{

    class Program
    {
        private static Logger _log;


        static int Main(string[] args)
        {
            var ret = 0;
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            _log = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            _log.Information($"Starting Site Updater");
            _log.Information($"".PadRight(50, '='));

            try
            {

                if (args.Length > 0 && (args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-?", StringComparison.OrdinalIgnoreCase)))
                {
                    Help();
                    return 0;
                }

                var siteManager = new SiteManager(config, _log);
                var siteCount = 0;
                var siteProcessedCount = 0;
                if (!siteManager.IsUserAdministrator())
                    return 1;

                if (args.Length > 0)
                {
                    ret = ProcessSite(config, siteManager, args);
                    siteProcessedCount = ret == 0 ? 1 : 0;
                    siteCount = 1;
                }
                else
                {
                    siteProcessedCount = ProcessTargets(config, siteManager, out siteCount);
                    ret = siteProcessedCount == siteCount ? 0 : 1;
                }
                _log.Information($"".PadRight(50, '='));

                _log.Write(ret == 0 ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Warning,
                    $"Site Updater {(ret == 0 ? "Successfully" : "")} Updated {siteProcessedCount} out of {siteCount} sites");
            }
            catch (SiteUpdaterMissingDirectoryException ex)
            {
                _log.Fatal("Directory setting not found in appSettings.json");
                _log.Fatal(ex.Message);
                _log.Fatal("SiteUpdater -h for help");
            }
            catch (SiteUpdaterBadDirectoryException ex)
            {
                _log.Fatal("Directory is not valid");
                _log.Fatal(ex.Message);
                _log.Fatal("SiteUpdater -h for help");
            }
            catch (SiteUpdaterMissingTargetsException)
            {
                _log.Fatal("No targets found in appSettings.json");
                _log.Fatal("SiteUpdater -h for help");
            }
            catch (SiteUpdaterConfigurationException ex)
            {
                _log.Fatal("Fatal error parsing cmd line arguments");
                _log.Fatal(ex.Message);
                _log.Fatal("SiteUpdater -h for help");
                ret = 2;
            }
            catch (SiteUpdaterConfigurationMissingArgumentsException)
            {
                _log.Fatal("When using cmd line arguements, all arguments are required");
                _log.Fatal("SiteUpdater -h for help");
                ret = 2;
            }
            return ret;
        }
        private static int ProcessSite(IConfigurationRoot config, SiteManager siteManager, string[] args)
        {

            int ret = 1;

            if (args.Length != 4)
                throw (new SiteUpdaterConfigurationMissingArgumentsException());

            var siteName = GetArgument(args, "sitename");
            var port = GetArgument<int>(args, "port");
            var source = GetArgument(args, "source"); ;
            var destination = GetArgument(args, "destination"); ;

            _log.Debug($"Update site {siteName} via command line");
            ret = siteManager.InstallSite(siteName, port, source, destination);
            return ret;
        }
        private static string GetArgument(string[] args, string optionName)
        {
            var ret= GetArgument<string>(args, optionName);
            if(string.IsNullOrWhiteSpace(ret))
                throw new SiteUpdaterConfigurationException(optionName, new Exception("Missing value"));
            return ret;
        }
        private static T GetArgument<T>(string[] args, string optionName)
        {
            T ret = default(T);
            try
            {
                var option = args.FirstOrDefault(a => a.ToLower().StartsWith($"--{optionName.ToLower()}"));
                if (string.IsNullOrWhiteSpace(option))
                {
                    throw new Exception($"{optionName} is required");
                }
                else
                {
                    var optionValue = option.Replace($"--{optionName}=", "");
                    ret = (T)Convert.ChangeType(optionValue, typeof(T));
                }
            }
            catch (Exception ex)
            {
                throw new SiteUpdaterConfigurationException(optionName, ex);
            }

            return ret;
        }
        private static int ProcessTargets(IConfigurationRoot config, SiteManager siteManager, out int siteCount)
        {
            var ret = 0;
            siteCount = 0;
            var targets = config.GetSection("Targets");
            siteCount=targets.GetChildren().Count();
            if(siteCount<=0)
                throw new SiteUpdaterMissingTargetsException();

            _log.Information($"Found {siteCount} sites to create/update");
            foreach (var target in targets.GetChildren())
            {
                var siteret = siteManager.InstallSite(target.Key, int.Parse(target.Value));
                if (siteret == 0)
                    ret++;
            }

            return ret;
        }

        private static void Help()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Site Updater");
            Console.WriteLine($"".PadRight(80, '='));
            Console.WriteLine("\tThis tool will update (or create) an IIS web site using ServerManager");
            Console.WriteLine("\tBy default the tool will read the sites (aka targets) from the appSettings.");
            Console.WriteLine();
            Console.WriteLine("\tYou can optionally specify a single site using the optional parameters below");
            Console.WriteLine("\tIf a site name is specified, then the targets in appSettings will be ignored and ");
            Console.WriteLine("\tthe remainder of the parameters must be specified");
            Console.WriteLine();
            Console.WriteLine("\tThis tool requires administrative privileges");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("\tSiteUpdater [--siteName=iiswebsite --port=80, --source==c:\\source --destination=c:\\destination]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"\t{"-n; --siteName".PadRight(17)} The name of the site to update/create");
            Console.WriteLine($"\t{"-p; --port".PadRight(17)} The port of the site");
            Console.WriteLine($"\t{"-s; --source".PadRight(17)} The source folder to copy the application from (excluding sitename)");
            Console.WriteLine($"\t{"-d; --destination".PadRight(17)} The folder for the site, aka the destination of the updated files");
            Console.WriteLine();
            Console.WriteLine("Returns:");
            Console.WriteLine("\t0 - Success");
            Console.WriteLine("\t1 - Completed with Errors");
            Console.WriteLine("\t2 - Fatal Error");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\tSiteUpdater --sitename=MyWebSite --port=80 --source=C:\\dev\\source --destination=c:\\mywebsite");
            Console.WriteLine($"".PadRight(80, '='));
        }
    }
}

