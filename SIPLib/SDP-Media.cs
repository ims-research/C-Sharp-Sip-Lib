using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class SDPMedia 
    {
        public string media { get; set; }
        public string port { get; set; }
        public string proto { get; set; }
        public string count { get; set; }
        public List<SDPMediaFormat> mediaformats { get; set; }
        public Dictionary<string, string> other_attributes { get; set; }

        public SDPMedia(string value = null, Dictionary<string, string> attr_dict = null)
        {
            string rest = null;
            this.mediaformats = new List<SDPMediaFormat>();
            this.other_attributes = new Dictionary<string, string>();
            if (value != null)
            {
                string[] values = value.Split(" ".ToCharArray(), 4);
                this.media = values[0];
                this.port = values[1];
                this.proto = values[2];
                rest = values[3];
                this.mediaformats = new List<SDPMediaFormat>();
                foreach (string s in rest.Split(' '))
                {
                    SDPMediaFormat fmt = new SDPMediaFormat();
                    fmt.pt = s;
                    this.mediaformats.Add(fmt);
                }
            }
            else if (attr_dict.ContainsKey("media"))
            {
                this.media = attr_dict["media"];
                this.port = attr_dict.ContainsKey("port") ? attr_dict["port"] : "0";
                this.proto = attr_dict.ContainsKey("proto") ? attr_dict["proto"] : "RTP/AVP";
                this.mediaformats = new List<SDPMediaFormat>();
                if (attr_dict.ContainsKey("fmt"))
                {
                    foreach (string s in attr_dict["fmt"].Split(' '))
                    {
                        SDPMediaFormat fmt = new SDPMediaFormat();
                        fmt.pt = s;
                        this.mediaformats.Add(fmt);
                    }
                }
            }
        }

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.media + " ");
            sb.Append(this.port + " ");
            sb.Append(this.proto + " ");
            foreach (SDPMediaFormat mf in this.mediaformats)
            {
                sb.Append(mf.pt + " ");
            }
            foreach (Char c in "icbka")
            {
                if (this.other_attributes.ContainsKey(c.ToString()))
                {
                    if (!SDP._multiple.Contains(c))
                    {
                        sb.Append("\r\n" + c + "=" + this.other_attributes[c.ToString()]);
                    }
                    else
                    {
                        // TODO: Handle multiple values of same type
                        //
                        //foreach (AttributeClass header in this[c.ToString(), true])
                        //{
                            //sb.Append("\r\n" + c + "=" + header.value);
                        //}
                        sb.Append("\r\n" + c + "=" + this.other_attributes[c.ToString()]);
                    }
                }
            }
            foreach (SDPMediaFormat mf in this.mediaformats)
            {
                if (mf.name.Length > 0)
                {
                    sb.Append("\r\n" + "a=rtpmap:" + mf.pt + " " + mf.name + "/" + mf.rate);
                    if (mf.parameters.Length > 0)
                    {
                        sb.Append("/" + mf.parameters);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
