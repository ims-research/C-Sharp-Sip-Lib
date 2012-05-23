using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SIPLib.SIP;
using SIPLib.utils;
using log4net;
using SIPLib;

namespace SIPLibDriver
{
    public class SIPApp : SIPLib.SIPApp
    {
        public override SIPStack Stack { get; set; }
        private byte[] TempBuffer { get; set; }
        public override TransportInfo Transport { get; set; }
        private UserAgent RegisterUA { get; set; }
        private UserAgent CallUA { get; set; }
        public UserAgent MessageUA { get; set; }
        public override event EventHandler<RawEventArgs> ReceivedDataEvent;
        public event EventHandler<RawEventArgs> Sent_Data_Event;

        private static ILog _log = LogManager.GetLogger(typeof(SIPApp));

        public SIPApp(TransportInfo transport)
        {
            log4net.Config.XmlConfigurator.Configure();
            this.TempBuffer = new byte[4096];
            if (transport.Type == ProtocolType.Tcp)
            {
                transport.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            else
            {
                transport.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            IPEndPoint localEP = new IPEndPoint(transport.Host, transport.Port);
            transport.Socket.Bind(localEP);

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint sendEP = (EndPoint)sender;
            transport.Socket.BeginReceiveFrom(TempBuffer, 0, TempBuffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(ReceiveDataCB), sendEP);
            this.Transport = transport;
        }

        public void Register(string uri)
        {
            this.RegisterUA = new UserAgent(this.Stack, null, false);
            Message register_msg = this.RegisterUA.CreateRegister(new SIPURI(uri));
            register_msg.InsertHeader(new Header("3600", "Expires"));
            this.RegisterUA.SendRequest(register_msg);
        }

        public void ReceiveDataCB(IAsyncResult asyncResult)
        {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint sendEP = (EndPoint)sender;
                int bytesRead = Transport.Socket.EndReceiveFrom(asyncResult, ref sendEP);
                string data = ASCIIEncoding.ASCII.GetString(TempBuffer, 0, bytesRead);
                string remote_host = ((IPEndPoint)sendEP).Address.ToString();
                string remote_port = ((IPEndPoint)sendEP).Port.ToString();
                if (this.ReceivedDataEvent != null)
                {
                    this.ReceivedDataEvent(this, new RawEventArgs(data, new string[] { remote_host, remote_port }));
                }
                this.Transport.Socket.BeginReceiveFrom(this.TempBuffer, 0, this.TempBuffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(this.ReceiveDataCB), sendEP);
        }

        public override void Send(string finalData, string destinationHost, int destinationPort, SIPStack stack)
        {
            IPAddress[] addresses = System.Net.Dns.GetHostAddresses(destinationHost);
            IPEndPoint dest = new IPEndPoint(addresses[0], destinationPort);
            EndPoint destEP = (EndPoint)dest;
            byte[] send_data = ASCIIEncoding.ASCII.GetBytes(finalData);
            stack.Transport.Socket.BeginSendTo(send_data, 0, send_data.Length, SocketFlags.None, destEP, new AsyncCallback(this.SendDataCB), destEP);
        }

        private void SendDataCB(IAsyncResult asyncResult)
        {
            try
            {
                Stack.Transport.Socket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                _log.Error("Error in sendDataCB", ex);
            }
        }

        public override UserAgent CreateServer(Message request, SIPURI uri, SIPStack stack)
        {
            if (request.Method == "INVITE")
            {
                return new UserAgent(this.Stack, request);
            }
            else return null;
        }

        public override void Sending(UserAgent ua, Message message, SIPStack stack)
        {
            if (Utils.IsRequest(message))
            {
                _log.Info("Sending request with method " + message.Method);
            }
            else
            {
                _log.Info("Sending response with code " + message.ResponseCode);
            }
            _log.Debug("\n\n" + message.ToString());
            //TODO: Allow App to modify message before it gets sent?;
        }

        public override void Cancelled(UserAgent ua, Message request, SIPStack stack)
        {
            throw new NotImplementedException();
        }

        public override void DialogCreated(Dialog dialog, UserAgent ua, SIPStack stack)
        {
            this.CallUA = dialog;
            _log.Info("New dialog created");
        }

        public override Timer CreateTimer(UserAgent app, SIPStack stack)
        {
            return new Timer(app);
        }

        public override string[] Authenticate(UserAgent ua, Header header, SIPStack stack)
        {
            string username = "alice";
            string realm = "open-ims.test";
            string password = "alice";
            return new string[] { username + "@" + realm, password };
        }

        public override void ReceivedResponse(UserAgent ua, Message response, SIPStack stack)
        {
            _log.Info("Received response with code " + response.ResponseCode + " " + response.ResponseText);
            _log.Debug("\n\n" + response.ToString());
            switch (response.ResponseCode)
            {
                case 180:
                    {

                        break;
                    }
                case 200:
                    {

                        break;
                    }
                case 401:
                    {
                        _log.Error("Transaction layer did not handle registration - APP received  401");
                        //UserAgent ua = new UserAgent(this.stack, null, false);
                        //ua.authenticate(response, transaction);
                        break;
                    }
                default:
                    {
                        _log.Info("Response code of " + response.ResponseCode + " is unhandled ");
                    }
                    break;
            }
        }

        public override void ReceivedRequest(UserAgent ua, Message request, SIPStack stack)
        {
            _log.Info("Received request with method " + request.Method.ToUpper());
            _log.Debug("\n\n" + request.ToString());
            switch (request.Method.ToUpper())
            {
                case "INVITE":
                    {
                        _log.Info("Generating 200 OK response for INVITE");
                        Message m = ua.CreateResponse(200, "OK");
                        ua.SendResponse(m);
                        break;
                    }
                case "CANCEL":
                    {
                        break;
                    }
                case "ACK":
                    {
                        break;
                    }
                case "BYE":
                    {
                        break;
                    }
                case "MESSAGE":
                    {
                        _log.Info("MESSAGE: " + request.Body);
                        //Address from = (Address) request.first("From").value;
                        //this.Message(from.uri.ToString(), request.body);
                        break;
                    }
                case "OPTIONS":
                case "REFER":
                case "SUBSCRIBE":
                case "NOTIFY":
                case "PUBLISH":
                case "INFO":
                default:
                    {
                        _log.Info("Request with method " + request.Method.ToUpper() + " is unhandled");
                        break;
                    }
            }
        }


        public void Timeout(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public void Error(Transaction transaction, string error)
        {
            throw new NotImplementedException();
        }

        public void Message(string uri, string message)
        {
            uri = CheckURI(uri);
            if (IsRegistered())
            {
                this.MessageUA = new UserAgent(this.Stack);
                this.MessageUA.LocalParty = this.RegisterUA.LocalParty;
                this.MessageUA.RemoteParty = new Address(uri);
                Message m = this.MessageUA.CreateRequest("MESSAGE", message);
                m.InsertHeader(new Header("text/plain", "Content-Type"));
                this.MessageUA.SendRequest(m);
            }
        }

        public void EndCurrentCall()
        {
            if (IsRegistered())
            {
                if (this.CallUA != null)
                {
                    try
                    {
                        Dialog d = (Dialog)this.CallUA;
                        Message bye = d.CreateRequest("BYE");
                        d.SendRequest(bye);
                    }
                    catch (InvalidCastException E)
                    {
                        _log.Error("Error ending current call, Dialog Does not Exist ?",E);
                    }
                    
                }
                else
                {
                    _log.Error("Call UA does not exist, not sending CANCEL message");
                }

            }
            else
            {
                _log.Error("Not registered, not sending CANCEL message");
            }

        }

        public void Invite(string uri)
        {
            uri = CheckURI(uri);
            if (IsRegistered())
            {
                this.CallUA = new UserAgent(this.Stack, null, false);
                this.CallUA.LocalParty = this.RegisterUA.LocalParty;
                this.CallUA.RemoteParty = new Address(uri);
                Message invite = this.CallUA.CreateRequest("INVITE");
                this.CallUA.SendRequest(invite);
            }
            else
            {
                _log.Error("isRegistered failed in invite message");
            }
        }

        private string CheckURI(string uri)
        {
            if (!uri.Contains("<sip:") && !uri.Contains("sip:"))
            {
                uri = "<sip:" + uri + ">";
            }
            return uri;
        }

        private bool IsRegistered()
        {
            if (this.RegisterUA == null || this.RegisterUA.LocalParty == null)
                return false;
            else return true;
        }
    }
}

