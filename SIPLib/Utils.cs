using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.Security.Cryptography;

namespace SIPLib
{
    public static class Utils
    {

        public enum SipMethods
        {
            REGISTER, INVITE, ACK, BYE, CANCEL, MESSAGE, OPTIONS, PRACK,
            SUBSCRIBE, NOTIFY, PUBLISH, INFO, REFER, UPDATE
        }

        public static bool isIPv4(string input)
        {
            IPAddress[] addresses;
            try
            {
                addresses = System.Net.Dns.GetHostAddresses(input);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error resolving domain name");
                return false;
            }
            IPAddress address;
            if (IPAddress.TryParse(addresses[0].ToString(), out address))
            {
                switch (address.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        return true;
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        return false;
                    default:
                        return false;
                }
            }
            else return false;
        }

        public static string Base64Encode(string str)
        {
            byte[] encbuff = Encoding.UTF8.GetBytes(str);
            return System.Convert.ToBase64String(encbuff);
        }

        public static string Base64Decode(string str)
        {
            byte[] decbuff = System.Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(decbuff);
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

        private static DateTime UnixTime
        {
        get { return new DateTime(1970, 1, 1); }
        }
 
        public static DateTime FromUnixTime( double unixTime )
        {
        return UnixTime.AddSeconds( unixTime );
        }
 
        public static double ToUnixTime( DateTime dateTime )
        {
        TimeSpan timeSpan = dateTime - UnixTime;
        return timeSpan.TotalSeconds;
        }

	public static string quote(string input)
	{
		return "\"" + input.Trim('"') + "\"";
	}

	public static string unquote(string input)
	{
		return input.Trim('"');	
	}

    public static bool isRequest(Message message)
    {
        return isRequest(message.method);
    }

    public static bool isRequest(string request_line)
    {
        foreach (string method in Enum.GetNames(typeof(SipMethods)))
        {
            if (request_line.Contains(method))
            { return true; }
        }
        return false;
    }

    public static string get_local_ip()
    {
        string strHostName = "";
        strHostName = Dns.GetHostName();
        IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
        IPAddress[] addr = ipEntry.AddressList;
        for (int i = 0; i < addr.Length; i++)
        {
            if (addr[i].AddressFamily.ToString() == "InterNetwork")
            {
                if (addr[i].ToString() == "127.0.0.1")
                {
                    break;
                }
                return addr[i].ToString();
            }
        }
        return "127.0.0.1";
    }

    }
}
