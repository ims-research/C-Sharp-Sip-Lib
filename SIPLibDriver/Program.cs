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

        public SIPStack stack;
        public SIPApp app;
        public UserAgent client_ua;

        public void Received_Data_Event(object sender, RawEventArgs e)
        {
            this.stack.received(e.data, e.src);
        }  
     
        private void Register(string uri)
        {
            this.client_ua = new UserAgent(this.stack, null, false);
            Message register_msg = this.client_ua.createRegister(new SIPURI(uri));
            register_msg.insertHeader(new Header("3600","Expires"));
            this.client_ua.sendRequest(register_msg);
        }

        public static SIPStack Create_Stack(string listen_ip, int listen_port, string proxy_ip = null, int proxy_port = -1)
        {
            string myHost = System.Net.Dns.GetHostName();
            System.Net.IPHostEntry myIPs = System.Net.Dns.GetHostEntry(myHost);
            TransportInfo sip_transport = new TransportInfo(IPAddress.Parse(listen_ip), listen_port, System.Net.Sockets.ProtocolType.Udp);
            SIPApp app = new SIPApp(sip_transport);
            SIPStack my_stack = new SIPStack(app);

            my_stack.uri.user = "alice";
            if (proxy_ip != null)
            {
                my_stack.proxy_ip = "192.168.0.7";
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

        static void Main(string[] args)
        {
            Program wrapper = new Program();
            wrapper.stack = Create_Stack(Utils.get_local_ip(), 5060, "192.168.0.7", 4060);
            wrapper.app = wrapper.stack.app;
            wrapper.Register("sip:alice@open-ims.test");
            Console.ReadKey();

            //System.Console.WriteLine("TEST");
            //Dictionary<string,string> context = new Dictionary<string,string>();
            //context.Add("cnonce","0a4f113b");
            //context.Add("nc","0");
            //string challenge = "Digest realm=\"open-ims.test\", nonce=\"f6b39889303acbce66517e52cb2b977b\", algorithm=MD5, qop=\"auth\"";
            //string response = Authenticate.createAuthorization(challenge,"alice@open-ims.test","alice","sip:open-ims.test","REGISTER",null,null);
            //System.Console.WriteLine("TEST");
            //Console.ReadKey();
        }
    }
}
