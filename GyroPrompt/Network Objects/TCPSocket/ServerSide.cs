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
using System.ComponentModel;
using System.Configuration;

namespace GyroPrompt.Network_Objects
{


    public class ServerSide : TCPNetSettings
    {
        public string serverIP;
        public string serverName;
        public TcpListener listener;
        public List<TcpClient> connectedClients = new List<TcpClient>();
        public List<ClientHandler> clients = new List<ClientHandler>();

        // Defines the logic and rules for handling incoming and outgoing data packets
        public IDictionary<TCPServerProtocols, TaskList> serverProtocols = new Dictionary<TCPServerProtocols, TaskList>();
        private Parser toplevelParser;

        public ServerSide (Parser toplvlparser, string name_)
        {
            toplevelParser = toplvlparser;
            serverName = name_;
            ParentName = name_;
            serverIP = GetLocalIPAddress();
            tcpobj_type = 1;
        }

        public async Task runProtocol(TCPServerProtocols protocol, string eventMessage)
        {
            string outputEventMessage = GetDescription(protocol);
            outputEventMessage += " " + eventMessage;
            toplevelParser.eventMessage_ = outputEventMessage;
            if (serverProtocols.ContainsKey(protocol))
            {
                toplevelParser.executeTask(serverProtocols[protocol].taskList, serverProtocols[protocol].taskType, serverProtocols[protocol].scriptDelay);
            }
        }

        public async Task Start()
        {
            bool startsuccess = true;
            listener = new TcpListener(IPAddress.Any, 8888); // Need to make the IPAddress.Any a parameter that user passes (white list + black list IP)
            try
            {
                listener.Start();
            } catch
            {
                startsuccess = false;
            } finally
            {
                if (startsuccess == true)
                {
                    runProtocol(TCPServerProtocols.protocols_serverStarted, "ServerStartSuccess");

                    while (true)
                    {
                        TcpClient client = await listener.AcceptTcpClientAsync();
                        IPAddress clientIpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                        string clientIP_ = clientIpAddress.ToString();

                        runProtocol(TCPServerProtocols.protocols_clientConnected, $"AttemptedConnection:{clientIP_}");
                        // Immediately filter out blacklisted connections
                        if (IPAddressBook.ContainsKey(clientIpAddress))
                        {
                            if (IPAddressBook[clientIpAddress] == IPStatus.Blacklist)
                            {
                                client.Close(); // Reject connection
                                runProtocol(TCPServerProtocols.protocols_clientRejected, $"ConnectionRejection:{clientIP_}");
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
                                    ClientHandler clientHandler = new ClientHandler(client, this, toplevelParser);
                                    clients.Add(clientHandler);
                                    Thread clientThread = new Thread(clientHandler.HandleClient);
                                    clientThread.Start();
                                    runProtocol(TCPServerProtocols.protocols_clientConnected, $"Connected:{clientIP_}");
                                }
                            }
                        }
                        else
                        {
                            ClientHandler clientHandler = new ClientHandler(client, this, toplevelParser);
                            clients.Add(clientHandler);
                            Thread clientThread = new Thread(clientHandler.HandleClient);
                            clientThread.Start();
                            runProtocol(TCPServerProtocols.protocols_clientConnected, $"Connected:{clientIP_}");
                        }
                    }

                } else if (startsuccess == false)
                {
                    runProtocol(TCPServerProtocols.protocols_serverStarted, "ServerStartFailed");
                }
            }

        }

        public void AddDataPacket(dataPacket newDataPacket)
        {
            newDataPacket.senderAddress = serverIP;
            outgoingDataPackets.Add(newDataPacket);
            runProtocol(TCPServerProtocols.protocols_addDataPacket, $"OutgoingDatapacketAdded:{newDataPacket.objType} Sender:{newDataPacket.senderAddress}");
        }

        public async Task BroadcastPacket(dataPacket packet, string omitIP = "0.0.0.0")
        {
            string jsonString = JsonConvert.SerializeObject(packet);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            foreach (var client in clients)
            {
                if (client.clientIP != omitIP)
                {
                    client.SendPacket(packet);
                }
            }
            outgoingDataPackets.Remove(packet);
            runProtocol(TCPServerProtocols.protocols_broadcastDatapacket, $"BroadcastDatapacketID:{packet.ID.ToString()}");
        }

        public async Task BroadcastAllOutgoing()
        {
            foreach (dataPacket packet in outgoingDataPackets)
            {
                string jsonString = JsonConvert.SerializeObject(packet);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
                foreach(var client in clients)
                {
                    client.SendPacket(packet);
                }
            }
            runProtocol(TCPServerProtocols.protocols_broadcastDatapacket, $"BroadcastedAllDatapackets");
        }

        public void MoveIncomingDPToOutgoingDP()
        {

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
        public string clientIP;
        private Parser toplvlParser;

        public ClientHandler(TcpClient client, ServerSide server, Parser topparser)
        {
            this.client = client;
            this.server = server;
            toplvlParser = topparser;
            stream = client.GetStream();
            clientIP = client.Client.RemoteEndPoint.ToString();
        }

        private async Task<string> readIncomingPacket()
        {
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        public async void HandleClient(object clientObj)
        {
            try
            {
                while (true)
                {
                    string dataPacketReceived = await readIncomingPacket();
                    dataPacket incomingDataPacket = JsonConvert.DeserializeObject<dataPacket>(dataPacketReceived);
                    // set a bool of 'RerouteToLocalStack' so in the future datapackets can first land in incomingDataPackets
                    // then optionally be automatically sent to local stack
                    server.incomingDataPackets.Add(incomingDataPacket);
                    
                    string objtypeStr = server.GetDescription(incomingDataPacket.objType);
                    server.runProtocol(TCPServerProtocols.protocols_receiveDataPacket, $"Sender:{incomingDataPacket.senderAddress} ID:{incomingDataPacket.ID} ObjectType:{objtypeStr}");
                }


                // Client disconnected, remove from the list
                server.connectedClients.Remove(client);
                server.clients.Remove(this);
                client.Close();
                server.runProtocol(TCPServerProtocols.protocols_clientDisconnected, $"{clientIP}");
            } catch
            {
                // ignore
            }
        }

        public async void SendPacket(dataPacket packet)
        {
            string jsonString = JsonConvert.SerializeObject(packet);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

    }
}