using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LazyRabbit
{
    public class OnehopMail
    {
        RetryQueue _RetryQueue = new RetryQueue();

        public void Send(MailMessage message, Action<Exception> callbackException = null)
        {
            HashSet<string> hosts = new HashSet<string>();
            foreach (var to in message.To)
            {
                hosts.Add(to.Host);
            }            

            foreach (var host in hosts)
                new MessageHostSender(message, host, callbackException, retryQ:_RetryQueue).TrySend();
        }

        public void EndAllRetries()
        {
            _RetryQueue.Stop();
        }
    }
}
