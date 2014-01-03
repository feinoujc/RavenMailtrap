using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using NLog;
using NLog.Config;
using NLog.Targets;
using Raven.Abstractions.Data;
using Raven.Client.Document;
using Raven.Client.Indexes;
using RavenMailtrap.Model;

namespace RavenMailtrap.Website
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication
    {
        public static DocumentStore Store;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            LogManager.Configuration = GetDefaultLoggingConfiguration();
            Store = new DocumentStore {ConnectionStringName = "Mailtrap"};
            Store.Initialize();

            IndexCreation.CreateIndexes(Assembly.GetCallingAssembly(), Store);

            //purge old emails in background

            if (WebConfigurationManager.AppSettings["PurgeOldMessages"] == "true")
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        const string indexName = "WeekOldEmail";


                        Store.DatabaseCommands.PutIndex(indexName,
                            new IndexDefinitionBuilder<Message>
                            {
                                Map = documents => documents.Where(m => m.ReceivedDate <= DateTime.Today.AddDays(-7))
                                    .Select(entity => new {})
                            }, overwrite: true);

                        Store.DatabaseCommands.DeleteByIndex(indexName,
                            new IndexQuery(), allowStale: true);
                    }
                    catch (Exception e)
                    {
                        LogManager.GetLogger("Web").ErrorException("The purge process failed. " + e, e);
                    }
                });
            }
        }

        private static LoggingConfiguration GetDefaultLoggingConfiguration()
        {
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            // Step 3. Set target properties 
            fileTarget.FileName = "${basedir}/mailtrap.log";
            fileTarget.Layout = @"${date:format=HH\:MM\:ss} ${logger} ${message}";

            // Step 4. Define rules
            var rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);
            return config;
        }
    }
}