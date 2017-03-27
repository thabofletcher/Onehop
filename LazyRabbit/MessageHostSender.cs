using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Bdev.Net.Dns;

namespace LazyRabbit
{
    public class MessageHostSender
    {
        private MailMessage _Message;
        private List<string> _EndPointIPs;
        private int _IPIndex;
        private SmtpClient _Client;
        private Action<Exception> _ExceptionLogger;
        private string _Host;
        private bool _Secure = false;
        private IPAddress _DNSServer;
        private RetryQueue _RetryQueue;
        private Action<MessageHostSender> _SendSuccess;

        public MessageHostSender(MailMessage message, 
                                string host, 
                                Action<Exception> callbackException = null, 
                                RetryQueue retryQ = null, 
                                Action<MessageHostSender> sendSuccess = null, 
                                bool secure = false)
        {
            _Message = message;
            _RetryQueue = retryQ;
            _ExceptionLogger = callbackException;
            _Host = host;
            _SendSuccess = sendSuccess;
            _Secure = secure;
        }

        public void TrySend()
        {
            try
            {
                _DNSServer = DefaultDns.Current;
                if (_DNSServer == null)
                    _DNSServer = IPAddress.Parse("8.8.8.8");

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
                catch (Exception exc)
                {
                    FailedSend(String.Format("Mail exchange lookups are failing for the configured DNS server: {0}", _DNSServer.ToString()), exc.ToString());
                    return;
                }

                _EndPointIPs = new List<string>();
                _EndPointIPs.AddRange(endPointIPs);

                if (_EndPointIPs.Count == 0)
                {
                    FailedSend(String.Format("No mail exchange endpoints were received from the configured DNS server: {0}", _DNSServer.ToString()));
                    return;
                }

                _IPIndex = 0;
                SendAsync();
            }
            catch (Exception exc)
            {
                LogException(exc);
            }
        }

        private void SendAsync()
        {
            if (_IPIndex < _EndPointIPs.Count)
            {
                _Client = new SmtpClient(_EndPointIPs[_IPIndex]);
                _Client.DeliveryMethod = SmtpDeliveryMethod.Network;
                _Client.EnableSsl = _Secure;
                _Client.SendCompleted += new SendCompletedEventHandler(_SendCompleted);
                _Client.SendAsync(_Message, this);
            }
        }

        private void FailedSend(string error, string details = "")
        {
            // if at first you don't succeed...
            if (_RetryQueue != null)
                _RetryQueue.BeginRetry(this, error, details);
            else
                LogException(new SendAttemptsExhaustedException(new List<DateTime>() { DateTime.Now }, this, error, details));
        }

        private void LogException(Exception exc)
        {
            if (_ExceptionLogger != null)
                _ExceptionLogger(exc);
        }

        private void _SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                {
                    // dont retry if it is a recipient error (that way the valid recipients dont get a billion emails)
                    Type errorType = e.Error.GetType();
                    if (errorType == typeof(SmtpFailedRecipientException))
                    {
                        LogException(new FailedRecipientException((e.Error as SmtpFailedRecipientException).FailedRecipient, this));
                        return;
                    }
                    if (errorType == typeof(SmtpFailedRecipientsException))
                    {
                        LogException(new FailedRecipientException((e.Error as SmtpFailedRecipientsException).InnerExceptions.Select(x => x.FailedRecipient).Aggregate((current, next) => current + "," + next), this));
                        return;
                    }
                    if (errorType == typeof(SmtpException))
                    {
                        SmtpStatusCode code = (e.Error as SmtpException).StatusCode;
                        switch (code)
                        {
                            case SmtpStatusCode.SyntaxError: // 500
                            case SmtpStatusCode.CommandUnrecognized: // 501?
                            case SmtpStatusCode.CommandNotImplemented: //502
                            case SmtpStatusCode.BadCommandSequence: //503
                            case SmtpStatusCode.CommandParameterNotImplemented: //504							
                            //case SmtpStatusCode.MailboxUnavailable: //550 
                            case SmtpStatusCode.MailboxNameNotAllowed: //553
                            case SmtpStatusCode.TransactionFailed: //554
                            case SmtpStatusCode.MustIssueStartTlsFirst:
                                LogException(new HostRejectedException(code, this));
                                return;
                        }
                    }

                    _IPIndex++;
                    if (_IPIndex < _EndPointIPs.Count)
                        SendAsync();
                    else
                        FailedSend("Message failed to send after attempting all mail exchanges.", e.Error.ToString());
                }
                else if (_SendSuccess != null)
                    _SendSuccess(this);
            }
            catch (Exception exc)
            {
                LogException(exc);
            }
        }

        public string Host
        {
            get
            {
                return _Host;
            }
        }

        public string MessageDetails
        {
            get
            {
                return "Bcc To: " + _Message.Bcc.ToString() + Environment.NewLine +
                "Subject: " + _Message.Subject + Environment.NewLine +
                "Message: " + _Message.Body;
            }
        }

        public string LastTriedMessage
        {
            get
            {
                return "Using DNS Server: " + _DNSServer + Environment.NewLine +
                    "Last Tried MX IPs: " + ((_EndPointIPs == null || _EndPointIPs.Count == 0) ? "" : _EndPointIPs.Aggregate((current, next) => current + "," + next));
            }
        }

        public override string ToString()
        {
            return MessageDetails + Environment.NewLine + LastTriedMessage + Environment.NewLine;
        }
    }
}
