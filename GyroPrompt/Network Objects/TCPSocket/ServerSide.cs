using GyroPrompt.Network_Objects.TCPSocket;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using static GyroPrompt.Network_Objects.TCPSocket.TCPNetSettings;

namespace GyroPrompt.Network_Objects
{
    public class ServerSide : TCPNetSettings
    {
        public string serverIP;
        public TcpListener listener;
        public List<TcpClient> connectedClients = new List<TcpClient>();
        public List<ClientHandler> clients = new List<ClientHandler>();
        // Incoming and outgoing data packets append to a list to be handled with server protocol logic
        public List<dataPacket> incomingDataPackets = new List<dataPacket>();
        public List<dataPacket> outgoingDataPackets = new List<dataPacket>();
        // Defines the logic and rules for handling incoming and outgoing data packets
        public List<string> protocols_serverStarted = new List<string>();
        public List<string> protocols_receiveDataPacket = new List<string>();
        public List<string> protocols_clientConnected = new List<string>();
        public List<string> protocols_clientRejected = new List<string>();
        public List<string> protocols_broadcastDatapacket = new List<string>();

        public async Task runProtocol(TCPServerProtocols protocol, string eventMessage)
        {

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
                            runProtocol(TCPServerProtocols.protocols_clientConnected, $"{clientIpAddress.ToString}-connected");
                        }
                    }
                }
                else
                {
                    ClientHandler clientHandler = new ClientHandler(client, this);
                    clients.Add(clientHandler);
                    Thread clientThread = new Thread(clientHandler.HandleClient);
                    clientThread.Start();
                    runProtocol(TCPServerProtocols.protocols_clientConnected, $"{clientIpAddress.ToString}-connected");
                }
            }
        }

 

        public void AddDataPacket(dataPacket newDataPacket)
        {
            newDataPacket.senderAddress = serverIP;
            outgoingDataPackets.Add(newDataPacket);
            runProtocol(TCPServerProtocols.protocols_addDataPacket, $"Outgoing datapacket added: {newDataPacket.objType} Sender: {newDataPacket.senderAddress}");
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
            Console.WriteLine("Client disconnected: " + client.Client.RemoteEndPoint);

            client.Close();
        }




    }
}