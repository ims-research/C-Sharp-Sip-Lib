// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="Events.cs">
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
    /// This is a simple class used to pass SIP messages around triggered on various events.
    /// </summary>
    public class SipMessageEventArgs : EventArgs
    {
        /// <summary>
        /// The SIP message
        /// </summary>
        public Message Message;
        /// <summary>
        /// The UA
        /// </summary>
        public UserAgent UA;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SipMessageEventArgs"/> class based on a SIP message.
        /// </summary>
        /// <param name="transferredMessage">The transferred message.</param>
        public SipMessageEventArgs(Message transferredMessage)
        {
            Message = transferredMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SipMessageEventArgs"/> class based on SIP message and useragent.
        /// </summary>
        /// <param name="transferredMessage">The transferred message.</param>
        /// <param name="ua">The ua.</param>
        public SipMessageEventArgs(Message transferredMessage, UserAgent ua)
        {
            Message = transferredMessage;
            UA = ua;
        }
    }

    /// <summary>
    /// This class is used to pass error messages from the SIP stack.
    /// </summary>
    public class StackErrorEventArgs : EventArgs
    {
        /// <summary>
        /// The exception
        /// </summary>
        public Exception Exception;
        /// <summary>
        /// A string representing the function the error occured in.
        /// </summary>
        public string Function;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.StackErrorEventArgs"/> class.
        /// </summary>
        /// <param name="inputFunction">A string representing the function name the error occured in.</param>
        /// <param name="e">An exception to report.</param>
        public StackErrorEventArgs(string inputFunction, Exception e)
        {
            Function = inputFunction;
            Exception = e;
        }
    }

    /// <summary>
    /// This class is used to pass information about SIP registration changes.
    /// </summary>
    public class RegistrationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The SIP message
        /// </summary>
        public Message Message;
        /// <summary>
        /// The current or changed registration state
        /// </summary>
        public string State;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.RegistrationChangedEventArgs"/> class.
        /// </summary>
        /// <param name="s">The registration state.</param>
        /// <param name="m">The SIP message.</param>
        public RegistrationChangedEventArgs(string s, Message m)
        {
            State = s;
            Message = m;
        }
    }

    /// <summary>
    /// This class is used to pass raw received data to higher levels.
    /// </summary>
    public class RawEventArgs : EventArgs
    {
        /// <summary>
        /// The transmitted data
        /// </summary>
        public string Data;
        /// <summary>
        /// Was the data sent or received ?
        /// </summary>
        public bool Sent;
        /// <summary>
        /// A string array representing the source of the data
        /// </summary>
        public string[] Src;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.RawEventArgs"/> class.
        /// </summary>
        /// <param name="d">Data to shuffle around</param>
        /// <param name="s">The source of the data</param>
        /// <param name="sent">if set to <c>true</c> then event represents sent data.</param>
        public RawEventArgs(string d, string[] s, bool sent)
        {
            Data = d;
            Src = s;
            Sent = sent;
        }
    }
}