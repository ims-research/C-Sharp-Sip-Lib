using System;
using System.Net;
using SIPLib.utils;

namespace SIPLib.SIP
{
    public class SDPOriginator
    {
        public string Username { get; set; }
        public string Sessionid { get; set; }
        public string Version { get; set; }
        public string Nettype { get; set; }
        public string Addrtype { get; set; }
        public string Address { get; set; }

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
                Sessionid = Utils.ToUnixTime(DateTime.Now).ToString();
                Version = Utils.ToUnixTime(DateTime.Now).ToString();
                Nettype = "IN";
                Addrtype = "IP4";
                Address = ipAddress;
            }
        }

        public string ToString()
        {
            return Username + " " + Sessionid + " " + Version + " " + Nettype + " " + Addrtype + " " + Address;
        }
    }
}
