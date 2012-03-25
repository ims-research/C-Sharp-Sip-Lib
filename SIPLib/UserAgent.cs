using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SIPLib
{
    public class UserAgent
    {

        public Message request { get; set; }
        public SIPStack stack { get; set; }
        public bool server { get; set; }
        public Transaction transaction { get; set; }
        public Message cancelRequest { get; set; }
        public string callId { get; set; }
        public Address remoteParty { get; set; }
        public Address localParty { get; set; }
        Random random = new Random();
        public string localTag { get; set; }
        public string remoteTag { get; set; }
        public string subject { get; set; }
        public List<Header> routeSet { get; set; }
        public int maxForwards { get; set; }

        public SIPURI localTarget { get; set; }
        public SIPURI remoteTarget { get; set; }
        public List<SIPURI> remoteCandidates { get; set; }
        public int remoteSeq { get; set; }
        public int localSeq { get; set; }

        public Address contact { get; set; }
        public bool autoack { get; set; }
        public Dictionary<string, string> auth { get; set; }

        public SIPApp app { get; set; }

        public UserAgent(SIPStack stack, Message request = null, bool server = false)
        {
            this.stack = stack;
            this.app = stack.app;
            this.request = request;
            if (server == true)
            {
                this.server = true;
            }
            else
            {
                this.server = (request == null);
            }
            this.transaction = null;
            this.cancelRequest = null;
            if ((request != null) && (request.headers.ContainsKey("Call-Id")))
            {
                this.callId = (string)request.headers["Call-ID"][0].value;
            }
            else
            {
                this.callId = stack.newCallId();
            }

            if ((request != null) && (request.headers.ContainsKey("From")))
            {
                this.remoteParty = (Address)request.headers["From"][0].value;
            }
            else
            {
                this.remoteParty = null;
            }

            if ((request != null) && (request.headers.ContainsKey("To")))
            {
                this.localParty = (Address)request.headers["To"][0].value;
            }
            else
            {
                this.localParty = null;
            }
            this.localTag = stack.tag + random.Next(0, 2147483647).ToString();
            this.remoteTag = stack.tag + random.Next(0, 2147483647).ToString();

            if ((request != null) && (request.headers.ContainsKey("Subject")))
            {
                this.subject = (string)request.headers["Subject"][0].value;
            }
            else
            {
                this.subject = "";
            }

            this.maxForwards = 70;
            this.routeSet = new List<Header>();

            this.localTarget = null;
            this.remoteTarget = null;
            this.remoteCandidates = null;
            this.localSeq = 0;
            this.remoteSeq = 0;

            this.contact = new Address(stack.uri.ToString());

            if (this.localParty != null)
            {
                if (this.localParty.uri.user.Length > 0)
                {
                    this.contact.uri.user = this.localParty.uri.user;
                }
            }

            this.autoack = true;
            this.auth = new Dictionary<string, string>();
        }

        public string repr()
        {
            string type = "";
            if (this is Dialog) type = "Dialog";
            if (this is UserAgent) type = "Useragent";
            return String.Format("<{0} call-id={1}>", type, this.callId);
        }

        public Message createRequest(string method, string content = "", string contentType = "")
        {
            this.server = false;
            if (this.remoteParty == null)
            {
                Debug.Assert(false, String.Format("No remoteParty for UAC\n"));
            }
            if (this.localParty == null)
            {
                this.localParty = new Address("\"Anonymous\" <sip:anonymous@anonymous.invalid>");
            }
            SIPURI uri;
            if (this.remoteTarget == null)
            {
                uri = new SIPURI(this.remoteParty.ToString());
            }
            else
            {
                uri = this.remoteParty.uri;
            }
            if (method == "REGISTER")
            {
                uri.user = "";
            }
            if ((method != "ACK") && (method != "CANCEL"))
            {
                this.localSeq = ++this.localSeq;
            }
            Header To = new Header(this.remoteParty.ToString(), "To");
            Header From = new Header(this.localParty.ToString(), "From");
            From.attributes["tag"] = this.localTag;
            Header CSeq = new Header(this.localSeq + " " + method, "CSeq");
            Header CallId = new Header(this.callId, "Call-ID");
            Header MaxForwards = new Header(this.maxForwards.ToString(), "Max-Forwards");
            Header Via = this.stack.createVia();
            Dictionary<string, object> branch_params = new Dictionary<string, object>();
            branch_params.Add("To", To.value);
            branch_params.Add("From", From.value);
            branch_params.Add("CallId", CallId.value);
            branch_params.Add("CSeq", CSeq.number);
            Via.attributes["branch"] = Transaction.createBranch(branch_params, false);
            if (this.localTarget == null)
            {
                this.localTarget = this.stack.uri.dup();
                this.localTarget.user = this.localParty.uri.user;
            }
            Header Contact = new Header(this.localTarget.ToString(), "Contact");
            List<Header> headers = new List<Header>();
            Header[] header_list = { To, From, CSeq, CallId, MaxForwards, Via, Contact };
            foreach (Header h in header_list)
            {
                headers.Add(h);
            }
            if (this.routeSet.Count != 0)
            {
                foreach (Header x in this.routeSet)
                {
                    //Secure parsing missing TODO
                    headers.Add(x);
                }
            }

            //app adds other headers such as Supported, Require and Proxy-Require
            if (contentType.Length > 0)
            {
                headers.Add(new Header(contentType, "Content-Type"));
            }
            Dictionary<string, List<Header>> header_dict = new Dictionary<string, List<Header>>();
            foreach (Header h in headers)
            {
                if (header_dict.ContainsKey(h.name))
                {
                    header_dict[h.name].Add(h);
                }
                else
                {
                    List<Header> temp = new List<Header>();
                    temp.Add(h);
                    header_dict.Add(h.name, temp);
                }
            }
            this.request = Message.createRequest(method, uri, header_dict, content);
            return this.request;
        }

        public Message createRegister(SIPURI aor)
        {
            if (aor != null)
            {
                this.remoteParty = new Address(aor.ToString());
            }
            if (this.localParty == null)
            {
                this.localParty = new Address(this.remoteParty.ToString());
            }
            Message m = this.createRequest("REGISTER");
            m.insertHeader(new Header("", "Authorization"));
            return m;
        }

        public void sendRequest(Message request)
        {
            if ((this.request == null) && (request.method == "REGISTER"))
            {
                if ((this.transaction == null) && (this.transaction.state != "completed") && (this.transaction.state != "terminated"))
                {
                    Debug.Assert(false, String.Format("Cannot re-REGISTER since pending registration\n{0}"));
                }
            }
            this.request = request;

            if (!request.headers.ContainsKey("Route"))
            {
                this.remoteTarget = request.uri;
            }
            SIPURI target = this.remoteTarget;
            if (request.headers.ContainsKey("Route"))
            {
                if (request.headers["Route"].Count > 0)
                {
                    target = ((Address)(request.headers["Route"][0].value)).uri;
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
            this.stack.sending(this, request);
            SIPURI dest = target.dup();
            if (dest.port == 0)
            {
                dest.port = 5060;
            }

            if (Utils.isIPv4(dest.host))
            {
                this.remoteCandidates = new List<SIPURI>();
                this.remoteCandidates.Add(dest);
            }
            if ((this.remoteCandidates == null) || (this.remoteCandidates.Count == 0))
            {
                this.error(null, "Cannot Resolve DNS target");
                return;
            }
            target = this.remoteCandidates.First();
            this.remoteCandidates.RemoveAt(0);
            if (this.request.method != "ACK")
            {
                this.transaction = Transaction.createClient(this.stack, this.app, this.request, this.stack.transport, target.host + ":" + target.port);
            }
            else
            {
                this.stack.send(this.request, target.host + ":" + target.port);
            }
        }

        public void timeout(Transaction transaction)
        {
            if ((transaction != null) && (transaction != this.transaction))
            {
                return;
            }
            this.transaction = null;
            if (this.server == false)
            {
                if ((this.remoteCandidates != null) && (this.remoteCandidates.Count > 0))
                {
                    this.retryNextCandidate();
                }
                else
                {
                    this.receivedResponse(null, Message.createResponse(408, "Request Timeoute", null, null, this.request));
                }
            }
        }

        private void retryNextCandidate()
        {
            if ((this.remoteCandidates == null) || (this.remoteCandidates.Count == 0))
            {
                Debug.Assert(false, String.Format("No more DNS resolved address to try\n"));
            }
            SIPURI target = this.remoteCandidates.First();
            this.remoteCandidates.RemoveAt(0);
            this.request.headers["Via"][0].attributes["branch"] += "A";
            transaction = Transaction.createClient(this.stack, this.app, this.request, this.stack.transport, target.host + ":" + target.port);

        }

        public void error(Transaction t, string error)
        {
            if ((t != null) && t != this.transaction)
            {
                return;
            }
            this.transaction = null;
            if (this.server == false)
            {
                if ((this.remoteCandidates != null) && (this.remoteCandidates.Count > 0))
                {
                    this.retryNextCandidate();
                }
                else
                {
                    this.receivedResponse(null, Message.createResponse(503, "Service unavailable - " + error, null, null, this.request));
                }
            }
        }

        public void receivedResponse(Transaction transaction, Message response)
        {
            if ((transaction != null) && transaction != this.transaction)
            {
                Debug.Assert(false, String.Format("Invalid transaction received {0} != {1}", transaction, this.transaction));
                return;
            }
            if (response.headers["Via"].Count > 1)
            {
                Debug.Assert(false, String.Format("More than one Via header in resposne"));
                return;
            }
            if (response.is1xx())
            {
                if (this.cancelRequest != null)
                {
                    Transaction cancel = Transaction.createClient(this.stack, this.app, this.cancelRequest, transaction.transport, transaction.remote);
                    this.cancelRequest = null;
                }
                else
                {
                    this.stack.receivedResponse(this, response);
                }
            }
            else if ((response.response_code == 401) || (response.response_code == 407))
            {
                if (!this.authenticate(response, this.transaction))
                {
                    this.stack.receivedResponse(this, response);
                }
            }
            else
            {
                if (canCreateDialog(this.request, response))
                {
                    Dialog dialog = Dialog.createClient(this.stack, this.request, response, transaction);
                    this.stack.dialogCreated(dialog, this);
                    this.stack.receivedResponse(dialog, response);
                    if ((this.autoack) && (this.request.method == "INVITE"))
                    {
                        dialog.sendRequest(dialog.createRequest("ACK"));
                    }
                }
                else
                {
                    this.stack.receivedResponse(this, response);
                }
            }
        }


        public static bool canCreateDialog(Message request, Message response)
        {
            return ((response.is2xx()) && ((request.method == "INVITE") || (request.method == "SUBSCRIBE")));
        }

        public void receivedRequest(Transaction transaction, Message request)
        {
            if ((transaction != null) && (this.transaction != null) && (transaction != this.transaction) && (request.method != "CANCEL"))
            {
                Debug.Assert(false, String.Format("Invalid transaction for received request"));
                return;
            }
            this.server = true;
            //if request.method == 'REGISTER':
            //response = transaction.createResponse(405, 'Method not allowed')
            //response.Allow = Header('INVITE, ACK, CANCEL, BYE', 'Allow') # TODO make this configurable
            //transaction.sendResponse(response)
            //return;
            if (request.uri.scheme != "sip")
            {
                transaction.sendResponse(transaction.createResponse(416, "Unsupported URI scheme"));
                return;
            }
            if (!request.headers["To"][0].ToString().Contains("tag"))
            {
                if (this.stack.findOtherTransactions(request, transaction) != null)
                {
                    transaction.sendResponse(transaction.createResponse(482, "Loop Detected - found another transaction"));
                }
            }
            if (request.headers.ContainsKey("Require"))
            {
                if ((request.method != "CANCEL") && (request.method != "ACK"))
                {
                    Message response = transaction.createResponse(420, "Bad extension");
                    response.insertHeader(new Header(request.headers["Require"][0].value.ToString(), "Unsupported"));
                    transaction.sendResponse(response);
                    return;
                }
            }
            if (this.transaction != null)
            {
                this.transaction = transaction;
            }
            if (request.method == "CANCEL")
            {
                Transaction original = this.stack.findTransaction(Transaction.createId(transaction.branch, "Invite"));
                if (original == null)
                {
                    transaction.sendResponse(transaction.createResponse(481, "Cannot find transaction??"));
                    return;
                }
                if (original.state == "proceeding" || original.state == "trying")
                {
                    original.sendResponse(original.createResponse(487, "Request terminated"));
                }
                transaction.sendResponse(transaction.createResponse(200, "OK"));
                // TODO: The To tag must be the same in the two responses
            }
            this.stack.receivedRequest(this, request);
        }

        public void sendResponse(object response, string response_text = "", string content = "", string contentType = "")
        {
            Message response_message = null;
            if (this.request == null)
            {
                Debug.Assert(false, String.Format("Invalid request in sending a response"));
                return;
            }
            if (response is int)
            {
                response_message = this.createResponse((int)(response), response_text, content, contentType);
            }
            else
            {
                response_message = (Message)(response);
            }
            if (createDialog() && UserAgent.canCreateDialog(this.request, response_message))
            {
                if (this.request.headers.ContainsKey("Record-Route"))
                {
                    response_message.headers.Add("Record-Route", this.request.headers["Record-Route"]);
                }

                if (!response_message.headers.ContainsKey("Contact"))
                {
                    Address contact = new Address(this.contact.ToString());
                    if (contact.uri.user.Length == 0)
                    {
                        contact.uri.user = ((SIPURI)this.request.headers["To"][0].value).user;
                        response_message.insertHeader(new Header(contact.ToString(), "Contact"));
                    }

                }
                Dialog dialog = Dialog.createServer(this.stack, this.request, response_message, this.transaction);
                this.stack.dialogCreated(dialog, this);
                this.stack.sending(dialog, response_message);
            }
            else
            {
                this.stack.sending(this, response_message);
            }
            if (this.transaction == null)
            {
                this.stack.send(response_message, response_message.headers["Via"][0].viaUri.hostPort(), null);
            }
            else
            {
                this.transaction.sendResponse(response_message);
            }

        }


        public Message createResponse(int response_code, string response_text, string content = null, string contentType = null)
        {
            if (this.request == null)
            {
                Debug.Assert(false, String.Format("Invalid request in creating a response"));
                return null;
            }
            Message response_message = Message.createResponse(response_code, response_text, null, content, this.request);
            if (contentType != null)
            {
                response_message.insertHeader(new Header(contentType, "Content-Type"));
            }
            if (response_message.response_code != 100 && !response_message.headers["To"][0].ToString().Contains("tag"))
            {
                response_message.headers["To"][0].attributes.Add("tag", this.localTag);
            }
            return response_message;
        }

        public void sendCancel()
        {
            if (this.transaction == null)
            {
                Debug.Assert(false, String.Format("No transaction for sending CANCEL"));
            }
            this.cancelRequest = this.transaction.createCancel();
            if (this.transaction.state != "trying" && this.transaction.state != "calling")
            {
                if (this.transaction.state == "proceeding")
                {
                    Transaction transaction = Transaction.createClient(this.stack, this.app, this.cancelRequest, this.transaction.transport, this.transaction.remote);
                }
                this.cancelRequest = null;
            }

        }

        private bool createDialog()
        {
            throw new NotImplementedException();
        }

        public bool authenticate(Message response, Transaction transaction)
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
                    if (a.attributes["realm"] == h.attributes["realm"] && (a.name == "WWW-Authenticate" && h.name == "Authorization" || a.name == "Proxy-Authenticate" && h.name == "Proxy-Authorization"))
                    {
                        present = true;
                        break;
                    }
                }
                catch (Exception e)
                {
                }
            }
            if (!present && a.attributes.ContainsKey("realm"))
            {
                string[] result = this.stack.authenticate(this, a);
                if (result.Length == 0 || a.attributes.ContainsKey("password") && a.attributes.ContainsKey("hashValue"))
                {
                    return false;
                }
                //string value = createAuthorization(a.value, a.attributes["username"], a.attributes["password"], request.uri.ToString(), this.request.method, this.request.body, this.auth);
                string value = Authenticate.createAuthorization(a.ToString(), result[0], result[1], request.uri.ToString(), request.method, request.body, this.auth);
                if (value.Length > 0)
                {
                    if (a.name == "WWW-Authenticate")
                    {
                        request.insertHeader(new Header(value, "Authorization"), false);
                    }
                    else
                    {
                        request.insertHeader(new Header(value, "Proxy-Authorization"), false);
                    }

                    resend = true;
                }

            }
            if (resend)
            {
                this.localSeq = request.first("CSeq").number + 1;
                request.insertHeader(new Header(this.localSeq.ToString() + " " + request.method, "CSeq"));
                //TODO FIX?
                //request.headers["Via"][0].attributes["branch"] = Transaction.createBranch(request, false);
                this.request = request;
                this.transaction = Transaction.createClient(this.stack, this.app, this.request, transaction.transport, transaction.remote);
                return true;
            }
            else return false;

        }
    }

}