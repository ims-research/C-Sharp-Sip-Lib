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
                transaction.App = this;
            }

        }
        public void Close()
        {
            if (Stack != null)
            {
                if (Stack.Dialogs.ContainsKey(ID))
                {
                    Stack.Dialogs.Remove(ID);
                }
            }
        }


        public static Dialog CreateServer(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, true) {Request = request};
            if (request.Headers.ContainsKey("Record-Route"))
            {
                d.RouteSet = request.Headers["Record-Route"];
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
            stack.Dialogs[d.CallID] = d;
            return d;

        }

        public static Dialog CreateClient(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, false) {Request = request};
            if (request.Headers.ContainsKey("Record-Route"))
            {
                d.RouteSet = request.Headers["Record-Route"];
                d.RouteSet.Reverse();
            }
            d.LocalSeq = request.First("CSeq").Number;
            d.RemoteSeq = 0;
            d.CallID = request.First("Call-ID").Value.ToString();
            d.LocalTag = request.First("From").Attributes["tag"];
            d.RemoteTag = response.First("To").Attributes["tag"];
            d.LocalParty = new Address(request.First("From").Value.ToString());
            d.RemoteParty = new Address(request.First("To").Value.ToString());
            if (response.Headers.ContainsKey("Contact"))
            {
                d.RemoteTarget = new SIPURI(((Address)(response.First("Contact").Value)).Uri.ToString());    
            }
            else d.RemoteTarget = new SIPURI(((Address)(response.First("To").Value)).Uri.ToString());    
            
            stack.Dialogs[d.CallID] = d;
            return d;
        }

        public static string ExtractID(Message m)
        {
            // TODO fix this and use more than just call id ?
            string temp = m.First("Call-ID").Value.ToString();// +"|";
            //if (m.method != null && m.method.Length > 0)
            //{
            //    temp = temp + m.first("To").attributes["tag"] + "|";
            //    temp = temp + m.first("From").attributes["tag"];
            //}
            //else
            //{
            //    temp = temp + m.first("From").attributes["tag"] + "|";
            //    temp = temp + m.first("To").attributes["tag"] + "|";
            //}
            return temp;
        }
        public override Message CreateRequest(string method, string content = null, string contentType = null)
        {
            Message request = base.CreateRequest(method, content, contentType);
            if (RemoteTag != "")
            {
                request.Headers["To"][0].Attributes["tag"] = RemoteTag;
            }
            if (RouteSet !=null && RouteSet.Count > 0 && !RouteSet[0].Value.ToString().Contains("lr"))
            {
                request.Uri = new SIPURI((string)(RouteSet[0].Value));
                request.Uri.Parameters.Remove("lr");
            }
            return request;
        }

        public override Message CreateResponse(int response_code, string response_text, string content = null, string contentType = null)
        {
            if (Servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return null;
            }


            // TODO REMOVE THIS LOGIC
            //string branchID = ((Message)response).First("Via").Attributes["branch"];
            //Transaction = null;
            //foreach (Transaction transaction in Servers)
            //{
            //    if (branchID == transaction.Branch)
            //    {
            //        Transaction = transaction;
            //        Request = transaction.Request;
            //    }
            //}
            //if (Transaction == null)
            //{
            //    Debug.Assert(false, String.Format("No transactions in dialog matched"));
            //    return;
            //}
            if (Servers.Count > 1)
            {
                Console.WriteLine("Got some transaction servers");
            }
            Message request = Servers[Servers.Count-1].Request;
            Message response = Message.CreateResponse(response_code, response_text, null, content, request);
            if (!string.IsNullOrEmpty(contentType))
            {
                response.InsertHeader(new Header(contentType, "Content-Type"));
            }
            if (response.ResponseCode != 100 && !response.Headers["To"][0].Attributes.ContainsKey("tag"))
            {
                response.Headers["To"][0].Attributes["tag"] = LocalTag;
            }
            return response;
        }

        public override void SendResponse(object response, string response_text = null, string content = null, string contentType = null, bool createDialog = true)
        {
            if (Servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return;
            }

            //TODO HOW ABOUT SERVERS[SERVERS.COUNT-1]
            string branchID = ((Message)response).First("Via").Attributes["branch"];
            Transaction = null;
            
            foreach (Transaction transaction in Servers)
            {
                if (branchID == transaction.Branch)
                {
                    Transaction = transaction;
                    Request = transaction.Request;
                }
            }
            if (Transaction == null)
                {
                    Debug.Assert(false, String.Format("No transactions in dialog matched"));
                    return;
                }
            
            base.SendResponse(response, response_text, content, contentType);
            int code = 0;
            if (response is int)
            {
                code = (int) response;
            }
            else if (response is Message)
            {
                code = ((Message)(response)).ResponseCode;
            }
            if (code > 200)
            {
                Servers.RemoveAt(0);
            }
        }

        public override void SendCancel()
        {
            if (Clients.Count == 0)
            {
                return;
            }
            Transaction = Clients[0];
            Request = Clients[0].Request;
            base.SendCancel();
        }

        public override void ReceivedRequest(Transaction transaction, Message request)
        {
            if (RemoteSeq != 0 && request.Headers["CSeq"][0].Number < RemoteSeq)
            {
                SendResponse(500, "Internal server error - invalid CSeq");
                Debug.Assert(false, String.Format("Dialog.receivedRequest() CSeq is old {0} < {1}", request.Headers["CSeq"][0].Number, RemoteSeq));
                return;
            }
            RemoteSeq = request.Headers["CSeq"][0].Number;

            if (request.Method == "INVITE" && request.Headers.ContainsKey("Contact"))
            {
                RemoteTarget =new SIPURI(((Address)(request.Headers["Contact"][0].Value)).Uri.ToString());
            }

            if (request.Method == "ACK" || request.Method == "CANCEL")
            {
                Servers.RemoveAll(x => x == transaction);
                if (request.Method == "ACK")
                {
                    Stack.ReceivedRequest(this,request);
                }
                else
                {
                    Stack.Cancelled(this,transaction.Request);
                }
                return;
            }
            Servers.Add(transaction);
            Stack.ReceivedRequest(this,request);
        
        }
        public override void ReceivedResponse(Transaction transaction, Message response)
        {
            if (response.Is2XX() && response.Headers.ContainsKey("Contact") && transaction != null && transaction.Request.Method == "INVITE")
            {
                RemoteTarget = new SIPURI(((Address)(Request.First("Contact").Value)).Uri.ToString());
            }
            if (!response.Is1XX())
                Clients.RemoveAll(x => x == transaction);

            if (response.ResponseCode == 408 || response.ResponseCode == 481)
            {
                Close();
            }

            if (response.ResponseCode == 401 || response.ResponseCode == 407)
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

            if (Autoack && response.Is2XX() && (transaction != null && transaction.Request.Method == "INVITE" || response.First("CSeq").Method == "INVITE"))
            {
                SendRequest(CreateRequest("ACK"));
            }

        }
    }
}
