using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SIPLib.utils;

namespace SIPLib.SIP
{
    public class UserAgent
    {

        public Message Request { get; set; }
        public SIPStack Stack { get; set; }
        public bool Server { get; set; }
        public Transaction Transaction { get; set; }
        public Message CancelRequest { get; set; }
        public string CallID { get; set; }
        public Address RemoteParty { get; set; }
        public Address LocalParty { get; set; }
        readonly Random _random = new Random();
        public string LocalTag { get; set; }
        public string RemoteTag { get; set; }
        public string Subject { get; set; }
        public List<Header> RouteSet { get; set; }
        public int MaxForwards { get; set; }

        public SIPURI LocalTarget { get; set; }
        public SIPURI RemoteTarget { get; set; }
        public List<SIPURI> RemoteCandidates { get; set; }
        public int RemoteSeq { get; set; }
        public int LocalSeq { get; set; }

        public Address Contact { get; set; }
        public bool Autoack { get; set; }
        public Dictionary<string, string> Auth { get; set; }

        public UserAgent App { get; set; }

        public UserAgent(SIPStack stack, Message request = null, bool server = false)
        {
            this.Stack = stack;
            //this.app = stack.app;
            this.Request = request;
            if (server == true)
            {
                this.Server = true;
            }
            else
            {
                this.Server = (request == null);
            }
            this.Transaction = null;
            this.CancelRequest = null;
            if ((request != null) && (request.headers.ContainsKey("Call-Id")))
            {
                this.CallID = (string)request.headers["Call-ID"][0].Value;
            }
            else
            {
                this.CallID = stack.NewCallId();
            }

            if ((request != null) && (request.headers.ContainsKey("From")))
            {
                this.RemoteParty = (Address)request.headers["From"][0].Value;
            }
            else
            {
                this.RemoteParty = null;
            }

            if ((request != null) && (request.headers.ContainsKey("To")))
            {
                this.LocalParty = (Address)request.headers["To"][0].Value;
            }
            else
            {
                this.LocalParty = null;
            }
            this.LocalTag = stack.tag + _random.Next(0, 2147483647).ToString();
            this.RemoteTag = stack.tag + _random.Next(0, 2147483647).ToString();

            if ((request != null) && (request.headers.ContainsKey("Subject")))
            {
                this.Subject = (string)request.headers["Subject"][0].Value;
            }
            else
            {
                this.Subject = "";
            }

            this.MaxForwards = 70;
            this.RouteSet = new List<Header>();

            this.LocalTarget = null;
            this.RemoteTarget = null;
            this.RemoteCandidates = null;
            this.LocalSeq = 0;
            this.RemoteSeq = 0;

            this.Contact = new Address(stack.uri.ToString());

            if (this.LocalParty != null)
            {
                if (this.LocalParty.Uri.user.Length > 0)
                {
                    this.Contact.Uri.user = this.LocalParty.Uri.user;
                }
            }

            this.Autoack = true;
            this.Auth = new Dictionary<string, string>();
        }

        public string Repr()
        {
            string type = "";
            if (this is Dialog) type = "Dialog";
            if (this is UserAgent) type = "Useragent";
            return String.Format("<{0} call-id={1}>", type, this.CallID);
        }

