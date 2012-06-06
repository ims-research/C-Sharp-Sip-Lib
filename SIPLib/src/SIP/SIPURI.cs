using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace SIPLib.SIP
{
    public class SIPURI
    {
        public string Scheme { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public Dictionary<string,string> Parameters { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public SIPURI(string uri)
        {
            Init();
            const string regEx = @"^(?<scheme>[a-zA-Z][a-zA-Z0-9\+\-\.]*):(((?<user>[a-zA-Z0-9\-_\.\!\~\*\'\(\)&=\+\$,;\?\/\%]+)(:(?<password>[^:@;\?]+))?)@)?(((?<host>[^;\?:]*)(:(?<port>[\d]+))?))(;(?<params>[^\?]*))?(\?(?<headers>.*))?$";
            Regex exp = new Regex(regEx,RegexOptions.IgnoreCase);

            MatchCollection mc = exp.Matches(uri);
            string param = "";
            string head = "";
            foreach (Match m in mc)
            {
                Scheme = m.Groups["scheme"].ToString();
                User = m.Groups["user"].ToString();
                Password = m.Groups["password"].ToString();
                Host = m.Groups["host"].ToString();
                int tempPort = 0;
                int.TryParse(m.Groups["port"].ToString(), out tempPort);
                Port = tempPort;
                param = m.Groups["params"].ToString();
                head = m.Groups["headers"].ToString();
                
            }
            if ((Scheme == "tel") && (User==""))
            {
                User = Host;
                Host = null;
            }
            foreach (string paramater in param.Split(';'))
            {
                if (paramater.Contains('='))
                {
                    int index = paramater.IndexOf('=');
                    string paramName = paramater.Substring(0,index);
                    string paramValue = paramater.Substring(index+1);
                    Parameters.Add(paramName,paramValue);
                }
                else if (paramater.ToLower() == "lr")
                {
                    Parameters.Add(paramater,"");
                }
                else break;
            }
            foreach (string header in head.Split('&'))
            {
                if (header.Contains('='))
                {
                    int index = header.IndexOf('=');
                    string headerName = header.Substring(0, index);
                    string headerValue = header.Substring(index + 1);
                    Headers.Add(headerName, headerValue);
                }
                else
                    break;
            }
        }
        public SIPURI()
        {
            Init();
        }
        private void Init()
        {
            Parameters = new Dictionary<string, string>();
            Headers = new Dictionary<string, string>();

            Scheme = null; 
            User = null; 
            Password = null; 
            Host = null;
            Port = 0; 
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string host;
            string user;
            if (!String.IsNullOrEmpty(Scheme) && Scheme.ToLower() == "tel")
            {
                user = "";
                host = User;
            }
            else
            {
                user = User;
                host = Host;
            }
            if (!String.IsNullOrEmpty(Scheme))
            {
                sb.Append(Scheme + ":");
                if (user.Length > 0)
                {
                    sb.Append(user);
                    if (Password.Length > 0)
                        sb.Append(":" + Password);
                    sb.Append("@");
                }
                if (host.Length > 0)
                {
                    sb.Append(host);
                    if (Port != 0)
                        sb.Append(":" + Port.ToString());
                }
                if (Parameters.Count > 0)
                {
                    sb.Append(";");
                    foreach (KeyValuePair<string, string> kvp in Parameters)
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
                if (Headers.Count > 0)
                {
                    sb.Append("?");
                    foreach (KeyValuePair<string, string> kvp in Headers)
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
            return new SIPURI(ToString());
        }

        public string Hash()
        {
            MD5 m = MD5.Create();
            string hash = GetMd5Hash(m, ToString().ToLower());
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
            return ((ToString().ToLower()) == (other.ToString().ToLower()));
        }

        public string HostPort()
        {
            return this.Host + ":" + Port.ToString();
        }
    }
}
