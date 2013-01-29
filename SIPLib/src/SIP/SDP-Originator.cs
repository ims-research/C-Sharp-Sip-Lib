#region

using System;
using System.Net;
using SIPLib.Utils;

#endregion

namespace SIPLib.SIP
{
    public class SDPOriginator
    {
        public SDPOriginator(string value = null)
        {
            if (value != null)
            {
                string[] values = value.Split(' ');
                Username = values[0];
                Sessionid = values[1];
                Version = values[2];
                Nettype = values[3];
                Addrtype = values[4];
                Address = values[5];
            }
            else
            {
                string hostname = Dns.GetHostName();
                IPHostEntry ip = Dns.GetHostEntry(hostname);
                string ipAddress = ip.ToString();
                Username = "-";
                Sessionid = Helpers.ToUnixTime(DateTime.Now).ToString();
                Version = Helpers.ToUnixTime(DateTime.Now).ToString();
                Nettype = "IN";
                Addrtype = "IP4";
                Address = ipAddress;
            }
        }

        public string Username { get; set; }
        public string Sessionid { get; set; }
        public string Version { get; set; }
        public string Nettype { get; set; }
        public string Addrtype { get; set; }
        public string Address { get; set; }

        public override string ToString()
        {
            return Username + " " + Sessionid + " " + Version + " " + Nettype + " " + Addrtype + " " + Address;
        }
    }
}