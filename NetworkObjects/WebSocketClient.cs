using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GyroPromptNameSpace;
using Newtonsoft.Json;
using System.Net;
using WebSocketSharp;
using System.Linq;
using BlockchainSpace;

namespace GyroPrompt.NetworkObjects
{
    public class WebSocketClient : Parser
    {
        public bool IsBlockchain { get; set; } // 100
        public bool IsStringData { get; set; } // 010
        public bool IsGyroScript { get; set; } // 001
        public int TunnelType { get; set; }
        public string TunnelName { get; set; }
        public string URL { get; set; }
        public int PORT { get; set; }
        public List<string> CommandList = new List<string>();

        public string Message { get; set; }
        public bool SendMessage = false;
        public bool Running = true;

        public WebSocketClient(string url, int port, bool IsChain, bool IsRegularData, bool IsScript)
        {
            URL = url;
            IsBlockchain = IsChain;
            IsStringData = IsStringData;
            IsGyroScript = IsScript;
            PORT = port;
        }

        public void Initialize()
        {
            CommandList.Add("PRINT {T}:{SOCKETBUFFER}");
            StringBuilder a = new StringBuilder("ws://");
            a.Append(URL + ":" + PORT);
            if (IsBlockchain == true)
            {
                //a.Append("/Blockchain");
                TunnelType = 0;
            }
            else if (IsStringData == true)
            {
                //a.Append("/DataTunnel");
                TunnelType = 1;
            }
            else if (IsGyroScript == true)
            {
                //a.Append("/ScriptStream");
                TunnelType = 2;
            }
            string b = a.ToString();
            WebSocketSharp.WebSocket ws = new WebSocketSharp.WebSocket(b);

            ws.Connect();

  
            ws.OnMessage += (sender, e) =>
            {
                foreach (Variable var in ActiveVariables)
                {
                    if (var.VarName == "SOCKETBUFFER")
                    {
                        var.Message = e.Data.ToString();
                        break;
                    }
                }
                foreach (string str in CommandList)
                {
                    Parse(str);
                }
            };

            while (Running == true)
            {
                if (SendMessage == true)
                {
                    ws.Send(Message);
                    SendMessage = false;
                } else
                {
                    // Do nothing
                }
            }
        } 

        public void Broadcast (string message)
        {
            SendMessage = true;
            Message = message;
        }
    }
}
