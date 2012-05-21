using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
namespace SIPLib
{
    public class Header
    {
        static string[] _address = { "contact", "from", "record-route", "refer-to", "referred-by", "route", "to" };
        static string[] _comma = { "authorization", "proxy-authenticate", "proxy-authorization", "www-authenticate" };
        static string[] _unstructured = { "call-id", "cseq", "date", "expires", "max-forwards", "organization", "server", "subject", "timestamp", "user-agent", "service-route" };
        static Dictionary<string, string> _short = new Dictionary<string, string> { {"u","allow-events"},{"i","call-id"},{"m","contact"},
            {"e","content-encoding"},{"l","content-length"},{"c",  "content-type"},{"o",  "event"},{"f", "from"},{"s",  "subject"},{"k","supported"},{"t","to"},{"v",  "via"}};
        static Dictionary<string, string> _exceptions = new Dictionary<string, string> { { "call-id", "Call-ID" }, { "cseq", "CSeq" }, { "www-authenticate", "WWW-Authenticate" } };

        public Dictionary<string, string> attributes { get; set; }
        public string header_type { get; set; }
        public object value { get; set; }
        public string authMethod { get; set; }
        public int number { get; set; }
        public string method { get; set; }
        public string name { get; set; }
        public SIPURI viaUri { get; set; }

        public static string _canon(string input)
        {
            input = input.ToLower();
            if ((input.Length == 1) && _short.Keys.Contains(input))
            {
                return _canon(_short[input]);
            }
            else if (_exceptions.Keys.Contains(input))
            {
                return _exceptions[input];
            }
            else
            {
                string[] words = input.Split('-');
                for (int i = 0; i < words.Length; i++)
                {
                    words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i]);
                }
                return String.Join("-", words);
            }
        }
        public static string _quote(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                return input;
            }
            else
            {
                return "\"" + input + "\"";
            }

        }
        public static string _unquote(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                return input.Substring(1, input.Length - 2);
            }
            else
            {
                return input;
            }
        }

        public Header(string value, string name)
        {
            this.number = -1;
            this.attributes = new Dictionary<string, string>();
            this.name = _canon(name.Trim());
            this.Parse(this.name, value.Trim());
        }

        public void Parse(string name, string value)
        {
            string rest = "";
            int index = 0;
            if (_address.Contains(name.ToLower()))
            {
                this.header_type = "address";
                Address addr = new Address();
                addr.mustQuote = true;
                int count = addr.Parse(value);
                this.value = addr;
                if (count < value.Length)
                    rest = value.Substring(count, value.Length - count);
                if (rest.Length > 0)
                {
                    foreach (string parm in rest.Split(';'))
                    {
                        if (parm.Contains('='))
                        {
                            index = parm.IndexOf('=');
                            string parm_name = parm.Substring(0, index);
                            string parm_value = parm.Substring(index + 1);
                            this.attributes.Add(parm_name, parm_value);
                        }
                    }
                }

            }
            else if (!(_comma.Contains(name.ToLower())) && !(_unstructured.Contains(name.ToLower())))
            {
                this.header_type = "standard";
                if (!value.Contains(";lr>"))
                {
                    if (value.Contains(';'))
                    {
                        index = value.IndexOf(';');
                        this.value = value.Substring(0, index);
                        string temp_str = value.Substring(index + 1).Trim();
                        foreach (string parm in temp_str.Split(';'))
                        {
                            if (parm.Contains('='))
                            {
                                index = parm.IndexOf('=');
                                string parm_name = parm.Substring(0, index);
                                string parm_value = parm.Substring(index + 1);
                                this.attributes.Add(parm_name, parm_value);
                            }
                        }
                    }
                    else
                    {
                        this.value = value;
                    }
                }
                else
                {
                    this.value = value;
                }
            }
            if (_comma.Contains(name.ToLower()))
            {
                this.header_type = "comma";
                if (value.Contains(' '))
                {
                    index = value.IndexOf(' ');
                    this.authMethod = value.Substring(0, index).Trim();
                    this.value = value.Substring(0, index).Trim();
                    string values = value.Substring(index + 1);
                    foreach (string parm in values.Split(','))
                    {
                        if (parm.Contains('='))
                        {
                            index = parm.IndexOf('=');
                            string parm_name = parm.Substring(0, index);
                            string parm_value = parm.Substring(index + 1);
                            this.attributes.Add(parm_name, parm_value);
                        }
                    }
                }
            }
            else if (name.ToLower() == "cseq")
            {
                this.header_type = "unstructured";
                string[] parts = value.Trim().Split(' ');
                int temp_number = -1;
                int.TryParse(parts[0], out temp_number);
                this.number = temp_number;
                this.method = parts[1];
            }
            if (_unstructured.Contains(name.ToLower()) && !(name.ToLower() == "cseq"))
            {
                this.header_type = "unstructured";
                this.value = value;
            }
            if (name.ToLower() == "via")
            {
                string[] parts = value.Split(' ');
                string proto = parts[0];
                string addr = parts[1].Split(';')[0];
                string type = proto.Split('/')[2].ToLower();
                this.viaUri = new SIPURI("sip:" + addr + ";transport=" + type);
                if (this.viaUri.port == 0)
                {
                    this.viaUri.port = 5060;
                }
                if (this.attributes.Keys.Contains("rport"))
                {
                    int temp_port = 5060;
                    int.TryParse(this.attributes["rport"], out temp_port);
                    this.viaUri.port = temp_port;
                }
                if ((type != "tcp") && (type != "sctp") && (type != "tls"))
                {
                    if (this.attributes.Keys.Contains("maddr"))
                    {
                        this.viaUri.host = this.attributes["maddr"];
                    }
                    else if (this.attributes.Keys.Contains("received"))
                    {
                        this.viaUri.host = this.attributes["received"];
                    }
                }
            }

        }

        public override string ToString()
        {
            string name = this.name.ToLower();
            StringBuilder sb = new StringBuilder();
            sb.Append(this.value);
            if (!(this.header_type == "comma") && !(this.header_type == "unstructured"))
            {
                foreach (KeyValuePair<string, string> kvp in this.attributes)
                {
                    sb.Append(";");
                    sb.Append(kvp.Key + "=" + kvp.Value);
                }
            }
            if ((this.header_type == "comma"))
            {
                sb.Append(" ");
                foreach (KeyValuePair<string, string> kvp in this.attributes)
                {

                    sb.Append(kvp.Key + "=" + kvp.Value);
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
            }
            if ((this.number > -1))
                sb.Append(" " + this.number.ToString());
            if (this.method != null)
                sb.Append(" " + this.method);
            return sb.ToString();
        }

        public string Repr()
        {
            return this.name + ":" + this.ToString();
        }

        public Header Dup()
        {
            return new Header(this.ToString(), this.name);
        }

        public static List<Header> CreateHeaders(string value)
        {
            int index = value.IndexOf(':');
            string name = value.Substring(0, index);
            value = value.Substring(index + 1);
            List<Header> headers = new List<Header>();
            //if (name == "WWW-Authenticate")
            //{
            //    foreach(string part in value.Split(','))
            //    {
            //        headers.Add(new Header(part.Trim(),name));
            //    }
            //}
            if (name == "Record-Route")
            {
                foreach (string part in value.Split(','))
                {
                    headers.Add(new Header(part.Trim(), name));
                }
            }
            else
            {
                headers.Add(new Header(value.Trim(), name));
            }
            return headers;
        }
    }


}
