using System;

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

        public SipMessageEventArgs(Message transferredMessage,UserAgent ua)
        {
            Message = transferredMessage;
            UA = ua;
        }

    }

    public class StackErrorEventArgs : EventArgs
    {
        public string Function;
        public Exception Exception;

        public StackErrorEventArgs(string inputFunction, Exception e)
        {
            Function = inputFunction;
            Exception = e;
        }
    }

    public class RegistrationChangedEventArgs : EventArgs
    {
        public string State;
        public Message Message;

        public RegistrationChangedEventArgs(string s, Message m)
        {
            State = s;
            Message = m;
        }
    }

    public class RawEventArgs : EventArgs
    {
        public string Data;
        public string[] Src;

        public RawEventArgs(string d,string[] s)
        {
            Data = d;
            Src = s;
        }
    }
}
