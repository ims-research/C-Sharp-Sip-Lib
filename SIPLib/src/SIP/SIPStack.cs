using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SIPLib.Utils;
using log4net;

namespace SIPLib.SIP
{
    public class SIPStack
    {
        public string Tag { get; set; }
        public TransportInfo Transport { get; set; }
        public SIPApp App { get; set; }
        private readonly Random _random = new Random();
        public bool Closing;
        public Dictionary<string, Dialog> Dialogs { get; set; }
        public Dictionary<string, Transaction> Transactions { get; set; }
        public string[] ServerMethods = { "INVITE", "BYE", "MESSAGE", "SUBSCRIBE", "NOTIFY" };
        public string ProxyHost { get; set; }
        public int ProxyPort { get; set; }

        private SIPURI _uri = null;
        private List<Header> ServiceRoute { get; set; }
        

        private static ILog _log = LogManager.GetLogger(typeof(SIPStack));

        private Dictionary<string,int> _seenNotifys = new Dictionary<string, int>();

        public SIPURI Uri
        {
            get
            {
                return new SIPURI("sip" + ":" + Transport.Host + ":" + Transport.Port.ToString());
            }
            set
            {
                _uri = value;
            }

        }

        public SIPStack(SIPApp app)
        {
            Init();
            Transport = app.Transport;
            App = app;
            App.ReceivedDataEvent += TransportReceivedDataEvent;
            Dialogs = new Dictionary<string, Dialog>();
            app.Stack = this;
        }

        void TransportReceivedDataEvent(object sender, RawEventArgs e)
        {
            try
            {
                Received(e.Data, e.Src);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, String.Format("Error receiving data with exception {0}",ex));
            }
        }

        ~SIPStack()  // destructor
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

        private void Init()
        {
            //this.tag = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
            Tag = _random.Next(0, 2147483647).ToString();
            Dialogs = new Dictionary<string, Dialog>();
            Transactions = new Dictionary<string, Transaction>();
        }

        public string NewCallId()
        {
            return _random.Next(0, 2147483647).ToString() + "@" + (this.Transport.Host);
        }

