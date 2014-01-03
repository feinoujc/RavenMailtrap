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
            : this(new DocumentStore {ConnectionStringName = "Mailtrap"})
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
                            To = message.To,
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
            mailMessage.Header.Bcc = new List<string>(headers.Bcc.Select(x => x.Raw));
            mailMessage.Header.Cc = new List<string>(headers.Cc.Select(x => x.Raw));
            mailMessage.Header.To = new List<string>(headers.To.Select(x => x.Raw));
            mailMessage.Header.UnknownHeaders = new Dictionary<string, string>();
            foreach (string key in headers.UnknownHeaders.AllKeys)
            {
                mailMessage.Header.UnknownHeaders[key] = headers.UnknownHeaders[key];
            }
            mailMessage.Header.ContentDescription = headers.ContentDescription;
            mailMessage.Header.ContentDisposition = headers.ContentDisposition;
            mailMessage.Header.ContentId = headers.ContentId;
            mailMessage.Header.ContentTransferEncoding = string.Format("{0}", headers.ContentTransferEncoding);
            mailMessage.Header.ContentType = headers.ContentType;
            mailMessage.Header.Date = headers.Date;
            mailMessage.Header.DateSent = headers.DateSent;
            mailMessage.Header.DispositionNotificationTo =
                new List<string>(headers.DispositionNotificationTo.Select(x => x.Raw));
            mailMessage.Header.From = headers.From.Raw;
            mailMessage.Header.Importance = headers.Importance;
            mailMessage.Header.InReplyTo = headers.InReplyTo;
            mailMessage.Header.Keywords = headers.Keywords;
            mailMessage.Header.MessageId = headers.MessageId;
            mailMessage.Header.MimeVersion = headers.MimeVersion;
            mailMessage.Header.Received = new List<string>(headers.Received.Select(x => x.Raw));
            mailMessage.Header.References = headers.References;
            mailMessage.Header.ReplyTo = headers.ReplyTo != null ? headers.ReplyTo.Raw : null;
            mailMessage.Header.ReturnPath = headers.ReturnPath != null ? headers.ReturnPath.Raw : null;
            mailMessage.Header.Sender = headers.Sender != null ? headers.Sender.Raw : null;
            mailMessage.Header.Subject = headers.Subject;
        }
    }
}