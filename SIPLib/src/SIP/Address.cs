using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SIPLib
{
    public class Address
    {
        public string displayName { get; set; }
        public SIPURI uri { get; set; }
        public bool wildcard { get; set; }
        public bool mustQuote { get; set; }
        public Address()
        {
            Init();
        }

        public Address(string address)
        {
            Init();
            Parse(address);
        }

        private void Init()
        {
            this.displayName = "";
            this.uri = new SIPURI();
            this.wildcard = false;
        }

        public int Parse(string address)
        {
            if (address.StartsWith("*"))
            {
                this.wildcard = true;
                return 1;
            }
            string[] reg_exs = { @"^(?<name>[a-zA-Z0-9\-\._\+\~\ \t]*)<(?<uri>[^>]+)>", @"^(""(?<name>[a-zA-Z0-9\-\._\+\~\ \t]+)"")[\ \t]*<(?<uri>[^>]+)>", @"^[\ \t]*(?<name>)(?<uri>[^;]+)" };

            foreach (string expression in reg_exs)
            {
                Regex exp = new Regex(expression, RegexOptions.IgnoreCase);
                MatchCollection mc = exp.Matches(address);
                foreach (Match m in mc)
                {
                    this.displayName = m.Groups["name"].ToString().Trim();
                    this.uri = new SIPURI(m.Groups["uri"].ToString().Trim());
                    return m.Length;
                }
            }
            return -1;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.displayName.Length > 0)
            {
                sb.Append("\"" + this.displayName + "\"");
                if (this.uri.ToString().Length > 0)
                {
                    sb.Append(" ");
                }
            }

            if (this.uri.ToString().Length > 0)
            {
                if ((this.mustQuote) || (this.displayName.Length > 0))
                {
                    sb.Append("<");
                }
                sb.Append(this.uri.ToString());
                if ((this.mustQuote) || (this.displayName.Length > 0))
                {
                    sb.Append(">");
                }
            }
            return sb.ToString();
        }

        public Address dup()
        {
            return new Address(this.ToString());
        }

        public string displayable()
        {
            string name = "";
            if (this.displayName.Length > 0)
            {
                name = this.displayName;
            }
            else if (this.uri.user.Length > 0)
            {
                name = this.uri.user;
            }
            else if (this.uri.host.Length > 0)
            {
                name = this.uri.host;
            }
            if (name.Length > 26)
            {
                name = name.Substring(0, 22);
                name = name + "...";
            }
            return name;
        }
    }
}
