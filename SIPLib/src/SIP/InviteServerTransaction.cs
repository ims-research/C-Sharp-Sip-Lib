// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="InviteServerTransaction.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class represents a server transaction that is used for SIP INVITE processing.
    /// </summary>
    public class InviteServerTransaction : Transaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.InviteServerTransaction"/> class.
        /// </summary>
        /// <param name="app">Takes in a user agent.</param>
        public InviteServerTransaction(UserAgent app) : base(app)
        {
            Server = true;
        }

        /// <summary>
        /// Starts this instance. This sends the response represented by the transaction and passes the request up the stack.
        /// </summary>
        public void Start()
        {
            State = "proceeding";
            SendResponse(CreateResponse(100, "Trying"));
            App.ReceivedRequest(this, Request, Stack);
        }

        /// <summary>
        /// Triggered on receipt of any requests. Updates state of transaction
        /// </summary>
        /// <param name="receivedRequest">The received request.</param>
        public override void ReceivedRequest(Message receivedRequest)
        {
            if (Request.Method == receivedRequest.Method)
            {
                if (State == "proceeding" || State == "completed")
                {
                    Stack.Send(LastResponse, Remote, Transport);
                }
            }
            else if (receivedRequest.Method == "ACK")
            {
                if (State == "completed")
                {
                    State = "confirmed";
                    if (!Transport.Reliable)
                    {
                        StartTimer("I", Timer.I());
                    }
                    else
                    {
                        Timeout("I", 0);
                    }
                }
                else if (State == "confirmed")
                {
                    //Ignore duplicate ACK
                }
            }
        }

        /// <summary>
        ///  Handles timeouts.
        /// </summary>
        /// <param name="name">The Timer name (G H I etc.)</param>
        /// <param name="timeout">The timeout.</param>
        public void Timeout(string name, int timeout)
        {
            if (State == "completed")
            {
                if (name == "G")
                {
                    StartTimer("G", Math.Min(2*timeout, Timer.T2));
                    Stack.Send(LastResponse, Remote, Transport);
                }
                else if (name == "H")
                {
                    State = "terminated";
                    App.Timeout(this);
                }
            }
            else if (State == "confirmed")
            {
                if (name == "I")
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
            if (State == "proceeding" || State == "confirmed")
            {
                State = "terminated";
                App.Error(this, error);
            }
        }

        /// <summary>
        /// Sends a response based on this transaction.
        /// </summary>
        /// <param name="response">The response.</param>
        public override void SendResponse(Message response)
        {
            LastResponse = response;
            if (response.Is1XX())
            {
                if (State == "proceeding")
                {
                    Stack.Send(response, Remote, Transport);
                }
            }
            else if (response.Is2XX())
            {
                if (State == "proceeding")
                {
                    State = "terminated";
                    Stack.Send(response, Remote, Transport);
                }
            }
            else
            {
                if (State == "proceeding")
                {
                    State = "completed";
                    if (!Transport.Reliable)
                    {
                        StartTimer("G", Timer.G());
                    }
                    StartTimer("H", Timer.H());
                    Stack.Send(response, Remote, Transport);
                }
            }
        }
    }
}