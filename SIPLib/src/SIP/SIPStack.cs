using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using log4net;
namespace SIPLib
{
    public class SIPStack
    {
        public string tag { get; set; }
        public TransportInfo transport { get; set; }
        public SIPApp app { get; set; }
        private Random random = new Random();
        public bool closing = false;
        public Dictionary<string, Dialog> dialogs { get; set; }
        public Dictionary<string, Transaction> transactions { get; set; }
        public string[] serverMethods = { "INVITE", "BYE", "MESSAGE", "SUBSCRIBE", "NOTIFY" };
        public string proxy_ip { get; set; }
        public int proxy_port { get; set; }
        private SIPURI _uri = null;
        private List<Header> service_route { get; set; }
        private static ILog _log = LogManager.GetLogger(typeof(SIPStack));

        public SIPURI uri
        {
            get
            {
                return new SIPURI("sip" + ":" + transport.host + ":" + transport.port.ToString());
            }
            set
            {
                _uri = value;
            }

        }

        public SIPStack(SIPApp app)
        {
            Init();
            this.transport = app.transport;
            this.app = app;
            this.app.Received_Data_Event += new EventHandler<RawEventArgs>(transport_Received_Data_Event);

            app.stack = this;
        }

        void transport_Received_Data_Event(object sender, RawEventArgs e)
        {
            this.received(e.data,e.src);
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
            this.tag = random.Next(0, 2147483647).ToString();
            this.dialogs = new Dictionary<string, Dialog>();
            this.transactions = new Dictionary<string, Transaction>();
        }

        public string newCallId()
        {
            return random.Next(0, 2147483647).ToString() + "@" + (this.transport.host);
        }

        public Header createVia()
        {
            return new Header("SIP/2.0/" + this.transport.type.ToString().ToUpper() + " " + this.transport.host + ':' + this.transport.port.ToString() + ";rport", "Via");
        }

        public void send(object data, object dest = null, TransportInfo transport = null)
        {
            //send(string data, string ip,int port,Stack stack)
            string destination_host = "";
            int destination_port = 0;
            string final_data;
            if (this.proxy_ip != null && this.proxy_port != null)
            {
                destination_host = proxy_ip;
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
                    if (!(Utils.isRequest(m) && (m.method.ToLower().Contains("register")||m.method.ToLower().Contains("ack"))))
                    {
                        if (m.headers.ContainsKey("Route"))
                        {
                            bool found = false;
                            foreach (Header message_header in m.headers["Route"])
                            {
                                foreach (Header route_header in this.service_route)
                                {
                                    if (message_header.ToString().ToLower().CompareTo(route_header.ToString().ToLower()) == 0)
                                    {
                                        found = true;
                                    }
                                }
                            }
                            if (!found)
                            {
                                m.headers["Route"].AddRange(this.service_route);
                            }
                        }
                        else
                        {
                            m.headers.Add("Route", this.service_route);
                        }
                    }
                }
                if (m.method != null && m.method.Length > 0)
                {
                    // TODO: Multicast handling of Maddr
                }
                else if (m.response_code > 0)
                {
                    if (dest == null)
                    {
                        destination_host = m.headers["Via"][0].viaUri.host;
                        destination_port = m.headers["Via"][0].viaUri.port;
                    }
                }
                final_data = m.ToString();
            }
            else
            {
                final_data = (string)data;
            }
            this.app.send(final_data, destination_host, destination_port, this);
        }

