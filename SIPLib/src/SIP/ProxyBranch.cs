using System.Collections.Generic;
using SIPLib.SIP;

namespace SIPLib.src.SIP
{
    class ProxyBranch
    {
        public Message Request;
        public Message Response;
        public List<SIPURI> RemoteCandidates;
        public Transaction Transaction;
        public Message CancelRequest;

        public ProxyBranch()
        {
            Request = null;
            Response = null;
            RemoteCandidates = null;
            Transaction = null;
            CancelRequest = null;
        }
    }
}
