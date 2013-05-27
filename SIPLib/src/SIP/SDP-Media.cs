// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="SDP-Media.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent SDP Media data (m=media port proto fmt ...)
    /// </summary>
    public class SDPMedia
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SDPMedia" /> class.
        /// </summary>
        /// <param name="value">The input string representing the m line in the SDP</param>
        /// <param name="attrDict">An optional dictionary containing the m= parameters</param>
        public SDPMedia(string value = null, Dictionary<string, string> attrDict = null)
        {
            Mediaformats = new List<SDPMediaFormat>();
            OtherAttributes = new Dictionary<string, string>();
            if (value != null)
            {
                string[] values = value.Split(" ".ToCharArray(), 4);
                Media = values[0];
                Port = values[1];
                Proto = values[2];
                string rest = values[3];
                Mediaformats = new List<SDPMediaFormat>();
                foreach (string s in rest.Split(' '))
                {
                    SDPMediaFormat fmt = new SDPMediaFormat {Pt = s};
                    Mediaformats.Add(fmt);
                }
            }
            else if (attrDict != null && attrDict.ContainsKey("media"))
            {
                Media = attrDict["media"];
                Port = attrDict.ContainsKey("port") ? attrDict["port"] : "0";
                Proto = attrDict.ContainsKey("proto") ? attrDict["proto"] : "RTP/AVP";
                Mediaformats = new List<SDPMediaFormat>();
                if (attrDict.ContainsKey("fmt"))
                {
                    foreach (string s in attrDict["fmt"].Split(' '))
                    {
                        SDPMediaFormat fmt = new SDPMediaFormat {Pt = s};
                        Mediaformats.Add(fmt);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the media type (e.g. "audio","video" etc.)
        /// </summary>
        /// <value>The media type ("audio", "video" etc).</value>
        public string Media { get; set; }
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public string Port { get; set; }
        /// <summary>
        /// Gets or sets the transport protocol (e.g. RTP/AVP)
        /// </summary>
        /// <value>The transport protocol (e.g. RTP/AVP).</value>
        public string Proto { get; set; }
        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
        public string Count { get; set; }
        /// <summary>
        /// Gets or sets the media format description, <see cref="T:SIPLib.SIP.SDPMediaFormat" /> class.
        /// </summary>
        /// <value>A list of media format descriptions.</value>
        public List<SDPMediaFormat> Mediaformats { get; set; }
        /// <summary>
        /// Gets or sets the other attributes.
        /// </summary>
        /// <value>The other attributes.</value>
        public Dictionary<string, string> OtherAttributes { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Media + " ");
            sb.Append(Port + " ");
            sb.Append(Proto + " ");
            foreach (SDPMediaFormat mf in Mediaformats)
            {
                sb.Append(mf.Pt + " ");
            }
            foreach (Char c in "icbka")
            {
                if (OtherAttributes.ContainsKey(c.ToString()))
                {
                    if (!SDP.Multiple.Contains(c))
                    {
                        sb.Append("\r\n" + c + "=" + OtherAttributes[c.ToString()]);
                    }
                    else
                    {
                        // TODO: Handle multiple values of same type
                        //
                        //foreach (AttributeClass header in this[c.ToString(), true])
                        //{
                        //sb.Append("\r\n" + c + "=" + header.value);
                        //}
                        sb.Append("\r\n" + c + "=" + OtherAttributes[c.ToString()]);
                    }
                }
            }
            foreach (SDPMediaFormat mf in Mediaformats)
            {
                if (mf.Name.Length > 0)
                {
                    sb.Append("\r\n" + "a=rtpmap:" + mf.Pt + " " + mf.Name + "/" + mf.Rate);
                    if (mf.Parameters.Length > 0)
                    {
                        sb.Append("/" + mf.Parameters);
                    }
                }
            }
            return sb.ToString();
        }
    }
}