using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SIPLib.utils;
using log4net;

namespace SIPLib.SIP
{
    public class SIPStack
    {
        public string tag { get; set; }
        public TransportInfo Transport { get; set; }
        public SIPApp app { get; set; }
        private Random _random = new Random();
        public bool closing = false;
        public Dictionary<string, Dialog> dialogs { get; set; }
        public Dictionary<string, Transaction> transactions { get; set; }
        public string[] serverMethods = { "INVITE", "BYE", "MESSAGE", "SUBSCRIBE", "NOTIFY" };
        public string proxy_host { get; set; }
        public int proxy_port { get; set; }

        private SIPURI _uri = null;
        private List<Header> service_route { get; set; }
        private static ILog _log = LogManager.GetLogger(typeof(SIPStack));

        public SIPURI uri
        {
            get
            {
                return new SIPURI("sip" + ":" + Transport.host + ":" + Transport.port.ToString());
            }
            set
            {
                _uri = value;
            }

        }

        public SIPStack(SIPApp app)
        {
            Init();
            this.Transport = app.Transport;
            this.app = app;
            this.app.ReceivedDataEvent += new EventHandler<RawEventArgs>(Transport_Received_Data_Event);

            app.Stack = this;
        }

        void Transport_Received_Data_Event(object sender, RawEventArgs e)
        {
            try
            {
                this.Received(e.Data, e.Src);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, String.Format("Error receiving data",ex));
            }
        }

        ~SIPStack()  // destructor
        {
            this.closing = true;

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

            this.dialogs = new Dictionary<string, Dialog>();
            this.transactions = new Dictionary<string, Transaction>();

        }

        private void Init()
        {
            //this.tag = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
            this.tag = _random.Next(0, 2147483647).ToString();
            this.dialogs = new Dictionary<string, Dialog>();
            this.transactions = new Dictionary<string, Transaction>();
        }

        public string NewCallId()
        {
            return _random.Next(0, 2147483647).ToString() + "@" + (this.Transport.host);
        }

        public Header CreateVia()
        {
            return new Header("SIP/2.0/" + this.Transport.type.ToString().ToUpper() + " " + this.Transport.host + ':' + this.Transport.port.ToString() + ";rport", "Via");
        }

