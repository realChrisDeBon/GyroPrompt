using GyroPrompt.Network_Objects.TCPSocket;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GyroPrompt.Network_Objects
{
    public class ClientSide : TCPNetSettings
    {
        public TcpClient client;
        public string username;
        public List<dataPacket> incomingDataPackets = new List<dataPacket>();
        public List<dataPacket> outgoingDataPackets = new List<dataPacket>();


        public void Start(string ipaddress, string clientIP)
        {

            client = new TcpClient();
            client.Connect(ipaddress, 8888); // Replace with the server IP address

            // Start a new thread to handle server communication
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();

            // Start a new thread to handle user input
            Thread sendThread = new Thread(SendMessages);
            sendThread.Start();
        }

        private void SendMessages()
        {
            NetworkStream stream = client.GetStream();
            while (true)
            {
                string message = Console.ReadLine();

                if (message == "TERMINATE")
                {
                    client.Close();
                    break;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(message);
                stream.Write(buffer, 0, buffer.Length);
            }

            Console.WriteLine("Disconnected.");
        }

        private void ReceiveMessages()
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break; // Server disconnected
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received from server: " + message);
            }

            Console.WriteLine("Server disconnected.");
            client.Dispose();
            Environment.Exit(0);
        }
    }
}