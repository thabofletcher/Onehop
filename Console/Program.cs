using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using LazyRabbit;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestToString();
            //TestDNS();
            var msg = TestMail();

            var ka = new OnehopMail();
            ka.Send(msg, exc => { Console.WriteLine(exc.ToString()); });

            Console.Read();

            ka.EndAllRetries();            
        }

        private static void TestDNS()
        {
            var dnsServer = DefaultDns.Current;
            if (dnsServer == null)
            {
                Console.WriteLine("Invalid DNS, reverting to Google");                
            }
            else
            {
                Console.WriteLine("MXs according to your nameserver " + dnsServer.ToString() + " :");
                try
                {
                    PrintMXs("gmailzzzzzzzzzzzzzzzzzzzzz.com", dnsServer);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("failed: " + exc.ToString());
                }
            }

            Console.WriteLine("MXs according to Google:");
            dnsServer = IPAddress.Parse("8.8.8.8");
            PrintMXs("gmail.com", dnsServer);
        }

        private static void PrintMXs(string host, IPAddress dnsServer)
        {
            var mxs = Bdev.Net.Dns.Resolver.MXLookup(host, dnsServer);
            foreach (var mx in mxs)
            {
                Console.WriteLine(mx.DomainName);
            }
        }

        private static void TestToString()
        {
            MailMessage message = TestMail();
            var dnsServer = DefaultDns.Current;
            if (dnsServer == null)
                dnsServer = IPAddress.Parse("8.8.8.8");

            //Console.WriteLine("using dns: " + dnsServer.ToString());

            HashSet<string> hosts = new HashSet<string>();
            foreach (var to in message.To)
            {
                hosts.Add(to.Host);
            }            

            foreach (var host in hosts)
                Console.WriteLine(new MessageHostSender(message, host, dnsServer));
        }

        private static MailMessage TestMail()
        {
            var msg = new MailMessage
            {
                From = new MailAddress("me@" + Environment.MachineName),
                Subject = "test",
                Body = "test",
            };

            msg.To.Add(new MailAddress("youdontexistforsure@gmail.com"));

            return msg;
        }
    }
}