        public void Send(object data, object dest = null, TransportInfo transport = null)
        {
            //send(string data, string ip,int port,Stack stack)
            string destination_host = "";
            int destination_port = 0;
            string final_data;
            if (this.proxy_host != null && this.proxy_port != 0)
            {
                destination_host = proxy_host;
                destination_port = Convert.ToInt32(proxy_port);
            }
            else if (dest is SIPURI)
            {
                SIPURI destination = (SIPURI)dest;
                if (destination.host.Length == 0)
                {
                    Debug.Assert(false, String.Format("No host in destination URI \n{0}\n", destination.ToString()));
                }
                else
                {
                    destination_host = destination.host;
                    destination_port = destination.port;
                }

            }
            else if (dest is string)
            {
                string destination = (string)(dest);
                string[] parts = destination.Split(':');
                destination_host = parts[0];
                destination_port = Convert.ToInt32(parts[1]);
            }

            if (data is Message)
            {
                Message m = (Message)data;
                if (this.service_route != null)
                {
                    if (!(Utils.IsRequest(m) && (m.method.ToLower().Contains("register"))))
                    {
                        if (m.headers.ContainsKey("Route"))
                        {
                            bool found = false;
                            foreach (Header route_header in this.service_route)
                            {
                                foreach (Header message_header in m.headers["Route"])
                                {
                                    if (message_header.ToString().ToLower().CompareTo(route_header.ToString().ToLower()) == 0)
                                    {
                                        found = true;
                                    }
                                }
                                if (!found)
                                {
                                    m.headers["Route"].Add(route_header);
                                }
                            }
                        }
                        else
                        {
                            m.headers.Add("Route",this.service_route);
                        }
                    }
                }
                if (m.headers.ContainsKey("Route"))
                {
                    bool found = false;
                    foreach (Header h in m.headers["Route"])
                    {
                        if (h.ToString().Contains(proxy_host))
                        {
                            found = true;
                        }
                        
                    }
                    if (!found)
                    {
                        m.InsertHeader(new Header("<sip:" + proxy_host + ":" + proxy_port + ">", "Route"), "insert");
                    }
                    
                }
                else
                {
                    m.InsertHeader(new Header("<sip:"+proxy_host+":"+proxy_port+">", "Route"));
                }
                if (m.method != null && m.method.Length > 0)
                {
                    // TODO: Multicast handling of Maddr
                }
                else if (m.response_code > 0)
                {
                    if (dest == null)
                    {
                        destination_host = m.headers["Via"][0].ViaUri.host;
                        destination_port = m.headers["Via"][0].ViaUri.port;
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
                
                final_data = m.ToString();
            }
            else
            {
                final_data = (string)data;
            }
            this.app.Send(final_data, destination_host, destination_port, this);
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
                try
                {
                    Message m = new Message(data);
                    SIPURI uri = new SIPURI("sip" + ":" + src[0] + ":" + src[1]);
                    if (m.method != null)
                    {
                        if (!m.headers.ContainsKey("Via"))
                        {
                            Debug.Assert(false, String.Format("No Via header in request \n{0}\n", m.ToString()));
                        }
                        Header via = m.headers["Via"].First();
                        if (via.ViaUri.host != src[0] || !src[1].ToString().Equals(via.ViaUri.port))
                        {
                            via.Attributes.Add("received", src[0]);
                            via.ViaUri.host = src[0];
                        }
                        if (via.Attributes.ContainsKey("rport"))
                        {
                            via.Attributes["rport"] = src[1];
                        }
                        via.ViaUri.port = Convert.ToInt32(src[1]);
                        this.ReceivedRequest(m, uri);
                    }
                    else if (m.response_code > 0)
                    {
                        this.ReceivedResponse(m, uri);
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
            else
            {
                //Console.WriteLine("Error, null message received");
            }

        }

        private void ReceivedRequest(Message m, SIPURI uri)
        {
            string branch = m.headers["Via"][0].Attributes["branch"];
            Transaction t = null;
            if (m.method == "ACK" && branch == "0")
            {
                t = null;
            }
            else
            {
                t = this.FindTransaction(Transaction.CreateId(branch, m.method));
            }
            if (t == null)
            {
                UserAgent app = null; // Huh ?
                if ((m.method != "CANCEL") && (m.headers["To"][0].Attributes.ContainsKey("tag")))
                {
                    //In dialog request
                    Dialog d = this.FindDialog(m);
                    if (d == null)
                    {
                        if (m.method != "ACK")
                        {
                            //Updated from latest code TODO
                            UserAgent u = this.CreateServer(m, uri);
                            if (u != null)
                            {
                                app = u;
                            }
                            else
                            {
                                this.Send(Message.CreateResponse(481, "Dialog does not exist", null, null, m));
                                return;
                            }
                        }
                        else
                        {
                            if ((t == null) && (branch != "0"))
                            {
                                t = this.FindTransaction(Transaction.CreateId(branch, "INVITE"));
                            }
                            if (t != null && t.state != "terminated")
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
                        app = d.App;
                    }

                }
                else if (!(m.method == "CANCEL"))
                {
                    //Out of dialog request
                    UserAgent u = this.CreateServer(m, uri);
                    if (u != null)
                    {
                        //TODO error.....
                        app = u;
                    }
                    else if (m.method == "OPTIONS")
                    {
                        //Handle OPTIONS
                        Message reply = Message.CreateResponse(200, "OK", null, null, m);
                        reply.InsertHeader(new Header("INVITE,ACK,CANCEL,BYE,OPTION,MESSAGE", "Allow"));
                        this.Send(m);
                        return;
                    }
                    else if (m.method == "MESSAGE")
                    {
                        //Handle MESSAGE
                        UserAgent ua = new UserAgent(this);
                        ua.Request = m;

                        Message reply = ua.CreateResponse(200, "OK");
                        this.Send(reply);

                        this.app.ReceivedRequest(ua, m, this);
                        return;
                    }
                    else if (m.method != "ACK")
                    {
                        this.Send(Message.CreateResponse(405, "Method not allowed", null, null, m));
                        return;
                    }
                }
                else
                {
                    //Cancel Request
                    Transaction o = this.FindTransaction(Transaction.CreateId(m.headers["Via"][0].Attributes["branch"], "INVITE"));
                    if (o == null)
                    {
                        this.Send(Message.CreateResponse(481, "Original transaction does not exist", null, null, m));
                        return;
                    }
                    else
                    {
                        app = o.app;
                    }
                }
                if (app != null)
                {
                    t = Transaction.CreateServer(this, app, m, this.Transport, this.tag);
                }
                else if (m.method != "ACK")
                {
                    this.Send(Message.CreateResponse(404, "Not found", null, null, m));
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
            if (this.dialogs.ContainsKey(id))
            {
                return this.dialogs[id];
            }
            else return null;
        }

        public Transaction FindTransaction(string id)
        {
            if (this.transactions.ContainsKey(id))
            {
                return this.transactions[id];
            }
            else return null;
        }

        public Transaction FindOtherTransactions(Message r, Transaction orig)
        {
            foreach (Transaction t in this.transactions.Values)
            {
                if ((t != orig) && (Transaction.Equals(t, r, orig)))
                {
                    return t;
                }
            }
            return null;
        }
        public UserAgent CreateServer(Message request, SIPURI uri) { return this.app.CreateServer(request, uri, this); }
        public void Sending(UserAgent ua, Message message) { this.app.Sending(ua, message, this); }
        public void ReceivedRequest(UserAgent ua, Message request) { this.app.ReceivedRequest(ua, request, this); }
        public void ReceivedResponse(UserAgent ua, Message response) { this.app.ReceivedResponse(ua, response, this); }
        public void Cancelled(UserAgent ua, Message request) { this.app.Cancelled(ua, request, this); }
        public void DialogCreated(Dialog dialog, UserAgent ua) { this.app.DialogCreated(dialog, ua, this); }
        public string[] Authenticate(UserAgent ua, Header header) { return this.app.Authenticate(ua, header, this); }
        public Timer CreateTimer(UserAgent obj) { return this.app.CreateTimer(obj, this); }

        private void ReceivedResponse(Message r, SIPURI uri)
        {
            if (r.headers.ContainsKey("Service-Route") && r.Is2xx() && r.First("CSeq").Method.Contains("REGISTER"))
            {
                this.service_route = r.headers["Service-Route"];
                foreach (Header h in this.service_route)
                {
                    h.Name = "Route";
                }
            }
            else if (r.headers.ContainsKey("Record-Route") && r.Is2xx())
            {
                this.service_route = r.headers["Record-Route"];
                foreach (Header h in this.service_route)
                {
                    h.Name = "Route";
                }
            }


            if (!r.headers.ContainsKey("Via"))
            {
                Debug.Assert(false, String.Format("No Via header in received response \n{0}\n", r.ToString()));
                return;
            }
            string branch = r.headers["Via"][0].Attributes["branch"];
            string method = r.headers["CSeq"][0].Method;
            Transaction t = this.FindTransaction(Transaction.CreateId(branch, method));
            if (t == null)
            {
                if ((method == "INVITE") && (r.Is2xx()))
                {
                    Dialog d = this.FindDialog(r);
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
            return new Timer(transaction.app);
        }
    }
}
