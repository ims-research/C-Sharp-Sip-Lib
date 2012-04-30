using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib.src.SIP
{
    public enum StatusCodes
    {
        Informational, Successful, Redirection,
        ClientFailure, ServerFailure, GlobalFailure, Unknown
    }

    public enum SipMethods
    {
        REGISTER, INVITE, ACK, BYE, CANCEL, MESSAGE, OPTIONS, PRACK,
        SUBSCRIBE, NOTIFY, PUBLISH, INFO, REFER, UPDATE
    }

    public enum CallState
    {
        Starting, Calling, Ringing, Queued, WaitingForAccept, Active, Ending, Ended
    }
}
