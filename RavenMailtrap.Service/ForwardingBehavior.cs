using System;
using System.IO;
using System.Net.Mail;
using NLog;
using Rnwood.SmtpServer;

namespace RavenMailtrap.Service
{
    public class ForwardingBehavior : DefaultServerBehaviour
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void OnMessageReceived(IConnection connection, Message message)
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
                Log.ErrorException("Failed to forward email. " + e.Message, e);
            }

            base.OnMessageReceived(connection, message);
        }
    }
}