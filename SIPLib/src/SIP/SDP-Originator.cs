using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using SIPLib.utils;

namespace SIPLib
{
    public class SDPOriginator
    {
        public string username { get; set; }
        public string sessionid { get; set; }
        public string version { get; set; }
        public string nettype { get; set; }
        public string addrtype { get; set; }
        public string address { get; set; }

        public SDPOriginator(string value = null)
        {
            if (value != null)
            {
                string[] values = value.Split(' ');
                this.username = values[0];
                this.sessionid = values[1];
                this.version = values[2];
                this.nettype = values[3];
                this.addrtype = values[4];
                this.address = values[5];
            }
            else
            {
                string hostname = Dns.GetHostName();
                IPHostEntry ip = Dns.GetHostEntry(hostname);
                string ip_address = ip.ToString();
                this.username = "-";
                this.sessionid = Utils.ToUnixTime(DateTime.Now).ToString();
                this.version = Utils.ToUnixTime(DateTime.Now).ToString();
                this.nettype = "IN";
                this.addrtype = "IP4";
                this.address = ip_address;
            }
        }

        public string ToString()
        {
            return this.username + " " + this.sessionid + " " + this.version + " " + this.nettype + " " + this.addrtype + " " + this.address;
        }
    }
}
