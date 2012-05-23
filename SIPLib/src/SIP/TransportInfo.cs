using System.Net;
using System.Net.Sockets;

namespace SIPLib.SIP
{
    public class TransportInfo
    {
        public IPAddress Host { get; set; }
        public int Port { get; set; }
        public ProtocolType Type {get;set;}
        public Socket Socket { get; set; }
        public bool Reliable { get; set; }
        
        public TransportInfo(IPAddress localAddress, int listenPort,ProtocolType type)
        {
            Host = localAddress;
            Port = listenPort;
            Type = type;
        }
    }
}
