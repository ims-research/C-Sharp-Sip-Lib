using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SIPLib
{
    public class SIPApp
    {
        public SIPStack stack { get; set; }
        private byte[] temp_buffer { get; set; }
        public TransportInfo transport { get; set; }

        public event EventHandler<RawEventArgs> Received_Data_Event;
        public event EventHandler<RawEventArgs> Sent_Data_Event;

        public void send(string data, string ip,int port,SIPStack stack)
        {
            IPAddress[] addresses = System.Net.Dns.GetHostAddresses(ip);
            IPEndPoint dest = new IPEndPoint(addresses[0], port);
            EndPoint destEP = (EndPoint)dest;
            byte[] send_data = ASCIIEncoding.ASCII.GetBytes(data);
            stack.transport.socket.BeginSendTo(send_data, 0, send_data.Length, SocketFlags.None, destEP, new AsyncCallback(this.SendDataCB), destEP);
        }
        
        public SIPApp(TransportInfo transport)
        {
            this.temp_buffer = new byte[4096];
            if (transport.type == ProtocolType.Tcp)
            {
                transport.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            else
            {
                transport.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            IPEndPoint localEP = new IPEndPoint(transport.host, transport.port);
            transport.socket.Bind(localEP);

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint sendEP = (EndPoint)sender;
            transport.socket.BeginReceiveFrom(temp_buffer, 0, temp_buffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(ReceiveDataCB), sendEP);
            this.transport = transport;
        }    

        public void ReceiveDataCB(IAsyncResult asyncResult)
        {
            try
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint sendEP = (EndPoint)sender;
                int bytesRead = transport.socket.EndReceiveFrom(asyncResult, ref sendEP);
                string data = ASCIIEncoding.ASCII.GetString(temp_buffer, 0, bytesRead);
                string remote_host = ((IPEndPoint)sendEP).Address.ToString();
                string remote_port = ((IPEndPoint)sendEP).Port.ToString();
                //IPAddress address = ((IPEndPoint)sendEP).Address;
                if (this.Received_Data_Event != null)
                {
                    this.Received_Data_Event(this, new RawEventArgs(data,new string[] {remote_host,remote_port}));
                }
                //if (data.Contains("SIP/2.0"))
                //{
                //    Message message = new Message(data);
                //    process_Recv_Message(message);
                //}
                this.transport.socket.BeginReceiveFrom(this.temp_buffer, 0, this.temp_buffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(this.ReceiveDataCB), sendEP);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        private void SendDataCB(IAsyncResult asyncResult)
        {
            try
            {
                stack.transport.socket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                throw new NotImplementedException();
            }
        }

        public UserAgent createServer(Message request, SIPURI uri, SIPStack stack)
        {
            if (request.method == "INVITE")
            {
                return new UserAgent(this.stack,request);
            }
            else return null;
        }

        public void sending(UserAgent ua, Message message, SIPStack stack)
        {
            Console.WriteLine("App sending message:");
            Console.WriteLine("UserAgent: " + ua.ToString());
            Console.WriteLine("Message: " + message.ToString());
            //TODO: Allow App to modify message before it gets sent?;
            //throw new NotImplementedException();
        }
        public void cancelled(UserAgent ua, Message request, SIPStack stack)
        {
            throw new NotImplementedException();
        }
        public void dialogCreated(Dialog dialog, UserAgent ua, SIPStack stack)
        {
            throw new NotImplementedException();
        }
        public string[] authenticate(UserAgent ua, SIPStack stack)
        {
            return new string[] {"username","password"};
        }
        public Timer createTimer(SIPApp app, SIPStack stack)
        {
            return new Timer(app);
        }

        public string[] authenticate(UserAgent ua, Header header, SIPStack stack)
        {
            return new string[] { "alice@open-ims.test", "alice" }; // TODO FIX PROPERLY
        }

        public void receivedRequest(Transaction transaction, Message request, SIPStack stack)
        {
            Console.WriteLine("App Received Request:");
            Console.WriteLine("Transaction: " + transaction.ToString());
            Console.WriteLine("Request: " + request.ToString());
        }

        public void receivedResponse(Transaction transaction, Message response)
        {
            Console.WriteLine("App Received Response:");
            Console.WriteLine("Transaction: " + transaction.ToString());
            Console.WriteLine("Response: " + response.ToString());
            if (response.response_code == 401)
            {
                UserAgent ua = new UserAgent(this.stack, null, false);
                ua.authenticate(response, transaction);
            }

        }

        public void timeout(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public void error(Transaction transaction, string error)
        {
            throw new NotImplementedException();
        }

        public void receivedRequest(UserAgent ua, Message request, SIPStack stack)
        {
            Console.WriteLine("App Received Request:");
            Console.WriteLine("UserAgent: " + ua.ToString());
            Console.WriteLine("Request: " + request.ToString());
        }

        public void receivedResponse(UserAgent ua, Message response, SIPStack stack)
        {
            Console.WriteLine("App Received Response:");
            Console.WriteLine("UserAgent: " + ua.ToString());
            Console.WriteLine("Response: " + response.ToString());
        }

    }
}
