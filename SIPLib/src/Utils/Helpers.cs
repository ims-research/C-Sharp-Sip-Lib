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
    public static class Helpers
    {
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

        private static DateTime UnixTime
        {
            get { return new DateTime(1970, 1, 1); }
        }

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

        public static string Base64Encode(string str)
        {
            byte[] encbuff = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(encbuff);
        }

        public static string Base64Decode(string str)
        {
            byte[] decbuff = Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(decbuff);
        }

        public static string RemoveAngelBrackets(string str)
        {
            if (str.StartsWith("<")) str = str.Remove(0, 1);
            if (str.EndsWith(">")) str = str.Remove(str.LastIndexOf(">"), 1);
            return str;
        }

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

        public static DateTime FromUnixTime(double unixTime)
        {
            return UnixTime.AddSeconds(unixTime);
        }

        public static double ToUnixTime(DateTime dateTime)
        {
            TimeSpan timeSpan = dateTime - UnixTime;
            return timeSpan.TotalSeconds;
        }

        public static string Quote(string input)
        {
            return "\"" + input.Trim('"') + "\"";
        }

        public static string Unquote(string input)
        {
            return input.Trim('"');
        }

        public static bool IsRequest(Message message)
        {
            if (message.Method == null) return false;
            return IsRequest(message.Method);
        }

        public static bool IsRequest(string requestLine)
        {
            return Enum.GetNames(typeof (SipMethods)).Any(requestLine.Contains);
        }

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