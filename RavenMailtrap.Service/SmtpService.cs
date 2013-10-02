using System;
using NLog;
using Rnwood.SmtpServer;

namespace RavenMailtrap.Service
{
    public class SmtpService : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private Server _server;

        public SmtpService()
            : this(new RavenPersistenceBehavior())
        {
        }

        internal SmtpService(Server server)
        {
            _server = server;
        }

        internal SmtpService(IServerBehaviour serverBehavior)
            : this(new Server(serverBehavior))
        {
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