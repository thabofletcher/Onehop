using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Bdev.Net.Dns;
using LazyRabbit;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestToString();
            //TestDNS();
			TestMailing();

			//MXLoadTest();

			Console.Read();
        }

		private static void MXLoadTest()
		{

			for (int i = 0; i < 10000; i++)
			{

				try
				{
					Resolver.MXLookup("google.com", DefaultDns.Current);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}

		}

        private static void TestMailing()
		{
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
            var mxs = Resolver.MXLookup(host, dnsServer);
            foreach (var mx in mxs)
            {
                Console.WriteLine(mx.DomainName);
            }
        }

        private static void TestToString()
        {
            MailMessage message = TestMail();
            HashSet<string> hosts = new HashSet<string>();
            foreach (var to in message.To)
            {
                hosts.Add(to.Host);
            }            

            foreach (var host in hosts)
                Console.WriteLine(new MessageHostSender(message, host));
        }

		private static MailMessage TestMail()
		{
			var msg = new MailMessage
			{
				From = new MailAddress("\"Thabo Fletcher \"<thabo@braverobot.net>"),
				Subject = "Lazy rabbit test",
				Body = "This is a test of the lazy rabbit notification system.",
			};

			// msg.To.Add(new MailAddress("thabo.fletcher@gmail.com"));
			// msg.To.Add(new MailAddress("youdontexistforsure@gmail.com"));
			//msg.To.Add(new MailAddress("neitherdoyouyouinsensitivecad@gmail.com"));
			//msg.To.Add(new MailAddress("johnstewie@rocketmail.com"));
			//msg.To.Add(new MailAddress("sakdfjlksajfdlksjadf@rocketmail.com"));
			msg.To.Add(new MailAddress("thabo@epi.com.mx"));

			return msg;
		}
    }
}
