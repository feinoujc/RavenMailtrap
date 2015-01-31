using System;
using Common.Logging;
using Raven.Client;
using Raven.Client.Embedded;

namespace RavenMailtrap
{
    public class RavenDB : IStartAndStop
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static readonly Lazy<IDocumentStore> LazyDocumentStore = new Lazy<IDocumentStore>(() =>
        {
            var store = new EmbeddableDocumentStore {DataDirectory = "Data"};
            store.Initialize();
          
            return store;
        });

        public static IDocumentStore DocumentStore
        {
            get { return LazyDocumentStore.Value; }
        }

        public void Start()
        {
            Log.Debug("Raven started at " + DocumentStore.Url);
        }

        public void Stop()
        {
            DocumentStore.Dispose();
        }
    }
}