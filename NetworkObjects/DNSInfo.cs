using System;
using System.Collections.Generic;
using System.Text;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net;
using System.IO;

namespace GyroPrompt.NetworkObjects
{
    class DNSInfo
    {
        public void ShowNetInfo()
        {
            string a = IPAddress.Broadcast.ToString();

            string hostName = Dns.GetHostName();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine($"\nHostname: {host.HostName}");
            string myIP = Dns.GetHostByName(hostName).AddressList[1].ToString();

            //Console.WriteLine($"IP Address: {myIP}\n");

            string ip = null;

            foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (f.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (GatewayIPAddressInformation d in f.GetIPProperties().GatewayAddresses)
                    {
                        ip = d.Address.ToString();
                        Console.WriteLine(ip);
                    }
                }
            }



        }
    }
}