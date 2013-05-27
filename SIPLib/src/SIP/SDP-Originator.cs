// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="SDP-Originator.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Net;
using SIPLib.Utils;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent the originator of a session. (o=username sess-id sess-version nettype addrtype unicast-address)
    /// </summary>
    public class SDPOriginator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SDPOriginator" /> class.
        /// </summary>
        /// <param name="value">The input string representing an o line in the SDP.</param>
        public SDPOriginator(string value = null)
        {
            if (value != null)
            {
                string[] values = value.Split(' ');
                Username = values[0];
                Sessionid = values[1];
                Version = values[2];
                Nettype = values[3];
                Addrtype = values[4];
                Address = values[5];
            }
            else
            {
                string hostname = Dns.GetHostName();
                IPHostEntry ip = Dns.GetHostEntry(hostname);
                string ipAddress = ip.ToString();
                Username = "-";
                Sessionid = Helpers.ToUnixTime(DateTime.Now).ToString();
                Version = Helpers.ToUnixTime(DateTime.Now).ToString();
                Nettype = "IN";
                Addrtype = "IP4";
                Address = ipAddress;
            }
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>The username.</value>
        public string Username { get; set; }
        /// <summary>
        /// Gets or sets the sessionid.
        /// </summary>
        /// <value>The sessionid.</value>
        public string Sessionid { get; set; }
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }
        /// <summary>
        /// Gets or sets the nettype.
        /// </summary>
        /// <value>The nettype.</value>
        public string Nettype { get; set; }
        /// <summary>
        /// Gets or sets the addrtype.
        /// </summary>
        /// <value>The addrtype.</value>
        public string Addrtype { get; set; }
        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        /// <value>The address.</value>
        public string Address { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Username + " " + Sessionid + " " + Version + " " + Nettype + " " + Addrtype + " " + Address;
        }
    }
}