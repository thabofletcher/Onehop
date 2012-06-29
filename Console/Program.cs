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

            msg.To.Add(new MailAddress("you@gmail.com"));

            var ka = new LazyRabbit.OnehopMail();
            ka.Send(msg, exc => { Console.WriteLine(exc.ToString()); });

            Console.Read();
        }
    }
}