        public Message CreateRequest(string method, string content = "", string contentType = "")
        {
            this.Server = false;
            if (this.RemoteParty == null)
            {
                Debug.Assert(false, String.Format("No remoteParty for UAC\n"));
            }
            if (this.LocalParty == null)
            {
                this.LocalParty = new Address("\"Anonymous\" <sip:anonymous@anonymous.invalid>");
            }
            SIPURI uri;
            if (this.RemoteTarget == null)
            {
                uri = new SIPURI(this.RemoteParty.ToString());
            }
            else
            {
                uri = this.RemoteParty.Uri;
            }
            if (method == "REGISTER")
            {
                uri.user = "";
            }
            if ((method != "ACK") && (method != "CANCEL"))
            {
                this.LocalSeq = ++this.LocalSeq;
            }
            Header To = new Header(this.RemoteParty.ToString(), "To");
            Header From = new Header(this.LocalParty.ToString(), "From");
            From.Attributes["tag"] = this.LocalTag;
            Header CSeq = new Header(this.LocalSeq + " " + method, "CSeq");
            Header CallId = new Header(this.CallID, "Call-ID");
            Header MaxForwards = new Header(this.MaxForwards.ToString(), "Max-Forwards");
            Header Via = this.Stack.CreateVia();
            Dictionary<string, object> branch_params = new Dictionary<string, object>();
            branch_params.Add("To", To.Value);
            branch_params.Add("From", From.Value);
            branch_params.Add("CallId", CallId.Value);
            branch_params.Add("CSeq", CSeq.Number);
            Via.Attributes["branch"] = Transaction.CreateBranch(branch_params, false);
            if (this.LocalTarget == null)
            {
                this.LocalTarget = this.Stack.uri.Dup();
                this.LocalTarget.user = this.LocalParty.Uri.user;
            }
            Header Contact = new Header(this.LocalTarget.ToString(), "Contact");
            List<Header> headers = new List<Header>();
            Header[] header_list = { To, From, CSeq, CallId, MaxForwards, Via, Contact };
            foreach (Header h in header_list)
            {
                headers.Add(h);
            }
            // Check this TODO
            //
            if (this.RouteSet.Count != 0)
            {
                foreach (Header x in this.RouteSet)
                {
                    //Secure parsing missing TODO
                    headers.Add(x);
                }
            }

            //app adds other headers such as Supported, Require and Proxy-Require
            if (contentType != null && contentType.Length > 0)
            {
                headers.Add(new Header(contentType, "Content-Type"));
            }
            Dictionary<string, List<Header>> header_dict = new Dictionary<string, List<Header>>();
            foreach (Header h in headers)
            {
                if (header_dict.ContainsKey(h.Name))
                {
                    header_dict[h.Name].Add(h);
                }
                else
                {
                    List<Header> temp = new List<Header>();
                    temp.Add(h);
                    header_dict.Add(h.Name, temp);
                }
            }
            this.Request = Message.CreateRequest(method, uri, header_dict, content);
            return this.Request;
        }

        public Message CreateRegister(SIPURI aor)
        {
            if (aor != null)
            {
                this.RemoteParty = new Address(aor.ToString());
            }
            if (this.LocalParty == null)
            {
                this.LocalParty = new Address(this.RemoteParty.ToString());
            }
            Message m = this.CreateRequest("REGISTER");
            m.InsertHeader(new Header("", "Authorization"));
            return m;
        }

        public void SendRequest(Message request)
        {
            if ((this.Request == null) && (request.method == "REGISTER"))
            {
                if ((this.Transaction == null) && (this.Transaction.state != "completed") && (this.Transaction.state != "terminated"))
                {
                    Debug.Assert(false, String.Format("Cannot re-REGISTER since pending registration\n{0}"));
                }
            }
            this.Request = request;

            if (!request.headers.ContainsKey("Route"))
            {
                this.RemoteTarget = request.uri;
            }
            SIPURI target = this.RemoteTarget;
            if (request.headers.ContainsKey("Route"))
            {
                if (request.headers["Route"].Count > 0)
                {
                    target = ((Address)(request.headers["Route"][0].Value)).Uri;
                    if ((target == null) || !target.parameters.ContainsKey("lr"))
                    {
                        request.headers["Route"].RemoveAt(0);
                        if (request.headers["Route"].Count > 0)
                        {
                            request.headers["Route"].Insert(request.headers["Route"].Count - 1, new Header(request.uri.ToString(), "Route"));
                        }
                        request.uri = target;
                    }
                }

            }
            // TODO: remove any Route Header in REGISTER request
            this.Stack.Sending(this, request);
            SIPURI dest = target.Dup();
            if (dest.port == 0)
            {
                dest.port = 5060;
            }

            if (Utils.IsIPv4(dest.host))
            {
                this.RemoteCandidates = new List<SIPURI>();
                this.RemoteCandidates.Add(dest);
            }
            if ((this.RemoteCandidates == null) || (this.RemoteCandidates.Count == 0))
            {
                this.Error(null, "Cannot Resolve DNS target");
                return;
            }
            target = this.RemoteCandidates.First();
            this.RemoteCandidates.RemoveAt(0);
            if (this.Request.method != "ACK")
            {
                this.Transaction = Transaction.CreateClient(this.Stack, this, this.Request, this.Stack.Transport, target.host + ":" + target.port);
            }
            else
            {
                this.Stack.Send(this.Request, target.host + ":" + target.port);
            }
        }

