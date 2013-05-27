// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 01-29-2013
// ***********************************************************************
// <copyright file="UserAgent.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIPLib.Utils;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent a user agent. From RFC3261 p.34 – A user agent represents an end system. It contains a user agent client (UAC), which
    /// generates requests, and a user agent server (UAS), which responds to them. A UAC is capable of generating a
    /// request based on some external stimulus (the user clicking a button, or a signal on a PSTN line) and processing
    /// a response. A UAS is capable of receiving a request and generating a response based on user input, external
    /// stimulus, the result of a program execution, or some other mechanism.
    /// </summary>
    public class UserAgent
    {
        /// <summary>
        /// A random number generator
        /// </summary>
        private readonly Random _random = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.UserAgent"/> class.
        /// </summary>
        /// <param name="stack">The SIP stack associated with this UA.</param>
        /// <param name="request">An optional SIP request.</param>
        /// <param name="server">if set to <c>true</c> [act as a user agent server].</param>
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
            if ((request != null) && (request.Headers.ContainsKey("Call-ID")))
            {
                CallID = (string) request.Headers["Call-ID"][0].Value;
            }
            else
            {
                CallID = stack.NewCallId();
            }

            if ((request != null) && (request.Headers.ContainsKey("From")))
            {
                RemoteParty = (Address) request.Headers["From"][0].Value;
            }
            else
            {
                RemoteParty = null;
            }

            if ((request != null) && (request.Headers.ContainsKey("To")))
            {
                LocalParty = (Address) request.Headers["To"][0].Value;
            }
            else
            {
                LocalParty = null;
            }
            LocalTag = stack.Tag + _random.Next(0, 2147483647).ToString();
            RemoteTag = stack.Tag + _random.Next(0, 2147483647).ToString();

            if ((request != null) && (request.Headers.ContainsKey("Subject")))
            {
                Subject = (string) request.Headers["Subject"][0].Value;
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

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public Message Request { get; set; }
        /// <summary>
        /// Gets or sets the stack.
        /// </summary>
        /// <value>The stack.</value>
        public SIPStack Stack { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:SIPLib.SIP.UserAgent"/> is a user agent server.
        /// </summary>
        /// <value><c>true</c> if server; otherwise, <c>false</c>.</value>
        public bool Server { get; set; }
        /// <summary>
        /// Gets or sets the transaction.
        /// </summary>
        /// <value>The transaction.</value>
        public Transaction Transaction { get; set; }
        /// <summary>
        /// Gets or sets the cancel request.
        /// </summary>
        /// <value>The cancel request.</value>
        public Message CancelRequest { get; set; }
        /// <summary>
        /// Gets or sets the call ID.
        /// </summary>
        /// <value>The call ID.</value>
        public string CallID { get; set; }
        /// <summary>
        /// Gets or sets the remote party.
        /// </summary>
        /// <value>The remote party.</value>
        public Address RemoteParty { get; set; }
        /// <summary>
        /// Gets or sets the local party.
        /// </summary>
        /// <value>The local party.</value>
        public Address LocalParty { get; set; }
        /// <summary>
        /// Gets or sets the local tag.
        /// </summary>
        /// <value>The local tag.</value>
        public string LocalTag { get; set; }
        /// <summary>
        /// Gets or sets the remote tag.
        /// </summary>
        /// <value>The remote tag.</value>
        public string RemoteTag { get; set; }
        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        /// <value>The subject.</value>
        public string Subject { get; set; }
        /// <summary>
        /// Gets or sets the route set.
        /// </summary>
        /// <value>The route set.</value>
        public List<Header> RouteSet { get; set; }
        /// <summary>
        /// Gets or sets the max forwards.
        /// </summary>
        /// <value>The max forwards.</value>
        public int MaxForwards { get; set; }

        /// <summary>
        /// Gets or sets the local target.
        /// </summary>
        /// <value>The local target.</value>
        public SIPURI LocalTarget { get; set; }
        /// <summary>
        /// Gets or sets the remote target.
        /// </summary>
        /// <value>The remote target.</value>
        public SIPURI RemoteTarget { get; set; }
        /// <summary>
        /// Gets or sets the remote candidates.
        /// </summary>
        /// <value>The remote candidates.</value>
        public List<SIPURI> RemoteCandidates { get; set; }
        /// <summary>
        /// Gets or sets the remote seq.
        /// </summary>
        /// <value>The remote seq.</value>
        public int RemoteSeq { get; set; }
        /// <summary>
        /// Gets or sets the local seq.
        /// </summary>
        /// <value>The local seq.</value>
        public int LocalSeq { get; set; }

        /// <summary>
        /// Gets or sets the contact.
        /// </summary>
        /// <value>The contact.</value>
        public Address Contact { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:SIPLib.SIP.UserAgent"/> should automatically ack requests.
        /// </summary>
        /// <value><c>true</c> if autoack; otherwise, <c>false</c>.</value>
        public bool Autoack { get; set; }
        /// <summary>
        /// Gets or sets the auth.
        /// </summary>
        /// <value>The auth.</value>
        public Dictionary<string, string> Auth { get; set; }

        /// <summary>
        /// Gets or sets the app.
        /// </summary>
        /// <value>The app.</value>
        public UserAgent App { get; set; }

        /// <summary>
        /// Returns a string representation of this user agent.
        /// </summary>
        /// <returns>System.String.</returns>
        public string Repr()
        {
            string type = "";
            if (this is Dialog) type = "Dialog";
            if (this is UserAgent) type = "Useragent";
            return String.Format("<{0} call-id={1}>", type, CallID);
        }

        /// <summary>
        /// Creates a SIP request.
        /// </summary>
        /// <param name="method">The SIP method (invite etc.)</param>
        /// <param name="content">The SIP body contents.</param>
        /// <param name="contentType">The type of the SIP body.</param>
        /// <returns>Message.</returns>
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
            //TODO: Use Remote Party instead of Remote Target?
            SIPURI uri;
            if (RemoteTarget != null)
            {
                uri = new SIPURI(RemoteTarget.ToString());
            }
            else
            {
                uri = new SIPURI(RemoteParty.ToString());
            }

            if (method == "REGISTER")
            {
                //TODO: Is this right ?
                //uri.User = "";
            }
            if ((method != "ACK") && (method != "CANCEL"))
            {
                LocalSeq = ++LocalSeq;
            }
            //TODO: Use Remote Party instead of Remote Target?
            Header to;
            if (RemoteTarget != null)
            {
                to = new Header(RemoteTarget.ToString(), "To");
            }
            else
            {
                to = new Header(RemoteParty.ToString(), "To");
            }

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
            Header contact = new Header(LocalTarget.ToString(), "Contact");
            Header[] headerList = {to, from, cSeq, callId, maxForwards, via, contact};
            List<Header> headers = headerList.ToList();
            // Check this TODO
            //
            if (RouteSet.Count != 0)
            {
                headers.AddRange(RouteSet);
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

        /// <summary>
        /// Creates a SIP register request.
        /// </summary>
        /// <param name="aor">The address-of-record.</param>
        /// <returns>Message.</returns>
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

        /// <summary>
        /// Sends a specific SIP request.
        /// </summary>
        /// <param name="request">The request.</param>
        public virtual void SendRequest(Message request)
        {
            if ((Request == null) && (request.Method == "REGISTER"))
            {
                if ((Transaction == null) && (Transaction.State != "completed") && (Transaction.State != "terminated"))
                {
                    //TODO This doesn't make sense....
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
                    target = ((Address) (request.Headers["Route"][0].Value)).Uri;
                    if ((target == null) || !target.Parameters.ContainsKey("lr"))
                    {
                        request.Headers["Route"].RemoveAt(0);
                        if (request.Headers["Route"].Count > 0)
                        {
                            request.Headers["Route"].Insert(request.Headers["Route"].Count - 1,
                                                            new Header(request.Uri.ToString(), "Route"));
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

                if (Helpers.IsIPv4(dest.Host))
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
                Transaction = Transaction.CreateClient(Stack, this, Request, Stack.Transport,
                                                       target.Host + ":" + target.Port);
            }
            else
            {
                Stack.Send(Request, target.Host + ":" + target.Port);
            }
        }

        /// <summary>
        /// Triggered on timeout.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
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
                    ReceivedResponse(null, Message.CreateResponse(408, "Request Timeout", null, null, Request));
                }
            }
        }

        /// <summary>
        /// Retries the next candidate.
        /// </summary>
        private void RetryNextCandidate()
        {
            if ((RemoteCandidates == null) || (RemoteCandidates.Count == 0))
            {
                Debug.Assert(false, String.Format("No more DNS resolved address to try\n"));
            }
            SIPURI target = RemoteCandidates.First();
            RemoteCandidates.RemoveAt(0);
            Request.Headers["Via"][0].Attributes["branch"] += "A";
            Transaction = Transaction.CreateClient(Stack, this, Request, Stack.Transport,
                                                   target.Host + ":" + target.Port);
        }

        /// <summary>
        /// Raises an error
        /// </summary>
        /// <param name="t">The transaction.</param>
        /// <param name="error">The error.</param>
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
                    ReceivedResponse(null,
                                     Message.CreateResponse(503, "Service unavailable - " + error, null, null, Request));
                }
            }
        }

        /// <summary>
        /// Virtual function for receiving a response.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="response">The response.</param>
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
                    Transaction cancel = Transaction.CreateClient(Stack, this, CancelRequest, transaction.Transport,
                                                                  transaction.Remote);
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
                        // TODO: Check dialog RouteSet creation (the manual hack below works)
                        //if (response.Headers.ContainsKey("Record-Route"))
                        //{
                        //    ack.Headers["Route"] = response.Headers["Record-Route"];
                        //    ack.Headers["Route"].Reverse();
                        //foreach (Header h in Headers["Route"])
                        //{
                        //    h.Name = "Route";
                        //}
                        //}
                        dialog.SendRequest(ack);
                    }
                }
                else
                {
                    Stack.ReceivedResponse(this, response);
                }
            }
        }


        /// <summary>
        /// Determines whether this instance [can create a dialog] for the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <returns><c>true</c> if this instance [can create a dialog] for the specified request; otherwise, <c>false</c>.</returns>
        public static bool CanCreateDialog(Message request, Message response)
        {
            return ((response.Is2XX()) && ((request.Method == "INVITE") || (request.Method == "SUBSCRIBE")));
        }

        /// <summary>
        /// Virtual function to receive a request.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="request">The request.</param>
        public virtual void ReceivedRequest(Transaction transaction, Message request)
        {
            if ((transaction != null) && (Transaction != null) && (transaction != Transaction) &&
                (request.Method != "CANCEL"))
            {
                Transaction t = Transaction;
                Transaction t2 = transaction;
                bool test = Transaction.TEquals(t, request, t2);
                Console.WriteLine("Invalid transaction for received request {0} != {1}, {2}", transaction, Transaction,
                                  test);
                // TODO: Re-enable this
                Debug.Assert(false,
                             String.Format("Invalid transaction for received request {0} != {1}", transaction,
                                           Transaction));
                return;
            }
            Server = true;
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
                if (Stack.FindOtherTransactions(request, transaction) != null)
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
                Transaction original = Stack.FindTransaction(Transaction.CreateId(transaction.Branch, "Invite"));
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

        /// <summary>
        /// Sends a SIP response.
        /// </summary>
        /// <param name="response">The SIP response (either a response code or SIP message).</param>
        /// <param name="responseText">Optional response text.</param>
        /// <param name="content">Optional content.</param>
        /// <param name="contentType">Optional type of the SIP body contents.</param>
        /// <param name="createDialog">if set to <c>true</c> [can create dialog].</param>
        public virtual void SendResponse(object response, string responseText = "", string content = "",
                                         string contentType = "", bool createDialog = true)
        {
            Message responseMessage;
            if (Request == null)
            {
                Debug.Assert(false, String.Format("Invalid request in sending a response"));
                return;
            }
            if (response is int)
            {
                responseMessage = CreateResponse((int) (response), responseText, content, contentType);
            }
            else
            {
                responseMessage = (Message) (response);
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
                    if (contact.Uri.User.Length != 0)
                    {
                        contact.Uri.User = ((Address) Request.First("To").Value).Uri.User;
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


        /// <summary>
        /// Creates a SIP response given the response code and responseText
        /// </summary>
        /// <param name="responseCode">The response code.</param>
        /// <param name="responseText">The response text.</param>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <returns>Message.</returns>
        public virtual Message CreateResponse(int responseCode, string responseText, string content = null,
                                              string contentType = null)
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

        /// <summary>
        /// Sends a SIP CANCEL request.
        /// </summary>
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
                    Transaction transaction = Transaction.CreateClient(Stack, this, CancelRequest, Transaction.Transport,
                                                                       Transaction.Remote);
                }
                CancelRequest = null;
            }
        }

        /// <summary>
        /// Authenticates the specified response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="transaction">The transaction.</param>
        /// <returns><c>true</c> if attempt to authenticate is created, <c>false</c> otherwise</returns>
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
                    if (a.Attributes["realm"] == h.Attributes["realm"] &&
                        (a.Name == "WWW-Authenticate" && h.Name == "Authorization" ||
                         a.Name == "Proxy-Authenticate" && h.Name == "Proxy-Authorization"))
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
                string value = SIP.Authenticate.CreateAuthorization(a.ToString(), result[0], result[1],
                                                                    request.Uri.ToString(), request.Method, request.Body,
                                                                    Auth);
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

        /// <summary>
        /// Virtual function to pass received message to real function.
        /// </summary>
        /// <param name="t">The transaction.</param>
        /// <param name="message">The SIP message.</param>
        /// <param name="sIPStack">The SIP stack.</param>
        internal virtual void ReceivedRequest(Transaction t, Message message, SIPStack SIPStack)
        {
            ReceivedRequest(t, message);
        }
    }
}