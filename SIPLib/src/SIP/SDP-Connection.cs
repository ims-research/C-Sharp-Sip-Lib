#region

using System.Collections.Generic;
using System.Text;

#endregion

namespace SIPLib.SIP
{
    public class SDPConnection
    {
        public SDPConnection(string value = null, Dictionary<string, string> attrDict = null)
        {
            if (value != null)
            {
                string[] values = value.Split(' ');
                Nettype = values[0];
                Addrtype = values[1];
                string rest = values[2];
                string[] rest2 = rest.Split('/');
                switch (rest2.Length)
                {
                    case 1:
                        Address = rest2[0];
                        break;
                    case 2:
                        Address = rest2[0];
                        TTL = rest2[1];
                        break;
                    default:
                        Address = rest2[0];
                        TTL = rest2[1];
                        Count = rest2[2];
                        break;
                }
            }
            else if (attrDict != null && attrDict.ContainsKey("address"))
            {
                Address = attrDict["address"];
                Nettype = attrDict.ContainsKey("nettype") ? attrDict["nettype"] : "IN";
                Addrtype = attrDict.ContainsKey("addrtype") ? attrDict["addrtype"] : "IP4";
                TTL = attrDict.ContainsKey("ttl") ? attrDict["ttl"] : null;
                Count = attrDict.ContainsKey("count") ? attrDict["count"] : null;
            }
        }

        public string Nettype { get; set; }
        public string Addrtype { get; set; }
        public string Address { get; set; }
        public string TTL { get; set; }
        public string Count { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Nettype + " ");
            sb.Append(Addrtype + " ");
            sb.Append(Address);
            if (TTL != null) sb.Append("/" + TTL);
            if (Count != null) sb.Append("/" + Count);
            return sb.ToString();
        }
    }
}