        public void Timeout(Transaction transaction)
        {
            if ((transaction != null) && (transaction != this.Transaction))
            {
                return;
            }
            this.Transaction = null;
            if (this.Server == false)
            {
                if ((this.RemoteCandidates != null) && (this.RemoteCandidates.Count > 0))
                {
                    this.RetryNextCandidate();
                }
                else
                {
                    this.ReceivedResponse(null, Message.CreateResponse(408, "Request Timeoute", null, null, this.Request));
                }
            }
        }

        private void RetryNextCandidate()
        {
            if ((this.RemoteCandidates == null) || (this.RemoteCandidates.Count == 0))
            {
                Debug.Assert(false, String.Format("No more DNS resolved address to try\n"));
            }
            SIPURI target = this.RemoteCandidates.First();
            this.RemoteCandidates.RemoveAt(0);
            this.Request.headers["Via"][0].Attributes["branch"] += "A";
            Transaction = Transaction.CreateClient(this.Stack, this, this.Request, this.Stack.Transport, target.host + ":" + target.port);

        }

        public void Error(Transaction t, string error)
        {
            if ((t != null) && t != this.Transaction)
            {
                return;
            }
            this.Transaction = null;
            if (this.Server == false)
            {
                if ((this.RemoteCandidates != null) && (this.RemoteCandidates.Count > 0))
                {
                    this.RetryNextCandidate();
                }
                else
                {
                    this.ReceivedResponse(null, Message.CreateResponse(503, "Service unavailable - " + error, null, null, this.Request));
                }
            }
        }

        public void ReceivedResponse(Transaction transaction, Message response)
        {
            if ((transaction != null) && transaction != this.Transaction)
            {
                Debug.Assert(false, String.Format("Invalid transaction received {0} != {1}", transaction, this.Transaction));
                return;
            }
            if (response.headers["Via"].Count > 1)
            {
                Debug.Assert(false, String.Format("More than one Via header in resposne"));
                return;
            }
            if (response.Is1xx())
            {
                if (this.CancelRequest != null)
                {
                    Transaction cancel = Transaction.CreateClient(this.Stack, this, this.CancelRequest, transaction.transport, transaction.remote);
                    this.CancelRequest = null;
                }
                else
                {
                    this.Stack.ReceivedResponse(this, response);
                }
            }
            else if ((response.response_code == 401) || (response.response_code == 407))
            {
                if (!this.Authenticate(response, this.Transaction))
                {
                    this.Stack.ReceivedResponse(this, response);
                }
            }
            else
            {
                if (CanCreateDialog(this.Request, response))
                {
                    Dialog dialog = Dialog.CreateClient(this.Stack, this.Request, response, transaction);
                    this.Stack.DialogCreated(dialog, this);
                    this.Stack.ReceivedResponse(dialog, response);
                    if ((this.Autoack) && (this.Request.method == "INVITE"))
                    {
                        Message ack = dialog.CreateRequest("ACK");
                        dialog.SendRequest(ack);

                        // Do we need this ?
                        /*
                        Header route = new Header("<sip:mo@pcscf.open-ims.test:4060;lr>", "Route");
                        ack.insertHeader(route, false);
                        route = new Header("<sip:mo@scscf.open-ims.test:6060;lr>", "Route");
                        ack.insertHeader(route, true);
                        route = new Header("<sip:mt@scscf.open-ims.test:6060;lr>", "Route");
                        ack.insertHeader(route, true);
                        route = new Header("<sip:mt@pcscf.open-ims.test:4060;lr>", "Route");
                        ack.insertHeader(route, true);
                        ack.insertHeader(new Header("\"Alice\"  <sip:alice@open-ims.test>","P-Preferred-Id entity"));
                         */
                        
                    }
                }
                else
                {
                    this.Stack.ReceivedResponse(this, response);
                }
            }
        }


