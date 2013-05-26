// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="ClientTransaction.cs">
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
    /// Class ClientTransaction. Used to represent SIP, non INVITE client transactions.
    /// </summary>
    internal class ClientTransaction : Transaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.ClientTransaction"/> class.
        /// </summary>
        /// <param name="app">Takes in a useragent instance.</param>
        public ClientTransaction(UserAgent app) : base(app)
        {
            Server = false;
        }

        /// <summary>
        /// Starts this instance. This sends the request represented by the transaction.
        /// </summary>
        public void Start()
        {
            State = "trying";
            if (!Transport.Reliable)
            {
                StartTimer("E", Timer.E());
            }
            StartTimer("F", Timer.F());
            Stack.Send(Request, Remote, Transport);
        }

        /// <summary>
        /// Triggered on receipt of any responses. Updates state of transaction.
        /// </summary>
        /// <param name="response">The SIP response message.</param>
        public override void ReceivedResponse(Message response)
        {
            if (response.Is1XX())
            {
                if (State == "trying")
                {
                    State = "proceeding";
                    App.ReceivedResponse(this, response);
                }
                else if (State == "proceeding")
                {
                    App.ReceivedResponse(this, response);
                }
            }
            else if (response.IsFinal())
            {
                if (State == "trying" || State == "proceeding")
                {
                    State = "completed";
                    App.ReceivedResponse(this, response);
                    if (!Transport.Reliable)
                    {
                        StartTimer("K", Timer.K());
                    }
                    else
                    {
                        Timeout("K", 0);
                    }
                }
            }
        }

        /// <summary>
        /// Handles timeouts.
        /// </summary>
        /// <param name="name">The Timer name (E F K etc.)</param>
        /// <param name="timeout">The timeout.</param>
        public void Timeout(string name, int timeout)
        {
            if (State == "trying" | State == "proceeding")
            {
                if (name == "E")
                {
                    timeout = State == "trying" ? Math.Min(2*timeout, Timer.T2) : Timer.T2;
                    StartTimer("E", timeout);
                    Stack.Send(Request, Remote, Transport);
                }
                else if (name == "F")
                {
                    State = "terminated";
                    App.Timeout(this);
                }
            }
            else if (State == "completed")
            {
                if (name == "K")
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
            if (State == "trying" || State == "proceeding")
            {
                State = "terminated";
                App.Error(this, error);
            }
        }
    }
}