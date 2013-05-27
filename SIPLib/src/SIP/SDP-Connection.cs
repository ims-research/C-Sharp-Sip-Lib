// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="SDP-Connection.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System.Collections.Generic;
using System.Text;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent a SDP Connection data (c=nettype addrtype connection-address)
    /// </summary>
    public class SDPConnection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SDPConnection" /> class.
        /// </summary>
        /// <param name="value">The input string representing the c line in the SDP</param>
        /// <param name="attrDict">An optional dictionary containing the c= parameters</param>
        public SDPConnection(string value = null, Dictionary<string, string> attrDict = null)
        {
            if (value != null)
            {
                string[] values = value.Split(' ');
                Nettype = values[0];
                Addrtype = values[1];
                string rest = values[2];
                string[] rest2 = rest.Split('/');
                switch (rest2.Length)
                {
                    case 1:
                        Address = rest2[0];
                        break;
                    case 2:
                        Address = rest2[0];
                        TTL = rest2[1];
                        break;
                    default:
                        Address = rest2[0];
                        TTL = rest2[1];
                        Count = rest2[2];
                        break;
                }
            }
            else if (attrDict != null && attrDict.ContainsKey("address"))
            {
                Address = attrDict["address"];
                Nettype = attrDict.ContainsKey("nettype") ? attrDict["nettype"] : "IN";
                Addrtype = attrDict.ContainsKey("addrtype") ? attrDict["addrtype"] : "IP4";
                TTL = attrDict.ContainsKey("ttl") ? attrDict["ttl"] : null;
                Count = attrDict.ContainsKey("count") ? attrDict["count"] : null;
            }
        }

        /// <summary>
        /// Gets or sets the nettype (e.g. "IN")
        /// </summary>
        /// <value>The nettype (e.g. "IN")</value>
        public string Nettype { get; set; }
        /// <summary>
        /// Gets or sets the addrtype (e.g. "IP4")
        /// </summary>
        /// <value>The addrtype (e.g. "IP4")</value>
        public string Addrtype { get; set; }
        /// <summary>
        /// Gets or sets the address (e.g. "192.168.0.1")
        /// </summary>
        /// <value>The address (e.g. "192.168.0.1")</value>
        public string Address { get; set; }
        /// <summary>
        /// Gets or sets the TTL (for multicast)
        /// </summary>
        /// <value>The TTL (for multicast)</value>
        public string TTL { get; set; }
        /// <summary>
        /// Gets or sets the number of addresses  (multiple multicast groups)
        /// </summary>
        /// <value>How many addresses should be used (multiple multicast groups)</value>
        public string Count { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Nettype + " ");
            sb.Append(Addrtype + " ");
            sb.Append(Address);
            if (TTL != null) sb.Append("/" + TTL);
            if (Count != null) sb.Append("/" + Count);
            return sb.ToString();
        }
    }
}