        public void received(string data, string[] src)
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
                        if (via.viaUri.host != src[0] || !src[1].ToString().Equals(via.viaUri.port))
                        {
                            via.attributes.Add("received", src[0]);
                            via.viaUri.host = src[0];
                        }
                        if (via.attributes.ContainsKey("rport"))
                        {
                            via.attributes["rport"] = src[1];
                        }
                        via.viaUri.port = Convert.ToInt32(src[1]);
                        this.receivedRequest(m, uri);
                    }
                    else if (m.response_code > 0)
                    {
                        this.receivedResponse(m, uri);
                    }
                    else
                    {
                        Debug.Assert(false, String.Format("Received invalid message \n{0}\n", m.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, String.Format("Error in received message \n{0}\n with error message {1}", data,ex.Message));
                }
            }
            else
            {
                //Console.WriteLine("Error, null message received");
            }

        }

        private void receivedRequest(Message m, SIPURI uri)
        {
            string branch = m.headers["Via"][0].attributes["branch"];
            Transaction t = null;
            if (m.method == "ACK" && branch == "0")
            {
                t = null;
            }
            else
            {
                t = this.findTransaction(Transaction.createId(branch, m.method));
            }
            if (t == null)
            {
                UserAgent app = null; // Huh ?
                if ((m.method != "CANCEL") && (m.headers["To"][0].attributes.ContainsKey("tag")))
                {
                    //In dialog request
                    Dialog d = this.findDialog(m);
                    if (d == null)
                    {
                        if (m.method != "ACK")
                        {
                            //Updated from latest code TODO
                            UserAgent u = this.createServer(m, uri);
                            if (u != null)
                            {
                                app = u;
                            }
                            else
                            {
                                this.send(Message.createResponse(481, "Dialog does not exist", null, null, m));
                                return;
                            }
                        }
                        else
                        {
                            if ((t == null) && (branch != "0"))
                            {
                                t = this.findTransaction(Transaction.createId(branch, "INVITE"));
                            }
                            if (t != null && t.state != "terminated")
                            {
                                t.receivedRequest(m);
                                return;
                            }
                            else
                            {
                                Debug.Assert(false, String.Format("No existing transaction for ACK \n{0}\n", m.ToString()));
                                UserAgent u = this.createServer(m, uri);
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
                        app = d.app;
                    }

                }
                else if (!(m.method == "CANCEL"))
                {
                    //Out of dialog request
                    UserAgent u = this.createServer(m, uri);
                    if (u != null)
                    {
                        //TODO error.....
                        app = u;
                    }
                    else if (m.method == "OPTIONS")
                    {
                        //Handle OPTIONS
                        Message reply = Message.createResponse(200, "OK", null, null, m);
                        reply.insertHeader(new Header("INVITE,ACK,CANCEL,BYE,OPTION,MESSAGE", "Allow"));
                        this.send(m);
                        return;
                    }
                    else if (m.method == "MESSAGE")
                    {
                        //Handle MESSAGE
                        UserAgent ua = new UserAgent(this);
                        ua.request = m;
                        Message reply = ua.createResponse(200, "OK");
                        this.send(reply);
                        this.app.receivedRequest(ua, m, this);
                        return;
                    }
                    else if (m.method != "ACK")
                    {
                        this.send(Message.createResponse(405, "Method not allowed", null, null, m));
                        return;
                    }
                }
                else
                {
                    //Cancel Request
                    Transaction o = this.findTransaction(Transaction.createId(m.headers["Via"][0].attributes["branch"], "INVITE"));
                    if (o == null)
                    {
                        this.send(Message.createResponse(481, "Original transaction does not exist", null, null, m));
                        return;
                    }
                    else
                    {
                        app = o.app;
                    }
                }
                if (app != null)
                {
                    t = Transaction.createServer(this, app, m, this.transport, this.tag);
                }
                else if (m.method != "ACK")
                {
                    this.send(Message.createResponse(404, "Not found", null, null, m));
                }
            }
            else
            {
                t.receivedRequest(m);
            }
        }

        private Dialog findDialog(object m)
        {
            string id = "";
            if (m is Message)
            {
                id = Dialog.extractID((Message)m);
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

        public Transaction findTransaction(string id)
        {
            if (this.transactions.ContainsKey(id))
            {
                return this.transactions[id];
            }
            else return null;
        }

        public Transaction findOtherTransactions(Message r, Transaction orig)
        {
            foreach (Transaction t in this.transactions.Values)
            {
                if ((t != orig) && (Transaction.equals(t, r, orig)))
                {
                    return t;
                }
            }
            return null;
        }

        public UserAgent createServer(Message request, SIPURI uri) { return this.app.createServer(request, uri, this); }
        public void sending(UserAgent ua, Message message) { this.app.sending(ua, message, this); }
        public void receivedRequest(UserAgent ua, Message request) { this.app.receivedRequest(ua, request, this); }
        public void receivedResponse(UserAgent ua, Message response) { this.app.receivedResponse(ua, response, this); }
        public void cancelled(UserAgent ua, Message request) { this.app.cancelled(ua, request, this); }
        public void dialogCreated(Dialog dialog, UserAgent ua) { this.app.dialogCreated(dialog, ua, this); }
        public string[] authenticate(UserAgent ua, Header header) { return this.app.authenticate(ua, header, this); }
        public Timer createTimer(UserAgent obj) { return this.app.createTimer(obj, this); }

        private void receivedResponse(Message r, SIPURI uri)
        {
            if (r.headers.ContainsKey("Service-Route") && r.is2xx() && r.first("CSeq").method.Contains("REGISTER"))
            {
                this.service_route = r.headers["Service-Route"];
                foreach (Header h in this.service_route)
                {
                    h.name = "Route";
                }
            }

            if (!r.headers.ContainsKey("Via"))
            {
                Debug.Assert(false, String.Format("No Via header in received response \n{0}\n", r.ToString()));
                return;
            }
            string branch = r.headers["Via"][0].attributes["branch"];
            string method = r.headers["CSeq"][0].method;
            Transaction t = this.findTransaction(Transaction.createId(branch, method));
            if (t == null)
            {
                if ((method == "INVITE") && (r.is2xx()))
                {
                    Dialog d = this.findDialog(r);
                    if (d == null)
                    {
                        Debug.Assert(false, String.Format("No transaction or dialog for 2xx of INVITE \n{0}\n", r.ToString()));
                        return;
                    }
                    else
                    {
                        d.receivedResponse(null, r);
                    }
                }
                else
                {
                    Debug.Assert(false, String.Format("No Transaction for response \n{0}\n", r.ToString()));
                }
            }
            else
            {
                t.receivedResponse(r); 
                return;
            }
        }

        internal Timer createTimer(Transaction transaction)
        {
            //TODO implement Timers;
            return new Timer(transaction.app);
        }
    }
}
