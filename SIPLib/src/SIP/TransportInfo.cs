using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SIPLib
{
    public class TransportInfo
    {
        public IPAddress host { get; set; }
        public int port { get; set; }
        public ProtocolType type {get;set;}
        public Socket socket { get; set; }
        public bool reliable { get; set; }
        
        public TransportInfo(IPAddress local_address, int ListenPort,ProtocolType type)
        {
            this.host = local_address;
            this.port = ListenPort;
            this.type = type;
        }
    }
}
