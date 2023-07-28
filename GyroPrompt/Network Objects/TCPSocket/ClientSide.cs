using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Network_Objects.TCPSocket;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;

namespace GyroPrompt.Network_Objects
{
    public class ClientSide : TCPNetSettings
    {
        public TcpClient client;
        public string username;
        public string name;
        public IDictionary<TCPClientProtocols, TaskList> clientProtocols = new Dictionary<TCPClientProtocols, TaskList>();
        private Parser toplevelParser;
        public bool hasStarted = false;
        public string thisClientIP {  get; set; }

        public async Task runProtocol(TCPClientProtocols protocol, string eventMessage)
        {
            string outputEventMessage = GetDescription(protocol);
            outputEventMessage += " " + eventMessage;
            lock (toplevelParser.eventMessageLock)
            {
                toplevelParser.eventMessage_ = outputEventMessage;
                if (clientProtocols.ContainsKey(protocol))
                {
                    toplevelParser.executeTask(clientProtocols[protocol].taskList, clientProtocols[protocol].taskType, clientProtocols[protocol].scriptDelay);
                }
            }
        }

        public ClientSide(Parser toplvlparser, string name_)
        {
            thisClientIP = GetLocalIPAddress();
            toplevelParser = toplvlparser;
            name = name_;
            ParentName = name_;
            tcpobj_type = 0;
        }

        public void AssignProtocol(TCPClientProtocols protocolToAssign, TaskList tasklistToAssign)
        {
            clientProtocols.Add(protocolToAssign, tasklistToAssign);
        }

        public void Start(string ipaddress)
        {
            bool startSuccess = true;
            client = new TcpClient();
            try
            {
                client.Connect(ipaddress, 8888); // Replace with the server IP address
                hasStarted = true;
            } catch
            {
                startSuccess = false;
                hasStarted = false;
            } finally
            {
                if (startSuccess == true)
                {
                    runProtocol(TCPClientProtocols.protocols_clientStarted, $"ConnectionSuccess:{ipaddress}");

                        // User will manually fine tune data packet handling
                        Thread receiveThread = new Thread(ReceiveDatapacket);
                        receiveThread.Start();

                } else if (startSuccess == false)
                {
                    runProtocol(TCPClientProtocols.protocols_clientStarted, $"ConnectionFailed:{ipaddress}");
                }
            }
            
        }
        public void SendDatapacket(dataPacket packet)
        {
            NetworkStream stream = client.GetStream();
            string jsonmsg = JsonConvert.SerializeObject(packet);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonmsg);
            stream.Write(buffer, 0, buffer.Length);
            runProtocol(TCPClientProtocols.protocols_broadcastDatapacket, $"BroadcastDatapacketID:{packet.ID.ToString()}");
        }
        public void ReceiveDatapacket()
        {
            try
            {
                NetworkStream stream = client.GetStream();
                while (true)
                {
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    dataPacket incomingDataPacket = JsonConvert.DeserializeObject<dataPacket>(message);
                    // set a bool of 'RerouteToLocalStack' so in the future datapackets can first land in incomingDataPackets
                    // then optionally be automatically sent to local stack
                    lock (toplevelParser.dpStackLock)
                    {
                        incomingDataPackets.Add(incomingDataPacket);
                        toplevelParser.addPacketToStack(incomingDataPacket);
                        string objtypeStr = GetDescription(incomingDataPacket.objType);
                        runProtocol(TCPClientProtocols.protocols_receiveDataPacket, $"Sender:{incomingDataPacket.senderAddress} ID:{incomingDataPacket.ID} ObjectType:{objtypeStr}");
                    }
                    if (bytesRead == 0)
                    {
                        hasStarted = false;
                        runProtocol(TCPClientProtocols.protocols_clientDisconnect, $"ConnectionLost");
                        break; // Server disconnected
                    }
                }
            }
            catch
            {
                runProtocol(TCPClientProtocols.protocols_clientDisconnect, "");
                client.Dispose();
                client.Close();
            }
        }

    }
}