using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using SIPLib.SIP;
using SIPLib.utils;

namespace SIPLib
{
    public abstract class  Transaction
    {
        public string branch { get; set; }
        public string id { get; set; }
        public SIPStack stack { get; set; }
        public UserAgent app { get; set; }
        public Message request { get; set; }
        public TransportInfo transport { get; set; }
        public string tag { get; set; }
        public bool server { get; set; }
        public Dictionary<string,Timer> timers { get; set; }
        public Timer timer { get; set; }
        public string remote { get; set; }
        public string _state;
        public Message lastResponse { get; set; }
        public string state
        {
            get
            {
                return this._state;
            }
            set
            {
                this._state = value;
                if (this._state == "terminating")
                {
                    this.Close();
                }
            }

        }
        public Dictionary<string, List<Header>> _headers;
        public Dictionary<string, List<Header>> headers
        {
            get
            {
                return (Dictionary<string, List<Header>>)this.request.headers.Where(p => p.Key == "To" || p.Key == "From" || p.Key == "CSeq" || p.Key == "Call-ID");
            }
        }

        protected Transaction(UserAgent app)
        {
            this.timers = new Dictionary<string, Timer>();
            this.timer = new Timer(this.app);
            this.app = app;
        }

        public Transaction(bool server)
        {
            this.timers = new Dictionary<string, Timer>();
            this.server = server;
            this.timer = new Timer(this.app);
        }

        

        public static string CreateBranch(object request, bool server)
        {
            string To = "", From = "", CallId = "", CSeq = "";
            if (request is Message)
            {
                Message request_message = (Message)(request);
                To = request_message.First("To").Value.ToString();
                From = request_message.First("From").Value.ToString();
                CallId = request_message.First("Call-ID").Value.ToString();
                CSeq = request_message.First("CSeq").Number.ToString();
            }
            else if (request is Dictionary<string,string>)
            {
                Dictionary<string, string> dict = (Dictionary<string, string>)request;
                string[] headers = dict.Values.ToArray();
                To = headers[0];
                From = headers[1];
                CallId = headers[2];
                CSeq = headers[3];
            }
            string data = To.ToLower() + "|" + From.ToLower() + "|" + CallId.ToLower() + "|" + CSeq.ToLower() + "|" + server.ToString();
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = Utils.GetMd5Hash(md5Hash, data);
            }
            data = Utils.Base64Encode(data.Replace('=', '.'));
            return "z9hG4bK" + data;

        }

        public static string CreateId(string branch, string method)
        {
            if (method != "ACK" && method != "CANCEL")
            {
                return branch;
            }
            else
            {
                return branch + "|" + method;
            }
        }

        public static Transaction CreateServer(SIPStack stack, UserAgent app, Message request, TransportInfo transport, string tag)
        {
            Transaction t = null;
            if (request.method == "INVITE")
            {
                t = new InviteServerTransaction(app);
            }
            else if (1 == 1)
            {
                t = new ServerTransaction(app);
            }
            t.stack = stack;
            t.app = app;
            t.request = request;
            t.transport = transport;
            t.tag = tag;
            t.remote = request.First("Via").ViaUri.HostPort();
            if (request.headers.ContainsKey("Via") && request.First("Via").Attributes.ContainsKey("branch"))
            {
                t.branch = request.First("Via").Attributes["branch"];
            }
            else
            {
                t.branch = Transaction.CreateBranch(request, true);
            }
            t.id = Transaction.CreateId(t.branch, request.method);
            stack.transactions[t.id] = t;
            if (request.method == "INVITE")
            {
                ((InviteServerTransaction)t).Start();
            }
            else if (1 == 1)
            {
                ((ServerTransaction)t).Start();
            }
            return t;
        }

