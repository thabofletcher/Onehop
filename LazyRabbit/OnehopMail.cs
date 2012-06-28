using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LazyRabbit
{
    public class OnehopMail
    {
        //static HashSet<MessageHostSender> _Pool = new HashSet<MessageHostSender>();

        public static void Send(MailMessage message)
        {
            HashSet<string> hosts = new HashSet<string>();
            foreach (var to in message.To)
            {
                hosts.Add(to.Host);
            }

            foreach (var host in hosts)
                new MessageHostSender(message, host);
        }
    }
}
