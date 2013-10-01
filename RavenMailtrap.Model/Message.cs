using System;

namespace RavenMailtrap.Model
{
    public class Message
    {
        public Message()
        {
            Header = new MessageHeader();
        }
        public string Id { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string From { get; set; }
        public string[] To { get; set; }
        public string Subject { get; set; }
        public string ServerHostName { get; set; }
        public MessageHeader Header { get; set; }

    }
}