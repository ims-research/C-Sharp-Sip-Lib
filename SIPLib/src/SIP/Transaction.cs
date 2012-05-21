using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

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
                    this.close();
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

        

        public static string createBranch(object request, bool server)
        {
            string To = "", From = "", CallId = "", CSeq = "";
            if (request is Message)
            {
                Message request_message = (Message)(request);
                To = request_message.first("To").value.ToString();
                From = request_message.first("From").value.ToString();
                CallId = request_message.first("Call-ID").value.ToString();
                CSeq = request_message.first("CSeq").number.ToString();
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
            data = Utils.Base64Encode(data).Replace('=', '.');
            return "z9hG4bK" + data;

        }

        public static string createId(string branch, string method)
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

        public static Transaction createServer(SIPStack stack, UserAgent app, Message request, TransportInfo transport, string tag)
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
            t.remote = request.first("Via").viaUri.hostPort();
            if (request.headers.ContainsKey("Via") && request.first("Via").attributes.ContainsKey("branch"))
            {
                t.branch = request.first("Via").attributes["branch"];
            }
            else
            {
                t.branch = Transaction.createBranch(request, true);
            }
            t.id = Transaction.createId(t.branch, request.method);
            stack.transactions[t.id] = t;
            if (request.method == "INVITE")
            {
                ((InviteServerTransaction)t).start();
            }
            else if (1 == 1)
            {
                ((ServerTransaction)t).start();
            }
            return t;
        }

        public static Transaction createClient(SIPStack stack, UserAgent app, Message request, TransportInfo transport, string remote)
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

            if (request.headers.ContainsKey("Via") && request.first("Via").attributes.ContainsKey("branch"))
            {
                t.branch = request.first("Via").attributes["branch"];
            }
            else
            {
                t.branch = Transaction.createBranch(request, false);
            }
            t.id = Transaction.createId(t.branch, request.method);
            stack.transactions[t.id] = t;
            if (request.method == "INVITE")
            {
                ((InviteClientTransaction)t).start();
            }
            else
            {
                ((ClientTransaction)t).start();
            }
            return t;
        }

        //private void start()
        //{
        //    //TODO Transaction start ?
        //    //throw new NotImplementedException();
        //}

        public static bool equals(Transaction t1, Message r, Transaction t2)
        {
            Message t = t1.request;
            Address request_To = (Address)(r.first("To").value);
            Address t1_To = (Address)(t.first("To").value);

            Address request_From = (Address)(r.first("To").value);
            Address t1_From = (Address)(t.first("To").value);

            bool a = (request_To.uri == t1_To.uri);
            a = a && (request_From.uri == t1_From.uri);

            a = a && (r.first("Call-ID").value.ToString() == t.first("Call-ID").value.ToString());
            a = a && (r.first("CSeq").value.ToString() == t.first("CSeq").value.ToString());

            a = a && (r.first("From").attributes["tag"] == t.first("From").attributes["tag"]);
            a = a && (t2.server == t1.server);
            return a;
        }

        public void close()
        {
            this.stopTimers();
            if (this.stack != null)
            {
                if (this.stack.transactions.ContainsKey(this.id))
                {
                    this.stack.transactions.Remove(this.id);
                }
            }
        }

        public Message createAck()
        {
            if (this.request != null && !this.server)
            {
                return Message.createRequest("ACK", this.request.uri, this.headers);
            }
            else
            {
                return null;
            }
        }

        public Message createCancel()
        {
            Message m = null;
            if (this.request != null && !this.server)
            {
                m = Message.createRequest("CANCEL", this.request.uri, this.headers);
                if (m != null && this.request.headers.ContainsKey("Route"))
                {
                    m.headers["Route"] = this.request.headers["Route"];
                }
                if (m != null && this.request.headers.ContainsKey("Via"))
                {
                    m.headers["Via"] = new List<Header>();
                    m.headers["Via"].Add(this.request.first("Route"));
                }
            }
            return m;
        }

        public Message createResponse(int response_code, string responsetext)
        {
            Message m = null;
            if (this.request != null && this.server)
            {
                m = Message.createResponse(response_code,responsetext,null,null,this.request);
                if (response_code != 100 && !m.headers["To"][0].attributes.ContainsKey("tag"))
                {
                    m.headers["To"][0].attributes.Add("tag", this.tag);
                }
            }
            return m;
        }

        public void startTimer(string name, int timeout)
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
                    timer = this.timers[name] = this.stack.createTimer(this);
                }
                timer.delay = timeout;
                timer.start();
            }
        }

        public void timedout(Timer timer)
        {
            if (timer.running)
            {
                timer.stop();
            }
            var found = this.timers.Where(p => p.Value == timer);
            if (found.Count() > 0)
            {
                foreach (KeyValuePair<string, Timer> kvp in found)
                {
                    this.timers.Remove(kvp.Key);
                }
            }
            this.timeout(found.First().Value, timer.delay);
        }

        private void timeout(Timer timer, int p)
        {
            throw new NotImplementedException("Timeout in Transaction is not implemented");
        }

        public void stopTimers()
        {
            foreach (Timer t in this.timers.Values)
            {
                t.stop();
            }
            this.timers = new Dictionary<string, Timer>();
        }

        public virtual void sendResponse(Message message)
        {
            throw new NotImplementedException("sendResponse in Transaction is not implemented");
        }

        public virtual void receivedRequest(Message m)
        {
            throw new NotImplementedException("receivedRequest in Transaction is not implemented");
        }

        public virtual void receivedResponse(Message r)
        {
            throw new NotImplementedException("receivedResponse in Transaction is not implemented");
        }
    }
}
