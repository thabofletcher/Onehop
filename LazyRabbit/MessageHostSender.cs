using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Bdev.Net.Dns;

namespace LazyRabbit
{
    public class UnresponsiveDNSServer : Exception
    {
        private IPAddress _SuspectDNSServer;
        public IPAddress SuspectDNSServer { get { return _SuspectDNSServer; } }
        public UnresponsiveDNSServer(IPAddress suspect)
        {
            _SuspectDNSServer = suspect;
        }
    }

    public class MessageHostSender
    {
        private MailMessage _Message;
        private List<string> _EndPointIPs;
        private int _IPIndex;
        SmtpClient _Client;
        Action<Exception> _ExceptionLogger;
        string _Host;
        IPAddress _DNSServer;
        RetryQueue _RetryQueue;

        public MessageHostSender(MailMessage message, string host, IPAddress dnsServer, Action<Exception> callbackException = null, RetryQueue retryQ = null)
        {
            _Message = message;
            _RetryQueue = retryQ;
            _ExceptionLogger = callbackException;
            _Host = host;
            _DNSServer = dnsServer;
        }

        public void TrySend()
        {
            var endPointIPs = GetMXIPs();

            _EndPointIPs = new List<string>();
            _EndPointIPs.AddRange(endPointIPs);

            if (_EndPointIPs.Count == 0)
            {
                // if at first you don't succeed...
                if (_RetryQueue != null)
                    _RetryQueue.BeginRetry(this);
            }
            else
            {
                _IPIndex = 0;
                SendAsync();
            }
        }

        private SortedSet<string> GetMXIPs()
        {
            var endPointIPs = new SortedSet<string>();
            try
            {
                // thank you bdev for sorting by pref already
                var mxs = Resolver.MXLookup(_Host, _DNSServer);
                foreach (var mx in mxs)
                {
                    var ips = Dns.GetHostAddresses(mx.DomainName);

                    foreach (var ip in ips)
                    {
                        endPointIPs.Add(ip.ToString());
                    }
                }
            }
            catch (NoResponseException)
            {
                throw new UnresponsiveDNSServer(_DNSServer);
            }
            return endPointIPs;
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
            try
            {
                if (e.Error != null)
                {
                    _IPIndex++;
                    if (_IPIndex < _EndPointIPs.Count)
                        SendAsync();
                    else
                        if (_ExceptionLogger != null)
                        {
                            _ExceptionLogger(new Exception("Message failed to send after attempting all mail exchanges.", e.Error));
                            // if at first you don't succeed...
                            if (_RetryQueue != null)
                                _RetryQueue.BeginRetry(this);
                        }
                }
            }
            catch (Exception exc)
            {
                if (_ExceptionLogger != null)
                    _ExceptionLogger(exc);
            }
        }

        public string Host
        {
            get
            {
                return _Host;
            }
        }

        public override string ToString()
        {
            return "To: " + _Message.To.ToString() + Environment.NewLine +
                "Subject: " + _Message.Subject + Environment.NewLine +
                "Message: " + _Message.Body + Environment.NewLine +
                "Using DNS Server: " + _DNSServer + Environment.NewLine +
                "Last Tried MX IPs: " + GetMXIPs().Aggregate((current, next) => current + "," + next) + Environment.NewLine;
        }
    }
}
