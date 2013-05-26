// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-26-2013
// ***********************************************************************
// <copyright file="Address.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent a SIP Address,
    /// e.g. "Display Name + &lt;sip_uri@example.com&gt;"
    /// </summary>
    public class Address
    {
        /// <summary>
        /// Initializes a new empty instance of the <see cref="T:SIPLib.SIP.Address"/> class.
        /// </summary>
        public Address()
        {
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.Address"/> class from a text string.
        /// </summary>
        /// <param name="address">A string representation of the SIP address.</param>
        public Address(string address)
        {
            Init();
            Parse(address);
        }

        /// <summary>
        /// Gets or sets the display name. The display name is normally how a SIP contact should be displayed to the end user.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; set; }
        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>The URI.</value>
        public SIPURI Uri { get; set; }
        /// <summary>
        /// There is a special SIP address which contains a wild card (“*”) which should only be used for a Contact header.
        /// Gets or sets a value indicating whether this specific <see cref="T:SIPLib.SIP.Address"/> is a wild card address.
        /// </summary>
        /// <value><c>true</c> if wildcard; otherwise, <c>false</c>.</value>
        public bool Wildcard { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the &lt; and &gt; characters should be appended after the URI.
        /// </summary>
        /// <value><c>true</c> if [must quote]; otherwise, <c>false</c>.</value>
        public bool MustQuote { get; set; }

        /// <summary>
        /// Initializes the necessary objects for the class.
        /// </summary>
        private void Init()
        {
            DisplayName = "";
            Uri = new SIPURI();
            Wildcard = false;
        }

        /// <summary>
        /// Parses the specified address and sets the local variables.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>System.Int32.</returns>
        public int Parse(string address)
        {
            if (address.StartsWith("*"))
            {
                Wildcard = true;
                return 1;
            }
            string[] regExs =
                {
                    @"^(?<name>[a-zA-Z0-9\-\._\+\~\ \t]*)<(?<uri>[^>]+)>",
                    @"^(""(?<name>[a-zA-Z0-9\-\._\+\~\ \t]+)"")[\ \t]*<(?<uri>[^>]+)>",
                    @"^[\ \t]*(?<name>)(?<uri>[^;]+)"
                };

            foreach (string expression in regExs)
            {
                Regex exp = new Regex(expression, RegexOptions.IgnoreCase);
                MatchCollection mc = exp.Matches(address);
                foreach (Match m in mc)
                {
                    DisplayName = m.Groups["name"].ToString().Trim();
                    Uri = new SIPURI(m.Groups["uri"].ToString().Trim());
                    return m.Length;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> instance that that represents this instance with a bit of formatting.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (DisplayName.Length > 0)
            {
                sb.Append("\"" + DisplayName + "\"");
                if (Uri.ToString().Length > 0)
                {
                    sb.Append(" ");
                }
            }

            if (Uri.ToString().Length > 0)
            {
                if ((MustQuote) || (DisplayName.Length > 0))
                {
                    sb.Append("<");
                }
                sb.Append(Uri);
                if ((MustQuote) || (DisplayName.Length > 0))
                {
                    sb.Append(">");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a new clone of this instance.
        /// </summary>
        /// <returns>Address.</returns>
        public Address Dup()
        {
            return new Address(ToString());
        }

        /// <summary>
        /// Returns a human readable, shortened length string representing the contact or address.
        /// </summary>
        /// <returns>System.String.</returns>
        public string Displayable()
        {
            string name = "";
            if (DisplayName.Length > 0)
            {
                name = DisplayName;
            }
            else if (Uri.User.Length > 0)
            {
                name = Uri.User;
            }
            else if (Uri.Host.Length > 0)
            {
                name = Uri.Host;
            }
            if (name.Length > 26)
            {
                name = name.Substring(0, 22);
                name = name + "...";
            }
            return name;
        }
    }
}