        public Header CreateVia()
        {
            return new Header("SIP/2.0/" + Transport.Type.ToString().ToUpper() + " " + Transport.Host + ':' + Transport.Port.ToString() + ";rport", "Via");
        }

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
                SIPURI destination = (SIPURI)dest;
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
                string destination = (string)(dest);
                string[] parts = destination.Split(':');
                destinationHost = parts[0];
                destinationPort = Convert.ToInt32(parts[1]);
            }

            if (data is Message)
            {
                Message m = (Message)data;
                //TODO: Fix stripping of record-route
                if (m.Headers.ContainsKey("Record-Route")) m.Headers.Remove("Record-Route");
                //if (!Helpers.IsRequest(m) && m.Is2XX() && m.First("CSeq").Method.Contains("INVITE"))
                //{
                //    m.Headers.Remove("Record-Route");
                //}
                if (Utils.Helpers.IsRequest(m) && m.Method == "ACK")
                {
                    _log.Info("Sending ACK");
                }
                m.InsertHeader(new Header("SIPLIB","User-Agent"));
                if (m.Headers.ContainsKey("Route"))
                {

                }
                else
                {
                    if (ServiceRoute != null)
                    {
                        if (!(Utils.Helpers.IsRequest(m) && (m.Method.ToLower().Contains("register"))))
                        {
                            m.Headers["Route"] = ServiceRoute;
                        }
                    }
                    else
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
                finalData = (string)data;
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

        public void Received(string data, string[] src)
        {
            if (data.Length > 2)
            {
                if (data.Contains("INVITE") && data.Contains("CSeq:  1 INVITE"))
                {
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
                            Debug.Assert(false, String.Format("No Via header in request \n{0}\n", m.ToString()));
                        }
                        Header via = m.Headers["Via"].First();
                        if (via.ViaUri.Host != src[0] || !src[1].ToString().Equals(via.ViaUri.Port))
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
                        Debug.Assert(false, String.Format("Received invalid message \n{0}\n", m.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, String.Format("Error in received message \n{0}\n with error message {1}", data, ex.Message));
                }
            }
        }

        private void ReceivedRequest(Message m, SIPURI uri)
        {
            string branch = m.Headers["Via"][0].Attributes["branch"];
            Transaction t;
            if (m.Method == "ACK" && branch == "0")
            {
                t = null;
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
                            UserAgent u = this.CreateServer(m, uri);
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
                            if ((branch != "0"))
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
                                Debug.Assert(false, String.Format("No existing transaction for ACK \n{0}\n", m.ToString()));
                                UserAgent u = this.CreateServer(m, uri);
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
                    UserAgent u = this.CreateServer(m, uri);
                    if (u != null)
                    {
                        //TODO error.....
                        app = u;
                    }
                    else if (m.Method == "OPTIONS")
                    {
                        //Handle OPTIONS
                        Message reply = Message.CreateResponse(200, "OK", null, null, m);
                        reply.InsertHeader(new Header("INVITE,ACK,CANCEL,BYE,OPTION,MESSAGE", "Allow"));
                        Send(m);
                        return;
                    }
                    else if (m.Method == "MESSAGE")
                    {
                        //Handle MESSAGE
                        UserAgent ua = new UserAgent(this) {Request = m};

                        Message reply = ua.CreateResponse(200, "OK");
                        Send(reply);

                        App.ReceivedRequest(ua, m, this);
                        return;
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
                    Transaction o = FindTransaction(Transaction.CreateId(m.Headers["Via"][0].Attributes["branch"], "INVITE"));
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

        private Dialog FindDialog(object m)
        {
            string id = "";
            if (m is Message)
            {
                id = Dialog.ExtractID((Message)m);
            }
            else
            {
                id = (string)(m);
            }
            if (Dialogs.ContainsKey(id))
            {
                return Dialogs[id];
            }
            return null;
        }

        public Transaction FindTransaction(string id)
        {
            if (Transactions.ContainsKey(id))
            {
                return Transactions[id];
            }
            return null;
        }

        public Transaction FindOtherTransactions(Message r, Transaction orig)
        {
            return Transactions.Values.FirstOrDefault(t => (t != orig) && (Transaction.TEquals(t, r, orig)));
        }

        public UserAgent CreateServer(Message request, SIPURI uri) { return App.CreateServer(request, uri, this); }
        public void Sending(UserAgent ua, Message message) { App.Sending(ua, message, this); }
        public void ReceivedRequest(UserAgent ua, Message request) { App.ReceivedRequest(ua, request, this); }
        public void ReceivedResponse(UserAgent ua, Message response) { App.ReceivedResponse(ua, response, this); }
        public void Cancelled(UserAgent ua, Message request) { App.Cancelled(ua, request, this); }
        public void DialogCreated(Dialog dialog, UserAgent ua) { App.DialogCreated(dialog, ua, this); }
        public string[] Authenticate(UserAgent ua, Header header) { return App.Authenticate(ua, header, this); }
        public Timer CreateTimer(UserAgent obj) { return App.CreateTimer(obj, this); }

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
                Debug.Assert(false, String.Format("No Via header in received response \n{0}\n", r.ToString()));
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
                        Debug.Assert(false, String.Format("No transaction or dialog for 2xx of INVITE \n{0}\n", r.ToString()));
                        return;
                    }
                    else
                    {
                        d.ReceivedResponse(null, r);
                    }
                }
                else
                {
                    Debug.Assert(false, String.Format("No Transaction for response \n{0}\n", r.ToString()));
                }
            }
            else
            {
                t.ReceivedResponse(r);
                return;
            }
        }

        internal Timer CreateTimer(Transaction transaction)
        {
            //TODO implement Timers;
            return new Timer(transaction.App);
        }
    }
}
