using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIPLib.SIP
{
    public class Dialog : UserAgent
    {
        public List<Transaction> Servers { get; set; }
        public List<Transaction> Clients { get; set; }
        private string _id;
        public string ID
        {
            get
            {
                if (_id.Length <= 0)
                {
                    return CallID + "|" + LocalTag + "|" + RemoteTag;
                }
                return _id;
            }
            set
            {
                _id = value;
            }

        }

        public Dialog(SIPStack stack, Message request, bool server, Transaction transaction = null)
            : base(stack, request, server)
        {
            Servers = new List<Transaction>();
            Clients = new List<Transaction>();
            _id = "";
            if (transaction != null)
            {
                transaction.app = this;
            }

        }
        public void Close()
        {
            if (Stack != null)
            {
                if (Stack.dialogs.ContainsKey(ID))
                {
                    Stack.dialogs.Remove(ID);
                }
            }
        }


        public static Dialog CreateServer(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, true) {Request = request};
            if (request.headers.ContainsKey("Record-Route"))
            {
                d.RouteSet = request.headers["Record-Route"];
            }
            // TODO: Handle multicast addresses
            // TODO: Handle tls / secure sip
            d.LocalSeq = 0;
            d.RemoteSeq = request.First("CSeq").Number;
            d.CallID = request.First("Call-ID").Value.ToString();
            d.LocalTag = response.First("To").Attributes["tag"];
            d.RemoteTag = request.First("From").Attributes["tag"];
            d.LocalParty = new Address(request.First("To").Value.ToString());
            d.RemoteParty = new Address(request.First("From").Value.ToString());
            d.RemoteTarget = new SIPURI(((Address)(request.First("Contact").Value)).Uri.ToString());
            // TODO: retransmission timer for 2xx in UAC
            stack.dialogs[d.CallID] = d;
            return d;

        }

        public static Dialog CreateClient(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, false) {Request = request};
            if (request.headers.ContainsKey("Record-Route"))
            {
                d.RouteSet = request.headers["Record-Route"];
                d.RouteSet.Reverse();
            }
            d.LocalSeq = request.First("CSeq").Number;
            d.RemoteSeq = 0;
            d.CallID = request.First("Call-ID").Value.ToString();
            d.LocalTag = request.First("From").Attributes["tag"];
            d.RemoteTag = response.First("To").Attributes["tag"];
            d.LocalParty = new Address(request.First("From").Value.ToString());
            d.RemoteParty = new Address(request.First("To").Value.ToString());
            d.RemoteTarget = new SIPURI(((Address)(response.First("Contact").Value)).Uri.ToString());
            stack.dialogs[d.CallID] = d;
            return d;
        }

        public static string ExtractID(Message m)
        {
            string temp = m.First("Call-ID").Value + "|";
            if (!string.IsNullOrEmpty(m.method))
            {
                temp = temp + m.First("To").Attributes["tag"] + "|";
                temp = temp + m.First("From").Attributes["tag"];
            }
            else
            {
                temp = temp + m.First("From").Attributes["tag"] + "|";
                temp = temp + m.First("To").Attributes["tag"] + "|";
            }
            return temp;
        }
        public Message CreateRequest(string method, string content = null, string contentType = null)
        {
            Message request = base.CreateRequest(method, content, contentType);
            if (RemoteTag != "")
            {
                request.headers["To"][0].Attributes["tag"] = RemoteTag;
            }
            if (RouteSet !=null && RouteSet.Count > 0 && !RouteSet[0].Value.ToString().Contains("lr"))
            {
                request.uri = new SIPURI((string)(RouteSet[0].Value));
                request.uri.parameters.Remove("lr");
            }
            return request;
        }

        public Message CreateResponse(int response_code, string response_text, string content = null, string contentType = null)
        {
            if (Servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return null;
            }
            Message request = Servers[0].request;
            Message response = Message.CreateResponse(response_code, response_text, null, content, request);
            if (!string.IsNullOrEmpty(contentType))
            {
                response.InsertHeader(new Header(contentType, "Content-Type"));
            }
            if (response.response_code != 100 && !response.headers["To"][0].Attributes.ContainsKey("tag"))
            {
                response.headers["To"][0].Attributes["tag"] = LocalTag;
            }
            return response;
        }

        public void SendResponse(object response, string response_text = null, string content = null, string contentType = null, bool createDialog = true)
        {
            if (Servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return;
            }
            Transaction = Servers[0];
            Request = Servers[0].request;
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
                Servers.RemoveAt(0);
            }
        }

        public void SendCancel()
        {
            if (Clients.Count == 0)
            {
                return;
            }
            Transaction = Clients[0];
            Request = Clients[0].request;
            base.SendCancel();
        }

        public void ReceivedRequest(Transaction transaction, Message request)
        {
            if (RemoteSeq != 0 && request.headers["CSeq"][0].Number < RemoteSeq)
            {
                SendResponse(500, "Internal server error - invalid CSeq");
                Debug.Assert(false, String.Format("Dialog.receivedRequest() CSeq is old {0} < {1}", request.headers["CSeq"][0].Number, RemoteSeq));
                return;
            }
            RemoteSeq = request.headers["CSeq"][0].Number;

            if (request.method == "INVITE" && request.headers.ContainsKey("Contact"))
            {
                RemoteTarget =new SIPURI(((Address)(request.headers["Contact"][0].Value)).Uri.ToString());
            }

            if (request.method == "ACK" || request.method == "CANCEL")
            {
                Servers.RemoveAll(x => x == transaction);
                if (request.method == "ACK")
                {
                    Stack.ReceivedRequest(this,request);
                }
                else
                {
                    Stack.Cancelled(this,transaction.request);
                }
                return;
            }
            Servers.Add(transaction);
            Stack.ReceivedRequest(this,request);
        
        }
        public void ReceivedResponse(Transaction transaction, Message response)
        {
            if (response.Is2xx() && response.headers.ContainsKey("Contact") && transaction != null && transaction.request.method == "INVITE")
            {
                RemoteTarget = new SIPURI(((Address)(Request.First("Contact").Value)).Uri.ToString());
            }
            if (!response.Is1xx())
                Clients.RemoveAll(x => x == transaction);

            if (response.response_code == 408 || response.response_code == 481)
            {
                Close();
            }

            if (response.response_code == 401 || response.response_code == 407)
            {
                if (Authenticate(response, transaction))
                {
                    Stack.ReceivedResponse(this, response);
                }
            }
            else if (transaction != null)
            {
                Stack.ReceivedResponse(this, response);
            }

            if (Autoack && response.Is2xx() && (transaction != null && transaction.request.method == "INVITE" || response.First("CSeq").Method == "INVITE"))
            {
                SendRequest(CreateRequest("ACK"));
            }

        }
    }
}
