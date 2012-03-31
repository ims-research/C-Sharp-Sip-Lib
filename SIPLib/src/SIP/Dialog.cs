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
        public void close()
        {
            if (this.stack != null)
            {
                if (this.stack.dialogs.ContainsKey(this.id))
                {
                    this.stack.dialogs.Remove(this.id);
                }
            }
        }


        public static Dialog createServer(SIPStack stack, Message request, Message response, Transaction transaction)
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
            d.remoteSeq = request.first("CSeq").number;
            d.callId = request.first("Call-ID").value.ToString();
            d.localTag = response.first("To").attributes["tag"];
            d.remoteTag = request.first("From").attributes["tag"];
            d.localParty = new Address(request.first("To").value.ToString());
            d.remoteParty = new Address(request.first("From").value.ToString());
            d.remoteTarget = new SIPURI(((Address)(request.first("Contact").value)).uri.ToString());
            // TODO: retransmission timer for 2xx in UAC
            stack.dialogs[d.callId] = d;
            return d;

        }

        public static Dialog createClient(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, false);
            d.request = request;
            if (request.headers.ContainsKey("Record-Route"))
            {
                d.routeSet = request.headers["Record-Route"];
                d.routeSet.Reverse();
            }
            d.localSeq = request.first("CSeq").number;
            d.remoteSeq = 0;
            d.callId = request.first("Call-ID").value.ToString();
            d.localTag = request.first("From").attributes["tag"];
            d.remoteTag = response.first("To").attributes["tag"];
            d.localParty = new Address(request.first("From").value.ToString());
            d.remoteParty = new Address(request.first("To").value.ToString());
            d.remoteTarget = new SIPURI(((Address)(response.first("Contact").value)).uri.ToString());
            stack.dialogs[d.callId] = d;
            return d;
        }

        public static string extractID(Message m)
        {
            string temp = m.first("Call-ID").value.ToString() + "|";
            if (m.method != null && m.method.Length > 0)
            {
                temp = temp + m.first("To").attributes["tag"] + "|";
                temp = temp + m.first("From").attributes["tag"];
            }
            else
            {
                temp = temp + m.first("From").attributes["tag"] + "|";
                temp = temp + m.first("To").attributes["tag"] + "|";
            }
            return temp;
        }
        public Message createRequest(string method, string content = null, string contentType = null)
        {
            Message request = base.createRequest(method, content, contentType);
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

        public Message createResponse(int response_code, string response_text, string content = null, string contentType = null)
        {
            if (this.servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return null;
            }
            Message request = this.servers[0].request;
            Message response = Message.createResponse(response_code, response_text, null, content, request);
            if (contentType.Length > 0)
            {
                response.insertHeader(new Header(contentType, "Content-Type"));
            }
            if (response.response_code != 100 && !response.headers["To"][0].attributes.ContainsKey("tag"))
            {
                response.headers["To"][0].attributes["tag"] = this.localTag;
            }
            return response;
        }

        public void sendResponse(object response, string response_text = null, string content = null, string contentType = null, bool createDialog = true)
        {
            if (this.servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return;
            }
            this.transaction = this.servers[0];
            this.request = this.servers[0].request;
            base.sendResponse(response, response_text, content, contentType);
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

        public void sendCancel()
        {
            if (this.clients.Count == 0)
            {
                return;
            }
            this.transaction = this.clients[0];
            this.request = this.clients[0].request;
            base.sendCancel();
        }

        public void receivedRequest(Transaction transaction, Message request)
        {
            if (this.remoteSeq != 0 && request.headers["CSeq"][0].number < this.remoteSeq)
            {
                Debug.Assert(false, String.Format("Dialog.receivedRequest() CSeq is old {0} < {1}", request.headers["CSeq"][0].number, this.remoteSeq));
                this.sendResponse(500, "Internal server error - invalid CSeq");
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
                    this.stack.receivedRequest(this,request);
                }
                else
                {
                    this.stack.cancelled(this,transaction.request);
                }
                return;
            }
            this.servers.Add(transaction);
            this.stack.receivedRequest(this,request);
        
        }
        public void receivedResponse(Transaction transaction, Message response)
        {
            if (response.is2xx() && response.headers.ContainsKey("Contact") && transaction != null && transaction.request.method == "INVITE")
            {
                this.remoteTarget = new SIPURI(((Address)(request.first("Contact").value)).uri.ToString());
            }
            if (!response.is1xx())
                this.clients.RemoveAll(x => x == transaction);

            if (response.response_code == 408 || response.response_code == 481)
            {
                this.close();
            }

            if (response.response_code == 401 || response.response_code == 407)
            {
                if (this.authenticate(response, transaction))
                {
                    this.stack.receivedResponse(this, response);
                }
            }
            else if (transaction != null)
            {
                this.stack.receivedResponse(this, response);
            }

            if (this.autoack && response.is2xx() && (transaction != null && transaction.request.method == "INVITE" || response.first("CSeq").method == "INVITE"))
            {
                this.sendRequest(this.createRequest("ACK"));
            }

        }
    }
}
