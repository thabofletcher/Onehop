using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            var msg = new MailMessage
            {
                From = new MailAddress("me@" + Environment.MachineName),
                Subject = "test",
                Body = "test",
            };

            msg.To.Add(new MailAddress("me@me.com"));

            LazyRabbit.OnehopMail.Send(msg);

            Console.Read();
        }
    }
}
