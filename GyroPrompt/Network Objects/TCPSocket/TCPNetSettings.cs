using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Network_Objects.TCPSocket
{
    public enum IPStatus
    {
        Whitelist,
        Blacklist
    }
    public enum NetObjType
    {
        ObjString,
        ObjInt,
        ObjList,
        ByteArray
    }
    public enum TCPServerProtocols
    {
        protocols_serverStarted,
        protocols_receiveDataPacket,
        protocols_clientConnected,
        protocols_clientRejected,
        protocols_addDataPacket,
        protocols_broadcastDatapacket,
        protocols_broadcastAllOutgoing,
    }
    public class TCPNetSettings
    {
        // An IPAddressBook that blacklists and whitelists IP address (some basic security settings)
        public IDictionary<IPAddress, IPStatus> IPAddressBook = new Dictionary<IPAddress, IPStatus>();
        public bool WhitelistConnectionsOnly = false;
        // Compartmentalize the data going over the network
        
        public void addtoWhitelist(string input, LocalList list = default)
        {
            IPAddress ipAddress;
            if (list != default(LocalList))
            {
                foreach (LocalVariable var in list.items)
                {
                    string ipAddressString = var.Value;
                    if (IPAddress.TryParse(ipAddressString, out ipAddress))
                    {
                        Console.WriteLine("Added to whitelist: " + ipAddress);
                        IPAddressBook.Add(ipAddress, IPStatus.Whitelist);
                    }
                    else
                    {
                        Console.WriteLine("Invalid IP address.");
                    }
                }
            } else
            {
                string ipAddressString = input;
                if (IPAddress.TryParse(ipAddressString, out ipAddress))
                {
                    Console.WriteLine("Added to whitelist: " + ipAddress);
                    IPAddressBook.Add(ipAddress, IPStatus.Whitelist);
                }
                else
                {
                    Console.WriteLine("Invalid IP address.");
                }
            }
        }
        public void addtoBlacklist(string input, LocalList list = default)
        {
            IPAddress ipAddress;
            if (list != default(LocalList))
            {
                foreach (LocalVariable var in list.items)
                {
                    string ipAddressString = var.Value;
                    if (IPAddress.TryParse(ipAddressString, out ipAddress))
                    {
                        Console.WriteLine("Added to blacklist: " + ipAddress);
                        IPAddressBook.Add(ipAddress, IPStatus.Blacklist);
                    }
                    else
                    {
                        Console.WriteLine("Invalid IP address.");
                    }
                }
            }
            else
            {
                string ipAddressString = input;
                if (IPAddress.TryParse(ipAddressString, out ipAddress))
                {
                    Console.WriteLine("Added to blacklist: " + ipAddress);
                    IPAddressBook.Add(ipAddress, IPStatus.Blacklist);
                }
                else
                {
                    Console.WriteLine("Invalid IP address.");
                }
            }
        }
    }
}
