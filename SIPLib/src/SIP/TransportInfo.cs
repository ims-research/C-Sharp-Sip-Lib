// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 01-29-2013
// ***********************************************************************
// <copyright file="TransportInfo.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System.Net;
using System.Net.Sockets;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent the transmission medium (TCP / UDP etc).
    /// </summary>
    public class TransportInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.TransportInfo"/> class.
        /// </summary>
        /// <param name="localAddress">The local IP address.</param>
        /// <param name="listenPort">The port to listen on.</param>
        /// <param name="type">The protocol type.</param>
        public TransportInfo(IPAddress localAddress, int listenPort, ProtocolType type)
        {
            Host = localAddress;
            Port = listenPort;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public IPAddress Host { get; set; }
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public ProtocolType Type { get; set; }
        /// <summary>
        /// Gets or sets the socket.
        /// </summary>
        /// <value>The socket.</value>
        public Socket Socket { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:SIPLib.SIP.TransportInfo"/> is reliable.
        /// </summary>
        /// <value><c>true</c> if reliable; otherwise, <c>false</c>.</value>
        public bool Reliable { get; set; }
    }
}