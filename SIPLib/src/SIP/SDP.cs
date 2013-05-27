// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="SDP.cs">
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
    /// This class is used to represent the SDP: Session Description Protocol (rfc4566).
    /// </summary>
    public class SDP
    {
        /// <summary>
        /// List of SDP lines that can be repeated.
        /// </summary>
        public static string Multiple = "tramb";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SDP" /> class.
        /// </summary>
        /// <param name="sdp">A input block of text representing SDP.</param>
        public SDP(string sdp = null)
        {
            Media = new List<SDPMedia>();
            Other = new Dictionary<string, string>();

            if (sdp != null)
            {
                Parse(sdp);
            }
        }

        /// <summary>
        /// Gets or sets a list of SDPMedia objects representing different media items in the SDP.
        /// </summary>
        /// <value>A list of SDPMedia objects</value>
        public List<SDPMedia> Media { get; set; }
        /// <summary>
        /// Gets or sets the SDP line representing the connection (c=...)
        /// </summary>
        /// <value>The connection line (c=...)</value>
        public SDPConnection Connection { get; set; }
        /// <summary>
        /// Gets or sets the originator of the session.
        /// </summary>
        /// <value>The originator line (o=...)</value>
        public SDPOriginator Originator { get; set; }
        /// <summary>
        /// Gets or sets the other parameters.
        /// </summary>
        /// <value>The other parameters.</value>
        public Dictionary<string, string> Other { get; set; }


        /// <summary>
        /// Parses the specified SDP.
        /// </summary>
        /// <param name="sdp">The input SDP text.</param>
        public void Parse(string sdp)
        {
            sdp = sdp.Replace("\r\n", "\n");
            sdp = sdp.Replace("\r", "\n");
            sdp = sdp.Trim();
            foreach (String line in sdp.Split('\n'))
            {
                //Per line parsing
                string[] values = line.Split("=".ToCharArray(), 2);
                string k = values[0];
                string currentObject = "";
                switch (k)
                {
                    case "o":
                        Originator = new SDPOriginator(values[1]);
                        currentObject = Originator.ToString();
                        break;
                    case "c":
                        Connection = new SDPConnection(values[1]);
                        currentObject = Connection.ToString();
                        break;
                    case "m":
                        {
                            SDPMedia currentMedia = new SDPMedia(values[1]);
                            Media.Add(currentMedia);
                            currentObject = currentMedia.ToString();
                        }
                        break;
                    default:
                        currentObject = values[1];
                        break;
                }

                if (k == "m")
                {
                    SDPMedia obj = Media.Last();
                }
                else if (Media.Count > 0)
                {
                    SDPMedia obj = Media.Last();
                    if (k == "a" && values[1].StartsWith("rtpmap:"))
                    {
                        string[] split = values[1].Remove(0, 7).Split(" ".ToCharArray(), 2);
                        string pt = split[0];
                        string[] rest = split[1].Split("/".ToCharArray(), 2);
                        string name = rest[0];
                        string rate = null;
                        string parameters = null;
                        if (rest.Length > 1)
                        {
                            string[] final = rest[1].Split("/".ToCharArray(), 2);
                            rate = final[0];

                            parameters = null;
                            if (final.Length > 1)
                            {
                                parameters = final[1];
                            }
                        }

                        foreach (SDPMediaFormat f in obj.Mediaformats)
                        {
                            if (f.Pt == pt)
                            {
                                f.Name = name;
                                if (rate != null)
                                {
                                    f.Rate = rate;
                                }
                                if (parameters != null)
                                {
                                    f.Parameters = parameters;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!Multiple.Contains(k))
                        {
                            obj.OtherAttributes.Add(k, currentObject);
                        }
                        else
                        {
                            if (!obj.OtherAttributes.ContainsKey(k))
                            {
                                obj.OtherAttributes.Add(k, currentObject);
                            }

                            //TODO HANDLE multiple attributes of the same type;
                            //if (obj.properties.ContainsKey(k))
                            //{
                            //    obj[k, true].Add(current_attribute);
                            //}
                            //else
                            //{
                            //    obj[k, true] = new List<AttributeClass>();
                            //    obj[k, true].Add(current_attribute);
                            //}
                        }
                    }
                }
                else
                {
                    if (k != "o" && k != "c")
                    {
                        SDP obj = this;
                        if (!Multiple.Contains(k))
                        {
                            obj.Other.Add(k, currentObject);
                        }
                        else
                        {
                            obj.Other.Add(k, currentObject);
                            //TODO HANDLE multiple attributes of the same type;
                            //if (obj.properties.ContainsKey(k))
                            //{
                            //    obj[k, true].Add(current_attribute);
                            //}
                            //else
                            //{
                            //    obj[k, true] = new List<AttributeClass>();
                            //    obj[k, true].Add(current_attribute);
                            //}
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Char c in "vosiuepcbtam")
            {
                if (Other.ContainsKey(c.ToString()))
                {
                    if (!Multiple.Contains(c))
                    {
                        sb.Append(c + "=" + Other[c.ToString()] + "\r\n");
                    }
                    else
                    {
                        sb.Append(c + "=" + Other[c.ToString()] + "\r\n");
                        // TODO: handle multiple lines of the same
                        //foreach (AttributeClass a in this[c.ToString(), true])
                        //{
                        //    sb.Append(c + "=" + a.value + "\r\n");
                        //}
                    }
                }
                else if (c == 'c')
                {
                    sb.Append(c + "=" + Connection + "\r\n");
                }
                else if (c == 'o')
                {
                    sb.Append(c + "=" + Originator + "\r\n");
                }
                else if (c == 'm')
                {
                    foreach (SDPMedia m in Media)
                    {
                        sb.Append(c + "=" + m + "\r\n");
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates a SDP offer.
        /// </summary>
        /// <param name="streams">A list of SDP Media objects.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="previous">Optional previous SDP.</param>
        /// <returns>SDP.</returns>
        public static SDP CreateOffer(List<SDPMedia> streams, Dictionary<string, string> parameters, SDP previous = null)
        {
            SDP s = new SDP();
            s.Other["v"] = "0";
            foreach (Char a in "iep")
            {
                if (parameters.ContainsKey(a.ToString()))
                {
                    s.Other[a.ToString()] = parameters[a.ToString()];
                }
            }
            if (previous != null && previous.Originator != null)
            {
                s.Originator = new SDPOriginator(previous.Originator.ToString());
                s.Originator.Version = s.Originator.Version + 1;
            }
            s.Other["s"] = "-";
            s.Other["t"] = "0";
            s.Media = streams;
            return s;
        }

        /// <summary>
        /// Creates a SDP answer.
        /// </summary>
        /// <param name="streams">A list of SDP Media objects..</param>
        /// <param name="offer">The SDP offer.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>SDP.</returns>
        public static SDP CreateAnswer(List<SDPMedia> streams, SDP offer, Dictionary<string, string> parameters = null)
        {
            SDP s = new SDP();
            s.Other["v"] = "0";
            foreach (Char a in "iep")
            {
                if (parameters.ContainsKey(a.ToString()))
                {
                    s.Other[a.ToString()] = parameters[a.ToString()];
                }
            }
            s.Originator = new SDPOriginator();
            s.Other["s"] = "-";
            s.Other["t"] = offer.Other["t"];
            foreach (SDPMedia yourMedia in offer.Media)
            {
                SDPMedia myMedia = null;
                foreach (SDPMedia t in streams)
                {
                    if (t.Media != yourMedia.Media) continue;
                    myMedia = new SDPMedia(t.ToString());
                    //streams.RemoveAt(i);
                    List<KeyValuePair<SDPMediaFormat, SDPMediaFormat>> found =
                        new List<KeyValuePair<SDPMediaFormat, SDPMediaFormat>>();
                    foreach (SDPMediaFormat yourmf in yourMedia.Mediaformats)
                    {
                        foreach (SDPMediaFormat mymf in myMedia.Mediaformats)
                        {
                            int mymfpt = -1;
                            int yourmfpt = -1;
                            try
                            {
                                mymfpt = Int32.Parse(mymf.Pt);
                                yourmfpt = Int32.Parse(yourmf.Pt);
                            }
                            catch (Exception)
                            {
                                mymfpt = -1;
                                yourmfpt = -1;
                            }
                            if ((0 <= mymfpt && mymfpt < 32 && 0 <= yourmfpt && yourmfpt <= 32 && mymfpt == yourmfpt)
                                || (mymfpt < 0 && yourmfpt < 0 && mymfpt == yourmfpt)
                                || (mymf.Name == yourmf.Name && mymf.Rate == yourmf.Rate && mymf.Count == yourmf.Count))
                            {
                                found.Add(new KeyValuePair<SDPMediaFormat, SDPMediaFormat>(yourmf, mymf));
                                break;
                            }
                        }
                    }
                    if (found.Count > 0)
                    {
                        foreach (KeyValuePair<SDPMediaFormat, SDPMediaFormat> kvp in found)
                        {
                            myMedia.Mediaformats.Add(kvp.Key);
                        }
                    }
                    else
                    {
                        myMedia.Mediaformats.Clear();
                        SDPMediaFormat temp = new SDPMediaFormat {Pt = "0"};
                        myMedia.Mediaformats.Add(temp);
                        myMedia.Port = "0";
                    }
                }
                if (myMedia == null)
                {
                    myMedia = new SDPMedia(yourMedia.ToString()) {Port = "0"};
                }
                s.Media.Add(myMedia);
            }
            bool valid = s.Media.Any(myMedia => myMedia.Port != "0");
            return valid ? s : null;
        }
    }
}