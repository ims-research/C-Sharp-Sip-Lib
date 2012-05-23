using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib.SIP
{
    public class SDPMedia 
    {
        public string Media { get; set; }
        public string Port { get; set; }
        public string Proto { get; set; }
        public string Count { get; set; }
        public List<SDPMediaFormat> Mediaformats { get; set; }
        public Dictionary<string, string> OtherAttributes { get; set; }

        public SDPMedia(string value = null, Dictionary<string, string> attrDict = null)
        {
            Mediaformats = new List<SDPMediaFormat>();
            OtherAttributes = new Dictionary<string, string>();
            if (value != null)
            {
                string[] values = value.Split(" ".ToCharArray(), 4);
                Media = values[0];
                Port = values[1];
                Proto = values[2];
                string rest = values[3];
                Mediaformats = new List<SDPMediaFormat>();
                foreach (string s in rest.Split(' '))
                {
                    SDPMediaFormat fmt = new SDPMediaFormat {Pt = s};
                    Mediaformats.Add(fmt);
                }
            }
            else if (attrDict != null && attrDict.ContainsKey("media"))
            {
                Media = attrDict["media"];
                Port = attrDict.ContainsKey("port") ? attrDict["port"] : "0";
                Proto = attrDict.ContainsKey("proto") ? attrDict["proto"] : "RTP/AVP";
                Mediaformats = new List<SDPMediaFormat>();
                if (attrDict.ContainsKey("fmt"))
                {
                    foreach (string s in attrDict["fmt"].Split(' '))
                    {
                        SDPMediaFormat fmt = new SDPMediaFormat {Pt = s};
                        Mediaformats.Add(fmt);
                    }
                }
            }
        }

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Media + " ");
            sb.Append(Port + " ");
            sb.Append(Proto + " ");
            foreach (SDPMediaFormat mf in Mediaformats)
            {
                sb.Append(mf.Pt + " ");
            }
            foreach (Char c in "icbka")
            {
                if (OtherAttributes.ContainsKey(c.ToString()))
                {
                    if (!SDP.Multiple.Contains(c))
                    {
                        sb.Append("\r\n" + c + "=" + OtherAttributes[c.ToString()]);
                    }
                    else
                    {
                        // TODO: Handle multiple values of same type
                        //
                        //foreach (AttributeClass header in this[c.ToString(), true])
                        //{
                            //sb.Append("\r\n" + c + "=" + header.value);
                        //}
                        sb.Append("\r\n" + c + "=" + this.OtherAttributes[c.ToString()]);
                    }
                }
            }
            foreach (SDPMediaFormat mf in Mediaformats)
            {
                if (mf.Name.Length > 0)
                {
                    sb.Append("\r\n" + "a=rtpmap:" + mf.Pt + " " + mf.Name + "/" + mf.Rate);
                    if (mf.Parameters.Length > 0)
                    {
                        sb.Append("/" + mf.Parameters);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
