using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class SDPMediaFormat
    {
        public string pt { get; set; }
        public string name { get; set; }
        public string rate { get; set; }
        public string parameters { get; set; }
        public int  count { get; set; }

        public SDPMediaFormat()
        {
            this.pt = "";
            this.name = "";
            this.rate = "";
            this.parameters = "";
            this.count = 0;

        }
    }
}
