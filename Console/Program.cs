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
                Console.WriteLine("MXs according to your nameserver:");
                try
                {
                    PrintMXs("gmail.com", dnsServer);
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
