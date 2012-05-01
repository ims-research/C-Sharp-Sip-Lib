using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIPLib;
namespace SIPLib
{
    public class Events
    {

    }

    public class SipMessageEventArgs : EventArgs
    {
        public Message message;
        public UserAgent ua;

        public SipMessageEventArgs(Message transferred_message)
        {
            this.message = transferred_message;
        }

        public SipMessageEventArgs(Message transferred_message,UserAgent ua)
        {
            this.message = transferred_message;
            this.ua = ua;
        }

    }

    public class StackErrorEventArgs : EventArgs
    {
        public string function;
        public Exception exception;

        public StackErrorEventArgs(string Function, Exception e)
        {
            this.function = Function;
            this.exception = e;
        }
    }

    public class RegistrationChangedEventArgs : EventArgs
    {
        public string state;
        public Message message;

        public RegistrationChangedEventArgs(string state, Message message)
        {
            this.state = state;
            this.message = message;
        }
    }

    public class RawEventArgs : EventArgs
    {
        public string data;
        public string[] src;

        public RawEventArgs(string data,string[] src)
        {
            this.data = data;
            this.src = src;
        }
    }
}
