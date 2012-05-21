using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIPLib;
using System.Net;

namespace SIPLibDriver
{
    class Program
    {

        public static SIPStack Create_Stack(SIPApp app,string proxy_ip = null, int proxy_port = -1)
        {
            SIPStack my_stack = new SIPStack(app);
            my_stack.uri.user = "alice";
            if (proxy_ip != null)
            {
                my_stack.proxy_host = "192.168.0.7";
                if (proxy_port == -1)
                {
                    my_stack.proxy_port = 5060;
                }
                else
                {
                    my_stack.proxy_port = proxy_port;
                }
            }
            return my_stack;
        }

        public static TransportInfo CreateTransport(string listen_ip, int listen_port)
        {
            return new TransportInfo(IPAddress.Parse(listen_ip), listen_port, System.Net.Sockets.ProtocolType.Udp);
        }

        static void Main(string[] args)
        {
            //TransportInfo local_transport = createTransport(Utils.get_local_ip(), 5060);
            TransportInfo local_transport = CreateTransport("192.168.0.5", 5060);
            SIPApp app = new SIPApp(local_transport);
            SIPStack stack = Create_Stack(app,"192.168.0.7", 4060);
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
