using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using log4net;
using SIPLib;

namespace SIPLibDriver
{
    public class SIPApp : SIPLib.SIPApp
    {
        public override SIPStack stack { get; set; }
        private byte[] temp_buffer { get; set; }
        public override TransportInfo transport { get; set; }
        private UserAgent registerUA { get; set; }
        private UserAgent callUA { get; set; }
        public UserAgent messageUA { get; set; }
        public override event EventHandler<RawEventArgs> Received_Data_Event;
        public event EventHandler<RawEventArgs> Sent_Data_Event;

        private static ILog _log = LogManager.GetLogger(typeof(SIPApp));

        public SIPApp(TransportInfo transport)
        {
            log4net.Config.XmlConfigurator.Configure();
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

        public void Register(string uri)
        {
            this.registerUA = new UserAgent(this.stack, null, false);
            Message register_msg = this.registerUA.createRegister(new SIPURI(uri));
            register_msg.insertHeader(new Header("3600", "Expires"));
            this.registerUA.sendRequest(register_msg);
        }

        public void ReceiveDataCB(IAsyncResult asyncResult)
        {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint sendEP = (EndPoint)sender;
                int bytesRead = transport.socket.EndReceiveFrom(asyncResult, ref sendEP);
                string data = ASCIIEncoding.ASCII.GetString(temp_buffer, 0, bytesRead);
                string remote_host = ((IPEndPoint)sendEP).Address.ToString();
                string remote_port = ((IPEndPoint)sendEP).Port.ToString();
                if (this.Received_Data_Event != null)
                {
                    this.Received_Data_Event(this, new RawEventArgs(data, new string[] { remote_host, remote_port }));
                }
                this.transport.socket.BeginReceiveFrom(this.temp_buffer, 0, this.temp_buffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(this.ReceiveDataCB), sendEP);
        }

        public override void send(string data, string ip, int port, SIPStack stack)
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
                _log.Error("Error in sendDataCB", ex);
            }
        }

        public override UserAgent createServer(Message request, SIPURI uri, SIPStack stack)
        {
            if (request.method == "INVITE")
            {
                return new UserAgent(this.stack, request);
            }
            else return null;
        }

        public override void sending(UserAgent ua, Message message, SIPStack stack)
        {
            if (Utils.isRequest(message))
            {
                _log.Info("Sending request with method " + message.method);
            }
            else
            {
                _log.Info("Sending response with code " + message.response_code);
            }
            _log.Debug("\n\n" + message.ToString());
            //TODO: Allow App to modify message before it gets sent?;
        }

        public override void cancelled(UserAgent ua, Message request, SIPStack stack)
        {
            throw new NotImplementedException();
        }

        public override void dialogCreated(Dialog dialog, UserAgent ua, SIPStack stack)
        {
            this.callUA = dialog;
            _log.Info("New dialog created");
        }

        public override Timer createTimer(UserAgent app, SIPStack stack)
        {
            return new Timer(app);
        }

        public override string[] authenticate(UserAgent ua, Header header, SIPStack stack)
        {
            string username = "alice";
            string realm = "open-ims.test";
            string password = "alice";
            return new string[] { username + "@" + realm, password };
        }

        public override void receivedResponse(UserAgent ua, Message response, SIPStack stack)
        {
            _log.Info("Received response with code " + response.response_code + " " + response.response_text);
            _log.Debug("\n\n" + response.ToString());
            switch (response.response_code)
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
                        _log.Info("Response code of " + response.response_code + " is unhandled ");
                    }
                    break;
            }
        }

        public override void receivedRequest(UserAgent ua, Message request, SIPStack stack)
        {
            _log.Info("Received request with method " + request.method.ToUpper());
            _log.Debug("\n\n" + request.ToString());
            switch (request.method.ToUpper())
            {
                case "INVITE":
                    {
                        _log.Info("Generating 200 OK response for INVITE");
                        Message m = ua.createResponse(200, "OK");
                        ua.sendResponse(m);
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
                        _log.Info("MESSAGE: " + request.body);
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
                        _log.Info("Request with method " + request.method.ToUpper() + " is unhandled");
                        break;
                    }
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

        public void Message(string uri, string message)
        {
            uri = checkURI(uri);
            if (isRegistered())
            {
                this.messageUA = new UserAgent(this.stack);
                this.messageUA.localParty = this.registerUA.localParty;
                this.messageUA.remoteParty = new Address(uri);
                Message m = this.messageUA.createRequest("MESSAGE", message);
                m.insertHeader(new Header("text/plain", "Content-Type"));
                this.messageUA.sendRequest(m);
            }
        }

        public void endCurrentCall()
        {
            if (isRegistered())
            {
                if (this.callUA != null)
                {
                    try
                    {
                        Dialog d = (Dialog)this.callUA;
                        Message bye = d.createRequest("BYE");
                        d.sendRequest(bye);
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
            uri = checkURI(uri);
            if (isRegistered())
            {
                this.callUA = new UserAgent(this.stack, null, false);
                this.callUA.localParty = this.registerUA.localParty;
                this.callUA.remoteParty = new Address(uri);
                Message invite = this.callUA.createRequest("INVITE");
                this.callUA.sendRequest(invite);
            }
            else
            {
                _log.Error("isRegistered failed in invite message");
            }
        }

        private string checkURI(string uri)
        {
            if (!uri.Contains("<sip:") && !uri.Contains("sip:"))
            {
                uri = "<sip:" + uri + ">";
            }
            return uri;
        }

        private bool isRegistered()
        {
            if (this.registerUA == null || this.registerUA.localParty == null)
                return false;
            else return true;
        }
    }
}

