using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using SIPLib.src.SIP;

namespace SIPLib
{
    public class Message
    {
        static string[] _keywords = { "method", "uri", "response", "responsetext", "protocol", "_body", "body" };
        static string[] _single = {"call-id", "content-disposition", "content-length", "content-type", "cseq", "date", "expires", "event", "max-forwards",
"organization", "refer-to", "referred-by", "server", "session-expires", "subject", "timestamp", "to", "user-agent"};

        public int response_code { get; set; }
        public string response_text { get; set; }
        public StatusCodes status_code_type = StatusCodes.Unknown;
        public string protocol { get; set; }
        public string method { get; set; }
        public string _body = "";
        public string body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                if (this.headers.ContainsKey("Content-Length"))
                {
                    this.headers["Content-Length"][0] = new Header(value.Length.ToString() + "\r\n", "Content-Length");
                }
                else
                {
                    List<Header> headers = new List<Header>();
                    headers.Add(new Header(value.Length.ToString() + "\r\n", "Content-Length"));
                    this.headers.Add("Content-Length", headers);
                }
            }
        }
        public SIPURI uri { get; set; }
        public Dictionary<string, List<Header>> headers { get; set; }

        private void Init()
        {
            this.headers = new Dictionary<string, List<Header>>();
        }

        public Message()
        {
            Init();
        }

        public Message(string value)
        {
            Init();
            this._parse(value);
        }

        public void _parse(string value)
        {
            int index = 0;
            index = value.IndexOf("\r\n\r\n");
            string body = "";
            string firstheaders = "";
            if (index == -1)
            {
                Debug.Assert(false, String.Format("No message body, assuming empty\n{0}\n", value));
                firstheaders = value;
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
            int temp_response_code = 0;

            if (int.TryParse(parts[1], out temp_response_code))
            {
                this.response_code = temp_response_code;
                this.response_text = parts[2];
                this.protocol = parts[0];
                this.status_code_type = Types.GetStatusType(response_code);
            }
            else
            {
                this.method = parts[0];
                this.uri = new SIPURI(parts[1]);
                this.protocol = parts[2];
            }
            string[] stringSeparators = new string[] { "\r\n" };
            foreach (string h in headers.Split(stringSeparators, StringSplitOptions.None))
            {
                if (Regex.IsMatch(h, @"^\s"))
                {
                    break;
                }
                else
                {
                    try
                    {
                        if (!h.StartsWith("Warning:"))
                        {
                        List<Header> createdHeaders = Header.createHeaders(h);
                        string name = createdHeaders[0].name;
                        if (this.headers.ContainsKey(name))
                        {
                            this.headers[name].AddRange(createdHeaders);
                        }
                        else
                        {
                            this.headers.Add(name, createdHeaders);
                        }
                        }
                    }
                    catch (Exception exp)
                    {
                        Debug.Assert(false, String.Format("Error parsing header {0}\n with error\n{1}",h,exp.Message));
                        break;
                    }
                }
            }
            int bodylength = 0;
            if (this.headers.ContainsKey("Content-Length"))
            {
                bodylength = Convert.ToInt32(this.first("Content-Length").value);
            }
            if (body.Length > 0)
            {
                this.body = body;
            }
            Debug.Assert(Math.Abs(body.Length - bodylength)<3, String.Format("Invalid content-length {0} != {1}\n", body.Length, bodylength));
            string[] mandatoryHeaders = { "To", "From", "CSeq", "Call-ID" };
            foreach (string s in mandatoryHeaders)
            {
                if (!this.headers.ContainsKey(s))
                {
                    Debug.Assert(false, String.Format("Mandatory header missing {0}\n", s));
                }
            }
        }

        public string ToString()
        {
            string m = "";
            if (this.method != null)
            {
                m = this.method + " " + this.uri.ToString() + " " + this.protocol + "\r\n";
            }
            else if (this.response_text.Length > 0)
            {
                m = this.protocol + " " + this.response_code.ToString() + " " + this.response_text + "\r\n";
            }
            string content_length = "";
            foreach (List<Header> headers in this.headers.Values)
            {
                if (headers.Count > 0)
                {
                    string current = "";
                    foreach (Header h in headers)
                    {
                        current = current + h.repr() + "\n";
                    }
                    current = current.Remove(current.Length - 1);
                    current = current + "\r\n";
                    if (current.ToLower().Contains("content-length"))
                    {
                        content_length = current;
                    }
                    else m = m + current;
                }
            }
            m = m + content_length;
            m = m + "\r\n";
            if (this.body.Length > 0)
            {
                m = m + this.body;
            }
            return m;
        }

        public Header first(string name)
        {
            return this.headers[name][0];
        }
        public Message dup()
        {
            return new Message(this.ToString());
        }

        public void insertHeader(Header header, string method = "replace")
        {
            string name = header.name;
            if (this.headers.ContainsKey(name))
            {
                switch (method)
                {
                    case "append":
                        {
                            this.headers[name].Add(header);
                            break;
                        }
                    case "replace":
                        {
                            List<Header> headers = new List<Header>();
                            headers.Add(header);
                            this.headers[name] = headers;
                            break;
                        }
                    case "insert":
                        {
                            this.headers[name].Insert(0, header);
                            break;
                        }
                    default:
                        break;
                }
            }
            else
            {
                List<Header> headers = new List<Header>();
                headers.Add(header);
                this.headers[name] = headers;
            }
        }

        public bool is1xx()
        {
            return (this.response_code / 100 == 1);
        }

        public bool is2xx()
        {
            return (this.response_code / 100 == 2);
        }
        public bool is3xx()
        {
            return (this.response_code / 100 == 3);
        }
        public bool is4xx()
        {
            return (this.response_code / 100 == 4);
        }
        public bool is5xx()
        {
            return (this.response_code / 100 == 5);
        }
        public bool is6xx()
        {
            return (this.response_code / 100 == 6);
        }
        public bool is7xx()
        {
            return (this.response_code / 100 == 7);
        }

        public bool isFinal()
        {
            return (this.response_code >= 200);
        }

        public static Message populateMessage(Message m, Dictionary<string, List<Header>> headers = null, string content = "")
        {
            if (headers != null)
            {
                foreach (List<Header> header in headers.Values)
                {
                    foreach (Header h in header)
                    {
                        m.insertHeader(h,"append");
                    }
                }
            }
            if (content !=null && content.Length > 0)
            {
                m.body = content;
            }
            else
            {
                if (m.headers.ContainsKey("Content-Length"))
                {
                    m.headers["Content-Length"][0] = new Header("0" + "\r\n", "Content-Length");
                }
                else
                {
                    List<Header> newheaders = new List<Header>();
                    newheaders.Add(new Header("0" + "\r\n", "Content-Length"));
                    m.headers.Add("Content-Length", newheaders);
                }

            }
            return m;
        }

        public static Message createRequest(string method, SIPURI uri, Dictionary<string, List<Header>> headers = null, string content = "")
        {
            Message m = new Message();
            m.method = method;
            m.uri = uri;
            m.protocol = "SIP/2.0";
            m = Message.populateMessage(m, headers, content);
            if (m.headers.ContainsKey("CSeq"))
            {
                Header cseq = new Header(m.first("CSeq").number.ToString() + " " + method, "CSeq");
                List<Header> cseq_headers = new List<Header>();
                cseq_headers.Add(cseq);
                m.headers["CSeq"] = cseq_headers;
            }
            return m;
        }

        public static Message createResponse(int response_code, string response_text, Dictionary<string, List<Header>> headers = null, string content = "", Message original_request = null)
        {
            Message m = new Message();
            m.response_code = response_code;
            m.response_text = response_text;
            m.protocol = "SIP/2.0";
            if (original_request != null)
            {
                m.headers["To"] = original_request.headers["To"];
                m.headers["From"] = original_request.headers["From"];
                m.headers["CSeq"] = original_request.headers["CSeq"];
                m.headers["Call-ID"] = original_request.headers["Call-ID"];
                m.headers["Via"] = original_request.headers["Via"];
                if (response_code == 100 && m.headers.ContainsKey("Timestamp"))
                {
                    m.headers["Timestamp"] = original_request.headers["Timestamp"];
                }
            }
            return Message.populateMessage(m, headers, content);
        }

    }
}
