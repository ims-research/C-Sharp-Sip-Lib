// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard
// Created          : 10-25-2012
//
// Last Modified By : Richard
// Last Modified On : 01-29-2013
// ***********************************************************************
// <copyright file="Timer.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#region

using System;

#endregion

namespace SIPLib.SIP
{
    /// <summary>
    /// Basic class used as a starting point for representing the timers indicated in the SIP RFC.
    /// </summary>
    public class Timer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.Timer"/> class with default time outs.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="T1">The t1.</param>
        /// <param name="T2">The t2.</param>
        /// <param name="T4">The t4.</param>
        public Timer(UserAgent app, int T1 = 500, int T2 = 4000, int T4 = 5000)
        {
            this.T1 = T1;
            this.T2 = T2;
            this.T4 = T4;
        }

        /// <summary>
        /// Gets or sets T1
        /// </summary>
        /// <value>T1.</value>
        public int T1 { get; set; }
        /// <summary>
        /// Gets or sets T2.
        /// </summary>
        /// <value>T2.</value>
        public int T2 { get; set; }
        /// <summary>
        /// Gets or sets T4.
        /// </summary>
        /// <value>The T4.</value>
        public int T4 { get; set; }
        /// <summary>
        /// Gets or sets the delay.
        /// </summary>
        /// <value>The delay.</value>
        public int Delay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:SIPLib.SIP.Timer"/> is running.
        /// </summary>
        /// <value><c>true</c> if running; otherwise, <c>false</c>.</value>
        public bool Running { get; set; }

        /// <summary>
        /// Return appropriate timer for timer named A.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int A()
        {
            return T1;
        }

        /// <summary>
        /// Return appropriate timer for timer named B.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int B()
        {
            return 64*T1;
        }

        /// <summary>
        /// Return appropriate timer for timer named D.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int D()
        {
            return Math.Max(64*T1, 32000);
        }

        /// <summary>
        /// Return appropriate timer for timer named E.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int E()
        {
            return A();
        }

        /// <summary>
        /// Return appropriate timer for timer named F.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int F()
        {
            return B();
        }


        /// <summary>
        /// Return appropriate timer for timer named G.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int G()
        {
            return A();
        }

        /// <summary>
        /// Return appropriate timer for timer named H.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int H()
        {
            return B();
        }

        /// <summary>
        /// Return appropriate timer for timer named I.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int I()
        {
            return T4;
        }

        /// <summary>
        /// Return appropriate timer for timer named J.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int J()
        {
            return B();
        }

        /// <summary>
        /// Return appropriate timer for timer named K.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int K()
        {
            return I();
        }

        /// <summary>
        /// Unused stub.
        /// </summary>
        /// <param name="delay">The delay.</param>
        public void Start(int delay = 0)
        {
        }

        /// <summary>
        /// Unused stub.
        /// </summary>
        /// <param name="delay">The delay.</param>
        public void Stop(int delay = 0)
        {
        }
    }
}