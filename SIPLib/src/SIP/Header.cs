// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="Header.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// Class Header. Class is used to represent a SIP header.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Private array holding headers relating to addresses.
        /// </summary>
        private static readonly string[] Address =
            {
                "contact", "from", "record-route", "refer-to", "referred-by",
                "route", "to"
            };

        /// <summary>
        /// Private array holding headers needing special formatting.
        /// </summary>
        private static readonly string[] Comma =
            {
                "authorization", "proxy-authenticate", "proxy-authorization",
                "www-authenticate"
            };

        /// <summary>
        /// Private array holding headers needing general formatting.
        /// </summary>
        private static readonly string[] Unstructured =
            {
                "call-id", "cseq", "date", "expires", "max-forwards",
                "organization", "server", "subject", "timestamp", "user-agent", "service-route"
            };

        /// <summary>
        /// Private dictionary holding shortened forms of headers.
        /// </summary>
        private static readonly Dictionary<string, string> Short = new Dictionary<string, string>
            {
                {"u", "allow-events"},
                {"i", "call-id"},
                {"m", "contact"},
                {"e", "content-encoding"},
                {"l", "content-length"},
                {"c", "content-type"},
                {"o", "event"},
                {"f", "from"},
                {"s", "subject"},
                {"k", "supported"},
                {"t", "to"},
                {"v", "via"}
            };

        /// <summary>
        /// Private array holding headers requiring special capitalization.
        /// </summary>
        private static readonly Dictionary<string, string> Exceptions = new Dictionary<string, string>
            {
                {"call-id", "Call-ID"},
                {"cseq", "CSeq"},
                {"www-authenticate", "WWW-Authenticate"}
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.Header"/> class, taking in the Header name and it's value.
        /// </summary>
        /// <param name="value">The value of the header.</param>
        /// <param name="name">The name of the header.</param>
        public Header(string value, string name)
        {
            Number = -1;
            Attributes = new Dictionary<string, string>();
            Name = Canon(name.Trim());
            Parse(Name, value.Trim());
        }

        /// <summary>
        /// Gets or sets attributes of the header.
        /// </summary>
        /// <value>The attributes.</value>
        public Dictionary<string, string> Attributes { get; set; }
        /// <summary>
        /// Gets or sets the type of the header.
        /// </summary>
        /// <value>The type of the header.</value>
        public string HeaderType { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }
        /// <summary>
        /// Gets or sets the auth method.
        /// </summary>
        /// <value>The auth method.</value>
        public string AuthMethod { get; set; }
        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public int Number { get; set; }
        /// <summary>
        /// Gets or sets the SIP method.
        /// </summary>
        /// <value>The SIP method.</value>
        public string Method { get; set; }
        /// <summary>
        /// Gets or sets the header name.
        /// </summary>
        /// <value>The header name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the via URI.
        /// </summary>
        /// <value>The via URI.</value>
        public SIPURI ViaUri { get; set; }

        /// <summary>
        /// Returns the correct capitalization etc of the input string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>System.String.</returns>
        public static string Canon(string input)
        {
            input = input.ToLower();
            if ((input.Length == 1) && Short.Keys.Contains(input))
            {
                return Canon(Short[input]);
            }
            if (Exceptions.Keys.Contains(input))
            {
                return Exceptions[input];
            }

            string[] words = input.Split('-');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i]);
            }
            return String.Join("-", words);
        }

        /// <summary>
        /// Adds quotation marks to the specified string.
        /// </summary>
        /// <param name="input">The input string without quotation marks.</param>
        /// <returns>System.String.</returns>
        public static string Quote(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                return input;
            }
            return "\"" + input + "\"";
        }

        /// <summary>
        /// Removes quotation marks from the specified string
        /// </summary>
        /// <param name="input">The input string with quotation marks.</param>
        /// <returns>System.String.</returns>
        public static string Unquote(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                return input.Substring(1, input.Length - 2);
            }
            return input;
        }

        /// <summary>
        /// Parses the specified string into an header object.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        public void Parse(string name, string value)
        {
            string rest = "";
            int index = 0;
            if (Address.Contains(name.ToLower()))
            {
                HeaderType = "address";
                Address addr = new Address {MustQuote = true};
                int count = addr.Parse(value);
                Value = addr;
                if (count < value.Length)
                    rest = value.Substring(count, value.Length - count);
                if (rest.Length > 0)
                {
                    foreach (string parm in rest.Split(';'))
                    {
                        if (parm.Contains('='))
                        {
                            index = parm.IndexOf('=');
                            string parmName = parm.Substring(0, index);
                            string parmValue = parm.Substring(index + 1);
                            Attributes.Add(parmName, parmValue);
                        }
                    }
                }
            }
            else if (!(Comma.Contains(name.ToLower())) && !(Unstructured.Contains(name.ToLower())))
            {
                HeaderType = "standard";
                if (!value.Contains(";lr>"))
                {
                    if (value.Contains(';'))
                    {
                        index = value.IndexOf(';');
                        Value = value.Substring(0, index);
                        string tempStr = value.Substring(index + 1).Trim();
                        foreach (string parm in tempStr.Split(';'))
                        {
                            if (parm.Contains('='))
                            {
                                index = parm.IndexOf('=');
                                string parmName = parm.Substring(0, index);
                                string parmValue = parm.Substring(index + 1);
                                Attributes.Add(parmName, parmValue);
                            }
                        }
                    }
                    else
                    {
                        Value = value;
                    }
                }
                else
                {
                    Value = value;
                }
            }
            if (Comma.Contains(name.ToLower()))
            {
                HeaderType = "comma";
                if (value.Contains(' '))
                {
                    index = value.IndexOf(' ');
                    AuthMethod = value.Substring(0, index).Trim();
                    Value = value.Substring(0, index).Trim();
                    string values = value.Substring(index + 1);
                    foreach (string parm in values.Split(','))
                    {
                        if (parm.Contains('='))
                        {
                            index = parm.IndexOf('=');
                            string parmName = parm.Substring(0, index);
                            string parmValue = parm.Substring(index + 1);
                            Attributes.Add(parmName, parmValue);
                        }
                    }
                }
            }
            else if (name.ToLower() == "cseq")
            {
                HeaderType = "unstructured";
                string[] parts = value.Trim().Split(' ');
                int tempNumber = -1;
                int.TryParse(parts[0], out tempNumber);
                Number = tempNumber;
                Method = parts[1];
            }
            if (Unstructured.Contains(name.ToLower()) && name.ToLower() != "cseq")
            {
                HeaderType = "unstructured";
                Value = value;
            }
            if (name.ToLower() == "via")
            {
                string[] parts = value.Split(' ');
                string proto = parts[0];
                string addr = parts[1].Split(';')[0];
                string type = proto.Split('/')[2].ToLower();
                ViaUri = new SIPURI("sip:" + addr + ";transport=" + type);
                if (ViaUri.Port == 0)
                {
                    ViaUri.Port = 5060;
                }
                if (Attributes.Keys.Contains("rport"))
                {
                    int tempPort = 5060;
                    int.TryParse(Attributes["rport"], out tempPort);
                    ViaUri.Port = tempPort;
                }
                if ((type != "tcp") && (type != "sctp") && (type != "tls"))
                {
                    if (Attributes.Keys.Contains("maddr"))
                    {
                        ViaUri.Host = Attributes["maddr"];
                    }
                    else if (Attributes.Keys.Contains("received"))
                    {
                        ViaUri.Host = Attributes["received"];
                    }
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance of a header for displaying / printing.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            string name = Name.ToLower();
            StringBuilder sb = new StringBuilder();
            sb.Append(Value);
            if (HeaderType != "comma" && HeaderType != "unstructured")
            {
                foreach (KeyValuePair<string, string> kvp in Attributes)
                {
                    sb.Append(";");
                    sb.Append(kvp.Key + "=" + kvp.Value);
                }
            }
            if ((HeaderType == "comma"))
            {
                sb.Append(" ");
                foreach (KeyValuePair<string, string> kvp in Attributes)
                {
                    sb.Append(kvp.Key + "=" + kvp.Value);
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
            }
            if ((Number > -1))
                sb.Append(" " + Number.ToString());
            if (Method != null)
                sb.Append(" " + Method);
            return sb.ToString();
        }

        /// <summary>
        /// Returns a human readable format of this header - e.g. Header Name:Header Value
        /// </summary>
        /// <returns>System.String.</returns>
        public string Repr()
        {
            return Name + ":" + ToString();
        }

        /// <summary>
        /// Clones this header and returns a new one.
        /// </summary>
        /// <returns>Header.</returns>
        public Header Dup()
        {
            return new Header(ToString(), Name);
        }

        /// <summary>
        /// Creates a list of headers from a text string (multiple headers of same name).
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>List{Header}.</returns>
        public static List<Header> CreateHeaders(string value)
        {
            int index = value.IndexOf(':');
            string name = value.Substring(0, index);
            value = value.Substring(index + 1);
            List<Header> headers = new List<Header>();
            //if (name == "WWW-Authenticate")
            //{
            //    foreach(string part in value.Split(','))
            //    {
            //        headers.Add(new Header(part.Trim(),name));
            //    }
            //}
            if (name == "Record-Route")
            {
                headers.AddRange(value.Split(',').Select(part => new Header(part.Trim(), name)));
            }
            else if (name == "Route" && value.Contains(","))
            {
                headers.AddRange(value.Split(',').Select(part => new Header(part.Trim(), name)));
            }
            else
            {
                headers.Add(new Header(value.Trim(), name));
            }
            return headers;
        }
    }
}