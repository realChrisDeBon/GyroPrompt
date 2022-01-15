using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Threading;

using System.Threading.Tasks;
using GyroPromptNameSpace;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Linq;
using BlockchainSpace;
using System.ComponentModel.DataAnnotations;

namespace GyroPrompt.NetworkObjects
{
    public class SocketServer
    {

        public int PortNumber { get; set; }
        public string ServerName { get; set; }

        public SocketServer(int _Port, string _Name)
        {
            PortNumber = _Port;
            ServerName = _Name;
        }

        public void Initalize()
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, PortNumber);
            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            newsock.Bind(localEndPoint);
            newsock.Listen(10);
            Socket client = newsock.Accept();         

        }

        public void RecieveData()
        {

        }

    }
}