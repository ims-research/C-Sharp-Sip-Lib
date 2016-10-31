// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 06-06-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 02-02-2013
// ***********************************************************************
// <copyright file="Message.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
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
    /// <summary>
    /// This class is used to represent a SIP message (both request and response messages).
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The _keywords used for special handling.
        /// </summary>
        private static string[] _keywords = {"method", "uri", "response", "responsetext", "protocol", "_body", "body"};

        /// <summary>
        /// Headers that should only occur once in a SIP message.
        /// </summary>
        private static string[] _single =
            {
                "call-id", "content-disposition", "content-length", "content-type", "cseq", "date", "expires", "event",
                "max-forwards",
                "organization", "refer-to", "referred-by", "server", "session-expires", "subject", "timestamp", "to",
                "user-agent"
            };

        /// <summary>
        /// The status code type (Informational, Successful, ClientFailure etc.)
        /// </summary>
        public StatusCodes StatusCodeType = StatusCodes.Unknown;
        /// <summary>
        /// The private variable holding the body of the SIP message
        /// </summary>
        private string _body = "";
        /// <summary>
        /// Indication of loose routing.
        /// </summary>
        public bool had_lr;

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="T:SIPLib.SIP.Message"/> class.
        /// </summary>
        public Message()
        {
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.Message"/> class based on a input string.
        /// </summary>
        /// <param name="value">The SIP message as a string.</param>
        public Message(string value)
        {
            Init();
            Parse(value);
        }

        /// <summary>
        /// Gets or sets the response code (200 etc.)
        /// </summary>
        /// <value>The response code.</value>
        public int ResponseCode { get; set; }
        /// <summary>
        /// Gets or sets the response text (OK etc.)
        /// </summary>
        /// <value>The response text.</value>
        public string ResponseText { get; set; }
        /// <summary>
        /// Gets or sets the protocol.
        /// </summary>
        /// <value>The protocol.</value>
        public string Protocol { get; set; }
        /// <summary>
        /// Gets or sets the SIP method (INVITE etc.)
        /// </summary>
        /// <value>The method.</value>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the SIP body contents. Updates the "Content-Length" header based on the body's length.
        /// </summary>
        /// <value>The body.</value>
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

        /// <summary>
        /// Gets or sets the SIP URI of the message
        /// </summary>
        /// <value>The URI.</value>
        public SIPURI Uri { get; set; }
        /// <summary>
        /// Gets or sets the SIP message headers - this is a list of a list of headers as some headers can be repeated more than once.
        /// </summary>
        /// <value>The headers.</value>
        public Dictionary<string, List<Header>> Headers { get; set; }

        /// <summary>
        /// Initilises the Message private variables.
        /// </summary>
        private void Init()
        {
            Headers = new Dictionary<string, List<Header>>();
        }

        /// <summary>
        /// Parses the input string into a SIP message object.
        /// </summary>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Creates the multi line representation of the via header list.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>System.String.</returns>
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

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance. For easy printing / reading.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            try
            {
				foreach (KeyValuePair<string, List<Header>> keyValuePair in Headers.ToList())
				{
					if (keyValuePair.Value.Count <= 0)
					{
						Headers.Remove(keyValuePair.Key);
					}
				}
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
						// Get stack trace for the exception with source file information
						var st = new StackTrace(ex, true);
						// Get the top stack frame
						var frame = st.GetFrame(0);
						// Get the line number from the stack frame
						var line = frame.GetFileLineNumber();
						Debug.Assert(false, String.Format("1 Error converting message to string {0} \n {1} \n {2}", ex, line, Headers));
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
				Debug.Assert(false, String.Format("2 Error converting message to string {0} \n {1} \n {2}", ex, Headers.ToList(), Headers.Values.ToList() ));
            }
            return "Error converting Message";
        }

        /// <summary>
        /// Helper function to return the first instance of a particular header.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Header.</returns>
        public Header First(string name)
        {
            if (Headers.ContainsKey(name))
            {
                return Headers[name][0];
            }
            else return null;
        }

        /// <summary>
        /// Creates a clone of this Header object.
        /// </summary>
        /// <returns>Message.</returns>
        public Message Dup()
        {
            return new Message(ToString());
        }

        /// <summary>
        /// Inserts a header into the SIP message by either replacing the existing header, appending it at the end of its own header list, or inserting at the front of its own header list depending on the method specified.
        /// </summary>
        /// <param name="header">The header to insert.</param>
        /// <param name="method">The method - "replace", "append" or "insert". "replace" is the default. </param>
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

        /// <summary>
        /// Helper function to indicate type of SIP response message (Informational, Successful, ClientFailure etc.)
        /// </summary>
        /// <returns><c>true</c> if SIP response code is 1XX, <c>false</c> otherwise</returns>
        public bool Is1XX()
        {
            return (ResponseCode/100 == 1);
        }

        /// <summary>
        /// Helper function to indicate type of SIP response message (Informational, Successful, ClientFailure etc.)
        /// </summary>
        /// <returns><c>true</c> if 2XX, <c>false</c> otherwise</returns>
        public bool Is2XX()
        {
            return (ResponseCode/100 == 2);
        }

        /// <summary>
        /// Helper function to indicate type of SIP response message (Informational, Successful, ClientFailure etc.)
        /// </summary>
        /// <returns><c>true</c> if 3XX, <c>false</c> otherwise</returns>
        public bool Is3XX()
        {
            return (ResponseCode/100 == 3);
        }

        /// <summary>
        /// Helper function to indicate type of SIP response message (Informational, Successful, ClientFailure etc.)
        /// </summary>
        /// <returns><c>true</c> if 4XX, <c>false</c> otherwise</returns>
        public bool Is4XX()
        {
            return (ResponseCode/100 == 4);
        }

        /// <summary>
        /// Helper function to indicate type of SIP response message (Informational, Successful, ClientFailure etc.)
        /// </summary>
        /// <returns><c>true</c> if 5XX, <c>false</c> otherwise</returns>
        public bool Is5XX()
        {
            return (ResponseCode/100 == 5);
        }

        /// <summary>
        /// Helper function to indicate type of SIP response message (Informational, Successful, ClientFailure etc.)
        /// </summary>
        /// <returns><c>true</c> if 6XX, <c>false</c> otherwise</returns>
        public bool Is6XX()
        {
            return (ResponseCode/100 == 6);
        }

        /// <summary>
        /// Helper function to indicate type of SIP response message (Informational, Successful, ClientFailure etc.)
        /// </summary>
        /// <returns><c>true</c> if 7XX, <c>false</c> otherwise</returns>
        public bool Is7XX()
        {
            return (ResponseCode/100 == 7);
        }

        /// <summary>
        /// Determines whether this instance is final (response code >= 200).
        /// </summary>
        /// <returns><c>true</c> if this instance is final; otherwise, <c>false</c>.</returns>
        public bool IsFinal()
        {
            return (ResponseCode >= 200);
        }

        /// <summary>
        /// Populates the message - inserts the input list of headers into the message
        /// </summary>
        /// <param name="m">The message being populated.</param>
        /// <param name="headers">The headers to insert.</param>
        /// <param name="content">The SIP body content.</param>
        /// <returns>Message.</returns>
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

        /// <summary>
        /// Creates a SIP request message based on the passed in parameters.
        /// </summary>
        /// <param name="method">The SIP method to use.</param>
        /// <param name="uri">The destination URI used in the first line.</param>
        /// <param name="headers">The SIP headers.</param>
        /// <param name="content">The SIP body content.</param>
        /// <returns>Message.</returns>
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

        /// <summary>
        /// Creates a SIP response based on the passed in parameters.
        /// </summary>
        /// <param name="responseCode">The response code (200 etc.)</param>
        /// <param name="responseText">The response text (OK etc.)</param>
        /// <param name="headers">The SIP headers.</param>
        /// <param name="content">The SIP body content.</param>
        /// <param name="originalRequest">The original request.</param>
        /// <returns>Message.</returns>
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