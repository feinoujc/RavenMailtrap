using System;
using System.Configuration;
using NLog;
using Rnwood.SmtpServer;

namespace RavenMailtrap.Service
{
    public class SmtpService : IStartAndStop , IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private Server _server;
        private static readonly Type BehaviorType;

        static SmtpService()
        {
            var configuredBehaviorType = ConfigurationManager.AppSettings["ServerBehavior"];
            if (string.IsNullOrEmpty(configuredBehaviorType) ||
                (BehaviorType = Type.GetType(configuredBehaviorType, true)) == null)
            {
                throw new InvalidOperationException(@"Could not load server behavior from appSettings. For example 
<appSettings>
    <add key=""ServerBehavior"" value=""RavenMailtrap.Service.RavenPersistenceBehavior, RavenMailtrap.Service""/>
  </appSettings>");
            }
         
        }


        public SmtpService()
            : this((IServerBehaviour)Activator.CreateInstance(BehaviorType))
        {
        }

        internal SmtpService(Server server)
        {
            if (server == null) throw new ArgumentNullException("server");

            _server = server;
        }

        internal SmtpService(IServerBehaviour serverBehavior)
            : this(new Server(serverBehavior))
        {
            if (serverBehavior == null) throw new ArgumentNullException("serverBehavior");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public void Start()
        {
            _server.Start();
            Log.Info("Service started");
            Log.Info("Service behavior: {0}", _server.Behaviour);
        }

        public void Stop()
        {
            _server.Stop();
            Log.Info("Service stopped");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_server != null && _server.IsRunning)
                {
                    _server.Stop();
                    _server = null;
                }
            }
        }
    }
}