#region

using System;

#endregion

namespace SIPLib.SIP
{
    public class Events
    {
    }

    public class SipMessageEventArgs : EventArgs
    {
        public Message Message;
        public UserAgent UA;

        public SipMessageEventArgs(Message transferredMessage)
        {
            Message = transferredMessage;
        }

        public SipMessageEventArgs(Message transferredMessage, UserAgent ua)
        {
            Message = transferredMessage;
            UA = ua;
        }
    }

    public class StackErrorEventArgs : EventArgs
    {
        public Exception Exception;
        public string Function;

        public StackErrorEventArgs(string inputFunction, Exception e)
        {
            Function = inputFunction;
            Exception = e;
        }
    }

    public class RegistrationChangedEventArgs : EventArgs
    {
        public Message Message;
        public string State;

        public RegistrationChangedEventArgs(string s, Message m)
        {
            State = s;
            Message = m;
        }
    }

    public class RawEventArgs : EventArgs
    {
        public string Data;
        public bool Sent;
        public string[] Src;

        public RawEventArgs(string d, string[] s, bool sent)
        {
            Data = d;
            Src = s;
            Sent = sent;
        }
    }
}