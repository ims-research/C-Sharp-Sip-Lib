using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
namespace SIPLib
{
    public class Dialog : UserAgent
    {
        public List<Transaction> servers { get; set; }
        public List<Transaction> clients { get; set; }
        private string _id;
        public string id
        {
            get
            {
                if (this._id.Length <= 0)
                {
                    return this.callId + "|" + this.localTag + "|" + this.remoteTag;
                }
                else return this._id;
            }
            set
            {
                _id = value;
            }

        }

        public Dialog(SIPStack stack, Message request, bool server, Transaction transaction = null)
            : base(stack, request, server)
        {
            this.servers = new List<Transaction>();
            this.clients = new List<Transaction>();
            this._id = "";
            if (transaction != null)
            {
                transaction.app = this;
            }

        }
        public void Close()
        {
            if (this.stack != null)
            {
                if (this.stack.dialogs.ContainsKey(this.id))
                {
                    this.stack.dialogs.Remove(this.id);
                }
            }
        }


        public static Dialog CreateServer(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, true);
            d.request = request;
            if (request.headers.ContainsKey("Record-Route"))
            {
                d.routeSet = request.headers["Record-Route"];
            }
            // TODO: Handle multicast addresses
            // TODO: Handle tls / secure sip
            d.localSeq = 0;
            d.remoteSeq = request.First("CSeq").number;
            d.callId = request.First("Call-ID").value.ToString();
            d.localTag = response.First("To").attributes["tag"];
            d.remoteTag = request.First("From").attributes["tag"];
            d.localParty = new Address(request.First("To").value.ToString());
            d.remoteParty = new Address(request.First("From").value.ToString());
            d.remoteTarget = new SIPURI(((Address)(request.First("Contact").value)).uri.ToString());
            // TODO: retransmission timer for 2xx in UAC
            stack.dialogs[d.callId] = d;
            return d;

        }

        public static Dialog CreateClient(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, false);
            d.request = request;
            if (request.headers.ContainsKey("Record-Route"))
            {
                d.routeSet = request.headers["Record-Route"];
                d.routeSet.Reverse();
            }
            d.localSeq = request.First("CSeq").number;
            d.remoteSeq = 0;
            d.callId = request.First("Call-ID").value.ToString();
            d.localTag = request.First("From").attributes["tag"];
            d.remoteTag = response.First("To").attributes["tag"];
            d.localParty = new Address(request.First("From").value.ToString());
            d.remoteParty = new Address(request.First("To").value.ToString());
            d.remoteTarget = new SIPURI(((Address)(response.First("Contact").value)).uri.ToString());
            stack.dialogs[d.callId] = d;
            return d;
        }

        public static string ExtractID(Message m)
        {
            string temp = m.First("Call-ID").value.ToString() + "|";
            if (m.method != null && m.method.Length > 0)
            {
                temp = temp + m.First("To").attributes["tag"] + "|";
                temp = temp + m.First("From").attributes["tag"];
            }
            else
            {
                temp = temp + m.First("From").attributes["tag"] + "|";
                temp = temp + m.First("To").attributes["tag"] + "|";
            }
            return temp;
        }
        public Message CreateRequest(string method, string content = null, string contentType = null)
        {
            Message request = base.CreateRequest(method, content, contentType);
            if (this.remoteTag != "")
            {
                request.headers["To"][0].attributes["tag"] = this.remoteTag;
            }
            if (this.routeSet !=null && this.routeSet.Count > 0 && !this.routeSet[0].value.ToString().Contains("lr"))
            {
                request.uri = new SIPURI((string)(this.routeSet[0].value));
                request.uri.parameters.Remove("lr");
            }
            return request;
        }

        public Message CreateResponse(int response_code, string response_text, string content = null, string contentType = null)
        {
            if (this.servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return null;
            }
            Message request = this.servers[0].request;
            Message response = Message.CreateResponse(response_code, response_text, null, content, request);
            if (contentType.Length > 0)
            {
                response.InsertHeader(new Header(contentType, "Content-Type"));
            }
            if (response.response_code != 100 && !response.headers["To"][0].attributes.ContainsKey("tag"))
            {
                response.headers["To"][0].attributes["tag"] = this.localTag;
            }
            return response;
        }

        public void SendResponse(object response, string response_text = null, string content = null, string contentType = null, bool createDialog = true)
        {
            if (this.servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return;
            }
            this.transaction = this.servers[0];
            this.request = this.servers[0].request;
            base.SendResponse(response, response_text, content, contentType);
            int code = 0;
            if (response is int)
            {
                code = (int) response;
            }
            else if (response is Message)
            {
                code = ((Message)(response)).response_code;
            }
            if (code > 200)
            {
                this.servers.RemoveAt(0);
            }
        }

        public void SendCancel()
        {
            if (this.clients.Count == 0)
            {
                return;
            }
            this.transaction = this.clients[0];
            this.request = this.clients[0].request;
            base.SendCancel();
        }

        public void ReceivedRequest(Transaction transaction, Message request)
        {
            if (this.remoteSeq != 0 && request.headers["CSeq"][0].number < this.remoteSeq)
            {
                Debug.Assert(false, String.Format("Dialog.receivedRequest() CSeq is old {0} < {1}", request.headers["CSeq"][0].number, this.remoteSeq));
                this.SendResponse(500, "Internal server error - invalid CSeq");
                return;
            }
            this.remoteSeq = request.headers["CSeq"][0].number;

            if (request.method == "INVITE" && request.headers.ContainsKey("Contact"))
            {
                this.remoteTarget =new SIPURI(((Address)(request.headers["Contact"][0].value)).uri.ToString());
            }

            if (request.method == "ACK" || request.method == "CANCEL")
            {
                this.servers.RemoveAll(x => x == transaction);
                if (request.method == "ACK")
                {
                    this.stack.ReceivedRequest(this,request);
                }
                else
                {
                    this.stack.Cancelled(this,transaction.request);
                }
                return;
            }
            this.servers.Add(transaction);
            this.stack.ReceivedRequest(this,request);
        
        }
        public void ReceivedResponse(Transaction transaction, Message response)
        {
            if (response.Is2xx() && response.headers.ContainsKey("Contact") && transaction != null && transaction.request.method == "INVITE")
            {
                this.remoteTarget = new SIPURI(((Address)(request.First("Contact").value)).uri.ToString());
            }
            if (!response.Is1xx())
                this.clients.RemoveAll(x => x == transaction);

            if (response.response_code == 408 || response.response_code == 481)
            {
                this.Close();
            }

            if (response.response_code == 401 || response.response_code == 407)
            {
                if (this.Authenticate(response, transaction))
                {
                    this.stack.ReceivedResponse(this, response);
                }
            }
            else if (transaction != null)
            {
                this.stack.ReceivedResponse(this, response);
            }

            if (this.autoack && response.Is2xx() && (transaction != null && transaction.request.method == "INVITE" || response.First("CSeq").method == "INVITE"))
            {
                this.SendRequest(this.CreateRequest("ACK"));
            }

        }
    }
}
