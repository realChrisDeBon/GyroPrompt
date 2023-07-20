using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.Variables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
        [Description("Null")]
        ObjNull,
        [Description("String")]
        ObjString,
        [Description("Int")]
        ObjInt,
        [Description("Float")]
        ObjFloat,
        [Description("Bool")]
        ObjBool,
        [Description("List")]
        ObjList,
        [Description("Binary")]
        ByteArray
    }
    public enum TCPServerProtocols
    {
        [Description("ServerProtocol:ServerStarted")]
        protocols_serverStarted,
        [Description("ServerProtocol:ReceivedDataPacket")]
        protocols_receiveDataPacket,
        [Description("ServerProtocol:ClientConnected")]
        protocols_clientConnected,
        [Description("ServerProtocol:ClientDisconnected")]
        protocols_clientDisconnected,
        [Description("ServerProtocol:ClientRejected")]
        protocols_clientRejected,
        [Description("ServerProtocol:DataPacketAdded")]
        protocols_addDataPacket,
        [Description("ServerProtocol:BroadcastDataPacket")]
        protocols_broadcastDatapacket,
        [Description("ServerProtocol:BroadcastAllOutDataPackets")]
        protocols_broadcastAllOutgoing,
    }
    public enum TCPClientProtocols
    {
        [Description("ClientProtocol:ClientStarted")]
        protocols_clientStarted,
        [Description("ClientProtocol:ClientDisconnect")]
        protocols_clientDisconnect,
        [Description("ClientProtocol:ReceivedDataPacket")]
        protocols_receiveDataPacket,
        [Description("ClientProtocol:DataPacketAdded")]
        protocols_addDataPacket,
        [Description("ClientProtocol:BroadcastDataPacket")]
        protocols_broadcastDatapacket,
        [Description("ClientProtocol:BroadcastAllOutDataPackets")]
        protocols_broadcastAllOutgoing,
    }
    public class TCPNetSettings
    {
        public string ParentName;
        public int tcpobj_type = -1; // 0 for client, 1 for server
        // An IPAddressBook that blacklists and whitelists IP address (some basic security settings)
        public IDictionary<IPAddress, IPStatus> IPAddressBook = new Dictionary<IPAddress, IPStatus>();
        public bool WhitelistConnectionsOnly = false;
        // Compartmentalize the data going over the network
        public List<dataPacket> incomingDataPackets = new List<dataPacket>();
        public List<dataPacket> outgoingDataPackets = new List<dataPacket>();

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

        public string GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "IP Address Not Found";
        }

        public string GetDescription(Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    }
}
