// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="Authenticate.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SIPLib.Utils;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to handle the SIP authentication headers.
    /// </summary>
    public class Authenticate
    {
        /// <summary>
        /// Random number generator.
        /// </summary>
        private static readonly Random Random = new Random();

        /// <summary>
        /// Creates the authentication header.
        /// </summary>
        /// <param name="authMethod">The auth method.</param>
        /// <param name="parameters">The parameters used.</param>
        /// <returns>System.String.</returns>
        public static string CreateAuthenticate(string authMethod = "Digest",
                                                Dictionary<string, string> parameters = null)
        {
            if (parameters == null) parameters = new Dictionary<string, string>();
            authMethod = authMethod.ToLower();
            if (authMethod.Equals("basic"))
            {
                return "Basic realm=" + Helpers.Quote(parameters.ContainsKey("realm") ? parameters["realm"] : "0");
            }
            if (authMethod.Equals("digest"))
            {
                string[] predef = {"realm", "domain", "qop", "nonce", "opaque", "stale", "algorithm"};
                string[] unquoted = {"stale", "algorithm"};
                double time = Helpers.ToUnixTime(DateTime.Now);
                Guid guid = new Guid();
                string md5Hashstr;
                using (MD5 md5Hash = MD5.Create())
                {
                    md5Hashstr = Helpers.GetMd5Hash(md5Hash, time.ToString() + ":" + guid.ToString());
                }
                string nonce = Helpers.Base64Encode(time.ToString() + " " + md5Hashstr);
                nonce = (parameters.ContainsKey("nonce") ? parameters["nonce"] : nonce);
                Dictionary<string, string> defaultDict = new Dictionary<string, string>
                    {
                        {"realm", ""},
                        {"domain", ""},
                        {"opaque", ""},
                        {"stale", "FALSE"},
                        {"algorithm", "MD5"},
                        {"qop", "auth"},
                        {"nonce", nonce}
                    };

                Dictionary<string, string> kv = predef.ToDictionary(s => s,
                                                                    s =>
                                                                    parameters.ContainsKey(s)
                                                                        ? parameters[s]
                                                                        : defaultDict[s]);
                foreach (KeyValuePair<string, string> kvp in parameters)
                {
                    if (!predef.Contains(kvp.Key))
                    {
                        kv.Add(kvp.Key, kvp.Value);
                    }
                }
                StringBuilder sb = new StringBuilder();
                sb.Append("Digest ");

                foreach (KeyValuePair<string, string> kvp in kv)
                {
                    if (unquoted.Contains(kvp.Key))
                    {
                        sb.Append(", ");
                        sb.Append(kvp.Key);
                        sb.Append("=");
                        sb.Append(kvp.Value);
                    }
                    else
                    {
                        sb.Append(", ");
                        sb.Append(kvp.Key);
                        sb.Append("=");
                        sb.Append(Helpers.Quote(kvp.Value));
                    }
                }
                sb.Replace("Digest , ", "Digest ");
                return sb.ToString();
            }
            Debug.Assert(false, String.Format("Invalid authMethod " + authMethod));
            return null;
        }

        /// <summary>
        /// Creates the actual authorization header values.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="method">The method.</param>
        /// <param name="entityBody">The entity body.</param>
        /// <param name="context">The context.</param>
        /// <returns>System.String.</returns>
        public static string CreateAuthorization(string challenge, string username, string password, string uri = null,
                                                 string method = null, string entityBody = null,
                                                 Dictionary<string, string> context = null)
        {
            challenge = challenge.Trim();
            string[] values = challenge.Split(" ".ToCharArray(), 2);
            string authMethod = values[0];
            string rest = values[1];
            Dictionary<string, string> ch = new Dictionary<string, string>();
            Dictionary<string, string> cr = new Dictionary<string, string>();
            cr["password"] = password;
            cr["username"] = username;
            if (authMethod.ToLower() == "basic")
            {
                return authMethod + " " + Basic(cr);
            }
            if (authMethod.ToLower() == "digest")
            {
                if (rest.Length > 0)
                {
                    foreach (string pairs in rest.Split(','))
                    {
                        string[] sides = pairs.Trim().Split('=');
                        ch[sides[0].ToLower().Trim()] = Helpers.Unquote(sides[1].Trim());
                    }
                }
                foreach (string s in new[] {"username", "realm", "nonce", "opaque", "algorithm"})
                {
                    if (ch.ContainsKey(s))
                    {
                        cr[s] = ch[s];
                    }
                }
                if (uri != null)
                {
                    cr["uri"] = uri;
                }
                if (method != null)
                {
                    cr["httpMethod"] = method;
                }
                if (ch.ContainsKey("qop"))
                {
                    string cnonce;
                    int nc;
                    if (context != null && context.ContainsKey("cnonce"))
                    {
                        cnonce = context["cnonce"];
                        nc = Int32.Parse(context["nc"]) + 1;
                    }
                    else
                    {
                        int randomInt = Random.Next(0, 2147483647);
                        cnonce = H(randomInt.ToString());
                        nc = 1;
                    }
                    if (context != null)
                    {
                        context["cnonce"] = cnonce;
                        context["nc"] = nc.ToString();
                    }
                    cr["qop"] = "auth";
                    cr["cnonce"] = cnonce;
                    cr["nc"] = Convert.ToString(nc, 10).PadLeft(8, '0');
                }
                cr["response"] = Digest(cr);
                Dictionary<string, string> items = (from kvp in cr
                                                    let filter =
                                                        new[]
                                                            {
                                                                "name", "authMethod", "value", "httpMethod",
                                                                "entityBody",
                                                                "password"
                                                            }
                                                    where !filter.Contains(kvp.Key)
                                                    select kvp).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                StringBuilder sb = new StringBuilder();
                sb.Append(authMethod + " ");

                foreach (KeyValuePair<string, string> kvp in items)
                {
                    if (kvp.Key == "cnonce")
                    {
                        // TODO re-enable cnonce values
                    }
                    else if (kvp.Key == "algorithm")
                    {
                        sb.Append(", ");
                        sb.Append(kvp.Key);
                        sb.Append("=");
                        sb.Append(kvp.Value);
                    }
                    else if (!(kvp.Key == "qop" || kvp.Key == "nc"))
                    {
                        sb.Append(", ");
                        sb.Append(kvp.Key);
                        sb.Append("=");
                        sb.Append(Helpers.Quote(kvp.Value));
                    }
                    else
                    {
                        // TODO re-enable qop/nc values
                        //sb.Append(", ");
                        //sb.Append(kvp.Key);
                        //sb.Append("=");
                        //sb.Append(kvp.Value);
                    }
                }
                sb.Replace(authMethod + " , ", authMethod + " ");
                return sb.ToString();
            }
            Debug.Assert(false, String.Format("Invalid auth Method -- " + authMethod));
            return null;
        }

        /// <summary>
        /// Creates the digest version of the challenge response.
        /// </summary>
        /// <param name="cr">The challenge/response.</param>
        /// <returns>System.String.</returns>
        public static string Digest(Dictionary<string, string> cr)
        {
            string nc;
            string algorithm = cr.ContainsKey("algorithm") ? cr["algorithm"] : null;
            string username = cr.ContainsKey("username") ? cr["username"] : null;
            string realm = cr.ContainsKey("realm") ? cr["realm"] : null;
            string password = cr.ContainsKey("password") ? cr["password"] : null;
            string nonce = cr.ContainsKey("nonce") ? cr["nonce"] : null;
            string cnonce = cr.ContainsKey("cnonce") ? cr["cnonce"] : null;
            nc = cr.ContainsKey("nc") ? cr["nc"] : null;
            string qop = cr.ContainsKey("qop") ? cr["qop"] : null;
            string httpMethod = cr.ContainsKey("httpMethod") ? cr["httpMethod"] : null;
            string uri = cr.ContainsKey("uri") ? cr["uri"] : null;
            string entityBody = cr.ContainsKey("entityBody") ? cr["entityBody"] : null;
            string A1, A2;

            if (algorithm != null && algorithm.ToLower() == "md5-sess")
            {
                A1 = H(username + ":" + realm + ":" + password) + ":" + nonce + ":" + cnonce;
            }
            else
            {
                A1 = username + ":" + realm + ":" + password;
            }

            if (qop == null || qop == "auth")
            {
                A2 = httpMethod + ":" + uri;
            }
            else
            {
                A2 = httpMethod + ":" + uri + ":" + H(entityBody);
            }
            string a;
            // TODO Re-enable qop/auth
            //if (qop != null && (qop == "auth" || qop == "auth-int"))
            //{
            //    a = nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + A2;
            //    return Utils.quote(KD(H(A1), nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + H(A2)));
            //}
            //else
            //{
            return Helpers.Quote(KD(H(A1), nonce + ":" + H(A2)));
            //}
        }

        /// <summary>
        /// Base64 encodes username:password from the challenge/response
        /// </summary>
        /// <param name="cr">The cr.</param>
        /// <returns>System.String.</returns>
        public static string Basic(Dictionary<string, string> cr)
        {
            return Helpers.Base64Encode(cr["username"] + ":" + cr["password"]);
        }

        /// <summary>
        /// Helper function to md5hash the input string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>System.String.</returns>
        private static string H(string input)
        {
            MD5 md5Hash = MD5.Create();
            return Helpers.GetMd5Hash(md5Hash, input);
        }

        /// <summary>
        /// Returns a md5hash of s:d
        /// </summary>
        /// <param name="s">Input string s.</param>
        /// <param name="d">Input string d.</param>
        /// <returns>System.String.</returns>
        private static string KD(string s, string d)
        {
            MD5 md5Hash = MD5.Create();
            return Helpers.GetMd5Hash(md5Hash, s + ":" + d);
        }
    }
}