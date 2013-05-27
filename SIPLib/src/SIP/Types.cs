// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 01-29-2013
// ***********************************************************************
// <copyright file="Types.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace SIPLib.SIP
{
    /// <summary>
    /// Enum used to represent classes of status codes.
    /// </summary>
    public enum StatusCodes
    {
        Informational,
        Successful,
        Redirection,
        ClientFailure,
        ServerFailure,
        GlobalFailure,
        Unknown
    }

    /// <summary>
    /// Enum used to represent possible SIP Methods
    /// </summary>
    public enum SipMethods
    {
        REGISTER,
        INVITE,
        ACK,
        BYE,
        CANCEL,
        MESSAGE,
        OPTIONS,
        PRACK,
        SUBSCRIBE,
        NOTIFY,
        PUBLISH,
        INFO,
        REFER,
        UPDATE
    }

    /// <summary>
    /// Enum used to represent state of calls.
    /// </summary>
    public enum CallState
    {
        Starting,
        Calling,
        Ringing,
        Queued,
        WaitingForAccept,
        Active,
        Ending,
        Ended,
        Inactive
    }

    /// <summary>
    /// Helper class used to provide static methods for working with SIP status / response codes.
    /// </summary>
    public class Types
    {
        /// <summary>
        /// Gets the type of response based on response code.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <returns>StatusCodes.</returns>
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