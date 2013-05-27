// ***********************************************************************
// Assembly         : SIPLibDriver
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 10-25-2012
// ***********************************************************************
// <copyright file="SIPApp.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SIPLib.SIP;
using SIPLib.Utils;
using log4net;

namespace SIPLibDriver
{
    /// <summary>
    /// Example implementation of SIP handling logic. See ReceivedResponse, ReceivedRequest, Authenticate, Register, Invite, Message and EndCurrentCall for starting points.
    /// </summary>
    [System.Runtime.InteropServices.GuidAttribute("10020A2C-6688-4454-8087-2201591C2192")]
    public class SIPApp : SIPLib.SIPApp
    {
        /// <summary>
        /// Gets or sets the SIP stack.
        /// </summary>
        /// <value>The stack.</value>
        public override SIPStack Stack { get; set; }
        /// <summary>
        /// Gets or sets the temp buffer, used for receiving SIP messages
        /// </summary>
        /// <value>The temp buffer.</value>
        private byte[] TempBuffer { get; set; }
        /// <summary>
        /// Gets or sets the transport.
        /// </summary>
        /// <value>The transport.</value>
        public override TransportInfo Transport { get; set; }
        /// <summary>
        /// Gets or sets the user agent that is used for registration.
        /// </summary>
        /// <value>The register UA.</value>
        private UserAgent RegisterUA { get; set; }
        /// <summary>
        /// Gets or sets the UA that is used for calls (this should actually be a list of useragents, one per call).
        /// </summary>
        /// <value>The call UA.</value>
        private UserAgent CallUA { get; set; }
        /// <summary>
        /// Gets or sets the UA that is used for messaging (this is a simplified example to keep tracking UAs easier).
        /// </summary>
        /// <value>The message UA.</value>
        public UserAgent MessageUA { get; set; }
        /// <summary>
        /// Occurs when [there is data received].
        /// </summary>
        public override event EventHandler<RawEventArgs> ReceivedDataEvent;
        /// <summary>
        /// Occurs when [data has been sent].
        /// </summary>
        public event EventHandler<RawEventArgs> Sent_Data_Event;

        /// <summary>
        /// Private variable for logging
        /// </summary>
        private static ILog _log = LogManager.GetLogger(typeof(SIPApp));

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLibDriver.SIPApp"/> class.
        /// </summary>
        /// <param name="transport">Takes in the specified trasnport object and creates the necessary sockets.</param>
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

        /// <summary>
        /// Registers the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void Register(string uri)
        {
            this.RegisterUA = new UserAgent(this.Stack, null, false);
            Message register_msg = this.RegisterUA.CreateRegister(new SIPURI(uri));
            register_msg.InsertHeader(new Header("3600", "Expires"));
            this.RegisterUA.SendRequest(register_msg);
        }

