using System.Text;
using System.Text.RegularExpressions;

namespace SIPLib.SIP
{
    public class Address
    {
        public string DisplayName { get; set; }
        public SIPURI Uri { get; set; }
        public bool Wildcard { get; set; }
        public bool MustQuote { get; set; }
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
            DisplayName = "";
            Uri = new SIPURI();
            Wildcard = false;
        }

        public int Parse(string address)
        {
            if (address.StartsWith("*"))
            {
                Wildcard = true;
                return 1;
            }
            string[] regExs = { @"^(?<name>[a-zA-Z0-9\-\._\+\~\ \t]*)<(?<uri>[^>]+)>", @"^(""(?<name>[a-zA-Z0-9\-\._\+\~\ \t]+)"")[\ \t]*<(?<uri>[^>]+)>", @"^[\ \t]*(?<name>)(?<uri>[^;]+)" };

            foreach (string expression in regExs)
            {
                Regex exp = new Regex(expression, RegexOptions.IgnoreCase);
                MatchCollection mc = exp.Matches(address);
                foreach (Match m in mc)
                {
                    DisplayName = m.Groups["name"].ToString().Trim();
                    Uri = new SIPURI(m.Groups["uri"].ToString().Trim());
                    return m.Length;
                }
            }
            return -1;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (DisplayName.Length > 0)
            {
                sb.Append("\"" + DisplayName + "\"");
                if (Uri.ToString().Length > 0)
                {
                    sb.Append(" ");
                }
            }

            if (Uri.ToString().Length > 0)
            {
                if ((MustQuote) || (DisplayName.Length > 0))
                {
                    sb.Append("<");
                }
                sb.Append(Uri.ToString());
                if ((MustQuote) || (DisplayName.Length > 0))
                {
                    sb.Append(">");
                }
            }
            return sb.ToString();
        }

        public Address Dup()
        {
            return new Address(ToString());
        }

        public string Displayable()
        {
            string name = "";
            if (DisplayName.Length > 0)
            {
                name = DisplayName;
            }
            else if (Uri.User.Length > 0)
            {
                name = Uri.User;
            }
            else if (Uri.Host.Length > 0)
            {
                name = Uri.Host;
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
