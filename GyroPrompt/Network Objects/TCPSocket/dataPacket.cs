using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.GUIComponents;
using GyroPrompt.Basic_Objects.Variables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Network_Objects.TCPSocket
{
    public class dataPacket
    {
        public string ID { get; set; }       
        public string senderAddress {get; set;}
        public NetObjType objType { get; set;}
        public string objName { get; set; }
        public string objData { get; set; }
    }
}