using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using HtmlAgilityPack;
using OpenPop.Mime;
using Raven.Abstractions.Data;
using Raven.Client;
using Message = RavenMailtrap.Model.Message;

namespace RavenMailtrap.Service
{
    [RoutePrefix("api/messages")]
    public class MessagesController : ApiController
    {
        public const int PageSize = 25;

        public IDocumentStore Store
        {
            get { return RavenDB.DocumentStore; }
        }

        public IAsyncDocumentSession Session { get; protected set; }


        [Route("")]
        public async Task<IEnumerable<Message>> Get(int page = 0)
        {
            return await Session.Query<Message>()
                .OrderByDescending(x => x.ReceivedDate)
                .Skip(page * PageSize)
                .Take(PageSize).ToListAsync();
        }

        [Route("{id:int}")]
        public async Task<Message> GetById(int id)
        {
            var result = await Session.LoadAsync<Message>(id);
            if (result == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return result;
        }

        [Route("{id:int}/html")]
        public HttpResponseMessage GetMailHtmlContent(int id)
        {
            Attachment attachment = Store.DatabaseCommands.GetAttachment("messages/" + id);
            if (attachment == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "no attachment");

            OpenPop.Mime.Message message;
            using (var memoryStream = new MemoryStream())
            using (Stream attachmentStream = attachment.Data())
            {
                attachmentStream.CopyTo(memoryStream);
                message = new OpenPop.Mime.Message(memoryStream.ToArray());
            }

            MessagePart html = message.FindFirstHtmlVersion();
            if (html != null)
            {
                //add a <base target="_blank"/> so that links open in new window
                var doc = new HtmlDocument();
                using (var s = new MemoryStream(html.Body))
                {
                    doc.Load(s, html.BodyEncoding);
                    HtmlNode head = doc.DocumentNode.SelectSingleNode("/html/head");
                    if (head != null)
                    {
                        HtmlNode baseTag = doc.CreateElement("base");
                        head.AppendChild(baseTag);
                        baseTag.SetAttributeValue("target", "_blank");
                    }
                }
                return new HttpResponseMessage
                {
                    Content = new StringContent(doc.DocumentNode.OuterHtml, doc.Encoding, "text/html")
                };
            }
            MessagePart plain = message.FindFirstPlainTextVersion();

            return new HttpResponseMessage
            {
                Content = new StringContent(string.Format(
                    "<html><head><title></title><base target=\"_blank\"></base></head><body><pre>{0}</pre></body></html>",
                    plain.GetBodyAsText()), plain.BodyEncoding, "text/html")
            };
        }


        [Route("{id:int}/raw", Name = "RawEmail")]
        [HttpGet]
        public HttpResponseMessage GetMailRawContent(int id)
        {
            Attachment attachment = Store.DatabaseCommands.GetAttachment("messages/" + id);
            if (attachment == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "no attachment");
            return RawEmail(attachment, id);
        }

        //TODO
        [Route("{to}/latest")]
        [HttpGet]
        public async Task<IHttpActionResult> MostRecentEmail(string to, int within = 10)
        {
            //if called from an integration test, we may need to wait until the email gets sent
            //so keep trying until we find something or we exhaust all retries
            int retries = 0;
            Message email = null;
            while (retries < 10)
            {
                IQueryable<Message> query = Session.Query<Message>()
                    .Where(message => message.DateSent >= DateTime.UtcNow.AddMinutes(-within))
                    .OrderByDescending(message => message.DateSent);

                if (!string.IsNullOrEmpty(to))
                {
                    query = query.Where(message => message.To.Any(x => x.Equals(to, StringComparison.OrdinalIgnoreCase)));
                }

                email = await query.FirstOrDefaultAsync();

                if (email != null)
                {
                    break;
                }
                await Task.Delay(2 * 1000);
                retries++;
            }
            if (email != null)
            {
                return Redirect(new Uri("/api/" + email.Id + "/raw", UriKind.Relative));
            }
            return NotFound();
        }

        private HttpResponseMessage RawEmail(Attachment attachment, int id)
        {
            var response = new HttpResponseMessage
            {
                Content = new StreamContent(attachment.Data()),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("message/rfc822");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = id + "." + attachment.Metadata["Format"]
            };
            return response;
        }

        public override async Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext,
            CancellationToken cancellationToken)
        {
            using (Session = Store.OpenAsyncSession())
            {
                HttpResponseMessage result = await base.ExecuteAsync(controllerContext, cancellationToken);
                //await Session.SaveChangesAsync(); //controller is readonly, no need

                return result;
            }
        }
    }
}