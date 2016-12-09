
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
        /// <summary>
        /// Inits the renderer.
        /// </summary>
        void Init();

        /// <summary>
        /// Shows the picture to the screen.
        /// </summary>
        /// <param name="lastPicturePath">
        /// The path of the previous picture (in case you need to clean things up).
        /// This can be null if there was none.
        /// </param>
        /// <param name="currentPicturePath">The path of the picture to show.</param>
        void ShowPicture( string lastPicturePath, string currentPicturePath );
    }
}
