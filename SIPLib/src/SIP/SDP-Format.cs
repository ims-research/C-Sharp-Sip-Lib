namespace SIPLib.SIP
{
    public class SDPMediaFormat
    {
        public SDPMediaFormat()
        {
            Pt = "";
            Name = "";
            Rate = "";
            Parameters = "";
            Count = 0;
        }

        public string Pt { get; set; }
        public string Name { get; set; }
        public string Rate { get; set; }
        public string Parameters { get; set; }
        public int Count { get; set; }
    }
}