        public static bool CanCreateDialog(Message request, Message response)
        {

            return ((response.Is2xx()) && ((request.method == "INVITE") || (request.method == "SUBSCRIBE")));
        }

        public void ReceivedRequest(Transaction transaction, Message request)
        {
            if ((transaction != null) && (this.Transaction != null) && (transaction != this.Transaction) && (request.method != "CANCEL"))
            {
                Debug.Assert(false, String.Format("Invalid transaction for received request"));
                return;
            }
            this.Server = true;
            //if request.method == 'REGISTER':
            //response = transaction.createResponse(405, 'Method not allowed')
            //response.Allow = Header('INVITE, ACK, CANCEL, BYE', 'Allow') # TODO make this configurable
            //transaction.sendResponse(response)
            //return;
            if (request.uri.scheme != "sip")
            {
                transaction.SendResponse(transaction.CreateResponse(416, "Unsupported URI scheme"));
                return;
            }
            if (!request.headers["To"][0].ToString().Contains("tag"))
            {
                if (this.Stack.FindOtherTransactions(request, transaction) != null)
                {
                    transaction.SendResponse(transaction.CreateResponse(482, "Loop Detected - found another transaction"));
                }
            }
            // TODO Fix support of Require Header
            //if (request.headers.ContainsKey("Require"))
            //{
            //    if ((request.method != "CANCEL") && (request.method != "ACK"))
            //    {
            //        Message response = transaction.createResponse(420, "Bad extension");
            //        response.insertHeader(new Header(request.headers["Require"][0].value.ToString(), "Unsupported"));
            //        transaction.sendResponse(response);
            //        return;
            //    }
            //}
            if (this.Transaction != null)
            {
                this.Transaction = transaction;
            }
            if (request.method == "CANCEL")
            {
                Transaction original = this.Stack.FindTransaction(Transaction.CreateId(transaction.branch, "Invite"));
                if (original == null)
                {
                    transaction.SendResponse(transaction.CreateResponse(481, "Cannot find transaction??"));
                    return;
                }
                if (original.state == "proceeding" || original.state == "trying")
                {
                    original.SendResponse(original.CreateResponse(487, "Request terminated"));
                }
                transaction.SendResponse(transaction.CreateResponse(200, "OK"));
                // TODO: The To tag must be the same in the two responses
            }
            this.Stack.ReceivedRequest(this, request);
        }

        public void SendResponse(object response, string response_text = "", string content = "", string contentType = "", bool createDialog=true)
        {
            Message response_message = null;
            if (this.Request == null)
            {
                Debug.Assert(false, String.Format("Invalid request in sending a response"));
                return;
            }
            if (response is int)
            {
                response_message = this.CreateResponse((int)(response), response_text, content, contentType);
            }
            else
            {
                response_message = (Message)(response);
            }
            if (createDialog && UserAgent.CanCreateDialog(this.Request, response_message))
            {
                if (this.Request.headers.ContainsKey("Record-Route"))
                {
                    response_message.headers.Add("Record-Route", this.Request.headers["Record-Route"]);
                }

                if (!response_message.headers.ContainsKey("Contact"))
                {
                    Address contact = new Address(this.Contact.ToString());
                    if (contact.Uri.user.Length == 0)
                    {
                        contact.Uri.user = ((Address)this.Request.First("To").Value).Uri.user;
                        response_message.InsertHeader(new Header(contact.ToString(), "Contact"));
                    }

                }
                Dialog dialog = Dialog.CreateServer(this.Stack, this.Request, response_message, this.Transaction);
                this.Stack.DialogCreated(dialog, this);
                this.Stack.Sending(dialog, response_message);
            }
            else
            {
                this.Stack.Sending(this, response_message);
            }
            if (this.Transaction == null)
            {
                this.Stack.Send(response_message, response_message.headers["Via"][0].ViaUri.HostPort(), null);
            }
            else
            {
                this.Transaction.SendResponse(response_message);
            }

        }


