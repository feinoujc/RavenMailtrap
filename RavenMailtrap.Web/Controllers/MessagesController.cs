using System;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using EmailServer.Web.Controllers;
using MvcContrib.Pagination;
using MvcContrib.Sorting;
using MvcContrib.UI.Grid;
using Raven.Abstractions.Data;
using RavenMailtrap.Model;

namespace RavenMailtrap.Web.Controllers
{
    public class MessagesController : RavenController
    {
        public ActionResult Index(int? page, GridSortOptions sort)
        {
            IQueryable<Message> query = RavenSession.Query<Message>();

            query = sort.Column != null
                        ? query.OrderBy(sort.Column, sort.Direction)
                        : query.OrderByDescending(m => m.ReceivedDate);
            ViewBag.Sort = sort;

            return View(query.AsPagination(page ?? 1));
        }


        public ActionResult View(string id)
        {
            Attachment attachement = RavenSession.Advanced.DatabaseCommands.GetAttachment(id);
            if (attachement == null)
                return HttpNotFound();
            return Email(attachement, id);
        }

        public ActionResult GetLatestEmailTo(string id)
        {
            RavenSession.Advanced.AllowNonAuthoritativeInformation = false;
            int retries = 0;
            Message email = null;
            while (retries < 10)
            {
                email = RavenSession.Query<Message>()
                                  .Where(message => message.To.Any(x => x.Equals(id, StringComparison.InvariantCultureIgnoreCase)))
                                  .OrderByDescending(message => message.ReceivedDate)
                                  .FirstOrDefault();

                if (email != null)
                {
                    break;
                }
                Thread.Sleep(1 * 1000);
                retries++;
            }
            if (email != null)
            {
                Attachment attachement = RavenSession.Advanced.DatabaseCommands.GetAttachment(email.Id);
                if (attachement == null)
                    return HttpNotFound("Could not find an attachemant for email " + email.Id);
                return Email(attachement, email.Id);

            }
            return HttpNotFound("Could not find an email " + id);
        }

        public ActionResult Purge()
        {
            foreach (Message message in RavenSession.Query<Message>().ToArray())
            {
                RavenSession.Advanced.DatabaseCommands.ForDefaultDatabase().DeleteAttachment(message.Id, null);
                RavenSession.Delete(message);
            }
            RavenSession.SaveChanges();
            return RedirectToAction("Index");
        }

        private ActionResult Email(Attachment attachment , string id)
        {
            return File(attachment.Data(), "message/rfc822", id + "." + attachment.Metadata["Format"]);
        }
    }
}