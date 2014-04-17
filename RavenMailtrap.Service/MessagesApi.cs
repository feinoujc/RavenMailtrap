using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.ExceptionHandling;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using Owin;

namespace RavenMailtrap.Service
{
    internal class MessagesApi:IStartAndStop
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private IDisposable webApp;

        public void Start()
        {
            string baseAddress = ConfigurationManager.AppSettings["ApiBaseAddress"] ?? "http://localhost:8081";


            // Start OWIN host 
            Log.Info("Starting web api on " + baseAddress);
            try
            {
                webApp = WebApp.Start<Startup>(baseAddress);
            }
            catch (Exception e)
            {
                Log.Error("Failed to start web api", e);
                throw;
            }
           
        }

        public void Stop()
        {
            using (webApp)
            {
                Log.Info("Stopping web app");
            }
            webApp = null;
        }

        public class Startup
        {
            // This code configures Web API. The Startup class is specified as a type
            // parameter in the WebApp.Start method.
            public void Configuration(IAppBuilder appBuilder)
            {
                var config = new HttpConfiguration();
                config.Services.Add(typeof(IExceptionLogger), new TraceExceptionLogger());                
                MediaTypeFormatterCollection formatters = config.Formatters;
                JsonMediaTypeFormatter jsonFormatter = formatters.JsonFormatter;
                JsonSerializerSettings settings = jsonFormatter.SerializerSettings;
                settings.Formatting = Formatting.Indented;
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                config.MapHttpAttributeRoutes();
                config.EnableCors(new EnableCorsAttribute("*", "*", "GET"));

                appBuilder.UseWebApi(config);
                config.EnsureInitialized();
            }
        }

        class TraceExceptionLogger : ExceptionLogger
        {
            private static readonly Logger Logger = LogManager.GetLogger("WebApi");

            public override Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
            {
                Logger.ErrorException(context.Exception.Message, context.Exception);
                return base.LogAsync(context, cancellationToken);

            }

            public override void Log(ExceptionLoggerContext context)
            {
                Logger.ErrorException(context.Exception.Message, context.Exception);
                base.Log(context);
            }

            public override bool ShouldLog(ExceptionLoggerContext context)
            {
                return true;
            }
        }
    }
}