using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;

namespace LazyRabbit
{
    public class DefaultDns
    {
        public static IPAddress Current
        {
            get
            {
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface ni in nics)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        IPAddressCollection ips = ni.GetIPProperties().DnsAddresses;
                        foreach (var ip in ips)
                            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                return ip;
                    }
                }
                return null;
            }

        }
    }
}
