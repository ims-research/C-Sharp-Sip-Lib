// ***********************************************************************
// Assembly         : SIPLibDriver
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 10-25-2012
// ***********************************************************************
// <copyright file="Program.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using SIPLib;
using System.Net;
using SIPLib.SIP;
using SIPLib.Utils;

namespace SIPLibDriver
{
    /// <summary>
    /// Example program to demonstrate the use of the SIP library.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Creates a SIP stack.
        /// </summary>
        /// <param name="app">The associated sip application.</param>
        /// <param name="username">The username representing this SIP endpoint.</param>
        /// <param name="proxyIp">The proxy ip if any.</param>
        /// <param name="proxyPort">The proxy port if any.</param>
        /// <returns>SIPStack.</returns>
        public static SIPStack CreateStack(SIPApp app,string username, string proxyIp = null, int proxyPort = -1)
        {
            SIPStack myStack = new SIPStack(app) {Uri = {User = username}};
            if (proxyIp != null)
            {
                myStack.ProxyHost = proxyIp;
                myStack.ProxyPort = (proxyPort == -1) ? 5060 : proxyPort;
            }
            return myStack;
        }

        /// <summary>
        /// Creates the actual transport - UDP is supported, TCP needs work.
        /// </summary>
        /// <param name="listenIp">The listen ip.</param>
        /// <param name="listenPort">The listen port.</param>
        /// <returns>TransportInfo.</returns>
        public static TransportInfo CreateTransport(string listenIp, int listenPort)
        {
            return new TransportInfo(IPAddress.Parse(listenIp), listenPort, System.Net.Sockets.ProtocolType.Udp);
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">Arguments not needed</param>
        static void Main(string[] args)
        {
            // Create transport object that will listen on the following port on the best guessed local address
            TransportInfo localTransport = CreateTransport(Helpers.GetLocalIP(), 3420);
            // Can also specify an IP if you know the IP to be used.
            //TransportInfo localTransport = CreateTransport("192.168.20.28", 8989);
            
            // Create an object of your SIP handling logic with the created transport
            SIPApp app = new SIPApp(localTransport);

            // Create a SIP stack with the proxy address if needed
            SIPStack stack = CreateStack(app,"alice","192.168.20.248", 9000);
            
            // Register with the specified user URI
            app.Register("sip:alice@open-ims.test");

            // Simple pause so you can test each function / watch the SIP signalling
            Console.ReadKey();
            app.Invite("bob@open-ims.test");
            // Simple pause so you can test each function / watch the SIP signalling
            Console.ReadKey();

            // End the current call (presuming it was accepted)
            app.EndCurrentCall();
            // Send an example IM
            app.Message("bob@open-ims.test", "Hello, this is alice testing the SIP library");
            Console.ReadKey();
        }
    }
}
