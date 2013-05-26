// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-03-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="ProxyBranch.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System.Collections.Generic;
using SIPLib.SIP;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// Simple helper class to represent proxy branch variables
    /// </summary>
    internal class ProxyBranch
    {
        /// <summary>
        /// The cancel request
        /// </summary>
        public Message CancelRequest;
        /// <summary>
        /// The remote candidates
        /// </summary>
        public List<SIPURI> RemoteCandidates;
        /// <summary>
        /// The SIP request
        /// </summary>
        public Message Request;
        /// <summary>
        /// The SIP response
        /// </summary>
        public Message Response;
        /// <summary>
        /// The transaction
        /// </summary>
        public Transaction Transaction;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.ProxyBranch"/> class.
        /// </summary>
        public ProxyBranch()
        {
            Request = null;
            Response = null;
            RemoteCandidates = null;
            Transaction = null;
            CancelRequest = null;
        }
    }
}