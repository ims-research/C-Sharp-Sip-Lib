using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SIPLib.Utils;

namespace SIPLib.SIP
{
    public abstract class  Transaction
    {
        public string Branch { get; set; }
        public string ID { get; set; }
        public SIPStack Stack { get; set; }
        public UserAgent App { get; set; }
        public Message Request { get; set; }
        public TransportInfo Transport { get; set; }
        public string Tag { get; set; }
        public bool Server { get; set; }
        public Dictionary<string,Timer> Timers { get; set; }
        public Timer Timer { get; set; }
        public string Remote { get; set; }
        private string _state;
        public Message LastResponse { get; set; }
        public string State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                if (_state == "terminating")
                {
                    Close();
                }
            }

        }

        public Dictionary<string, List<Header>> Headers
        {
            get
            {
                return (Dictionary<string, List<Header>>)Request.Headers.Where(p => p.Key == "To" || p.Key == "From" || p.Key == "CSeq" || p.Key == "Call-ID");
            }
        }

        protected Transaction(UserAgent app)
        {
            Timers = new Dictionary<string, Timer>();
            Timer = new Timer(App);
            App = app;
        }

        protected Transaction(bool server)
        {
            Timers = new Dictionary<string, Timer>();
            Server = server;
            Timer = new Timer(App);
        }

        public static string CreateBranch(object request, bool server)
        {
            string to = "", from = "", callId = "", cSeq = "";
            if (request is Message)
            {
                Message requestMessage = (Message)(request);
                to = requestMessage.First("To").Value.ToString();
                from = requestMessage.First("From").Value.ToString();
                callId = requestMessage.First("Call-ID").Value.ToString();
                cSeq = requestMessage.First("CSeq").Number.ToString();
            }
            else if (request is Dictionary<string,object>)
            {
                Dictionary<string, object> dict = (Dictionary<string, object>)request;
                object[] headers = dict.Values.ToArray();
                to = headers[0].ToString();
                from = headers[1].ToString();
                callId = headers[2].ToString();
                cSeq = headers[3].ToString();
            }
            string data = to.ToLower() + "|" + from.ToLower() + "|" + callId.ToLower() + "|" + cSeq.ToLower() + "|" + server.ToString();
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = Utils.Helpers.GetMd5Hash(md5Hash, data);
            }
            //TODO fix this ? replace data with hash ?
            data = Utils.Helpers.Base64Encode(data).Replace('=', '.');
            return "z9hG4bK" + data;
        }

        public static string CreateId(string branch, string method)
        {
            if (method != "ACK" && method != "CANCEL")
            {
                return branch;
            }
            return branch + "|" + method;
        }

        public static Transaction CreateServer(SIPStack stack, UserAgent app, Message request, TransportInfo transport, string tag,Boolean start = true)
        {
            Transaction t;
            if (request.Method == "INVITE")
            {
                t = new InviteServerTransaction(app);
            }
            else
            {
                t = new ServerTransaction(app);
            }
            t.Stack = stack;
            t.App = app;
            t.Request = request;
            t.Transport = transport;
            t.Tag = tag;
            t.Remote = request.First("Via").ViaUri.HostPort();
            if (request.Headers.ContainsKey("Via") && request.First("Via").Attributes.ContainsKey("branch"))
            {
                t.Branch = request.First("Via").Attributes["branch"];
            }
            else
            {
                t.Branch = CreateBranch(request, true);
            }
            t.ID = CreateId(t.Branch, request.Method);
            stack.Transactions[t.ID] = t;
            if (request.Method == "INVITE")
            {
                ((InviteServerTransaction)t).Start();
            }
            else
            {
                ((ServerTransaction)t).Start();
            }
            return t;
        }

        public static Transaction CreateClient(SIPStack stack, UserAgent app, Message request, TransportInfo transport, string remote)
        {
            Transaction t;
            if (request.Method == "INVITE")
            {
                t = new InviteClientTransaction(app);
            }
            else
            {
                t = new ClientTransaction(app);
            }
            t.Stack = stack;
            t.App = app;
            t.Request = request;
            t.Transport = transport;
            t.Remote = remote;

            if (request.Headers.ContainsKey("Via") && request.First("Via").Attributes.ContainsKey("branch"))
            {
                t.Branch = request.First("Via").Attributes["branch"];
            }
            else
            {
                t.Branch = CreateBranch(request, false);
            }
            t.ID = CreateId(t.Branch, request.Method);
            stack.Transactions[t.ID] = t;
            if (request.Method == "INVITE")
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

        public static bool TEquals(Transaction t1, Message r, Transaction t2)
        {
            Message t = t1.Request;
            Address requestTo = (Address)(r.First("To").Value);
            Address t1To = (Address)(t.First("To").Value);

            Address requestFrom = (Address)(r.First("To").Value);
            Address t1From = (Address)(t.First("To").Value);

            bool a = (String.Compare(requestTo.Uri.ToString(), t1To.Uri.ToString()) == 0);
            a = a && (String.Compare(requestFrom.Uri.ToString(), t1From.Uri.ToString()) == 0);

            a = a && (r.First("Call-ID").Value.ToString() == t.First("Call-ID").Value.ToString());
            a = a && (r.First("CSeq").Number.ToString() == t.First("CSeq").Number.ToString());

            a = a && (r.First("From").Attributes["tag"] == t.First("From").Attributes["tag"]);
            a = a && (t2.Server == t1.Server);
            return a;
        }

        public void Close()
        {
            StopTimers();
            if (Stack != null)
            {
                if (Stack.Transactions.ContainsKey(ID))
                {
                    Stack.Transactions.Remove(ID);
                }
            }
        }

        public virtual Message CreateAck()
        {
            if (Request != null && !Server)
            {
                return Message.CreateRequest("ACK", Request.Uri, Headers);
            }
            return null;
        }

        public virtual Message CreateCancel()
        {
            Message m = null;
            if (Request != null && !Server)
            {
                m = Message.CreateRequest("CANCEL", Request.Uri, Headers);
                if (m != null && Request.Headers.ContainsKey("Route"))
                {
                    m.Headers["Route"] = Request.Headers["Route"];
                }
                if (m != null && Request.Headers.ContainsKey("Via"))
                {
                    m.Headers["Via"] = new List<Header> {Request.First("Route")};
                }
            }
            return m;
        }

        public virtual Message CreateResponse(int responseCode, string responseText)
        {
            Message m = null;
            if (Request != null && Server)
            {
                m = Message.CreateResponse(responseCode,responseText,null,null,Request);
                if (responseCode != 100 && !m.Headers["To"][0].Attributes.ContainsKey("tag"))
                {
                    m.Headers["To"][0].Attributes.Add("tag", Tag);
                }
            }
            return m;
        }

        public virtual void StartTimer(string name, int timeout)
        {
            if (timeout > 0)
            {
                Timer timer;
                if (Timers.ContainsKey(name))
                {
                    timer = Timers[name];
                }
                else
                {
                    timer = Timers[name] = Stack.CreateTimer(this);
                }
                timer.Delay = timeout;
                timer.Start();
            }
        }

        public virtual void Timedout(Timer timer)
        {
            if (timer.Running)
            {
                timer.Stop();
            }
            var found = this.Timers.Where(p => p.Value == timer);
            foreach (KeyValuePair<string, Timer> pair in found)
            {
                foreach (KeyValuePair<string, Timer> kvp in found)
                {
                    Timers.Remove(kvp.Key);
                }
                break;
            }
            Timeout(found.First().Value, timer.Delay);
        }

        private void Timeout(Timer timer, int p)
        {
            throw new NotImplementedException("Timeout in Transaction is not implemented");
        }

        public virtual void StopTimers()
        {
            foreach (Timer t in Timers.Values)
            {
                t.Stop();
            }
            Timers = new Dictionary<string, Timer>();
        }

        public virtual void SendResponse(Message message)
        {
            throw new NotImplementedException("sendResponse in Transaction is not implemented");
        }

        public virtual void ReceivedRequest(Message receivedRequest)
        {
            throw new NotImplementedException("receivedRequest in Transaction is not implemented");
        }

        public virtual void ReceivedResponse(Message r)
        {
            throw new NotImplementedException("receivedResponse in Transaction is not implemented");
        }

        internal static string createProxyBranch(Message request, bool server)
        {
            Header via = request.First("Via");
            if (via != null && via.Attributes.ContainsKey("branch"))
            {
                string data = via.Attributes["branch"];
                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = Utils.Helpers.GetMd5Hash(md5Hash, data);
                }
                //TODO fix this ? replace data with hash ?
                data = Utils.Helpers.Base64Encode(data).Replace('=', '.');
                return "z9hG4bK" + data;
            }
            else
            {
                return Transaction.CreateBranch(request, server);
            }
        }
    }
}
