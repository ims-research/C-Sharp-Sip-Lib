// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="SIPURI.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent a SIPURI (“Firstname Lastname” &lt;sip:firstname@domain.net&gt; etc)
    /// </summary>
    public class SIPURI
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SIPURI" /> class.
        /// </summary>
        /// <param name="uri">A string representing a SIP URI.</param>
        public SIPURI(string uri)
        {
            Console.WriteLine("Creating URI with: "+ uri);
            Init();
            nonRegExpInit(uri);
        }

        public SIPURI(string uri, bool useRegex)
        {
            Console.WriteLine("Creating URI with: " + uri);
            Init();
            if (useRegex)
            {
                regExInit(uri);
            }
            else
            {
                nonRegExpInit(uri);
            }
        }

        /// <summary>
        /// Initializes an empty instance of the <see cref="T:SIPLib.SIP.SIPURI" /> class.
        /// </summary>
        public SIPURI()
        {
            Init();
        }

        private void regExInit(string uri)
        {
            const string regEx =
                @"^(?<scheme>[a-zA-Z][a-zA-Z0-9\+\-\.]*):(((?<user>[a-zA-Z0-9\-_\.\!\~\*\'\(\)&=\+\$,;\?\/\%]+)(:(?<password>[^:@;\?]+))?)@)?(((?<host>[^;\?:]*)(:(?<port>[\d]+))?))(;(?<params>[^\?]*))?(\?(?<headers>.*))?$";
            Regex exp = new Regex(regEx, RegexOptions.IgnoreCase);

            MatchCollection mc = exp.Matches(uri);
            string param = "";
            string head = "";
            foreach (Match m in mc)
            {
                Scheme = m.Groups["scheme"].ToString();
                User = m.Groups["user"].ToString();
                Password = m.Groups["password"].ToString();
                Host = m.Groups["host"].ToString();
                int tempPort = 0;
                int.TryParse(m.Groups["port"].ToString(), out tempPort);
                Port = tempPort;
                param = m.Groups["params"].ToString();
                head = m.Groups["headers"].ToString();
            }
            if ((Scheme == "tel") && (User == ""))
            {
                User = Host;
                Host = null;
            }
            foreach (string parameter in param.Split(';'))
            {
                if (parameter.Contains('='))
                {
                    int index = parameter.IndexOf('=');
                    string paramName = parameter.Substring(0, index);
                    string paramValue = parameter.Substring(index + 1);
                    Parameters.Add(paramName, paramValue);
                }
                else if (parameter.ToLower() == "lr")
                {
                    Parameters.Add(parameter, "");
                }
                else break;
            }
            foreach (string header in head.Split('&'))
            {
                if (header.Contains('='))
                {
                    int index = header.IndexOf('=');
                    string headerName = header.Substring(0, index);
                    string headerValue = header.Substring(index + 1);
                    Headers.Add(headerName, headerValue);
                }
                else
                    break;
            }
        }

        private void nonRegExpInit(string uri)
        {
            Console.WriteLine("Creating URI in new method with: " + uri);
            Init();
            string[] strings = uri.Split(':');
            Scheme = strings[0];
            string[] userHost = strings[1].Split('@');
            if (userHost.Length > 1)
            {
                User = userHost[0];
                Host = userHost[1];
            }
            else
            {
                Host = userHost[0];
                User = "";
            }
            int tempPort = 0;
            if (strings.Length > 2)
            {
                string port = strings[2].Split(';')[0];
                int.TryParse(port, out tempPort);
            }
            Port = tempPort;
            string param = "";
            string head = "";
            Password = "";
            string[] paramSection = uri.Split(';');
            if (paramSection.Length > 1)
            {
                param = String.Join(";", paramSection.Skip(1));
            }
            if ((Scheme == "tel") && (User == ""))
            {
                User = Host;
                Host = null;
            }
            foreach (string parameter in param.Split(';'))
            {
                if (parameter.Contains('='))
                {
                    int index = parameter.IndexOf('=');
                    string paramName = parameter.Substring(0, index);
                    string paramValue = parameter.Substring(index + 1);
                    Parameters.Add(paramName, paramValue);
                }
                else if (parameter.ToLower() == "lr")
                {
                    Parameters.Add(parameter, "");
                }
                else break;
            }
            foreach (string header in head.Split('&'))
            {
                if (header.Contains('='))
                {
                    int index = header.IndexOf('=');
                    string headerName = header.Substring(0, index);
                    string headerValue = header.Substring(index + 1);
                    Headers.Add(headerName, headerValue);
                }
                else
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the scheme (e.g. sip)
        /// </summary>
        /// <value>The scheme (e.g. sip)</value>
        public string Scheme { get; set; }
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public string User { get; set; }
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }
        /// <summary>
        /// Gets or sets the IP.
        /// </summary>
        /// <value>The IP.</value>
        public string IP { get; set; }
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }
        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public Dictionary<string, string> Parameters { get; set; }
        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Initializes the SIPURI object
        /// </summary>
        private void Init()
        {
            Parameters = new Dictionary<string, string>();
            Headers = new Dictionary<string, string>();

            Scheme = null;
            User = null;
            Password = null;
            Host = null;
            Port = 0;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string host;
            string user;
            if (!String.IsNullOrEmpty(Scheme) && Scheme.ToLower() == "tel")
            {
                user = "";
                host = User;
            }
            else
            {
                user = User;
                host = Host;
            }
            if (!String.IsNullOrEmpty(Scheme))
            {
                sb.Append(Scheme + ":");
                if (user.Length > 0)
                {
                    sb.Append(user);
                    if (Password.Length > 0)
                        sb.Append(":" + Password);
                    sb.Append("@");
                }
                if (host.Length > 0)
                {
                    sb.Append(host);
                    if (Port != 0)
                        sb.Append(":" + Port.ToString());
                }
                if (Parameters.Count > 0)
                {
                    sb.Append(";");
                    foreach (KeyValuePair<string, string> kvp in Parameters)
                    {
                        if (kvp.Key.ToLower() == "lr")
                        {
                            sb.Append(kvp.Key);
                        }
                        else
                        {
                            sb.Append(kvp.Key + "=" + kvp.Value);
                        }
                        sb.Append(";");
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
                if (Headers.Count > 0)
                {
                    sb.Append("?");
                    foreach (KeyValuePair<string, string> kvp in Headers)
                    {
                        sb.Append(kvp.Key + "=" + kvp.Value);
                        sb.Append("&");
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Clones this SIPURI and returns a new SIPURI
        /// </summary>
        /// <returns>SIPURI.</returns>
        public SIPURI Dup()
        {
            return new SIPURI(ToString());
        }

        /// <summary>
        /// Returns a md5 hash based on the lowercased string contents of this SIPURI.
        /// </summary>
        /// <returns>System.String.</returns>
        public string Hash()
        {
            MD5 m = MD5.Create();
            string hash = GetMd5Hash(m, ToString().ToLower());
            return hash;
        }

        /// <summary>
        /// Helper function used to get the MD5 hash of the inputed string using the MD5 object passed in.
        /// </summary>
        /// <param name="md5Hash">The MD5 hash.</param>
        /// <param name="input">The input.</param>
        /// <returns>System.String.</returns>
        private static string GetMd5Hash(MD5 md5Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        /// <summary>
        /// Helper function to compares this SIPURI to another specified SIPURI.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Compare(SIPURI other)
        {
            return ((ToString().ToLower()) == (other.ToString().ToLower()));
        }

        /// <summary>
        /// Helper function returning a string combining the SIPURI's host and port.
        /// </summary>
        /// <returns>System.String.</returns>
        public string HostPort()
        {
            return Host + ":" + Port.ToString();
        }
    }
}