
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