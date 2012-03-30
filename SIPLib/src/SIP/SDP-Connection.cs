using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class SDPConnection
    {
        public string nettype { get; set; }
        public string addrtype { get; set; }
        public string address { get; set; }
        public string ttl { get; set; }
        public string count { get; set; }

        public SDPConnection(string value = null,Dictionary<string,string> attr_dict = null)
        {
            string rest = null;
            string[] rest2 = null;
            if (value != null)
            {
                string[] values = value.Split(' ');
                this.nettype = values[0];
                this.addrtype = values[1];
                rest = values[2];
                rest2 = rest.Split('/');
                if (rest2.Length == 1)
                {
                    this.address = rest2[0];
                }
                else if (rest2.Length == 2)
                {
                    this.address = rest2[0];
                    this.ttl = rest2[1];
                }
                else
                {
                    this.address = rest2[0];
                    this.ttl = rest2[1];
                    this.count = rest2[2];
                }
            }
            else if (attr_dict.ContainsKey("address"))
            {
                this.address = attr_dict["address"];
                this.nettype = attr_dict.ContainsKey("nettype") ? attr_dict["nettype"] : "IN";
                this.addrtype = attr_dict.ContainsKey("addrtype") ? attr_dict["addrtype"] : "IP4";
                this.ttl = attr_dict.ContainsKey("ttl") ? attr_dict["ttl"] : null;
                this.count = attr_dict.ContainsKey("count") ? attr_dict["count"] : null;
          }
        }

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(nettype+" ");
            sb.Append(addrtype + " ");
            sb.Append(address);
            if (this.ttl != null) sb.Append("/"+ttl);
            if (this.count != null) sb.Append("/"+count);
            return sb.ToString();
        }
    }
}
