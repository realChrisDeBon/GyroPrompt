using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace GyroPrompt.Network_Functions
{
    public class SendNetPing
    {
        Ping pingSender = new Ping();
        public string sendPingAsync(string address_, int timeout_ = 500, byte[] buffer_= null)
        {
            byte[] buffer = buffer_ ?? new byte[1024];
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            PingReply reply = pingSender.Send(address_, timeout_, buffer_);
            string message = "";
            switch(reply.Status)
            {
                case IPStatus.Success:
                    string a = reply.RoundtripTime.ToString();
                    break;
                case IPStatus.DestinationNetworkUnreachable:

                    break;
                case IPStatus.DestinationHostUnreachable:

                    break;
                case IPStatus.DestinationProhibited:

                    break;
                case IPStatus.BadDestination:

                    break;
                case IPStatus.HardwareError:

                    break;
                case IPStatus.BadRoute:

                    break;
            }
            return message;
        }
    }
}