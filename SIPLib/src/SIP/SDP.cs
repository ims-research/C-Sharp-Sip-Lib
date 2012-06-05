using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib.SIP
{
    public class SDP
    {

        public static string Multiple = "tramb";
        public List<SDPMedia> Media { get; set; }
        public SDPConnection Connection { get; set; }
        public SDPOriginator Originator { get; set; }
        public Dictionary<string, string> Other { get; set; }


        public SDP(string sdp = null)
        {
            Media = new List<SDPMedia>();
            Other = new Dictionary<string, string>();

            if (sdp != null)
            {
                Parse(sdp);
            }
        }

        public void Parse(string sdp)
        {
            sdp = sdp.Replace("\r\n", "\n");
            sdp = sdp.Replace("\r", "\n");
            sdp = sdp.Trim();
            foreach (String line in sdp.Split('\n'))
            {
                //Per line parsing
                string[] values = line.Split("=".ToCharArray(), 2);
                string k = values[0];
                string currentObject = "";
                switch (k)
                {
                    case "o":
                        Originator = new SDPOriginator(values[1]);
                        currentObject = Originator.ToString();
                        break;
                    case "c":
                        Connection = new SDPConnection(values[1]);
                        currentObject = Connection.ToString();
                        break;
                    case "m":
                        {
                            SDPMedia currentMedia = new SDPMedia(values[1]);
                            Media.Add(currentMedia);
                            currentObject = currentMedia.ToString();
                        }
                        break;
                    default:
                        currentObject = values[1];
                        break;
                }

                if (k == "m")
                {
                    SDPMedia obj = Media.Last();
                }
                else if (Media.Count > 0)
                {
                    SDPMedia obj = Media.Last();
                    if (k == "a" && values[1].StartsWith("rtpmap:"))
                    {
                        string[] split = values[1].Remove(0, 7).Split(" ".ToCharArray(), 2);
                        string pt = split[0];
                        string[] rest = split[1].Split("/".ToCharArray(), 2);
                        string name = rest[0];

                        string[] final = rest[1].Split("/".ToCharArray(), 2);
                        string rate = final[0];
                        string paramaters = null;
                        if (final.Length > 1)
                        {
                            paramaters = final[1];
                        }

                        foreach (SDPMediaFormat f in obj.Mediaformats)
                        {
                            if (f.Pt == pt)
                            {
                                f.Name = name;
                                f.Rate = rate;
                                if (paramaters != null)
                                {
                                    f.Parameters = paramaters;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!Multiple.Contains(k))
                        {
                            obj.OtherAttributes.Add(k, currentObject);
                        }
                        else
                        {
                            obj.OtherAttributes.Add(k, currentObject);
                            //TODO HANDLE multiple attributes of the same type;
                            //if (obj.properties.ContainsKey(k))
                            //{
                            //    obj[k, true].Add(current_attribute);
                            //}
                            //else
                            //{
                            //    obj[k, true] = new List<AttributeClass>();
                            //    obj[k, true].Add(current_attribute);
                            //}
                        }
                    }
                }
                else
                {
                    if (k != "o" && k != "c")
                    {
                        SDP obj = this;
                        if (!Multiple.Contains(k))
                        {
                            obj.Other.Add(k, currentObject);
                        }
                        else
                        {
                            obj.Other.Add(k, currentObject);
                            //TODO HANDLE multiple attributes of the same type;
                            //if (obj.properties.ContainsKey(k))
                            //{
                            //    obj[k, true].Add(current_attribute);
                            //}
                            //else
                            //{
                            //    obj[k, true] = new List<AttributeClass>();
                            //    obj[k, true].Add(current_attribute);
                            //}
                        }
                    }

                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Char c in "vosiuepcbtam")
            {
                if (Other.ContainsKey(c.ToString()))
                {
                    if (!Multiple.Contains(c))
                    {
                        sb.Append(c + "=" + Other[c.ToString()] + "\r\n");
                    }
                    else
                    {
                        sb.Append(c + "=" + Other[c.ToString()] + "\r\n");
                        // TODO: handle multiple lines of the same
                        //foreach (AttributeClass a in this[c.ToString(), true])
                        //{
                        //    sb.Append(c + "=" + a.value + "\r\n");
                        //}
                    }
                }
                else if (c == 'c')
                {
                    sb.Append(c + "=" + Connection.ToString() + "\r\n");
                }
                else if (c == 'o')
                {
                    sb.Append(c + "=" + Originator.ToString() + "\r\n");
                }
                else if (c == 'm')
                {
                    foreach (SDPMedia m in Media)
                    {
                        sb.Append(c + "=" + m.ToString() + "\r\n");
                    }
                }

            }
            return sb.ToString();
        }

        public static SDP CreateOffer(List<SDPMedia> streams, Dictionary<string, string> parameters, SDP previous = null)
        {
            SDP s = new SDP();
            s.Other["v"] = "0";
            foreach (Char a in "iep")
            {
                if (parameters.ContainsKey(a.ToString()))
                {
                    s.Other[a.ToString()] = parameters[a.ToString()];
                }
            }
            if (previous != null && previous.Originator != null)
            {
                s.Originator = new SDPOriginator(previous.Originator.ToString());
                s.Originator.Version = s.Originator.Version + 1;
            }
            s.Other["s"] = "-";
            s.Other["t"] = "0";
            s.Media = streams;
            return s;
        }

        public static SDP CreateAnswer(List<SDPMedia> streams, SDP offer, Dictionary<string, string> parameters = null)
        {
            SDP s = new SDP();
            s.Other["v"] = "0";
            foreach (Char a in "iep")
            {
                if (parameters.ContainsKey(a.ToString()))
                {
                    s.Other[a.ToString()] = parameters[a.ToString()];
                }
            }
            s.Originator = new SDPOriginator();
            s.Other["s"] = "-";
            s.Other["t"] = offer.Other["t"];
            foreach (SDPMedia yourMedia in offer.Media)
            {
                SDPMedia myMedia = null;
                foreach (SDPMedia t in streams)
                {
                    if (t.Media != yourMedia.Media) continue;
                    myMedia = new SDPMedia(t.ToString());
                    //streams.RemoveAt(i);
                    List<KeyValuePair<SDPMediaFormat, SDPMediaFormat>> found = new List<KeyValuePair<SDPMediaFormat, SDPMediaFormat>>();
                    foreach (SDPMediaFormat yourmf in yourMedia.Mediaformats)
                    {
                        foreach (SDPMediaFormat mymf in myMedia.Mediaformats)
                        {

                            int mymfpt = -1;
                            int yourmfpt = -1;
                            try
                            {
                                mymfpt = Int32.Parse(mymf.Pt);
                                yourmfpt = Int32.Parse(yourmf.Pt);
                            }
                            catch (Exception)
                            {

                                mymfpt = -1;
                                yourmfpt = -1;
                            }
                            if ((0 <= mymfpt && mymfpt < 32 && 0 <= yourmfpt && yourmfpt <= 32 && mymfpt == yourmfpt)
                                || (mymfpt < 0 && yourmfpt < 0 && mymfpt == yourmfpt)
                                || (mymf.Name == yourmf.Name && mymf.Rate == yourmf.Rate && mymf.Count == yourmf.Count))
                            {
                                found.Add(new KeyValuePair<SDPMediaFormat, SDPMediaFormat>(yourmf, mymf)); break;
                            }

                        }
                    }
                    if (found.Count > 0)
                    {
                        foreach (KeyValuePair<SDPMediaFormat, SDPMediaFormat> kvp in found)
                        {
                            myMedia.Mediaformats.Add(kvp.Key);
                        }
                    }
                    else
                    {
                        myMedia.Mediaformats.Clear();
                        SDPMediaFormat temp = new SDPMediaFormat {Pt = "0"};
                        myMedia.Mediaformats.Add(temp);
                        myMedia.Port = "0";
                    }
                }
                if (myMedia == null)
                {
                    myMedia = new SDPMedia(yourMedia.ToString()) {Port = "0"};
                }
                s.Media.Add(myMedia);
            }
            bool valid = s.Media.Any(myMedia => myMedia.Port != "0");
            return valid ? s : null;
        }
    }
}
