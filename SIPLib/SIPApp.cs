#region

using System;
using SIPLib.SIP;

#endregion

namespace SIPLib
{
    public abstract class SIPApp
    {
        public abstract TransportInfo Transport { get; set; }

        public abstract SIPStack Stack { get; set; }
        public abstract string[] Authenticate(UserAgent ua, Header header, SIPStack sipStack);

        public abstract void DialogCreated(Dialog dialog, UserAgent ua, SIPStack sipStack);

        public abstract void Cancelled(UserAgent ua, Message request, SIPStack sipStack);

        public abstract void ReceivedResponse(UserAgent ua, Message response, SIPStack sipStack);

        public abstract void ReceivedRequest(UserAgent ua, Message request, SIPStack sipStack);

        public abstract void Sending(UserAgent ua, Message message, SIPStack sipStack);

        public abstract UserAgent CreateServer(Message request, SIPURI uri, SIPStack sipStack);

        public abstract Timer CreateTimer(UserAgent obj, SIPStack sipStack);

        public abstract void Send(string finalData, string destinationHost, int destinationPort, SIPStack sipStack);

        public virtual event EventHandler<RawEventArgs> ReceivedDataEvent;
    }
}