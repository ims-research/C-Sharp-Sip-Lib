// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 01-29-2013
// ***********************************************************************
// <copyright file="Helpers.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using SIPLib.SIP;
using log4net;

#endregion

namespace SIPLib.Utils
{
    /// <summary>
    /// This class contains static helper functions to provide several utility functions.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Enum representing the possible SIP methods.
        /// </summary>
        public enum SipMethods
        {
            REGISTER,
            INVITE,
            ACK,
            BYE,
            CANCEL,
            MESSAGE,
            OPTIONS,
            PRACK,
            SUBSCRIBE,
            NOTIFY,
            PUBLISH,
            INFO,
            REFER,
            UPDATE
        }

        private static ILog _log = LogManager.GetLogger(typeof (SIPStack));

        /// <summary>
        /// Returns a DateTime object for the Unix Epoch.
        /// </summary>
        /// <value>The unix time.</value>
        private static DateTime UnixTime
        {
            get { return new DateTime(1970, 1, 1); }
        }

        /// <summary>
        /// Determines whether an IP address is an IPV4 address.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns><c>true</c> if the IP is IPV4; otherwise, <c>false</c>.</returns>
        public static bool IsIPv4(string input)
        {
            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(input);
            }
            catch (Exception ex)
            {
                _log.Error("Error resolving domain name, check DNS server and local network properties");
                _log.Error(ex.Message);
                return false;
            }
            IPAddress address;
            if (IPAddress.TryParse(addresses[0].ToString(), out address))
            {
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        return true;
                    case AddressFamily.InterNetworkV6:
                        return false;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Base64 encodes the string.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>System.String.</returns>
        public static string Base64Encode(string str)
        {
            byte[] encbuff = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(encbuff);
        }

        /// <summary>
        /// Base64 decodes the input string.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>System.String.</returns>
        public static string Base64Decode(string str)
        {
            byte[] decbuff = Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(decbuff);
        }

        /// <summary>
        /// Removes angel brackets.
        /// </summary>
        /// <param name="str">The input string with angle brackets.</param>
        /// <returns>System.String.</returns>
        public static string RemoveAngelBrackets(string str)
        {
            if (str.StartsWith("<")) str = str.Remove(0, 1);
            if (str.EndsWith(">")) str = str.Remove(str.LastIndexOf(">"), 1);
            return str;
        }

        /// <summary>
        /// Helper function to calculate a MD5 hash of the input string.
        /// </summary>
        /// <param name="md5Hash">The MD5 hash object.</param>
        /// <param name="input">The input string.</param>
        /// <returns>System.String.</returns>
        public static string GetMd5Hash(MD5 md5Hash, string input)
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
        /// Helper function used in converting from unix time.
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <returns>DateTime.</returns>
        public static DateTime FromUnixTime(double unixTime)
        {
            return UnixTime.AddSeconds(unixTime);
        }

        /// <summary>
        /// Helper function used in converting to unix time.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>System.Double.</returns>
        public static double ToUnixTime(DateTime dateTime)
        {
            TimeSpan timeSpan = dateTime - UnixTime;
            return timeSpan.TotalSeconds;
        }

        /// <summary>
        /// Helper function to add quotation marks to a string
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>System.String.</returns>
        public static string Quote(string input)
        {
            return "\"" + input.Trim('"') + "\"";
        }

        /// <summary>
        /// Helper function to remove quotation marks from a string
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>System.String.</returns>
        public static string Unquote(string input)
        {
            return input.Trim('"');
        }

        /// <summary>
        /// Determines whether the specified message is a request message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns><c>true</c> if the specified message is request; otherwise, <c>false</c>.</returns>
        public static bool IsRequest(Message message)
        {
            if (message.Method == null) return false;
            return IsRequest(message.Method);
        }

        /// <summary>
        /// Determines whether the specified request line is from a request.
        /// </summary>
        /// <param name="requestLine">The request line.</param>
        /// <returns><c>true</c> if the specified request line is request; otherwise, <c>false</c>.</returns>
        public static bool IsRequest(string requestLine)
        {
            return Enum.GetNames(typeof (SipMethods)).Any(requestLine.Contains);
        }

        /// <summary>
        /// Helper function to return the the local IP.
        /// </summary>
        /// <returns>System.String.</returns>
        public static string GetLocalIP()
        {
            string strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            foreach (
                IPAddress t in
                    addr.Where(t => t.AddressFamily.ToString() == "InterNetwork")
                        .TakeWhile(t => t.ToString() != "127.0.0.1"))
            {
                return t.ToString();
            }
            return "127.0.0.1";
        }
    }
}