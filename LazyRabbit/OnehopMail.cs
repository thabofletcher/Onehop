using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LazyRabbit
{
    public class OnehopMail
    {
        RetryQueue _RetryQueue = new RetryQueue();

        public void Send(MailMessage message, Action<Exception> callbackException = null, Action<MessageHostSender> sendCallback = null)
        {
			Dictionary<string, MailMessage> hosts = new Dictionary<string,MailMessage>();
            foreach (var to in message.To)
            {
                AddToUnique(to, message, hosts);
            }

            foreach (var to in message.CC)
            {
                AddToUnique(to, message, hosts);
            }

            foreach (var to in message.Bcc)
            {
                AddToUnique(to, message, hosts);
            }

            foreach (var unique in hosts)
			{
				new MessageHostSender(unique.Value, unique.Key, callbackException, retryQ: _RetryQueue, sendSuccess: sendCallback).TrySend();
			}
        }

        private void AddToUnique(MailAddress to, MailMessage message, Dictionary<string, MailMessage> uniqueMessages)
        {
            MailMessage uniqueMessage;
            if (!uniqueMessages.TryGetValue(to.Host, out uniqueMessage))
            {
                uniqueMessage = new MailMessage();
                uniqueMessage.Subject = message.Subject;
                uniqueMessage.Body = message.Body;
                uniqueMessage.BodyEncoding = message.BodyEncoding;
                foreach (var attachment in message.Attachments)
                    uniqueMessage.Attachments.Add(attachment);
                foreach (var alternate in message.AlternateViews)
                    uniqueMessage.AlternateViews.Add(alternate);
                foreach (var replyTo in message.ReplyToList)
                    uniqueMessage.ReplyToList.Add(replyTo);
                uniqueMessage.IsBodyHtml = message.IsBodyHtml;
                uniqueMessage.Priority = message.Priority;
                
                uniqueMessage.From = message.From;
                uniqueMessage.Sender = message.Sender;
                uniqueMessage.DeliveryNotificationOptions = message.DeliveryNotificationOptions;
                uniqueMessage.SubjectEncoding = message.SubjectEncoding;

                uniqueMessages.Add(to.Host, uniqueMessage);
            }

            uniqueMessage.Bcc.Add(to);
        }

        public void EndAllRetries()
        {
            _RetryQueue.Stop();
        }
    }   
}
