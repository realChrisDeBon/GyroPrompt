using GyroPrompt.Network_Objects.TCPSocket;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using static GyroPrompt.Network_Objects.TCPSocket.TCPNetSettings;
using GyroPrompt.Basic_Objects.Collections;

namespace GyroPrompt.Network_Objects
{
    public class ServerSide : TCPNetSettings
    {
        public string serverIP;
        public string serverName;
        public TcpListener listener;
        public List<TcpClient> connectedClients = new List<TcpClient>();
        public List<ClientHandler> clients = new List<ClientHandler>();
        // Incoming and outgoing data packets append to a list to be handled with server protocol logic
        public List<dataPacket> incomingDataPackets = new List<dataPacket>();
        public List<dataPacket> outgoingDataPackets = new List<dataPacket>();
        // Defines the logic and rules for handling incoming and outgoing data packets
        public IDictionary<TCPServerProtocols, TaskList> serverProtocols = new Dictionary<TCPServerProtocols, TaskList>();
        private Parser toplevelParser;


        public ServerSide (Parser toplvlparser, string name_)
        {
            toplevelParser = toplvlparser;
            serverName = name_;
            serverIP = GetLocalIPAddress();
        }

        public async Task runProtocol(TCPServerProtocols protocol, string eventMessage)
        {
            if (serverProtocols.ContainsKey(protocol))
            {
                toplevelParser.executeTask(serverProtocols[protocol].taskList, serverProtocols[protocol].taskType, serverProtocols[protocol].scriptDelay);
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

        public async Task Start()
        {
            serverIP = GetLocalIPAddress();
            listener = new TcpListener(IPAddress.Any, 8888); // Need to make the IPAddress.Any a parameter that user passes (white list + black list IP)
            listener.Start();
            runProtocol(TCPServerProtocols.protocols_serverStarted, "serverstarted");
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                runProtocol(TCPServerProtocols.protocols_clientConnected, $"newconnection");
                IPAddress clientIpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                
                // Immediately filter out blacklisted connections
                if (IPAddressBook.ContainsKey(clientIpAddress))
                {
                    if (IPAddressBook[clientIpAddress] == IPStatus.Blacklist)
                    {
                        client.Close(); // Reject connection
                        continue;
                    }
                }
                // Connection is generally open unless it is set to only accept whitelisted addresses
                if (WhitelistConnectionsOnly == true)
                {
                    if (IPAddressBook.ContainsKey(clientIpAddress))
                    {
                        if (IPAddressBook[clientIpAddress] == IPStatus.Whitelist)
                        {
                            ClientHandler clientHandler = new ClientHandler(client, this);
                            clients.Add(clientHandler);
                            Thread clientThread = new Thread(clientHandler.HandleClient);
                            clientThread.Start();
                            runProtocol(TCPServerProtocols.protocols_clientConnected, $"Connected:{clientIpAddress.ToString}");
                        }
                    }
                }
                else
                {
                    ClientHandler clientHandler = new ClientHandler(client, this);
                    clients.Add(clientHandler);
                    Thread clientThread = new Thread(clientHandler.HandleClient);
                    clientThread.Start();
                    runProtocol(TCPServerProtocols.protocols_clientConnected, $"Connected:{clientIpAddress.ToString}");
                }
            }
        }

        public void AddDataPacket(dataPacket newDataPacket)
        {
            newDataPacket.senderAddress = serverIP;
            outgoingDataPackets.Add(newDataPacket);
            runProtocol(TCPServerProtocols.protocols_addDataPacket, $"Outgoing_datapacket_added:{newDataPacket.objType} Sender:{newDataPacket.senderAddress}");
        }

        public async Task BroadcastPacket(dataPacket packet, TcpClient senderClient)
        {
            string jsonString = JsonConvert.SerializeObject(packet);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            foreach (TcpClient client in connectedClients)
            {
                if (client != senderClient)
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public async Task BroadcastAllOutgoing()
        {
            foreach (dataPacket packet in outgoingDataPackets)
            {
                string jsonString = JsonConvert.SerializeObject(packet);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
                foreach(TcpClient client in connectedClients)
                {
                        NetworkStream stream = client.GetStream();
                        stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public void AssignProtocol(TCPServerProtocols protocolToAssign, TaskList tasklistToAssign)
        {
            serverProtocols.Add(protocolToAssign, tasklistToAssign);
        }

    }

    public class ClientHandler
    {
        private TcpClient client;
        private ServerSide server;
        private NetworkStream stream;

        public ClientHandler(TcpClient client, ServerSide server)
        {
            this.client = client;
            this.server = server;
            stream = client.GetStream();
        }

        public async void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break; // Client disconnected
                }

                // Broadcast the received message to all other connected clients
                string datamessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                dataPacket packet = JsonConvert.DeserializeObject<dataPacket>(datamessage);
                // Logic to handle data packet will go here
                // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            }

            // Client disconnected, remove from the list
            Console.WriteLine("Disconnected:" + client.Client.RemoteEndPoint);

            client.Close();
        }
    }
}