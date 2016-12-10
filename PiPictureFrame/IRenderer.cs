
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;

namespace PiPictureFrame.Core
{
    /// <summary>
    /// This interface renders the picture to the screen.
    /// </summary>
    public interface IRenderer : IDisposable
    {
        // ---------------- Properties ----------------

        /// <summary>
        /// Returns the current picture path.
        /// </summary>
        string CurrentPicturePath { get; }

        // ---------------- Functions ----------------

        /// <summary>
        /// Inits the renderer.
        /// </summary>
        /// <param name="pictureDirectory">Where the picture directory is.</param>
        void Init( string pictureDirectory );

        /// <summary>
        /// Tells the renderer to go to the next picture.
        /// </summary>
        void GoToNextPicture();
    }
}
