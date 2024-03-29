﻿using GlobalSuppressions;
using System.Net.NetworkInformation;

namespace GyroPrompt.Network_Functions
{
    public class SendNetPing
    {
        Ping pingSender = new Ping();
#pragma warning disable 8625
        public string sendPingAsync(string address_, int timeout_ = 500, byte[] buffer_= null)
        {
            byte[] buffer = buffer_ ?? new byte[1024];
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            PingReply reply = pingSender.Send(address_, timeout_, buffer_);
            string message = "";
            switch(reply.Status)
            {
                case System.Net.NetworkInformation.IPStatus.Success:
                    string a = reply.RoundtripTime.ToString();
                    break;
                case System.Net.NetworkInformation.IPStatus.DestinationNetworkUnreachable:

                    break;
                case System.Net.NetworkInformation.IPStatus.DestinationHostUnreachable:

                    break;
                case System.Net.NetworkInformation.IPStatus.DestinationProhibited:

                    break;
                case System.Net.NetworkInformation.IPStatus.BadDestination:

                    break;
                case System.Net.NetworkInformation.IPStatus.HardwareError:

                    break;
                case System.Net.NetworkInformation.IPStatus.BadRoute:

                    break;
            }
            return message;
        }
    }
}