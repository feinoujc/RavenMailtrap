﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using HtmlAgilityPack;
using MvcContrib.Pagination;
using MvcContrib.Sorting;
using MvcContrib.UI.Grid;
using OpenPop.Mime;
using Raven.Abstractions.Data;
using Raven.Client;
using Message = RavenMailtrap.Model.Message;

namespace RavenMailtrap.Website.Controllers
{
    public class MessagesController : Controller
    {
        public IDocumentStore Store
        {
            get { return MvcApplication.Store; }
        }

        public IDocumentSession RavenSession { get; protected set; }

        public ActionResult Index(int? page, GridSortOptions sort)
        {
            IQueryable<Message> query = RavenSession.Query<Message>();

            query = sort.Column != null
                        ? query.OrderBy(sort.Column, sort.Direction)
                        : query.OrderByDescending(m => m.ReceivedDate);
            ViewBag.Sort = sort;

            return View(query.AsPagination(page ?? 1));
        }

        public ActionResult Details(string id)
        {
            var message = RavenSession.Load<Message>(id);
            if (message == null)
                return HttpNotFound();
            return PartialView("_Mail", message);
        }

        public ActionResult View(string id)
        {
            Attachment attachment = MvcApplication.Store.DatabaseCommands.GetAttachment(id);
            if (attachment == null)
                return HttpNotFound();
            return Email(attachment, id);
        }

        public async Task<ActionResult> MostRecentEmail(string to)
        {
            RavenSession.Advanced.AllowNonAuthoritativeInformation = false;
            int retries = 0;
            Message email = null;
            while (retries < 10)
            {
                IQueryable<Message> query = RavenSession.Query<Message>().OrderByDescending(message => message.ReceivedDate);
                              
                if (!string.IsNullOrEmpty(to))
                {
                    query = query.Where(message => message.To.Any(x => x.Equals(to, StringComparison.OrdinalIgnoreCase)));
                }

                email = query.FirstOrDefault();

                if (email != null)
                {
                    break;
                }
                await Task.Delay(1 * 1000);
                retries++;
            }
            if (email != null)
            {
                Attachment attachment = MvcApplication.Store.DatabaseCommands.GetAttachment(email.Id);
                if (attachment == null)
                    return HttpNotFound("Could not find an attachment for email " + email.Id);
                return Email(attachment, email.Id);
            }
            return HttpNotFound("Could not find an email " + to);
        }

        private ActionResult Email(Attachment attachment, string id)
        {
            return File(attachment.Data(), "message/rfc822", id + "." + attachment.Metadata["Format"]);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            RavenSession = Store.OpenSession();
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.IsChildAction)
                return;

            using (RavenSession)
            {
                if (filterContext.Exception != null)
                    return;

                if (RavenSession != null)
                    RavenSession.SaveChanges();
            }
        }

        public ActionResult HtmlContent(string id)
        {
            Attachment attachment = MvcApplication.Store.DatabaseCommands.GetAttachment(id);
            if (attachment == null)
                return HttpNotFound();


            OpenPop.Mime.Message m;
            using (var stream = new MemoryStream())
            using (Stream otherStream = attachment.Data())
            {
                otherStream.CopyTo(stream);

                m = new OpenPop.Mime.Message(stream.ToArray());
            }

            MessagePart html = m.FindFirstHtmlVersion();
            if (html != null)
            {
                //add a <base target="_blank"/> so that links open in new window
                var doc = new HtmlDocument();
                using (var s = new MemoryStream(html.Body))
                {
                    doc.Load(s, html.BodyEncoding);
                    HtmlNode head = doc.DocumentNode.SelectSingleNode("/html/head");
                    HtmlNode baseTag = doc.CreateElement("base");
                    head.AppendChild(baseTag);
                    baseTag.SetAttributeValue("target", "_blank");
                }
                return Content(doc.DocumentNode.OuterHtml, "text/html", doc.Encoding);
            }
            else
            {
                MessagePart plain = m.FindFirstPlainTextVersion();
                return Content(string.Format("<html><head><title></title><base target=\"_blank\"></base></head><body><pre>{0}</pre></body></html>", plain.GetBodyAsText()), "text/html", plain.BodyEncoding);
            }
        }
    }
}