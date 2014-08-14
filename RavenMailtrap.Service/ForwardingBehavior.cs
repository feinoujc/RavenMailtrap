using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NLog;
using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace RavenMailtrap
{
    public class ForwardingBehavior : IServerBehaviour
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly DefaultServerBehaviour _instance = new DefaultServerBehaviour();

        public ForwardingBehavior()
        {
            _instance.MessageCompleted += OnMessageCompleted;
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

        private void OnMessageCompleted(object sender, MessageEventArgs messageEventArgs)
        {
            ForwardMessage(messageEventArgs.Message);
        }

        private void ForwardMessage(IMessage message)
        {
            try
            {
                OpenPop.Mime.Message message2;
                using (Stream stream = message.GetData())
                {
                    message2 = OpenPop.Mime.Message.Load(stream);
                }
                SmtpClient client = null;
                try
                {
                    client = new SmtpClient();
                    client.Send(message2.ToMailMessage());
                }
                finally
                {
                    var disposable = client as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to forward email. " + e.Message, e);
            }
        }
    }
}