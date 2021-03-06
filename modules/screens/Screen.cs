﻿//-----------------------------------------------------------------------
// <copyright file="Screen.cs" company="Gavin Kendall">
//     Copyright (c) 2020 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A class representing a region.</summary>
//-----------------------------------------------------------------------
using System;

namespace AutoScreenCapture
{
    /// <summary>
    /// A class representing a screen.
    /// </summary>
    public class Screen
    {
        /// <summary>
        /// A unique identifier for the view which this screen is associated with.
        /// </summary>
        public Guid ViewId { get; set; }

        /// <summary>
        /// The name of the screen.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The folder path for the screen capture.
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        /// The macro for the screen capture's filename.
        /// </summary>
        public string Macro { get; set; }

        /// <summary>
        /// The component (whether it be the active window or an available screen).
        /// </summary>
        public int Component { get; set; }

        /// <summary>
        /// The image format of the screen capture.
        /// </summary>
        public ImageFormat Format { get; set; }

        /// <summary>
        /// The JPEG quality of the screen capture.
        /// </summary>
        public int JpegQuality { get; set; }

        /// <summary>
        /// The resolution ratio of the screen capture.
        /// </summary>
        public int ResolutionRatio { get; set; }

        /// <summary>
        /// Determines if we include the mouse pointer in the screen capture.
        /// </summary>
        public bool Mouse { get; set; }

        /// <summary>
        /// Determines if the screen is active or inactive.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Determines if we should consider the active window title upon capture.
        /// </summary>
        public bool ActiveWindowTitleCaptureCheck { get; set; }

        /// <summary>
        /// The text to find in the active window title.
        /// </summary>
        public string ActiveWindowTitleCaptureText { get; set; } 

        /// <summary>
        /// The empty constructor of the screen.
        /// </summary>
        public Screen()
        {

        }
    }
}