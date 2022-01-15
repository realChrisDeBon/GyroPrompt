using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;

namespace GyroPrompt.NetworkObjects
{
    class NetworkPing
    {
        public string PingStatus { get; set; }
        public bool IsSuccess { get; set; }

        public string PingAddress(string urlSite, int timeout, string data, int pingCount)
        {
            Ping pingSend = new Ping();
            PingOptions options = new PingOptions();
            byte[] buff = Encoding.ASCII.GetBytes(data);
            options.DontFragment = true;
            try
            {
                PingReply pingreply = pingSend.Send(urlSite, timeout, buff, options);
                if (pingreply.Status == IPStatus.Success)
                {
                    StringBuilder newStr = new StringBuilder();
                    newStr.Append($"Ping address: {pingreply.Address.ToString()}\n");
                    newStr.Append($"Trip time: {pingreply.RoundtripTime} ms\n");
                    newStr.Append($"Bytes: {pingreply.Buffer.Length}\n");
                    PingStatus = Convert.ToString(newStr);
                    IsSuccess = true;
                    return PingStatus;
                }
                else
                {
                    PingStatus = $"Ping to {urlSite} failed.\n";
                    IsSuccess = false;
                    return PingStatus;
                }
            } catch
            {
                PingStatus = $"Ping to {urlSite} failed.\n";
                IsSuccess = false;
                return PingStatus;
            }
        }
    }
}
