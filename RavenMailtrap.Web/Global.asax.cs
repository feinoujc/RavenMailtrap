using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Raven.Client.Document;
using Raven.Client.Indexes;
using RavenMailtrap.Model;

namespace EmailServer.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication
    {
        public static DocumentStore Store;

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");


            routes.MapRoute(
                "WithParam", // Route name
                "{controller}/{action}/{*id}", // URL with parameters
                  new { controller = "Messages", action = "Index" } // Parameter defaults
                );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            Store = new MessageDocumentStore();
            Store.Initialize();

            IndexCreation.CreateIndexes(Assembly.GetCallingAssembly(), Store);

            //purge old emails in background
            Task.Factory.StartNew(() =>
                {
                    var cutOffDate = DateTime.Today.AddDays(-7);
                    using (var session = Store.OpenSession())
                    {
                        foreach (var message in
                            session.Query<Message>()
                                   .Where(m => m.ReceivedDate <= cutOffDate)
                                   .ToArray())
                        {
                            session.Advanced.DatabaseCommands.ForDefaultDatabase()
                                   .DeleteAttachment(message.Id, null);
                            session.Delete(message);
                        }
                        session.SaveChanges();
                    }
                });

        }
    }
}