using Raven.Client.Document;

namespace EmailServer.Web
{
    public class MessageDocumentStore : DocumentStore
    {
        public MessageDocumentStore()
        {
            ConnectionStringName = "RavenDb";
            DefaultDatabase = "IntegrationEmailStore";
        }
    }
}