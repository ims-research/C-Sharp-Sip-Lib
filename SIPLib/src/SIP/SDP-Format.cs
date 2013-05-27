// ***********************************************************************
// Assembly         : SIPLib
// Author           : Richard Spiers
// Created          : 10-25-2012
//
// Last Modified By : Richard Spiers
// Last Modified On : 05-27-2013
// ***********************************************************************
// <copyright file="SDP-Format.cs">
//     Copyright (c) Richard Spiers. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace SIPLib.SIP
{
    /// <summary>
    /// This class is used to represent SDP Media Format data. The interpretation of the media format depends on the value of the proto sub-field of the corresponding media line.
    /// </summary>
    public class SDPMediaFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SIPLib.SIP.SDPMediaFormat" /> class.
        /// </summary>
        public SDPMediaFormat()
        {
            Pt = "";
            Name = "";
            Rate = "";
            Parameters = "";
            Count = 0;
        }

        /// <summary>
        /// Gets or sets the pt.
        /// </summary>
        /// <value>The pt.</value>
        public string Pt { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the rate.
        /// </summary>
        /// <value>The rate.</value>
        public string Rate { get; set; }
        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public string Parameters { get; set; }
        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; set; }
    }
}