        public static Transaction CreateClient(SIPStack stack, UserAgent app, Message request, TransportInfo transport, string remote)
        {
            Transaction t;
            if (request.method == "INVITE")
            {
                t = new InviteClientTransaction(app);
            }
            else
            {
                t = new ClientTransaction(app);
            }
            t.stack = stack;
            t.app = app;
            t.request = request;
            t.transport = transport;
            t.remote = remote;

            if (request.headers.ContainsKey("Via") && request.First("Via").Attributes.ContainsKey("branch"))
            {
                t.branch = request.First("Via").Attributes["branch"];
            }
            else
            {
                t.branch = Transaction.CreateBranch(request, false);
            }
            t.id = Transaction.CreateId(t.branch, request.method);
            stack.transactions[t.id] = t;
            if (request.method == "INVITE")
            {
                ((InviteClientTransaction)t).Start();
            }
            else
            {
                ((ClientTransaction)t).Start();
            }
            return t;
        }

        //private void start()
        //{
        //    //TODO Transaction start ?
        //    //throw new NotImplementedException();
        //}

        public static bool Equals(Transaction t1, Message r, Transaction t2)
        {
            Message t = t1.request;
            Address request_To = (Address)(r.First("To").Value);
            Address t1_To = (Address)(t.First("To").Value);

            Address request_From = (Address)(r.First("To").Value);
            Address t1_From = (Address)(t.First("To").Value);

            bool a = (request_To.Uri == t1_To.Uri);
            a = a && (request_From.Uri == t1_From.Uri);

            a = a && (r.First("Call-ID").Value.ToString() == t.First("Call-ID").Value.ToString());
            a = a && (r.First("CSeq").Value.ToString() == t.First("CSeq").Value.ToString());

            a = a && (r.First("From").Attributes["tag"] == t.First("From").Attributes["tag"]);
            a = a && (t2.server == t1.server);
            return a;
        }

        public void Close()
        {
            this.StopTimers();
            if (this.stack != null)
            {
                if (this.stack.transactions.ContainsKey(this.id))
                {
                    this.stack.transactions.Remove(this.id);
                }
            }
        }

        public Message CreateAck()
        {
            if (this.request != null && !this.server)
            {
                return Message.CreateRequest("ACK", this.request.uri, this.headers);
            }
            else
            {
                return null;
            }
        }

        public Message CreateCancel()
        {
            Message m = null;
            if (this.request != null && !this.server)
            {
                m = Message.CreateRequest("CANCEL", this.request.uri, this.headers);
                if (m != null && this.request.headers.ContainsKey("Route"))
                {
                    m.headers["Route"] = this.request.headers["Route"];
                }
                if (m != null && this.request.headers.ContainsKey("Via"))
                {
                    m.headers["Via"] = new List<Header>();
                    m.headers["Via"].Add(this.request.First("Route"));
                }
            }
            return m;
        }

        public Message CreateResponse(int response_code, string responsetext)
        {
            Message m = null;
            if (this.request != null && this.server)
            {
                m = Message.CreateResponse(response_code,responsetext,null,null,this.request);
                if (response_code != 100 && !m.headers["To"][0].Attributes.ContainsKey("tag"))
                {
                    m.headers["To"][0].Attributes.Add("tag", this.tag);
                }
            }
            return m;
        }

        public void StartTimer(string name, int timeout)
        {
            Timer timer = null;
            if (timeout > 0)
            {
                if (this.timers.ContainsKey(name))
                {
                    timer = this.timers[name];
                }
                else
                {
                    timer = this.timers[name] = this.stack.CreateTimer(this);
                }
                timer.delay = timeout;
                timer.Start();
            }
        }

        public void Timedout(Timer timer)
        {
            if (timer.running)
            {
                timer.Stop();
            }
            var found = this.timers.Where(p => p.Value == timer);
            if (found.Count() > 0)
            {
                foreach (KeyValuePair<string, Timer> kvp in found)
                {
                    this.timers.Remove(kvp.Key);
                }
            }
            this.Timeout(found.First().Value, timer.delay);
        }

        private void Timeout(Timer timer, int p)
        {
            throw new NotImplementedException();
        }

        public void StopTimers()
        {
            foreach (Timer t in this.timers.Values)
            {
                t.Stop();
            }
            this.timers = new Dictionary<string, Timer>();
        }

        public virtual void SendResponse(Message message)
        {
            throw new NotImplementedException();
        }

        public virtual void ReceivedRequest(Message m)
        {
            throw new NotImplementedException();
        }

        public virtual void ReceivedResponse(Message r)
        {
            throw new NotImplementedException();
        }
    }
}
