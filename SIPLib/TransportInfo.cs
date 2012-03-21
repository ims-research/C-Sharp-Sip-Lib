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
        private byte[] temp_buffer { get; set; }

        public event EventHandler<RawEventArgs> Received_Data_Event;
        public event EventHandler<RawEventArgs> Sent_Data_Event;

        public TransportInfo(IPAddress local_address, int ListenPort,ProtocolType type)
        {
            this.host = local_address;
            this.port = ListenPort;
            this.type = type;
            this.temp_buffer = new byte[4096];

            if (type == ProtocolType.Tcp)
            {
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            else
            {
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            IPEndPoint localEP = new IPEndPoint(local_address, ListenPort);
            this.socket.Bind(localEP);

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint sendEP = (EndPoint)sender;
            this.socket.BeginReceiveFrom(temp_buffer, 0, temp_buffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(ReceiveDataCB), sendEP);
        }

        public void ReceiveDataCB(IAsyncResult asyncResult)
        {
            try
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint sendEP = (EndPoint)sender;
                int bytesRead = this.socket.EndReceiveFrom(asyncResult, ref sendEP);
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
                this.socket.BeginReceiveFrom(this.temp_buffer, 0, this.temp_buffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(this.ReceiveDataCB), sendEP);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }
    }
}
