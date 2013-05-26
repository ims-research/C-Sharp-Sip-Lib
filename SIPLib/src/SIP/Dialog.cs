// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="Dialog.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// Class Dialog. This class represents a SIP Dialog.
    /// </summary>
    public class Dialog : UserAgent
    {
        /// <summary>
        /// Private dialog _id
        /// </summary>
        private string _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.Dialog"/> class.
        /// </summary>
        /// <param name="stack">The SIP stack currently being used.</param>
        /// <param name="request">The request.</param>
        /// <param name="server">If set to <c>true</c> then set the relevant useragent properties for a server.</param>
        /// <param name="transaction">The transaction.</param>
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

        /// <summary>
        /// Gets or sets the servers.
        /// </summary>
        /// <value>The servers.</value>
        public List<Transaction> Servers { get; set; }
        /// <summary>
        /// Gets or sets the clients.
        /// </summary>
        /// <value>The clients.</value>
        public List<Transaction> Clients { get; set; }
        /// <summary>
        /// Gets or sets the invite record route.
        /// </summary>
        /// <value>The invite record route.</value>
        private List<Header> InviteRecordRoute { get; set; }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
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
            set { _id = value; }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
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


        /// <summary>
        /// Creates the server side dialog.
        /// </summary>
        /// <param name="stack">The stack.</param>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <param name="transaction">The transaction.</param>
        /// <returns>Dialog.</returns>
        public static Dialog CreateServer(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, true) {Request = request};
            if (request.Headers.ContainsKey("Record-Route"))
            {
                d.RouteSet = request.Headers["Record-Route"];
                foreach (Header h in d.RouteSet)
                {
                    h.Name = "Route";
                }
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
            d.RemoteTarget = new SIPURI(((Address) (request.First("Contact").Value)).Uri.ToString());
            // TODO: retransmission timer for 2xx in UAC
            stack.Dialogs[d.CallID] = d;
            return d;
        }

        /// <summary>
        /// Creates the client side dialog.
        /// </summary>
        /// <param name="stack">The stack.</param>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <param name="transaction">The transaction.</param>
        /// <returns>Dialog.</returns>
        public static Dialog CreateClient(SIPStack stack, Message request, Message response, Transaction transaction)
        {
            Dialog d = new Dialog(stack, request, false) {Request = request};
            if (response.Headers.ContainsKey("Record-Route"))
            {
                d.RouteSet = response.Headers["Record-Route"];
                d.RouteSet.Reverse();
                foreach (Header h in d.RouteSet)
                {
                    h.Name = "Route";
                }
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
                d.RemoteTarget = new SIPURI(((Address) (response.First("Contact").Value)).Uri.ToString());
            }
            else d.RemoteTarget = new SIPURI(((Address) (response.First("To").Value)).Uri.ToString());
            try
            {
                stack.Dialogs[d.CallID] = d;
            }
            catch (Exception)
            {
                Debug.Assert(false, "Error assiging callID to stack dialog list");
            }
            return d;
        }

        /// <summary>
        /// Extracts the call ID from a message.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns>System.String.</returns>
        public static string ExtractID(Message m)
        {
            // TODO fix this and use more than just call id ?
            string temp = m.First("Call-ID").Value.ToString(); // +"|";
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

        /// <summary>
        /// Creates a SIP request with the necessary Dialog parameters.
        /// </summary>
        /// <param name="method">The SIP method to use.</param>
        /// <param name="content">The SIP body contents.</param>
        /// <param name="contentType">The type of the SIP body.</param>
        /// <returns>Message.</returns>
        public override Message CreateRequest(string method, string content = null, string contentType = null)
        {
            Message request = base.CreateRequest(method, content, contentType);
            if (RemoteTag != "")
            {
                request.Headers["To"][0].Attributes["tag"] = RemoteTag;
            }
            if (RouteSet != null && RouteSet.Count > 0 && !RouteSet[0].Value.ToString().Contains("lr"))
            {
                //TODO: Check this RouteSet
                request.Uri = new SIPURI((string) (RouteSet[0].Value));
                request.Uri.Parameters.Remove("lr");
            }
            //if (RouteSet.Count > 0)
            //{
            //    request.Headers["Route"] = this.RouteSet;
            //    foreach (Header h in request.Headers["Route"])
            //    {
            //        h.Name = "Route";
            //    }
            //}
            return request;
        }

        /// <summary>
        /// Creates a SIP response, filling in the necessary parameters from the dialog.
        /// </summary>
        /// <param name="response_code">The SIP response_code.</param>
        /// <param name="response_text">The SIP response_text.</param>
        /// <param name="content">The SIP body contents.</param>
        /// <param name="contentType">The type of the SIP body.</param>
        /// <returns>Message.</returns>
        public override Message CreateResponse(int response_code, string response_text, string content = null,
                                               string contentType = null)
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
            Message request = Servers[Servers.Count - 1].Request;
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

        /// <summary>
        /// Sends a SIP response on the matching transaction.
        /// </summary>
        /// <param name="response">The SIP response message</param>
        /// <param name="responseText">The SIP response text.</param>
        /// <param name="content">The SIP body content.</param>
        /// <param name="contentType">The type of the SIP body.</param>
        /// <param name="createDialog">if set to <c>true</c> [create dialog].</param>
        public override void SendResponse(object response, string responseText = null, string content = null,
                                          string contentType = null, bool createDialog = true)
        {
            if (Servers.Count == 0)
            {
                Debug.Assert(false, String.Format("No server transaction to create response"));
                return;
            }

            //TODO HOW ABOUT SERVERS[SERVERS.COUNT-1]
            string branchID = "z9hG4bK" + "ERROR";
            try
            {
                branchID = ((Message) response).First("Via").Attributes["branch"];
            }
            catch (Exception)
            {
                Debug.Assert(false, "Error trying to convert response object into Message");
                return;
            }

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

            base.SendResponse(response, responseText, content, contentType);
            int code = 0;
            if (response is int)
            {
                code = (int) response;
            }
            else if (response is Message)
            {
                code = ((Message) (response)).ResponseCode;
            }
            if (code > 200)
            {
                Servers.RemoveAt(0);
            }
        }

        /// <summary>
        /// Sends a SIP cancel message.
        /// </summary>
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

        /// <summary>
        /// Triggered on receipt of a SIP request.
        /// </summary>
        /// <param name="transaction">The SIP transaction.</param>
        /// <param name="request">The SIP request.</param>
        public override void ReceivedRequest(Transaction transaction, Message request)
        {
            if (RemoteSeq != 0 && request.Headers["CSeq"][0].Number < RemoteSeq)
            {
                Message m = transaction.CreateResponse(500, "Internal server error - invalid CSeq");
                SendResponse(m);
                Debug.Assert(false,
                             String.Format("Dialog.receivedRequest() CSeq is old {0} < {1}",
                                           request.Headers["CSeq"][0].Number, RemoteSeq));
                return;
            }
            RemoteSeq = request.Headers["CSeq"][0].Number;

            if (request.Method == "INVITE" && request.Headers.ContainsKey("Contact"))
            {
                RemoteTarget = new SIPURI(((Address) (request.Headers["Contact"][0].Value)).Uri.ToString());
            }

            if (request.Method == "ACK" || request.Method == "CANCEL")
            {
                Servers.RemoveAll(x => x == transaction);
                if (request.Method == "ACK")
                {
                    Stack.ReceivedRequest(this, request);
                }
                else
                {
                    Stack.Cancelled(this, transaction.Request);
                }
                return;
            }
            Servers.Add(transaction);
            Stack.ReceivedRequest(this, request);
        }

        /// <summary>
        /// Triggered on receipt of a SIP response.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="response">The response.</param>
        public override void ReceivedResponse(Transaction transaction, Message response)
        {
            if (response.Is2XX() && response.Headers.ContainsKey("Contact") && transaction != null &&
                transaction.Request.Method == "INVITE")
            {
                RemoteTarget = new SIPURI(((Address) (Request.First("Contact").Value)).Uri.ToString());
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

            if (Autoack && response.Is2XX() &&
                (transaction != null && transaction.Request.Method == "INVITE" ||
                 response.First("CSeq").Method == "INVITE"))
            {
                Message ack = CreateRequest("ACK");
                SendRequest(ack);
            }
        }
    }
}