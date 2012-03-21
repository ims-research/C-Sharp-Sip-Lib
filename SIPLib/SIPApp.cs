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

        public void send(string data, string ip,int port,SIPStack stack)
        {
            IPAddress[] addresses = System.Net.Dns.GetHostAddresses(ip);
            IPEndPoint dest = new IPEndPoint(addresses[0], port);
            EndPoint destEP = (EndPoint)dest;
            byte[] send_data = ASCIIEncoding.ASCII.GetBytes(data);
            stack.transport.socket.BeginSendTo(send_data, 0, send_data.Length, SocketFlags.None, destEP, new AsyncCallback(this.SendDataCB), destEP);
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
