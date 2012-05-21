using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class SDP
    {

        public const string _multiple = "tramb";
        public List<SDPMedia> Media { get; set; }
        public SDPConnection Connection { get; set; }
        public SDPOriginator Originator { get; set; }
        public Dictionary<string, string> Other { get; set; }


        public SDP(string sdp = null)
        {
            this.Media = new List<SDPMedia>();
            this.Other = new Dictionary<string, string>();

            if (sdp != null)
            {
                this._parse(sdp);
            }
        }

        public void _parse(string sdp)
        {
            sdp = sdp.Replace("\r\n", "\n");
            sdp = sdp.Replace("\r", "\n");
            sdp = sdp.Trim();
            string current_object = "";
            foreach (String line in sdp.Split('\n'))
            {
                //Per line parsing
                string[] values = line.Split("=".ToCharArray(), 2);
                string k = values[0];
                if (k == "o")
                {
                    this.Originator = new SDPOriginator(values[1]);
                    current_object = this.Originator.ToString();
                }
                else if (k == "c")
                {
                    this.Connection = new SDPConnection(values[1]);
                    current_object = this.Connection.ToString();
                }
                else if (k == "m")
                {
                    SDPMedia current_media = new SDPMedia(values[1]);
                    this.Media.Add(current_media);
                    current_object = current_media.ToString();
                }
                else
                {
                    //this.Other.Add(k, values[1]);
                    current_object = values[1];
                }

                if (k == "m")
                {
                    SDPMedia obj = this.Media.Last();
                }
                else if (this.Media.Count > 0)
                {
                    SDPMedia obj = this.Media.Last();
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

                        foreach (SDPMediaFormat f in obj.mediaformats)
                        {
                            if (f.pt == pt)
                            {
                                f.name = name;
                                f.rate = rate;
                                if (paramaters != null)
                                {
                                    f.parameters = paramaters;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!SDP._multiple.Contains(k))
                        {
                            obj.other_attributes.Add(k, current_object);
                        }
                        else
                        {
                            obj.other_attributes.Add(k, current_object);
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
                        if (!SDP._multiple.Contains(k))
                        {
                            obj.Other.Add(k, current_object);
                        }
                        else
                        {
                            obj.Other.Add(k, current_object);
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

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Char c in "vosiuepcbtam")
            {
                if (this.Other.ContainsKey(c.ToString()))
                {
                    if (!SDP._multiple.Contains(c))
                    {
                        sb.Append(c + "=" + this.Other[c.ToString()] + "\r\n");
                    }
                    else
                    {
                        sb.Append(c + "=" + this.Other[c.ToString()] + "\r\n");
                        // TODO: handle multiple lines of the same
                        //foreach (AttributeClass a in this[c.ToString(), true])
                        //{
                        //    sb.Append(c + "=" + a.value + "\r\n");
                        //}
                    }
                }
                else if (c == 'c')
                {
                    sb.Append(c + "=" + this.Connection.ToString() + "\r\n");
                }
                else if (c == 'o')
                {
                    sb.Append(c + "=" + this.Originator.ToString() + "\r\n");
                }
                else if (c == 'm')
                {
                    foreach (SDPMedia m in this.Media)
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
                s.Originator.version = s.Originator.version + 1;
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
            foreach (SDPMedia your_media in offer.Media)
            {
                SDPMedia my_media = null;
                for (int i = 0; i < streams.Count; i++)
                {
                    if (streams[i].media == your_media.media)
                    {
                        my_media = new SDPMedia(streams[i].ToString());
                        //streams.RemoveAt(i);
                        List<KeyValuePair<SDPMediaFormat, SDPMediaFormat>> found = new List<KeyValuePair<SDPMediaFormat, SDPMediaFormat>>();
                        foreach (SDPMediaFormat yourmf in your_media.mediaformats)
                        {
                            foreach (SDPMediaFormat mymf in my_media.mediaformats)
                            {

                                int mymfpt = -1;
                                int yourmfpt = -1;
                                try
                                {
                                    mymfpt = Int32.Parse(mymf.pt);
                                    yourmfpt = Int32.Parse(yourmf.pt);
                                }
                                catch (Exception)
                                {

                                    mymfpt = -1;
                                    yourmfpt = -1;
                                }
                                if ((0 <= mymfpt && mymfpt < 32 && 0 <= yourmfpt && yourmfpt <= 32 && mymfpt == yourmfpt)
                                    || (mymfpt < 0 && yourmfpt < 0 && mymfpt == yourmfpt)
                                    || (mymf.name == yourmf.name && mymf.rate == yourmf.rate && mymf.count == yourmf.count))
                                {
                                    found.Add(new KeyValuePair<SDPMediaFormat, SDPMediaFormat>(yourmf, mymf)); break;
                                }

                            }
                        }
                        if (found.Count > 0)
                        {
                            foreach (KeyValuePair<SDPMediaFormat, SDPMediaFormat> kvp in found)
                            {
                                my_media.mediaformats.Add(kvp.Key);
                            }
                        }
                        else
                        {
                            my_media.mediaformats.Clear();
                            SDPMediaFormat temp = new SDPMediaFormat();
                            temp.pt = "0";
                            my_media.mediaformats.Add(temp);
                            my_media.port = "0";
                        }
                    }
                }
                if (my_media == null)
                {
                    my_media = new SDPMedia(your_media.ToString());
                    my_media.port = "0";
                }
                s.Media.Add(my_media);
            }
            bool valid = false;
            foreach (SDPMedia my_media in s.Media)
            {
                if (my_media.port != "0")
                {
                    valid = true;
                    break;
                }
            }
            if (valid)
            {
                return s;
            }
            else
            {
                return null;
            }
        }
    }
}
