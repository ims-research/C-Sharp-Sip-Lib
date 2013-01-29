#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace SIPLib.SIP
{
    public class Message
    {
        private static string[] _keywords = {"method", "uri", "response", "responsetext", "protocol", "_body", "body"};

        private static string[] _single =
            {
                "call-id", "content-disposition", "content-length", "content-type", "cseq", "date", "expires", "event",
                "max-forwards",
                "organization", "refer-to", "referred-by", "server", "session-expires", "subject", "timestamp", "to",
                "user-agent"
            };

        public StatusCodes StatusCodeType = StatusCodes.Unknown;
        private string _body = "";
        public bool had_lr;

        public Message()
        {
            Init();
        }

        public Message(string value)
        {
            Init();
            Parse(value);
        }

        public int ResponseCode { get; set; }
        public string ResponseText { get; set; }
        public string Protocol { get; set; }
        public string Method { get; set; }

        public string Body
        {
            get { return _body; }
            set
            {
                _body = value;
                if (Headers.ContainsKey("Content-Length"))
                {
                    Headers["Content-Length"][0] = new Header(value.Length.ToString() + "\r\n", "Content-Length");
                }
                else
                {
                    List<Header> headers = new List<Header>
                        {
                            new Header(value.Length.ToString() + "\r\n", "Content-Length")
                        };
                    Headers.Add("Content-Length", headers);
                }
            }
        }

        public SIPURI Uri { get; set; }
        public Dictionary<string, List<Header>> Headers { get; set; }

        private void Init()
        {
            Headers = new Dictionary<string, List<Header>>();
        }

        public void Parse(string value)
        {
            int index = 0;
            index = value.IndexOf("\r\n\r\n");
            string body = "";
            string firstheaders = "";
            if (index == -1)
            {
                firstheaders = value;
                Debug.Assert(false, String.Format("No message body, assuming empty\n{0}\n", value));
            }
            else
            {
                firstheaders = value.Substring(0, index);
                body = value.Substring(index + 1).Trim();
            }
            index = firstheaders.IndexOf("\r\n");
            string firstline = firstheaders.Substring(0, index);
            string headers = firstheaders.Substring(index + 2);
            string[] parts = firstline.Split(" ".ToCharArray(), 3);
            if (parts.Length < 3)
            {
                Debug.Assert(false, String.Format("First line has less than 3 parts \n{0}\n", firstline));
            }
            int tempResponseCode = 0;

            if (int.TryParse(parts[1], out tempResponseCode))
            {
                ResponseCode = tempResponseCode;
                ResponseText = parts[2];
                Protocol = parts[0];
                StatusCodeType = Types.GetStatusType(ResponseCode);
            }
            else
            {
                Method = parts[0];
                Uri = new SIPURI(parts[1]);
                Protocol = parts[2];
            }
            string[] stringSeparators = new[] {"\r\n"};
            foreach (string bh in headers.Split(stringSeparators, StringSplitOptions.None))
            {
                string h = bh.Trim();
                if (Regex.IsMatch(h, @"^\s"))
                {
                    break;
                }
                try
                {
                    if (!h.StartsWith("Warning:"))
                    {
                        List<Header> createdHeaders = Header.CreateHeaders(h);
                        string name = createdHeaders[0].Name;
                        if (Headers.ContainsKey(name))
                        {
                            Headers[name].AddRange(createdHeaders);
                        }
                        else
                        {
                            Headers.Add(name, createdHeaders);
                        }
                    }
                }
                catch (Exception exp)
                {
                    Debug.Assert(false, String.Format("Error parsing header {0}\n with error\n{1}", h, exp.Message));
                    break;
                }
            }
            int bodylength = 0;
            if (Headers.ContainsKey("Content-Length"))
            {
                bodylength = Convert.ToInt32(First("Content-Length").Value);
            }
            if (body.Length > 0)
            {
                Body = body;
            }
            Debug.Assert(Math.Abs(body.Length - bodylength) < 3,
                         String.Format("Invalid content-length {0} != {1}\n", body.Length, bodylength));
            string[] mandatoryHeaders = {"To", "From", "CSeq", "Call-ID"};
            foreach (string s in mandatoryHeaders)
            {
                if (!Headers.ContainsKey(s))
                {
                    Debug.Assert(false, String.Format("Mandatory header missing {0}\n", s));
                }
            }
        }

        private string HandleVia(List<Header> headers)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Header h in headers)
            {
                sb.Append("Via: ");
                sb.Append(h);
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            foreach (KeyValuePair<string, List<Header>> keyValuePair in Headers.ToList())
            {
                if (keyValuePair.Value.Count <= 0)
                {
                    Headers.Remove(keyValuePair.Key);
                }
            }
            try
            {
                string m = "";
                if (Method != null)
                {
                    m = Method + " " + Uri + " " + Protocol + "\r\n";
                }
                else if (ResponseText.Length > 0)
                {
                    m = Protocol + " " + ResponseCode.ToString() + " " + ResponseText + "\r\n";
                }
                string contentLength = "";
                foreach (List<Header> headers in Headers.Values.ToList())
                {
                    try
                    {
                        if (headers.First().Name == "Via")
                        {
                            m = m + HandleVia(headers);
                        }
                        else
                        {
                            if (headers.Count > 0)
                            {
                                string current = headers[0].Name + ": ";
                                foreach (Header h in headers)
                                {
                                    current = current + h + ", ";
                                }
                                current = current.Remove(current.Length - 2);
                                current = current + "\r\n";
                                if (current.ToLower().Contains("content-length"))
                                {
                                    contentLength = current;
                                }
                                else m = m + current;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Assert(false, String.Format("Error converting message to string {0}", ex.Message));
                    }
                }
                m = m + contentLength;
                m = m + "\r\n";
                if (Body.Length > 0)
                {
                    m = m + Body;
                }
                return m;
            }
            catch (Exception ex)
            {
                Debug.Assert(false, String.Format("Error converting message to string {0}", ex.Message));
            }
            return "Error converting Message";
        }

        public Header First(string name)
        {
            if (Headers.ContainsKey(name))
            {
                return Headers[name][0];
            }
            else return null;
        }

        public Message Dup()
        {
            return new Message(ToString());
        }

        public void InsertHeader(Header header, string method = "replace")
        {
            string name = header.Name;
            if (Headers.ContainsKey(name))
            {
                switch (method)
                {
                    case "append":
                        {
                            Headers[name].Add(header);
                            break;
                        }
                    case "replace":
                        {
                            List<Header> headers = new List<Header> {header};
                            Headers[name] = headers;
                            break;
                        }
                    case "insert":
                        {
                            Headers[name].Insert(0, header);
                            break;
                        }
                }
            }
            else
            {
                List<Header> headers = new List<Header> {header};
                Headers[name] = headers;
            }
        }

        public bool Is1XX()
        {
            return (ResponseCode/100 == 1);
        }

        public bool Is2XX()
        {
            return (ResponseCode/100 == 2);
        }

        public bool Is3XX()
        {
            return (ResponseCode/100 == 3);
        }

        public bool Is4XX()
        {
            return (ResponseCode/100 == 4);
        }

        public bool Is5XX()
        {
            return (ResponseCode/100 == 5);
        }

        public bool Is6XX()
        {
            return (ResponseCode/100 == 6);
        }

        public bool Is7XX()
        {
            return (ResponseCode/100 == 7);
        }

        public bool IsFinal()
        {
            return (ResponseCode >= 200);
        }

        public static Message PopulateMessage(Message m, Dictionary<string, List<Header>> headers = null,
                                              string content = "")
        {
            if (headers != null)
            {
                foreach (List<Header> header in headers.Values)
                {
                    foreach (Header h in header)
                    {
                        m.InsertHeader(h, "append");
                    }
                }
            }
            if (!string.IsNullOrEmpty(content))
            {
                m.Body = content;
            }
            else
            {
                if (m.Headers.ContainsKey("Content-Length"))
                {
                    m.Headers["Content-Length"][0] = new Header("0" + "\r\n", "Content-Length");
                }
                else
                {
                    List<Header> newheaders = new List<Header> {new Header("0" + "\r\n", "Content-Length")};
                    m.Headers.Add("Content-Length", newheaders);
                }
            }
            return m;
        }

        public static Message CreateRequest(string method, SIPURI uri, Dictionary<string, List<Header>> headers = null,
                                            string content = "")
        {
            Message m = new Message {Method = method, Uri = uri, Protocol = "SIP/2.0"};
            m = PopulateMessage(m, headers, content);
            if (m.Headers.ContainsKey("CSeq"))
            {
                Header cseq = new Header(m.First("CSeq").Number.ToString() + " " + method, "CSeq");
                List<Header> cseqHeaders = new List<Header> {cseq};
                m.Headers["CSeq"] = cseqHeaders;
            }
            return m;
        }

        public static Message CreateResponse(int responseCode, string responseText,
                                             Dictionary<string, List<Header>> headers = null, string content = "",
                                             Message originalRequest = null)
        {
            Message m = new Message {ResponseCode = responseCode, ResponseText = responseText, Protocol = "SIP/2.0"};
            if (originalRequest != null)
            {
                m.Headers["To"] = originalRequest.Headers["To"];
                m.Headers["From"] = originalRequest.Headers["From"];
                m.Headers["CSeq"] = originalRequest.Headers["CSeq"];
                m.Headers["Call-ID"] = originalRequest.Headers["Call-ID"];
                m.Headers["Via"] = originalRequest.Headers["Via"];
                if (originalRequest.Headers.ContainsKey("Route"))
                {
                    //Todo check this
                    //m.Headers["Route"] = originalRequest.Headers["Route"];
                }

                if (responseCode == 100 && m.Headers.ContainsKey("Timestamp"))
                {
                    m.Headers["Timestamp"] = originalRequest.Headers["Timestamp"];
                }
            }
            return PopulateMessage(m, headers, content);
        }
    }
}