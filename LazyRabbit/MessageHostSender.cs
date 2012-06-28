using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LazyRabbit
{
    public class MessageHostSender
    {
        private static Queue<MessageHostSender> _Q = new Queue<MessageHostSender>();
        private static object _L = new object();

        private MailMessage _Message;
        private List<string> _EndPointIPs;
        private int _IPIndex;
        SmtpClient _Client;

        public MessageHostSender(MailMessage message, string host)
        {
            _Message = message;

            var endPointIPs = new SortedSet<string>();
            // thank you bdev for sorting by pref already
            var mxs = Bdev.Net.Dns.Resolver.MXLookup(host, IPAddress.Parse("8.8.8.8"));
            foreach (var mx in mxs)
            {
                var ips = System.Net.Dns.GetHostAddresses(mx.DomainName);

                foreach (var ip in ips)
                {
                    endPointIPs.Add(ip.ToString());
                }
            }

            _EndPointIPs = new List<string>();
            _EndPointIPs.AddRange(endPointIPs);

            lock (_L)
                _Q.Enqueue(this);

            SendMsg();
        }


        private void Send()
        {
            var ep = _EndPointIPs[_IPIndex];
            _Client = new SmtpClient(ep);

            _Client.SendCompleted += _SendCompleted;

            _Client.Send(_Message);

            //_Client.SendCompleted += _SendCompleted;
            //_Client.SendAsync(_Message, this);

            //_Client.SendMailAsync(_Message).ContinueWith(task => {
            //    var t = task.IsFaulted;
            //    });
        }

        private static void SendMsg()
        {
            MessageHostSender msg;
            lock (_L)
                msg = _Q.Dequeue();

            msg.Send();
        }

        private static void _SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                {
                    //_IPIndex++;
                    //if (_IPIndex < _EndPointIPs.Count)
                    //    Send();
                    //else
                        throw new Exception("Message failed to send after attempting all mail exchanges.", e.Error);
                }
            }
            finally
            {
                SendMsg();
            }
        }
    }
}
