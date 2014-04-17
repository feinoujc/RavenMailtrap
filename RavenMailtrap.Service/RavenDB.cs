using System;
using System.Threading;
using Raven.Client;
using Raven.Client.Document;

namespace RavenMailtrap.Service
{
    public static class RavenDB
    {
        private static readonly Lazy<IDocumentStore> LazyDocumentStore = new Lazy<IDocumentStore>(() =>
        {
            var store = new DocumentStore {ConnectionStringName = "Mailtrap"};
            store.Initialize();
            return store;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static IDocumentStore DocumentStore
        {
            get
            {
                return LazyDocumentStore.Value;
            }    
               
        }

    }
}