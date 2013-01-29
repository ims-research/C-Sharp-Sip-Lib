#region

using System.Collections.Generic;
using SIPLib.SIP;

#endregion

namespace SIPLib.src.SIP
{
    internal class ProxyBranch
    {
        public Message CancelRequest;
        public List<SIPURI> RemoteCandidates;
        public Message Request;
        public Message Response;
        public Transaction Transaction;

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