        /// <summary>
        /// Call back to handle receiving data.
        /// </summary>
        /// <param name="asyncResult">The async result to use to end the receive</param>
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
                    this.ReceivedDataEvent(this, new RawEventArgs(data, new string[] { remote_host, remote_port },false));
                }
                this.Transport.Socket.BeginReceiveFrom(this.TempBuffer, 0, this.TempBuffer.Length, SocketFlags.None, ref sendEP, new AsyncCallback(this.ReceiveDataCB), sendEP);
        }

        /// <summary>
        /// Sends the data to the destination host and destination port
        /// </summary>
        /// <param name="finalData">The final data.</param>
        /// <param name="destinationHost">The destination host.</param>
        /// <param name="destinationPort">The destination port.</param>
        /// <param name="stack">The stack.</param>
        public override void Send(string finalData, string destinationHost, int destinationPort, SIPStack stack)
        {
            IPAddress[] addresses = System.Net.Dns.GetHostAddresses(destinationHost);
            IPEndPoint dest = new IPEndPoint(addresses[0], destinationPort);
            EndPoint destEP = (EndPoint)dest;
            byte[] send_data = ASCIIEncoding.ASCII.GetBytes(finalData);
            stack.Transport.Socket.BeginSendTo(send_data, 0, send_data.Length, SocketFlags.None, destEP, new AsyncCallback(this.SendDataCB), destEP);
        }

        /// <summary>
        /// Call back for sending data
        /// </summary>
        /// <param name="asyncResult">The async result used to end the socket sending.</param>
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

        /// <summary>
        /// Creates a user agent server on receipt of a request
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="stack">The stack.</param>
        /// <returns>UserAgent.</returns>
        public override UserAgent CreateServer(Message request, SIPURI uri, SIPStack stack)
        {
            if (request.Method == "INVITE")
            {
                return new UserAgent(this.Stack, request);
            }
            else return null;
        }

        /// <summary>
        /// Allows logging of message sending.
        /// </summary>
        /// <param name="ua">The user agent that is sending the message.</param>
        /// <param name="message">The SIP message being sent.</param>
        /// <param name="stack">The stack sending the message.</param>
        public override void Sending(UserAgent ua, Message message, SIPStack stack)
        {
            if (Helpers.IsRequest(message))
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

        /// <summary>
        /// Alert on cancelation of a call. Not implemented.
        /// </summary>
        /// <param name="ua">The useragent.</param>
        /// <param name="request">The request.</param>
        /// <param name="stack">The stack.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Cancelled(UserAgent ua, Message request, SIPStack stack)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allows logging / alerting on the creation of a dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="ua">The ua.</param>
        /// <param name="stack">The stack.</param>
        public override void DialogCreated(Dialog dialog, UserAgent ua, SIPStack stack)
        {
            this.CallUA = dialog;
            _log.Info("New dialog created");
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="stack">The stack.</param>
        /// <returns>Timer.</returns>
        public override Timer CreateTimer(UserAgent app, SIPStack stack)
        {
            return new Timer(app);
        }

        /// <summary>
        /// Authenticates the specified ua, using the specified username and password.
        /// Your own method to retrieve the users username and password should be placed in this method.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="header">The header.</param>
        /// <param name="stack">The stack.</param>
        /// <returns>System.String[].</returns>
        public override string[] Authenticate(UserAgent ua, Header header, SIPStack stack)
        {
            string username = "alice";
            string realm = "open-ims.test";
            string password = "alice";
            return new string[] { username + "@" + realm, password };
        }

        /// <summary>
        /// Function used to determine how to handle a received response.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="response">The response.</param>
        /// <param name="stack">The stack.</param>
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

        /// <summary>
        /// Function used to determine how to handle a received request.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="request">The request.</param>
        /// <param name="stack">The stack.</param>
        public override void ReceivedRequest(UserAgent ua, Message request, SIPStack stack)
        {
            _log.Info("Received request with method " + request.Method.ToUpper());
            _log.Debug("\n\n" + request.ToString());
            switch (request.Method.ToUpper())
            {
                case "INVITE":
                    {
                        // Auto accepts any SIP INVITE request
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
                        // Logs any received request
                        _log.Info("MESSAGE: " + request.Body);
                        
                        // Can also echo back any received message for testing purposes
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


        /// <summary>
        /// Timeouts the specified transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Timeout(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Errors the specified transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="error">The error.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Error(Transaction transaction, string error)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends a message to a particular URI
        /// </summary>
        /// <param name="uri">The destination URI.</param>
        /// <param name="message">The message.</param>
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

        /// <summary>
        /// Ends the current call.
        /// </summary>
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

        /// <summary>
        /// Sends a SIP INVITE request to the specified URI (i.e. make a call)
        /// </summary>
        /// <param name="uri">The URI.</param>
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

        /// <summary>
        /// Checks the URI for angled brackets and scheme
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>System.String.</returns>
        private string CheckURI(string uri)
        {
            if (!uri.Contains("<sip:") && !uri.Contains("sip:"))
            {
                uri = "<sip:" + uri + ">";
            }
            return uri;
        }

        /// <summary>
        /// Determines whether this instance is registered.
        /// </summary>
        /// <returns><c>true</c> if this instance is registered; otherwise, <c>false</c>.</returns>
        private bool IsRegistered()
        {
            if (this.RegisterUA == null || this.RegisterUA.LocalParty == null)
                return false;
            else return true;
        }
    }
}

