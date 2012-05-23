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
                myStack.ProxyHost = "192.168.0.7";
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
            TransportInfo localTransport = CreateTransport("192.168.0.5", 5060);
            SIPApp app = new SIPApp(localTransport);
            SIPStack stack = CreateStack(app,"192.168.0.7", 4060);
            app.Register("sip:alice@open-ims.test");
            Console.ReadKey();
            app.Invite("bob@open-ims.test");
            Console.ReadKey();
            app.EndCurrentCall();
            //app.Message("bob@open-ims.test", "Hello, this is alice saying howzit to bob");
            Console.ReadKey();
        }
    }
}
