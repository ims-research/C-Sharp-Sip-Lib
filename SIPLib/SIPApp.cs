// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 01-29-2013
// ***********************************************************************
// <copyright file="SIPApp.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using SIPLib.SIP;

#endregion

namespace SIPLib
{
    /// <summary>
    /// This class needs to be overridden to implement the custom logic needed for the application that is to be developed on top of this SIP stack. See the SIPLibDriver example to see how this works in practice.
    /// </summary>
    public abstract class SIPApp
    {
        /// <summary>
        /// Gets or sets the transport.
        /// </summary>
        /// <value>The transport.</value>
        public abstract TransportInfo Transport { get; set; }

        /// <summary>
        /// Gets or sets the stack.
        /// </summary>
        /// <value>The stack.</value>
        public abstract SIPStack Stack { get; set; }
        /// <summary>
        /// Stub to handle the authentication of the specified ua.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="header">The header.</param>
        /// <param name="sipStack">The sip stack.</param>
        /// <returns>System.String[].</returns>
        public abstract string[] Authenticate(UserAgent ua, Header header, SIPStack sipStack);

        /// <summary>
        /// Stub to alert on creation of a dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="ua">The ua.</param>
        /// <param name="sipStack">The sip stack.</param>
        public abstract void DialogCreated(Dialog dialog, UserAgent ua, SIPStack sipStack);

        /// <summary>
        /// Stub to alert on cancellation.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="request">The request.</param>
        /// <param name="sipStack">The sip stack.</param>
        public abstract void Cancelled(UserAgent ua, Message request, SIPStack sipStack);

        /// <summary>
        /// Stub to receive a response.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="response">The response.</param>
        /// <param name="sipStack">The sip stack.</param>
        public abstract void ReceivedResponse(UserAgent ua, Message response, SIPStack sipStack);

        /// <summary>
        /// Stub to receive a request.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="request">The request.</param>
        /// <param name="sipStack">The sip stack.</param>
        public abstract void ReceivedRequest(UserAgent ua, Message request, SIPStack sipStack);

        /// <summary>
        /// Stub to alert on sending of a SIP message.
        /// </summary>
        /// <param name="ua">The ua.</param>
        /// <param name="message">The message.</param>
        /// <param name="sipStack">The sip stack.</param>
        public abstract void Sending(UserAgent ua, Message message, SIPStack sipStack);

        /// <summary>
        /// Stub to create a SIP server user agent.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="sipStack">The sip stack.</param>
        /// <returns>UserAgent.</returns>
        public abstract UserAgent CreateServer(Message request, SIPURI uri, SIPStack sipStack);

        /// <summary>
        /// Stub to create timers.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="sipStack">The sip stack.</param>
        /// <returns>Timer.</returns>
        public abstract Timer CreateTimer(UserAgent obj, SIPStack sipStack);

        /// <summary>
        /// Stub to actually send data.
        /// </summary>
        /// <param name="finalData">The final data.</param>
        /// <param name="destinationHost">The destination host.</param>
        /// <param name="destinationPort">The destination port.</param>
        /// <param name="sipStack">The sip stack.</param>
        public abstract void Send(string finalData, string destinationHost, int destinationPort, SIPStack sipStack);

        /// <summary>
        /// Occurs when [there is data received].
        /// </summary>
        public virtual event EventHandler<RawEventArgs> ReceivedDataEvent;
    }
}