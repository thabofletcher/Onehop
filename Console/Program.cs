using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {

            var msg = new MailMessage
            {
                From = new MailAddress("me@me.com"),
                Subject = "test",
                Body = "test",
            };

            msg.To.Add(msg.From);

            LazyRabbit.OnehopMail.Send(msg);
        }
    }
}