        public Message CreateResponse(int response_code, string response_text, string content = null, string contentType = null)
        {
            if (this.Request == null)
            {
                Debug.Assert(false, String.Format("Invalid request in creating a response"));
                return null;
            }
            Message response_message = Message.CreateResponse(response_code, response_text, null, content, this.Request);
            if (contentType != null)
            {
                response_message.InsertHeader(new Header(contentType, "Content-Type"));
            }
            if (response_message.response_code != 100 && !response_message.headers["To"][0].ToString().Contains("tag"))
            {
                response_message.headers["To"][0].Attributes.Add("tag", this.LocalTag);
            }
            return response_message;
        }

        public void SendCancel()
        {
            if (this.Transaction == null)
            {
                Debug.Assert(false, String.Format("No transaction for sending CANCEL"));
            }
            this.CancelRequest = this.Transaction.CreateCancel();
            if (this.Transaction.state != "trying" && this.Transaction.state != "calling")
            {
                if (this.Transaction.state == "proceeding")
                {
                    Transaction transaction = Transaction.CreateClient(this.Stack, this, this.CancelRequest, this.Transaction.transport, this.Transaction.remote);
                }
                this.CancelRequest = null;
            }

        }

        public bool Authenticate(Message response, Transaction transaction)
        {
            Header a = null;
            if (response.headers.ContainsKey("WWW-Authenticate") || response.headers.ContainsKey("Proxy-Authenticate"))
            {
                a = response.headers["WWW-Authenticate"][0];
            }
            else if (response.headers.ContainsKey("Proxy-Authenticate"))
            {
                a = response.headers["Proxy-Authenticate"][0];
            }
            else return false;
            Message request = new Message(transaction.request.ToString());
            bool resend = false, present = false;
            // foreach (Header h in request.headers["Authorization"].Concat(request.headers["Proxy-Authorization"]))
            foreach (Header h in request.headers["Authorization"])
            {
                try
                {
                    if (a.Attributes["realm"] == h.Attributes["realm"] && (a.Name == "WWW-Authenticate" && h.Name == "Authorization" || a.Name == "Proxy-Authenticate" && h.Name == "Proxy-Authorization"))
                    {
                        present = true;
                        break;
                    }
                }
                catch (Exception e)
                {
                }
            }
            if (!present && a.Attributes.ContainsKey("realm"))
            {
                string[] result = this.Stack.Authenticate(this, a);
                if (result.Length == 0 || a.Attributes.ContainsKey("password") && a.Attributes.ContainsKey("hashValue"))
                {
                    return false;
                }
                //string value = createAuthorization(a.value, a.attributes["username"], a.attributes["password"], request.uri.ToString(), this.request.method, this.request.body, this.auth);
                string value = SIP.Authenticate.CreateAuthorization(a.ToString(), result[0], result[1], request.uri.ToString(), request.method, request.body, this.Auth);
                if (value.Length > 0)
                {
                    if (a.Name == "WWW-Authenticate")
                    {
                        request.InsertHeader(new Header(value, "Authorization"), "replace");
                    }
                    else
                    {
                        request.InsertHeader(new Header(value, "Proxy-Authorization"), "replace");
                    }

                    resend = true;
                }

            }
            if (resend)
            {
                this.LocalSeq = request.First("CSeq").Number + 1;
                request.InsertHeader(new Header(this.LocalSeq.ToString() + " " + request.method, "CSeq"));
                //TODO FIX?
                //request.headers["Via"][0].attributes["branch"] = Transaction.createBranch(request, false);
                this.Request = request;
                this.Transaction = Transaction.CreateClient(this.Stack, this, this.Request, transaction.transport, transaction.remote);
                return true;
            }
            else return false;

        }
internal void ReceivedRequest(Transaction t, Message message, SIPStack sIPStack)
        {
            this.ReceivedRequest(t, message);
        }
    }

}