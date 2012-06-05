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
            Stack = stack;
            //this.app = stack.app;
            Request = request;
            if (server)
            {
                Server = true;
            }
            else
            {
                Server = (request == null);
            }
            Transaction = null;
            CancelRequest = null;
            if ((request != null) && (request.Headers.ContainsKey("Call-Id")))
            {
                CallID = (string)request.Headers["Call-ID"][0].Value;
            }
            else
            {
                CallID = stack.NewCallId();
            }

            if ((request != null) && (request.Headers.ContainsKey("From")))
            {
                RemoteParty = (Address)request.Headers["From"][0].Value;
            }
            else
            {
                RemoteParty = null;
            }

            if ((request != null) && (request.Headers.ContainsKey("To")))
            {
                LocalParty = (Address)request.Headers["To"][0].Value;
            }
            else
            {
                LocalParty = null;
            }
            LocalTag = stack.Tag + _random.Next(0, 2147483647).ToString();
            RemoteTag = stack.Tag + _random.Next(0, 2147483647).ToString();

            if ((request != null) && (request.Headers.ContainsKey("Subject")))
            {
                Subject = (string)request.Headers["Subject"][0].Value;
            }
            else
            {
                Subject = "";
            }

            MaxForwards = 70;
            RouteSet = new List<Header>();

            LocalTarget = null;
            RemoteTarget = null;
            RemoteCandidates = null;
            LocalSeq = 0;
            RemoteSeq = 0;

            Contact = new Address(stack.Uri.ToString());

            if (LocalParty != null)
            {
                if (LocalParty.Uri.User.Length > 0)
                {
                    Contact.Uri.User = LocalParty.Uri.User;
                }
            }

            Autoack = true;
            Auth = new Dictionary<string, string>();
        }

        public string Repr()
        {
            string type = "";
            if (this is Dialog) type = "Dialog";
            if (this is UserAgent) type = "Useragent";
            return String.Format("<{0} call-id={1}>", type, CallID);
        }

        public virtual Message CreateRequest(string method, string content = "", string contentType = "")
        {
            Server = false;
            if (RemoteParty == null)
            {
                Debug.Assert(false, String.Format("No remoteParty for UAC\n"));
            }
            if (LocalParty == null)
            {
                LocalParty = new Address("\"Anonymous\" <sip:anonymous@anonymous.invalid>");
            }
            SIPURI uri = RemoteTarget == null ? new SIPURI(RemoteParty.ToString()) : RemoteParty.Uri;
            if (method == "REGISTER")
            {
                uri.User = "";
            }
            if ((method != "ACK") && (method != "CANCEL"))
            {
                LocalSeq = ++LocalSeq;
            }
            Header to = new Header(RemoteParty.ToString(), "To");
            Header from = new Header(LocalParty.ToString(), "From");
            from.Attributes["tag"] = LocalTag;
            Header cSeq = new Header(LocalSeq + " " + method, "CSeq");
            Header callId = new Header(CallID, "Call-ID");
            Header maxForwards = new Header(MaxForwards.ToString(), "Max-Forwards");
            Header via = Stack.CreateVia();
            Dictionary<string, object> branchParams = new Dictionary<string, object>
                                                          {
                                                              {"To", to.Value},
                                                              {"From", @from.Value},
                                                              {"CallId", callId.Value},
                                                              {"CSeq", cSeq.Number}
                                                          };
            via.Attributes["branch"] = Transaction.CreateBranch(branchParams, false);
            if (LocalTarget == null)
            {
                LocalTarget = Stack.Uri.Dup();
                LocalTarget.User = LocalParty.Uri.User;
            }
            Header contact = new Header(this.LocalTarget.ToString(), "Contact");
            Header[] headerList = { to, from, cSeq, callId, maxForwards, via, contact };
            List<Header> headers = headerList.ToList();
            // Check this TODO
            //
            if (this.RouteSet.Count != 0)
            {
                headers.AddRange(this.RouteSet);
            }

            //app adds other headers such as Supported, Require and Proxy-Require
            if (!string.IsNullOrEmpty(contentType))
            {
                headers.Add(new Header(contentType, "Content-Type"));
            }
            Dictionary<string, List<Header>> headerDict = new Dictionary<string, List<Header>>();
            foreach (Header h in headers)
            {
                if (headerDict.ContainsKey(h.Name))
                {
                    headerDict[h.Name].Add(h);
                }
                else
                {
                    List<Header> temp = new List<Header> {h};
                    headerDict.Add(h.Name, temp);
                }
            }
            Request = Message.CreateRequest(method, uri, headerDict, content);
            return Request;
        }

        public virtual Message CreateRegister(SIPURI aor)
        {
            if (aor != null)
            {
                RemoteParty = new Address(aor.ToString());
            }
            if (LocalParty == null)
            {
                LocalParty = new Address(RemoteParty.ToString());
            }
            Message m = CreateRequest("REGISTER");
            m.InsertHeader(new Header("", "Authorization"));
            return m;
        }

        public virtual void SendRequest(Message request)
        {
            if ((Request == null) && (request.Method == "REGISTER"))
            {
                if ((Transaction == null) && (Transaction.State != "completed") && (Transaction.State != "terminated"))
                { //TODO This doesn't make sense....
                    Debug.Assert(false, String.Format("Cannot re-REGISTER since pending registration\n"));
                }
            }
            Request = request;

            if (!request.Headers.ContainsKey("Route"))
            {
                RemoteTarget = request.Uri;
            }
            SIPURI target = RemoteTarget;
            if (request.Headers.ContainsKey("Route"))
            {
                if (request.Headers["Route"].Count > 0)
                {
                    target = ((Address)(request.Headers["Route"][0].Value)).Uri;
                    if ((target == null) || !target.Parameters.ContainsKey("lr"))
                    {
                        request.Headers["Route"].RemoveAt(0);
                        if (request.Headers["Route"].Count > 0)
                        {
                            request.Headers["Route"].Insert(request.Headers["Route"].Count - 1, new Header(request.Uri.ToString(), "Route"));
                        }
                        request.Uri = target;
                    }
                }

            }
            // TODO: remove any Route Header in REGISTER request
            Stack.Sending(this, request);
            if (target != null)
            {
                SIPURI dest = target.Dup();
                if (dest.Port == 0)
                {
                    dest.Port = 5060;
                }

                if (Utils.IsIPv4(dest.Host))
                {
                    RemoteCandidates = new List<SIPURI> {dest};
                }
            }
            if ((RemoteCandidates == null) || (RemoteCandidates.Count == 0))
            {
                Error(null, "Cannot Resolve DNS target");
                return;
            }
            target = RemoteCandidates.First();
            RemoteCandidates.RemoveAt(0);
            if (Request.Method != "ACK")
            {
                Transaction = Transaction.CreateClient(Stack, this, Request, Stack.Transport, target.Host + ":" + target.Port);
            }
            else
            {
                Stack.Send(Request, target.Host + ":" + target.Port);
            }
        }

        public virtual void Timeout(Transaction transaction)
        {
            if ((transaction != null) && (transaction != Transaction))
            {
                return;
            }
            Transaction = null;
            if (Server == false)
            {
                if ((RemoteCandidates != null) && (RemoteCandidates.Count > 0))
                {
                    RetryNextCandidate();
                }
                else
                {
                    ReceivedResponse(null, Message.CreateResponse(408, "Request Timeoute", null, null, Request));
                }
            }
        }

        private void RetryNextCandidate()
        {
            if ((RemoteCandidates == null) || (RemoteCandidates.Count == 0))
            {
                Debug.Assert(false, String.Format("No more DNS resolved address to try\n"));
            }
            SIPURI target = RemoteCandidates.First();
            RemoteCandidates.RemoveAt(0);
            Request.Headers["Via"][0].Attributes["branch"] += "A";
            Transaction = Transaction.CreateClient(Stack, this, Request, Stack.Transport, target.Host + ":" + target.Port);

        }

        public virtual void Error(Transaction t, string error)
        {
            if ((t != null) && t != Transaction)
            {
                return;
            }
            Transaction = null;
            if (Server == false)
            {
                if ((RemoteCandidates != null) && (RemoteCandidates.Count > 0))
                {
                    RetryNextCandidate();
                }
                else
                {
                    ReceivedResponse(null, Message.CreateResponse(503, "Service unavailable - " + error, null, null, Request));
                }
            }
        }

        public virtual void ReceivedResponse(Transaction transaction, Message response)
        {
            if ((transaction != null) && transaction != Transaction)
            {
                Debug.Assert(false, String.Format("Invalid transaction received {0} != {1}", transaction, Transaction));
                return;
            }
            if (response.Headers["Via"].Count > 1)
            {
                Debug.Assert(false, String.Format("More than one Via header in resposne"));
                return;
            }
            if (response.Is1XX())
            {
                if (CancelRequest != null)
                {
                    Transaction cancel = Transaction.CreateClient(Stack, this, CancelRequest, transaction.Transport, transaction.Remote);
                    CancelRequest = null;
                }
                else
                {
                    Stack.ReceivedResponse(this, response);
                }
            }
            else if ((response.ResponseCode == 401) || (response.ResponseCode == 407))
            {
                if (!Authenticate(response, Transaction))
                {
                    Stack.ReceivedResponse(this, response);
                }
            }
            else
            {
                if (CanCreateDialog(Request, response))
                {
                    Dialog dialog = Dialog.CreateClient(Stack, Request, response, transaction);
                    dialog.App = this;
                    Stack.DialogCreated(dialog, this);
                    Stack.ReceivedResponse(dialog, response);
                    if ((Autoack) && (Request.Method == "INVITE"))
                    {
                        Message ack = dialog.CreateRequest("ACK");
                        dialog.SendRequest(ack);
                    }
                }
                else
                {
                    Stack.ReceivedResponse(this, response);
                }
            }
        }


        public static bool CanCreateDialog(Message request, Message response)
        {

            return ((response.Is2XX()) && ((request.Method == "INVITE") || (request.Method == "SUBSCRIBE")));
        }

        public virtual void ReceivedRequest(Transaction transaction, Message request)
        {
            if ((transaction != null) && (Transaction != null) && (transaction != Transaction) && (request.Method != "CANCEL"))
            {
                Transaction t = Transaction;
                Transaction t2 = transaction;
                bool test = Transaction.TEquals(t,request,t2);
                System.Console.WriteLine("Invalid transaction for received request {0} != {1}, {2}", transaction, Transaction,test);
                // TODO: Re-enable this
                Debug.Assert(false, String.Format("Invalid transaction for received request {0} != {1}", transaction, Transaction));
                return;
            }
            this.Server = true;
            //if request.method == 'REGISTER':
            //response = transaction.createResponse(405, 'Method not allowed')
            //response.Allow = Header('INVITE, ACK, CANCEL, BYE', 'Allow') # TODO make this configurable
            //transaction.sendResponse(response)
            //return;
            if (request.Uri.Scheme != "sip")
            {
                transaction.SendResponse(transaction.CreateResponse(416, "Unsupported URI scheme"));
                return;
            }
            if (!request.Headers["To"][0].ToString().Contains("tag"))
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
            if (Transaction != null)
            {
                Transaction = transaction;
            }
            if (request.Method == "CANCEL")
            {
                Transaction original = this.Stack.FindTransaction(Transaction.CreateId(transaction.Branch, "Invite"));
                if (original == null)
                {
                    transaction.SendResponse(transaction.CreateResponse(481, "Cannot find transaction??"));
                    return;
                }
                if (original.State == "proceeding" || original.State == "trying")
                {
                    original.SendResponse(original.CreateResponse(487, "Request terminated"));
                }
                transaction.SendResponse(transaction.CreateResponse(200, "OK"));
                // TODO: The To tag must be the same in the two responses
            }
            Stack.ReceivedRequest(this, request);
        }

        public virtual void SendResponse(object response, string responseText = "", string content = "", string contentType = "", bool createDialog = true)
        {
            Message responseMessage;
            if (Request == null)
            {
                Debug.Assert(false, String.Format("Invalid request in sending a response"));
                return;
            }
            if (response is int)
            {
                responseMessage = CreateResponse((int)(response), responseText, content, contentType);
            }
            else
            {
                responseMessage = (Message)(response);
            }
            if (createDialog && CanCreateDialog(Request, responseMessage))
            {
                if (Request.Headers.ContainsKey("Record-Route"))
                {
                    responseMessage.Headers.Add("Record-Route", Request.Headers["Record-Route"]);
                }

                if (!responseMessage.Headers.ContainsKey("Contact"))
                {
                    Address contact = new Address(Contact.ToString());
                    if (contact.Uri.User.Length == 0)
                    {
                        contact.Uri.User = ((Address)Request.First("To").Value).Uri.User;
                        responseMessage.InsertHeader(new Header(contact.ToString(), "Contact"));
                    }

                }
                Dialog dialog = Dialog.CreateServer(Stack, Request, responseMessage, Transaction);
                Stack.DialogCreated(dialog, this);
                Stack.Sending(dialog, responseMessage);
            }
            else
            {
                Stack.Sending(this, responseMessage);
            }
            if (Transaction == null)
            {
                Stack.Send(responseMessage, responseMessage.Headers["Via"][0].ViaUri.HostPort());
            }
            else
            {
                Transaction.SendResponse(responseMessage);
            }

        }


        public virtual Message CreateResponse(int responseCode, string responseText, string content = null, string contentType = null)
        {
            if (Request == null)
            {
                Debug.Assert(false, String.Format("Invalid request in creating a response"));
                return null;
            }
            Message responseMessage = Message.CreateResponse(responseCode, responseText, null, content, Request);
            if (contentType != null)
            {
                responseMessage.InsertHeader(new Header(contentType, "Content-Type"));
            }
            if (responseMessage.ResponseCode != 100 && !responseMessage.Headers["To"][0].ToString().Contains("tag"))
            {
                responseMessage.Headers["To"][0].Attributes.Add("tag", LocalTag);
            }
            return responseMessage;
        }

        public virtual void SendCancel()
        {
            if (Transaction == null)
            {
                Debug.Assert(false, String.Format("No transaction for sending CANCEL"));
            }
            CancelRequest = Transaction.CreateCancel();
            if (Transaction.State != "trying" && Transaction.State != "calling")
            {
                if (Transaction.State == "proceeding")
                {
                    Transaction transaction = Transaction.CreateClient(this.Stack, this, this.CancelRequest, this.Transaction.Transport, this.Transaction.Remote);
                }
                CancelRequest = null;
            }

        }

        public virtual bool Authenticate(Message response, Transaction transaction)
        {
            Header a;
            if (response.Headers.ContainsKey("WWW-Authenticate") || response.Headers.ContainsKey("Proxy-Authenticate"))
            {
                a = response.Headers["WWW-Authenticate"][0];
            }
            else if (response.Headers.ContainsKey("Proxy-Authenticate"))
            {
                a = response.Headers["Proxy-Authenticate"][0];
            }
            else return false;
            Message request = new Message(transaction.Request.ToString());
            bool resend = false, present = false;
            // foreach (Header h in request.headers["Authorization"].Concat(request.headers["Proxy-Authorization"]))
            foreach (Header h in request.Headers["Authorization"])
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
                string[] result = Stack.Authenticate(this, a);
                if (result.Length == 0 || a.Attributes.ContainsKey("password") && a.Attributes.ContainsKey("hashValue"))
                {
                    return false;
                }
                //string value = createAuthorization(a.value, a.attributes["username"], a.attributes["password"], request.uri.ToString(), this.request.method, this.request.body, this.auth);
                string value = SIP.Authenticate.CreateAuthorization(a.ToString(), result[0], result[1], request.Uri.ToString(), request.Method, request.Body, Auth);
                if (value.Length > 0)
                {
                    request.InsertHeader(
                        a.Name == "WWW-Authenticate"
                            ? new Header(value, "Authorization")
                            : new Header(value, "Proxy-Authorization"));

                    resend = true;
                }
            }
            if (resend)
            {
                LocalSeq = request.First("CSeq").Number + 1;
                request.InsertHeader(new Header(LocalSeq.ToString() + " " + request.Method, "CSeq"));
                //TODO FIX?
                //request.headers["Via"][0].attributes["branch"] = Transaction.createBranch(request, false);
                Request = request;
                Transaction = Transaction.CreateClient(Stack, this, Request, transaction.Transport, transaction.Remote);
                return true;
            }
            return false;
        }
        internal virtual void ReceivedRequest(Transaction t, Message message, SIPStack sIPStack)
        {
            ReceivedRequest(t, message);
        }
    }

}