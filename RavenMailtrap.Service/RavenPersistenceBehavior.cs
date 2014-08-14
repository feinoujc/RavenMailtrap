using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NLog;
using OpenPop.Mime.Header;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Json.Linq;
using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace RavenMailtrap
{
    public class RavenPersistenceBehavior : IServerBehaviour
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IDocumentStore _documentStore;
        private readonly DefaultServerBehaviour _instance = new DefaultServerBehaviour();


        public RavenPersistenceBehavior()
            : this(RavenDB.DocumentStore)
        {
        }

        public RavenPersistenceBehavior(IDocumentStore documentStore)
        {
            _instance.MessageCompleted += OnMessageCompleted;
            _documentStore = documentStore;
        }

        public IEditableSession OnCreateNewSession(IConnection connection, IPAddress clientAddress, DateTime startDate)
        {
            return _instance.OnCreateNewSession(connection, clientAddress, startDate);
        }

        public Encoding GetDefaultEncoding(IConnection connection)
        {
            return _instance.GetDefaultEncoding(connection);
        }

        public void OnMessageReceived(IConnection connection, IMessage message)
        {
            _instance.OnMessageReceived(connection, message);
        }

        public bool IsSSLEnabled(IConnection connection)
        {
            return _instance.IsSSLEnabled(connection);
        }

        public bool IsSessionLoggingEnabled(IConnection connection)
        {
            return _instance.IsSessionLoggingEnabled(connection);
        }

        public long? GetMaximumMessageSize(IConnection connection)
        {
            return _instance.GetMaximumMessageSize(connection);
        }

        public X509Certificate GetSSLCertificate(IConnection connection)
        {
            return _instance.GetSSLCertificate(connection);
        }

        public void OnMessageRecipientAdding(IConnection connection, IMessage message, string recipient)
        {
            _instance.OnMessageRecipientAdding(connection, message, recipient);
        }

        public IEnumerable<IExtension> GetExtensions(IConnection connection)
        {
            return _instance.GetExtensions(connection);
        }

        public void OnSessionCompleted(IConnection connection, ISession session)
        {
            _instance.OnSessionCompleted(connection, session);
        }

        public void OnSessionStarted(IConnection connection, ISession session)
        {
            _instance.OnSessionStarted(connection, session);
        }

        public int GetReceiveTimeout(IConnection connection)
        {
            return _instance.GetReceiveTimeout(connection);
        }

        public AuthenticationResult ValidateAuthenticationCredentials(IConnection connection,
            IAuthenticationCredentials request)
        {
            return _instance.ValidateAuthenticationCredentials(connection, request);
        }

        public void OnMessageStart(IConnection connection, string @from)
        {
            _instance.OnMessageStart(connection, @from);
        }

        public bool IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism)
        {
            return _instance.IsAuthMechanismEnabled(connection, authMechanism);
        }

        public void OnCommandReceived(IConnection connection, SmtpCommand command)
        {
            _instance.OnCommandReceived(connection, command);
        }

        public IEditableMessage OnCreateNewMessage(IConnection connection)
        {
            return _instance.OnCreateNewMessage(connection);
        }

        public void OnMessageCompleted(IConnection connection)
        {
            _instance.OnMessageCompleted(connection);
        }

        public string DomainName
        {
            get { return _instance.DomainName; }
        }

        public IPAddress IpAddress
        {
            get { return _instance.IpAddress; }
        }

        public int PortNumber
        {
            get { return _instance.PortNumber; }
        }

        public int MaximumNumberOfSequentialBadCommands
        {
            get { return _instance.MaximumNumberOfSequentialBadCommands; }
        }

        private void OnMessageCompleted(object sender, MessageEventArgs e)
        {
            StoreMessage(e.Message);
        }


        private void StoreMessage(IMessage message)
        {
            try
            {
                Log.Info("Received message address from {0}", message.From);

                using (IDocumentSession session = _documentStore.OpenSession())
                {
                    var mailMessage = new Message
                    {
                        From = message.From,
                        ReceivedDate = message.ReceivedDate,
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

                        session.Advanced.GetMetadataFor(mailMessage)["Raven-Cascade-Delete-Attachments"] =
                            RavenJToken.FromObject(new[] {mailMessage.Id});

                        session.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Log(LogLevel.Error, "Failed to store message", e);
            }
        }

        public event EventHandler<CommandEventArgs> CommandReceived
        {
            add { _instance.CommandReceived += value; }
            remove { _instance.CommandReceived -= value; }
        }

        public event EventHandler<MessageEventArgs> MessageCompleted
        {
            add { _instance.MessageCompleted += value; }
            remove { _instance.MessageCompleted -= value; }
        }

        public event EventHandler<MessageEventArgs> MessageReceived
        {
            add { _instance.MessageReceived += value; }
            remove { _instance.MessageReceived -= value; }
        }

        public event EventHandler<SessionEventArgs> SessionCompleted
        {
            add { _instance.SessionCompleted += value; }
            remove { _instance.SessionCompleted -= value; }
        }

        public event EventHandler<SessionEventArgs> SessionStarted
        {
            add { _instance.SessionStarted += value; }
            remove { _instance.SessionStarted -= value; }
        }

        public event EventHandler<AuthenticationCredentialsValidationEventArgs>
            AuthenticationCredentialsValidationRequired
            {
                add { _instance.AuthenticationCredentialsValidationRequired += value; }
                remove { _instance.AuthenticationCredentialsValidationRequired -= value; }
            }

        private static void MapHeaders(Message mailMessage, MessageHeader headers)
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