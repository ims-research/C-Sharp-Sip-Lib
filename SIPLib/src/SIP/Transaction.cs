// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="Transaction.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SIPLib.Utils;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This is the base class used to represent SIP transactions.
    /// </summary>
    public abstract class Transaction
    {
        /// <summary>
        /// Private variable holding the current  _state of the transaction
        /// </summary>
        private string _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.Transaction" /> class.
        /// </summary>
        /// <param name="app">The associated useragent / application</param>
        protected Transaction(UserAgent app)
        {
            Timers = new Dictionary<string, Timer>();
            Timer = new Timer(App);
            App = app;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.Transaction" /> class.
        /// </summary>
        /// <param name="server">if set to <c>true</c> [server].</param>
        protected Transaction(bool server)
        {
            Timers = new Dictionary<string, Timer>();
            Server = server;
            Timer = new Timer(App);
        }

        /// <summary>
        /// Gets or sets the branch.
        /// </summary>
        /// <value>The branch.</value>
        public string Branch { get; set; }
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        public string ID { get; set; }
        /// <summary>
        /// Gets or sets the stack.
        /// </summary>
        /// <value>The stack.</value>
        public SIPStack Stack { get; set; }
        /// <summary>
        /// Gets or sets the associated useragent / application.
        /// </summary>
        /// <value>The useragent / application.</value>
        public UserAgent App { get; set; }
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public Message Request { get; set; }
        /// <summary>
        /// Gets or sets the transport.
        /// </summary>
        /// <value>The transport.</value>
        public TransportInfo Transport { get; set; }
        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>The tag.</value>
        public string Tag { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:SIPLib.SIP.Transaction" /> is a server transaction.
        /// </summary>
        /// <value><c>true</c> if server transaction; otherwise, <c>false</c>.</value>
        public bool Server { get; set; }
        /// <summary>
        /// Gets or sets the timers.
        /// </summary>
        /// <value>The timers.</value>
        public Dictionary<string, Timer> Timers { get; set; }
        /// <summary>
        /// Gets or sets the timer.
        /// </summary>
        /// <value>The timer.</value>
        public Timer Timer { get; set; }
        /// <summary>
        /// Gets or sets the remote.
        /// </summary>
        /// <value>The remote.</value>
        public string Remote { get; set; }
        /// <summary>
        /// Gets or sets the last response.
        /// </summary>
        /// <value>The last response.</value>
        public Message LastResponse { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public string State
        {
            get { return _state; }
            set
            {
                _state = value;
                if (_state == "terminating")
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Gets the relevant transaction headers ("To", "From", "CSeq", "Call-ID").
        /// </summary>
        /// <value>The headers.</value>
        public Dictionary<string, List<Header>> Headers
        {
            get
            {
                return
                    (Dictionary<string, List<Header>>)
                    Request.Headers.Where(p => p.Key == "To" || p.Key == "From" || p.Key == "CSeq" || p.Key == "Call-ID");
            }
        }

        /// <summary>
        /// Creates the transaction branch ID.
        /// </summary>
        /// <param name="request">The request (can be a SIP message or a Dictionary with the necessary headers).</param>
        /// <param name="server">if set to <c>true</c> [server].</param>
        /// <returns>System.String.</returns>
        public static string CreateBranch(object request, bool server)
        {
            string to = "", from = "", callId = "", cSeq = "";
            if (request is Message)
            {
                Message requestMessage = (Message) (request);
                to = requestMessage.First("To").Value.ToString();
                from = requestMessage.First("From").Value.ToString();
                callId = requestMessage.First("Call-ID").Value.ToString();
                cSeq = requestMessage.First("CSeq").Number.ToString();
            }
            else if (request is Dictionary<string, object>)
            {
                Dictionary<string, object> dict = (Dictionary<string, object>) request;
                object[] headers = dict.Values.ToArray();
                to = headers[0].ToString();
                from = headers[1].ToString();
                callId = headers[2].ToString();
                cSeq = headers[3].ToString();
            }
            string data = to.ToLower() + "|" + from.ToLower() + "|" + callId.ToLower() + "|" + cSeq.ToLower() + "|" +
                          server.ToString();
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = Helpers.GetMd5Hash(md5Hash, data);
            }
            //TODO fix this ? replace data with hash ?
            data = Helpers.Base64Encode(data).Replace('=', '.');
            return "z9hG4bK" + data;
        }

        /// <summary>
        /// Creates the transaction ID.
        /// </summary>
        /// <param name="branch">The branch.</param>
        /// <param name="method">The SIP method (INVITE etc).</param>
        /// <returns>System.String.</returns>
        public static string CreateId(string branch, string method)
        {
            if (method != "ACK" && method != "CANCEL")
            {
                return branch;
            }
            return branch + "|" + method;
        }

        /// <summary>
        /// Creates a server transaction
        /// </summary>
        /// <param name="stack">The SIP stack to use.</param>
        /// <param name="app">The associated useragent / application.</param>
        /// <param name="request">The SIP request.</param>
        /// <param name="transport">A TransportInfo object representing transmission medium.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="start">Not used.</param>
        /// <returns>Transaction.</returns>
        public static Transaction CreateServer(SIPStack stack, UserAgent app, Message request, TransportInfo transport,
                                               string tag, Boolean start = true)
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
                ((InviteServerTransaction) t).Start();
            }
            else
            {
                ((ServerTransaction) t).Start();
            }
            return t;
        }

        /// <summary>
        /// Creates a client transaction
        /// </summary>
        /// <param name="stack">The SIP stack to use.</param>
        /// <param name="app">The associated useragent / application.</param>
        /// <param name="request">The SIP request.</param>
        /// <param name="transport">A TransportInfo object representing transmission medium.</param>
        /// <param name="remote">The remote.</param>
        /// <returns>Transaction.</returns>
        public static Transaction CreateClient(SIPStack stack, UserAgent app, Message request, TransportInfo transport,
                                               string remote)
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
                ((InviteClientTransaction) t).Start();
            }
            else
            {
                ((ClientTransaction) t).Start();
            }
            return t;
        }

        //private void start()
        //{
        //    //TODO Transaction start ?
        //    //throw new NotImplementedException();
        //}

        /// <summary>
        /// Helper function to check whether two Transactions are equal.
        /// </summary>
        /// <param name="t1">The first transaction.</param>
        /// <param name="r">A SIP message to help the comparison.</param>
        /// <param name="t2">The second transaction.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public static bool TEquals(Transaction t1, Message r, Transaction t2)
        {
            Message t = t1.Request;
            Address requestTo = (Address) (r.First("To").Value);
            Address t1To = (Address) (t.First("To").Value);

            Address requestFrom = (Address) (r.First("To").Value);
            Address t1From = (Address) (t.First("To").Value);

            bool a = (String.Compare(requestTo.Uri.ToString(), t1To.Uri.ToString()) == 0);
            a = a && (String.Compare(requestFrom.Uri.ToString(), t1From.Uri.ToString()) == 0);

            a = a && (r.First("Call-ID").Value.ToString() == t.First("Call-ID").Value.ToString());
            a = a && (r.First("CSeq").Number.ToString() == t.First("CSeq").Number.ToString());

            a = a && (r.First("From").Attributes["tag"] == t.First("From").Attributes["tag"]);
            a = a && (t2.Server == t1.Server);
            return a;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
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

        /// <summary>
        /// Creates a SIP ACK message.
        /// </summary>
        /// <returns>Message if possible, else null</returns>
        public virtual Message CreateAck()
        {
            if (Request != null && !Server)
            {
                return Message.CreateRequest("ACK", Request.Uri, Headers);
            }
            return null;
        }

        /// <summary>
        /// Creates a SIP CANCEL request.
        /// </summary>
        /// <returns>Message.</returns>
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

        /// <summary>
        /// Creates a SIP response.
        /// </summary>
        /// <param name="responseCode">The response code.</param>
        /// <param name="responseText">The response text.</param>
        /// <returns>The created SIP response.</returns>
        public virtual Message CreateResponse(int responseCode, string responseText)
        {
            Message m = null;
            if (Request != null && Server)
            {
                m = Message.CreateResponse(responseCode, responseText, null, null, Request);
                if (responseCode != 100 && !m.Headers["To"][0].Attributes.ContainsKey("tag"))
                {
                    m.Headers["To"][0].Attributes.Add("tag", Tag);
                }
            }
            return m;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="timeout">The timeout.</param>
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

        /// <summary>
        /// Triggered on expiration of the specified timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        public virtual void Timedout(Timer timer)
        {
            if (timer.Running)
            {
                timer.Stop();
            }
            var found = Timers.Where(p => p.Value == timer);
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

        /// <summary>
        /// Triggered on expiration of the specified timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="p">The p.</param>
        /// <exception cref="System.NotImplementedException">Timeout in Transaction is not implemented</exception>
        private void Timeout(Timer timer, int p)
        {
            throw new NotImplementedException("Timeout in Transaction is not implemented");
        }

        /// <summary>
        /// Stops the timers.
        /// </summary>
        public virtual void StopTimers()
        {
            foreach (Timer t in Timers.Values)
            {
                t.Stop();
            }
            Timers = new Dictionary<string, Timer>();
        }

        /// <summary>
        /// Sends a SIP response. Not implemented in this abstract class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="System.NotImplementedException">sendResponse in Transaction is not implemented</exception>
        public virtual void SendResponse(Message message)
        {
            throw new NotImplementedException("sendResponse in Transaction is not implemented");
        }

        /// <summary>
        /// Receives a SIP request. Not implemented in this abstract class.
        /// </summary>
        /// <param name="receivedRequest">The received request.</param>
        /// <exception cref="System.NotImplementedException">receivedRequest in Transaction is not implemented</exception>
        public virtual void ReceivedRequest(Message receivedRequest)
        {
            throw new NotImplementedException("receivedRequest in Transaction is not implemented");
        }

        /// <summary>
        /// Receives a SIP response. Not implemented in this abstract class.
        /// </summary>
        /// <param name="r">The r.</param>
        /// <exception cref="System.NotImplementedException">receivedResponse in Transaction is not implemented</exception>
        public virtual void ReceivedResponse(Message r)
        {
            throw new NotImplementedException("receivedResponse in Transaction is not implemented");
        }

        /// <summary>
        /// Creates a proxy branch.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="server">if set to <c>true</c> [server].</param>
        /// <returns>System.String.</returns>
        internal static string createProxyBranch(Message request, bool server)
        {
            Header via = request.First("Via");
            if (via != null && via.Attributes.ContainsKey("branch"))
            {
                string data = via.Attributes["branch"];
                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = Helpers.GetMd5Hash(md5Hash, data);
                }
                //TODO fix this ? replace data with hash ?
                data = Helpers.Base64Encode(data).Replace('=', '.');
                return "z9hG4bK" + data;
            }
            else
            {
                return CreateBranch(request, server);
            }
        }
    }
}