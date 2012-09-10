namespace SIPLib.SIP
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
        Starting, Calling, Ringing, Queued, WaitingForAccept, Active, Ending, Ended, Inactive
    }
    public class Types
    {
        public static StatusCodes GetStatusType(int statusCode)
        {

            if ((statusCode) >= 100 && (statusCode) < 200)
            {
                return StatusCodes.Informational;
            }
            if ((statusCode) >= 200 && (statusCode) < 300)
            {
                return StatusCodes.Successful;
            }
            if ((statusCode) >= 300 && (statusCode) < 400)
            {
                return StatusCodes.Redirection;
            }
            if ((statusCode) >= 400 && (statusCode) < 500)
            {
                return StatusCodes.ClientFailure;
            }
            if ((statusCode) >= 500 && (statusCode) < 600)
            {
                return StatusCodes.ServerFailure;
            }
            if ((statusCode) >= 600 && (statusCode) < 700)
            {
                return StatusCodes.GlobalFailure;
            }
            return StatusCodes.Unknown;
        }
    }

    
}

