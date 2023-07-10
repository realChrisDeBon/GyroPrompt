using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Network_Objects.TCPSocket
{
    public class dataPacket
    {
        public string ID { get; set; }
        public string senderAddress { get; set; }
        public NetObjType objType { get; set; }
        public object sentData { get; set; }
    }
}