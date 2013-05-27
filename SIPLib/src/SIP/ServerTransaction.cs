// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="ServerTransaction.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent a SIP, non INVITE server transaction.
    /// </summary>
    public class ServerTransaction : Transaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.ServerTransaction" /> class.
        /// </summary>
        /// <param name="app">The useragent</param>
        public ServerTransaction(UserAgent app) : base(app)
        {
            Server = true;
        }

        /// <summary>
        /// Starts the transaction (set state to trying, pass received request up the stack).
        /// </summary>
        public void Start()
        {
            State = "trying";
            if (App is Dialog)
            {
                ((App)).ReceivedRequest(this, Request);
            }
            else App.ReceivedRequest(this, Request);
        }

        /// <summary>
        /// Handles retransmitted requests / completed requests
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
                else if (State == "trying")
                {
                    // Ignore the retransmitted request
                }
            }
        }

        /// <summary>
        /// Handles timeouts
        /// </summary>
        /// <param name="name">The name of the timer (J etc).</param>
        /// <param name="timeout">The timeout.</param>
        public void Timeout(string name, int timeout)
        {
            if (State == "completed")
            {
                if (name == "J")
                {
                    State = "terminated";
                }
            }
        }

        /// <summary>
        /// Raises an error
        /// </summary>
        /// <param name="error">The error.</param>
        public void Error(string error)
        {
            if (State == "completed")
            {
                State = "terminated";
                App.Error(this, error);
            }
        }

        /// <summary>
        /// Sends a SIP response.
        /// </summary>
        /// <param name="response">The SIP response.</param>
        public override void SendResponse(Message response)
        {
            LastResponse = response;
            if (response.Is1XX())
            {
                if (State == "trying" || State == "proceeding")
                {
                    State = "proceeding";
                    Stack.Send(response, Remote, Transport);
                }
            }
            else if (response.IsFinal())
            {
                if (State == "trying" || State == "proceeding")
                {
                    State = "completed";
                    Stack.Send(response, Remote, Transport);
                    if (!Transport.Reliable)
                    {
                        StartTimer("J", Timer.J());
                    }
                    else
                    {
                        Timeout("J", 0);
                    }
                }
            }
        }
    }
}