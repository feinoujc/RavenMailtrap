using System;
using System.Configuration;
using System.Threading;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;

namespace RavenMailtrap
{
    public static class RavenDB
    {
        private static readonly Lazy<IDocumentStore> LazyDocumentStore = new Lazy<IDocumentStore>(() =>
        {
            var store = new EmbeddableDocumentStore() {DataDirectory = "Data"};
            store.Initialize();
            return store;
        });

        public static IDocumentStore DocumentStore
        {
            get { return LazyDocumentStore.Value; }
        }
    }
}