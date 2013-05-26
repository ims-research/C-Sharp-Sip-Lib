// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="InviteClientTransaction.cs">
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
    /// Class InviteClientTransaction. This class represents an client transaction that is used for client SIP INVITE processing.
    /// </summary>
    public class InviteClientTransaction : Transaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.InviteClientTransaction"/> class.
        /// </summary>
        /// <param name="app">Takes in the useragent as a parameter.</param>
        public InviteClientTransaction(UserAgent app) : base(app)
        {
            Server = false;
        }

        /// <summary>
        /// Starts this instance. This sends the request represented by the transaction.
        /// </summary>
        public void Start()
        {
            State = "calling";
            if (!Transport.Reliable)
                StartTimer("A", Timer.A());
            StartTimer("B", Timer.B());
            Stack.Send(Request, Remote, Transport);
        }

        /// <summary>
        /// Triggered on receipt of any responses. Updates state of transaction.
        /// </summary>
        /// <param name="response">The response.</param>
        public override void ReceivedResponse(Message response)
        {
            if (response.Is1XX())
            {
                if (State == "calling")
                {
                    State = "proceeding";
                    App.ReceivedResponse(this, response);
                }
                else if (State == "proceeding")
                {
                    App.ReceivedResponse(this, response);
                }
            }
            else if (response.Is2XX())
            {
                if (State == "calling" || State == "proceeding")
                {
                    State = "terminated";
                    App.ReceivedResponse(this, response);
                }
            }
            else
            {
                if (State == "calling" || State == "proceeding")
                {
                    State = "completed";
                    Stack.Send(CreateAck(response), Remote, Transport);
                    App.ReceivedResponse(this, response);
                    if (!Transport.Reliable)
                    {
                        StartTimer("D", Timer.D());
                    }
                    else
                    {
                        Timeout("D", 0);
                    }
                }
                else if (State == "completed")
                {
                    Stack.Send(CreateAck(response), Remote, Transport);
                }
            }
        }

        /// <summary>
        /// Handles timeouts.
        /// </summary>
        /// <param name="name">The Timer name (A B D etc.)</param>
        /// <param name="timeout">The timeout.</param>
        public void Timeout(string name, int timeout)
        {
            if (State == "calling")
            {
                if (name == "A")
                {
                    StartTimer("A", 2*timeout);
                    Stack.Send(Request, Remote, Transport);
                }
                else if (name == "B")
                {
                    State = "terminated";
                    App.Timeout(this);
                }
            }
            else if (State == "completed")
            {
                if (name == "D")
                {
                    State = "terminated";
                }
            }
        }

        /// <summary>
        /// Raises an error.
        /// </summary>
        /// <param name="error">The error.</param>
        public void Error(string error)
        {
            if (State == "calling" || State == "completed")
            {
                State = "terminated";
                App.Error(this, error);
            }
        }

        /// <summary>
        /// Creates a SIP ACK message based on the transaction and SIP response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>Message.</returns>
        public Message CreateAck(Message response)
        {
            if (Request == null)
            {
                Debug.Assert(false, String.Format("Error creating Ack message when request is null"));
                return null;
            }
            Message m = Message.CreateRequest("ACK", Request.Uri);
            m.Headers["Call-ID"] = Request.Headers["Call-ID"];
            m.Headers["From"] = Request.Headers["From"];

            if (response != null)
            {
                m.Headers["To"] = response.Headers["To"];
            }
            else
            {
                m.Headers["To"] = Request.Headers["To"];
            }

            m.Headers["Via"] = new List<Header> {Request.First("Via")};

            m.Headers["CSeq"] = new List<Header> {new Header(Request.First("CSeq").Number + " ACK", "CSeq")};

            if (Request.Headers.ContainsKey("Route"))
            {
                m.Headers["Route"] = Request.Headers["Route"];
            }
            return m;
        }
    }
}