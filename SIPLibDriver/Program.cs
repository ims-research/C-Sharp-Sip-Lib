using System;
using SIPLib;
using System.Net;
using SIPLib.SIP;

namespace SIPLibDriver
{
    class Program
    {

        public static SIPStack CreateStack(SIPApp app,string proxyIp = null, int proxyPort = -1)
        {
            SIPStack myStack = new SIPStack(app) {Uri = {User = "alice"}};
            if (proxyIp != null)
            {
                myStack.ProxyHost = proxyIp;
                myStack.ProxyPort = (proxyPort == -1) ? 5060 : proxyPort;
            }
            return myStack;
        }

        public static TransportInfo CreateTransport(string listenIp, int listenPort)
        {
            return new TransportInfo(IPAddress.Parse(listenIp), listenPort, System.Net.Sockets.ProtocolType.Udp);
        }

        static void Main(string[] args)
        {
            //TransportInfo local_transport = createTransport(Utils.get_local_ip(), 5060);
            TransportInfo localTransport = CreateTransport("192.168.20.28", 6060);
            SIPApp app = new SIPApp(localTransport);
            SIPStack stack = CreateStack(app,"192.168.20.28", 5060);
            app.Register("sip:r@192.168.20.28");
            Console.ReadKey();
            app.Invite("bob@open-ims.test");
            Console.ReadKey();
            app.EndCurrentCall();
            //app.Message("bob@open-ims.test", "Hello, this is alice saying howzit to bob");
            Console.ReadKey();
        }
    }
}
