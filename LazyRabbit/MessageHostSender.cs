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
        private MailMessage _Message;
        private List<string> _EndPointIPs;
        private int _IPIndex;
        SmtpClient _Client;
        Action<Exception> _ExceptionLogger;

        public MessageHostSender(MailMessage message, string host, IPAddress dnsServer, Action<Exception> callbackException)
        {
            _Message = message;

            var endPointIPs = new SortedSet<string>();
            // thank you bdev for sorting by pref already
            var mxs = Bdev.Net.Dns.Resolver.MXLookup(host, dnsServer);
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
            _IPIndex = 0;
            _ExceptionLogger = callbackException;

            SendAsync();
        }


        private void Send()
        {
            foreach (var ip in _EndPointIPs)
            {
                try
                {
                    _Client = new SmtpClient(ip);
                    _Client.Send(_Message);
                    return;
                }
                catch { }
            }

            throw new Exception("Message failed to send after attempting all mail exchanges.");
        }

        private void SendAsync()
        {
            if (_IPIndex < _EndPointIPs.Count)
            {
                _Client = new SmtpClient(_EndPointIPs[_IPIndex]);
                _Client.SendCompleted += new SendCompletedEventHandler(_SendCompleted);
                _Client.SendAsync(_Message, this);
            }
        }

        private void _SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _IPIndex++;
                if (_IPIndex < _EndPointIPs.Count)
                    SendAsync();
                else
                    if (_ExceptionLogger != null)
                        _ExceptionLogger(new Exception("Message failed to send after attempting all mail exchanges.", e.Error));
            }
        }
    }
}
