using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using OpenPop.Mime.Header;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Client.Document;
using Raven.Json.Linq;
using Rnwood.SmtpServer;

namespace RavenMailtrap.Service
{
    public class RavenPersistenceBehavior : DefaultServerBehaviour
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IDocumentStore _documentStore;


        public RavenPersistenceBehavior()
            : this(RavenDB.DocumentStore)
        {
        }

        public RavenPersistenceBehavior(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
            _documentStore.Initialize();
        }


        public override void OnMessageReceived(IConnection connection, Message message)
        {
            try
            {
                Log.Info("Received message address from {0} with client address {1}", message.From,
                         connection.Session.ClientAddress);

                using (IDocumentSession session = _documentStore.OpenSession())
                {
                    var mailMessage = new Model.Message
                        {
                            From = message.From,
                            ReceivedDate = message.ReceivedDate,
                            ServerHostName = Dns.GetHostEntry(connection.Session.ClientAddress).HostName.ToLower()
                        };
                    using (Stream msgStream = message.GetData())
                    {
                        MessageHeader headers = OpenPop.Mime.Message.Load(msgStream).Headers;
                        mailMessage.Subject = headers.Subject;
                        MapHeaders(mailMessage, headers);


                        session.Store(mailMessage);
                        Log.Info("Stored message {0} in raven. Original message id {1}", mailMessage.Id,
                                 headers.MessageId);
                        IDatabaseCommands dbCommands = _documentStore.DatabaseCommands;

                        var optionalMetaData = new RavenJObject();
                        optionalMetaData["Format"] = "EML";
                        msgStream.Seek(0, SeekOrigin.Begin);
                        dbCommands.PutAttachment(mailMessage.Id, null, msgStream, optionalMetaData);

                        session.Advanced.GetMetadataFor(mailMessage)["Raven-Cascade-Delete-Attachments"] = RavenJToken.FromObject(new[] { mailMessage.Id });

                        session.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorException("Failed to store message", e);
            }

            base.OnMessageReceived(connection, message);
        }

        private static void MapHeaders(Model.Message mailMessage, MessageHeader headers)
        {
            mailMessage.Bcc = new List<string>(headers.Bcc.Select(x => x.Raw));
            mailMessage.Cc = new List<string>(headers.Cc.Select(x => x.Raw));
            mailMessage.To = new List<string>(headers.To.Select(x => x.Raw));
            mailMessage.UnknownHeaders = new Dictionary<string, string>();
            foreach (string key in headers.UnknownHeaders.AllKeys)
            {
                mailMessage.UnknownHeaders[key] = headers.UnknownHeaders[key];
            }
            mailMessage.ContentDescription = headers.ContentDescription;
            mailMessage.ContentDisposition = headers.ContentDisposition;
            mailMessage.ContentId = headers.ContentId;
            mailMessage.ContentTransferEncoding = string.Format("{0}", headers.ContentTransferEncoding);
            mailMessage.ContentType = headers.ContentType;
            mailMessage.Date = headers.Date;
            mailMessage.DateSent = headers.DateSent;
            mailMessage.DispositionNotificationTo =
                new List<string>(headers.DispositionNotificationTo.Select(x => x.Raw));
            mailMessage.From = headers.From.Raw;
            mailMessage.Importance = headers.Importance;
            mailMessage.InReplyTo = headers.InReplyTo;
            mailMessage.Keywords = headers.Keywords;
            mailMessage.MessageId = headers.MessageId;
            mailMessage.MimeVersion = headers.MimeVersion;
            mailMessage.Received = new List<string>(headers.Received.Select(x => x.Raw));
            mailMessage.References = headers.References;
            mailMessage.ReplyTo = headers.ReplyTo != null ? headers.ReplyTo.Raw : null;
            mailMessage.ReturnPath = headers.ReturnPath != null ? headers.ReturnPath.Raw : null;
            mailMessage.Sender = headers.Sender != null ? headers.Sender.Raw : null;
            mailMessage.Subject = headers.Subject;
        }
    }
}