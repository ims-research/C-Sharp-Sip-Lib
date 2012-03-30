using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class Timer
    {
        public int T1 { get; set; }
        public int T2 { get; set; }
        public int T4 { get; set; }

        public Timer(UserAgent app, int T1 = 500, int T2 = 4000, int T4 = 5000)
        {
            this.T1 = T1;
            this.T2 = T2;
            this.T4 = T4;
        }

        public int A()
        {
            return this.T1;
        }

        public int B()
        {
            return 64*this.T1;
        }

        public int D()
        {
            return Math.Max(64 * this.T1, 32000);
        }

        public int E()
        {
            return A();
        }

        public int F()
        {
            return B();
        }


        public int G()
        {
            return A();
        }

        public int H()
        {
            return B();
        }

        public int I()
        {
            return this.T4;
        }

        public int J()
        {
            return B();
        }

        public int K()
        {
            return I();
        }

        public void start(int delay = 0)
        {

        }
        public void stop(int delay = 0)
        {

        }

        public int delay { get; set; }

        public bool running { get; set; }
    }
}
