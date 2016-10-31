using NUnit.Framework;
using SIPLib.SIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPLib.SIP.Tests
{
    [TestFixture()]
    public class SIPURITests
    {
        [Test()]
        public void SIPURITest()
        {
            List<string> inputs = new List<string>();
            inputs.Add("sip:alice@open-ims.test");
            inputs.Add("sip:192.168.20.25:7202;lr");
            inputs.Add("sip:anonymous@anonymous.invalid");
            inputs.Add("sip:anonymous@192.168.20.25:7242");
            inputs.Add("sip:scim@open-ims.test");

            foreach (string address in inputs)
            {
                SIPURI original = new SIPURI(address, true);
                SIPURI second = new SIPURI(address, false);
                Assert.AreEqual(original.ToString(), second.ToString());
            }
        }
    }
}