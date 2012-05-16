using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
namespace SIPLib
{
    public class SIPURI
    {
        public string scheme { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string host { get; set; }
        public string IP { get; set; }
        public int port { get; set; }
        public Dictionary<string,string> parameters { get; set; }
        public Dictionary<string, string> headers { get; set; }

        public SIPURI(string URI)
        {
            Init();
            string reg_ex = @"^(?<scheme>[a-zA-Z][a-zA-Z0-9\+\-\.]*):(((?<user>[a-zA-Z0-9\-_\.\!\~\*\'\(\)&=\+\$,;\?\/\%]+)(:(?<password>[^:@;\?]+))?)@)?(((?<host>[^;\?:]*)(:(?<port>[\d]+))?))(;(?<params>[^\?]*))?(\?(?<headers>.*))?$";
            Regex exp = new Regex(reg_ex,RegexOptions.IgnoreCase);

            MatchCollection mc = exp.Matches(URI);
            string param = "";
            string head = "";
            foreach (Match m in mc)
            {
                this.scheme = m.Groups["scheme"].ToString();
                this.user = m.Groups["user"].ToString();
                this.password = m.Groups["password"].ToString();
                this.host = m.Groups["host"].ToString();
                int temp_port = 0;
                int.TryParse(m.Groups["port"].ToString(), out temp_port);
                this.port = temp_port;
                param = m.Groups["params"].ToString();
                head = m.Groups["headers"].ToString();
                
            }
            if ((this.scheme == "tel") && (this.user==""))
            {
                this.user = this.host;
                this.host = null;
            }
            foreach (string paramater in param.Split(';'))
            {
                int index = 0;
                if (paramater.Contains('='))
                {
                    index = paramater.IndexOf('=');
                    string param_name = paramater.Substring(0,index);
                    string param_value = paramater.Substring(index+1);
                    this.parameters.Add(param_name,param_value);
                }
                else if (paramater.ToLower() == "lr")
                {
                    this.parameters.Add(paramater,"");
                }
                else break;
            }
            foreach (string header in head.Split('&'))
            {
                int index = 0;
                if (header.Contains('='))
                {
                    index = header.IndexOf('=');
                    string header_name = header.Substring(0, index);
                    string header_value = header.Substring(index + 1);
                    this.headers.Add(header_name, header_value);
                }
                else break;
            }
        }
        public SIPURI()
        {
            Init();
        }
        private void Init()
        {
            this.parameters = new Dictionary<string, string>();
            this.headers = new Dictionary<string, string>();

            this.scheme = null; 
            this.user = null; 
            this.password = null; 
            this.host = null;
            this.port = 0; 
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string host ="";
            string user = "";
            if (this.scheme.ToLower() == "tel")
            {
                user = "";
                host = this.user;
            }
            else
            {
                user = this.user;
                host = this.host;
            }
            if (this.scheme.Length > 0)
            {
                sb.Append(this.scheme + ":");
                if (user.Length > 0)
                {
                    sb.Append(user);
                    if (this.password.Length > 0)
                        sb.Append(":" + this.password);
                    sb.Append("@");
                }
                if (host.Length > 0)
                {
                    sb.Append(host);
                    if (this.port != 0)
                        sb.Append(":" + this.port.ToString());
                }
                if (this.parameters.Count > 0)
                {
                    sb.Append(";");
                    foreach (KeyValuePair<string, string> kvp in this.parameters)
                    {
                        if (kvp.Key.ToLower() == "lr")
                        {
                            sb.Append(kvp.Key);
                        }
                        else
                        {
                            sb.Append(kvp.Key + "=" + kvp.Value);
                        }
                        sb.Append(";");
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
                if (this.headers.Count > 0)
                {
                    sb.Append("?");
                    foreach (KeyValuePair<string, string> kvp in this.headers)
                    {
                        sb.Append(kvp.Key + "=" + kvp.Value);
                        sb.Append("&");
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
            }
            return sb.ToString();
        }

        public SIPURI Dup()
        {
            return new SIPURI(this.ToString());
        }

        public string Hash()
        {
            MD5 m = MD5.Create();
            string hash = GetMd5Hash(m, this.ToString().ToLower());
            return hash;
        }
        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public bool Compare(SIPURI other)
        {
            return ((this.ToString().ToLower()) == (other.ToString().ToLower()));
        }

        public string HostPort()
        {
            return this.host + ":" + port.ToString();
        }
    }
}
