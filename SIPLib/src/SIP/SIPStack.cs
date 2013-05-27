// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="SIPStack.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIPLib.Utils;
using log4net;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This is the main SIP stack class. It is used to process all SIP messages.
    /// </summary>
    public class SIPStack
    {
        /// <summary>
        /// Private handle to logger object.
        /// </summary>
        private static ILog _log = LogManager.GetLogger(typeof (SIPStack));
        /// <summary>
        /// A random number generator.
        /// </summary>
        private readonly Random _random = new Random();
        /// <summary>
        /// Private string representing the user agent name for this stack. It will be appended to all sent messages if it exists.
        /// </summary>
        private readonly string _userAgentName;
        /// <summary>
        /// Variable indicating whether the stack is closing down.
        /// </summary>
        public bool Closing;
        /// <summary>
        /// The SIP methods that this stack can handle
        /// </summary>
        public string[] ServerMethods = {"INVITE", "BYE", "MESSAGE", "SUBSCRIBE", "NOTIFY"};
        /// <summary>
        /// Private variable to ignore first NOTIFY - hack to handle case where NOTIFY arrives before OK to subscription request.
        /// </summary>
        private Dictionary<string, int> _seenNotifys = new Dictionary<string, int>();
        /// <summary>
        /// The SIPURI representing the stack / user address.
        /// </summary>
        private SIPURI _uri;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SIPStack" /> class.
        /// </summary>
        /// <param name="app">The SIPApp that messages should be passed to.</param>
        /// <param name="userAgentName">Name of the user agent / stack identifier.</param>
        public SIPStack(SIPApp app, string userAgentName = "SIPLIB")
        {
            Init();
            Transport = app.Transport;
            App = app;
            App.ReceivedDataEvent += TransportReceivedDataEvent;
            Dialogs = new Dictionary<string, Dialog>();
            app.Stack = this;
            _userAgentName = userAgentName;
        }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>The tag.</value>
        public string Tag { get; set; }
        /// <summary>
        /// Gets or sets the transport (how messages are actually sent and received).
        /// </summary>
        /// <value>A TransportInfo object representing a TCP / UDP connection.</value>
        public TransportInfo Transport { get; set; }
        /// <summary>
        /// Gets or sets the application that will receive the SIP messages.
        /// </summary>
        /// <value>The SIPApp to pass messages to.</value>
        public SIPApp App { get; set; }

        /// <summary>
        /// Gets or sets the list of current dialogs.
        /// </summary>
        /// <value>A dictionary representing current dialogs.</value>
        public Dictionary<string, Dialog> Dialogs { get; set; }
        /// <summary>
        /// Gets or sets the list of current transactions.
        /// </summary>
        /// <value>A dictionary representing current transactions.</value>
        public Dictionary<string, Transaction> Transactions { get; set; }
        /// <summary>
        /// Gets or sets the proxy host.
        /// </summary>
        /// <value>The proxy host.</value>
        public string ProxyHost { get; set; }
        /// <summary>
        /// Gets or sets the proxy port.
        /// </summary>
        /// <value>The proxy port.</value>
        public int ProxyPort { get; set; }

        /// <summary>
        /// Gets or sets the service route (used for special routing of SIP requests - see rfc3608)
        /// </summary>
        /// <value>A list of SIP headers representing the indicated service route.</value>
        private List<Header> ServiceRoute { get; set; }


        /// <summary>
        /// Gets or sets the SIP URI. Always returns sip:ip:port based on the Transport information.
        /// </summary>
        /// <value>The URI.</value>
        public SIPURI Uri
        {
            get { return new SIPURI("sip" + ":" + Transport.Host + ":" + Transport.Port.ToString()); }
            set { _uri = value; }
        }

        /// <summary>
        /// Triggered on receipt of data
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="T:SIPLib.SIP.RawEventArgs" /> instance containing the event data.</param>
        private void TransportReceivedDataEvent(object sender, RawEventArgs e)
        {
            try
            {
                Received(e.Data, e.Src);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, String.Format("Error receiving data with exception {0}", ex));
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="T:SIPLib.SIP.SIPStack" /> class.
        /// </summary>
        ~SIPStack() // destructor
        {
            Closing = true;

            //foreach (Dialog d in this.dialogs.Values)
            //{
            //    d.close();
            //    // TODO: Check this ? d.del ?
            //}

            //foreach (Transaction t in this.transactions.Values)
            //{
            //    t.close();
            //    // ToDO: Check this? t.del ?
            //}

            Dialogs = new Dictionary<string, Dialog>();
            Transactions = new Dictionary<string, Transaction>();
        }

        /// <summary>
        /// Initialises the stack. Sets random Tag and creates private dictionaries.
        /// </summary>
        private void Init()
        {
            //this.tag = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
            Tag = _random.Next(0, 2147483647).ToString();
            Dialogs = new Dictionary<string, Dialog>();
            Transactions = new Dictionary<string, Transaction>();
        }

        /// <summary>
        /// Generates a new call ID.
        /// </summary>
        /// <returns>System.String.</returns>
        public string NewCallId()
        {
            return _random.Next(0, 2147483647).ToString() + "@" + (Transport.Host);
        }

        /// <summary>
        /// Helper function to create a VIA header for this stack.
        /// </summary>
        /// <returns>Header.</returns>
        public Header CreateVia()
        {
            return
                new Header(
                    "SIP/2.0/" + Transport.Type.ToString().ToUpper() + " " + Transport.Host + ':' +
                    Transport.Port.ToString() + ";rport", "Via");
        }

        /// <summary>
        /// Method to send SIP messages. Handles appending of necessary information and routing rules.
        /// </summary>
        /// <param name="data">The data to send (can be a SIPMessage or string representing a SIP message).</param>
        /// <param name="dest">The destination (can be a SIPURI or a string).</param>
        /// <param name="transport">Optional TransportInfo object.</param>
        public void Send(object data, object dest = null, TransportInfo transport = null)
        {
            string destinationHost = "";
            int destinationPort = 0;
            string finalData;
            if (ProxyHost != null && ProxyPort != 0)
            {
                destinationHost = ProxyHost;
                destinationPort = Convert.ToInt32(ProxyPort);
            }
            else if (dest is SIPURI)
            {
                SIPURI destination = (SIPURI) dest;
                if (destination.Host.Length == 0)
                {
                    Debug.Assert(false, String.Format("No host in destination URI \n{0}\n", destination));
                }
                else
                {
                    destinationHost = destination.Host;
                    destinationPort = destination.Port;
                }
            }
            else if (dest is string)
            {
                string destination = (string) (dest);
                string[] parts = destination.Split(':');
                destinationHost = parts[0];
                destinationPort = Convert.ToInt32(parts[1]);
            }

            if (data is Message)
            {
                Message m = (Message) data;
                //TODO: Fix stripping of record-route
                //if (m.Headers.ContainsKey("Record-Route")) m.Headers.Remove("Record-Route");
                //if (!Helpers.IsRequest(m) && m.Is2XX() && m.First("CSeq").Method.Contains("INVITE"))
                //{
                //    m.Headers.Remove("Record-Route");
                //}
                if (Helpers.IsRequest(m) && m.Method == "ACK")
                {
                    _log.Info("Sending ACK");
                }
                m.InsertHeader(new Header(_userAgentName, "User-Agent"));
                if (m.Headers.ContainsKey("Route"))
                {
                }
                else
                {
                    if (ServiceRoute != null)
                    {
                        if (
                            !(Helpers.IsRequest(m) &&
                              ((m.Method.ToLower().Contains("register") ||
                                (m.Method.ToLower().Contains("ack") || (m.Method.ToLower().Contains("bye")))))))
                        {
                            m.Headers["Route"] = ServiceRoute;
                        }
                    }
                    else if (!string.IsNullOrEmpty(ProxyHost))
                    {
                        m.InsertHeader(new Header("<sip:" + ProxyHost + ":" + ProxyPort + ">", "Route"));
                    }
                }
                if (!string.IsNullOrEmpty(m.Method))
                {
                    // TODO: Multicast handling of Maddr
                }
                else if (m.ResponseCode > 0)
                {
                    if (dest == null)
                    {
                        destinationHost = m.Headers["Via"][0].ViaUri.Host;
                        destinationPort = m.Headers["Via"][0].ViaUri.Port;
                    }
                }
                ////TODO FIX HACK
                //string route_list = "";
                //if (m.headers.ContainsKey("Route"))
                //{
                //foreach (Header h in m.headers["Route"])
                //{
                //    route_list = route_list + h.value.ToString() + ",";
                //}
                //route_list = route_list.Remove(route_list.Length - 1);
                //Header temp = new Header(route_list, "Temp");
                //temp.value = route_list;
                //temp.name = "Route";
                //m.headers.Remove("Route");
                //m.insertHeader(temp);
                //}

                finalData = m.ToString();
            }
            else
            {
                finalData = (string) data;
            }
            App.Send(finalData, destinationHost, destinationPort, this);
        }

        //private string mergeRoutes(string message_text)
        //{
        //    string route = "Route: ";
        //    StringBuilder sb = new StringBuilder();
        //    int index = -1;
        //    foreach (string line in message_text.Split(new string[] { "\r\n" }, StringSplitOptions.None))
        //    {
        //        if (line.StartsWith("Route:"))
        //        {
        //            route = route + line.Remove(0, 6) + ",";
        //        }
        //        else if (line.StartsWith("Content-Length"))
        //        {
        //            route = route.Remove(route.Length - 1);
        //            sb.Append(route + "\r\n");
        //            sb.Append(line + "\r\n");
        //            sb.Append("\r\n");
        //            index = message_text.IndexOf(line) + line.Length;
        //            break;
        //        }
        //        else
        //        {
        //            sb.Append(line + "\r\n");
        //        }
        //    }
        //    sb.Append(message_text.Substring(index));
        //    return sb.ToString();
        //}

        /// <summary>
        /// Processing of raw received data
        /// </summary>
        /// <param name="data">The received data.</param>
        /// <param name="src">The data source.</param>
        public void Received(string data, string[] src)
        {
            // Ignore empty messages sent by the openIMS core.
            if (data.Length > 2)
            {
                if (data.Contains("INVITE"))
                {
                    //Hook to log particular types of messages
                    _log.Debug(new Message(data));
                }
                try
                {
                    Message m = new Message(data);
                    SIPURI uri = new SIPURI("sip" + ":" + src[0] + ":" + src[1]);
                    if (m.Method != null)
                    {
                        if (!m.Headers.ContainsKey("Via"))
                        {
                            Debug.Assert(false, String.Format("No Via header in request \n{0}\n", m));
                        }
                        Header via = m.Headers["Via"].First();
                        if (via.ViaUri.Host != src[0] || !src[1].Equals(via.ViaUri.Port))
                        {
                            via.Attributes.Add("received", src[0]);
                            via.ViaUri.Host = src[0];
                        }
                        if (via.Attributes.ContainsKey("rport"))
                        {
                            via.Attributes["rport"] = src[1];
                        }
                        via.ViaUri.Port = Convert.ToInt32(src[1]);
                        ReceivedRequest(m, uri);
                    }
                    else if (m.ResponseCode > 0)
                    {
                        ReceivedResponse(m, uri);
                    }
                    else
                    {
                        Debug.Assert(false, String.Format("Received invalid message \n{0}\n", m));
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false,
                                 String.Format("Error in received message \n{0}\n with error message {1}", data,
                                               ex.Message));
                }
            }
        }

        /// <summary>
        /// Handle received request
        /// </summary>
        /// <param name="m">The received message.</param>
        /// <param name="uri">The SIPURI that sent the message.</param>
        private void ReceivedRequest(Message m, SIPURI uri)
        {
            string branch = m.Headers["Via"][0].Attributes["branch"];
            Transaction t;
            if (m.Method == "ACK")
            {
                if (branch == "0")
                {
                    t = null;
                }
                else
                {
                    t = FindTransaction(branch);
                    if (t == null || (t.LastResponse != null && t.LastResponse.Is2XX()))
                    {
                        t = FindTransaction(Transaction.CreateId(branch, m.Method));
                    }
                }
            }
            else
            {
                t = FindTransaction(Transaction.CreateId(branch, m.Method));
            }
            if (t == null)
            {
                UserAgent app = null; // Huh ?
                if ((m.Method != "CANCEL") && (m.Headers["To"][0].Attributes.ContainsKey("tag")))
                {
                    //In dialog request
                    Dialog d = FindDialog(m);
                    if (d == null)
                    {
                        if (m.Method != "ACK")
                        {
                            //Updated from latest code TODO
                            UserAgent u = CreateServer(m, uri);
                            if (u != null)
                            {
                                app = u;
                            }
                            else
                            {
                                // TODO: FIX NOTIFY ON SUBSCRIBE HANDLING
                                if (m.Method != "NOTIFY")
                                {
                                    Send(Message.CreateResponse(481, "Dialog does not exist", null, null, m));
                                    return;
                                }
                                else
                                {
                                    string branchID = m.Headers["Via"][0].Attributes["branch"];
                                    if (_seenNotifys.ContainsKey(branchID) && _seenNotifys[branchID] > 1)
                                    {
                                        Send(Message.CreateResponse(481, "Dialog does not exist", null, null, m));
                                        return;
                                    }
                                    else
                                    {
                                        if (_seenNotifys.ContainsKey(branchID))
                                        {
                                            _seenNotifys[branchID] = _seenNotifys[branchID] + 1;
                                        }
                                        else
                                        {
                                            _seenNotifys[branchID] = 1;
                                        }
                                    }
                                }
                                return;
                            }
                        }
                        else
                        {
                            _log.Info("No dialog for ACK, finding transaction");
                            if (t == null && branch != "0")
                            {
                                t = FindTransaction(Transaction.CreateId(branch, "INVITE"));
                            }
                            if (t != null && t.State != "terminated")
                            {
                                t.ReceivedRequest(m);
                                return;
                            }
                            else
                            {
                                _log.Info("No existing transaction for ACK \n");
                                UserAgent u = CreateServer(m, uri);
                                if (u != null)
                                {
                                    app = u;
                                }
                                else return;
                            }
                        }
                    }
                    else
                    {
                        app = d;
                    }
                }
                else if (m.Method != "CANCEL")
                {
                    //Out of dialog request
                    UserAgent u = CreateServer(m, uri);
                    if (u != null)
                    {
                        //TODO error.....
                        app = u;
                    }
                    else if (m.Method == "OPTIONS")
                    {
                        //Handle OPTIONS
                        Message reply = Message.CreateResponse(200, "OK", null, null, m);
                        reply.InsertHeader(new Header("INVITE,ACK,CANCEL,BYE,OPTION,MESSAGE,PUBLISH", "Allow"));
                        Send(m);
                        return;
                    }
                    else if (m.Method == "MESSAGE")
                    {
                        //Handle MESSAGE
                        UserAgent ua = new UserAgent(this) {Request = m};
                        /*Message reply = ua.CreateResponse(200, "OK");
                        Send(reply);*/
                        App.ReceivedRequest(ua, m, this);
                        return;
                    }
                    else if (m.Method == "PUBLISH")
                    {
                        UserAgent ua = new UserAgent(this) {Request = m};
                        App.ReceivedRequest(ua, m, this);
                    }
                    else if (m.Method != "ACK")
                    {
                        Send(Message.CreateResponse(405, "Method not allowed", null, null, m));
                        return;
                    }
                }
                else
                {
                    //Cancel Request
                    Transaction o =
                        FindTransaction(Transaction.CreateId(m.Headers["Via"][0].Attributes["branch"], "INVITE"));
                    if (o == null)
                    {
                        Send(Message.CreateResponse(481, "Original transaction does not exist", null, null, m));
                        return;
                    }
                    app = o.App;
                }
                if (app != null)
                {
                    //t = Transaction.CreateServer(app.Stack, app, app.Request, app.Stack.Transport, app.Stack.Tag);
                    // TODO: Check app or this ?
                    t = Transaction.CreateServer(this, app, m, Transport, Tag);
                }
                else if (m.Method != "ACK")
                {
                    Send(Message.CreateResponse(404, "Not found", null, null, m));
                }
            }
            else
            {
                t.ReceivedRequest(m);
            }
        }

        /// <summary>
        /// Finds the corresponding dialog by message or string
        /// </summary>
        /// <param name="m">A SIP message or string containing the dialog ID.</param>
        /// <returns>The matching dialog.</returns>
        private Dialog FindDialog(object m)
        {
            string id = "";
            if (m is Message)
            {
                id = Dialog.ExtractID((Message) m);
            }
            else
            {
                id = (string) (m);
            }
            if (Dialogs.ContainsKey(id))
            {
                return Dialogs[id];
            }
            return null;
        }

        /// <summary>
        /// Finds a transaction given an ID
        /// </summary>
        /// <param name="id">The id of the transaction to find..</param>
        /// <returns>The matched transaction.</returns>
        public Transaction FindTransaction(string id)
        {
            if (Transactions.ContainsKey(id))
            {
                return Transactions[id];
            }
            return null;
        }

        /// <summary>
        /// Finds any other transactions.
        /// </summary>
        /// <param name="r">The SIP message.</param>
        /// <param name="orig">The original transaction.</param>
        /// <returns>Another matching transaction.</returns>
        public Transaction FindOtherTransactions(Message r, Transaction orig)
        {
            foreach (Transaction t in Transactions.Values)
            {
                if ((t != orig) && (Transaction.TEquals(t, r, orig))) return t;
            }
            return null;
        }

        /// <summary>
        /// Creates the SIP server.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>UserAgent.</returns>
        public UserAgent CreateServer(Message request, SIPURI uri)
        {
            return App.CreateServer(request, uri, this);
        }

        /// <summary>
        /// Sends a particular message through the associated SIP application.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="message">The message.</param>
        public void Sending(UserAgent ua, Message message)
        {
            App.Sending(ua, message, this);
        }

        /// <summary>
        /// Passes a received request to the associated SIP application.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="request">The request.</param>
        public void ReceivedRequest(UserAgent ua, Message request)
        {
            App.ReceivedRequest(ua, request, this);
        }

        /// <summary>
        /// Passes a received response to the associated SIP application.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="response">The response.</param>
        public void ReceivedResponse(UserAgent ua, Message response)
        {
            App.ReceivedResponse(ua, response, this);
        }

        /// <summary>
        /// Passes the notification of a cancellation to the associated SIP application.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="request">The request.</param>
        public void Cancelled(UserAgent ua, Message request)
        {
            App.Cancelled(ua, request, this);
        }

        /// <summary>
        /// Notifies the associated SIP application that a dialog has been created.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="ua">The ua.</param>
        public void DialogCreated(Dialog dialog, UserAgent ua)
        {
            App.DialogCreated(dialog, ua, this);
        }

        /// <summary>
        /// Authenticates through the associated SIP application.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="header">The header.</param>
        /// <returns>System.String[][].</returns>
        public string[] Authenticate(UserAgent ua, Header header)
        {
            return App.Authenticate(ua, header, this);
        }

        /// <summary>
        /// Creates a timer on the associated SIP application.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>Timer.</returns>
        public Timer CreateTimer(UserAgent obj)
        {
            return App.CreateTimer(obj, this);
        }

        /// <summary>
        /// Handles the received response and passes it to the appropriate transaction or dialog for further handling.
        /// </summary>
        /// <param name="r">The received response.</param>
        /// <param name="uri">The SIP URI.</param>
        private void ReceivedResponse(Message r, SIPURI uri)
        {
            if (r.Headers.ContainsKey("Service-Route") && r.Is2XX() && r.First("CSeq").Method.Contains("REGISTER"))
            {
                ServiceRoute = r.Headers["Service-Route"];
                foreach (Header h in ServiceRoute)
                {
                    h.Name = "Route";
                }
            }
            else if (r.Headers.ContainsKey("Record-Route") && r.Is2XX())
            {
                // TODO: FIX This ? don't need to keep building record-route ?
                //InviteRecordRoute = r.Headers["Record-Route"];
                //foreach (Header h in InviteRecordRoute)
                //{
                //    h.Name = "Route";
                //}
            }


            if (!r.Headers.ContainsKey("Via"))
            {
                Debug.Assert(false, String.Format("No Via header in received response \n{0}\n", r));
                return;
            }
            string branch = r.Headers["Via"][0].Attributes["branch"];
            string method = r.Headers["CSeq"][0].Method;
            Transaction t = FindTransaction(Transaction.CreateId(branch, method));
            if (t == null)
            {
                if ((method == "INVITE") && (r.Is2XX()))
                {
                    _log.Debug("Looking for dialog with ID " + Dialog.ExtractID(r));
                    foreach (KeyValuePair<string, Dialog> keyValuePair in Dialogs)
                    {
                        _log.Debug("Current Dialogs " + keyValuePair.Key);
                    }
                    Dialog d = FindDialog(r);
                    if (d == null)
                    {
                        Debug.Assert(false, String.Format("No transaction or dialog for 2xx of INVITE \n{0}\n", r));
                        return;
                    }
                    else
                    {
                        d.ReceivedResponse(null, r);
                    }
                }
                else
                {
                    Console.WriteLine("No Transaction for response...ignoring....");
                    //Debug.Assert(false, String.Format("No Transaction for response \n{0}\n", r.ToString()));
                    return;
                }
            }
            else
            {
                t.ReceivedResponse(r);
                return;
            }
        }

        /// <summary>
        /// Creates the timer.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <returns>Timer.</returns>
        internal Timer CreateTimer(Transaction transaction)
        {
            //TODO implement Timers;
            return new Timer(transaction.App);
        }
    }
}